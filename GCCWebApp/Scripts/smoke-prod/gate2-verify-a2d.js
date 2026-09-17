'use strict';
const { chromium } = require('playwright');
const BASE = 'http://localhost:5101';
const path = require('path');
const fs = require('fs');
const SCREENSHOTS_DIR = path.join(__dirname, '_results', 'screenshots');

(async () => {
  const b   = await chromium.launch({ headless: true });
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.locator('#Email').fill('external');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);

  await page.goto(BASE + '/Certificate/CertificateTileViewExternal', { waitUntil: 'domcontentloaded', timeout: 30000 });
  // wait for tiles to load
  try {
    await page.waitForFunction(
      () => document.querySelectorAll('.certificate-card').length > 0,
      { timeout: 10000 }
    );
  } catch { /* fall through */ }
  await page.waitForTimeout(1500);

  // Force table view by showing #tableView
  await page.evaluate(() => {
    const tv = document.getElementById('tableView');
    if (tv) tv.style.display = 'block';
    const cv = document.getElementById('cardView');
    if (cv) cv.style.display = 'none';
  });
  // Wait for JS to potentially render rows
  await page.waitForTimeout(2000);

  if (!fs.existsSync(SCREENSHOTS_DIR)) fs.mkdirSync(SCREENSHOTS_DIR, { recursive: true });
  await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'gate2-a2-ext-table-view.png'), fullPage: false });
  console.log('Screenshot: gate2-a2-ext-table-view.png');

  const rows = await page.locator('.table-body-cell.tc-actions').count();
  console.log('tc-actions body cells:', rows);
  if (rows > 0) {
    const html = await page.locator('.table-body-cell.tc-actions').first().innerHTML();
    console.log('First tc-actions cell HTML:\n', html.slice(0, 500));
  }

  // What buttons are in the action cells?
  const editBtns = await page.locator('.tc-actions .action-btn:not(.download-btn), .tc-actions a[href*="Edit"]').count();
  const downloadBtns = await page.locator('.tc-actions .download-btn, .tc-actions .action-btn[title*="Download"]').count();
  const deleteBtns = await page.locator('.tc-actions .delete-btn, .tc-actions .action-btn[title*="Delete"]').count();
  console.log(`Action buttons: edit=${editBtns} download=${downloadBtns} delete=${deleteBtns}`);

  await ctx.close();
  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
