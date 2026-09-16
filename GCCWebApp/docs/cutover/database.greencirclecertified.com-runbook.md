# Cutover runbook — `database.greencirclecertified.com`

> ### ⚠️ Read this first — scope changed 2026-09-14
>
> Shawn: *"the test link is dbtest.greencirclecertified.com because database and db are both being
> used by the existing database."*
>
> - **`dbtest.greencirclecertified.com` already exists and works.** It resolves to `64.27.52.66`,
>   its TLS certificate explicitly lists the name (SAN alongside `gccwebapp.com`), and
>   `/api/v1/certificates` returns 401 to an anonymous caller. `verify-cutover.sh` → **4/4 pass**.
>   **No work is needed for the test host.** It is what partners integrate against today.
> - **`database.greencirclecertified.com` is NOT free.** It belongs to the existing certificate
>   database — so the steps below **cannot run until the legacy database is retired or renamed**,
>   and that is a client decision, not a scheduling one.
> - **Open question before any of this executes:** does the application take over
>   `database.` at go-live, or does it get a different hostname? The guide currently promises
>   `database.`; if that changes, the guide changes with it.
>
> Everything below stays valid as the *procedure* — only its trigger date is unknown.

**Status: PREPARED, BLOCKED ON THE LEGACY DATABASE BEING RETIRED (2026-09-14).** Fill the timestamp column as you go.
**Inventory:** `database.greencirclecertified.com-inventory.md` · **Audit:** `…-audit.md`
**Verify:** `scripts/verify-cutover.sh`

**One-line summary of the change:** move `database.greencirclecertified.com` from an nginx redirect
shim at `70.32.82.212` to the GCC application at `64.27.52.66` (Plesk / AccuWebHosting), as a
**domain alias** of the existing `gccwebapp.com` site.

---

## Why this is lower-risk than it sounds

- The hostname currently serves **no application** — only a 302 that discards the path.
- The box it points at (`70.32.82.212`) is **never touched**, so rollback is one DNS record.
- The app needs **no code change** — `AllowedHosts` is `*`, CORS already lists the origin, and
  partner URLs are generated from the request host.
- `db.greencirclecertified.com` — the live legacy database — is a **different hostname on a different
  host** and is not affected.

## Hard blocker before anything else

**Who can edit DNS at HostMonster?** Everything below depends on changing one A record in that zone.
If the client holds the login, steps 1 and 7 are theirs to perform and the schedule is theirs to set.
Confirm this before promising a date.

---

## Pre-go-live — zero production risk

These can all be done now. Nothing a partner or user touches changes until step 1 propagates, and
step 1 only moves a redirect that was already lossy.

| # | Step | Where | Owner | Done |
|---|---|---|---|---|
| 1 | Change the **A record** `database` → **`64.27.52.66`**. Delete the existing `70.32.82.212` A record — do not layer a second one. **TTL 300.** Do **not** let the panel create a "hosted subdomain" or offer SSL; this is a bare DNS record pointing away from HostMonster. | HostMonster zone editor (cPanel-style) | Whoever holds DNS | ☐ |
| 2 | Wait for propagation, then confirm: `nslookup database.greencirclecertified.com` → `64.27.52.66` | anywhere | — | ☐ |
| 3 | Add `database.greencirclecertified.com` as a **Domain Alias** of the existing `gccwebapp.com` site. **Websites & Domains → gccwebapp.com → Add Domain Alias.** **Uncheck "Synchronise DNS zone with primary domain"** (DNS lives at HostMonster, not Plesk). **Do NOT create a new site** — a second site gets its own data-protection key ring and breaks auth cookies and antiforgery tokens across the two hostnames. | Plesk | Shawn | ☐ |
| 4 | Issue **Let's Encrypt** for the alias (Plesk → SSL/TLS Certificates → Let's Encrypt, tick the alias). This only succeeds **after** step 2 resolves to this server. | Plesk | Shawn | ☐ |
| 5 | Confirm the app answers on the new hostname over TLS, unauthenticated: `/api/v1/certificates` must return **401**, not 200 and not an HTML page. | `scripts/verify-cutover.sh` | Shawn | ☐ |
| 6 | Confirm with a **real Amazon API key** that `/api/v1/certificates` returns JSON on the new host, and that the GS1 `id` values now read `https://database.greencirclecertified.com/...`. Per memory: use an **unauthenticated session** — an admin cookie makes the partner middleware stand aside and the 401 reads like a bad key. | curl / script | Shawn | ☐ |

## Go-live

| # | Step | Owner | Done |
|---|---|---|---|
| 7 | Announce to Amazon (and any other partner already integrated) that the base URL is live, **and that the GS1 `id` URI changes with it** — see the audit, §3. Give them a window before you retire anything. | GCC | ☐ |
| 8 | Update the Integration Guide: remove the "available at go-live" marker and the NOT-YET-LIVE note from the how-to box. Both are already in place in `GCC_Partner_API_Integration_Guide_TEMPLATE_GCC_3.docx`. | Anuki | ☐ |
| 9 | **Optional, and only on the client's say-so:** canonical redirect `gccwebapp.com` → `database.greencirclecertified.com` at the **IIS/Plesk layer**, path and query preserved. Not in app code. Hold this until partners have migrated — `gccwebapp.com` is still the working test host. | Shawn | ☐ |

## Post-go-live

| # | Step | Done |
|---|---|---|
| 10 | Watch IIS logs for 404s on the new hostname — unmapped paths show up here first | ☐ |
| 11 | Raise TTL back to 3600+ once stable | ☐ |
| 12 | Tighten `AllowedHosts` from `"*"` to an explicit list (hardening, deliberately deferred — see audit §1) | ☐ |
| 13 | Record the issue in `docs/tracking/partner-api-guide-issuance-log.md` | ☐ |
| 14 | Fill in the timestamps above and change this file's status to EXECUTED | ☐ |

---

## Rollback

**Revert the A record: `database` → `70.32.82.212`, TTL 300.**

That is the whole procedure. The nginx shim is never reconfigured, so it resumes the moment DNS
propagates. The Plesk alias and its certificate can be left in place — they do nothing while DNS
points elsewhere, and leaving them means a re-attempt skips steps 3 and 4.

**Do not** add redirects in application code. A rollback must be a config flip, not a redeploy.

---

## Explicitly out of scope today

- **Retiring `db.greencirclecertified.com`** (the Vercel legacy database, 1,021 public certificates,
  the signed-in portal, S3-backed PDFs). Separate cutover, real deep-link mapping work, and the
  client's vendor conversation. Do not bundle it in.
- **Moving the email sender** to the client domain — now written up separately as
  `outbound-email-client-domain.md`. It is independent of the web-hostname cutover and can run before
  or after it. Headline: the client domain enforces **DMARC `p=quarantine`**, so SPF *and* DKIM are
  mandatory rather than optional, and a misconfigured sender lands in junk while the app reports
  success.
- **Tightening `AllowedHosts`** — deferred to step 12 on purpose.
