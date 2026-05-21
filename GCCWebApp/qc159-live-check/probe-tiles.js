// Discover cert IDs and the Edit URL pattern from the internal tile view.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const BASE = 'https://gccwebapp.com';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // login
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('analyst');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
  await page.waitForTimeout(2500);
  console.log('post-login URL:', page.url());

  // Go to the internal tile view (post-login destination)
  await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle', timeout: 45000 });
  await page.waitForTimeout(4000);

  // Sample HTML of a tile
  const allLinks = await page.locator('a').evaluateAll(els => els.slice(0, 50).map(e => ({ href: e.getAttribute('href'), text: (e.innerText || '').trim().slice(0, 60) })));
  console.log('FIRST 50 LINK HREFS:');
  for (const a of allLinks) if (a.href) console.log(' ', a.href, '|', a.text);

  const dataIds = await page.locator('[data-id], [data-cert-id], [data-certificate-id]').evaluateAll(els =>
    els.slice(0, 20).map(e => ({ dataId: e.getAttribute('data-id'), dataCertId: e.getAttribute('data-cert-id'), dataCertificateId: e.getAttribute('data-certificate-id'), tag: e.tagName }))
  );
  console.log('DATA-IDs:', JSON.stringify(dataIds, null, 2));

  // Capture one tile's outerHTML for structure reference
  const firstTile = page.locator('.certificate-card, .cert-tile, .tile, .card').first();
  if (await firstTile.count() > 0) {
    const oh = await firstTile.evaluate(el => el.outerHTML);
    fs.writeFileSync(path.join(OUT, 'first-tile.html'), oh.slice(0, 4000));
    console.log('first-tile saved (first 4000 chars)');
  } else {
    console.log('no tile selector matched');
  }

  await page.screenshot({ path: path.join(OUT, 'probe-tiles.png'), fullPage: true });
  await browser.close();
})();
