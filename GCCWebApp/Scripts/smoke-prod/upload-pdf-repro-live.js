// PDF-upload repro — user reports they can't upload a PDF when creating a certificate.
// Drives a REAL Create on gccwebapp.com (behind the Basic-Auth test gate): logs in as
// admin, fills the required cascade, attaches a genuine PDF, submits, and reports exactly
// what the server returns — success redirect vs. a ModelState error on #certificatePdf
// (size / content-type / write-permission failure from FileUploadService.UploadCertificateAsync).
//
// LIVE TEST. Run:
//   SMOKE_REMOTE_USER=gcc SMOKE_REMOTE_PASS="Cert-Test-Access-2026!" \
//   node Scripts/smoke-prod/upload-pdf-repro-live.js
//
// Optional: SIZE_MB=15 to probe the request-size / MaxFileSize boundary with a big PDF.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = (process.env.SMOKE_REMOTE_USER && process.env.SMOKE_REMOTE_PASS)
  ? { username: process.env.SMOKE_REMOTE_USER, password: process.env.SMOKE_REMOTE_PASS }
  : { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const LOGIN = { email: process.env.APP_USER || 'admin', password: process.env.APP_PASS || 'TestPass123!' };
const SIZE_MB = parseFloat(process.env.SIZE_MB || '0'); // 0 = small valid PDF

const OUT = path.join(__dirname, '_results');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
const SHOTS = path.join(OUT, 'upload-repro');
if (!fs.existsSync(SHOTS)) fs.mkdirSync(SHOTS, { recursive: true });

const results = [];
const rec = (id, status, detail = '') => {
  results.push({ id, status, detail });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} [${status}] ${id}${detail ? '  — ' + detail : ''}`);
};

// Build a genuine, valid single-page PDF on disk. If SIZE_MB>0, pad it with a
// comment blob so total bytes ~= SIZE_MB to probe the size/request-body limit.
function makePdf(targetPath) {
  const objs = [
    '%PDF-1.4\n',
    '1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n',
    '2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n',
    '3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n',
    '4 0 obj\n<< /Length 64 >>\nstream\nBT /F1 24 Tf 72 720 Td (GCC Upload Test Certificate) Tj ET\nendstream\nendobj\n',
    '5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n',
  ];
  let body = objs.join('');
  if (SIZE_MB > 0) {
    const padBytes = Math.max(0, Math.floor(SIZE_MB * 1024 * 1024) - body.length - 64);
    body += '% ' + 'A'.repeat(padBytes) + '\n';
  }
  body += 'trailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF\n';
  fs.writeFileSync(targetPath, body, 'latin1');
  return fs.statSync(targetPath).size;
}

(async () => {
  const pdfPath = path.join(SHOTS, SIZE_MB > 0 ? `test-${SIZE_MB}mb.pdf` : 'test-small.pdf');
  const pdfBytes = makePdf(pdfPath);
  rec('FIXTURE', 'PASS', `${pdfPath} (${pdfBytes.toLocaleString()} bytes)`);

  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    httpCredentials: GATE,
    ignoreHTTPSErrors: true,
    acceptDownloads: true,
  });
  const page = await ctx.newPage();
  // surface server-side problems: capture failed POST status + any console errors
  const netLog = [];
  page.on('response', r => { if (/\/Certificate\/Create/i.test(r.url())) netLog.push(`${r.request().method()} ${r.status()} ${r.url()}`); });
  page.on('requestfailed', r => { if (/\/Certificate\//i.test(r.url())) netLog.push(`FAILED ${r.request().method()} ${r.url()} :: ${r.failure()?.errorText}`); });

  try {
    // ---- 1. Login through the gate ----
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('#Email').fill(LOGIN.email);
    await page.locator('#Password').fill(LOGIN.password);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1500);
    const loggedIn = !/invalid email or password/i.test(await page.evaluate(() => document.body.innerText));
    rec('LOGIN', loggedIn ? 'PASS' : 'FAIL', `as ${LOGIN.email} → ${page.url()}`);
    if (!loggedIn) throw new Error('login failed — cannot reach Create');

    // ---- 2. Open Create ----
    await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(900);
    const onCreate = await page.locator('#certificateCreateForm').count();
    rec('CREATE-PAGE', onCreate ? 'PASS' : 'FAIL', `#certificateCreateForm present=${!!onCreate}`);
    if (!onCreate) throw new Error('Create form not found');

    // ---- 3. Fill required cascade (Company → Product auto-fills cat/type; CertType; dates; status) ----
    const companyId = await page.evaluate(() => {
      const s = document.getElementById('CompanyId');
      for (const o of s.options) if (o.value && !o.disabled && !/add new/i.test(o.textContent)) return o.value;
      return null;
    });
    await page.evaluate(cid => { const s = document.getElementById('CompanyId'); s.value = cid; s.dispatchEvent(new Event('change', { bubbles: true })); }, companyId);
    await page.waitForFunction(() => document.getElementById('ProductId')?.options.length > 1, { timeout: 8000 }).catch(() => {});

    const productId = await page.evaluate(() => {
      const s = document.getElementById('ProductId');
      for (const o of s.options) if (o.value) return o.value;
      return null;
    });
    await page.evaluate(pid => { const s = document.getElementById('ProductId'); s.value = pid; s.dispatchEvent(new Event('change', { bubbles: true })); }, productId);
    await page.waitForTimeout(1500);

    await page.evaluate(() => { const t = document.getElementById('ProductTypeId'); if (t) for (const o of t.options) if (o.value) { t.value = o.value; t.dispatchEvent(new Event('change', { bubbles: true })); break; } });
    await page.waitForTimeout(600);
    await page.evaluate(() => { const t = document.getElementById('CertificationTypeId'); if (t) for (const o of t.options) if (o.value) { t.value = o.value; t.dispatchEvent(new Event('change', { bubbles: true })); break; } });
    await page.waitForTimeout(400);

    await page.evaluate(() => {
      const ed = document.querySelector('[name="EffectiveDate"]'); if (ed) ed.value = '2026-01-01';
      const xd = document.querySelector('[name="ExpirationDate"]'); if (xd) xd.value = '2027-01-01';
      const st = document.getElementById('Status'); if (st) for (const o of st.options) if (o.value) { st.value = o.value; st.dispatchEvent(new Event('change', { bubbles: true })); break; }
    });

    const certNum = await page.evaluate(() => document.getElementById('Number')?.value || '(blank)');
    rec('FORM-FILL', 'PASS', `company=${companyId} product=${productId} number=${certNum}`);

    // ---- 4. Attach the PDF to the (hidden) file input ----
    await page.setInputFiles('#certificatePdf', pdfPath);
    const attached = await page.evaluate(() => {
      const i = document.getElementById('certificatePdf');
      return i && i.files && i.files.length ? `${i.files[0].name} (${i.files[0].size}b, ${i.files[0].type})` : 'NONE';
    });
    rec('ATTACH', attached !== 'NONE' ? 'PASS' : 'FAIL', attached);
    await page.screenshot({ path: path.join(SHOTS, '1-before-submit.png'), fullPage: true });

    // ---- 5. Submit (native submit to force the real multipart POST) ----
    await page.evaluate(() => { window.__nav = 'present'; });
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle', timeout: 60000 }).catch(() => {}),
      page.evaluate(() => document.getElementById('certificateCreateForm').submit()),
    ]);
    await page.waitForTimeout(2500);
    await page.screenshot({ path: path.join(SHOTS, '2-after-submit.png'), fullPage: true });

    // ---- 6. Diagnose outcome ----
    const after = await page.evaluate(() => {
      const txt = document.body.innerText;
      const pdfErr = (document.querySelector('[data-valmsg-for="certificatePdf"], span[data-valmsg-for="certificatePdf"]')?.textContent || '').trim();
      const summary = (document.querySelector('.validation-summary-errors, .alert-danger, .text-danger')?.textContent || '').trim();
      return {
        url: location.pathname + location.search,
        navGone: typeof window.__nav === 'undefined',
        stillOnCreate: !!document.getElementById('certificateCreateForm'),
        pdfErr,
        summary: summary.slice(0, 300),
        errorPage: /error|exception|status code|HTTP 4|HTTP 5/i.test(document.title) || /An error occurred while processing your request|Request Entity Too Large|413/i.test(txt),
        title: document.title,
      };
    });

    const success = after.navGone && !after.stillOnCreate && !/\/Certificate\/Create/i.test(after.url);
    rec('SUBMIT-RESULT', success ? 'PASS' : 'FAIL',
      `url=${after.url} stillOnCreate=${after.stillOnCreate} pdfErr="${after.pdfErr}" summary="${after.summary}" title="${after.title}"`);
    rec('NET', netLog.length ? 'INFO' : 'INFO', netLog.join(' | ') || '(no Create responses captured)');

    if (success) {
      rec('VERDICT', 'PASS', `Upload SUCCEEDED for ${certNum} — could not reproduce the failure with a ${(pdfBytes/1024/1024).toFixed(2)}MB PDF.`);
    } else if (after.pdfErr) {
      rec('VERDICT', 'FAIL', `Reproduced: server rejected the PDF → "${after.pdfErr}" (FileUploadService validation).`);
    } else if (after.errorPage) {
      rec('VERDICT', 'FAIL', `Reproduced: request errored at server/host level (likely request-size limit or write permission). title="${after.title}".`);
    } else if (after.stillOnCreate) {
      rec('VERDICT', 'FAIL', `Reproduced: re-rendered Create with no explicit PDF error — summary="${after.summary}". Inspect 2-after-submit.png.`);
    } else {
      rec('VERDICT', 'WARN', `Ambiguous outcome — inspect screenshots in ${SHOTS}.`);
    }

  } catch (e) {
    rec('ERR', 'FAIL', e.message);
    try { await page.screenshot({ path: path.join(SHOTS, 'error.png'), fullPage: true }); } catch {}
  } finally {
    fs.writeFileSync(path.join(OUT, 'upload-pdf-repro-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }
  const f = results.filter(r => r.status === 'FAIL').length;
  console.log(`\nSUMMARY: ${results.filter(r => r.status === 'PASS').length} PASS / ${f} FAIL — screenshots in ${SHOTS}`);
  process.exit(f > 0 ? 1 : 0);
})();
