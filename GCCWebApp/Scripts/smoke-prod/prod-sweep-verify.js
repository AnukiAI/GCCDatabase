// Validate the missing-pdf sweep: (1) true cert total + pageSize cap, (2) confirm 404s are
// REAL via full GET (not a HEAD artifact), (3) probe for ANY present PDF. READ-ONLY.
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1200);
  console.log('login ->', /\/Account\/Login/i.test(page.url()) ? 'FAILED' : 'ok', '\n');

  // (1) Envelope shape + does pageSize=100 / page 2 return more?
  for (const [label, u] of [
    ['pageSize=100 p1', `${BASE}/api/certificate-tiles-internal?sortBy=newest&page=1&pageSize=100`],
    ['pageSize=500 p1', `${BASE}/api/certificate-tiles-internal?sortBy=newest&page=1&pageSize=500`],
    ['pageSize=50  p2', `${BASE}/api/certificate-tiles-internal?sortBy=newest&page=2&pageSize=50`],
  ]) {
    const d = await page.evaluate(async (uu) => { const r = await fetch(uu, { headers: { Accept: 'application/json' } }); return r.json(); }, u);
    const arr = d.certificates || d.data || d.items || [];
    const envKeys = Object.keys(d).filter((k) => !Array.isArray(d[k]) || true);
    const totalish = ['total', 'totalCount', 'totalItems', 'count', 'hasMore', 'totalPages'].filter((k) => k in d).map((k) => `${k}=${d[k]}`);
    console.log(`${label}: arrLen=${arr.length}  envelope=[${envKeys.join(',')}]  ${totalish.join(' ') || '(no total field)'}`);
  }

  // (2) REAL GET on 3 "missing" certs — confirm 404 + body (proves not a HEAD artifact).
  console.log('\nFull GET on reported-missing certs:');
  for (const n of ['26-0004', '26-0106', '28-0002']) {
    const r = await page.evaluate(async (uu) => {
      const resp = await fetch(uu);
      const ct = resp.headers.get('content-type') || '';
      let body = ''; if (!ct.includes('pdf')) { try { body = (await resp.text()).slice(0, 120); } catch {} }
      return { s: resp.status, ct, body };
    }, `${BASE}/Certificate/Download/${n}`);
    console.log(`  ${n}: GET ${r.s} ${r.ct}${r.body ? ' :: ' + r.body : ''}`);
  }

  // (3) Does HEAD ever differ from GET? Cross-check one cert both ways to validate the method.
  console.log('\nHEAD-vs-GET method check on 26-0173:');
  const m = await page.evaluate(async (uu) => {
    const h = await fetch(uu, { method: 'HEAD' });
    const g = await fetch(uu);
    return { head: h.status, get: g.status };
  }, `${BASE}/Certificate/Download/26-0173`);
  console.log(`  HEAD=${m.head}  GET=${m.get}  -> ${m.head === m.get ? 'consistent (HEAD is reliable)' : 'MISMATCH (HEAD unreliable!)'}`);

  await browser.close();
})();
