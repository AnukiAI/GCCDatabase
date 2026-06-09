// One-off read-only live probe (DEF-256 follow-up / QC #186 part 3):
// Does live /Product/GetProducts return productTypeName/productSubcategoryName for the
// "Test Brava Product" (client screenshot shows table blank but edit form = Plastics/PVC)?
// Run: $env:BASE_URL='https://gccwebapp.com'; node Scripts/smoke-prod/probe-brava-producttype.js
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  let captured = null;
  page.on('response', async (resp) => {
    if (resp.url().includes('/Product/GetProducts')) {
      try { captured = { status: resp.status(), json: await resp.json() }; } catch { /* not json */ }
    }
  });
  try {
    await login(page, 'admin');
    await page.goto(BASE + '/Product/Management', { waitUntil: 'networkidle', timeout: 120000 });
    await page.waitForTimeout(2000);
    console.log('BASE =', BASE);
    if (!captured) { console.log('No GetProducts response captured.'); }
    else {
      const arr = captured.json.data || captured.json;
      console.log(`GetProducts HTTP ${captured.status}  count=${Array.isArray(arr) ? arr.length : 'n/a'}`);
      const brava = (arr || []).filter(p => /brava/i.test(p.name || ''));
      console.log('--- brava rows (raw API) ---');
      brava.forEach(p => console.log(`  id=${p.id} name='${p.name}' typeId=${p.productTypeId} typeName='${p.productTypeName}' subId=${p.productSubcategoryId} subName='${p.productSubcategoryName}'`));
      const withType = (arr || []).filter(p => p.productTypeId);
      console.log(`--- products WITH typeId set: ${withType.length}; sample ---`);
      withType.slice(0, 8).forEach(p => console.log(`  id=${p.id} '${p.name}' t=${p.productTypeId}'${p.productTypeName}' s=${p.productSubcategoryId}'${p.productSubcategoryName}'`));
    }
    // also read what the DOM table actually shows for the brava row
    const rowText = await page.locator('table tbody tr', { hasText: 'Brava' }).first().innerText().catch(() => '(row not found)');
    console.log('--- DOM table row text ---\n' + rowText);
  } catch (e) {
    console.log('ERROR:', e.message);
  } finally {
    await browser.close();
  }
})();
