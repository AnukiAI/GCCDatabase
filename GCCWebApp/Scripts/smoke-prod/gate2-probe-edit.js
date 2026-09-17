'use strict';
const { chromium } = require('playwright');

(async () => {
  const b   = await chromium.launch({ headless: true });
  const ctx = await b.newContext();
  const page = await ctx.newPage();

  // Login as admin
  await page.goto('http://localhost:5101/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(1000);

  // Test Edit/1 (numeric id)
  const r1 = await page.goto('http://localhost:5101/Certificate/Edit/1', { waitUntil: 'domcontentloaded', timeout: 30000 });
  console.log('Edit/1 status:', r1.status(), '| url:', page.url());

  // Go to internal tile view to find a real edit link
  await page.goto('http://localhost:5101/Certificate/CertificateTileViewInternal', { waitUntil: 'domcontentloaded', timeout: 30000 });
  try {
    await page.waitForFunction(
      () => document.querySelectorAll('#certificatesGrid .certificate-card').length > 0,
      { timeout: 10000 }
    );
    const count = await page.locator('#certificatesGrid .certificate-card').count();
    console.log('Cards found:', count);
    // Check the page source for edit links
    const bodyHTML = await page.content();
    const matches = bodyHTML.match(/\/Certificate\/Edit\/[^"'\s]+/g) || [];
    const unique = [...new Set(matches)].slice(0, 5);
    console.log('Edit links found:', unique);
  } catch (e) {
    console.log('Error finding cards:', e.message);
  }

  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
