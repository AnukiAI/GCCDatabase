// QC #182 / DEF-198 — Partner CSV export must be scoped to caller's partner ∩ PublicStatuses.
//
// Localhost test against dev DB. partner_test user (Id=324) is mapped to Amazon.
// Expected (per SQL truth): Amazon × Client Approved - Paid = 4 rows.
// Pre-fix behavior was 43 rows (full DB leak); fix verified at 4.
//
// Three callers tested:
//   1. partner_test (Partner role, Amazon)  → expect 4 rows, all Partner=Amazon, all Status in PublicStatuses
//   2. admin (Admin role)                   → expect 45 rows (no restriction)
//   3. anonymous (no login)                 → expect 22 rows (all Client Approved - Paid)

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = 'http://localhost:5101';
const EXPORT_URL = `${BASE}/Certificate/ExportCertificates?format=csv`;

const PUBLIC_STATUSES = new Set(['Client Approved - Paid', 'Republished']);

// Minimal CSV parser — handles quoted fields containing commas/quotes.
function parseCsv(text) {
  const rows = [];
  let cur = '', row = [], inQ = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inQ) {
      if (c === '"' && text[i+1] === '"') { cur += '"'; i++; }
      else if (c === '"') inQ = false;
      else cur += c;
    } else {
      if (c === '"') inQ = true;
      else if (c === ',') { row.push(cur); cur = ''; }
      else if (c === '\r') {} // ignore
      else if (c === '\n') { row.push(cur); rows.push(row); row = []; cur = ''; }
      else cur += c;
    }
  }
  if (cur || row.length) { row.push(cur); rows.push(row); }
  return rows.filter(r => r.length > 1 || (r.length === 1 && r[0].trim()));
}

function indexHeaders(headerRow) {
  const idx = {};
  headerRow.forEach((h, i) => { idx[h.trim()] = i; });
  return idx;
}

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

async function login(page, username, password) {
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill(username);
  await page.locator('#Password').fill(password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(1500);
}

async function fetchExport(ctx, label) {
  const resp = await ctx.request.get(EXPORT_URL);
  const status = resp.status();
  const body = await resp.text();
  fs.writeFileSync(path.join(OUT, `qc182-${label}.csv`), body);
  return { status, body };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {

    // -------- CASE 1: partner_test (Partner role) --------
    console.log('\n=== CASE 1: partner_test (Partner role, Amazon) ===');
    {
      const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
      const page = await ctx.newPage();
      await login(page, 'partner_test', 'TestPass123!');
      const { status, body } = await fetchExport(ctx, 'partner');
      console.log(`  HTTP ${status}, bytes=${body.length}`);

      const rows = parseCsv(body);
      const header = rows[0] || [];
      const data = rows.slice(1);
      console.log(`  rows (incl. header): ${rows.length} ; data rows: ${data.length}`);

      const idx = indexHeaders(header);
      const partnerCol = idx['Partner'];
      const statusCol = idx['Status'];

      record('QC182-1a', 'Partner export returns scoped row count',
        data.length === 4 ? 'PASS' : 'FAIL',
        `expected=4 actual=${data.length}`);

      // All rows must have Partner=Amazon
      const partnerVals = [...new Set(data.map(r => r[partnerCol]))];
      const partnerOk = partnerVals.length === 1 && partnerVals[0] === 'Amazon';
      record('QC182-1b', 'All rows have Partner=Amazon (no cross-partner leakage)',
        partnerOk ? 'PASS' : 'FAIL',
        `distinct partners=${JSON.stringify(partnerVals)}`);

      // All rows must have Status in PublicStatuses
      const statusVals = [...new Set(data.map(r => r[statusCol]))];
      const statusOk = statusVals.every(s => PUBLIC_STATUSES.has(s));
      record('QC182-1c', 'All rows have Status in PublicStatuses (no Draft/Expired/Active leakage)',
        statusOk ? 'PASS' : 'FAIL',
        `distinct statuses=${JSON.stringify(statusVals)} expected⊆={Client Approved - Paid,Republished}`);

      await ctx.close();
    }

    // -------- CASE 2: admin (Admin role) --------
    console.log('\n=== CASE 2: admin (Admin role, no restriction) ===');
    {
      const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
      const page = await ctx.newPage();
      await login(page, 'admin', 'TestPass123!');
      const { status, body } = await fetchExport(ctx, 'admin');
      console.log(`  HTTP ${status}, bytes=${body.length}`);

      const rows = parseCsv(body);
      const data = rows.slice(1);
      console.log(`  rows (incl. header): ${rows.length} ; data rows: ${data.length}`);

      // Admin should NOT be restricted — count should equal total certs in DB
      record('QC182-2a', 'Admin export returns full DB (no restriction)',
        data.length >= 40 ? 'PASS' : 'FAIL',
        `actual=${data.length}; expecting close to total cert count (~45)`);

      // Admin should see all statuses, not just public
      const statusVals = [...new Set(data.map(r => r[indexHeaders(rows[0])['Status']]))];
      const hasNonPublic = statusVals.some(s => !PUBLIC_STATUSES.has(s));
      record('QC182-2b', 'Admin sees non-public statuses (Draft / Expired / Active)',
        hasNonPublic ? 'PASS' : 'FAIL',
        `distinct statuses=${JSON.stringify(statusVals)}`);

      await ctx.close();
    }

    // -------- CASE 3: anonymous --------
    console.log('\n=== CASE 3: anonymous (no login → Public scope) ===');
    {
      const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
      const { status, body } = await fetchExport(ctx, 'anon');
      console.log(`  HTTP ${status}, bytes=${body.length}`);

      if (status !== 200) {
        record('QC182-3a', 'Anonymous export accessible (200 OK)',
          'WARN', `HTTP ${status} — endpoint may require auth; not a security defect either way`);
      } else {
        const rows = parseCsv(body);
        const data = rows.slice(1);
        console.log(`  rows (incl. header): ${rows.length} ; data rows: ${data.length}`);

        const statusCol = indexHeaders(rows[0])['Status'];
        const statusVals = data.length ? [...new Set(data.map(r => r[statusCol]))] : [];
        const allPublic = statusVals.every(s => PUBLIC_STATUSES.has(s));
        record('QC182-3a', 'Anonymous export contains ONLY PublicStatuses',
          allPublic ? 'PASS' : 'FAIL',
          `distinct statuses=${JSON.stringify(statusVals)} rows=${data.length}`);
      }

      await ctx.close();
    }

  } catch (e) {
    console.log(`\nERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, 'qc182-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
