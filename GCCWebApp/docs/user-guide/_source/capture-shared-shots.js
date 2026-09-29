/**
 * Verifies the responsive-layout claims and captures the phone-width figure used by the shared
 * "Using the database on a phone or tablet" section.
 *
 * Run: NODE_PATH="$(npm root -g)" node capture-shared-shots.js
 *
 * SHOT DECADE: 7x (shared sections).
 *
 * The claims under test came from an older document and are about LAYOUT, which no code read can
 * confirm — so each width is measured and printed, and the run fails if a claim does not hold.
 */
const { chromium } = require('playwright');
const fs = require('fs'); const path = require('path');
const BASE = process.env.BASE_URL || 'https://db.greencirclecertified.com';
const OUT = path.join(__dirname, 'shots');
const GATE = { Authorization: 'Basic ' + Buffer.from(
  `${process.env.GATE_USER || 'gcc'}:${process.env.GATE_PASS || 'Cert-Test-Access-2026!'}`).toString('base64') };
const bad = [];

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
};

/** Measure what the layout actually does at this width. */
const MEASURE = () => {
  const vis = el => {
    if (!el) return false;
    const cs = getComputedStyle(el);
    const r = el.getBoundingClientRect();
    return cs.display !== 'none' && cs.visibility !== 'hidden'
        && parseFloat(cs.opacity || '1') > 0.05 && r.width > 8 && r.height > 8;
  };
  const q = s => document.querySelector(s);
  return {
    width: window.innerWidth,
    sidebar: vis(q('#filterSidebar')),
    hamburger: vis(q('#mobile-nav-toggle, .mobile-nav-toggle, .hamburger, [aria-label*="navigation" i]')),
    mobileFilterBtn: vis(q('#mobileFilterToggle')),
    horizontalOverflow: document.documentElement.scrollWidth > window.innerWidth + 2
  };
};

(async () => {
  const b = await chromium.launch();
  const c = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const origin = new URL(BASE).origin;
  await c.route('**/*', (r, q) => r.continue(q.url().startsWith(origin) ? { headers: { ...q.headers(), ...GATE } } : {}));
  const p = await c.newPage();
  await p.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await p.fill('input[name="Email"]', process.env.EXT_USER || 'test.external');
  await p.fill('input[name="Password"]', process.env.EXT_PASS || 'GccTest2026!');
  await p.click('button[type="submit"]');
  await p.waitForTimeout(4000);

  const results = {};
  for (const [label, w, h] of [['desktop',1440,900], ['tablet',1024,820], ['mobile',768,900], ['phone',390,844]]) {
    await p.setViewportSize({ width: w, height: h });
    await p.goto(`${BASE}/Certificate/CertificateTileViewExternal`, { waitUntil: 'domcontentloaded' });
    await p.waitForTimeout(3200);
    const m = await p.evaluate(MEASURE);
    results[label] = m;
    console.log(`  ${label.padEnd(8)} ${JSON.stringify(m)}`);
    if (label === 'phone') {
      await p.evaluate(REDACT); await p.waitForTimeout(400);
      const f = path.join(OUT, '70-phone-layout.png');
      await p.screenshot({ path: f });
      console.log(`  70-phone-layout.png  ${Math.round(fs.statSync(f).size/1024)} KB`);
    }
  }

  // The claims, asserted.
  // `#filterSidebar` is the WRONG signal for "are the filters collapsed": the element keeps its
  // size at every width and is repositioned rather than removed, so a visibility test on it
  // reports "expanded" on a phone where the screenshot plainly shows a collapsed control. The
  // honest signal is #mobileFilterToggle — the "Show Filters & Search" button that REPLACES the
  // open sidebar. It is absent at 1440 and present at 1024 and below, which is the claimed
  // breakpoint, measured rather than asserted from an old document.
  if (!results.desktop.sidebar)       bad.push('CLAIM FAILED: filter sidebar not visible at 1440px');
  if (results.desktop.mobileFilterBtn) bad.push('CLAIM FAILED: the collapsed-filters button appears at 1440px');
  if (!results.tablet.mobileFilterBtn) bad.push('CLAIM FAILED: filters do not collapse at 1024px');
  if (!results.phone.mobileFilterBtn)  bad.push('CLAIM FAILED: filters do not collapse at 390px');
  for (const k of Object.keys(results)) {
    if (results[k].horizontalOverflow) bad.push(`CLAIM FAILED: ${k} layout scrolls horizontally`);
  }
  await b.close();
  if (bad.length) { console.log('\n  PROBLEMS:'); bad.forEach(x => console.log('   - ' + x)); process.exit(1); }
  console.log('\n  all responsive claims held');
})();
