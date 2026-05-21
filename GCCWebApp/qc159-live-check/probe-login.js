// Probe the login page DOM to find correct selectors + behavior.
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

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle', timeout: 45000 });
  await page.waitForTimeout(2500);

  console.log('URL:', page.url());
  console.log('TITLE:', await page.title());

  // dump all input fields
  const inputs = await page.locator('input').evaluateAll(els =>
    els.map(e => ({ id: e.id, name: e.name, type: e.type, placeholder: e.placeholder, ariaLabel: e.getAttribute('aria-label') }))
  );
  console.log('INPUTS:', JSON.stringify(inputs, null, 2));

  const buttons = await page.locator('button, input[type=submit]').evaluateAll(els =>
    els.map(e => ({ id: e.id, type: e.type, text: (e.innerText || e.value || '').trim().slice(0, 60) }))
  );
  console.log('BUTTONS:', JSON.stringify(buttons, null, 2));

  // save raw HTML
  const html = await page.content();
  fs.writeFileSync(path.join(OUT, 'login-page.html'), html);
  await page.screenshot({ path: path.join(OUT, 'probe-login.png'), fullPage: true });

  await browser.close();
})();
