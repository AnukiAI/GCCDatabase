// QC #170 walkthrough v5 — call productTypeManager.deleteProductType directly
// (skips the click-through-overlay issue), and diagnose Edit page if selectors
// miss.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `qc170v5-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

const ADMIN_USER = 'admin';
const ADMIN_PASS = 'TestPass123!';

function saveText(name, text) {
  fs.writeFileSync(path.join(OUT, `${name}.txt`), text || '(empty)');
}
async function viewportShot(page, name) {
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: false });
  console.log('  shot:', name);
}
async function elementShot(page, locator, name) {
  try {
    await locator.scrollIntoViewIfNeeded({ timeout: 3000 });
    await locator.screenshot({ path: path.join(OUT, `${name}.png`) });
    console.log('  shot (el):', name);
  } catch (e) {
    await viewportShot(page, name);
  }
}

async function login(page) {
  await page.goto(`${BASE}/Account/Login`);
  await page.fill('input[name="Email"]', ADMIN_USER);
  await page.fill('input[name="Password"]', ADMIN_PASS);
  await Promise.all([
    page.waitForLoadState('networkidle'),
    page.click('button[type="submit"], input[type="submit"]')
  ]);
}

async function step1ProductTypes(page) {
  console.log('\n[1] Admin → Product Types — Carly Test rows');
  await page.goto(`${BASE}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  await page.locator('button[data-view="table"]').click();
  await page.waitForTimeout(700);

  const headers = await page.locator('table thead th').allInnerTexts().catch(() => []);
  saveText('00-table-headers', JSON.stringify(headers));

  const carlyCatRow = page.locator('tr', { has: page.locator('td:has-text("Carly Test")').first() }).first();
  const carlyTypeRow = page.locator('tr', { has: page.locator('td:has-text("Carly Test Type")').first() }).first();
  await elementShot(page, carlyCatRow, '01-row-carly-test-category');
  await elementShot(page, carlyTypeRow, '02-row-carly-test-type');
  saveText('01-row-carly-test-category', JSON.stringify(await carlyCatRow.locator('td').allInnerTexts().catch(() => [])));
  saveText('02-row-carly-test-type', JSON.stringify(await carlyTypeRow.locator('td').allInnerTexts().catch(() => [])));

  await page.locator('text=Carly Test').first().scrollIntoViewIfNeeded().catch(() => {});
  await viewportShot(page, '03-table-viewport');
}

async function step2ConfirmModalScreenshot(page) {
  console.log('\n[2a] Confirm modal screenshot via real click');
  await page.goto(`${BASE}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  await page.locator('button[data-view="table"]').click();
  await page.waitForTimeout(700);

  await page.locator('.delete-btn[data-id="53"]').first().click();
  await page.waitForSelector('.admin-modal-overlay button[data-action="confirm"]', { timeout: 5000 });
  await page.waitForTimeout(400);

  const confirmOverlay = page.locator('.admin-modal-overlay').first();
  await elementShot(page, confirmOverlay, '04-confirm-modal');
  saveText('04-confirm-modal', await confirmOverlay.innerText().catch(() => ''));
  await viewportShot(page, '04-confirm-modal-viewport');
}

async function step2bExerciseDeleteAndCaptureError(page) {
  console.log('\n[2b] Trigger delete via JS — capture server error modal');
  await page.goto(`${BASE}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  await page.locator('button[data-view="table"]').click();
  await page.waitForTimeout(800);

  // Directly call deleteProductType — bypasses the confirm modal entirely
  await page.evaluate(() => {
    return window.productTypeManager.deleteProductType(53, false);
  });

  await page.waitForFunction(() => {
    const ov = document.querySelector('.admin-modal-overlay');
    if (!ov) return false;
    const t = (ov.innerText || '').toLowerCase();
    return t.includes('cannot delete') || t.includes('error') || t.includes('failed') || t.includes('certificate');
  }, { timeout: 8000 }).catch(() => console.log('  ! error overlay never appeared'));

  await page.waitForTimeout(500);
  const errorOverlay = page.locator('.admin-modal-overlay').first();
  const errorText = (await errorOverlay.count())
    ? (await errorOverlay.innerText().catch(() => ''))
    : '(no overlay)';
  saveText('05-error-modal-text', errorText);
  await elementShot(page, errorOverlay, '05-error-modal');
  await viewportShot(page, '05-error-modal-viewport');
  console.log('  ERROR TEXT:', JSON.stringify(errorText.slice(0, 400)));
}

async function step3EditDropdown(page) {
  console.log('\n[3] Edit cert 26-0001 — Type dropdown filtered behavior');
  // Edit takes a cert NUMBER (string), not id — cert id=2 is "26-0001"
  const resp = await page.goto(`${BASE}/Certificate/Edit/26-0001`);
  console.log('  status:', resp ? resp.status() : '?', 'url:', page.url());
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(1500);

  if (!page.url().includes('/Edit/2')) {
    console.log('  ! Redirected away from Edit/2');
    await viewportShot(page, '06-edit-redirect');
    saveText('06-edit-redirect-url', page.url());
    return;
  }

  const exists = await page.locator('#ProductCategoryId').count();
  if (!exists) {
    saveText('06-edit-no-selector', 'No #ProductCategoryId on page. URL: ' + page.url());
    const heading = await page.locator('h1, h2').first().innerText().catch(() => '');
    saveText('06-edit-heading', heading);
    await viewportShot(page, '06-edit-noselector');
    return;
  }

  await page.waitForFunction(() => {
    const sel = document.querySelector('#ProductTypeId');
    return sel && sel.options && sel.options.length > 1;
  }, { timeout: 10000 }).catch(() => console.log('  ! Type dropdown never populated'));

  const initialOpts = await page.$$eval('#ProductTypeId option', els =>
    els.map(o => ({ value: o.value, text: o.textContent.trim() })));
  const catVal = await page.$eval('#ProductCategoryId', s => s.value);
  const catText = await page.$eval('#ProductCategoryId option:checked', o => o.textContent.trim()).catch(() => '?');
  saveText('06-edit-initial-state',
    `Category: ${catVal} (${catText})\nType options (${initialOpts.length}):\n` +
    initialOpts.map(o => `  ${o.value || '-'} → ${o.text}`).join('\n'));
  console.log('  Initial — Cat:', catVal, '"' + catText + '"', '| Type opts:', initialOpts.length);

  await page.locator('#ProductCategoryId').scrollIntoViewIfNeeded().catch(() => {});
  await viewportShot(page, '07-edit-product-section-zoom');

  // Open Type dropdown for visual proof
  await page.locator('#ProductTypeId').click();
  await page.waitForTimeout(400);
  await viewportShot(page, '08-type-dropdown-open');
  await page.keyboard.press('Escape').catch(() => {});

  // Switch Category and watch Type repopulate
  await page.selectOption('#ProductCategoryId', '1');
  await page.waitForFunction(() => {
    const sel = document.querySelector('#ProductTypeId');
    return sel && sel.options && sel.options.length > 2;
  }, { timeout: 10000 }).catch(() => console.log('  ! Type did not repopulate'));

  const afterOpts = await page.$$eval('#ProductTypeId option', els =>
    els.map(o => ({ value: o.value, text: o.textContent.trim() })));
  saveText('09-edit-after-category-change',
    `Category id=1 (Chemicals and Raw Materials)\nType options now (${afterOpts.length}):\n` +
    afterOpts.map(o => `  ${o.value || '-'} → ${o.text}`).join('\n'));
  console.log('  After switch — Type opts:', afterOpts.length);

  await page.locator('#ProductCategoryId').scrollIntoViewIfNeeded().catch(() => {});
  await viewportShot(page, '10-edit-viewport-after-switch');
  await page.locator('#ProductTypeId').click();
  await page.waitForTimeout(400);
  await viewportShot(page, '11-type-dropdown-after-switch');
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  pageerror:', e.message));
  try {
    await login(page);
    await step1ProductTypes(page);
    await step2ConfirmModalScreenshot(page);
    await step2bExerciseDeleteAndCaptureError(page);
    await step3EditDropdown(page);
    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await viewportShot(page, '99-failure').catch(() => {});
  } finally {
    await browser.close();
  }
})();
