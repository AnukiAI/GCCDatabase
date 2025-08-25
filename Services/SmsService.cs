using System.Text;
using System.Text.Json;

namespace GCCWebApp.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string message);
        Task<bool> TestConnectionAsync();
        string GetProviderName();
    }

    public class TwilioSmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TwilioSmsService> _logger;
        private readonly HttpClient _httpClient;

        public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            var twilioSettings = _configuration.GetSection("SmsSettings:Twilio");
            var accountSid = twilioSettings["AccountSid"];
            var authToken = twilioSettings["AuthToken"];
            var fromNumber = twilioSettings["FromNumber"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
            {
                throw new InvalidOperationException("Twilio SMS settings are not properly configured");
            }

            // Clean phone number format
            var cleanPhoneNumber = CleanPhoneNumber(phoneNumber);

            var url = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
            
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("To", cleanPhoneNumber),
                new("From", fromNumber),
                new("Body", message)
            };

            var content = new FormUrlEncodedContent(parameters);
            
            // Add basic authentication
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SMS sent successfully to {phoneNumber} via Twilio");
                }
                else
                {
                    _logger.LogError($"Failed to send SMS via Twilio: {response.StatusCode} - {responseContent}");
                    throw new InvalidOperationException($"Failed to send SMS: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending SMS to {phoneNumber} via Twilio");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var twilioSettings = _configuration.GetSection("SmsSettings:Twilio");
                var testNumber = twilioSettings["TestNumber"];
                
                if (!string.IsNullOrEmpty(testNumber))
                {
                    await SendSmsAsync(testNumber, "Test SMS from GCC Database System");
                    return true;
                }
                
                _logger.LogWarning("No test number configured for Twilio SMS testing");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Twilio SMS connection test failed");
                return false;
            }
        }

        public string GetProviderName() => "Twilio";

        private string CleanPhoneNumber(string phoneNumber)
        {
            // Remove all non-numeric characters except +
            var cleaned = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());
            
            // If no country code, assume US (+1)
            if (!cleaned.StartsWith("+"))
            {
                if (cleaned.Length == 10)
                {
                    cleaned = "+1" + cleaned;
                }
                else if (cleaned.Length == 11 && cleaned.StartsWith("1"))
                {
                    cleaned = "+" + cleaned;
                }
            }
            
            return cleaned;
        }
    }

    public class TextBeltSmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TextBeltSmsService> _logger;
        private readonly HttpClient _httpClient;

        public TextBeltSmsService(IConfiguration configuration, ILogger<TextBeltSmsService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            var textBeltSettings = _configuration.GetSection("SmsSettings:TextBelt");
            var apiKey = textBeltSettings["ApiKey"];

            // TextBelt has a free tier with "textbelt" as key for testing
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = "textbelt"; // Free tier (limited)
                _logger.LogWarning("Using TextBelt free tier - limited messages per day");
            }

            var cleanPhoneNumber = CleanPhoneNumber(phoneNumber);
            var url = "https://textbelt.com/text";

            var payload = new
            {
                phone = cleanPhoneNumber,
                message = message,
                key = apiKey
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    _logger.LogInformation($"SMS sent successfully to {phoneNumber} via TextBelt");
                }
                else
                {
                    var error = result.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : "Unknown error";
                    _logger.LogError($"Failed to send SMS via TextBelt: {error}");
                    throw new InvalidOperationException($"Failed to send SMS: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending SMS to {phoneNumber} via TextBelt");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var textBeltSettings = _configuration.GetSection("SmsSettings:TextBelt");
                var testNumber = textBeltSettings["TestNumber"];
                
                if (!string.IsNullOrEmpty(testNumber))
                {
                    await SendSmsAsync(testNumber, "Test SMS from GCC Database System");
                    return true;
                }
                
                _logger.LogWarning("No test number configured for TextBelt SMS testing");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TextBelt SMS connection test failed");
                return false;
            }
        }

        public string GetProviderName() => "TextBelt";

        private string CleanPhoneNumber(string phoneNumber)
        {
            // Remove all non-numeric characters
            var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
            
            // If 10 digits, assume US number
            if (cleaned.Length == 10)
            {
                return cleaned;
            }
            
            // If 11 digits and starts with 1, remove the 1
            if (cleaned.Length == 11 && cleaned.StartsWith("1"))
            {
                return cleaned.Substring(1);
            }
            
            return cleaned;
        }
    }

    public class MockSmsService : ISmsService
    {
        private readonly ILogger<MockSmsService> _logger;

        public MockSmsService(ILogger<MockSmsService> logger)
        {
            _logger = logger;
        }

        public Task SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogWarning($"MOCK SMS: To={phoneNumber}");
            _logger.LogInformation($"MOCK SMS MESSAGE: {message}");
            return Task.CompletedTask;
        }

        public Task<bool> TestConnectionAsync()
        {
            _logger.LogInformation("Mock SMS service - connection test always returns true");
            return Task.FromResult(true);
        }

        public string GetProviderName() => "Mock";
    }

    // Factory to create appropriate SMS service based on configuration
    public class SmsServiceFactory
    {
        public static ISmsService CreateSmsService(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var smsProvider = configuration.GetValue<string>("SmsSettings:Provider")?.ToLower() ?? "mock";

            return smsProvider switch
            {
                "twilio" => serviceProvider.GetRequiredService<TwilioSmsService>(),
                "textbelt" => serviceProvider.GetRequiredService<TextBeltSmsService>(),
                _ => serviceProvider.GetRequiredService<MockSmsService>()
            };
        }
    }
}