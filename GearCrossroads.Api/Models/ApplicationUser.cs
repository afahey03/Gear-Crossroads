using Microsoft.AspNetCore.Identity;

namespace GearCrossroads.Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = "";
        public string Bio { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
        public bool IsAdmin { get; set; } = false;
        public bool IsBanned { get; set; } = false;
        public DateTime? BannedAt { get; set; }
        public bool IsWife { get; set; } = false;
    }
}
