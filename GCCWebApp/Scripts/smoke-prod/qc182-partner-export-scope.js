// QC #182 (6/3 log) — Partner "Export All to CSV" data-scoping verification.
// Claim: partner export returns ALL certs in the DB, not just the partner's scope.
//
// Method (READ-ONLY, no writes): log in as a real partner principal, then compare
//   (a) authorized scope  = totalCount from the scoped tile API /api/CertificateTileExternal
//   (b) export cert count  = DISTINCT certificate numbers in /Certificate/ExportCertificates
//                            (the endpoint the "Export All" button calls, with no filters)
// If (b) > (a)  => scoping gap / leak confirmed.  If (b) <= (a) => scoped OK.
//
// FIXES (2026-06-18): the old version hard-coded a partner user that did not exist
// ('partner.amazon'), so login silently failed and the test measured the PUBLIC/anonymous
// scope and reported a bogus "leak". It also counted CSV rows by splitting on newlines,
// which over-counts when a field contains an embedded newline. This version (1) uses the
// real partner user (env-overridable), (2) HARD-ASSERTS login succeeded before measuring,
// and (3) parses the CSV properly and compares DISTINCT cert numbers.
//
// Run (local):  BASE_URL=http://localhost:5101 node Scripts/smoke-prod/qc182-partner-export-scope.js
// Run (prod):   node Scripts/smoke-prod/qc182-partner-export-scope.js   (set SMOKE_PARTNER_USER if the prod username differs)
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
// Basic-auth gate in front of the remote test env (not the app login).
const GATE = { username: process.env.SMOKE_REMOTE_USER || 'gcc',
               password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' };
// App login for the partner principal under test. The real partner user is an email;
// override per environment if prod uses a different one.
const PARTNER_USER = process.env.SMOKE_PARTNER_USER || 'partner@amazon.com';
const PARTNER_PASS = process.env.SMOKE_PARTNER_PASS || 'TestPass123!';
const OUT = path.join(__dirname, '_results');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

// Minimal RFC-4180 CSV parser: handles quoted fields containing commas, embedded
// newlines, and escaped double-quotes (""). Returns an array of rows (arrays of fields).
function parseCsv(text) {
  const rows = [];
  let row = [], field = '', inQuotes = false;
  for (let i = 0; i < text.length; i++) {
    const ch = text[i];
    if (inQuotes) {
      if (ch === '"') {
        if (text[i + 1] === '"') { field += '"'; i++; }
        else inQuotes = false;
      } else field += ch;
    } else if (ch === '"') {
      inQuotes = true;
    } else if (ch === ',') {
      row.push(field); field = '';
    } else if (ch === '\n' || ch === '\r') {
      if (ch === '\r' && text[i + 1] === '\n') i++;
      row.push(field); rows.push(row); row = []; field = '';
    } else field += ch;
  }
  // flush trailing field/row if the file doesn't end with a newline
  if (field.length || row.length) { row.push(field); rows.push(row); }
  // drop fully-empty trailing rows
  return rows.filter(r => r.some(c => c && c.length));
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ httpCredentials: GATE, ignoreHTTPSErrors: true,
    viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  const out = { ts: new Date().toISOString(), base: BASE, partnerUser: PARTNER_USER };
  try {
    // login as partner
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('#Email').fill(PARTNER_USER);
    await page.locator('#Password').fill(PARTNER_PASS);
    await Promise.all([ page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click() ]);
    await page.waitForTimeout(1200);
    out.landedAfterLogin = page.url().replace(BASE, '') || '/';

    // (a) authorized scope from the scoped tile API
    const tile = await page.evaluate(async () => {
      const r = await fetch('/api/CertificateTileExternal?page=1&pageSize=1', { headers: { 'Accept': 'application/json' } });
      const j = await r.json().catch(() => ({}));
      return { status: r.status, totalCount: j.totalCount ?? j.TotalCount ?? null };
    });
    out.authorizedScope = tile;

    // HARD login-success gate: a 401 from the authenticated tile API (or a bounce back to
    // the login page) means we are NOT logged in as the partner. Bail loudly instead of
    // silently measuring the public scope (that produced the old false-positive "leak").
    out.loginOk = !/\/Account\/Login/i.test(page.url()) && tile.status !== 401;
    if (!out.loginOk) {
      out.verdict = `LOGIN FAILED for '${PARTNER_USER}' (tileStatus=${tile.status}, landed=${out.landedAfterLogin}) — ` +
        `cannot assess scope. Set SMOKE_PARTNER_USER/SMOKE_PARTNER_PASS to a valid partner login.`;
      console.log(JSON.stringify(out, (k, v) => k === 'text' ? undefined : v, 2));
      fs.writeFileSync(path.join(OUT, 'qc182-partner-export-results.json'),
        JSON.stringify(out, (k, v) => k === 'text' ? undefined : v, 2));
      process.exitCode = 1;
      return;
    }

    // (b) what Export All actually returns (no filters = "export all")
    const exp = await page.evaluate(async () => {
      const r = await fetch('/Certificate/ExportCertificates?format=csv', { headers: { 'Accept': 'text/csv' } });
      const text = await r.text();
      return { status: r.status, contentType: r.headers.get('content-type'), len: text.length, text };
    });

    const rows = parseCsv(exp.text);
    const header = rows.length ? rows[0] : [];
    const dataRows = rows.slice(1);
    // distinct certificate numbers = the true cert count (rows can exceed certs if a field
    // wraps lines; certs can't be double-counted this way).
    let certIdx = header.findIndex(h => /cert/i.test(h) && /(num|#)/i.test(h));
    if (certIdx < 0) certIdx = header.findIndex(h => /number/i.test(h));
    const distinctCerts = certIdx >= 0
      ? new Set(dataRows.map(r => (r[certIdx] || '').trim()).filter(Boolean)).size
      : null;
    out.export = { status: exp.status, contentType: exp.contentType, bytes: exp.len,
                   dataRows: dataRows.length, distinctCerts,
                   certColumn: certIdx >= 0 ? header[certIdx] : '(not found)',
                   headerSample: header.join(',').slice(0, 120) };

    // best-effort: distinct company values, to show the (legitimate) cross-company spread
    const compIdx = header.findIndex(h => /company/i.test(h));
    if (compIdx >= 0) {
      const comps = new Set(dataRows.map(r => (r[compIdx] || '').trim()).filter(Boolean));
      out.distinctCompaniesInExport = comps.size;
      out.companiesSample = [...comps].slice(0, 12);
    }

    // verdict — compare DISTINCT certs (fallback to dataRows if the cert column is missing)
    const a = out.authorizedScope.totalCount;
    const b = out.export.distinctCerts != null ? out.export.distinctCerts : out.export.dataRows;
    if (b != null && a != null) {
      out.verdict = (b > a)
        ? `LEAK CONFIRMED — export exposed ${b} distinct certs but partner is scoped to ${a}`
        : `SCOPED OK — export ${b} distinct certs within authorized scope ${a}`;
      if (b > a) process.exitCode = 1;
    } else {
      out.verdict = `INCONCLUSIVE — could not read counts (scope=${a}, exportCerts=${b})`;
    }
    console.log(JSON.stringify(out, (k, v) => k === 'text' ? undefined : v, 2));
    fs.writeFileSync(path.join(OUT, 'qc182-partner-export-results.json'),
      JSON.stringify(out, (k, v) => k === 'text' ? undefined : v, 2));
    // keep the raw CSV for evidence
    fs.writeFileSync(path.join(OUT, 'qc182-partner-export-raw.csv'), exp.text);
  } catch (e) {
    console.error('ERROR', e.message);
    out.error = e.message;
    process.exitCode = 1;
  } finally {
    await ctx.close(); await browser.close();
  }
})();
