using System.ComponentModel.DataAnnotations;

namespace GearCrossroads.Api.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public ICollection<SetupTag> SetupTags { get; set; } = new List<SetupTag>();
    }
}
