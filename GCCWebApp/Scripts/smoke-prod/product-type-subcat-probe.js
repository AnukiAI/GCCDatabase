// READ-ONLY probe: why are Product Type & Subcategory blank on /Product/Management?
// Hits /Product/GetProducts as admin on live and reports, per product, the FK ids vs
// the resolved names. This separates "FK ids are NULL (data)" from "ids set but names
// blank (join/config bug)" from "names present but table not rendering (JS bug)".
//
// Run: node Scripts/smoke-prod/product-type-subcat-probe.js

const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const PASS = 'TestPass123!';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE });
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill(PASS);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1500);
  if (/\/Account\/Login/i.test(page.url())) { console.log('admin login failed'); process.exit(1); }

  const resp = await ctx.request.get(`${BASE}/Product/GetProducts`);
  const json = await resp.json();
  await browser.close();

  const rows = (json && json.data) || [];
  console.log(`HTTP ${resp.status()} · products: ${rows.length}\n`);

  let typeIdSet = 0, typeNameSet = 0, subIdSet = 0, subNameSet = 0;
  for (const p of rows) {
    if (p.productTypeId != null) typeIdSet++;
    if (p.productTypeName && p.productTypeName.trim()) typeNameSet++;
    if (p.productSubcategoryId != null) subIdSet++;
    if (p.productSubcategoryName && p.productSubcategoryName.trim()) subNameSet++;
  }

  console.log('Aggregate over', rows.length, 'products:');
  console.log(`  productTypeId set        : ${typeIdSet}`);
  console.log(`  productTypeName non-blank: ${typeNameSet}`);
  console.log(`  productSubcategoryId set : ${subIdSet}`);
  console.log(`  productSubcategoryName ne: ${subNameSet}\n`);

  console.log('First 15 products (id | name | typeId/typeName | subId/subName):');
  for (const p of rows.slice(0, 15)) {
    console.log(`  #${p.id} ${String(p.name).slice(0,28).padEnd(28)} | T:${String(p.productTypeId ?? '-').padStart(4)}/"${p.productTypeName||''}" | S:${String(p.productSubcategoryId ?? '-').padStart(4)}/"${p.productSubcategoryName||''}"`);
  }
})();
