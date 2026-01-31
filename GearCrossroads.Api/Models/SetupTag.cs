namespace GearCrossroads.Api.Models
{
    public class SetupTag
    {
        public int SetupId { get; set; }
        public Setup Setup { get; set; } = default!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = default!;
    }
}
