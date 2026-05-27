// Quick visual verification of the new dashboard table totals (DEF-221).
const { chromium } = require('playwright');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 1800 },
  });
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(2000);

  await page.goto(BASE + '/Dashboard', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3000); // animation settle

  // Capture footer rows
  const totals = await page.locator('tfoot.dashboard-table-total').evaluateAll(els =>
    els.map(el => {
      const tbl = el.closest('.table-section');
      const title = (tbl?.querySelector('.table-title')?.innerText || '').trim();
      const cells = Array.from(el.querySelectorAll('td')).map(td => td.innerText.trim());
      return { title, footerLabel: cells[0], footerValue: cells[1] };
    })
  );

  console.log(`\nDashboard table totals (DEF-221):`);
  totals.forEach(t => console.log(`  ${t.title.padEnd(50)} → ${t.footerLabel}: ${t.footerValue}`));

  await page.screenshot({ path: path.join(OUT, 'qc221-local-tables-with-totals.png'), fullPage: true });
  console.log(`\nScreenshot: qc221-local-tables-with-totals.png`);

  await browser.close();
})();
