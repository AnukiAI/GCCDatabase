const { chromium } = require('playwright');
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const BASE = 'https://gccwebapp.com';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC });
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('analyst');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
  await page.waitForTimeout(2500);

  // Load the edit page to discover the internal cert id
  await page.goto(BASE + '/Certificate/Edit/26-0160', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3000);

  const info = await page.evaluate(async () => {
    const certId = window.productIdManager?.certificateId;
    if (!certId) return { error: 'no cert id', mgr: typeof window.productIdManager };
    const r = await fetch(`/api/productids/certificate/${certId}?page=1&pageSize=50`);
    const j = await r.json();
    return {
      certId,
      total: j.total ?? j.totalCount ?? j.Total,
      totalPages: j.totalPages,
      pageSize: j.pageSize,
      keys: Object.keys(j),
      firstRowKeys: Array.isArray(j.items) && j.items[0] ? Object.keys(j.items[0]) : null,
    };
  });
  console.log(JSON.stringify(info, null, 2));
  await browser.close();
})();
