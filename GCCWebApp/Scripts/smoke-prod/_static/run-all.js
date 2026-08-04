// Static deploy-gate roll-up — Gate 9. Runs the selftest, then all four static checks, and writes
// one summary to ../_results/static-gate-{stamp}.json.
//
// SAFETY: every check here is read-only static analysis (plus read-only `git` queries). No server,
// no database, no network, no writes to the project. The only file written is the summary JSON.
// This gate cannot change application behaviour; the worst a bug in it can do is misreport.
//
// EXIT CODES
//   0  no FAILs (WARNs may be present - they are for reading, not blocking)
//   1  at least one FAIL, or the selftest failed
//
// The selftest runs FIRST and is fatal. A check that has silently stopped detecting anything looks
// exactly like a clean codebase, so a green gate is meaningless until the selftest passes.
//
// Run:  node Scripts/smoke-prod/_static/run-all.js

const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');
const { RESULTS_DIR } = require('./_lib/static-harness');

const CHECKS = ['tempdata-pairing', 'redirect-targets', 'orphan-views', 'unshipped-fixes'];

console.log('=== Static deploy gate (Gate 9) ===');
console.log('read-only: parses source + read-only git. No server, no DB, no writes to the project.');

// ---- selftest first, and it is fatal -------------------------------------------------------
let selftestOk = true;
try {
  execFileSync(process.execPath, [path.join(__dirname, 'selftest.js')], { stdio: 'inherit' });
} catch {
  selftestOk = false;
}
if (!selftestOk) {
  console.log('\n=== Static gate ABORTED: selftest failed ===');
  console.log('The checks cannot be trusted to detect anything. Fix the checks before reading their output.');
  process.exit(1);
}

const summaries = [];
for (const name of CHECKS) {
  try {
    summaries.push(require(`./${name}`).run());
  } catch (e) {
    console.error(`  [${name}] threw: ${e.message}`);
    summaries.push({ spec: name, pass: 0, fail: 1, warn: 0, info: 0, findings: [{ level: 'FAIL', msg: `check threw: ${e.message}` }] });
  }
}

const totals = summaries.reduce((a, s) => ({
  pass: a.pass + s.pass, fail: a.fail + s.fail, warn: a.warn + s.warn, info: a.info + (s.info || 0),
}), { pass: 0, fail: 0, warn: 0, info: 0 });

const status = totals.fail > 0 ? 'RED-FAIL' : totals.warn > 0 ? 'GREEN-WITH-WARNINGS' : 'GREEN';

console.log('\n=== Static gate roll-up ===');
for (const s of summaries) {
  console.log(`  ${s.spec.padEnd(20)} ${s.pass}P / ${s.fail}F / ${s.warn}W`);
}
console.log('  ----');
console.log(`  TOTAL  ${totals.pass}P / ${totals.fail}F / ${totals.warn}W  => ${status}`);
if (totals.fail > 0) {
  console.log('\n  FAILs (deploy blockers):');
  for (const s of summaries) {
    for (const f of s.findings.filter(x => x.level === 'FAIL')) console.log(`    [${s.spec}] ${f.msg}`);
  }
}

if (!fs.existsSync(RESULTS_DIR)) fs.mkdirSync(RESULTS_DIR, { recursive: true });
const stamp = new Date().toISOString().replace(/[:.]/g, '-');
const outFile = path.join(RESULTS_DIR, `static-gate-${stamp}.json`);
fs.writeFileSync(outFile, JSON.stringify({ status, totals, checks: summaries, finishedAt: new Date().toISOString() }, null, 2));
console.log(`  summary -> ${path.relative(process.cwd(), outFile)}\n`);

process.exit(totals.fail > 0 ? 1 : 0);
