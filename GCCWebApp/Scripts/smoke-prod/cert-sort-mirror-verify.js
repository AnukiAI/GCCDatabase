// Verifies the DEF-309 mirror (dropped-sort fix + repaint nudge) on the EXTERNAL and PUBLIC
// certificate tile views. Confirms: (a) the JS still parses & the page renders (a syntax error
// would leave the grid empty), (b) sorting still renders, (c) a rapid double-sort leaves a
// correctly populated grid with no page errors (the supersede path). LOCALHOST ONLY.
// Run: node Scripts/smoke-prod/cert-sort-mirror-verify.js

const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';

const VIEWS = [
  { id: 'public', url: '/Certificate/CertificateTileView', login: null, api: '/api/certificate-tiles' },
  { id: 'external', url: '/Certificate/CertificateTileViewExternal', login: { email: 'external', password: 'TestPass123!' }, api: '/api/CertificateTileExternal' },
];

const results = [];
function rec(view, id, status, detail) {
  results.push({ view, id, status });
  console.log(`  [${status}] ${view} :: ${id}${detail ? ' — ' + detail : ''}`);
}

async function login(page, creds) {
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('#Email').fill(creds.email);
  await page.locator('#Password').fill(creds.password);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(700);
}

const gridCount = (page) => page.evaluate(() => {
  const g = document.getElementById('certificatesGrid');
  const b = document.getElementById('gridBody');
  return { grid: g ? g.children.length : -1, body: b ? b.children.length : -1 };
});

async function run() {
  const browser = await chromium.launch({ headless: process.env.HEADLESS !== '0' });
  for (const v of VIEWS) {
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await ctx.newPage();
    const pageErrors = [];
    page.on('pageerror', (e) => pageErrors.push(e.message));
    page.on('console', (m) => { if (m.type() === 'error' && !/favicon|ResizeObserver loop|404/i.test(m.text())) pageErrors.push('console:' + m.text()); });
    console.log(`\n[${v.id}] ${v.url}`);
    try {
      if (v.login) await login(page, v.login);
      await page.goto(BASE + v.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForResponse(r => r.url().includes(v.api), { timeout: 30000 }).catch(() => null);
      await page.waitForTimeout(900);

      const initial = await gridCount(page);
      rec(v.id, 'initial-render', initial.grid > 0 ? 'PASS' : 'FAIL', `grid=${initial.grid} body=${initial.body}`);

      // Which sort options exist? Use this view's own options (public omits oldest-effective).
      const opts = await page.evaluate(() => [...document.querySelectorAll('[data-sort]')].map(o => o.dataset.sort));
      rec(v.id, 'sort-options', opts.length > 0 ? 'PASS' : 'FAIL', `${opts.length} options`);

      if (opts.length >= 2) {
        const sortA = opts.find(o => /oldest|newest|az|cert-number|company/.test(o)) || opts[0];
        const sortB = opts.find(o => o !== sortA) || opts[1];
        // Apply a single sort.
        const p1 = page.waitForResponse(r => r.url().includes(v.api), { timeout: 20000 }).catch(() => null);
        await page.evaluate((s) => document.querySelector(`[data-sort="${s}"]`)?.click(), sortA);
        await p1; await page.waitForTimeout(700);
        const afterSort = await gridCount(page);
        rec(v.id, `after-sort(${sortA})`, (afterSort.grid > 0 || afterSort.body > 0) ? 'PASS' : 'FAIL', `grid=${afterSort.grid} body=${afterSort.body}`);

        // Rapid double-sort (supersede path): sortB then sortA back-to-back — grid must stay populated.
        await page.evaluate(([a, b]) => {
          document.querySelector(`[data-sort="${b}"]`)?.click();
          document.querySelector(`[data-sort="${a}"]`)?.click();
        }, [sortA, sortB]);
        await page.waitForTimeout(2500);
        const afterRapid = await gridCount(page);
        rec(v.id, `after-rapid-supersede(${sortB}->${sortA})`, (afterRapid.grid > 0 || afterRapid.body > 0) ? 'PASS' : 'FAIL', `grid=${afterRapid.grid} body=${afterRapid.body}`);
      }

      rec(v.id, 'no-page-errors', pageErrors.length === 0 ? 'PASS' : 'FAIL', pageErrors.slice(0, 3).join(' || '));
    } catch (e) {
      rec(v.id, 'run', 'FAIL', e.message);
    }
    await ctx.close();
  }
  await browser.close();
  const fails = results.filter(r => r.status === 'FAIL');
  console.log(`\n[mirror-verify] ${results.filter(r => r.status === 'PASS').length} PASS / ${fails.length} FAIL`);
  process.exit(fails.length ? 1 : 0);
}
run();
