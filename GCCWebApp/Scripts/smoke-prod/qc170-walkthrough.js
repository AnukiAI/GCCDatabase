// QC #170 walkthrough on localhost:5101 against DEF-196 + DEF-197 fixes.
// Captures: (1) Admin → Product Types Usage column for Carly Test row,
// (2) delete-attempt error message, (3) cert listing filter showing the
// tagged cert, (4) Edit form Type dropdown showing only "Carly Test Type"
// when Category is "Carly Test", and (5) the same dropdown repopulated
// after switching Category — to prove the workflow is the path forward.
//
// Prereq: run qc170-setup.sql first (creates Carly Test category id=14,
// Carly Test Type id=53, tags cert 26-0001).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `qc170-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

const ADMIN_USER = 'admin';
const ADMIN_PASS = 'TestPass123!';

async function shot(page, name, full = true) {
  const file = path.join(OUT, `${name}.png`);
  await page.screenshot({ path: file, fullPage: full });
  console.log('  shot:', name);
}

async function login(page) {
  await page.goto(`${BASE}/Account/Login`);
  await page.fill('input[name="Email"]', ADMIN_USER);
  await page.fill('input[name="Password"]', ADMIN_PASS);
  await Promise.all([
    page.waitForLoadState('networkidle'),
    page.click('button[type="submit"], input[type="submit"]')
  ]);
  console.log('  logged in -> ', page.url());
}

async function step1AdminProductTypes(page) {
  console.log('\n[1] /Admin/ProductTypes — Carly Test rows + Usage column');
  await page.goto(`${BASE}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  // Switch to Table View so the count column is visible
  const tableBtn = page.locator('button[data-view="table"]');
  if (await tableBtn.count()) {
    await tableBtn.click();
    await page.waitForTimeout(500);
  }
  // Expand "Carly Test" if it's collapsed
  const carlyRow = page.locator('text=Carly Test').first();
  await carlyRow.scrollIntoViewIfNeeded().catch(() => {});
  await shot(page, '01-product-types-table-view');
  // Tree view too
  const treeBtn = page.locator('button[data-view="tree"]');
  if (await treeBtn.count()) {
    await treeBtn.click();
    await page.waitForTimeout(500);
    // Find the Carly Test branch and expand it
    const expandable = page.locator('text=Carly Test').first();
    if (await expandable.count()) {
      await expandable.click({ force: true }).catch(() => {});
    }
    await page.waitForTimeout(400);
    await shot(page, '02-product-types-tree-view');
  }
}

async function step2DeleteAttempt(page) {
  console.log('\n[2] Attempt delete on Carly Test Type — capture error toast');
  await page.goto(`${BASE}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  // Use Table View — delete buttons are reliable here
  await page.locator('button[data-view="table"]').click();
  await page.waitForTimeout(600);
  // Find the delete button for the Carly Test Type row
  // Rows have data-id; we need the type id (53) per setup
  const delBtn = page.locator('.delete-btn[data-id="53"]').first();
  if (!(await delBtn.count())) {
    console.log('  ! delete button for type id=53 not found, dumping page');
    await shot(page, '02b-no-delete-btn');
    return;
  }
  await delBtn.scrollIntoViewIfNeeded();
  await shot(page, '03-before-delete-click');
  await delBtn.click();
  // Wait for confirmation modal
  await page.waitForTimeout(700);
  await shot(page, '04-delete-confirm-modal');
  // Click confirm. Modal uses AdminCore.showConfirmation — confirm button text "Delete" or "Confirm".
  const confirmBtn = page.locator('button:has-text("Delete"), button:has-text("Confirm"), .btn-danger:has-text("Delete")').last();
  await confirmBtn.click();
  // Wait for toast notification (success or error)
  await page.waitForTimeout(1500);
  await shot(page, '05-delete-error-toast');
  // Also grab the toast text alone for the report
  const toast = page.locator('.toast, .notification, .admin-notification, [class*="toast"]').first();
  let toastText = '';
  if (await toast.count()) {
    toastText = (await toast.innerText().catch(() => '')) || '';
  }
  fs.writeFileSync(path.join(OUT, '05-delete-error-toast.txt'), toastText || '(no toast text captured)');
  console.log('  toast text:', JSON.stringify(toastText));
}

async function step3CertListing(page) {
  console.log('\n[3] Cert listing — filter by Carly Test Type');
  await page.goto(`${BASE}/Certificate/CertificateTileView`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(800);
  await shot(page, '06-cert-listing-initial');
  // Filter via UI if a Type filter exists; otherwise use URL param
  await page.goto(`${BASE}/Certificate/CertificateTileView?productTypeId=53`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(800);
  await shot(page, '07-cert-listing-filtered-carly-test-type');
}

async function step4EditDropdown(page) {
  console.log('\n[4] /Certificate/Edit/2 — Type dropdown filtered behavior');
  await page.goto(`${BASE}/Certificate/Edit/2`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(1500); // wait for cascading fetches
  await shot(page, '08-edit-initial');
  // Open the Type dropdown (just expose its options)
  const typeSelect = page.locator('#ProductTypeId, select[name="ProductTypeId"]').first();
  let typeOptions = [];
  if (await typeSelect.count()) {
    typeOptions = await typeSelect.locator('option').allTextContents();
  }
  fs.writeFileSync(path.join(OUT, '08-type-options-when-carly-category.txt'),
    'Category = Carly Test\nType dropdown options:\n' + typeOptions.map(o => '  ' + o).join('\n'));
  console.log('  Type options (Carly Test category):', typeOptions);

  // Now switch Category to a real one and screenshot the repopulated Type dropdown
  const catSelect = page.locator('#ProductCategoryId, select[name="ProductCategoryId"]').first();
  if (await catSelect.count()) {
    // Find a real category id (1 = "Chemicals and Raw Materials" per seed)
    await catSelect.selectOption('1');
    await page.waitForTimeout(1500);
    await shot(page, '09-edit-after-category-change');
    const newOptions = await typeSelect.locator('option').allTextContents();
    fs.writeFileSync(path.join(OUT, '09-type-options-after-category-change.txt'),
      'Category = Chemicals and Raw Materials (id=1)\nType dropdown options:\n' + newOptions.map(o => '  ' + o).join('\n'));
    console.log('  Type options after category change:', newOptions);
  }
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  pageerror:', e.message));
  try {
    await login(page);
    await step1AdminProductTypes(page);
    await step2DeleteAttempt(page);
    await step3CertListing(page);
    await step4EditDropdown(page);
    console.log('\nScreenshots in:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure-state', true).catch(() => {});
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
})();
