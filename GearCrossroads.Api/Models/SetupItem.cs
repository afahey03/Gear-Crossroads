namespace GearCrossroads.Api.Models
{
    public class SetupItem
    {
        public int SetupId { get; set; }
        public Setup Setup { get; set; } = default!;

        public int ItemId { get; set; }
        public Item Item { get; set; } = default!;
    }
}
