// DEF-261 FIX VERIFY (localhost) — attaching a PDF must show a NON-BLOCKING toast,
// not a centered modal that overlays the Save button. Mirrors the user's flow:
// open an existing cert's Edit page, attach a PDF, confirm Save is reachable and the
// cert actually updates. Also checks the >10MB error path is a toast (still non-blocking).
//
// Run (app must be up on localhost:5101):
//   node Scripts/smoke-prod/upload-fix-verify-local.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const LOGIN = { email: 'admin', password: 'TestPass123!' };
const OUT = path.join(__dirname, '_results');
const SHOTS = path.join(OUT, 'upload-fix-verify');
fs.mkdirSync(SHOTS, { recursive: true });
const results = [];
const rec = (id, status, detail = '') => {
  results.push({ id, status, detail });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : 'ℹ️ ';
  console.log(`${tag} [${status}] ${id}${detail ? '  — ' + detail : ''}`);
};

function makePdf(p, sizeMb) {
  let body = '%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n4 0 obj\n<< /Length 64 >>\nstream\nBT /F1 24 Tf 72 720 Td (GCC Fix Verify) Tj ET\nendstream\nendobj\n5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n';
  if (sizeMb > 0) body += '% ' + 'A'.repeat(Math.max(0, Math.floor(sizeMb * 1024 * 1024) - body.length - 64)) + '\n';
  body += 'trailer\n<< /Root 1 0 R /Size 6 >>\n%%EOF\n';
  fs.writeFileSync(p, body, 'latin1');
}

// Returns a description of any page-blocking overlay (the bug), or null if none.
async function blockingOverlay(page) {
  return await page.evaluate(() => {
    // AdminCore centered modal
    const m = document.querySelector('.gcc-modal-overlay.show, .admin-modal-overlay.show');
    if (m) return 'AdminCore modal: ' + (m.querySelector('[id$="-modal-title"]')?.innerText || 'modal');
    // any element that covers screen-center with a high z-index (the old fallback overlay)
    const cx = Math.floor(window.innerWidth / 2), cy = Math.floor(window.innerHeight / 2);
    const el = document.elementFromPoint(cx, cy);
    let n = el;
    while (n) {
      const s = getComputedStyle(n);
      if (s.position === 'fixed' && parseInt(s.zIndex || '0') >= 9999 &&
          n.offsetWidth > window.innerWidth * 0.8 && n.offsetHeight > window.innerHeight * 0.8) {
        return 'full-screen overlay at center (' + n.id + '.' + n.className + ')';
      }
      n = n.parentElement;
    }
    return null;
  });
}

(async () => {
  const small = path.join(SHOTS, 'small.pdf'); makePdf(small, 0);
  const big = path.join(SHOTS, 'big-12mb.pdf'); makePdf(big, 12);

  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  try {
    // login
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(LOGIN.email);
    await page.locator('#Password').fill(LOGIN.password);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1000);

    // discover an existing cert to edit
    await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(800);
    // Cards call viewCertificateDetails('<number>') (opens detail in a new tab). Parse the number from the DOM.
    const num = await page.evaluate(() => {
      const html = document.body.innerHTML;
      const m = html.match(/viewCertificateDetails\((?:&#39;|'|")([^'")]+)(?:&#39;|'|")\)/);
      return m ? m[1] : null;
    });
    const editHref = num ? `/Certificate/Edit/${num}` : null;
    rec('DISCOVER', editHref ? 'PASS' : 'FAIL', `cert#=${num} → ${editHref}`);
    if (!editHref) throw new Error('could not derive a cert number to edit on localhost (url=' + page.url() + ')');

    // ---------- CASE 1: valid small PDF — toast, Save reachable, cert updates ----------
    await page.goto(BASE + editHref, { waitUntil: 'networkidle' });
    await page.waitForTimeout(700);
    await page.setInputFiles('#certificatePdf', small);
    await page.waitForTimeout(1000);

    const filesLen1 = await page.evaluate(() => document.getElementById('certificatePdf')?.files?.length || 0);
    rec('C1-ATTACH', filesLen1 === 1 ? 'PASS' : 'FAIL', `files=${filesLen1}`);

    const block1 = await blockingOverlay(page);
    rec('C1-NO-BLOCK', block1 ? 'FAIL' : 'PASS', block1 ? `STILL BLOCKED → ${block1}` : 'no page-blocking overlay after attach');

    const toast1 = await page.evaluate(() => {
      const c = document.getElementById('certUploadToastStack');
      return c ? Array.from(c.children).map(t => t.innerText.replace(/\s+/g, ' ').trim()).join(' | ') : null;
    });
    rec('C1-TOAST', toast1 ? 'PASS' : 'FAIL', `toast="${toast1 || '(none)'}"`);
    await page.screenshot({ path: path.join(SHOTS, '1-small-attach.png'), fullPage: true });

    // Save WITHOUT dismissing anything — must succeed (not intercepted)
    const click1 = await page.locator('#saveButton').click({ timeout: 7000 }).then(() => null).catch(e => e.message.split('\n')[0]);
    rec('C1-SAVE-CLICK', click1 ? 'FAIL' : 'PASS', click1 ? `Save blocked: ${click1}` : 'Save clicked directly (not intercepted)');
    await page.waitForLoadState('networkidle', { timeout: 20000 }).catch(() => {});
    await page.waitForTimeout(1500);
    const savedOff = !/\/Certificate\/Edit\//i.test(page.url());
    rec('C1-SAVED', savedOff ? 'PASS' : 'FAIL', `landed on ${page.url()}`);

    // ---------- CASE 2: >10MB PDF — error is a toast, still non-blocking ----------
    await page.goto(BASE + editHref, { waitUntil: 'networkidle' });
    await page.waitForTimeout(700);
    await page.setInputFiles('#certificatePdf', big);
    await page.waitForTimeout(1000);
    const filesLen2 = await page.evaluate(() => document.getElementById('certificatePdf')?.files?.length || 0);
    const block2 = await blockingOverlay(page);
    const toast2 = await page.evaluate(() => {
      const c = document.getElementById('certUploadToastStack');
      return c ? Array.from(c.children).map(t => t.innerText.replace(/\s+/g, ' ').trim()).join(' | ') : null;
    });
    rec('C2-REJECT', filesLen2 === 0 ? 'PASS' : 'FAIL', `oversized file cleared (files=${filesLen2})`);
    rec('C2-NO-BLOCK', block2 ? 'FAIL' : 'PASS', block2 ? `STILL BLOCKED → ${block2}` : 'error shown without blocking the page');
    rec('C2-TOAST', /10MB/i.test(toast2 || '') ? 'PASS' : 'FAIL', `toast="${toast2 || '(none)'}"`);
    await page.screenshot({ path: path.join(SHOTS, '2-big-rejected.png'), fullPage: true });
    const click2 = await page.locator('#saveButton').click({ timeout: 7000 }).then(() => null).catch(e => e.message.split('\n')[0]);
    rec('C2-SAVE-CLICK', click2 ? 'FAIL' : 'PASS', click2 ? `Save blocked: ${click2}` : 'Save reachable even after error toast');

  } catch (e) {
    rec('ERR', 'FAIL', e.message);
    try { await page.screenshot({ path: path.join(SHOTS, 'error.png'), fullPage: true }); } catch {}
  } finally {
    fs.writeFileSync(path.join(OUT, 'upload-fix-verify-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }
  const f = results.filter(r => r.status === 'FAIL').length;
  console.log(`\nSUMMARY: ${results.filter(r => r.status === 'PASS').length} PASS / ${f} FAIL — screenshots in ${SHOTS}`);
  process.exit(f > 0 ? 1 : 0);
})();
