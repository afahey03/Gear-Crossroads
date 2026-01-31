using GearCrossroads.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GearCrossroads.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DebugController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var user = await _userManager.GetUserAsync(User);

            return Ok(new
            {
                PrincipalName = User.Identity?.Name,
                Claims = claims,
                ResolvedUser = user == null ? null : new { user.Id, user.UserName, user.Email }
            });
        }
    }
}
