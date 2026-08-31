// Shared infrastructure for the full-regression run against the TEST environment.
//
// WHY THIS EXISTS SEPARATELY FROM _lib/smoke-template.js
// smoke-template.js predates two facts about gccwebapp.com:
//   1. the test-env gate is Cookie mode, not Basic Auth (it still sends httpCredentials, which
//      silently yields a 200 gate page for every URL — see _lib/test-env-gate.js), and
//   2. the role-matrix accounts on TEST are test.admin / test.analyst / test.external /
//      test.external2 / test.amazon / test.novi, not the analyst/external/admin trio it hardcodes.
// Rather than mutate a template a dozen shipped smokes depend on, the regression run gets its own
// entry point that composes test-env-gate.js correctly and knows the real accounts.

const { chromium, request: pwRequest } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const HEADLESS = process.env.REG_HEADLESS !== '0';
const NAV_TIMEOUT = parseInt(process.env.REG_NAV_TIMEOUT || '60000', 10);

// Gate password. This is the LIVE value on gccwebapp.com and matches what the other 92 files under
// Scripts/ already use (including _lib/test-env-gate.js DEFAULT_PASS) — one shared literal, so a
// future rotation is a single find-and-replace rather than a hunt.
//
// A rotation to 'GreenCircle-Test-2026' was drafted on 2026-08-17 but was never applied, to either
// the deployed config or the scripts: it returns 401 today while this value succeeds. Deliberately
// NOT rotating — UAT users are mid-test (Shawn, 2026-08-26). When the rotation does happen it needs
// an app-pool recycle as well as the config edit, because TestEnvironmentGateMiddleware binds its
// options once in the constructor and is a singleton.
const GATE_CANDIDATES = [process.env.GATE_PASS || 'Cert-Test-Access-2026!'];
let GATE_PASS_WORKING = null;

const RESULTS_DIR = path.join(__dirname, '..', '_results');
const ARTIFACT_DIR = path.join(__dirname, '..', '_artifacts');

// Role-matrix accounts — CLAUDE.md "Quick Credentials", TEST environment only.
// The pairs matter: external/external2 sit on different companies and amazon/novi on different
// partners, because a scoped account can only prove it SEES something, never that it is correctly
// DENIED someone else's data. Cross-checks below rely on both halves of each pair.
const TEST_PASSWORD = process.env.REG_PASSWORD || 'GccTest2026!';
const ACCOUNTS = {
  admin:     { user: 'test.admin',     role: 'Admin',    scope: null,                 lands: '/Certificate/CertificateTileViewInternal' },
  analyst:   { user: 'test.analyst',   role: 'Analyst',  scope: null,                 lands: '/Certificate/CertificateTileViewInternal' },
  external:  { user: 'test.external',  role: 'External', scope: 'Sika Corporation',   lands: '/Certificate/CertificateTileViewExternal' },
  external2: { user: 'test.external2', role: 'External', scope: 'MAPEI',              lands: '/Certificate/CertificateTileViewExternal' },
  amazon:    { user: 'test.amazon',    role: 'Partner',  scope: 'Amazon',             lands: '/Certificate/CertificateTileViewExternal' },
  novi:      { user: 'test.novi',      role: 'Partner',  scope: 'Novi',               lands: '/Certificate/CertificateTileViewExternal' },
};

function ensureDirs() {
  for (const d of [RESULTS_DIR, ARTIFACT_DIR]) {
    if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true });
  }
}

// ---------------------------------------------------------------------------
// Test-environment gate
// ---------------------------------------------------------------------------

// Ask the site how it is gated WITHOUT following redirects. 'open' would be a finding in its own
// right on a publicly routable test env, not a convenience.
async function detectGate() {
  const anon = await pwRequest.newContext({ ignoreHTTPSErrors: true });
  try {
    const r = await anon.get(`${BASE}/`, { maxRedirects: 0, failOnStatusCode: false });
    const status = r.status();
    const loc = r.headers()['location'] || '';
    const auth = r.headers()['www-authenticate'] || '';
    if (status === 401 && /basic/i.test(auth)) return { mode: 'basic', detail: `401 ${auth}` };
    if (status >= 300 && status < 400 && loc.startsWith('/__gate')) return { mode: 'cookie', detail: `${status} -> ${loc}` };
    if (status === 200) return { mode: 'open', detail: 'no gate — site publicly browsable' };
    return { mode: 'unknown', detail: `status=${status} location=${loc || 'none'}` };
  } finally {
    await anon.dispose();
  }
}

// Pass the cookie gate inside a live browser context. The CSRF formToken is minted with the form
// and must be fresh; posting the password alone returns 400 "Your session expired", NOT 401 —
// which reads like a wrong password and sends you looking in the wrong place.
async function passGate(page) {
  // Once one candidate is known good, stop probing — a wrong guess costs a full round trip and
  // there are six sessions per phase.
  const candidates = GATE_PASS_WORKING ? [GATE_PASS_WORKING] : GATE_CANDIDATES;
  let last = '';
  for (const pw of candidates) {
    await page.goto(`${BASE}/__gate?returnUrl=%2F`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
    if (!(await page.locator('input[name="password"]').count())) {
      return { ok: true, detail: 'no gate form present (already through, or gate disabled)' };
    }
    await page.locator('input[name="password"]').fill(pw);
    await page.locator('form button[type="submit"], form input[type="submit"]').first().click();
    await page.waitForLoadState('domcontentloaded');
    if ((await page.context().cookies()).some((c) => c.name === 'GCC.TestGate')) {
      GATE_PASS_WORKING = pw;
      return { ok: true, detail: `GCC.TestGate issued (password: ${pw})`, password: pw };
    }
    last = `rejected at ${page.url()}`;
  }
  return { ok: false, detail: `no candidate password accepted; ${last}` };
}

function workingGatePassword() { return GATE_PASS_WORKING; }

// ---------------------------------------------------------------------------
// Application login
// ---------------------------------------------------------------------------

// The login field is named `Email` but accepts a username OR an email
// (LoginViewModel.Email is labelled "Email or Username"). Posting `Username=` silently re-renders
// the login page as HTTP 200, which reads like a bad password rather than a wrong field name.
async function login(page, accountKey) {
  const acct = ACCOUNTS[accountKey];
  if (!acct) throw new Error(`unknown account ${accountKey}`);
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
  await page.locator('#Email').fill(acct.user);
  await page.locator('#Password').fill(TEST_PASSWORD);
  await page.locator('button[type="submit"]').first().click();
  await page.waitForLoadState('domcontentloaded');
  await page.waitForTimeout(700);
  // Confirm by asking for a page that bounces anonymous callers, rather than trusting the 302.
  const landed = page.url().replace(BASE, '');
  const ok = !/\/Account\/Login/i.test(landed);
  return { ok, landed, account: acct };
}

// One browser context that is through the gate AND logged in as `accountKey`.
// Pass accountKey = null for an anonymous-but-gated context (public-route testing).
async function session(browser, accountKey) {
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    ignoreHTTPSErrors: true,
    acceptDownloads: true,
  });
  const page = await ctx.newPage();
  const gate = await passGate(page);
  if (!gate.ok) {
    await ctx.close();
    throw new Error(`gate failed: ${gate.detail}`);
  }
  let auth = { ok: true, landed: '/', account: null };
  if (accountKey) {
    auth = await login(page, accountKey);
    if (!auth.ok) {
      await ctx.close();
      throw new Error(`login failed for ${accountKey}: landed ${auth.landed}`);
    }
  }
  return { ctx, page, auth };
}

// ---------------------------------------------------------------------------
// Result recording
// ---------------------------------------------------------------------------

function makeRecorder(phaseId) {
  const rows = [];
  const rec = (id, name, status, notes = '', extra = {}) => {
    rows.push({ id, name, status, notes, ...extra });
    const tag = status === 'PASS' ? 'PASS' : status === 'FAIL' ? 'FAIL' : status === 'WARN' ? 'WARN' : 'INFO';
    console.log(`  [${tag}] ${id} — ${name}${notes ? ' :: ' + notes : ''}`);
  };
  const save = (meta = {}) => {
    ensureDirs();
    const pass = rows.filter((r) => r.status === 'PASS').length;
    const fail = rows.filter((r) => r.status === 'FAIL').length;
    const warn = rows.filter((r) => r.status === 'WARN').length;
    const info = rows.filter((r) => r.status === 'INFO').length;
    const out = path.join(RESULTS_DIR, `${phaseId}.json`);
    fs.writeFileSync(out, JSON.stringify({ phaseId, base: BASE, ...meta, pass, fail, warn, info, rows }, null, 2));
    console.log(`\n[${phaseId}] ${pass} PASS / ${fail} FAIL / ${warn} WARN / ${info} INFO -> ${path.relative(process.cwd(), out)}`);
    return { pass, fail, warn, info, out };
  };
  return { rec, rows, save };
}

module.exports = {
  BASE, HEADLESS, NAV_TIMEOUT, GATE_CANDIDATES, TEST_PASSWORD, ACCOUNTS,
  RESULTS_DIR, ARTIFACT_DIR, ensureDirs,
  detectGate, passGate, workingGatePassword, login, session, makeRecorder,
  chromium,
};
