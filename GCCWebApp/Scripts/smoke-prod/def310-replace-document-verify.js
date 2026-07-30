/**
 * DEF-310 (QC #191) — Edit Certificate: replacing the uploaded document must immediately
 * show the NEW document in the preview and the "View" control.
 *
 * Client report: "Trying to replace the uploaded document with a new one says it works but
 * still shows the old document in the preview and in the 'view' option further down the page.
 * When I tried to replicate it to see if I missed anything, it worked correctly."
 *
 * Root cause was browser heuristic caching: /Certificate/ViewCertificateImage/{id} is a stable
 * URL whose bytes change on replace, and the response carried no Cache-Control, so the browser
 * applied heuristic freshness (~10% of the document's age) and reused the old copy.
 *
 * This harness does the full round trip against a REAL certificate with REAL PDFs:
 *   1. create a certificate with PDF A ("DOCUMENT ALPHA")
 *   2. record the preview URL, its ?v= token, and a hash of the served image bytes
 *   3. edit it, replace the PDF with PDF B ("DOCUMENT BRAVO"), save
 *   4. re-read the page and assert the preview URL, token, AND rendered bytes all changed
 *   5. assert the served response carries a revalidation Cache-Control header
 *
 * Step 4 is the actual client-reported symptom. Step 5 is the fix that makes it stick for a
 * browser with a warm cache.
 *
 * Usage:  node def310-replace-document-verify.js
 * Env:    BASE (default http://localhost:5101), HEADLESS=0 to watch
 */

const { chromium } = require('playwright');
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const os = require('os');

const BASE = process.env.BASE || 'http://localhost:5101';
const HEADLESS = process.env.HEADLESS !== '0';
const USER = 'jane.analyst@anukiconsulting.com';
const PASS = 'TestPass123!';

// Scoped to the cert form's own Save button — a bare button[type=submit] also matches the
// nav's hidden logout button, which is not visible and stalls the click.
const SUBMIT = '#certificateCreateForm button[type="submit"][value="save"], '
             + '#certificateEditForm button[type="submit"][value="save"]';

const results = [];
function check(name, pass, detail) {
  results.push({ name, pass, detail });
  console.log(`${pass ? 'PASS' : 'FAIL'}  ${name}${detail ? ` — ${detail}` : ''}`);
}

/**
 * Build a minimal but structurally valid one-page PDF showing `label` in large type.
 * Written by hand (with correct xref offsets) so the two files differ in RENDERED PIXELS,
 * not just in bytes — a thumbnail of A must be visibly distinguishable from a thumbnail of B.
 */
function makePdf(label) {
  const objs = [];
  objs[1] = '<< /Type /Catalog /Pages 2 0 R >>';
  objs[2] = '<< /Type /Pages /Kids [3 0 R] /Count 1 >>';
  objs[3] = '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] '
          + '/Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>';
  objs[4] = '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>';
  const stream = `BT /F1 48 Tf 60 400 Td (${label}) Tj ET`;
  objs[5] = `<< /Length ${stream.length} >>\nstream\n${stream}\nendstream`;

  let pdf = '%PDF-1.4\n';
  const offsets = [];
  for (let i = 1; i < objs.length; i++) {
    offsets[i] = pdf.length;
    pdf += `${i} 0 obj\n${objs[i]}\nendobj\n`;
  }
  const xrefStart = pdf.length;
  pdf += `xref\n0 ${objs.length}\n0000000000 65535 f \n`;
  for (let i = 1; i < objs.length; i++) {
    pdf += `${String(offsets[i]).padStart(10, '0')} 00000 n \n`;
  }
  pdf += `trailer\n<< /Size ${objs.length} /Root 1 0 R >>\nstartxref\n${xrefStart}\n%%EOF\n`;
  return Buffer.from(pdf, 'latin1');
}

/** Fetch a URL inside the authenticated browser context; return status, cache header, body hash. */
async function probe(page, url) {
  return await page.evaluate(async (u) => {
    const r = await fetch(u, { cache: 'no-store' });
    const buf = await r.arrayBuffer();
    let bin = '';
    const bytes = new Uint8Array(buf);
    for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
    return {
      status: r.status,
      cacheControl: r.headers.get('cache-control'),
      contentType: r.headers.get('content-type'),
      length: bytes.length,
      b64: btoa(bin),
    };
  }, url);
}
const hash = (b64) => crypto.createHash('sha256').update(Buffer.from(b64, 'base64')).digest('hex').slice(0, 16);

/**
 * Fill every empty visible field on the form.
 *
 * Selects are driven one at a time through Playwright (not raw DOM setters) and awaited between
 * picks, because several are cascading — Product Name only populates after a Company is chosen —
 * so the later dropdowns literally do not have their options yet when the loop starts.
 */
async function fillForm(page, stamp) {
  // text / date / number first
  await page.evaluate((s) => {
    const setVal = (el, v) => {
      const proto = el instanceof HTMLSelectElement ? HTMLSelectElement.prototype
        : el instanceof HTMLTextAreaElement ? HTMLTextAreaElement.prototype
          : HTMLInputElement.prototype;
      Object.getOwnPropertyDescriptor(proto, 'value').set.call(el, v);
      el.dispatchEvent(new Event('input', { bubbles: true }));
      el.dispatchEvent(new Event('change', { bubbles: true }));
    };
    for (const el of document.querySelectorAll('input, textarea')) {
      if (['file', 'hidden', 'checkbox', 'radio', 'submit', 'button'].includes(el.type)) continue;
      if (el.disabled || el.readOnly || el.offsetParent === null) continue;
      if (el.value && el.value.trim() !== '') continue;
      // Expiration must be strictly after Effective — validateCertDateRange() silently blocks
      // the submit event otherwise, which looks exactly like "nothing happened".
      if (el.type === 'date') setVal(el, /expir/i.test(el.name || el.id) ? '2027-06-30' : '2026-01-15');
      else if (el.type === 'number') setVal(el, '1');
      else setVal(el, `DEF310 ${s}`);
    }
  }, stamp);

  // Then the selects. These are set through the DOM rather than page.selectOption because several
  // are wrapped by the SearchableSelect widget, which sets the native <select> to display:none —
  // Playwright's visibility check refuses to drive a hidden element. Dispatching a native 'change'
  // is enough for the widget and for the page's jQuery handlers, which is what drives the cascades
  // (Company -> Product Name, Certification Type -> Subcategories).
  //
  // Repeated because those cascades populate asynchronously: a dependent select genuinely has no
  // options to choose from on the first pass.
  for (let pass = 0; pass < 8; pass++) {
    const state = await page.evaluate(() => {
      const setVal = (el, v) => {
        Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, 'value').set.call(el, v);
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
      };
      const form = document.querySelector('#certificateCreateForm, #certificateEditForm') || document;
      let touched = 0, stillEmpty = 0;
      for (const el of form.querySelectorAll('select')) {
        if (el.disabled) continue;
        if (el.value && el.value !== '' && el.value !== '0') continue;
        const opt = [...el.options].find((o) => o.value && o.value !== '' && o.value !== '0');
        if (opt) { setVal(el, opt.value); touched++; }
        else if (el.required) { stillEmpty++; }
      }
      return { touched, stillEmpty };
    });
    if (!state.touched && !state.stillEmpty) break;
    await page.waitForTimeout(700);
  }
}

/** Read the preview <img> src off the Edit page. */
async function previewSrc(page) {
  return await page.evaluate(() => {
    const img = document.querySelector('#certificateImagePreviewImg');
    return img ? img.getAttribute('src') : null;
  });
}

(async () => {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'def310-'));
  const pdfA = path.join(tmp, 'document-alpha.pdf');
  const pdfB = path.join(tmp, 'document-bravo.pdf');
  fs.writeFileSync(pdfA, makePdf('DOCUMENT ALPHA'));
  fs.writeFileSync(pdfB, makePdf('DOCUMENT BRAVO'));
  console.log(`fixtures: ${pdfA} (${fs.statSync(pdfA).size}b), ${pdfB} (${fs.statSync(pdfB).size}b)\n`);

  const browser = await chromium.launch({ headless: HEADLESS });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', (e) => console.log(`  [pageerror] ${e.message}`));

  try {
    // ---- login ----
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('#Email', USER);
    await page.fill('#Password', PASS);
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'domcontentloaded' }).catch(() => {}),
      page.click('button[type="submit"], input[type="submit"]'),
    ]);
    check('login as analyst', !/\/Account\/Login/i.test(page.url()), page.url());

    // ---- create a certificate with PDF A ----
    await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'networkidle' });

    const stamp = Date.now().toString().slice(-6);
    await page.setInputFiles('input[type="file"][name="certificatePdf"]', pdfA);
    await page.waitForTimeout(2500); // let the pre-save thumbnail round trip finish

    await fillForm(page, stamp);

    await page.click(SUBMIT);
    await page.waitForLoadState('networkidle');

    const createdUrl = page.url();
    const certNumber = await page.evaluate(() => {
      const m = document.body.innerText.match(/\b\d{2}-\d{4,5}\b/);
      return m ? m[0] : null;
    });
    check('certificate created with PDF A', !!certNumber && !/\/Create/i.test(createdUrl),
      `number=${certNumber} url=${createdUrl}`);
    if (!certNumber) {
      const errs = await page.evaluate(() => {
        const form = document.querySelector('#certificateCreateForm');
        const invalid = [...form.querySelectorAll('input,select,textarea')]
          .filter((el) => el.willValidate && !el.checkValidity())
          .map((el) => `${el.name || el.id} [${el.type}] value="${el.value}" :: ${el.validationMessage}`);
        const summary = [...document.querySelectorAll('.validation-summary-errors li, .field-validation-error')]
          .map((e) => e.textContent.trim()).filter((t) => t && t !== '*');
        return { invalid, summary };
      });
      console.log('  client-invalid fields:\n' + (errs.invalid.length ? errs.invalid.map((e) => `    - ${e}`).join('\n') : '    (none)'));
      console.log('  server validation summary:\n' + (errs.summary.length ? errs.summary.map((e) => `    - ${e}`).join('\n') : '    (none)'));
      await page.screenshot({ path: path.join(__dirname, '_results', 'def310-create-failed.png'), fullPage: true }).catch(() => {});
      throw new Error(`could not determine created certificate number (url=${createdUrl})`);
    }

    // ---- BEFORE: record preview URL + rendered bytes ----
    await page.goto(`${BASE}/Certificate/Edit/${certNumber}`, { waitUntil: 'networkidle' });
    const srcBefore = await previewSrc(page);
    check('preview rendered after create', !!srcBefore, srcBefore || 'no #certificateImagePreviewImg');
    if (!srcBefore) throw new Error('no preview image on the Edit page after create');

    const beforeProbe = await probe(page, new URL(srcBefore, BASE).href);
    const beforeHash = hash(beforeProbe.b64);
    check('PDF A thumbnail served', beforeProbe.status === 200 && beforeProbe.length > 0,
      `status=${beforeProbe.status} ${beforeProbe.contentType} ${beforeProbe.length}b hash=${beforeHash}`);
    check('response carries revalidation Cache-Control (the DEF-310 fix)',
      /no-cache/i.test(beforeProbe.cacheControl || ''), `Cache-Control: ${beforeProbe.cacheControl}`);

    // ---- REPLACE with PDF B ----
    await page.setInputFiles('input[type="file"][name="certificatePdf"]', pdfB);
    await page.waitForTimeout(2500);
    await page.click(SUBMIT);
    await page.waitForLoadState('networkidle');

    // ---- AFTER: re-read the Edit page exactly as the analyst would ----
    await page.goto(`${BASE}/Certificate/Edit/${certNumber}`, { waitUntil: 'networkidle' });
    const srcAfter = await previewSrc(page);
    const afterProbe = await probe(page, new URL(srcAfter, BASE).href);
    const afterHash = hash(afterProbe.b64);

    check('preview URL changed after replace (cache-busting token)', srcBefore !== srcAfter,
      `before=${srcBefore}  after=${srcAfter}`);
    check('PDF B thumbnail served', afterProbe.status === 200 && afterProbe.length > 0,
      `status=${afterProbe.status} ${afterProbe.length}b hash=${afterHash}`);
    check('RENDERED DOCUMENT CHANGED (the reported defect)', beforeHash !== afterHash,
      `before=${beforeHash} after=${afterHash}`);

    // ---- the "view" control further down the page ----
    const viewHref = await page.evaluate(() => {
      const el = [...document.querySelectorAll('a,button')]
        .find((e) => /view/i.test(e.textContent || '') && /ViewCertificate|openImageZoom/i.test(
          (e.getAttribute('href') || '') + (e.getAttribute('onclick') || '')));
      return el ? (el.getAttribute('href') || el.getAttribute('onclick')) : null;
    });
    check('"View" control points at the refreshed URL',
      !!viewHref && (srcAfter ? viewHref.includes(srcAfter.split('?')[1] || '@@none@@') : false),
      `view=${viewHref}`);

    // ---- what the browser does with a WARM cache (the actual client scenario) ----
    const warm = await page.evaluate(async (u) => {
      await fetch(u); // populate cache
      const r = await fetch(u); // default cache mode — would serve stale pre-fix
      const buf = await r.arrayBuffer();
      let bin = ''; const b = new Uint8Array(buf);
      for (let i = 0; i < b.length; i++) bin += String.fromCharCode(b[i]);
      return { status: r.status, length: b.length, b64: btoa(bin) };
    }, new URL(srcAfter, BASE).href);
    check('warm-cache re-fetch still returns the NEW document', hash(warm.b64) === afterHash,
      `warm=${hash(warm.b64)} expected=${afterHash}`);

    await page.screenshot({ path: path.join(__dirname, '_results', 'def310-after-replace.png'), fullPage: true })
      .catch(() => {});
    console.log(`\ncertificate used: ${certNumber} (left in place for inspection)`);
  } catch (err) {
    check('harness completed without error', false, err.message);
  } finally {
    await browser.close();
  }

  const failed = results.filter((r) => !r.pass);
  console.log(`\n${results.length - failed.length}/${results.length} PASS`);
  process.exit(failed.length ? 1 : 0);
})();
