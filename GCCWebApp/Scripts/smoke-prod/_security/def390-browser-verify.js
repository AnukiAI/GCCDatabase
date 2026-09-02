// DEF-390 — end-to-end BROWSER verification of the idle-session fix, against the live test env.
//
// This is the half the automated spec cannot reach. session-keepalive.js proves the server contract
// (the ping re-issues the cookie); it says nothing about whether a real user actually sees the
// warning, stays signed in while working, or recovers when a backgrounded tab comes forward. The
// client's complaint was half about the ALERT, which is entirely client-side, so this is the part
// that answers her.
//
// Four cases, run CONCURRENTLY in separate browser contexts so the whole thing is one ~35 minute
// window rather than four:
//
//   A  idle, foreground        -> warning modal at ~25 min, auto-logout at ~30 min
//   B  active, never navigates -> still signed in past 35 min; POST /Account/KeepAlive every ~5 min
//   C  backgrounded tab        -> on return to visible, resolves IMMEDIATELY (no silent bounce)
//   D  Remember Me             -> no logout at 35 min, and NO pings at all
//
// HONEST LIMIT ON CASE C: a headless page does not throttle timers the way a real backgrounded tab
// does, so this proves the visibilitychange handler fires and reaches the right verdict — not that
// Chrome's throttling is survived. The throttling itself is what the timestamp-based rewrite
// removes reliance on; that is a code property, verified by reading, not by this run.
//
// Read-only apart from its own logins. Run:
//   node Scripts/smoke-prod/_security/def390-browser-verify.js
// Env: DEF390_MINUTES (default 36), BASE_URL (default https://gccwebapp.com)

const { chromium, request } = require('playwright');
const { detectGateMode, passGate } = require('../_lib/test-env-gate');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const USER = process.env.DEF390_USER || 'test.analyst';
const PWD = process.env.DEF390_PWD || 'GccTest2026!';
const RUN_MIN = parseFloat(process.env.DEF390_MINUTES || '36');

const t0 = Date.now();
const mins = () => ((Date.now() - t0) / 60000).toFixed(1);
const log = (c, m) => console.log(`  [${String(mins()).padStart(4)}m] ${c}: ${m}`);

async function signIn(browser, storageState, rememberMe) {
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, storageState });
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('#Email').fill(USER);
  await page.locator('#Password').fill(PWD);
  if (rememberMe) await page.locator('#RememberMe').check();
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(1500);
  const ok = !/\/Account\/Login/i.test(page.url());
  return { ctx, page, ok };
}

// Count keep-alive pings and note when the session dies.
//
// Logout is detected by AUTHENTICATION STATE, not by matching the destination URL. The first
// version of this spec matched /Account/Login|SessionExpired|CertificateTileView and reported three
// false FAILs on 2026-09-02: the app actually lands on the site ROOT after logout, so a real logout
// went unseen and the fix looked broken. `#logoutForm` renders only for an authenticated user, so
// its disappearance is the signal — independent of wherever the app chooses to redirect.
function instrument(page, tag, state) {
  page.on('request', (r) => {
    if (r.url().includes('/Account/KeepAlive')) {
      state.pings.push(parseFloat(mins()));
      log(tag, `ping #${state.pings.length} -> /Account/KeepAlive`);
    }
  });
}

async function isSignedOut(page) {
  try {
    return await page.evaluate(() => !document.getElementById('logoutForm'));
  } catch {
    return false; // page mid-navigation; try again next poll
  }
}

const warningVisible = (page) =>
  page.evaluate(() => {
    const m = document.querySelector('.gcc-modal-message');
    return !!(m && /session will expire/i.test(m.textContent || ''));
  }).catch(() => false);

async function run() {
  console.log(`\n[DEF-390] browser verification — ${BASE} as ${USER}`);
  console.log(`run length: ${RUN_MIN} min (warning expected ~25, logout ~30)\n`);

  const mode = await detectGateMode(request, BASE);
  let storageState;
  if (mode.mode === 'cookie') {
    const g = await passGate(request, BASE);
    if (!g.ok) { console.error(`GATE FAILED: ${g.detail}`); process.exit(2); }
    storageState = g.state;
    console.log(`gate: ${g.detail}\n`);
  }

  const browser = await chromium.launch({ headless: true });
  const S = {
    A: { pings: [], loggedOutAt: null, warnAt: null },
    B: { pings: [], loggedOutAt: null, warnAt: null },
    C: { pings: [], loggedOutAt: null, warnAt: null, resolvedOnReturn: null },
    D: { pings: [], loggedOutAt: null, warnAt: null },
  };

  const sessions = {};
  for (const [tag, remember] of [['A', false], ['B', false], ['C', false], ['D', true]]) {
    const s = await signIn(browser, storageState, remember);
    if (!s.ok) { console.error(`${tag}: LOGIN FAILED`); await browser.close(); process.exit(2); }
    instrument(s.page, tag, S[tag]);
    sessions[tag] = s;
    log(tag, `signed in${remember ? ' (Remember Me ON)' : ''} -> ${s.page.url()}`);
  }

  // C goes to the background immediately and stays there until near the end.
  await sessions.C.page.emulateVisibilityState?.('hidden');
  log('C', 'tab hidden (backgrounded)');

  const deadline = Date.now() + RUN_MIN * 60000;
  let lastActivity = 0;
  let broughtForward = false;

  while (Date.now() < deadline) {
    const m = parseFloat(mins());

    // B: real user activity every ~2 min, WITHOUT navigating. Trusted events, so auth.js sees them.
    if (m - lastActivity >= 2 && !S.B.loggedOutAt) {
      lastActivity = m;
      try {
        await sessions.B.page.mouse.move(200 + (m % 50), 300 + (m % 40));
        await sessions.B.page.mouse.move(210 + (m % 50), 310 + (m % 40));
      } catch { /* page may be gone */ }
    }

    // C: bring the tab forward at 32 min — past the 30-minute deadline, so the correct behaviour
    // is to resolve at once rather than sit there looking alive.
    if (!broughtForward && m >= 32) {
      broughtForward = true;
      log('C', 'bringing tab to foreground at 32m (past the 30-minute deadline)');
      try {
        await sessions.C.page.emulateVisibilityState?.('visible');
        await sessions.C.page.waitForTimeout(4000);
        const w = await warningVisible(sessions.C.page);
        const out = (S.C.loggedOutAt !== null) || await isSignedOut(sessions.C.page);
        S.C.resolvedOnReturn = (w || out);
        log('C', `on return: warning=${w} loggedOut=${out} -> resolved=${S.C.resolvedOnReturn}`);
      } catch (e) { log('C', 'return failed: ' + e.message.split('\n')[0]); }
    }

    for (const tag of ['A', 'B', 'C', 'D']) {
      if (S[tag].loggedOutAt !== null) continue;
      if (S[tag].warnAt === null && await warningVisible(sessions[tag].page)) {
        S[tag].warnAt = parseFloat(mins());
        log(tag, 'WARNING MODAL shown');
      }
      if (await isSignedOut(sessions[tag].page)) {
        S[tag].loggedOutAt = parseFloat(mins());
        S[tag].logoutUrl = sessions[tag].page.url();
        log(tag, `LOGGED OUT (no #logoutForm) -> ${S[tag].logoutUrl}`);
      }
    }
    await new Promise((r) => setTimeout(r, 20000));
  }

  // ---- verdicts ----
  const results = [];
  const rec = (id, name, pass, notes) => { results.push({ id, name, status: pass ? 'PASS' : 'FAIL', notes }); };

  const near = (v, target, tol) => v !== null && Math.abs(v - target) <= tol;

  rec('A.warn', 'idle foreground: warning at ~25 min',
    near(S.A.warnAt, 25, 3), `warned at ${S.A.warnAt}m (expect 25 ±3)`);
  rec('A.logout', 'idle foreground: logged out at ~30 min',
    near(S.A.loggedOutAt, 30, 3), `logged out at ${S.A.loggedOutAt}m (expect 30 ±3)`);
  rec('A.order', 'warning came BEFORE logout',
    S.A.warnAt !== null && S.A.loggedOutAt !== null && S.A.warnAt < S.A.loggedOutAt,
    `warn ${S.A.warnAt}m vs logout ${S.A.loggedOutAt}m`);

  rec('B.alive', 'active-but-not-navigating: still signed in',
    S.B.loggedOutAt === null, S.B.loggedOutAt === null
      ? `survived ${RUN_MIN}m of activity without navigating — this is the case that was silently broken`
      : `LOGGED OUT at ${S.B.loggedOutAt}m`);
  rec('B.pings', 'active session sent keep-alive pings',
    S.B.pings.length >= 3, `${S.B.pings.length} pings at ${S.B.pings.join(', ')}m`);

  rec('C.resolved', 'backgrounded tab resolves on return (no silent bounce)',
    S.C.resolvedOnReturn === true,
    S.C.resolvedOnReturn === true ? 'warning or logout on foreground' : 'tab looked alive after its deadline');

  rec('D.alive', 'Remember Me: NOT logged out',
    S.D.loggedOutAt === null, S.D.loggedOutAt === null ? `still signed in at ${RUN_MIN}m` : `LOGGED OUT at ${S.D.loggedOutAt}m`);
  rec('D.nopings', 'Remember Me: sent NO keep-alive pings',
    S.D.pings.length === 0, `${S.D.pings.length} pings (expect 0)`);

  console.log('\n=== DEF-390 browser verification ===');
  for (const r of results) console.log(`  [${r.status}] ${r.id.padEnd(11)} ${r.name} — ${r.notes}`);
  const pass = results.filter((r) => r.status === 'PASS').length;
  const fail = results.length - pass;
  console.log(`\n  ${pass} PASS / ${fail} FAIL`);

  const dir = path.join(__dirname, '..', '_results');
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  const out = path.join(dir, `def390-browser-verify-${new Date().toISOString().replace(/[:.]/g, '-')}.json`);
  fs.writeFileSync(out, JSON.stringify({ base: BASE, user: USER, runMinutes: RUN_MIN, state: S, results, finishedAt: new Date().toISOString() }, null, 2));
  console.log(`  results -> ${path.relative(process.cwd(), out)}`);

  await browser.close();
  process.exit(fail > 0 ? 1 : 0);
}

run();
