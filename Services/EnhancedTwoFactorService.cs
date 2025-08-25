using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public enum TwoFactorDeliveryMethod
    {
        Email,
        SMS,
        Both // Send to both email and SMS
    }

    public class TwoFactorPreferences
    {
        public string UserId { get; set; } = string.Empty;
        public TwoFactorDeliveryMethod PreferredMethod { get; set; } = TwoFactorDeliveryMethod.Email;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
    }

    public interface IEnhancedTwoFactorService
    {
        string GenerateCode();
        bool ValidateCode(string userId, string code);
        Task SendCodeAsync(string userId, string code, TwoFactorDeliveryMethod method, string? email = null, string? phoneNumber = null);
        Task SendCodeByEmailAsync(string email, string code);
        Task SendCodeBySmsAsync(string phoneNumber, string code);
        void StoreCode(string userId, string code);
        Task<bool> ResendCodeAsync(string userId, TwoFactorPreferences preferences);
        Task<TwoFactorPreferences> GetUserPreferencesAsync(string userId);
        Task SaveUserPreferencesAsync(TwoFactorPreferences preferences);
        Task<bool> VerifyEmailAsync(string userId, string email, string verificationCode);
        Task<bool> VerifyPhoneAsync(string userId, string phoneNumber, string verificationCode);
        Task SendVerificationCodeAsync(string userId, string contact, TwoFactorDeliveryMethod method);
    }

    public class EnhancedTwoFactorService : IEnhancedTwoFactorService
    {
        private readonly ILogger<EnhancedTwoFactorService> _logger;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        
        // Cache keys
        private const string CODE_CACHE_KEY = "2fa_code_{0}";
        private const string ATTEMPTS_CACHE_KEY = "2fa_attempts_{0}";
        private const string RESEND_CACHE_KEY = "2fa_resend_{0}";
        private const string PREFERENCES_CACHE_KEY = "2fa_prefs_{0}";
        private const string VERIFICATION_CACHE_KEY = "2fa_verify_{0}_{1}"; // userId_method

        public EnhancedTwoFactorService(
            ILogger<EnhancedTwoFactorService> logger,
            IEmailService emailService,
            ISmsService smsService,
            IMemoryCache cache,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _logger = logger;
            _emailService = emailService;
            _smsService = smsService;
            _cache = cache;
            _configuration = configuration;
            _context = context;
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

        public async Task SendCodeAsync(string userId, string code, TwoFactorDeliveryMethod method, string? email = null, string? phoneNumber = null)
        {
            var tasks = new List<Task>();

            switch (method)
            {
                case TwoFactorDeliveryMethod.Email:
                    if (!string.IsNullOrEmpty(email))
                    {
                        tasks.Add(SendCodeByEmailAsync(email, code));
                    }
                    break;

                case TwoFactorDeliveryMethod.SMS:
                    if (!string.IsNullOrEmpty(phoneNumber))
                    {
                        tasks.Add(SendCodeBySmsAsync(phoneNumber, code));
                    }
                    break;

                case TwoFactorDeliveryMethod.Both:
                    if (!string.IsNullOrEmpty(email))
                    {
                        tasks.Add(SendCodeByEmailAsync(email, code));
                    }
                    if (!string.IsNullOrEmpty(phoneNumber))
                    {
                        tasks.Add(SendCodeBySmsAsync(phoneNumber, code));
                    }
                    break;
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
                _logger.LogInformation($"2FA code sent via {method} for user {userId}");
            }
            else
            {
                throw new InvalidOperationException($"No valid contact information provided for {method} delivery");
            }
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
                            <h2>📧 Two-Factor Authentication</h2>
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
        }

        public async Task SendCodeBySmsAsync(string phoneNumber, string code)
        {
            var message = $"Your GCC Database verification code is: {code}. This code expires in 5 minutes. Do not share this code with anyone.";
            await _smsService.SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> ResendCodeAsync(string userId, TwoFactorPreferences preferences)
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
            
            await SendCodeAsync(userId, newCode, preferences.PreferredMethod, preferences.Email, preferences.PhoneNumber);
            
            // Set resend rate limit (60 seconds)
            var resendCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
                Size = 1 // Required when SizeLimit is set
            };
            _cache.Set(resendKey, true, resendCacheOptions);
            
            _logger.LogInformation($"2FA code resent via {preferences.PreferredMethod} for user {userId}");
            return true;
        }

        public async Task<TwoFactorPreferences> GetUserPreferencesAsync(string userId)
        {
            var cacheKey = string.Format(PREFERENCES_CACHE_KEY, userId);
            
            if (_cache.TryGetValue(cacheKey, out TwoFactorPreferences? cached) && cached != null)
            {
                return cached;
            }

            // Query the database for user preferences
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
            
            var preferences = new TwoFactorPreferences
            {
                UserId = userId,
                PreferredMethod = user?.TwoFactorMethod switch
                {
                    "SMS" => TwoFactorDeliveryMethod.SMS,
                    "Both" => TwoFactorDeliveryMethod.Both,
                    _ => TwoFactorDeliveryMethod.Email
                },
                Email = user?.Email ?? userId,
                PhoneNumber = user?.PhoneNumber,
                IsEmailVerified = user?.IsEmailVerified ?? true,
                IsPhoneVerified = user?.IsPhoneVerified ?? false
            };

            // Cache for 30 minutes
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                Size = 1 // Required when SizeLimit is set
            };
            _cache.Set(cacheKey, preferences, cacheOptions);
            
            return preferences;
        }

        public async Task SaveUserPreferencesAsync(TwoFactorPreferences preferences)
        {
            var cacheKey = string.Format(PREFERENCES_CACHE_KEY, preferences.UserId);
            
            // Update the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == preferences.UserId);
            if (user != null)
            {
                user.TwoFactorMethod = preferences.PreferredMethod.ToString();
                user.PhoneNumber = preferences.PhoneNumber;
                user.IsPhoneVerified = preferences.IsPhoneVerified;
                user.IsEmailVerified = preferences.IsEmailVerified;
                user.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
            }
            
            // Update cache
            var updateCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                Size = 1 // Required when SizeLimit is set
            };
            _cache.Set(cacheKey, preferences, updateCacheOptions);
            
            _logger.LogInformation($"2FA preferences saved for user {preferences.UserId}: Method={preferences.PreferredMethod}");
        }

        public async Task SendVerificationCodeAsync(string userId, string contact, TwoFactorDeliveryMethod method)
        {
            var code = GenerateCode();
            var verificationKey = string.Format(VERIFICATION_CACHE_KEY, userId, method.ToString().ToLower());
            
            // Store verification code
            var verificationCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                Size = 1 // Required when SizeLimit is set
            };
            _cache.Set(verificationKey, new { Code = code, Contact = contact }, verificationCacheOptions);

            switch (method)
            {
                case TwoFactorDeliveryMethod.Email:
                    await SendVerificationEmailAsync(contact, code);
                    break;
                case TwoFactorDeliveryMethod.SMS:
                    await SendVerificationSmsAsync(contact, code);
                    break;
            }

            _logger.LogInformation($"Verification code sent to {contact} via {method} for user {userId}");
        }

        public async Task<bool> VerifyEmailAsync(string userId, string email, string verificationCode)
        {
            var verificationKey = string.Format(VERIFICATION_CACHE_KEY, userId, "email");
            
            if (_cache.TryGetValue(verificationKey, out dynamic? stored) && stored != null)
            {
                if (stored.Code == verificationCode && stored.Contact == email)
                {
                    _cache.Remove(verificationKey);
                    
                    // Update user preferences to mark email as verified
                    var preferences = await GetUserPreferencesAsync(userId);
                    preferences.Email = email;
                    preferences.IsEmailVerified = true;
                    await SaveUserPreferencesAsync(preferences);
                    
                    _logger.LogInformation($"Email {email} verified for user {userId}");
                    return true;
                }
            }
            
            _logger.LogWarning($"Failed to verify email {email} for user {userId}");
            return false;
        }

        public async Task<bool> VerifyPhoneAsync(string userId, string phoneNumber, string verificationCode)
        {
            var verificationKey = string.Format(VERIFICATION_CACHE_KEY, userId, "sms");
            
            if (_cache.TryGetValue(verificationKey, out dynamic? stored) && stored != null)
            {
                if (stored.Code == verificationCode && stored.Contact == phoneNumber)
                {
                    _cache.Remove(verificationKey);
                    
                    // Update user preferences to mark phone as verified
                    var preferences = await GetUserPreferencesAsync(userId);
                    preferences.PhoneNumber = phoneNumber;
                    preferences.IsPhoneVerified = true;
                    await SaveUserPreferencesAsync(preferences);
                    
                    _logger.LogInformation($"Phone {phoneNumber} verified for user {userId}");
                    return true;
                }
            }
            
            _logger.LogWarning($"Failed to verify phone {phoneNumber} for user {userId}");
            return false;
        }

        private async Task SendVerificationEmailAsync(string email, string code)
        {
            var subject = "Verify Your Email - GCC Database";
            var body = $@"
                <h2>Email Verification</h2>
                <p>Please verify your email address by entering this code:</p>
                <p style='font-size: 24px; font-weight: bold; color: #002B76;'>{code}</p>
                <p>This verification code will expire in 10 minutes.</p>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task SendVerificationSmsAsync(string phoneNumber, string code)
        {
            var message = $"Your GCC Database email verification code is: {code}. This code expires in 10 minutes.";
            await _smsService.SendSmsAsync(phoneNumber, message);
        }
    }

    // Keep the original for backward compatibility
    public class TwoFactorData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Attempts { get; set; }
    }
}