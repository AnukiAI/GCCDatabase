// Why does Edit show 0 category options on prod? Inspect page state.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `testtest-edit-deep-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, t) { fs.writeFileSync(path.join(OUT, `${name}.txt`), t || '(empty)'); }
async function shot(page, name) { await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: true }); console.log('  shot:', name); }

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    httpCredentials: { username: 'gcc', password: 'Cert-Test-Access-2026!' }
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  pageerror:', e.message));
  page.on('console', msg => { if (/error|warn/i.test(msg.type())) console.log('  console:', msg.type(), msg.text()); });

  try {
    await page.goto(`${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', 'admin');
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([page.waitForLoadState('networkidle'), page.click('button[type="submit"]')]);

    // First, find an actual cert number on prod by visiting the tile view
    console.log('[A] Find a real cert number on prod');
    await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2500);
    const certNumbers = await page.$$eval('[data-cert-number], .cert-number, .certificate-number', els =>
      els.slice(0, 5).map(e => e.getAttribute('data-cert-number') || e.textContent.trim())).catch(() => []);
    // Also try generic regex on visible text
    const visibleText = await page.locator('body').innerText().catch(() => '');
    const certMatches = (visibleText.match(/\d{2}-\d{4}/g) || []).slice(0, 5);
    console.log('  cert numbers spotted:', JSON.stringify(certMatches));
    saveText('00-cert-numbers-found', JSON.stringify({ certNumbers, certMatches }));

    const target = certMatches[0] || '26-0001';
    console.log('  using target:', target);

    // Visit Edit for that cert
    console.log(`\n[B] /Certificate/Edit/${target}`);
    const resp = await page.goto(`${BASE}/Certificate/Edit/${target}`);
    console.log('  status:', resp ? resp.status() : '?', 'url after nav:', page.url());
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2500);
    await shot(page, '01-edit-page');

    // What's on the page?
    const title = await page.title();
    const h1 = await page.locator('h1').first().innerText().catch(() => '');
    console.log('  title:', title);
    console.log('  h1:', h1);
    saveText('02-page-meta', `title: ${title}\nh1: ${h1}\nurl: ${page.url()}`);

    // Inspect #ProductCategoryId
    const catState = await page.evaluate(() => {
      const sel = document.querySelector('#ProductCategoryId');
      if (!sel) return { error: 'no #ProductCategoryId in DOM' };
      return {
        tagName: sel.tagName,
        name: sel.name,
        value: sel.value,
        optionCount: sel.options.length,
        options: Array.from(sel.options).map(o => ({ value: o.value, text: o.text })),
        isDisplayed: sel.offsetParent !== null,
        parentClasses: sel.parentElement?.className,
        outerHtml: sel.outerHTML.slice(0, 500)
      };
    });
    saveText('03-ProductCategoryId-state', JSON.stringify(catState, null, 2));
    console.log('  #ProductCategoryId state — options:', catState.optionCount, 'displayed:', catState.isDisplayed);
    console.log('  first 5 options:', JSON.stringify((catState.options || []).slice(0, 5)));

    // Check if the form is in a hidden state or accordion or something
    const allCatSelects = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('select')).map(s => ({
        id: s.id,
        name: s.name,
        optionCount: s.options.length,
        visible: s.offsetParent !== null
      }));
    });
    saveText('04-all-selects', JSON.stringify(allCatSelects, null, 2));

    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure').catch(() => {});
  } finally {
    await browser.close();
  }
})();
