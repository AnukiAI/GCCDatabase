'use strict';
const { chromium } = require('playwright');

(async () => {
  const b   = await chromium.launch({ headless: true });
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Login as admin to hit the API for cert numbers
  await page.goto('http://localhost:5101/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(1000);

  // Hit the tile API to get a real certificate number
  const apiResp = await page.goto('http://localhost:5101/api/internal/certificates/tiles?page=1&pageSize=5', {
    waitUntil: 'domcontentloaded', timeout: 30000
  });
  let certNumber = null;
  try {
    const json = await apiResp.json();
    const first = json.data ? json.data[0] : (Array.isArray(json) ? json[0] : null);
    certNumber = first ? (first.certificateNumber || first.CertificateNumber || first.number || first.Number) : null;
    console.log('API response keys (first item):', first ? Object.keys(first) : 'none');
    console.log('Cert number candidate:', certNumber);
    if (!certNumber) {
      console.log('First item:', JSON.stringify(first).slice(0, 300));
    }
  } catch (e) {
    const text = await apiResp.text();
    console.log('API not JSON or error:', e.message, '| body:', text.slice(0, 200));
  }

  if (!certNumber) {
    // Try alternate API path
    const r2 = await page.goto('http://localhost:5101/api/certificates/tiles?page=1&pageSize=5', {
      waitUntil: 'domcontentloaded', timeout: 30000
    });
    try {
      const json = await r2.json();
      console.log('Alt API status:', r2.status());
      console.log('Alt API resp (truncated):', JSON.stringify(json).slice(0, 300));
    } catch (e2) {
      const text = await r2.text();
      console.log('Alt API text:', text.slice(0, 200));
    }
  } else {
    // Test Edit with real cert number
    console.log('\nTesting Edit with real cert number:', certNumber);
    const r = await page.goto('http://localhost:5101/Certificate/Edit/' + encodeURIComponent(certNumber), {
      waitUntil: 'domcontentloaded', timeout: 30000
    });
    console.log('Edit/' + certNumber + ' status:', r.status(), '| url:', page.url());

    // Now test as analyst
    await ctx.close();
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
    const r3 = await page2.goto('http://localhost:5101/Certificate/Edit/' + encodeURIComponent(certNumber), {
      waitUntil: 'domcontentloaded', timeout: 30000
    });
    console.log('Analyst Edit/' + certNumber + ' status:', r3.status(), '| url:', page2.url());
    await ctx2.close();
    await b.close();
    return;
  }

  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
