using System.Text;
using Microsoft.AspNetCore.Diagnostics;

namespace GCCWebApp.Middleware;

public class TestEnvironmentGateOptions
{
    public bool Enabled { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Realm { get; set; } = "GCC Test Environment";
}

public class TestEnvironmentGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TestEnvironmentGateOptions _options;
    private readonly ILogger<TestEnvironmentGateMiddleware> _logger;

    public TestEnvironmentGateMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<TestEnvironmentGateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _options = new TestEnvironmentGateOptions();
        configuration.GetSection("TestEnvironmentGate").Bind(_options);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Skip API endpoints — they authenticate with X-API-Key.
        // Skip robots.txt so bots can see the Disallow directive without a 401.
        // Skip Google Search Console verification tokens (google*.html at root)
        // so the crawler can confirm site ownership despite the gate.
        if (context.Request.Path.StartsWithSegments("/api") ||
            path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
            IsSearchEngineVerificationFile(path))
        {
            await _next(context);
            return;
        }

        if (TryAuthenticate(context))
        {
            await _next(context);
            return;
        }

        // Disable the app's UseStatusCodePages handler for this request so
        // the 401 surfaces the Basic Auth browser prompt instead of being
        // redirected to /Account/Login.
        var statusCodeFeature = context.Features.Get<IStatusCodePagesFeature>();
        if (statusCodeFeature != null)
        {
            statusCodeFeature.Enabled = false;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers["WWW-Authenticate"] =
            $"Basic realm=\"{_options.Realm}\", charset=\"UTF-8\"";
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(
            "This is a private test environment. Enter the shared test credentials to continue.");
    }

    private bool TryAuthenticate(HttpContext context)
    {
        if (string.IsNullOrEmpty(_options.Username) || string.IsNullOrEmpty(_options.Password))
        {
            _logger.LogWarning(
                "TestEnvironmentGate is enabled but Username/Password are not configured — denying all access.");
            return false;
        }

        string? header = context.Request.Headers["Authorization"];
        if (string.IsNullOrEmpty(header) ||
            !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var encoded = header.Substring("Basic ".Length).Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var idx = decoded.IndexOf(':');
            if (idx <= 0) return false;

            var user = decoded.Substring(0, idx);
            var pass = decoded.Substring(idx + 1);

            return CryptographicEquals(user, _options.Username)
                && CryptographicEquals(pass, _options.Password);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // Google/Bing place a verification file at the site root (e.g.
    // /googlef0a6fc6347de275d.html, /BingSiteAuth.xml). The bots fetch these
    // unauthenticated, so the Basic Auth gate has to let them through.
    private static bool IsSearchEngineVerificationFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        // Only match root-level files (one slash, e.g. "/google<hash>.html"),
        // not anything nested under a directory.
        if (path.IndexOf('/', 1) >= 0) return false;

        // Google: /google<hash>.html
        if (path.StartsWith("/google", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Bing: /BingSiteAuth.xml
        if (path.Equals("/BingSiteAuth.xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    // Constant-time string compare — prevents timing-attack enumeration of the
    // shared test password. Overkill for a shared-secret gate but harmless.
    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }
}
