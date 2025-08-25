using System.Net;
using System.Net.Mail;

namespace GCCWebApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task<bool> TestConnectionAsync();
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            
            // Validate SMTP configuration
            if (!ValidateSmtpSettings(smtpSettings))
            {
                throw new InvalidOperationException("SMTP settings are not properly configured");
            }

            using var client = new SmtpClient
            {
                Host = smtpSettings["Host"]!,
                Port = int.Parse(smtpSettings["Port"]!),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"]!),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    smtpSettings["Username"]!,
                    smtpSettings["Password"]!
                )
            };

            var message = new MailMessage
            {
                From = new MailAddress(
                    smtpSettings["FromEmail"]!, 
                    smtpSettings["FromName"] ?? "GCC Database"
                ),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = MailPriority.High
            };
            
            message.To.Add(to);

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, $"SMTP error sending email to {to}: {ex.Message}");
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error sending email to {to}: {ex.Message}");
                throw;
            }
            finally
            {
                message.Dispose();
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                
                if (!ValidateSmtpSettings(smtpSettings))
                {
                    _logger.LogError("SMTP settings validation failed during connection test");
                    return false;
                }

                using var client = new SmtpClient
                {
                    Host = smtpSettings["Host"]!,
                    Port = int.Parse(smtpSettings["Port"]!),
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"]!),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(
                        smtpSettings["Username"]!,
                        smtpSettings["Password"]!
                    ),
                    Timeout = 10000 // 10 second timeout
                };

                // Test by sending a test email to the sender's address
                var testMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["FromEmail"]!, "Test"),
                    Subject = "SMTP Test - GCC Database",
                    Body = "This is a test email to verify SMTP configuration.",
                    IsBodyHtml = false
                };
                
                testMessage.To.Add(smtpSettings["FromEmail"]!);

                await client.SendMailAsync(testMessage);
                _logger.LogInformation("SMTP connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SMTP connection test failed: {ex.Message}");
                return false;
            }
        }

        private bool ValidateSmtpSettings(IConfigurationSection smtpSettings)
        {
            var requiredSettings = new[] { "Host", "Port", "EnableSsl", "Username", "Password", "FromEmail" };
            
            foreach (var setting in requiredSettings)
            {
                var value = smtpSettings[setting];
                if (string.IsNullOrWhiteSpace(value))
                {
                    _logger.LogError($"SMTP setting '{setting}' is missing or empty");
                    return false;
                }
            }

            // Validate port is a number
            if (!int.TryParse(smtpSettings["Port"], out var port) || port <= 0 || port > 65535)
            {
                _logger.LogError("SMTP Port must be a valid number between 1 and 65535");
                return false;
            }

            // Validate EnableSsl is a boolean
            if (!bool.TryParse(smtpSettings["EnableSsl"], out _))
            {
                _logger.LogError("SMTP EnableSsl must be true or false");
                return false;
            }

            // Validate email address format
            try
            {
                var addr = new MailAddress(smtpSettings["FromEmail"]!);
            }
            catch
            {
                _logger.LogError("SMTP FromEmail is not a valid email address");
                return false;
            }

            return true;
        }
    }

    // Fallback service for when SMTP is not configured
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string to, string subject, string body)
        {
            _logger.LogWarning($"MOCK EMAIL: To={to}, Subject={subject}");
            _logger.LogInformation($"MOCK EMAIL BODY: {body}");
            return Task.CompletedTask;
        }

        public Task<bool> TestConnectionAsync()
        {
            _logger.LogInformation("Mock email service - connection test always returns true");
            return Task.FromResult(true);
        }
    }
}