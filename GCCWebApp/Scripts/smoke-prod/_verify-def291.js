// DEF-291 / QC #193 runtime verification: the "created successfully" toast must show once
// per save and must NOT re-fire (stale) when the Create page is restored from bfcache/Back
// or revisited fresh via "Add Certificate".
const { chromium } = require('playwright');
const BASE = 'http://localhost:5101';
const results = [];
const log = (id, pass, detail) => { results.push({ id, pass, detail }); console.log(`[${pass ? 'PASS' : 'FAIL'}] ${id}: ${detail}`); };

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 900 } });
  const page = await ctx.newPage();
  // Patch AdminCore.showNotification as soon as it exists; record every call in window.__toasts.
  await ctx.addInitScript(() => {
    window.__toasts = [];
    const t = setInterval(() => {
      try {
        if (typeof AdminCore !== 'undefined' && AdminCore.showNotification && !AdminCore.__toastPatched) {
          const orig = AdminCore.showNotification.bind(AdminCore);
          AdminCore.showNotification = function (m, type, o) { window.__toasts.push({ m, type }); return orig(m, type, o); };
          AdminCore.__toastPatched = true;
          clearInterval(t);
        }
      } catch (e) {}
    }, 1);
  });
  page.on('pageerror', e => console.log('  [pageerror]', e.message));

  // login as admin
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.fill('#Email', 'admin'); await page.fill('#Password', 'TestPass123!');
  await page.click('button[type=submit], input[type=submit]');
  await page.waitForTimeout(1500);

  // GET Create
  await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded' });
  await page.locator('#CompanyId').waitFor({ state: 'visible', timeout: 10000 });

  // Selects may be visually replaced/overlaid → set value + dispatch change via DOM (bypasses
  // visibility, still fires cascade listeners).
  const setSelect = (sel) => page.evaluate((s) => {
    const el = document.querySelector(s);
    if (!el) return null;
    const opt = Array.from(el.options).find(o => o.value && o.value !== '');
    if (!opt) return null;
    el.value = opt.value;
    el.dispatchEvent(new Event('change', { bubbles: true }));
    return opt.value;
  }, sel);

  const companyVal = await setSelect('#CompanyId');
  await page.waitForFunction(() => {
    const s = document.querySelector('#ProductId');
    return s && s.querySelectorAll('option:not([value=""])').length > 0;
  }, { timeout: 10000 }).catch(() => {});
  const prodVal = await setSelect('#ProductId');
  const certTypeVal = await setSelect('#CertificationTypeId');
  await page.evaluate(() => {
    const e = document.querySelector('#EffectiveDate'); if (e) { e.value = '2026-01-01'; e.dispatchEvent(new Event('change', { bubbles: true })); }
    const x = document.querySelector('#ExpirationDate'); if (x) { x.value = '2027-01-01'; x.dispatchEvent(new Event('change', { bubbles: true })); }
  });

  log('form-ready', !!(companyVal && prodVal && certTypeVal), `company=${companyVal} product=${prodVal} certType=${certTypeVal}`);

  // reset counter, submit Save & Add Another
  await page.evaluate(() => { window.__toasts = []; });
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'domcontentloaded' }).catch(() => {}),
    page.click('button[name="action"][value="saveAndAddAnother"]'),
  ]);
  await page.waitForTimeout(1500);
  const onCreate = (await page.url()).includes('/Certificate/Create');
  const afterSave = await page.evaluate(() => (window.__toasts || []).length);
  log('toast-shows-once-after-save', onCreate && afterSave === 1, `redirected to Create=${onCreate}, toasts=${afterSave} (expect 1)`);

  // ---- stale-toast test via bfcache Back ----
  await page.goto(`${BASE}/`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(400);
  await page.goBack({ waitUntil: 'domcontentloaded' }).catch(() => {});
  await page.waitForTimeout(1200);
  const afterBack = await page.evaluate(() => (window.__toasts || []).length);
  // On bfcache restore the counter persists; a correctly-suppressed stale toast leaves it unchanged (1).
  // On a fresh re-fetch the document reloads (counter resets to 0) and TempData is gone → no script → 0.
  log('no-stale-toast-on-back', afterBack <= 1, `toasts after Back=${afterBack} (must be <= 1; a stale re-show would be 2)`);

  // ---- fresh "Add Certificate" GET shows nothing ----
  await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded' });
  await page.locator('#CompanyId').waitFor({ state: 'visible', timeout: 10000 });
  await page.waitForTimeout(1000);
  const freshToasts = await page.evaluate(() => (window.__toasts || []).length);
  log('no-toast-on-fresh-create', freshToasts === 0, `toasts on fresh Create GET=${freshToasts} (expect 0)`);

  await browser.close();
  const failed = results.filter(r => !r.pass).length;
  console.log(`\n=== ${results.length - failed}/${results.length} passed ===`);
  process.exit(failed ? 1 : 0);
})();
