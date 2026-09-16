# Hostname audit — `database.greencirclecertified.com`

**Run:** 2026-09-14, read-only · **Consolidated:** 2026-09-15 from two parallel audit passes
**Scope:** what in the application assumes a hostname.
**Verdict: no code change is required for the app to serve on a new hostname.**

> **Consolidation note.** Two audits were produced independently on 2026-09-14 — this file and
> `…-hostname-audit.md` — because the skill names its audit output two different ways (`-audit.md` in
> its Outputs section, `-hostname-audit.md` in its audit-prompt template). **This file is now the
> only audit; `-hostname-audit.md` has been removed and everything in it is folded in below.** Each
> pass caught things the other missed, and each corrected the other; where that happened it is marked
> **[corrected]**.

> ### ⚠️ Scope changed 2026-09-14 — read before acting on any of this
>
> Shawn: *"the test link is dbtest.greencirclecertified.com because database and db are both being
> used by the existing database."*
>
> - **`dbtest.greencirclecertified.com` is live and is what partners integrate against today.**
>   Resolves to `64.27.52.66` (verified again 2026-09-15), TLS SAN covers the name,
>   `/api/v1/certificates` returns 401 anonymously. No work needed.
> - **`database.greencirclecertified.com` is NOT free** — it belongs to the existing certificate
>   database. A cutover onto it is **blocked on the legacy system being retired or renamed**, which
>   is a client decision.
>
> The findings below are about the application and hold regardless of which hostname it ends up on.

**Related:** `…-inventory.md` (Step 0 + DNS evidence) · `…-documentation-inventory.md` (what is and
is not documented) · `…-runbook.md` (the procedure) · `outbound-email-client-domain.md` (sender
move) · `scripts/verify-cutover.sh`

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
blocker:** `"*"` also means the app answers to any Host header. Tightening it is worth doing *after*
cutover — doing it now adds a way for go-live to fail, and host filtering runs **before** any
redirect rule, so a missing entry turns a redirect into a bare HTTP 400 that reads like a dead site.

Proposed value when it is tightened (hardening, deliberately deferred):

```json
"AllowedHosts": "database.greencirclecertified.com;dbtest.greencirclecertified.com;gccwebapp.com;www.gccwebapp.com;test.gccwebapp.com;localhost"
```

## 2. CORS — already prepared

`appsettings.Production.json:186-192` already allows the new origin:

```
"https://database.greencirclecertified.com",
"https://gccwebapp.com",
"https://www.gccwebapp.com",
"https://greencirclecertified.com",
"https://www.greencirclecertified.com"
```

Added 2026-08-24, commented in-file as defensive. It becomes load-bearing at cutover.

Two accuracy notes for whoever reads that block next:

- Its comment says *"read by Program.cs:473"*. The live reader is **`Program.cs:509-520`** — the named
  policy `ApiCorsPolicy`, applied at `Program.cs:793`.
- `Extensions/SecurityServicesExtensions.cs:181-213` registers a **default** CORS policy from the same
  config key, but nothing applies it (`UseCors()` with no name exists only in dead code — see §5).
  Harmless duplication; do not fix one and assume the other changed.

## 3. Generated URLs — follow the request host, correctly

`Controllers/Api/v1/CertificatesController.cs:1670`

```csharp
var baseUrl = $"{request.Scheme}://{request.Host}";
```

Every absolute URL the app emits is built this way, so all of it follows the incoming hostname:

| file:line | Used for |
|---|---|
| `AccountController.cs:530` | **password-reset email link** |
| `AccountController.cs:406` | signed-out callback |
| `Api/v1/CertificatesController.cs:1670` → `:1713`, `:1716` | **GS1 JSON-LD `id` and product URIs** |
| `Api/CertificateTilesApiController.cs:601` | public tile share URL |
| `Api/CertificateTileInternalApiController.cs:779` | internal tile share URL |
| `Api/CertificateTileExternalApiController.cs:874` | external tile share URL |
| `Views/Certificate/CertificateInternalView.cshtml:413` | certificate **Verify** link |
| `Views/Certificate/CertificateExternalView.cshtml:293` | certificate **Verify** link |
| `Views/Admin/GCCAPI.cshtml:51,59,246` | API docs show the caller their own base URL — self-updating |

**Nothing in this table needs editing.** That is the finding: the usually-expensive part of a
hostname change does not exist here.

**Consequence worth stating to partners:** the GS1 `id` is a URI and it *changes* with the hostname.
A partner storing `id` as a primary key sees new values after cutover. Already drafted for Amazon in
`docs/tracking/2026-08-14-amazon-host-change-note.md:33-37` — **still unsent, still carrying a literal
`[DATE]`**, and its own internal notes say it must reach Amazon *before* cutover, not with it.

`pdfDownloadUrl` is emitted **relative** (`/api/v1/certificates/{number}/download`,
`CertificatesController.cs:300`), so it is hostname-agnostic. Good.

**No background job builds a URL.** The one job (`Jobs:CertificateExpiration`, a daily expiry sweep)
generates none, and `EmailService` receives fully-built links from the controller — so there is no
need for a configured `Site:BaseUrl` today. If a notification email is ever sent from a background
job, that becomes a real gap.

## 4. Hardcoded hostnames — four, all correct as-is

| Location | Value | Verdict |
|---|---|---|
| `Api/v1/CertificatesController.cs:1734` | `AgencyURL = "https://www.greencirclecertified.com"` | **Correct** — the certifying body's marketing site, not the app. Leave. |
| `AccountController.cs:1214` | `email.EndsWith("@greencirclecertified.com")` | Email-domain rule, unrelated to web hostname. Leave. |
| `appsettings.Test.json:68` | `"BaseUrl": "https://gccwebapp.com"` | Test-harness config, not runtime link generation. Leave. |
| `Middleware/SecurityHeadersMiddleware.cs:158` | CSP `connect-src … https://api.greencirclecertified.com` | **Dead** — the hostname is NXDOMAIN (verified 2026-09-14). Harmless, but do not carry it forward as if it meant something. |

No `SiteUrl` / `AppUrl` / `PublicUrl` in any live environment file — only
`appsettings.Production.template.json` placeholders, which nothing reads.

The ~3,600 other occurrences of `gccwebapp.com` are in `Scripts/smoke-prod/` and `qc*-live-check/` —
test harnesses deliberately targeting the test host, not shipped app code.

## 5. Proxy headers — configured but **NEVER EXECUTED** [corrected]

> **This corrects the earlier reading of "`UseForwardedHeaders()` is called."** The call exists at
> `Extensions/SecurityServicesExtensions.cs:221`, but it sits inside `UseSecurityPipeline(...)`, and
> **`UseSecurityPipeline` is never invoked.** Verified by exhaustive grep (excluding build output):
> `UseSecurityPipeline` appears **exactly once** in the codebase — its own definition at `:218` — and
> `UseForwardedHeaders` appears **exactly once** — inside it. `Program.cs:528` calls
> `ConfigureSecurityServices` (service registration, a different method) and then builds the pipeline
> by hand.

So the `ForwardedHeadersOptions` at `:47-63` (`XForwardedFor | XForwardedProto`) are **inert**. The
same dead method also holds an unreached `UseHsts`, `UseHttpsRedirection` and `UseCors` — all three
are separately and correctly called from `Program.cs` (`:755`, `:789`, `:793`), so nothing is
missing; the extension is simply dead code.

**Does it matter for the cutover? Almost certainly not.** Plesk on **Windows** hosts ASP.NET Core
through IIS + ANCM **in-process** (`web.config:20`, `hostingModel="inprocess"`). No nginx sits in
front, so `Request.Scheme` and `Request.Host` already carry the real client-facing values.

**It matters the moment anything is put in front of the app** — WAF, CDN, load balancer. Two
consequences then land at once: every generated URL in §3 silently reverts to the internal host,
**and** `IpRateLimiting.RealIpHeader: X-Forwarded-For` (`appsettings.Production.json:198`) is read by
the rate limiter directly, independent of this middleware — so with no trusted proxy configured a
client can **spoof `X-Forwarded-For` to evade rate limiting today**. That deserves its own defect;
it is noted here only because a cutover is when a proxy is most likely to appear.

`Scripts/deployment/nginx-plesk-config.conf` (2025-10-10) describes a Linux/nginx front end and is
dead. It contradicts the live hosting model and should be deleted or marked.

## 6. Cookies and auth — no domain pinning, and no IdP to reconfigure [corrected]

No explicit `Cookie.Domain` is set anywhere — auth (`Program.cs:348-358`), session (`:64-68`) and
antiforgery (`:460-469`). Cookies are host-scoped, which is the safe default: a session on one
hostname will not carry to another, so users sign in again on the new host once. Deliberately do
**not** set it to the client's root domain — that would share cookies with their Squarespace site.

> **Azure AD B2C is definitively NOT in use — this resolves the earlier "confirm before go-live".**
> `appsettings.Production.json:161-172` is all `{PLACEHOLDER}` tokens, so `isAzureAdB2CConfigured` is
> false and the app runs on its own cookie authentication (`Program.cs:159-170, 249`). Independently
> recorded in `production-cutover-plan.md` §4. **There is no external IdP redirect URI to register**
> — a standard cutover step that simply does not exist here. No confirmation needed.

## 7. Data-protection keys — and the alias-vs-second-site tension

Auth cookies and antiforgery tokens are signed with the data-protection key ring
(`ApplicationName: "GCC-Production"`, `appsettings.Production.json:325`). A **domain alias** shares
the site, app pool and key ring, so existing sessions are unaffected. A **second Plesk site** gets
its own ring.

**Both models have a real cost, and they pull in opposite directions. State it rather than picking
silently:**

| | Domain alias | Second Plesk site |
|---|---|---|
| Data-protection key ring | shared — nobody is signed out | separate — users re-authenticate on the new host |
| Test gate (§8) and `robots.txt` can differ per hostname | **no** — one process, one config, one wwwroot | **yes** |
| Effort | hours | server/site build |

The key-ring argument for an alias is weaker than it first looks: §6 already establishes that
host-scoped cookies mean users sign in again on a new hostname *anyway*, which is correct behaviour.
The §8 conflict is the harder constraint, because it is functional rather than cosmetic.

**Note the project is already not applying one rule universally:** `test.gccwebapp.com` was
deliberately built as a **separate site with its own database and key ring** (CR-050), precisely so
its configuration could diverge.

## 8. The two blockers for a PUBLIC production hostname

Neither is an application-code problem, and neither appears in the runbook's step list. Both are
single-process, single-file state — so under a **domain alias** they cannot differ between the two
hostnames.

### 8.1 The test-environment gate

`Middleware/TestEnvironmentGateMiddleware.cs`, registered `Program.cs:806`, config
`appsettings.Production.json:57-68` with **`Enabled: true`**. It is a global password wall on every
non-API path, letting through only `/api/*`, `/robots.txt`, `/favicon.ico` and Google
site-verification files (`:85-88`).

That includes the deliberately public certificate directory —
`/Certificate/CertificateTileView` and `/Certificate/CertificateSingleView/{number}`, both
`[AllowAnonymous]`. On a public production hostname the gate must come off; on the test hostname it
must stay on. **The middleware is host-blind** — one flag, no per-hostname condition — so under an
alias those cannot both be true.

Two further facts: it binds its options **once in the constructor** and is a **singleton**
(`appsettings.Production.json:62-64`), so changing `Enabled` does nothing until an **app-pool
recycle**; and it does **not** protect `/api/*` by design.

### 8.2 `robots.txt`

`wwwroot/robots.txt` is, in full:

```
# Test environment — block all crawlers/indexers
User-agent: *
Disallow: /
```

Correct for a test host. **Wrong for a public production hostname**, where the certificate directory
is the product. It is one static file in one wwwroot, so the same alias constraint applies. Pair
this with deindexing the old host — see the SEO deindex playbook.

## 9. Redirect placement — the server layer is available and proven

`web.config:42-53` already contains a working IIS **URL Rewrite** rule (HTTP → HTTPS, permanent,
path preserved via `{R:1}`). So the module is installed and functioning, and a canonical-host 301 is
a server-layer rule needing **no application code** — exactly what the skill requires.

**The catch:** `web.config` is **tracked in git and republished on every deploy**. A rule added on
the server disappears at the next publish, silently — the same mechanism that loses SMTP environment
variables (`production-cutover-plan.md:107-112`). **The rule must go in the repo's `web.config` and
ship with the bundle.** Rollback is then: edit on the server (immediate), *and* re-apply to the repo
or the next deploy restores it.

No canonical-host redirect exists today: no rewrite rule other than HTTP→HTTPS, no
`Site:CanonicalHost` key, no host-checking middleware. Correct starting state — nothing to unwind.

## 10. Old-site deep links — OUT OF SCOPE today, but the map already exists

Retiring `db.greencirclecertified.com` (Vercel, the live legacy database) is a **separate cutover**
and is explicitly not part of this one. Recorded here so the work is not re-derived later.

Old URL shapes, read from the site's own JS chunks in August 2026
(`Scripts/data-ops/2026-08-12-greencircle-public-db-scrape.py:28-33, :123-125`):

| Old URL | New equivalent | Mappable? |
|---|---|---|
| `/certificate` | `/Certificate/CertificateTileView` | yes — static rule |
| `/api/certificates/{id}/download` — keyed by the **old integer id** | `/Certificate/CertificateSingleView/{number}` | **yes, via lookup** |
| `/api/certificates` — unauthenticated JSON of every record | `/api/v1/certificates` is **API-key gated** | **no** — see below |
| `/api/trpc/certificate.getAll?…` | none | no |

**The mapping table already exists.** `C:\Users\swest\GCCPortalPull\manifest.csv` (2026-08-13 portal
pull) profiled for this audit: **6,000 rows, 6,000 distinct integer `record_id`, 5,822 distinct
certificate numbers, zero blanks.** The `record_id → certificate_number` direction is **total and
unambiguous** — exactly the direction a redirect needs. Use the **portal** manifest, not the
public-only mirror (1,021 rows).

Before generating the map, set-compare the 5,822 numbers against live certificate numbers — equal
counts are not the same rows.

**Do not redirect `/api/*` from the old host.** It would convert a working unauthenticated
integration into a 401, which is worse than a clean 404 with a message. Whether anyone consumes it
is a client question, answerable only from the old site's access logs — which have not been
obtained, so deep-link coverage is currently inferred from what the site *exposes*, not what people
*use*.

---

## Items deliberately NOT changed

- `AllowedHosts` left at `"*"` — tightening is post-cutover hardening, not a go-live step (§1).
- Sender address left at `admin@gccwebapp.com` — now tracked separately in
  `outbound-email-client-domain.md`. The client domain enforces **DMARC `p=quarantine`**, so SPF
  *and* DKIM are mandatory, and a misconfigured sender lands in junk while the app reports success.
- No canonical redirect added in application code — redirects belong at the host layer so a rollback
  is a config flip, not a redeploy (§9).
- The dead `UseSecurityPipeline` method left in place — removing it is a separate tidy-up with its
  own defect, and it changes no behaviour today (§5).

## Open questions

1. **Does the application take over `database.` at go-live, or get a different hostname?** The
   Integration Guide currently promises `database.`; if that changes, the guide changes with it.
2. **Who holds the HostMonster login?** The hard blocker on any DNS change in that zone.
3. **Alias or second site?** §7 — it decides whether §8 is solvable at all.
4. **Is `db.greencirclecertified.com` staying?** If it is also being retired, §10 becomes real work.
5. **Does anything consume the old unauthenticated `/api/certificates`?** (§10)
6. **Is anything in front of the app server** — WAF, CDN, load balancer? If yes, §5 stops being
   theoretical and the `X-Forwarded-For` spoofing note needs its own defect.
7. **TLS renewal owner** — an expired certificate takes the Amazon integration down, not just the site.
8. **Do the ~3,600 harness references to `gccwebapp.com` follow anything?** They should move to
   `test.gccwebapp.com` (CR-050), not to a production hostname.
