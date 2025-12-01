using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using GCCWebApp.Models.Api;
using GCCWebApp.Services;

namespace GCCWebApp.Middleware
{
    /// <summary>
    /// Middleware for authenticating Partner API requests using X-API-Key header
    /// Partners (Amazon, Wayfair, etc.) use API keys to access GCC REST API endpoints
    /// </summary>
    public class PartnerApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PartnerApiKeyAuthenticationMiddleware> _logger;

        public PartnerApiKeyAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<PartnerApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPartnerApiKeyService apiKeyService)
        {
            // Only process API requests
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            // Skip if already authenticated (cookie/JWT takes precedence)
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            // Check for X-API-Key header
            if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValue))
            {
                // No API key provided, continue to next middleware
                await _next(context);
                return;
            }

            var apiKey = apiKeyValue.ToString();

            // Validate API key format - only process keys with gcc_partner_ prefix
            // Non-partner keys (e.g., general API keys) should be handled by ApiKeyAuthenticationHandler
            if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("gcc_partner_"))
            {
                _logger.LogDebug("API key does not have partner prefix, passing to next middleware: {ApiKey}",
                    apiKey?.Substring(0, Math.Min(20, apiKey?.Length ?? 0)));

                // Continue to next middleware (ApiKeyAuthenticationHandler will handle non-partner keys)
                await _next(context);
                return;
            }

            try
            {
                // Validate API key
                var isValid = await apiKeyService.ValidateApiKeyAsync(apiKey);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid or expired API key used: {ApiKey}",
                        apiKey.Substring(0, Math.Min(30, apiKey.Length)));

                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";

                    var errorResponse = StandardApiResponse<object>.ErrorResult(
                        "Unauthorized: Invalid or expired API key",
                        ApiErrorCodes.Unauthorized);

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }));
                    return;
                }

                // Get API key details
                var keyDetails = await apiKeyService.GetApiKeyByValueAsync(apiKey);
                if (keyDetails == null || keyDetails.Partner == null)
                {
                    _logger.LogError("API key validated but details not found: {ApiKey}",
                        apiKey.Substring(0, Math.Min(30, apiKey.Length)));

                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";

                    var errorResponse = StandardApiResponse<object>.ErrorResult(
                        "Unauthorized: API key not found",
                        ApiErrorCodes.Unauthorized);

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }));
                    return;
                }

                // Check endpoint permissions
                var requestPath = context.Request.Path.Value ?? string.Empty;
                if (!IsEndpointAllowed(requestPath, keyDetails.AllowedEndpoints))
                {
                    _logger.LogWarning("Partner {Partner} attempted to access restricted endpoint: {Path}",
                        keyDetails.Partner.Name, requestPath);

                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";

                    var errorResponse = StandardApiResponse<object>.ErrorResult(
                        "Forbidden: This API key does not have permission to access this endpoint",
                        ApiErrorCodes.Forbidden);

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }));
                    return;
                }

                // Create claims identity for partner
                var claims = new List<Claim>
                {
                    new Claim("partner_id", keyDetails.PartnerId.ToString()),
                    new Claim("partner_name", keyDetails.Partner.Name),
                    new Claim("api_key_id", keyDetails.Id.ToString()),
                    new Claim("partner_rate_limit", keyDetails.RateLimitPerHour?.ToString() ?? "2000"),
                    new Claim(ClaimTypes.Role, "Partner"),
                    new Claim(ClaimTypes.AuthenticationMethod, "PartnerApiKey")
                };

                var identity = new ClaimsIdentity(claims, "PartnerApiKey");
                var principal = new ClaimsPrincipal(identity);
                context.User = principal;

                _logger.LogInformation("Partner API authenticated: {Partner} (ID: {PartnerId}) accessing {Path}",
                    keyDetails.Partner.Name, keyDetails.PartnerId, requestPath);

                // Update usage statistics inline (await to avoid DbContext disposal issues)
                try
                {
                    await apiKeyService.UpdateLastUsedAsync(apiKey);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    _logger.LogWarning(ex, "Failed to update API key usage statistics for {ApiKeyId}",
                        keyDetails.Id);
                }

                // Continue to next middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Partner API key authentication");

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var errorResponse = StandardApiResponse<object>.ErrorResult(
                    "Internal server error during authentication",
                    ApiErrorCodes.InternalError);

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
            }
        }

        /// <summary>
        /// Checks if the requested endpoint is allowed based on the AllowedEndpoints configuration
        /// </summary>
        private bool IsEndpointAllowed(string requestPath, string? allowedEndpointsJson)
        {
            // If no restrictions configured, allow all endpoints
            if (string.IsNullOrWhiteSpace(allowedEndpointsJson))
            {
                return true;
            }

            try
            {
                // Parse JSON array of allowed endpoint patterns
                var allowedEndpoints = JsonSerializer.Deserialize<string[]>(allowedEndpointsJson);
                if (allowedEndpoints == null || allowedEndpoints.Length == 0)
                {
                    return true; // No restrictions
                }

                // Check if request path matches any allowed pattern
                foreach (var pattern in allowedEndpoints)
                {
                    if (MatchesWildcardPattern(requestPath, pattern))
                    {
                        return true;
                    }
                }

                // No matching pattern found
                return false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AllowedEndpoints JSON: {Json}", allowedEndpointsJson);
                // On parse error, allow access (fail open for this check)
                return true;
            }
        }

        /// <summary>
        /// Matches a request path against a wildcard pattern
        /// Examples:
        /// - /api/v1/certificates/* matches /api/v1/certificates/123
        /// - /api/v1/* matches /api/v1/certificates, /api/v1/products, etc.
        /// - /api/v1/certificates matches /api/v1/certificates exactly
        /// </summary>
        private bool MatchesWildcardPattern(string path, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            // Exact match
            if (pattern == path)
            {
                return true;
            }

            // No wildcard - exact match only
            if (!pattern.Contains('*'))
            {
                return pattern.Equals(path, StringComparison.OrdinalIgnoreCase);
            }

            // Convert wildcard pattern to regex
            // Escape special regex characters except *
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";

            return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
        }
    }
}
