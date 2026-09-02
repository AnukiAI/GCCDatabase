// Security-regression harness — shared infra for the maintained isolation / RBAC / IDOR suite.
//
// Phase 1 of docs/qa/regression-e2e-strategy.md (P0 gaps). This promotes the ad-hoc one-off
// probes (cr171-*.js, def236-idor-verify.js, the partner section of prod-e2e-readonly-batch.js)
// into a UNIFORM, RE-RUNNABLE suite so they act as a deploy gate instead of going stale after
// they prove a fix once.
//
// Design mirrors _lib/smoke-template.js conventions on purpose:
//   - LOCALHOST ONLY by default (refuses gccwebapp.com / greencirclecertified.com unless
//     SMOKE_ALLOW_REMOTE=1) — these are READ-ONLY checks, but the guard keeps parity.
//   - Uniform record()/roll-up + JSON results into ../_results/security-{spec}-{stamp}.json.
//   - Playwright context.request for API calls (shares the login cookie + test-env gate).
//
// Each spec exports:  async function run() -> { specId, pass, fail, warn, blocked, results }
// and run-all.js aggregates them into one summary + exit code.
//
// Env: BASE_URL (default http://localhost:5101), SMOKE_ALLOW_REMOTE, SMOKE_REMOTE_USER/PASS
//      (test-env basic-auth gate on remote), SEC_PWD (test-account password, default TestPass123!).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const ALLOW_REMOTE = process.env.SMOKE_ALLOW_REMOTE === '1';
const PWD = process.env.SEC_PWD || 'TestPass123!';
const NAV_TIMEOUT = parseInt(process.env.SMOKE_NAV_TIMEOUT || '60000', 10);

// Timeout for API/request calls. Playwright's built-in default is 30s, which is NOT enough on a
// loaded dev box: DEF-393 measured the same certificate page at 47ms one moment and 80s the next,
// purely from machine contention. That made `partner-isolation` fail intermittently on a DIFFERENT
// user each run with "Timeout 30000ms exceeded" - noise that looked like a security failure and
// stopped the gate ever reaching green. 90s covers the worst observed outlier with margin while
// staying bounded; raise it with SMOKE_REQ_TIMEOUT (or SMOKE_NAV_TIMEOUT, honoured for both).
const REQ_TIMEOUT = parseInt(
  process.env.SMOKE_REQ_TIMEOUT || process.env.SMOKE_NAV_TIMEOUT || '90000', 10);

// Basic-auth for the test-env gate on remote targets (no-op on localhost).
const REMOTE_USER = process.env.SMOKE_REMOTE_USER || '';
const REMOTE_PASS = process.env.SMOKE_REMOTE_PASS || '';
const HTTP_CREDENTIALS = (REMOTE_USER && REMOTE_PASS)
  ? { username: REMOTE_USER, password: REMOTE_PASS }
  : null;

const RESULTS_DIR = path.join(__dirname, '..', '..', '_results');

// Known test accounts. Role users per CLAUDE.md "Quick Credentials"; partner users per the
// restored CR-171 test data (cr171-partner-isolation-local.js). If any are missing from the
// local DB the spec records BLOCKED (login failed), not FAIL — a data-setup gap, not a leak.
const ROLE_USERS = {
  admin:    'admin',
  analyst:  'analyst',
  external: 'external',
};

const PARTNER_USERS = [
  { email: 'partner@amazon.com',      own: ['Amazon'] },
  { email: 'perry.novi@example.com',  own: ['Novi'] },
  { email: 'pam.wayfair@example.com', own: ['Wayfair'] },
  { email: 'pip.other@example.com',   own: ['Other'] },
  { email: 'pru.multi@example.com',   own: ['Amazon', 'Novi'] }, // multi-partner
];
const ALL_PARTNERS = ['Amazon', 'Novi', 'Other', 'Wayfair'];

function guardRemote() {
  if ((BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) && !ALLOW_REMOTE) {
    console.error(`REFUSING TO RUN against ${BASE} — set SMOKE_ALLOW_REMOTE=1 to override.`);
    process.exit(2);
  }
}

async function newContext(browser) {
  const ctx = await browser.newContext({
    ignoreHTTPSErrors: true,
    viewport: { width: 1440, height: 900 },
    ...(HTTP_CREDENTIALS ? { httpCredentials: HTTP_CREDENTIALS } : {}),
  });
  // Page-level actions (locator clicks, waitForLoadState) default to 30s too - same problem.
  ctx.setDefaultTimeout(NAV_TIMEOUT);
  ctx.setDefaultNavigationTimeout(NAV_TIMEOUT);
  return ctx;
}

// Log in via the real login form. Returns { ok, page }. ok=false means bounced to /Account/Login.
async function login(ctx, emailOrRole, pwd = PWD) {
  const email = ROLE_USERS[emailOrRole] || emailOrRole;
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
  await page.locator('#Email').fill(email);
  await page.locator('#Password').fill(pwd);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(900); // absorb redirect chains (2FA prompt on fresh devices, etc.)
  const ok = !/\/Account\/Login/i.test(page.url());
  return { ok, page };
}

// GET a JSON API endpoint through the context (carries the login cookie). Normalizes the
// tile-API response shape ({totalCount|TotalCount, certificates|Certificates}).
async function apiJson(ctx, apiPath) {
  const r = await ctx.request.get(BASE + apiPath, { headers: { Accept: 'application/json' }, timeout: REQ_TIMEOUT });
  let j = null;
  try { j = await r.json(); } catch { /* non-JSON */ }
  return {
    status: r.status(),
    total: j ? (j.totalCount ?? j.TotalCount ?? null) : null,
    certs: (j && (j.certificates || j.Certificates)) || [],
    raw: j,
  };
}

// Sentinels for "no HTTP response was received". Deliberately OUTSIDE the 100-599 range so they
// can never be compared equal to a real status code, and so a timeout can never satisfy a
// "was it denied?" check — a hung request is not proof of a denial.
const STATUS = { TIMEOUT: -2, ERROR: -1 };

// Render a status (or sentinel) for a result note.
function fmtStatus(code) {
  if (code === STATUS.TIMEOUT) return 'TIMEOUT (no HTTP response)';
  if (code === STATUS.ERROR) return 'REQUEST FAILED (no HTTP response)';
  return `HTTP ${code}`;
}

// Raw GET returning status only (for RBAC negative-path checks). maxRedirects:0 so a login
// redirect surfaces as 302 rather than being followed to a 200 login page.
//
// On any error this returns a STATUS sentinel, never a fabricated code. The previous version
// regex-matched the FIRST THREE DIGITS of the exception message, so Playwright's
// "Timeout 30000ms exceeded" was reported as "HTTP 300" — a status that does not exist. Two
// cert-detail-scoping assertions failed that way on 2026-09-01 and the output read as a bizarre
// redirect, sending the reader hunting for a routing bug instead of the hung request that was
// actually there. A test harness that invents evidence is worse than one that reports nothing.
async function status(ctx, urlPath, opts = {}) {
  try {
    const r = await ctx.request.get(BASE + urlPath, { maxRedirects: 0, timeout: REQ_TIMEOUT, ...opts });
    return r.status();
  } catch (e) {
    const msg = ((e && e.message) || String(e)).split(/\r?\n/)[0];
    if (/timeout/i.test(msg)) {
      console.log(`    ! ${urlPath} -> TIMEOUT: ${msg}`);
      return STATUS.TIMEOUT;
    }
    // Some Playwright versions throw on 3xx with maxRedirects:0. Accept a code ONLY when the
    // message explicitly labels it as a status AND it is a valid 1xx-5xx value — never a bare
    // number that happens to appear in the text.
    const m = /status(?:\s*code)?\D{0,4}([1-5]\d{2})\b/i.exec(msg);
    if (m) return parseInt(m[1], 10);
    console.log(`    ! ${urlPath} -> REQUEST FAILED: ${msg}`);
    return STATUS.ERROR;
  }
}

// word-boundary partner-name scan over a stringified cert (matches the one-off convention).
function scanLeaks(certs, forbiddenNames) {
  const leaks = [];
  for (const c of certs) {
    const blob = JSON.stringify(c);
    for (const fp of forbiddenNames) {
      if (new RegExp(`\\b${fp}\\b`).test(blob)) {
        leaks.push({ cert: c.certificateNumber || c.CertificateNumber || c.id || '?', leaked: fp });
      }
    }
  }
  return leaks;
}

// Uniform recorder. status in PASS|FAIL|WARN|BLOCKED.
function makeRecorder(specId) {
  const results = [];
  const glyph = { PASS: 'PASS', FAIL: 'FAIL', WARN: 'WARN', BLOCKED: 'BLOCK' };
  const record = (id, name, st, notes = '') => {
    results.push({ id, name, status: st, notes });
    console.log(`  [${glyph[st] || st}] ${id} ${name}${notes ? ' - ' + notes : ''}`);
  };
  return { results, record };
}

function summarize(specId, url, results) {
  const c = (s) => results.filter((r) => r.status === s).length;
  const summary = {
    specId, base: BASE, url: url || null,
    pass: c('PASS'), fail: c('FAIL'), warn: c('WARN'), blocked: c('BLOCKED'),
    total: results.length, results,
  };
  if (!fs.existsSync(RESULTS_DIR)) fs.mkdirSync(RESULTS_DIR, { recursive: true });
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `security-${specId}-${stamp}.json`);
  fs.writeFileSync(outFile, JSON.stringify({ ...summary, finishedAt: new Date().toISOString() }, null, 2));
  console.log(`\n  [${specId}] ${summary.pass} PASS / ${summary.fail} FAIL / ${summary.warn} WARN / ${summary.blocked} BLOCKED`);
  console.log(`  results -> ${path.relative(process.cwd(), outFile)}`);
  return { ...summary, outFile };
}

module.exports = {
  chromium, BASE, PWD, REQ_TIMEOUT, NAV_TIMEOUT,
  ROLE_USERS, PARTNER_USERS, ALL_PARTNERS,
  guardRemote, newContext, login, apiJson, status, STATUS, fmtStatus, scanLeaks,
  makeRecorder, summarize,
};
