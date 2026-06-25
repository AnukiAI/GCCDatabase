// CR-023 — end-to-end UI validation of Certificate Import batch UNDO.
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/_cr023-import-undo-validate.js
//
// Flow (admin):
//   1. Import #1 creates cert ZZ-9001 under "CR023 Undo Co A"            (no acks)
//   2. Import #2 overwrites ZZ-9001 -> "CR023 Undo Co B" AND creates ZZ-9002  (ack overwrite + reassign)
//   3. Open Import History, Undo batch #2
//   4. Assert (DB ground truth): ZZ-9001 restored to Co A, ZZ-9002 removed, batch = UNDONE,
//      audit has RESTORE + DELETE rows. Then clean up the throwaway ZZ-90xx fixtures.
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');
const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');

const SQLCMD = 'sqlcmd';
const SQL_ARGS = ['-S', 'ANUKI01', '-d', 'GCCDatabase_Dev', '-U', 'GCC_DevUser', '-P', 'DevP@ss2024!SQL', '-h', '-1', '-W', '-s', '|'];
const OUT = path.join(__dirname, '_results', 'cr023');
fs.mkdirSync(OUT, { recursive: true });

const COA = 'CR023 Undo Co A';
const COB = 'CR023 Undo Co B';
const CT = 'Carbon Footprint Reduction';
const PROD = 'CR023 Test Product';

const results = [];
function rec(id, status, notes = '') {
  results.push({ id, status, notes });
  console.log(`  [${status}] ${id}${notes ? ' :: ' + notes : ''}`);
}
function sql(q) {
  // QUOTED_IDENTIFIER ON is required for DML against Certificates (indexed computed Normalized* cols, CR-187).
  return execFileSync(SQLCMD, [...SQL_ARGS, '-Q', 'SET QUOTED_IDENTIFIER ON; SET NOCOUNT ON; ' + q], { encoding: 'utf8' }).trim();
}
function certCompany(num) {
  const out = sql(`SELECT co.Name FROM Certificates c JOIN Companies co ON co.Id=c.CompanyId WHERE c.Number='${num}'`);
  return out.replace(/\r?\n/g, '').trim();
}
function certExists(num) {
  return parseInt(sql(`SELECT COUNT(*) FROM Certificates WHERE Number='${num}'`).trim(), 10) > 0;
}

const HEADER = '"Certificate #","Certification Type","Status (x)","Company","Product Name","Effective Date","Expiration Date"';
function row(num, company) {
  return `"${num}","${CT}","Draft","${company}","${PROD}","2026-01-01","2027-01-01"`;
}
function writeCsv(name, rows) {
  const p = path.join(os.tmpdir(), name);
  fs.writeFileSync(p, [HEADER, ...rows].join('\n'), 'utf8');
  return p;
}

// Navigate resiliently: a just-submitted Apply redirect can still be in flight, which
// interrupts an immediate goto. Wait for the page to go idle first, then retry once.
async function gotoStable(page, url) {
  await page.waitForLoadState('load').catch(() => {});
  await page.waitForLoadState('networkidle').catch(() => {});
  try {
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
  } catch (e) {
    await page.waitForTimeout(1000);
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 60000 });
  }
}

async function doImport(page, csvPath, { ackOverwrite = false, ackReassign = false, reason = 'CR-023 validation' } = {}) {
  await gotoStable(page, BASE + '/CertificateImport');
  await page.setInputFiles('#csvFile', csvPath);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('#uploadForm button[type="submit"]').click(),
  ]);
  // Now on Preview. Tick acknowledgements if the warnings are present.
  if (ackOverwrite && await page.locator('#acknowledgeOverwrites').count()) {
    await page.locator('#acknowledgeOverwrites').check();
  }
  if (ackReassign && await page.locator('#acknowledgeReassignments').count()) {
    await page.locator('#acknowledgeReassignments').check();
  }
  await page.locator('#reason').fill(reason);
  // Submit the form element directly — bypasses the JS confirm modal (server still enforces acks).
  await Promise.all([
    page.waitForLoadState('load'),
    page.evaluate(() => document.getElementById('applyForm').submit()),
  ]);
  await page.waitForLoadState('networkidle').catch(() => {});
  return page.url();
}

(async () => {
  // Pre-clean any leftovers from a prior run.
  cleanup();

  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 1200 } });
  const page = await ctx.newPage();

  try {
    await login(page, 'admin');

    // --- 1. Import #1: create ZZ-9001 under Co A ---
    const csv1 = writeCsv('cr023_1.csv', [row('ZZ-9001', COA)]);
    await doImport(page, csv1);
    rec('import1.created', certExists('ZZ-9001') ? 'PASS' : 'FAIL', `ZZ-9001 exists`);
    rec('import1.company', certCompany('ZZ-9001') === COA ? 'PASS' : 'FAIL', `company=${certCompany('ZZ-9001')}`);

    // --- 2. Import #2: overwrite ZZ-9001 -> Co B AND create ZZ-9002 ---
    const csv2 = writeCsv('cr023_2.csv', [row('ZZ-9001', COB), row('ZZ-9002', COA)]);
    await gotoStable(page, BASE + '/CertificateImport');
    await page.setInputFiles('#csvFile', csv2);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('#uploadForm button[type="submit"]').click()]);
    await page.screenshot({ path: path.join(OUT, 'preview-overwrite-reassign.png'), fullPage: true });
    rec('import2.overwrite-warning', await page.locator('#acknowledgeOverwrites').count() ? 'PASS' : 'FAIL', 'overwrite ack shown');
    rec('import2.reassign-warning', await page.locator('#acknowledgeReassignments').count() ? 'PASS' : 'FAIL', 'reassign ack shown');
    if (await page.locator('#acknowledgeOverwrites').count()) await page.locator('#acknowledgeOverwrites').check();
    if (await page.locator('#acknowledgeReassignments').count()) await page.locator('#acknowledgeReassignments').check();
    await page.locator('#reason').fill('CR-023 validation overwrite+create');
    await Promise.all([page.waitForLoadState('load'), page.evaluate(() => document.getElementById('applyForm').submit())]);
    await page.waitForLoadState('networkidle').catch(() => {});

    rec('import2.reassigned', certCompany('ZZ-9001') === COB ? 'PASS' : 'FAIL', `ZZ-9001 company=${certCompany('ZZ-9001')}`);
    rec('import2.created', certExists('ZZ-9002') ? 'PASS' : 'FAIL', 'ZZ-9002 created');

    // --- 3. Import History: find the latest batch + Undo it ---
    await gotoStable(page, BASE + '/CertificateImport/History');
    await page.screenshot({ path: path.join(OUT, 'history-before-undo.png'), fullPage: true });
    const undoForms = page.locator('.undo-import-form');
    rec('history.has-undo', await undoForms.count() > 0 ? 'PASS' : 'FAIL', `${await undoForms.count()} undoable batch(es)`);
    const batchId = await undoForms.first().locator('input[name="batchId"]').inputValue();
    rec('history.batchid', /^[0-9a-f-]{36}$/i.test(batchId) ? 'PASS' : 'FAIL', batchId);

    // Submit the first (most recent) undo form directly (bypass the confirm modal).
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.evaluate(() => document.querySelector('.undo-import-form').submit()),
    ]);
    await page.waitForTimeout(900);

    // --- 4. Assert rollback via DB ground truth ---
    rec('undo.restored-company', certCompany('ZZ-9001') === COA ? 'PASS' : 'FAIL', `ZZ-9001 company=${certCompany('ZZ-9001')} (expect ${COA})`);
    rec('undo.created-removed', !certExists('ZZ-9002') ? 'PASS' : 'FAIL', 'ZZ-9002 removed');
    const batchStatus = sql(`SELECT Status FROM ImportBatches WHERE Id='${batchId}'`).trim();
    rec('undo.batch-status', batchStatus === 'UNDONE' ? 'PASS' : 'FAIL', `status=${batchStatus}`);
    const restoreCount = sql(`SELECT COUNT(*) FROM AuditHistory WHERE Action='RESTORE' AND EntityType='Certificate' AND Reason LIKE '%${batchId}%'`).trim();
    rec('undo.audit-restore', parseInt(restoreCount, 10) >= 1 ? 'PASS' : 'FAIL', `RESTORE rows=${restoreCount}`);
    const deleteCount = sql(`SELECT COUNT(*) FROM AuditHistory WHERE Action='DELETE' AND EntityType='Certificate' AND Reason LIKE '%${batchId}%'`).trim();
    rec('undo.audit-delete', parseInt(deleteCount, 10) >= 1 ? 'PASS' : 'FAIL', `DELETE rows=${deleteCount}`);

    await gotoStable(page, BASE + '/CertificateImport/History');
    await page.screenshot({ path: path.join(OUT, 'history-after-undo.png'), fullPage: true });
    const undoneBadge = await page.locator('span.badge:has-text("Undone")').count();
    rec('history.shows-undone', undoneBadge >= 1 ? 'PASS' : 'FAIL', `${undoneBadge} Undone badge(s)`);

    // --- 5. Idempotency: the undone batch must no longer be undoable ---
    const stillUndoable = await page.locator(`.undo-import-form input[value="${batchId}"]`).count();
    rec('undo.idempotent', stillUndoable === 0 ? 'PASS' : 'FAIL', 'no Undo button on undone batch');

  } catch (err) {
    rec('exception', 'FAIL', err.message);
    console.error(err);
  } finally {
    await browser.close();
    cleanup();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(results, null, 2));
  console.log(`\nCR-023 UNDO VALIDATION: ${pass} passed, ${fail} failed.  Screenshots+json in ${OUT}`);
  process.exit(fail === 0 ? 0 : 1);
})();

function cleanup() {
  try {
    sql(`
      DECLARE @ids TABLE(Id INT);
      INSERT INTO @ids SELECT Id FROM Certificates WHERE Number LIKE 'ZZ-90%';
      DELETE FROM CertificateStatusHistory WHERE CertificateId IN (SELECT Id FROM @ids);
      DELETE FROM CertificateProductIds WHERE CertificateId IN (SELECT Id FROM @ids);
      DELETE FROM CustomFieldValues WHERE CertificateId IN (SELECT Id FROM @ids);
      DELETE FROM CertificateDocuments WHERE CertificateId IN (SELECT Id FROM @ids);
      DELETE FROM Certificates WHERE Id IN (SELECT Id FROM @ids);
      DELETE FROM AuditHistory WHERE EntityType='Certificate' AND ChangeSource IN ('Import','System') AND (Reason LIKE '%import%' OR Reason LIKE '%Undo of import%') AND CAST(EntityId AS varchar) NOT IN (SELECT CAST(Id AS varchar) FROM Certificates);
      DELETE FROM ImportBatches WHERE FileName LIKE 'cr023_%';
      DELETE FROM Products WHERE Name='${PROD}';
      DELETE FROM Companies WHERE Name IN ('${COA}','${COB}');
    `);
  } catch (e) {
    console.log('  (cleanup note: ' + e.message.split('\n')[0] + ')');
  }
}
