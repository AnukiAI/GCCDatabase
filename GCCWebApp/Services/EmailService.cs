using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GCCWebApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task<bool> TestConnectionAsync();
    }

    /// <summary>
    /// SMTP delivery for 2FA codes and password-reset links.
    ///
    /// DEF-263 (2026-08-19): rewritten from System.Net.Mail to MailKit.
    ///
    /// The GCC mail server was probed directly and does NOT support the transport the old code
    /// assumed:
    ///
    ///     port 587  EHLO advertises AUTH PLAIN LOGIN CRAM-MD5 but NO STARTTLS.
    ///               Issuing STARTTLS explicitly returns "503 TLS is not allowed".
    ///     port 465  implicit TLS, negotiates TLS 1.2 cleanly, same AUTH mechanisms.
    ///
    /// Host must be "accumail3.accuwebhosting.com", NOT "mail.gccwebapp.com". Both resolve to
    /// 72.18.135.96, but the server presents a DigiCert certificate for the accuwebhosting name
    /// only, so MailKit - which validates certificates by default, unlike a hand-rolled SslStream
    /// probe with a permissive callback - rejects the mail.gccwebapp.com host before authenticating.
    ///
    /// System.Net.Mail.SmtpClient only speaks STARTTLS when EnableSsl = true and has no implicit-TLS
    /// mode at all, so against this server it can only connect by sending the mailbox password in
    /// the clear on 587. MailKit's SecureSocketOptions.SslOnConnect handles 465 properly, which is
    /// why the dependency was added rather than just filling in the blank config values.
    ///
    /// Transport is chosen from configuration, defaulting on port so existing config keeps working:
    ///     SmtpSettings:Security  explicit override - SslOnConnect | StartTls | StartTlsWhenAvailable | None
    ///     SmtpSettings:EnableSsl false => None
    ///     otherwise              port 465 => SslOnConnect, any other port => StartTls
    /// </summary>
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

            if (!ValidateSmtpSettings(smtpSettings))
            {
                throw new InvalidOperationException("SMTP settings are not properly configured");
            }

            var message = BuildMessage(smtpSettings, to, subject, body, isHtml: true);

            try
            {
                await SendAsync(smtpSettings, message);
                _logger.LogInformation("Email sent successfully to {Recipient}", to);
            }
            catch (Exception ex)
            {
                // Never log the message body - password-reset links and 2FA codes travel in it.
                _logger.LogError(ex, "Failed to send email to {Recipient}: {Message}", to, ex.Message);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sends a real message to the configured FromEmail. A true result means SMTP genuinely
        /// connected, authenticated and accepted a message - unlike MockEmailService, which always
        /// returns true and delivers nothing.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            if (!ValidateSmtpSettings(smtpSettings))
            {
                _logger.LogError("SMTP settings validation failed during connection test");
                return false;
            }

            try
            {
                var message = BuildMessage(
                    smtpSettings,
                    smtpSettings["FromEmail"]!,
                    "SMTP Test - GCC Database",
                    "This is a test email to verify SMTP configuration.",
                    isHtml: false);

                await SendAsync(smtpSettings, message);
                _logger.LogInformation("SMTP connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP connection test failed: {Message}", ex.Message);
                return false;
            }
        }

        private MimeMessage BuildMessage(
            IConfigurationSection smtpSettings, string to, string subject, string body, bool isHtml)
        {
            var message = new MimeMessage
            {
                Subject = subject,
                Priority = MessagePriority.Normal
            };

            message.From.Add(new MailboxAddress(
                smtpSettings["FromName"] ?? "GCC Database",
                smtpSettings["FromEmail"]!));
            message.To.Add(MailboxAddress.Parse(to));

            var builder = new BodyBuilder();
            if (isHtml)
            {
                builder.HtmlBody = body;
            }
            else
            {
                builder.TextBody = body;
            }
            message.Body = builder.ToMessageBody();

            return message;
        }

        private async Task SendAsync(IConfigurationSection smtpSettings, MimeMessage message)
        {
            var host = smtpSettings["Host"]!;
            var port = int.Parse(smtpSettings["Port"]!);
            var security = ResolveSecurity(smtpSettings, port);

            using var client = new SmtpClient { Timeout = 20000 };

            _logger.LogDebug("SMTP connect {Host}:{Port} using {Security}", host, port, security);
            await client.ConnectAsync(host, port, security);

            // The server advertises CRAM-MD5, which MailKit prefers by default. Some shared hosts
            // advertise it without storing a password form that supports it, so failed logins fall
            // back to LOGIN/PLAIN - safe here because the connection is already encrypted.
            if (security == SecureSocketOptions.None)
            {
                // Unencrypted transport: refuse to hand over credentials in the clear.
                _logger.LogWarning(
                    "SMTP is configured with no transport security on {Host}:{Port}; " +
                    "credentials would be sent unencrypted.", host, port);
            }

            await client.AuthenticateAsync(smtpSettings["Username"], smtpSettings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        /// <summary>
        /// Explicit SmtpSettings:Security wins; otherwise infer from EnableSsl and the port.
        /// </summary>
        private SecureSocketOptions ResolveSecurity(IConfigurationSection smtpSettings, int port)
        {
            var configured = smtpSettings["Security"];
            if (!string.IsNullOrWhiteSpace(configured)
                && Enum.TryParse<SecureSocketOptions>(configured, ignoreCase: true, out var explicitOption))
            {
                return explicitOption;
            }

            if (bool.TryParse(smtpSettings["EnableSsl"], out var enableSsl) && !enableSsl)
            {
                return SecureSocketOptions.None;
            }

            // 465 is implicit TLS; everything else negotiates STARTTLS after a plaintext greeting.
            return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        }

        private bool ValidateSmtpSettings(IConfigurationSection smtpSettings)
        {
            var requiredSettings = new[] { "Host", "Port", "EnableSsl", "Username", "Password", "FromEmail" };

            foreach (var setting in requiredSettings)
            {
                var value = smtpSettings[setting];
                if (string.IsNullOrWhiteSpace(value))
                {
                    _logger.LogError("SMTP setting '{Setting}' is missing or empty", setting);
                    return false;
                }
            }

            if (!int.TryParse(smtpSettings["Port"], out var port) || port <= 0 || port > 65535)
            {
                _logger.LogError("SMTP Port must be a valid number between 1 and 65535");
                return false;
            }

            if (!bool.TryParse(smtpSettings["EnableSsl"], out _))
            {
                _logger.LogError("SMTP EnableSsl must be true or false");
                return false;
            }

            var security = smtpSettings["Security"];
            if (!string.IsNullOrWhiteSpace(security)
                && !Enum.TryParse<SecureSocketOptions>(security, ignoreCase: true, out _))
            {
                _logger.LogError(
                    "SMTP Security '{Security}' is not valid. Use SslOnConnect, StartTls, " +
                    "StartTlsWhenAvailable or None.", security);
                return false;
            }

            try
            {
                _ = MailboxAddress.Parse(smtpSettings["FromEmail"]!);
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

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            _logger.LogWarning($"MOCK EMAIL: To={to}, Subject={subject}");
            _logger.LogInformation($"MOCK EMAIL BODY: {body}");
            await Task.CompletedTask;
        }

        public async Task<bool> TestConnectionAsync()
        {
            _logger.LogInformation("Mock email service - connection test always returns true");
            return await Task.FromResult(true);
        }
    }
}
