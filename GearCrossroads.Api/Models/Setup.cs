using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GearCrossroads.Api.Models
{
    public class Setup
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public string Category { get; set; } = "Other";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public ICollection<SetupItem> SetupItems { get; set; } = new List<SetupItem>();
        public ICollection<SetupTag> SetupTags { get; set; } = new List<SetupTag>();

        public ICollection<SetupVote> Votes { get; set; } = new List<SetupVote>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
