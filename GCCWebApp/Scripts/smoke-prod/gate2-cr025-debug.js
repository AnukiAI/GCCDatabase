const { chromium } = require('playwright');
const path = require('path');
const BASE = 'http://localhost:5101';
const PWD = 'TestPass123!';
const CERT_NUMBER = '26-1733';
const OUT = path.join(__dirname, '_results', 'gate2-20260720');

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  page.on('console', msg => console.log('PAGE:', msg.type(), msg.text()));
  page.on('pageerror', err => console.log('PAGEERROR:', err.message));

  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.fill('#Email', 'jane.analyst@anukiconsulting.com');
  await page.fill('#Password', PWD);
  await Promise.all([page.waitForLoadState('networkidle').catch(() => {}), page.click('button[type=submit]')]);
  await page.waitForTimeout(1000);

  await page.goto(BASE + '/Certificate/Edit/' + CERT_NUMBER, { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);

  const setupResult = await page.evaluate(() => {
    try {
      if (typeof productIdManager === 'undefined') return { error: 'productIdManager undefined' };
      productIdManager.importPreviewData = [
        { data: { productIdType: 'ASIN', productIdValue: 'B000TEST01', productName: 'Test Product A', productSku: 'SKU1', partner: 'Amazon' }, status: 'new', selected: true, errors: [], changes: [] },
        { data: { productIdType: 'ASIN', productIdValue: 'B000TEST02', productName: 'Test Product B', productSku: 'SKU2', partner: 'Amazon' }, status: 'modified', selected: true, errors: [], changes: [{ field: 'Product Name', oldValue: 'Old B', newValue: 'Test Product B' }] },
        { data: { productIdType: 'ASIN', productIdValue: 'B000TEST03', productName: 'Test Product C', productSku: 'SKU3', partner: 'Amazon' }, status: 'unchanged', selected: false, errors: [], changes: [] },
        { data: { productIdType: 'ASIN', productIdValue: 'BADVALUE', productName: 'Test Product D', productSku: 'SKU4', partner: 'Amazon' }, status: 'error', selected: false, errors: ['Invalid ASIN format'], changes: [] },
      ];
      productIdManager.showImportPreviewModal();
      return { ok: true, modalDisplay: document.getElementById('import-preview-modal')?.style.display, rowCount: document.querySelectorAll('#import-preview-body tr').length, statBoxExists: !!document.getElementById('stat-box-new') };
    } catch (e) {
      return { error: e.message, stack: e.stack };
    }
  });
  console.log('SETUP RESULT:', JSON.stringify(setupResult, null, 2));
  await page.screenshot({ path: path.join(OUT, 'cr025-debug.png'), fullPage: true });

  await browser.close();
})();
