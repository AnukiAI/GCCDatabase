using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GCCWebApp.Services.Performance;

/// <summary>
/// Service for managing application-level response caching with intelligent cache invalidation
/// </summary>
public interface IResponseCachingService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task ClearAsync();
    Task<bool> ExistsAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
}

public class ResponseCachingService : IResponseCachingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResponseCachingService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _cacheKeys;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(15);

    public ResponseCachingService(IMemoryCache cache, ILogger<ResponseCachingService> logger)
    {
        _cache = cache;
        _logger = logger;
        _cacheKeys = new ConcurrentDictionary<string, DateTime>();
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            if (_cache.TryGetValue(key, out var cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue as T;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache entry for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var cacheExpiration = expiration ?? _defaultExpiration;
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };

            // Add eviction callback to clean up our tracking
            cacheEntryOptions.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _cacheKeys.TryRemove(k.ToString()!, out _);
                _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", k, reason);
            });

            _cache.Set(key, value, cacheEntryOptions);
            _cacheKeys[key] = DateTime.UtcNow.Add(cacheExpiration);

            _logger.LogDebug("Cache entry set for key: {Key}, Expiration: {Expiration}", key, cacheExpiration);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache entry for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            _cache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            _logger.LogDebug("Cache entry removed for key: {Key}", key);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var keysToRemove = _cacheKeys.Keys
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                await RemoveAsync(key);
            }

            _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            var keysToRemove = _cacheKeys.Keys.ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
            
            _cacheKeys.Clear();
            _logger.LogInformation("Cache cleared - {Count} entries removed", keysToRemove.Count);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        await Task.CompletedTask;
        return _cache.TryGetValue(key, out _);
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class
    {
        var cachedValue = await GetAsync<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        try
        {
            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiration);
            }
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in factory method for cache key: {Key}", key);
            throw;
        }
    }

    private static bool IsPatternMatch(string key, string pattern)
    {
        // Simple pattern matching - supports wildcards (*)
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
        }
        
        return key.Contains(pattern);
    }
}

/// <summary>
/// Cache key constants and builders for consistent cache key management
/// </summary>
public static class CacheKeys
{
    public const string CertificatesList = "certificates:list";
    public const string CertificateDetails = "certificate:details:{0}";
    public const string CompanyDetails = "company:details:{0}";
    public const string ProductsList = "products:list";
    public const string ProductDetails = "product:details:{0}";
    public const string UserProfile = "user:profile:{0}";
    public const string PartnersList = "partners:list";
    public const string PartnerDetails = "partner:details:{0}";

    public static string CertificateDetailsByNumber(string certificateNumber) 
        => string.Format(CertificateDetails, certificateNumber);
    
    public static string CompanyDetailsByName(string companyName) 
        => string.Format(CompanyDetails, companyName);
    
    public static string ProductDetailsByName(string productName) 
        => string.Format(ProductDetails, productName);
    
    public static string UserProfileById(string userId) 
        => string.Format(UserProfile, userId);
    
    public static string PartnerDetailsByName(string partnerName) 
        => string.Format(PartnerDetails, partnerName);

    public static class Patterns
    {
        public const string AllCertificates = "certificate:*";
        public const string AllCompanies = "company:*";
        public const string AllProducts = "product:*";
        public const string AllUsers = "user:*";
        public const string AllPartners = "partner:*";
    }
}