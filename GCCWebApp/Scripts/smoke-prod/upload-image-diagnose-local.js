// Confirm PDF thumbnail generation WORKS on localhost (isolating the gccwebapp.com
// "Preview not generated" to the server environment, not the code). Discovers a cert,
// uploads a PDF on Edit, saves, then checks the detail page for a rendered preview image.
//
// Run (app up on localhost:5101):  node Scripts/smoke-prod/upload-image-diagnose-local.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const LOGIN = { email: 'admin', password: 'TestPass123!' };
const OUT = path.join(__dirname, '_results');
const SHOTS = path.join(OUT, 'image-diagnose-local');
fs.mkdirSync(SHOTS, { recursive: true });
const results = [];
const rec = (id, status, detail = '') => {
  results.push({ id, status, detail });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : 'ℹ️ ';
  console.log(`${tag} [${status}] ${id}${detail ? '  — ' + detail : ''}`);
};
function makePdf(p) {
  fs.writeFileSync(p, '%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n4 0 obj\n<< /Length 70 >>\nstream\nBT /F1 24 Tf 72 700 Td (GCC Local Thumbnail Test) Tj ET\nendstream\nendobj\n5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\ntrailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF\n', 'latin1');
}
(async () => {
  const pdf = path.join(SHOTS, 'diag.pdf'); makePdf(pdf);
  const browser = await chromium.launch();
  const page = await browser.newContext({ viewport: { width: 1440, height: 900 } }).then(c => c.newPage());
  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(LOGIN.email);
    await page.locator('#Password').fill(LOGIN.password);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1000);

    await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(800);
    const num = await page.evaluate(() => {
      const m = document.body.innerHTML.match(/viewCertificateDetails\((?:&#39;|'|")([^'")]+)(?:&#39;|'|")\)/);
      return m ? m[1] : null;
    });
    rec('DISCOVER', num ? 'PASS' : 'FAIL', `cert#=${num}`);
    if (!num) throw new Error('no cert found');

    await page.goto(`${BASE}/Certificate/Edit/${num}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(600);
    await page.setInputFiles('#certificatePdf', pdf);
    await page.waitForTimeout(1000);
    await page.evaluate(() => document.querySelectorAll('#certUploadToastStack button').forEach(b => b.click()));
    await page.waitForTimeout(300);
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle', timeout: 30000 }).catch(() => {}),
      page.locator('#saveButton').click({ timeout: 10000 }).catch(() => {}),
    ]);
    await page.waitForTimeout(2500);

    if (!/CertificateInternalView/i.test(page.url())) {
      await page.goto(`${BASE}/Certificate/CertificateInternalView/${num}`, { waitUntil: 'networkidle' });
    }
    await page.waitForTimeout(1200);
    await page.screenshot({ path: path.join(SHOTS, 'detail.png'), fullPage: true });
    const d = await page.evaluate(() => {
      const img = document.querySelector('.certificate-preview-box img, .certificate-image-column img');
      const ph = document.querySelector('.preview-placeholder');
      return {
        imgSrc: img ? img.getAttribute('src') : null,
        imgNaturalW: img ? img.naturalWidth : null,
        placeholder: !!ph && getComputedStyle(ph).display !== 'none',
        phText: ph ? ph.innerText.replace(/\s+/g, ' ').trim().slice(0, 60) : null,
      };
    });
    rec('IMG', 'INFO', `src=${d.imgSrc || '(none)'} naturalW=${d.imgNaturalW} placeholder=${d.placeholder} "${d.phText || ''}"`);
    const ok = d.imgSrc && d.imgNaturalW > 0 && !d.placeholder;
    rec('VERDICT', ok ? 'PASS' : 'FAIL', ok
      ? `THUMBNAIL GENERATED locally (${d.imgNaturalW}px) → code path works; gccwebapp.com failure is SERVER environment (pdfium/VC++ runtime).`
      : `Thumbnail ALSO failed locally — placeholder=${d.placeholder} text="${d.phText}". Then it's not purely server env; investigate code.`);
  } catch (e) {
    rec('ERR', 'FAIL', e.message);
    try { await page.screenshot({ path: path.join(SHOTS, 'error.png'), fullPage: true }); } catch {}
  } finally {
    fs.writeFileSync(path.join(OUT, 'image-diagnose-local-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }
  console.log(`\nScreenshots: ${SHOTS}`);
})();
