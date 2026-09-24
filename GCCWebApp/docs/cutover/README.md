# `docs/cutover/` — hostname and environment cutover work

Index for this folder. **Consolidated 2026-09-15.**


> ## ➡️ SUPERSEDED IN PART — 2026-09-21
> The hostname question is **decided**: `db.greencirclecertified.com` is the canonical public
> hostname, with `database.` and `gccwebapp.com` both 301ing to it. That answers open questions
> **1, 3 and 4** in the audit and removes this folder's "blocked on the legacy database" status.
> **Read `go-live-plan-2026-09-21.md` first** — it is the current plan of record and it
> promotes the legacy deep-link redirect map (audit §10) from out-of-scope to critical path.

## Why this file exists

Two audits of the same thing were produced independently on 2026-09-14 — `…-audit.md` and
`…-hostname-audit.md`. Root cause: the `client-domain-cutover` skill names its audit output **two
different ways** — `<hostname>-audit.md` in its Outputs section, `<hostname>-hostname-audit.md` in
its `assets/audit-prompt.md` template. Two passes following the same skill wrote to two filenames
and neither saw the other.

**Resolved:** the two are merged into `…-audit.md`, `…-hostname-audit.md` is deleted, and the skill's
template has been corrected in **both** the repo copy and the source `client-domain-cutover.skill` package (backup: `.bak-20260916`), so reinstalling the skill will not reintroduce it. Each pass had caught things the other missed and each
corrected the other; the merged file marks those **[corrected]**.

## What is here

| File | What it is | Status |
|---|---|---|
| `database.greencirclecertified.com-inventory.md` | **Step 0** — who owns what, with live DNS/TLS/PTR evidence | current |
| `database.greencirclecertified.com-audit.md` | **The findings.** What in the app assumes a hostname, the blockers, redirect placement, old-site deep links | **canonical — merged 2026-09-15** |
| `database.greencirclecertified.com-documentation-inventory.md` | What hosting/deploy/DNS/TLS/email documentation exists, what is stale, what is missing | current |
| `database.greencirclecertified.com-runbook.md` | **The procedure** — rewritten 2026-09-21 for the two-alias model: **`db.` canonical**, `database.` and `gccwebapp.com` 301ing to it | **EXECUTED 2026-09-22 ~20:56 EDT** — `db` A → 64.27.52.66, `database` repointed, Let's Encrypt covers both. Open: steps 11 (gccwebapp retirement rule), 13 (Amazon notice), TTL raise, deindex |
| `outbound-email-client-domain.md` | Moving the sender off `admin@gccwebapp.com` onto the client domain | **LIVE 2026-09-22** — `noreply@db.greencirclecertified.com`, SPF on `db`, inbox delivery confirmed. DKIM still open |
| `test.gccwebapp.com-setup-runbook.md` | Standing up `test.gccwebapp.com` as a **separate site with its own database** (CR-050) | **EXECUTED 2026-09-21** — site live, gated, on `gccwebap_test`; isolation items still open in CR-050 |
| `scripts/verify-cutover.sh` | External verification — DNS, TLS, app identity, redirects | working |

## The three hostnames, which are constantly confused

| Hostname | Host | What it is | In scope? |
|---|---|---|---|
| `dbtest.greencirclecertified.com` | `64.27.52.66` (the app) | **Live. What partners integrate against today.** | no work needed |
| `database.greencirclecertified.com` | `70.32.82.212` — nginx 302 shim that drops the path | Moves to the app and **301s to `db.`** | **yes — Phase 1**, near-zero risk |
| `db.greencirclecertified.com` | Vercel | The **live legacy public database** — its **hostname** becomes the app's canonical name | **yes — Phase 2, the cutover itself.** Decommissioning the Vercel *system* stays out of scope |

Plus `gccwebapp.com` (the current test host, `64.27.52.66`) and `test.gccwebapp.com` (CR-050, being
stood up as its own site against its own database `gccwebap_test`).

## Read in this order

1. `…-inventory.md` — what is true today
2. `…-audit.md` — what that means for the application
3. `…-runbook.md` — what to do, when it unblocks

## Two things the runbook does not list

Both are in the audit at **§8**, and both are **blockers for a public production hostname** rather
than for the DNS move itself:

- **The test-environment gate** (`Enabled: true`) is a global password wall on every non-API path,
  including the deliberately public certificate directory. It is **host-blind**.
- **`robots.txt` is `Disallow: /`** — correct for a test host, wrong for a public one.

Both are single-process, single-file state, so under a **domain alias** they cannot differ between
the two hostnames. That is the constraint that decides alias vs. second site — audit **§7**.

## Not a cutover doc

`docs/tracking/production-cutover-plan.md` is a **2FA/Duo readiness plan** despite its name, and its
config snapshot is stale. It contains no hostname, DNS, TLS or redirect content. Do not send anyone
there expecting cutover guidance.
