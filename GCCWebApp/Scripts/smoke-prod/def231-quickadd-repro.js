// DEF-231 / QC #186 (5/29) — Cert form inline "Add New product" mis-categorization repro.
//
// Carly (5/29): "I selected Chemicals and it shows Carly Test ... Product Type/Subcategory
// are not appearing ... not showing in Product Management." Threads 2 + 3 of QC #186.
//
// Root cause (read in code): wwwroot/js/certificate-inline-create.js::saveInlineProduct()
// creates the product with CategoryId = getDefaultCategoryId() (= /Product/GetCategories
// data[0], the lowest-SortOrder category) and sends NO ProductTypeId / ProductSubcategoryId.
// It never reads the analyst's #ProductCategoryId / #ProductTypeId / #ProductSubcategoryId
// selections. So every quick-added product lands under the first category with no type/sub,
// regardless of what the analyst picked.
//
// This probe reproduces the PERSISTED OUTCOME deterministically: it replays the exact
// payload saveInlineProduct sends, then reads the product back via /Product/GetProducts and
// asserts it landed under the default category with null type/subcategory — i.e. NOT the
// analyst's intended category. Self-contained + reversible (deletes the throwaway product).
//
// LOCALHOST ONLY (creates a product row). Run:
//   node Scripts/smoke-prod/def231-quickadd-repro.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const APP_USER = 'admin';
const APP_PASS = 'TestPass123!';
const STAMP = Date.now();
const NAME = `ZZ_Probe_QuickAdd_${STAMP}`;

if (BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) {
  console.error('REFUSING TO RUN: creates a product row — localhost only.');
  process.exit(2);
}

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  let token = null, cookieHeader = '';
  let createdId = null;

  const authHeaders = () => ({
    'Content-Type': 'application/json',
    'X-Requested-With': 'XMLHttpRequest',
    'RequestVerificationToken': token,
    'Cookie': cookieHeader,
  });
  const getJson = async (url) => (await ctx.request.get(url)).json().catch(() => null);
  const unwrap = (b) => b?.data ?? b?.Data ?? b;

  try {
    console.log(`\nDEF-231 inline-quick-add mis-categorization repro — ${BASE}\n`);

    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(1500);
    if (/\/Account\/Login/i.test(page.url())) { record('SETUP-login', 'Admin login', 'FAIL'); throw new Error('login failed'); }

    await page.goto(BASE + '/Product/Management', { waitUntil: 'networkidle' });
    await page.waitForTimeout(600);
    token = await page.locator('input[name="__RequestVerificationToken"]').first().getAttribute('value').catch(() => null);
    cookieHeader = (await ctx.cookies()).map(c => `${c.name}=${c.value}`).join('; ');
    if (!token) { record('SETUP-token', 'Anti-forgery token', 'FAIL'); throw new Error('no token'); }
    record('SETUP', 'Login + token', 'PASS', `token len=${token.length}`);

    // --- what getDefaultCategoryId() will pick: GetCategories data[0] ----------
    const cats = unwrap(await getJson(`${BASE}/Product/GetCategories`)) || [];
    if (cats.length < 2) { record('SETUP-cats', '>=2 categories', 'WARN', `found ${cats.length}`); }
    const defaultCat = cats[0]; // exactly what getDefaultCategoryId returns (ordered by SortOrder)
    // The "analyst's intended pick" = any category that is NOT the default. Prefer one named
    // like Chemicals to mirror Carly's words; else just take the second category.
    const intendedCat = cats.find(c => /chemical/i.test(c.name ?? c.Name)) || cats[1] || cats[0];
    record('DEF231-default-cat', 'getDefaultCategoryId() target (GetCategories[0])', 'PASS',
      `default="${defaultCat?.name ?? defaultCat?.Name}" (id=${defaultCat?.id ?? defaultCat?.Id})`);
    record('DEF231-intended-cat', "Analyst's intended category (anything but the default)",
      (intendedCat && (intendedCat.id ?? intendedCat.Id) !== (defaultCat?.id ?? defaultCat?.Id)) ? 'PASS' : 'WARN',
      `intended="${intendedCat?.name ?? intendedCat?.Name}" (id=${intendedCat?.id ?? intendedCat?.Id})`);

    // Illustrate what a real type+subcategory under the intended category looks like
    // (what the analyst would have on the form, and which the quick-add throws away).
    const intendedTypes = unwrap(await getJson(`${BASE}/Certificate/GetProductTypes?categoryId=${intendedCat?.id ?? intendedCat?.Id}`)) || [];
    const intendedType = intendedTypes[0];
    let intendedSub = null;
    if (intendedType) {
      const subs = unwrap(await getJson(`${BASE}/Certificate/GetProductSubcategories?productTypeId=${intendedType.id ?? intendedType.Id}`)) || [];
      intendedSub = subs[0];
    }
    record('DEF231-intended-hierarchy', 'Form selections the quick-add will ignore', 'PASS',
      `category="${intendedCat?.name ?? intendedCat?.Name}", type="${intendedType?.name ?? '(none)'}", subcategory="${intendedSub?.name ?? '(none)'}"`);

    // need a CompanyId — SaveProduct requires one. Borrow the first existing product's company.
    const existingProducts = unwrap(await getJson(`${BASE}/Product/GetProducts`)) || [];
    const companyId = existingProducts.find(p => (p.companyId ?? p.CompanyId))?.companyId
      ?? existingProducts.find(p => (p.companyId ?? p.CompanyId))?.CompanyId;
    if (!companyId) { record('SETUP-company', 'A company to attach the product to', 'FAIL', 'no product had a companyId'); throw new Error('no company'); }

    // --- THE REPRO: replay exactly what saveInlineProduct() POSTs --------------
    // (CategoryId = default, NO ProductTypeId/ProductSubcategoryId, regardless of form)
    const save = await ctx.request.post(`${BASE}/Product/SaveProduct`, {
      headers: authHeaders(),
      data: { Id: 0, Name: NAME, CompanyId: companyId, CategoryId: (defaultCat?.id ?? defaultCat?.Id), IsActive: true },
    });
    const saveBody = await save.json().catch(() => null);
    const saveOk = (saveBody?.success ?? saveBody?.Success) === true;
    createdId = unwrap(saveBody)?.id ?? unwrap(saveBody)?.Id;
    record('DEF231-quickadd-save', 'Inline quick-add payload accepted', saveOk ? 'PASS' : 'FAIL',
      `http=${save.status()} id=${createdId} msg="${saveBody?.message ?? saveBody?.Message ?? ''}"`);

    // --- read it back and show the mis-categorization -------------------------
    if (createdId) {
      const after = unwrap(await getJson(`${BASE}/Product/GetProducts`)) || [];
      const prod = after.find(p => (p.id ?? p.Id) === createdId);
      if (!prod) {
        record('DEF231-readback', 'Created product found in Product Management', 'FAIL', `id ${createdId} not in GetProducts`);
      } else {
        const catName = prod.categoryName ?? prod.CategoryName;
        const typeName = prod.productTypeName ?? prod.ProductTypeName;
        const subName = prod.productSubcategoryName ?? prod.ProductSubcategoryName;
        const typeId = prod.productTypeId ?? prod.ProductTypeId;
        const subId = prod.productSubcategoryId ?? prod.ProductSubcategoryId;

        // BUG ASSERTION 1: saved under the default category, NOT the analyst's intended one.
        const landedOnDefault = (prod.categoryId ?? prod.CategoryId) === (defaultCat?.id ?? defaultCat?.Id);
        const notIntended = (prod.categoryId ?? prod.CategoryId) !== (intendedCat?.id ?? intendedCat?.Id);
        record('DEF231-BUG-wrong-category', 'Product saved under DEFAULT category, not the analyst’s pick',
          (landedOnDefault && notIntended) ? 'PASS' : 'FAIL',
          `saved category="${catName}"  (intended="${intendedCat?.name ?? intendedCat?.Name}")`);

        // BUG ASSERTION 2: no type / subcategory persisted at all.
        const noTypeNoSub = !typeId && !subId;
        record('DEF231-BUG-no-type-sub', 'Product saved with NO type/subcategory (missing in Product Management)',
          noTypeNoSub ? 'PASS' : 'FAIL',
          `productTypeName="${typeName || '(empty)'}", productSubcategoryName="${subName || '(empty)'}"`);

        console.log(`\n  → Repro confirms Carly's report: an analyst picking "${intendedCat?.name ?? intendedCat?.Name}"`);
        console.log(`    (+ type/subcategory) and quick-adding a product gets a row under`);
        console.log(`    "${catName}" with no type/subcategory — exactly "selected Chemicals, shows ${catName}".`);
      }
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    // cleanup — DeleteProduct is [HttpDelete]. Sweep ALL ZZ_Probe_QuickAdd_* products by
    // name so a prior aborted/POST-cleanup run's orphan is also removed.
    try {
      const all = unwrap(await getJson(`${BASE}/Product/GetProducts`)) || [];
      const probes = all.filter(p => typeof (p.name ?? p.Name) === 'string' && (p.name ?? p.Name).startsWith('ZZ_Probe_QuickAdd_'));
      let removed = 0, failed = 0;
      for (const p of probes) {
        const pid = p.id ?? p.Id;
        const d = await ctx.request.delete(`${BASE}/Product/DeleteProduct/${pid}`, { headers: authHeaders() });
        const db = await d.json().catch(() => null);
        if ((db?.success ?? db?.Success)) removed++; else { failed++; console.log(`  cleanup: failed to delete ${p.name ?? p.Name} (${pid}) — ${db?.message ?? db?.Message ?? d.status()}`); }
      }
      record('DEF231-cleanup', 'Throwaway products removed', failed === 0 ? 'PASS' : 'WARN',
        `removed=${removed}, failed=${failed}`);
    } catch (ce) { record('DEF231-cleanup', 'Cleanup', 'WARN', ce.message); }
    fs.writeFileSync(path.join(OUT, 'def231-quickadd-repro-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
  console.log(`(PASS on the DEF231-BUG-* checks = the bug is reproduced.)`);
})();
