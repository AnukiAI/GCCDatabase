// QC #182 / DEF-198 verification on gccwebapp.com — read-only.
//
// Strategy: anonymous and partner_test branches; never modify data. The Partner-scoped
// test only runs if partner_test exists on the prod DB. Anonymous always runs — it's
// enough by itself to detect a regression because the fix added PublicStatuses filtering
// to ANY anonymous caller (pre-fix: full DB; post-fix: PublicStatuses only).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = 'https://gccwebapp.com';
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const EXPORT_URL = `${BASE}/Certificate/ExportCertificates?format=csv`;
const PUBLIC_STATUSES = new Set(['Client Approved - Paid', 'Republished']);

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
      else if (c === '\r') {}
      else if (c === '\n') { row.push(cur); rows.push(row); row = []; cur = ''; }
      else cur += c;
    }
  }
  if (cur || row.length) { row.push(cur); rows.push(row); }
  return rows.filter(r => r.length > 1 || (r.length === 1 && r[0].trim()));
}
function indexHeaders(h) { const i = {}; h.forEach((x, k) => i[x.trim()] = k); return i; }

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    // -------- ANONYMOUS: should return only PublicStatuses --------
    console.log('\n=== gccwebapp.com — ANONYMOUS export (PublicStatuses only) ===');
    {
      const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
      const resp = await ctx.request.get(EXPORT_URL);
      const status = resp.status();
      console.log(`  HTTP ${status}`);

      if (status === 200) {
        const body = await resp.text();
        fs.writeFileSync(path.join(OUT, 'qc182-live-anon.csv'), body);
        const rows = parseCsv(body);
        const data = rows.slice(1);
        console.log(`  data rows: ${data.length}`);

        const statusVals = data.length ? [...new Set(data.map(r => r[indexHeaders(rows[0])['Status']]))] : [];
        const partnerVals = data.length ? [...new Set(data.map(r => r[indexHeaders(rows[0])['Partner']]))] : [];
        const allPublic = statusVals.every(s => PUBLIC_STATUSES.has(s));

        record('QC182-LIVE-anon-1', 'Anonymous export accessible (200)', 'PASS', `${data.length} rows`);
        record('QC182-LIVE-anon-2', 'Anonymous export contains ONLY PublicStatuses',
          allPublic ? 'PASS' : 'FAIL',
          `distinct statuses=${JSON.stringify(statusVals)}`);
        record('QC182-LIVE-anon-3', 'No Draft/Expired/Active leakage to anonymous',
          allPublic ? 'PASS' : 'FAIL',
          `if FAIL → DEF-198 not deployed`);

        console.log(`  partners seen: ${JSON.stringify(partnerVals.slice(0, 8))}${partnerVals.length > 8 ? '...' : ''}`);
      } else if (status === 302 || status === 401) {
        record('QC182-LIVE-anon-1', 'Anonymous export blocked (auth-gated)', 'PASS',
          `HTTP ${status} — endpoint requires login on prod; equally valid scoping (caller has zero scope without auth)`);
      } else {
        record('QC182-LIVE-anon-1', 'Anonymous export unexpected response', 'WARN', `HTTP ${status}`);
      }
      await ctx.close();
    }

    // -------- PARTNER (partner_test if it exists on prod) --------
    console.log('\n=== gccwebapp.com — PARTNER export (partner_test → Amazon, if exists) ===');
    {
      const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
      const page = await ctx.newPage();
      await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
      await page.locator('#Email').fill('partner_test');
      await page.locator('#Password').fill('TestPass123!');
      await Promise.all([
        page.waitForLoadState('domcontentloaded'),
        page.locator('button[type="submit"]').first().click(),
      ]);
      await page.waitForTimeout(2000);
      const postLoginUrl = page.url();
      console.log(`  post-login URL: ${postLoginUrl}`);

      if (/\/Account\/Login/i.test(postLoginUrl)) {
        record('QC182-LIVE-partner-1', 'partner_test exists on prod DB',
          'WARN', 'login failed — fixture not on prod; anonymous check alone is the evidence');
      } else {
        const resp = await ctx.request.get(EXPORT_URL);
        const status = resp.status();
        const body = await resp.text();
        fs.writeFileSync(path.join(OUT, 'qc182-live-partner.csv'), body);
        const rows = parseCsv(body);
        const data = rows.slice(1);
        console.log(`  HTTP ${status}, rows=${data.length}`);

        if (status === 200 && data.length > 0) {
          const idx = indexHeaders(rows[0]);
          const partnerVals = [...new Set(data.map(r => r[idx['Partner']]))];
          const statusVals = [...new Set(data.map(r => r[idx['Status']]))];
          const onePartner = partnerVals.length === 1 && partnerVals[0] === 'Amazon';
          const allPublic = statusVals.every(s => PUBLIC_STATUSES.has(s));

          record('QC182-LIVE-partner-1', 'Partner export scoped to Amazon only',
            onePartner ? 'PASS' : 'FAIL',
            `distinct partners=${JSON.stringify(partnerVals)}`);
          record('QC182-LIVE-partner-2', 'Partner export filtered to PublicStatuses',
            allPublic ? 'PASS' : 'FAIL',
            `distinct statuses=${JSON.stringify(statusVals)}`);
        } else {
          record('QC182-LIVE-partner-1', 'Partner export returned 0/error rows',
            'WARN', `HTTP ${status} rows=${data.length}`);
        }
      }
      await ctx.close();
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, 'qc182-live-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
