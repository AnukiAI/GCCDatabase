// Verifies the Expiration > Effective cross-field rule on /Certificate/Edit/{id}.
// Mirrors cert-date-range-validation.js (Create) but against a real existing cert.
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/cert-edit-date-range-validation.js
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

    // Discover an editable cert id from the internal tile API.
    const apiResp = await page.evaluate(async (base) => {
      const r = await fetch(base + '/api/certificate-tiles?page=1&pageSize=20', { credentials: 'include' });
      if (!r.ok) return { ok: false, status: r.status };
      const j = await r.json();
      const rows = j.data || j.certificates || j.items || [];
      const withNum = rows.map(x => x.certificateNumber ?? x.CertificateNumber).find(n => n);
      return { ok: true, number: withNum || null };
    }, BASE);
    rec('discover.cert', 'Found a cert to edit via internal tile API', apiResp.ok && apiResp.number ? 'PASS' : 'FAIL', JSON.stringify(apiResp));
    if (!apiResp.ok || !apiResp.number) throw new Error('No editable cert number found: ' + JSON.stringify(apiResp));

    const editUrl = `${BASE}/Certificate/Edit/${encodeURIComponent(apiResp.number)}`;
    const resp = await page.goto(editUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
    rec('edit.loads', `Edit page loads (number=${apiResp.number})`, resp && resp.status() === 200 ? 'PASS' : 'FAIL', `HTTP ${resp && resp.status()}`);
    await page.waitForTimeout(1000);

    // Confirm the date inputs exist and the change-listener is wired.
    const haveDates = await page.evaluate(() =>
      !!document.getElementById('EffectiveDate') && !!document.getElementById('ExpirationDate'));
    rec('edit.date-fields', 'Both date fields present on Edit', haveDates ? 'PASS' : 'FAIL');

    // Set both dates and explicitly dispatch `change` (the Edit page's pre-populated
    // date inputs don't always emit change from Playwright fill() the way empty Create
    // inputs do — dispatching directly mirrors a real user committing the value).
    const setDates = async (eff, exp) => {
      await page.evaluate(({ eff, exp }) => {
        const e = document.getElementById('EffectiveDate');
        const x = document.getElementById('ExpirationDate');
        e.value = eff; e.dispatchEvent(new Event('change', { bubbles: true }));
        x.value = exp; x.dispatchEvent(new Event('change', { bubbles: true }));
      }, { eff, exp });
      await page.waitForTimeout(200);
    };

    // Case 1: expiration BEFORE effective -> live inline error.
    await setDates('2026-06-19', '2026-06-01');
    rec('edit.before.error', 'Expiration < Effective shows live inline error',
      (await page.locator('.date-range-error').count()) > 0 ? 'PASS' : 'FAIL');

    // Case 1b: Save with bad dates is blocked (stays on Edit).
    const urlBefore = page.url();
    await page.locator('button[type="submit"]:has-text("Save"), button:has-text("Save Changes")').first().click().catch(() => {});
    await page.waitForTimeout(600);
    rec('edit.before.blocked', 'Save with bad dates blocks navigation', page.url() === urlBefore ? 'PASS' : 'FAIL', page.url().replace(BASE, ''));

    // Case 2: equal -> error (strictly-after).
    await setDates('2026-06-19', '2026-06-19');
    rec('edit.equal.error', 'Expiration == Effective shows error (strictly-after)',
      (await page.locator('.date-range-error').count()) > 0 ? 'PASS' : 'FAIL');

    // Case 3: after -> error cleared.
    await setDates('2026-06-19', '2026-12-31');
    rec('edit.after.cleared', 'Expiration > Effective clears the date error',
      (await page.locator('.date-range-error').count()) === 0 ? 'PASS' : 'FAIL');
  } catch (e) {
    rec('fatal', 'ran without throwing', 'FAIL', e.message);
  } finally {
    await browser.close();
  }
  const fail = results.filter(r => r.status === 'FAIL').length;
  console.log(`\n[edit-date-range] ${results.filter(r => r.status === 'PASS').length} PASS / ${fail} FAIL`);
  process.exitCode = fail === 0 ? 0 : 1;
})();
