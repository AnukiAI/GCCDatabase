// DEF-269 verification — partner API GET /api/v1/certificates/search optional params.
//
// Before the fix:
//   - ?term= (empty) returned HTTP 400 ("The term field is required." / "status ... required.")
//     because `term`/`status` were non-nullable params (implicitly [Required] under .NET 8).
//   - ?status=Active matched the INTERNAL status literally, so a partner caller got 0 rows
//     (seed certs are stored "Client Approved - Paid" etc., reported as "Active" only on output).
//
// After the fix:
//   (A) no params / empty term  -> 200 with the partner's full scoped set
//   (B) ?term=26&status=Active  -> 200 WITH records (partner status filter is ignored)
//   (C) results stay contract-clean (status=="Active", no co-partner leak)
//
// Self-contained: admin-cookie login -> Amazon partner key -> cookie-free partner calls.
const { chromium, request } = require('playwright');
const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const OTHER_PARTNERS = ['Novi', 'Wayfair', 'Other'];

let pass = 0, fail = 0;
function check(name, ok, detail) {
  if (ok) { pass++; console.log(`  PASS  ${name}${detail ? '  — ' + detail : ''}`); }
  else { fail++; console.log(`  FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  const partnerReq = await request.newContext({ ignoreHTTPSErrors: true }); // cookie-free = how Amazon calls

  try {
    // login (cookie) + generate an Amazon partner key
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill('admin');
    await page.locator('#Password').fill('TestPass123!');
    await Promise.all([page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(700);

    const plist = await ctx.request.get(`${BASE}/api/admin/partner/list`, { headers: { Accept: 'application/json' } });
    const amazon = ((await plist.json()).data || []).find(p => p.name === 'Amazon');
    const gen = await ctx.request.post(`${BASE}/api/admin/partner/${amazon.id}/apikey`, {
      headers: { 'Content-Type': 'application/json' }, data: { description: 'DEF-269 verify' } });
    const key = (await gen.json()).data.apiKey;
    check('setup: got Amazon partner key', !!key && key.startsWith('gcc_partner_'));

    const get = async (url) => {
      // 60s: first search query after a cold boot can be slow (DEF-234 EF cold-start class).
      const r = await partnerReq.get(`${BASE}${url}`, { headers: { 'X-API-Key': key, Accept: 'application/json' }, timeout: 60000 });
      let body = null; try { body = await r.json(); } catch (e) {}
      const data = body?.data ?? body?.Data ?? [];
      return { status: r.status(), count: Array.isArray(data) ? data.length : 0, body };
    };

    // baseline: the plain list endpoint (what the partner is scoped to)
    const list = await get('/api/v1/certificates?pageSize=100');
    check('baseline /certificates → 200', list.status === 200, `${list.count} certs`);

    // (A) DEF-269 core repro: empty term must no longer 400
    const a1 = await get('/api/v1/certificates/search?pageSize=100');
    check('(A) search with NO params → 200 (was 400)', a1.status === 200, `status ${a1.status}, ${a1.count} certs`);
    const a2 = await get('/api/v1/certificates/search?term=&pageSize=100');
    check('(A) search?term= (empty) → 200 (was 400)', a2.status === 200, `status ${a2.status}, ${a2.count} certs`);
    check('(A) empty-term search returns the scoped set', a2.count === list.count,
      `search=${a2.count} vs list=${list.count}`);

    // (B) partner passing status=Active now returns records (filter ignored for partners)
    const b = await get('/api/v1/certificates/search?term=26&status=Active&pageSize=100');
    check('(B) search?term=26&status=Active → 200', b.status === 200, `status ${b.status}`);
    check('(B) status=Active no longer zeroes out partner results', b.count > 0, `${b.count} certs (was 0)`);

    // (C) contract still clean on the search output
    const blob = JSON.stringify(b.body || {});
    for (const p of OTHER_PARTNERS) {
      check(`(C) no '${p}' co-partner leak`, !new RegExp(`\\b${p}\\b`).test(blob));
    }
    const statuses = [...blob.matchAll(/"status"\s*:\s*"([^"]*)"/g)].map(m => m[1]);
    const bad = statuses.filter(s => s && s.toUpperCase() !== 'ACTIVE');
    check('(C) all reported status == Active', bad.length === 0, bad.length ? bad.join(',') : `${statuses.length} status fields`);

  } catch (e) {
    fail++; console.log(`FATAL: ${e.message}`);
  } finally {
    await partnerReq.dispose().catch(() => {});
    await ctx.close();
    await browser.close();
  }

  console.log(`\n=== DEF-269 verify: ${pass} passed, ${fail} failed ===`);
  process.exit(fail ? 1 : 0);
})();
