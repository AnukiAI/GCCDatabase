const { chromium } = require('playwright');
const BASE = 'https://gccwebapp.com';
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
  await page.waitForTimeout(2000);

  await page.goto(BASE + '/Certificate/Edit/26-0163', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2500);

  // Find every text input that's part of a searchable-select
  const inputs = await page.evaluate(() => {
    const all = [];
    document.querySelectorAll('input[type="text"], input:not([type])').forEach(i => {
      // Find nearby label or hidden select
      const labelTxt = (i.closest('.form-group, .field-row, .col')?.querySelector('label')?.innerText || '').trim().slice(0, 30);
      all.push({
        id: i.id,
        name: i.name,
        placeholder: i.placeholder,
        value: i.value?.slice(0, 30),
        classes: i.className?.toString().slice(0, 80),
        label: labelTxt,
      });
    });
    return all.slice(0, 30);
  });
  console.log('TEXT INPUTS:');
  inputs.forEach(i => console.log(' ', JSON.stringify(i)));

  // Also dump SearchableSelect-related elements
  const sel = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('[class*="searchable"], [data-searchable], .ss-main, .ss-content').forEach(el => {
      out.push({ tag: el.tagName, classes: el.className?.toString().slice(0, 80), id: el.id });
    });
    return out.slice(0, 10);
  });
  console.log('\nSEARCHABLE-related:');
  sel.forEach(s => console.log(' ', JSON.stringify(s)));

  await browser.close();
})();
