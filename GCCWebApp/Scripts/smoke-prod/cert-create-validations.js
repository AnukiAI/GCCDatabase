// Validation smoke for /Certificate/Create (Analyst).
// Exercises every validation the user actually hits on the Create form:
//   - Required fields (client HTML5 `required` + ASP.NET unobtrusive)
//   - Custom-dropdown inline "required" errors (Company / Product Name)
//   - Auto Certificate # (readonly, pre-filled)
//   - maxlength caps (CertificationResults=1000, Location=175)
//   - Company email regex (QC #181 stricter-than-[EmailAddress])
//   - Date fields present + required
//   - Empty-submit: form is blocked, nothing navigates away
//   - Full happy-path submit: fill every required field via the REAL UI (Certification
//     Type via type+Enter — the DEF-299 gesture), attach a PDF, Save, and assert the
//     server accepts it and redirects to the new cert's internal view (creates 1 dev cert).
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

// Pick the first company that actually has products, set it, wait for the product
// list to load, then select the first product. Company/Product are custom dropdowns
// covered separately by cert-product-clickflow.js; here we drive their hidden <select>
// directly (stable) so the happy-path submit isn't flaky on dropdown timing.
async function pickCompanyWithProducts(page) {
  const companyId = await page.evaluate(async () => {
    const sel = document.getElementById('CompanyId');
    for (const o of sel.options) {
      if (!o.value || /add new/i.test(o.textContent)) continue;
      try {
        const r = await fetch(`/Certificate/GetProductsByCompany?companyId=${o.value}`, { credentials: 'include' });
        const j = await r.json();
        if (j.success && j.data && j.data.length > 0) return o.value;
      } catch { /* ignore */ }
    }
    return null;
  });
  if (!companyId) return { companyId: null, productId: null };
  await page.evaluate(cid => { const s = document.getElementById('CompanyId'); s.value = cid; s.dispatchEvent(new Event('change', { bubbles: true })); }, companyId);
  await page.waitForFunction(() => document.getElementById('ProductId')?.options.length > 1, { timeout: 10000 }).catch(() => {});
  const productId = await page.evaluate(() => {
    const s = document.getElementById('ProductId');
    for (const o of s.options) if (o.value) { s.value = o.value; s.dispatchEvent(new Event('change', { bubbles: true })); return o.value; }
    return null;
  });
  return { companyId, productId };
}

// Select a Certification Type the way a real user does: type its name into the
// SearchableSelect type-ahead box and press Enter. This is exactly the gesture that
// DEF-299 fixed — before the fix this left the hidden <select> empty and the create
// POST failed with ".NET: The value '' is invalid".
async function pickCertTypeByTyping(page) {
  const opt = await page.$$eval('#CertificationTypeId option', os => { const r = os.find(o => o.value); return r ? { v: r.value, t: r.text } : null; });
  if (!opt) return { typedText: null, committed: '' };
  const boxHandle = await page.evaluateHandle(() => { const s = document.querySelector('#CertificationTypeId'); return s.nextElementSibling && s.nextElementSibling.querySelector('input'); });
  const el = boxHandle.asElement();
  if (!el) return { typedText: opt.t, committed: '' };
  await el.click();
  await el.fill('');
  await el.type(opt.t, { delay: 10 });
  await page.waitForTimeout(200);
  await page.keyboard.press('Enter');
  await page.waitForTimeout(250);
  const committed = await page.$eval('#CertificationTypeId', s => s.value);
  return { typedText: opt.t, committed };
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

    // ---- 8. Full happy-path submit succeeds (end-to-end create) ----
    // Fills every required field through the REAL UI — notably the Certification Type
    // type-ahead via keyboard (type + Enter), the gesture DEF-299 fixed — attaches a PDF,
    // clicks the real Save button, and asserts the server accepts it and redirects to the
    // new cert's internal view. Section 3 only proved an EMPTY submit is blocked; this proves
    // a COMPLETE submit goes through (the gap that let DEF-299 ship). Creates one test
    // certificate in the dev DB per run — localhost only, disposable.
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(1200);

    const cp = await pickCompanyWithProducts(page);
    rec('happy.company-product', 'Company + Product selected', cp.companyId && cp.productId ? 'PASS' : 'FAIL', `company=${cp.companyId} product=${cp.productId}`);

    const ct = await pickCertTypeByTyping(page);
    rec('happy.certtype-keyboard', 'Certification Type commits via type+Enter (DEF-299 guard)', ct.committed ? 'PASS' : 'FAIL', `typed="${ct.typedText}" committed=${ct.committed}`);

    // Dates (Expiration must be after Effective), Status, and a PDF file.
    await page.evaluate(() => {
      const ed = document.getElementById('EffectiveDate'); if (ed) { ed.value = '2026-01-01'; ed.dispatchEvent(new Event('change', { bubbles: true })); }
      const xd = document.getElementById('ExpirationDate'); if (xd) { xd.value = '2027-01-01'; xd.dispatchEvent(new Event('change', { bubbles: true })); }
      const st = document.getElementById('Status'); if (st) { for (const o of st.options) if (o.value) { st.value = o.value; st.dispatchEvent(new Event('change', { bubbles: true })); break; } }
    });
    const SAMPLE_PDF = path.join(__dirname, '..', '..', 'App_Data', 'uploads', 'certificates', '26-0023_20260612_161411.pdf');
    const havePdf = fs.existsSync(SAMPLE_PDF);
    if (havePdf) { await page.locator('#certificatePdf').setInputFiles(SAMPLE_PDF).catch(() => {}); }
    rec('happy.pdf', 'Sample PDF attached', havePdf ? 'PASS' : 'WARN', havePdf ? path.relative(process.cwd(), SAMPLE_PDF) : 'sample PDF not found — submit may be blocked by required PDF');

    // Click the REAL Save button (name=action value=save) and wait for the success redirect.
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 15000 }).catch(() => {}),
      page.locator('button[name="action"][value="save"]').click().catch(() => {}),
    ]);
    await page.waitForTimeout(1200);
    const afterUrl = page.url();
    const onInternalView = /\/Certificate\/CertificateInternalView\//i.test(afterUrl);
    // If we stayed on Create, surface whatever blocked it (server ModelState or inline errors).
    let failDetail = '';
    if (!onInternalView) {
      failDetail = await page.evaluate(() => {
        const ctErr = document.querySelector('span[data-valmsg-for="CertificationTypeId"]');
        const summary = document.querySelector('.validation-summary-errors, .alert-danger');
        const inline = document.querySelector('.inline-validation-error');
        return [
          ctErr && ctErr.textContent.trim() ? 'certType="' + ctErr.textContent.trim() + '"' : '',
          summary && summary.textContent.trim() ? 'summary="' + summary.textContent.trim().slice(0, 160) + '"' : '',
          inline && inline.textContent.trim() ? 'inline="' + inline.textContent.trim() + '"' : '',
        ].filter(Boolean).join(' ');
      });
    }
    rec('happy.submit-redirect', 'Full create submits and redirects to cert view',
      onInternalView ? 'PASS' : 'FAIL',
      onInternalView ? afterUrl.replace(BASE, '') : `stayed at ${afterUrl.replace(BASE, '')} :: ${failDetail || '(no error surfaced)'}`);

    const happyShot = path.join(OUT, 'cert-create-happy-path.png');
    await page.screenshot({ path: happyShot, fullPage: true });
    rec('happy.screenshot', 'Captured happy-path result screenshot', 'PASS', path.relative(process.cwd(), happyShot));

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
