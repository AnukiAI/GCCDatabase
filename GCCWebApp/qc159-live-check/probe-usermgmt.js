const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('[pageerror]', e.message));
  page.on('console', m => { if (m.type() === 'error') console.log('[console err]', m.text().slice(0, 200)); });

  await page.goto('http://localhost:5101/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
  await page.waitForTimeout(2000);
  await page.goto('http://localhost:5101/Admin/UserManagement', { waitUntil: 'domcontentloaded', timeout: 45000 });
  await page.waitForTimeout(4000);
  console.log('URL:', page.url(), 'Title:', await page.title());

  const btns = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('button, a.btn').forEach(b => {
      const t = (b.innerText || '').trim().slice(0, 30);
      if (t || b.id) out.push({ id: b.id, text: t, visible: b.offsetParent !== null });
    });
    return out.slice(0, 30);
  });
  console.log('BUTTONS:', JSON.stringify(btns, null, 2));

  // Try a direct click via JS to see if showImportModal fires
  try {
    await page.evaluate(() => {
      if (window.userManagement && window.userManagement.showImportModal) {
        console.log('calling showImportModal directly');
        window.userManagement.showImportModal();
        return 'called';
      }
      return 'no window.userManagement';
    });
  } catch (e) { console.log('eval err:', e.message); }

  await browser.close();
})();
