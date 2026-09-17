'use strict';
const { chromium } = require('playwright');
const BASE = 'http://localhost:5101';

(async () => {
  const b   = await chromium.launch({ headless: true });
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Login as external
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.locator('#Email').fill('external');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);

  await page.goto(BASE + '/Certificate/CertificateTileViewExternal', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);

  // Check what the selector matched
  const actionsSelectors = [
    '[data-col="tc-actions"]',
    '.tc-actions',
    'th:has-text("Actions")',
  ];
  for (const sel of actionsSelectors) {
    const count = await page.locator(sel).count();
    if (count > 0) {
      const texts = [];
      const els = page.locator(sel);
      for (let i = 0; i < count; i++) {
        texts.push(await els.nth(i).textContent());
      }
      console.log(`Selector "${sel}" matched ${count}: ${JSON.stringify(texts.map(t => t.trim().slice(0, 80)))}`);
    } else {
      console.log(`Selector "${sel}" matched 0`);
    }
  }

  // Also check: is there an "Actions" text anywhere visible on the page?
  const actionsText = await page.locator('text="Actions"').count();
  console.log('text="Actions" count:', actionsText);

  // Look for the specific page structure
  const h1 = await page.locator('h1').textContent().catch(() => 'none');
  console.log('h1:', h1.trim());

  // Dump any th elements with "action" in them
  const allTh = await page.locator('th').all();
  for (const th of allTh) {
    const txt = await th.textContent();
    if (/action/i.test(txt)) {
      const cls = await th.getAttribute('class');
      const dat = await th.getAttribute('data-col');
      console.log(`th with "action": text="${txt.trim()}" class="${cls}" data-col="${dat}"`);
    }
  }

  // What's the final URL?
  console.log('Final URL:', page.url().replace(BASE, ''));

  await ctx.close();
  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
