// READ-ONLY prod diagnosis for cert 26-0173 ("Chris's download not there").
// NO mutations — GET + login only. Distinguishes:
//   Case 1: CertificateFile empty  -> internal-view icon DISABLED ("No PDF available"); never uploaded.
//   Case 2: CertificateFile set but physical file missing -> icon ENABLED but Download returns 404
//           "No certificate PDF has been uploaded" (deploy overwrite / App_Data migration gap).
const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const NUM = process.env.CERT_NUM || '26-0173';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // App login as admin (internal view requires AnalystOrAdmin).
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1200);
  console.log('login ->', /\/Account\/Login/i.test(page.url()) ? 'FAILED' : 'ok');

  // 1) Internal view: read the download-icon state (disabled => CertificateFile empty).
  const url = `${BASE}/Certificate/CertificateInternalView/${encodeURIComponent(NUM)}`;
  const resp = await page.goto(url, { waitUntil: 'domcontentloaded' });
  console.log(`\nINTERNAL VIEW ${NUM}: HTTP ${resp.status()}`);
  await page.waitForTimeout(800);
  const icon = await page.evaluate(() => {
    const el = document.querySelector('.certificate-download-action .action-icon.download');
    if (!el) return { found: false };
    return { found: true, disabled: el.classList.contains('disabled'), title: el.getAttribute('title') };
  });
  console.log('  download icon:', JSON.stringify(icon));
  // Pull any visible cert meta (company/product) so we can confirm this is "Chris's" cert.
  const meta = await page.evaluate(() => {
    const txt = (sel) => { const e = document.querySelector(sel); return e ? e.textContent.trim().replace(/\s+/g, ' ').slice(0, 80) : null; };
    return { title: document.title, h: txt('h1, h2, .cert-title, .page-title') };
  });
  console.log('  page meta:', JSON.stringify(meta));

  // 2) Hit the Download endpoint directly — status + body distinguishes Case 1 vs Case 2 root cause.
  const dl = await page.evaluate(async (u) => {
    const r = await fetch(u);
    const ct = r.headers.get('content-type') || '';
    let body = '';
    if (!ct.includes('pdf')) { try { body = (await r.text()).slice(0, 200); } catch {} }
    return { status: r.status, contentType: ct, len: r.headers.get('content-length'), body };
  }, `${BASE}/Certificate/Download/${encodeURIComponent(NUM)}`);
  console.log(`\nDOWNLOAD /Certificate/Download/${NUM}:`);
  console.log('  HTTP', dl.status, '| content-type:', dl.contentType, '| length:', dl.len);
  if (dl.body) console.log('  body:', dl.body);

  console.log('\n=== VERDICT ===');
  if (dl.contentType.includes('pdf')) {
    console.log('  PDF DOWNLOADS FINE on prod — issue is not server-side (browser/cache/permission on Chris end?).');
  } else if (icon.found && icon.disabled) {
    console.log('  CASE 1: CertificateFile is EMPTY in DB — no PDF was ever uploaded for', NUM, '(analyst must upload).');
  } else {
    console.log('  CASE 2: CertificateFile is SET but physical file MISSING on server —');
    console.log('          consistent with a Plesk deploy overwriting uploads / App_Data migration gap. Restore from backup.');
  }

  await browser.close();
})();
