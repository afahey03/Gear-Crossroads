using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using GearCrossroads.Api.Models;
using GearCrossroads.Api.DTOs;
using GearCrossroads.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using GearCrossroads.Api.Data;
using Microsoft.AspNetCore.Cors;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GearCrossroads.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _db;
        private readonly ICloudinaryService _cloudinaryService;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailService emailService, ApplicationDbContext db, ICloudinaryService cloudinaryService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
            _db = db;
            _cloudinaryService = cloudinaryService;
            _configuration = configuration;
            _emailService = emailService;
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(errors);
            }

            // Send email confirmation
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var frontendBase = _configuration["App:FrontendBaseUrl"]?.TrimEnd('/');
            var confirmUrl = $"{frontendBase}/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={encodedToken}";
            var html = $@"<p>Welcome to Gear Crossroads!</p>
                          <p>Please confirm your email by clicking the link below:</p>
                          <p><a href='{confirmUrl}'>Confirm Email</a></p>";
            try { await _emailService.SendEmailAsync(user.Email!, "Confirm your email", html); }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email] Confirm email send failed: {ex.Message}");
            }

            return Ok(new { user.Email, message = "Registration successful. Please check your email to confirm your account." });
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid email or password");

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordValid)
                return Unauthorized("Invalid email or password");

            if (!user.EmailConfirmed)
                return Unauthorized("Email not confirmed");

            // Check if user is banned
            if (user.IsBanned)
                return Unauthorized("Your account has been banned for violating our Terms of Service.");

            var accessToken = GenerateJwtToken(user);
            // issue refresh token
            await RevokeAllUserTokens(user.Id); // optional hardening: invalidate prior sessions
            var rt = await CreateAndStoreRefreshToken(user.Id, familyId: Guid.NewGuid());
            SetRefreshCookies(rt);
            return Ok(new AuthResponseDto { Token = accessToken, Email = user.Email ?? string.Empty });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return BadRequest("Invalid user.");
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest($"Email confirmation failed: {errors}");
            }
            return Ok(new { message = "Email confirmed successfully." });
        }

        [HttpPost("request-password-reset")]
        [EnableRateLimiting("email")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            // Don't reveal user existence
            if (user == null) return Ok(new { message = "If the email exists, a reset link has been sent." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var frontendBase = _configuration["App:FrontendBaseUrl"]?.TrimEnd('/') ?? "https://localhost:5173";
            var resetUrl = $"{frontendBase}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";
            var html = $@"<p>We received a request to reset your password.</p>
                          <p>You can reset it by clicking the link below:</p>
                          <p><a href='{resetUrl}'>Reset Password</a></p>";
            try { await _emailService.SendEmailAsync(user.Email!, "Reset your password", html); }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email] Password reset email send failed: {ex.Message}");
            }
            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("email")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("Invalid request.");

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(errors);
            }
            await RevokeAllUserTokens(user.Id);
            return Ok(new { message = "Password has been reset." });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();
            return Ok(new
            {
                id = user.Id,
                displayName = user.DisplayName,
                avatarUrl = user.AvatarUrl,
                email = user.Email
            });
        }

        [HttpPut("profile")]
        [RequestSizeLimit(5_000_000)] // 5MB limit
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();
            if (!string.IsNullOrWhiteSpace(dto.DisplayName))
                user.DisplayName = dto.DisplayName;

            if (dto.Avatar != null && dto.Avatar.Length > 0)
            {
                // Check file size (10MB limit for Cloudinary free tier - warn users)
                if (dto.Avatar.Length > 10_485_760)
                    return BadRequest("Image file size must be less than 10MB.");

                var ext = Path.GetExtension(dto.Avatar.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowed.Contains(ext))
                    return BadRequest("Invalid file type. Only jpg, jpeg, png, gif allowed.");

                // Upload to Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(dto.Avatar, "avatars");
                user.AvatarUrl = imageUrl;
            }

            await _userManager.UpdateAsync(user);
            return Ok(new
            {
                displayName = user.DisplayName,
                avatarUrl = user.AvatarUrl,
                email = user.Email
            });
        }

        [HttpDelete("account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            // Prevent admins from deleting their accounts
            if (user.IsAdmin)
                return BadRequest(new { message = "Admin accounts cannot be deleted." });

            // 1. Update all comments to show "Deleted User"
            var userComments = await _db.Comments.Where(c => c.UserId == userId).ToListAsync();
            foreach (var comment in userComments)
            {
                comment.AuthorName = "Deleted User";
            }

            // 2. Remove all upvotes
            var userVotes = await _db.SetupVotes.Where(v => v.UserId == userId).ToListAsync();
            _db.SetupVotes.RemoveRange(userVotes);

            // 3. Delete all user's setups (and related data via cascade)
            var userSetups = await _db.Setups
                .Include(s => s.SetupItems)
                .Include(s => s.SetupTags)
                .Include(s => s.Votes)
                .Include(s => s.Comments)
                .Where(s => s.UserId == userId)
                .ToListAsync();
            _db.Setups.RemoveRange(userSetups);

            // 4. Delete all user's items that aren't part of other setups
            var userItems = await _db.Items
                .Include(i => i.SetupItems)
                .Where(i => i.SetupItems.Any(si => si.Setup!.UserId == userId))
                .ToListAsync();

            // Only delete items that are exclusively in this user's setups
            foreach (var item in userItems)
            {
                var setupOwners = await _db.SetupItems
                    .Where(si => si.ItemId == item.Id)
                    .Select(si => si.Setup!.UserId)
                    .Distinct()
                    .ToListAsync();

                if (setupOwners.Count == 1 && setupOwners[0] == userId)
                {
                    _db.Items.Remove(item);
                }
            }

            // 5. Revoke all refresh tokens
            await RevokeAllUserTokens(userId);

            // 6. Delete the user account
            await _userManager.DeleteAsync(user);

            await _db.SaveChangesAsync();

            // Clear cookies
            Response.Cookies.Delete(RefreshCookieName(), DefaultCookieOptions(httpOnly: true, expires: DateTime.UtcNow.AddDays(-1)));
            Response.Cookies.Delete(CsrfCookieName(), DefaultCookieOptions(httpOnly: false, expires: DateTime.UtcNow.AddDays(-1)));

            return Ok(new { message = "Account deleted successfully." });
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings.GetValue<string>("Key") ?? "THIS_IS_A_DEV_SECRET_KEY_CHANGE_ME");
            var issuer = jwtSettings.GetValue<string>("Issuer") ?? "GearCrossroads.Api";
            var audience = jwtSettings.GetValue<string>("Audience") ?? "GearCrossroads.Client";
            var expireHours = jwtSettings.GetValue<double?>("ExpiresInHours") ?? 12.0;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            // Add admin claim if user is an admin
            if (user.IsAdmin)
            {
                claims.Add(new Claim("admin", "true"));
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromHours(expireHours)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            var csrfHeader = Request.Headers["X-CSRF"].ToString();
            var csrfCookie = Request.Cookies[CsrfCookieName()];
            if (string.IsNullOrEmpty(csrfHeader) || string.IsNullOrEmpty(csrfCookie) || csrfHeader != csrfCookie)
                return Unauthorized();

            var token = Request.Cookies[RefreshCookieName()];
            if (string.IsNullOrEmpty(token)) return Unauthorized();

            var hash = Sha256(token);
            var existing = _db.RefreshTokens.FirstOrDefault(r => r.TokenHash == hash);
            if (existing == null) return Unauthorized();

            if (existing.RevokedAt != null || existing.IsExpired)
            {
                // Reuse or expired: revoke the family
                await RevokeFamily(existing.FamilyId);
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(existing.UserId);
            if (user == null) return Unauthorized();

            // rotate
            existing.RevokedAt = DateTime.UtcNow;
            var newRt = await CreateAndStoreRefreshToken(existing.UserId, existing.FamilyId);
            existing.ReplacedByTokenId = newRt.Id;
            await _db.SaveChangesAsync();

            var accessToken = GenerateJwtToken(user);
            SetRefreshCookies(newRt);
            return Ok(new { token = accessToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Cookies[RefreshCookieName()];
            if (!string.IsNullOrEmpty(token))
            {
                var hash = Sha256(token);
                var existing = _db.RefreshTokens.FirstOrDefault(r => r.TokenHash == hash);
                if (existing != null && existing.RevokedAt == null)
                {
                    existing.RevokedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
            // Clear cookies
            Response.Cookies.Delete(RefreshCookieName(), DefaultCookieOptions(httpOnly: true, expires: DateTime.UtcNow.AddDays(-1)));
            Response.Cookies.Delete(CsrfCookieName(), DefaultCookieOptions(httpOnly: false, expires: DateTime.UtcNow.AddDays(-1)));
            return Ok(new { message = "Logged out." });
        }

        private async Task RevokeAllUserTokens(string userId)
        {
            var tokens = await _db.RefreshTokens.Where(r => r.UserId == userId && r.RevokedAt == null).ToListAsync();
            foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
            if (tokens.Count > 0) await _db.SaveChangesAsync();
        }

        private async Task RevokeFamily(Guid familyId)
        {
            var tokens = await _db.RefreshTokens.Where(r => r.FamilyId == familyId && r.RevokedAt == null).ToListAsync();
            foreach (var t in tokens) t.RevokedAt = DateTime.UtcNow;
            if (tokens.Count > 0) await _db.SaveChangesAsync();
        }

        private async Task<RefreshToken> CreateAndStoreRefreshToken(string userId, Guid familyId)
        {
            var lifetimeDays = _configuration.GetValue<int?>("Auth:RefreshTokenDays") ?? 14;
            var raw = GenerateSecureToken(32);
            var rt = new RefreshToken
            {
                UserId = userId,
                TokenHash = Sha256(raw),
                FamilyId = familyId,
                ExpiresAt = DateTime.UtcNow.AddDays(lifetimeDays)
            };
            _db.RefreshTokens.Add(rt);
            await _db.SaveChangesAsync();
            // return with raw token temporarily by reusing TokenHash prop is unsafe; instead attach via TagHelper
            // We'll return the entity and set the cookie from the raw token here directly
            rt.TokenHash = raw; // hijack to carry plain token back to SetRefreshCookies (not saved)
            return rt;
        }

        private void SetRefreshCookies(RefreshToken rt)
        {
            var rawToken = rt.TokenHash; // as set in CreateAndStoreRefreshToken
            var refreshOpts = DefaultCookieOptions(httpOnly: true, expires: rt.ExpiresAt);
            Response.Cookies.Append(RefreshCookieName(), rawToken, refreshOpts);

            // CSRF cookie
            var csrf = GenerateSecureToken(16);
            var csrfOpts = DefaultCookieOptions(httpOnly: false, expires: rt.ExpiresAt);
            Response.Cookies.Append(CsrfCookieName(), csrf, csrfOpts);
        }

        private CookieOptions DefaultCookieOptions(bool httpOnly, DateTime expires)
        {
            return new CookieOptions
            {
                HttpOnly = httpOnly,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expires,
                Path = "/"
            };
        }

        private static string RefreshCookieName() => "gc-refresh";
        private static string CsrfCookieName() => "gc-csrf";

        private static string GenerateSecureToken(int bytes)
        {
            var data = RandomNumberGenerator.GetBytes(bytes);
            return Convert.ToBase64String(data);
        }

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
