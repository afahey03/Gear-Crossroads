using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace GearCrossroads.Api.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? replyTo = null);
    }

    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? replyTo = null)
        {
            var usePickup = _config.GetValue<bool>("Smtp:UsePickupDirectory");
            var verbose = _config.GetValue<bool>("Smtp:VerboseLogging");
            var rawFrom = _config["Smtp:From"] ?? _config["Smtp:User"];
            var userEmail = _config["Smtp:User"];

            // Normalize From formatting to "Display Name <email@domain>" when possible
            var from = NormalizeFromAddress(rawFrom, userEmail);

            using var message = new MailMessage(from!, toEmail, subject, htmlBody) { IsBodyHtml = true };

            // Set Reply-To header if provided
            if (!string.IsNullOrWhiteSpace(replyTo))
            {
                message.ReplyToList.Add(new MailAddress(replyTo));
            }

            if (usePickup)
            {
                var pickupDir = _config["Smtp:PickupDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "emails");
                Directory.CreateDirectory(pickupDir);
                using var client = new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                    PickupDirectoryLocation = pickupDir
                };
                _logger.LogInformation("[Email] Writing email to pickup directory {PickupDirectory} (To: {To})", pickupDir, toEmail);
                await client.SendMailAsync(message);
                _logger.LogInformation("[Email] Email written to pickup directory successfully.");
                return;
            }

            var host = _config["Smtp:Host"];
            var port = _config.GetValue<int>("Smtp:Port");
            var enableSsl = _config.GetValue<bool>("Smtp:EnableSsl");
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                _logger.LogError("[Email] SMTP configuration is missing. Configure Smtp:Host, Smtp:Port, Smtp:User, Smtp:Pass or enable Smtp:UsePickupDirectory.");
                throw new InvalidOperationException("SMTP configuration is missing. Configure Smtp:Host, Smtp:Port, Smtp:User, Smtp:Pass");
            }

            try
            {
                // Optional: try alternate ports if configured (e.g., "465,25")
                var ports = new List<int>();
                ports.Add(port);
                var alt = _config["Smtp:AlternatePorts"];
                if (!string.IsNullOrWhiteSpace(alt))
                {
                    foreach (var p in alt.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (int.TryParse(p, out var ap) && ap != port && !ports.Contains(ap)) ports.Add(ap);
                    }
                }

                // Optional: DNS resolution for diagnostics
                if (verbose)
                {
                    try
                    {
                        var addrs = await Dns.GetHostAddressesAsync(host);
                        _logger.LogInformation("[Email][SMTP] DNS {Host} -> {Addresses}", host, string.Join(",", addrs.Select(a => a.ToString())));
                    }
                    catch (Exception dnsEx)
                    {
                        _logger.LogWarning(dnsEx, "[Email][SMTP] DNS resolution failed for {Host}", host);
                    }
                }

                Exception? lastError = null;
                foreach (var p in ports)
                {
                    try
                    {
                        using var client = new SmtpClient(host, p);
                        client.EnableSsl = enableSsl;
                        client.Credentials = new NetworkCredential(user, pass);
                        client.Timeout = 15000; // 15s timeout

                        var sw = Stopwatch.StartNew();
                        if (verbose)
                        {
                            _logger.LogInformation("[Email][SMTP] Connecting to {Host}:{Port} (SSL={Ssl})", host, p, enableSsl);
                        }

                        _logger.LogInformation("[Email] Attempting SMTP send {Host}:{Port} (SSL={Ssl}) From={From} To={To}", host, p, enableSsl, from, toEmail);
                        await client.SendMailAsync(message);

                        sw.Stop();
                        _logger.LogInformation("[Email] Email SENT to {To} Subject='{Subject}' ElapsedMs={Elapsed}", toEmail, subject, sw.ElapsedMilliseconds);
                        lastError = null;
                        break; // success
                    }
                    catch (SmtpException ex)
                    {
                        lastError = ex;
                        _logger.LogError(ex, "[Email] SMTP failure Host={Host} Port={Port} To={To} StatusCode={StatusCode} Message={Message} Inner={Inner}", host, p, toEmail, ex.StatusCode, ex.Message, ex.InnerException?.Message);
                        // If there are more ports to try, continue; otherwise rethrow below
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        _logger.LogError(ex, "[Email] Unexpected email send failure Host={Host} Port={Port} To={To} Message={Message} Inner={Inner}", host, p, toEmail, ex.Message, ex.InnerException?.Message);
                    }
                }

                if (lastError != null)
                {
                    throw lastError; // bubble up last exception after trying all ports
                }
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "[Email] SMTP failure Host={Host} Port={Port} To={To} StatusCode={StatusCode} Message={Message} Inner={Inner}", host, port, toEmail, ex.StatusCode, ex.Message, ex.InnerException?.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Email] Unexpected email send failure Host={Host} Port={Port} To={To} Message={Message} Inner={Inner}", host, port, toEmail, ex.Message, ex.InnerException?.Message);
                throw;
            }
        }

        private static string? NormalizeFromAddress(string? from, string? fallbackEmail)
        {
            if (string.IsNullOrWhiteSpace(from)) return fallbackEmail;

            // If already in Display <email@domain> form, keep it
            if (from.Contains('<') && from.Contains('>')) return from;

            // If looks like plain email, keep it
            if (from.Contains('@') && !from.Contains(' ')) return from;

            // If looks like "Display email@domain" convert to "Display <email@domain>"
            var match = Regex.Match(from, @"^(.+?)\s+([\w.+'\-]+@[\w\-]+\.[A-Za-z]{2,})$",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var display = match.Groups[1].Value.Trim();
                var email = match.Groups[2].Value.Trim();
                return $"{display} <{email}>";
            }

            // Otherwise, treat 'from' as display name and wrap fallback email
            if (!string.IsNullOrWhiteSpace(fallbackEmail))
            {
                return $"{from.Trim()} <{fallbackEmail}>";
            }

            return from;
        }
    }
}
