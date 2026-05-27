const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const BASE = 'https://gccwebapp.com';
const CERT = '26-0160';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  const consoleErrs = [];
  page.on('console', m => { if (m.type() === 'error') consoleErrs.push(m.text().slice(0, 250)); });
  page.on('pageerror', e => consoleErrs.push('PAGEERROR: ' + e.message.slice(0, 250)));

  // 1. Login
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('analyst');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type=submit]').first().click(),
  ]);
  await page.waitForTimeout(2500);

  // 2. Go to Edit page for 26-0160
  const editUrl = `${BASE}/Certificate/Edit/${CERT}`;
  const resp = await page.goto(editUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(4000); // wait for JS PID manager to init

  const status = resp.status();
  const finalUrl = page.url();
  const title = await page.title();

  // 3. Probe DOM for bulk-delete artifacts
  const probe = await page.evaluate(() => {
    const q = sel => document.querySelector(sel);
    const qa = sel => Array.from(document.querySelectorAll(sel));

    const bulkBtn = q('#bulkDeleteProductIds');
    const selectAll = q('#selectAllProductIds');
    const rowBoxes = qa('.bulk-select-checkbox');
    const tableRows = qa('#productIdsTableBody tr');
    const tableHead = q('#productIdsTable thead tr');
    const productIdRows = qa('#productIdsTableBody tr[data-product-id]');

    return {
      productIdManagerPresent: !!q('.product-id-manager'),
      bulkDeleteBtn: bulkBtn ? {
        exists: true,
        display: bulkBtn.style.display,
        computed: getComputedStyle(bulkBtn).display,
        text: bulkBtn.innerText.trim().slice(0, 60),
      } : { exists: false },
      selectAllCheckbox: selectAll ? {
        exists: true,
        visible: getComputedStyle(selectAll).display !== 'none',
      } : { exists: false },
      rowCheckboxCount: rowBoxes.length,
      productIdRowCount: productIdRows.length,
      tableRowCount: tableRows.length,
      headerCells: tableHead ? tableHead.children.length : 0,
      headerHTML: tableHead ? tableHead.innerHTML.slice(0, 600) : null,
      firstRowHTML: productIdRows[0] ? productIdRows[0].innerHTML.slice(0, 600) : null,
      jsPidManagerLoaded: typeof window.productIdManager !== 'undefined',
      pidManagerIsReadOnly: typeof window.productIdManager !== 'undefined' ? window.productIdManager.isReadOnly : null,
    };
  });

  // 4. Try clicking a row checkbox and verify button appears
  let postClick = null;
  if (probe.rowCheckboxCount > 0) {
    await page.locator('.bulk-select-checkbox').first().check();
    await page.waitForTimeout(500);
    postClick = await page.evaluate(() => {
      const btn = document.querySelector('#bulkDeleteProductIds');
      const countSpan = document.querySelector('#bulkDeleteCount');
      return {
        btnDisplay: btn ? btn.style.display : null,
        btnComputed: btn ? getComputedStyle(btn).display : null,
        countText: countSpan ? countSpan.textContent : null,
      };
    });
  }

  // 5. Screenshot
  await page.screenshot({ path: path.join(OUT, `bulk-${CERT}-initial.png`), fullPage: true });

  // Scroll to PID manager and tighter screenshot
  const pidLoc = page.locator('.product-id-manager').first();
  if (await pidLoc.count() > 0) {
    await pidLoc.scrollIntoViewIfNeeded();
    await page.waitForTimeout(500);
    await pidLoc.screenshot({ path: path.join(OUT, `bulk-${CERT}-pidmanager.png`) }).catch(() => {});
  }

  const result = {
    cert: CERT,
    httpStatus: status,
    finalUrl,
    title,
    probe,
    postCheckboxClick: postClick,
    consoleErrors: consoleErrs.slice(0, 10),
  };
  fs.writeFileSync(path.join(OUT, `bulk-${CERT}-results.json`), JSON.stringify(result, null, 2));
  console.log(JSON.stringify(result, null, 2));

  await browser.close();
})();
