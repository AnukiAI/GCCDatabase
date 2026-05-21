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

  page.on('response', r => { if (r.status() >= 400) console.log(`[${r.status()}] ${r.url().slice(0, 120)}`); });

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('analyst');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
  await page.waitForTimeout(2500);

  // Try to open the Edit page for a real cert number
  const certNum = '26-0163';
  console.log(`Opening /Certificate/Edit/${certNum}`);
  const resp = await page.goto(`${BASE}/Certificate/Edit/${certNum}`, { waitUntil: 'networkidle', timeout: 45000 });
  console.log('status:', resp.status());
  console.log('url:', page.url());
  console.log('title:', await page.title());
  await page.waitForTimeout(3000);
  await page.screenshot({ path: path.join(OUT, `probe-edit-${certNum}.png`), fullPage: true });

  // Dump every button + clickable + section heading
  const labels = await page.locator('button, a.btn, h2, h3, h4').evaluateAll(els =>
    els.slice(0, 100).map(e => ({ tag: e.tagName, text: (e.innerText || '').trim().slice(0, 80), id: e.id || null, classes: e.className.slice(0, 60) }))
  );
  console.log('LABELS/BUTTONS:');
  for (const l of labels) if (l.text) console.log(' ', l.tag, l.id || '-', '|', l.text);

  // Also look for product-id-related elements
  const pidElems = await page.locator('[id*="product-id"], [class*="product-id"], [class*="productid"]').evaluateAll(els =>
    els.slice(0, 20).map(e => ({ tag: e.tagName, id: e.id || null, classes: e.className.slice(0, 80) }))
  );
  console.log('PRODUCT-ID-related elements:');
  for (const p of pidElems) console.log(' ', p.tag, p.id, '|', p.classes);

  // Save the full page HTML for inspection
  fs.writeFileSync(path.join(OUT, `edit-${certNum}.html`), await page.content());

  await browser.close();
})();
