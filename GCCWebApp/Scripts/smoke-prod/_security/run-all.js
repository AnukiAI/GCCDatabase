// Security-suite roll-up — runs every maintained security spec and writes one compact summary
// to ../_results/security-summary-{stamp}.json. This is the T2/T4 security gate from
// docs/qa/regression-e2e-strategy.md §5.
//
// Any FAIL is a HARD deploy block (a scoping/RBAC/IDOR FAIL is never advisory). BLOCKED means a
// spec couldn't run (test account/data missing) — surfaced loudly but exits non-zero so it can't
// masquerade as green.
//
// Requires the dev server already running (read-only suite; it does not own the server lifecycle).
// Run:  node Scripts/smoke-prod/_security/run-all.js

const fs = require('fs');
const path = require('path');

const SPECS = ['partner-isolation', 'rbac-matrix', 'idor-download', 'cert-detail-scoping'];
const RESULTS_DIR = path.join(__dirname, '..', '_results');

(async () => {
  console.log('=== Security regression suite ===');
  const summaries = [];
  for (const spec of SPECS) {
    const mod = require(`./${spec}`);
    try {
      const s = await mod.run();
      summaries.push({ spec, pass: s.pass, fail: s.fail, warn: s.warn, blocked: s.blocked });
    } catch (e) {
      console.error(`  [${spec}] threw: ${e.message}`);
      summaries.push({ spec, pass: 0, fail: 1, warn: 0, blocked: 0, error: e.message });
    }
  }

  const totals = summaries.reduce((a, s) => ({
    pass: a.pass + s.pass, fail: a.fail + s.fail, warn: a.warn + s.warn, blocked: a.blocked + s.blocked,
  }), { pass: 0, fail: 0, warn: 0, blocked: 0 });

  const status = totals.fail > 0 ? 'RED-FAIL'
    : totals.blocked > 0 ? 'BLOCKED'
    : 'GREEN';

  console.log('\n=== Security suite roll-up ===');
  for (const s of summaries) {
    console.log(`  ${s.spec.padEnd(20)} ${s.pass}P / ${s.fail}F / ${s.warn}W / ${s.blocked}B`);
  }
  console.log(`  ----`);
  console.log(`  TOTAL  ${totals.pass}P / ${totals.fail}F / ${totals.warn}W / ${totals.blocked}B  => ${status}`);

  if (!fs.existsSync(RESULTS_DIR)) fs.mkdirSync(RESULTS_DIR, { recursive: true });
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `security-summary-${stamp}.json`);
  fs.writeFileSync(outFile, JSON.stringify({ status, totals, specs: summaries, finishedAt: new Date().toISOString() }, null, 2));
  console.log(`  summary -> ${path.relative(process.cwd(), outFile)}\n`);

  // Exit non-zero on FAIL or BLOCKED — the gate must not go green on unrun specs.
  process.exit(totals.fail > 0 || totals.blocked > 0 ? 1 : 0);
})();
