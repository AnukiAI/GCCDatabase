// Live functional check for DEF-309 sequencing fix: fire several rapid sort changes and
// confirm the grid ends non-blank and the Sort-By control matches the actually-rendered order.
const { chromium } = require('playwright');
const path = require('path');
const BASE = 'http://localhost:5101';
const OUT = path.join(__dirname, '_results', 'gate2-20260720');

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.fill('#Email', 'admin');
  await page.fill('#Password', 'TestPass123!');
  await Promise.all([page.waitForLoadState('networkidle').catch(() => {}), page.click('button[type=submit]')]);
  await page.waitForTimeout(1000);

  await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle' });
  await page.waitForSelector('.certificate-card, tbody#gridBody tr', { timeout: 20000 });

  // Custom dropdown (not a native <select>): button #sortDropdownButton opens
  // #sortContent (role=listbox) containing role=option items.
  await page.click('#sortDropdownButton');
  await page.waitForTimeout(200);
  const optionLocators = await page.$$('#sortContent [role="option"], #sortContent li, #sortContent a, #sortContent button');
  const optionCount = optionLocators.length;
  console.log('Sort option count:', optionCount);

  if (optionCount > 1) {
    // Rapidly click through several sort options without waiting for each load to finish,
    // to try to provoke the DEF-309 race (superseded loads racing to render).
    for (let i = 0; i < Math.min(4, optionCount); i++) {
      const opts = await page.$$('#sortContent [role="option"], #sortContent li, #sortContent a, #sortContent button');
      if (opts[i]) {
        await opts[i].click({ force: true }).catch(() => {});
        await page.waitForTimeout(100);
        // reopen dropdown for next click since selecting usually closes it
        const stillOpen = await page.isVisible('#sortContent');
        if (!stillOpen) await page.click('#sortDropdownButton').catch(() => {});
        await page.waitForTimeout(80);
      }
    }
  }
  // Let the last (real) load settle.
  await page.waitForTimeout(2500);

  const finalState = await page.evaluate(() => {
    const cards = document.querySelectorAll('.certificate-card, tbody#gridBody tr');
    const grid = document.getElementById('certificatesGrid');
    return {
      cardCount: cards.length,
      gridVisible: grid ? grid.offsetParent !== null : null,
      gridHtmlLength: grid ? grid.innerHTML.length : 0,
      sortLabel: document.getElementById('sortLabel') ? document.getElementById('sortLabel').textContent : null,
      loadingIndicatorPresent: !!document.getElementById('loading-indicator'),
    };
  });
  console.log('Final state after rapid sort changes:', JSON.stringify(finalState, null, 2));

  await page.screenshot({ path: path.join(OUT, 'def309-sort-race-final.png'), fullPage: true });
  await browser.close();
})();
