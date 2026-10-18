using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using GCCWebApp.Models.Api;

namespace GCCWebApp.Middleware
{
    public class ApiRateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiRateLimitingMiddleware> _logger;
        private readonly RateLimitOptions _options;
        private readonly ConcurrentDictionary<string, RateLimitCounter> _counters = new();

        public ApiRateLimitingMiddleware(
            RequestDelegate next,
            ILogger<ApiRateLimitingMiddleware> logger,
            RateLimitOptions options)
        {
            _next = next;
            _logger = logger;
            _options = options;
        }

        // Internal browser-to-server API paths that should not be rate-limited
        private static readonly string[] ExemptApiPaths = new[]
        {
            "/api/CertificateTiles",
            "/api/certificate-tiles",
            "/api/certificate-tiles-internal",
            "/api/CertificateTileExternal"
        };

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            // Skip rate limiting for internal tile API calls (used by page views)
            if (ExemptApiPaths.Any(p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            var clientId = GetClientIdentifier(context);
            var rateLimitKey = $"{clientId}:{DateTime.UtcNow:yyyyMMddHH}";

            var counter = _counters.AddOrUpdate(rateLimitKey,
                k => new RateLimitCounter { Count = 1, FirstRequestTime = DateTime.UtcNow },
                (k, existing) =>
                {
                    existing.Count++;
                    return existing;
                });

            var limit = GetRateLimitForClient(context);
            var remaining = Math.Max(0, limit - counter.Count);

            context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString());
            context.Response.Headers.Append("X-RateLimit-Reset", GetResetTime().ToString());

            if (counter.Count > limit)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}. Count: {Count}, Limit: {Limit}",
                    clientId, counter.Count, limit);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Append("Retry-After", "3600");

                var response = StandardApiResponse<object>.ErrorResult(
                    "Rate limit exceeded. Please try again later.",
                    ApiErrorCodes.RateLimitExceeded);

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
                return;
            }

            CleanupOldCounters();
            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var apiKeyId = context.User.FindFirst("api_key_id")?.Value;
                if (!string.IsNullOrEmpty(apiKeyId))
                    return $"api_key:{apiKeyId}";

                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                    return $"user:{userId}";
            }

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip:{ipAddress}";
        }

        private int GetRateLimitForClient(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // Check for partner-specific rate limit from claims (set by PartnerApiKeyAuthenticationMiddleware)
                var partnerRateLimitClaim = context.User.FindFirst("partner_rate_limit")?.Value;
                if (!string.IsNullOrEmpty(partnerRateLimitClaim) &&
                    int.TryParse(partnerRateLimitClaim, out var partnerLimit))
                {
                    _logger.LogDebug("Using partner-specific rate limit: {Limit} requests/hour", partnerLimit);
                    return partnerLimit; // Uses PartnerApiKey.RateLimitPerHour from database
                }

                // Check if user is authenticated via partner API key (but no custom limit)
                var partnerId = context.User.FindFirst("partner_id")?.Value;
                if (!string.IsNullOrEmpty(partnerId))
                {
                    _logger.LogDebug("Using default partner API key limit: {Limit} requests/hour", _options.PartnerApiKeyLimit);
                    return _options.PartnerApiKeyLimit; // Default: 200/hour (set in Program.cs)
                }

                // Check for general API key authentication (non-partner)
                var apiKeyId = context.User.FindFirst("api_key_id")?.Value;
                if (!string.IsNullOrEmpty(apiKeyId))
                {
                    return _options.ApiKeyLimit; // Default: 1000/hour
                }

                // Admin users get higher limits
                if (context.User.IsInRole("Admin"))
                {
                    return _options.AdminLimit; // Default: 5000/hour
                }

                // Authenticated users (cookie/JWT)
                return _options.AuthenticatedLimit; // Default: 500/hour (set in Program.cs)
            }

            // Anonymous/unauthenticated users
            return _options.AnonymousLimit; // Default: 100/hour
        }

        private long GetResetTime()
        {
            var nextHour = DateTime.UtcNow.AddHours(1);
            var resetTime = new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 0, DateTimeKind.Utc);
            return ((DateTimeOffset)resetTime).ToUnixTimeSeconds();
        }

        private void CleanupOldCounters()
        {
            var cutoff = DateTime.UtcNow.AddHours(-2);
            var keysToRemove = _counters
                .Where(kvp => kvp.Value.FirstRequestTime < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _counters.TryRemove(key, out _);
            }
        }

        private class RateLimitCounter
        {
            public int Count { get; set; }
            public DateTime FirstRequestTime { get; set; }
        }
    }

    public class RateLimitOptions
    {
        public int AnonymousLimit { get; set; } = 100;
        public int AuthenticatedLimit { get; set; } = 500;
        public int ApiKeyLimit { get; set; } = 1000;
        public int PartnerApiKeyLimit { get; set; } = 2000;
        public int AdminLimit { get; set; } = 5000;
    }

    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, Action<RateLimitOptions>? configureOptions = null)
        {
            var options = new RateLimitOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            return services;
        }

        public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiRateLimitingMiddleware>();
        }
    }
}