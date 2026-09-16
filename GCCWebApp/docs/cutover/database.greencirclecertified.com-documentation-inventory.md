# Documentation inventory — hosting, deploy, DNS, TLS, email

**Part 1 of the read-only cutover audit.** Produced 2026-09-14. **Nothing was changed.**

Scope of the search: the whole repo plus `../docs2/` (the reference set that lives at the repo root).
Patterns: `deploy`, `hosting`, `dns`, `plesk`, `ssl`, `tls`, `smtp`, `email`, `server`, `env`,
`runbook`, `handover`, `README`. "Last updated" is `git log -1` on the file, not a date written
inside it — several documents carry a headline date newer than their last real edit.

---

## 1. What exists

| File | What it covers | Last updated | Still accurate? | Gap it leaves |
|---|---|---|---|---|
| `docs/tracking/pre-deploy-agent-checklist.md` | Gates 0-12 before every Plesk deploy | 2026-09-11 | **yes** — the most current operational doc in the repo | Written for deploying *to the existing host*. No gate for a hostname change, a new binding, TLS issuance, or a redirect. |
| `docs/tracking/2026-09-10-deploy-runbook-CR048-CR049.md` | Most recent per-deploy runbook; migration-before-code ordering | 2026-09-10 | **yes** | Per-release, not per-environment. |
| `Scripts/deploy.ps1` | Builds the bundle; refuses to ship `appsettings.Secrets.json`; appends `post-deploy/*.sql` | 2026-08-31 | **yes** | Bundles `web.config` — see §3.1. No awareness of hostname or bindings. |
| `docs/tracking/email-draft-2026-08-12-production-hosting-request.md` | **Full spec for a new production environment** — server, DB, storage, DNS/TLS, SMTP, Duo, backups, handover | 2026-08-12 | **substantially — but 4 facts have since changed**, and it was **never sent** | See §2. This is the single most useful document for the cutover and it is a draft. |
| `docs/tracking/2026-08-14-amazon-host-change-note.md` | Decision that production is a dedicated subdomain; drafted note to Amazon | 2026-08-31 | **yes** | Cutover **date still a literal `[DATE]`**. Never sent. |
| `docs/tracking/production-cutover-plan.md` | 2FA + Duo readiness; lockout analysis; break-glass | 2026-08-10 | **partly stale** — see §2.2 | Despite the name, contains **no hostname, DNS, TLS or redirect content at all**. |
| `docs/tracking/go-live-test-runbook-2026-08-14.md` | Go-live functional test pass | 2026-08-31 | yes | Tests the app, not the cutover. |
| `docs/tracking/CR-039-golive-test-data-purge.md` | Test-data purge keep-list for go-live | 2026-08-21 | yes (raised, not sent) | Data, not infrastructure. |
| `../docs2/claude-ref/DEPLOYMENT.md` | Production env, web.config rules, deploy checklist, common 500s | 2026-07-30 | **stale** — predates DEF-363 / DEF-395 / the Sept runbooks | Presented as *the* deployment reference; a reader would not know it is six weeks behind. |
| `../docs2/claude-ref/DATABASE.md` | DB connections, schema, prod reset | 2026-07-30 | mostly | Nothing on the production/test DB split the hosting request asks for. |
| `Scripts/uat/README.md`, `POST_DEPLOYMENT_CHECKLIST.md`, `PRE_DEPLOYMENT_CHECKLIST.md` | UAT deploy + smoke | 2025-12-07 | **stale** — cites `admin@greencirclecertified.com` / `Admin123!`, accounts that no longer apply | Actively misleading if followed today. |
| `DEPLOYMENT_LOG.md` | Historical deploy log | 2026-03-25 | historical | Superseded by `docs/tracking/shipped-2026-MM.md`. |
| `Scripts/deployment/*` (~25 files: `deploy-plesk-*.sh`, `nginx-plesk-config.conf`, `deploy-to-azure.*`, `deploy-iis.*`) | Early deployment experiments — Linux Plesk, nginx, Azure, Docker | 2025-10-10 | **dead** | The hosting-request email says Docker was evaluated and rejected. **`nginx-plesk-config.conf` is the trap**: it implies a Linux/nginx front end, and the real host is Windows/IIS in-process. |
| `Scripts/docker/*` | Docker deploy variants | 2025-10 | **dead — explicitly** | `email-draft-2026-08-12:43` says to ignore them. They are still in the repo. |
| `.env.example` | Env var template | 2025-09-28 | **stale** | Predates the whole current config approach. |

---

## 2. What has changed since the documents were written

**These are not nitpicks. Each one would send a reader to the wrong conclusion.**

### 2.1 The hosting-request email (2026-08-12) — four facts have moved

It is the best document we have, and it is measurably out of date in four places. Anyone re-using it
should fix these first:

| Claim in the email | State on 2026-09-14 |
|---|---|
| "SMTP settings are empty → `MockEmailService` → total lockout" cited at `appsettings.Production.json:92-99` | Still true **in principle**, but the line numbers are now **150-159**, and `Username`/`Password` are deliberately blank because they come from `appsettings.Secrets.json` on the server. Mail **does** work on test today. |
| `Authentication:Enable2FA` is `false` in Production | **Now `true`** (`appsettings.Production.json:144`), safe only because a per-user check was added (`Resolve2FARequirementAsync`). |
| Uploads default to `wwwroot/uploads` | **Now `App_Data\uploads`** (`UploadPathResolver.cs:99`); config key is `FileStorage:StoragePath`, still unset — see `appsettings.Production.json:110-133`. |
| "`AutoMigrateOnStartup: true` contradicts policy" (internal notes) | **Now `false`** (`appsettings.Production.json:107`). The contradiction it flags is resolved. |

### 2.2 `production-cutover-plan.md` — stale in its central table

§2a states Production has `Enable2FA: false` and SMTP `""`. Both have moved (above). The §1 lockout
analysis is still the right analysis; the config snapshot under it is not current. **Do not read the
config values out of this file** — read them out of `appsettings.Production.json`.

### 2.3 The name "production-cutover-plan" is misleading

It is a **2FA/Duo readiness plan**. A person told to "read the cutover plan" before a DNS cutover
gets nothing about DNS, bindings, TLS, redirects or deep links. Either rename it or cross-link it to
these `docs/cutover/` files.

---

## 3. What is NOT documented anywhere

The audit prompt asks for this explicitly. Each item below was searched for and **not found** in the
repo or `docs2/`.

| Missing | Why it matters for cutover | Now recorded in |
|---|---|---|
| **Server public IP** | The A record for the new hostname points at it. Nowhere in any doc. | inventory §2 — `64.27.52.66` |
| **Which DNS provider is authoritative for `greencirclecertified.com`** | Records go where the NS points. Never stated; the audit template *guessed* Bluehost. | inventory §1 — **HostMonster** |
| **That the target hostname already resolves to an unrelated host** | Blocks TLS issuance. Not known to any document. | inventory §3.1 — `70.32.82.212` |
| **TLS issuance method / renewal owner** | An expired cert takes down the Amazon API, not just the site. The hosting email *asks* for auto-renewal; no answer is recorded. | — **still open** |
| **Plesk access — who has it, what plan, which panel version** | Every binding and certificate step needs it. | — **still open** |
| **A rollback procedure for a hostname change** | Distinct from the app rollback the deploy runbooks cover. | audit Part 4 |
| **SMTP setup as built** (vs. as requested) | `appsettings.Secrets.json` is server-only by design, so the only record of what is actually configured is on the server. | — **still open** |
| **Backup/restore procedure, and whether a restore was ever tested** | §7 of the hosting email asks for one before go-live. No result recorded. | — **still open** |
| **Old-site (`db.` / Vercel) ownership and vendor contact** | Nobody can retire a hostname they do not control. | inventory §5 |
| **Go-live date** | Everything sequences from it. | — **still open** |

---

## 4. The decision these documents disagree about

**This is the most important finding in Part 1, and it is not a documentation gap — it is an
unresolved choice that changes the whole shape of the work.**

Two live plans point in different directions:

- **`email-draft-2026-08-12-production-hosting-request.md`** asks for a **brand-new production
  environment** — separate Windows box, separate SQL Server, separate empty database, separate data
  volume. Its stated headline reason is that *"today test and production are the same box and the
  same database — the test site writes to live client data."* That is confirmed in
  `appsettings.Production.json:92-97`.

- **The `client-domain-cutover` skill's default path** (Step 2) is to add the client hostname as a
  **domain alias on the existing site** — same box, same app, same database, one more name.

They are not variations of each other:

| | New environment | Alias on existing site |
|---|---|---|
| Fixes test/prod sharing one database | **yes** — the stated reason for the request | **no** |
| Effort | server build, DB build, data migration, full re-verification | hours |
| Rollback | environment-level | one DNS record |
| Data Protection keys / sessions | new key ring; everyone signed out | shared with the existing site; nothing disturbed |
| Can the test gate stay on for `gccwebapp.com` and off for the client host? | **yes** — separate processes, separate config | **no** — see audit Part 3 |

The last row is the one that forces the decision rather than leaving it to preference, and it is
covered in the audit file. **Neither option is chosen here.** It is a cost and scope question for
Shawn and the client, and the hosting request has not been sent, so nothing is committed.

---

## 5. Recommendation for the documentation itself

Ordered by how much confusion each removes, cheapest first:

1. **Put a stale-banner at the top of `production-cutover-plan.md` and `../docs2/claude-ref/DEPLOYMENT.md`** pointing at the current values, rather than editing the analysis. One line each.
2. **Delete or clearly mark `Scripts/deployment/nginx-plesk-config.conf` and `Scripts/docker/`.** A Linux/nginx config in a Windows/IIS project is the kind of thing that gets read as truth at 2am.
3. **Fix the four moved facts in the hosting-request email** before it is sent — it is the document that will be judged by an outside sysadmin.
4. **Fix `Scripts/uat/*`** or delete it; the credentials it prints do not work.
5. Leave `pre-deploy-agent-checklist.md` alone — it is current and it is the one that gets used.

---

Related: `database.greencirclecertified.com-inventory.md` (Step 0 + DNS evidence) ·
`database.greencirclecertified.com-audit.md` (Parts 2-5, consolidated 2026-09-15).
