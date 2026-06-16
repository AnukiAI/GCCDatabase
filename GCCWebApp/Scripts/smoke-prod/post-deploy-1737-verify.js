// Post-deploy smoke for GCCWebApp-deploy-20260615-1737.zip on prod (gccwebapp.com).
//   H1  app healthy: admin login succeeds (the DEF-239 schema canary)
//   H2  /Product/Management data endpoint responds 200
//   D   DEF-264: real certificate PDFs download (application/pdf), not the
//       post-App_Data "No certificate PDF has been uploaded" 404
// Read-only. Run: node Scripts/smoke-prod/post-deploy-1737-verify.js
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const PASS = 'TestPass123!';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE });
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill(PASS);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1500);
  const loggedIn = !/\/Account\/Login/i.test(page.url());
  console.log(`H1 admin login: ${loggedIn ? 'OK' : 'FAIL'}  (${page.url()})`);

  const pm = await ctx.request.get(`${BASE}/Product/GetProducts`);
  console.log(`H2 /Product/GetProducts: HTTP ${pm.status()}`);

  // public certs (likely to have PDFs) for the DEF-264 download check
  let nums = [];
  for (const url of [`${BASE}/api/certificate-tiles?page=1&pageSize=25`]) {
    try { const j = await (await ctx.request.get(url)).json();
      nums = (j.certificates || j.data || j.items || []).map(c => c.certificateNumber || c.number).filter(Boolean);
    } catch {}
  }
  console.log(`public certs found: ${nums.length}`);

  let pdf = 0, noPdf = 0, other = 0;
  for (const n of nums.slice(0, 15)) {
    const r = await ctx.request.get(`${BASE}/Certificate/Download/${encodeURIComponent(n)}`);
    const ct = (r.headers()['content-type'] || '').toLowerCase();
    const len = r.headers()['content-length'] || '?';
    if (r.status() === 200 && (ct.includes('pdf') || ct.includes('octet-stream'))) {
      pdf++; if (pdf <= 4) console.log(`   ${n}: PDF ${ct} ${len} bytes`);
    } else {
      const body = await r.text().catch(() => '');
      if (/no certificate pdf/i.test(body) || r.status() === 404) noPdf++;
      else { other++; console.log(`   ${n}: HTTP ${r.status()} ${ct}`); }
    }
  }
  console.log(`D downloads — pdf=${pdf}, noPdf/empty=${noPdf}, other=${other}`);
  console.log(`\n${loggedIn && pm.status() === 200 ? '✅ app healthy' : '❌ health issue'} · ${pdf > 0 ? '✅ DEF-264 PDF download works' : '⚠ no PDFs downloaded (check certs have files)'}`);
  await browser.close();
})();
