# Hostname audit — `database.greencirclecertified.com`

**Parts 2-5 of the read-only cutover audit.** Produced 2026-09-14.

> **READ-ONLY. Nothing was changed.** No code, no configuration, no DNS record, no Plesk setting, no
> deploy. No request was sent to any application on any host — the only network activity was public
> DNS resolution, recorded in `database.greencirclecertified.com-inventory.md` §2.

**Context taken as given** (source: `docs/tracking/2026-08-14-amazon-host-change-note.md`):

| | |
|---|---|
| App | GCC Database Solution — ASP.NET Core 8.0 MVC, EF Core 9, SQL Server |
| Placeholder hostname | `gccwebapp.com` |
| Target client hostname | `database.greencirclecertified.com` (decided 2026-08-14) |
| Old hostname | `db.greencirclecertified.com` — Next.js static export on **Vercel** |
| App hosting | AccuWebHosting, **Plesk on Windows / IIS, in-process** |
| Client DNS | **HostMonster** (not Bluehost — corrected by live lookup) |
| Go-live | **unknown** |

---

## Headline

**The application's own URL generation is clean.** Every absolute URL it emits is built from
`Request.Scheme` + `Request.Host`, so it follows whatever hostname the request arrived on. There is
**no hardcoded hostname anywhere in the URL-generating path** — not in email links, not in the
partner API payloads, not in the views. Renaming the host requires **zero changes to C# code**.

The work is therefore not in the app. It is in five things around it, and four of them are blockers:

| | Blocker | Where |
|---|---|---|
| 1 | **The test-environment gate** is a global password wall and cannot be host-aware in a single process | Part 3.1 |
| 2 | **`robots.txt` is `Disallow: /`** — the public certificate directory would launch unindexable | Part 3.2 |
| 3 | **The target hostname already resolves to an unrelated server** — blocks TLS issuance | inventory §3.1 |
| 4 | **The sender address is `admin@gccwebapp.com`** and cannot survive cutover | Part 3.5 |
| 5 | Old-site deep links keyed by the old system's integer id — **solvable, and the map already exists** | Part 5 |

---

## Part 2 — Hardcoded hostname and URL audit

### 2.1 Every absolute URL the app generates is request-derived

Searched: `appsettings*.json`, `launchSettings.json`, `Program.cs`, all controllers and services,
all `.cshtml`, `wwwroot/**/*.js`, `web.config`, CI config, tests. Pattern set: the placeholder and
old hostnames, `http://`, `https://`, and `BaseUrl` / `SiteUrl` / `AppUrl` / `PublicUrl` /
`CanonicalHost` / `Domain`.

| file:line | Value | Used for | Fix |
|---|---|---|---|
| `Controllers/AccountController.cs:530` | `$"{Request.Scheme}://{Request.Host}/Account/ResetPasswordWithToken?…"` | **password-reset email link** | **none** — follows the request host |
| `Controllers/AccountController.cs:406` | `Url.Action(…, protocol: Request.Scheme)` | signed-out callback | none |
| `Controllers/Api/v1/CertificatesController.cs:1670` | `$"{request.Scheme}://{request.Host}"` | **base for GS1 JSON-LD `id` fields** (`:1713`, `:1716`) | none — but **partner-visible**, see 2.2 |
| `Controllers/Api/CertificateTilesApiController.cs:601` | `$"{Request.Scheme}://{Request.Host}/Certificate/CertificateTileView"` | shareable tile URL | none |
| `Controllers/Api/CertificateTileInternalApiController.cs:779` | same shape, internal view | shareable tile URL | none |
| `Controllers/Api/CertificateTileExternalApiController.cs:874` | same shape, external view | shareable tile URL | none |
| `Views/Certificate/CertificateInternalView.cshtml:413` | `…/Certificate/Verify/{number}` | **verification link printed on the certificate view** | none |
| `Views/Certificate/CertificateExternalView.cshtml:293` | same | same | none |
| `Views/Admin/GCCAPI.cshtml:51,59,246` | `@(Context.Request.Scheme)://@(Context.Request.Host)/api/v1` | API docs page shows the caller their own base URL | none — **desirable**; it self-updates |

**Nothing in this table needs editing.** That is the finding, and it is worth stating positively:
the usual expensive part of a hostname change does not exist here.

### 2.2 The one place the change is visible to a third party

`Controllers/Api/v1/CertificatesController.cs:1670` builds the GS1 JSON-LD `id` from the request
host, so Amazon's payload `id` values change from `https://gccwebapp.com/api/v1/certificates/gs1/…`
to `https://database.greencirclecertified.com/api/v1/certificates/gs1/…` the moment they point at
the new host.

This is **correct** behaviour and it is **already drafted for Amazon** in
`docs/tracking/2026-08-14-amazon-host-change-note.md:33-37`. Two things to carry forward:

- The note is **still unsent** and still carries a literal `[DATE]`. Its own internal notes say it
  must reach Amazon **before** cutover, not with it.
- The API contract is otherwise frozen and unchanged, so this is a configuration change on their
  side, not a contract change ([[feedback_amazon_api_immutable]]).

### 2.3 Hostnames that appear in config but do not generate URLs

| file:line | Value | Verdict |
|---|---|---|
| `appsettings.Production.json:186-192` | `AllowedOrigins` — already lists `https://database.greencirclecertified.com` | **already done**, added 2026-08-24. CORS origins, not hosts the app runs on. |
| `appsettings.Production.json:157` | `SmtpSettings:FromEmail = admin@gccwebapp.com` | **must change** — Part 3.5 |
| `Middleware/SecurityHeadersMiddleware.cs:158` | CSP `connect-src … https://api.greencirclecertified.com` | **dead** — the hostname returns NXDOMAIN. Harmless; do not carry it forward as meaningful. |
| `appsettings.Test.json:68` | `TestConfiguration:BaseUrl = https://gccwebapp.com` | test-harness only; not read by the app |
| `appsettings.Production.template.json:28` | `ApiSettings:BaseUrl = https://yourdomain.com/api` | placeholder; nothing reads it |
| `appsettings.Performance.json:45` | `CDN:BaseUrl` (CDN disabled) | inert |
| `Services/EmailService.cs:25,28` | `mail.gccwebapp.com` in a **doc comment** explaining why it must NOT be used | accurate; keep |

The ~3,600 other occurrences of `gccwebapp.com` are in `Scripts/smoke-prod/`, `qc*-live-check/` and
data artifacts — test harnesses that deliberately target the test host. They are **not** shipped app
code and are out of scope for cutover, but see Open Question 8.

### 2.4 There is no `Site:BaseUrl` — and nothing currently needs one

The skill's ASP.NET reference notes that background jobs and email senders have no `HttpContext` and
therefore need a configured base URL. Checked: the one background job
(`Jobs:CertificateExpiration`, a daily expiry sweep) **generates no URLs**, and `EmailService`
receives fully-built links from the controller. So no configured base URL is required today.

**If a future notification email is ever sent from a background job, this becomes a real gap.** Worth
knowing before someone adds one.

---

## Part 3 — Hostname-sensitive configuration

### 3.1 BLOCKER — the test-environment gate cannot be host-aware in one process

`Middleware/TestEnvironmentGateMiddleware.cs`, registered at `Program.cs:806`, config at
`appsettings.Production.json:57-68` with **`Enabled: true`**.

It is a global HTTP password wall in front of **every non-API path**. It lets through only `/api/*`,
`/robots.txt`, `/favicon.ico` and Google site-verification files (`:85-88`). Everything else —
including the deliberately public certificate directory (`/Certificate/CertificateTileView` and
`/Certificate/CertificateSingleView/{number}`, both `[AllowAnonymous]`) — is redirected to a
shared-password page.

**Why this collides with the cutover, specifically:**

- The gate reads **one flag**, and it is **host-blind**. There is no per-hostname condition anywhere
  in the middleware.
- A Plesk **domain alias** — the skill's recommended Step 2 — puts both hostnames on the **same site,
  same app pool, same process, same config file**. So `Enabled` is necessarily the same value for
  `gccwebapp.com` and `database.greencirclecertified.com`.
- Both environments already run as `ASPNETCORE_ENVIRONMENT=Production` (`web.config:23`), so they
  read the same `appsettings.Production.json` — the identical trap `production-cutover-plan.md:126`
  documents for `Enable2FA`.

**Therefore you must pick one of three, and it is a decision, not a detail:**

| Option | Consequence |
|---|---|
| **(a)** Turn the gate off at cutover | The client host is public **and so is `gccwebapp.com`** — the test env loses its password wall on the same instant. Combined with `robots.txt` (3.2) this is how the earlier indexing problem recurs. |
| **(b)** Make the gate host-aware | Small, well-contained code change: skip the gate when `Request.Host.Host` equals the canonical host. Needs a defect/CR number, a build and a deploy — it is **not** a config flip. |
| **(c)** Build the separate production environment | What `email-draft-2026-08-12-production-hosting-request.md` actually asks for. Two processes, two configs, no conflict — and it also fixes test and production sharing one database. Much larger. |

**Two further facts about the gate that will bite whichever option is chosen:**

- It binds its options **once in the constructor** and is a **singleton** — `appsettings.Production.json:62-64`
  says so explicitly. Changing `Enabled` has **no effect until the app pool recycles.**
- `appsettings.Production.json` is **gitignored** (`GCCWebApp/.gitignore:45`) but **is** published in
  the deploy bundle. The copy in the working tree is the only copy, and it overwrites the server's on
  every deploy.

### 3.2 BLOCKER — `robots.txt` blocks all crawlers

`wwwroot/robots.txt`, in full:

```
# Test environment — block all crawlers/indexers
User-agent: *
Disallow: /
```

Correct for a pre-launch test host. **Wrong for the client's production hostname**, where the public
certificate directory is a product feature — the whole point of
`/Certificate/CertificateTileView` being `[AllowAnonymous]`.

It is a **single static file served from one wwwroot**, so — exactly like the gate — an alias cannot
serve `Disallow: /` on `gccwebapp.com` and an open policy on the client host. Same three options,
same decision. Note the gate deliberately lets `/robots.txt` through unauthenticated (`:86`) so this
file is the one thing crawlers *can* read today.

There is a documented playbook for the surrounding work ([[reference_seo_deindex_playbook]]) — the
inverse direction (deindexing `gccwebapp.com`) is as much a part of go-live as indexing the new host.

### 3.3 `AllowedHosts` — already permissive, so no change needed (and that is not entirely good news)

`"AllowedHosts": "*"` in **every** environment file: `appsettings.json:12`,
`appsettings.Production.json:24`, `appsettings.Test.json:9`, `appsettings.Staging.json:18`,
`appsettings.Production.template.json:9`.

**Consequence for cutover:** the new hostname works the instant it is bound. There is no
400-on-unknown-host to trip over — the failure mode the skill's reference warns about does not apply.

**Recommended value if you choose to tighten it** (the audit prompt asks for an exact proposal):

```json
"AllowedHosts": "database.greencirclecertified.com;db.greencirclecertified.com;gccwebapp.com;www.gccwebapp.com;localhost"
```

Include `db.…` **only if** the old hostname is bound to this server for the Part 5 redirect — host
filtering runs **before** any redirect rule, so omitting it turns the redirect into a bare 400.

**This is optional and it is a tightening, not a cutover requirement.** Recommendation: **do not
change it during the cutover.** Ship the cutover with `*`, confirm everything works, then tighten as
a separate change with its own verification. A cutover is the wrong moment to introduce a setting
whose failure mode is a blank 400.

### 3.4 Forwarded headers — configured, but the middleware never runs

`Extensions/SecurityServicesExtensions.cs:47-63` configures `ForwardedHeadersOptions`
(`XForwardedFor | XForwardedProto`) and `:221` calls `app.UseForwardedHeaders()` inside
`UseSecurityPipeline(...)`.

**`UseSecurityPipeline` is never called.** The only reference to it in the entire codebase is its own
definition. `Program.cs:528` calls `ConfigureSecurityServices` (the service registration) and then
builds the pipeline by hand. So `UseForwardedHeaders()` never executes anywhere, and the configured
options are inert. The same dead method also contains an unreached `UseHsts`, `UseHttpsRedirection`
and `UseCors` — all three are separately and correctly called from `Program.cs` (`:755`, `:789`,
`:793`), so nothing is missing; the extension is simply dead code.

**Does this matter for the cutover? Almost certainly not, and here is why it is safe to say so:**
Plesk on **Windows** hosts ASP.NET Core through IIS + ANCM **in-process** (`web.config:20`,
`hostingModel="inprocess"`). There is no nginx in front, so `Request.Scheme` and `Request.Host`
already carry the real client-facing values. `X-Forwarded-Host` is not needed.

**It matters the moment anything is put in front of the app** — a WAF, a CDN, a load balancer. The
hosting-request email (§4) explicitly asks the sysadmin to disclose that. If the answer is yes:

- every generated link in Part 2.1 silently reverts to the internal host, **and**
- `IpRateLimiting.RealIpHeader: X-Forwarded-For` (`appsettings.Production.json:198`) is read by the
  rate-limiter directly, independent of this middleware — so with no trusted proxy configured, a
  client can currently **spoof `X-Forwarded-For` to evade rate limiting**. Worth its own defect;
  raising it here only because the cutover is when a proxy is most likely to appear.

Also note `Scripts/deployment/nginx-plesk-config.conf` exists in the repo and describes a Linux/nginx
front end. It is dead (2025-10-10) and contradicts the live hosting model — see the documentation
inventory §5.

### 3.5 BLOCKER — outbound email

`appsettings.Production.json:150-159`:

```
Host      accumail3.accuwebhosting.com     (NOT mail.gccwebapp.com — DEF-263, cert CN mismatch)
Port      465, Security SslOnConnect       (587 answers STARTTLS with "503 TLS is not allowed")
Username/Password   ""  — supplied by appsettings.Secrets.json on the server, never in git
FromEmail admin@gccwebapp.com
FromName  GCC Database System
```

**`admin@gccwebapp.com` cannot survive cutover.** Once the product lives at the client's domain, a
password-reset mail arriving from `gccwebapp.com` reads as phishing, and the placeholder domain will
eventually stop existing.

From the live DNS (inventory §3.3), the two options are not equal:

- **Subdomain sender — `noreply@database.greencirclecertified.com`. Recommended.**
  `TXT database.greencirclecertified.com` is **empty**, so SPF/DKIM/DMARC there are greenfield and
  touch none of the client's existing mail. DKIM key generated on the Plesk mail server.
- **Root-domain sender — `noreply@greencirclecertified.com`.** The client runs **Microsoft 365**
  (`MX → …mail.protection.outlook.com`) and their root SPF does **not** include `72.18.135.96`, the
  AccuWeb relay. This **fails SPF on day one** unless their M365 admin edits the root SPF — a
  coordination item with their IT, not something we can configure.

Two operational traps already documented and both still live:

- If **any** of Host/Username/Password is empty the app silently selects `MockEmailService` — mail
  stops, nothing errors (`production-cutover-plan.md` §1, `email-draft-2026-08-12` §5). **Verify from
  the startup `Email:` banner, never from the config file.**
- Environment variables on this host live in `web.config`, which **is overwritten by every publish**
  (`production-cutover-plan.md:107-112`). Anything set on the server that way is lost silently on the
  next deploy.

And one that is new here: **`appsettings.Secrets.json` is keyed to the mailbox, not the hostname.**
Changing the sender address means a new mailbox and a new password on the server, applied as a
post-deploy step. `Scripts/deploy.ps1:348-350` already **aborts** the build if that file is ever
staged into the bundle — that guard is correct and must not be relaxed to "make the new sender work".

### 3.6 Cookies, auth and Data Protection — all fine, for a specific reason

| Item | State | Cutover impact |
|---|---|---|
| Auth cookie `GCC.Auth` | `Cookie.Domain` **never set** (`Program.cs:348-358`) | **None.** Host-scoped, so it just works on the new host. Deliberately do **not** set it to the client's root domain — that would share cookies with their Squarespace site. |
| Session `GCC.Session` | `Cookie.Domain` not set (`Program.cs:64-68`) | none |
| Antiforgery `__RequestVerificationToken` | `Cookie.Domain` not set, `SameSite=Strict` (`Program.cs:460-469`) | none |
| Data Protection `ApplicationName` | `"GCC-Production"` (`appsettings.Production.json:325`) | **Alias shares the key ring → nobody is signed out.** A *separate environment* does not → everyone is. A real difference between the two options in the documentation inventory §4. |
| Data Protection `KeyPath` | unset → `<app root>\DataProtectionKeys` | Keys sit **inside the deploy folder**, so a deploy can invalidate every session. Pre-existing (DEF-395 / the hosting email §3), not caused by the cutover — but a cutover is a deploy. |
| External IdP redirect URIs | **none to register.** `AzureAdB2C` is all `{PLACEHOLDER}` tokens (`appsettings.Production.json:161-172`); the app runs its own cookie auth (`production-cutover-plan.md` §4) | **None.** The usual "register the new redirect URI with the IdP" step does not exist here. |

### 3.7 CORS — already correct, with a stale comment

`AllowedOrigins` (`appsettings.Production.json:186-192`) already contains
`https://database.greencirclecertified.com`. **No change needed.**

Two accuracy notes for whoever reads that block next:

- Its comment says *"read by Program.cs:473"*. The live reader is now **`Program.cs:509-520`**, the
  named policy `ApiCorsPolicy`, applied at `Program.cs:793`.
- `Extensions/SecurityServicesExtensions.cs:181-213` registers a **default** CORS policy from the
  same config key, but nothing ever applies it (`UseCors()` with no name is only in the dead
  `UseSecurityPipeline`). Harmless duplication; do not "fix" one and assume the other changed.

### 3.8 HSTS — check the `preload` directive before the client's domain is in play

`Middleware/SecurityHeadersMiddleware.cs:75` emits, on every HTTPS response:

```
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

On `database.greencirclecertified.com` this asserts HSTS for **that host and its subdomains only** —
it does **not** reach the client's root domain or their Squarespace site. Safe as-is.

Two things to be deliberate about:

- **Do not submit the domain to the HSTS preload list.** Preload is claimed at the registrable-domain
  level; submitting `greencirclecertified.com` would force HTTPS across everything the client owns,
  including hosts we do not control, and is **very hard to undo**. The directive sitting in the
  header is inert until someone submits it. Leave it inert.
- **If the app is ever bound to the apex** `greencirclecertified.com`, `includeSubDomains` would
  immediately assert HTTPS for every subdomain of the client's domain. Nothing plans to do that —
  the subdomain decision of 2026-08-14 rules it out — but it is the reason that decision should not
  be quietly revisited.

---

## Part 4 — Redirect placement

### 4.1 The server layer is available and already proven

`web.config:42-53` contains a working IIS **URL Rewrite** rule (HTTP → HTTPS, `redirectType="Permanent"`,
preserving path via `{R:1}`). So the URL Rewrite module **is installed and functioning** on this
host — verified by the fact that this rule is live, not by assumption.

A canonical-host 301 is therefore a **server-layer rule**, exactly as the skill requires, and needs
**no application code**:

```xml
<rule name="Canonical host" stopProcessing="true">
  <match url="(.*)" />
  <conditions logicalGrouping="MatchAll">
    <add input="{HTTP_HOST}" pattern="^database\.greencirclecertified\.com$" negate="true" />
    <add input="{HTTP_HOST}" pattern="^localhost" negate="true" />
  </conditions>
  <action type="Redirect" url="https://database.greencirclecertified.com/{R:1}"
          redirectType="Permanent" appendQueryString="true" />
</rule>
```

*Illustrative — not applied, not tested.* Order matters: it must sit **after** the HTTPS rule, and
`appendQueryString="true"` is what preserves the query string Part 5 depends on.

### 4.2 The catch — `web.config` is overwritten by every deploy

`web.config` is **tracked in git** and is republished on every deploy (`Scripts/deploy.ps1`). This is
the same mechanism that loses SMTP environment variables (`production-cutover-plan.md:107-112`).

So a rule added **on the server** disappears at the next publish, silently, and the symptom is "the
old links stopped redirecting" — weeks later, with no error anywhere.

**The rule must go in the repo's `web.config` and ship with the bundle.** That is still the server
layer and still keeps the redirect out of application code, but be honest about what rollback then
costs:

| | Rollback |
|---|---|
| DNS record | revert one record — minutes, no deploy |
| `web.config` rule | edit the file on the server (immediate, IIS re-reads it) — but **re-apply to the repo** or the next deploy silently restores it |
| App middleware | full redeploy |

The middle row is acceptable; the bottom row is what the skill says to avoid. **Do not** use the
app-middleware canonical-redirect pattern from `references/aspnet-core.md` §"Canonical host redirect"
here — the server layer is available, so use it.

### 4.3 No canonical-host redirect exists today

Searched: no rewrite rule other than HTTP→HTTPS; no `Site:CanonicalHost` config key; no host-checking
middleware. The app answers on any hostname bound to it, with no canonicalisation. That is the
correct starting state — nothing has to be unwound.

---

## Part 5 — Old-site deep links

### 5.1 The URL shapes that must keep working

`db.greencirclecertified.com` is a Next.js static export on Vercel. Its shapes are documented in this
repo from the migration work (`Scripts/data-ops/2026-08-12-greencircle-public-db-scrape.py:28-33`,
`:123-125`; `2026-09-03-batch2-pull-pdfs.py:52-53`) — **read from the site's own JS chunks in August
2026, not inferred**:

| Old URL | What it does | New equivalent | Mappable? |
|---|---|---|---|
| `/certificate` | public listing, client-side paging | `/Certificate/CertificateTileView` | **yes** — static rule |
| `/` | home | `/` | **yes** — static rule |
| `/api/certificates` | JSON array of **all** records | `/api/v1/certificates` (**API-key gated**) | **no** — see 5.3 |
| `/api/certificates/{id}/download` | 307 → presigned S3 PDF. **Keyed by the old system's integer `id`.** | `/Certificate/CertificateSingleView/{number}` | **yes, via a lookup** — see 5.2 |
| `/api/trpc/certificate.getAll?…` | signed-in portal (tRPC + NextAuth) | no equivalent | **no** — internal to the old vendor's app |

### 5.2 The mapping table already exists — this is the good news

The hard part of a retirement like this is normally that old integer ids are meaningless in the new
system. **Here they are not**, because the migration pull kept them.

`C:\Users\swest\GCCPortalPull\manifest.csv` (2026-08-13, from the signed-in portal — **not** just the
public subset) was profiled for this audit:

```
rows                 6,000
distinct record_id   6,000    (every row unique, every value a plain integer)
distinct cert number 5,822
blank record_id          0
blank cert number        0
sample   record_id 1 -> 20-0245 ;  2 -> 20-0411 ;  3 -> 15-0304
```

**The `record_id → certificate_number` direction is total and unambiguous** — one old id yields
exactly one certificate number. That is precisely the direction a redirect needs. (The reverse is
many-to-one: 6,000 ids over 5,822 numbers, consistent with the known duplicate-certificate-number
population, [[feedback_duplicate_certificate_numbers_exist]] — and it does not matter here, because
nothing needs to go number → id.)

The public-only mirror (`C:\Users\swest\GCCPublicMirror\manifest.csv`, 1,021 rows) carries the same
two columns and is a strict subset. **Use the portal manifest**, not the public one — 1,021 vs 6,000
is the difference between redirecting a sixth of the deep links and redirecting all of them.

**Practical shape:** a generated IIS rewrite map (`<rewriteMap>`) of 6,000 `id → number` pairs, or a
small keyed lookup, turning `/api/certificates/5923/download` into
`/Certificate/CertificateSingleView/24-1633`. Landing on the certificate page rather than straight at
a PDF is the right target anyway — the new app's asset routes take the **new** database Id, not the
certificate number ([[feedback_asset_routes_take_int_id]]), so a PDF-to-PDF redirect would need a
second lookup for no user benefit.

**Not yet verified:** whether all 6,000 old records exist in the new system under the same numbers.
The new system holds ~6,032 certificates and the loads were staged (batch 2 on 2026-09-03, batch 3 on
2026-09-08). A set-comparison of the 5,822 manifest numbers against the live certificate numbers is
cheap and should be done before the map is generated — equal counts are not the same rows
([[feedback_equal_counts_are_not_the_same_rows]]).

### 5.3 What cannot be redirected, and why that needs a client decision

`/api/certificates` on the old site returns **every record as unauthenticated JSON**. The new system
has no equivalent: `/api/v1/certificates` requires an API key
(`[Authorize(AuthenticationSchemes = "ApiKey")]`, `CertificatesController.cs:33`).

That is a **deliberate security posture change**, not an oversight. But if anyone is consuming that
open endpoint today, they break at cutover with a 401 and no warning. Two sub-questions, both for the
client:

1. Is anyone actually calling it? Only the old site's request logs can answer that.
2. If so, do they get an API key, or is the endpoint's retirement the intended outcome?

Redirecting it to the key-gated endpoint would convert a working integration into a 401 — worse than
a clean 404 with a message. **Recommend: do not redirect `/api/*` from the old host.** Decide it
explicitly instead.

### 5.4 Deep-link coverage is inferred, and should not be

Everything above is derived from the scrape scripts and the manifests. **No old-site request log has
been obtained**, so which deep links are actually in use — and whether any URL shape exists that the
scrape never touched — is unknown. The client should ask the outgoing vendor for access logs as part
of the handover request (`references/client-comms.md`). Without them, the redirect map is built from
what the site *exposes*, not from what people *use*.

---

## Recommended change list

**Ordered. Nothing below has been done.**

### Pre-go-live — no production risk

| # | Change | Where | Depends on |
|---|---|---|---|
| 1 | **Get the `database.` A record corrected** — it points at `70.32.82.212` (`new-verbcomm.com`) today. Must be removed and repointed at `64.27.52.66`. Nothing else can start until this is done. | client's HostMonster zone | client access |
| 2 | Decide **alias vs. separate environment** (documentation inventory §4). Everything below branches on it. | decision | Shawn + client |
| 3 | Decide the **sender address** — subdomain recommended (Part 3.5) | decision | client |
| 4 | Add the hostname as a **Plesk domain alias**, DNS management **unchecked**, then issue Let's Encrypt | Plesk | 1 |
| 5 | Resolve the **test-gate conflict** (Part 3.1) — (a), (b) or (c). If (b), it needs a DEF/CR number, a build and a deploy. | code or config | 2 |
| 6 | Resolve **`robots.txt`** (Part 3.2), together with deindexing `gccwebapp.com` | wwwroot + SEO playbook | 2 |
| 7 | Set up the sender mailbox + SPF/DKIM/DMARC for the chosen domain; verify `dkim=pass; spf=pass` in a real header | Plesk + client DNS | 3 |
| 8 | **Send the Amazon host-change note** — fill `[DATE]`, then send. Before cutover, not with it. | `2026-08-14-amazon-host-change-note.md` | go-live date |
| 9 | Build and **verify** the old-id → number map; set-compare the 5,822 numbers against live data first (5.2) | script + `_results` artifact | — |
| 10 | Client sends the **vendor handover request** — including **access logs** (5.4) | client | client |
| 11 | UAT on the new hostname; record sign-off | — | 4-7 |

### Go-live day

| # | Change | Rollback |
|---|---|---|
| 12 | Add the canonical-host rule to the repo `web.config` (Part 4.1) and deploy | edit on server, then re-apply to repo |
| 13 | Bind `db.greencirclecertified.com` on the app server + TLS, with the 301 map, **then** flip its DNS off Vercel | revert one DNS record |
| 14 | Flip the gate / `robots.txt` per the decisions in 5-6; **recycle the app pool** — the gate will not change without it | flip back + recycle |
| 15 | Verify from **outside** the hosting network with `scripts/verify-cutover.sh -n database.greencirclecertified.com -o db.greencirclecertified.com -p gccwebapp.com -i 64.27.52.66 -d deeplinks.txt` | — |
| 16 | Re-check `/appsettings.Production.json` does **not** return 200 (`web.config:72-77`) — a deploy day is exactly when the handler mapping breaks | — |

### Post-go-live

17. Old Vercel site stays up behind the redirect **≥ 30 days**.
18. Watch 404s on the new host for unmapped deep links.
19. Raise DNS TTLs once stable.
20. Write `docs/cutover/database.greencirclecertified.com-runbook.md` — what was **actually** done, with timestamps.
21. Rotate the exposed Amazon partner key ([[reference_amazon_key_in_devguide_pdf]]) — decided 2026-08-14 to happen **after** cutover; zero-downtime because a partner can hold two keys.

---

## Proposed `AllowedHosts`

The audit prompt asks for an exact value. It is:

```json
"AllowedHosts": "database.greencirclecertified.com;db.greencirclecertified.com;gccwebapp.com;www.gccwebapp.com;localhost"
```

**With the recommendation that it is not applied during the cutover.** The value today is `*`, which
means the new hostname works with no change at all. Tightening it is a security improvement worth
making — separately, afterwards, with its own verification — because its failure mode is a bare HTTP
400 that looks exactly like a broken site, and host filtering runs *before* the Part 5 redirect.

---

## Open questions needing a human answer

1. **Go-live date**, and who calls go/no-go. Everything sequences from it; `[DATE]` is still a
   literal in the Amazon note.
2. **Alias on the existing site, or the separate production environment** the 2026-08-12 hosting
   request asks for? This is the largest open question and it is a cost question. The hosting request
   **has not been sent**.
3. **What created `database.greencirclecertified.com` → `70.32.82.212`**, and who can remove it?
4. **Who holds the HostMonster/cPanel login** for `greencirclecertified.com`?
5. **Sender address** — `noreply@database.greencirclecertified.com` (recommended) or root domain
   (needs the client's M365 admin to edit the root SPF)?
6. **Must `db.greencirclecertified.com` keep working**, or is the client content to retire the
   hostname? This decides whether Part 5 happens at all.
7. **Will the outgoing vendor provide access logs**, so the redirect map covers what people actually
   use rather than what the site exposes?
8. **Does anything consume the old open `/api/certificates`?** (5.3)
9. **Is anything in front of the app server** — WAF, CDN, load balancer? If yes, Part 3.4 stops being
   theoretical, and the `X-Forwarded-For` rate-limit spoofing note needs its own defect.
10. **TLS renewal owner** — who is paged when the certificate is 7 days from expiry? An expired cert
    takes the Amazon integration down, not just the website.
11. **Do the ~3,600 `gccwebapp.com` references in `Scripts/smoke-prod/` and `qc*-live-check/` need to
    follow the new host**, or do those harnesses continue to target the test environment? (They
    should continue targeting test — asking so it is a decision rather than a drift.)

---

*Audit performed read-only against the working tree at branch `ci-regression`, 2026-09-14. No files
outside `docs/cutover/` were created or modified.*
