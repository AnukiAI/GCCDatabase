# Retire test.gccwebapp.com → 301 to dbtest.greencirclecertified.com (CR-055)

Raised 2026-09-29. Shawn: *"I just don't want the ability to navigate to test.gccwebapp instead the
test site goes to dbtest.greencirclecertified…"* — and: *"dbtest is separate project and db on the same
hosting platform."*

Status: **PLAN — not started.**

> **Correction, same day.** The first version of this runbook said dbtest "is the production site",
> based on its home page being byte-identical to db.'s. That was an over-read: the home page is the
> layout shell, and certificate data loads afterwards by AJAX, so two separate sites on the same build
> look identical in that test. Shawn confirmed dbtest is its **own Plesk site**. The Amazon decision
> ("D1") that followed from the wrong premise is **withdrawn**.

## What exists (probed 2026-09-29)

| Host | What it is | Probe |
|---|---|---|
| `test.gccwebapp.com` | Old gated test site, own Plesk site — **to be retired** | 302 → `/__gate` |
| `dbtest.greencirclecertified.com` | The test site going forward (own Plesk site) | 200, **no gate**, robots `Allow: /` (see note) |
| `db.greencirclecertified.com` | Production | 200 |

All three resolve to `64.27.52.66`. **No DNS change needed.**

## Steps (Shawn, Plesk) — the no-code route (recommended)

The site is being retired, so nothing needs to keep running on it except the redirect. Plesk's
**Forwarding** hosting type does that without the app.

1. **Back up the test.gccwebapp.com site folder first.** At minimum take `appsettings.Secrets.json`,
   `logs\` and any uploads. Changing the hosting type can remove the site's web content, and this is
   the only copy of the test-site secrets overlay.
2. Websites & Domains → **test.gccwebapp.com** → **Hosting Settings** (or *Hosting & DNS → Hosting*) →
   **Change hosting type → Forwarding**.
   - Destination: `https://dbtest.greencirclecertified.com`
   - Forwarding type: **Moved permanently (code 301)**
3. **Keep the SSL/TLS certificate on test.gccwebapp.com.** Without it, `https://test.gccwebapp.com`
   shows a certificate warning before the redirect can happen.
4. **Verify from outside:**
   - `http://test.gccwebapp.com/` and `https://test.gccwebapp.com/` → **301** → dbtest, no certificate
     warning
   - a deep link such as `https://test.gccwebapp.com/Certificate/CertificateTileView`: note whether
     Plesk keeps the path. Either result is acceptable for a retired test host; just record which.
5. **Stop deploying to test.gccwebapp.com.** Deploy runbooks name it as a target (the DEF-442 bundle
   went there on 2026-09-29). Future test deploys go to dbtest.

**Rollback:** change the hosting type back to Website hosting and restore the backed-up folder.

## Alternative — a `web.config` rule (only if Forwarding misbehaves)

A host-matched rule in the repo's `web.config` (`test.gccwebapp.com` → `https://dbtest…/{R:1}`, 301).
The catch: the app has to stay installed on test.gccwebapp.com just to answer the redirect, and the
rule only takes effect after a deploy to that site. About 1 hr, including a bundle.

## Note for the dbtest site (not part of this change)

dbtest serves **no gate** and robots.txt `Allow: /`, so crawlers are invited in. If dbtest is meant to be
private like test.gccwebapp.com was, its `appsettings.Secrets.json` needs `TestEnvironmentGate:Enabled:
true`. After DEF-443 ships, robots.txt then becomes `Disallow: /` automatically. If partners (Amazon)
call dbtest's API, the gate does not affect `/api/*` (test.gccwebapp.com's API answered 401, not a
gate redirect, on 2026-09-29).

## Estimate

Plesk steps: **~20 min, Shawn.** No code, no bundle.
