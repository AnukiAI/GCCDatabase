'use strict';
const { chromium } = require('playwright');
const BASE = 'http://localhost:5101';
const path = require('path');
const SCREENSHOTS_DIR = path.join(__dirname, '_results', 'screenshots');
const fs = require('fs');

(async () => {
  const b   = await chromium.launch({ headless: true });
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Login as external
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.locator('#Email').fill('external');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);

  await page.goto(BASE + '/Certificate/CertificateTileViewExternal', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);

  // Get the .tc-actions element's full outer HTML, visibility, and context
  const els = page.locator('.tc-actions');
  const count = await els.count();
  console.log('.tc-actions count:', count);

  for (let i = 0; i < count; i++) {
    const el = els.nth(i);
    const html = await el.evaluate(n => n.outerHTML);
    const vis = await el.isVisible();
    const computed = await el.evaluate(n => {
      const s = window.getComputedStyle(n);
      return {
        display: s.display,
        visibility: s.visibility,
        opacity: s.opacity,
      };
    });
    const parentTag = await el.evaluate(n => n.parentElement ? n.parentElement.tagName : 'none');
    console.log(`\n.tc-actions[${i}]: visible=${vis} display=${computed.display} vis=${computed.visibility} parent=${parentTag}`);
    console.log('  HTML:', html.slice(0, 300));
  }

  // Also check: does "text='Actions'" appear in any visible element?
  const allText = await page.locator('text="Actions"').all();
  for (const el of allText) {
    const vis = await el.isVisible();
    const tag = await el.evaluate(n => n.tagName);
    const cls = await el.getAttribute('class') || '';
    console.log(`\ntext="Actions": tag=${tag} class="${cls}" visible=${vis}`);
  }

  // Screenshot the relevant area
  if (!fs.existsSync(SCREENSHOTS_DIR)) fs.mkdirSync(SCREENSHOTS_DIR, { recursive: true });
  await page.screenshot({ path: path.join(SCREENSHOTS_DIR, 'gate2-a2-ext-tile-1440.png'), fullPage: true });
  console.log('\nScreenshot saved: gate2-a2-ext-tile-1440.png');

  await ctx.close();
  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
