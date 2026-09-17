const { chromium } = require('playwright');
const path = require('path');
const BASE = 'http://localhost:5101';
const PWD = 'TestPass123!';
const OUT = path.join(__dirname, '_results', 'gate2-20260720');

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.fill('#Email', 'external');
  await page.fill('#Password', PWD);
  await Promise.all([page.waitForLoadState('networkidle').catch(() => {}), page.click('button[type=submit]')]);
  await page.waitForTimeout(1000);

  await page.goto(BASE + '/Product/Management', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);

  const headers = await page.$$eval('th', ths => ths.map(th => th.textContent.trim()));
  console.log('Headers:', JSON.stringify(headers));

  const actionsColInfo = await page.evaluate(() => {
    const ths = Array.from(document.querySelectorAll('th'));
    const idx = ths.findIndex(th => /actions/i.test(th.textContent));
    if (idx === -1) return { found: false };
    const th = ths[idx];
    const rect = th.getBoundingClientRect();
    // find a sample cell in that column from first row
    const table = th.closest('table');
    const firstRow = table ? table.querySelector('tbody tr') : null;
    const cell = firstRow ? firstRow.children[idx] : null;
    return {
      found: true,
      index: idx,
      visible: rect.width > 0 && rect.height > 0,
      headerHtml: th.outerHTML.slice(0, 200),
      sampleCellHtml: cell ? cell.outerHTML.slice(0, 300) : null,
    };
  });
  console.log('Actions column info:', JSON.stringify(actionsColInfo, null, 2));

  await page.screenshot({ path: path.join(OUT, 'external__product-management-check.png'), fullPage: true });
  await browser.close();
})();
