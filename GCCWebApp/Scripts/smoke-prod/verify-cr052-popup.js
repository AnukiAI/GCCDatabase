/**
 * CR-052 follow-up: prove the "View all pages" control is INSIDE #imageZoomModal on every
 * popup-bearing surface, that clicking it does not close the popup, and that the redundant
 * Edit green-bar button is gone.
 *
 * Playwright is installed globally, not in the project, so run with NODE_PATH set:
 *   NODE_PATH="$(npm root -g)" node Scripts/smoke-prod/verify-cr052-popup.js
 *
 * NOTE: a login failure ABORTS the run. An earlier version continued, and its
 * "the old button is absent" assertion passed on the login page. Every absence
 * assertion here carries a positive control on the same page.
 */
const path = require('path');
const fs = require('fs');
const { chromium } = require('playwright');
const BASE = 'http://localhost:5101';
const OUT = path.join(__dirname, '_results', 'cr052');
fs.mkdirSync(OUT, { recursive: true });
const CERT = '21-0117';
const PUBLIC_CERT = '26-0481';
const IN_MODAL = '#imageZoomModal a.zoom-allpages-link';
// 2026-09-17 rework #2: the duplicate control in the action row was removed at the client's
// request — paging belongs in the popup, and they did not want a second way out to the PDF.
// ROW is the positive control: an 'icon is absent' assertion passes trivially on a page that
// failed to load, so the row itself must be proven present on the same page first.
const ROW = '.certificate-download-action';
const ROW_ICON = '.certificate-download-action a[href*="ViewCertificateFile"]';
async function assertRowIconGone(pg, id) {
  const row = await pg.$$eval(ROW, els => els.length);
  const icons = await pg.$$eval(ROW_ICON, els => els.length);
  rec(id, row >= 1 && icons === 0,
      `${ROW} x${row} (must be >=1, else vacuous), ViewCertificateFile icons in it: ${icons}`);
}
const results = [];
const rec = (id, pass, detail) => { results.push({ id, pass, detail }); console.log(`${pass ? 'PASS' : 'FAIL'}  ${id}  ${detail}`); };

async function login(page, user, pass) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.fill('input[name="Email"]', user);
  await page.fill('input[name="Password"]', pass);
  await Promise.all([page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 60000 }).catch(()=>{}), page.click('button[type="submit"], input[type="submit"]')]);
  return !page.url().includes('/Account/Login');
}

async function openPopup(page) {
  // click the preview image / View button, whichever this surface uses
  const trigger = await page.$('button:has-text("View"), img.certificate-preview-img, #certificateImagePreviewImg, [onclick*="openImageZoom"]');
  if (trigger) { await trigger.click().catch(()=>{}); }
  else { await page.evaluate(() => window.openImageZoom && window.openImageZoom(document.querySelector('img[src*="ViewCertificateImage"], img[src*="PreviewThumbnail"]')?.src || '')); }
  await page.waitForFunction(() => {
    const m = document.querySelector('#imageZoomModal');
    return m && getComputedStyle(m).display !== 'none';
  }, null, { timeout: 15000 });
}

(async () => {
  const browser = await chromium.launch();

  // --- admin surfaces ---
  const ctx = await browser.newContext({ viewport: { width: 1400, height: 900 } });
  const page = await ctx.newPage();
  const loggedIn = await login(page, 'admin.uat', 'TestPass123!');
  rec('login-admin', loggedIn, `admin.uat -> ${page.url()}`);
  if (!loggedIn) { console.error('ABORTING: every later assertion is vacuous on a login page.'); await browser.close(); process.exit(3); }

  // Edit: link inside modal, old green-bar button gone
  await page.goto(`${BASE}/Certificate/Edit/${CERT}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  const barPresent = await page.$$eval('.current-pdf-info', els => els.length);
  const barLinks = await page.$$eval('.current-pdf-info a[href*="ViewCertificateFile"]', els => els.length);
  rec('E1 old green-bar button removed', barPresent === 1 && barLinks === 0,
      `.current-pdf-info x${barPresent} (must be 1, else this assertion is vacuous), ViewCertificateFile links inside: ${barLinks}`);
  await openPopup(page);
  const editLink = await page.$(IN_MODAL);
  rec('E2 link inside popup (Edit)', !!editLink, editLink ? await editLink.getAttribute('href') : 'absent');
  // clicking must NOT close the modal (stopPropagation)
  if (editLink) {
    await editLink.evaluate(el => el.removeAttribute('target'));   // keep it in-tab so we can observe
    await editLink.dispatchEvent('click');
    const stillOpen = await page.evaluate(() => getComputedStyle(document.querySelector('#imageZoomModal')).display !== 'none');
    rec('E3 popup stays open on click', stillOpen, stillOpen ? 'modal still displayed' : 'modal closed — stopPropagation failed');
  }
  await page.goto(`${BASE}/Certificate/Edit/${CERT}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await openPopup(page);
  await page.waitForTimeout(600);
  await page.screenshot({ path: path.join(OUT, 'cr052-popup-edit.png') });

  // Internal detail view
  await page.goto(`${BASE}/Certificate/CertificateInternalView/${CERT}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await openPopup(page);
  const intLink = await page.$(IN_MODAL);
  rec('I1 link inside popup (CertificateInternalView)', !!intLink, intLink ? await intLink.getAttribute('href') : 'absent');
  await assertRowIconGone(page, 'I2 action-row icon removed (CertificateInternalView)');
  await page.waitForTimeout(600);
  await page.screenshot({ path: path.join(OUT, 'cr052-popup-internal.png') });
  await ctx.close();

  // --- anonymous public surface ---
  const anon = await browser.newContext({ viewport: { width: 1400, height: 900 } });
  const ap = await anon.newPage();
  await ap.goto(`${BASE}/Certificate/CertificateSingleView/${PUBLIC_CERT}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  const inMarkup = await ap.$$eval(IN_MODAL, els => els.length);
  rec('P1 link inside popup (CertificateSingleView, anonymous)', inMarkup === 1, `${inMarkup} found`);
  await assertRowIconGone(ap, 'P2 action-row icon removed (CertificateSingleView, anonymous)');
  if (inMarkup) {
    await openPopup(ap).catch(()=>{});
    await ap.waitForTimeout(600);
    await ap.screenshot({ path: path.join(OUT, 'cr052-popup-public.png') });
  }
  await anon.close();

  await browser.close();
  const failed = results.filter(r => !r.pass);
  console.log(`\n${results.length - failed.length}/${results.length} PASS`);
  if (failed.length) { console.log('FAILURES: ' + failed.map(f => f.id).join(', ')); process.exit(2); }
})().catch(e => { console.error('HARNESS FAILED:', e.message); process.exit(1); });
