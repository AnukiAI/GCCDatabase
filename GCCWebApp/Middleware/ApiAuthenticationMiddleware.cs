using System.Text.Json;

namespace GCCWebApp.Middleware;

/// <summary>
/// Middleware to ensure API endpoints return JSON responses for authentication/authorization failures
/// instead of HTML redirect responses (login pages, access denied pages, etc.)
/// </summary>
public class ApiAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiAuthenticationMiddleware> _logger;

    public ApiAuthenticationMiddleware(RequestDelegate next, ILogger<ApiAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process API requests (exclude /api-docs which is public documentation)
        if (!context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/api-docs"))
        {
            await _next(context);
            return;
        }

        // Capture the original response body to check for redirects
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            // Check if this is a redirect response for an API endpoint
            if (IsAuthenticationRedirect(context))
            {
                _logger.LogWarning(
                    "API request to {Path} resulted in authentication redirect. Converting to JSON 401 response.",
                    context.Request.Path);

                // Clear the redirect response
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = "Authentication required. Please log in.",
                    errorCode = "UNAUTHORIZED"
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
            }
            else if (IsAccessDeniedRedirect(context))
            {
                _logger.LogWarning(
                    "API request to {Path} resulted in access denied redirect. Converting to JSON 403 response.",
                    context.Request.Path);

                // Clear the redirect response
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = "Access denied. You do not have permission to perform this action.",
                    errorCode = "FORBIDDEN"
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
            }
            else if (IsHtmlResponse(context) && context.Response.StatusCode >= 400)
            {
                // If we got an HTML error page for an API request, convert it to JSON
                _logger.LogWarning(
                    "API request to {Path} returned HTML error response (status {StatusCode}). Converting to JSON.",
                    context.Request.Path, context.Response.StatusCode);

                responseBody.Seek(0, SeekOrigin.Begin);
                var htmlContent = await new StreamReader(responseBody).ReadToEndAsync();

                // Clear and rewrite as JSON
                context.Response.Clear();
                context.Response.ContentType = "application/json";

                var statusCode = context.Response.StatusCode;
                context.Response.StatusCode = statusCode >= 400 ? statusCode : StatusCodes.Status500InternalServerError;

                var response = new
                {
                    success = false,
                    message = statusCode switch
                    {
                        401 => "Authentication required. Please log in.",
                        403 => "Access denied. You do not have permission to perform this action.",
                        404 => "The requested resource was not found.",
                        _ => "An error occurred processing your request."
                    },
                    errorCode = statusCode switch
                    {
                        401 => "UNAUTHORIZED",
                        403 => "FORBIDDEN",
                        404 => "NOT_FOUND",
                        _ => "INTERNAL_ERROR"
                    }
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(json);
            }
            else
            {
                // Copy the original response
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool IsAuthenticationRedirect(HttpContext context)
    {
        // Check for redirect status codes
        if (context.Response.StatusCode != 302 && context.Response.StatusCode != 301)
            return false;

        // Check if redirecting to login page
        var location = context.Response.Headers.Location.ToString();
        return !string.IsNullOrEmpty(location) &&
               (location.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("/signin", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("/oauth2", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("login.microsoftonline", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("b2clogin.com", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAccessDeniedRedirect(HttpContext context)
    {
        // Check for redirect status codes
        if (context.Response.StatusCode != 302 && context.Response.StatusCode != 301)
            return false;

        // Check if redirecting to access denied page
        var location = context.Response.Headers.Location.ToString();
        return !string.IsNullOrEmpty(location) &&
               (location.Contains("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("/accessdenied", StringComparison.OrdinalIgnoreCase) ||
                location.Contains("/forbidden", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsHtmlResponse(HttpContext context)
    {
        var contentType = context.Response.ContentType;
        return !string.IsNullOrEmpty(contentType) &&
               (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
                contentType.Contains("application/xhtml", StringComparison.OrdinalIgnoreCase));
    }
}

public static class ApiAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiAuthenticationMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiAuthenticationMiddleware>();
    }
}
