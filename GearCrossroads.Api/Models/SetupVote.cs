using System.ComponentModel.DataAnnotations;

namespace GearCrossroads.Api.Models
{
    public class SetupVote
    {
        public int SetupId { get; set; }
        public Setup? Setup { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
