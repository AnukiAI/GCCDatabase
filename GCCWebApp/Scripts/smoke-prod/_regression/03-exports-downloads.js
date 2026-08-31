// Phase 3 — exports, template downloads, and certificate PDF downloads.
//
// Two things are being tested at once and they fail differently:
//   ACCESS  — can this role reach the endpoint at all (policy attribute honoured)?
//   SCOPING — when it can, does the file contain only that role's rows?
// An export that 200s with every row in the database passes an access test and is a data breach.
// DEF-198/QC #182 was exactly that: Partner web users fell through to the unscoped path and
// received every cert in the DB. So each successful export is parsed and its rows are compared
// against the population that role is allowed to see (established in phase 2).

const fs = require('fs');
const path = require('path');
const { BASE, HEADLESS, ARTIFACT_DIR, session, makeRecorder, chromium, ensureDirs } = require('./_lib/regression-lib');

// Minimal RFC-4180 CSV parse — quoted fields carry embedded commas and newlines in this data.
function parseCsv(text) {
  const rows = [];
  let row = [], field = '', inQ = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inQ) {
      if (c === '"') { if (text[i + 1] === '"') { field += '"'; i++; } else inQ = false; }
      else field += c;
    } else if (c === '"') inQ = true;
    else if (c === ',') { row.push(field); field = ''; }
    else if (c === '\n') { row.push(field); rows.push(row); row = []; field = ''; }
    else if (c !== '\r') field += c;
  }
  if (field.length || row.length) { row.push(field); rows.push(row); }
  return rows.filter((r) => r.length > 1 || (r[0] ?? '').trim() !== '');
}

const CERT_EXPORT = '/Certificate/ExportCertificates?format=csv';

// Analyst/Admin-only export surfaces. `type` values are the ones the admin pages actually request.
const TABLE_EXPORTS = [
  { url: '/Export/DownloadActiveProductIds', label: 'Active Product IDs' },
  { url: '/CertificateImport/DownloadTemplate', label: 'Certificate import template' },
  { url: '/Product/DownloadProductImportTemplate', label: 'Product import template' },
];

const ROLES = ['anon', 'external', 'external2', 'amazon', 'novi', 'analyst', 'admin'];

// Column visibility is a role contract: DEF-128 — only Admin/Analyst get the Internal Tag column.
const INTERNAL_ONLY_COLUMNS = [/internal\s*tag/i];

(async () => {
  ensureDirs();
  const { rec, save } = makeRecorder('03-exports-downloads');
  const startedAt = new Date().toISOString();
  console.log('\n=== Phase 3: exports + downloads ===\n');

  const browser = await chromium.launch({ headless: HEADLESS });
  const exports = {};

  for (const role of ROLES) {
    const key = role === 'anon' ? null : role;
    let s;
    try { s = await session(browser, key); }
    catch (e) { rec(`exp.session.${role}`, `Session for ${role}`, 'FAIL', e.message); continue; }
    const { ctx, page, auth } = s;

    // --- 1. The main certificate CSV export ---
    const t0 = Date.now();
    const resp = await page.request.get(BASE + CERT_EXPORT, { failOnStatusCode: false, timeout: 180000 });
    const ms = Date.now() - t0;
    const status = resp.status();
    const ctype = resp.headers()['content-type'] || '';
    const disp = resp.headers()['content-disposition'] || '';

    if (status !== 200) {
      rec(`exp.certcsv.${role}`, `Certificate CSV export as ${role}`, 'FAIL', `HTTP ${status}`);
    } else if (/text\/html/i.test(ctype)) {
      // A 200 that is HTML is a login bounce or the test-env gate wearing a success costume.
      rec(`exp.certcsv.${role}`, `Certificate CSV export as ${role}`, 'FAIL',
        `HTTP 200 but content-type is ${ctype} — not a CSV (login bounce or gate page)`);
    } else {
      const text = await resp.text();
      const rows = parseCsv(text);
      const header = rows[0] || [];
      const body = rows.slice(1);
      const file = path.join(ARTIFACT_DIR, `cert-export-${role}.csv`);
      fs.writeFileSync(file, text);

      // CSV ONLY is a hard project rule — no Excel/JSON/PDF from admin surfaces.
      rec(`exp.certcsv.${role}.format`, `${role}: export is CSV`,
        /csv|octet-stream/i.test(ctype) && /\.csv/i.test(disp) ? 'PASS' : 'WARN',
        `content-type=${ctype} disposition=${disp}`);

      // The column ships as "Certificate #", and the first header cell carries a UTF-8 BOM, so
      // match loosely on both spellings rather than an exact string.
      const numIdx = header.findIndex((h) => /cert(ificate)?\s*(number|no\.?|#)/i.test(h.replace(/^﻿/, '')));
      const nums = numIdx >= 0 ? new Set(body.map((r) => (r[numIdx] || '').trim()).filter(Boolean)) : new Set();
      exports[role] = { rows: body.length, header, numbers: nums, bytes: text.length, ms };

      rec(`exp.certcsv.${role}`, `Certificate CSV export as ${role}`, 'PASS',
        `${body.length} rows, ${header.length} cols, ${(text.length / 1024).toFixed(0)}KB in ${ms}ms`);

      rec(`exp.certcsv.${role}.has-certnumber`, `${role}: export carries a Certificate Number column`,
        numIdx >= 0 ? 'PASS' : 'FAIL',
        numIdx >= 0 ? `column "${header[numIdx]}" at index ${numIdx}` : `header: ${header.slice(0, 12).join(' | ')}`);

      // DEF-128 column gating, both directions.
      const internalCols = header.filter((h) => INTERNAL_ONLY_COLUMNS.some((rx) => rx.test(h)));
      const shouldHave = role === 'analyst' || role === 'admin';
      rec(`exp.certcsv.${role}.internal-tag-column`,
        `${role}: Internal Tag column ${shouldHave ? 'present' : 'absent'} (DEF-128)`,
        (shouldHave ? internalCols.length > 0 : internalCols.length === 0) ? 'PASS' : 'FAIL',
        internalCols.length ? `found: ${internalCols.join(', ')}` : 'not present');
    }

    // --- 2. Analyst/Admin export + template surfaces ---
    for (const t of TABLE_EXPORTS) {
      const r2 = await page.request.get(BASE + t.url, { failOnStatusCode: false, timeout: 120000 });
      const st = r2.status();
      const ct = r2.headers()['content-type'] || '';
      const isFile = st === 200 && !/text\/html/i.test(ct);
      const allowed = role === 'analyst' || role === 'admin';
      rec(`exp.table.${t.label}.${role}`, `${t.label} as ${role}`,
        allowed === isFile ? 'PASS' : 'FAIL',
        `expected ${allowed ? 'a file' : 'denial'}, got HTTP ${st} content-type=${ct}`);
    }

    await ctx.close();
  }

  // --- 3. Export scoping: the file must not be wider than the role's own population ---
  // Compared against the role's tile-API population from phase 2 where available.
  const phase2Path = path.join(__dirname, '_results', '02-data-scoping.json');
  if (fs.existsSync(phase2Path)) {
    const p2 = JSON.parse(fs.readFileSync(phase2Path, 'utf8'));
    const pop = p2.populations || {};
    const expected = {
      anon: pop.public?.totalCount, external: pop.external?.totalCount, external2: pop.external2?.totalCount,
      amazon: pop.amazon?.totalCount, novi: pop.novi?.totalCount,
      analyst: pop.internal_analyst?.totalCount, admin: pop.internal_admin?.totalCount,
    };
    for (const role of ROLES) {
      if (!exports[role] || expected[role] == null) continue;
      const got = exports[role].rows, want = expected[role];
      rec(`exp.scope.${role}`, `${role}: export row count matches what the role can see`,
        got === want ? 'PASS' : 'FAIL',
        `export=${got} rows, tile API totalCount=${want}, delta=${got - want}`);
    }
    // Cross-role: the two scoped externals must not appear in each other's export.
    if (exports.external && exports.external2) {
      const shared = [...exports.external.numbers].filter((n) => exports.external2.numbers.has(n));
      rec('exp.scope.external-vs-external2', 'Company exports do not overlap',
        shared.length === 0 ? 'PASS' : 'FAIL',
        `external=${exports.external.numbers.size} external2=${exports.external2.numbers.size} shared=${shared.length}${shared.length ? ' e.g. ' + shared.slice(0, 5) : ''}`);
    }
  } else {
    rec('exp.scope', 'Export scoping comparison', 'WARN', 'phase 2 results not found — run 02-data-scoping.js first');
  }

  await browser.close();
  const ser = Object.fromEntries(Object.entries(exports).map(([k, v]) => [k, { ...v, numbers: undefined, numberCount: v.numbers?.size }]));
  save({ startedAt, finishedAt: new Date().toISOString(), exports: ser });
})().catch((e) => { console.error('FATAL', e); process.exit(1); });
