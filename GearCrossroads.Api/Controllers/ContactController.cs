using GearCrossroads.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GearCrossroads.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IEmailService emailService, ILogger<ContactController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] ContactFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var issueTypeDisplay = GetIssueTypeDisplay(dto.IssueType);

                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #2563eb; border-bottom: 2px solid #2563eb; padding-bottom: 10px;'>
                                New Support Request
                            </h2>
                            
                            <div style='background-color: #f3f4f6; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 5px 0;'><strong>From:</strong> {dto.Email}</p>
                                <p style='margin: 5px 0;'><strong>Issue Type:</strong> {issueTypeDisplay}</p>
                                <p style='margin: 5px 0;'><strong>Submitted:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                            </div>
                            
                            <div style='margin: 20px 0;'>
                                <h3 style='color: #374151;'>Issue Description:</h3>
                                <div style='background-color: #ffffff; padding: 15px; border-left: 4px solid #2563eb; white-space: pre-wrap;'>
{dto.Description}
                                </div>
                            </div>
                            
                            <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 12px;'>
                                <p>This is an automated message from the Gear Crossroads support form.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                var subject = $"[Gear Crossroads Support] {issueTypeDisplay}";

                // Send email to support team
                await _emailService.SendEmailAsync("support@gearcrossroads.com", subject, emailBody);

                // Send confirmation email to user
                var confirmationBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #2563eb; border-bottom: 2px solid #2563eb; padding-bottom: 10px;'>
                                Thank You for Contacting Gear Crossroads
                            </h2>
                            
                            <p style='color: #374151; font-size: 16px;'>
                                We've received your support request and will get back to you as soon as possible.
                            </p>
                            
                            <div style='background-color: #f3f4f6; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <h3 style='color: #374151; margin-top: 0;'>Your Submission:</h3>
                                <p style='margin: 5px 0;'><strong>Issue Type:</strong> {issueTypeDisplay}</p>
                                <p style='margin: 5px 0;'><strong>Submitted:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                            </div>
                            
                            <div style='margin: 20px 0;'>
                                <h3 style='color: #374151;'>Your Message:</h3>
                                <div style='background-color: #ffffff; padding: 15px; border-left: 4px solid #2563eb; white-space: pre-wrap;'>
{dto.Description}
                                </div>
                            </div>
                            
                            <div style='background-color: #eff6ff; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p style='margin: 0; color: #1e40af;'>
                                    <strong>💡 Tip:</strong> You can reply directly to this email to continue the conversation with our support team at support@gearcrossroads.com.
                                </p>
                            </div>
                            
                            <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 12px;'>
                                <p>This is an automated confirmation from Gear Crossroads.</p>
                                <p>If you did not submit this request, please contact us immediately.</p>
                            </div>
                        </div>
                    </body>
                    </html>
                ";

                var confirmationSubject = "Your Gear Crossroads Support Request - Confirmation";
                await _emailService.SendEmailAsync(dto.Email, confirmationSubject, confirmationBody, "support@gearcrossroads.com");

                _logger.LogInformation("Contact form submitted by {Email} with issue type {IssueType}", dto.Email, dto.IssueType);

                return Ok(new { message = "Your message has been sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing contact form submission from {Email}", dto.Email);
                return StatusCode(500, new { message = "An error occurred while sending your message. Please try again later." });
            }
        }

        private static string GetIssueTypeDisplay(string issueType)
        {
            return issueType switch
            {
                "inappropriate-content" => "Report: Inappropriate Content",
                "harassment" => "Report: Harassment or Discrimination",
                "technical-issue" => "Technical Issue",
                "account-issue" => "Account Issue",
                "bug-report" => "Bug Report",
                "feature-request" => "Feature Request",
                "other" => "Other",
                _ => issueType
            };
        }
    }

    public class ContactFormDto
    {
        public string Email { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
