# Security Regression Suite (`_security/`)

Maintained, re-runnable **P0 security checks** — data isolation, RBAC negative paths, and IDOR.
Phase 1 of `docs/qa/regression-e2e-strategy.md`. These promote the one-shot probes
(`cr171-*.js`, `def236-idor-verify.js`, the partner section of `prod-e2e-readonly-batch.js`) into
a uniform suite so they run as a **deploy gate** instead of proving a fix once and going stale.

## What's here

| Spec | Gap | Asserts |
|---|---|---|
| `partner-isolation.js` | C9 (P0) | Each partner user sees a scoped, non-empty set; **no co-partner name leaks** in list; multi-partner user sees all own partners; export CSV rows ≤ tile scope. |
| `rbac-matrix.js` | RBAC (P0) | Every gated page returns 401/403/302 for roles below it (**allowed-when-should-deny = FAIL**); positive controls confirm each role reaches its own pages. |
| `idor-download.js` | C9 (P0) | `DownloadSelectedZip` with a wide id range returns only the external user's authorized certs (< pre-fix 32-entry cross-company dump). |
| `cert-detail-scoping.js` | C2 + C9 (P0) | Per-cert DETAIL pages: external sees own-company cert (200) but **another company's cert 404s** (no cross-company detail leak); internal tags never render on external/public views; anonymous sees public certs (200) but non-public 404. Runtime-discovered fixtures. |
| `_lib/security-harness.js` | — | Shared login, `apiJson`, `status`, leak scan, recorder, roll-up + JSON writer. Localhost-guarded. |
| `run-all.js` | — | Runs all specs → `_results/security-summary-{stamp}.json`. **Any FAIL or BLOCKED exits non-zero.** |

## Run

```bash
# server must already be running on localhost:5101 (read-only suite)
node Scripts/smoke-prod/_security/run-all.js       # full gate
node Scripts/smoke-prod/_security/partner-isolation.js   # single spec
```

Env: `BASE_URL` (default `http://localhost:5101`), `SEC_PWD` (default `TestPass123!`),
`SMOKE_ALLOW_REMOTE=1` + `SMOKE_REMOTE_USER`/`SMOKE_REMOTE_PASS` to point at the gated test env.

## Result semantics

- **PASS** — check held.
- **FAIL** — a real security regression (a leak, or a gated page returning 200). **Hard deploy block.**
- **WARN** — functional/data-scope oddity, not a leak (e.g. a role denied a page it should reach — investigate, don't panic).
- **BLOCKED** — spec couldn't run (test account/data missing). Fix test data; the gate stays non-green until then.

## Test-data dependency

`partner-isolation.js` needs the CR-171 partner users (`partner@amazon.com`, `perry.novi@…`,
`pam.wayfair@…`, `pip.other@…`, `pru.multi@…`, all `TestPass123!`). If they're absent locally the
spec records **BLOCKED**, not FAIL — restore the CR-171 test data (see
`cr171-partner-isolation-local.js` history) before treating the suite as authoritative.
