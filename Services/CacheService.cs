using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GCCWebApp.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? cachedValue))
                {
                    return cachedValue;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item from cache with key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions();
                
                if (expiration.HasValue)
                {
                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    cacheEntryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                }

                _memoryCache.Set(key, data, cacheEntryOptions);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting item in cache with key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cache with key: {Key}", key);
            }
        }

        public async Task ClearByPatternAsync(string pattern)
        {
            try
            {
                // Note: IMemoryCache doesn't natively support pattern-based clearing
                // This is a simplified implementation
                _logger.LogWarning("Pattern-based cache clearing is not fully implemented for MemoryCache: {Pattern}", pattern);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache by pattern: {Pattern}", pattern);
            }
        }
    }
}