# Claude Code prompt — hosting documentation + hostname cutover audit (read-only)

Fill the placeholders, then paste. Keep it read-only; the build step is a separate
prompt once the audit is reviewed.

```
You are auditing the <APP NAME> codebase and any bundled documentation for a
hostname cutover. This is READ-ONLY: do not modify code, configuration, DNS, or
infrastructure. Write findings to markdown files only.

## Context

- App: <APP NAME>, <FRAMEWORK e.g. ASP.NET Core 8 / MySQL>
- Current (placeholder) hostname: <gccwebapp.com>
- Target client hostname: <database.greencirclecertified.com>
- Old hostname being retired (if any): <db.greencirclecertified.com>, currently
  served by <Vercel via outside vendor; CNAME to cname.vercel-dns.com>
- App hosting: <AccuWebHosting, Plesk on Windows/Linux>, managed by <PointSolve>
- Client DNS provider: <Bluehost / cPanel> (client's WordPress also lives there)
- Go-live: <date>
- Goal: the client hostname becomes the only public URL; the placeholder 301s to it;
  the old hostname (if any) 301s to it with path and query preserved.

## Part 1 — What documentation already exists

Search this repo, and any docs/ or wiki folders mounted, for anything describing
hosting, deployment, DNS, Plesk, TLS/certificates, email/SMTP, or environment
configuration. Include README*, HANDOVER*, docs/**, *.md, deploy scripts, CI
config, web.config, appsettings*.json comments, and any *.txt or *.docx.

Produce a table: file | what it covers | last updated (git log) | still accurate?
(yes / stale / unknown) | gap it leaves. Explicitly list what is NOT documented:
server IP and hostname, Plesk access, which DNS provider is authoritative, TLS
issuance method, SMTP setup, backup/restore, deploy procedure.

## Part 2 — Hardcoded hostname and URL audit

Find every reference to the placeholder hostname, the old hostname, "http://",
"https://", and base-URL-style config keys (BaseUrl, SiteUrl, AppUrl, PublicUrl,
CanonicalHost, Domain) in:
appsettings*.json, launchSettings.json, environment files, Program.cs/Startup.cs,
Razor views and layouts, email templates, PDF/label/certificate/QR generation,
wwwroot JavaScript, web.config / nginx / Caddy files, CI/CD config, and tests.

For each hit: file:line | value | what it's used for | fix (use request host /
move to Site:BaseUrl config / leave). Group by category.

## Part 3 — Hostname-sensitive configuration

Report, with file and line:
- AllowedHosts value(s) per environment
- Whether UseForwardedHeaders is configured and which headers; whether a reverse
  proxy is in front (Plesk nginx, IIS ARR)
- HTTPS redirection / HSTS configuration
- Cookie authentication and antiforgery options — is Cookie.Domain set?
- Any external identity provider and its registered redirect URIs
- CORS policy origins
- SMTP settings and the From address; where the password is sourced
- Any URL generated outside an HTTP request (background jobs, email senders) and
  what base URL it uses
- Data Protection key storage location

## Part 4 — Redirect placement

Identify where a server-layer 301 would be implemented on this stack (web.config
URL Rewrite, Plesk additional nginx directives, Caddy) and whether one already
exists for any hostname. If the only option is app middleware, say so and explain
the rollback consequence (redeploy vs. config flip).

## Part 5 — Old-site deep links (if an old hostname is being retired)

List every public URL shape the OLD site exposes that must keep working (record
detail pages, downloads, search with query params). If old-site logs or a sitemap
are available in the repo or docs, use them; otherwise infer from any migration
notes or import code and flag as unverified. Map each old shape to the new app's
equivalent route. Flag shapes with no equivalent.

## Output

Write to:
- docs/cutover/<target-hostname>-documentation-inventory.md  (Part 1)
- docs/cutover/<target-hostname>-hostname-audit.md            (Parts 2–5)

End the audit file with:
1. A recommended change list, ordered, marked pre-go-live vs go-live-day
2. Open questions that need a human answer (server IP, Plesk OS, who owns the old
   hostname's DNS, sender address decision)
3. The exact AllowedHosts value you'd propose

Do not implement anything. Do not run anything against production hosts.
```
