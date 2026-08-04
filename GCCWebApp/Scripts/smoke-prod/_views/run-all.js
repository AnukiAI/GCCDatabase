// View roll-up runner — runs all 11 Tier 1 critical-view smokes plus the Tier 2
// cert-state-matrix (12 total) and writes a compact summary
// JSON to Scripts/smoke-prod/_results/summary-{timestamp}.json.
//
// Per the E2E Smoke Plan acceptance criteria:
//   - All 10 Tier 1 views return 0 FAIL across 4 roles x 3 viewports.
//   - Negative-path access (Public->Internal, External->Admin, etc.) returns the
//     expected 401/403 and is counted as PASS.
//   - The roll-up summary is <=30 lines.
//
// Two modes:
//   1. DEFAULT (single run) — you start the dev server; this just runs all 12 views
//      back-to-back with a warm-up, inter-view cooldown, and a single retry of any RED
//      view. Simple, but the local dev server can exhaust itself near the tail of a
//      sustained ~6-min run (it exits / refuses connections — environmental, not a view
//      defect; see docs/tracking/e2e-smoke-plan.md DEF-234 + observations_log).
//   2. BATCH-WITH-RESTART (SMOKE_BATCH_RESTART=1) — RECOMMENDED for a reliable single
//      green artifact. This script OWNS the server: it kills any running dotnet, starts a
//      fresh `dotnet run` before EACH batch of SMOKE_BATCH_SIZE views (default 5), waits
//      for readiness, then runs that batch. No batch accumulates enough sustained load to
//      fell the server. Leaves the last server running (healthy) at the end.
//
// Run:   node Scripts/smoke-prod/_views/run-all.js
//        SMOKE_BATCH_RESTART=1 node Scripts/smoke-prod/_views/run-all.js
// Env:   BASE_URL (default http://localhost:5101), SMOKE_SCREENSHOTS, SMOKE_COOLDOWN_MS,
//        SMOKE_BATCH_RESTART=1, SMOKE_BATCH_SIZE (default 5).

const fs = require('fs');
const path = require('path');
const { spawn, execSync } = require('child_process');

// Order matches Tier 1 #1..#11 in docs/tracking/e2e-smoke-plan.md, then Tier 2 last.
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
  'product-mgmt',
  // Tier 2 — seeds + tears down its own DB fixtures. Last on purpose so that seed/teardown
  // can't perturb the fixture-free Tier 1 views above. Added 2026-08-03: leaving it out meant
  // a routine roll-up skipped all 27 workflow-state visibility cells (Draft/Confidential/Hold
  // must not reach the public view) — the P0 control most worth never forgetting.
  'cert-state-matrix',
];

const RESULTS_DIR = path.join(__dirname, '..', '_results');
// Project dir containing the .csproj (…/GCCWebApp). __dirname = …/GCCWebApp/Scripts/smoke-prod/_views
const PROJECT_DIR = path.resolve(__dirname, '..', '..', '..');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const COOLDOWN_MS = parseInt(process.env.SMOKE_COOLDOWN_MS || '4000', 10);
const BATCH_RESTART = process.env.SMOKE_BATCH_RESTART === '1';
const BATCH_SIZE = parseInt(process.env.SMOKE_BATCH_SIZE || '5', 10);

const sleep = (ms) => new Promise((res) => setTimeout(res, ms));

// Probe one URL with a short per-request timeout so a hung server can't block us.
async function probe(url, timeoutMs = 5000) {
  try {
    const resp = await fetch(url, { method: 'GET', signal: AbortSignal.timeout(timeoutMs) });
    return resp.status;
  } catch {
    return 0;
  }
}

// Warm the dev server before a batch. A cold first hit can stall ~20-25s (heavy stats
// pages + EF query-compile, per DEF-234), which otherwise shows up as a spurious timeout
// on whichever view runs first.
async function warmUp() {
  for (const u of ['/', '/Account/Login', '/api/certificate-tiles?page=1&pageSize=1']) {
    // eslint-disable-next-line no-await-in-loop
    await probe(BASE + u, 30000);
  }
}

// ----- Server lifecycle (batch mode only) -------------------------------------------

function killServer() {
  try {
    if (process.platform === 'win32') {
      execSync('taskkill /F /IM dotnet.exe', { stdio: 'ignore' });
    } else {
      execSync('pkill -f "dotnet run"', { stdio: 'ignore' });
    }
  } catch {
    /* none running — fine */
  }
}

function startServer(batchNum) {
  const logPath = path.join(RESULTS_DIR, `_batch-server-${batchNum}.log`);
  const out = fs.openSync(logPath, 'a');
  const child = spawn('dotnet', ['run', '--urls', BASE], {
    cwd: PROJECT_DIR,
    detached: true,
    stdio: ['ignore', out, out],
    windowsHide: true,
  });
  child.unref();
  return logPath;
}

// Poll until the server answers 200, or give up. dotnet run cold-build can take a while.
async function waitForServer(timeoutMs = 180000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    // eslint-disable-next-line no-await-in-loop
    const code = await probe(BASE + '/api/certificate-tiles?page=1&pageSize=1', 5000);
    if (code === 200) return true;
    // eslint-disable-next-line no-await-in-loop
    await sleep(2000);
  }
  return false;
}

async function restartServer(batchNum) {
  console.log(`[run-all] (batch ${batchNum}) restarting dev server...`);
  killServer();
  await sleep(3000); // let the port release
  const logPath = startServer(batchNum);
  const ready = await waitForServer();
  if (!ready) {
    console.error(`[run-all] WARNING: server did not become ready for batch ${batchNum} (see ${path.basename(logPath)}). Views will surface real failures.`);
  } else {
    console.log(`[run-all] (batch ${batchNum}) server ready.`);
  }
}

// ----- View execution ----------------------------------------------------------------

// Run one view with a single retry of a RED result (transient load clears; real failures
// reproduce). Returns the perView record.
async function runView(v) {
  const mod = require(`./${v}.js`);
  let r = await mod.run();
  if (r.fail > 0) {
    console.log(`[run-all] ${v} returned ${r.fail} FAIL — cooling down ${COOLDOWN_MS}ms and retrying once...`);
    await sleep(COOLDOWN_MS);
    await warmUp();
    const retry = await mod.run();
    r = { ...retry, retried: true, firstFail: r.fail };
  }
  await sleep(COOLDOWN_MS);
  return {
    viewId: r.viewId,
    pass: r.pass,
    fail: r.fail,
    warn: r.warn,
    retried: r.retried || false,
    results: path.basename(r.outFile),
  };
}

function chunk(arr, size) {
  const out = [];
  for (let i = 0; i < arr.length; i += size) out.push(arr.slice(i, i + size));
  return out;
}

(async () => {
  const startedAt = new Date().toISOString();
  const perView = [];

  if (BATCH_RESTART) {
    const batches = chunk(VIEWS, BATCH_SIZE);
    console.log(`[run-all] BATCH-WITH-RESTART mode: ${batches.length} batches of up to ${BATCH_SIZE} views (this script manages the server).`);
    for (let b = 0; b < batches.length; b++) {
      // eslint-disable-next-line no-await-in-loop
      await restartServer(b + 1);
      // eslint-disable-next-line no-await-in-loop
      await warmUp();
      for (const v of batches[b]) {
        // eslint-disable-next-line no-await-in-loop
        perView.push(await runView(v));
      }
    }
  } else {
    console.log('[run-all] single-run mode (you manage the server). Warming up...');
    await warmUp();
    for (const v of VIEWS) {
      // eslint-disable-next-line no-await-in-loop
      perView.push(await runView(v));
    }
  }

  const totals = perView.reduce(
    (acc, r) => ({ pass: acc.pass + r.pass, fail: acc.fail + r.fail, warn: acc.warn + r.warn }),
    { pass: 0, fail: 0, warn: 0 }
  );
  const failingViews = perView.filter((r) => r.fail > 0).map((r) => `${r.viewId} (${r.fail})`);

  const summary = {
    startedAt,
    finishedAt: new Date().toISOString(),
    base: BASE,
    mode: BATCH_RESTART ? `batch-restart(${BATCH_SIZE})` : 'single-run',
    viewsRun: perView.length,
    totals,
    green: totals.fail === 0,
    failingViews,
    perView: perView.map((r) => `${r.viewId}: ${r.pass}P/${r.fail}F/${r.warn}W${r.retried ? ' (retried)' : ''}`),
    retriedViews: perView.filter((r) => r.retried).map((r) => r.viewId),
  };

  const stamp = startedAt.replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `summary-${stamp}.json`);
  fs.writeFileSync(outFile, JSON.stringify(summary, null, 2));

  console.log('\n========== TIER 1 SMOKE ROLL-UP ==========');
  console.log(`  mode: ${summary.mode}`);
  for (const line of summary.perView) console.log('  ' + line);
  console.log('------------------------------------------');
  console.log(`  TOTAL: ${totals.pass} PASS / ${totals.fail} FAIL / ${totals.warn} WARN`);
  console.log(`  STATUS: ${summary.green ? 'GREEN ✅' : 'RED ❌ — ' + failingViews.join(', ')}`);
  console.log(`  summary -> ${path.relative(process.cwd(), outFile)}`);
  if (BATCH_RESTART) console.log('  (batch mode left the last dev server running)');
  console.log('==========================================\n');

  process.exitCode = totals.fail === 0 ? 0 : 1;
})();
