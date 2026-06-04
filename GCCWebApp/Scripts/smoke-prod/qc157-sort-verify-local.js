// QC #157 (5/29/26 re-raise) — verify Cert List View column sorting on LOCALHOST.
//
// Carly's 5/29 note: Location, ASIN, Cert Subcategories, Company URL "still don't sort"
// (Status confirmed working). DEF-165 + DEF-181 wired all of these server-side.
//
// Sorting is 100% SERVER-SIDE, so this drives the real internal tile API
// (/api/certificate-tiles-internal) with each sortBy value, paginates the FULL result
// set, and asserts the returned sort key is monotonic. This removes every confound of a
// DOM test (tile-vs-list toggle, count badges instead of values, composite display text,
// enum-vs-label, click-direction races). Oracle handles NULL/empty placement (SQL Server:
// NULLs first in ASC, last in DESC) and uses contiguity for the enum-backed Status column.
//
// Run (app up on 5101):
//   BASE_URL=http://localhost:5101 node Scripts/smoke-prod/qc157-sort-verify-local.js
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const ADMIN = { email: process.env.SMOKE_USER || 'admin', password: process.env.SMOKE_PASS || 'TestPass123!' };
// Test-env HTTP Basic gate — only applied for non-localhost targets (e.g. gccwebapp.com).
const IS_REMOTE = !/localhost|127\.0\.0\.1/.test(BASE);
const GATE = IS_REMOTE
  ? { username: process.env.SMOKE_REMOTE_USER || 'gcc', password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' }
  : undefined;
const OUT = path.join(__dirname, '_results');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

// min of a comma-separated multi-value field — mirrors the server's .Min() per-cert pick.
const MIN = (raw) => {
  const p = String(raw ?? '').split(',').map(s => s.trim()).filter(Boolean);
  // SQL MIN() over the collation = case-insensitive codepoint min (digits before letters).
  return p.length ? p.sort((a, b) => { const A = a.toUpperCase(), B = b.toUpperCase(); return A < B ? -1 : A > B ? 1 : 0; })[0] : '';
};
const STR = (v) => String(v ?? '').trim();

// each column: the asc/desc sortBy keys + how to pull the per-row sort key from an API item.
const COLUMNS = [
  { label: 'Location',           asc: 'location-az', desc: 'location-za', key: c => STR(c.location),  kind: 'string' },
  { label: 'ASIN',               asc: 'asin',        desc: 'asin-za',     key: c => MIN(c.asin),      kind: 'string' },
  { label: 'Cert Subcategories', asc: 'cert-sub',    desc: 'cert-sub-za', key: c => MIN(c.certSubcategories), kind: 'string' },
  { label: 'Company URL',        asc: 'company-url', desc: 'company-url-za', key: c => STR(c.companyUrl), kind: 'string' },
  // context columns Carly said WORK — confirm no regression. Status is enum-backed: its
  // display label is NOT its sort key, so we check contiguity (equal labels adjacent) + asc!=desc.
  { label: 'Partner',            asc: 'partner',     desc: 'partner-za',  key: c => STR(c.partner),   kind: 'string' },
  { label: 'Status',             asc: 'status',      desc: 'status-za',   key: c => STR(c.status),    kind: 'enum' },
];

const results = [];
const rec = (id, status, detail = '') => { results.push({ id, status, detail }); console.log(`[${status}] ${id}${detail ? '  — ' + detail : ''}`); };
// Mirror SQL Server's default CI collation (SQL_Latin1_General_CP1_CI_AS): case-insensitive,
// ordinal by code point — which orders DIGITS before LETTERS ('5' < 'R'). JS localeCompare
// (esp. numeric:true) does the opposite for digit-vs-letter and produces false breaks on
// alphanumeric IDs like ASINs. Compare uppercased strings by code point to match the DB.
const cmp = (a, b) => { const A = a.toUpperCase(), B = b.toUpperCase(); return A < B ? -1 : A > B ? 1 : 0; };

// QC #157 fix expectation: empty/blank values sort LAST in BOTH directions, so populated
// values always lead. Assert (a) blanks are clustered at the END (populated form a prefix)
// and (b) that populated prefix is monotonic in the requested direction.
function checkMonotonic(keys, dir) {
  if (keys.every(k => !k.length)) return { ok: true, note: 'all empty (nothing to order)' };
  const populated = keys.filter(k => k.length);
  // blanks must all be at the tail: the first `populated.length` entries are the non-empty prefix.
  let placementOk = true, placeNote = '';
  if (keys.length !== populated.length) {
    const prefixAllPopulated = keys.slice(0, populated.length).every(k => k.length);
    const tailAllEmpty = keys.slice(populated.length).every(k => !k.length);
    if (!(prefixAllPopulated && tailAllEmpty)) { placementOk = false; placeNote = 'blanks not clustered at END (a blank precedes a populated value)'; }
  }
  for (let i = 1; i < populated.length; i++) {
    const c = cmp(populated[i - 1], populated[i]);
    if (dir === 'asc' && c > 0) return { ok: false, note: `asc break: "${populated[i - 1]}" > "${populated[i]}"` };
    if (dir === 'desc' && c < 0) return { ok: false, note: `desc break: "${populated[i - 1]}" < "${populated[i]}"` };
  }
  return { ok: placementOk, note: placementOk ? `${populated.length} populated lead, blanks last` : placeNote };
}

// enum-backed: sort key != label, so verify (a) equal labels are contiguous in both
// directions and (b) asc and desc are not the identical sequence (proves it re-sorts).
function checkEnum(ascKeys, descKeys) {
  const contiguous = (arr) => {
    const seen = new Set(); let prev = null;
    for (const k of arr) { if (k !== prev) { if (seen.has(k)) return false; seen.add(k); prev = k; } }
    return true;
  };
  const distinct = ascKeys.length > 1 && ascKeys.join('|') !== descKeys.join('|');
  const ok = contiguous(ascKeys) && contiguous(descKeys) && distinct;
  return { ok, note: `asc-contig=${contiguous(ascKeys)} desc-contig=${contiguous(descKeys)} asc!=desc=${distinct}` };
}

async function fetchAll(req, sortBy) {
  const all = []; let page = 1;
  for (;;) {
    const r = await req.get(`${BASE}/api/certificate-tiles-internal?sortBy=${encodeURIComponent(sortBy)}&page=${page}&pageSize=100`);
    if (r.status() !== 200) throw new Error(`API ${sortBy} page ${page} -> HTTP ${r.status()}`);
    const j = await r.json();
    const arr = j.certificates || [];
    all.push(...arr);
    if (!j.hasMore || arr.length === 0 || page > 50) break;
    page++;
  }
  return all;
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, httpCredentials: GATE });
  const page = await ctx.newPage();
  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('#Email').fill(ADMIN.email);
    await page.locator('#Password').fill(ADMIN.password);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(800);
    rec('login', /\/Account\/Login/i.test(page.url()) ? 'FAIL' : 'PASS', page.url().replace(BASE, '') || '/');

    const total = (await fetchAll(ctx.request, 'newest')).length;
    rec('dataset', total > 1 ? 'PASS' : 'FAIL', `${total} certs total (analyst/admin scope)`);

    // DEF-181 canary: sorting by cert-results touches the CertificationResultsNumeric
    // computed column. When its migration was missing on prod, this query threw
    // "Invalid column name" and the API returned non-200 / empty (shipped-2026-05.md:743).
    // A clean 200 here = the DEF-181 migration AND the sort code path are live.
    try {
      const cr = await fetchAll(ctx.request, 'cert-results');
      rec('DEF-181 canary (cert-results sort)', 'PASS', `HTTP 200, ${cr.length} rows — CertificationResultsNumeric column present + queried`);
    } catch (e) {
      rec('DEF-181 canary (cert-results sort)', 'FAIL', e.message.split('\n')[0]);
    }

    for (const col of COLUMNS) {
      const ascItems = await fetchAll(ctx.request, col.asc);
      const descItems = await fetchAll(ctx.request, col.desc);
      const ascKeys = ascItems.map(col.key);
      const descKeys = descItems.map(col.key);
      const populated = ascKeys.filter(k => k.length).length;

      if (col.kind === 'enum') {
        const v = checkEnum(ascKeys, descKeys);
        rec(`${col.label}`, v.ok ? 'PASS' : 'FAIL', v.note);
      } else {
        const a = checkMonotonic(ascKeys, 'asc');
        const d = checkMonotonic(descKeys, 'desc');
        rec(`${col.label}:asc`, a.ok ? 'PASS' : 'FAIL',
          `${populated}/${ascKeys.length} populated; ${a.note}; firstPopulated=[${ascKeys.filter(k => k.length).slice(0, 3).join(' | ')}]`);
        rec(`${col.label}:desc`, d.ok ? 'PASS' : 'FAIL',
          `${d.note}; firstPopulated=[${descKeys.filter(k => k.length).slice(0, 3).join(' | ')}]`);
      }
    }
  } catch (e) {
    rec('fatal', 'ERROR', e.message.split('\n')[0]);
  } finally {
    await ctx.close();
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status !== 'PASS').length;
  fs.writeFileSync(path.join(OUT, 'qc157-sort-local-results.json'),
    JSON.stringify({ ts: new Date().toISOString(), base: BASE, pass, fail, results }, null, 2));
  console.log(`\n[qc157-sort] ${pass} PASS / ${fail} not-pass against ${BASE}`);
  process.exit(fail === 0 ? 0 : 1);
})();
