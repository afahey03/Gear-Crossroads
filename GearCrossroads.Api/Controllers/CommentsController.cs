using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GearCrossroads.Api.Data;
using GearCrossroads.Api.Models;
using GearCrossroads.Api.DTOs;
using GearCrossroads.Api.Services;
using System.Security.Claims;

namespace GearCrossroads.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IContentModerationService _moderationService;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            ApplicationDbContext context,
            IContentModerationService moderationService,
            ILogger<CommentsController> logger)
        {
            _context = context;
            _moderationService = moderationService;
            _logger = logger;
        }

        // GET: api/comments/setup/{setupId}
        [HttpGet("setup/{setupId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetSetupComments(int setupId)
        {
            var setup = await _context.Setups.FindAsync(setupId);
            if (setup == null)
            {
                return NotFound(new { message = "Setup not found" });
            }

            var comments = await _context.Comments
                .Where(c => c.SetupId == setupId)
                .Include(c => c.User)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.IsDeleted ? "Deleted" : c.Content,
                    CreatedAt = c.CreatedAt,
                    EditedAt = c.EditedAt,
                    IsDeleted = c.IsDeleted,
                    SetupId = c.SetupId,
                    UserId = c.UserId,
                    Username = c.AuthorName ?? (c.User!.Email != null && c.User.Email.Contains("@")
                        ? c.User.Email.Substring(0, c.User.Email.IndexOf("@"))
                        : (c.User.UserName ?? "Unknown")),
                    IsSetupOwner = c.UserId == setup.UserId
                })
                .ToListAsync();

            return Ok(comments);
        }

        // POST: api/comments/setup/{setupId}
        [Authorize]
        [HttpPost("setup/{setupId}")]
        public async Task<ActionResult<CommentDto>> CreateComment(int setupId, [FromBody] CreateCommentDto dto)
        {
            var setup = await _context.Setups.FindAsync(setupId);
            if (setup == null)
            {
                return NotFound(new { message = "Setup not found" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Validate content
            if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 2000)
            {
                return BadRequest(new { message = "Comment must be between 1 and 2000 characters" });
            }

            // Content moderation
            var (isClean, violations) = await _moderationService.ModerateTextAsync(dto.Content);
            if (!isClean)
            {
                _logger.LogWarning($"User {userId} attempted to post inappropriate comment: {string.Join(", ", violations)}");
                return BadRequest(new
                {
                    message = "Your comment contains inappropriate content and cannot be posted. Please review our Terms of Service.",
                    violations = violations
                });
            }

            var user = await _context.Users.FindAsync(userId);
            var username = "Unknown";
            if (user?.Email != null && user.Email.Contains("@"))
            {
                username = user.Email.Substring(0, user.Email.IndexOf("@"));
            }
            else if (user?.UserName != null)
            {
                username = user.UserName;
            }

            var comment = new Comment
            {
                Content = dto.Content,
                SetupId = setupId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                AuthorName = username
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var commentDto = new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                EditedAt = comment.EditedAt,
                IsDeleted = comment.IsDeleted,
                SetupId = comment.SetupId,
                UserId = comment.UserId,
                Username = username,
                IsSetupOwner = comment.UserId == setup.UserId
            };

            return CreatedAtAction(nameof(GetSetupComments), new { setupId }, commentDto);
        }

        // PUT: api/comments/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentDto dto)
        {
            var comment = await _context.Comments
                .Include(c => c.Setup)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound(new { message = "Comment not found" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (comment.UserId != userId)
            {
                return Forbid();
            }

            if (comment.IsDeleted)
            {
                return BadRequest(new { message = "Cannot edit a deleted comment" });
            }

            // Validate content
            if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 2000)
            {
                return BadRequest(new { message = "Comment must be between 1 and 2000 characters" });
            }

            // Content moderation
            var (isClean, violations) = await _moderationService.ModerateTextAsync(dto.Content);
            if (!isClean)
            {
                _logger.LogWarning($"User {userId} attempted to edit comment with inappropriate content: {string.Join(", ", violations)}");
                return BadRequest(new
                {
                    message = "Your comment contains inappropriate content and cannot be posted. Please review our Terms of Service.",
                    violations = violations
                });
            }

            comment.Content = dto.Content;
            comment.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/comments/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound(new { message = "Comment not found" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if (comment.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            // Soft delete
            comment.IsDeleted = true;
            comment.Content = "Deleted";
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
