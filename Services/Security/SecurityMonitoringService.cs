using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace GCCWebApp.Services.Security;

/// <summary>
/// Service for security monitoring, threat detection, and incident response
/// </summary>
public interface ISecurityMonitoringService
{
    Task LogSecurityEventAsync(SecurityEvent securityEvent);
    Task<bool> DetectSuspiciousActivityAsync(string ipAddress, string userAgent, string userId = null);
    Task<SecurityReport> GenerateSecurityReportAsync(DateTime fromDate, DateTime toDate);
    Task<List<SecurityAlert>> GetActiveAlertsAsync();
    Task ProcessFailedLoginAttemptAsync(string ipAddress, string username, string userAgent);
    Task ProcessSuccessfulLoginAsync(string ipAddress, string username, string userAgent);
    Task<bool> IsIpAddressBlockedAsync(string ipAddress);
    Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason);
}

public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ILogger<SecurityMonitoringService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, FailedLoginTracker> _failedLogins = new();
    private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();
    private readonly List<SecurityEvent> _securityEvents = new();
    private readonly object _lockObject = new();

    public SecurityMonitoringService(
        ILogger<SecurityMonitoringService> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        securityEvent.Timestamp = DateTime.UtcNow;
        securityEvent.Id = Guid.NewGuid().ToString();

        lock (_lockObject)
        {
            _securityEvents.Add(securityEvent);
        }

        // Log to structured logging
        _logger.LogWarning("Security Event: {EventType} from {IpAddress} - {Message}", 
            securityEvent.EventType, securityEvent.IpAddress, securityEvent.Message);

        // Check if this event triggers any automated responses
        await ProcessSecurityEventAsync(securityEvent);
    }

    public async Task<bool> DetectSuspiciousActivityAsync(string ipAddress, string userAgent, string userId = null)
    {
        var suspiciousIndicators = new List<string>();

        // Check for suspicious user agents
        if (IsSuspiciousUserAgent(userAgent))
        {
            suspiciousIndicators.Add("Suspicious user agent");
        }

        // Check for rapid requests from same IP
        if (await IsRapidRequestPatternAsync(ipAddress))
        {
            suspiciousIndicators.Add("Rapid request pattern");
        }

        // Check for multiple failed login attempts
        if (_failedLogins.TryGetValue(ipAddress, out var tracker) && tracker.AttemptCount > 5)
        {
            suspiciousIndicators.Add("Multiple failed login attempts");
        }

        // Check for requests from known malicious IP ranges
        if (await IsKnownMaliciousIpAsync(ipAddress))
        {
            suspiciousIndicators.Add("Known malicious IP address");
        }

        if (suspiciousIndicators.Any())
        {
            await LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.SuspiciousActivity,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                UserId = userId,
                Message = $"Suspicious activity detected: {string.Join(", ", suspiciousIndicators)}",
                Severity = SecuritySeverity.Medium,
                Details = JsonSerializer.Serialize(new { SuspiciousIndicators = suspiciousIndicators })
            });

            return true;
        }

        return false;
    }

    public async Task<SecurityReport> GenerateSecurityReportAsync(DateTime fromDate, DateTime toDate)
    {
        List<SecurityEvent> eventsInRange;
        lock (_lockObject)
        {
            eventsInRange = _securityEvents
                .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
                .ToList();
        }

        var report = new SecurityReport
        {
            FromDate = fromDate,
            ToDate = toDate,
            GeneratedAt = DateTime.UtcNow,
            TotalEvents = eventsInRange.Count,
            EventsByType = eventsInRange.GroupBy(e => e.EventType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            EventsBySeverity = eventsInRange.GroupBy(e => e.Severity)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            TopSourceIPs = eventsInRange.GroupBy(e => e.IpAddress)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key ?? "Unknown", g => g.Count()),
            FailedLoginAttempts = eventsInRange
                .Count(e => e.EventType == SecurityEventType.FailedLogin),
            SuspiciousActivities = eventsInRange
                .Count(e => e.EventType == SecurityEventType.SuspiciousActivity),
            BlockedIpAddresses = _blockedIps.Count
        };

        // Calculate trends
        var previousPeriod = fromDate.Subtract(toDate - fromDate);
        var previousEvents = _securityEvents
            .Where(e => e.Timestamp >= previousPeriod && e.Timestamp < fromDate)
            .ToList();

        if (previousEvents.Any())
        {
            var currentCount = eventsInRange.Count;
            var previousCount = previousEvents.Count;
            report.TrendPercentage = previousCount > 0 ? 
                ((double)(currentCount - previousCount) / previousCount) * 100 : 0;
        }

        return report;
    }

    public async Task<List<SecurityAlert>> GetActiveAlertsAsync()
    {
        var alerts = new List<SecurityAlert>();

        // Check for blocked IPs
        foreach (var blockedIp in _blockedIps)
        {
            if (blockedIp.Value > DateTime.UtcNow)
            {
                alerts.Add(new SecurityAlert
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = SecurityAlertType.BlockedIpAddress,
                    Message = $"IP address {blockedIp.Key} is currently blocked",
                    Severity = SecuritySeverity.High,
                    CreatedAt = blockedIp.Value.Subtract(TimeSpan.FromHours(1)), // Approximate
                    IsActive = true
                });
            }
        }

        // Check for IPs with excessive failed login attempts
        foreach (var failedLogin in _failedLogins.Where(fl => fl.Value.AttemptCount >= 10))
        {
            alerts.Add(new SecurityAlert
            {
                Id = Guid.NewGuid().ToString(),
                Type = SecurityAlertType.ExcessiveFailedLogins,
                Message = $"IP address {failedLogin.Key} has {failedLogin.Value.AttemptCount} failed login attempts",
                Severity = SecuritySeverity.High,
                CreatedAt = failedLogin.Value.LastAttempt,
                IsActive = true
            });
        }

        return alerts;
    }

    public async Task ProcessFailedLoginAttemptAsync(string ipAddress, string username, string userAgent)
    {
        var tracker = _failedLogins.GetOrAdd(ipAddress, _ => new FailedLoginTracker());
        tracker.AttemptCount++;
        tracker.LastAttempt = DateTime.UtcNow;
        tracker.LastUsername = username;

        await LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.FailedLogin,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            UserId = username,
            Message = $"Failed login attempt for user '{username}' from {ipAddress}",
            Severity = SecuritySeverity.Medium,
            Details = JsonSerializer.Serialize(new { 
                Username = username, 
                AttemptCount = tracker.AttemptCount,
                UserAgent = userAgent 
            })
        });

        // Auto-block after threshold
        var threshold = _configuration.GetValue<int>("SecurityMonitoring:AlertThresholds:FailedLoginsPerMinute", 10);
        if (tracker.AttemptCount >= threshold)
        {
            await BlockIpAddressAsync(ipAddress, TimeSpan.FromMinutes(30), 
                $"Automatic block due to {tracker.AttemptCount} failed login attempts");
        }
    }

    public async Task ProcessSuccessfulLoginAsync(string ipAddress, string username, string userAgent)
    {
        // Reset failed login counter for this IP
        _failedLogins.TryRemove(ipAddress, out _);

        await LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.SuccessfulLogin,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            UserId = username,
            Message = $"Successful login for user '{username}' from {ipAddress}",
            Severity = SecuritySeverity.Info,
            Details = JsonSerializer.Serialize(new { Username = username, UserAgent = userAgent })
        });
    }

    public async Task<bool> IsIpAddressBlockedAsync(string ipAddress)
    {
        if (_blockedIps.TryGetValue(ipAddress, out var blockedUntil))
        {
            if (blockedUntil > DateTime.UtcNow)
            {
                return true;
            }
            else
            {
                // Block has expired, remove it
                _blockedIps.TryRemove(ipAddress, out _);
            }
        }

        return false;
    }

    public async Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason)
    {
        var blockedUntil = DateTime.UtcNow.Add(duration);
        _blockedIps[ipAddress] = blockedUntil;

        await LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.IpAddressBlocked,
            IpAddress = ipAddress,
            Message = $"IP address blocked for {duration.TotalMinutes} minutes. Reason: {reason}",
            Severity = SecuritySeverity.High,
            Details = JsonSerializer.Serialize(new { 
                Duration = duration.ToString(), 
                Reason = reason,
                BlockedUntil = blockedUntil
            })
        });

        _logger.LogWarning("IP address {IpAddress} blocked until {BlockedUntil}. Reason: {Reason}", 
            ipAddress, blockedUntil, reason);
    }

    private async Task ProcessSecurityEventAsync(SecurityEvent securityEvent)
    {
        // Auto-response logic based on event type and severity
        switch (securityEvent.EventType)
        {
            case SecurityEventType.SuspiciousActivity when securityEvent.Severity >= SecuritySeverity.High:
                if (!string.IsNullOrEmpty(securityEvent.IpAddress))
                {
                    await BlockIpAddressAsync(securityEvent.IpAddress, TimeSpan.FromMinutes(15),
                        "Automatic block due to high-severity suspicious activity");
                }
                break;

            case SecurityEventType.SqlInjectionAttempt:
                if (!string.IsNullOrEmpty(securityEvent.IpAddress))
                {
                    await BlockIpAddressAsync(securityEvent.IpAddress, TimeSpan.FromHours(1),
                        "SQL injection attempt detected");
                }
                break;
        }
    }

    private bool IsSuspiciousUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return true;

        var suspiciousPatterns = new[]
        {
            "sqlmap", "nmap", "nikto", "w3af", "acunetix", "appscan",
            "burp", "zap", "wget", "curl", "python", "perl", "php",
            "script", "bot", "crawler", "spider"
        };

        return suspiciousPatterns.Any(pattern => 
            userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> IsRapidRequestPatternAsync(string ipAddress)
    {
        var cacheKey = $"request_count_{ipAddress}";
        var requestCount = _cache.Get<int>(cacheKey);
        
        if (requestCount > 50) // More than 50 requests per minute
        {
            return true;
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
            Size = 1
        };
        _cache.Set(cacheKey, requestCount + 1, cacheEntryOptions);
        return false;
    }

    private async Task<bool> IsKnownMaliciousIpAsync(string ipAddress)
    {
        // In a real implementation, this would check against threat intelligence feeds
        // For now, we'll check against a simple list of known bad IP ranges
        
        if (IPAddress.TryParse(ipAddress, out var ip))
        {
            // Block common attack sources (simplified example)
            var ipBytes = ip.GetAddressBytes();
            
            // Example: Block certain IP ranges known for attacks
            if (ipBytes[0] == 185 && ipBytes[1] == 220) // Example malicious range
            {
                return true;
            }
        }

        return false;
    }

    private class FailedLoginTracker
    {
        public int AttemptCount { get; set; }
        public DateTime LastAttempt { get; set; }
        public string LastUsername { get; set; } = string.Empty;
    }
}

public class SecurityEvent
{
    public string Id { get; set; } = string.Empty;
    public SecurityEventType EventType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? UserId { get; set; }
    public SecuritySeverity Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}

public class SecurityReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<string, int> EventsBySeverity { get; set; } = new();
    public Dictionary<string, int> TopSourceIPs { get; set; } = new();
    public int FailedLoginAttempts { get; set; }
    public int SuspiciousActivities { get; set; }
    public int BlockedIpAddresses { get; set; }
    public double TrendPercentage { get; set; }
}

public class SecurityAlert
{
    public string Id { get; set; } = string.Empty;
    public SecurityAlertType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public SecuritySeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public enum SecurityEventType
{
    FailedLogin,
    SuccessfulLogin,
    SuspiciousActivity,
    IpAddressBlocked,
    SqlInjectionAttempt,
    XssAttempt,
    UnauthorizedAccess,
    RateLimitExceeded,
    InvalidApiKey,
    DataExfiltrationAttempt
}

public enum SecuritySeverity
{
    Info = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}

public enum SecurityAlertType
{
    BlockedIpAddress,
    ExcessiveFailedLogins,
    SuspiciousActivity,
    SecurityPolicyViolation,
    DataBreach,
    UnauthorizedAccess
}