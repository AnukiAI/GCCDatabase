using GCCWebApp.Services.Security;
using Microsoft.AspNetCore.Hosting;

namespace GCCWebApp.Middleware;

/// <summary>
/// Middleware for adding comprehensive security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var securityMonitoring = context.RequestServices.GetService<ISecurityMonitoringService>();

        // Check if IP is blocked before processing (only if service is available)
        var ipAddress = GetClientIpAddress(context);
        if (securityMonitoring != null && await securityMonitoring.IsIpAddressBlockedAsync(ipAddress))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied");
            return;
        }

        // Detect suspicious activity (only if service is available)
        if (securityMonitoring != null)
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            await securityMonitoring.DetectSuspiciousActivityAsync(ipAddress, userAgent);
        }

        // Add security headers before processing the request
        AddSecurityHeaders(context);

        // Check for common attack patterns
        if (await DetectAttackPatternsAsync(context, securityMonitoring))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Bad request");
            return;
        }

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;
        var isHttps = context.Request.IsHttps;
        var isDevelopment = _environment.IsDevelopment();

        // Content Security Policy
        var cspPolicy = BuildContentSecurityPolicy(isDevelopment);
        response.Headers.Append("Content-Security-Policy", cspPolicy);

        // Strict Transport Security (HTTPS only)
        if (isHttps)
        {
            response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        }

        // X-Frame-Options
        response.Headers.Append("X-Frame-Options", "DENY");

        // X-Content-Type-Options
        response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-XSS-Protection (modern approach: disable and rely on CSP)
        response.Headers.Append("X-XSS-Protection", "0");

        // Referrer Policy
        response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions Policy
        response.Headers.Append("Permissions-Policy", 
            "camera=(), microphone=(), geolocation=(), payment=(), usb=()");

        // Feature Policy (for older browsers)
        response.Headers.Append("Feature-Policy", 
            "camera 'none'; microphone 'none'; geolocation 'none'; payment 'none'; usb 'none'");

        // Cross-Origin Resource Policy
        response.Headers.Append("Cross-Origin-Resource-Policy", "same-origin");

        // Cross-Origin Embedder Policy
        response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");

        // Cross-Origin Opener Policy
        response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");

        // X-Permitted-Cross-Domain-Policies
        response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        // Server header removal (completely remove for security)
        response.Headers.Remove("Server");

        // X-Powered-By header removal (if exists)
        response.Headers.Remove("X-Powered-By");

        // Custom security headers
        response.Headers.Append("X-Security-Policy", "enforced");
        response.Headers.Append("X-Content-Security-Policy", cspPolicy);
    }

    private string BuildContentSecurityPolicy(bool isDevelopment)
    {
        var cspBuilder = new List<string>();

        // Default source
        cspBuilder.Add("default-src 'self'");

        // Script source
        if (isDevelopment)
        {
            cspBuilder.Add("script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com");
        }
        else
        {
            cspBuilder.Add("script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com");
        }

        // Style source
        cspBuilder.Add("style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com");

        // Font source
        cspBuilder.Add("font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com");

        // Image source
        cspBuilder.Add("img-src 'self' data: https: blob:");

        // Connect source (for AJAX requests)
        cspBuilder.Add("connect-src 'self' https://api.greencirclecertified.com");

        // Frame source
        cspBuilder.Add("frame-src 'none'");

        // Object source
        cspBuilder.Add("object-src 'none'");

        // Base URI
        cspBuilder.Add("base-uri 'self'");

        // Form action
        cspBuilder.Add("form-action 'self'");

        // Frame ancestors
        cspBuilder.Add("frame-ancestors 'none'");

        return string.Join("; ", cspBuilder);
    }

    private async Task<bool> DetectAttackPatternsAsync(HttpContext context, ISecurityMonitoringService? securityMonitoring)
    {
        var request = context.Request;
        var ipAddress = GetClientIpAddress(context);
        var userAgent = request.Headers.UserAgent.ToString();

        // Check for SQL injection patterns
        if (await DetectSqlInjectionAsync(context))
        {
            if (securityMonitoring != null)
            {
                await securityMonitoring.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.SqlInjectionAttempt,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Message = "SQL injection attempt detected",
                    Severity = SecuritySeverity.High
                });
            }
            return true;
        }

        // Check for XSS patterns
        if (await DetectXssAttackAsync(context))
        {
            if (securityMonitoring != null)
            {
                await securityMonitoring.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.XssAttempt,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Message = "XSS attack attempt detected",
                    Severity = SecuritySeverity.High
                });
            }
            return true;
        }

        // Check for path traversal
        if (DetectPathTraversal(context))
        {
            if (securityMonitoring != null)
            {
                await securityMonitoring.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.UnauthorizedAccess,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Message = "Path traversal attempt detected",
                    Severity = SecuritySeverity.High
                });
            }
            return true;
        }

        return false;
    }

    private async Task<bool> DetectSqlInjectionAsync(HttpContext context)
    {
        var path = context.Request.Path.ToString().ToLowerInvariant();
        var queryString = context.Request.QueryString.ToString().ToLowerInvariant();

        // Whitelist legitimate endpoints that might contain SQL-like keywords
        var whitelistedPaths = new[]
        {
            "/export/downloadstat",
            "/export/downloadtable",
            "/export/testdownloadnoauth",
            "/export/testdata",
            "/export/testtuples",
            "/dashboard",
            "/certificate",
            "/admin"
        };

        // Check if this is a whitelisted path
        if (whitelistedPaths.Any(whitelist => path.StartsWith(whitelist)))
        {
            // For whitelisted paths, use more specific SQL injection patterns
            var strictSqlInjectionPatterns = new[]
            {
                "' or ", "\" or ", "' and ", "\" and ",
                "'; drop", "\"; drop", "'; delete", "\"; delete",
                "union all select", "union select", "1=1", "1' or '1'='1",
                "admin'--", "' or 1=1--", "or 1=1#", "' or 'a'='a",
                "xp_cmdshell", "exec(", "execute(", "sp_executesql"
            };

            return strictSqlInjectionPatterns.Any(pattern => 
                queryString.Contains(pattern) || path.Contains(pattern));
        }

        // For non-whitelisted paths, use broader detection
        var sqlInjectionPatterns = new[]
        {
            "union", "select", "insert", "update", "delete", "drop", "exec", "execute",
            "'", "\"", ";", "--", "/*", "*/", "xp_", "sp_", "@@", "char(",
            "nchar(", "varchar(", "nvarchar(", "alter", "begin", "cast", "create",
            "cursor", "declare", "fetch", "kill", "open", "sys", "table"
        };

        foreach (var pattern in sqlInjectionPatterns)
        {
            if (queryString.Contains(pattern) || path.Contains(pattern))
            {
                return true;
            }
        }

        // Check form data if available
        if (context.Request.HasFormContentType)
        {
            try
            {
                var form = await context.Request.ReadFormAsync();
                var formContent = string.Join(" ", form.Select(kvp => kvp.Value.ToString())).ToLowerInvariant();
                
                foreach (var pattern in sqlInjectionPatterns)
                {
                    if (formContent.Contains(pattern))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore form reading errors
            }
        }

        return false;
    }

    private Task<bool> DetectXssAttackAsync(HttpContext context)
    {
        var xssPatterns = new[]
        {
            "<script", "</script", "javascript:", "onload=", "onerror=", "onclick=",
            "onmouseover=", "onfocus=", "onblur=", "onsubmit=", "vbscript:",
            "expression(", "url(javascript:", "style=", "background:", "iframe"
        };

        var queryString = context.Request.QueryString.ToString().ToLowerInvariant();
        var path = context.Request.Path.ToString().ToLowerInvariant();

        foreach (var pattern in xssPatterns)
        {
            if (queryString.Contains(pattern) || path.Contains(pattern))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    private bool DetectPathTraversal(HttpContext context)
    {
        var path = context.Request.Path.ToString();
        var pathTraversalPatterns = new[] { "../", "..\\", "%2e%2e", "%2e%2e%2f", "%2e%2e%5c" };

        return pathTraversalPatterns.Any(pattern => 
            path.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP headers (when behind a reverse proxy)
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

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}