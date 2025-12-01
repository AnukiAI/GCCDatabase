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
            IApiKeyService apiKeyService)
            : base(options, logger, encoder)
        {
            _apiKeyService = apiKeyService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Skip if already authenticated by Partner API key middleware
            if (Context.User?.Identity?.AuthenticationType == "PartnerApiKey")
            {
                return AuthenticateResult.Success(new AuthenticationTicket(Context.User, Options.Scheme));
            }

            // Check for API key in header
            if (!Request.Headers.ContainsKey(Options.HeaderName))
            {
                return AuthenticateResult.NoResult();
            }

            string? apiKey = Request.Headers[Options.HeaderName];

            // Skip partner API keys - they are handled by PartnerApiKeyAuthenticationMiddleware
            if (!string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("gcc_partner_"))
            {
                // Partner keys should have been authenticated by middleware before this handler runs
                // If we reach here, the partner key middleware rejected it or didn't run
                Logger.LogDebug("Partner API key detected, deferring to PartnerApiKeyAuthenticationMiddleware");
                return AuthenticateResult.NoResult();
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                Logger.LogWarning("Empty API key provided from IP: {ClientIP}", 
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString());
                return AuthenticateResult.Fail("Invalid API Key");
            }

            // Enhanced security validations
            var clientIP = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();
            var origin = Request.Headers["Origin"].FirstOrDefault();
            var referer = Request.Headers["Referer"].FirstOrDefault();

            // Check if request is over HTTPS in production
            if (!Request.IsHttps && !Request.Host.Host.Contains("localhost"))
            {
                Logger.LogWarning("API Key used over insecure connection from IP: {ClientIP}", clientIP);
                return AuthenticateResult.Fail("HTTPS required");
            }

            var validApiKey = await _apiKeyService.ValidateApiKeyAsync(apiKey);

            if (validApiKey == null)
            {
                Logger.LogWarning("Invalid API key attempt from IP: {ClientIP}, UserAgent: {UserAgent}", clientIP, userAgent);
                return AuthenticateResult.Fail("Invalid API Key");
            }

            // Enhanced security checks
            if (!validApiKey.CanBeUsed)
            {
                Logger.LogWarning("Blocked API Key {ApiKeyName} attempted use from IP: {ClientIP}", validApiKey.Name, clientIP);
                return AuthenticateResult.Fail("API Key is blocked or expired");
            }

            if (validApiKey.RequireTLS && !Request.IsHttps)
            {
                Logger.LogWarning("API Key {ApiKeyName} requires TLS but used over HTTP from IP: {ClientIP}", validApiKey.Name, clientIP);
                return AuthenticateResult.Fail("HTTPS required for this API key");
            }

            if (!_apiKeyService.IsValidIP(validApiKey, clientIP ?? "Unknown"))
            {
                Logger.LogWarning("API Key {ApiKeyName} used from unauthorized IP: {ClientIP}", validApiKey.Name, clientIP);
                await _apiKeyService.RecordSecurityViolationAsync(validApiKey.Id, "Unauthorized IP", clientIP ?? "Unknown");
                return AuthenticateResult.Fail("Unauthorized IP address");
            }

            if (!_apiKeyService.IsValidOrigin(validApiKey, origin ?? "Unknown"))
            {
                Logger.LogWarning("API Key {ApiKeyName} used from unauthorized origin: {Origin} from IP: {ClientIP}", validApiKey.Name, origin, clientIP);
                await _apiKeyService.RecordSecurityViolationAsync(validApiKey.Id, "Unauthorized Origin", origin ?? "Unknown");
                return AuthenticateResult.Fail("Unauthorized origin");
            }

            // Validate User-Agent if specified
            if (!string.IsNullOrEmpty(validApiKey.UserAgent) && userAgent != validApiKey.UserAgent)
            {
                Logger.LogWarning("API Key {ApiKeyName} used with unexpected User-Agent: {UserAgent}", validApiKey.Name, userAgent);
                await _apiKeyService.RecordSecurityViolationAsync(validApiKey.Id, "Unexpected User-Agent", userAgent ?? "Unknown");
                return AuthenticateResult.Fail("Unauthorized client");
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