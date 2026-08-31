// Amazon API contract regression — confirm the response Amazon receives has NOT changed shape.
//
// Compares the STRUCTURE (field paths, array-vs-object nesting, leaf types) of each Amazon-facing
// response against the saved pre-CR-171 baseline samples in docs/tracking/api-samples/. Data VALUES
// are ignored (the dataset differs run-to-run) — only the contract shape is asserted. Any added,
// removed, renamed, or retyped field is reported as a contract change.
//
// Per [[feedback_amazon_api_immutable]] the /api/v1/certificates* shape is frozen; CR-171 was only
// allowed to change WHICH partner value is returned (caller-scoped), never the shape. This test is
// the mechanical proof of that.
//
// Endpoints vs baselines:
//   GS1 list      /api/v1/certificates/gs1                  <-> 2026-04-30_amazon_gs1.json
//   GS1 single    /api/v1/certificates/gs1/26-0146          <-> 2026-04-30_amazon_gs1_single.json
//   Standard list /api/v1/certificates?includeProductIds=1  <-> 2026-04-24_amazon_standard.json
//
// Self-contained: admin-cookie login -> Amazon partner key -> cookie-free partner calls.
const { chromium, request } = require('playwright');
const { detectGateMode, passGate } = require('./_lib/test-env-gate');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const SAMPLE_DIR = path.join(__dirname, '..', '..', 'docs', 'tracking', 'api-samples');

let pass = 0, fail = 0;
// Counted separately from `fail` on purpose: a request that never returned proves nothing about the
// contract, and folding it into the failure count is what produced a false "CONTRACT CHANGED".
let timeouts = 0;
function check(name, ok, detail) {
  if (ok) { pass++; console.log(`  PASS  ${name}${detail ? '  — ' + detail : ''}`); }
  else { fail++; console.log(`  FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
}

// Build the set of structural "paths" for a JSON value. Arrays collapse to a single [] segment and
// we UNION the shapes of every element (so optional/heterogeneous rows are all captured). Leaves
// record their type, with null treated as a wildcard (a nullable field is contract-compatible
// whether it currently holds null or a value).
function shapePaths(node, prefix, acc) {
  if (Array.isArray(node)) {
    acc.add(prefix + '[]');
    for (const el of node) shapePaths(el, prefix + '[]', acc);
  } else if (node && typeof node === 'object') {
    for (const k of Object.keys(node)) {
      const p = prefix ? `${prefix}.${k}` : k;
      acc.add(p);
      shapePaths(node[k], p, acc);
    }
  } else {
    // leaf — record type so an array<->object<->scalar change is caught; null is a wildcard
    const t = node === null ? 'null' : typeof node;
    acc.add(`${prefix}::${t}`);
  }
  return acc;
}

// Two leaf-type tokens for the same path are compatible if either side is null (nullable field).
// Returns {added, removed} path lists after collapsing null-wildcard leaf types.
function diffShapes(baseline, live) {
  const norm = (set) => {
    // index leaf paths by their base path so null vs typed don't false-diff
    const containers = new Set();
    const leafTypes = new Map(); // path -> Set(types)
    for (const tok of set) {
      const i = tok.indexOf('::');
      if (i === -1) { containers.add(tok); }
      else {
        const base = tok.slice(0, i), t = tok.slice(i + 2);
        if (!leafTypes.has(base)) leafTypes.set(base, new Set());
        leafTypes.get(base).add(t);
      }
    }
    return { containers, leafTypes };
  };
  const b = norm(baseline), l = norm(live);

  const added = [], removed = [];
  // container/structural paths
  for (const c of l.containers) if (!b.containers.has(c)) added.push(c);
  for (const c of b.containers) if (!l.containers.has(c)) removed.push(c);
  // leaf presence
  for (const p of l.leafTypes.keys()) if (!b.leafTypes.has(p) && !b.containers.has(p)) added.push(p);
  for (const p of b.leafTypes.keys()) if (!l.leafTypes.has(p) && !l.containers.has(p)) removed.push(p);
  // leaf type changes (ignore when either side is null = nullable)
  const typeChanges = [];
  for (const p of b.leafTypes.keys()) {
    if (!l.leafTypes.has(p)) continue;
    const bt = b.leafTypes.get(p), lt = l.leafTypes.get(p);
    const bset = new Set([...bt].filter(t => t !== 'null'));
    const lset = new Set([...lt].filter(t => t !== 'null'));
    if (bset.size && lset.size) {
      for (const t of lset) if (!bset.has(t)) typeChanges.push(`${p}: ${[...bset]} -> ${[...lset]}`);
    }
  }
  return { added: added.sort(), removed: removed.sort(), typeChanges: [...new Set(typeChanges)].sort() };
}

(async () => {
  const browser = await chromium.launch();

  // The deployed test environment sits behind a COOKIE gate (`TestEnvironmentGate.Mode=Cookie`):
  // every path 302s to /__gate. Without passing it first, `page.goto('/Account/Login')` lands on
  // the gate form, `#Email` never appears, and this script reports **CONTRACT CHANGED** — a
  // completely false result caused by never reaching the API at all. Reuse the shared helper
  // rather than re-deriving it; a private copy is exactly how DEF-341's false security regression
  // happened. On localhost the gate is absent, detectGateMode returns 'open', and this is a no-op.
  // Both helpers take the Playwright `request` MODULE (they call request.newContext() themselves),
  // not an already-created context.
  let gateState;
  const mode = await detectGateMode(request, BASE);
  if (mode.mode === 'cookie') {
    const gate = await passGate(request, BASE);
    if (!gate.ok) {
      console.log(`FATAL: could not pass the test-env gate — ${gate.detail}`);
      console.log('This is a GATE failure, not a contract failure. Set GATE_PASS if it rotated.');
      process.exit(2);
    }
    gateState = gate.state;
    console.log(`  gate: passed cookie gate at ${BASE}`);
  } else {
    console.log(`  gate: ${mode.mode} (${mode.detail})`);
  }

  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, storageState: gateState });
  const page = await ctx.newPage();
  // Cookie-free of the APP's auth = how Amazon calls, but it still needs the gate cookie to
  // reach the deployed host at all. The X-API-Key auth under test is unaffected by that cookie.
  const partnerReq = await request.newContext({ ignoreHTTPSErrors: true, storageState: gateState });

  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    // Credentials are overridable because the hardcoded pair is env-specific and silently
    // wrong on one of the two targets: `admin` had its password reset on the TEST environment
    // on 2026-08-19 (DEF-263 verification) and only Shawn knows the new value, while on the dev
    // database it is unchanged. A failed login here does not announce itself — the page simply
    // re-renders as HTTP 200 and the run fails later at "got Amazon partner key", which reads
    // like a contract/API fault rather than a credential one. Use admin.uat against test.
    await page.locator('#Email').fill(process.env.ADMIN_USER || 'admin');
    await page.locator('#Password').fill(process.env.ADMIN_PASS || 'TestPass123!');
    await Promise.all([page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(700);

    const plist = await ctx.request.get(`${BASE}/api/admin/partner/list`, { headers: { Accept: 'application/json' } });
    const amazon = ((await plist.json()).data || []).find(p => p.name === 'Amazon');
    const gen = await ctx.request.post(`${BASE}/api/admin/partner/${amazon.id}/apikey`, {
      headers: { 'Content-Type': 'application/json' }, data: { description: 'amazon-contract-diff' } });
    const key = (await gen.json()).data.apiKey;
    check('setup: got Amazon partner key', !!key && key.startsWith('gcc_partner_'));

    // A SLOW response must never be reported as a contract change. Measured on localhost against
    // the migrated dataset (5,702 certs) on 2026-08-24: /certificates/gs1?pageSize=100 took 120.3s
    // on its first call and 120.2s on an immediate repeat, then 2.4s once warm — while pageSize=50
    // stayed at 0.6s throughout. At the old 60s timeout the request threw, the throw landed in the
    // outer catch, and the run printed "CONTRACT CHANGED" having never compared a single field.
    // That is a false deploy blocker on the one contract the client cannot afford noise about
    // ([[feedback_amazon_api_immutable]]), so a timeout is now classified as its OWN outcome:
    // retried once, then reported as INCONCLUSIVE with exit 3 — never as a contract verdict.
    const REQ_TIMEOUT = Number(process.env.REQ_TIMEOUT_MS || 180000);

    const getLive = async (url) => {
      const started = Date.now();
      try {
        const r = await partnerReq.get(`${BASE}${url}`,
          { headers: { 'X-API-Key': key, Accept: 'application/json' }, timeout: REQ_TIMEOUT });
        let body = null; try { body = await r.json(); } catch (e) {}
        return { status: r.status(), body, ms: Date.now() - started };
      } catch (e) {
        if (!/Timeout .*exceeded/i.test(e.message)) throw e;
        return { status: 0, body: null, ms: Date.now() - started, timedOut: true };
      }
    };

    const comparisons = [
      { name: 'GS1 list',      url: '/api/v1/certificates/gs1?pageSize=100',                  baseline: '2026-04-30_amazon_gs1.json' },
      { name: 'GS1 single',    url: '/api/v1/certificates/gs1/26-0146',                       baseline: '2026-04-30_amazon_gs1_single.json' },
      { name: 'Standard list', url: '/api/v1/certificates?includeProductIds=true&pageSize=100', baseline: '2026-04-24_amazon_standard.json' },
    ];

    for (const c of comparisons) {
      let live = await getLive(c.url);
      if (live.timedOut) {
        // One retry, because the slow path above was reproducibly the FIRST execution of a given
        // query shape. If the retry also times out the endpoint is genuinely unusable and that is
        // a performance finding to report as such — not a shape verdict.
        console.log(`  RETRY ${c.name}: timed out after ${(live.ms / 1000).toFixed(1)}s — retrying once`);
        live = await getLive(c.url);
      }
      if (live.timedOut) {
        timeouts++;
        console.log(`  TIMEOUT ${c.name} — ${(live.ms / 1000).toFixed(1)}s, twice. NOT a contract result; nothing was compared.`);
        continue;
      }
      check(`${c.name}: live → 200`, live.status === 200, `status ${live.status} in ${(live.ms / 1000).toFixed(1)}s`);
      if (live.status !== 200) continue;

      const baselineRaw = JSON.parse(fs.readFileSync(path.join(SAMPLE_DIR, c.baseline), 'utf8'));
      const baseShape = shapePaths(baselineRaw, '', new Set());
      const liveShape = shapePaths(live.body, '', new Set());
      const { added, removed, typeChanges } = diffShapes(baseShape, liveShape);

      const clean = added.length === 0 && removed.length === 0 && typeChanges.length === 0;
      check(`${c.name}: contract shape unchanged vs ${c.baseline}`, clean,
        clean ? 'identical structure' : `${added.length} added / ${removed.length} removed / ${typeChanges.length} retyped`);
      if (!clean) {
        if (added.length)    console.log('       + ADDED (new fields Amazon would now see):   ' + JSON.stringify(added));
        if (removed.length)  console.log('       - REMOVED (fields Amazon expected, now gone): ' + JSON.stringify(removed));
        if (typeChanges.length) console.log('       ~ RETYPED: ' + JSON.stringify(typeChanges));
      }
    }

  } catch (e) {
    fail++; console.log(`FATAL: ${e.message}`);
  } finally {
    await partnerReq.dispose().catch(() => {});
    await ctx.close();
    await browser.close();
  }

  console.log(`\n=== Amazon contract diff: ${pass} passed, ${fail} failed, ${timeouts} timed out ===`);
  if (fail) {
    console.log('CONTRACT CHANGED — review diffs above.');
    process.exit(1);
  }
  if (timeouts) {
    // Exit 3, distinct from both 0 and 1, so a caller (and Gate 4) can tell "the contract is fine"
    // from "we could not find out". Treat this as UNVERIFIED — do not deploy on it.
    console.log(`INCONCLUSIVE — ${timeouts} endpoint(s) timed out and were never compared.`);
    console.log('This is a PERFORMANCE result, not a contract result. Warm the endpoint and re-run;');
    console.log('raise REQ_TIMEOUT_MS if the cold path legitimately needs longer. Gate 4 is NOT satisfied.');
    process.exit(3);
  }
  console.log('CONTRACT UNCHANGED — Amazon receives the same structure as the April baseline.');
  process.exit(0);
})();
