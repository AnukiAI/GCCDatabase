# Runbook — stand up `test.gccwebapp.com` as a separate test site

**Opened:** 2026-09-14 · **Owner:** Anuki Consulting
**Status: EXECUTED 2026-09-21 — steps 1-5 and 7 done and externally verified. Items 1-8 in CR-050's
"Still outstanding" table remain, and one of them is live.**
**CR:** CR-050 · **Shape:** two independent Plesk sites on one AccuWeb platform

> ## ✅ What happened on 2026-09-21
>
> Shawn stood the site up. Verified externally the same day:
>
> | Step | Result |
> |---|---|
> | 1. DNS `test A 64.27.52.66` | ✅ resolves |
> | 2. Plesk subdomain, own app pool | ✅ |
> | 3. Let's Encrypt | ✅ chain validates |
> | 4. Deploy | ✅ the app answers |
> | 5. Overlay | ✅ **banner reads `DB catalog : gccwebap_test`** — the non-negotiable check, passed |
> | 6. Uploads | ⬜ unverified — sign in and open a certificate |
> | 7. Verify | ✅ gate active, `robots.txt` correct, handler mapped, API 401 anonymous |
>
> **Still outstanding, and #1 is live right now:** rotate or disable the partner API keys inside
> `gccwebap_test`; confirm `Jwt:Key`, `DataProtection:ApplicationName` and `SmtpSettings:FromName`
> are the test values and not production's; delete the copied `DataProtectionKeys/`; run the
> write-isolation check; set `AutoMigrateOnStartup` true here only; repoint the smoke harness.
> Full table with reasons: **CR-050 in `docs/tracking/change-request-register.md`.**
>
> **This site is now the PERMANENT test environment** (2026-09-21 go-live plan), not a convenience.
> `gccwebapp.com` becomes production, so **CR-050 must be complete before the test gate is switched
> off there** — otherwise there is no gated test environment anywhere.
> See `go-live-plan-2026-09-21.md`.

## What was decided (Shawn, 2026-09-14)

| Decision | Value |
|---|---|
| Site model | **Two separate configurations** on the same hosting platform — separate folders, separate app pools, separate config |
| Database | **Separate DB now** — `gccwebap_test`, a copy of the existing database, same SQL host |
| `gccwebapp.com` | **Leave alone.** No redirect, no change, decide later |

**Already done by Shawn:** the database copy exists and is named **`gccwebap_test`**.

## The starting position is unusually clean

- `test.gccwebapp.com` is **NXDOMAIN** — no existing record, and **no wildcard** on the zone
  (checked 2026-09-14). Nothing has to be removed first. Contrast
  `database.greencirclecertified.com`, which already points at a stranger's server.
- The `gccwebapp.com` zone is on **AccuWeb's own nameservers**
  (`ns1/ns2.accu22.accuwebhosting.com`), so **we control it end to end.** No client involvement.
- App server: **`64.27.52.66`** (PTR `ACCU22.accuwebhosting.com`).
- SQL host: **`68.71.129.120,1533`**.

---

## The one thing that can go badly wrong

**Both sites deploy from the same zip, and that zip contains `appsettings.Production.json` with the
PRODUCTION connection string inside it.** Extracting the bundle into the test site's folder writes
the production connection string into the test site — every deploy, forever.

The only thing steering the test site away from live client data is its own
`appsettings.Secrets.json`, which is deliberately **absent** from the zip (`Scripts/deploy.ps1`
aborts if it reaches staging) and is registered **last** (`Program.cs:41`), so it wins. Because
extracting a zip adds and overwrites but never deletes, a file absent from the zip survives.

**If that overlay is missing, malformed, or the key is misspelled, the test site silently reads and
writes real client records.** Nothing errors. Every page loads. The data looks right — because it
*is* the real data. Same silent-fallback shape as DEF-263.

**Mitigation added with this work** (`Program.cs`, just after the provider line): the startup banner
now names the live database. Verified locally 2026-09-14 — prints server, catalog and user, never
the password:

```
Using SQL Server database provider
  DB server  : 68.71.129.120,1533
  DB catalog : gccwebap_test        <-- MUST be gccwebap_test on the test site
  DB user    : <login>
```

**This is the check. Do it after every deploy and every app-pool recycle.** If the catalog reads
`gccwebap_certificates` on the test site, stop and fix the overlay before touching anything else.

---

## Steps

### 1. DNS — ours to make

Add at AccuWeb, in the `gccwebapp.com` zone:

```
test    A    64.27.52.66    TTL 300
```

TTL 300 while standing it up; raise to 3600+ once stable. Do **not** request SSL from the DNS side —
TLS lives on the server that answers.

Verify before moving on: `Resolve-DnsName test.gccwebapp.com` returns `64.27.52.66`.

### 2. Plesk — a separate site, NOT a domain alias

**Add it as a subdomain / separate site.** A domain alias would put both hostnames on the same site,
app pool, folder and config — which is what this exercise exists to avoid, and it would make the
separate database impossible.

Confirm after creation:

- its own document root (Plesk's default `.../gccwebapp.com/test.gccwebapp.com/` is fine — it is a
  separate folder, which is what was asked for);
- its own **application pool**, set to **Always Running, no idle timeout** — in-process hosting
  means a recycle is a cold start;
- .NET 8 Hosting Bundle is already on the box, since `gccwebapp.com` runs on it.

### 3. TLS

Issue **Let's Encrypt for `test.gccwebapp.com`** once step 1 resolves. Issuance validates over HTTP,
so it cannot succeed before DNS points here — sequence matters. Confirm auto-renewal is on.

### 4. Deploy the app

Build as normal (`pwsh -File Scripts/deploy.ps1`) and extract **the same zip** into the test site's
folder. The bundle is host-agnostic — nothing in it names a hostname — so one build serves both
sites.

Create `logs\` in the site root if absent (`web.config` writes to `.\logs\stdout`).

### 5. The overlay — the step that makes this a test site

Copy **`appsettings.Secrets.test-site.template.json`** (repo root, added with this work) to
`appsettings.Secrets.json` in the **test site's** folder, alongside `GCCWebApp.dll`. Fill in real
values and delete every `_README` / `_comment` key.

It sets four things; only the first is strictly required:

| Key | Value | Why |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Database=gccwebap_test` | **The whole point.** If the existing login was granted access to the copy, this differs from production only in the `Database=` value. A login scoped to `gccwebap_test` is better — then a mistake fails to *connect* rather than succeeding against production. |
| `Jwt:Key` | a fresh base64 key | The shipped config gives both sites the **same signing key**, so without this a token minted on test validates on `gccwebapp.com`. The environments are not isolated at the token layer until this is set. |
| `DataProtection:ApplicationName` | `GCC-Test` | Safe to set **here only**. Changing it on an existing site signs everyone out; this site is new and has no outstanding cookies. Guarantees the key rings can never merge. |
| `SmtpSettings:FromName` | `GCC Database - TEST` | `admin@gccwebapp.com` is the only mailbox that exists, so both sites send as the same address. Without this, a reset email from test is indistinguishable from a real one in an inbox. |

**Recycle the application pool**, then do the banner check above.

### 6. Uploads — the test site starts empty, and the copied DB does not know that

`IUploadPathResolver` resolves to `ContentRootPath/App_Data/uploads` unless `FileStorage:StoragePath`
is set. That is **per-site**, so the new site gets an **empty** uploads folder while `gccwebap_test`
carries rows referencing roughly 5,700 certificate PDFs. Every PDF and thumbnail 404s until this is
dealt with.

| Option | Verdict |
|---|---|
| **Copy `App_Data/uploads` from the production site into the test site** | **Recommended.** One-time cost, on the order of **3.5-4 GB**. Preserves isolation and gives realistic data. |
| Point `FileStorage:StoragePath` at the production site's uploads folder | **Do not.** Reads would work, but a test upload, replace or delete writes into the production store — it reintroduces exactly the coupling this exercise removes. |
| Accept an empty store | Fine only if you are testing fresh upload flows and nothing else. Every existing certificate shows a missing document. |

### 7. Verify

- Startup banner reads **`DB catalog : gccwebap_test`**. Non-negotiable.
- The test gate is inherited (`Enabled: true` in `appsettings.Production.json`), so the site should
  sit behind the password wall. Confirm it does — it is now the only thing keeping a **second copy
  of client data** off the open internet.
- `robots.txt` ships as `Disallow: /`, which is **correct** here. Leave it.
- `/appsettings.Production.json` and `/GCCWebApp.dll` must **not** return 200; `/robots.txt` must
  (`web.config:72-77`). A deploy is exactly when handler mapping breaks.
- Log in with a test account, open a certificate, confirm the PDF renders (step 6).
- **Write something on the test site, then confirm it did not appear on `gccwebapp.com`.** Two sites
  that agree prove nothing; a write that stays put is the only real isolation evidence.

---

## Decide, then record

> **Superseded 2026-09-21.** Items 1 and 2 below are now decided: **rotate/disable the partner
> keys** (the gate skips `/api/*`, so this is live exposure) and **set `AutoMigrateOnStartup`
> true in the test overlay only**. Item 3 is promoted to pre-go-live work. The current list,
> with state per item, is the **"Still outstanding" table in CR-050**.

1. **Partner API keys came across in the copy.** A live Amazon key will authenticate against
   `test.gccwebapp.com` and return data. The gate does **not** cover this — it skips `/api/*` by
   design. Decide whether to rotate or disable the partner keys inside `gccwebap_test`.
2. **`Database:AutoMigrateOnStartup` is `false`.** It was set false because the test site ran
   against production data. That reason is gone for *this* site. Setting it `true` in the test
   overlay keeps the test schema in lockstep with deployed code and removes a class of "forgot
   Gate 7" outages. Leave it false on `gccwebapp.com`.
3. **Where the smoke harness points.** Roughly 3,600 references to `gccwebapp.com` across
   `Scripts/smoke-prod/` and `qc*-live-check/`. They should move to `test.gccwebapp.com`, but that
   is a separate mechanical change, not part of standing the site up.
4. **How `gccwebap_test` gets refreshed** from production, and by whom.

## Historical note — the original closing paragraph

Until 2026-09-21 this runbook ended with *"No DNS record created, no Plesk change made, no deploy
run, nothing executed against any server."* That is no longer true: everything above was executed on
2026-09-21. Kept so the change of state is visible rather than silently overwritten.

---

## Troubleshooting — what actually went wrong on 2026-09-21

### `403 Forbidden` on `/`, `404` on everything else — this is NOT `web.config`

**Symptom.** Root returns `403 - Forbidden: Access is denied.`, every other path returns
`404 - File or directory not found`, both as plain IIS error pages with
`Server: Microsoft-IIS/10.0` and no sign of Kestrel.

**Cause: document-root mismatch.** IIS is serving a folder the published files are not in — Plesk's
subdomain root is typically `...\vhosts\gccwebapp.com\test.gccwebapp.com\httpdocs`, and the easy slip
is extracting one level up into `...\test.gccwebapp.com\`. An empty folder with no default document
is IIS 403.14; everything under it is a genuine 404.

**The discriminator — one request tells you which fault it is.** Ask for a path that would exist
**only if the publish folder were the document root**:

```
/GCCWebApp.deps.json      /wwwroot/css/main.css      /GCCWebApp.dll
```

| Response | Meaning | Action |
|---|---|---|
| **404** | Wrong folder. The files are elsewhere | Fix the document root, or move the files into it |
| **200** | Files are right, **the ASP.NET Core Module is disengaged** | **Stop the site.** Config and binaries are being served raw |
| **500.19** | A real `web.config` / module fault | Read the substatus |

A clean 403-then-404 pair is never a broken `web.config` — a broken one gives **500.19**.

**Do not "fix" it by adding `fileExtensions` / `hiddenSegments` to `web.config`.** This Plesk box
rejects them and the site returns an empty HTTP 500 on every path. Proven twice — see the comment at
`web.config:55-78` (DEF-363).

### Confirming the handler is healthy, from outside

Four requests, and the pattern matters more than any one of them:

| Path | Healthy on a gated site | Healthy on an ungated site |
|---|---|---|
| `/robots.txt` | **200** (the gate allow-lists it) | **200** |
| `/appsettings.Production.json` | **302 → `/__gate`** | **404** |
| `/GCCWebApp.dll` | **302 → `/__gate`** | **404** |
| `/api/v1/certificates` | **401** — the gate skips `/api/*` by design | **401** |

If static assets work while binaries and config also return content, that is inverted — the handler
is unmapped and secrets are public. Stop the site.

### The copy-from-production trap

Copying the live site's `httpdocs` to seed the new site brings **production's
`appsettings.Secrets.json`** with it. That file is registered last (`Program.cs:41`) so it wins over
everything, and it is what decides which database the site talks to. It also survives every future
deploy, because extracting a zip adds and overwrites but never deletes.

Left in place, the "test" site is production with a different hostname — every page loads and the
data looks right, because it *is* the real data.

Three other things ride along in that copy and each needs a decision:

| Rode along | Do |
|---|---|
| `DataProtectionKeys/` | **Delete it.** `ApplicationName: GCC-Test` isolates the rings anyway, but leave no ambiguity about which is in use |
| `App_Data/uploads` | Keep it — it satisfies step 6 for free (~3.5-4 GB). Confirm by opening a certificate |
| `logs/` | Clear it, so the first startup banner is the only thing in there |

**The banner is the control, and on 2026-09-21 it is what cleared this** — it read `gccwebap_test`,
which is the only evidence that distinguishes a correctly-pointed test site from production wearing
a different hostname.
