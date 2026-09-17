'use strict';
const { chromium } = require('playwright');

(async () => {
  const b   = await chromium.launch({ headless: true });
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('response', (resp) => {
    if (resp.url().includes('tiles') || resp.url().includes('certificate')) {
      console.log('RESP:', resp.status(), resp.url());
    }
  });

  // Login as admin
  await page.goto('http://localhost:5101/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(1000);

  // Navigate to internal tile view and intercept API calls
  let certNumber = null;
  page.on('response', async (resp) => {
    if (certNumber) return;
    const url = resp.url();
    if (!url.includes('localhost:5101')) return;
    const ct = resp.headers()['content-type'] || '';
    if (!ct.includes('json')) return;
    try {
      const body = await resp.json();
      // Look for certificate data with number field
      const arr = Array.isArray(body) ? body : (body.data || body.items || body.results || []);
      if (arr.length > 0) {
        const first = arr[0];
        const num = first.certificateNumber || first.CertificateNumber || first.number || first.Number;
        if (num) {
          certNumber = num;
          console.log('Found cert number from API ' + url + ': ' + num);
        }
      }
    } catch { /* ignore */ }
  });

  await page.goto('http://localhost:5101/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle', timeout: 45000 }).catch(() => {});
  await page.waitForTimeout(3000);

  if (!certNumber) {
    // Try to scrape from the Management page or index
    console.log('API intercept missed cert number, trying Admin Certificate list...');
    const r = await page.goto('http://localhost:5101/Certificate', { waitUntil: 'domcontentloaded', timeout: 30000 });
    console.log('Cert index status:', r.status());
    const links = await page.locator('a[href*="/Certificate/Edit/"]').all();
    if (links.length > 0) {
      const href = await links[0].getAttribute('href');
      console.log('Edit link from index:', href);
      certNumber = href.split('/Certificate/Edit/')[1];
    }
  }

  if (!certNumber) {
    // Just try Edit with a typical cert number format that might exist
    console.log('Trying default cert number format: GCC-0001');
    certNumber = 'GCC-0001';
  }

  console.log('\nFinal cert number to test:', certNumber);

  // Test Edit as admin
  const r1 = await page.goto('http://localhost:5101/Certificate/Edit/' + encodeURIComponent(certNumber), {
    waitUntil: 'domcontentloaded', timeout: 30000
  });
  console.log('Admin Edit/' + certNumber + ' status:', r1.status(), '| url:', page.url().replace('http://localhost:5101', ''));
  await ctx.close();

  // Test as analyst
  const ctx2 = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page2 = await ctx2.newPage();
  await page2.goto('http://localhost:5101/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page2.locator('#Email').fill('analyst');
  await page2.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page2.waitForLoadState('domcontentloaded'),
    page2.locator('button[type="submit"]').first().click(),
  ]);
  await page2.waitForTimeout(800);
  const r2 = await page2.goto('http://localhost:5101/Certificate/Edit/' + encodeURIComponent(certNumber), {
    waitUntil: 'domcontentloaded', timeout: 30000
  });
  console.log('Analyst Edit/' + certNumber + ' status:', r2.status(), '| url:', page2.url().replace('http://localhost:5101', ''));
  await ctx2.close();

  // Test external user denied
  const ctx3 = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page3 = await ctx3.newPage();
  await page3.goto('http://localhost:5101/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page3.locator('#Email').fill('external');
  await page3.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page3.waitForLoadState('domcontentloaded'),
    page3.locator('button[type="submit"]').first().click(),
  ]);
  await page3.waitForTimeout(800);
  const r3 = await page3.goto('http://localhost:5101/Certificate/Edit/' + encodeURIComponent(certNumber), {
    waitUntil: 'domcontentloaded', timeout: 30000
  });
  console.log('External Edit/' + certNumber + ' status:', r3.status(), '| url:', page3.url().replace('http://localhost:5101', ''));
  await ctx3.close();

  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
