// Tier 1 roll-up runner — runs all 10 critical-view smokes sequentially and writes a
// compact summary JSON to Scripts/smoke-prod/_results/summary-{timestamp}.json.
//
// Per the E2E Smoke Plan acceptance criteria:
//   - All 10 Tier 1 views return 0 FAIL across 4 roles x 3 viewports.
//   - Negative-path access (Public->Internal, External->Admin, etc.) returns the
//     expected 401/403 and is counted as PASS.
//   - The roll-up summary is <=30 lines.
//
// Run:  node Scripts/smoke-prod/_views/run-all.js
// Env:  BASE_URL (default http://localhost:5101), SMOKE_SCREENSHOTS=on-fail|always|never

const fs = require('fs');
const path = require('path');

// Order matches Tier 1 #1..#10 in docs/tracking/e2e-smoke-plan.md.
const VIEWS = [
  'home',
  'login',
  'cert-tile-public',
  'cert-tile-external',
  'cert-tile-internal',
  'cert-create',
  'cert-edit',
  'dashboard',
  'admin-user-mgmt',
  'admin-tags',
];

const RESULTS_DIR = path.join(__dirname, '..', '_results');

(async () => {
  const startedAt = new Date().toISOString();
  const perView = [];

  for (const v of VIEWS) {
    const mod = require(`./${v}.js`);
    // eslint-disable-next-line no-await-in-loop
    const r = await mod.run();
    perView.push({
      viewId: r.viewId,
      pass: r.pass,
      fail: r.fail,
      warn: r.warn,
      results: path.basename(r.outFile),
    });
  }

  const totals = perView.reduce(
    (acc, r) => ({ pass: acc.pass + r.pass, fail: acc.fail + r.fail, warn: acc.warn + r.warn }),
    { pass: 0, fail: 0, warn: 0 }
  );
  const failingViews = perView.filter((r) => r.fail > 0).map((r) => `${r.viewId} (${r.fail})`);

  const summary = {
    startedAt,
    finishedAt: new Date().toISOString(),
    base: process.env.BASE_URL || 'http://localhost:5101',
    viewsRun: perView.length,
    totals,
    green: totals.fail === 0,
    failingViews,
    perView: perView.map((r) => `${r.viewId}: ${r.pass}P/${r.fail}F/${r.warn}W`),
  };

  const stamp = startedAt.replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `summary-${stamp}.json`);
  fs.writeFileSync(outFile, JSON.stringify(summary, null, 2));

  console.log('\n========== TIER 1 SMOKE ROLL-UP ==========');
  for (const line of summary.perView) console.log('  ' + line);
  console.log('------------------------------------------');
  console.log(`  TOTAL: ${totals.pass} PASS / ${totals.fail} FAIL / ${totals.warn} WARN`);
  console.log(`  STATUS: ${summary.green ? 'GREEN ✅' : 'RED ❌ — ' + failingViews.join(', ')}`);
  console.log(`  summary -> ${path.relative(process.cwd(), outFile)}`);
  console.log('==========================================\n');

  process.exitCode = totals.fail === 0 ? 0 : 1;
})();
