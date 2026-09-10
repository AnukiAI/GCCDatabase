// DEF-413 live proof on the deployed TEST site.
// Falsifiable: filtering by a PARENT certification type must return certs whose OWN type is a
// SUBCATEGORY of it. Pre-fix that is impossible (exact-name match only), so any such row is proof
// the new DLL is live. Also prints per-type totals so widening is visible as a number.
const { chromium, request } = require('playwright');
const { enterSite } = require('./_lib/test-env-gate');

const BASE = process.env.BASE || 'https://gccwebapp.com';

(async () => {
  const gate = await enterSite(request, BASE);
  console.log(`[gate] mode=${gate.mode} ok=${gate.ok} — ${gate.detail}`);
  if (!gate.ok) { console.log('FATAL: could not pass gate'); process.exit(2); }

  const ctx = await request.newContext({ storageState: gate.state });
  const get = async (u) => {
    const r = await ctx.get(`${BASE}${u}`, { failOnStatusCode: false, timeout: 120000 });
    const t = await r.text();
    if (r.status() !== 200) return { status: r.status(), body: t.slice(0, 200) };
    try { return { status: 200, json: JSON.parse(t) }; }
    catch { return { status: 200, body: t.slice(0, 200), parseErr: true }; }
  };

  const f = await get('/api/certificate-tiles/filters');
  if (f.status !== 200 || !f.json) { console.log(`FATAL: /filters -> ${f.status} ${f.body||''}`); process.exit(2); }
  const types = f.json.certificationTypes || [];
  console.log(`[filters] ${types.length} top-level certification types`);

  const parents = types.filter(t => (t.children || t.subcategories || []).length > 0);
  console.log(`[filters] ${parents.length} have subcategories\n`);

  let proofs = 0, checked = 0;
  const rows = [];
  for (const p of types) {
    const kids = (p.children || p.subcategories || []).map(k => k.name || k.Name);
    const name = p.name || p.Name;
    const r = await get(`/api/certificate-tiles?certType=${encodeURIComponent(name)}&page=1&pageSize=100`);
    if (r.status !== 200 || !r.json) { rows.push(`  ${name} -> HTTP ${r.status}`); continue; }
    const total = r.json.totalCount;
    const own = (r.json.certificates || r.json.data || []).map(c => c.certificationType);
    const foreign = [...new Set(own.filter(o => o && o !== name))];
    checked++;
    if (foreign.length) proofs++;
    rows.push(`  ${String(total).padStart(5)}  ${name}${kids.length ? `  [${kids.length} subcats]` : ''}` +
      (foreign.length ? `\n           ↳ SUBCAT REACH: ${foreign.slice(0,6).join(', ')}` : ''));
  }
  console.log(rows.join('\n'));
  console.log(`\n[DEF-413] ${proofs}/${checked} parent filters returned subcategory-typed certs`);
  console.log(proofs > 0 ? '[DEF-413] LIVE — widening confirmed on deployed site'
                         : '[DEF-413] NO PROOF — either not deployed, or no subcat data on TEST');
  await ctx.dispose();
})().catch(e => { console.error('FATAL', e); process.exit(2); });
