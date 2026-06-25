// QC #199 (DEF-293) — Certificate tile vertical-spacing visual check.
// Captures public + internal(login) tile views at desktop+mobile, screenshots,
// and measures .certificate-card heights (min/median/max) as quantitative
// before/after evidence of condensing. LOCALHOST ONLY.
//
// NOTE: the project lives in a OneDrive folder where ASP.NET's file-change watcher
// is unreliable, so asp-append-version keeps serving a STALE ?v= hash and browsers
// reuse the cached CSS. We bypass that here by routing *.css through a cache-buster
// querystring (the server ignores the query and streams current disk content, but the
// new URL is a fresh cache key). Run:
//   node Scripts/smoke-prod/qc199-tile-spacing-shots.js before
//   node Scripts/smoke-prod/qc199-tile-spacing-shots.js after
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');
const path = require('path');
const fs = require('fs');

const LABEL = (process.argv[2] || 'shot').replace(/[^a-z0-9_-]/gi, '');
const BUST = process.argv.includes('--no-bust') ? false : true;
const OUT = path.join(__dirname, '_results', 'qc199');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

async function newCtx(browser, vp) {
  const ctx = await browser.newContext({ viewport: vp });
  if (BUST) {
    // Append a unique query to every stylesheet request so the browser can't reuse a
    // cached response keyed on the app's stale ?v= hash.
    let n = 0;
    await ctx.route('**/*.css*', (route) => {
      const u = new URL(route.request().url());
      u.searchParams.set('cb', `${Date.now()}_${n++}`);
      route.continue({ url: u.toString() });
    });
  }
  return ctx;
}

async function cardStats(page) {
  return await page.evaluate(() => {
    const cards = [...document.querySelectorAll('.certificate-card')];
    const hs = cards.map(c => Math.round(c.getBoundingClientRect().height)).filter(h => h > 0).sort((a, b) => a - b);
    if (!hs.length) return { n: 0 };
    return { n: hs.length, min: hs[0], median: hs[Math.floor(hs.length / 2)], max: hs[hs.length - 1],
             gridAlign: getComputedStyle(document.querySelector('.certificates-grid')).alignItems };
  });
}

async function shoot(page, view, vp) {
  await page.setViewportSize(vp);
  await page.waitForSelector('.certificate-card', { timeout: 30000 });
  await page.waitForTimeout(900);
  const stats = await cardStats(page);
  const file = path.join(OUT, `${view}-${vp.width}w-${LABEL}.png`);
  await page.screenshot({ path: file, clip: { x: 0, y: 0, width: vp.width, height: Math.min(1700, vp.height) } });
  console.log(`  [${view} @${vp.width}w] cards=${stats.n} min=${stats.min} median=${stats.median} max=${stats.max}px align=${stats.gridAlign} -> ${path.basename(file)}`);
  return { view, vp: vp.width, ...stats };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const out = [];
  try {
    let ctx = await newCtx(browser, { width: 1440, height: 1700 });
    let page = await ctx.newPage();
    await page.goto(BASE + '/Certificate/CertificateTileView', { waitUntil: 'networkidle', timeout: 60000 });
    out.push(await shoot(page, 'public', { width: 1440, height: 1700 }));
    out.push(await shoot(page, 'public', { width: 390, height: 1700 }));
    await ctx.close();

    ctx = await newCtx(browser, { width: 1440, height: 1700 });
    page = await ctx.newPage();
    await login(page, 'admin');
    await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle', timeout: 60000 });
    out.push(await shoot(page, 'internal', { width: 1440, height: 1700 }));
    out.push(await shoot(page, 'internal', { width: 390, height: 1700 }));
    await ctx.close();

    fs.writeFileSync(path.join(OUT, `stats-${LABEL}.json`), JSON.stringify(out, null, 2));
    console.log(`\nWrote stats-${LABEL}.json (cache-bust=${BUST})`);
  } catch (e) {
    console.error('ERROR:', e.message);
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
})();
