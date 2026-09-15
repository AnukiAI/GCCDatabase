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

const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
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

async function shot(page, name, opts = {}) {
  const title = await page.title();
  if (/gate|restricted access/i.test(title)) { bad.push(`${name}: GATE PAGE - not saved`); return; }
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
  const u = await login(p, TWOFA.u, TWOFA.p);
  if (/TwoFactorAuthentication/i.test(u)) await shot(p, '03-2fa-challenge', { fullPage: true });
  else bad.push('03-2fa-challenge: landed on ' + u);
  await c.close();

  // ── admin: user list + row Actions menu ──────────────────────────────────────
  c = await ctx(b, 1000); p = await c.newPage();
  await login(p, ADMIN.u, ADMIN.p);
  await p.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2600);
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
  await shot(p, '07-setup-authenticator', { fullPage: true });
  await c.close();

  await b.close();
  console.log(`\ncaptured ${ok.length}: ${ok.join(', ')}`);
  if (bad.length) { console.log('\nFAILED:'); bad.forEach(x => console.log('  ' + x)); process.exitCode = 1; }
  console.log('\nNOTE: 07-setup-authenticator shows a REAL TOTP secret for ' + PLAIN.u +
              '. Clear it afterwards so the printed QR code cannot enrol anything:');
  console.log("      UPDATE Users SET TwoFactorSecret=NULL WHERE Username='" + PLAIN.u + "' AND TwoFactorEnabled=0;");
})();
