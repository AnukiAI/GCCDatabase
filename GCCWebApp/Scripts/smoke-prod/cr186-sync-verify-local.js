// CR-186 — LOCAL verification of the product Type/Subcategory sync-from-certificate.
// Proves both client decisions on http://localhost:5101:
//   (1) editing a cert's Product Type populates the linked product's Type/Subcategory
//   (2) clearing the cert's Product Type blanks the product (no stale value retained)
//
// Run (app must be running on :5101):  node Scripts/smoke-prod/cr186-sync-verify-local.js
const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const PASS = 'TestPass123!';

const prod = async (ctx, id) =>
  ((await (await ctx.request.get(`${BASE}/Product/GetProducts`)).json()).data || [])
    .find(p => p.id === id);

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();

  // login (no http gate locally)
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill(PASS);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1000);
  if (/\/Account\/Login/i.test(page.url())) { console.log('login failed'); process.exit(1); }

  // pull some cert numbers from the internal API (Edit route keys on the cert NUMBER)
  const certResp = await ctx.request.get(`${BASE}/api/certificate-tiles-internal?page=1&pageSize=40`);
  const certJson = await certResp.json();
  const certs = certJson.certificates || certJson.data || certJson.items || [];
  const certNums = certs.map(c => c.certificateNumber || c.CertificateNumber || c.number).filter(Boolean);
  console.log(`Found ${certNums.length} cert numbers to try.`);
  const editUrl = n => `${BASE}/Certificate/Edit/${encodeURIComponent(n)}`;

  // find a cert whose Edit page offers >= 1 non-empty Product Type option
  let chosen = null;
  for (const num of certNums) {
    await page.goto(editUrl(num), { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200); // let savedCategory cascade populate #ProductTypeId
    const info = await page.evaluate(() => {
      const t = document.getElementById('ProductTypeId');
      const p = document.getElementById('ProductId');
      if (!t || !p) return null;
      const opts = [...t.options].filter(o => o.value).map(o => ({ v: o.value, txt: o.text }));
      return { productId: parseInt(p.value, 10), curType: t.value, opts };
    });
    if (info && info.productId && info.opts.length) { chosen = { num, ...info }; break; }
  }
  if (!chosen) { console.log('No editable cert with a Product Type option found locally.'); process.exit(1); }

  const target = chosen.opts.find(o => o.v !== chosen.curType) || chosen.opts[0];
  console.log(`Cert ${chosen.num} -> product #${chosen.productId}. Setting Product Type = "${target.txt}" (${target.v}).`);

  const before = await prod(ctx, chosen.productId);
  console.log(`  product BEFORE: type="${before?.productTypeName || ''}" subcat="${before?.productSubcategoryName || ''}"`);

  // ---- PASS 1: set a Product Type, save, verify product mirrors it ----
  await page.goto(editUrl(chosen.num), { waitUntil: 'networkidle' });
  await page.waitForTimeout(800);
  await page.selectOption('#ProductTypeId', target.v);
  await page.waitForTimeout(600); // subcategory cascade (optional, leave unset)
  await Promise.all([
    page.waitForURL(u => !/\/Certificate\/Edit/i.test(u.toString()), { timeout: 15000 }).catch(() => {}),
    page.locator('#certificateEditForm button[type="submit"]').last().click(),
  ]);
  await page.waitForTimeout(1200);
  const after1 = await prod(ctx, chosen.productId);
  const pass1 = (after1?.productTypeName || '') === target.txt;
  console.log(`  product AFTER set: type="${after1?.productTypeName || ''}" subcat="${after1?.productSubcategoryName || ''}"  -> ${pass1 ? 'PASS' : 'FAIL'}`);

  // ---- PASS 2: clear the Product Type, save, verify product blanks ----
  await page.goto(editUrl(chosen.num), { waitUntil: 'networkidle' });
  await page.waitForTimeout(800);
  await page.selectOption('#ProductTypeId', '');
  await page.selectOption('#ProductSubcategoryId', '').catch(() => {});
  await Promise.all([
    page.waitForURL(u => !/\/Certificate\/Edit/i.test(u.toString()), { timeout: 15000 }).catch(() => {}),
    page.locator('#certificateEditForm button[type="submit"]').last().click(),
  ]);
  await page.waitForTimeout(1200);
  const after2 = await prod(ctx, chosen.productId);
  const pass2 = (after2?.productTypeName || '') === '';
  console.log(`  product AFTER clear: type="${after2?.productTypeName || ''}" -> ${pass2 ? 'PASS (blanked)' : 'FAIL (stale value retained)'}`);

  await browser.close();
  console.log(`\n${pass1 && pass2 ? '✅ BOTH PASS' : '❌ FAIL'} — populate-on-edit=${pass1}, blank-on-empty=${pass2}`);
  process.exit(pass1 && pass2 ? 0 : 1);
})();
