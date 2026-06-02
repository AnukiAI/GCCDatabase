// DEF-231 / QC #186 — FIX verification (drives the real cert-Create UI).
//
// Sets Company + Category="Chemicals…" + a Product Type on the form, then uses the inline
// "Add New" footer on the Product Name dropdown to create a product by name. Asserts the
// product now persists under the analyst's chosen category + type (the fix), NOT the
// lowest-sorted default with null type/subcategory (the bug). Pairs with def231-quickadd-repro.js.
//
// LOCALHOST ONLY. Run:  node Scripts/smoke-prod/def231-fix-verify.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const APP_USER = 'admin', APP_PASS = 'TestPass123!';
const STAMP = Date.now();
const NAME = `ZZ_Probe_FixVerify_${STAMP}`;

const LIVE = BASE.includes('gccwebapp') || BASE.includes('greencirclecertified');
// Creates a product row. Localhost by default; live test env only with explicit opt-in.
// Operates on a throwaway ZZ_Probe_* product that it deletes at the end.
if (LIVE && process.env.LIVE_OK !== '1') {
  console.error('REFUSING TO RUN against the live test env without LIVE_OK=1. ' +
    'Creates a throwaway ZZ_Probe_* product and deletes it; re-run with LIVE_OK=1 to allow.');
  process.exit(2);
}
const GATE = LIVE ? { username: 'gcc', password: 'Cert-Test-Access-2026!' } : undefined;

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  let token = null, cookieHeader = '';

  const authHeaders = () => ({ 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token, 'Cookie': cookieHeader });
  const getJson = async (url) => (await ctx.request.get(url)).json().catch(() => null);
  const unwrap = (b) => b?.data ?? b?.Data ?? b;

  try {
    console.log(`\nDEF-231 FIX verification (real UI) — ${BASE}\n`);

    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1200);
    if (/\/Account\/Login/i.test(page.url())) { record('SETUP-login', 'Admin login', 'FAIL'); throw new Error('login failed'); }

    // intended hierarchy: Chemicals + first type under it
    const cats = unwrap(await getJson(`${BASE}/Product/GetCategories`)) || [];
    const defaultCat = cats[0];
    const chem = cats.find(c => /chemical/i.test(c.name ?? c.Name)) || cats[1];
    const chemId = chem?.id ?? chem?.Id;
    const types = unwrap(await getJson(`${BASE}/Certificate/GetProductTypes?categoryId=${chemId}`)) || [];
    const type = types[0];
    const typeId = type?.id ?? type?.Id;
    record('SETUP-hierarchy', 'Intended Category + Type chosen', (chemId && typeId) ? 'PASS' : 'FAIL',
      `category="${chem?.name ?? chem?.Name}"(${chemId}), type="${type?.name}"(${typeId}); default="${defaultCat?.name ?? defaultCat?.Name}"`);

    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    token = await page.locator('input[name="__RequestVerificationToken"]').first().getAttribute('value').catch(() => null);
    cookieHeader = (await ctx.cookies()).map(c => `${c.name}=${c.value}`).join('; ');

    // pick the first real company option on the form
    const companyId = await page.evaluate(() => {
      const sel = document.getElementById('CompanyId');
      if (!sel) return null;
      for (const o of sel.options) {
        if (o.value && !o.disabled && !/add new/i.test(o.textContent)) return o.value;
      }
      return null;
    });
    if (!companyId) { record('SETUP-company', 'A company option on the form', 'FAIL'); throw new Error('no company option'); }

    // set Company → triggers product load
    await page.evaluate((cid) => {
      const sel = document.getElementById('CompanyId');
      sel.value = cid; sel.dispatchEvent(new Event('change', { bubbles: true }));
    }, companyId);
    await page.waitForTimeout(1200);

    // set Category (SearchableSelect keeps the native #ProductCategoryId in sync) → cascade loads types
    await page.evaluate((cid) => {
      const sel = document.getElementById('ProductCategoryId');
      sel.value = String(cid); sel.dispatchEvent(new Event('change', { bubbles: true }));
    }, chemId);
    // wait for the type dropdown to populate
    await page.waitForFunction(() => document.getElementById('ProductTypeId')?.options.length > 1, { timeout: 8000 }).catch(() => {});
    await page.evaluate((tid) => {
      const sel = document.getElementById('ProductTypeId');
      sel.value = String(tid); sel.dispatchEvent(new Event('change', { bubbles: true }));
    }, typeId);
    await page.waitForTimeout(800);

    // confirm the native selects hold the analyst's pick (what saveInlineProduct reads)
    const formState = await page.evaluate(() => ({
      cat: document.getElementById('ProductCategoryId')?.value,
      type: document.getElementById('ProductTypeId')?.value,
    }));
    record('DEF231-form-state', 'Form holds analyst pick before quick-add',
      (formState.cat === String(chemId) && formState.type === String(typeId)) ? 'PASS' : 'FAIL',
      `#ProductCategoryId=${formState.cat} (want ${chemId}), #ProductTypeId=${formState.type} (want ${typeId})`);

    // drive the inline "Add New" footer on the Product Name dropdown
    const wrapper = page.locator('.custom-dropdown-wrapper', { has: page.locator('#ProductId') });
    await wrapper.locator('.custom-dropdown-trigger').click();
    await page.waitForTimeout(300);
    await wrapper.locator('.custom-dropdown-add-input').fill(NAME);
    const [resp] = await Promise.all([
      page.waitForResponse(r => r.url().includes('/Product/SaveProduct') && r.request().method() === 'POST', { timeout: 10000 }).catch(() => null),
      wrapper.locator('.custom-dropdown-add-btn').click(),
    ]);
    await page.waitForTimeout(1000);
    record('DEF231-quickadd-fired', 'Inline Add New posted to /Product/SaveProduct', resp ? 'PASS' : 'FAIL',
      resp ? `http=${resp.status()}` : 'no SaveProduct POST observed');

    // read back the created product
    const all = unwrap(await getJson(`${BASE}/Product/GetProducts`)) || [];
    const prod = all.find(p => (p.name ?? p.Name) === NAME);
    if (!prod) {
      record('DEF231-readback', 'Created product found', 'FAIL', `"${NAME}" not in GetProducts`);
    } else {
      const savedCat = prod.categoryId ?? prod.CategoryId;
      const savedType = prod.productTypeId ?? prod.ProductTypeId;
      record('DEF231-FIX-category', 'Product saved under the analyst’s category (not the default)',
        (savedCat === chemId && savedCat !== (defaultCat?.id ?? defaultCat?.Id)) ? 'PASS' : 'FAIL',
        `saved category="${prod.categoryName ?? prod.CategoryName}" (want "${chem?.name ?? chem?.Name}")`);
      record('DEF231-FIX-type', 'Product saved with the analyst’s product type',
        (savedType === typeId) ? 'PASS' : 'FAIL',
        `saved type="${(prod.productTypeName ?? prod.ProductTypeName) || '(empty)'}" (want "${type?.name}")`);
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    // cleanup — sweep ZZ_Probe_FixVerify_* (DeleteProduct is [HttpDelete])
    try {
      const all = unwrap(await getJson(`${BASE}/Product/GetProducts`)) || [];
      const probes = all.filter(p => typeof (p.name ?? p.Name) === 'string' && (p.name ?? p.Name).startsWith('ZZ_Probe_FixVerify_'));
      let removed = 0, failed = 0;
      for (const p of probes) {
        const d = await ctx.request.delete(`${BASE}/Product/DeleteProduct/${p.id ?? p.Id}`, { headers: authHeaders() });
        const db = await d.json().catch(() => null);
        if ((db?.success ?? db?.Success)) removed++; else failed++;
      }
      record('DEF231-cleanup', 'Throwaway products removed', failed === 0 ? 'PASS' : 'WARN', `removed=${removed}, failed=${failed}`);
    } catch (ce) { record('DEF231-cleanup', 'Cleanup', 'WARN', ce.message); }
    fs.writeFileSync(path.join(OUT, 'def231-fix-verify-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
  process.exit(fail > 0 ? 1 : 0);
})();
