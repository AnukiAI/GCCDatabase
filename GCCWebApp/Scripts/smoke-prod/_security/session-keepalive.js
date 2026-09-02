// SECURITY / SESSION — the DEF-390 keep-alive contract.
//
// DEF-390: the auth cookie counted from the last HTTP request while wwwroot/js/auth.js counted
// from the last mousemove, and ASP.NET Core only reissues a sliding cookie past the half-life —
// so the real idle timeout was a 15-30 minute range, not 30, and the expiry warning was
// unreliable. POST /Account/KeepAlive turns real user activity into a real request AND re-issues
// the cookie explicitly, which is what pins the two clocks together.
//
// This spec exists because every part of that is silently reversible:
//   * make KeepAlive [AllowAnonymous]  -> the 401 signal auth.js relies on to detect a dead
//                                         session disappears, and it hangs on a 200 forever
//   * drop the SignInAsync re-issue    -> looks fine, endpoint still 200s, bug fully returns
//   * revert / ship a stale auth.js    -> server half live, client half absent, bug returns
//
// K4 + K5 are the pair that matter: a ping MUST reissue the cookie, and a plain page request at
// the same moment must NOT. Without the K5 control, K4 would pass on any request and prove
// nothing — a green K4 alone cannot tell the fix apart from ambient sliding-expiration behaviour.
//
// Assertions:
//   K1  anon  POST /Account/KeepAlive  (X-Requested-With)  = 401          (auth.js's expiry signal)
//   K2  anon  POST /Account/KeepAlive  (no header)         never success  (no anon keep-alive)
//   K3  auth  POST /Account/KeepAlive                      = 200 {"success":true}
//   K4  auth  POST /Account/KeepAlive                      REISSUES GCC.Auth   *** the fix ***
//   K5  auth  GET  /Dashboard (control, same session)      does NOT reissue    *** the control ***
//   K6  auth  POST /Account/KeepAlive                      Cache-Control: no-store
//   K7  GET   /js/auth.js                                  ships the client half (ping + visibilitychange)
//   K8  Remember Me session: a ping must not downgrade a persistent cookie to the idle window
//
// Read-only — creates no data and mutates nothing but its own session cookie.
// Run:  node Scripts/smoke-prod/_security/session-keepalive.js

const H = require('./_lib/security-harness');

const KEEPALIVE = '/Account/KeepAlive';
const AJAX = { 'X-Requested-With': 'XMLHttpRequest' };

// Any authenticated role proves this contract — it is role-independent. Tried in order so the
// spec survives both databases: admin.uat is the one Admin that works on dev AND test (the
// `admin` password was rotated on test 2026-08-19, see CLAUDE.md); test.analyst exists on test.
const CANDIDATES = ['admin.uat', 'admin', 'test.analyst'];

const setCookies = (res) =>
  res.headersArray()
    .filter((h) => h.name.toLowerCase() === 'set-cookie')
    .map((h) => h.value);

const reissuesAuth = (res) => setCookies(res).some((c) => /^GCC\.Auth=/.test(c));

async function loginAny(browser, record, specId) {
  for (const user of CANDIDATES) {
    const ctx = await H.newContext(browser);
    const { ok } = await H.login(ctx, user);
    if (ok) return { ctx, user };
    await ctx.close();
  }
  record(`${specId}.login`, 'authenticated session for keep-alive checks', 'BLOCKED',
    `none of ${CANDIDATES.join(', ')} could sign in`);
  return { ctx: null, user: null };
}

// Log in with "Remember Me" ticked so the cookie is persistent (14 days, IsPersistent=true).
async function loginPersistent(browser, user) {
  const ctx = await H.newContext(browser);
  const page = await ctx.newPage();
  await page.goto(H.BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill(user);
  await page.locator('#Password').fill(H.PWD);
  await page.locator('#RememberMe').check();
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(900);
  const ok = !/\/Account\/Login/i.test(page.url());
  return { ctx, ok };
}

async function run() {
  H.guardRemote();
  console.log(`\n[security] session-keepalive (DEF-390) — ${H.BASE}\n`);
  const specId = 'session-keepalive';
  const { results, record } = H.makeRecorder(specId);
  const browser = await H.chromium.launch({ headless: true });

  try {
    // ---- K1 / K2: an anonymous caller must never be kept alive ----
    const anonCtx = await H.newContext(browser);

    const anonAjax = await anonCtx.request.post(H.BASE + KEEPALIVE, { headers: AJAX, maxRedirects: 0, timeout: H.REQ_TIMEOUT });
    record('K1', 'anon ping (X-Requested-With) returns 401',
      anonAjax.status() === 401 ? 'PASS' : 'FAIL',
      `got ${anonAjax.status()}` + (anonAjax.status() === 200
        ? ' — either the endpoint is anonymous, or the 401 is arriving as login HTML at 200 (the DEF-387 trap)'
        : ''));

    const anonPlain = await anonCtx.request.post(H.BASE + KEEPALIVE, { maxRedirects: 0, timeout: H.REQ_TIMEOUT });
    let anonBody = '';
    try { anonBody = await anonPlain.text(); } catch { /* ignore */ }
    const anonSucceeded = /"success"\s*:\s*true/i.test(anonBody);
    record('K2', 'anon ping never returns a success body',
      anonSucceeded ? 'FAIL' : 'PASS',
      `status ${anonPlain.status()}` + (anonSucceeded ? ' — ANONYMOUS SESSION KEPT ALIVE' : ''));

    await anonCtx.close();

    // ---- authenticated session ----
    const { ctx, user } = await loginAny(browser, record, specId);
    if (!ctx) { await browser.close(); return H.summarize(specId, null, results); }

    // K5 runs FIRST, deliberately. Straight after login the cookie has just been issued, so a
    // plain page request is nowhere near the sliding half-life and must not reissue it. Taking
    // the control before the ping keeps it honest — it cannot be explained away by the ping's
    // own refresh having already moved the window.
    const control = await ctx.request.get(H.BASE + '/Dashboard', { maxRedirects: 0, timeout: H.REQ_TIMEOUT });
    const controlReissued = reissuesAuth(control);
    record('K5', 'CONTROL: plain GET /Dashboard does not reissue GCC.Auth',
      controlReissued ? 'WARN' : 'PASS',
      controlReissued
        ? 'a normal request reissued the cookie — K4 cannot distinguish the fix from ambient sliding behaviour'
        : `status ${control.status()}, no GCC.Auth Set-Cookie (as expected)`);

    // ---- K3 / K4 / K6: the ping itself ----
    const ping = await ctx.request.post(H.BASE + KEEPALIVE, { headers: AJAX, maxRedirects: 0, timeout: H.REQ_TIMEOUT });
    let pingJson = null;
    try { pingJson = await ping.json(); } catch { /* non-JSON */ }

    record('K3', 'authenticated ping returns 200 with a success body',
      (ping.status() === 200 && pingJson && pingJson.success === true) ? 'PASS' : 'FAIL',
      `status ${ping.status()}, body ${JSON.stringify(pingJson)} (as ${user})`);

    const pingReissued = reissuesAuth(ping);
    record('K4', 'ping RE-ISSUES the auth cookie (the DEF-390 fix)',
      pingReissued ? 'PASS' : 'FAIL',
      pingReissued
        ? 'GCC.Auth Set-Cookie present — the session window restarts at ping time'
        : 'NO GCC.Auth Set-Cookie — KeepAlive is a no-op again. Sliding expiration will not refresh '
          + 'before the half-life, so the 15-30 minute early-timeout and the unreliable warning are BACK');

    const cacheControl = (ping.headers()['cache-control'] || '').toLowerCase();
    record('K6', 'ping response is not cacheable',
      cacheControl.includes('no-store') ? 'PASS' : 'FAIL',
      `Cache-Control: ${cacheControl || '(absent)'}`);

    // ---- K7: the client half must actually ship ----
    // Per feedback_verify_deployed_asset_not_symptom — a live server half proves nothing if the
    // JS that calls it never reached the server.
    const js = await ctx.request.get(H.BASE + '/js/auth.js', { timeout: H.REQ_TIMEOUT });
    const jsBody = js.ok() ? await js.text() : '';
    const hasPing = jsBody.includes(KEEPALIVE);
    const hasVisibility = jsBody.includes('visibilitychange');
    record('K7', 'served auth.js contains the keep-alive client half',
      (hasPing && hasVisibility) ? 'PASS' : 'FAIL',
      `status ${js.status()}, ping: ${hasPing}, visibilitychange: ${hasVisibility}`
        + ((hasPing && hasVisibility) ? '' : ' — stale or reverted asset: the server half is live but nothing calls it'));

    await ctx.close();

    // ---- K8: a persistent ("Remember Me") cookie must not be downgraded ----
    const persistent = await loginPersistent(browser, user);
    if (!persistent.ok) {
      record('K8', 'ping does not downgrade a Remember Me cookie', 'BLOCKED',
        'could not establish a Remember Me session');
    } else {
      const pPing = await persistent.ctx.request.post(H.BASE + KEEPALIVE, { headers: AJAX, maxRedirects: 0, timeout: H.REQ_TIMEOUT });
      const rewritten = setCookies(pPing).find((c) => /^GCC\.Auth=/.test(c));
      // Correct behaviour is to leave a persistent cookie alone entirely. If it IS rewritten,
      // the expiry must at least not have collapsed to the ~30-minute non-persistent window.
      let verdict = 'PASS';
      let note = 'persistent cookie left untouched by the ping (expected)';
      if (rewritten) {
        const m = /expires=([^;]+)/i.exec(rewritten);
        const expiresAt = m ? Date.parse(m[1]) : NaN;
        const hoursOut = isNaN(expiresAt) ? null : (expiresAt - Date.now()) / 36e5;
        if (hoursOut !== null && hoursOut < 24) {
          verdict = 'FAIL';
          note = `Remember Me cookie rewritten to expire in ~${hoursOut.toFixed(1)}h — a 14-day session was downgraded to the idle window`;
        } else {
          note = `cookie rewritten but still long-lived (~${hoursOut === null ? '?' : hoursOut.toFixed(0)}h)`;
        }
      }
      record('K8', 'ping does not downgrade a Remember Me cookie', verdict, note);
      await persistent.ctx.close();
    }
  } finally {
    await browser.close();
  }

  return H.summarize(specId, null, results);
}

module.exports = { run };

if (require.main === module) {
  run().then((s) => process.exit(s.fail > 0 || s.blocked > 0 ? 1 : 0));
}
