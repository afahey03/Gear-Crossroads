using GearCrossroads.Api.Models;

public class Setup
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Visibility { get; set; } = "public";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string OwnerId { get; set; } = "";
    public ApplicationUser Owner { get; set; }

    public ICollection<SetupItem> SetupItems { get; set; } = new List<SetupItem>();
    public ICollection<SetupTag> SetupTags { get; set; } = new List<SetupTag>(); // ✅ add this
}
