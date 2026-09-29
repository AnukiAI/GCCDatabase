/**
 * Captures every screenshot used in the Accounts & Sign-In Guide, from the TEST environment.
 *
 * Run:  NODE_PATH="$(npm root -g)" node capture-screenshots.js
 * Then: python build-guide.py
 *
 * Notes that cost time to work out, kept so they do not have to be rediscovered:
 *
 *  - The access gate serves an HTML FORM, not a 401 challenge, so Playwright's `httpCredentials`
 *    never fires and every capture silently comes back as the gate page. The Authorization header
 *    must be sent on every request instead. `shot()` refuses to save anything whose <title> still
 *    looks like the gate, so that failure can never pass silently again.
 *  - The Add New User dialog is taller than a 900px viewport; its lower half was cut off. It is
 *    captured at 1500px.
 *  - Company Access / Partner fields only exist AFTER a role is chosen, so those variants need
 *    the role selected first (#user-role).
 *  - Email addresses in the user list are blurred before capture. This is a client-facing
 *    document and the test accounts' addresses are not part of the instructions.
 */
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'https://db.greencirclecertified.com';
const OUT = path.join(__dirname, 'shots');
const GATE = {
  Authorization: 'Basic ' + Buffer.from(
    `${process.env.GATE_USER || 'gcc'}:${process.env.GATE_PASS || 'Cert-Test-Access-2026!'}`
  ).toString('base64')
};
const ADMIN = { u: process.env.ADMIN_USER || 'admin.uat', p: process.env.ADMIN_PASS || 'TestPass123!' };
const PLAIN = { u: process.env.PLAIN_USER || 'test.analyst', p: process.env.PLAIN_PASS || 'GccTest2026!' };
const TWOFA = { u: process.env.TWOFA_USER || '2fatest',     p: process.env.TWOFA_PASS || 'GccTest2026!' };

if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

const ok = [], bad = [];

const REDACT = () => {
  document.querySelectorAll('td, .cell, span').forEach(el => {
    if (el.children.length === 0 && /@[\w.-]+\.\w+/.test(el.textContent || '')) {
      el.style.filter = 'blur(4.5px)';
    }
  });
};

/**
 * Drop the test-environment banner and neutralise the signed-in account name before every shot.
 * Added 2026-09-25: this script predated the clean-up the Company/Analyst scripts do, so this
 * guide's shots carried a red "TEST ENVIRONMENT ... do not share this URL publicly" banner and the
 * internal test account names into a client document. Pixels do not show up in a text leak-grep,
 * so `shot()` also refuses to save if the banner is still visible afterwards.
 */
const CLEAN_CHROME = () => {
  [...document.querySelectorAll('div, header, section, p')]
    .filter(el => /TEST ENVIRONMENT/i.test(el.textContent || '') && el.textContent.length < 200)
    .forEach(el => { el.style.display = 'none'; });
  document.querySelectorAll('span, div, a, p, td, button').forEach(el => {
    if (el.children.length) return;
    if (/^\s*(test\.\w+|admin\.uat|2fatest)\s*$/i.test(el.textContent || '')) el.textContent = 'your.account';
  });
};
const BANNER_VISIBLE = () => [...document.querySelectorAll('body *')].some(el =>
  !el.children.length && /TEST ENVIRONMENT/i.test(el.textContent || '') && el.offsetParent !== null);

async function shot(page, name, opts = {}) {
  const title = await page.title();
  if (/gate|restricted access/i.test(title)) { bad.push(`${name}: GATE PAGE - not saved`); return; }
  await page.evaluate(CLEAN_CHROME);
  if (await page.evaluate(BANNER_VISIBLE)) { bad.push(`${name}: TEST ENVIRONMENT banner still visible — refused to save`); return; }
  await page.waitForTimeout(900);
  const f = path.join(OUT, name + '.png');
  await page.screenshot({ path: f, ...opts });
  ok.push(name);
  console.log(`  ${name}.png  ${Math.round(fs.statSync(f).size / 1024)} KB  [${title}]`);
}

async function login(page, u, p) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[name="Email"]', u);
  await page.fill('input[name="Password"]', p);
  await page.click('button[type="submit"], input[type="submit"]');
  await page.waitForTimeout(1800);
  return page.url();
}

// The gate header goes ONLY to the app's own origin.
//
// This used to be `extraHTTPHeaders: GATE`, which Playwright applies to EVERY request including
// cross-origin ones. Font Awesome is pulled from cdn.jsdelivr.net with crossorigin="anonymous"
// (Views/Shared/_Layout.cshtml:36); forcing an Authorization header onto that request makes the
// font fail to load, and every icon in every capture renders as an empty grey square. Scoping the
// header to BASE fixes it. Do not go back to extraHTTPHeaders.
const ctx = async (b, h = 900) => {
  const c = await b.newContext({ viewport: { width: 1440, height: h } });
  const appOrigin = new URL(BASE).origin;
  await c.route('**/*', (route, req) => {
    route.continue(req.url().startsWith(appOrigin) ? { headers: { ...req.headers(), ...GATE } } : {});
  });
  return c;
};

(async () => {
  const b = await chromium.launch();

  // ── signed out ───────────────────────────────────────────────────────────────
  let c = await ctx(b), p = await c.newPage();
  await p.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await shot(p, '01-login', { fullPage: true });
  await p.goto(`${BASE}/Account/ForgotPassword`, { waitUntil: 'domcontentloaded' });
  await shot(p, '02-forgot-password', { fullPage: true });
  // SKIP_2FA=1 leaves 03 untouched. Each failed sign-in counts toward the 5-failure lockout on the
  // one account that can receive mail, so do not keep re-running this step while it is failing.
  if (process.env.SKIP_2FA) console.log('  03-2fa-challenge  skipped (SKIP_2FA)');
  else {
    const u = await login(p, TWOFA.u, TWOFA.p);
    if (/TwoFactorAuthentication/i.test(u)) await shot(p, '03-2fa-challenge', { fullPage: true });
    else bad.push('03-2fa-challenge: landed on ' + u);
  }
  await c.close();

  // ── admin: user list + row Actions menu ──────────────────────────────────────
  c = await ctx(b, 1000); p = await c.newPage();
  await login(p, ADMIN.u, ADMIN.p);
  await p.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'domcontentloaded' });
  // Wait for real rows AND for the "Loading..." overlay to go. A fixed 2.6s sleep captured the page
  // mid-load on 2026-09-25: dimmed, spinner up, empty table.
  const loaded = await p.waitForFunction(() => {
    const rows = document.querySelectorAll('tbody tr td').length;
    const spinner = [...document.querySelectorAll('body *')].some(e => !e.children.length &&
      /^\s*Loading/i.test(e.textContent || '') && e.offsetParent !== null);
    return rows > 5 && !spinner;
  }, null, { timeout: 30000 }).then(() => true).catch(() => false);
  if (!loaded) bad.push('04/08: user list never finished loading');
  await p.waitForTimeout(800);
  await p.evaluate(() => {
    const t = document.querySelector('table');
    if (t) t.scrollIntoView({ block: 'start' });
    window.scrollBy(0, -90);
  });
  await p.waitForTimeout(700);
  await p.evaluate(REDACT);
  await shot(p, '04-user-management');
  try {
    await p.locator('tbody tr').nth(1).locator('td').last().locator('button').first().click({ timeout: 8000 });
    await p.waitForTimeout(1000);
    await p.evaluate(REDACT);
    await shot(p, '08-actions-menu');
  } catch (e) { bad.push('08-actions-menu: ' + e.message); }
  await c.close();

  // ── admin: the Add New User dialog, three states ─────────────────────────────
  for (const [role, name] of [[null, '05-add-user'], ['External', '09-add-user-external'], ['Partner', '10-add-user-partner']]) {
    c = await ctx(b, 1500); p = await c.newPage();
    await login(p, ADMIN.u, ADMIN.p);
    await p.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'domcontentloaded' });
    await p.waitForTimeout(2600);
    try {
      await p.locator('#add-user-btn').click({ timeout: 8000 });
      await p.waitForTimeout(1500);
      if (role) { await p.selectOption('#user-role', role); await p.waitForTimeout(1600); }
      await shot(p, name);
    } catch (e) { bad.push(`${name}: ${e.message}`); }
    await c.close();
  }

  // ── two-factor preferences + authenticator setup ─────────────────────────────
  c = await ctx(b, 1200); p = await c.newPage();
  await login(p, PLAIN.u, PLAIN.p);
  await p.goto(`${BASE}/Account/TwoFactorPreferences`, { waitUntil: 'domcontentloaded' });
  await shot(p, '06-2fa-preferences', { fullPage: true });
  await p.goto(`${BASE}/Account/SetupTotp`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2500);
  // The QR code on this page encodes a REAL, live TOTP secret for the account. Until 2026-09-25
  // it was captured sharp enough to scan and the only protection was a console NOTE asking someone
  // to clear the secret in SQL afterwards. Blur it before the shutter instead, and refuse to save
  // if nothing QR-shaped was found — a layout change must not silently put a scannable code back.
  const blurred = await p.evaluate(() => {
    let n = 0;
    document.querySelectorAll('img, canvas, svg, table').forEach(el => {
      const r = el.getBoundingClientRect();
      if (r.width >= 100 && r.width <= 400 && Math.abs(r.width - r.height) < 8) {
        el.style.filter = 'blur(9px)'; n++;
      }
    });
    // The manual-entry secret is the same credential as text; make sure it is not on screen.
    document.querySelectorAll('code, .manual-code, #manualEntryKey, [id*="secret" i], [class*="secret" i]')
      .forEach(el => { el.style.filter = 'blur(9px)'; });
    return n;
  });
  if (!blurred) bad.push('07-setup-authenticator: no QR-shaped element found to blur — refused to save');
  else await shot(p, '07-setup-authenticator', { fullPage: true });
  await c.close();

  await b.close();
  console.log(`\ncaptured ${ok.length}: ${ok.join(', ')}`);
  if (bad.length) { console.log('\nFAILED:'); bad.forEach(x => console.log('  ' + x)); process.exitCode = 1; }
  console.log('\nNOTE: the QR in 07-setup-authenticator is blurred, but visiting the page still wrote a pending TOTP' +
              ' secret for ' + PLAIN.u + '. Optional tidy-up on the test database:');
  console.log("      UPDATE Users SET TwoFactorSecret=NULL WHERE Username='" + PLAIN.u + "' AND TwoFactorEnabled=0;");
})();
