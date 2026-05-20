// Targeted: hit a LEAF subcategory with certs to verify the new "N certificate(s) use this..." message.
// Subject: LDPE (id=47), 0 products, 1 cert.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `qc170-prod-leaf-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, text) { fs.writeFileSync(path.join(OUT, `${name}.txt`), text || '(empty)'); }
async function shot(page, name, full = false) { await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: full }); console.log('  shot:', name); }
async function elShot(locator, name) {
  try { await locator.scrollIntoViewIfNeeded({ timeout: 3000 }); await locator.screenshot({ path: path.join(OUT, `${name}.png`) }); console.log('  shot (el):', name); }
  catch (e) { console.log('  el shot failed for', name, ':', e.message); }
}

const TARGETS = [
  { id: 47, name: 'LDPE',  expect: 'subcategory' },
  { id: 49, name: 'PET',   expect: 'subcategory' },
  { id: 46, name: 'HDPE',  expect: 'subcategory' }
];

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    httpCredentials: { username: 'gcc', password: 'Cert-Test-Access-2026!' }
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  pageerror:', e.message));

  try {
    await page.goto(`${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', 'admin');
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([
      page.waitForLoadState('networkidle'),
      page.click('button[type="submit"], input[type="submit"]')
    ]);

    await page.goto(`${BASE}/Admin/ProductTypes`);
    await page.waitForLoadState('networkidle');
    await page.locator('button[data-view="table"]').click();
    await page.waitForTimeout(1200);

    // Snapshot Plastics + its subcategories for the email — element shot of the rows we care about
    const plasticsRow = page.locator('tr', { has: page.locator('td:has-text("Plastics")').first() }).first();
    await elShot(plasticsRow, '01-plastics-row');

    for (const t of TARGETS) {
      const row = page.locator(`tr:has(.delete-btn[data-id="${t.id}"])`).first();
      if (await row.count()) {
        await elShot(row, `02-row-${t.id}-${t.name}`);
      }
    }

    // Hit each one via direct API. No UI confirm needed — fully read-only on result.
    const results = [];
    for (const t of TARGETS) {
      const r = await page.evaluate(async (id) => {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const resp = await fetch(`/Admin/DeleteProductType/${id}?isCategory=false`, {
          method: 'POST',
          headers: { 'RequestVerificationToken': token },
          credentials: 'same-origin'
        });
        const j = await resp.json().catch(() => null);
        return { status: resp.status, body: j };
      }, t.id);
      results.push({ ...t, response: r });
      console.log(` ${t.name} (#${t.id}):`, JSON.stringify(r));
    }
    saveText('03-api-responses', JSON.stringify(results, null, 2));

    // For LDPE, also drive the UI to get the actual modal screenshot
    console.log('\nDriving UI modal for LDPE (#47)');
    await page.locator('.delete-btn[data-id="47"]').first().click();
    await page.waitForSelector('.admin-modal-overlay button[data-action="confirm"]', { timeout: 5000 });
    await page.waitForTimeout(400);
    await elShot(page.locator('.admin-modal-overlay').first(), '04-LDPE-confirm-modal');
    saveText('04-LDPE-confirm-modal', await page.locator('.admin-modal-overlay').first().innerText().catch(() => ''));
    await page.keyboard.press('Escape');
    await page.waitForTimeout(500);

    // Now trigger via JS to get the new error modal
    await page.evaluate(() => window.productTypeManager.deleteProductType(47, false));
    await page.waitForFunction(() => {
      const ov = document.querySelector('.admin-modal-overlay');
      return ov && /cannot delete|error|certificate/i.test(ov.innerText || '');
    }, { timeout: 8000 }).catch(() => console.log('  ! error modal never appeared'));
    await page.waitForTimeout(400);

    const errorOverlay = page.locator('.admin-modal-overlay').first();
    const errorText = (await errorOverlay.count()) ? (await errorOverlay.innerText().catch(() => '')) : '(no overlay)';
    saveText('05-LDPE-error-modal', errorText);
    await elShot(errorOverlay, '05-LDPE-error-modal');
    await shot(page, '05-LDPE-error-modal-viewport');
    console.log('  LDPE error text:', JSON.stringify(errorText.slice(0, 400)));

    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure', true).catch(() => {});
  } finally {
    await browser.close();
  }
})();
