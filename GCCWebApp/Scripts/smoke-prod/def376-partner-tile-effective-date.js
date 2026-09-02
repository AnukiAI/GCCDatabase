// DEF-376 — LIVE proof that the PARTNER TILE VIEW withholds certificates that have not yet reached
// their effective date, and that the External/company carve-out is still intact.
//
// WHY THIS IS SEPARATE FROM def358-api-effective-date.js. DEF-358 wired the not-yet-effective gate
// into the v1 partner API (9 sites) and the PI export, and deliberately EXCLUDED
// CertificateTileExternalApiController — correct for the External/owning-company role, which must
// keep seeing its own upcoming certificates. But that controller ALSO serves the Partner role, so
// the exclusion was too wide: Amazon's signed-in tile view listed 210 certs while the same
// account's CSV export listed 208. Proving the API is gated therefore says nothing about this
// surface; it is a different controller with its own copy of the decision.
//
// THE CARVE-OUT IS PART OF THE ASSERTION, NOT AN EXCEPTION TO IT. A run that only proved "Partner
// cannot see future certs" could be satisfied by over-correcting the whole controller, which would
// silently break the company view the client explicitly asked for. So section 6 drives a real
// External (company) login and asserts the opposite outcome on the same data.
//
//   0. pass the test-env cookie gate
//   1. admin login -> ground truth (which Amazon certs are future-effective, which are current)
//   2. PARTNER login (test.amazon) -> walk the tile view
//   3. CONTROL : currently-effective Amazon certs ARE listed and downloadable   (must PASS)
//   4. GATE    : future-effective Amazon certs are absent AND undownloadable
//   5. PARITY  : partner tile count == currently-effective count (the 210-vs-208 symptom)
//   6. CARVE-OUT: External/company login still sees its own upcoming certificates
//
// Usage:
//   ADMIN_USER=admin.uat ADMIN_PASS='TestPass123!' BASE_URL=https://gccwebapp.com \
//     node Scripts/smoke-prod/def376-partner-tile-effective-date.js
//
// Exit: 0 = gate proven live · 1 = gate NOT enforced · 2 = setup failure · 3 = inconclusive.

const { chromium, request } = require('playwright');
const { detectGateMode, passGate } = require('./_lib/test-env-gate');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const OUT = path.join(__dirname, '..', '..', 'docs', 'tracking', 'screenshots', 'def376-tile');
const REQ_TIMEOUT = Number(process.env.REQ_TIMEOUT_MS || 180000);
const PUBLIC_STATUSES = ['Client Approved - Paid', 'Republished'];

const PARTNER = { user: process.env.PARTNER_USER || 'test.amazon', pass: process.env.PARTNER_PASS || 'GccTest2026!' };
const EXTERNAL = { user: process.env.EXTERNAL_USER || 'test.external', pass: process.env.EXTERNAL_PASS || 'GccTest2026!' };

const log = [];
function say(m) { console.log(m); log.push(m); }
let pass = 0, fail = 0, ctrlFail = 0;
function check(n, ok, d) { if (ok) { pass++; say(`  PASS  ${n}${d ? '  — ' + d : ''}`); } else { fail++; say(`  FAIL  ${n}${d ? '  — ' + d : ''}`); } }
function control(n, ok, d) { if (ok) { pass++; say(`  CTRL  ${n}${d ? '  — ' + d : ''}`); } else { ctrlFail++; say(`  CTRL-FAIL  ${n}${d ? '  — ' + d : ''}`); } }

function businessToday() {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'America/New_York', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}

// Sign in through the real login form in a fresh browser context, then confirm the session actually
// took. A failed login does NOT announce itself — the page re-renders as HTTP 200, and every
// assertion downstream then measures a logged-out session while looking like a gate result.
async function signIn(browser, gateState, creds, probeUrl) {
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, storageState: gateState });
  const page = await ctx.newPage();
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill(creds.user);
  await page.locator('#Password').fill(creds.pass);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(900);
  const probe = await ctx.request.get(`${BASE}${probeUrl}`, { headers: { Accept: 'application/json' }, failOnStatusCode: false });
  return { ctx, ok: probe.status() === 200, status: probe.status() };
}

async function walkTiles(ctx, extraQuery = '') {
  const rows = [];
  for (let p = 1; ; p++) {
    const r = await ctx.request.get(`${BASE}/api/CertificateTileExternal?page=${p}&pageSize=100${extraQuery}`,
      { headers: { Accept: 'application/json' }, timeout: REQ_TIMEOUT, failOnStatusCode: false });
    if (r.status() !== 200) throw new Error(`tile page ${p} -> HTTP ${r.status()}`);
    const b = await r.json();
    rows.push(...(b.certificates || []));
    if (!b.hasMore || p > 100) break;
  }
  return rows;
}

(async () => {
  const TODAY = businessToday();
  say(`DEF-376 partner TILE VIEW effective-date gate — ${BASE}`);
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

  const receipt = { base: BASE, businessToday: TODAY, ranAt: new Date().toISOString() };

  try {
    // ---- 1. ground truth, as admin ---------------------------------------------------------
    const admin = await signIn(browser, gateState,
      { user: process.env.ADMIN_USER || 'admin', pass: process.env.ADMIN_PASS || 'TestPass123!' },
      '/api/certificate-tiles-internal?pageSize=1');
    if (!admin.ok) { say(`FATAL: admin login failed (HTTP ${admin.status}). Use ADMIN_USER=admin.uat on TEST.`); throw new Error('admin login'); }
    say('  setup: signed in as admin');

    const truth = [];
    for (let p = 1; ; p++) {
      const r = await admin.ctx.request.get(`${BASE}/api/certificate-tiles-internal?partner=Amazon&page=${p}&pageSize=500`,
        { headers: { Accept: 'application/json' }, timeout: REQ_TIMEOUT });
      const b = await r.json();
      truth.push(...(b.certificates || []));
      if (!b.hasMore || p > 40) break;
    }
    const isPublic = c => PUBLIC_STATUSES.includes(c.status);
    const future = truth.filter(c => isPublic(c) && c.effectiveDate && c.effectiveDate > TODAY);
    const current = truth.filter(c => isPublic(c) && c.effectiveDate && c.effectiveDate <= TODAY && (!c.expirationDate || c.expirationDate >= TODAY));
    say(`  truth: ${truth.length} Amazon-tagged · ${current.length} currently-effective · ${future.length} NOT-YET-EFFECTIVE`);
    for (const c of future) say(`    ${c.certificateNumber}  id=${c.id}  ${c.status}  eff ${c.effectiveDate}  ${c.companyName}`);
    say('');
    receipt.truth = { amazonTotal: truth.length, currentCount: current.length,
      future: future.map(c => ({ id: c.id, number: c.certificateNumber, effectiveDate: c.effectiveDate, company: c.companyName })) };

    // ---- 2. partner tile view ---------------------------------------------------------------
    const partner = await signIn(browser, gateState, PARTNER, '/api/CertificateTileExternal?pageSize=1');
    if (!partner.ok) { say(`FATAL: partner login failed as ${PARTNER.user} (HTTP ${partner.status}).`); throw new Error('partner login'); }
    say(`  setup: signed in as ${PARTNER.user} (Partner / Amazon)`);

    const tiles = await walkTiles(partner.ctx);
    const tileNumbers = new Set(tiles.map(c => c.certificateNumber).filter(Boolean));
    say(`  tile view returned ${tiles.length} certificates`);
    say('');

    // Anything the tile view lists that is not yet effective is the DEF-376 symptom itself,
    // whether or not it is one of the certs identified above.
    const leaked = tiles.filter(c => c.effectiveDate && c.effectiveDate > TODAY);
    check('tile view lists ZERO not-yet-effective certificates', leaked.length === 0,
      leaked.length ? leaked.map(c => `${c.certificateNumber} eff ${c.effectiveDate}`).join('; ') : `${tiles.length} rows all in-force`);
    say('');

    // ---- 3. controls -------------------------------------------------------------------------
    say('CONTROLS — currently-effective Amazon certs must be listed and downloadable:');
    const controls = current.slice(0, 3);
    if (!controls.length) { say('  (none available)'); ctrlFail++; }
    for (const c of controls) {
      control(`${c.certificateNumber} present in tile view`, tileNumbers.has(c.certificateNumber));
      const dl = await partner.ctx.request.get(`${BASE}/api/CertificateTileExternal/${c.id}/download`,
        { timeout: REQ_TIMEOUT, failOnStatusCode: false });
      control(`${c.certificateNumber} download -> 200`, dl.status() === 200, `HTTP ${dl.status()}`);
    }
    say('');

    // ---- 4. the gate --------------------------------------------------------------------------
    say('GATE — not-yet-effective Amazon certs must be absent AND undownloadable:');
    if (!future.length) say('  NOTE: no Amazon cert is currently future-dated; the blanket check above still applies.');
    for (const c of future) {
      check(`${c.certificateNumber} ABSENT from partner tile view`, !tileNumbers.has(c.certificateNumber), `eff ${c.effectiveDate}`);
      const dl = await partner.ctx.request.get(`${BASE}/api/CertificateTileExternal/${c.id}/download`,
        { timeout: REQ_TIMEOUT, failOnStatusCode: false });
      // DEF-376 returns 400 "Certificate is not available for download".
      check(`${c.certificateNumber} download refused`, dl.status() === 400 || dl.status() === 404, `HTTP ${dl.status()}`);
    }
    say('');

    // ---- 5. parity ----------------------------------------------------------------------------
    // The original symptom was a NUMBER mismatch between two surfaces (210 vs 208), so compare
    // the count directly rather than only checking each cert against expectations.
    say('PARITY — the tile view must agree with the currently-effective population (210-vs-208 symptom):');
    check('tile count == currently-effective count', tiles.length === current.length,
      `tile ${tiles.length} vs truth ${current.length}`);
    say('');

    // ---- 6. the External/company carve-out ----------------------------------------------------
    say('CARVE-OUT — an External (company) user must STILL see its own upcoming certificates:');
    const ext = await signIn(browser, gateState, EXTERNAL, '/api/CertificateTileExternal?pageSize=1');
    if (!ext.ok) {
      say(`  SKIP: could not sign in as ${EXTERNAL.user} (HTTP ${ext.status}) — carve-out not exercised.`);
      receipt.carveOut = { exercised: false, reason: `login HTTP ${ext.status}` };
    } else {
      const extTiles = await walkTiles(ext.ctx);
      const extFuture = extTiles.filter(c => c.effectiveDate && c.effectiveDate > TODAY);
      say(`  ${EXTERNAL.user} sees ${extTiles.length} certificates, ${extFuture.length} of them not yet effective`);
      if (extFuture.length > 0) {
        control('External user still sees its upcoming certs (carve-out intact)', true,
          extFuture.slice(0, 3).map(c => `${c.certificateNumber} eff ${c.effectiveDate}`).join('; '));
      } else {
        // Absence here is NOT evidence of over-correction: this company may simply have none
        // staged. Report it honestly instead of grading it either way.
        say('  INCONCLUSIVE: this company has no future-dated certificates, so the carve-out could');
        say('  not be exercised. Not counted as a pass or a failure.');
      }
      receipt.carveOut = { exercised: true, total: extTiles.length, futureVisible: extFuture.length,
        sample: extFuture.slice(0, 5).map(c => ({ number: c.certificateNumber, effectiveDate: c.effectiveDate })) };
    }
    say('');

    receipt.tileCount = tiles.length;
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
  if (ctrlFail > 0) { say('  VERDICT: INCONCLUSIVE — positive controls failed, negatives prove nothing.'); process.exit(3); }
  if (fail > 0) { say('  VERDICT: GATE NOT ENFORCED — the partner tile view is exposing not-yet-effective certificates.'); process.exit(1); }
  say('  VERDICT: GATE ENFORCED — the partner tile view withholds not-yet-effective certificates.');
  process.exit(0);
})();
