using GearCrossroads.Api.Data;
using GearCrossroads.Api.Models;
using GearCrossroads.Api.DTOs;
using GearCrossroads.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearCrossroads.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IContentModerationService _contentModeration;
        private readonly ICloudinaryService _cloudinaryService;

        public ItemsController(ApplicationDbContext context, IContentModerationService contentModeration, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _contentModeration = contentModeration;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.Items.ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Items
                .Include(i => i.SetupItems)
                .ThenInclude(si => si.Setup)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
                return NotFound();

            string? currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            bool canEdit = false;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                canEdit = item.SetupItems.Any(si => si.Setup != null && si.Setup.UserId == currentUserId);
            }
            return Ok(new
            {
                item.Id,
                item.Name,
                item.Description,
                item.ImageUrl,
                setupItems = item.SetupItems.Select(si => new { si.SetupId }).ToList(),
                canEdit
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(Item item)
        {
            // Content moderation - check item name and description
            var nameCheck = await _contentModeration.ModerateTextAsync(item.Name);
            if (!nameCheck.IsClean)
            {
                return BadRequest(new
                {
                    message = "The item name contains inappropriate content and cannot be posted. Please review our Terms of Service.",
                    violations = nameCheck.ViolationReasons
                });
            }

            var descriptionCheck = await _contentModeration.ModerateTextAsync(item.Description ?? "");
            if (!descriptionCheck.IsClean)
            {
                return BadRequest(new
                {
                    message = "The item description contains inappropriate content and cannot be posted. Please review our Terms of Service.",
                    violations = descriptionCheck.ViolationReasons
                });
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        private async Task<bool> UserOwnsAnySetupWithItem(int itemId)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return false;
            return await _context.SetupItems
                .Include(si => si.Setup)
                .AnyAsync(si => si.ItemId == itemId && si.Setup != null && si.Setup.UserId == userId);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Item updated)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            if (!await UserOwnsAnySetupWithItem(id))
                return Forbid();

            // Content moderation - check updated name and description
            var nameCheck = await _contentModeration.ModerateTextAsync(updated.Name);
            if (!nameCheck.IsClean)
            {
                return BadRequest(new
                {
                    message = "The item name contains inappropriate content and cannot be updated. Please review our Terms of Service.",
                    violations = nameCheck.ViolationReasons
                });
            }

            var descriptionCheck = await _contentModeration.ModerateTextAsync(updated.Description ?? "");
            if (!descriptionCheck.IsClean)
            {
                return BadRequest(new
                {
                    message = "The item description contains inappropriate content and cannot be updated. Please review our Terms of Service.",
                    violations = descriptionCheck.ViolationReasons
                });
            }

            item.Name = updated.Name;
            item.Description = updated.Description;
            // Do not overwrite ImageUrl during metadata updates; image changes are handled via /image endpoints

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items
                .Include(i => i.SetupItems)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null)
                return NotFound();

            if (!await UserOwnsAnySetupWithItem(id))
                return Forbid();

            _context.SetupItems.RemoveRange(item.SetupItems);
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpPut("{id}/image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile image)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();

            if (!await UserOwnsAnySetupWithItem(id))
                return Forbid();

            if (image == null || image.Length == 0)
                return BadRequest("No image uploaded.");

            // Check file size (10MB limit for Cloudinary free tier - warn users)
            if (image.Length > 10_485_760)
                return BadRequest("Image file size must be less than 10MB.");

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (!allowed.Contains(ext))
                return BadRequest("Invalid file type. Only jpg, jpeg, png, gif allowed.");

            // Upload to Cloudinary
            var imageUrl = await _cloudinaryService.UploadImageAsync(image, "item-images");
            item.ImageUrl = imageUrl;
            await _context.SaveChangesAsync();

            return Ok(new { item.ImageUrl });
        }

        [Authorize]
        [HttpDelete("{id}/image")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return NotFound();

            if (!await UserOwnsAnySetupWithItem(id))
                return Forbid();

            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
            {
                try
                {
                    // Extract public ID from Cloudinary URL for deletion
                    var uri = new Uri(item.ImageUrl);
                    var segments = uri.AbsolutePath.Split('/');
                    var uploadIndex = Array.IndexOf(segments, "upload");
                    if (uploadIndex >= 0 && uploadIndex < segments.Length - 1)
                    {
                        var pathAfterUpload = string.Join("/", segments.Skip(uploadIndex + 1));
                        var publicId = pathAfterUpload.Substring(0, pathAfterUpload.LastIndexOf('.'));
                        await _cloudinaryService.DeleteImageAsync(publicId);
                    }
                }
                catch { /* best-effort delete; ignore errors */ }
            }

            item.ImageUrl = null;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> CreateWithImage([FromForm] ItemCreateWithImageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var item = new Item
            {
                Name = dto.Name,
                Description = dto.Description
            };

            if (dto.Image != null && dto.Image.Length > 0)
            {
                // Check file size (10MB limit for Cloudinary free tier - warn users)
                if (dto.Image.Length > 10_485_760)
                    return BadRequest("Image file size must be less than 10MB.");

                var ext = Path.GetExtension(dto.Image.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowed.Contains(ext))
                    return BadRequest("Invalid file type. Only jpg, jpeg, png, gif allowed.");

                // Upload to Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(dto.Image, "item-images");
                item.ImageUrl = imageUrl;
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
    }
}
