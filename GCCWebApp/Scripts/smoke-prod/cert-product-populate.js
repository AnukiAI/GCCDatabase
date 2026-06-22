// Reproduces "Product Names not populating" on Certificate Create + Edit.
// Picks a company, then checks: (1) /GetProductsByCompany returns data,
// (2) native #ProductId <select> gets options, (3) the custom dropdown panel
// shows those product rows to the user.
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/cert-product-populate.js
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');
const path = require('path');

const results = [];
const rec = (id, name, status, notes = '') => {
  results.push({ id, status });
  console.log(`  [${status}] ${id} — ${name}${notes ? ' :: ' + notes : ''}`);
};

async function openCompanyAndPick(page) {
  // Company is also a custom dropdown (certificate-inline-create.js).
  const companyTrigger = page.locator('.form-group:has(#CompanySelect) .custom-dropdown-trigger, .form-group:has(#CompanyId) .custom-dropdown-trigger').first();
  // Fallback: find the company select id dynamically.
  const companyInfo = await page.evaluate(() => {
    const labels = Array.from(document.querySelectorAll('label'));
    const lbl = labels.find(l => /company/i.test(l.textContent));
    const sel = lbl ? document.querySelector(`#${lbl.getAttribute('for')}`) : null;
    const opts = sel ? Array.from(sel.options).filter(o => o.value).map(o => ({ v: o.value, t: o.textContent })) : [];
    return { id: sel ? sel.id : null, firstVal: opts[0] ? opts[0].v : null, firstText: opts[0] ? opts[0].t : null, count: opts.length };
  });
  return companyInfo;
}

async function exercise(page, where) {
  const company = await openCompanyAndPick(page);
  rec(`${where}.company-options`, 'Company dropdown has options', company.count > 0 ? 'PASS' : 'FAIL', `count=${company.count}`);
  if (!company.firstVal) return;

  // Directly verify the data source first.
  const api = await page.evaluate(async (cid) => {
    try {
      const r = await fetch(`/Certificate/GetProductsByCompany?companyId=${cid}`, { credentials: 'include' });
      const j = await r.json();
      const data = j.data || j.products || [];
      return { ok: true, count: data.length, sample: data.slice(0, 3).map(p => p.name) };
    } catch (e) { return { ok: false, err: e.message }; }
  }, company.firstVal);
  rec(`${where}.api-products`, 'GetProductsByCompany returns products', api.ok && api.count > 0 ? 'PASS' : 'FAIL', api.ok ? `count=${api.count} sample=[${api.sample}]` : api.err);

  // Now drive the UI: set the company value + fire change like a user selection would.
  await page.evaluate((cid) => {
    const labels = Array.from(document.querySelectorAll('label'));
    const lbl = labels.find(l => /company/i.test(l.textContent));
    const sel = document.querySelector(`#${lbl.getAttribute('for')}`);
    sel.value = cid;
    sel.dispatchEvent(new Event('change', { bubbles: true }));
  }, company.firstVal);
  await page.waitForTimeout(1200); // allow fetch + MutationObserver rebuild

  // Native select populated?
  const native = await page.evaluate(() => {
    const sel = document.getElementById('ProductId');
    return { count: sel ? Array.from(sel.options).filter(o => o.value).length : -1,
             help: (document.getElementById('productHelpText') || {}).textContent || '' };
  });
  rec(`${where}.native-products`, 'Native #ProductId <select> populated', native.count > 0 ? 'PASS' : 'FAIL', `count=${native.count} help="${native.help}"`);

  // Custom dropdown visible to the user? Open product dropdown and count rows.
  const prodTriggerClicked = await page.evaluate(() => {
    const sel = document.getElementById('ProductId');
    const wrapper = sel ? sel.closest('.custom-dropdown-wrapper') : null;
    const trig = wrapper ? wrapper.querySelector('.custom-dropdown-trigger') : null;
    if (trig) { trig.click(); return true; }
    return false;
  });
  await page.waitForTimeout(400);
  const uiRows = await page.evaluate(() => {
    const sel = document.getElementById('ProductId');
    const wrapper = sel ? sel.closest('.custom-dropdown-wrapper') : null;
    const panel = wrapper ? wrapper.querySelector('.custom-dropdown-panel') : null;
    if (!panel) return { rows: -1, triggerText: '' };
    const rows = panel.querySelectorAll('.custom-dropdown-option');
    const trig = wrapper.querySelector('.custom-dropdown-trigger');
    return { rows: rows.length, triggerText: trig ? trig.textContent.trim() : '' };
  });
  rec(`${where}.ui-products`, 'Custom product dropdown shows product rows', uiRows.rows > 0 ? 'PASS' : 'FAIL', `rows=${uiRows.rows} (triggerClicked=${prodTriggerClicked})`);

  await page.screenshot({ path: path.join(__dirname, '_results', `cert-product-populate-${where}.png`), fullPage: true });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1440, height: 1600 } })).newPage();
  try {
    await login(page, 'analyst');

    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(1000);
    await exercise(page, 'create');

    // EDIT: load an existing cert (already has company+product), confirm product still shows.
    const num = await page.evaluate(async (base) => {
      try {
        const r = await fetch(base + '/api/certificate-tiles?page=1&pageSize=20', { credentials: 'include' });
        const j = await r.json();
        const rows = j.data || j.certificates || j.items || [];
        const found = rows.map(x => x.certificateNumber ?? x.CertificateNumber).find(n => n);
        if (found) return found;
      } catch { /* ignore */ }
      return '28-0002';
    }, BASE);
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(num)}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(1200);
    const editProduct = await page.evaluate(() => {
      const sel = document.getElementById('ProductId');
      const wrapper = sel ? sel.closest('.custom-dropdown-wrapper') : null;
      const trig = wrapper ? wrapper.querySelector('.custom-dropdown-trigger') : null;
      return {
        count: sel ? Array.from(sel.options).filter(o => o.value).length : -1,
        selected: sel ? (sel.selectedOptions[0] || {}).textContent || '' : '',
        triggerText: trig ? trig.textContent.trim() : '',
      };
    });
    rec('edit.preselect-product', 'Edit pre-fills the saved product name', editProduct.triggerText && !/select|enter/i.test(editProduct.triggerText) ? 'PASS' : 'FAIL', `trigger="${editProduct.triggerText}" selected="${editProduct.selected}" count=${editProduct.count}`);
    await exercise(page, 'edit');
  } catch (e) {
    rec('fatal', 'ran without throwing', 'FAIL', e.message + '\n' + (e.stack || ''));
  } finally {
    await browser.close();
  }
  const fail = results.filter(r => r.status === 'FAIL').length;
  console.log(`\n[cert-product-populate] ${results.filter(r => r.status === 'PASS').length} PASS / ${fail} FAIL`);
  process.exitCode = fail === 0 ? 0 : 1;
})();
