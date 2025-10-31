namespace GearCrossroads.Api.Models
{
    public class SetupItem
    {
        public int Id { get; set; }
        public int SetupId { get; set; }
        public Setup Setup { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; }

        public string Note { get; set; } = "";
        public int Position { get; set; } = 0;
    }
}
