// whats-deployed.js — answer "which build is actually live on the test env?"
//
// WHY THIS EXISTS
// The registers assert "deployed 2026-08-04" for several defects, but nobody could say which
// artifact that was. Register text is a claim; this script is evidence. It is READ-ONLY: it
// passes the test-env gate, issues GETs, and writes nothing to the server.
//
// TWO INDEPENDENT SIGNALS, because each alone can mislead:
//
//  1. ROUTE PRESENCE (proves what is in the DLL — views/controllers compile into GCCWebApp.dll,
//     so no static asset can tell you about them). DocumentController was deleted in the current
//     batch. Routing runs BEFORE authorization, so an anonymous GET to one of its routes gives:
//        302 -> /Account/Login   = controller still present  => batch NOT deployed
//        404                     = controller gone           => batch IS deployed
//     Both are non-destructive GETs against [Authorize] endpoints we never authenticate to.
//
//  2. STATIC ASSET HASHES (proves which wwwroot generation is live). Compared by md5 against the
//     working tree. Static files are copied verbatim by publish, so equal hash == same file shipped.
//
// The two can disagree — a deploy that extracted the DLL but not wwwroot, or vice versa, is exactly
// the sort of half-applied state worth knowing about, so they are reported separately, never merged.
//
// Usage:  node Scripts/smoke-prod/whats-deployed.js [--base https://gccwebapp.com]

const { chromium, request: pwRequest } = require('playwright');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const { detectGateMode, passGate } = require('./_lib/test-env-gate');

const argBase = (() => {
  const i = process.argv.indexOf('--base');
  return i > -1 ? process.argv[i + 1] : 'https://gccwebapp.com';
})();
const BASE = argBase.replace(/\/$/, '');
const PROJECT_ROOT = path.resolve(__dirname, '..', '..');

// Routes that exist ONLY in the pre-DEF-337 build. Anonymous GET; never authenticated, never POSTed.
const REMOVED_ROUTES = [
  '/Document/GetDocuments?certificateId=1',
  '/Document/Preview/1',
  '/Document/Download/1',
];

// A control: a route that exists in BOTH builds. If this does not behave like a live authorized
// route, the probe is talking to something other than the app (gate page, WAF, error handler) and
// every other result in this run is meaningless.
const CONTROL_ROUTE = '/Certificate/Create';

const ASSETS = [
  'wwwroot/js/main.js',
  'wwwroot/js/certificate-create.js',
  'wwwroot/js/certificate-tile-view.js',
  'wwwroot/js/certificate-tile-view-internal.js',
  'wwwroot/js/certificate-tile-view-external.js',
  'wwwroot/js/userManagement.js',            // DEF-387 - lets a JS-only deploy be verified
  'wwwroot/js/product-id-manager.js',        // DEF-389 - same reason; JS-only, no DLL
  'wwwroot/css/certificatetileview.css',
  'wwwroot/css/certificate-autocomplete.css',
];

const md5 = (buf) => crypto.createHash('md5').update(buf).digest('hex');

function classifyRoute(status, location) {
  if (status === 404) return { verdict: 'GONE', note: 'route does not exist in the deployed DLL' };
  if (status >= 300 && status < 400 && /\/Account\/Login/i.test(location || '')) {
    return { verdict: 'PRESENT', note: `route exists, redirected to login (${status})` };
  }
  if (status === 200) return { verdict: 'AMBIGUOUS', note: '200 anonymously — likely the gate page, not the app' };
  return { verdict: 'AMBIGUOUS', note: `status=${status} location="${location || 'none'}"` };
}

(async () => {
  console.log(`\n=== What is actually deployed at ${BASE}? ===`);
  console.log('read-only: GETs only, no writes, no app login\n');

  // NOTE: detectGateMode/passGate call `.newContext()` on what they are handed, so they take the
  // Playwright `request` MODULE, not an already-created APIRequestContext. Passing a context here
  // fails with "request.newContext is not a function".
  let browser;
  try {
    const mode = await detectGateMode(pwRequest, BASE);
    console.log(`gate mode: ${mode.mode} — ${mode.detail}`);
    let storageState;
    if (mode.mode === 'cookie') {
      const gate = await passGate(pwRequest, BASE);
      if (!gate.ok) {
        console.log(`\n  ABORT — could not pass the test-env gate: ${gate.detail}`);
        process.exit(2);
      }
      storageState = gate.state;
      console.log('gate: passed\n');
    } else {
      console.log('gate: not a cookie gate; continuing\n');
    }

    browser = await chromium.launch();
    const ctx = await browser.newContext({ storageState });

    // ---- signal 1: route presence (the DLL) --------------------------------
    console.log('--- signal 1: DLL contents, via route presence ---');
    const ctrl = await ctx.request.get(`${BASE}${CONTROL_ROUTE}`, { maxRedirects: 0, failOnStatusCode: false });
    const ctrlClass = classifyRoute(ctrl.status(), ctrl.headers()['location']);
    console.log(`  [control] ${CONTROL_ROUTE} -> ${ctrlClass.verdict} (${ctrlClass.note})`);
    if (ctrlClass.verdict !== 'PRESENT') {
      console.log('\n  ABORT — the control route did not behave like a live authorized route.');
      console.log('  Every route verdict below would be unreliable, so none are reported.');
      console.log('  (This is the DEF-341 failure mode: grading the gate page as the application.)');
      process.exit(2);
    }

    const verdicts = [];
    for (const r of REMOVED_ROUTES) {
      const res = await ctx.request.get(`${BASE}${r}`, { maxRedirects: 0, failOnStatusCode: false });
      const c = classifyRoute(res.status(), res.headers()['location']);
      verdicts.push(c.verdict);
      console.log(`  ${r} -> ${c.verdict} (${c.note})`);
    }

    const allGone = verdicts.every((v) => v === 'GONE');
    const allPresent = verdicts.every((v) => v === 'PRESENT');
    console.log('');
    if (allGone) {
      console.log('  => DLL VERDICT: the DEF-337 batch (DocumentController removal) IS deployed.');
    } else if (allPresent) {
      console.log('  => DLL VERDICT: the DEF-337 batch is NOT deployed — live DLL predates it.');
    } else {
      console.log('  => DLL VERDICT: INCONCLUSIVE — routes disagree; do not infer a build from this.');
    }

    // ---- signal 2: static assets (wwwroot) ---------------------------------
    console.log('\n--- signal 2: wwwroot generation, via asset md5 vs working tree ---');
    let same = 0, diff = 0, missing = 0;
    for (const rel of ASSETS) {
      const localPath = path.join(PROJECT_ROOT, rel);
      if (!fs.existsSync(localPath)) { console.log(`  [skip] ${rel} — not in working tree`); continue; }
      const localHash = md5(fs.readFileSync(localPath));
      const urlPath = '/' + rel.replace(/^wwwroot\//, '');
      const res = await ctx.request.get(`${BASE}${urlPath}`, { failOnStatusCode: false });
      if (res.status() !== 200) { missing++; console.log(`  [MISS] ${urlPath} -> HTTP ${res.status()}`); continue; }
      const liveHash = md5(await res.body());
      if (liveHash === localHash) { same++; console.log(`  [SAME] ${urlPath}`); }
      else { diff++; console.log(`  [DIFF] ${urlPath}  live=${liveHash.slice(0, 12)} tree=${localHash.slice(0, 12)}`); }
    }
    console.log(`\n  => ASSET VERDICT: ${same} same / ${diff} differ / ${missing} unreachable`);
    if (diff === 0 && missing === 0) console.log('     live wwwroot matches the working tree exactly.');
    else if (same === 0) console.log('     live wwwroot is an entirely different generation from the working tree.');
    else console.log('     PARTIAL — some assets current, some stale. Treat the last deploy as half-applied.');

    console.log('\nNote: these two signals are deliberately not combined into one number. A DLL/wwwroot');
    console.log('split is a real deployment state and averaging it away is how it stays hidden.\n');

    await ctx.close();
  } finally {
    if (browser) await browser.close();
  }
})().catch((e) => { console.error('probe failed:', e.message); process.exit(1); });
