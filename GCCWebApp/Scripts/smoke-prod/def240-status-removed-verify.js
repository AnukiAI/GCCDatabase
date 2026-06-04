// DEF-240 — Status removed from Product Management (table + Add New Product popup + filter bar).
// Verifies on localhost:5101 (admin):
//   S0  Page loads, products table renders
//   S1  Table has NO "Status" column header
//   S2  Filter bar has NO Status filter (#status-filter absent)
//   S3  Add New Product modal has NO #product-status field
//   S4  Modal still has the expected fields (name, company, category, subcategory)
//   S5  No console / page errors on load + modal open
//   Screenshots: full page + modal open.
//
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/def240-status-removed-verify.js
const { chromium } = require('playwright');
const { login } = require('./_lib/smoke-template');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const OUT = path.join(__dirname, '_results');
const results = [];
const rec = (id, desc, status, detail = '') => {
  results.push({ id, desc, status, detail });
  console.log(`[${status}] ${id}  ${desc}${detail ? '  — ' + detail : ''}`);
};

(async () => {
  if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const consoleErrors = [];
  const pageErrors = [];
  page.on('console', (m) => { if (m.type() === 'error' && !/favicon/i.test(m.text())) consoleErrors.push(m.text()); });
  page.on('pageerror', (e) => pageErrors.push(e.message));

  try {
    await login(page, 'admin');
    const resp = await page.goto(`${BASE}/Product/Management`, { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(800);
    rec('S0', 'Product Management loads + table present',
      (resp && resp.status() === 200) && (await page.locator('#product-table, table').count()) > 0 ? 'PASS' : 'FAIL',
      `http=${resp ? resp.status() : 'n/a'}`);

    // S1: no Status column header
    const headers = await page.evaluate(() =>
      Array.from(document.querySelectorAll('thead th')).map(th => th.textContent.trim().replace(/\s+/g, ' ')));
    const hasStatusHeader = headers.some(h => /^status\b/i.test(h));
    rec('S1', 'Table has NO Status column', hasStatusHeader ? 'FAIL' : 'PASS', `headers=[${headers.join(' | ')}]`);

    // S2: no Status filter
    const statusFilter = await page.locator('#status-filter').count();
    rec('S2', 'Filter bar has NO Status filter', statusFilter === 0 ? 'PASS' : 'FAIL', `#status-filter count=${statusFilter}`);

    // Screenshot the page (no modal)
    await page.screenshot({ path: path.join(OUT, 'def240-page.png'), fullPage: true });

    // Open Add New Product modal
    await page.locator('#add-product-btn, button:has-text("Add New Product"), [onclick*="showAddProductModal"]').first().click();
    await page.waitForTimeout(500);

    // S3: no #product-status in modal
    const statusField = await page.locator('#product-status').count();
    rec('S3', 'Add New Product modal has NO Status field', statusField === 0 ? 'PASS' : 'FAIL', `#product-status count=${statusField}`);

    // S4: expected fields still present
    const fields = await page.evaluate(() => ({
      name: !!document.getElementById('product-name'),
      company: !!document.getElementById('product-company'),
      category: !!document.getElementById('product-category'),
      type: !!document.getElementById('product-type'),
      subcategory: !!document.getElementById('product-subcategory'),
      description: !!document.getElementById('product-description'),
    }));
    const allCore = fields.name && fields.company && fields.category && fields.subcategory;
    rec('S4', 'Modal retains core fields (name/company/category/subcategory)', allCore ? 'PASS' : 'FAIL', JSON.stringify(fields));

    await page.screenshot({ path: path.join(OUT, 'def240-modal.png'), fullPage: true });

    // S5: no console / page errors
    rec('S5', 'No console / page errors', (consoleErrors.length === 0 && pageErrors.length === 0) ? 'PASS' : 'FAIL',
      [...consoleErrors, ...pageErrors].slice(0, 3).join(' | ') || 'clean');

  } catch (err) {
    rec('ERR', 'Script error', 'FAIL', err.message);
  } finally {
    const pass = results.filter(r => r.status === 'PASS').length;
    const fail = results.filter(r => r.status === 'FAIL').length;
    console.log(`\n=== DEF-240 verify: ${pass} PASS / ${fail} FAIL ===`);
    fs.writeFileSync(path.join(OUT, 'def240-results.json'), JSON.stringify({ pass, fail, results }, null, 2));
    await browser.close();
    process.exit(fail > 0 ? 1 : 0);
  }
})();
