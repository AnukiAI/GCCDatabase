// DIAGNOSE "saved but image not saving" on gccwebapp.com (post DEF-261 deploy).
// Uploads a valid PDF to a cert, saves, then inspects the detail page:
//   - did the PDF itself store (downloadable)?
//   - did CertificateImagePath / preview thumbnail render, or fall back to the
//     "Certificate Unavailable" placeholder? (PDF thumbnail = Docnet/pdfium native render)
//
// Run:
//   CERT=26-0160 SMOKE_REMOTE_USER=gcc SMOKE_REMOTE_PASS="Cert-Test-Access-2026!" \
//   node Scripts/smoke-prod/upload-image-diagnose-live.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = { username: process.env.SMOKE_REMOTE_USER || 'gcc', password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' };
const LOGIN = { email: process.env.APP_USER || 'admin', password: process.env.APP_PASS || 'TestPass123!' };
const CERT = process.env.CERT || '26-0160';
const OUT = path.join(__dirname, '_results');
const SHOTS = path.join(OUT, 'image-diagnose');
fs.mkdirSync(SHOTS, { recursive: true });
const results = [];
const rec = (id, status, detail = '') => {
  results.push({ id, status, detail });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : 'ℹ️ ';
  console.log(`${tag} [${status}] ${id}${detail ? '  — ' + detail : ''}`);
};

function makePdf(p) {
  const body = '%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n4 0 obj\n<< /Length 70 >>\nstream\nBT /F1 24 Tf 72 700 Td (GCC Image Diagnose Test) Tj ET\nendstream\nendobj\n5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\ntrailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF\n';
  fs.writeFileSync(p, body, 'latin1');
}

(async () => {
  const pdf = path.join(SHOTS, 'diag.pdf'); makePdf(pdf);
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, httpCredentials: GATE, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  const imgFails = [];
  page.on('response', r => { if (/\/uploads\/|\/images\//i.test(r.url()) && r.status() >= 400) imgFails.push(`${r.status()} ${r.url()}`); });
  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('#Email').fill(LOGIN.email);
    await page.locator('#Password').fill(LOGIN.password);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1200);

    // open Edit, attach PDF, save (modal fix is deployed → dismiss toast if any, then Save)
    await page.goto(`${BASE}/Certificate/Edit/${CERT}`, { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(700);
    await page.setInputFiles('#certificatePdf', pdf);
    await page.waitForTimeout(1200);
    // dismiss any toast/modal that might still intercept (defensive)
    await page.evaluate(() => {
      document.querySelectorAll('#certUploadToastStack button, .gcc-modal-overlay.show button, .admin-modal-overlay.show button').forEach(b => b.click());
    });
    await page.waitForTimeout(400);
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}),
      page.locator('#saveButton').click({ timeout: 10000 }).catch(() => {}),
    ]);
    await page.waitForTimeout(2500);
    rec('SAVED', /\/Edit\//i.test(page.url()) ? 'FAIL' : 'PASS', `landed on ${page.url()}`);

    // ensure we're on the detail page
    if (!/CertificateInternalView/i.test(page.url())) {
      await page.goto(`${BASE}/Certificate/CertificateInternalView/${CERT}`, { waitUntil: 'networkidle', timeout: 60000 });
    }
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(SHOTS, 'detail-after-save.png'), fullPage: true });

    const detail = await page.evaluate(() => {
      const img = document.querySelector('.certificate-preview-box img, .certificate-image-column img');
      const placeholder = document.querySelector('.preview-placeholder');
      const pdfLink = Array.from(document.querySelectorAll('a')).find(a => /\.pdf|download|GetCertificate|view.*pdf/i.test(a.getAttribute('href') || '') || /pdf/i.test(a.textContent));
      return {
        imgSrc: img ? img.getAttribute('src') : null,
        imgNaturalW: img ? img.naturalWidth : null,   // 0 => broken/missing image bytes
        imgComplete: img ? img.complete : null,
        placeholderShown: !!placeholder && getComputedStyle(placeholder).display !== 'none',
        placeholderText: placeholder ? placeholder.innerText.replace(/\s+/g, ' ').trim().slice(0, 80) : null,
        pdfHref: pdfLink ? pdfLink.getAttribute('href') : null,
      };
    });
    rec('PDF-STORED', detail.pdfHref ? 'PASS' : 'WARN', `pdf link = ${detail.pdfHref || '(none found on page)'}`);
    rec('IMG-ELEMENT', detail.imgSrc ? 'INFO' : 'INFO', `src=${detail.imgSrc || '(no img — placeholder path)'} naturalW=${detail.imgNaturalW}`);
    rec('PLACEHOLDER', detail.placeholderShown ? 'INFO' : 'INFO', detail.placeholderShown ? `"Certificate Unavailable" placeholder IS showing → no thumbnail saved. text="${detail.placeholderText}"` : 'no placeholder (image present)');

    // If the PDF stored, actually fetch the thumbnail src to see if it 404s / is empty
    let imgVerdict;
    if (detail.placeholderShown || !detail.imgSrc) {
      imgVerdict = 'NO IMAGE: CertificateImagePath is empty after save → PDF thumbnail was NOT generated/saved (server-side render failed — likely Docnet/pdfium native).';
    } else if (detail.imgNaturalW === 0) {
      imgVerdict = `IMAGE BROKEN: <img> present (src=${detail.imgSrc}) but renders 0px → the thumbnail file is missing/404 on the server.`;
    } else {
      imgVerdict = `IMAGE OK: thumbnail rendered ${detail.imgNaturalW}px wide.`;
    }
    rec('VERDICT', (detail.placeholderShown || !detail.imgSrc || detail.imgNaturalW === 0) ? 'FAIL' : 'PASS', imgVerdict);
    rec('IMG-404s', imgFails.length ? 'FAIL' : 'INFO', imgFails.join(' | ') || '(no image/upload 4xx-5xx responses captured)');

  } catch (e) {
    rec('ERR', 'FAIL', e.message);
    try { await page.screenshot({ path: path.join(SHOTS, 'error.png'), fullPage: true }); } catch {}
  } finally {
    fs.writeFileSync(path.join(OUT, 'image-diagnose-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }
  console.log(`\nScreenshots: ${SHOTS}`);
  process.exit(0);
})();
