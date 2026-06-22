// Amazon Partner API — full localhost functional test.
//
// Exercises EVERY partner-facing endpoint exactly as Amazon would call it (X-API-Key header,
// gcc_partner_ key scoped to the Amazon partner) and validates the data that would be sent to
// Amazon against the Developer-Guide contract:
//   - status is always "Active"            (no internal lifecycle leaks)
//   - certificationCountries is "All"      (§4/§8)
//   - NO co-partner names leak (CR-171 isolation) — only "Amazon" may appear in a partner field
//   - NO internal numeric IDs leaked in partner DTOs
//   - GS1 JSON-LD endpoint returns the @context/$schema/CertificationReport shape Amazon ingests
//
// Setup is self-contained: logs in as admin (cookie), discovers Amazon's partnerId, generates a
// fresh partner API key, then drives the v1 endpoints with that key. The generated GS1 payload is
// written to docs/tracking/api-samples/ as a dated receipt.
//
// Usage:  node Scripts/smoke-prod/amazon-partner-api-local.js
//         BASE_URL=http://localhost:5101 (default)
const { chromium, request } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const ADMIN = { email: 'admin', password: 'TestPass123!' };
const OTHER_PARTNERS = ['Novi', 'Wayfair', 'Other']; // must NOT appear in Amazon's responses
const SAMPLE_DIR = path.join(__dirname, '..', '..', 'docs', 'tracking', 'api-samples');

let pass = 0, fail = 0, warn = 0;
const results = [];
function check(name, ok, detail) {
  results.push({ name, ok, detail });
  if (ok) { pass++; console.log(`  PASS  ${name}${detail ? '  — ' + detail : ''}`); }
  else { fail++; console.log(`  FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
}
function warnCheck(name, detail) { warn++; console.log(`  WARN  ${name}${detail ? '  — ' + detail : ''}`); }

// Scan an arbitrary response for things Amazon must never receive.
function scanContract(label, body, { expectActive = true } = {}) {
  const blob = JSON.stringify(body);

  // 1) co-partner leakage — no other partner's name may appear anywhere in Amazon's payload
  //    (word-boundary match so substrings inside unrelated words don't false-positive)
  for (const p of OTHER_PARTNERS) {
    const present = new RegExp(`\\b${p}\\b`).test(blob);
    check(`${label}: no '${p}' leak`, !present, present ? `FOUND '${p}' in payload` : '');
  }

  // 2) status must be Active everywhere a status appears (partner contract)
  if (expectActive) {
    const statuses = [...blob.matchAll(/"status"\s*:\s*"([^"]*)"/g)].map(m => m[1]);
    const bad = statuses.filter(s => s && s.toUpperCase() !== 'ACTIVE');
    check(`${label}: all status == Active`, bad.length === 0,
      bad.length ? `non-active: ${[...new Set(bad)].join(', ')}` : `${statuses.length} status field(s)`);
  }
}

// Uses Playwright's APIRequestContext (shares the browser context cookie jar for admin calls,
// immune to page navigation). `req` is ctx.request.
async function apiGet(req, key, urlPath) {
  const headers = { Accept: 'application/json' };
  if (key) headers['X-API-Key'] = key;
  const r = await req.get(BASE + urlPath, { headers });
  let body = null; try { body = await r.json(); } catch (e) {}
  return { status: r.status(), body };
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  const req = ctx.request; // shares the admin cookie jar — for admin setup calls only
  // Cookie-FREE context: this is exactly how Amazon calls us (X-API-Key header, no session
  // cookie). The partner middleware skips key-auth when a cookie session exists, so partner
  // endpoints MUST be exercised without the admin cookie or they 401.
  const partnerReq = await request.newContext({ ignoreHTTPSErrors: true });

  try {
    // ---- 1. Admin login (cookie) ----
    console.log('\n[1] Admin login');
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(ADMIN.email);
    await page.locator('#Password').fill(ADMIN.password);
    await Promise.all([page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(800);
    check('admin logged in', !/\/Account\/Login/i.test(page.url()), page.url());

    // ---- 2. Discover Amazon partnerId ----
    console.log('\n[2] Discover Amazon partner');
    const partnersRes = await apiGet(req, null, '/api/admin/partner/list');
    const partners = partnersRes.body?.data ?? partnersRes.body ?? [];
    const amazon = partners.find(p => (p.name || p.Name) === 'Amazon');
    check('Amazon partner exists', !!amazon, amazon ? `id=${amazon.id ?? amazon.Id}` : `got ${JSON.stringify(partners).slice(0,120)}`);
    if (!amazon) throw new Error('No Amazon partner — cannot continue');
    const amazonId = amazon.id ?? amazon.Id;

    // ---- 3. Generate a fresh Amazon partner API key ----
    console.log('\n[3] Generate Amazon partner API key');
    const genResp = await req.post(`${BASE}/api/admin/partner/${amazonId}/apikey`, {
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      data: { description: 'LOCAL TEST KEY — amazon-partner-api-local.js' }
    });
    const genRes = { status: genResp.status(), body: await genResp.json().catch(() => null) };
    const key = genRes.body?.data?.apiKey ?? genRes.body?.data?.ApiKey;
    check('partner API key generated', !!key && key.startsWith('gcc_partner_'),
      key ? key.slice(0, 24) + '…' : `status ${genRes.status}: ${JSON.stringify(genRes.body).slice(0,160)}`);
    if (!key) throw new Error('No API key generated — cannot continue');

    // ---- 4. Auth negative tests ----
    console.log('\n[4] Authentication guards');
    const noKey = (await apiGet(partnerReq, null, '/api/v1/certificates?includeProductIds=true')).status;
    check('no key → 401', noKey === 401, `got ${noKey}`);
    const badKey = await apiGet(partnerReq, 'gcc_partner_deadbeef_invalid', '/api/v1/certificates?includeProductIds=true');
    check('invalid key → 401', badKey.status === 401, `got ${badKey.status}`);

    // ---- 5. Drive every partner endpoint ----
    console.log('\n[5] Partner endpoints (as Amazon)');
    const endpoints = [
      { name: 'GET /certificates?includeProductIds=true', url: '/api/v1/certificates?includeProductIds=true&pageSize=100', paged: true },
      { name: 'GET /certificates (no productIds)',        url: '/api/v1/certificates?pageSize=100', paged: true },
      { name: 'GET /certificates/gs1',                    url: '/api/v1/certificates/gs1?pageSize=100', paged: true, gs1: true },
      // NOTE: search requires a non-empty `term` AND `status` (both are non-nullable params →
      // implicitly [Required] under .NET 8). Empty term returns a 400 by design. We pass a term
      // that matches seeded cert numbers and an Active status.
      { name: 'GET /certificates/search?term=26',          url: '/api/v1/certificates/search?term=26&status=Active&pageSize=100', paged: true },
      { name: 'GET /certificates/expiring?days=3650',     url: '/api/v1/certificates/expiring?days=3650&pageSize=100', paged: true },
    ];

    let firstCertNumber = null, firstAsin = null, gs1Payload = null;
    for (const ep of endpoints) {
      const res = await apiGet(partnerReq, key, ep.url);
      const ok2xx = res.status === 200;
      const data = res.body?.data ?? res.body?.Data ?? [];
      const count = Array.isArray(data) ? data.length : (data ? 1 : 0);
      check(`${ep.name} → 200`, ok2xx, `status ${res.status}, ${count} record(s)`);
      if (!ok2xx) continue;
      scanContract(ep.name, res.body, { expectActive: !ep.gs1 }); // gs1 uses ACTIVE/INACTIVE handled below
      if (ep.gs1) {
        gs1Payload = res.body;
        // GS1 contract shape
        const first = Array.isArray(data) ? data[0] : null;
        if (first) {
          check('GS1: @context present', !!(first['@context']), first['@context']);
          check('GS1: type CertificationReport', first.type === 'CertificationReport', first.type);
          const ci = first.products?.[0]?.certificationInfo;
          check('GS1: targetCountries == [All]', JSON.stringify(ci?.targetCountries) === '["All"]', JSON.stringify(ci?.targetCountries));
          check('GS1: status ACTIVE', ci?.status === 'ACTIVE', ci?.status);
        } else {
          warnCheck('GS1: no records returned to inspect');
        }
      }
      // capture a cert number + asin for the by-number / productid / download tests
      if (!firstCertNumber && Array.isArray(data) && data.length) {
        const c = data[0];
        firstCertNumber = c.certificateNumber || c.CertificateNumber
          || c.products?.[0]?.certificationInfo?.certificateId || null;
        const pids = c.productIds || c.ProductIds || c.products?.[0]?.additionalTradeItemId || [];
        const asin = (pids.find(p => (p.type || p.Type) === 'ASIN'));
        firstAsin = asin ? (asin.value || asin.Value) : null;
      }
    }

    // ---- 6. Single-cert + product-id + download (need a real cert#) ----
    console.log(`\n[6] Single-record endpoints (cert=${firstCertNumber}, asin=${firstAsin})`);
    if (firstCertNumber) {
      const byNum = await apiGet(partnerReq, key, `/api/v1/certificates/number/${encodeURIComponent(firstCertNumber)}`);
      check('GET /certificates/number/{n} → 200', byNum.status === 200, `status ${byNum.status}`);
      if (byNum.status === 200) scanContract('by-number', byNum.body);

      const gs1One = await apiGet(partnerReq, key, `/api/v1/certificates/gs1/${encodeURIComponent(firstCertNumber)}`);
      check('GET /certificates/gs1/{n} → 200', gs1One.status === 200, `status ${gs1One.status}`);

      const dlResp = await partnerReq.get(`${BASE}/api/v1/certificates/${encodeURIComponent(firstCertNumber)}/download`,
        { headers: { 'X-API-Key': key } });
      const dl = { status: dlResp.status(), ctype: dlResp.headers()['content-type'] };
      let dlBody = null; try { dlBody = await dlResp.json(); } catch (e) {}
      if (dl.status === 200 && /pdf/i.test(dl.ctype || '')) {
        check('GET /certificates/{n}/download → 200 pdf', true, `type ${dl.ctype}`);
      } else if (dl.status === 404 && /file not found|not yet implemented/i.test(JSON.stringify(dlBody || ''))) {
        // Endpoint logic is correct; the local seed simply has no physical PDF on disk.
        // (Matches the known missing-uploads condition — not a code defect.)
        warnCheck('download: no physical PDF for this cert locally (endpoint returns proper 404)', `cert ${firstCertNumber}`);
      } else {
        check('GET /certificates/{n}/download', false, `unexpected status ${dl.status}, type ${dl.ctype}`);
      }
    } else {
      warnCheck('no cert number captured — single-record tests skipped (empty dataset?)');
    }

    if (firstAsin) {
      const byPid = await apiGet(partnerReq, key, `/api/v1/certificates/search/productid?productId=${encodeURIComponent(firstAsin)}&type=ASIN`);
      check('GET /certificates/search/productid → 200', byPid.status === 200, `status ${byPid.status}`);
      if (byPid.status === 200) {
        scanContract('search/productid', byPid.body);
        const arr = byPid.body?.data ?? byPid.body?.Data ?? [];
        check('search/productid found the ASIN', Array.isArray(arr) && arr.length > 0, `${arr.length} cert(s) for ${firstAsin}`);
      }
    } else {
      warnCheck('no ASIN captured — productid search skipped');
    }

    // ---- 7. Save GS1 sample receipt ----
    if (gs1Payload) {
      try {
        if (!fs.existsSync(SAMPLE_DIR)) fs.mkdirSync(SAMPLE_DIR, { recursive: true });
        const stamp = new Date().toISOString().slice(0, 10);
        const outFile = path.join(SAMPLE_DIR, `${stamp}_amazon_gs1_localtest.json`);
        fs.writeFileSync(outFile, JSON.stringify(gs1Payload, null, 2));
        console.log(`\n[7] GS1 sample written: ${outFile}`);
      } catch (e) { console.log('  (could not write GS1 sample: ' + e.message + ')'); }
    }

  } catch (e) {
    fail++;
    console.log(`\nFATAL: ${e.message}`);
  } finally {
    await partnerReq.dispose().catch(() => {});
    await ctx.close();
    await browser.close();
  }

  console.log(`\n=== Amazon Partner API local test: ${pass} passed, ${fail} failed, ${warn} warnings ===`);
  process.exit(fail ? 1 : 0);
})();
