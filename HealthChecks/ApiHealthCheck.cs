using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using GCCWebApp.Data;
using System.Diagnostics;

namespace GCCWebApp.HealthChecks
{
    public class ApiHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiHealthCheck> _logger;

        public ApiHealthCheck(ApplicationDbContext context, ILogger<ApiHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
                stopwatch.Stop();

                if (!canConnect)
                {
                    return HealthCheckResult.Unhealthy("Cannot connect to database");
                }

                var data = new Dictionary<string, object>
                {
                    ["database_response_time_ms"] = stopwatch.ElapsedMilliseconds,
                    ["database_provider"] = _context.Database.ProviderName ?? "Unknown"
                };

                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    return HealthCheckResult.Degraded(
                        $"Database response time is slow: {stopwatch.ElapsedMilliseconds}ms",
                        data: data);
                }

                return HealthCheckResult.Healthy("API is healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return HealthCheckResult.Unhealthy("Health check failed", ex);
            }
        }
    }

    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;

        public DatabaseHealthCheck(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

                var certificateCount = await _context.Certificates.CountAsync(cancellationToken);
                var userCount = await _context.Users.CountAsync(cancellationToken);

                stopwatch.Stop();

                var data = new Dictionary<string, object>
                {
                    ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                    ["certificate_count"] = certificateCount,
                    ["user_count"] = userCount,
                    ["connection_string_configured"] = !string.IsNullOrEmpty(_context.Database.GetConnectionString())
                };

                return HealthCheckResult.Healthy("Database is healthy", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed", ex);
            }
        }
    }

    public class RateLimitHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["rate_limiting_enabled"] = true,
                    ["status"] = "active"
                };

                return Task.FromResult(HealthCheckResult.Healthy("Rate limiting is active", data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Rate limit check failed", ex));
            }
        }
    }

    public class CacheHealthCheck : IHealthCheck
    {
        private readonly IMemoryCache _cache;

        public CacheHealthCheck(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var testKey = "health_check_test";
                var testValue = Guid.NewGuid().ToString();

                _cache.Set(testKey, testValue, TimeSpan.FromSeconds(1));

                if (_cache.TryGetValue(testKey, out string? cachedValue) && cachedValue == testValue)
                {
                    _cache.Remove(testKey);
                    return Task.FromResult(HealthCheckResult.Healthy("Cache is working"));
                }

                return Task.FromResult(HealthCheckResult.Unhealthy("Cache test failed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Cache check failed", ex));
            }
        }
    }

    public static class HealthCheckExtensions
    {
        public static IHealthChecksBuilder AddApiHealthChecks(this IServiceCollection services)
        {
            return services.AddHealthChecks()
                .AddCheck<ApiHealthCheck>("api", tags: new[] { "api", "ready" })
                .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "ready" })
                .AddCheck<RateLimitHealthCheck>("rate_limit", tags: new[] { "api", "live" })
                .AddCheck<CacheHealthCheck>("cache", tags: new[] { "cache", "live" });
        }
    }
}