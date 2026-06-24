// READ-ONLY (QC #198 follow-up). For every certificate that has a stored PDF path, report
// BOTH the PDF download status AND the thumbnail-image status, so we know exactly which certs
// still need a PDF and/or a thumbnail restored after moving/restoring files on the server.
//
//   PDF:    HEAD /Certificate/Download/{number}        -> PRESENT(200) / MISSING(404)
//   THUMB:  GET  /Certificate/ViewCertificateImage/{id}
//            - 404                          -> MISSING (image path set but file gone)
//            - 200, size == placeholder     -> NONE    (no thumbnail; app serves the placeholder)
//            - 200, size != placeholder     -> PRESENT (real thumbnail)
//
// Run:  node prod-cert-files-sweep.js     (BASE_URL env overrides; defaults to prod test env)
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1200);

  // Size of the no-thumbnail placeholder, fetched through the SAME image endpoint so any
  // transport encoding matches. id=-1 has no cert -> NotFound, so use the static placeholder.
  const placeholderLen = await page.evaluate(async (u) => {
    const r = await fetch(u); if (!r.ok) return -1;
    return (await r.arrayBuffer()).byteLength;
  }, `${BASE}/img/certpreview.png`);

  const d = await page.evaluate(async (u) => (await fetch(u, { headers: { Accept: 'application/json' } })).json(),
    `${BASE}/api/certificate-tiles-internal?sortBy=newest&page=1&pageSize=500`);
  const withPath = (d.certificates || []).filter((c) => c.pdfPath).map((c) => ({ id: c.id, n: c.certificateNumber, p: c.pdfPath }));

  const rows = [];
  for (const c of withPath) {
    const pdf = await page.evaluate(async (u) => (await fetch(u, { method: 'HEAD' })).status,
      `${BASE}/Certificate/Download/${encodeURIComponent(c.n)}`);
    const img = await page.evaluate(async (u) => {
      const r = await fetch(u);
      if (!r.ok) return { status: r.status, len: -1 };
      return { status: r.status, len: (await r.arrayBuffer()).byteLength };
    }, `${BASE}/Certificate/ViewCertificateImage/${c.id}`);

    let thumb;
    if (img.status === 404 || img.len < 0) thumb = 'MISSING';
    else if (placeholderLen > 0 && img.len === placeholderLen) thumb = 'NONE';
    else thumb = 'PRESENT';

    rows.push({ n: c.n, pdf: pdf === 200, thumb, p: c.p });
  }
  rows.sort((a, b) => (a.pdf === b.pdf ? 0 : a.pdf ? -1 : 1));

  console.log(`\n  placeholder size = ${placeholderLen} bytes; ${rows.length} certs with a stored PDF path\n`);
  console.log('  PDF      THUMB     cert       stored pdfPath');
  for (const r of rows)
    console.log(`  ${r.pdf ? 'PRESENT' : 'MISSING'}  ${r.thumb.padEnd(8)}  ${(r.n + '').padEnd(9)}  ${r.p}`);

  const pdfMissing = rows.filter((r) => !r.pdf).map((r) => r.n);
  const thumbMissing = rows.filter((r) => r.thumb === 'MISSING').map((r) => r.n);
  console.log(`\n  SUMMARY`);
  console.log(`    PDFs:   present ${rows.filter((r) => r.pdf).length}, missing ${pdfMissing.length}`);
  console.log(`    Thumbs: present ${rows.filter((r) => r.thumb === 'PRESENT').length}, none ${rows.filter((r) => r.thumb === 'NONE').length}, missing(404) ${thumbMissing.length}`);
  console.log(`    Need PDF restore/re-upload:   ${pdfMissing.join(', ') || '(none)'}`);
  console.log(`    Need thumbnail (file gone):   ${thumbMissing.join(', ') || '(none)'}`);

  await browser.close();
})();
