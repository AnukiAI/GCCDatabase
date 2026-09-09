// Phase 3 — exports, template downloads, and certificate PDF downloads.
//
// Two things are being tested at once and they fail differently:
//   ACCESS  — can this role reach the endpoint at all (policy attribute honoured)?
//   SCOPING — when it can, does the file contain only that role's rows?
// An export that 200s with every row in the database passes an access test and is a data breach.
// DEF-198/QC #182 was exactly that: Partner web users fell through to the unscoped path and
// received every cert in the DB. So each successful export is parsed and its rows are compared
// against the population that role is allowed to see.
//
// DEF-408: that population is now fetched HERE, in the same session and at the same moment as the
// export, instead of being read out of phase 2's results file. The old cross-phase read had three
// failure modes and all three broke the check silently or falsely:
//   1. a day-old snapshot graded today's export and produced 4 false FAILs (5,924 vs 5,731 — the
//      difference was simply the batch-2 load);
//   2. a missing role population hit `continue`, so the check VANISHED rather than reporting;
//   3. no phase-2 file at all skipped the whole block, collapsing seven per-role checks into a
//      single WARN — visible, but the phase still reported 0 FAIL and no role was actually checked.
// This check is directional — it exists to catch an export WIDER than the role's population — so
// crying wolf and silently disappearing are both ways of losing the one signal that catches a
// DEF-198-class leak.

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

// DEF-408: the tile API that defines each role's legitimate population. Same mapping phase 2 uses.
// Only page 1 is requested — totalCount comes back on it, and the row SET comparison that needs a
// full walk is phase 2's job, not this one's.
const POPULATION_API = {
  anon: '/api/certificate-tiles',
  external: '/api/CertificateTileExternal',
  external2: '/api/CertificateTileExternal',
  amazon: '/api/CertificateTileExternal',
  novi: '/api/CertificateTileExternal',
  analyst: '/api/certificate-tiles-internal',
  admin: '/api/certificate-tiles-internal',
};

// Returns { totalCount } or { error }. Never returns a number it did not read from a 200 body —
// DEF-392: a harness must not report a figure it did not actually measure.
async function fetchPopulation(page, role) {
  const endpoint = POPULATION_API[role];
  if (!endpoint) return { error: `no population endpoint mapped for role ${role}` };
  try {
    const resp = await page.request.get(`${BASE}${endpoint}?page=1&pageSize=1`, {
      failOnStatusCode: false, timeout: 180000,
    });
    if (resp.status() !== 200) return { error: `HTTP ${resp.status()} from ${endpoint}` };
    const body = await resp.json();
    if (typeof body.totalCount !== 'number') return { error: `no numeric totalCount in ${endpoint} response` };
    return { totalCount: body.totalCount };
  } catch (e) {
    return { error: `${endpoint}: ${String(e.message).slice(0, 120)}` };
  }
}

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
      // DEF-408: same session, same moment as the export it will be compared against.
      const popResult = await fetchPopulation(page, role);
      exports[role] = {
        rows: body.length, header, numbers: nums, bytes: text.length, ms,
        population: popResult.totalCount ?? null, populationError: popResult.error ?? null,
      };

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
  // DEF-408: both numbers come from this run. No phase-2 file is read, so there is nothing to be
  // stale, and every role reports an outcome — a role that cannot be measured says so.
  {
    for (const role of ROLES) {
      if (!exports[role]) {
        // The export itself failed and was already recorded as such above; do not also imply the
        // scope check passed by staying silent about it.
        rec(`exp.scope.${role}`, `${role}: export row count matches what the role can see`, 'WARN',
          'NOT RUN — no export was retrieved for this role');
        continue;
      }
      const { rows: got, population: want, populationError } = exports[role];
      if (want == null) {
        // Never silently skip. An absent population used to `continue`, and a check that vanishes
        // reads as a pass.
        rec(`exp.scope.${role}`, `${role}: export row count matches what the role can see`, 'FAIL',
          `NOT MEASURED — could not read this role's population: ${populationError || 'unknown reason'}`);
        continue;
      }
      // Wider than the population is the breach direction (DEF-198); narrower is a different
      // problem but still a disagreement, so both fail — the note says which way it went.
      const delta = got - want;
      rec(`exp.scope.${role}`, `${role}: export row count matches what the role can see`,
        delta === 0 ? 'PASS' : 'FAIL',
        `export=${got} rows, tile API totalCount=${want}, delta=${delta}` +
        (delta > 0 ? ' — export is WIDER than the role may see (breach direction)'
          : delta < 0 ? ' — export is narrower than the role may see' : ''));
    }
    // Cross-role: the two scoped externals must not appear in each other's export.
    if (exports.external && exports.external2) {
      const shared = [...exports.external.numbers].filter((n) => exports.external2.numbers.has(n));
      rec('exp.scope.external-vs-external2', 'Company exports do not overlap',
        shared.length === 0 ? 'PASS' : 'FAIL',
        `external=${exports.external.numbers.size} external2=${exports.external2.numbers.size} shared=${shared.length}${shared.length ? ' e.g. ' + shared.slice(0, 5) : ''}`);
    }
  }

  await browser.close();
  const ser = Object.fromEntries(Object.entries(exports).map(([k, v]) => [k, { ...v, numbers: undefined, numberCount: v.numbers?.size }]));
  save({ startedAt, finishedAt: new Date().toISOString(), exports: ser });
})().catch((e) => { console.error('FATAL', e); process.exit(1); });
