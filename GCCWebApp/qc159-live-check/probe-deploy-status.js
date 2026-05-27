// Quick probe — is DEF-194 v2 live on gccwebapp.com yet?
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

  await page.goto(BASE + '/Certificate/Edit/26-0160', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3500);

  const probe = await page.evaluate(() => ({
    bannerEl: !!document.getElementById('bulkSelectAllBanner'),
    selectAllLink: !!document.getElementById('bulkSelectAllLink'),
    jsHasFlag: typeof window.productIdManager !== 'undefined' &&
               'selectAllAcrossPages' in window.productIdManager,
  }));
  // Try the new endpoint directly
  const apiCheck = await page.evaluate(async () => {
    const certId = window.productIdManager?.certificateId;
    if (!certId) return { error: 'no certId' };
    const r = await fetch(`/api/productids/certificate/${certId}/bulk-delete`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ deleteAll: true, search: 'ZZ_NONEXISTENT_ZZ' }),
    });
    return { status: r.status };
  });
  console.log(JSON.stringify({ probe, apiCheck }, null, 2));
  await browser.close();
})();
