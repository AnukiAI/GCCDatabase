// QC #170 post-deploy verification on gccwebapp.com.
// READ-ONLY effective: we trigger productTypeManager.deleteProductType against
// a type that already has certs attached. The new pre-check (DEF-196) should
// return the specific error JSON; if it's somehow not deployed, the SQL FK
// constraint (OnDelete.Restrict) will block at the DB layer. Either way,
// nothing actually gets deleted.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const GATE_USER = 'gcc';
const GATE_PASS = 'Cert-Test-Access-2026!';
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'TestPass123!';

const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `qc170-prod-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, text) { fs.writeFileSync(path.join(OUT, `${name}.txt`), text || '(empty)'); }
async function shot(page, name, full = false) {
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: full });
  console.log('  shot:', name);
}
async function elShot(locator, name) {
  try {
    await locator.scrollIntoViewIfNeeded({ timeout: 3000 });
    await locator.screenshot({ path: path.join(OUT, `${name}.png`) });
    console.log('  shot (el):', name);
  } catch (e) {
    console.log('  el shot failed for', name, ':', e.message);
  }
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    httpCredentials: { username: GATE_USER, password: GATE_PASS }
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  pageerror:', e.message));

  try {
    // Login
    console.log('[A] Login as admin');
    await page.goto(`${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', ADMIN_USER);
    await page.fill('input[name="Password"]', ADMIN_PASS);
    await Promise.all([
      page.waitForLoadState('networkidle'),
      page.click('button[type="submit"], input[type="submit"]')
    ]);
    console.log('  landed on:', page.url());

    // Admin → Product Types
    console.log('\n[B] /Admin/ProductTypes — Table View');
    await page.goto(`${BASE}/Admin/ProductTypes`);
    await page.waitForLoadState('networkidle');
    await page.locator('button[data-view="table"]').click();
    await page.waitForTimeout(1200);

    const headers = await page.locator('table thead th').allInnerTexts().catch(() => []);
    saveText('00-table-headers', JSON.stringify(headers));
    console.log('  headers:', headers);

    // Snapshot the full table so we can see the count column populated
    await shot(page, '01-product-types-table', true);

    // Scrape every row to find one with a populated Certificates count
    const certColIdx = headers.findIndex(h => /Cert/i.test(h));
    const productColIdx = headers.findIndex(h => /Product/i.test(h));
    if (certColIdx < 0) { console.log('  ! no Certificates column found in headers'); }
    console.log('  Cert col index:', certColIdx, 'Product col index:', productColIdx);

    const allRows = await page.$$eval('table tbody tr', (rows) => rows.map(r => {
      const cells = Array.from(r.querySelectorAll('td')).map(td => td.innerText.trim());
      const delBtn = r.querySelector('.delete-btn');
      const id = delBtn ? delBtn.getAttribute('data-id') : null;
      const isCat = delBtn ? delBtn.getAttribute('data-is-category') : null;
      return { id, isCat, cells };
    }));

    saveText('02-all-rows', JSON.stringify(allRows.slice(0, 80), null, 2));

    // Find a Type-level row (NOT category) with Certificates > 0
    const candidate = allRows.find(r => {
      if (!r.id) return false;
      if (r.isCat === 'true') return false; // skip categories
      const name = r.cells[0] || '';
      if (/Category|^$/.test(name.trim())) return false;
      const certs = parseInt((r.cells[certColIdx] || '').replace(/\D/g, ''), 10);
      return certs > 0;
    });

    if (!candidate) {
      console.log('  ! No populated Type rows found — cannot test delete-error message via existing data');
      saveText('03-no-candidate', 'No type row with >0 certificates found. Email screenshots cannot include the new error message without creating test data.');
      console.log('\nDone. Output:', OUT);
      return;
    }

    console.log('  candidate type:', candidate.id, candidate.cells[0], '| Certs=' + candidate.cells[certColIdx], '| Products=' + candidate.cells[productColIdx]);
    saveText('03-candidate', JSON.stringify(candidate, null, 2));

    // Screenshot the candidate row at full resolution
    const candidateRow = page.locator(`tr:has(.delete-btn[data-id="${candidate.id}"])`).first();
    await elShot(candidateRow, '04-candidate-row');

    // Trigger delete via JS — this is the same call the UI makes after confirm
    console.log('\n[C] Trigger deleteProductType via JS (SQL FK + new pre-check both backstop)');
    const apiResp = await page.evaluate(async (id) => {
      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
      const r = await fetch(`/Admin/DeleteProductType/${id}?isCategory=false`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': token },
        credentials: 'same-origin'
      });
      const j = await r.json().catch(() => null);
      return { status: r.status, body: j };
    }, candidate.id);
    saveText('05-delete-api-response', JSON.stringify(apiResp, null, 2));
    console.log('  API response:', JSON.stringify(apiResp));

    // Also exercise the UI path so we can show the actual modal Carly would see
    console.log('\n[D] UI flow — trigger delete, capture confirm modal, then trigger via JS to capture error modal');
    await page.goto(`${BASE}/Admin/ProductTypes`);
    await page.waitForLoadState('networkidle');
    await page.locator('button[data-view="table"]').click();
    await page.waitForTimeout(1000);

    await page.locator(`.delete-btn[data-id="${candidate.id}"]`).first().click();
    await page.waitForSelector('.admin-modal-overlay button[data-action="confirm"]', { timeout: 5000 });
    await page.waitForTimeout(400);
    const confirmOverlay = page.locator('.admin-modal-overlay').first();
    await elShot(confirmOverlay, '06-confirm-modal');
    saveText('06-confirm-modal', await confirmOverlay.innerText().catch(() => ''));

    // Close confirm modal via Escape so we don't double-fire
    await page.keyboard.press('Escape');
    await page.waitForTimeout(500);

    // Now drive the same productTypeManager.deleteProductType — this is what
    // the UI calls after confirm; it returns the new specific error
    await page.evaluate((id) => window.productTypeManager.deleteProductType(parseInt(id, 10), false), candidate.id);
    await page.waitForFunction(() => {
      const ov = document.querySelector('.admin-modal-overlay');
      if (!ov) return false;
      const t = (ov.innerText || '').toLowerCase();
      return t.includes('cannot delete') || t.includes('error') || t.includes('certificate');
    }, { timeout: 8000 }).catch(() => console.log('  ! error modal never appeared'));

    await page.waitForTimeout(400);
    const errorOverlay = page.locator('.admin-modal-overlay').first();
    const errorText = (await errorOverlay.count()) ? (await errorOverlay.innerText().catch(() => '')) : '(no overlay)';
    saveText('07-error-modal-text', errorText);
    await elShot(errorOverlay, '07-error-modal');
    await shot(page, '07-error-modal-viewport');
    console.log('  ERROR TEXT:', JSON.stringify(errorText.slice(0, 400)));

    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure', true).catch(() => {});
  } finally {
    await browser.close();
  }
})();
