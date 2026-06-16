// CR-186 SPLIT — LOCAL verify on :5101 that a cert with a given product + Type/Subcategory
// resolves to a SEPARATE product line item (find-or-create), with the cert count on it.
//   - Edit a cert, set a Product Type that differs from the cert's current product combo
//   - Confirm a product record with the SAME name + the new Type now exists (its own line item)
//   - Confirm it is a different record id than the originally-picked product
//
// Run (app running on :5101):  node Scripts/smoke-prod/cr186-split-verify-local.js
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const PASS = 'TestPass123!';

const getProducts = async ctx =>
  ((await (await ctx.request.get(`${BASE}/Product/GetProducts`)).json()).data || []);

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill(PASS);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1000);
  if (/\/Account\/Login/i.test(page.url())) { console.log('login failed'); process.exit(1); }

  const certJson = await (await ctx.request.get(`${BASE}/api/certificate-tiles-internal?page=1&pageSize=40`)).json();
  const certNums = (certJson.certificates || certJson.data || certJson.items || [])
    .map(c => c.certificateNumber || c.CertificateNumber || c.number).filter(Boolean);
  const editUrl = n => `${BASE}/Certificate/Edit/${encodeURIComponent(n)}`;

  // find a cert whose edit page exposes a productId + >= 1 type option
  let chosen = null;
  for (const num of certNums) {
    await page.goto(editUrl(num), { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
    const info = await page.evaluate(() => {
      const t = document.getElementById('ProductTypeId'), p = document.getElementById('ProductId');
      if (!t || !p) return null;
      const opts = [...t.options].filter(o => o.value).map(o => ({ v: o.value, txt: o.text }));
      return { productId: parseInt(p.value, 10), curType: t.value, opts };
    });
    if (info && info.productId && info.opts.length) { chosen = { num, ...info }; break; }
  }
  if (!chosen) { console.log('No editable cert with a Product Type option found.'); process.exit(1); }

  const products = await getProducts(ctx);
  const baseProduct = products.find(p => p.id === chosen.productId);
  const baseName = baseProduct?.name;
  const target = chosen.opts.find(o => o.v !== chosen.curType) || chosen.opts[0];
  console.log(`Cert ${chosen.num}, picked product #${chosen.productId} "${baseName}". Target Type "${target.txt}" (${target.v}).`);

  const beforeRows = products.filter(p => p.name === baseName);
  console.log(`  BEFORE: ${beforeRows.length} line item(s) named "${baseName}": ` +
    beforeRows.map(p => `#${p.id}[type="${p.productTypeName||''}",certs=${p.certificateCount}]`).join(' '));

  // edit cert -> set the target Product Type, save
  await page.goto(editUrl(chosen.num), { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.selectOption('#ProductTypeId', target.v);
  await page.waitForTimeout(600);
  await Promise.all([
    page.waitForURL(u => !/\/Certificate\/Edit/i.test(u.toString()), { timeout: 15000 }).catch(() => {}),
    page.locator('#certificateEditForm button[type="submit"]').last().click(),
  ]);
  await page.waitForTimeout(1500);

  const after = await getProducts(ctx);
  const afterRows = after.filter(p => p.name === baseName);
  console.log(`  AFTER : ${afterRows.length} line item(s) named "${baseName}": ` +
    afterRows.map(p => `#${p.id}[type="${p.productTypeName||''}",certs=${p.certificateCount}]`).join(' '));

  const match = afterRows.find(p => (p.productTypeName || '') === target.txt && p.certificateCount >= 1);
  const isSeparate = match && match.id !== chosen.productId; // new combo -> its own record (unless base already was this combo)
  const pass = !!match && (match.id !== chosen.productId || chosen.curType === target.v);

  await browser.close();
  console.log(`\n  line item for "${baseName}" + "${target.txt}" exists with a cert: ${!!match}`);
  console.log(`  it is a separate record from the picked product #${chosen.productId}: ${isSeparate}`);
  console.log(`\n${pass ? '✅ PASS — cert resolved to the matching (Product+Type) line item' : '❌ FAIL'}`);
  process.exit(pass ? 0 : 1);
})();
