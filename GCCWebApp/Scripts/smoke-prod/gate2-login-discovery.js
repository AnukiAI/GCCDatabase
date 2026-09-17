// Discover which seeded test accounts actually work on this local GCCDatabase_Dev instance.
const { chromium } = require('playwright');

const BASE = 'http://localhost:5101';
const PWD = 'TestPass123!';
const candidates = [
  'admin', 'analyst', 'external',
  'jane.analyst@anukiconsulting.com',
  'swest@anukiconsulting.com',
  'analyst@test-gcc.local',
  'external@test-gcc.local',
  'testuser@test-gcc.local',
  'admin@test-gcc.local',
  'admin@anukiconsulting.com',
];

(async () => {
  const browser = await chromium.launch();
  for (const email of candidates) {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await ctx.newPage();
    try {
      await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
      await page.fill('#Email', email);
      await page.fill('#Password', PWD);
      await Promise.all([
        page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {}),
        page.click('button[type=submit]'),
      ]);
      await page.waitForTimeout(1500);
      const url = page.url();
      const bodyText = await page.textContent('body').catch(() => '');
      const loggedIn = !url.includes('/Account/Login');
      console.log(`${email.padEnd(40)} -> ${loggedIn ? 'LOGIN OK' : 'FAILED'}  url=${url}`);
      if (loggedIn) {
        // try to find role/nav hint
        const hint = (bodyText || '').match(/Admin|Analyst|External|Dashboard/gi);
      }
    } catch (e) {
      console.log(`${email.padEnd(40)} -> ERROR ${e.message}`);
    } finally {
      await ctx.close();
    }
  }
  await browser.close();
})();
