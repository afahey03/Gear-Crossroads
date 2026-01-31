using System.ComponentModel.DataAnnotations;

namespace GearCrossroads.Api.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public ICollection<SetupItem> SetupItems { get; set; } = new List<SetupItem>();
    }
}
