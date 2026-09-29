/**
 * Captures the screenshots used in the Administrator Guide, from the TEST environment.
 *
 * Run:  NODE_PATH="$(npm root -g)" node capture-admin-shots.js
 * Then: python build-guide.py ../administrator-guide.md
 *
 * SHOT DECADE: 6x. (0x-1x Accounts, 2x Partner, 4x Company, 5x Analyst.)
 *
 * SIGNED IN AS admin.uat — the Administration menu is the subject of this guide, so unlike the
 * analyst capture the admin affordances are what we are here to photograph. The guard is inverted:
 * `shot()` refuses to save if the Administration menu is ABSENT, because that would mean the
 * session is not an admin one and every shot would be of the wrong product.
 *
 * NOTHING IS EVER SAVED OR DELETED. These are the client's real configuration pages on the test
 * environment. No submit, no delete, no confirmation dialog is ever clicked — several of these
 * pages carry destructive controls sitting right beside the ones worth photographing.
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
const USER = { u: process.env.ADMIN_USER || 'admin.uat', p: process.env.ADMIN_PASS || 'TestPass123!' };

if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
const ok = [], bad = [];

const REDACT = () => {
  document.querySelectorAll('td, span, div, a, p').forEach(el => {
    if (el.children.length) return;
    if (/@[\w.-]+\.\w+/.test(el.textContent || '')) el.style.filter = 'blur(4.5px)';
  });
  [...document.querySelectorAll('div, header, section, p')]
    .filter(el => /TEST ENVIRONMENT/i.test(el.textContent || '') && el.textContent.length < 160)
    .forEach(el => { el.style.display = 'none'; });
  document.querySelectorAll('span, div, a, p, td').forEach(el => {
    if (el.children.length) return;
    if (/^\s*(test\.\w+|admin\.uat|2fatest)/i.test(el.textContent || '')) el.textContent = 'your.account';
  });
  // API keys and anything key-shaped, same rule as the Partner guide.
  document.querySelectorAll('td, span, code, div, input').forEach(el => {
    if (el.children.length) return;
    const v = el.tagName === 'INPUT' ? (el.value || '') : (el.textContent || '');
    if (/gcc_partner_/i.test(v)) el.style.filter = 'blur(5px)';
  });
};

/** Inverted guard: this guide REQUIRES an admin session. Absence of the menu is the failure. */
const ADMIN_SESSION_PRESENT = () => {
  const shown = el => el && el.offsetParent !== null;
  return [...document.querySelectorAll('a.nav-link, .nav-link')]
    .some(a => /administration/i.test(a.textContent || '') && shown(a));
};

async function shot(page, name, opts = {}) {
  const title = await page.title();
  if (/gate|restricted access/i.test(title)) { bad.push(`${name}: GATE PAGE — not saved`); return; }
  if (/log ?in|sign ?in/i.test(title))       { bad.push(`${name}: LOGIN PAGE — not saved`); return; }
  if (!(await page.evaluate(ADMIN_SESSION_PRESENT))) {
    bad.push(`${name}: NO ADMINISTRATION MENU — not an admin session, refused to save`);
    return;
  }
  await page.evaluate(REDACT);
  await page.waitForTimeout(400);
  const f = path.join(OUT, name + '.png');
  await page.screenshot({ path: f, ...opts });
  ok.push(name);
  console.log(`  ${name}.png  ${Math.round(fs.statSync(f).size / 1024)} KB  [${title}]`);
}

async function clipTo(page, selector, name, maxH) {
  const el = await page.$(selector);
  if (!el) { bad.push(`${name}: selector not found (${selector})`); return; }
  const box = await el.boundingBox();
  if (!box) { bad.push(`${name}: element has no box — hidden? (${selector})`); return; }
  const pad = 14;
  await shot(page, name, {
    fullPage: true,
    clip: {
      x: Math.max(0, box.x - pad), y: Math.max(0, box.y - pad),
      width: box.width + pad * 2,
      height: maxH ? Math.min(box.height + pad * 2, maxH) : box.height + pad * 2
    }
  });
}

async function login(page, u, p) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[name="Email"]', u);
  await page.fill('input[name="Password"]', p);
  await page.click('button[type="submit"], input[type="submit"]');
  await page.waitForTimeout(1800);
  return page.url();
}

/** Go to an admin page and shoot its main card, below the hero banner. */
async function adminPage(p, url, name, selector, maxH) {
  await p.goto(`${BASE}${url}`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2600);
  const t = await p.title();
  if (/error|not found|bad request/i.test(t)) { bad.push(`${name}: unexpected page "${t}" at ${url}`); return; }
  // An EMPTY title is a failure too. `/Company` (no action) renders a completely blank page with no
  // title and no content; the original check only looked for error words, so the blank page sailed
  // through and the run reported the misleading "selector not found" instead of "the page is empty".
  if (!t.trim()) { bad.push(`${name}: BLANK PAGE (no title, no content) at ${url}`); return; }
  // Refuse to save a page with a navigation dropdown hanging over it.
  await p.mouse.move(20, 860);
  await p.waitForTimeout(500);
  // `offsetParent !== null` is NOT a visibility test here: the submenus stay `display: block` with a
  // real height and are hidden with `visibility: hidden; opacity: 0`. Testing offsetParent rejected
  // all seven pages on a run where the menus were in fact closed. Read the computed style.
  const menuOpen = await p.evaluate(() => [...document.querySelectorAll('.nested-submenu, .submenu, .dropdown-menu')]
    .some(el => {
      const cs = getComputedStyle(el);
      return cs.display !== 'none'
          && cs.visibility !== 'hidden'
          && parseFloat(cs.opacity || '1') > 0.05
          && el.getBoundingClientRect().height > 40;
    }));
  if (menuOpen) { bad.push(`${name}: a navigation dropdown is open over the page — not saved`); return; }
  if (selector) await clipTo(p, selector, name, maxH);
  else await shot(p, name, { fullPage: false });
}

(async () => {
  const b = await chromium.launch();
  const c = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const appOrigin = new URL(BASE).origin;
  await c.route('**/*', (route, req) => {
    route.continue(req.url().startsWith(appOrigin) ? { headers: { ...req.headers(), ...GATE } } : {});
  });
  const p = await c.newPage();

  const landed = await login(p, USER.u, USER.p);
  if (/Account\/Login/i.test(landed)) {
    console.error(`  LOGIN FAILED as ${USER.u} — still on ${landed}`); await b.close(); process.exit(1);
  }
  console.log(`  signed in as ${USER.u} -> ${landed}`);

  // 1. The Administration menu, opened. This is the map the guide is organised around, so it has
  //    to show the real menu rather than a description of it.
  await p.waitForTimeout(2200);
  const adminNav = await p.$('a.nav-link:has-text("Administration"), .nav-link:has-text("Administration")');
  if (adminNav) {
    await adminNav.hover();
    await adminNav.click().catch(() => {});
    await p.waitForTimeout(1200);
    await shot(p, '60-administration-menu', { fullPage: false });
  } else { bad.push('60-administration-menu: Administration nav link not found'); }

  // Park the pointer away from the navigation. The menu opens on HOVER, and the cursor stays where
  // it was left across navigations — so every page captured after this one had the Administration
  // dropdown floating over the top of its card. The overlay looked like part of the page.
  await p.mouse.move(20, 860);
  await p.waitForTimeout(700);

  // 2-8. One card per configuration page.
  await adminPage(p, '/Company/Index',        '61-companies',         '.admin-container', 1150);
  await adminPage(p, '/Admin/ProductTypes',   '62-product-types',     '.admin-container', 1150);
  await adminPage(p, '/Admin/CertificateTypes','63-certificate-types','.admin-container', 1150);
  await adminPage(p, '/Admin/Tags',           '64-tags',              '.admin-container', 1150);
  await adminPage(p, '/Admin/RoleManagement', '65-role-management',   '.admin-container', 1150);
  await adminPage(p, '/Admin/Pathways',       '66-pathways',          '.admin-container', 1150);
  await adminPage(p, '/Dashboard',            '67-dashboard',         null);

  // PROVE THE GUARD CAN FAIL: the inverted check must reject a NON-admin session, or a set of
  // shots taken as the wrong role would sail through as "clean".
  const anonCtx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  await anonCtx.route('**/*', (route, req) => {
    route.continue(req.url().startsWith(appOrigin) ? { headers: { ...req.headers(), ...GATE } } : {});
  });
  const np = await anonCtx.newPage();
  await login(np, process.env.ANALYST_USER || 'test.analyst', process.env.ANALYST_PASS || 'GccTest2026!');
  await np.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
  await np.waitForTimeout(2500);
  if (await np.evaluate(ADMIN_SESSION_PRESENT)) {
    bad.push('GUARD SELF-TEST FAILED: the admin-session check passed on an ANALYST session, so it '
           + 'cannot prove these shots were taken as an administrator.');
  } else {
    console.log('\n  guard self-test PASSED — rejected the analyst session (no Administration menu)');
  }
  await anonCtx.close();

  await b.close();
  console.log(`\n  captured ${ok.length}`);
  if (bad.length) { console.log('  PROBLEMS:'); bad.forEach(x => console.log('   - ' + x)); process.exit(1); }
})();
