using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace GCCWebApp.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ApiCacheAttribute : ActionFilterAttribute
    {
        private readonly int _durationInSeconds;
        private readonly bool _varyByUser;
        private readonly bool _varyByQueryParams;
        private readonly string[]? _varyByHeaders;

        public ApiCacheAttribute(int durationInSeconds = 60, bool varyByUser = false, bool varyByQueryParams = true, string[]? varyByHeaders = null)
        {
            _durationInSeconds = durationInSeconds;
            _varyByUser = varyByUser;
            _varyByQueryParams = varyByQueryParams;
            _varyByHeaders = varyByHeaders;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
            if (cache == null)
            {
                await next();
                return;
            }

            var cacheKey = GenerateCacheKey(context.HttpContext);

            if (cache.TryGetValue(cacheKey, out CachedResponse? cachedResponse) && cachedResponse != null)
            {
                var result = new ContentResult
                {
                    Content = cachedResponse.Content,
                    ContentType = cachedResponse.ContentType,
                    StatusCode = cachedResponse.StatusCode
                };

                foreach (var header in cachedResponse.Headers)
                {
                    context.HttpContext.Response.Headers[header.Key] = header.Value;
                }

                context.HttpContext.Response.Headers.Append("X-Cache", "HIT");
                context.HttpContext.Response.Headers.Append("X-Cache-Duration", _durationInSeconds.ToString());

                context.Result = result;
                return;
            }

            var executedContext = await next();

            if (executedContext.Result is ObjectResult objectResult &&
                executedContext.HttpContext.Response.StatusCode >= 200 &&
                executedContext.HttpContext.Response.StatusCode < 300)
            {
                var responseToCache = new CachedResponse
                {
                    Content = JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    }),
                    ContentType = "application/json",
                    StatusCode = objectResult.StatusCode ?? 200,
                    Headers = new Dictionary<string, string>
                    {
                        ["X-Cache"] = "MISS",
                        ["X-Cache-Duration"] = _durationInSeconds.ToString()
                    }
                };

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_durationInSeconds),
                    SlidingExpiration = TimeSpan.FromSeconds(_durationInSeconds / 2)
                };

                cacheOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<ApiCacheAttribute>>();
                    logger?.LogDebug("Cache entry evicted. Key: {Key}, Reason: {Reason}", key, reason);
                });

                cache.Set(cacheKey, responseToCache, cacheOptions);

                context.HttpContext.Response.Headers.Append("X-Cache", "MISS");
                context.HttpContext.Response.Headers.Append("X-Cache-Duration", _durationInSeconds.ToString());
            }
        }

        private string GenerateCacheKey(HttpContext context)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"api_cache:{context.Request.Path}");

            if (_varyByQueryParams && context.Request.QueryString.HasValue)
            {
                keyBuilder.Append($":{context.Request.QueryString}");
            }

            if (_varyByUser && context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                            context.User.FindFirst("api_key_id")?.Value ??
                            "anonymous";
                keyBuilder.Append($":user_{userId}");
            }

            if (_varyByHeaders != null)
            {
                foreach (var header in _varyByHeaders)
                {
                    if (context.Request.Headers.TryGetValue(header, out var headerValue))
                    {
                        keyBuilder.Append($":h_{header}_{headerValue}");
                    }
                }
            }

            return keyBuilder.ToString();
        }

        private class CachedResponse
        {
            public string Content { get; set; } = string.Empty;
            public string ContentType { get; set; } = "application/json";
            public int StatusCode { get; set; } = 200;
            public Dictionary<string, string> Headers { get; set; } = new();
        }
    }

    public static class CacheProfiles
    {
        public const int NoCache = 0;
        public const int Short = 30;
        public const int Medium = 300;
        public const int Long = 3600;
        public const int VeryLong = 86400;
    }
}