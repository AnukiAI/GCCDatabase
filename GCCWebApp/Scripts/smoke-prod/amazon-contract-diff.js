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
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const SAMPLE_DIR = path.join(__dirname, '..', '..', 'docs', 'tracking', 'api-samples');

let pass = 0, fail = 0;
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
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  const partnerReq = await request.newContext({ ignoreHTTPSErrors: true }); // cookie-free = how Amazon calls

  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill('admin');
    await page.locator('#Password').fill('TestPass123!');
    await Promise.all([page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(700);

    const plist = await ctx.request.get(`${BASE}/api/admin/partner/list`, { headers: { Accept: 'application/json' } });
    const amazon = ((await plist.json()).data || []).find(p => p.name === 'Amazon');
    const gen = await ctx.request.post(`${BASE}/api/admin/partner/${amazon.id}/apikey`, {
      headers: { 'Content-Type': 'application/json' }, data: { description: 'amazon-contract-diff' } });
    const key = (await gen.json()).data.apiKey;
    check('setup: got Amazon partner key', !!key && key.startsWith('gcc_partner_'));

    const getLive = async (url) => {
      const r = await partnerReq.get(`${BASE}${url}`, { headers: { 'X-API-Key': key, Accept: 'application/json' }, timeout: 60000 });
      let body = null; try { body = await r.json(); } catch (e) {}
      return { status: r.status(), body };
    };

    const comparisons = [
      { name: 'GS1 list',      url: '/api/v1/certificates/gs1?pageSize=100',                  baseline: '2026-04-30_amazon_gs1.json' },
      { name: 'GS1 single',    url: '/api/v1/certificates/gs1/26-0146',                       baseline: '2026-04-30_amazon_gs1_single.json' },
      { name: 'Standard list', url: '/api/v1/certificates?includeProductIds=true&pageSize=100', baseline: '2026-04-24_amazon_standard.json' },
    ];

    for (const c of comparisons) {
      const live = await getLive(c.url);
      check(`${c.name}: live → 200`, live.status === 200, `status ${live.status}`);
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

  console.log(`\n=== Amazon contract diff: ${pass} passed, ${fail} failed ===`);
  console.log(fail ? 'CONTRACT CHANGED — review diffs above.' : 'CONTRACT UNCHANGED — Amazon receives the same structure as the April baseline.');
  process.exit(fail ? 1 : 0);
})();
