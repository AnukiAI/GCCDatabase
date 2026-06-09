// DEF-248 / QC #157 (6/4) — verify multi-value product-ID cells DISPLAY in the same order
// the column SORTS by, so the first value the eye lands on is the value the row was sorted by.
//
// Root cause recap: ASIN/UPC/GTIN/EAN are one-to-many ProductIds collections. The internal Cert
// List displayed a cell's values in INSERTION/PK order while the row sorts by the cell's
// alphabetical MINIMUM (.Min() for ASIN/GTIN/EAN). So "B07ZEBRA, B07ALPHA" displayed but the row
// was positioned as "B07ALPHA" — reads as "ASIN not sorting." The fix orders the displayed list
// ascending (in-memory) so the LEADING shown token == the .Min() sort key.
//
// This probe is FULLY NON-DESTRUCTIVE (read-only; no records created or changed) and conclusive —
// note it could NOT be caught by qc157-sort-verify-local.js, which already used MIN() as its oracle
// and so was blind to the displayed order. For every cert with a multi-value ID cell:
//   1) Pull the cell as shown by the internal (and public) tiles API.
//   2) Pull the SAME cert's raw product IDs from /api/productids/certificate/{id}, which returns
//      them in DisplayOrder = INSERTION order (the pre-fix display order).
//   3) Assert the displayed cell == the values sorted ASCENDING (the fix) AND first token == MIN
//      (== the .Min() sort key). Track "differentiating" certs whose insertion order != sorted —
//      those PROVE the fix actually re-orders the display (≥1 = conclusive, not "data happened to
//      already be in order").
//
// LOCALHOST default. Run (app up on :5101):  node Scripts/smoke-prod/def248-asin-display-order.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const APP_USER = process.env.SMOKE_USER || 'admin', APP_PASS = process.env.SMOKE_PASS || 'TestPass123!';

const LIVE = BASE.includes('gccwebapp') || BASE.includes('greencirclecertified');
const GATE = LIVE ? { username: 'gcc', password: 'Cert-Test-Access-2026!' } : undefined;

const ID_TYPES = ['ASIN', 'UPC', 'GTIN', 'EAN'];
const results = [];
const rec = (id, status, detail = '') => {
  results.push({ id, status, detail });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : status === 'WARN' ? '⚠️ ' : '· ';
  console.log(`${tag} ${id}${detail ? '  — ' + detail : ''}`);
};

const STR = (v) => String(v ?? '').trim();
// Abbreviate a long list for readable proof output: first 3 … last 1 (+N more).
const abbr = (arr) => arr.length <= 5 ? arr.join(', ') : `${arr.slice(0, 3).join(', ')}, …(+${arr.length - 4} more)…, ${arr[arr.length - 1]}`;
// Mirror SQL Server's default CI collation (case-insensitive, ordinal by code point — digits
// before letters). JS localeCompare does the opposite for digit-vs-letter, so compare uppercased
// strings by code point to match what the server's .Min() / ORDER BY actually does.
const cmp = (a, b) => { const A = STR(a).toUpperCase(), B = STR(b).toUpperCase(); return A < B ? -1 : A > B ? 1 : 0; };
const tokens = (cell) => STR(cell).split(',').map(s => s.trim()).filter(Boolean);
const MIN = (cell) => { const t = tokens(cell); return t.length ? t.slice().sort(cmp)[0] : ''; };
const isAscending = (arr) => arr.every((v, i) => i === 0 || cmp(arr[i - 1], v) <= 0);

async function fetchAll(req, endpoint, sortParam) {
  const all = []; let page = 1;
  for (;;) {
    const sep = endpoint.includes('?') ? '&' : '?';
    // 90s timeout: the internal sortBy=asin split-query (now returns the full set, not a 20-cap)
    // is slow on a COLD EF cache; warm runs are sub-second.
    const r = await req.get(`${BASE}${endpoint}${sep}${sortParam}page=${page}&pageSize=100`, { timeout: 90000 });
    if (r.status() !== 200) throw new Error(`${endpoint} ${sortParam} page ${page} -> HTTP ${r.status()}`);
    const j = await r.json();
    const arr = j.certificates || j.Certificates || [];
    all.push(...arr);
    if (!(j.hasMore ?? j.HasMore) || arr.length === 0 || page > 50) break;
    page++;
  }
  return all;
}

// Pull a cert's raw product IDs of a type in DisplayOrder (= insertion order) — the pre-fix display
// order. /api/productids/certificate/{id} (un-paged) returns rows ordered by DisplayOrder, IsActive only.
async function rawInsertionOrder(req, certId, type) {
  const r = await req.get(`${BASE}/api/productids/certificate/${certId}`);
  if (r.status() !== 200) return null;
  const body = await r.json().catch(() => null);
  const rows = body?.data ?? body?.Data ?? [];
  return rows
    .filter(p => String(p.productIdType ?? p.ProductIdType).toUpperCase() === type && (p.isActive ?? p.IsActive))
    .map(p => String(p.productIdValue ?? p.ProductIdValue));
}

// Verify a view's multi-value cells against each cert's raw insertion order.
async function verifyView(req, items, label) {
  let multi = 0, ok = 0, differentiating = 0; const breaks = []; const proofs = [];
  for (const it of items) {
    const certId = it.id ?? it.Id;
    for (const type of ID_TYPES) {
      const f = type.toLowerCase();
      const cell = it[f] ?? it[type];
      const shown = tokens(cell);
      if (shown.length < 2) continue; // single/empty cells can't be mis-ordered
      multi++;
      const ascending = isAscending(shown);
      const leadIsMin = cmp(shown[0], MIN(cell)) === 0;
      if (ascending && leadIsMin) ok++;
      else { breaks.push(`${type}@cert${certId}="${shown.join(',')}" (asc=${ascending} leadMin=${leadIsMin})`); continue; }
      // cross-check against raw insertion order to prove the fix actually re-ordered the display
      const raw = certId != null ? await rawInsertionOrder(req, certId, type) : null;
      if (raw && raw.length >= 2) {
        const rawJoined = raw.join('|');
        const sortedJoined = raw.slice().sort(cmp).join('|');
        if (rawJoined !== sortedJoined) {
          differentiating++;
          if (proofs.length < 4) proofs.push(`${type}@cert${certId}: insertion=[${abbr(raw)}] → shown=[${abbr(shown)}]`);
        }
      }
    }
  }
  if (multi === 0) { rec(`${label}-cells`, 'INFO', 'no multi-value ID cells in this view (single-value data only)'); return { multi, breaks: 0, differentiating }; }
  rec(`${label}-cells`, breaks.length === 0 ? 'PASS' : 'FAIL',
    `${ok}/${multi} multi-value cells ascending + lead==min(sortKey)${breaks.length ? '; breaks: ' + breaks.slice(0, 5).join(' | ') : ''}`);
  rec(`${label}-differentiates-fix`, differentiating > 0 ? 'PASS' : 'INFO',
    differentiating > 0
      ? `${differentiating} cell(s) had a NON-alphabetical insertion order yet display alphabetically → proves the fix re-orders. e.g. ${proofs[0]}`
      : 'all multi-value cells were already inserted in alphabetical order — display correct but not differentiated on this data');
  if (proofs.length > 1) console.log('       proofs: ' + proofs.slice(1).join(' ; '));
  return { multi, breaks: breaks.length, differentiating };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  const getJson = async (url) => (await ctx.request.get(url)).json().catch(() => null);

  try {
    console.log(`\nDEF-248 ASIN/UPC/GTIN/EAN display-order verification (read-only) — ${BASE}\n`);

    await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1000);
    if (/\/Account\/Login/i.test(page.url())) { rec('login', 'FAIL', 'admin login failed'); throw new Error('login failed'); }
    rec('login', 'PASS', page.url().replace(BASE, '') || '/');

    // ---------- Internal view: cells vs raw insertion order ----------
    const internal = await fetchAll(ctx.request, '/api/certificate-tiles-internal', 'sortBy=asin&');
    rec('dataset', internal.length > 0 ? 'PASS' : 'FAIL', `${internal.length} certs in internal scope`);
    const iv = await verifyView(ctx.request, internal, 'internal');

    // ---------- Public view: same guarantee via CertificateService.GetProductIdValues ----------
    try {
      const publicItems = await fetchAll(ctx.request, '/api/certificate-tiles', 'sort=cert-number-newest&');
      await verifyView(ctx.request, publicItems, 'public');
    } catch (e) { rec('public-cells', 'WARN', `public tiles fetch failed: ${e.message.split('\n')[0]}`); }

    // ---------- Sort alignment: rows ordered by each cell's leading(==min) token, both directions;
    //            cell contents stay ascending regardless of row direction ----------
    for (const [dir, sortParam] of [['asc', 'sortBy=asin&'], ['desc', 'sortBy=asin-za&']]) {
      const items = await fetchAll(ctx.request, '/api/certificate-tiles-internal', sortParam);
      const leads = items.map(it => MIN(it.asin ?? it.Asin)).filter(Boolean);
      const monotonic = leads.every((v, i) => i === 0 || (dir === 'asc' ? cmp(leads[i - 1], v) <= 0 : cmp(leads[i - 1], v) >= 0));
      const cellOk = items.every(it => isAscending(tokens(it.asin ?? it.Asin)));
      rec(`sort-${dir}-rows-by-lead`, monotonic ? 'PASS' : 'FAIL', `${leads.length} populated rows monotonic by leading(min) token`);
      rec(`sort-${dir}-cells-ascending`, cellOk ? 'PASS' : 'FAIL', 'every ASIN cell ordered ascending (direction-independent)');
    }

    if (iv.multi === 0) rec('coverage', 'WARN', 'no multi-value ID cells exist in the dataset — fix is logically correct but unexercised; seed a cert with ≥2 ASINs to exercise it');
  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    rec('ERR', 'FAIL', e.message.split('\n')[0]);
  } finally {
    fs.writeFileSync(path.join(OUT, 'def248-asin-display-order-results.json'),
      JSON.stringify({ ts: new Date().toISOString(), base: BASE, results }, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\n[def248] ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length}) against ${BASE}`);
  process.exit(fail === 0 ? 0 : 1);
})();
