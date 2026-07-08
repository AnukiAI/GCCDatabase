// Local pre-deploy gate — runs the DETERMINISTIC script suites that back the pre-deploy checklist
// (docs/tracking/pre-deploy-agent-checklist.md) and returns ONE blocking PASS/FAIL. This is where
// the CRUD write-path suite is wired into the deploy gate, alongside the security suite and the
// Amazon contract diff. It's also the intended CI entrypoint for Phase 5 (regression strategy §8).
//
// It does NOT own the server lifecycle (that fragility bit us before — DEF-234/235). Start the app
// yourself first, then run this. Fails fast with guidance if the app or sqlcmd isn't ready.
//
// Gates run (each blocks on non-zero exit):
//   1. security      Scripts/smoke-prod/_security/run-all.js   (isolation / RBAC / IDOR / detail scoping)
//   2. crud          Scripts/smoke-prod/_crud/run-all.js       (cert/tag/company/product/user write paths)
//   3. contract      Scripts/smoke-prod/amazon-contract-diff.js (Amazon API shape — must be UNCHANGED)
//
// Opt-in extra (heavier, owns its own server restarts):
//   DEPLOY_GATE_INCLUDE_VIEWS=1  also runs _views/run-all.js (Tier-1 view smokes, SMOKE_BATCH_RESTART)
//
// Run:  (app already on :5101)  node Scripts/smoke-prod/deploy-gate.js
// Exit: 0 = all gates green (safe to deploy) · 1 = a gate FAILED (blocks) · 2 = preflight not met.

const { spawnSync, execFileSync } = require('child_process');
const http = require('http');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const HERE = __dirname;
const RESULTS_DIR = path.join(HERE, '_results');
const INCLUDE_VIEWS = process.env.DEPLOY_GATE_INCLUDE_VIEWS === '1';

function httpOk(url) {
  return new Promise((resolve) => {
    const req = http.get(url, (res) => { res.resume(); resolve(res.statusCode >= 200 && res.statusCode < 500); });
    req.on('error', () => resolve(false));
    req.setTimeout(5000, () => { req.destroy(); resolve(false); });
  });
}

function sqlcmdAvailable() {
  try { execFileSync('sqlcmd', ['-?'], { stdio: 'pipe' }); return true; } catch { return false; }
}

function runGate(name, scriptRelPath, env = {}) {
  console.log(`\n${'='.repeat(70)}\n▶ GATE: ${name}  (${scriptRelPath})\n${'='.repeat(70)}`);
  const started = Date.now();
  const res = spawnSync('node', [path.join(HERE, scriptRelPath)], {
    stdio: 'inherit',
    env: { ...process.env, ...env },
  });
  const code = res.status == null ? -1 : res.status;
  const secs = ((Date.now() - started) / 1000).toFixed(0);
  const pass = code === 0;
  console.log(`\n◀ GATE ${name}: ${pass ? 'PASS' : 'FAIL'} (exit ${code}, ${secs}s)`);
  return { name, script: scriptRelPath, exit: code, pass, seconds: Number(secs) };
}

(async () => {
  console.log(`\n╔${'═'.repeat(50)}╗\n║  PRE-DEPLOY GATE — deterministic suites          ║\n╚${'═'.repeat(50)}╝`);
  console.log(`Target: ${BASE}`);

  // ---- Preflight: the write-path/API gates need the app + dev DB reachable ----
  const appUp = await httpOk(BASE + '/');
  const sql = sqlcmdAvailable();
  if (!appUp || !sql) {
    console.error('\n✖ PREFLIGHT FAILED — cannot run the gate:');
    if (!appUp) console.error(`  • App not responding at ${BASE}. Start it first:  dotnet run --urls "${BASE}"`);
    if (!sql)   console.error('  • sqlcmd not on PATH (needed by the CRUD write-path suite).');
    process.exit(2);
  }
  console.log('Preflight OK — app is up, sqlcmd present.\n');

  // ---- Gates (order: cheapest security first, then write-path, then contract) ----
  const gates = [];
  gates.push(runGate('security', path.join('_security', 'run-all.js')));
  gates.push(runGate('crud', path.join('_crud', 'run-all.js')));
  // The Amazon contract diff needs the frozen real Amazon certs its baselines were captured from
  // (docs/tracking/api-samples/). It's a LOCAL gate — skip it where that dataset isn't present
  // (e.g. CI with the synthetic seed): set DEPLOY_GATE_SKIP_CONTRACT=1.
  if (process.env.DEPLOY_GATE_SKIP_CONTRACT === '1') {
    console.log('\n(skipping contract gate — DEPLOY_GATE_SKIP_CONTRACT=1; run it locally against the frozen Amazon dataset)');
  } else {
    gates.push(runGate('contract', 'amazon-contract-diff.js'));
  }
  if (INCLUDE_VIEWS) gates.push(runGate('views', path.join('_views', 'run-all.js'), { SMOKE_BATCH_RESTART: '1' }));

  // ---- Roll-up ----
  const failed = gates.filter((g) => !g.pass);
  const overall = failed.length === 0 ? 'GREEN — safe to deploy' : 'RED — deploy BLOCKED';
  console.log(`\n${'#'.repeat(70)}\n#  DEPLOY GATE ROLL-UP\n${'#'.repeat(70)}`);
  for (const g of gates) console.log(`  ${g.pass ? '✔' : '✖'} ${g.name.padEnd(10)} exit=${g.exit}  ${g.seconds}s`);
  console.log(`  ${'-'.repeat(40)}`);
  console.log(`  => ${overall}`);
  if (failed.length) console.log(`  Blocking gate(s): ${failed.map((g) => g.name).join(', ')} — fix before deploy, or waive + log in pre-deploy-agent-checklist.md.`);

  if (!fs.existsSync(RESULTS_DIR)) fs.mkdirSync(RESULTS_DIR, { recursive: true });
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `deploy-gate-${stamp}.json`);
  fs.writeFileSync(outFile, JSON.stringify({
    base: BASE, includeViews: INCLUDE_VIEWS,
    overall: failed.length === 0 ? 'GREEN' : 'RED', gates, finishedAt: new Date().toISOString(),
  }, null, 2));
  console.log(`  summary -> ${path.relative(process.cwd(), outFile)}\n`);

  process.exit(failed.length ? 1 : 0);
})();
