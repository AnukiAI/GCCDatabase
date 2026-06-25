// Capture the 3 tile views as they actually render with the current (now-live) CSS.
// Fresh Playwright context = no cache, so we see the real DEF-293 render, not a stale hash.
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');
const path = require('path');
const OUT = path.join(__dirname, '_results', 'tilefix');
require('fs').mkdirSync(OUT, { recursive: true });

const VIEWS = [
  { id: 'public',   url: '/Certificate/CertificateTileView',         role: null },
  { id: 'external', url: '/Certificate/CertificateTileViewExternal', role: 'external' },
  { id: 'internal', url: '/Certificate/CertificateTileViewInternal', role: 'analyst' },
];

(async () => {
  const browser = await chromium.launch({ headless: true });
  for (const v of VIEWS) {
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 1300 }, bypassCSP: true });
    const page = await ctx.newPage();
    try {
      if (v.role) await login(page, v.role);
      await page.goto(BASE + v.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
      // Wait for tiles to populate (JS-driven grid).
      await page.waitForSelector('.certificate-card', { timeout: 30000 }).catch(() => {});
      await page.waitForTimeout(2500);
      const cards = await page.locator('.certificate-card').count();
      // Measure the first 8 cards' heights to detect non-uniformity.
      const heights = await page.evaluate(() => {
        const els = Array.from(document.querySelectorAll('.certificate-card')).slice(0, 8);
        return els.map(e => Math.round(e.getBoundingClientRect().height));
      });
      const shot = path.join(OUT, `${v.id}.png`);
      await page.screenshot({ path: shot });
      console.log(`[${v.id}] cards=${cards} firstRowHeights=${JSON.stringify(heights)} -> ${shot}`);
    } catch (e) {
      console.log(`[${v.id}] ERROR ${e.message}`);
    } finally {
      await ctx.close();
    }
  }
  await browser.close();
})();
