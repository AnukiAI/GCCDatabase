using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using QRCoder;

namespace GCCWebApp.Services
{
    public enum TwoFactorDeliveryMethod
    {
        Email,
        SMS,
        TOTP, // Time-based One-Time Password (Microsoft Authenticator)
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
        public string? TotpSecret { get; set; }
        public bool IsTotpEnabled { get; set; }
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
        
        // TOTP Methods
        Task<string> GenerateTotpSecretAsync(string userId);
        Task<string> GenerateQrCodeUriAsync(string userId, string email, string issuer = "GCC Database");
        Task<byte[]> GenerateQrCodeImageAsync(string userId);
        Task<bool> ValidateTotpCodeAsync(string userId, string userCode);
        Task<bool> EnableTotpAsync(string userId, string userCode);
        Task<bool> DisableTotpAsync(string userId);

        // Recovery Code Methods
        Task<List<string>> GenerateRecoveryCodesAsync(string userId, int count = 10);
        Task<bool> ValidateRecoveryCodeAsync(string userId, string code);
        Task<int> GetRemainingRecoveryCodeCountAsync(string userId);
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
                // DEF-365: "TOTP" previously fell through to the Email default, so an
                // authenticator user was emailed a code at sign-in — the wrong factor entirely, and
                // it silently made TOTP indistinguishable from email 2FA. TOTP is a *generated*
                // factor: SendCodeAsync deliberately has no TOTP branch, because the code comes
                // from the user's app and there is nothing to deliver.
                PreferredMethod = user?.TwoFactorMethod switch
                {
                    "SMS" => TwoFactorDeliveryMethod.SMS,
                    "Both" => TwoFactorDeliveryMethod.Both,
                    "TOTP" => TwoFactorDeliveryMethod.TOTP,
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
                if (stored?.Code == verificationCode && stored?.Contact == email)
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
                if (stored?.Code == verificationCode && stored?.Contact == phoneNumber)
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

        #region TOTP Methods

        public async Task<string> GenerateTotpSecretAsync(string userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"User not found: {userId}");
                }

                if (!string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    return user.TwoFactorSecret; // Return existing secret
                }

                var secret = GenerateTotpSecret();
                user.TwoFactorSecret = secret;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"TOTP secret generated for user {userId}");
                return secret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to generate TOTP secret for user {userId}");
                throw;
            }
        }

        public async Task<string> GenerateQrCodeUriAsync(string userId, string email, string issuer = "GCC Database")
        {
            var secret = await GenerateTotpSecretAsync(userId);
            var label = $"{issuer}:{email}";
            return $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
        }

        public async Task<byte[]> GenerateQrCodeImageAsync(string userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
                if (user?.TwoFactorSecret == null)
                {
                    throw new InvalidOperationException("No TOTP secret found for user");
                }

                var qrCodeUri = await GenerateQrCodeUriAsync(userId, user.Email);
                
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new BitmapByteQRCode(qrCodeData);
                return qrCode.GetGraphic(20); // 20 pixels per module
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to generate QR code image for user {userId}");
                throw;
            }
        }

        public async Task<bool> ValidateTotpCodeAsync(string userId, string userCode)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
                if (user?.TwoFactorSecret == null)
                {
                    _logger.LogWarning($"No TOTP secret found for user {userId}");
                    return false;
                }

                if (string.IsNullOrEmpty(userCode) || userCode.Length != 6 || !userCode.All(char.IsDigit))
                {
                    _logger.LogWarning($"Invalid TOTP code format for user {userId}");
                    return false;
                }

                var secretBytes = Base32Decode(user.TwoFactorSecret);
                var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var timeStep = unixTime / 30; // 30-second time window
                
                // Check current time window and ±1 window for clock drift tolerance
                for (int i = -1; i <= 1; i++)
                {
                    var expectedCode = GenerateTotpCode(secretBytes, timeStep + i);
                    if (expectedCode == userCode)
                    {
                        _logger.LogInformation($"TOTP code validated successfully for user {userId}");
                        return true;
                    }
                }
                
                _logger.LogWarning($"TOTP code validation failed for user {userId}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating TOTP code for user {userId}");
                return false;
            }
        }

        public async Task<bool> EnableTotpAsync(string userId, string userCode)
        {
            try
            {
                if (!await ValidateTotpCodeAsync(userId, userCode))
                {
                    return false; // Invalid verification code
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
                if (user == null)
                {
                    return false;
                }

                user.TwoFactorEnabled = true;
                user.TwoFactorMethod = "TOTP";
                await _context.SaveChangesAsync();

                _logger.LogInformation($"TOTP enabled for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to enable TOTP for user {userId}");
                return false;
            }
        }

        public async Task<bool> DisableTotpAsync(string userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
                if (user == null)
                {
                    return false;
                }

                user.TwoFactorEnabled = false;
                user.TwoFactorMethod = "Email"; // Fallback to email
                user.TwoFactorSecret = null; // Clear the secret
                await _context.SaveChangesAsync();

                _logger.LogInformation($"TOTP disabled for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to disable TOTP for user {userId}");
                return false;
            }
        }

        private string GenerateTotpSecret()
        {
            var key = new byte[20]; // 160-bit key (recommended for TOTP)
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return Base32Encode(key);
        }

        private string GenerateTotpCode(byte[] secretBytes, long timeStep)
        {
            var timeBytes = BitConverter.GetBytes(timeStep);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(timeBytes);

            using var hmac = new HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(timeBytes);
            
            var offset = hash[hash.Length - 1] & 0x0F;
            var binaryCode = ((hash[offset] & 0x7F) << 24) |
                           ((hash[offset + 1] & 0xFF) << 16) |
                           ((hash[offset + 2] & 0xFF) << 8) |
                           (hash[offset + 3] & 0xFF);
                           
            var code = binaryCode % 1000000;
            return code.ToString("D6");
        }

        private static string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();
            
            for (int i = 0; i < data.Length; i += 5)
            {
                var chunk = new byte[5];
                Array.Copy(data, i, chunk, 0, Math.Min(5, data.Length - i));
                
                var buffer = (long)chunk[0] << 32 | (long)chunk[1] << 24 | (long)chunk[2] << 16 | (long)chunk[3] << 8 | chunk[4];
                
                for (int j = 7; j >= 0; j--)
                {
                    if (i * 8 / 5 + j < (data.Length * 8 + 4) / 5)
                        result.Append(alphabet[(int)(buffer >> (5 * j)) & 0x1F]);
                }
            }
            
            return result.ToString();
        }

        private static byte[] Base32Decode(string encoded)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            encoded = encoded.ToUpper().Replace("=", "");
            
            var result = new List<byte>();
            var buffer = 0L;
            var bitsLeft = 0;
            
            foreach (var c in encoded)
            {
                var value = alphabet.IndexOf(c);
                if (value < 0) continue;
                
                buffer = (buffer << 5) | (uint)value;
                bitsLeft += 5;
                
                if (bitsLeft >= 8)
                {
                    result.Add((byte)(buffer >> (bitsLeft - 8)));
                    bitsLeft -= 8;
                }
            }
            
            return result.ToArray();
        }

        #endregion

        #region Recovery Code Methods

        public async Task<List<string>> GenerateRecoveryCodesAsync(string userId, int count = 10)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
            if (user == null)
                throw new InvalidOperationException($"User not found: {userId}");

            var plainCodes = new List<string>();
            var hashedCodes = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var code = GenerateRecoveryCode();
                plainCodes.Add(code);
                hashedCodes.Add(HashRecoveryCode(code));
            }

            user.RecoveryCodes = System.Text.Json.JsonSerializer.Serialize(hashedCodes);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated {Count} recovery codes for user {UserId}", count, userId);
            return plainCodes;
        }

        public async Task<bool> ValidateRecoveryCodeAsync(string userId, string code)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
            if (user == null || string.IsNullOrEmpty(user.RecoveryCodes))
                return false;

            var hashedCodes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.RecoveryCodes);
            if (hashedCodes == null || hashedCodes.Count == 0)
                return false;

            var hashedInput = HashRecoveryCode(code);
            var matchIndex = hashedCodes.IndexOf(hashedInput);

            if (matchIndex >= 0)
            {
                hashedCodes.RemoveAt(matchIndex);
                user.RecoveryCodes = System.Text.Json.JsonSerializer.Serialize(hashedCodes);
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Recovery code used for user {UserId}. {Remaining} codes remaining", userId, hashedCodes.Count);
                return true;
            }

            _logger.LogWarning("Invalid recovery code attempt for user {UserId}", userId);
            return false;
        }

        public async Task<int> GetRemainingRecoveryCodeCountAsync(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
            if (user == null || string.IsNullOrEmpty(user.RecoveryCodes))
                return 0;

            var codes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.RecoveryCodes);
            return codes?.Count ?? 0;
        }

        private static string GenerateRecoveryCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Ambiguity-free charset (no I, O, 0, 1)
            var code = new char[8];
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            for (int i = 0; i < 8; i++)
                code[i] = chars[bytes[i] % chars.Length];
            return new string(code);
        }

        private static string HashRecoveryCode(string code)
        {
            var normalized = code.Replace("-", "").ToUpperInvariant();
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash);
        }

        #endregion
    }

    // Keep the original for backward compatibility
    public class TwoFactorData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Attempts { get; set; }
    }
}