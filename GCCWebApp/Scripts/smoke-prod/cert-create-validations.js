// Validation smoke for /Certificate/Create (Analyst).
// Exercises every validation the user actually hits on the Create form:
//   - Required fields (client HTML5 `required` + ASP.NET unobtrusive)
//   - Custom-dropdown inline "required" errors (Company / Product Name)
//   - Auto Certificate # (readonly, pre-filled)
//   - maxlength caps (CertificationResults=1000, Location=175)
//   - Company email regex (QC #181 stricter-than-[EmailAddress])
//   - Date fields present + required
//   - Empty-submit: form is blocked, nothing navigates away
//
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/cert-create-validations.js
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');
const path = require('path');
const fs = require('fs');

const OUT = path.join(__dirname, '_results');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

const results = [];
function rec(id, name, status, notes = '') {
  results.push({ id, name, status, notes });
  console.log(`  [${status}] ${id} — ${name}${notes ? ' :: ' + notes : ''}`);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 1600 } });
  const page = await ctx.newPage();

  try {
    await login(page, 'analyst');
    const resp = await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
    rec('access', 'Create page loads for analyst', resp && resp.status() === 200 ? 'PASS' : 'FAIL', `HTTP ${resp && resp.status()}`);
    await page.waitForTimeout(1200); // let inline-create custom dropdowns initialize

    // ---- 1. Auto Certificate # is pre-filled + readonly ----
    const num = await page.locator('#Number').inputValue().catch(() => '');
    const numReadonly = await page.locator('#Number').getAttribute('readonly');
    rec('cert-number.autofilled', 'Certification # pre-filled', /\S/.test(num) ? 'PASS' : 'FAIL', `value="${num}"`);
    rec('cert-number.readonly', 'Certification # readonly', numReadonly !== null ? 'PASS' : 'FAIL');

    // ---- 2. Required attributes present on the key fields ----
    const requiredFields = {
      CompanyId: '#CompanyId',
      ProductId: '#ProductId',
      CertificationTypeId: '#CertificationTypeId',
      EffectiveDate: '#EffectiveDate',
      ExpirationDate: '#ExpirationDate',
      Status: '#Status',
      certificatePdf: '#certificatePdf',
    };
    for (const [name, sel] of Object.entries(requiredFields)) {
      const isReq = await page.locator(sel).getAttribute('required').catch(() => null);
      rec(`required.${name}`, `${name} marked required`, isReq !== null ? 'PASS' : 'FAIL');
    }

    // ---- 3. Empty submit is blocked (form does not navigate) ----
    const urlBefore = page.url();
    await page.locator('button[type="submit"], button:has-text("Save")').first().click().catch(() => {});
    await page.waitForTimeout(800);
    const urlAfter = page.url();
    rec('empty-submit.blocked', 'Empty submit does NOT navigate away', urlBefore === urlAfter ? 'PASS' : 'FAIL', urlAfter.replace(BASE, ''));

    // Report which required controls are reported invalid by the browser (checkValidity).
    const invalidReport = await page.evaluate((sels) => {
      const out = {};
      for (const [name, sel] of Object.entries(sels)) {
        const el = document.querySelector(sel);
        out[name] = el ? { exists: true, valid: el.checkValidity(), msg: el.validationMessage } : { exists: false };
      }
      return out;
    }, requiredFields);
    for (const [name, info] of Object.entries(invalidReport)) {
      const flagged = info.exists && info.valid === false;
      rec(`empty-submit.invalid.${name}`, `${name} flagged invalid on empty submit`, flagged ? 'PASS' : 'WARN',
        info.exists ? info.msg || '(no message)' : 'element missing');
    }

    // ---- 4. Custom-dropdown inline error wiring (Company / Product Name) ----
    // createCustomDropdown adds .inline-validation-error after the wrapper when the hidden
    // select fires `invalid`. Empty submit should surface these.
    const inlineErrs = await page.locator('.inline-validation-error').count();
    rec('inline-error.custom-dropdowns', 'Inline validation errors render for custom dropdowns', inlineErrs > 0 ? 'PASS' : 'WARN', `count=${inlineErrs}`);

    // ---- 5. maxlength caps ----
    const crMax = await page.locator('textarea[name="CertificationResults"]').getAttribute('maxlength').catch(() => null);
    rec('maxlength.CertificationResults', 'Certification Results maxlength=1000', crMax === '1000' ? 'PASS' : 'WARN', `maxlength=${crMax}`);
    // Type 1100 chars, confirm DOM truncates to 1000.
    if (crMax === '1000') {
      await page.locator('textarea[name="CertificationResults"]').fill('x'.repeat(1100)).catch(() => {});
      const len = (await page.locator('textarea[name="CertificationResults"]').inputValue().catch(() => '')).length;
      rec('maxlength.CertificationResults.enforced', 'Certification Results truncates to 1000 chars', len === 1000 ? 'PASS' : 'FAIL', `actual=${len}`);
    }
    const locMax = await page.locator('#Location, input[name="Location"]').first().getAttribute('maxlength').catch(() => null);
    rec('maxlength.Location', 'Location maxlength=175', locMax === '175' ? 'PASS' : 'WARN', `maxlength=${locMax}`);

    // ---- 6. Company email regex (QC #181) ----
    // Pick a company to populate email field, then test an invalid value's client validity.
    const emailSel = '#CompanyEmail, input[name="CompanyEmail"]';
    const emailExists = await page.locator(emailSel).first().count();
    if (emailExists > 0) {
      // "a@b" (no TLD) should be invalid under the stricter regex; "real@example.com" valid.
      const emailCheck = await page.evaluate((sel) => {
        const el = document.querySelector(sel);
        if (!el) return null;
        const test = (v) => { el.value = v; el.dispatchEvent(new Event('input', { bubbles: true })); return el.checkValidity(); };
        return { noTld: test('a@b'), good: test('real@example.com'), empty: test('') };
      }, emailSel.split(',')[0].trim());
      if (emailCheck) {
        rec('email.regex.no-tld-rejected', 'Company email "a@b" rejected by pattern', emailCheck.noTld === false ? 'PASS' : 'WARN', `valid=${emailCheck.noTld}`);
        rec('email.regex.valid-accepted', 'Company email "real@example.com" accepted', emailCheck.good === true ? 'PASS' : 'FAIL', `valid=${emailCheck.good}`);
        rec('email.optional', 'Company email empty is allowed (optional)', emailCheck.empty === true ? 'PASS' : 'WARN', `valid=${emailCheck.empty}`);
      }
    } else {
      rec('email.regex', 'Company email field present', 'WARN', 'field not found');
    }

    // ---- 7. PDF accept filter ----
    const accept = await page.locator('#certificatePdf').getAttribute('accept').catch(() => null);
    rec('pdf.accept', 'PDF upload accepts application/pdf only', accept === 'application/pdf' ? 'PASS' : 'WARN', `accept=${accept}`);

    // Screenshot the post-empty-submit error state.
    const shot = path.join(OUT, 'cert-create-validations.png');
    await page.screenshot({ path: shot, fullPage: true });
    rec('screenshot', 'Captured validation-state screenshot', 'PASS', path.relative(process.cwd(), shot));

  } catch (e) {
    rec('fatal', 'Script ran without throwing', 'FAIL', e.message);
  } finally {
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\n[validations] ${pass} PASS / ${fail} FAIL / ${warn} WARN (${results.length} checks)`);
  fs.writeFileSync(path.join(OUT, 'cert-create-validations.json'), JSON.stringify({ pass, fail, warn, results }, null, 2));
  process.exitCode = fail === 0 ? 0 : 1;
})();
