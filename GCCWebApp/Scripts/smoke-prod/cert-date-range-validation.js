// Verifies the Expiration > Effective cross-field rule on /Certificate/Create.
// Client-side: bad date order blocks submit + shows inline error; good order does not.
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/cert-date-range-validation.js
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');

const results = [];
const rec = (id, name, status, notes = '') => {
  results.push({ id, status });
  console.log(`  [${status}] ${id} — ${name}${notes ? ' :: ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1440, height: 1400 } })).newPage();
  try {
    await login(page, 'analyst');
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(800);

    // Set dates and fire `change` (blur commits the native date input) so live validation runs.
    const setDates = async (eff, exp) => {
      await page.locator('#EffectiveDate').fill(eff);
      await page.locator('#EffectiveDate').blur();
      await page.locator('#ExpirationDate').fill(exp);
      await page.locator('#ExpirationDate').blur();
      await page.waitForTimeout(200);
    };

    // Case 1: expiration BEFORE effective -> live inline error.
    await setDates('2026-06-19', '2026-06-01');
    const err1 = await page.locator('.date-range-error').count();
    rec('before.error-shown', 'Expiration < Effective shows live inline error', err1 > 0 ? 'PASS' : 'FAIL', `errCount=${err1}`);

    // Case 1b: clicking Save with bad dates blocks navigation.
    const urlBefore = page.url();
    await page.locator('button[type="submit"], button:has-text("Save")').first().click().catch(() => {});
    await page.waitForTimeout(500);
    rec('before.blocked', 'Save with bad dates blocks navigation', page.url() === urlBefore ? 'PASS' : 'FAIL');

    // Case 2: expiration EQUAL to effective -> error (rule is strictly-after).
    await setDates('2026-06-19', '2026-06-19');
    const err2 = await page.locator('.date-range-error').count();
    rec('equal.error-shown', 'Expiration == Effective shows error (strictly-after)', err2 > 0 ? 'PASS' : 'FAIL', `errCount=${err2}`);

    // Case 3: expiration AFTER effective -> error cleared.
    await setDates('2026-06-19', '2026-12-31');
    const err3 = await page.locator('.date-range-error').count();
    rec('after.no-date-error', 'Expiration > Effective clears the date error', err3 === 0 ? 'PASS' : 'FAIL', `errCount=${err3}`);
  } catch (e) {
    rec('fatal', 'ran without throwing', 'FAIL', e.message);
  } finally {
    await browser.close();
  }
  const fail = results.filter(r => r.status === 'FAIL').length;
  console.log(`\n[date-range] ${results.filter(r => r.status === 'PASS').length} PASS / ${fail} FAIL`);
  process.exitCode = fail === 0 ? 0 : 1;
})();
