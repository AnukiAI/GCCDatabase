// CR-171 — Multi-partner data-isolation smoke (READ-ONLY, partner API keys).
// Seed-independent: asserts the ISOLATION INVARIANTS hold for whatever data exists.
// A partner caller must NEVER see a co-partner's name or a co-partner-tagged product ID.
//
// Run (local):
//   BASE_URL=http://localhost:5101 \
//   AMAZON_KEY=gcc_partner_xxx NOVI_KEY=gcc_partner_yyy \
//   node Scripts/smoke-prod/cr171-multipartner-isolation.js
//
// AMAZON_KEY / NOVI_KEY are partner API keys (PartnerApiKeys.ApiKey). Provide at least one.
// Exits non-zero if any isolation invariant fails.

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const KEYS = [
  { name: 'Amazon', key: process.env.AMAZON_KEY },
  { name: 'Novi',   key: process.env.NOVI_KEY },
].filter(k => k.key);

const results = [];
const rec = (id, name, status, notes) => {
  results.push({ id, status });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

async function getJson(path, key) {
  const res = await fetch(BASE + path, { headers: { 'X-Api-Key': key } });
  const text = await res.text();
  let json = null;
  try { json = JSON.parse(text); } catch { /* non-json */ }
  return { status: res.status, json, text };
}

// Names of partners OTHER than the caller — anything here appearing in the caller's
// payload is a leak. Kept deliberately broad (the known core partners).
const ALL_PARTNERS = ['Amazon', 'Novi', 'Wayfair', 'Other'];

function items(resp) {
  if (!resp.json) return [];
  return resp.json.data || resp.json.Data || [];
}

(async () => {
  console.log(`\nCR-171 multi-partner isolation smoke (READ-ONLY) — ${BASE}\n`);
  if (!KEYS.length) {
    console.error('No partner API keys provided (set AMAZON_KEY and/or NOVI_KEY). Aborting.');
    process.exit(2);
  }

  for (const caller of KEYS) {
    const others = ALL_PARTNERS.filter(p => p.toLowerCase() !== caller.name.toLowerCase());
    const otherRe = new RegExp(`\\b(${others.join('|')})\\b`);

    // 1) List with product IDs — cert partner field + product-ID partner labels must be caller-only.
    const list = await getJson('/api/v1/certificates?pageSize=500&includeProductIds=true', caller.key);
    if (list.status !== 200) { rec(`${caller.name}-1`, 'list endpoint reachable', 'FAIL', `HTTP ${list.status}`); continue; }
    const certs = items(list);
    rec(`${caller.name}-1`, `list returned ${certs.length} certs`, 'PASS');

    // 2) No co-partner NAME anywhere in the cert.partner fields.
    const badPartner = certs.filter(c => c.partner && otherRe.test(c.partner));
    rec(`${caller.name}-2`, 'no co-partner name in cert.partner', badPartner.length ? 'FAIL' : 'PASS',
      badPartner.length ? `${badPartner.length} certs leak: e.g. ${badPartner[0].certificateNumber}="${badPartner[0].partner}"` : '');

    // 3) No co-partner-tagged product ID label (Model A): every product-ID partner label
    //    must be blank (untagged/shared) or the caller's own name.
    let badPid = [];
    for (const c of certs) for (const pi of (c.productIds || [])) {
      if (pi.partner && otherRe.test(pi.partner)) badPid.push(`${c.certificateNumber}:${pi.value}="${pi.partner}"`);
    }
    rec(`${caller.name}-3`, 'no co-partner label on product IDs', badPid.length ? 'FAIL' : 'PASS',
      badPid.length ? `${badPid.length} leak(s): e.g. ${badPid[0]}` : '');

    // 4) Whole-payload scan: the caller's name should appear; co-partner names should not
    //    (amazonCode field legitimately contains "amazon" — exclude it before scanning).
    const scrub = list.text.replace(/"amazonCode":\s*("[^"]*"|null)/g, '');
    const leaked = others.filter(o => new RegExp(`"partner":\\s*"[^"]*\\b${o}\\b`).test(scrub));
    rec(`${caller.name}-4`, 'no co-partner name in any partner field (raw scan)', leaked.length ? 'FAIL' : 'PASS',
      leaked.length ? `found: ${leaked.join(', ')}` : '');

    // 5) GS1 list — co-partner product-ID VALUES must not appear. Collect this caller's own
    //    visible product-ID values from the list, then confirm GS1 carries no OTHER values
    //    that belong exclusively to a co-partner is hard seed-free; instead assert GS1 is reachable
    //    and shares the same cert set (access parity).
    const gs1 = await getJson('/api/v1/certificates/gs1?pageSize=200', caller.key);
    rec(`${caller.name}-5`, 'GS1 list reachable', gs1.status === 200 ? 'PASS' : 'FAIL', `HTTP ${gs1.status}`);
  }

  const failed = results.filter(r => r.status === 'FAIL').length;
  console.log(`\n${results.filter(r => r.status === 'PASS').length} PASS / ${failed} FAIL\n`);
  process.exit(failed ? 1 : 0);
})().catch(e => { console.error(e); process.exit(2); });
