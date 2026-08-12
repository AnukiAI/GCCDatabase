# Email draft — production hosting request (September go-live)

**Drafted:** 2026-08-12 · **To:** Anuki systems administration · **From:** Shawn West
**Purpose:** stand up a **new production environment** for the GCC Database application.
`gccwebapp.com` remains the **test** environment.

> Fill in before sending: `[GO-LIVE DATE]`, `[ENV-READY DATE]`, `[PROD HOSTNAME]`, distribution list.
> Everything else is measured from the repo and the current deployed configuration.

---

## Email body (paste-ready)

**Subject:** GCC Database — production hosting build request (needed by [ENV-READY DATE], go-live [GO-LIVE DATE])

Team,

The GCC Database application goes live in September. We need a **new, separate production
environment** built and handed over by **[ENV-READY DATE]** — two weeks ahead of go-live, so we can
run a pilot against real infrastructure (SMTP relay, TLS, Duo) rather than discovering problems on
cutover day.

Note this is a **new environment**, not a promotion of the current one. `gccwebapp.com` stays as the
test environment. Today test and production are the same box **and the same database** — the test
site writes to live client data. Separating them is the single most important outcome of this build.

Below is what the application needs. Where I've given a number it's measured from the code or the
current deployment, not estimated.

### 1. Application server

| Item | Requirement | Note |
|---|---|---|
| OS | Windows Server 2019 or 2022 | Windows is required — see hosting model below |
| Web server | IIS with **ASP.NET Core Module V2** | App runs `hostingModel="inprocess"` |
| Runtime | **.NET 8 Hosting Bundle** (ASP.NET Core 8.0, x64) | Target framework `net8.0`, published `win-x64` |
| Sizing | 4 vCPU / 8 GB RAM to start | Low concurrency; a few dozen named users plus partner API traffic |
| OS/app volume | 100 GB | |
| Data volume | **Separate volume, 250 GB** | See §3 — this is not optional |
| App pool | Always-running, **no idle timeout**, no scheduled recycle during business hours | In-process hosting means a recycle is a cold start and drops in-flight requests |
| Max request size | IIS `maxAllowedContentLength` **104,857,600** (100 MB) | Certificate PDF uploads; already set in `web.config` |

We do **not** use Docker. It was evaluated at the start of the project and rejected; the
`Scripts/docker/` files in the repo are dead and should be ignored.

### 2. Database

| Item | Requirement |
|---|---|
| Engine | **SQL Server** (2019 or later; Standard is sufficient) |
| Database | A **new, empty production database**, separate from the one behind `gccwebapp.com` today |
| Auth | SQL login for the app, least-privilege (`db_datareader` + `db_datawriter` + EXECUTE); **not** `sa` |
| Connectivity | App server → SQL over a private/internal path; SQL **must not** be reachable from the public internet |
| TLS | Encrypted connection with a **valid certificate** — we should not be relying on `TrustServerCertificate=true` in production |
| Collation / timezone | Match the current production database exactly |
| Migration runs | The app does **not** self-migrate in production by policy. Schema changes are applied as SQL by a DBA as part of each deploy — please confirm who owns that and how we request it |

Data migration from the current database to the new one is ours (Anuki) to run and validate; we just
need the empty target and the credentials.

### 3. File storage — the item most likely to be missed

The application stores uploaded certificate PDFs and generated thumbnails on disk. **Today they live
inside the application folder**, which means a deploy can overwrite or remove them. We have already
lost a full set of production PDFs once (June 2026) and had to recover from backup.

For the new environment:

- Provision the uploads directory on the **separate data volume**, e.g. `D:\GCCData\uploads`, **outside
  the IIS site folder**. The app supports this via a configuration setting (`FileStorage:StoragePath`) —
  we'll set it, we just need the path to exist and be writable.
- Grant the app pool identity modify rights on that path only.
- The `DataProtectionKeys` folder must also live outside the deploy folder. Today it sits inside it,
  so every deploy invalidates all sessions and logs everyone out. Put it on the data volume too.
- Also writable: the app's `logs\` directory (IIS stdout log + Serilog file sink).

**Sizing:** the current store is small (tens of MB), but it grows one PDF per certificate and never
shrinks. 250 GB gives us years of headroom. Please confirm the volume can be expanded without an
outage.

### 4. Networking, DNS and TLS

- Public hostname: **[PROD HOSTNAME]** — needs DNS plus a valid TLS certificate with **auto-renewal**
  (an expired cert takes down the Amazon partner API integration, not just the website).
- HTTP → HTTPS redirect is handled by the application's `web.config`; no additional rewrite rules needed.
- **Inbound:** 443 only. 80 open solely for the redirect and certificate validation.
- **Outbound HTTPS required** to: the SMTP relay (§5), Duo's API hostname (§6), and Microsoft Graph
  (`graph.microsoft.com`) if directory integration is turned on.
- The application does its own IP-based rate limiting and reads the client IP from `X-Forwarded-For`.
  **If anything sits in front of the app server** (load balancer, WAF, reverse proxy), tell me — the
  configuration has to change, and getting it wrong either breaks rate limiting or lets clients spoof
  their IP.
- Please confirm whether outbound email or any egress is filtered.

### 5. SMTP relay — this is a cutover blocker

Two-factor authentication is a client requirement for go-live, and **password reset and 2FA codes are
currently not delivered at all** in production: the SMTP settings are empty, so the app silently falls
back to a mock mail service that logs codes instead of sending them. If we enable 2FA in that state,
**every user including every admin is locked out with no self-service recovery.**

We need, on the production environment:

- A real SMTP relay: host, port, TLS setting, username, password, and a from-address on the client's
  domain (`noreply@…`), with SPF/DKIM aligned so the mail isn't filtered as spam.
- Please confirm the relay accepts mail from the production server's IP.

**Where the credentials go matters.** On this hosting model, application environment variables live in
`web.config` — which is overwritten by every deploy. Anything we set there is silently lost on the next
publish, and the failure mode is quiet (mail just stops sending). I'd like your recommendation on a
durable place to hold these: machine-level environment variables, a Plesk-level setting that survives
publish, or a secrets store. **Please don't plan on them living in a file in the repo** — we already
have an open finding for a plaintext credential committed to source control and I don't want a second.

### 6. Duo

The client requires Duo MFA at go-live and already has a Duo tenant. We'll be integrating at the
application level (Duo Universal Prompt over OIDC) — there's no separate identity provider in play.
From you I need:

- outbound HTTPS to the Duo API hostname allowed from the production server;
- confirmation of who requests the Duo **application integration keys** (client id, client secret, API
  hostname) from the client's IT — and a **second, separate Duo application** for the pilot environment,
  so pilot enrolments can be discarded without touching production.

### 7. Backups and recovery

| What | Requirement |
|---|---|
| Database | Nightly full + transaction log backups; **off-server**; 30-day retention minimum |
| Uploads volume | Nightly; 30-day retention. The database rows are useless without the matching PDFs |
| Restore test | One documented restore test **before go-live**, not after |
| RPO / RTO | Proposing 24h RPO / 4h RTO — tell me if that's not achievable so I can take it back to the client |

Please also confirm the deploy process takes a snapshot or that we have a documented rollback, since
we publish the whole site folder on each release.

### 8. Monitoring, access and handover

- Uptime monitoring on the public hostname and on the partner API health endpoint, alerting to a
  monitored address.
- Disk-space alerting on the data volume — the uploads directory grows monotonically.
- Windows Event Log and the app's file logs retained and readable by us.
- Accounts: named admin access for our deploy engineer, plus a documented break-glass account. At
  cutover, **someone with server access needs to be reachable** — if 2FA delivery fails, the rollback is
  a configuration change on the server and the outage lasts exactly as long as it takes to find that
  person.
- Patching window and reboot policy, so we can schedule around it.

### 9. What I need back, and when

1. **Confirmation you can meet this by [ENV-READY DATE]** — or where you can't, so I can adjust the
   plan with the client.
2. Anything above that changes cost, so I can price it before committing.
3. Whether SMTP and Duo are things you provision or things I request from the client's IT.
4. The item in §5 about where credentials can durably live — I'd rather follow your recommendation
   than invent something.

Happy to walk through any of this. The two things I'd flag as genuinely time-critical are the **SMTP
relay** (§5) and the **separate database** (§2) — everything else can be tuned after go-live, but those
two block the pilot, and the pilot is what keeps go-live from being the first real test.

Thanks,
Shawn

---

## Internal notes (do not send)

Sources for the claims above, in case anything is challenged:

| Claim | Evidence |
|---|---|
| .NET 8, IIS in-process, AspNetCoreModuleV2 | `GCCWebApp.csproj` (`net8.0`), `web.config:6-12` |
| 100 MB upload limit | `web.config:50` |
| Docker rejected; `Scripts/docker/` dead | `[[plesk-test-deploy-uses-production-env]]` (confirmed 2026-07-16) |
| Test env runs against the **production** database | `[[plesk-test-deploy-uses-production-env]]` — `appsettings.Test.json` is placeholders only |
| `TrustServerCertificate=true` in use | `appsettings.json:54` |
| Uploads default to `wwwroot/uploads` inside the deploy folder | No `FileStorage:StoragePath` key set in `appsettings.json` or `appsettings.Production.json`; resolver falls back to the wwwroot path (DEF-286) |
| PDFs lost once already | `[[incident_prod_all_pdfs_missing_2026-06-15]]` — 15/15 stored PDFs 404 |
| DataProtection keys inside the deploy folder | `Program.cs:57-61` — `Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys")` |
| SMTP empty → `MockEmailService` → total 2FA lockout | `appsettings.Production.json:92-99` + `Program.cs:494-516`; full analysis in `production-cutover-plan.md` §1 |
| Env vars live in `web.config` and are lost on publish | `production-cutover-plan.md` §2a |
| Plaintext credential already an open finding | DEF-263 / `[[security-plaintext-gmail-credential]]` |
| `X-Forwarded-For` as the rate-limit client IP | `appsettings.Production.json:127` |
| Duo = app-level OIDC; no IdP in play (B2C not configured) | `production-cutover-plan.md` §4 |
| Prod does not self-migrate (policy) | Pre-deploy checklist Gate 7 / DEF-239. **Note:** `appsettings.Production.json` has `Database:AutoMigrateOnStartup: true`, which contradicts the policy — worth resolving separately before the new env is built, and worth **not** carrying forward into production config |

**Open decisions this email does not settle:**
- SMS as a 2FA delivery option — configure Twilio or hide it (`production-cutover-plan.md` §1a). Left
  out of this email deliberately; it's a product decision, not a hosting one.
- Which database the pilot/subdomain points at (restored copy recommended).
- Search-engine exclusion on the test env once production takes the primary hostname.
