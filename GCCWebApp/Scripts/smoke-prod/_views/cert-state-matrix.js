// E2E Smoke Tier 2 — certificate workflow-state visibility matrix.
//
// Verifies, per the 8-state lifecycle, which certificates each tile view shows vs hides.
// This is the cross-view "state" dimension the per-view Tier 1 smokes don't cover.
//
// SELF-CONTAINED: seeds fixtures (sqlcmd → seed-state-fixtures.sql), drives the 3 tile
// views per their required role, then tears the fixtures down (always, even on failure).
// Kept separate from run-all.js on purpose — Tier 1 is fixture-free and fast; Tier 2 needs
// seeded DB rows + teardown, so it's its own opt-in command.
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

const NAV_TIMEOUT = parseInt(process.env.SMOKE_NAV_TIMEOUT || '60000', 10);
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
  { key: 'int', label: 'Internal', url: '/Certificate/CertificateTileViewInternal', role: 'admin',    api: '/api/CertificateTileInternal' },
];

// --- DB connection parsed from appsettings.Development.json (dev server uses this) -------
function sqlcmdArgs() {
  const cfgPath = path.resolve(__dirname, '..', '..', '..', 'appsettings.Development.json');
  const conn = JSON.parse(fs.readFileSync(cfgPath, 'utf8')).ConnectionStrings.DefaultConnection;
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

async function settleAfterSearch(page, apiPath) {
  const resp = page.waitForResponse((r) => r.url().includes(apiPath), { timeout: 20000 }).catch(() => null);
  await resp;
  await page.waitForTimeout(500); // let the grid render the filtered result
}

(async () => {
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
          await page.goto(`${BASE}${view.url}?search=${encodeURIComponent(fx.number)}`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
          await settleAfterSearch(page, view.api);
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

  process.exitCode = fail === 0 ? 0 : 1;
})();
