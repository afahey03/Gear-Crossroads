using Microsoft.AspNetCore.Http;

namespace GearCrossroads.Api.DTOs
{
    public class UpdateProfileDto
    {
        public string? DisplayName { get; set; }
        public IFormFile? Avatar { get; set; }
    }

    public class RegisterDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    public class RequestPasswordResetDto
    {
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
