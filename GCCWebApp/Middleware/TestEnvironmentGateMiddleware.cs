using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;

namespace GCCWebApp.Middleware;

public class TestEnvironmentGateOptions
{
    public bool Enabled { get; set; }

    /// <summary>
    /// "Cookie" (default) — browsers are redirected to a shared-password gate page that
    /// sets a first-party cookie; automated clients may still pass an HTTP Basic header.
    /// "Basic" — legacy behaviour: every browser request is met with a 401 Basic challenge.
    /// Cookie mode exists because Basic Auth in front of the app's own cookie login breaks
    /// on mobile browsers (the browser inconsistently re-sends the Authorization header
    /// across the login POST→redirect chain, colliding with the app's own 401s and bouncing
    /// the tester back to the sign-in page). A first-party SameSite=Lax cookie is sent
    /// reliably on those navigations, so the mobile login loop goes away.
    /// </summary>
    public string Mode { get; set; } = "Cookie";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Realm { get; set; } = "GCC Test Environment";

    /// <summary>How long the gate cookie stays valid before the tester must re-enter the password.</summary>
    public int CookieDays { get; set; } = 7;
}

public class TestEnvironmentGateMiddleware
{
    // Reserved path that serves/handles the cookie gate page. Deliberately unlikely to
    // collide with an app route, and always allowed through the gate check.
    private const string GatePath = "/__gate";
    private const string GateCookieName = "GCC.TestGate";
    // Bump this purpose string to invalidate every outstanding gate cookie at once.
    private const string ProtectorPurpose = "GCCWebApp.TestEnvironmentGate.v1";
    // A form token older than this (minutes) is rejected — bounds CSRF replay on the gate POST.
    private const int FormTokenMaxAgeMinutes = 30;

    private readonly RequestDelegate _next;
    private readonly TestEnvironmentGateOptions _options;
    private readonly ILogger<TestEnvironmentGateMiddleware> _logger;
    private readonly IDataProtector _protector;

    public TestEnvironmentGateMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        IDataProtectionProvider dataProtection,
        ILogger<TestEnvironmentGateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _options = new TestEnvironmentGateOptions();
        configuration.GetSection("TestEnvironmentGate").Bind(_options);
        _protector = dataProtection.CreateProtector(ProtectorPurpose);
    }

    private bool CookieMode =>
        !string.Equals(_options.Mode, "Basic", StringComparison.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // The gate page itself must always be reachable without a cookie (cookie mode only).
        if (CookieMode && context.Request.Path.StartsWithSegments(GatePath))
        {
            await HandleGatePageAsync(context);
            return;
        }

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

        // A valid gate cookie (browsers, cookie mode) OR a valid Basic header
        // (automated smoke/deploy-gate scripts pass httpCredentials on every request)
        // both satisfy the gate.
        if ((CookieMode && HasValidGateCookie(context)) || TryAuthenticateBasic(context))
        {
            await _next(context);
            return;
        }

        if (CookieMode)
        {
            // Redirect browsers to the cookie gate page instead of issuing a 401 Basic
            // challenge. Preserve the originally-requested URL for GET navigations so the
            // tester lands where they intended after entering the password.
            var returnUrl = context.Request.Method == HttpMethods.Get
                ? SafeLocalUrl(path + context.Request.QueryString)
                : "/";
            context.Response.Redirect($"{GatePath}?returnUrl={UrlEncoder.Default.Encode(returnUrl)}");
            return;
        }

        // Legacy Basic mode: surface the browser's native Basic Auth prompt.
        // Disable the app's UseStatusCodePages handler for this request so the 401
        // surfaces the prompt instead of being redirected to /Account/Login.
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

    // ---- Cookie gate page (GET renders the form, POST validates + sets the cookie) ----

    private async Task HandleGatePageAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(_options.Password))
        {
            _logger.LogWarning(
                "TestEnvironmentGate is enabled but Password is not configured — denying all access.");
            await WriteGatePageAsync(context, StatusCodes.Status503ServiceUnavailable,
                returnUrl: "/", error: "The test environment gate is misconfigured. Contact the administrator.");
            return;
        }

        // Already authenticated — no reason to show the form again.
        if (HasValidGateCookie(context))
        {
            context.Response.Redirect(SafeLocalUrl(context.Request.Query["returnUrl"]));
            return;
        }

        if (HttpMethods.IsPost(context.Request.Method))
        {
            await HandleGatePostAsync(context);
            return;
        }

        await WriteGatePageAsync(context, StatusCodes.Status200OK,
            returnUrl: SafeLocalUrl(context.Request.Query["returnUrl"]), error: null);
    }

    private async Task HandleGatePostAsync(HttpContext context)
    {
        // ReadFormAsync throws on a non-form body; a stray/malformed POST to the gate
        // shouldn't 500 — just re-render the form.
        if (!context.Request.HasFormContentType)
        {
            await WriteGatePageAsync(context, StatusCodes.Status400BadRequest,
                returnUrl: "/", error: "Your session expired. Please try again.");
            return;
        }

        var form = await context.Request.ReadFormAsync();
        var returnUrl = SafeLocalUrl(form["returnUrl"]);
        var password = form["password"].ToString();
        var formToken = form["formToken"].ToString();

        // Lightweight CSRF guard: the token was minted (data-protected) when the form was
        // rendered and must still be present, valid, and fresh.
        if (!IsFormTokenValid(formToken))
        {
            await WriteGatePageAsync(context, StatusCodes.Status400BadRequest, returnUrl,
                error: "Your session expired. Please try again.");
            return;
        }

        if (!CryptographicEquals(password, _options.Password))
        {
            _logger.LogWarning("TestEnvironmentGate: failed cookie-gate password attempt from {Ip}.",
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            await WriteGatePageAsync(context, StatusCodes.Status401Unauthorized, returnUrl,
                error: "Incorrect password. Please try again.");
            return;
        }

        IssueGateCookie(context);
        context.Response.Redirect(returnUrl);
    }

    private void IssueGateCookie(HttpContext context)
    {
        var payload = $"ok|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        context.Response.Cookies.Append(GateCookieName, _protector.Protect(payload),
            new CookieOptions
            {
                HttpOnly = true,
                // Lax (not Strict) so the cookie survives a top-level navigation from an
                // external link — how mobile testers usually arrive — and rides along the
                // app's same-site login POST→redirect chain.
                SameSite = SameSiteMode.Lax,
                // Secure over HTTPS (the test env is HTTPS/HSTS); relaxed on plain-HTTP localhost.
                Secure = context.Request.IsHttps,
                IsEssential = true,
                Path = "/",
                MaxAge = TimeSpan.FromDays(Math.Max(1, _options.CookieDays))
            });
    }

    private bool HasValidGateCookie(HttpContext context)
    {
        var cookie = context.Request.Cookies[GateCookieName];
        if (string.IsNullOrEmpty(cookie)) return false;

        try
        {
            var payload = _protector.Unprotect(cookie);
            var parts = payload.Split('|');
            if (parts.Length != 2 || parts[0] != "ok") return false;
            if (!long.TryParse(parts[1], out var issuedUnix)) return false;

            var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issuedUnix);
            return age <= TimeSpan.FromDays(Math.Max(1, _options.CookieDays));
        }
        catch
        {
            // Tampered, or protected under a rotated key/purpose — treat as unauthenticated.
            return false;
        }
    }

    private string MintFormToken() =>
        _protector.Protect($"form|{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");

    private bool IsFormTokenValid(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        try
        {
            var payload = _protector.Unprotect(token);
            var parts = payload.Split('|');
            if (parts.Length != 2 || parts[0] != "form") return false;
            if (!long.TryParse(parts[1], out var mintedUnix)) return false;

            var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(mintedUnix);
            return age <= TimeSpan.FromMinutes(FormTokenMaxAgeMinutes);
        }
        catch
        {
            return false;
        }
    }

    private async Task WriteGatePageAsync(HttpContext context, int statusCode, string returnUrl, string? error)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/html; charset=utf-8";
        // No-store so the browser doesn't cache the gate (or an error state) for the origin.
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";

        var enc = HtmlEncoder.Default;
        var errorHtml = string.IsNullOrEmpty(error)
            ? string.Empty
            : $"<p class=\"error\" role=\"alert\">{enc.Encode(error)}</p>";

        var html = $@"<!doctype html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<meta name=""robots"" content=""noindex, nofollow"">
<title>{enc.Encode(_options.Realm)}</title>
<style>
  * {{ box-sizing: border-box; }}
  body {{
    margin: 0; min-height: 100vh; display: flex; align-items: center; justify-content: center;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    background: linear-gradient(135deg, #0b5cad 0%, #0a8a3f 100%); padding: 24px;
  }}
  .card {{
    background: #fff; width: 100%; max-width: 400px; border-radius: 12px;
    box-shadow: 0 12px 40px rgba(0,0,0,0.25); padding: 32px 28px; text-align: center;
  }}
  h1 {{ font-size: 20px; color: #0b3c66; margin: 0 0 4px; }}
  p.sub {{ font-size: 16px; color: #556; margin: 0 0 24px; }}
  label {{ display: block; text-align: left; font-size: 16px; color: #334; margin-bottom: 6px; font-weight: 600; }}
  input[type=password] {{
    width: 100%; font-size: 16px; padding: 12px 14px; border: 1px solid #c4ccd6;
    border-radius: 8px; margin-bottom: 16px; -webkit-appearance: none;
  }}
  input[type=password]:focus {{ outline: none; border-color: #0b5cad; box-shadow: 0 0 0 3px rgba(11,92,173,0.2); }}
  button {{
    width: 100%; font-size: 16px; font-weight: 600; padding: 13px; border: 0; border-radius: 8px;
    background: #6cb33f; color: #fff; cursor: pointer;
  }}
  button:hover {{ background: #5da02f; }}
  .error {{ background: #fdecec; color: #a12020; font-size: 15px; padding: 10px 12px; border-radius: 8px; margin: 0 0 16px; text-align: left; }}
  .note {{ font-size: 13px; color: #8894a3; margin-top: 20px; }}
</style>
</head>
<body>
  <div class=""card"">
    <h1>{enc.Encode(_options.Realm)}</h1>
    <p class=""sub"">This is a private test environment. Enter the shared access password to continue.</p>
    {errorHtml}
    <form method=""post"" action=""{GatePath}"" autocomplete=""off"">
      <input type=""hidden"" name=""returnUrl"" value=""{enc.Encode(returnUrl)}"">
      <input type=""hidden"" name=""formToken"" value=""{enc.Encode(MintFormToken())}"">
      <label for=""password"">Access password</label>
      <input type=""password"" id=""password"" name=""password"" autofocus required
             autocapitalize=""off"" autocorrect=""off"" spellcheck=""false"">
      <button type=""submit"">Enter</button>
    </form>
    <p class=""note"">Access is restricted to authorized testers.</p>
  </div>
</body>
</html>";

        await context.Response.WriteAsync(html);
    }

    // ---- Shared helpers ----

    private bool TryAuthenticateBasic(HttpContext context)
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

    // Guards against open-redirect: only same-origin, path-absolute URLs are allowed.
    private static string SafeLocalUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "/";
        // Must be a rooted path, not "//host" or "/\host" (protocol-relative escapes),
        // and must not point back at the gate page itself.
        if (url[0] != '/' || url.StartsWith("//") || url.StartsWith("/\\")) return "/";
        if (url.StartsWith(GatePath, StringComparison.OrdinalIgnoreCase)) return "/";
        return url;
    }

    // Google/Bing place a verification file at the site root (e.g.
    // /googlef0a6fc6347de275d.html, /BingSiteAuth.xml). The bots fetch these
    // unauthenticated, so the gate has to let them through.
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
