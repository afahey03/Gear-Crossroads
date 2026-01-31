using GearCrossroads.Api.Data;
using GearCrossroads.Api.Models;
using GearCrossroads.Api.DTOs;
using GearCrossroads.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearCrossroads.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SetupsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContentModerationService _contentModeration;
        private readonly ICloudinaryService _cloudinaryService;

        private static readonly string[] AllowedCategories = new[]
        {
            "Photography","Gaming","Climbing","Music","Streaming","Magic: The Gathering","Disc Golf","Fishing/Tackle",
            "Podcasting","Woodworking","Cooking","Cycling","Running","Art","Work/Office","Home/Desk","Other"
        };

        public SetupsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IContentModerationService contentModeration,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _userManager = userManager;
            _contentModeration = contentModeration;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var setups = await _context.Setups
                .Include(s => s.SetupItems)
                    .ThenInclude(si => si.Item)
                .Include(s => s.Votes)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Category,
                    s.ImageUrl,
                    VoteCount = s.Votes.Count(),
                    Items = s.SetupItems.Select(si => new
                    {
                        si.Item.Id,
                        si.Item.Name
                    })
                })
                .ToListAsync();

            return Ok(setups);
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var setups = await _context.Setups
                .Where(s => s.UserId == user.Id)
                .Include(s => s.SetupItems).ThenInclude(si => si.Item)
                .Include(s => s.Votes)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Category,
                    s.ImageUrl,
                    VoteCount = s.Votes.Count(),
                    Items = s.SetupItems.Select(si => new { si.Item.Id, si.Item.Name })
                })
                .ToListAsync();

            return Ok(setups);
        }

        [HttpGet("categories")]
        public IActionResult GetCategories() => Ok(AllowedCategories);

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var setup = await _context.Setups
                .Include(s => s.SetupItems)
                    .ThenInclude(si => si.Item)
                .Include(s => s.SetupTags)
                    .ThenInclude(st => st.Tag)
                .Include(s => s.Votes)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (setup == null)
                return NotFound();

            var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var hasVoted = currentUserId != null && setup.Votes.Any(v => v.UserId == currentUserId);

            return Ok(new
            {
                setup.Id,
                setup.Title,
                setup.Description,
                setup.Category,
                setup.ImageUrl,
                setup.UserId,
                setup.CreatedAt,
                VoteCount = setup.Votes.Count,
                HasVoted = hasVoted,
                User = setup.User != null ? new
                {
                    setup.User.Email,
                    setup.User.AvatarUrl
                } : null,
                Items = setup.SetupItems.Select(si => new
                {
                    si.Item.Id,
                    si.Item.Name,
                    si.Item.Description,
                    si.Item.ImageUrl
                }).ToList(),
                Tags = setup.SetupTags.Select(st => new
                {
                    st.Tag.Id,
                    st.Tag.Name
                }).ToList()
            });
        }

        [Authorize]
        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Create([FromForm] CreateSetupDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Content moderation - check title and description
            var titleCheck = await _contentModeration.ModerateTextAsync(dto.Title);
            if (!titleCheck.IsClean)
            {
                return BadRequest(new
                {
                    message = "Your setup title contains inappropriate content and cannot be posted. Please review our Terms of Service.",
                    violations = titleCheck.ViolationReasons
                });
            }

            var descriptionCheck = await _contentModeration.ModerateTextAsync(dto.Description ?? "");
            if (!descriptionCheck.IsClean)
            {
                return BadRequest(new
                {
                    message = "Your setup description contains inappropriate content and cannot be posted. Please review our Terms of Service.",
                    violations = descriptionCheck.ViolationReasons
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Category) ||
                !AllowedCategories.Contains(dto.Category, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest($"Invalid category. Allowed: {string.Join(", ", AllowedCategories)}");
            }

            string? imageUrl = null;
            if (dto.Image != null && dto.Image.Length > 0)
            {
                // Check file size (10MB limit for Cloudinary free tier - warn users)
                if (dto.Image.Length > 10_485_760)
                    return BadRequest("Image file size must be less than 10MB.");

                // Upload to Cloudinary
                imageUrl = await _cloudinaryService.UploadImageAsync(dto.Image, "setups");
            }

            var setup = new Setup
            {
                Title = dto.Title,
                Description = dto.Description,
                Category = AllowedCategories.First(c => c.Equals(dto.Category, StringComparison.OrdinalIgnoreCase)),
                ImageUrl = imageUrl,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            if (dto.ItemIds != null && dto.ItemIds.Any())
            {
                setup.SetupItems = dto.ItemIds.Select(id => new SetupItem
                {
                    ItemId = id,
                    Setup = setup
                }).ToList();
            }

            if (dto.TagNames != null && dto.TagNames.Any())
            {
                foreach (var tagName in dto.TagNames)
                {
                    var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName)
                              ?? new Tag { Name = tagName };

                    setup.SetupTags.Add(new SetupTag
                    {
                        Setup = setup,
                        Tag = tag
                    });
                }
            }

            _context.Setups.Add(setup);
            await _context.SaveChangesAsync();

            // Auto-upvote by creator
            try
            {
                var existing = await _context.SetupVotes.FirstOrDefaultAsync(v => v.SetupId == setup.Id && v.UserId == user.Id);
                if (existing == null)
                {
                    _context.SetupVotes.Add(new SetupVote { SetupId = setup.Id, UserId = user.Id });
                    await _context.SaveChangesAsync();
                }
            }
            catch { /* non-fatal */ }

            return CreatedAtAction(nameof(GetById), new { id = setup.Id }, new { setup.Id });
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSetupDto dto)
        {
            var setup = await _context.Setups
                .Include(s => s.SetupItems)
                .Include(s => s.SetupTags)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (setup == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (setup.UserId != user?.Id)
                return Forbid();

            // Content moderation - check title and description
            var titleCheck = await _contentModeration.ModerateTextAsync(dto.Title);
            if (!titleCheck.IsClean)
            {
                return BadRequest(new
                {
                    message = "Your setup title contains inappropriate content and cannot be updated. Please review our Terms of Service.",
                    violations = titleCheck.ViolationReasons
                });
            }

            var descriptionCheck = await _contentModeration.ModerateTextAsync(dto.Description ?? "");
            if (!descriptionCheck.IsClean)
            {
                return BadRequest(new
                {
                    message = "Your setup description contains inappropriate content and cannot be updated. Please review our Terms of Service.",
                    violations = descriptionCheck.ViolationReasons
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Category) ||
                !AllowedCategories.Contains(dto.Category, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest($"Invalid category. Allowed: {string.Join(", ", AllowedCategories)}");
            }

            setup.Title = dto.Title;
            setup.Description = dto.Description;
            setup.Category = AllowedCategories.First(c => c.Equals(dto.Category, StringComparison.OrdinalIgnoreCase));

            setup.SetupItems.Clear();
            if (dto.ItemIds != null && dto.ItemIds.Any())
            {
                setup.SetupItems = dto.ItemIds.Select(i => new SetupItem
                {
                    SetupId = setup.Id,
                    ItemId = i
                }).ToList();
            }

            setup.SetupTags.Clear();
            if (dto.TagNames != null && dto.TagNames.Any())
            {
                foreach (var tagName in dto.TagNames)
                {
                    var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName)
                              ?? new Tag { Name = tagName };

                    setup.SetupTags.Add(new SetupTag
                    {
                        SetupId = setup.Id,
                        Tag = tag
                    });
                }
            }

            await _context.SaveChangesAsync();

            var result = await _context.Setups
                .Include(s => s.SetupItems).ThenInclude(si => si.Item)
                .Include(s => s.SetupTags).ThenInclude(st => st.Tag)
                .Include(s => s.Votes)
                .Where(s => s.Id == setup.Id)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Category,
                    s.ImageUrl,
                    s.UserId,
                    s.CreatedAt,
                    VoteCount = s.Votes.Count(),
                    Items = s.SetupItems.Select(si => new
                    {
                        si.Item.Id,
                        si.Item.Name,
                        si.Item.Description,
                        si.Item.ImageUrl
                    }).ToList(),
                    Tags = s.SetupTags.Select(st => new
                    {
                        st.Tag.Id,
                        st.Tag.Name
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}/image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile image)
        {
            var setup = await _context.Setups.FindAsync(id);
            if (setup == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (setup.UserId != user?.Id)
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

            if (!string.IsNullOrWhiteSpace(setup.ImageUrl))
            {
                try
                {
                    // Delete image from Cloudinary
                    var uri = new Uri(setup.ImageUrl);
                    var segments = uri.AbsolutePath.Split('/');
                    var uploadIndex = Array.IndexOf(segments, "upload");
                    if (uploadIndex >= 0 && uploadIndex < segments.Length - 1)
                    {
                        var pathAfterUpload = string.Join("/", segments.Skip(uploadIndex + 1));
                        var publicId = pathAfterUpload.Substring(0, pathAfterUpload.LastIndexOf('.'));
                        await _cloudinaryService.DeleteImageAsync(publicId);
                    }
                }
                catch { }
            }

            // Upload new image to Cloudinary
            var imageUrl = await _cloudinaryService.UploadImageAsync(image, "setups");
            setup.ImageUrl = imageUrl;
            await _context.SaveChangesAsync();
            return Ok(new { setup.ImageUrl });
        }

        [Authorize]
        [HttpDelete("{id}/image")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var setup = await _context.Setups.FindAsync(id);
            if (setup == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (setup.UserId != user?.Id)
                return Forbid();

            if (!string.IsNullOrWhiteSpace(setup.ImageUrl))
            {
                try
                {
                    // Delete image from Cloudinary
                    var uri = new Uri(setup.ImageUrl);
                    var segments = uri.AbsolutePath.Split('/');
                    var uploadIndex = Array.IndexOf(segments, "upload");
                    if (uploadIndex >= 0 && uploadIndex < segments.Length - 1)
                    {
                        var pathAfterUpload = string.Join("/", segments.Skip(uploadIndex + 1));
                        var publicId = pathAfterUpload.Substring(0, pathAfterUpload.LastIndexOf('.'));
                        await _cloudinaryService.DeleteImageAsync(publicId);
                    }
                }
                catch { }
            }

            setup.ImageUrl = null;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var setup = await _context.Setups
                .Include(s => s.SetupItems)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (setup == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (setup.UserId != user?.Id)
                return Forbid();

            _context.SetupItems.RemoveRange(setup.SetupItems);
            _context.Setups.Remove(setup);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("feed")]
        public async Task<IActionResult> Feed([FromQuery] string? category, [FromQuery] int? maxAgeDays, [FromQuery] int? minAgeDays, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var q = _context.Setups
                .Include(s => s.Votes)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && AllowedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                var cat = AllowedCategories.First(c => c.Equals(category, StringComparison.OrdinalIgnoreCase));
                q = q.Where(s => s.Category == cat);
            }
            if (maxAgeDays.HasValue && maxAgeDays.Value > 0)
            {
                var since = DateTime.UtcNow.AddDays(-maxAgeDays.Value);
                q = q.Where(s => s.CreatedAt >= since);
            }
            if (minAgeDays.HasValue && minAgeDays.Value > 0)
            {
                var before = DateTime.UtcNow.AddDays(-minAgeDays.Value);
                q = q.Where(s => s.CreatedAt < before);
            }

            var items = await q
                .OrderByDescending(s => s.Votes.Count())
                .ThenByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Category,
                    s.ImageUrl,
                    s.CreatedAt,
                    VoteCount = s.Votes.Count(),
                    s.UserId,
                    HasVoted = currentUserId != null && s.Votes.Any(v => v.UserId == currentUserId)
                })
                .ToListAsync();

            return Ok(items);
        }

        [Authorize]
        [HttpGet("upvoted")]
        public async Task<IActionResult> GetUpvoted([FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var q = _context.Setups
                .Include(s => s.Votes)
                .Where(s => s.Votes.Any(v => v.UserId == user.Id));

            if (!string.IsNullOrWhiteSpace(category) && AllowedCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            {
                var cat = AllowedCategories.First(c => c.Equals(category, StringComparison.OrdinalIgnoreCase));
                q = q.Where(s => s.Category == cat);
            }

            var currentUserId = user.Id;
            var items = await q
                .OrderByDescending(s => s.Votes.Count())
                .ThenByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Category,
                    s.ImageUrl,
                    s.CreatedAt,
                    VoteCount = s.Votes.Count(),
                    s.UserId,
                    HasVoted = s.Votes.Any(v => v.UserId == currentUserId)
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("popular")]
        public async Task<IActionResult> GetPopular()
        {
            var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);
            var thirtyDaysAgo = now.AddDays(-30);
            var ninetyDaysAgo = now.AddDays(-90);

            // Helper to project to response shape
            IQueryable<Setup> BaseQuery() => _context.Setups.Include(s => s.Votes);

            var results = new List<dynamic>();

            // Bucket 1: last 7 days
            var bucket1 = await BaseQuery()
                .Where(s => s.CreatedAt >= sevenDaysAgo)
                .OrderByDescending(s => s.Votes.Count())
                .ThenByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Category,
                    s.ImageUrl,
                    s.CreatedAt,
                    VoteCount = s.Votes.Count(),
                    s.UserId,
                    HasVoted = currentUserId != null && s.Votes.Any(v => v.UserId == currentUserId)
                })
                .Take(3)
                .ToListAsync();
            results.AddRange(bucket1);

            if (results.Count < 3)
            {
                // Bucket 2: last 30 days but older than 7 days
                var bucket2 = await BaseQuery()
                    .Where(s => s.CreatedAt >= thirtyDaysAgo && s.CreatedAt < sevenDaysAgo)
                    .OrderByDescending(s => s.Votes.Count())
                    .ThenByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.Description,
                        s.Category,
                        s.ImageUrl,
                        s.CreatedAt,
                        VoteCount = s.Votes.Count(),
                        s.UserId,
                        HasVoted = currentUserId != null && s.Votes.Any(v => v.UserId == currentUserId)
                    })
                    .Take(3 - results.Count)
                    .ToListAsync();
                results.AddRange(bucket2);
            }

            if (results.Count < 3)
            {
                // Bucket 3: last 90 days but older than 30 days
                var bucket3 = await BaseQuery()
                    .Where(s => s.CreatedAt >= ninetyDaysAgo && s.CreatedAt < thirtyDaysAgo)
                    .OrderByDescending(s => s.Votes.Count())
                    .ThenByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.Description,
                        s.Category,
                        s.ImageUrl,
                        s.CreatedAt,
                        VoteCount = s.Votes.Count(),
                        s.UserId,
                        HasVoted = currentUserId != null && s.Votes.Any(v => v.UserId == currentUserId)
                    })
                    .Take(3 - results.Count)
                    .ToListAsync();
                results.AddRange(bucket3);
            }

            if (results.Count < 3)
            {
                // Bucket 4: older than 90 days (90+)
                var bucket4 = await BaseQuery()
                    .Where(s => s.CreatedAt < ninetyDaysAgo)
                    .OrderByDescending(s => s.Votes.Count())
                    .ThenByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.Description,
                        s.Category,
                        s.ImageUrl,
                        s.CreatedAt,
                        VoteCount = s.Votes.Count(),
                        s.UserId,
                        HasVoted = currentUserId != null && s.Votes.Any(v => v.UserId == currentUserId)
                    })
                    .Take(3 - results.Count)
                    .ToListAsync();
                results.AddRange(bucket4);
            }

            return Ok(results);
        }

        [Authorize]
        [HttpPost("{id}/upvote")]
        public async Task<IActionResult> ToggleUpvote(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var setup = await _context.Setups.Include(s => s.Votes).FirstOrDefaultAsync(s => s.Id == id);
            if (setup == null) return NotFound();

            var existing = setup.Votes.FirstOrDefault(v => v.UserId == user.Id);
            if (existing != null)
            {
                _context.SetupVotes.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { voted = false, voteCount = setup.Votes.Count });
            }
            else
            {
                var vote = new SetupVote { SetupId = setup.Id, UserId = user.Id };
                _context.SetupVotes.Add(vote);
                await _context.SaveChangesAsync();
                return Ok(new { voted = true, voteCount = setup.Votes.Count });
            }
        }

        [Authorize]
        [HttpPost("{setupId}/items/{itemId}")]
        public async Task<IActionResult> AddItemToSetup(int setupId, int itemId)
        {
            try
            {
                var setup = await _context.Setups.Include(s => s.SetupItems).FirstOrDefaultAsync(s => s.Id == setupId);
                if (setup == null)
                    return NotFound();
                var user = await _userManager.GetUserAsync(User);
                if (setup.UserId != user?.Id)
                    return Forbid();
                if (setup.SetupItems.Any(si => si.ItemId == itemId))
                    return BadRequest("Item already in setup");
                var item = await _context.Items.FindAsync(itemId);
                if (item == null)
                    return NotFound("Item not found");
                var setupItem = new SetupItem { SetupId = setupId, ItemId = itemId };
                _context.SetupItems.Add(setupItem);
                await _context.SaveChangesAsync();
                return Ok(new { setupItem.SetupId, setupItem.ItemId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [Authorize]
        [HttpDelete("{setupId}/items/{itemId}")]
        public async Task<IActionResult> RemoveItemFromSetup(int setupId, int itemId)
        {
            var setup = await _context.Setups.Include(s => s.SetupItems).FirstOrDefaultAsync(s => s.Id == setupId);
            if (setup == null)
                return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (setup.UserId != user?.Id)
                return Forbid();
            var setupItem = await _context.SetupItems.FirstOrDefaultAsync(si => si.SetupId == setupId && si.ItemId == itemId);
            if (setupItem == null)
                return NotFound();
            _context.SetupItems.Remove(setupItem);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
