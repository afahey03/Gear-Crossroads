namespace GearCrossroads.Api.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ICollection<SetupTag> SetupTags { get; set; } = new List<SetupTag>();
    }

    public class SetupTag
    {
        public int SetupId { get; set; }
        public Setup Setup { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
