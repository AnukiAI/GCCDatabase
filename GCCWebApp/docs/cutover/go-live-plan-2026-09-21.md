# GCC Database — Go-Live Plan

**Prepared:** 2026-09-21 · **Prepared by:** Anuki Consulting
**Status:** PLAN — not executed. Dates are proposed, not committed.
**Proposed go-live:** **Tuesday 2026-10-13** (fallback Tuesday 2026-10-20)

**Companion docs:** `database.greencirclecertified.com-inventory.md` (who owns what) ·
`database.greencirclecertified.com-audit.md` (what the app assumes about its hostname) ·
`database.greencirclecertified.com-runbook.md` (the DNS/Plesk procedure) ·
`outbound-email-client-domain.md` (sender move) · `scripts/verify-cutover.sh`

---

## 0. The decision that shapes everything (Shawn, 2026-09-21)

> *"Retire/rename `database` and `db` subdomains to point to gccwebapp. Make the gccwebapp domain
> always display as `db.greencircle…`"*

**`db.greencirclecertified.com` becomes the single canonical public hostname for the new system.**
Everything else redirects to it.

| Hostname | Today (verified 2026-09-21) | After go-live |
|---|---|---|
| `db.greencirclecertified.com` | **CNAME → `cname.vercel-dns.com`** — **Vercel, the live legacy database** (HTTP 200) | `64.27.52.66` **A** — **the new app. Canonical.** |
| `database.greencirclecertified.com` | `70.32.82.212` — nginx shim, 302s to `db.` | `64.27.52.66` — alias, **301 → `db.`** |
| `gccwebapp.com` | `64.27.52.66` — the app, behind the test gate (302 `/__gate`) | `64.27.52.66` — **301 → `db.`** |
| `dbtest.greencirclecertified.com` | `64.27.52.66` — what Amazon integrates against today | unchanged — stays a partner test host |
| `test.gccwebapp.com` | `64.27.52.66` — **live, gated, running on `gccwebap_test`** (stood up 2026-09-21) | the gated test environment (CR-050) |

### What this decision changes versus the existing audit

The audit (§10) recorded retiring the legacy `db.` site as **out of scope**. Shawn's answer puts the
**hostname** in scope while leaving the legacy **system's** decommissioning as Phase 2. That is a
workable split, but one consequence must be stated plainly:

> ⚠️ **The moment `db.` DNS moves, every existing link to the legacy database stops working unless
> we ship a redirect map with it.** There is no partial version of this — DNS is all-or-nothing per
> hostname. The legacy site has ~1,021 public certificates and its URLs are keyed by an **integer
> record id**, not a certificate number.

**The map already exists and is complete.** `C:\Users\swest\GCCPortalPull\manifest.csv` (2026-08-13
portal pull): 6,000 rows, 6,000 distinct integer `record_id`, 5,822 distinct certificate numbers,
zero blanks. The `record_id → certificate_number` direction — the one a redirect needs — is total and
unambiguous. Building the rewrite map is **work, not research**: ~0.5–1 day including the
set-comparison against live certificate numbers in both directions.

### It also resolves the hardest open question in the audit

Audit §7/§8 could not decide **domain alias vs. second site**, because the test-password gate and
`robots.txt` are single-file, single-process state and cannot differ between two hostnames on one
site. Shawn's model resolves it:

- **`gccwebapp.com`'s existing Plesk site becomes PRODUCTION** — gate **off**, `robots.txt` open,
  `db.` and `database.` added as aliases of it.
- **`test.gccwebapp.com` (CR-050) becomes the test environment** — its own site, own database
  (`gccwebap_test`), own key ring, gate **on**, `robots.txt` Disallow.

**Therefore CR-050 must be finished before the gate comes off.** If it is not, turning the gate off
leaves the project with no gated test environment at all. This is the single most under-appreciated
dependency in the plan.

---

## 1. What is remaining

Five workstreams. Only three are on the critical path.

### A. Application — finish and ship one final build · **Anuki · ~3 days**

Per the standing rule, there is **one** delivery, not a series ([[project_single_final_build_only]]).

| # | Item | Status | Effort | On critical path |
|---|---|---|---|---|
| A1 | **Bundle `GCCWebApp-deploy-20260921-0805.zip`** — 61.2 MB / 340 entries, in-zip DLL md5 `0d2203208598b959`. Carries all 11 resolved-but-unshipped items (CR-051/052/053, DEF-430/431/432/434/436/437/438/439). Gate 0B reported 11 RESOLVED-AND-WAITING, 0 unclassified. Verifier: 0 failures. | **BUILT, VERIFIED, NOT UPLOADED** | — | yes |
| A2 | **DEF-433 — anonymous PDF-render routes are unthrottled and block on a timeout-free lock** | **RAISED, NOT FIXED** | **3–4 hr** + a real concurrency load check | **yes — hard blocker** |
| A3 | **DEF-440 — overwrite-import aborts because its own safety backup times out** (a single-certificate export exceeds 60s; non-deterministic) | **RAISED, NOT FIXED** | ~3–4 hr | yes — denies a documented client operation |
| A4 | **Production `robots.txt`** — currently `Disallow: /`, correct for test, wrong for a public certificate directory | not started | ~15 min | yes |
| A5 | **`TestEnvironmentGate:Enabled = false`** on the production site. Note: the middleware binds options **once in the constructor** and is a singleton — **an app-pool recycle is mandatory**, config alone does nothing | **DONE EARLY 2026-09-22 — deliberate (Shawn).** Gate off and `robots.txt` open on gccwebapp.com while `db.` still points at the legacy host. The gated test site is now `test.gccwebapp.com` (CR-050). **DEF-433 is therefore no longer mitigated by the gate** — see §A2 | ~15 min + recycle | yes |
| A6 | **Canonical-host 301** → `db.greencirclecertified.com`, path and query preserved, as an IIS URL Rewrite rule. Must go in the **repo's `web.config`** and ship in the bundle — `web.config` is tracked and republished on every deploy, so a rule added on the server disappears silently at the next publish | not started | ~1 hr incl. staging test | yes |
| A7 | **Legacy deep-link 301 map** — `/certificate` → tile view; `/api/certificates/{id}/download` → `/Certificate/CertificateSingleView/{number}` via `record_id` lookup. **Do NOT redirect `/api/*`** — it would turn a working unauthenticated integration into a 401, which is worse than a clean 404 with a message | **BUILT 2026-09-21** — `rewritemaps.config`, **5,999 entries**, generated by `Scripts/cutover/generate-legacy-redirect-map.py`; rules + `configSource` in `web.config`; `Content Update` in the csproj; publish verified to carry it. **Live + target checks still owed** — `Scripts/cutover/verify-redirect-map.py` | done | — |
| A8 | DEF-435 (preview accessibility backlog), DEF-126 (stray modal glyph) | RAISED, low | — | **no — defer past go-live** |
| A9 | `AllowedHosts` tighten from `"*"` to an explicit list | deliberately deferred | ~30 min | **no — post-cutover hardening** |

### B. Test environment — CR-050 · **STOOD UP 2026-09-21; isolation items open**

| Step | Status (verified 2026-09-21) |
|---|---|
| DNS `test.gccwebapp.com` → `64.27.52.66` | ✅ |
| Plesk site + Let's Encrypt | ✅ — TLS validates |
| **App deployed** | ✅ — deployed and running |
| **Pointed at its own database** | ✅ — startup banner reads **`DB catalog : gccwebap_test`** |
| Gate on, `robots.txt` `Disallow: /`, handler mapped | ✅ — `/` 302s to `/__gate`; config and binaries 302 rather than returning content; `/robots.txt` 200 |
| **Partner API keys rotated inside `gccwebap_test`** | ❌ **open — live exposure.** The gate skips `/api/*` by design, so a live Amazon key authenticates here today and returns a week-old copy of client data |
| `Jwt:Key`, `DataProtection:ApplicationName`, `FromName` confirmed as test values | ❌ unverified — the banner proves the connection string only |
| Copied `DataProtectionKeys/` deleted · uploads confirmed · write-isolation check | ❌ not done |
| ~3,600 smoke-harness refs repointed off `gccwebapp.com` | ❌ open — **required before go-live**, or every script follows the 301 into production |

Full list with reasons: the **"Still outstanding" table in CR-050**.

> The 2026-09-21 build hit a `403 Forbidden` that read like a `web.config` fault and was a
> **document-root mismatch**. The one-request discriminator, and why adding `fileExtensions` /
> `hiddenSegments` to `web.config` is the wrong fix on this box, are now in the runbook's
> Troubleshooting section.

### C. Data — the long pole · **Anuki + client · ~2–3 days elapsed**

| # | Item | Status |
|---|---|---|
| C1 | **Certificate numbering must be seeded before cutover.** Measured against the client's live DB 2026-08-13: load-set high-water is `26-0481`, so the app's next auto-number is `26-0482`, while the client's live system has issued to **`26-0591`**. **110 numbers would be silently re-issued** — 107 `DRAFT - Internal`, 3 `Client Approved - Paid`. A re-issue collides *later*, as a failed import row | **OPEN — go-live blocker** |
| C2 | **Dual-entry window.** Since the 2026-09-03 batch-2 load, GCC has been adding certificates to **both** systems, both allocating `26-XXXX`. `Certificate.Number` is UNIQUE ⇒ collision risk | **OPEN** |
| C3 | **Final delta load** — everything added to the legacy system since 2026-09-03 | not started |
| C4 | **51 live certificates absent from the load set** (ASSA ABLOY brands; 50 verified live on the public mirror). Loading exactly what was sent drops them at cutover | **OPEN — asked, not answered** |
| C5 | **Reconciliation in both directions** after the final load — key sets compared both ways, not counts (equal counts are not the same rows) | pending C3 |
| C6 | **Cutover assertion, falsifiable:** after the final load, assert the app's next certificate number is **greater than** GCC's written last-issued number | pending C1 |

> Migrated certificates arrive with **no audit history** — the SQL loader bypasses the application by
> design. If the client expects "imported on *date* by *user*" per record, they will not see it. Raise
> this before go-live, not after.

### D. Infrastructure — DNS, TLS, email · **client + Anuki · ~0.5 day + propagation**

Detailed in §2. The **hard dependency is the HostMonster login** — the `greencirclecertified.com`
zone is on `ns1/ns2.hostmonster.com`, which is the client's, not ours. Nothing in §2 can be scheduled
until we know who performs those edits.

> **Update 2026-09-22:** go-live was brought forward and executed the evening of 2026-09-22, not
> 13 Oct. Email moved the same night: sender `noreply@db.greencirclecertified.com`, SPF-only (DKIM
> still open), inbox delivery confirmed. See `outbound-email-client-domain.md`.

Outbound email (`admin@gccwebapp.com` → the client domain) is **independent of the web cutover** and
can run before or after. Headline: the client domain enforces **DMARC `p=quarantine`**, so SPF *and*
DKIM are mandatory rather than optional — a misconfigured sender lands in junk while the application
reports success. ~0.5–1 day plus DNS records. **Recommend scheduling it after go-live**, so a mail
problem cannot hold the cutover.

### E. Comms, sign-off and hygiene · **~2 days spread across the window**

| # | Item | Status |
|---|---|---|
| E1 | **Amazon notice** — the base URL changes **and the GS1 `id` URI changes with it**. They asked for a window before anything is retired | not sent |
| E2 | **Partner Integration Guide** — remove the "available at go-live" marker and the NOT-YET-LIVE note; confirm the promised hostname matches §0 (it currently says `database.`, which will now be a redirect, not the canonical name) | pending |
| E3 | **Amazon API key rotation** — the live production key appears in plaintext in the Dev Guide PDF | **open** |
| E4 | **User guides** — Administrator, Analyst Certificate, and Company User guides are written (`docs/user-guide/`, v1.0, .docx + .html) but **not issued** | ready to send |
| E5 | **Client UAT and written sign-off** across the three roles | not started |
| E6 | **Repo hygiene before a public host:** plaintext production DB credentials committed in `docs2/claude-ref/DATABASE.md` and `TESTING.md`; committed `.env` secrets; live infra identifiers in tracked `*.template.json` files | **open** |
| E7 | **SEO deindex** of the old host once the redirect is in place | after go-live |

---

## 2. DNS changes

All records live in the **`greencirclecertified.com` zone at HostMonster** (`ns1/ns2.hostmonster.com`).
`gccwebapp.com` is in a zone we control at AccuWeb — no client involvement for that one.

### 2.1 The record changes

| # | Record | From | To | TTL | When |
|---|---|---|---|---|---|
| 1 | `database` **A** | `70.32.82.212` | `64.27.52.66` | **300** | D-7 (pre-stage) |
| 2 | `db` **CNAME → A** | **CNAME `cname.vercel-dns.com`** *(Vercel)* | `64.27.52.66` **A** | **300** | **D-0 — this is the cutover** |
| 3 | `dbtest` **A** | `64.27.52.66` | *unchanged* | — | — |

> 🛑 **Neither record change can be saved until one TTL is fixed.** The cPanel zone editor rejects
> **every** save in this zone because the **apex TXT RRset** holds three records at 14400 and one —
> `apple-domain-verification=OC6SuW8PHizJzPcP` — at **3600**. A genuine RFC 2181 §5.2 violation, and
> the only non-14400 record in all 53. **Fix: edit that one record's TTL to 14400.** See **runbook
> step 0**, which also explains why external DNS queries cannot see this (servers collapse an RRset
> to its lowest TTL) and why lowering a single A record to 300 may hit the same validator.

**Rules, from experience:**

- **Replace** the existing A record. Do not layer a second one — two A records round-robin and half
  your traffic lands on the old host.
- HostMonster's cPanel will offer to create a **"hosted subdomain"** (a folder on *its* server) and to
  issue **SSL** for it. **Decline both.** This is a bare DNS record pointing away from HostMonster;
  TLS lives on the server that answers.
- **TTL 300** through the cutover window; raise to 3600+ once stable (D+7). **Lower the TTL as a
  separate, earlier edit than the value change** — the zone is on **14400 (4 h)** today, and a TTL
  reduction is not honoured until the previously-cached TTL expires, so changing both at once buys
  no agility on either the flip or the rollback. Runbook steps 1a / 7b carry the detail.
- **`db.` IS a CNAME to Vercel** — confirmed authoritative 2026-09-21, `cname.vercel-dns.com`, TTL
  14400. So record 2 is a **delete-then-create**, not an edit: delete the CNAME, create the A record,
  both in the same visit (in between, the hostname NXDOMAINs). **Note the CNAME target down first —
  it is the rollback.** Roll back by recreating the CNAME, **never** by pointing an A record at a
  Vercel IP: those are rotating anycast addresses (`66.33.60.67` / `76.76.21.98` on 2026-09-21) and a
  pinned one is a rollback that looks done and serves nothing.

### 2.2 Server side, on `64.27.52.66` (Plesk / AccuWebHosting)

| # | Step | Note |
|---|---|---|
| 4 | Add **`db.greencirclecertified.com`** and **`database.greencirclecertified.com`** as **Domain Aliases** of the existing `gccwebapp.com` site | **Websites & Domains → gccwebapp.com → Add Domain Alias.** **Uncheck "Synchronise DNS zone with primary domain"** — DNS lives at HostMonster. **Do not create new sites** — a second site gets its own data-protection key ring |
| 5 | Issue **Let's Encrypt** covering both aliases | Only succeeds **after** the matching A record resolves to this server. Sequence matters |
| 6 | Deploy the final bundle (carries A4/A6/A7 — robots, canonical 301, legacy map) | |
| 7 | Flip `TestEnvironmentGate:Enabled` → `false` **and recycle the app pool** | The setting is read once at construction; without the recycle nothing changes |

### 2.3 Verification, from outside the network

`docs/cutover/scripts/verify-cutover.sh` already does DNS, TLS, app identity and redirects. Add:

- `https://db.greencirclecertified.com/api/v1/certificates` → **401** anonymously (not 200, not HTML)
- The same endpoint with a **real Amazon key from an unauthenticated session** → JSON, and the GS1
  `id` values now read `https://db.greencirclecertified.com/...`
  *(An admin cookie makes the partner middleware stand aside and the 401 reads like a bad key.)*
- `https://gccwebapp.com/Certificate/CertificateSingleView/26-0224` → **301** to the same path on `db.`
- `https://database.greencirclecertified.com/anything?q=1` → **301** preserving path **and query**
- A sample of **20 legacy integer-id URLs** → 301 to the correct certificate number
- `https://db.greencirclecertified.com/robots.txt` → no longer `Disallow: /`
- `https://db.greencirclecertified.com/appsettings.Production.json` → **must NOT return 200**

---

## 3. Proposed schedule

Anchor: **go-live Tuesday 2026-10-13.** Tuesday deliberately — never a Friday, and it leaves three
business days of support before a weekend.

| Window | What happens | Owner |
|---|---|---|
| **Mon 22 – Wed 24 Sep** | Fix DEF-433 and DEF-440. Build the legacy deep-link redirect map and validate it against live certificate numbers both directions | Anuki |
| **Thu 25 – Fri 26 Sep** | Finish CR-050: deploy the app to `test.gccwebapp.com` with its own database, uploads and gate **on**. Confirm it is gated before it holds data | Anuki |
| **Fri 26 Sep** | **Client decisions due** (see §5) — DNS owner, last-issued certificate number, the 51 missing certificates, UAT dates | **Client** |
| **Mon 29 – Tue 30 Sep** | Cut and gate the **final** build. Deploy to the test environment. Full smoke + security regression | Anuki |
| **Wed 1 – Fri 3 Oct** | Certificate-number seed, dual-entry reconciliation, final delta load, two-way reconciliation | Anuki + client |
| **Mon 6 – Fri 10 Oct** | **Client UAT** against the three user guides. Amazon given notice of the base-URL and GS1 `id` change. Written sign-off by **Fri 10 Oct** | **Client** |
| **Fri 10 Oct** | Pre-stage DNS: `database` A → `64.27.52.66`. Plesk aliases + Let's Encrypt for both names. Zero production impact — `database.` only ever served a redirect | Client (DNS) + Anuki |
| **🚀 Tue 13 Oct, 09:00** | **GO-LIVE.** Change freeze on the legacy system → final delta check → flip `db` A record → verify → gate off + app-pool recycle → run the full verification set | Both |
| **Tue 13 – Fri 17 Oct** | Hypercare. Watch IIS logs for 404s on unmapped legacy links. Legacy Vercel project stays alive (reachable by its direct Vercel URL) as a data fallback | Anuki |
| **Mon 20 Oct** | Raise TTLs. Tighten `AllowedHosts`. Submit the deindex request for the old host | Anuki |
| **Late Oct – Nov** | **Phase 2:** move outbound email to the client domain (SPF/DKIM/DMARC); decommission the legacy Vercel project and hand over its data and ownership | Both |

**Fallback: Tuesday 2026-10-20.** Trigger it if UAT sign-off slips past Fri 10 Oct or any §5 decision
is still open on Fri 3 Oct.

**A small reason to prefer the 13th.** The only test account that can prove a company still sees its
own *future-dated* certificates while a partner cannot is X-SENSE, whose fixture certificates
(`26-0441`/`26-0442`) become effective **2026-10-21**. After that date the carve-out is untestable
without building a new fixture. Going live on the 13th keeps it usable through UAT.

---

## 4. Rollback

**Revert the A record.** That is the whole procedure, and it is why the DNS flip is last.

| Failure | Rollback | Time to recover |
|---|---|---|
| App is wrong on the new hostname | **Delete the `db` A record, recreate CNAME → `cname.vercel-dns.com`**, TTL 300. Never an A record at a Vercel IP — they rotate | one TTL (~5 min) |
| Redirects are wrong | Edit the rule on the server **and** in the repo's `web.config` — otherwise the next publish restores the broken rule | minutes |
| Gate should not have come off | `Enabled: true` + **app-pool recycle** | minutes |
| Data load is wrong | Logical restore — drilled 2026-08-13, 8,823 rows in 24–82s. **Uploaded PDFs are a separate restore path** — zip `wwwroot/uploads/` first | under an hour |

**Two standing rules:** redirects live at the server layer, never in application code, so a rollback is
a config flip and not a redeploy. And **back up the server's `wwwroot/uploads/` before extracting any
bundle** — the 2026-06-15 incident, where every production PDF disappeared, is why.

The legacy Vercel project is **never reconfigured** by us, so it resumes the moment DNS propagates
back. Ask the client not to delete it for 30 days.

---

## 5. What we need from the client — the actual blockers

Nothing in §3 can be committed until these are answered. Ordered by how much of the schedule each one
holds up.

1. **Who edits DNS at HostMonster?** Every record change in §2.1 depends on it. If the client holds
   the login, steps 1 and 2 are theirs to perform and the go-live hour is theirs to set.
2. **What is the last certificate number issued in the legacy system?** In writing. Without it the new
   system will silently re-issue roughly 110 numbers that already belong to other certificates. The
   preferred fix is to include the drafts in the final pull and load them as `Draft`, which makes it
   correct by construction rather than by a manual floor.
3. **The 51 live certificates absent from the load set** — load them, or accept that they disappear at
   cutover. Asked 2026-08-12, not yet answered.
4. **Confirm the change freeze** on the legacy system for the dual-entry window, and the agreed go-live
   hour.
5. **UAT dates and who signs off**, per role.
6. **Amazon's required notice period** for the base URL and GS1 `id` change.
7. **Keep the legacy Vercel project alive for 30 days** after cutover, and confirm who owns its data
   export and eventual decommissioning.

---

## 6. Honest risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Legacy deep links break at cutover | **High if A7 slips** | High — printed and bookmarked certificate links | A7 is on the critical path. The mapping data is complete; this is build work, not discovery |
| Certificate numbers silently re-issued | **High if C1 unanswered** | High — surfaces weeks later as failed imports | Falsifiable assertion C6, run **after** the final load |
| DEF-433 lets an anonymous caller stall the whole site | Medium | **Critical — takes the Amazon API down with it** | A2, before the gate comes off. The gate is the *only* thing currently mitigating it |
| No gated test environment after the gate comes off | Medium | Medium — nowhere safe to test | Finish CR-050 (§B) first |
| HostMonster DNS owner unknown | **High** | Schedule-breaking | §5 item 1, answer needed by Fri 26 Sep |
| Email sender lands in junk under DMARC `p=quarantine` | Medium | Medium | Deliberately scheduled **after** go-live |
| Live Amazon key exposed in a distributed PDF | **Certain — already true** | High | E3, rotate before the public host goes up |
| Plaintext production DB credentials in committed docs | **Certain — already true** | High | E6, before the repo is anywhere near public |

---

## 7. What this plan does not claim

- **Nothing has been deployed.** The 2026-09-21 bundle has not left the build machine.
- **What is currently live on `gccwebapp.com` was not probed** for this plan. The bundle's contents
  were compared against the previous *bundle*, not against the *server*.
- **No smoke suite has been run against the final build.** DEF-437 verified itself 23/23 on localhost;
  the other ten items were verified by their own sessions and not re-run.
- **The effort figures are estimates**, except DEF-433 (~3–4 hr) and CR-050 infrastructure (~3–4 hr),
  which were priced when the items were raised.
- **The dates are proposed, not agreed.** Every one of them moves if §5 does not land by Fri 26 Sep.
