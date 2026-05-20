// QC #170 walkthrough v3 — correct selectors for admin-modal-overlay confirm
// dialog + notification, waitForFunction to settle async dropdown population.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `qc170v3-${STAMP}`);
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
    console.log('  fallback viewport for', name, ':', e.message);
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

// ---- Step 1: Admin → Product Types — show Usage column

async function step1ProductTypes(page) {
  console.log('\n[1] Admin → Product Types');
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

  saveText('01-row-carly-test-category',
    'Cells: ' + JSON.stringify(await carlyCatRow.locator('td').allInnerTexts().catch(() => [])));
  saveText('02-row-carly-test-type',
    'Cells: ' + JSON.stringify(await carlyTypeRow.locator('td').allInnerTexts().catch(() => [])));

  await page.locator('text=Carly Test').first().scrollIntoViewIfNeeded().catch(() => {});
  await viewportShot(page, '03-table-viewport');
}

// ---- Step 2: Delete attempt — capture the confirm dialog AND error modal

async function step2DeleteAttempt(page) {
  console.log('\n[2] Delete Carly Test Type — capture error modal');
  await page.goto(`${BASE}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  await page.locator('button[data-view="table"]').click();
  await page.waitForTimeout(700);

  const delBtn = page.locator('.delete-btn[data-id="53"]').first();
  await delBtn.scrollIntoViewIfNeeded();
  await delBtn.click();

  // Wait for the confirm dialog to render
  await page.waitForSelector('.admin-modal-overlay button[data-action="confirm"]', { timeout: 5000 });
  await page.waitForTimeout(300);
  const confirmOverlay = page.locator('.admin-modal-overlay').first();
  await elementShot(page, confirmOverlay, '04-confirm-modal');
  saveText('04-confirm-modal',
    'Confirm dialog text: ' + (await confirmOverlay.innerText().catch(() => '')));

  await confirmOverlay.locator('button[data-action="confirm"]').click();

  // The first overlay disappears; a new overlay (the error notification) appears.
  // showNotification routes through showConfirmation, so it's another .admin-modal-overlay.
  await page.waitForTimeout(1500); // give server + UI a moment

  // Wait for an overlay that contains "Cannot" or "error" or similar
  const errorOverlay = page.locator('.admin-modal-overlay').first();
  if (await errorOverlay.count()) {
    const errorText = await errorOverlay.innerText().catch(() => '');
    saveText('05-error-modal', errorText);
    await elementShot(page, errorOverlay, '05-error-modal');
    console.log('  ERROR MODAL TEXT:', JSON.stringify(errorText.slice(0, 300)));
  } else {
    saveText('05-error-modal', '(no overlay found)');
  }
  await viewportShot(page, '05-error-modal-viewport');
}

// ---- Step 3: Edit cert 26-0001 — Type dropdown filtered behavior

async function step3EditDropdown(page) {
  console.log('\n[3] Edit cert 26-0001');
  await page.goto(`${BASE}/Certificate/Edit/2`);
  await page.waitForLoadState('networkidle');

  // Wait for the cascading dropdown population to finish.
  // Type dropdown should have > 1 option when AJAX returns.
  await page.waitForFunction(() => {
    const sel = document.querySelector('#ProductTypeId');
    return sel && sel.options && sel.options.length > 1;
  }, { timeout: 10000 }).catch(() => console.log('  ! Type dropdown never populated past 1 option'));

  const initialOpts = await page.$$eval('#ProductTypeId option', els =>
    els.map(o => ({ value: o.value, text: o.textContent.trim() }))
  );
  const catVal = await page.$eval('#ProductCategoryId', s => s.value);
  const catText = await page.$eval('#ProductCategoryId option:checked', o => o.textContent.trim()).catch(() => '?');

  saveText('06-edit-initial-state',
    `Category: ${catVal} (${catText})\n` +
    `Type dropdown options (${initialOpts.length}):\n` +
    initialOpts.map(o => `  ${o.value || '-'} → ${o.text}`).join('\n'));
  console.log('  Cat:', catVal, catText, '| Type opts:', initialOpts.length);

  // Element shot of the product hierarchy block (find the closest section/fieldset)
  const cat = page.locator('#ProductCategoryId');
  const section = cat.locator('xpath=ancestor::*[contains(@class,"form-section") or contains(@class,"card") or self::fieldset][1]').first();
  await elementShot(page, section.count().then(c => c > 0 ? section : cat.locator('xpath=..')), '06-edit-product-section');

  await viewportShot(page, '07-edit-viewport-carly-category');

  // Open the Type dropdown so the screenshot shows the options
  await page.locator('#ProductTypeId').click();
  await page.waitForTimeout(400);
  await viewportShot(page, '08-type-dropdown-open');
  // Close it before next step
  await page.keyboard.press('Escape').catch(() => {});

  // Switch Category to id=1 ("Chemicals and Raw Materials") and watch Type repopulate
  await page.selectOption('#ProductCategoryId', '1');
  await page.waitForFunction(() => {
    const sel = document.querySelector('#ProductTypeId');
    // After repopulation, Type should have multiple options for category 1
    return sel && sel.options && sel.options.length > 2;
  }, { timeout: 10000 }).catch(() => console.log('  ! Type did not repopulate'));

  const afterOpts = await page.$$eval('#ProductTypeId option', els =>
    els.map(o => ({ value: o.value, text: o.textContent.trim() }))
  );
  saveText('09-edit-after-category-change',
    `Category changed to id=1 (Chemicals and Raw Materials)\n` +
    `Type dropdown options now (${afterOpts.length}):\n` +
    afterOpts.map(o => `  ${o.value || '-'} → ${o.text}`).join('\n'));
  console.log('  After switch — Type opts:', afterOpts.length);

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
    await step2DeleteAttempt(page);
    await step3EditDropdown(page);
    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message, e.stack);
    await viewportShot(page, '99-failure').catch(() => {});
  } finally {
    await browser.close();
  }
})();
