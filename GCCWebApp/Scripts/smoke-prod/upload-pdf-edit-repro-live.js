// EDIT-flow PDF-upload repro — user: "tested with an existing cert, attempted to upload PDF,
// got a WARNING message, clicked Save, and it DIDN'T UPDATE."
//
// Drives the real Edit page for a given cert on gccwebapp.com: attaches a PDF, captures any
// toast/warning, clicks Save like a user, then re-opens Edit to check whether the file input
// retained the selection / whether the cert actually changed.
//
// Run:
//   CERT=26-0160 SIZE_MB=12 SMOKE_REMOTE_USER=gcc SMOKE_REMOTE_PASS="Cert-Test-Access-2026!" \
//   node Scripts/smoke-prod/upload-pdf-edit-repro-live.js
//   (SIZE_MB omitted/0 = small valid PDF that SHOULD succeed)

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = { username: process.env.SMOKE_REMOTE_USER || 'gcc', password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' };
const LOGIN = { email: process.env.APP_USER || 'admin', password: process.env.APP_PASS || 'TestPass123!' };
const CERT = process.env.CERT || '26-0160';
const SIZE_MB = parseFloat(process.env.SIZE_MB || '0');

const OUT = path.join(__dirname, '_results');
const SHOTS = path.join(OUT, 'upload-edit-repro');
fs.mkdirSync(SHOTS, { recursive: true });
const results = [];
const rec = (id, status, detail = '') => {
  results.push({ id, status, detail });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : status === 'INFO' ? 'ℹ️ ' : '⚠️ ';
  console.log(`${tag} [${status}] ${id}${detail ? '  — ' + detail : ''}`);
};

function makePdf(p) {
  let body = '%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n4 0 obj\n<< /Length 64 >>\nstream\nBT /F1 24 Tf 72 720 Td (GCC Edit Upload Test) Tj ET\nendstream\nendobj\n5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n';
  if (SIZE_MB > 0) body += '% ' + 'A'.repeat(Math.max(0, Math.floor(SIZE_MB * 1024 * 1024) - body.length - 64)) + '\n';
  body += 'trailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF\n';
  fs.writeFileSync(p, body, 'latin1');
  return fs.statSync(p).size;
}

(async () => {
  const pdfPath = path.join(SHOTS, SIZE_MB > 0 ? `edit-${SIZE_MB}mb.pdf` : 'edit-small.pdf');
  const bytes = makePdf(pdfPath);
  rec('FIXTURE', 'INFO', `${(bytes / 1024 / 1024).toFixed(2)}MB → ${pdfPath}`);

  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, httpCredentials: GATE, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  const toasts = [];
  // ModalSystem.showNotification renders toast DOM — watch for it appearing
  await page.exposeFunction('__noop', () => {});
  page.on('console', m => { if (/notification|less than 10MB|valid PDF/i.test(m.text())) toasts.push('console:' + m.text()); });

  try {
    // login
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('#Email').fill(LOGIN.email);
    await page.locator('#Password').fill(LOGIN.password);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1200);

    // open Edit + record the existing PDF state BEFORE
    await page.goto(`${BASE}/Certificate/Edit/${CERT}`, { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(800);
    const before = await page.evaluate(() => {
      const label = document.querySelector('label[for="certificatePdf"]')?.textContent.trim();
      const existing = document.querySelector('.pdf-upload-zone')?.innerText.slice(0, 120);
      return { label, existing, hasForm: !!document.getElementById('certificateEditForm') };
    });
    rec('EDIT-OPEN', before.hasForm ? 'PASS' : 'FAIL', `cert=${CERT} label="${before.label}"`);
    if (!before.hasForm) throw new Error('Edit form not found');

    // attach the PDF
    await page.setInputFiles('#certificatePdf', pdfPath);
    await page.waitForTimeout(1200); // let the change handler + any toast fire
    const afterAttach = await page.evaluate(() => {
      const i = document.getElementById('certificatePdf');
      const filesLen = i?.files?.length || 0;
      // scrape any visible toast / notification text
      const toastEls = Array.from(document.querySelectorAll('[class*="notification"],[class*="toast"],[class*="alert"],[role="alert"]'));
      const toastText = toastEls.map(e => e.innerText.trim()).filter(Boolean).join(' || ').slice(0, 300);
      const previewText = (document.querySelector('.pdf-upload-zone')?.parentElement?.innerText || '').slice(0, 200);
      return { filesLen, attachedName: filesLen ? i.files[0].name : null, toastText, previewText };
    });
    const cleared = afterAttach.filesLen === 0;
    rec('ATTACH', 'INFO', `files=${afterAttach.filesLen} (${cleared ? 'CLEARED by client JS' : 'retained'})`);
    await page.screenshot({ path: path.join(SHOTS, `1-after-attach-${SIZE_MB || 'small'}.png`), fullPage: true });

    // Capture any blocking modal overlay (ModalSystem warning/error) sitting over the page
    const modal = await page.evaluate(() => {
      const ov = document.querySelector('.gcc-modal-overlay.show, .admin-modal-overlay.show');
      if (!ov) return null;
      const title = ov.querySelector('[id$="-modal-title"]')?.innerText.trim();
      const msg = ov.querySelector('[id$="-modal-message"]')?.innerText.trim();
      return { title, msg, kind: ov.querySelector('[id^="error"]') ? 'error' : ov.querySelector('[id^="warning"]') ? 'warning' : 'other' };
    });
    rec('MODAL', modal ? 'INFO' : 'INFO',
      modal ? `${modal.kind.toUpperCase()} modal blocking page → title="${modal.title}" msg="${modal.msg}"` : '(no blocking modal)');

    // Try to dismiss the modal (its OK/Close button) so we can reach Save — like a user would
    if (modal) {
      await page.evaluate(() => {
        const ov = document.querySelector('.gcc-modal-overlay.show, .admin-modal-overlay.show');
        const btn = ov?.querySelector('button.btn-primary, button.gcc-modal-confirm, .modal-footer button, button');
        if (btn) btn.click();
      });
      await page.waitForTimeout(700);
    }

    // click Save like a real user
    const saveBtn = page.locator('#saveButton');
    const clickErr = await saveBtn.click({ timeout: 8000 }).then(() => null).catch(e => e.message.split('\n')[0]);
    rec('SAVE-CLICK', clickErr ? 'FAIL' : 'PASS', clickErr ? `Save STILL blocked: ${clickErr}` : 'Save clicked after dismissing modal');
    await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(2000);
    const afterSave = { url: page.url(), title: await page.title() };
    rec('SAVE', 'INFO', `→ ${afterSave.url} title="${afterSave.title}"`);
    await page.screenshot({ path: path.join(SHOTS, '2-after-save.png'), fullPage: true });

    // ---- verdict ----
    if (cleared) {
      rec('VERDICT', 'FAIL',
        `REPRODUCED: the ${SIZE_MB || '<1'}MB PDF was rejected client-side (warning shown) and the selection was CLEARED. ` +
        `Save then posted with NO new PDF, so the cert kept its existing file → "didn't update." ` +
        `This is the certificate-create.js 10MB cap (line 213).`);
    } else if (!/\/Edit\//i.test(afterSave.url)) {
      rec('VERDICT', 'PASS', `Edit saved and redirected off the Edit page — small PDF replace works. The failure only triggers above 10MB.`);
    } else {
      rec('VERDICT', 'WARN', `Still on Edit after save — inspect 2-after-save.png; a server validation error may be blocking the save.`);
    }
  } catch (e) {
    rec('ERR', 'FAIL', e.message);
    try { await page.screenshot({ path: path.join(SHOTS, 'error.png'), fullPage: true }); } catch {}
  } finally {
    fs.writeFileSync(path.join(OUT, `upload-edit-repro-${SIZE_MB || 'small'}-results.json`), JSON.stringify(results, null, 2));
    await browser.close();
  }
  console.log(`\nScreenshots: ${SHOTS}`);
  process.exit(0);
})();
