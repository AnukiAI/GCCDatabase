using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using QRCoder;

namespace GCCWebApp.Services
{
    public interface ITwoFactorService
    {
        string GenerateCode();
        bool ValidateCode(string userId, string code);
        Task SendCodeByEmailAsync(string email, string code);
        void StoreCode(string userId, string code);
        Task<bool> ResendCodeAsync(string userId, string email);
        
        // TOTP Methods
        string GenerateTotpSecret();
        string GenerateQrCodeUri(string email, string secret, string issuer = "GCC Database");
        byte[] GenerateQrCodeImage(string qrCodeUri);
        bool ValidateTotpCode(string secret, string userCode);
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
                Priority = CacheItemPriority.High
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
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
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
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            };
            _cache.Set(resendKey, true, resendCacheOptions);
            
            _logger.LogInformation($"2FA code resent to {email} for user {userId}");
            return true;
        }

        #region TOTP Methods

        public string GenerateTotpSecret()
        {
            var key = new byte[20]; // 160-bit key (recommended for TOTP)
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return Base32Encode(key);
        }

        public string GenerateQrCodeUri(string email, string secret, string issuer = "GCC Database")
        {
            var label = $"{issuer}:{email}";
            return $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
        }

        public byte[] GenerateQrCodeImage(string qrCodeUri)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new BitmapByteQRCode(qrCodeData);
                return qrCode.GetGraphic(20); // 20 pixels per module
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate QR code image");
                throw;
            }
        }

        public bool ValidateTotpCode(string secret, string userCode)
        {
            try
            {
                if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(userCode))
                    return false;

                if (userCode.Length != 6 || !userCode.All(char.IsDigit))
                    return false;

                var secretBytes = Base32Decode(secret);
                var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var timeStep = unixTime / 30; // 30-second time window
                
                // Check current time window and ±1 window for clock drift tolerance
                for (int i = -1; i <= 1; i++)
                {
                    var expectedCode = GenerateTotpCode(secretBytes, timeStep + i);
                    if (expectedCode == userCode)
                    {
                        _logger.LogInformation("TOTP code validated successfully");
                        return true;
                    }
                }
                
                _logger.LogWarning("TOTP code validation failed");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating TOTP code");
                return false;
            }
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
    }
}