// QC #184 / DEF-202 — verify Product Management export on gccwebapp.com
// includes SKU and Partners columns (Product Name pre-existed).
//
// Strategy:
//   1. Basic-auth past the test-env gate, then login as analyst.
//   2. Hit GetProducts JSON directly — confirms backend projection has sku + partners.
//   3. Drive the actual Export button — confirms the CSV blob the user gets has the right headers.
//
// Saves: api-sample.json, products_export.csv, summary.json
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const BASE = 'https://gccwebapp.com';
const OUT = __dirname;

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    httpCredentials: BASIC,
    acceptDownloads: true,
  });
  const page = await ctx.newPage();

  // 1. Login
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('analyst');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type=submit]').first().click(),
  ]);
  await page.waitForTimeout(2500);

  // 2. Navigate to Product Management
  await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3500);

  // 3. Pull the GetProducts API response so we can prove sku + partners are in the payload
  const apiSample = await page.evaluate(async () => {
    const r = await fetch('/Product/GetProducts');
    if (!r.ok) return { error: `status ${r.status}` };
    const j = await r.json();
    const data = j.data || j.Data || [];
    const withSku = data.filter(p => p.sku && String(p.sku).trim());
    const withPartners = data.filter(p => Array.isArray(p.partners) && p.partners.length);
    return {
      totalProducts: data.length,
      withSkuCount: withSku.length,
      withPartnersCount: withPartners.length,
      firstKeys: data[0] ? Object.keys(data[0]).sort() : [],
      hasSkuField: data[0] ? 'sku' in data[0] : false,
      hasPartnersField: data[0] ? 'partners' in data[0] : false,
      sampleWithBoth: data.find(p => p.sku && Array.isArray(p.partners) && p.partners.length) || null,
      sampleFirstThree: data.slice(0, 3).map(p => ({
        name: p.name,
        sku: p.sku,
        partners: p.partners,
        companyName: p.companyName,
      })),
    };
  });
  fs.writeFileSync(path.join(OUT, 'api-sample.json'), JSON.stringify(apiSample, null, 2));

  // 4. Actually click the Export button and capture the downloaded CSV
  let csvPath = null;
  let csvHeaders = null;
  let csvRowsTotal = null;
  let csvFirstDataRow = null;
  let csvSkuColIdx = null;
  let csvPartnersColIdx = null;
  let csvSampleSkuValues = [];
  let csvSamplePartnerValues = [];

  try {
    const [download] = await Promise.all([
      page.waitForEvent('download', { timeout: 15000 }),
      page.locator('#export-btn').click(),
    ]);
    csvPath = path.join(OUT, 'products_export.csv');
    await download.saveAs(csvPath);
    const csv = fs.readFileSync(csvPath, 'utf8');
    const lines = csv.split(/\r?\n/).filter(l => l.length);
    csvHeaders = lines[0];
    csvRowsTotal = lines.length - 1;
    csvFirstDataRow = lines[1] || null;

    // Locate SKU and Partners column positions (header row is quoted CSV)
    const headerCells = lines[0].split(',').map(c => c.replace(/^"|"$/g, ''));
    csvSkuColIdx = headerCells.indexOf('SKU');
    csvPartnersColIdx = headerCells.indexOf('Partners');

    // Grab a few sample SKU + Partners cells from data rows (handle quoted commas crudely — fine for sample)
    const sampleLines = lines.slice(1, 50);
    sampleLines.forEach(l => {
      const cells = l.match(/("([^"]|"")*"|[^,]*)/g)?.filter(c => c !== '') || [];
      const clean = cells.map(c => c.replace(/^"|"$/g, '').replace(/""/g, '"'));
      const sku = clean[csvSkuColIdx];
      const partners = clean[csvPartnersColIdx];
      if (sku && csvSampleSkuValues.length < 5) csvSampleSkuValues.push(sku);
      if (partners && csvSamplePartnerValues.length < 5) csvSamplePartnerValues.push(partners);
    });
  } catch (e) {
    csvHeaders = `ERROR: ${e.message}`;
  }

  const summary = {
    base: BASE,
    timestamp: new Date().toISOString(),
    api: apiSample,
    csv: {
      savedTo: csvPath,
      headers: csvHeaders,
      dataRows: csvRowsTotal,
      firstDataRow: csvFirstDataRow,
      skuColumnIndex: csvSkuColIdx,
      partnersColumnIndex: csvPartnersColIdx,
      sampleSkuValues: csvSampleSkuValues,
      samplePartnerValues: csvSamplePartnerValues,
    },
    verdict: {
      apiHasSku: apiSample.hasSkuField,
      apiHasPartners: apiSample.hasPartnersField,
      csvHasSkuHeader: csvSkuColIdx != null && csvSkuColIdx >= 0,
      csvHasPartnersHeader: csvPartnersColIdx != null && csvPartnersColIdx >= 0,
      anySkuPopulated: csvSampleSkuValues.length > 0,
      anyPartnersPopulated: csvSamplePartnerValues.length > 0,
    },
  };
  fs.writeFileSync(path.join(OUT, 'summary.json'), JSON.stringify(summary, null, 2));
  console.log(JSON.stringify(summary, null, 2));

  await browser.close();
})();
