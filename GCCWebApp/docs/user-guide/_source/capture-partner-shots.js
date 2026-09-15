/**
 * Captures the screenshots used in the Partner Setup Guide, from the TEST environment.
 *
 * Run:  NODE_PATH="$(npm root -g)" node capture-partner-shots.js
 * Then: python build-guide.py ../partner-api-setup-guide.md
 *
 * Sibling of capture-screenshots.js (Accounts guide). Kept separate so each guide's shots can be
 * re-taken without re-running the other's.
 *
 * SAFETY — this guide is about credentials, so two rules are enforced here, not left to care:
 *
 *  1. NO REAL API KEY IS EVER CAPTURED. Every value that looks like a key (`gcc_partner_…`) is
 *     blurred before the shutter, and `shot()` refuses to save a PNG if an unblurred key is still
 *     visible in the DOM. Two reasons the blur is needed even though the list "masks" keys: the
 *     mask still shows the first 16 and last 8 characters, and the FULL key is present in each
 *     row's `data-key-value` attribute (it feeds the revoke confirmation), so it can reach the DOM
 *     even where no UI renders it.
 *  2. NO KEY IS GENERATED to take a picture of one. The "key generated" confirmation is therefore
 *     deliberately NOT captured — photographing it would mean creating a live credential on the
 *     test environment first. The guide describes that step in words instead.
 *
 * Other notes kept so they are not rediscovered:
 *  - The access gate serves an HTML FORM, not a 401 challenge, so Playwright's `httpCredentials`
 *    never fires. The Authorization header goes on every request instead.
 *  - Partners uses an INLINE ADD ROW (#partners-add-row), not a modal — clicking Add reveals a row
 *    at the top of the table, so the shot must be of the table, not of a dialog.
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

if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
const ok = [], bad = [];

/** Blur every rendered API key, and anything else credential-shaped. Runs in the page. */
const REDACT_KEYS = () => {
  const KEYISH = /gcc_partner_/i;
  document.querySelectorAll('td, span, code, div, input').forEach(el => {
    if (el.children.length) return;
    const v = el.tagName === 'INPUT' ? (el.value || '') : (el.textContent || '');
    if (KEYISH.test(v)) el.style.filter = 'blur(5px)';
  });
  // Blur email addresses too — same convention as the Accounts guide.
  document.querySelectorAll('td, span').forEach(el => {
    if (!el.children.length && /@[\w.-]+\.\w+/.test(el.textContent || '')) {
      el.style.filter = 'blur(4.5px)';
    }
  });
};

/** True if a key is still legible (rendered, not blurred) anywhere on the page. */
const UNBLURRED_KEY = () => {
  const els = [...document.querySelectorAll('td, span, code, div, input')];
  return els.some(el => {
    if (el.children.length) return false;
    const v = el.tagName === 'INPUT' ? (el.value || '') : (el.textContent || '');
    if (!/gcc_partner_/i.test(v)) return false;
    return !(el.style.filter || '').includes('blur');
  });
};

async function shot(page, name, opts = {}) {
  const title = await page.title();
  if (/gate|restricted access/i.test(title)) { bad.push(`${name}: GATE PAGE — not saved`); return; }
  if (/log ?in|sign ?in/i.test(title))       { bad.push(`${name}: LOGIN PAGE — not saved`); return; }
  await page.evaluate(REDACT_KEYS);
  await page.waitForTimeout(400);
  if (await page.evaluate(UNBLURRED_KEY)) {
    bad.push(`${name}: AN API KEY IS STILL VISIBLE — refused to save`);
    return;
  }
  await page.waitForTimeout(500);
  const f = path.join(OUT, name + '.png');
  await page.screenshot({ path: f, ...opts });
  ok.push(name);
  console.log(`  ${name}.png  ${Math.round(fs.statSync(f).size / 1024)} KB  [${title}]`);
}

/**
 * Shoot one element rather than the whole page, optionally capping the height so a long table
 * becomes a readable excerpt instead of a sliver. `maxH` of 0 means "whole element".
 */
async function clipTo(page, selector, name, maxH) {
  const el = await page.$(selector.split(',')[0].trim())
          || await page.$(selector.split(',').pop().trim());
  if (!el) { bad.push(`${name}: selector not found (${selector})`); return; }
  const box = await el.boundingBox();
  // A null box means the element (or an ancestor) is display:none — a real failure, not a
  // rendering quirk. Say so, rather than saving a picture of the wrong thing.
  if (!box) { bad.push(`${name}: element has no box — hidden? (${selector})`); return; }
  const pad = 12;
  const clip = {
    x: Math.max(0, box.x - pad),
    y: Math.max(0, box.y - pad),
    width: box.width + pad * 2,
    height: maxH ? Math.min(box.height + pad * 2, maxH) : box.height + pad * 2
  };
  // fullPage is required: the clip is page-relative, so an element below the fold throws
  // "clip is outside the viewport" without it.
  await shot(page, name, { fullPage: true, clip });
}

async function login(page, u, p) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[name="Email"]', u);
  await page.fill('input[name="Password"]', p);
  await page.click('button[type="submit"], input[type="submit"]');
  await page.waitForTimeout(1800);
  return page.url();
}

(async () => {
  const b = await chromium.launch();
  // The gate header goes ONLY to the app's own origin. Playwright's `extraHTTPHeaders` applies to
  // every request including cross-origin ones, and Font Awesome is pulled from cdn.jsdelivr.net
  // with crossorigin="anonymous" — forcing an Authorization header onto that request makes the
  // font fail to load, and every icon in the capture renders as an empty grey square. That is why
  // this uses a route handler rather than extraHTTPHeaders.
  const c = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const appOrigin = new URL(BASE).origin;
  await c.route('**/*', (route, req) => {
    route.continue(req.url().startsWith(appOrigin) ? { headers: { ...req.headers(), ...GATE } } : {});
  });
  const p = await c.newPage();

  const landed = await login(p, ADMIN.u, ADMIN.p);
  if (/Account\/Login/i.test(landed)) {
    console.error(`  LOGIN FAILED as ${ADMIN.u} — still on ${landed}`);
    await b.close();
    process.exit(1);
  }
  console.log(`  signed in as ${ADMIN.u} -> ${landed}`);

  // 1. Partners list
  await p.goto(`${BASE}/Admin/Partners`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(1600);
  await shot(p, '20-partners-list', { fullPage: true });

  // 2. Add-partner inline row (NOT a modal)
  await p.click('#partners-add-btn').catch(() => {});
  await p.waitForTimeout(700);
  await shot(p, '21-add-partner', { fullPage: true });

  // 3. Partner API Access. NOT fullPage: this page is ~4900px tall with 24 key rows, and
  //    build-guide.py caps a figure at 6in wide x 7.2in tall — a 1:3.4 image renders as a 2.1in
  //    sliver. Clip to the toolbar and partner cards, which is what the guide actually points at.
  await p.goto(`${BASE}/Admin/PartnerAPI`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2200);
  //    `fullPage` is required alongside `clip`: without it Playwright constrains the clip to the
  //    900px viewport and silently returns a short image.
  await shot(p, '22-partner-api-access', { fullPage: true, clip: { x: 0, y: 0, width: 1440, height: 1180 } });

  // 4. The key table, trimmed to its first rows — for the revoking section. Note this is
  //    #all-api-keys-table; #api-keys-table is the PER-PARTNER table inside
  //    #partner-details-section, which is display:none until a partner is selected and therefore
  //    has no bounding box at all.
  await clipTo(p, '#all-api-keys-table', '24-key-list', 620);

  // 5. Generate API Key dialog — an empty form, so no key exists to leak. Target
  //    .admin-modal-container: #generate-key-modal itself is the full-viewport backdrop.
  await p.click('#generate-key-btn').catch(() => {});
  await p.waitForTimeout(900);
  await clipTo(p, '#generate-key-modal .admin-modal-container', '23-generate-key', 0);

  await b.close();
  console.log(`\n  captured ${ok.length}`);
  if (bad.length) { console.log('  PROBLEMS:'); bad.forEach(x => console.log('   - ' + x)); process.exit(1); }
})();
