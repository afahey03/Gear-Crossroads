using System.ComponentModel.DataAnnotations;

namespace GearCrossroads.Api.Models
{
    public class Item
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = "";
        public string Brand { get; set; } = "";
        public string ModelNumber { get; set; } = "";
        public string Url { get; set; } = ""; // optional product link
        public ICollection<SetupItem> SetupItems { get; set; } = new List<SetupItem>();
    }
}
