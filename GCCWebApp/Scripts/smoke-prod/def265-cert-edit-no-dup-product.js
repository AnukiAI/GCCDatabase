// DEF-265 verify (localhost): editing a sole-owner certificate's Product Type must UPDATE its
// product line item in place, NOT create a new product (the "keeps adding new instead of
// updating" report). Picks a product with exactly 1 cert + a category that has >=2 types,
// edits that cert's type, and asserts the total product count does NOT increase.
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';

async function getProducts(page) {
  return page.evaluate(async () => {
    const r = await fetch('/Product/GetProducts', { headers: { Accept: 'application/json' } });
    const j = await r.json();
    const arr = j.data || j.products || j || [];
    return (Array.isArray(arr) ? arr : []).map(p => ({
      id: p.id ?? p.Id, name: p.name ?? p.Name,
      type: p.productTypeName ?? p.ProductTypeName ?? p.productType ?? '',
      certCount: p.certificateCount ?? p.CertificateCount ?? 0,
    }));
  });
}

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(800);

  const before = await getProducts(page);
  const soleOwners = before.filter(p => p.certCount === 1);
  console.log(`products total=${before.length}; sole-owner(certCount=1)=${soleOwners.length}`);

  // Map product name -> a cert id via the internal tile API.
  const certs = await page.evaluate(async (base) => {
    const r = await fetch(base + '/api/certificate-tiles-internal?sortBy=newest&page=1&pageSize=500', { headers: { Accept: 'application/json' } });
    const j = await r.json();
    return (j.certificates || []).map(c => ({ id: c.id, num: c.certificateNumber, productName: c.productName, productType: c.productType || c.productTypeName || '' }));
  }, BASE);

  // Try sole-owner products until we find one whose cert edit page offers a different type.
  let done = false;
  for (const p of soleOwners) {
    const cert = certs.find(c => c.productName === p.name);
    if (!cert) continue;
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(cert.num)}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1200);
    const typeOpts = await page.evaluate(() => {
      const el = document.getElementById('ProductTypeId');
      if (!el) return [];
      return Array.from(el.options).map(o => ({ v: o.value, t: o.textContent.trim(), sel: o.selected })).filter(o => o.v && o.v !== '0');
    });
    const current = typeOpts.find(o => o.sel) || { v: '' };
    const target = typeOpts.find(o => o.v && o.v !== current.v);
    if (!target) { continue; } // category has <2 types; try next

    console.log(`\nCERT ${cert.num} (id ${cert.id}) | product "${p.name}" #${p.id} certCount=1`);
    console.log(`  change ProductType: "${current.t || '(none)'}" -> "${target.t}"`);

    // Set the new type (fire change so dependent subcategory list refreshes), clear subcategory.
    await page.evaluate((v) => { const el = document.getElementById('ProductTypeId'); el.value = v; el.dispatchEvent(new Event('change', { bubbles: true })); }, target.v);
    await page.waitForTimeout(700);
    await page.evaluate(() => { const s = document.getElementById('ProductSubcategoryId'); if (s) { s.value = ''; s.dispatchEvent(new Event('change', { bubbles: true })); } });

    page.once('dialog', d => d.accept());
    await page.locator('#saveButton').click();
    // wait for navigation away from the edit page (successful save redirects)
    await page.waitForTimeout(2500);
    console.log('  after save, url:', page.url());

    const after = await getProducts(page);
    const sameProduct = after.find(x => x.id === p.id);
    console.log(`  product total: ${before.length} -> ${after.length}`);
    console.log(`  product #${p.id} type now: "${sameProduct ? sameProduct.type : '(gone)'}"`);

    console.log('\n=== VERDICT ===');
    if (after.length <= before.length && sameProduct && sameProduct.type === target.t) {
      console.log(`  PASS: no new product created (${before.length}->${after.length}); line item #${p.id} UPDATED in place to "${target.t}".`);
    } else if (after.length > before.length) {
      console.log(`  FAIL: a NEW product was created (${before.length}->${after.length}) — duplicate, fix not effective.`);
    } else {
      console.log(`  CHECK: count ok but product #${p.id} type is "${sameProduct ? sameProduct.type : 'gone'}" (expected "${target.t}"). May have repointed to an existing tuple — inspect.`);
    }
    done = true;
    break;
  }
  if (!done) console.log('Could not find a sole-owner cert with a multi-type category to test.');
  await browser.close();
})();
