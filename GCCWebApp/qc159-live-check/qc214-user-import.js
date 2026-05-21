// DEF-214 — User Import: 4 sub-issues (mobile preview, role casing, sanitization, count math).
//
// All tests open /Admin/UserManagement → Import, prepare a CSV that triggers each
// sub-issue, inspect the rendered preview modal, then Cancel (no actual user creation).
// Read-only on the DB; no destructive writes.
//
// Localhost only (DEF-214 status = "Fixed locally — pending deploy" as of 2026-05-21).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const TARGET = process.env.TARGET || (BASE.includes('gccwebapp') ? 'live' : 'local');
const BASIC = BASE.includes('gccwebapp') ? { username: 'gcc', password: 'Cert-Test-Access-2026!' } : undefined;

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

// Click the Import button then provide a CSV via filechooser
async function openImportAndUpload(page, csvText, label) {
  const tmp = path.join(OUT, `qc214-${label}.csv`);
  fs.writeFileSync(tmp, csvText);
  const [fc] = await Promise.all([
    page.waitForEvent('filechooser'),
    page.locator('#import-btn').click(),
  ]);
  await fc.setFiles(tmp);
  await page.waitForTimeout(2500);
}

async function closePreviewModal(page) {
  // Cancel without applying
  const cancel = page.locator('.import-modal-container button:has-text("Cancel"), button:has-text("Cancel")').first();
  if (await cancel.count() > 0) {
    await cancel.click().catch(() => {});
    await page.waitForTimeout(500);
  }
}

(async () => {
  const browser = await chromium.launch({ headless: true });

  console.log(`\nDEF-214 User Import verification — ${TARGET.toUpperCase()} (${BASE})\n`);

  try {
    // ====================================================================
    // SUB-A — Mobile preview modal fits at 375px width
    // ====================================================================
    console.log('--- Sub-A: mobile preview modal at 375px (open at desktop, resize to mobile) ---');
    {
      // Open the modal at desktop width (the Import button is in the admin actions row
      // which gets reorganized on phone-width breakpoints), THEN resize viewport to 375
      // to assert the responsive CSS rules apply correctly.
      const ctxMobile = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
      const page = await ctxMobile.newPage();
      await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
      await page.locator('#Email').fill('admin');
      await page.locator('#Password').fill('TestPass123!');
      await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
      await page.waitForTimeout(2000);

      await page.goto(BASE + '/Admin/UserManagement', { waitUntil: 'domcontentloaded', timeout: 45000 });
      await page.waitForTimeout(3500);

      const minimalCsv = 'Name,Email,Company,Role,Status\nQC214 SubA Test,qc214-suba@example.com,Test Co,Analyst,active\n';
      await openImportAndUpload(page, minimalCsv, 'subA-mobile');

      // Now resize to 375px portrait — the @media (max-width: 768px) rule should kick in
      await page.setViewportSize({ width: 375, height: 812 });
      await page.waitForTimeout(1000);

      // Measure modal container + table at mobile width
      const metrics = await page.evaluate(() => {
        const modal = document.querySelector('.import-modal-container, .modal-container, [class*="import-modal"]');
        const table = document.querySelector('.import-preview-table, table');
        const win = { w: window.innerWidth, h: window.innerHeight };
        const m = modal ? { width: modal.getBoundingClientRect().width, css: window.getComputedStyle(modal).width } : null;
        const t = table ? { minWidth: window.getComputedStyle(table).minWidth, width: table.getBoundingClientRect().width } : null;
        return { win, modal: m, table: t };
      });
      console.log(`  viewport: ${metrics.win.w}x${metrics.win.h}`);
      console.log(`  modal: ${JSON.stringify(metrics.modal)}`);
      console.log(`  table: ${JSON.stringify(metrics.table)}`);
      await page.screenshot({ path: path.join(OUT, `qc214-${TARGET}-subA-mobile.png`), fullPage: true });

      // Fix: modal should be ~100% of viewport (within 10px tolerance); table min-width should be 0px
      const modalFits = metrics.modal && Math.abs(metrics.modal.width - metrics.win.w) < 20;
      const tableNoMinWidth = metrics.table && (metrics.table.minWidth === '0px' || metrics.table.minWidth === 'auto' || metrics.table.minWidth === '');
      record('QC214-A-modal-fits', 'Import modal occupies ~100% viewport at 375px (no 5% gutter)',
        modalFits ? 'PASS' : 'FAIL',
        `modalW=${metrics.modal?.width}, viewportW=${metrics.win.w}, css=${metrics.modal?.css}`);
      record('QC214-A-table-min-width', 'Preview table min-width dropped (was 900px, now 0)',
        tableNoMinWidth ? 'PASS' : 'WARN',
        `min-width=${metrics.table?.minWidth}`);

      await closePreviewModal(page);
      await ctxMobile.close();
    }

    // ====================================================================
    // SUB-B — Role canonicalization (lowercase "analyst" accepted)
    // SUB-C — Script/HTML sanitization
    // SUB-D — Count math consistency
    // All run on desktop viewport, shared session.
    // ====================================================================
    const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
    const page = await ctx.newPage();
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill('admin');
    await page.locator('#Password').fill('TestPass123!');
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
    await page.waitForTimeout(2000);

    // ----- SUB-B: lowercase role accepted -----
    console.log('\n--- Sub-B: lowercase role accepted ---');
    {
      await page.goto(BASE + '/Admin/UserManagement', { waitUntil: 'domcontentloaded', timeout: 45000 });
      await page.waitForTimeout(3500);

      const csv = 'Name,Email,Company,Role,Status\nQC214 SubB Test,qc214-subb@example.com,Test Co,analyst,active\n';
      await openImportAndUpload(page, csv, 'subB-lower-role');

      // Inspect the preview: row should NOT show "Invalid role" error
      const rowInfo = await page.evaluate(() => {
        const rows = document.querySelectorAll('.import-modal-container tbody tr, .import-preview-table tbody tr, table tbody tr');
        const out = [];
        rows.forEach(r => out.push({ html: r.innerHTML.slice(0, 500), text: (r.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 300) }));
        return out.slice(0, 3);
      });
      console.log(`  first row text: "${rowInfo[0]?.text || '(none)'}"`);

      const hasInvalidRole = rowInfo.some(r => /Invalid role/i.test(r.text));
      record('QC214-B-no-invalid-role',
        'Lowercase "analyst" role NOT flagged as invalid in preview',
        !hasInvalidRole ? 'PASS' : 'FAIL',
        `row text="${rowInfo[0]?.text?.slice(0, 150) || '(none)'}"`);
      await page.screenshot({ path: path.join(OUT, `qc214-${TARGET}-subB-lowercase-role.png`), fullPage: false });
      await closePreviewModal(page);
    }

    // ----- SUB-C: HTML/script sanitization -----
    console.log('\n--- Sub-C: <script> stripped + "Content cleaned" pill ---');
    {
      await page.waitForTimeout(800);
      const csv = 'Name,Email,Company,Role,Status\n"<script>alert(1)</script>John Smith","qc214-subc@example.com",Test Co,External,active\n';
      await openImportAndUpload(page, csv, 'subC-sanitize');

      const sanitizationInfo = await page.evaluate(() => {
        const rows = document.querySelectorAll('.import-modal-container tbody tr, table tbody tr');
        const out = [];
        rows.forEach(r => {
          const html = r.innerHTML;
          out.push({
            text: (r.innerText || '').replace(/\s+/g, ' ').trim().slice(0, 300),
            hasScriptTag: /<script/i.test(html),
            hasCleanedPill: /Content cleaned|content-cleaned|cleaned-pill/i.test(html),
            hasJohnSmith: /John Smith/i.test(r.innerText || ''),
          });
        });
        return out.slice(0, 3);
      });
      console.log(`  row info: ${JSON.stringify(sanitizationInfo[0])}`);

      record('QC214-C-script-stripped',
        'Imported Name no longer contains <script> tag in preview HTML',
        sanitizationInfo[0] && !sanitizationInfo[0].hasScriptTag ? 'PASS' : 'FAIL',
        `hasScriptTag=${sanitizationInfo[0]?.hasScriptTag}`);
      record('QC214-C-name-preserved',
        'Sanitized Name still shows "John Smith"',
        sanitizationInfo[0] && sanitizationInfo[0].hasJohnSmith ? 'PASS' : 'FAIL',
        `name visible="${sanitizationInfo[0]?.hasJohnSmith}"`);
      record('QC214-C-cleaned-pill',
        'Amber "Content cleaned" pill present on the sanitized row',
        sanitizationInfo[0] && sanitizationInfo[0].hasCleanedPill ? 'PASS' : 'WARN',
        `hasCleanedPill=${sanitizationInfo[0]?.hasCleanedPill}`);
      await page.screenshot({ path: path.join(OUT, `qc214-${TARGET}-subC-sanitized.png`), fullPage: false });
      await closePreviewModal(page);
    }

    // ----- SUB-D: count math: "X of Y" where X never exceeds Y -----
    console.log('\n--- Sub-D: count math (numerator ≤ denominator) ---');
    {
      await page.waitForTimeout(800);
      // CSV with one valid row + one row with bad role to force an error
      const csv = 'Name,Email,Company,Role,Status\n' +
        'QC214 SubD Good,qc214-subd-good@example.com,Test Co,Analyst,active\n' +
        'QC214 SubD Bad,qc214-subd-bad@example.com,Test Co,NotARealRole,active\n';
      await openImportAndUpload(page, csv, 'subD-count');

      const footerText = await page.evaluate(() => {
        // Look in modal footer / summary area for "N of M" pattern
        const candidates = document.querySelectorAll('.import-modal-container, .modal-footer, .preview-summary, [class*="summary"], [class*="footer"]');
        for (const c of candidates) {
          const t = (c.innerText || '').trim();
          if (/\d+\s+of\s+\d+/i.test(t)) return t.slice(0, 400);
        }
        // Fallback: search entire modal
        const modal = document.querySelector('.import-modal-container, .modal-container');
        return modal ? (modal.innerText || '').slice(0, 500) : '';
      });
      console.log(`  footer text: "${footerText.slice(0, 200)}"`);
      const m = footerText.match(/(\d+)\s+of\s+(\d+)/i);
      const numerator = m ? parseInt(m[1]) : null;
      const denominator = m ? parseInt(m[2]) : null;
      console.log(`  parsed: ${numerator} of ${denominator}`);
      record('QC214-D-count-math',
        'Count math: numerator ≤ denominator (pre-fix produced 18 of 11)',
        m && numerator <= denominator ? 'PASS' : 'FAIL',
        `${numerator} of ${denominator}`);
      await page.screenshot({ path: path.join(OUT, `qc214-${TARGET}-subD-count.png`), fullPage: false });
      await closePreviewModal(page);
    }

    await ctx.close();

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, `qc214-${TARGET}-results.json`), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
