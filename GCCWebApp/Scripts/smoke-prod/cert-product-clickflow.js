// Reproduces "Product Names not populating" via the REAL user click flow:
// open the Company custom dropdown, CLICK a company option (not a programmatic
// change event), then check whether Product Names populate. Tests several
// companies, not just the first.
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/cert-product-clickflow.js
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');

const results = [];
const rec = (id, status, notes = '') => {
  results.push({ id, status });
  console.log(`  [${status}] ${id}${notes ? ' :: ' + notes : ''}`);
};

async function pickCompanyByClick(page, companyText) {
  // Open the company custom dropdown.
  const ok = await page.evaluate(() => {
    const sel = document.getElementById('CompanyId');
    const wrapper = sel ? sel.closest('.custom-dropdown-wrapper') : null;
    const trig = wrapper ? wrapper.querySelector('.custom-dropdown-trigger') : null;
    if (trig) { trig.click(); return true; }
    return false;
  });
  if (!ok) return { opened: false };
  await page.waitForTimeout(300);
  // Type into the dropdown's search box to filter, then click the matching option.
  await page.evaluate((txt) => {
    const sel = document.getElementById('CompanyId');
    const wrapper = sel.closest('.custom-dropdown-wrapper');
    const search = wrapper.querySelector('input[type="text"], .custom-dropdown-search, input');
    if (search) { search.value = txt; search.dispatchEvent(new Event('input', { bubbles: true })); }
  }, companyText);
  await page.waitForTimeout(400);
  const clicked = await page.evaluate((txt) => {
    const sel = document.getElementById('CompanyId');
    const wrapper = sel.closest('.custom-dropdown-wrapper');
    const panel = wrapper.querySelector('.custom-dropdown-panel');
    const rows = Array.from(panel.querySelectorAll('.custom-dropdown-option'));
    const match = rows.find(r => r.textContent.trim().toLowerCase().includes(txt.toLowerCase())) || rows[0];
    if (match) { match.click(); return match.textContent.trim(); }
    return null;
  }, companyText);
  return { opened: true, clicked };
}

async function checkProducts(page) {
  await page.waitForTimeout(1400); // fetch + rebuild
  return await page.evaluate(() => {
    const sel = document.getElementById('ProductId');
    const wrapper = sel ? sel.closest('.custom-dropdown-wrapper') : null;
    const panel = wrapper ? wrapper.querySelector('.custom-dropdown-panel') : null;
    const trig = wrapper ? wrapper.querySelector('.custom-dropdown-trigger') : null;
    // Open product dropdown to count visible rows.
    if (trig) trig.click();
    const rows = panel ? panel.querySelectorAll('.custom-dropdown-option').length : -1;
    if (trig) trig.click(); // close again
    return {
      nativeCount: sel ? Array.from(sel.options).filter(o => o.value).length : -1,
      uiRows: rows,
      help: (document.getElementById('productHelpText') || {}).textContent || '',
    };
  });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1440, height: 1600 } })).newPage();
  try {
    await login(page, 'analyst');
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(1000);

    // Discover several companies that DO have products (via API) to test the click flow.
    const companies = await page.evaluate(async () => {
      const sel = document.getElementById('CompanyId');
      const opts = Array.from(sel.options).filter(o => o.value);
      const withProducts = [];
      for (const o of opts) {
        if (withProducts.length >= 5) break;
        try {
          const r = await fetch(`/Certificate/GetProductsByCompany?companyId=${o.value}`, { credentials: 'include' });
          const j = await r.json();
          if (j.success && j.data && j.data.length > 0) withProducts.push({ id: o.value, text: o.textContent.trim(), n: j.data.length });
        } catch { /* ignore */ }
      }
      return withProducts;
    });
    console.log('Companies with products to test:', JSON.stringify(companies));

    for (const c of companies) {
      // Fresh page each time to avoid prior-selection state bleed.
      await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(800);
      const pick = await pickCompanyByClick(page, c.text);
      const prod = await checkProducts(page);
      const pass = prod.nativeCount > 0 && prod.uiRows > 0;
      rec(`click "${c.text}" (api n=${c.n})`, pass ? 'PASS' : 'FAIL',
        `picked="${pick.clicked}" native=${prod.nativeCount} ui=${prod.uiRows} help="${prod.help}"`);
    }
  } catch (e) {
    rec('fatal', 'FAIL', e.message + '\n' + (e.stack || ''));
  } finally {
    await browser.close();
  }
  const fail = results.filter(r => r.status === 'FAIL').length;
  console.log(`\n[cert-product-clickflow] ${results.filter(r => r.status === 'PASS').length} PASS / ${fail} FAIL`);
  process.exitCode = fail === 0 ? 0 : 1;
})();
