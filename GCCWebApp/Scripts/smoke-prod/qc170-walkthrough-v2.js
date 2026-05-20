// QC #170 walkthrough v2 — targeted element screenshots + reliable text capture.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `qc170v2-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

const ADMIN_USER = 'admin';
const ADMIN_PASS = 'TestPass123!';

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
    console.log('  el shot fallback (viewport) for', name, ':', e.message);
    await viewportShot(page, name);
  }
}
function saveText(name, text) {
  fs.writeFileSync(path.join(OUT, `${name}.txt`), text || '(empty)');
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
  console.log('\n[1] Admin → Product Types — Usage column for Carly Test row');
  await page.goto(`${BASE}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  await page.locator('button[data-view="table"]').click();
  await page.waitForTimeout(800);

  // Carly Test category row (use cell with text "Carly Test" — exact match)
  const carlyCatRow = page.locator('tr', { has: page.locator('td:has-text("Carly Test")').first() }).first();
  await elementShot(page, carlyCatRow, '01-row-carly-test-category');

  // Carly Test Type row (Level 2 under Carly Test)
  const carlyTypeRow = page.locator('tr', { has: page.locator('td:has-text("Carly Test Type")').first() }).first();
  await elementShot(page, carlyTypeRow, '02-row-carly-test-type');

  // Full table viewport for context
  await page.locator('text=Carly Test').first().scrollIntoViewIfNeeded().catch(() => {});
  await viewportShot(page, '03-table-viewport');

  // Capture the visible text of both rows (cell-by-cell) for the email
  const carlyCatCells = await carlyCatRow.locator('td').allInnerTexts().catch(() => []);
  const carlyTypeCells = await carlyTypeRow.locator('td').allInnerTexts().catch(() => []);
  saveText('01-row-carly-test-category', 'Cells: ' + JSON.stringify(carlyCatCells));
  saveText('02-row-carly-test-type', 'Cells: ' + JSON.stringify(carlyTypeCells));

  // Also dump the table header so we know which column is "Usage"
  const headers = await page.locator('table thead th').allInnerTexts().catch(() => []);
  saveText('00-table-headers', JSON.stringify(headers));
}

async function step2DeleteAttempt(page) {
  console.log('\n[2] Try to delete Carly Test Type — capture error');
  await page.goto(`${BASE}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  await page.locator('button[data-view="table"]').click();
  await page.waitForTimeout(700);

  const delBtn = page.locator('.delete-btn[data-id="53"]').first();
  await delBtn.scrollIntoViewIfNeeded();
  await delBtn.click();
  await page.waitForTimeout(600);
  // Confirm modal
  const modal = page.locator('.modal:visible, [role="dialog"]:visible').first();
  await elementShot(page, modal, '04-confirm-modal');

  // Capture all visible buttons in the modal so we know which one to press
  const modalButtons = await modal.locator('button').allInnerTexts().catch(() => []);
  saveText('04-modal-buttons', JSON.stringify(modalButtons));

  // Click the confirmation button (text="Delete" or "Yes" or .btn-danger)
  const confirmBtn = modal.locator('.btn-danger, button:has-text("Delete"), button:has-text("Yes"), button:has-text("Confirm")').last();
  await confirmBtn.click();
  await page.waitForTimeout(2500);

  // Capture any toast / notification / modal that appears with the error
  const toastSelectors = [
    '.toast', '.notification', '.admin-notification',
    '.alert', '.swal2-container', '[role="alert"]', '.notyf__toast'
  ];
  let toastText = '';
  let toastHtml = '';
  for (const sel of toastSelectors) {
    const t = page.locator(sel).first();
    if (await t.count() && await t.isVisible().catch(() => false)) {
      toastText = await t.innerText().catch(() => '');
      toastHtml = await t.innerHTML().catch(() => '');
      console.log('  toast matched selector:', sel, '->', JSON.stringify(toastText.slice(0, 200)));
      break;
    }
  }
  saveText('05-error-toast-text', toastText);
  saveText('05-error-toast-html', toastHtml);
  await viewportShot(page, '05-after-delete-click');

  // Last resort — capture all visible text on the page for forensic
  const bodyText = await page.locator('body').innerText().catch(() => '');
  saveText('05-body-text-snippet', bodyText.split('\n').filter(l => l.trim()).slice(0, 80).join('\n'));
}

async function step3EditDropdown(page) {
  console.log('\n[3] Edit cert 26-0001 — Type dropdown filtered behavior');
  await page.goto(`${BASE}/Certificate/Edit/2`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(3000); // cascading fetches

  // Find the product cat/type/subcat selects under whatever section heading
  const catSelect = page.locator('#ProductCategoryId, select[name="ProductCategoryId"]').first();
  const typeSelect = page.locator('#ProductTypeId, select[name="ProductTypeId"]').first();
  const subSelect = page.locator('#ProductSubcategoryId, select[name="ProductSubcategoryId"]').first();

  // Confirm Category is set to Carly Test
  const catVal = await catSelect.inputValue().catch(() => '?');
  const catText = await catSelect.locator('option:checked').innerText().catch(() => '?');
  const typeVal = await typeSelect.inputValue().catch(() => '?');
  const typeText = await typeSelect.locator('option:checked').innerText().catch(() => '?');
  const typeOpts = await typeSelect.locator('option').allInnerTexts().catch(() => []);
  saveText('06-edit-initial-state',
    `Category value: ${catVal} (${catText})\n` +
    `Type value: ${typeVal} (${typeText})\n` +
    `Type dropdown options: ${JSON.stringify(typeOpts)}\n`);
  console.log('  initial cat:', catVal, catText, '| type:', typeVal, typeText, '| typeOpts count:', typeOpts.length);

  // Element shot of the Cat+Type+Sub block — find their common container
  const productSection = catSelect.locator('xpath=ancestor::*[self::div or self::fieldset][1]').first();
  await elementShot(page, productSection, '06-edit-product-section-carly');
  await viewportShot(page, '07-edit-viewport-carly');

  // Now open the dropdown literally to show "only Carly Test Type" as an option
  await typeSelect.click().catch(() => {});
  await page.waitForTimeout(300);
  await viewportShot(page, '08-type-dropdown-open');

  // Switch Category to "Chemicals and Raw Materials" (id=1)
  await catSelect.selectOption('1');
  // Wait for the AJAX repopulation of Type dropdown
  await page.waitForTimeout(2000);
  const newTypeOpts = await typeSelect.locator('option').allInnerTexts().catch(() => []);
  saveText('09-edit-after-category-change',
    `Category changed to id=1 (Chemicals and Raw Materials)\n` +
    `Type dropdown options now: ${JSON.stringify(newTypeOpts)}\n`);
  console.log('  after switch — typeOpts count:', newTypeOpts.length);

  const productSection2 = catSelect.locator('xpath=ancestor::*[self::div or self::fieldset][1]').first();
  await elementShot(page, productSection2, '09-edit-after-category-change');
  await viewportShot(page, '10-edit-viewport-after-switch');
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
    console.error('FAIL:', e.message);
    await viewportShot(page, '99-failure').catch(() => {});
  } finally {
    await browser.close();
  }
})();
