using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using GCCWebApp.Services.Security;

namespace GCCWebApp.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly HashSet<string> _sensitiveHeaders;
    private readonly HashSet<string> _excludedPaths;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        
        _sensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Cookie", "Set-Cookie", "X-API-Key", 
            "X-Auth-Token", "Authentication", "Proxy-Authorization"
        };
        
        _excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/favicon.ico", "/robots.txt", "/health", "/ping"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for excluded paths
        if (_excludedPaths.Contains(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Get scoped service from request services
        var structuredLogger = context.RequestServices.GetRequiredService<IStructuredLoggingService>();
        
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        // Log incoming request
        await LogRequestAsync(context, requestId, structuredLogger);

        // Create response body logging stream
        var originalResponseBodyStream = context.Response.Body;
        using var responseBodyMemoryStream = new MemoryStream();
        context.Response.Body = responseBodyMemoryStream;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogExceptionAsync(context, ex, stopwatch.Elapsed, requestId, structuredLogger);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, responseBodyMemoryStream, stopwatch.Elapsed, requestId, structuredLogger);

            // Copy response body back to original stream
            responseBodyMemoryStream.Position = 0;
            await responseBodyMemoryStream.CopyToAsync(originalResponseBodyStream);
            context.Response.Body = originalResponseBodyStream;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string requestId, IStructuredLoggingService structuredLogger)
    {
        var request = context.Request;
        
        var requestData = new
        {
            RequestId = requestId,
            Method = request.Method,
            Path = request.Path.ToString(),
            QueryString = request.QueryString.ToString(),
            Headers = FilterHeaders(request.Headers),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            UserAgent = request.Headers.UserAgent.ToString(),
            RemoteIpAddress = GetClientIpAddress(context),
            Protocol = request.Protocol,
            Scheme = request.Scheme,
            Host = request.Host.ToString(),
            UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            UserName = context.User.FindFirst(ClaimTypes.Name)?.Value ?? 
                      context.User.FindFirst("preferred_username")?.Value,
            Timestamp = DateTime.UtcNow
        };

        // Log request body for non-GET requests (with size limits)
        string? requestBody = null;
        if (request.Method != "GET" && request.ContentLength.HasValue && 
            request.ContentLength > 0 && request.ContentLength <= 1024 * 10) // 10KB limit
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength.Value)];
            await request.Body.ReadExactlyAsync(buffer, 0, buffer.Length);
            requestBody = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0; // Reset stream position
            
            // Sanitize sensitive data in request body
            requestBody = SanitizeRequestBody(requestBody, request.ContentType);
        }

        _logger.LogInformation("HTTP Request: {Method} {Path} - RequestId: {RequestId}", 
            request.Method, request.Path, requestId);

        // Use structured logging service for detailed request logging
        var detailedRequestData = new
        {
            requestData.RequestId,
            requestData.Method,
            requestData.Path,
            requestData.QueryString,
            requestData.Headers,
            requestData.ContentType,
            requestData.ContentLength,
            requestData.UserAgent,
            requestData.RemoteIpAddress,
            requestData.Protocol,
            requestData.Scheme,
            requestData.Host,
            requestData.UserId,
            requestData.UserName,
            requestData.Timestamp,
            RequestBody = requestBody
        };

        await structuredLogger.LogApiRequestAsync(
            $"{request.Method} {request.Path}",
            request.Method,
            0, // Status code not available yet
            TimeSpan.Zero, // Duration not available yet
            context.User);
    }

    private async Task LogResponseAsync(HttpContext context, MemoryStream responseBodyStream, 
        TimeSpan duration, string requestId, IStructuredLoggingService structuredLogger)
    {
        var response = context.Response;
        
        string? responseBody = null;
        if (responseBodyStream.Length > 0 && responseBodyStream.Length <= 1024 * 50) // 50KB limit
        {
            responseBodyStream.Position = 0;
            responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBody = SanitizeResponseBody(responseBody, response.ContentType);
        }

        var responseData = new
        {
            RequestId = requestId,
            StatusCode = response.StatusCode,
            Headers = FilterHeaders(response.Headers),
            ContentType = response.ContentType,
            ContentLength = responseBodyStream.Length,
            DurationMs = duration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow,
            ResponseBody = responseBodyStream.Length <= 1024 * 10 ? responseBody : "[Response too large to log]"
        };

        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                      response.StatusCode >= 400 ? LogLevel.Warning :
                      LogLevel.Information;

        _logger.Log(logLevel, "HTTP Response: {StatusCode} for {RequestId} in {DurationMs}ms", 
            response.StatusCode, requestId, duration.TotalMilliseconds);

        // Use structured logging for detailed response logging
        await structuredLogger.LogApiRequestAsync(
            $"{context.Request.Method} {context.Request.Path}",
            context.Request.Method,
            response.StatusCode,
            duration,
            context.User);

        // Log performance concerns
        if (duration.TotalSeconds > 5)
        {
            await structuredLogger.LogPerformanceMetricAsync(
                $"{context.Request.Method} {context.Request.Path}",
                duration,
                new { StatusCode = response.StatusCode, RequestId = requestId });
        }
    }

    private async Task LogExceptionAsync(HttpContext context, Exception exception, 
        TimeSpan duration, string requestId, IStructuredLoggingService structuredLogger)
    {
        var exceptionData = new
        {
            RequestId = requestId,
            Method = context.Request.Method,
            Path = context.Request.Path.ToString(),
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message,
            DurationMs = duration.TotalMilliseconds,
            UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            UserName = context.User.FindFirst(ClaimTypes.Name)?.Value,
            RemoteIpAddress = GetClientIpAddress(context),
            Timestamp = DateTime.UtcNow
        };

        _logger.LogError(exception, "HTTP Request Exception: {Method} {Path} - RequestId: {RequestId}", 
            context.Request.Method, context.Request.Path, requestId);

        await structuredLogger.LogExceptionAsync(
            exception, 
            $"HTTP Request: {context.Request.Method} {context.Request.Path}",
            context.User,
            exceptionData);
    }

    private Dictionary<string, string> FilterHeaders(IHeaderDictionary headers)
    {
        var filteredHeaders = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            var value = _sensitiveHeaders.Contains(header.Key) 
                ? "[REDACTED]" 
                : string.Join(", ", header.Value.Where(v => !string.IsNullOrEmpty(v)));
            
            filteredHeaders[header.Key] = value;
        }
        
        return filteredHeaders;
    }

    private string? GetClientIpAddress(HttpContext context)
    {
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

    private string SanitizeRequestBody(string requestBody, string? contentType)
    {
        if (string.IsNullOrEmpty(requestBody) || string.IsNullOrEmpty(contentType))
            return requestBody;

        // For JSON content, try to sanitize password fields
        if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return SanitizeJsonContent(requestBody);
        }

        // For form data, sanitize password fields
        if (contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            return SanitizeFormData(requestBody);
        }

        return requestBody;
    }

    private string SanitizeResponseBody(string responseBody, string? contentType)
    {
        if (string.IsNullOrEmpty(responseBody) || string.IsNullOrEmpty(contentType))
            return responseBody;

        // For JSON responses, sanitize sensitive fields
        if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return SanitizeJsonContent(responseBody);
        }

        return responseBody;
    }

    private string SanitizeJsonContent(string jsonContent)
    {
        try
        {
            // Simple regex-based sanitization for common sensitive fields
            var sensitiveFields = new[]
            {
                "password", "secret", "key", "token", "apikey", "api_key", 
                "connectionstring", "connection_string", "credentials"
            };

            foreach (var field in sensitiveFields)
            {
                // Match "field": "value" or "field":"value"
                var pattern = $@"""({field})""\s*:\s*""[^""]*""";
                jsonContent = System.Text.RegularExpressions.Regex.Replace(
                    jsonContent, pattern, $@"""$1"":""[REDACTED]""", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return jsonContent;
        }
        catch
        {
            return "[Could not sanitize JSON content]";
        }
    }

    private string SanitizeFormData(string formData)
    {
        try
        {
            var sensitiveFields = new[] { "password", "secret", "key", "token" };
            
            foreach (var field in sensitiveFields)
            {
                // Match field=value&
                var pattern = $@"{field}=[^&]*";
                formData = System.Text.RegularExpressions.Regex.Replace(
                    formData, pattern, $"{field}=[REDACTED]", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return formData;
        }
        catch
        {
            return "[Could not sanitize form data]";
        }
    }
}