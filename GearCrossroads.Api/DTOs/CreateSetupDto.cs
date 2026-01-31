using Microsoft.AspNetCore.Http;

namespace GearCrossroads.Api.DTOs
{
    public class CreateSetupDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "Other";
        public IFormFile? Image { get; set; }
        public List<int>? ItemIds { get; set; }
        public List<string>? TagNames { get; set; }
    }
}
