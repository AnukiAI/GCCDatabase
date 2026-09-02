// DEF-359 — LIVE proof that the "Active Product Identifiers" export no longer emits identifiers
// for certificates that have not yet reached their effective date.
//
// WHY THIS IS A THIRD SCRIPT. DEF-358 (v1 partner API) and DEF-376 (partner tile view) each had to
// be verified on their own surface because each holds its own copy of the visibility decision. This
// is the third: `ExportController.DownloadActiveProductIds`. `Models/CertificateVisibility.cs`
// records ExportController as deliberately excluded from the DEF-358 gate — right for a staff
// screen, wrong for THIS export, whose own docstring defines its filter as "parent cert is publicly
// live", the exact phrase DEF-358 redefined. It leaked 14 rows (26-0441 x12, 26-0442 x2) two months
// early, on the one artifact most likely to be forwarded, carrying `ID Partner` = Amazon.
//
// THE ASSERTION IS BLANKET, NOT TARGETED. The export writes each row's Effective Date in column 3
// as yyyy-MM-dd, so every row can be checked directly rather than looking only for the two known
// certificates. That matters because the exposure scales with whatever happens to be staged
// ([[feedback_enumerate_dont_sample_populations]]) — a targeted check would pass the day someone
// stages a different cert.
//
//   0. gate  1. admin login  2. download the CSV  3. assert NO row is future-dated
//   4. CONTROL: the file is non-trivial and its dates parse (a 0-row or malformed file would
//      "pass" the blanket check while proving nothing)
//   5. Cross-check the two DEF-359 certs by number explicitly, for the record.
//
// Usage:
//   ADMIN_USER=admin.uat ADMIN_PASS='TestPass123!' BASE_URL=https://gccwebapp.com \
//     node Scripts/smoke-prod/def359-active-pid-export-effective-date.js
//
// Exit: 0 = clean · 1 = leaking future-dated identifiers · 2 = setup failure · 3 = inconclusive.

const { chromium, request } = require('playwright');
const { detectGateMode, passGate } = require('./_lib/test-env-gate');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const OUT = path.join(__dirname, '..', '..', 'docs', 'tracking', 'screenshots', 'def359-pid-export');
const REQ_TIMEOUT = Number(process.env.REQ_TIMEOUT_MS || 180000);

const log = [];
function say(m) { console.log(m); log.push(m); }
let pass = 0, fail = 0, ctrlFail = 0;
function check(n, ok, d) { if (ok) { pass++; say(`  PASS  ${n}${d ? '  — ' + d : ''}`); } else { fail++; say(`  FAIL  ${n}${d ? '  — ' + d : ''}`); } }
function control(n, ok, d) { if (ok) { pass++; say(`  CTRL  ${n}${d ? '  — ' + d : ''}`); } else { ctrlFail++; say(`  CTRL-FAIL  ${n}${d ? '  — ' + d : ''}`); } }

function businessToday() {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'America/New_York', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}

// Minimal RFC-4180 splitter — the export quotes fields containing commas, and product descriptions
// do contain them. Splitting on a bare comma silently shifts every later column.
function splitCsvLine(line) {
  const out = []; let cur = ''; let q = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (q) {
      if (ch === '"' && line[i + 1] === '"') { cur += '"'; i++; }
      else if (ch === '"') q = false;
      else cur += ch;
    } else if (ch === '"') q = true;
    else if (ch === ',') { out.push(cur); cur = ''; }
    else cur += ch;
  }
  out.push(cur);
  return out;
}

(async () => {
  const TODAY = businessToday();
  say(`DEF-359 Active Product Identifiers export — effective-date gate — ${BASE}`);
  say(`Business today (US Eastern): ${TODAY}`);
  say('');

  const browser = await chromium.launch();
  let gateState;
  const mode = await detectGateMode(request, BASE);
  if (mode.mode === 'cookie') {
    const gate = await passGate(request, BASE);
    if (!gate.ok) { say(`FATAL: gate — ${gate.detail}`); await browser.close(); process.exit(2); }
    gateState = gate.state; say(`  gate: passed cookie gate at ${BASE}`);
  } else { say(`  gate: ${mode.mode} (${mode.detail})`); }

  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, storageState: gateState });
  const page = await ctx.newPage();
  const receipt = { base: BASE, businessToday: TODAY, ranAt: new Date().toISOString() };

  try {
    const ADMIN_USER = process.env.ADMIN_USER || 'admin';
    const ADMIN_PASS = process.env.ADMIN_PASS || 'TestPass123!';
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(ADMIN_USER);
    await page.locator('#Password').fill(ADMIN_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(900);

    const probe = await ctx.request.get(`${BASE}/api/certificate-tiles-internal?pageSize=1`,
      { headers: { Accept: 'application/json' }, failOnStatusCode: false });
    if (probe.status() !== 200) { say(`FATAL: admin login failed as "${ADMIN_USER}" (HTTP ${probe.status()}). Use admin.uat on TEST.`); throw new Error('admin login'); }
    say(`  setup: signed in as ${ADMIN_USER}`);

    // ---- download the export -----------------------------------------------------------------
    const r = await ctx.request.get(`${BASE}/Export/DownloadActiveProductIds`,
      { timeout: REQ_TIMEOUT, failOnStatusCode: false });
    say(`  export: HTTP ${r.status()} (${(r.headers()['content-type'] || '?')})`);
    if (r.status() !== 200) {
      // DEF-360 turned a server-side timeout into HTTP 400 here, so a non-200 is worth naming
      // rather than treating as a visibility result.
      say('  FATAL: the export did not return 200 — this is an export failure, not a gate result.');
      throw new Error(`export HTTP ${r.status()}`);
    }
    const text = await r.text();
    fs.mkdirSync(OUT, { recursive: true });
    // The raw file is 4.2 MB of real customer product identifiers (ASIN/UPC/GTIN), company names
    // and partner columns. run.log and receipt.json carry every assertion and count, so the raw
    // CSV is NOT retained by default -- keeping it would commit live client data to git. Set
    // KEEP_CSV=1 to write it when a human needs to eyeball the file.
    if (process.env.KEEP_CSV === '1') {
      fs.writeFileSync(path.join(OUT, 'active-product-identifiers.csv'), text, 'utf8');
      say('  NOTE: KEEP_CSV=1 -- raw export written to disk; do NOT commit it.');
    }

    // 4-line preamble (title / generated / total / blank), then the header row.
    const lines = text.split(/\r?\n/).filter(l => l.length);
    const headerIdx = lines.findIndex(l => l.startsWith('Company,Certificate Number,'));
    if (headerIdx < 0) throw new Error('could not find the CSV header row');
    const cols = splitCsvLine(lines[headerIdx]);
    const iNum = cols.indexOf('Certificate Number');
    const iEff = cols.indexOf('Effective Date');
    const iStatus = cols.indexOf('Certificate Status');
    const iPartner = cols.indexOf('ID Partner');
    const dataRows = lines.slice(headerIdx + 1).map(splitCsvLine).filter(f => f.length >= cols.length - 2);

    const declaredTotal = /Total Active Product Identifiers:\s*(\d+)/.exec(text);
    say(`  export: ${dataRows.length} identifier rows parsed` +
        (declaredTotal ? ` (file declares ${declaredTotal[1]})` : ''));
    say('');

    // ---- controls: the file has to be worth checking ------------------------------------------
    say('CONTROLS — the export must actually contain parseable, dated rows:');
    control('export returned a non-trivial number of rows', dataRows.length > 100, `${dataRows.length} rows`);
    const dated = dataRows.filter(f => /^\d{4}-\d{2}-\d{2}$/.test((f[iEff] || '').trim()));
    control('every row carries a yyyy-MM-dd effective date', dated.length === dataRows.length,
      `${dated.length}/${dataRows.length} parseable`);
    if (declaredTotal) {
      control('parsed row count matches the count the file declares',
        Number(declaredTotal[1]) === dataRows.length, `${dataRows.length} vs ${declaredTotal[1]}`);
    }
    say('');

    // ---- the gate: blanket check over every row -----------------------------------------------
    say('GATE — no exported identifier may belong to a not-yet-effective certificate:');
    const leaking = dated.filter(f => f[iEff].trim() > TODAY);
    check('zero rows with a future effective date', leaking.length === 0,
      leaking.length ? `${leaking.length} rows leaking` : `all ${dated.length} rows are in force`);
    if (leaking.length) {
      const byCert = {};
      for (const f of leaking) {
        const k = `${f[iNum]}|${f[iEff]}|${f[iStatus]}|${f[iPartner] || '(no partner)'}`;
        byCert[k] = (byCert[k] || 0) + 1;
      }
      say('  Leaking certificates (cert | effective | status | ID Partner -> rows):');
      for (const [k, n] of Object.entries(byCert)) say(`    ${k}  ->  ${n} rows`);
    }
    say('');

    // ---- explicit cross-check on the two certs DEF-359 was raised against ---------------------
    say('CROSS-CHECK — the two certificates named in DEF-359 (12 + 2 rows when it was raised):');
    for (const num of ['26-0441', '26-0442']) {
      const hits = dataRows.filter(f => (f[iNum] || '').trim().endsWith(num.split('-')[1])
                                     && (f[iEff] || '').trim() === '2026-10-21');
      check(`${num} contributes 0 rows`, hits.length === 0, `${hits.length} rows`);
    }
    say('');

    receipt.rowCount = dataRows.length;
    receipt.leakingRows = leaking.length;
    receipt.leakingSample = leaking.slice(0, 20).map(f => ({ number: f[iNum], effectiveDate: f[iEff], status: f[iStatus], idPartner: f[iPartner] }));
    receipt.results = { pass, fail, controlFailures: ctrlFail };
  } catch (e) {
    say(`FATAL: ${e.message}`);
    fs.mkdirSync(OUT, { recursive: true });
    fs.writeFileSync(path.join(OUT, 'run.log'), log.join('\n'), 'utf8');
    await browser.close();
    process.exit(2);
  }

  await browser.close();
  fs.mkdirSync(OUT, { recursive: true });
  fs.writeFileSync(path.join(OUT, 'run.log'), log.join('\n'), 'utf8');
  fs.writeFileSync(path.join(OUT, 'receipt.json'), JSON.stringify(receipt, null, 2), 'utf8');

  say('─'.repeat(78));
  say(`  ${pass} passed, ${fail} failed, ${ctrlFail} control failures`);
  say(`  receipts: ${OUT}`);
  if (ctrlFail > 0) { say('  VERDICT: INCONCLUSIVE — controls failed, the blanket check proves nothing.'); process.exit(3); }
  if (fail > 0) { say('  VERDICT: LEAKING — the export is emitting identifiers for not-yet-effective certificates.'); process.exit(1); }
  say('  VERDICT: CLEAN — the export withholds identifiers for not-yet-effective certificates.');
  process.exit(0);
})();
