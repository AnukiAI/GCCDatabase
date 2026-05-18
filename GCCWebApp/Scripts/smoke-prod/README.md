# Post-deploy smoke test — `smoke-prod`

60-second operational confidence check against `gccwebapp.com` (or wherever you point it). **Run after every Plesk deploy.**

## Why this exists

DEF-195 (QC #174) was deployed on 2026-05-15. The first upload "looked clean" in a manual browser walk-through, but the actual CSS files on the server were never replaced — Plesk's default extract behavior skips existing files. The smoke test caught it via two checks the eyeball missed:

1. The deployed file's content didn't match what the local build produced (asp-append-version hash diverged).
2. `Last-Modified` on `/css/main.css` and `/css/certificatetileview.css` was days old.

This runner exists so the same class of failure never sneaks past again. Anything that asserts "the site loads" is not enough — you have to assert "the files I just built are the files the server is serving."

## How to run

```bash
cd scripts/smoke-prod
npm install                  # one-time
node smoke.js                # full smoke against gccwebapp.com

# variations
VERBOSE=1 node smoke.js                                  # show per-check details
node smoke.js --base https://greencirclecertified.com    # different env
GATE_USER=gcc GATE_PASS='Cert-Test-Access-2026!' node smoke.js
```

Defaults:

- **Base URL:** `https://gccwebapp.com` — change with `--base` or `BASE_URL` env var
- **Gate creds:** `gcc` / `Cert-Test-Access-2026!` — the Basic Auth wall on the test env (see `Middleware/TestEnvironmentGateMiddleware.cs`). Set `GATE_USER` / `GATE_PASS` for prod or rotated creds.
- **Screenshots:** dropped in `scripts/smoke-prod/screenshots/` (gitignored). One full-page PNG per viewport for the tile view.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | All checks passed — safe to call the deploy good |
| 1 | At least one check failed — fix-then-redeploy, **do not announce** |
| 2 | Runner itself errored (network, browser launch, etc.) — investigate before trusting results |

## What's covered

### [1] Gate behavior (anonymous, no Basic Auth credentials)

| # | Check | What it proves |
|---|---|---|
| 1.1 | `robots.txt` bypasses gate + serves `Disallow: /` | Test-env crawl blocker is live |
| 1.2 | `favicon.ico` bypasses gate (no Basic challenge regardless of file existence) | Gate's path skip list works |
| 1.3 | Site root requires Basic Auth challenge | Middleware actually mounted |
| 1.4 | Partner API bypasses Basic gate, requires X-API-Key | `/api/*` exception preserved (Amazon depends on this) |
| 1.5 | Partner API rejects bogus key with 401 | Key validation enforced, not bypassed |

### [2] UI surfaces (behind the gate)

| # | Check | Catches |
|---|---|---|
| 2.1 | Site root loads after Basic Auth | Middleware lets authed traffic through |
| 2.2 | `/Account/Login` renders password field | Login form regression |
| 2.3 | `/Certificate/CertificateTileView` renders cards (>0) | API or query regression |
| 2.4 | `/Certificate/CertificateSingleView/{n}` returns 200 | **DEF-103 regression** — public cert reachable, status filter not over-broad |

### [3] DEF-195 visual — multi-viewport

For each viewport (desktop 1440, tablet 1024, mobile 390):

- `.header-row` computes `align-items: flex-start` on every rendered tile
- Multi-line-category tiles have cert-id pill drift ≤ 1px

This is the regression check for [QC #174 / DEF-195](../../docs/tracking/defect-register.md). The drift assertion only runs on viewports where categories actually wrap to 2+ lines (typically desktop and tablet — mobile reflows to a single column so wrapping shifts).

### [4] CSS sanity — content + freshness

For both `main.css` and `certificatetileview.css`:

- **Content check** — deployed file body matches the DEF-195 fix pattern
- **Freshness check** — `Last-Modified` is within the last 14 days

The freshness check is the critical addition. **If you see a CSS freshness failure right after a deploy, the deploy didn't carry — Plesk skipped existing files.** Re-extract with "Overwrite All" explicitly selected.

### [5] Role-gated pages reachable (no 500s)

| Path | Why |
|---|---|
| `/Admin/UserManagement` | Admin user CRUD (DEF-181, DEF-186, DEF-187 area) |
| `/Admin/ProductTypes` | DEF-196 area — recent fix to delete-with-references |
| `/Admin/Companies` | Admin company CRUD |
| `/Admin/PartnerApiKeys` | Partner API key management |
| `/Certificate/CertificateTileViewInternal` | Analyst+ tile view |
| `/Certificate/CertificateTileViewExternal` | External user tile view |
| `/Certificate/Create` | Cert authoring form (DEF-185, DEF-193, DEF-194 area) |

Unauthenticated requests should return 302 (redirect to login). Anything 500+ means a regression in the page itself (model binding, view rendering, controller exception). The test treats 302/401/403/404 as "no server error" — only ≥ 500 fails.

## Adding a check

Edit `smoke.js`. The `record(section, name, pass, detail)` helper appends to the results array and exits non-zero if any check failed. Group related checks under a numbered `section(n, title)` heading. Keep test names specific — when a check fails three months from now, the name is the only context you'll have.

Specific patterns this runner uses on purpose:

- **Use `getComputedStyle` + measured geometry** for visual assertions, not snapshot images. Snapshots are fragile across font renders, OS subpixel differences, viewport changes; pixel measurements (drift, dimensions) survive those.
- **Hit the actual deployed CSS** with `ctx.request.get(...)` and grep the body. Don't rely on what the browser thinks it's seeing — the browser can be cached.
- **Assert `Last-Modified` is recent.** It's the only cheap way to prove a static-asset deploy actually replaced the files. Stale mtime + 200 OK = deploy half-failed.

## Maintenance

- **When you ship a new CSS fix**, add a content-pattern check under section 4 so a future "deploy didn't carry" failure caught early.
- **When you add a new admin/analyst page**, add a path under section 5 so a controller-throws regression surfaces.
- **When the 14-day freshness window false-alarms** on a genuinely stable file you haven't touched in 3 weeks, bump the threshold rather than removing the check — the signal is too valuable.

## Why Node + Playwright instead of `GCCWebApp.UAT.Tests` (NUnit + Playwright)

The .NET UAT project is the right home for *feature* tests (login flows, CRUD round-trips, BDD scenarios). This runner is a *deploy* check: lightweight, runs in 60 seconds with no .NET build, no test discovery overhead, no Visual Studio. It's the kind of thing you want to fire one-shot from a terminal right after clicking Extract in Plesk.

If this grows past ~50 checks or needs CI integration, port to UAT.Tests as `PostDeploySmokeTests` and keep the npm version as a quick-iteration scratch pad.
