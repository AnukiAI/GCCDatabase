// Shared test-environment gate handling for every script that talks to gccwebapp.com.
//
// WHY THIS IS A SHARED MODULE AND NOT COPY-PASTE
// `TestEnvironmentGate.Mode` is "Cookie" (appsettings.Production.json), not Basic Auth. Every path
// 302s to /__gate, which renders a form and sets a data-protected GCC.TestGate cookie; the POST also
// needs a CSRF `formToken` minted when that form renders.
//
// Scripts that send httpCredentials and follow redirects therefore receive HTTP 200 and a
// ~2,519-byte gate page for EVERY url, and grade that page as the application. smoke.js did exactly
// this and reported a FALSE security regression ("/uploads/certificates/* served, status=200" when
// the deployed answer is 404) — see DEF-341. Every script under Scripts/smoke-prod/ that hits the
// deployed site has the same failure mode, so the fix lives in one place. A second private copy of
// this logic would drift from the middleware the same way the old assumption did.
//
// Usage (Playwright APIRequestContext or browser context):
//   const { detectGateMode, passGate } = require('./_lib/test-env-gate');
//   const mode  = await detectGateMode(request, BASE);
//   const gate  = mode.mode === 'cookie' ? await passGate(request, BASE) : { ok: true, state: undefined };
//   const ctx   = await browser.newContext({ storageState: gate.state });   // through the gate

const GATE_PATH = '/__gate';
const GATE_COOKIE = 'GCC.TestGate';

const DEFAULT_USER = process.env.GATE_USER || 'gcc';
const DEFAULT_PASS = process.env.GATE_PASS || 'Cert-Test-Access-2026!';

/**
 * Ask the deployed site how it is gated — anonymously, and WITHOUT following redirects.
 * Returns { mode: 'basic' | 'cookie' | 'open' | 'unknown', detail }.
 * 'open' is a finding in its own right on a public test env, not a convenience.
 */
async function detectGateMode(request, base) {
  const anon = await request.newContext();
  try {
    const r = await anon.get(`${base}/`, { maxRedirects: 0, failOnStatusCode: false });
    const status = r.status();
    const auth = r.headers()['www-authenticate'] || '';
    const loc = r.headers()['location'] || '';
    if (status === 401 && /basic/i.test(auth)) return { mode: 'basic', detail: `401, www-authenticate="${auth}"` };
    if (status >= 300 && status < 400 && loc.startsWith(GATE_PATH)) return { mode: 'cookie', detail: `${status} -> ${loc}` };
    if (status === 200) return { mode: 'open', detail: 'no gate — site is publicly browsable' };
    return { mode: 'unknown', detail: `status=${status}, location="${loc || 'none'}"` };
  } finally {
    await anon.dispose();
  }
}

/**
 * Pass the cookie gate. Returns { ok, detail, state } where `state` is a Playwright storageState
 * carrying GCC.TestGate — usable by both request.newContext() and browser.newContext().
 *
 * The CSRF `formToken` is minted with the form and must be fresh; posting the password alone
 * returns 400 "Your session expired", NOT 401, which reads like a wrong password and sends you
 * looking in the wrong place.
 */
async function passGate(request, base, opts = {}) {
  const password = opts.password || DEFAULT_PASS;
  const ctx = await request.newContext();
  try {
    const form = await ctx.get(`${base}${GATE_PATH}?returnUrl=%2F`, { failOnStatusCode: false });
    const html = await form.text();
    const tok = /name="formToken"[^>]*value="([^"]+)"/.exec(html);
    if (!tok) return { ok: false, detail: `gate page carried no formToken field (status=${form.status()}, ${html.length}B)` };

    const post = await ctx.post(`${base}${GATE_PATH}`, {
      form: { password, returnUrl: '/', formToken: tok[1] },
      maxRedirects: 0,
      failOnStatusCode: false,
    });
    const state = await ctx.storageState();
    if (!state.cookies.some(c => c.name === GATE_COOKIE)) {
      const why = post.status() === 401 ? 'password rejected — check GATE_PASS'
        : post.status() === 400 ? 'CSRF formToken rejected as stale'
        : `unexpected status ${post.status()}`;
      return { ok: false, detail: why };
    }
    return { ok: true, detail: `${GATE_COOKIE} issued (POST ${post.status()})`, state };
  } finally {
    await ctx.dispose();
  }
}

/**
 * One-call convenience for scripts that just want to get in: detect, pass if needed, return
 * { mode, ok, state }. Callers should treat ok === false as fatal — running on gate pages produces
 * confident nonsense, which is worse than not running.
 */
async function enterSite(request, base, opts = {}) {
  const mode = await detectGateMode(request, base);
  if (mode.mode !== 'cookie') return { mode: mode.mode, detail: mode.detail, ok: mode.mode !== 'unknown', state: undefined };
  const g = await passGate(request, base, opts);
  return { mode: 'cookie', detail: g.detail, ok: g.ok, state: g.state };
}

module.exports = {
  GATE_PATH, GATE_COOKIE, DEFAULT_USER, DEFAULT_PASS,
  detectGateMode, passGate, enterSite,
};
