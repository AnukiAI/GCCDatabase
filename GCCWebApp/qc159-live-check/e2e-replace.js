// QC #159 — Full E2E test of the Replace-all CSV import flow on LOCALHOST.
// Cert 26-0147 (id=48) has 2 active Product IDs. We upload a CSV with 1 NEW ID
// and the Replace toggle ticked. Expected outcome: cert ends with 1 active ID,
// the 2 prior IDs are deactivated.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = 'http://localhost:5101';
const CERT_NUM = '26-0147';
const APP_USER = 'analyst';
const APP_PASS = 'TestPass123!';

const log = (m) => console.log(`[${new Date().toISOString().slice(11,19)}] ${m}`);
const step = (n, m) => console.log(`\n===== STEP ${n}: ${m} =====`);

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  [pageerror]', e.message));

  try {
    step(1, 'Sign in as analyst');
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(2000);
    log(`  post-login url: ${page.url()}`);

    step(2, `Open /Certificate/Edit/${CERT_NUM}`);
    await page.goto(`${BASE}/Certificate/Edit/${CERT_NUM}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    await page.locator('#importProductIds').scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(OUT, 'e2e-01-edit-page.png'), fullPage: true });

    const beforeRows = await page.locator('.btn-edit-product-id').count();
    log(`  visible product-id rows BEFORE: ${beforeRows}`);

    step(3, 'Click Import → upload 1-row CSV via filechooser');
    // ASIN must be exactly 10 alphanumeric chars (^[A-Z0-9]{10}$). Use a valid one.
    const csv = 'Type,Value,ProductName,ProductSKU\nASIN,B00REPL001,QC159 Replace Test Product,SKU-QC159\n';
    const tmpCsv = path.join(OUT, 'replace.csv');
    fs.writeFileSync(tmpCsv, csv);
    log(`  CSV: ${csv.replace(/\n/g, ' | ')}`);

    const [fc] = await Promise.all([
      page.waitForEvent('filechooser'),
      page.locator('#importProductIds').click(),
    ]);
    await fc.setFiles(tmpCsv);
    log('  file uploaded to chooser');
    await page.waitForTimeout(2500);
    await page.screenshot({ path: path.join(OUT, 'e2e-02-import-preview.png'), fullPage: true });

    step(4, 'Tick the "Replace all existing IDs with this file" toggle');
    const toggle = page.locator('#overwrite-existing-toggle');
    log(`  toggle present: ${(await toggle.count()) > 0}`);
    log(`  toggle visible BEFORE click: ${await toggle.isVisible()}`);
    log(`  toggle checked BEFORE: ${await toggle.isChecked()}`);
    // Click the label so the visual confirmation is recorded (the input itself is small)
    await page.locator('#overwrite-existing-label').click();
    await page.waitForTimeout(500);
    log(`  toggle checked AFTER click: ${await toggle.isChecked()}`);
    await page.screenshot({ path: path.join(OUT, 'e2e-03-toggle-ticked.png'), fullPage: true });
    // tight crop of the footer with toggle
    const footer = page.locator('.admin-modal-footer').first();
    await footer.screenshot({ path: path.join(OUT, 'e2e-03b-footer-toggle.png') });

    step(5, 'Click Apply Import');
    const applyBtn = page.locator('#apply-import-btn');
    const applyEnabled = await applyBtn.isEnabled();
    log(`  Apply Import enabled: ${applyEnabled}`);
    if (!applyEnabled) {
      log('  Apply is disabled — likely the row is in error state. Capturing state…');
      const stats = await page.locator('.modal-stat, .stat-tile, .preview-stat').allTextContents().catch(() => []);
      log(`  stats text: ${JSON.stringify(stats)}`);
    }
    // Wait for the API response so we know the save round-tripped
    const importRespP = page.waitForResponse(r => /\/api\/.*ProductId|ProductIds/i.test(r.url()) && r.request().method() !== 'GET', { timeout: 15000 }).catch(() => null);
    await applyBtn.click();
    const importResp = await importRespP;
    if (importResp) {
      log(`  POST ${importResp.url()} → ${importResp.status()}`);
    } else {
      log('  (no obvious import API response observed)');
    }
    await page.waitForTimeout(3000);
    await page.screenshot({ path: path.join(OUT, 'e2e-04-after-apply.png'), fullPage: true });

    step(6, 'Reload edit page and capture AFTER state');
    await page.goto(`${BASE}/Certificate/Edit/${CERT_NUM}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    const afterRows = await page.locator('.btn-edit-product-id').count();
    log(`  visible product-id rows AFTER: ${afterRows}`);

    // Grab all product-id values visible in the manager
    const afterValues = await page.locator('.product-id-value').allTextContents();
    log(`  visible IDs AFTER: ${JSON.stringify(afterValues)}`);
    await page.locator('#importProductIds').scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(OUT, 'e2e-05-after-reload.png'), fullPage: true });

    log('\nE2E COMPLETE');
  } catch (e) {
    log(`ERROR: ${e.message}`);
    await page.screenshot({ path: path.join(OUT, 'e2e-error.png'), fullPage: true }).catch(() => {});
  } finally {
    await browser.close();
  }
})();
