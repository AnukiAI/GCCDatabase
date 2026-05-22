// Cross-page persistence only. Uses state:'attached' to avoid visibility timeout.
const { chromium } = require('playwright');
const path = require('path');

const BASE = 'http://localhost:5101';
const APP_USER = 'analyst';
const APP_PASS = 'TestPass123!';
const LARGE_CERT_NUMBER = '26-0106';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  [pageerror]', e.message));
  page.on('console', m => { if (m.type() === 'error') console.log('  [console.error]', m.text()); });

  const out = [];

  try {
    await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await page.locator('button[type="submit"]').first().click();
    await page.waitForURL(/\/(Home|Certificate|Dashboard)/i, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(1500);

    console.log(`Navigating to Edit ${LARGE_CERT_NUMBER}...`);
    const resp = await page.goto(`${BASE}/Certificate/Edit/${LARGE_CERT_NUMBER}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    console.log(`  HTTP ${resp ? resp.status() : '??'}`);

    // attached, not visible
    await page.waitForSelector('#productIdsTable', { state: 'attached', timeout: 30000 });
    console.log('  #productIdsTable attached');

    // Wait for the productids API to settle.
    await page.waitForResponse(r => /\/api\/productids\/certificate\/\d+/.test(r.url()) && r.status() === 200, { timeout: 30000 }).catch(() => console.log('  api wait timed out'));
    await page.waitForTimeout(2500);

    // Make sure table is scrolled into view
    await page.locator('#productIdsTable').scrollIntoViewIfNeeded().catch(() => {});

    const rowCount = await page.locator('.bulk-select-checkbox').count();
    console.log(`  visible row checkboxes: ${rowCount}`);

    if (rowCount === 0) {
      console.log('FAIL: no checkboxes rendered');
      await page.screenshot({ path: path.join('C:/Users/swest/AppData/Local/Temp', 'crosspage-no-rows.png'), fullPage: true });
      return;
    }

    // Check first checkbox
    await page.locator('.bulk-select-checkbox').first().check();
    await page.waitForTimeout(300);
    const cp1 = (await page.locator('#bulkDeleteCount').innerText()).trim();
    out.push(['D1', `Page 1 single check: count=1`, cp1 === '1']);
    console.log(`  D1 count after one selection: ${cp1}`);

    // Find pagination button "2"
    const page2Btn = page.locator('.pagination-btn').filter({ hasText: /^2$/ }).first();
    const page2Count = await page2Btn.count();
    console.log(`  pagination "2" buttons found: ${page2Count}`);
    if (page2Count === 0) {
      console.log('  no page-2 button — cert may have <51 IDs or pagination control differs');
      const allBtns = await page.locator('.pagination-btn').allTextContents();
      console.log('  pagination-btn texts:', allBtns);
      return;
    }

    await page2Btn.click();
    await page.waitForTimeout(2000);
    const cp2 = (await page.locator('#bulkDeleteCount').innerText()).trim();
    out.push(['D2', `Page 2: count persists =1`, cp2 === '1']);
    console.log(`  D2 count after page2 nav: ${cp2}`);

    await page.locator('.bulk-select-checkbox').first().check();
    await page.waitForTimeout(300);
    const cp3 = (await page.locator('#bulkDeleteCount').innerText()).trim();
    out.push(['D3', `Page 2 +1 selection: count=2`, cp3 === '2']);
    console.log(`  D3 count after second selection: ${cp3}`);

    const page1Btn = page.locator('.pagination-btn').filter({ hasText: /^1$/ }).first();
    await page1Btn.click();
    await page.waitForTimeout(2000);
    const firstChecked = await page.locator('.bulk-select-checkbox').first().isChecked();
    out.push(['D4', `Back on page 1: first row still checked`, firstChecked]);
    console.log(`  D4 first checkbox still checked: ${firstChecked}`);

    await page.screenshot({ path: path.join('C:/Users/swest/AppData/Local/Temp', 'crosspage-back-page1.png'), fullPage: true });
  } catch (e) {
    console.log('ERROR:', e.message);
    await page.screenshot({ path: path.join('C:/Users/swest/AppData/Local/Temp', 'crosspage-error.png'), fullPage: true }).catch(() => {});
  } finally {
    await browser.close();
    const pass = out.filter(r => r[2]).length;
    const fail = out.filter(r => !r[2]).length;
    console.log(`\nCROSS-PAGE: ${pass} PASS / ${fail} FAIL`);
    out.forEach(r => console.log(`  [${r[2] ? 'PASS' : 'FAIL'}] ${r[0]} ${r[1]}`));
  }
})();
