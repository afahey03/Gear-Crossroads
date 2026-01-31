using GearCrossroads.Api.Data;
using GearCrossroads.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GearCrossroads.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Middleware to check if user is admin
        private async Task<ApplicationUser?> GetCurrentAdminUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsAdmin)
            {
                return null;
            }
            return user;
        }

        // GET: api/admin/accounts - List all accounts
        [HttpGet("accounts")]
        public async Task<IActionResult> GetAllAccounts()
        {
            var adminUser = await GetCurrentAdminUser();
            if (adminUser == null)
                return Forbid();

            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.DisplayName,
                    u.IsAdmin,
                    u.IsWife,
                    u.IsBanned,
                    u.BannedAt,
                    u.EmailConfirmed,
                    SetupCount = u.Id != null ? _context.Setups.Count(s => s.UserId == u.Id) : 0
                })
                .OrderByDescending(u => u.IsAdmin)
                .ThenBy(u => u.UserName)
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/admin/accounts/{userId} - Get specific account details
        [HttpGet("accounts/{userId}")]
        public async Task<IActionResult> GetAccountDetails(string userId)
        {
            var adminUser = await GetCurrentAdminUser();
            if (adminUser == null)
                return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            var setups = await _context.Setups
                .Where(s => s.UserId == userId)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Category,
                    s.ImageUrl,
                    s.CreatedAt,
                    VoteCount = s.Votes.Count
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                User = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.DisplayName,
                    user.Bio,
                    user.AvatarUrl,
                    user.IsAdmin,
                    user.IsWife,
                    user.IsBanned,
                    user.BannedAt,
                    user.EmailConfirmed
                },
                Setups = setups
            });
        }

        // POST: api/admin/accounts/{userId}/ban - Ban a user
        [HttpPost("accounts/{userId}/ban")]
        public async Task<IActionResult> BanUser(string userId)
        {
            var adminUser = await GetCurrentAdminUser();
            if (adminUser == null)
                return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            // Prevent banning other admins
            if (user.IsAdmin)
                return BadRequest("Cannot ban another admin account.");

            // Prevent self-ban
            if (user.Id == adminUser.Id)
                return BadRequest("Cannot ban yourself.");

            // Mark user as banned
            user.IsBanned = true;
            user.BannedAt = DateTime.UtcNow;

            // Delete all setups created by this user
            var userSetups = await _context.Setups
                .Include(s => s.SetupItems)
                .Include(s => s.SetupTags)
                .Include(s => s.Votes)
                .Where(s => s.UserId == userId)
                .ToListAsync();

            foreach (var setup in userSetups)
            {
                // Delete setup image if exists
                if (!string.IsNullOrWhiteSpace(setup.ImageUrl))
                {
                    try
                    {
                        var relativePath = setup.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                    catch { /* Ignore file deletion errors */ }
                }

                _context.Setups.Remove(setup);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"User {user.UserName} has been banned and all their setups ({userSetups.Count}) have been deleted.",
                bannedUserId = userId,
                setupsDeleted = userSetups.Count
            });
        }

        // POST: api/admin/accounts/{userId}/unban - Unban a user
        [HttpPost("accounts/{userId}/unban")]
        public async Task<IActionResult> UnbanUser(string userId)
        {
            var adminUser = await GetCurrentAdminUser();
            if (adminUser == null)
                return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.IsBanned = false;
            user.BannedAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User {user.UserName} has been unbanned." });
        }
    }
}
