// E2E Smoke Tier 2 — certificate workflow-state visibility matrix.
//
// Verifies, per the 8-state lifecycle, which certificates each tile view shows vs hides.
// This is the cross-view "state" dimension the per-view Tier 1 smokes don't cover.
//
// SELF-CONTAINED: seeds fixtures (sqlcmd → seed-state-fixtures.sql), drives the 3 tile
// views per their required role, then tears the fixtures down (always, even on failure).
//
// RUNS LAST IN run-all.js (added 2026-08-03). It was previously opt-in-only — Tier 1 is
// fixture-free and fast, Tier 2 needs seeded DB rows + teardown — but that meant a routine
// roll-up silently skipped all 27 workflow-state visibility cells, i.e. the P0 control that
// stops Draft/Confidential/Hold certificates leaking to the public view. A gate nobody
// remembers to run is not a gate. It is ordered last so its seed/teardown can't perturb the
// fixture-free Tier 1 views, and it still runs standalone unchanged.
//
// Matrix is CODE-GROUNDED (not the plan's prose). The external view filters to
// PublicStatuses {Paid, Republished} + not-date-expired, scoped to the user's company —
// it does NOT show NotPaid/Confidential (the plan's matrix was wrong on that). Verified
// against CertificateTileExternalApiController.cs line ~221 + the public API.
//
//   | cert    | state                         | Public | External(own) | Internal |
//   | 99-9001 | Draft                         | hidden | hidden        | shown    |
//   | 99-9002 | Client Approved - Not Paid    | hidden | hidden        | shown    |
//   | 99-9003 | Client Approved - Confidential| hidden | hidden        | shown    |
//   | 99-9004 | Client Approved - Paid        | SHOWN  | SHOWN         | shown    |
//   | 99-9005 | Hold                          | hidden | hidden        | shown    |
//   | 99-9006 | Expired                       | hidden | hidden        | shown    |
//   | 99-9007 | Republished                   | SHOWN  | SHOWN         | shown    |
//   | 99-9008 | Expired - No Annual Verif.    | hidden | hidden        | shown    |
//   | 99-9009 | Paid (OTHER company)          | SHOWN  | HIDDEN        | shown    |
//
// Run:  node Scripts/smoke-prod/_views/cert-state-matrix.js
// Env:  BASE_URL (default http://localhost:5101), SMOKE_NO_SEED=1 (skip seed/teardown,
//       assume fixtures already present), SMOKE_KEEP_FIXTURES=1 (seed but don't tear down).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');
const { login, BASE } = require('../_lib/smoke-template');
// Config files here are JSONC (`//` comments) — JSON.parse chokes on them. See _lib/read-jsonc.js.
const { readJsonc } = require('../_lib/read-jsonc');

const NAV_TIMEOUT = parseInt(process.env.SMOKE_NAV_TIMEOUT || '60000', 10);
const API_TIMEOUT = parseInt(process.env.SMOKE_API_TIMEOUT || '20000', 10);
// Grid-settle budget. SETTLE_TIMEOUT only bounds the "card is absent" path — a present card
// returns immediately — so it can be generous without slowing the common case.
const SETTLE_TIMEOUT = parseInt(process.env.SMOKE_SETTLE_TIMEOUT || '15000', 10);
const SETTLE_POLL_MS = parseInt(process.env.SMOKE_SETTLE_POLL_MS || '250', 10);
const SETTLE_STABLE_SAMPLES = parseInt(process.env.SMOKE_SETTLE_STABLE || '3', 10);
const LIB = path.join(__dirname, '..', '_lib');
const RESULTS_DIR = path.join(__dirname, '..', '_results');
const NO_SEED = process.env.SMOKE_NO_SEED === '1';
const KEEP = process.env.SMOKE_KEEP_FIXTURES === '1';

const FIXTURES = [
  { number: '99-9001', state: 'Draft',                             pub: false, ext: false, int: true },
  { number: '99-9002', state: 'Client Approved - Not Paid',        pub: false, ext: false, int: true },
  { number: '99-9003', state: 'Client Approved - Confidential',    pub: false, ext: false, int: true },
  { number: '99-9004', state: 'Client Approved - Paid',            pub: true,  ext: true,  int: true },
  { number: '99-9005', state: 'Hold',                              pub: false, ext: false, int: true },
  { number: '99-9006', state: 'Expired',                           pub: false, ext: false, int: true },
  { number: '99-9007', state: 'Republished',                       pub: true,  ext: true,  int: true },
  { number: '99-9008', state: 'Expired - No Annual Verification',  pub: false, ext: false, int: true },
  { number: '99-9009', state: 'Client Approved - Paid (OTHER co)', pub: true,  ext: false, int: true },
];

const VIEWS = [
  { key: 'pub', label: 'Public',   url: '/Certificate/CertificateTileView',         role: 'public',   api: '/api/certificate-tiles' },
  { key: 'ext', label: 'External', url: '/Certificate/CertificateTileViewExternal', role: 'external', api: '/api/CertificateTileExternal' },
  // NB: these must match what each view's JS actually calls (wwwroot/js/certificate-tile-view*.js).
  // 'int' read '/api/CertificateTileInternal' until 2026-08-03 — a path this app has never served.
  // waitForResponse therefore never matched and every Internal cell burned the full 20s timeout,
  // stretching a 27-cell run to ~9 min and holding it open across the 60s expiration sweep.
  { key: 'int', label: 'Internal', url: '/Certificate/CertificateTileViewInternal', role: 'admin',    api: '/api/certificate-tiles-internal' },
];

// --- DB connection parsed from appsettings.Development.json (dev server uses this) -------
function sqlcmdArgs() {
  const cfgPath = path.resolve(__dirname, '..', '..', '..', 'appsettings.Development.json');
  const conn = readJsonc(cfgPath).ConnectionStrings.DefaultConnection;
  const kv = {};
  for (const part of conn.split(';')) {
    const i = part.indexOf('=');
    if (i > 0) kv[part.slice(0, i).trim().toLowerCase()] = part.slice(i + 1).trim();
  }
  return ['-S', kv['server'], '-d', kv['database'], '-U', kv['user id'], '-P', kv['password'], '-C', '-b'];
}

function runSql(file) {
  execFileSync('sqlcmd', [...sqlcmdArgs(), '-i', path.join(LIB, file)], { stdio: 'pipe' });
}

// Navigate to a filtered grid and wait for it to reach a DEFINITE state.
//
// Replaces a "wait for the API response, then sleep 500ms" implementation that was the source
// of this suite's flake (2026-08-03 runs: 27/0, 26/1, 22/5, 26/1). Every failure was
// "expected=shown actual=hidden" on the Internal view and never the reverse — i.e. a card the
// API HAD returned simply had not painted within the flat 500ms under load, so a render delay
// read as a visibility result. A fixed sleep cannot be both fast and safe; it has to be a
// condition. Two defects are fixed here:
//
//   1. The response listener was registered AFTER page.goto() resolved. With
//      waitUntil:'domcontentloaded' the tile API call can finish before the listener exists,
//      so the wait blocked for its full timeout on a response that would never come again.
//      It is now armed before navigation.
//   2. The flat sleep is replaced by settleGrid() below.
async function gotoAndSettle(page, url, apiPath, certNumber) {
  const apiResponse = page
    .waitForResponse((r) => r.url().includes(apiPath), { timeout: API_TIMEOUT })
    .catch(() => null);
  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
  await apiResponse;
  return settleGrid(page, certNumber);
}

// Resolve as soon as the answer is unambiguous, rather than after a fixed delay.
//
// Fast path: the target card is on screen. Presence is definitive — nothing can make a
// rendered card un-render — so return immediately. This keeps the common case quick.
//
// Slow path: "hidden" is only trustworthy once the grid has genuinely finished rendering, so
// require the initial skeleton gone, no lazy-load spinner running, and a card count that has
// stopped changing across consecutive samples. Deliberately asymmetric: we exit early on
// presence but never on absence, so the failure mode is a slower test, never a false "hidden"
// (and never a false "shown" — this only ever waits longer before reading absence).
// Returns 'rendered' once the grid has a trustworthy answer, or 'never-loaded' if the grid
// never painted at all (#initialLoadingState still up at the deadline — renderCertificates()
// never ran, so the tile API hung or errored).
//
// That distinction is the point. 18 of the 27 cells assert a certificate is HIDDEN, and a grid
// that never loaded is empty — so a hung API satisfies every one of them trivially. That is the
// same vacuous-pass trap DEF-326 fixed in the partner suite: emptiness masquerading as
// correctness. Absence only means "hidden" if the grid actually rendered.
async function settleGrid(page, certNumber) {
  const card = page.locator('#certificatesGrid .certificate-card', { hasText: certNumber });
  const deadline = Date.now() + SETTLE_TIMEOUT;
  let stableSamples = 0;
  let lastCount = -1;

  while (Date.now() < deadline) {
    if ((await card.count()) > 0) return 'rendered';

    const state = await page.evaluate(() => ({
      count: document.querySelectorAll('#certificatesGrid .certificate-card').length,
      // removeSkeleton() deletes #initialLoadingState once the first page has rendered, so
      // its presence means the grid has not painted yet. #loading-indicator is the lazy-load
      // spinner appended during subsequent fetches; #loadingIndicator is its static markup
      // twin in the internal/external views, which is CSS-toggled rather than removed.
      busy: !!document.getElementById('initialLoadingState')
        || !!document.getElementById('loading-indicator')
        || (() => {
          const el = document.getElementById('loadingIndicator');
          return !!el && el.offsetParent !== null;
        })(),
    }));

    stableSamples = (!state.busy && state.count === lastCount) ? stableSamples + 1 : 0;
    lastCount = state.count;
    if (stableSamples >= SETTLE_STABLE_SAMPLES) return 'rendered';

    await page.waitForTimeout(SETTLE_POLL_MS);
  }

  // Deadline hit. If the skeleton is still up the grid never rendered — report that rather
  // than letting an empty grid be read as a visibility result.
  const skeletonStillUp = await page.evaluate(() => !!document.getElementById('initialLoadingState'));
  return skeletonStillUp ? 'never-loaded' : 'rendered';
}

async function run() {
  if (!NO_SEED) {
    console.log('[state-matrix] seeding fixtures...');
    runSql('seed-state-fixtures.sql');
  }

  const records = [];
  const record = (view, fx, expected, actual) => {
    const status = expected === actual ? 'PASS' : 'FAIL';
    records.push({ view: view.label, number: fx.number, state: fx.state, expected, actual, status });
    console.log(`  [${status}] ${view.label.padEnd(8)} ${fx.number} ${fx.state.padEnd(34)} expected=${expected ? 'shown' : 'hidden'} actual=${actual ? 'shown' : 'hidden'}`);
  };

  const browser = await chromium.launch({ headless: process.env.SMOKE_HEADLESS !== '0' });
  try {
    for (const view of VIEWS) {
      const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
      const page = await ctx.newPage();
      await login(page, view.role);
      console.log(`\n[state-matrix] ${view.label} view (${view.role})`);
      for (const fx of FIXTURES) {
        try {
          const url = `${BASE}${view.url}?search=${encodeURIComponent(fx.number)}`;
          const outcome = await gotoAndSettle(page, url, view.api, fx.number);
          if (outcome === 'never-loaded') {
            // Not a visibility answer at all — the grid never rendered. Recorded as its own
            // status so it can never be mistaken for a clean "hidden" (see settleGrid).
            records.push({
              view: view.label, number: fx.number, state: fx.state,
              expected: fx[view.key], actual: null, status: 'FAIL',
              error: `grid never rendered within ${SETTLE_TIMEOUT}ms (tile API hung or errored) — result INCONCLUSIVE, not "hidden"`,
            });
            console.log(`  [FAIL] ${view.label.padEnd(8)} ${fx.number} ${fx.state.padEnd(34)} grid never rendered — INCONCLUSIVE (tile API hung)`);
            continue;
          }
          const shown = (await page.locator('#certificatesGrid .certificate-card', { hasText: fx.number }).count()) > 0;
          record(view, fx, fx[view.key], shown);
        } catch (e) {
          records.push({ view: view.label, number: fx.number, state: fx.state, expected: fx[view.key], actual: null, status: 'FAIL', error: e.message });
          console.log(`  [FAIL] ${view.label} ${fx.number} — ${e.message}`);
        }
      }
      await ctx.close();
    }
  } finally {
    await browser.close();
    if (!NO_SEED && !KEEP) {
      console.log('\n[state-matrix] tearing down fixtures...');
      try { runSql('teardown-state-fixtures.sql'); } catch (e) { console.error('[state-matrix] teardown FAILED:', e.message); }
    }
  }

  const pass = records.filter((r) => r.status === 'PASS').length;
  const fail = records.filter((r) => r.status === 'FAIL').length;
  if (!fs.existsSync(RESULTS_DIR)) fs.mkdirSync(RESULTS_DIR, { recursive: true });
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `cert-state-matrix-${stamp}.json`);
  fs.writeFileSync(outFile, JSON.stringify({ base: BASE, pass, fail, total: records.length, records }, null, 2));

  console.log('\n========== TIER 2 STATE-MATRIX ==========');
  console.log(`  ${pass} PASS / ${fail} FAIL  (${records.length} cells: ${VIEWS.length} views x ${FIXTURES.length} fixtures)`);
  console.log(`  STATUS: ${fail === 0 ? 'GREEN ✅' : 'RED ❌'}`);
  console.log(`  results -> ${path.relative(process.cwd(), outFile)}`);
  console.log('=========================================\n');

  // Shape matches the Tier 1 per-view smokes so run-all.js can consume it identically
  // (it reads .pass/.fail/.warn and retries any view returning fail > 0).
  return { viewId: 'cert-state-matrix', pass, fail, warn: 0, total: records.length, outFile };
}

module.exports = { run };

if (require.main === module) {
  run().then((r) => {
    process.exitCode = r.fail === 0 ? 0 : 1;
  });
}
