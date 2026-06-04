// QC #182 (6/3 log) — Partner "Export All to CSV" data-scoping verification.
// Claim: partner export returns ALL certs in the DB, not just the partner's scope.
//
// Method (READ-ONLY, no writes): log in as partner.amazon on the live site, then compare
//   (a) authorized scope  = totalCount from the scoped tile API /api/CertificateTileExternal
//   (b) export row count   = data rows returned by /Certificate/ExportCertificates (the
//                            endpoint the "Export All" button calls, with no filters)
//   (c) total DB certs     = 39 (measured directly against prod)
// If (b) > (a)  => scoping gap / leak confirmed.  If (b) == (a) => DEF-198 fix holds.
//
// Run: node Scripts/smoke-prod/qc182-partner-export-scope.js
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = { username: process.env.SMOKE_REMOTE_USER || 'gcc',
               password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' };
const TOTAL_DB_CERTS = 39; // measured on prod 2026-06-03
const OUT = path.join(__dirname, '_results');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ httpCredentials: GATE, ignoreHTTPSErrors: true,
    viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  const out = { ts: new Date().toISOString(), base: BASE, totalDbCerts: TOTAL_DB_CERTS };
  try {
    // login as partner
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('#Email').fill('partner.amazon');
    await page.locator('#Password').fill('TestPass123!');
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

    // (b) what Export All actually returns (no filters = "export all")
    const exp = await page.evaluate(async () => {
      const r = await fetch('/Certificate/ExportCertificates?format=csv', { headers: { 'Accept': 'text/csv' } });
      const text = await r.text();
      return { status: r.status, contentType: r.headers.get('content-type'), len: text.length, text };
    });
    // count data rows (handle quoted newlines crudely: count lines, minus header)
    const lines = exp.text.split(/\r?\n/).filter(l => l.trim().length > 0);
    out.export = { status: exp.status, contentType: exp.contentType, bytes: exp.len,
                   dataRows: Math.max(0, lines.length - 1), headerSample: lines[0]?.slice(0, 120) };

    // best-effort: pull distinct company-ish column values to show cross-partner spread
    const header = (lines[0] || '').split(',').map(s => s.replace(/^"|"$/g, '').trim().toLowerCase());
    const compIdx = header.findIndex(h => h.includes('company'));
    if (compIdx >= 0) {
      const comps = new Set();
      for (const l of lines.slice(1)) {
        const cols = l.match(/("([^"]|"")*"|[^,]*)(,|$)/g) || [];
        const v = (cols[compIdx] || '').replace(/^"|",?$|,$/g, '').trim();
        if (v) comps.add(v);
      }
      out.distinctCompaniesInExport = comps.size;
      out.companiesSample = [...comps].slice(0, 12);
    }

    // verdict
    const a = out.authorizedScope.totalCount;
    const b = out.export.dataRows;
    if (b != null && a != null) {
      out.verdict = (b > a)
        ? `LEAK CONFIRMED — export returned ${b} rows but partner is scoped to ${a} (DB total ${TOTAL_DB_CERTS})`
        : `SCOPED OK — export ${b} rows matches authorized scope ${a}`;
    } else {
      out.verdict = `INCONCLUSIVE — could not read counts (scope=${a}, exportRows=${b})`;
    }
    console.log(JSON.stringify(out, (k, v) => k === 'text' ? undefined : v, 2));
    fs.writeFileSync(path.join(OUT, 'qc182-partner-export-results.json'),
      JSON.stringify(out, (k, v) => k === 'text' ? undefined : v, 2));
    // keep the raw CSV for evidence
    fs.writeFileSync(path.join(OUT, 'qc182-partner-export-raw.csv'), exp.text);
  } catch (e) {
    console.error('ERROR', e.message);
    out.error = e.message;
  } finally {
    await ctx.close(); await browser.close();
  }
})();
