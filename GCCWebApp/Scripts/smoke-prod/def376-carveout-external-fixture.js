// DEF-376 carve-out — prove that an External (company) user STILL sees its own certificates that
// have not yet reached their effective date.
//
// WHY THIS EXISTS. DEF-376 added the not-yet-effective gate to the Partner branch of
// CertificateTileExternalApiController. The risk in that fix is OVER-correction: gating the whole
// controller would silently stop a certifying company seeing its own upcoming certificates, which
// is behaviour the client explicitly asked for ("a company needs to know its cert is queued").
// Proving Amazon can't see them says nothing about that — it is the opposite assertion on the same
// code path, and only one of the two branches was supposed to change.
//
// It could not be run on 2026-09-02 because no test account owned a future-dated certificate: the
// whole test environment holds just three (26-0441/26-0442, X-SENSE, eff 2026-10-21; and 26-0330,
// DuPont, eff 2026-09-11), while test.external is Sika and test.external2 is MAPEI. This script
// creates the missing fixture and then makes the assertion.
//
// The fixture is created through /Admin/CreateUser, NOT by SQL, so it goes through the same
// validation, hashing and audit path as a real account — the same choice made for the six
// role-matrix accounts on 2026-08-20.
//
// >> SHELF LIFE. This proves the carve-out only while X-SENSE still has a FUTURE-dated cert. On
// >> 2026-10-21 those two go into force and this script's assertion silently becomes vacuous — it
// >> will report SKIPPED, not PASS, so it cannot quietly rot into a green light. See the review
// >> to-do in docs/tracking/defect-register.md (DEF-376) and observations_log.md.
//
// Usage:
//   ADMIN_USER=admin.uat ADMIN_PASS='TestPass123!' BASE_URL=https://gccwebapp.com \
//     node Scripts/smoke-prod/def376-carveout-external-fixture.js
//
// Exit: 0 = carve-out proven intact · 1 = carve-out BROKEN (over-corrected) · 2 = setup failure
//       · 3 = inconclusive / no future-dated cert left to assert on.

const { chromium, request } = require('playwright');
const { detectGateMode, passGate } = require('./_lib/test-env-gate');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const OUT = path.join(__dirname, '..', '..', 'docs', 'tracking', 'screenshots', 'def376-carveout');
const REQ_TIMEOUT = Number(process.env.REQ_TIMEOUT_MS || 180000);
const PUBLIC_STATUSES = ['Client Approved - Paid', 'Republished'];

// The company must be one that actually has a staged certificate. X-SENSE is chosen over DuPont
// because its certs are effective 2026-10-21 rather than 2026-09-11 — a longer runway before the
// fixture stops being able to prove anything.
const TARGET_COMPANY = process.env.CARVEOUT_COMPANY || 'X-SENSE';
const FIXTURE = {
  name: 'Test XSense External',
  email: process.env.CARVEOUT_EMAIL || 'shawnmwest+xsense@gmail.com',
  password: process.env.CARVEOUT_PASS || 'GccTest2026!',
};

const log = [];
function say(m) { console.log(m); log.push(m); }
let pass = 0, fail = 0, ctrlFail = 0;
function check(n, ok, d) { if (ok) { pass++; say(`  PASS  ${n}${d ? '  — ' + d : ''}`); } else { fail++; say(`  FAIL  ${n}${d ? '  — ' + d : ''}`); } }
function control(n, ok, d) { if (ok) { pass++; say(`  CTRL  ${n}${d ? '  — ' + d : ''}`); } else { ctrlFail++; say(`  CTRL-FAIL  ${n}${d ? '  — ' + d : ''}`); } }

function businessToday() {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'America/New_York', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}

async function signIn(browser, gateState, creds, probeUrl) {
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, storageState: gateState });
  const page = await ctx.newPage();
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill(creds.user);
  await page.locator('#Password').fill(creds.pass);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(900);
  const probe = await ctx.request.get(`${BASE}${probeUrl}`, { headers: { Accept: 'application/json' }, failOnStatusCode: false });
  return { ctx, page, ok: probe.status() === 200, status: probe.status() };
}

async function walkTiles(ctx) {
  const rows = [];
  for (let p = 1; ; p++) {
    const r = await ctx.request.get(`${BASE}/api/CertificateTileExternal?page=${p}&pageSize=100`,
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
  say(`DEF-376 carve-out — External/company still sees its own upcoming certs — ${BASE}`);
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

  const receipt = { base: BASE, businessToday: TODAY, ranAt: new Date().toISOString(), company: TARGET_COMPANY };

  try {
    // ---- 1. admin + ground truth -------------------------------------------------------------
    const admin = await signIn(browser, gateState,
      { user: process.env.ADMIN_USER || 'admin', pass: process.env.ADMIN_PASS || 'TestPass123!' },
      '/api/certificate-tiles-internal?pageSize=1');
    if (!admin.ok) { say(`FATAL: admin login failed (HTTP ${admin.status}). Use ADMIN_USER=admin.uat on TEST.`); throw new Error('admin login'); }
    say('  setup: signed in as admin');

    const all = [];
    for (let p = 1; ; p++) {
      const r = await admin.ctx.request.get(`${BASE}/api/certificate-tiles-internal?page=${p}&pageSize=500`,
        { headers: { Accept: 'application/json' }, timeout: REQ_TIMEOUT });
      const b = await r.json();
      all.push(...(b.certificates || []));
      if (!b.hasMore || p > 40) break;
    }
    const mine = all.filter(c => (c.companyName || '').startsWith(TARGET_COMPANY));
    const mineFuture = mine.filter(c => PUBLIC_STATUSES.includes(c.status) && c.effectiveDate && c.effectiveDate > TODAY);
    say(`  truth: ${TARGET_COMPANY} owns ${mine.length} certificates, ${mineFuture.length} of them not yet effective`);
    for (const c of mineFuture) say(`    ${c.certificateNumber}  eff ${c.effectiveDate}  ${c.status}`);
    say('');
    receipt.truth = { companyCerts: mine.length, futureCerts: mineFuture.map(c => ({ number: c.certificateNumber, effectiveDate: c.effectiveDate })) };

    // The whole point of the fixture is to assert on a FUTURE-dated cert. If there is no longer
    // one, say so and exit 3 — never report a vacuous pass.
    if (!mineFuture.length) {
      say(`  SKIPPED: ${TARGET_COMPANY} has no future-dated certificate any more, so the carve-out`);
      say('  cannot be asserted. This is the expected state after 2026-10-21 — pick another company');
      say('  with staged certs via CARVEOUT_COMPANY, or retire this fixture. NOT a pass, NOT a failure.');
      fs.mkdirSync(OUT, { recursive: true });
      fs.writeFileSync(path.join(OUT, 'run.log'), log.join('\n'), 'utf8');
      await browser.close();
      process.exit(3);
    }

    // ---- 2. create the fixture account -------------------------------------------------------
    const cRes = await admin.ctx.request.get(`${BASE}/Admin/GetCompanies`, { headers: { Accept: 'application/json' } });
    const companies = (await cRes.json()).data || [];
    const company = companies.find(c => (c.name || c.Name || '').startsWith(TARGET_COMPANY));
    if (!company) throw new Error(`no company matching "${TARGET_COMPANY}" in /Admin/GetCompanies`);
    const companyId = company.id ?? company.Id;
    say(`  fixture: ${TARGET_COMPANY} resolves to company id ${companyId}`);

    // /Admin/CreateUser is [ValidateAntiForgeryToken] with a [FromBody] model, so the token travels
    // in the RequestVerificationToken HEADER, not the JSON body.
    await admin.page.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'domcontentloaded' });
    const html = await admin.page.content();
    const tok = /name="__RequestVerificationToken"[^>]*value="([^"]+)"/.exec(html);
    if (!tok) throw new Error('no __RequestVerificationToken on /Admin/UserManagement');

    const create = await admin.ctx.request.post(`${BASE}/Admin/CreateUser`, {
      headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': tok[1] },
      data: {
        Name: FIXTURE.name,
        Email: FIXTURE.email,
        Role: 'External',
        Password: FIXTURE.password,
        CompanyIds: [companyId],
        PartnerIds: [],
        // Must be false: AdminService sets RequirePasswordReset = model.RequirePasswordReset ||
        // !passwordWasProvided, and a forced reset would bounce every programmatic login.
        RequirePasswordReset: false,
      },
      failOnStatusCode: false,
    });
    let createBody = null; try { createBody = await create.json(); } catch (e) {}
    const already = JSON.stringify(createBody || '').match(/exist|already|duplicate/i);
    if (createBody?.success) say(`  fixture: created ${FIXTURE.email} (External, scoped to ${TARGET_COMPANY})`);
    else if (already) say(`  fixture: ${FIXTURE.email} already exists — reusing it`);
    else throw new Error(`CreateUser failed: HTTP ${create.status()} ${JSON.stringify(createBody)}`);
    receipt.fixture = { email: FIXTURE.email, companyId, created: !!createBody?.success };
    say('');

    // ---- 3. the assertion --------------------------------------------------------------------
    const ext = await signIn(browser, gateState, { user: FIXTURE.email, pass: FIXTURE.password },
      '/api/CertificateTileExternal?pageSize=1');
    if (!ext.ok) { say(`FATAL: fixture login failed (HTTP ${ext.status}).`); throw new Error('fixture login'); }
    say(`  setup: signed in as ${FIXTURE.email} (External / ${TARGET_COMPANY})`);

    const tiles = await walkTiles(ext.ctx);
    const nums = new Set(tiles.map(c => c.certificateNumber));
    const seenFuture = tiles.filter(c => c.effectiveDate && c.effectiveDate > TODAY);
    say(`  tile view returned ${tiles.length} certificates, ${seenFuture.length} not yet effective`);
    say('');

    say('CONTROLS — the fixture must be correctly scoped, or the assertion means nothing:');
    control('sees a non-zero number of certificates', tiles.length > 0, `${tiles.length}`);
    const foreign = tiles.filter(c => !(c.companyName || '').startsWith(TARGET_COMPANY));
    control(`sees ONLY ${TARGET_COMPANY} certificates`, foreign.length === 0,
      foreign.length ? `${foreign.length} foreign rows, e.g. ${foreign[0].companyName}` : `all ${tiles.length} rows`);
    say('');

    say('CARVE-OUT — the company must STILL see its own not-yet-effective certificates:');
    for (const c of mineFuture) {
      check(`${c.certificateNumber} (eff ${c.effectiveDate}) IS visible to its owning company`,
        nums.has(c.certificateNumber));
    }
    check('company sees at least one upcoming certificate', seenFuture.length > 0, `${seenFuture.length}`);
    say('');

    receipt.results = { tileCount: tiles.length, futureVisible: seenFuture.length, pass, fail, controlFailures: ctrlFail };
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
  if (ctrlFail > 0) { say('  VERDICT: INCONCLUSIVE — the fixture is not correctly scoped.'); process.exit(3); }
  if (fail > 0) { say('  VERDICT: CARVE-OUT BROKEN — a company can no longer see its own upcoming certificates.'); process.exit(1); }
  say('  VERDICT: CARVE-OUT INTACT — the company still sees its own upcoming certificates.');
  process.exit(0);
})();
