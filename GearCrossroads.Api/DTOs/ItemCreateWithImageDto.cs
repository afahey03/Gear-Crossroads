using Microsoft.AspNetCore.Http;

namespace GearCrossroads.Api.DTOs
{
    public class ItemCreateWithImageDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IFormFile? Image { get; set; }
    }
}
