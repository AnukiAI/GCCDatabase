// CR-184 — independent count verification (READ-ONLY).
// Downloads the Active Product Identifiers CSV from prod as admin and checks that the
// app's self-reported "Total Active Product Identifiers: N" header equals the actual
// number of DATA RECORDS written (RFC-4180 aware: a quoted Description may contain
// commas/newlines, so we must parse, not line-count).
//
// Run: node Scripts/smoke-prod/cr184-count-verify.js

const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const PASS = 'TestPass123!';

// Minimal RFC-4180 parser -> array of records (each an array of fields).
function parseCsv(text) {
  const records = [];
  let field = '', record = [], inQuotes = false;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (inQuotes) {
      if (ch === '"') {
        if (text[i + 1] === '"') { field += '"'; i++; }
        else inQuotes = false;
      } else field += ch;
    } else {
      if (ch === '"') inQuotes = true;
      else if (ch === ',') { record.push(field); field = ''; }
      else if (ch === '\r') { /* skip */ }
      else if (ch === '\n') { record.push(field); records.push(record); record = []; field = ''; }
      else field += ch;
    }
  }
  if (field.length > 0 || record.length > 0) { record.push(field); records.push(record); }
  return records;
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE });
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill(PASS);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1500);
  if (/\/Account\/Login/i.test(page.url())) { console.log('❌ admin login failed'); process.exit(1); }

  const resp = await ctx.request.get(`${BASE}/Export/DownloadActiveProductIds`);
  const csv = await resp.text();
  await browser.close();

  // 1) Header self-reported total
  const headerLine = (csv.split('\n').find(l => l.startsWith('Total Active Product Identifiers:')) || '').trim();
  const headerCount = parseInt((headerLine.match(/(\d+)/) || [])[1] || '-1', 10);

  // 2) Naive physical line count (what a human eyeballing the file might do)
  const physicalLines = csv.split('\n').filter(l => l.trim().length > 0).length;

  // 3) RFC-4180 record count. The file = 3 preamble lines + blank + 1 column header + data records.
  const records = parseCsv(csv);
  const COL_HEADER = 'Certificate Number';
  const headerIdx = records.findIndex(r => r[0] === COL_HEADER);
  const dataRecords = headerIdx >= 0 ? records.slice(headerIdx + 1).filter(r => r.some(f => f.trim() !== '')) : [];
  const dataCount = dataRecords.length;

  // 4) Field-width integrity: every data record should have exactly 12 columns.
  const colCount = headerIdx >= 0 ? records[headerIdx].length : 0;
  const badWidth = dataRecords.filter(r => r.length !== colCount);

  // 5) Multi-line cells: detect records whose raw form spanned >1 physical line (embedded newline in a quoted field)
  const embeddedNewlines = csv.split('\n').filter(l => /"[^"]*$/.test(l) && l.trim().length).length;

  console.log(`\nCR-184 count verification — ${BASE}`);
  console.log(`HTTP ${resp.status()} · content-type ${resp.headers()['content-type']}`);
  console.log(`  Header line              : "${headerLine}"  -> ${headerCount}`);
  console.log(`  Column header fields     : ${colCount} (expect 12)`);
  console.log(`  RFC-4180 DATA records    : ${dataCount}`);
  console.log(`  Naive non-blank lines    : ${physicalLines} (preamble+header inflate this)`);
  console.log(`  Records with != ${colCount} cols : ${badWidth.length}`);
  console.log(`  Lines opening an unclosed quote (possible embedded newline): ${embeddedNewlines}`);

  const match = headerCount === dataCount;
  console.log(`\n${match ? '✅' : '❌'} Header total (${headerCount}) ${match ? '==' : '!='} actual data records (${dataCount})`);
  if (badWidth.length) console.log(`⚠️  ${badWidth.length} record(s) not 12 columns — first: ${JSON.stringify(badWidth[0]).slice(0,200)}`);
  process.exit(match && badWidth.length === 0 ? 0 : 1);
})();
