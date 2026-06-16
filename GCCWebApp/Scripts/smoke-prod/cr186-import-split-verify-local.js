// CR-186 SPLIT — LOCAL verify that the CERTIFICATE CSV IMPORT files same-name/different-type
// rows onto SEPARATE product records (tuple find-or-create at commit), on :5101.
//   - Build a CSV: 2 cert rows, SAME product name, DIFFERENT product type
//   - Preview -> Apply through /api/CertificateImport/{Preview,Apply}
//   - Confirm /Product/GetProducts now has 2 records for that name, one per type, each with a cert
//
// Run (app on :5101):  node Scripts/smoke-prod/cr186-import-split-verify-local.js
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const PASS = 'TestPass123!';
const csvField = v => /[",\n]/.test(String(v)) ? `"${String(v).replace(/"/g, '""')}"` : String(v);

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

  // discover reference values
  const products0 = ((await (await ctx.request.get(`${BASE}/Product/GetProducts`)).json()).data) || [];
  const companies = ((await (await ctx.request.get(`${BASE}/Product/GetCompanies`)).json()).data) || [];
  const companyName = companies[0]?.name;
  const types = ((await (await ctx.request.get(`${BASE}/Certificate/GetProductTypes?categoryId=1`)).json()).data) || [];
  const certs = (await (await ctx.request.get(`${BASE}/api/certificate-tiles-internal?page=1&pageSize=5`)).json());
  const certType = (certs.certificates || certs.data || [])
    .map(c => c.certificationType || c.CertificationType).find(Boolean);
  if (!companyName || types.length < 2 || !certType) {
    console.log(`missing refs: company=${companyName}, types=${types.length}, certType=${certType}`); process.exit(1);
  }
  const t1 = types[0].name, t2 = types[1].name;
  const stamp = String(products0.length) + '' + String((products0[0]?.id) || 0);
  const pname = `ZZ Split Import ${stamp}`;
  console.log(`Company="${companyName}"  CertType="${certType}"  Types="${t1}" / "${t2}"  Product="${pname}"`);

  const header = ['CertificateNumber','CompanyName','ProductName','ProductType','CertificationType','EffectiveDate','ExpirationDate','Status','ASIN'];
  const r1 = [`ZZIMP-${stamp}-1`, companyName, pname, t1, certType, '2026-01-01', '2027-01-01', 'Draft', `ZZASIN${stamp}A`];
  const r2 = [`ZZIMP-${stamp}-2`, companyName, pname, t2, certType, '2026-01-01', '2027-01-01', 'Draft', `ZZASIN${stamp}B`];
  const csv = [header, r1, r2].map(r => r.map(csvField).join(',')).join('\n');
  console.log('--- CSV ---\n' + csv + '\n-----------');

  // Preview
  const prevResp = await ctx.request.post(`${BASE}/api/CertificateImport/Preview`, {
    multipart: { csvFile: { name: 'cr186-split.csv', mimeType: 'text/csv', buffer: Buffer.from(csv) } }
  });
  const prev = await prevResp.json();
  console.log(`Preview HTTP ${prevResp.status()} — importId=${prev.importId}, newCerts=${prev.newCertificates}, productsToCreate=${prev.productsToCreate}, canApply=${prev.canApply}`);
  if (prev.errorRows?.length) console.log('  errorRows:', JSON.stringify(prev.errorRows));
  if (!prev.importId || !prev.canApply) { console.log('❌ preview not applyable'); await browser.close(); process.exit(1); }

  // Apply
  const applyResp = await ctx.request.post(`${BASE}/api/CertificateImport/Apply`, {
    data: { importId: prev.importId, reason: 'cr186 import split test' }
  });
  const apply = await applyResp.json();
  console.log(`Apply HTTP ${applyResp.status()} — created=${apply.certificatesCreated}, failed=${apply.certificatesFailed}, msg=${apply.message}`);

  // Verify
  const after = ((await (await ctx.request.get(`${BASE}/Product/GetProducts`)).json()).data) || [];
  const rows = after.filter(p => p.name === pname);
  console.log(`Product records named "${pname}": ${rows.length}`);
  rows.forEach(p => console.log(`  #${p.id} type="${p.productTypeName||''}" certs=${p.certificateCount}`));

  const hasT1 = rows.find(p => (p.productTypeName||'') === t1 && p.certificateCount >= 1);
  const hasT2 = rows.find(p => (p.productTypeName||'') === t2 && p.certificateCount >= 1);
  const pass = rows.length === 2 && hasT1 && hasT2 && hasT1.id !== hasT2.id;

  await browser.close();
  console.log(`\n${pass ? '✅ PASS — import split same-name rows into 2 records by type' : '❌ FAIL'}`);
  process.exit(pass ? 0 : 1);
})();
