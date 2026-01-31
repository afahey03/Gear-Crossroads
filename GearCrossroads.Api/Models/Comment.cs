using System.ComponentModel.DataAnnotations;

namespace GearCrossroads.Api.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        [Required]
        public int SetupId { get; set; }
        public Setup? Setup { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        // Store the author's name at the time of comment creation
        // This allows us to preserve "Deleted User" for accounts that get deleted
        [MaxLength(100)]
        public string? AuthorName { get; set; }
    }
}
