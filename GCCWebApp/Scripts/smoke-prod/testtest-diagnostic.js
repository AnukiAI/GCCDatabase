// Inspect what got saved when Shawn created "testtest" on gccwebapp.com,
// and whether the cert form's cascade endpoint actually returns it.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `testtest-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, t) { fs.writeFileSync(path.join(OUT, `${name}.txt`), t || '(empty)'); }
async function shot(page, name) { await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: true }); console.log('  shot:', name); }

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    httpCredentials: { username: 'gcc', password: 'Cert-Test-Access-2026!' }
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  pageerror:', e.message));

  try {
    await page.goto(`${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', 'admin');
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([
      page.waitForLoadState('networkidle'),
      page.click('button[type="submit"]')
    ]);

    // 1. Admin → Product Types — find every row whose name contains "testtest" (case-insensitive)
    console.log('\n[1] Search Admin → Product Types for "testtest"');
    await page.goto(`${BASE}/Admin/ProductTypes`);
    await page.waitForLoadState('networkidle');
    await page.locator('button[data-view="table"]').click();
    await page.waitForTimeout(1200);

    const headers = await page.locator('table thead th').allInnerTexts().catch(() => []);
    saveText('00-headers', JSON.stringify(headers));

    const allRows = await page.$$eval('table tbody tr', (rows) => rows.map(r => {
      const cells = Array.from(r.querySelectorAll('td')).map(td => td.innerText.trim());
      const delBtn = r.querySelector('.delete-btn');
      const editBtn = r.querySelector('.edit-btn, [data-id]');
      const id = delBtn?.getAttribute('data-id') || editBtn?.getAttribute('data-id');
      const isCat = delBtn?.getAttribute('data-is-category') || editBtn?.getAttribute('data-is-category');
      return { id, isCat, cells };
    }));

    const matches = allRows.filter(r =>
      r.cells.some(c => /test\s*test/i.test(c))
    );
    saveText('01-matches', JSON.stringify(matches, null, 2));
    console.log('  matches:', matches.length);
    matches.forEach(m => console.log('   ', m.id, m.isCat === 'true' ? 'CATEGORY' : 'TYPE-or-SUB', JSON.stringify(m.cells)));

    // 2. Hit the underlying admin tree API for the raw data
    console.log('\n[2] Read raw hierarchy via /Admin/GetProductTypes (or similar)');
    const adminApiResp = await page.evaluate(async () => {
      // try a couple of likely endpoints
      const candidates = ['/Admin/GetProductTypes', '/Admin/ProductTypes/Tree', '/Admin/ProductTypesData'];
      const tries = [];
      for (const url of candidates) {
        try {
          const r = await fetch(url, { credentials: 'same-origin' });
          tries.push({ url, status: r.status, ok: r.ok });
          if (r.ok) {
            const j = await r.json().catch(() => null);
            return { url, status: r.status, body: j, tries };
          }
        } catch (e) {
          tries.push({ url, error: e.message });
        }
      }
      return { tries };
    });
    saveText('02-admin-api-response', JSON.stringify(adminApiResp, null, 2).slice(0, 50000));
    if (adminApiResp.body) {
      // Find testtest in the tree
      const found = [];
      const walk = (nodes, path = []) => {
        for (const n of (Array.isArray(nodes) ? nodes : [])) {
          const myPath = [...path, n.name || n.Name];
          if (/test\s*test/i.test(n.name || n.Name || '')) {
            found.push({
              path: myPath.join(' > '),
              id: n.id || n.Id,
              isCategory: n.isCategory || n.IsCategory,
              isActive: n.isActive ?? n.IsActive,
              categoryId: n.categoryId ?? n.CategoryId,
              parentId: n.parentId ?? n.ParentId,
              level: n.level ?? n.Level,
              certificateCount: n.certificateCount ?? n.CertificateCount,
              productCount: n.productCount ?? n.ProductCount,
              raw: n
            });
          }
          walk(n.children || n.Children || [], myPath);
        }
      };
      walk(adminApiResp.body.data || adminApiResp.body);
      saveText('03-testtest-in-tree', JSON.stringify(found, null, 2));
      console.log('  testtest nodes in tree:', found.length);
      found.forEach(f => console.log('   ', f.path, '| isCategory:', f.isCategory, '| isActive:', f.isActive, '| catId:', f.categoryId, '| parentId:', f.parentId, '| level:', f.level));
    }

    // 3. Test the cert-form cascade endpoint for every category — does it surface testtest?
    console.log('\n[3] Probe /Certificate/GetProductTypes for each category');
    const cascadeResults = await page.evaluate(async () => {
      const cats = await (await fetch('/Certificate/GetProductCategories', { credentials: 'same-origin' })).json().catch(() => null);
      const results = { categoriesEndpoint: cats };
      if (cats && cats.data) {
        results.byCategory = [];
        for (const c of cats.data) {
          const types = await (await fetch(`/Certificate/GetProductTypes?categoryId=${c.id}`, { credentials: 'same-origin' })).json().catch(() => null);
          results.byCategory.push({ catId: c.id, catName: c.name, types: types?.data });
        }
      }
      return results;
    });
    saveText('04-cascade-by-category', JSON.stringify(cascadeResults, null, 2).slice(0, 50000));

    // Search for testtest in the cascade output
    if (cascadeResults.byCategory) {
      const found = [];
      for (const c of cascadeResults.byCategory) {
        for (const t of (c.types || [])) {
          if (/test\s*test/i.test(t.name || '')) {
            found.push({ inCategory: `${c.catId} (${c.catName})`, type: t });
          }
        }
        if (/test\s*test/i.test(c.catName || '')) {
          found.push({ asCategory: c });
        }
      }
      saveText('05-testtest-in-cascade', JSON.stringify(found, null, 2));
      console.log('  testtest surfacing in cascade:', found.length);
      found.forEach(f => console.log('   ', JSON.stringify(f).slice(0, 200)));
    }

    // 4. Subcategory cascade — for any type that is testtest, or for a type whose subcategories include testtest
    if (adminApiResp.body) {
      console.log('\n[4] Probe /Certificate/GetProductSubcategories for parent types of testtest');
      // Find any node whose parent might be relevant
      const subProbe = await page.evaluate(async () => {
        // pull every level-1 type id from the cascade
        const cats = await (await fetch('/Certificate/GetProductCategories', { credentials: 'same-origin' })).json().catch(() => null);
        const out = [];
        for (const c of (cats?.data || [])) {
          const types = await (await fetch(`/Certificate/GetProductTypes?categoryId=${c.id}`, { credentials: 'same-origin' })).json().catch(() => null);
          for (const t of (types?.data || [])) {
            const subs = await (await fetch(`/Certificate/GetProductSubcategories?productTypeId=${t.id}`, { credentials: 'same-origin' })).json().catch(() => null);
            const matches = (subs?.data || []).filter(s => /test\s*test/i.test(s.name || ''));
            if (matches.length) {
              out.push({ underCategory: c.name, underType: t.name, subcategoryMatches: matches });
            }
          }
        }
        return out;
      });
      saveText('06-testtest-in-subcategory-cascade', JSON.stringify(subProbe, null, 2));
      console.log('  testtest surfacing as subcategory:', subProbe.length);
      subProbe.forEach(s => console.log('   ', JSON.stringify(s).slice(0, 200)));
    }

    // 5. Open /Certificate/Create and screenshot the actual dropdowns
    console.log('\n[5] Open /Certificate/Create — dropdown screenshots');
    await page.goto(`${BASE}/Certificate/Create`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2500);
    await shot(page, '07-cert-create-page');

    // Read all category options on the form
    const catOpts = await page.$$eval('#ProductCategoryId option', els =>
      els.map(o => ({ value: o.value, text: o.textContent.trim() }))).catch(() => []);
    saveText('08-create-category-options', JSON.stringify(catOpts, null, 2));
    console.log('  Category options on Create page:', catOpts.length);
    const testInCat = catOpts.find(o => /test\s*test/i.test(o.text));
    console.log('  testtest in Category dropdown?', testInCat ? `YES (${JSON.stringify(testInCat)})` : 'NO');

    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure').catch(() => {});
  } finally {
    await browser.close();
  }
})();
