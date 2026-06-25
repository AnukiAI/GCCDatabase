// QC #199 (DEF-292) — Certificate tile vertical-spacing visual check.
// Captures public + internal(login) tile views at desktop+mobile, screenshots,
// and measures .certificate-card heights (min/median/max) as quantitative
// before/after evidence of condensing. LOCALHOST ONLY.
// Run: node Scripts/smoke-prod/qc199-tile-spacing-shots.js before
//      node Scripts/smoke-prod/qc199-tile-spacing-shots.js after
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');
const path = require('path');
const fs = require('fs');

const LABEL = (process.argv[2] || 'shot').replace(/[^a-z0-9_-]/gi, '');
const OUT = path.join(__dirname, '_results', 'qc199');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

async function cardStats(page) {
  return await page.evaluate(() => {
    const cards = [...document.querySelectorAll('.certificate-card')];
    const hs = cards.map(c => Math.round(c.getBoundingClientRect().height)).filter(h => h > 0).sort((a, b) => a - b);
    if (!hs.length) return { n: 0 };
    return { n: hs.length, min: hs[0], median: hs[Math.floor(hs.length / 2)], max: hs[hs.length - 1] };
  });
}

async function shoot(page, view, vp) {
  await page.setViewportSize(vp);
  await page.waitForSelector('.certificate-card', { timeout: 30000 });
  await page.waitForTimeout(800); // let lazy images/layout settle
  const stats = await cardStats(page);
  const file = path.join(OUT, `${view}-${vp.width}w-${LABEL}.png`);
  await page.screenshot({ path: file, clip: { x: 0, y: 0, width: vp.width, height: Math.min(1700, vp.height) } });
  console.log(`  [${view} @${vp.width}w] cards=${stats.n} min=${stats.min} median=${stats.median} max=${stats.max}px -> ${path.basename(file)}`);
  return { view, vp: vp.width, ...stats };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const out = [];
  try {
    // ---- Public (logged-out) ----
    let ctx = await browser.newContext({ viewport: { width: 1440, height: 1700 } });
    let page = await ctx.newPage();
    await page.goto(BASE + '/Certificate/CertificateTileView', { waitUntil: 'networkidle', timeout: 60000 });
    out.push(await shoot(page, 'public', { width: 1440, height: 1700 }));
    out.push(await shoot(page, 'public', { width: 390, height: 1700 }));
    await ctx.close();

    // ---- Internal (logged-in admin: Status + Partner rows) ----
    ctx = await browser.newContext({ viewport: { width: 1440, height: 1700 } });
    page = await ctx.newPage();
    await login(page, 'admin');
    await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle', timeout: 60000 });
    out.push(await shoot(page, 'internal', { width: 1440, height: 1700 }));
    out.push(await shoot(page, 'internal', { width: 390, height: 1700 }));
    await ctx.close();

    fs.writeFileSync(path.join(OUT, `stats-${LABEL}.json`), JSON.stringify(out, null, 2));
    console.log(`\nWrote stats-${LABEL}.json`);
  } catch (e) {
    console.error('ERROR:', e.message);
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
})();
