// DEF-358 / DEF-376 — LIVE proof that the Amazon partner API withholds certificates that have
// not yet reached their effective date.
//
// WHY THIS EXISTS. The client asked (QC, 8/27): "Anuki to confirm the API would work the same way"
// after confirming the public side was fixed. The existing coverage is
// `DEF358_EffectiveDateVisibilityTests` (25 cases) — but every one of those exercises the
// *predicate* (`CertificateVisibility`), not the controllers that call it. They would stay green if
// a controller stopped calling it entirely. That is the DEF-369 failure mode
// ([[feedback_test_below_the_defect_layer]]): 9 green unit tests while the controller was still
// wrong. This script tests the layer the client actually cares about — the deployed HTTP endpoint,
// authenticated exactly the way Amazon authenticates (X-API-Key, no app cookie).
//
// STRUCTURE — every negative has a positive control.
//   A "404 everywhere" result is worthless on its own: a broken key, a bad gate cookie or a typo'd
//   route produces exactly the same output as a working gate ([[feedback_role_gated_404_is_not_absence]]).
//   So each hidden-cert assertion is paired with the same call against a *currently-effective*
//   Amazon cert, which must return 200. If the controls fail, the run reports INCONCLUSIVE and
//   exits 3 rather than claiming the gate works.
//
//   0. pass the test-env cookie gate           (_lib/test-env-gate — never re-derive it, DEF-341)
//   1. admin login  -> ground truth from the INTERNAL tile API (admin sees everything, ungated)
//   2. mint a fresh Amazon partner API key
//   3. CONTROL   : currently-effective Amazon certs are visible over the partner API   (must PASS)
//   4. GATE      : future-effective Amazon certs are absent from list / by-number / GS1 / download
//   5. CROSSCHECK: search?effectiveDateFrom=<tomorrow> must return 0 rows — that filter and the
//                  DEF-358 lower bound are mutually exclusive iff the gate is live. Proven
//                  falsifiable by the same call with a past date, which must return rows.
//
// Usage:
//   TEST env : ADMIN_USER=admin.uat ADMIN_PASS=TestPass123! BASE_URL=https://gccwebapp.com \
//              node Scripts/smoke-prod/def358-api-effective-date.js
//   localhost: node Scripts/smoke-prod/def358-api-effective-date.js
//
// Exit codes: 0 = gate proven live · 1 = gate NOT enforced (real regression) · 2 = setup/gate
// failure · 3 = inconclusive (controls failed — proves nothing either way).

const { chromium, request } = require('playwright');
const { detectGateMode, passGate } = require('./_lib/test-env-gate');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const OUT = path.join(__dirname, '..', '..', 'docs', 'tracking', 'screenshots', 'def358-api');
const REQ_TIMEOUT = Number(process.env.REQ_TIMEOUT_MS || 180000);

// Source of truth: Models/CertificateStatus.cs -> CertificateStatusHelper.PublicStatuses.
// DEF-223 narrowed this to exactly these two; Confidential is deliberately NOT public (CR-043).
const PUBLIC_STATUSES = ['Client Approved - Paid', 'Republished'];

const log = [];
function say(m) { console.log(m); log.push(m); }

let pass = 0, fail = 0, ctrlFail = 0;
function check(name, ok, detail) {
  if (ok) { pass++; say(`  PASS  ${name}${detail ? '  — ' + detail : ''}`); }
  else { fail++; say(`  FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
}
function control(name, ok, detail) {
  if (ok) { pass++; say(`  CTRL  ${name}${detail ? '  — ' + detail : ''}`); }
  else { ctrlFail++; say(`  CTRL-FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
}

// Business "today" is US Eastern (CertificateStatusHelper.BusinessToday). Compare as yyyy-MM-dd
// strings so no timezone conversion can shift the calendar date under us — the exact class of bug
// the internal API's own `EffectiveDate.ToString("yyyy-MM-dd")` comment warns about.
function businessToday() {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'America/New_York', year: 'numeric', month: '2-digit', day: '2-digit',
  }).format(new Date());
}
function addDays(ymd, n) {
  const d = new Date(`${ymd}T12:00:00Z`);
  d.setUTCDate(d.getUTCDate() + n);
  return d.toISOString().slice(0, 10);
}

(async () => {
  const TODAY = businessToday();
  say(`DEF-358 partner-API effective-date gate — ${BASE}`);
  say(`Business today (US Eastern): ${TODAY}`);
  say('');

  const browser = await chromium.launch();
  let gateState;

  const mode = await detectGateMode(request, BASE);
  if (mode.mode === 'cookie') {
    const gate = await passGate(request, BASE);
    if (!gate.ok) {
      say(`FATAL: could not pass the test-env gate — ${gate.detail}`);
      say('This is a GATE failure, not a visibility finding. Set GATE_PASS if it rotated.');
      await browser.close();
      process.exit(2);
    }
    gateState = gate.state;
    say(`  gate: passed cookie gate at ${BASE}`);
  } else {
    say(`  gate: ${mode.mode} (${mode.detail})`);
  }

  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, storageState: gateState });
  const page = await ctx.newPage();
  // Cookie-free of the APP's auth = exactly how Amazon calls. It still carries the gate cookie,
  // which is a property of the test host, not of the API under test.
  const partnerReq = await request.newContext({ ignoreHTTPSErrors: true, storageState: gateState });

  const receipt = { base: BASE, businessToday: TODAY, ranAt: new Date().toISOString() };

  try {
    // ---- 1. admin login -------------------------------------------------------------------
    // `admin`'s password was reset on TEST on 2026-08-19 and only Shawn knows it — use admin.uat
    // there. A failed login does NOT announce itself; the page re-renders as HTTP 200 and the run
    // dies later at "got Amazon partner key", which reads like an API fault, not a credential one.
    const ADMIN_USER = process.env.ADMIN_USER || 'admin';
    const ADMIN_PASS = process.env.ADMIN_PASS || 'TestPass123!';
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(ADMIN_USER);
    await page.locator('#Password').fill(ADMIN_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(800);

    const whoami = await ctx.request.get(`${BASE}/api/certificate-tiles-internal?pageSize=1`,
      { headers: { Accept: 'application/json' }, failOnStatusCode: false });
    if (whoami.status() !== 200) {
      say(`FATAL: admin login failed as "${ADMIN_USER}" (internal API -> HTTP ${whoami.status()}).`);
      say('On the TEST env use ADMIN_USER=admin.uat ADMIN_PASS=TestPass123!');
      throw new Error('admin login failed');
    }
    say(`  setup: signed in as ${ADMIN_USER}`);

    // ---- 2. ground truth: every Amazon-tagged certificate, as seen by an ADMIN --------------
    // The internal grid is deliberately ungated (analysts stage certs on purpose), so it is the
    // right place to learn what SHOULD and SHOULD NOT reach Amazon.
    const truth = [];
    for (let page_ = 1; ; page_++) {
      const r = await ctx.request.get(
        `${BASE}/api/certificate-tiles-internal?partner=Amazon&page=${page_}&pageSize=500`,
        { headers: { Accept: 'application/json' }, timeout: REQ_TIMEOUT });
      const b = await r.json();
      truth.push(...(b.certificates || []));
      if (!b.hasMore) break;
      if (page_ > 40) break; // hard stop; 20k rows is far past anything real
    }
    say(`  truth: ${truth.length} Amazon-tagged certificates visible to admin`);

    const isPublic = (c) => PUBLIC_STATUSES.includes(c.status);
    const future = truth.filter(c => isPublic(c) && c.effectiveDate && c.effectiveDate > TODAY);
    const current = truth.filter(c => isPublic(c) && c.effectiveDate && c.effectiveDate <= TODAY
                                      && (!c.expirationDate || c.expirationDate >= TODAY));

    say(`  truth: ${current.length} currently-effective, ${future.length} NOT-YET-EFFECTIVE (public status)`);
    say('');

    // Enumerate the future set in full — never sample it ([[feedback_enumerate_dont_sample_populations]]).
    if (future.length) {
      say('  Not-yet-effective Amazon certificates (these must be invisible over the API):');
      for (const c of future) {
        say(`    ${c.certificateNumber}  id=${c.id}  ${c.status}  eff ${c.effectiveDate}  exp ${c.expirationDate}  ${c.companyName}`);
      }
      say('');
    }
    receipt.truth = {
      amazonTotal: truth.length,
      currentCount: current.length,
      future: future.map(c => ({ id: c.id, number: c.certificateNumber, status: c.status,
                                 effectiveDate: c.effectiveDate, expirationDate: c.expirationDate,
                                 company: c.companyName })),
    };

    // ---- 3. mint an Amazon partner API key --------------------------------------------------
    const plist = await ctx.request.get(`${BASE}/api/admin/partner/list`, { headers: { Accept: 'application/json' } });
    const amazon = ((await plist.json()).data || []).find(p => p.name === 'Amazon');
    if (!amazon) throw new Error('no Amazon partner row found');
    const gen = await ctx.request.post(`${BASE}/api/admin/partner/${amazon.id}/apikey`, {
      headers: { 'Content-Type': 'application/json' }, data: { description: 'def358-api-effective-date' } });
    const key = (await gen.json()).data.apiKey;
    if (!key || !key.startsWith('gcc_partner_')) throw new Error('failed to mint an Amazon partner key');
    say(`  setup: minted Amazon partner key ${key.slice(0, 20)}…`);
    say('');

    const api = async (url) => {
      const r = await partnerReq.get(`${BASE}${url}`, {
        headers: { 'X-API-Key': key, Accept: 'application/json' },
        timeout: REQ_TIMEOUT, failOnStatusCode: false,
      });
      let body = null; try { body = await r.json(); } catch (e) {}
      return { status: r.status(), body };
    };

    // Walk the whole partner feed once — this is the list Amazon actually consumes.
    const feed = [];
    for (let p = 1; ; p++) {
      const r = await api(`/api/v1/certificates?pageNumber=${p}&pageSize=100`);
      if (r.status !== 200) throw new Error(`partner feed page ${p} -> HTTP ${r.status}`);
      const rows = r.body?.data || [];
      feed.push(...rows);
      const total = r.body?.pagination?.totalCount ?? r.body?.totalCount;
      if (!rows.length || (total != null && feed.length >= total) || p > 100) break;
    }
    const feedNumbers = new Set(feed.map(c => c.certificateNumber || c.number).filter(Boolean));
    say(`  feed: Amazon partner API returned ${feed.length} certificates`);
    say('');

    // ---- 4. CONTROLS: currently-effective certs MUST be reachable ---------------------------
    say('CONTROLS — a currently-effective Amazon cert must be visible (proves the key + routes work):');
    const controls = current.slice(0, 3);
    if (!controls.length) {
      say('  (none available — cannot establish a positive control)');
      ctrlFail++;
    }
    for (const c of controls) {
      control(`${c.certificateNumber} present in partner feed`, feedNumbers.has(c.certificateNumber));
      const byNum = await api(`/api/v1/certificates/number/${encodeURIComponent(c.certificateNumber)}`);
      control(`${c.certificateNumber} by-number -> 200`, byNum.status === 200, `HTTP ${byNum.status}`);
    }
    say('');

    // ---- 5. THE GATE: future-effective certs must be absent everywhere ----------------------
    say('GATE — not-yet-effective Amazon certs must be withheld on every partner read endpoint:');
    if (!future.length) {
      say('  NOTE: no Amazon-tagged cert currently has a future effective date, so the per-cert');
      say('  assertions below have nothing to bite on. The cross-check in section 6 still applies.');
    }
    for (const c of future) {
      check(`${c.certificateNumber} ABSENT from partner feed`, !feedNumbers.has(c.certificateNumber),
        `eff ${c.effectiveDate}`);

      const byNum = await api(`/api/v1/certificates/number/${encodeURIComponent(c.certificateNumber)}`);
      check(`${c.certificateNumber} by-number -> 404`, byNum.status === 404, `HTTP ${byNum.status}`);

      const byId = await api(`/api/v1/certificates/${c.id}`);
      check(`${c.certificateNumber} by-id -> 404`, byId.status === 404, `HTTP ${byId.status}`);

      const gs1 = await api(`/api/v1/certificates/gs1/${encodeURIComponent(c.certificateNumber)}`);
      check(`${c.certificateNumber} GS1 -> 404`, gs1.status === 404, `HTTP ${gs1.status}`);

      const dl = await api(`/api/v1/certificates/${c.id}/download`);
      check(`${c.certificateNumber} download refused`, dl.status === 404 || dl.status === 400,
        `HTTP ${dl.status}`);
    }
    say('');

    // ---- 6. CROSS-CHECK: the search filter and the DEF-358 bound are mutually exclusive ------
    // This holds regardless of what data happens to exist, which is why it is worth having: it
    // cannot silently pass by there being nothing to find.
    say('CROSS-CHECK — search(effectiveDateFrom=tomorrow) and the DEF-358 lower bound cannot both match:');
    const tomorrow = addDays(TODAY, 1);
    const futureSearch = await api(`/api/v1/certificates/search?effectiveDateFrom=${tomorrow}&pageSize=100`);
    const futureRows = futureSearch.body?.data || [];
    check(`search(effectiveDateFrom=${tomorrow}) returns 0 rows`,
      futureSearch.status === 200 && futureRows.length === 0,
      `HTTP ${futureSearch.status}, ${futureRows.length} rows`);

    // Falsifiability: prove that filter can return rows at all, or the 0 above means nothing.
    const pastSearch = await api(`/api/v1/certificates/search?effectiveDateFrom=2000-01-01&pageSize=100`);
    const pastRows = pastSearch.body?.data || [];
    control(`search(effectiveDateFrom=2000-01-01) returns rows (filter is live)`,
      pastSearch.status === 200 && pastRows.length > 0,
      `HTTP ${pastSearch.status}, ${pastRows.length} rows`);
    say('');

    receipt.feedCount = feed.length;
    receipt.results = { pass, fail, controlFailures: ctrlFail };
  } catch (e) {
    say(`FATAL: ${e.message}`);
    receipt.fatal = e.message;
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
  if (ctrlFail > 0) {
    say('  VERDICT: INCONCLUSIVE — the positive controls failed, so the negatives prove nothing.');
    process.exit(3);
  }
  if (fail > 0) {
    say('  VERDICT: GATE NOT ENFORCED — the partner API is exposing not-yet-effective certificates.');
    process.exit(1);
  }
  say('  VERDICT: GATE ENFORCED — the Amazon partner API withholds not-yet-effective certificates.');
  process.exit(0);
})();
