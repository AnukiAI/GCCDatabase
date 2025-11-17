using System.Net;
using System.Security.Claims;
using System.Text.Json;
using GCCWebApp.Models.Api;
using GCCWebApp.Services.Security;

namespace GCCWebApp.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Get scoped service from request services
            var structuredLogger = context.RequestServices.GetRequiredService<IStructuredLoggingService>();
            await HandleExceptionAsync(context, ex, structuredLogger);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IStructuredLoggingService structuredLogger)
    {
        var requestId = context.TraceIdentifier;
        
        // Log the exception with structured logging
        await structuredLogger.LogExceptionAsync(
            exception,
            $"Global Exception Handler - {context.Request.Method} {context.Request.Path}",
            context.User,
            new
            {
                RequestId = requestId,
                Path = context.Request.Path.ToString(),
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                RemoteIpAddress = GetClientIpAddress(context),
                Timestamp = DateTime.UtcNow
            });

        // Determine response details based on exception type
        var (statusCode, message, errorCode) = GetResponseDetails(exception);
        
        // Set response details
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // Determine if this is an API request
        var isApiRequest = context.Request.Path.StartsWithSegments("/api") ||
                          context.Request.Headers.Accept.Any(a => a?.Contains("application/json") == true);

        if (isApiRequest)
        {
            await HandleApiExceptionAsync(context, exception, statusCode, message, errorCode, requestId);
        }
        else
        {
            HandleWebException(context, exception, statusCode, message, requestId);
        }
    }

    private async Task HandleApiExceptionAsync(HttpContext context, Exception exception, 
        HttpStatusCode statusCode, string message, string errorCode, string requestId)
    {
        var response = new StandardApiResponse<object>
        {
            Success = false,
            Message = message,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow,
            Errors = new List<ApiError>
            {
                new ApiError(errorCode, message)
            }
        };

        // Add additional details in development
        if (_environment.IsDevelopment())
        {
            response.Errors[0].Details = new
            {
                ExceptionType = exception.GetType().Name,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            };
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private void HandleWebException(HttpContext context, Exception exception,
        HttpStatusCode statusCode, string message, string requestId)
    {
        // For web requests, redirect to appropriate error page
        var errorPath = statusCode switch
        {
            HttpStatusCode.NotFound => "/Home/Error",
            HttpStatusCode.Forbidden => "/Home/Error",
            HttpStatusCode.Unauthorized => "/Account/AccessDenied",
            _ => "/Home/Error"
        };

        // Store error details in TempData for error page
        context.Items["ErrorMessage"] = message;
        context.Items["RequestId"] = requestId;
        context.Items["StatusCode"] = (int)statusCode;

        if (_environment.IsDevelopment())
        {
            context.Items["ExceptionDetails"] = new
            {
                Type = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            };
        }

        // For web requests, we need to handle the response differently
        // since we can't redirect after starting to write the response
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.Redirect(errorPath);
        }
        else
        {
            // If response has already started, just log the error
            _logger.LogWarning("Could not redirect to error page for request {RequestId} - response already started", requestId);
        }
    }

    private (HttpStatusCode statusCode, string message, string errorCode) GetResponseDetails(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Invalid request parameters", ApiErrorCodes.BadRequest),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid request parameters", ApiErrorCodes.BadRequest),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid operation", ApiErrorCodes.BadRequest),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access", ApiErrorCodes.Unauthorized),
            NotImplementedException => (HttpStatusCode.NotImplemented, "Feature not implemented", ApiErrorCodes.InternalError),
            TimeoutException => (HttpStatusCode.RequestTimeout, "Request timeout", ApiErrorCodes.InternalError),
            TaskCanceledException => (HttpStatusCode.RequestTimeout, "Request was cancelled", ApiErrorCodes.InternalError),
            SecurityException => (HttpStatusCode.Forbidden, "Access forbidden", ApiErrorCodes.Forbidden),
            FileNotFoundException => (HttpStatusCode.NotFound, "Resource not found", ApiErrorCodes.NotFound),
            DirectoryNotFoundException => (HttpStatusCode.NotFound, "Resource not found", ApiErrorCodes.NotFound),
            _ => (HttpStatusCode.InternalServerError, 
                  _environment.IsDevelopment() ? exception.Message : "An internal server error occurred", 
                  ApiErrorCodes.InternalError)
        };
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
}

// Custom security exception for application-specific security violations
public class SecurityException : Exception
{
    public SecurityException() : base() { }
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}