'use strict';
const { chromium } = require('playwright');

(async () => {
  const b   = await chromium.launch({ headless: true });
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
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

  // Directly fetch the internal tiles API (the response intercept on on('response') may miss it if response body is read by the page)
  const r = await page.goto(
    'http://localhost:5101/api/certificate-tiles-internal?sortBy=newest&page=1&pageSize=3',
    { waitUntil: 'domcontentloaded', timeout: 30000 }
  );
  const text = await page.textContent('body');
  console.log('API response (truncated):', text.slice(0, 600));

  // Try to parse and find number field
  try {
    const json = JSON.parse(text);
    const arr = json.data || json.items || json.results || (Array.isArray(json) ? json : []);
    if (arr.length > 0) {
      console.log('\nFirst item keys:', Object.keys(arr[0]));
      console.log('First item:', JSON.stringify(arr[0]).slice(0, 400));
    }
  } catch (e) {
    console.log('Parse error:', e.message);
  }

  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
