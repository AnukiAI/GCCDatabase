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

  page.on('console', m => { if (m.type() === 'error') console.log('[browser err]', m.text().slice(0, 200)); });
  page.on('requestfailed', r => console.log('[req fail]', r.url(), '-', r.failure()?.errorText));
  page.on('response', r => {
    if (r.status() >= 400) console.log(`[${r.status()}] ${r.url()}`);
  });

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('analyst');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
  await page.waitForTimeout(2500);

  await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3500);

  // Find a "Edit" or "View" anchor inside a tile and click it (real user path)
  // Print all relative-href anchors that contain a numeric segment
  const candidates = await page.locator('a[href*="/Certificate/"]').evaluateAll(els =>
    els.map(e => ({ href: e.getAttribute('href'), text: (e.innerText || '').trim().slice(0, 40) }))
  );
  console.log('CERTIFICATE LINKS:');
  for (const a of candidates.slice(0, 40)) console.log(' ', a.href, '|', a.text);

  // Look for buttons/icons inside tiles
  const inTileBtns = await page.locator('.certificate-card a, .cert-tile a, [data-id] a, [data-id] button').evaluateAll(els =>
    els.slice(0, 20).map(e => ({ tag: e.tagName, href: e.getAttribute('href'), onclick: e.getAttribute('onclick'), text: (e.innerText || '').trim().slice(0, 40) }))
  );
  console.log('TILE-INTERNAL ACTIONS:');
  for (const b of inTileBtns) console.log(' ', b.tag, b.href || b.onclick, '|', b.text);

  // Try going to one Edit url and see what loads
  console.log('\n--- Trying /Certificate/Edit/76 ---');
  const resp = await page.goto(BASE + '/Certificate/Edit/76', { waitUntil: 'domcontentloaded', timeout: 30000 });
  console.log('status:', resp.status());
  console.log('url:', page.url());
  console.log('title:', await page.title());
  await page.waitForTimeout(2500);
  await page.screenshot({ path: path.join(OUT, 'probe-edit-76.png'), fullPage: true });

  // Dump key buttons / sections present
  const btns = await page.locator('button, a.btn').evaluateAll(els =>
    els.slice(0, 60).map(e => ({ tag: e.tagName, text: (e.innerText || '').trim().slice(0, 50), id: e.id || null }))
  );
  console.log('BUTTONS on Edit page:');
  for (const b of btns) if (b.text) console.log(' ', b.tag, b.id || '-', '|', b.text);

  await browser.close();
})();
