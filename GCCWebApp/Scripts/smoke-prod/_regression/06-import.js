// Phase 6 — certificate CSV import: template -> upload -> preview -> apply -> verify -> undo.
//
// SAFETY. Imports exactly ONE self-created row, marked ZZ_RegressionImport_<runId> in Location, and
// undoes the batch afterwards; an admin delete is the backstop if undo does not remove it. No
// existing certificate is targeted, so the reassignment/overwrite acknowledgement paths are
// deliberately NOT exercised — those rewrite real client rows and need a CR, not a smoke test.
// That gap is reported rather than quietly skipped.

const fs = require('fs');
const path = require('path');
const { BASE, HEADLESS, NAV_TIMEOUT, ARTIFACT_DIR, session, makeRecorder, chromium, ensureDirs } = require('./_lib/regression-lib');

const RUN_ID = `${new Date().toISOString().slice(0, 10)}_${Date.now().toString(36)}`;
const MARKER = `ZZ_RegressionImport_${RUN_ID}`;
// Import requires an explicit Certificate # (see imp.autonumber-parity below), so the fixture
// carries one well outside the migrated dataset's range.
const FIXTURE_NUMBER = `26-99${String(Math.floor(Date.now() / 1000) % 100).padStart(2, '0')}`;
const ymd = (d) => d.toISOString().slice(0, 10);
const q = (v) => `"${String(v ?? '').replace(/"/g, '""')}"`;

async function readBack(page, number) {
  const r = await page.request.get(`${BASE}/api/certificate-tiles-internal?search=${encodeURIComponent(number)}&page=1&pageSize=50`, { failOnStatusCode: false });
  if (r.status() !== 200) return { error: `HTTP ${r.status()}` };
  const b = await r.json();
  return { hit: (b.certificates || []).find((c) => c.certificateNumber === number), count: b.totalCount };
}

(async () => {
  ensureDirs();
  const { rec, save } = makeRecorder('06-import');
  const startedAt = new Date().toISOString();
  console.log(`\n=== Phase 6: certificate import — fixture ${MARKER} ===\n`);

  const browser = await chromium.launch({ headless: HEADLESS });
  let s = null, importedNumber = null, batchId = null;

  try {
    s = await session(browser, 'analyst');
    const page = s.page;

    // ---- 1. Template ----
    const tr = await page.request.get(`${BASE}/CertificateImport/DownloadTemplate`, { failOnStatusCode: false });
    const templateText = await tr.text();
    const header = templateText.split(/\r?\n/)[0];
    rec('imp.template', 'Import template downloads as CSV', tr.status() === 200 && /csv/i.test(tr.headers()['content-type'] || '') ? 'PASS' : 'FAIL',
      `HTTP ${tr.status()}, ${header.split('","').length} columns`);
    // The template doubles as the status vocabulary the client sees; the workflow has 8 states.
    const statesInTemplate = ['Draft', 'Client Approved - Not Paid', 'Client Approved - Confidential', 'Client Approved - Paid', 'Hold', 'Expired', 'Republished', 'Expired - No Annual Verification'];
    const missing = statesInTemplate.filter((st) => !header.includes(st));
    rec('imp.template.states', 'Template documents all 8 workflow states',
      missing.length === 0 ? 'PASS' : 'FAIL', missing.length ? `missing: ${missing.join(', ')}` : 'all 8 present');

    // ---- 2. Build a one-row CSV using dimensions that already exist ----
    // Cloning a real company/type/product combo makes CR-186 find-or-create RESOLVE instead of
    // spawning new product rows, which would be pollution this phase cannot clean up.
    const src = await page.request.get(`${BASE}/api/certificate-tiles-internal?page=1&pageSize=1`, { failOnStatusCode: false });
    const srcRow = (await src.json()).certificates[0];
    const today = new Date(), expiry = new Date(Date.now() + 365 * 864e5);
    const cols = header.replace(/^﻿/, '').split('","').map((c) => c.replace(/^"|"$/g, ''));
    const value = (col) => {
      if (/^Certificate #/i.test(col)) return FIXTURE_NUMBER;
      if (/^Certification Type/i.test(col)) return srcRow.certificationType;
      if (/^Status/i.test(col)) return 'Draft';
      if (/^Company$/i.test(col)) return srcRow.companyName;
      if (/^Product Name/i.test(col)) return srcRow.productName;
      if (/^Product Category/i.test(col)) return srcRow.productCategory;
      if (/^Product Type/i.test(col)) return srcRow.productType;
      if (/^Subcategory/i.test(col)) return srcRow.productSubcategory;
      if (/^Effective Date/i.test(col)) return ymd(today);
      if (/^Expiration Date/i.test(col)) return ymd(expiry);
      if (/^Location/i.test(col)) return MARKER;
      if (/^Cert Results/i.test(col)) return 'Imported by regression run';
      return '';
    };
    const csv = header + '\r\n' + cols.map((c) => q(value(c))).join(',') + '\r\n';
    const csvPath = path.join(ARTIFACT_DIR, `import-fixture-${RUN_ID}.csv`);
    fs.writeFileSync(csvPath, csv, 'utf8');
    rec('imp.fixture', 'One-row import CSV built from an existing company/product combo', 'INFO',
      `company="${srcRow.companyName}" type="${srcRow.certificationType}" product="${srcRow.productName}"`);

    // Read the preview's own counters rather than page text — the layout contains the word "new"
    // and a digit "1" no matter what happened, so a loose text match passes on a rejected import.
    const previewCounts = async () => page.evaluate(() => {
      const t = document.body.innerText.replace(/\s+/g, ' ');
      const g = (label) => { const m = new RegExp('(\\d+)\\s+' + label, 'i').exec(t); return m ? parseInt(m[1], 10) : null; };
      return {
        total: g('Total Rows'), added: g('New'), modified: g('Modified'), errors: g('Errors'),
        cannotApply: /Cannot Apply Import/i.test(t),
        errorText: (/Validation Errors[\s\S]{0,240}/i.exec(t) || [''])[0].replace(/\s+/g, ' ').slice(0, 240),
      };
    });
    const upload = async (file) => {
      await page.goto(`${BASE}/CertificateImport/Index`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
      await page.setInputFiles('input[name="csvFile"]', file);
      await Promise.all([
        page.waitForLoadState('domcontentloaded'),
        page.locator('form:has(input[name="csvFile"]) button[type="submit"]').first().click(),
      ]);
      await page.waitForTimeout(1200);
    };

    // ---- 3a. Does import auto-number a blank Certificate #, the way Create does? ----
    // /Certificate/Create mints a number when Number is left blank (phase 4 confirmed it), and the
    // template annotates Status with "defaults to Draft if blank" but marks nothing on Certificate #.
    // So post a blank one explicitly rather than assume either behaviour.
    const blankCsv = header + '\r\n' + cols.map((c) => q(/^Certificate #/i.test(c) ? '' : value(c))).join(',') + '\r\n';
    const blankPath = path.join(ARTIFACT_DIR, `import-blanknum-${RUN_ID}.csv`);
    fs.writeFileSync(blankPath, blankCsv, 'utf8');
    await upload(blankPath);
    const blank = await previewCounts();
    rec('imp.autonumber-parity', 'Import auto-numbers a blank Certificate # (parity with the Create form)',
      blank.cannotApply || blank.errors ? 'FAIL' : 'PASS',
      blank.cannotApply || blank.errors
        ? `import rejects a blank Certificate # (${blank.errorText}); /Certificate/Create mints one automatically, so the two write paths disagree and the template does not mark the column mandatory`
        : 'blank number accepted and auto-assigned');

    // ---- 3b. Upload the real fixture -> Preview ----
    await upload(csvPath);
    const previewUrl = page.url().replace(BASE, '');
    const counts = await previewCounts();
    rec('imp.upload', 'CSV upload reaches the Preview step',
      counts.total !== null ? 'PASS' : 'FAIL', `landed ${previewUrl}`);
    rec('imp.preview.classification', 'Preview classifies the row as 1 new certificate with 0 errors',
      counts.total === 1 && counts.added === 1 && counts.errors === 0 && !counts.cannotApply ? 'PASS' : 'FAIL',
      `total=${counts.total} new=${counts.added} modified=${counts.modified} errors=${counts.errors} cannotApply=${counts.cannotApply}${counts.errors ? ' :: ' + counts.errorText : ''}`);

    const importId = await page.locator('input[name="importId"]').first().inputValue().catch(() => null);
    rec('imp.preview.importId', 'Preview carries an importId for Apply', importId ? 'PASS' : 'FAIL', importId || '(none)');

    // ---- 4. Apply ----
    if (importId) {
      const tok = await page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
      const ap = await page.request.post(`${BASE}/CertificateImport/Apply`, {
        form: { importId, reason: `Full regression run ${RUN_ID}`, acknowledgeReassignments: 'false', acknowledgeOverwrites: 'false', __RequestVerificationToken: tok },
        failOnStatusCode: false,
      });
      rec('imp.apply', 'Import applies', ap.status() < 400 ? 'PASS' : 'FAIL', `HTTP ${ap.status()}`);

      // Find the imported row by its number, falling back to the Location marker.
      const byNum = await readBack(page, FIXTURE_NUMBER);
      let hit = byNum.hit;
      if (!hit) {
        const found = await page.request.get(`${BASE}/api/certificate-tiles-internal?search=${encodeURIComponent(MARKER)}&page=1&pageSize=10`, { failOnStatusCode: false });
        const fb = found.status() === 200 ? await found.json() : { certificates: [] };
        hit = (fb.certificates || [])[0];
      }
      importedNumber = hit?.certificateNumber || null;
      rec('imp.apply.persisted', 'Imported certificate is readable through the internal API',
        hit ? 'PASS' : 'FAIL', hit ? `number=${importedNumber} location="${hit.location}"` : `neither ${FIXTURE_NUMBER} nor the marker found`);
      if (hit) {
        rec('imp.apply.number', 'Imported certificate keeps the number given in the CSV',
          importedNumber === FIXTURE_NUMBER ? 'PASS' : 'FAIL', `sent "${FIXTURE_NUMBER}", stored "${importedNumber}"`);
        rec('imp.apply.certresults', 'Cert Results column imported',
          (hit.certificationResults || '').includes('Imported by regression run') ? 'PASS' : 'FAIL',
          `got "${hit.certificationResults ?? '(absent)'}"`);
        rec('imp.apply.location', 'Location column imported',
          (hit.location || '') === MARKER ? 'PASS' : 'FAIL', `got "${hit.location ?? '(absent)'}"`);
        rec('imp.apply.status', 'Status column imported as Draft',
          hit.status === 'Draft' ? 'PASS' : 'FAIL', `got "${hit.status}"`);
      }

      // ---- 5. History + Undo ----
      await page.goto(`${BASE}/CertificateImport/History`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
      const hist = (await page.locator('body').innerText()).replace(/\s+/g, ' ');
      rec('imp.history', 'Import appears in History with its reason',
        hist.includes(RUN_ID) ? 'PASS' : 'WARN', hist.includes(RUN_ID) ? 'batch reason found' : 'reason not visible on the page');

      const guid = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i.exec(
        await page.content().then((h) => { const i = h.indexOf(RUN_ID); return i > 0 ? h.slice(Math.max(0, i - 1500), i + 500) : h; })
      );
      batchId = guid ? guid[0] : null;
      if (batchId) {
        const tok2 = await page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
        const un = await page.request.post(`${BASE}/CertificateImport/UndoImport`, {
          form: { batchId, __RequestVerificationToken: tok2 }, failOnStatusCode: false,
        });
        const after = importedNumber ? await readBack(page, importedNumber) : { hit: null };
        rec('imp.undo', 'Undo removes the imported certificate',
          un.status() < 400 && !after.hit ? 'PASS' : 'FAIL',
          `POST ${un.status()}; still present=${!!after.hit}`);
      } else {
        rec('imp.undo', 'Undo removes the imported certificate', 'WARN', 'batchId not discoverable from the History page markup');
      }
    }

    rec('imp.coverage.gap', 'Reassignment / overwrite acknowledgement paths', 'INFO',
      'NOT exercised — those paths rewrite existing client rows and need a CR, not a smoke run');
  } catch (e) {
    rec('imp.fatal', 'Import round-trip ran without throwing', 'FAIL', e.message);
  } finally {
    // Backstop cleanup as Admin, whatever happened above.
    if (importedNumber) {
      try {
        const a = await session(browser, 'admin');
        const still = await readBack(a.page, importedNumber);
        if (still.hit) {
          await a.page.goto(`${BASE}/Certificate/Delete/${encodeURIComponent(importedNumber)}`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
          const tok = await a.page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
          await a.page.request.post(`${BASE}/Certificate/Delete/${encodeURIComponent(importedNumber)}`, {
            form: { number: importedNumber, __RequestVerificationToken: tok }, failOnStatusCode: false,
          });
        }
        const gone = await readBack(a.page, importedNumber);
        rec('imp.teardown', 'Imported fixture removed from the test environment',
          !gone.hit ? 'PASS' : 'FAIL',
          gone.hit ? `MANUAL CLEANUP REQUIRED: certificate ${importedNumber} (Location ${MARKER})` : 'clean');
        await a.ctx.close();
      } catch (e) {
        rec('imp.teardown', 'Imported fixture removed from the test environment', 'FAIL',
          `MANUAL CLEANUP REQUIRED: certificate ${importedNumber} — ${e.message}`);
      }
    }
    if (s) await s.ctx.close().catch(() => {});
    await browser.close();
    save({ startedAt, finishedAt: new Date().toISOString(), runId: RUN_ID, marker: MARKER, importedNumber, batchId });
  }
})().catch((e) => { console.error('FATAL', e); process.exit(1); });
