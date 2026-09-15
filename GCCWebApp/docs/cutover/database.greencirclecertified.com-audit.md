# Hostname audit — `database.greencirclecertified.com`

**Run:** 2026-09-14, read-only. Scope: what in the application assumes a hostname.
**Verdict: no code change is required for the app to serve on the new hostname.**

---

## 1. Host allow-list — nothing to do

`AllowedHosts` is `"*"` in every environment file:

| File | Value |
|---|---|
| `appsettings.json:12` | `*` |
| `appsettings.Production.json:24` | `*` |
| `appsettings.Staging.json:18` | `*` |
| `appsettings.Test.json:9` | `*` |
| `appsettings.Production.template.json:9` | `*` |

A new hostname is accepted with no config change. **Noted as a security observation, not a cutover
blocker:** `"*"` also means the app answers to any Host header. Tightening it to an explicit list is
worth doing *after* cutover — doing it now adds a way for go-live to fail.

## 2. CORS — already prepared

`appsettings.Production.json:186-192` already allows the new origin:

```
"https://database.greencirclecertified.com",
"https://gccwebapp.com",
"https://www.gccwebapp.com",
"https://greencirclecertified.com",
"https://www.greencirclecertified.com"
```

Added 2026-08-24 and commented in-file as defensive. It becomes load-bearing at cutover.

## 3. Generated URLs — follow the request host, correctly

`Controllers/Api/v1/CertificatesController.cs:1670`

```csharp
var baseUrl = $"{request.Scheme}://{request.Host}";
```

The GS1 report `id` and product URIs are built from this. **Consequence worth stating to partners:**
the GS1 `id` is a URI, and it *changes* when the hostname changes — a partner storing `id` as a
primary key will see new values after cutover. The guide already documents this behaviour ("The
report-level `id` is built from the host you called"), so no surprise, but it belongs in the go-live
note to Amazon.

`pdfDownloadUrl` is emitted **relative** (`/api/v1/certificates/{number}/download`), so it is
hostname-agnostic. Good.

## 4. Hardcoded hostnames — three, all correct as-is

| Location | Value | Verdict |
|---|---|---|
| `CertificatesController.cs:1734` | `AgencyURL = "https://www.greencirclecertified.com"` | **Correct** — the certifying body's marketing site, not the app. Leave. |
| `AccountController.cs:1214` | `email.EndsWith("@greencirclecertified.com")` | Email-domain rule, unrelated to web hostname. Leave. |
| `appsettings.Test.json:68` | `"BaseUrl": "https://gccwebapp.com"` | Test-harness config, not runtime link generation. Leave. |

No `SiteUrl` / `AppUrl` / `PublicUrl` in any live environment file — only in
`appsettings.Production.template.json` as `https://yourdomain.com` placeholders, which are unused.

## 5. Proxy headers — configured

`Extensions/SecurityServicesExtensions.cs:47-49, 221` — `XForwardedFor | XForwardedProto`, and
`UseForwardedHeaders()` is called. Correct for IIS in front of Kestrel; scheme-aware URL generation
will produce `https://` rather than `http://`.

## 6. Cookies and auth — no domain pinning found

No explicit `Cookie.Domain` is set, so auth cookies are host-scoped. That is the safe default here:
a session on `gccwebapp.com` simply will not carry to `database.…`, which is correct — users sign in
again on the new hostname once. No OAuth/OIDC redirect URIs are registered against an external IdP
for the hostname being changed (the app uses its own login; Azure AD B2C is configured but not the
path in use for these accounts — confirm before go-live if B2C is enabled in Production).

## 7. Data-protection keys — check before, not after

Auth cookies and antiforgery tokens are signed with the data-protection key ring. Cutover does not
change the key ring (same server, same site, same app pool), so **existing sessions on
`gccwebapp.com` are unaffected**. Flagged only because adding a *second Plesk site* rather than a
domain alias would create a second key ring and break tokens across the two hostnames — which is the
reason the runbook insists on **alias, not new site**.

---

## Items deliberately NOT changed

- `AllowedHosts` left at `"*"` — tightening it is a post-cutover hardening task, not a go-live step.
- Sender address left at `admin@gccwebapp.com` — moving it to the client domain needs SPF/DKIM at
  HostMonster plus the M365 admin, and is out of today's scope.
- No canonical redirect added in application code. Per the skill, redirects belong at the host layer
  so a rollback is a config flip rather than a redeploy.
