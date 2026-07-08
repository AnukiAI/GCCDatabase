// CRUD write-path suite roll-up. Runs every CRUD round-trip spec and writes a compact summary to
// ../_results/crud-summary-{stamp}.json. See docs/qa/regression-e2e-strategy.md §5 (T3).
//
// ⚠ WRITES to the dev DB — fixtures only, self-cleaning (see _crud/README.md). Requires the dev
// server on :5101 and sqlcmd. Any FAIL/BLOCKED exits non-zero.
//
// Run:  node Scripts/smoke-prod/_crud/run-all.js

const fs = require('fs');
const path = require('path');

const SPECS = [
  'tag-crud-roundtrip',
  'company-crud-roundtrip',
  'product-crud-roundtrip',
  'user-crud-roundtrip',
  'cert-crud-roundtrip',
];
const RESULTS_DIR = path.join(__dirname, '..', '_results');

(async () => {
  console.log('=== CRUD write-path suite (writes fixtures, self-cleaning) ===');
  const summaries = [];
  for (const spec of SPECS) {
    try {
      const s = await require(`./${spec}`).run();
      summaries.push({ spec, pass: s.pass, fail: s.fail, warn: s.warn, blocked: s.blocked });
    } catch (e) {
      console.error(`  [${spec}] threw: ${e.message}`);
      summaries.push({ spec, pass: 0, fail: 1, warn: 0, blocked: 0, error: e.message });
    }
  }

  const totals = summaries.reduce((a, s) => ({
    pass: a.pass + s.pass, fail: a.fail + s.fail, warn: a.warn + s.warn, blocked: a.blocked + s.blocked,
  }), { pass: 0, fail: 0, warn: 0, blocked: 0 });
  const status = totals.fail > 0 ? 'RED-FAIL' : totals.blocked > 0 ? 'BLOCKED' : 'GREEN';

  console.log('\n=== CRUD suite roll-up ===');
  for (const s of summaries) console.log(`  ${s.spec.padEnd(22)} ${s.pass}P / ${s.fail}F / ${s.warn}W / ${s.blocked}B`);
  console.log(`  ----`);
  console.log(`  TOTAL  ${totals.pass}P / ${totals.fail}F / ${totals.warn}W / ${totals.blocked}B  => ${status}`);

  if (!fs.existsSync(RESULTS_DIR)) fs.mkdirSync(RESULTS_DIR, { recursive: true });
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `crud-summary-${stamp}.json`);
  fs.writeFileSync(outFile, JSON.stringify({ status, totals, specs: summaries, finishedAt: new Date().toISOString() }, null, 2));
  console.log(`  summary -> ${path.relative(process.cwd(), outFile)}\n`);
  process.exit(totals.fail > 0 || totals.blocked > 0 ? 1 : 0);
})();
