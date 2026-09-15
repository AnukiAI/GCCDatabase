# ASP.NET Core — hostname change checklist

Run the audit prompt (`assets/audit-prompt.md`) first so you're editing from
evidence, not memory.

## AllowedHosts

`appsettings.json` (and any environment override):

```json
"AllowedHosts": "database.greencirclecertified.com;db.greencirclecertified.com;gccwebapp.com"
```

Semicolon-separated. Include every hostname the site will answer on, including the
old hostname if it's being bound here for a redirect — host filtering runs before
your redirect does. A missing host returns a bare 400.

## Forwarded headers (when a proxy sits in front)

Plesk on Linux puts nginx in front of the app; Plesk on Windows uses IIS with
ANCM (in-process, no forwarding needed unless there's a separate proxy). If any
proxy terminates TLS, add in `Program.cs` **before** `UseHttpsRedirection`/`UseHsts`:

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                       | ForwardedHeaders.XForwardedProto
                       | ForwardedHeaders.XForwardedHost;
    o.KnownProxies.Clear();      // or add the proxy IP; clearing trusts all — only
    o.KnownNetworks.Clear();     // acceptable when the proxy is on the same box
});
// ...
app.UseForwardedHeaders();
```

Without `XForwardedHost`, `Url.Action(..., protocol: "https")` and
`LinkGenerator.GetUriByAction` build links with the internal host.

## URL generation

Absolute links built from the request (`Url.Action` with `protocol`/`host`,
`LinkGenerator.GetUri*`, `Request.Scheme + Request.Host`) follow the incoming
hostname automatically. Links built from configuration don't. Grep for:

```
gccwebapp | BaseUrl | SiteUrl | AppUrl | PublicUrl | https:// | http://
```

across `appsettings*.json`, `launchSettings.json`, `*.cshtml`, email template
folders, PDF/label/QR generation, `wwwroot/**/*.js`, `web.config`. For each hit,
decide: move to the request host, or centralise into a single `Site:BaseUrl`
setting that the environment sets to the client hostname.

Background jobs and email senders have no `HttpContext` — they need the configured
base URL. That's the one legitimate hardcode; keep it in configuration, not code.

## Cookies and auth

- `CookieAuthenticationOptions.Cookie.Domain`: if unset (default), cookies are
  host-scoped and just work on the new host. If set, update it. Don't set it to the
  client's root domain unless you mean to share cookies with their WordPress site.
- Antiforgery cookie: same rule.
- External identity providers (Microsoft Entra, Google, Auth0): register the new
  redirect URI (`https://<host>/signin-oidc` or whatever the scheme uses) *before*
  cutover, keep the old one until after.
- Data Protection keys: if they're stored on disk per site, an alias on the same
  site shares them. A second Plesk domain would not — another reason to use an alias.

## CORS

If the partner API or a separate front end calls in, update `WithOrigins(...)` to
include the client hostname and any partner origins. Don't switch to `AllowAnyOrigin`
to make a test pass.

## Canonical host redirect (in app, as a fallback)

Prefer the web-server layer (see `plesk.md`). If you have to do it in code, put it
early in the pipeline, after `UseForwardedHeaders`, before auth:

```csharp
var canonical = builder.Configuration["Site:CanonicalHost"]; // database.greencirclecertified.com
if (!string.IsNullOrEmpty(canonical))
{
    app.Use(async (ctx, next) =>
    {
        if (!ctx.Request.Host.Host.Equals(canonical, StringComparison.OrdinalIgnoreCase)
            && !ctx.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            var target = $"https://{canonical}{ctx.Request.Path}{ctx.Request.QueryString}";
            ctx.Response.Redirect(target, permanent: true);
            return;
        }
        await next();
    });
}
```

Leave `Site:CanonicalHost` empty in non-production environments.

## SMTP configuration

Bind an options class rather than reading loose keys:

```json
"Smtp": {
  "Host": "mail.<plesk-hostname>",
  "Port": 587,
  "UseStartTls": true,
  "Username": "noreply@database.greencirclecertified.com",
  "Password": "<user-secrets / environment>",
  "From": "GreenCircle Certified <noreply@database.greencirclecertified.com>"
}
```

Password goes in `dotnet user-secrets` locally and an environment variable or
Plesk-managed setting in production — never committed.

## Verification after deploy

- `curl -sI https://<host>/` → 200, `Strict-Transport-Security` present.
- `curl -sI http://<host>/` → 301 to https.
- Log in; inspect a password-reset email; confirm the link host is the client host.
- Generate one PDF/certificate/QR and check the embedded URL.
- Run `scripts/verify-cutover.sh` from a machine outside the hosting network.
