using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace GCCWebApp.Services
{
    public interface ITwoFactorService
    {
        string GenerateCode();
        bool ValidateCode(string userId, string code);
        Task SendCodeByEmailAsync(string email, string code);
        void StoreCode(string userId, string code);
        Task<bool> ResendCodeAsync(string userId, string email);
    }

    public class TwoFactorService : ITwoFactorService
    {
        private readonly ILogger<TwoFactorService> _logger;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        
        // Cache keys
        private const string CODE_CACHE_KEY = "2fa_code_{0}";
        private const string ATTEMPTS_CACHE_KEY = "2fa_attempts_{0}";
        private const string RESEND_CACHE_KEY = "2fa_resend_{0}";

        public TwoFactorService(
            ILogger<TwoFactorService> logger, 
            IEmailService emailService, 
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _logger = logger;
            _emailService = emailService;
            _cache = cache;
            _configuration = configuration;
        }

        public string GenerateCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
            return code.ToString("D6");
        }

        public void StoreCode(string userId, string code)
        {
            var cacheKey = string.Format(CODE_CACHE_KEY, userId);
            var expiry = TimeSpan.FromMinutes(5); // 5 minute expiry
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry,
                Priority = CacheItemPriority.High,
                Size = 1 // Required when SizeLimit is set
            };

            _cache.Set(cacheKey, new TwoFactorData 
            { 
                Code = code, 
                CreatedAt = DateTime.UtcNow,
                Attempts = 0
            }, cacheOptions);
            
            _logger.LogInformation($"2FA code stored for user {userId}");
        }

        public bool ValidateCode(string userId, string code)
        {
            var cacheKey = string.Format(CODE_CACHE_KEY, userId);
            var attemptsKey = string.Format(ATTEMPTS_CACHE_KEY, userId);

            if (!_cache.TryGetValue(cacheKey, out TwoFactorData? data) || data == null)
            {
                _logger.LogWarning($"No 2FA code found for user {userId}");
                return false;
            }

            // Check if too many attempts
            var attempts = _cache.Get<int>(attemptsKey);
            if (attempts >= 3)
            {
                _logger.LogWarning($"Too many 2FA attempts for user {userId}");
                _cache.Remove(cacheKey);
                return false;
            }

            // Check if expired
            if (DateTime.UtcNow > data.CreatedAt.AddMinutes(5))
            {
                _logger.LogWarning($"2FA code expired for user {userId}");
                _cache.Remove(cacheKey);
                return false;
            }

            // Increment attempts
            var attemptsCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Size = 1 // Required when SizeLimit is set
            };
            _cache.Set(attemptsKey, attempts + 1, attemptsCacheOptions);

            // Validate code
            if (data.Code == code)
            {
                _logger.LogInformation($"2FA code validated successfully for user {userId}");
                _cache.Remove(cacheKey);
                _cache.Remove(attemptsKey);
                return true;
            }

            _logger.LogWarning($"Invalid 2FA code attempt for user {userId}");
            return false;
        }

        public async Task SendCodeByEmailAsync(string email, string code)
        {
            var subject = "Your GCC Database 2FA Code";
            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #92C83E 0%, #447638 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .code {{ background: #fff; padding: 20px; margin: 20px 0; border-radius: 8px; text-align: center; border: 2px solid #92C83E; }}
                        .code-number {{ font-size: 32px; font-weight: bold; color: #002B76; letter-spacing: 5px; }}
                        .warning {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>🔐 Two-Factor Authentication</h2>
                            <p>GreenCircle Certified Database</p>
                        </div>
                        <div class='content'>
                            <h3>Your verification code is:</h3>
                            <div class='code'>
                                <div class='code-number'>{code}</div>
                            </div>
                            <div class='warning'>
                                <strong>⚠️ Important:</strong>
                                <ul>
                                    <li>This code will expire in <strong>5 minutes</strong></li>
                                    <li>Do not share this code with anyone</li>
                                    <li>If you didn't request this code, please contact support</li>
                                </ul>
                            </div>
                            <p>Enter this code on the verification page to complete your login.</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message from GCC Database System.<br>
                            Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(email, subject, body);
            _logger.LogInformation($"2FA code sent to {email}");
        }

        public async Task<bool> ResendCodeAsync(string userId, string email)
        {
            var resendKey = string.Format(RESEND_CACHE_KEY, userId);
            
            // Check if user has already requested a resend recently (rate limiting)
            if (_cache.TryGetValue(resendKey, out _))
            {
                _logger.LogWarning($"Resend rate limit hit for user {userId}");
                return false;
            }

            // Generate and send new code
            var newCode = GenerateCode();
            StoreCode(userId, newCode);
            await SendCodeByEmailAsync(email, newCode);
            
            // Set resend rate limit (60 seconds)
            var resendCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
                Size = 1 // Required when SizeLimit is set
            };
            _cache.Set(resendKey, true, resendCacheOptions);
            
            _logger.LogInformation($"2FA code resent to {email} for user {userId}");
            return true;
        }
    }

}