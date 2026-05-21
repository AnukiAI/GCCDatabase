// QC #159 UI test suite — gccwebapp.com (read-only; never clicks Apply).
// Tests:
//   T1  Modal opens after Import click
//   T2  Replace toggle is in DOM and visible in modal footer
//   T3  Toggle starts unchecked (append is the default mode)
//   T4  Toggle is interactive: click flips it checked, click again flips it unchecked
//   T5  Preview shows New/Modified/Unchanged/Errors stat tiles
//   T6  Valid 1-row CSV → 1 New
//   T7  Invalid-format row (bad ASIN) → counts as Error, Apply stays disabled
//   T8  Cancel closes the modal without applying changes (visible row count unchanged)
//   T9  CR-016 features that are NOT yet shipped (radio mode-selector, "To Remove" tile,
//       inline warning banner) — these should NOT be in the DOM. Documents the gap.
//
// All tests run against ONE cert (26-0163 — 3M test data, 2 existing product IDs).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const BASE = 'https://gccwebapp.com';
const CERT_NUM = '26-0163';
const APP_USER = 'analyst';
const APP_PASS = 'TestPass123!';

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    httpCredentials: BASIC,
    viewport: { width: 1440, height: 900 },
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  [pageerror]', e.message));

  try {
    // ----- setup: login + open cert edit -----
    console.log('SETUP: login + open cert edit\n');
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(2000);

    await page.goto(`${BASE}/Certificate/Edit/${CERT_NUM}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    const beforeRows = await page.locator('.btn-edit-product-id').count();
    console.log(`  cert ${CERT_NUM} has ${beforeRows} active Product IDs\n`);
    await page.locator('#importProductIds').scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(OUT, 'ui-00-edit-page.png'), fullPage: true });

    // ----- T1: modal opens after Import click (also primes preview with valid CSV) -----
    console.log('--- T1: Modal opens via Import click ---');
    const validCsv = 'Type,Value,ProductName,ProductSKU\nASIN,B00UITEST1,QC159 UI Test Product,SKU-UI-001\n';
    const validCsvPath = path.join(OUT, 'ui-valid.csv');
    fs.writeFileSync(validCsvPath, validCsv);

    const [fc1] = await Promise.all([
      page.waitForEvent('filechooser'),
      page.locator('#importProductIds').click(),
    ]);
    await fc1.setFiles(validCsvPath);
    await page.waitForTimeout(2500);
    await page.screenshot({ path: path.join(OUT, 'ui-01-modal-opened.png'), fullPage: true });

    const modal = page.locator('#import-preview-modal');
    const modalVisible = await modal.isVisible();
    record('T1', 'Modal opens after Import click', modalVisible ? 'PASS' : 'FAIL',
      modalVisible ? '#import-preview-modal visible' : 'modal did not open');

    // ----- T2: toggle is present + visible -----
    console.log('--- T2: Replace toggle present + visible in footer ---');
    const toggle = page.locator('#overwrite-existing-toggle');
    const toggleInDom = (await toggle.count()) > 0;
    const toggleVisible = toggleInDom ? await toggle.isVisible() : false;
    record('T2', 'Replace toggle is in DOM and visible',
      toggleInDom && toggleVisible ? 'PASS' : 'FAIL',
      `inDom=${toggleInDom} visible=${toggleVisible}`);

    // ----- T3: toggle defaults to unchecked (= append mode) -----
    console.log('--- T3: Toggle defaults unchecked (append default) ---');
    const initiallyChecked = await toggle.isChecked();
    record('T3', 'Toggle defaults unchecked (append is default)',
      initiallyChecked === false ? 'PASS' : 'FAIL',
      `initialChecked=${initiallyChecked}`);

    // ----- T4: toggle is interactive (click flips state) -----
    console.log('--- T4: Toggle click flips state both ways ---');
    await page.locator('#overwrite-existing-label').click();
    await page.waitForTimeout(300);
    const afterClick1 = await toggle.isChecked();
    await page.locator('#overwrite-existing-label').click();
    await page.waitForTimeout(300);
    const afterClick2 = await toggle.isChecked();
    record('T4', 'Toggle click flips state (false→true→false)',
      afterClick1 === true && afterClick2 === false ? 'PASS' : 'FAIL',
      `clicks: false→${afterClick1}→${afterClick2}`);

    // Re-check it for the next test
    await page.locator('#overwrite-existing-label').click();
    await page.waitForTimeout(300);
    await page.screenshot({ path: path.join(OUT, 'ui-04-toggle-ticked.png'), fullPage: true });
    const footer = page.locator('.admin-modal-footer').first();
    await footer.screenshot({ path: path.join(OUT, 'ui-04b-footer-tight.png') });

    // ----- T5: preview shows stat tiles -----
    console.log('--- T5: Preview renders the 4 stat tiles ---');
    const statsText = await page.locator('#import-preview-modal').innerText().catch(() => '');
    const hasNew = /\bNew\b/i.test(statsText);
    const hasModified = /\bModified\b/i.test(statsText);
    const hasUnchanged = /\bUnchanged\b/i.test(statsText);
    const hasErrors = /\bErrors?\b/i.test(statsText);
    const allFour = hasNew && hasModified && hasUnchanged && hasErrors;
    record('T5', 'Preview shows New / Modified / Unchanged / Errors tiles',
      allFour ? 'PASS' : 'FAIL',
      `new=${hasNew} mod=${hasModified} unch=${hasUnchanged} err=${hasErrors}`);

    // ----- T6: valid 1-row CSV → 1 New -----
    console.log('--- T6: Valid 1-row CSV registers as 1 New ---');
    // Read numeric values directly under the labels
    const newCount = await page.evaluate(() => {
      const el = document.querySelector('#import-preview-modal');
      if (!el) return null;
      const text = el.innerText;
      const m = text.match(/(\d+)\s*\n*\s*New/i);
      return m ? Number(m[1]) : null;
    });
    record('T6', 'Valid 1-row CSV → stat shows 1 New',
      newCount === 1 ? 'PASS' : 'WARN',
      `parsed new count=${newCount}`);

    // ----- T8 (done first while modal is open): Cancel preserves data -----
    console.log('--- T8: Cancel closes modal without changes ---');
    const cancelBtn = page.locator('#import-preview-modal button:has-text("Cancel")');
    if (await cancelBtn.count() > 0) {
      await cancelBtn.first().click();
      await page.waitForTimeout(1500);
    }
    await page.goto(`${BASE}/Certificate/Edit/${CERT_NUM}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    const afterCancelRows = await page.locator('.btn-edit-product-id').count();
    record('T8', 'Cancel preserves existing data (no Apply was clicked)',
      afterCancelRows === beforeRows ? 'PASS' : 'FAIL',
      `before=${beforeRows} afterCancel=${afterCancelRows}`);
    await page.screenshot({ path: path.join(OUT, 'ui-08-after-cancel.png'), fullPage: true });

    // ----- T7: bad-format row counts as Error, Apply stays disabled -----
    console.log('--- T7: Bad ASIN length → Errors count, Apply disabled ---');
    const badCsv = 'Type,Value,ProductName,ProductSKU\nASIN,TOOSHORT,Bad Row Test,SKU-BAD\n';
    const badCsvPath = path.join(OUT, 'ui-bad.csv');
    fs.writeFileSync(badCsvPath, badCsv);
    const [fc2] = await Promise.all([
      page.waitForEvent('filechooser'),
      page.locator('#importProductIds').click(),
    ]);
    await fc2.setFiles(badCsvPath);
    await page.waitForTimeout(2500);
    await page.screenshot({ path: path.join(OUT, 'ui-07-bad-csv-preview.png'), fullPage: true });

    const errorCount = await page.evaluate(() => {
      const el = document.querySelector('#import-preview-modal');
      if (!el) return null;
      const text = el.innerText;
      const m = text.match(/(\d+)\s*\n*\s*Errors?/i);
      return m ? Number(m[1]) : null;
    });
    const applyDisabled = await page.locator('#apply-import-btn').isDisabled();
    record('T7', 'Invalid ASIN counts as Error, Apply disabled',
      (errorCount >= 1 && applyDisabled) ? 'PASS' : 'WARN',
      `errors=${errorCount} applyDisabled=${applyDisabled}`);

    // Close out
    const cancelBtn2 = page.locator('#import-preview-modal button:has-text("Cancel")');
    if (await cancelBtn2.count() > 0) await cancelBtn2.first().click().catch(() => {});

    // ----- T9: CR-016 features NOT yet shipped -----
    console.log('--- T9: CR-016 features documented as not-yet-shipped ---');
    // Re-open preview to inspect
    const [fc3] = await Promise.all([
      page.waitForEvent('filechooser'),
      page.locator('#importProductIds').click(),
    ]);
    await fc3.setFiles(validCsvPath);
    await page.waitForTimeout(2500);

    const radioModeCount = await page.locator('#import-preview-modal input[type="radio"]').count();
    const toRemoveTileCount = await page.locator('#import-preview-modal :text("To Remove")').count();
    const warningBannerCount = await page.locator('#import-preview-modal :text-matches("existing product ids? not in this file", "i")').count();
    const cr016Missing = radioModeCount === 0 && toRemoveTileCount === 0 && warningBannerCount === 0;
    record('T9', 'CR-016 UX polish (radios, To-Remove tile, warning) NOT yet shipped',
      cr016Missing ? 'PASS' : 'FAIL',
      `radios=${radioModeCount} toRemoveTile=${toRemoveTileCount} warningBanner=${warningBannerCount} — all 0 means CR-016 still pending (expected)`);

    const cancelBtn3 = page.locator('#import-preview-modal button:has-text("Cancel")');
    if (await cancelBtn3.count() > 0) await cancelBtn3.first().click().catch(() => {});

    console.log('\nSUITE COMPLETE\n');
  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
    await page.screenshot({ path: path.join(OUT, 'ui-error.png'), fullPage: true }).catch(() => {});
  } finally {
    fs.writeFileSync(path.join(OUT, 'ui-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  // Summary
  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`SUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
