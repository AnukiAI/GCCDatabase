using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using GCCWebApp.Services;

namespace GCCWebApp.Authentication
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "ApiKey";
        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
        public string HeaderName { get; set; } = "X-API-Key";
    }

    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly IApiKeyService _apiKeyService;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IApiKeyService apiKeyService)
            : base(options, logger, encoder, clock)
        {
            _apiKeyService = apiKeyService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(Options.HeaderName))
            {
                return AuthenticateResult.NoResult();
            }

            string apiKey = Request.Headers[Options.HeaderName];

            if (string.IsNullOrEmpty(apiKey))
            {
                return AuthenticateResult.Fail("Invalid API Key");
            }

            var validApiKey = await _apiKeyService.ValidateApiKeyAsync(apiKey);

            if (validApiKey == null)
            {
                return AuthenticateResult.Fail("Invalid API Key");
            }

            var clientIP = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var origin = Request.Headers["Origin"].FirstOrDefault();

            if (!_apiKeyService.IsValidIP(validApiKey, clientIP))
            {
                Logger.LogWarning($"API Key {validApiKey.Name} used from unauthorized IP: {clientIP}");
                return AuthenticateResult.Fail("Unauthorized IP address");
            }

            if (!_apiKeyService.IsValidOrigin(validApiKey, origin))
            {
                Logger.LogWarning($"API Key {validApiKey.Name} used from unauthorized origin: {origin}");
                return AuthenticateResult.Fail("Unauthorized origin");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, validApiKey.Name),
                new(ClaimTypes.NameIdentifier, validApiKey.Id.ToString()),
                new("api_key_id", validApiKey.Id.ToString()),
                new("api_key_name", validApiKey.Name),
                new("created_by", validApiKey.CreatedBy ?? "Unknown")
            };

            if (!string.IsNullOrEmpty(validApiKey.Scopes))
            {
                var scopes = validApiKey.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var scope in scopes)
                {
                    claims.Add(new Claim("scope", scope.Trim()));
                }
            }

            var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
            var principal = new ClaimsPrincipal(identity);

            await _apiKeyService.UpdateLastUsedAsync(apiKey);

            return AuthenticateResult.Success(new AuthenticationTicket(principal, Options.Scheme));
        }
    }
}