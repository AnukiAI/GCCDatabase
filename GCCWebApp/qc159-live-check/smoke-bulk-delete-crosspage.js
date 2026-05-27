// DEF-194 cross-page bulk-delete smoke test (LOCAL)
// Runs against http://localhost:5101 against cert 26-0023 (1005 PIDs) in dev DB.
// Verifies:
//   1. Banner is hidden initially
//   2. Banner appears when header select-all is checked
//   3. Clicking the link puts the page in cross-page mode
//   4. Delete-Selected button shows the TOTAL count
//   5. Bulk-delete endpoint accepts the cross-page payload (dry-run via 404 cert)
//   6. Unchecking the header select-all clears everything
const { chromium } = require('playwright');
const BASE = 'http://localhost:5101';
const CERT = '26-0023';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  const log = [];
  const fail = (msg) => { log.push('FAIL: ' + msg); console.log('❌', msg); };
  const pass = (msg) => { log.push('PASS: ' + msg); console.log('✓', msg); };

  page.on('console', m => { if (m.type() === 'error') log.push('[console err] ' + m.text().slice(0, 200)); });

  // Login
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('analyst');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
  await page.waitForTimeout(2000);

  // Edit page
  await page.goto(`${BASE}/Certificate/Edit/${CERT}`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(4000);

  // 1. Banner hidden initially
  const banner = page.locator('#bulkSelectAllBanner');
  const bannerInitiallyVisible = await banner.isVisible();
  if (!bannerInitiallyVisible) pass('Banner hidden initially');
  else fail('Banner should be hidden initially');

  // 2. Click header select-all → banner appears with "Select all N" prompt
  await page.locator('#selectAllProductIds').check();
  await page.waitForTimeout(500);
  const bannerVisibleAfter = await banner.isVisible();
  if (bannerVisibleAfter) pass('Banner visible after header select-all');
  else fail('Banner should be visible after header select-all');

  const linkText = await page.locator('#bulkSelectAllLink').innerText().catch(() => '');
  if (/Select all\s+\d+\s+product IDs/.test(linkText)) pass(`Link prompts: "${linkText}"`);
  else fail(`Unexpected link text: "${linkText}"`);

  const totalFromBanner = parseInt((linkText.match(/(\d+)/) || [])[1] || '0');
  if (totalFromBanner > 50) pass(`Total in banner = ${totalFromBanner} (> page size, so cross-page is meaningful)`);
  else fail(`Total in banner = ${totalFromBanner}, expected > 50`);

  // Count badge should show 50 (page size) at this point, not total
  const countBeforeLink = await page.locator('#bulkDeleteCount').innerText();
  if (countBeforeLink === '50') pass('Delete-Selected count = 50 before clicking the link');
  else fail(`Expected count = 50, got ${countBeforeLink}`);

  // 3. Click the "Select all N" link → cross-page mode
  await page.locator('#bulkSelectAllLink').click();
  await page.waitForTimeout(300);

  const bannerActiveClass = await banner.evaluate(el => el.classList.contains('bulk-select-all-active'));
  if (bannerActiveClass) pass('Banner gained .bulk-select-all-active');
  else fail('Banner missing .bulk-select-all-active');

  // 4. Count badge now shows total
  const countAfterLink = await page.locator('#bulkDeleteCount').innerText();
  if (parseInt(countAfterLink) === totalFromBanner) pass(`Delete-Selected count = ${countAfterLink} (matches total)`);
  else fail(`Expected count = ${totalFromBanner}, got ${countAfterLink}`);

  // The flag is set internally
  const flag = await page.evaluate(() => window.productIdManager && window.productIdManager.selectAllAcrossPages);
  if (flag === true) pass('selectAllAcrossPages flag = true');
  else fail(`selectAllAcrossPages = ${flag}`);

  // Clear-selection button visible
  const clearVisible = await page.locator('#bulkSelectAllClear').isVisible();
  if (clearVisible) pass('Clear-selection button visible');
  else fail('Clear-selection button should be visible in cross-page mode');

  // 5. Hit the bulk-delete endpoint with a deliberately-empty filter that
  //    matches nothing (search="ZZ_NONEXISTENT_ZZ") so we get a real 200
  //    response without destroying data.
  const apiResp = await page.evaluate(async (certId) => {
    const r = await fetch(`/api/productids/certificate/${certId}/bulk-delete`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ deleteAll: true, search: 'ZZ_NONEXISTENT_ZZ' }),
    });
    const j = await r.json();
    return { status: r.status, body: j };
  }, await page.evaluate(() => window.productIdManager.certificateId));
  if (apiResp.status === 200 && apiResp.body.success === true && apiResp.body.data?.deleted === 0) {
    pass(`Bulk-delete endpoint OK: ${JSON.stringify(apiResp.body.data)}`);
  } else {
    fail(`Endpoint failed: ${JSON.stringify(apiResp)}`);
  }

  // Also verify a tiny ids[] payload doesn't crash (pass an obviously-invalid ID)
  const apiResp2 = await page.evaluate(async (certId) => {
    const r = await fetch(`/api/productids/certificate/${certId}/bulk-delete`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids: [-99999] }),
    });
    return { status: r.status, body: await r.json() };
  }, await page.evaluate(() => window.productIdManager.certificateId));
  if (apiResp2.status === 200) pass('Bulk-delete with ids[-99999] returns 200 (no-op)');
  else fail(`ids-mode failed: ${JSON.stringify(apiResp2)}`);

  // 6. Click Clear → everything resets
  await page.locator('#bulkSelectAllClear').click();
  await page.waitForTimeout(300);
  const bannerAfterClear = await banner.isVisible();
  if (!bannerAfterClear) pass('Banner hidden after Clear');
  else fail('Banner should hide after Clear');
  const flagAfter = await page.evaluate(() => window.productIdManager.selectAllAcrossPages);
  if (flagAfter === false) pass('selectAllAcrossPages flag cleared');
  else fail(`Flag still set: ${flagAfter}`);

  // Screenshot the active state for visual evidence
  await page.locator('#selectAllProductIds').check();
  await page.waitForTimeout(200);
  await page.locator('#bulkSelectAllLink').click();
  await page.waitForTimeout(300);
  await page.screenshot({ path: 'crosspage-active.png', fullPage: false });

  await browser.close();

  const failed = log.filter(l => l.startsWith('FAIL')).length;
  console.log(`\n=== ${log.length - failed}/${log.length} checks passed ===`);
  process.exit(failed === 0 ? 0 : 1);
})();
