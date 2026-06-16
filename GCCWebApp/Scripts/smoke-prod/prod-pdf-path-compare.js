// READ-ONLY: dump stored pdfPath for all 15 certs-with-path, marked present/missing,
// so we can see why 8 still 404 after the wwwroot move (path format vs genuine loss).
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

  const d = await page.evaluate(async (u) => (await fetch(u, { headers: { Accept: 'application/json' } })).json(),
    `${BASE}/api/certificate-tiles-internal?sortBy=newest&page=1&pageSize=500`);
  const withPath = (d.certificates || []).filter((c) => c.pdfPath).map((c) => ({ n: c.certificateNumber, p: c.pdfPath }));

  const rows = [];
  for (const c of withPath) {
    const s = await page.evaluate(async (u) => (await fetch(u, { method: 'HEAD' })).status,
      `${BASE}/Certificate/Download/${encodeURIComponent(c.n)}`);
    rows.push({ n: c.n, ok: s === 200, p: c.p });
  }
  rows.sort((a, b) => (a.ok === b.ok ? 0 : a.ok ? -1 : 1));
  console.log('\n  PRESENT(200) / MISSING(404)  cert       stored pdfPath');
  for (const r of rows) console.log(`  ${r.ok ? 'PRESENT ' : 'MISSING '}  ${(r.n + '').padEnd(9)}  ${r.p}`);

  // Distinct path "shapes" (dir + extension) to spot a format split.
  const shape = (p) => { const m = (p || '').replace(/\\/g, '/'); const dir = m.split('/').slice(0, -1).join('/'); const ext = (m.match(/\.[a-z0-9]+$/i) || [''])[0]; return dir + '/*' + ext; };
  const byShape = {};
  for (const r of rows) { const k = shape(r.p); (byShape[k] = byShape[k] || { present: 0, missing: 0 })[r.ok ? 'present' : 'missing']++; }
  console.log('\n  path-shape breakdown (dir/*ext):');
  for (const [k, v] of Object.entries(byShape)) console.log(`    ${k}  -> present ${v.present}, missing ${v.missing}`);

  await browser.close();
})();
