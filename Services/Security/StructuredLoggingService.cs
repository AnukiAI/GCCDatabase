using Serilog;
using Serilog.Context;
using Serilog.Events;
using System.Security.Claims;
using System.Text.Json;

namespace GCCWebApp.Services.Security;

/// <summary>
/// Enhanced structured logging service with security-focused logging capabilities
/// </summary>
public interface IStructuredLoggingService
{
    Task LogUserActionAsync(string action, ClaimsPrincipal? user, object? data = null);
    Task LogSecurityEventAsync(string eventType, string message, object? data = null, LogEventLevel level = LogEventLevel.Warning);
    Task LogDataAccessAsync(string entityType, string operation, ClaimsPrincipal? user, object? data = null);
    Task LogPerformanceMetricAsync(string operation, TimeSpan duration, object? metadata = null);
    Task LogApiRequestAsync(string endpoint, string method, int statusCode, TimeSpan duration, ClaimsPrincipal? user = null);
    Task LogExceptionAsync(Exception exception, string context, ClaimsPrincipal? user = null, object? additionalData = null);
    Task LogConfigurationChangeAsync(string setting, string? oldValue, string? newValue, ClaimsPrincipal? user);
}

public class StructuredLoggingService : IStructuredLoggingService
{
    private readonly ILogger<StructuredLoggingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StructuredLoggingService(
        ILogger<StructuredLoggingService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogUserActionAsync(string action, ClaimsPrincipal? user, object? data = null)
    {
        var logData = new
        {
            ActionType = "UserAction",
            Action = action,
            UserId = GetUserId(user),
            Username = GetUsername(user),
            UserRoles = GetUserRoles(user),
            SessionId = GetSessionId(),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent(),
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        using (LogContext.PushProperty("UserId", logData.UserId))
        using (LogContext.PushProperty("Username", logData.Username))
        using (LogContext.PushProperty("Action", action))
        using (LogContext.PushProperty("IpAddress", logData.IpAddress))
        {
            _logger.LogInformation("User action: {Action} by {Username} ({UserId})", 
                action, logData.Username, logData.UserId);
        }

        // Also log to security audit trail
        await LogToAuditTrailAsync("USER_ACTION", JsonSerializer.Serialize(logData));
    }

    public async Task LogSecurityEventAsync(string eventType, string message, object? data = null, LogEventLevel level = LogEventLevel.Warning)
    {
        var logData = new
        {
            EventType = "SecurityEvent",
            SecurityEventType = eventType,
            Message = message,
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent(),
            UserId = GetCurrentUserId(),
            Username = GetCurrentUsername(),
            SessionId = GetSessionId(),
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        using (LogContext.PushProperty("SecurityEventType", eventType))
        using (LogContext.PushProperty("IpAddress", logData.IpAddress))
        using (LogContext.PushProperty("UserId", logData.UserId))
        {
            switch (level)
            {
                case LogEventLevel.Error:
                    _logger.LogError("Security event: {SecurityEventType} - {Message}", eventType, message);
                    break;
                case LogEventLevel.Warning:
                    _logger.LogWarning("Security event: {SecurityEventType} - {Message}", eventType, message);
                    break;
                case LogEventLevel.Information:
                    _logger.LogInformation("Security event: {SecurityEventType} - {Message}", eventType, message);
                    break;
                default:
                    _logger.LogInformation("Security event: {SecurityEventType} - {Message}", eventType, message);
                    break;
            }
        }

        await LogToAuditTrailAsync("SECURITY_EVENT", JsonSerializer.Serialize(logData));
    }

    public async Task LogDataAccessAsync(string entityType, string operation, ClaimsPrincipal? user, object? data = null)
    {
        var logData = new
        {
            EventType = "DataAccess",
            EntityType = entityType,
            Operation = operation,
            UserId = GetUserId(user),
            Username = GetUsername(user),
            IpAddress = GetClientIpAddress(),
            SessionId = GetSessionId(),
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        using (LogContext.PushProperty("EntityType", entityType))
        using (LogContext.PushProperty("Operation", operation))
        using (LogContext.PushProperty("UserId", logData.UserId))
        {
            _logger.LogInformation("Data access: {Operation} on {EntityType} by {Username}", 
                operation, entityType, logData.Username);
        }

        await LogToAuditTrailAsync("DATA_ACCESS", JsonSerializer.Serialize(logData));
    }

    public Task LogPerformanceMetricAsync(string operation, TimeSpan duration, object? metadata = null)
    {
        var logData = new
        {
            EventType = "PerformanceMetric",
            Operation = operation,
            DurationMs = duration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow,
            IpAddress = GetClientIpAddress(),
            UserId = GetCurrentUserId(),
            Metadata = metadata
        };

        using (LogContext.PushProperty("Operation", operation))
        using (LogContext.PushProperty("DurationMs", logData.DurationMs))
        {
            if (duration.TotalSeconds > 5) // Log slow operations as warnings
            {
                _logger.LogWarning("Slow operation: {Operation} took {DurationMs}ms", 
                    operation, logData.DurationMs);
            }
            else
            {
                _logger.LogDebug("Performance metric: {Operation} took {DurationMs}ms", 
                    operation, logData.DurationMs);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task LogApiRequestAsync(string endpoint, string method, int statusCode, TimeSpan duration, ClaimsPrincipal? user = null)
    {
        var logData = new
        {
            EventType = "ApiRequest",
            Endpoint = endpoint,
            Method = method,
            StatusCode = statusCode,
            DurationMs = duration.TotalMilliseconds,
            UserId = GetUserId(user),
            Username = GetUsername(user),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent(),
            Timestamp = DateTime.UtcNow
        };

        using (LogContext.PushProperty("Endpoint", endpoint))
        using (LogContext.PushProperty("Method", method))
        using (LogContext.PushProperty("StatusCode", statusCode))
        using (LogContext.PushProperty("DurationMs", logData.DurationMs))
        using (LogContext.PushProperty("UserId", logData.UserId))
        {
            var level = statusCode >= 500 ? LogEventLevel.Error :
                       statusCode >= 400 ? LogEventLevel.Warning :
                       LogEventLevel.Information;

            switch (level)
            {
                case LogEventLevel.Error:
                    _logger.LogError("API Request: {Method} {Endpoint} returned {StatusCode} in {DurationMs}ms", 
                        method, endpoint, statusCode, logData.DurationMs);
                    break;
                case LogEventLevel.Warning:
                    _logger.LogWarning("API Request: {Method} {Endpoint} returned {StatusCode} in {DurationMs}ms", 
                        method, endpoint, statusCode, logData.DurationMs);
                    break;
                default:
                    _logger.LogInformation("API Request: {Method} {Endpoint} returned {StatusCode} in {DurationMs}ms", 
                        method, endpoint, statusCode, logData.DurationMs);
                    break;
            }
        }
        
        return Task.CompletedTask;
    }

    public async Task LogExceptionAsync(Exception exception, string context, ClaimsPrincipal? user = null, object? additionalData = null)
    {
        var logData = new
        {
            EventType = "Exception",
            Context = context,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException?.Message,
            UserId = GetUserId(user),
            Username = GetUsername(user),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent(),
            Timestamp = DateTime.UtcNow,
            AdditionalData = additionalData
        };

        using (LogContext.PushProperty("ExceptionType", logData.ExceptionType))
        using (LogContext.PushProperty("Context", context))
        using (LogContext.PushProperty("UserId", logData.UserId))
        {
            _logger.LogError(exception, "Exception in {Context}: {ExceptionMessage}", 
                context, exception.Message);
        }

        await LogToAuditTrailAsync("EXCEPTION", JsonSerializer.Serialize(logData));
    }

    public async Task LogConfigurationChangeAsync(string setting, string? oldValue, string? newValue, ClaimsPrincipal? user)
    {
        var logData = new
        {
            EventType = "ConfigurationChange",
            Setting = setting,
            OldValue = SanitizeConfigValue(setting, oldValue),
            NewValue = SanitizeConfigValue(setting, newValue),
            UserId = GetUserId(user),
            Username = GetUsername(user),
            IpAddress = GetClientIpAddress(),
            Timestamp = DateTime.UtcNow
        };

        using (LogContext.PushProperty("Setting", setting))
        using (LogContext.PushProperty("UserId", logData.UserId))
        {
            _logger.LogWarning("Configuration changed: {Setting} changed by {Username} ({UserId})", 
                setting, logData.Username, logData.UserId);
        }

        await LogToAuditTrailAsync("CONFIG_CHANGE", JsonSerializer.Serialize(logData));
    }

    private async Task LogToAuditTrailAsync(string eventType, string logData)
    {
        // In a production environment, this could write to:
        // - Dedicated audit database table
        // - External audit service (e.g., Azure Monitor, Splunk)
        // - Compliance logging system
        // - SIEM system

        try
        {
            // For now, we'll use structured logging with specific format for audit trail
            using (LogContext.PushProperty("AuditEvent", true))
            using (LogContext.PushProperty("AuditEventType", eventType))
            {
                _logger.LogInformation("AUDIT: {EventType} - {LogData}", eventType, logData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write to audit trail");
        }
    }

    private string? GetUserId(ClaimsPrincipal? user)
    {
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? GetCurrentUserId();
    }

    private string? GetUsername(ClaimsPrincipal? user)
    {
        return user?.FindFirst(ClaimTypes.Name)?.Value ?? 
               user?.FindFirst("preferred_username")?.Value ?? 
               GetCurrentUsername();
    }

    private string[] GetUserRoles(ClaimsPrincipal? user)
    {
        return user?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToArray() ?? Array.Empty<string>();
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetCurrentUsername()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.Name)?.Value ?? 
               user?.FindFirst("preferred_username")?.Value ?? 
               "Anonymous";
    }

    private string? GetSessionId()
    {
        return _httpContextAccessor.HttpContext?.Session?.Id;
    }

    private string? GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        return _httpContextAccessor.HttpContext?.Request?.Headers?.UserAgent.ToString();
    }

    private string? SanitizeConfigValue(string setting, string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Don't log sensitive configuration values
        var sensitiveSettings = new[]
        {
            "password", "secret", "key", "token", "connectionstring", "connection_string"
        };

        if (sensitiveSettings.Any(s => setting.ToLowerInvariant().Contains(s)))
        {
            return "[REDACTED]";
        }

        return value;
    }
}