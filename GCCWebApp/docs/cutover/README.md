# `docs/cutover/` — hostname and environment cutover work

Index for this folder. **Consolidated 2026-09-15.**

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
| `database.greencirclecertified.com-runbook.md` | **The procedure** for moving the app onto `database.` | **PREPARED, BLOCKED** — see below |
| `outbound-email-client-domain.md` | Moving the sender off `admin@gccwebapp.com` onto the client domain | PREPARED, NOT EXECUTED |
| `test.gccwebapp.com-setup-runbook.md` | Standing up `test.gccwebapp.com` as a **separate site with its own database** (CR-050) | in progress |
| `scripts/verify-cutover.sh` | External verification — DNS, TLS, app identity, redirects | working |

## The three hostnames, which are constantly confused

| Hostname | Host | What it is | In scope? |
|---|---|---|---|
| `dbtest.greencirclecertified.com` | `64.27.52.66` (the app) | **Live. What partners integrate against today.** | no work needed |
| `database.greencirclecertified.com` | `70.32.82.212` — nginx 302 shim that drops the path | **In use by the existing certificate database.** Not free. | **blocked** on the legacy system being retired or renamed — a client decision |
| `db.greencirclecertified.com` | Vercel | The **live legacy public database** | separate cutover, explicitly out of scope |

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
