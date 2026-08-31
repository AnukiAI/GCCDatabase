// Phase 4 — write path: create -> read back -> edit -> read back -> delete, driven through the
// real UI as an Analyst, with the delete performed as an Admin (Delete is AdminOnly).
//
// SAFETY. This writes to the TEST environment, which since 2026-08-18 holds the real migrated
// client dataset. It therefore touches ONLY a certificate it creates itself, tagged in Location
// with ZZ_RegressionFixture_<runId>, and removes it in a finally block. No existing row is read
// destructively, updated, or deleted. The CR rule on real business records is unaffected.
//
// Verification is through the app's own read paths (internal tile API + the Edit form), not SQL —
// the remote DB is not reachable from here, and asserting through the same surface the client uses
// is the stronger test anyway: a save that persists to SQL but never reappears in the UI is still
// a broken save.

const fs = require('fs');
const path = require('path');
const { BASE, HEADLESS, NAV_TIMEOUT, ARTIFACT_DIR, session, makeRecorder, chromium, ensureDirs } = require('./_lib/regression-lib');

const RUN_ID = `${new Date().toISOString().slice(0, 10)}_${Date.now().toString(36)}`;
const MARKER = `ZZ_RegressionFixture_${RUN_ID}`;
const MARKER_EDITED = `${MARKER}_EDITED`;
const RESULTS_TEXT = 'Regression probe 87.5% reduction';
const RESULTS_TEXT_EDITED = 'Regression probe 91.2% reduction (edited)';

// A minimal but structurally valid single-page PDF — the upload field is required and the app
// derives a thumbnail from it, so an empty or fake-extension file is not a fair exercise.
function minimalPdf() {
  const objs = [
    '1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj',
    '2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj',
    '3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj',
    '4 0 obj<</Length 62>>stream\nBT /F1 12 Tf 20 100 Td (GCC regression fixture) Tj ET\nendstream endobj',
    '5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj',
  ];
  let pdf = '%PDF-1.4\n';
  const offsets = [];
  for (const o of objs) { offsets.push(pdf.length); pdf += o + '\n'; }
  const xref = pdf.length;
  pdf += `xref\n0 ${objs.length + 1}\n0000000000 65535 f \n`;
  for (const off of offsets) pdf += String(off).padStart(10, '0') + ' 00000 n \n';
  pdf += `trailer<</Size ${objs.length + 1}/Root 1 0 R>>\nstartxref\n${xref}\n%%EOF\n`;
  return Buffer.from(pdf, 'latin1');
}

const ymd = (d) => d.toISOString().slice(0, 10);

// Read one certificate back out of the internal tile API by number.
async function readBack(page, number) {
  const r = await page.request.get(`${BASE}/api/certificate-tiles-internal?search=${encodeURIComponent(number)}&page=1&pageSize=50`, { failOnStatusCode: false });
  if (r.status() !== 200) return { error: `HTTP ${r.status()}` };
  const b = await r.json();
  const hit = (b.certificates || []).find((c) => c.certificateNumber === number);
  return { hit, count: b.totalCount };
}

(async () => {
  ensureDirs();
  const { rec, save } = makeRecorder('04-create-edit');
  const startedAt = new Date().toISOString();
  console.log(`\n=== Phase 4: create / edit / delete — fixture ${MARKER} ===\n`);

  const pdfPath = path.join(ARTIFACT_DIR, `regression-fixture-${RUN_ID}.pdf`);
  fs.writeFileSync(pdfPath, minimalPdf());

  const browser = await chromium.launch({ headless: HEADLESS });
  let createdNumber = null;
  let analystS = null;

  try {
    analystS = await session(browser, 'analyst');
    const page = analystS.page;

    // ---------------- CREATE ----------------
    await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });

    // Company drives an AJAX product load, so it must settle before ProductId is selectable.
    const companyOpts = await page.$$eval('select[name="CompanyId"] option', (os) => os.map((o) => ({ v: o.value, t: o.textContent.trim() })).filter((o) => o.v));
    // Pick a company that actually has products, otherwise ProductId stays empty and the failure
    // reads as "create is broken" when it is really "this fixture picked a bad company".
    let chosenCompany = null, productOpts = [];
    for (const c of companyOpts.slice(0, 40)) {
      await page.selectOption('select[name="CompanyId"]', c.v);
      await page.waitForTimeout(900);
      productOpts = await page.$$eval('select[name="ProductId"] option', (os) => os.map((o) => ({ v: o.value, t: o.textContent.trim() })).filter((o) => o.v));
      if (productOpts.length) { chosenCompany = c; break; }
    }
    if (!chosenCompany) throw new Error('no company with selectable products found in the first 40 options');
    rec('crud.create.company-products', 'Company selection loads its products (AJAX)', 'PASS',
      `${chosenCompany.t} -> ${productOpts.length} products`);

    await page.selectOption('select[name="ProductId"]', productOpts[0].v);

    // CertificationTypeId's native <select> is display:none — the form replaces it with a
    // hierarchical picker widget (CertificationTypes is a tree: same Name can repeat at different
    // Levels under different parents). Playwright cannot selectOption a display:none element, so the
    // value is set directly and `change` is dispatched so the dependent subcategory loader still
    // runs. NOTE for the report: this one field is driven programmatically rather than by click, so
    // this phase does not exercise the picker widget's own UI — that is covered visually instead.
    const certTypeOpts = await page.$$eval('select[name="CertificationTypeId"] option', (os) => os.map((o) => o.value).filter(Boolean));
    if (!certTypeOpts.length) throw new Error('CertificationTypeId has no selectable options');
    await page.evaluate((v) => {
      const el = document.querySelector('[name="CertificationTypeId"]');
      el.value = v;
      el.dispatchEvent(new Event('change', { bubbles: true }));
    }, certTypeOpts[0]);
    await page.waitForTimeout(800);

    const today = new Date();
    const expiry = new Date(today.getTime() + 365 * 864e5);
    await page.fill('input[name="EffectiveDate"]', ymd(today));
    await page.fill('input[name="ExpirationDate"]', ymd(expiry));
    await page.selectOption('select[name="Status"]', 'Draft');
    await page.fill('input[name="Location"]', MARKER);
    // The optional Certification Results field (client requirement — analyst records the achieved
    // number in case Amazon requires a threshold). Round-tripping it is the point of the check.
    await page.fill('textarea[name="CertificationResults"]', RESULTS_TEXT);
    await page.setInputFiles('input[name="certificatePdf"]', pdfPath);

    // Submit via the form's own "Save" button. Two traps here, both of which produce a silent
    // no-op that reads like a rejected save:
    //   - the page's FIRST form is the nav's logout form, so a bare button[type=submit] resolves to
    //     a hidden logout button inside a dropdown;
    //   - a comma-joined selector like `form:has(X) button, form:has(Y) input` also matches the
    //     FORM elements themselves, and .first() then "clicks" the form.
    // Naming the form by id and the button by its accessible name avoids both.
    const t0 = Date.now();
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('#certificateCreateForm button[type="submit"]').first().click(),   // [0]="Save", [1]="Save & Add Another"
    ]);
    await page.waitForTimeout(1500);
    const createMs = Date.now() - t0;
    const landedAfterCreate = page.url().replace(BASE, '');

    // Auto-numbering is a requirement: Number was left blank, so the app must mint one.
    const m = /CertificateSingleView\/([^/?#]+)|CertificateInternalView\/([^/?#]+)|Edit\/([^/?#]+)/i.exec(landedAfterCreate);
    createdNumber = m ? decodeURIComponent(m[1] || m[2] || m[3]) : null;
    if (!createdNumber) {
      // Fall back to finding the fixture by its marker.
      const r = await page.request.get(`${BASE}/api/certificate-tiles-internal?search=${encodeURIComponent(MARKER)}&page=1&pageSize=50`, { failOnStatusCode: false });
      const b = r.status() === 200 ? await r.json() : { certificates: [] };
      createdNumber = (b.certificates || [])[0]?.certificateNumber || null;
    }
    if (!createdNumber) {
      // Landing back on Create means model validation rejected the post. Report WHICH field —
      // "create failed" on its own is unactionable and sends the next person re-running blind.
      const errs = await page.evaluate(() => {
        const out = [];
        document.querySelectorAll('.field-validation-error, .validation-summary-errors li, [data-valmsg-for]').forEach((e) => {
          const t = (e.textContent || '').trim();
          if (t) out.push((e.getAttribute('data-valmsg-for') || e.className.split(' ')[0]) + ': ' + t);
        });
        return [...new Set(out)];
      });
      const shot = path.join(ARTIFACT_DIR, `create-failed-${RUN_ID}.png`);
      await page.screenshot({ path: shot, fullPage: true }).catch(() => {});
      rec('crud.create', 'Analyst creates a certificate', 'FAIL',
        `landed ${landedAfterCreate} in ${createMs}ms; validation errors: ${errs.length ? errs.join(' | ') : '(none surfaced in the DOM)'}`);
      throw new Error(`create rejected: ${errs.join(' | ') || 'no validation message rendered'}`);
    }
    rec('crud.create', 'Analyst creates a certificate', 'PASS',
      `landed ${landedAfterCreate} in ${createMs}ms; number=${createdNumber}`);

    rec('crud.create.autonumber', 'Certificate number auto-generated (Number left blank)',
      /^\d{2}-\d{3,}$/.test(createdNumber) ? 'PASS' : 'WARN',
      `generated "${createdNumber}" (expected NN-NNNN form)`);

    // ---------------- READ BACK ----------------
    const rb = await readBack(page, createdNumber);
    rec('crud.create.persisted', 'Created certificate is readable through the internal API',
      rb.hit ? 'PASS' : 'FAIL', rb.hit ? `found ${createdNumber}` : `not found (${rb.error || 'no match'})`);
    if (rb.hit) {
      rec('crud.create.location', 'Location field persisted', rb.hit.location === MARKER ? 'PASS' : 'FAIL',
        `expected "${MARKER}", got "${rb.hit.location}"`);
      rec('crud.create.certresults', 'Certification Results persisted (client requirement)',
        (rb.hit.certificationResults || '') === RESULTS_TEXT ? 'PASS' : 'FAIL',
        `expected "${RESULTS_TEXT}", got "${rb.hit.certificationResults ?? '(absent)'}"`);
      rec('crud.create.status', 'Status persisted as Draft', rb.hit.status === 'Draft' ? 'PASS' : 'FAIL',
        `got "${rb.hit.status}"`);
      rec('crud.create.pdf', 'Uploaded PDF is linked to the certificate',
        rb.hit.pdfPath ? 'PASS' : 'FAIL', `pdfPath=${rb.hit.pdfPath || '(none)'}`);
    }

    // A Draft must NOT be publicly visible — the write path is also a visibility test.
    {
      const anonS = await session(browser, null);
      const pr = await anonS.page.request.get(`${BASE}/api/certificate-tiles?search=${encodeURIComponent(createdNumber)}&limit=50`, { failOnStatusCode: false });
      const pb = pr.status() === 200 ? await pr.json() : { certificates: [] };
      const leaked = (pb.certificates || []).some((c) => c.certificateNumber === createdNumber);
      rec('crud.create.draft-not-public', 'Draft certificate is not visible anonymously',
        leaked ? 'FAIL' : 'PASS', leaked ? `${createdNumber} is publicly listed` : 'not public');
      await anonS.ctx.close();
    }

    // ---------------- EDIT ----------------
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(createdNumber)}`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
    const editLoaded = await page.locator('input[name="Location"]').count();
    rec('crud.edit.loads', `Edit form loads for ${createdNumber}`, editLoaded ? 'PASS' : 'FAIL', `url=${page.url().replace(BASE, '')}`);

    if (editLoaded) {
      // Prefill is its own defect class — an Edit form that renders blank silently rewrites the
      // stored value on save.
      const prefillLoc = await page.inputValue('input[name="Location"]');
      const prefillRes = await page.locator('textarea[name="CertificationResults"]').count()
        ? await page.inputValue('textarea[name="CertificationResults"]') : '(field absent)';
      rec('crud.edit.prefill', 'Edit form prefills stored values',
        prefillLoc === MARKER && prefillRes === RESULTS_TEXT ? 'PASS' : 'FAIL',
        `Location="${prefillLoc}" CertificationResults="${prefillRes}"`);

      await page.fill('input[name="Location"]', MARKER_EDITED);
      await page.fill('textarea[name="CertificationResults"]', RESULTS_TEXT_EDITED);
      const t1 = Date.now();
      const editForm = (await page.locator('#certificateEditForm').count()) ? '#certificateEditForm' : 'form:has(input[name="Location"])';
      await Promise.all([
        page.waitForLoadState('domcontentloaded'),
        page.locator(`${editForm} button[type="submit"]`).first().click(),
      ]);
      await page.waitForTimeout(1500);
      const editMs = Date.now() - t1;

      const rb2 = await readBack(page, createdNumber);
      rec('crud.edit.persisted', 'Edit persisted', rb2.hit && rb2.hit.location === MARKER_EDITED ? 'PASS' : 'FAIL',
        `expected "${MARKER_EDITED}", got "${rb2.hit?.location ?? '(cert not found)'}" (${editMs}ms)`);
      rec('crud.edit.certresults', 'Certification Results edit persisted',
        rb2.hit && (rb2.hit.certificationResults || '') === RESULTS_TEXT_EDITED ? 'PASS' : 'FAIL',
        `expected "${RESULTS_TEXT_EDITED}", got "${rb2.hit?.certificationResults ?? '(absent)'}"`);
      // An edit must not silently mint a second row — the duplicate-number problem this dataset
      // already has makes that failure mode worth asserting explicitly.
      rec('crud.edit.no-duplicate', 'Edit did not create a second certificate',
        rb2.count === 1 ? 'PASS' : 'WARN', `search for ${createdNumber} returns ${rb2.count} rows`);
    }

    // ---------------- DELETE IS ADMIN-ONLY ----------------
    {
      const del = await page.request.get(`${BASE}/Certificate/Delete/${encodeURIComponent(createdNumber)}`, { failOnStatusCode: false, maxRedirects: 0 });
      const denied = del.status() === 401 || del.status() === 403 || (del.status() >= 300 && del.status() < 400 && /Account\/(Login|AccessDenied)/i.test(del.headers()['location'] || ''));
      rec('crud.delete.analyst-denied', 'Analyst cannot reach certificate Delete (AdminOnly)',
        denied ? 'PASS' : 'FAIL', `HTTP ${del.status()} location=${del.headers()['location'] || '-'}`);
    }
  } catch (e) {
    rec('crud.fatal', 'Write round-trip ran without throwing', 'FAIL', e.message);
  } finally {
    // ---------------- TEARDOWN (as Admin) ----------------
    if (analystS) await analystS.ctx.close().catch(() => {});
    if (createdNumber) {
      try {
        const adminS = await session(browser, 'admin');
        const p = adminS.page;
        await p.goto(`${BASE}/Certificate/Delete/${encodeURIComponent(createdNumber)}`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
        const tok = await p.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => null);
        const resp = await p.request.post(`${BASE}/Certificate/Delete/${encodeURIComponent(createdNumber)}`, {
          form: { number: createdNumber, __RequestVerificationToken: tok || '' },
          failOnStatusCode: false,
        });
        const gone = await readBack(p, createdNumber);
        rec('crud.delete', `Admin deletes fixture ${createdNumber}`,
          !gone.hit ? 'PASS' : 'FAIL', `POST ${resp.status()}; still present=${!!gone.hit}`);
        rec('crud.teardown', 'Fixture removed from the test environment',
          !gone.hit ? 'PASS' : 'FAIL',
          gone.hit ? `MANUAL CLEANUP REQUIRED: certificate ${createdNumber} (Location ${MARKER_EDITED})` : 'clean');
        await adminS.ctx.close();
      } catch (e) {
        rec('crud.teardown', 'Fixture removed from the test environment', 'FAIL',
          `MANUAL CLEANUP REQUIRED: certificate ${createdNumber} — ${e.message}`);
      }
    }
    await browser.close();
    save({ startedAt, finishedAt: new Date().toISOString(), runId: RUN_ID, marker: MARKER, createdNumber });
  }
})().catch((e) => { console.error('FATAL', e); process.exit(1); });
