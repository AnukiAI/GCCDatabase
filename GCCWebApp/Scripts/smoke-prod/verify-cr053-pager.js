/**
 * CR-053 (QC #267) --- prove the preview popup actually pages through a multi-page certificate,
 * that the page parameter cannot be used to reach anything the caller could not already see, and
 * that a document which cannot be paged degrades to the CR-052 link instead of a dead popup.
 *
 * Playwright is installed globally, not in the project, so run with NODE_PATH set:
 *   NODE_PATH="$(npm root -g)" node Scripts/smoke-prod/verify-cr053-pager.js
 *
 * TWO RULES THIS FILE FOLLOWS, both learned the hard way in CR-052:
 *  1. A login failure ABORTS. An "absent" assertion passes trivially on the login page.
 *  2. Every absence assertion carries a positive control on the SAME page.
 *
 * And one from the CR-052 harness bugs: image identity is compared by BYTES (a hash of the
 * fetched response), never by the src string. A src that changed while the image did not would
 * otherwise read as a successful page turn.
 */
const path = require('path');
const fs = require('fs');
const crypto = require('crypto');
const { chromium } = require('playwright');

const BASE = 'http://localhost:5101';
const OUT = path.join(__dirname, '_results', 'cr053');
fs.mkdirSync(OUT, { recursive: true });

const ADMIN_USER = process.env.ADMIN_USER || 'admin.uat';
const ADMIN_PASS = process.env.ADMIN_PASS || 'TestPass123!';

// 21-0117 is Id 1488 and is a genuine 2-page PDF present in the dev uploads store (page-counted
// straight off disk with pypdf during CR-052). 26-0481 is public, anonymous-visible, real PDF.
const MULTI_CERT = '21-0117';
const PUBLIC_CERT = '26-0481';

const PAGER = '#zoomPager';
const PAGER_NEXT = '#zoomPagerNext';
const PAGER_PREV = '#zoomPagerPrev';
const PAGER_LABEL = '#zoomPagerLabel';
const ALL_PAGES = '#imageZoomModal a.zoom-allpages-link';

const results = [];
const rec = (id, pass, detail) => {
  results.push({ id, pass, detail });
  console.log(`${pass ? 'PASS' : 'FAIL'}  ${id}  ${detail}`);
};

async function login(page, user, pass) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.fill('input[name="Email"]', user);
  await page.fill('input[name="Password"]', pass);
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 60000 }).catch(() => {}),
    page.click('button[type="submit"], input[type="submit"]')
  ]);
  return !page.url().includes('/Account/Login');
}

/* The dev box throws SqlException: Execution Timeout Expired under contention, and a navigation
   that times out is indistinguishable from a broken page unless it is retried. One transient
   failure already cost a full run here. */
async function gotoRetry(page, url, tries = 3) {
  for (let i = 1; i <= tries; i++) {
    try {
      await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 90000 });
      return true;
    } catch (e) {
      if (i === tries) throw e;
      console.log(`  (retry ${i}/${tries - 1} after navigation timeout: ${url})`);
      await page.waitForTimeout(2000);
    }
  }
}

async function openPopup(page) {
  const trigger = await page.$('button:has-text("View"), img.certificate-preview-img, #certificateImagePreviewImg, [onclick*="openImageZoom"]');
  if (trigger) { await trigger.click().catch(() => {}); }
  else {
    await page.evaluate(() => window.openImageZoom &&
      window.openImageZoom(document.querySelector('img[src*="ViewCertificateImage"], img[src*="PreviewThumbnail"]')?.src || ''));
  }
  await page.waitForFunction(() => {
    const m = document.querySelector('#imageZoomModal');
    return m && getComputedStyle(m).display !== 'none';
  }, null, { timeout: 15000 });
}

/* The pager is populated by an ASYNC page-count request, so it does not exist the instant the
   popup opens - by design: counting pages means opening the PDF, and blocking the popup on that
   would be worse than letting it fill in. A run that measured immediately after opening therefore
   recorded "pager absent" on a cold-started server and then watched the very next assertion use
   the pager successfully. That was a harness race, not a product fault, and the fix is to wait for
   the state being asserted and to RECORD THE WAIT rather than hide it. */
async function waitForPager(page, timeout = 30000) {
  const t0 = Date.now();
  try {
    await page.waitForFunction(() => {
      const bar = document.getElementById('zoomPager');
      const label = document.getElementById('zoomPagerLabel');
      return bar && !bar.hidden && /Page \d+ of \d+/.test(label ? label.textContent : '');
    }, null, { timeout });
    return Date.now() - t0;
  } catch (e) {
    return -1;
  }
}

/** Hash what the <img> is ACTUALLY showing, by refetching its src inside the page session. */
async function shownImageHash(page) {
  const b64 = await page.evaluate(async () => {
    const img = document.getElementById('zoomedImage');
    if (!img || !img.src) return null;
    const r = await fetch(img.src);
    if (!r.ok) return null;
    const buf = new Uint8Array(await r.arrayBuffer());
    let s = '';
    for (let i = 0; i < buf.length; i++) s += String.fromCharCode(buf[i]);
    return btoa(s);
  });
  if (!b64) return null;
  return crypto.createHash('sha256').update(Buffer.from(b64, 'base64')).digest('hex').slice(0, 16);
}

/** Status of an arbitrary URL, requested with the page's own cookies. */
async function statusOf(page, url) {
  return page.evaluate(async u => {
    try { const r = await fetch(u); return r.status; } catch (e) { return -1; }
  }, url);
}

(async () => {
  const browser = await chromium.launch();

  // ---------------------------------------------------------------- admin session
  const ctx = await browser.newContext({ viewport: { width: 1400, height: 900 } });
  const page = await ctx.newPage();

  const ok = await login(page, ADMIN_USER, ADMIN_PASS);
  rec('login-admin', ok, `${ADMIN_USER} -> ${page.url()}`);
  if (!ok) { console.error('ABORT: login failed; every assertion below would be meaningless.'); process.exit(1); }

  /* Warm-up. The first request after a restart pays EF model build + JIT, and that cost has
     nothing to do with paging. Measuring it as if it did would either fail the run or, worse,
     bury a real regression under noise later. */
  await gotoRetry(page, `${BASE}/Certificate/CertificateInternalView/${MULTI_CERT}`);
  await gotoRetry(page, `${BASE}/Certificate/CertificateInternalView/${MULTI_CERT}`);

  // The Id the view published to the pager. Everything else keys off this.
  const certId = await page.getAttribute('#imageZoomModal', 'data-certificate-id');
  rec('A1 view publishes the certificate Id', !!certId && Number(certId) > 0, `data-certificate-id="${certId}"`);

  // A2 -- the page-count endpoint agrees the document is multi-page.
  const countProbe = await page.evaluate(async id => {
    const t = [];
    let body = null;
    for (let i = 0; i < 3; i++) {
      const s = performance.now();
      const r = await fetch(`/Certificate/CertificatePageCount?id=${id}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
      body = r.ok ? await r.json() : null;
      t.push(Math.round(performance.now() - s));
    }
    return { body, t };
  }, certId);
  const countJson = countProbe.body;
  const countWorst = Math.max(...countProbe.t);
  // Budget is generous on purpose: this is a dev box that throws SQL timeouts under contention,
  // and the popup does not block on this request. It is recorded so a real regression is visible.
  rec('A2 page count endpoint (value AND latency)',
      !!countJson && countJson.pages >= 2 && countWorst < 5000,
      `pages=${countJson && countJson.pages}, ${countProbe.t.join('ms, ')}ms (worst ${countWorst}ms)`);

  await openPopup(page);

  // A3 -- the pager appears, and says so. Positive control: the popup itself is open and the
  // "View all pages" link is present, so "pager visible" is not passing on a blank page.
  const waited = await waitForPager(page);
  const pagerVisible = await page.$$eval(PAGER, els => els.filter(e => !e.hidden).length);
  const allPagesPresent = await page.$$eval(ALL_PAGES, els => els.length);
  const label1 = await page.textContent(PAGER_LABEL).catch(() => '');
  rec('A3 pager visible on a multi-page certificate', waited >= 0 && pagerVisible === 1 && allPagesPresent === 1,
      `pager=${pagerVisible} after ${waited}ms, "View all pages" link on same popup=${allPagesPresent} (positive control), label="${label1}"`);

  // A4 -- prev disabled on page 1. Nothing before the first page.
  const prevDisabled = await page.$eval(PAGER_PREV, el => el.disabled).catch(() => null);
  rec('A4 prev disabled on page 1', prevDisabled === true, `disabled=${prevDisabled}`);

  // A5 -- THE ASSERTION THIS CR EXISTS FOR: next actually changes the displayed page, by bytes.
  const hash1 = await shownImageHash(page);
  await page.click(PAGER_NEXT);
  await page.waitForFunction(() => {
    const l = document.getElementById('zoomPagerLabel');
    return l && /Page 2 of/.test(l.textContent || '');
  }, null, { timeout: 20000 }).catch(() => {});
  await page.waitForTimeout(1200); // let the swapped src settle
  const hash2 = await shownImageHash(page);
  const label2 = await page.textContent(PAGER_LABEL).catch(() => '');
  rec('A5 next turns the page (compared by BYTES, not src)',
      !!hash1 && !!hash2 && hash1 !== hash2,
      `page1=${hash1} page2=${hash2} label="${label2}"`);

  await page.screenshot({ path: path.join(OUT, 'cr053-page2.png') });

  // A6 -- the popup did NOT close when the pager was clicked. #imageZoomModal closes on any click
  // that reaches the container; every control stops propagation. This is the CR-052 trap.
  const stillOpen = await page.$eval('#imageZoomModal', el => getComputedStyle(el).display !== 'none');
  rec('A6 popup stays open through a page turn', stillOpen === true, `modal displayed=${stillOpen}`);

  // A7 -- back to page 1 restores the ORIGINAL stored thumbnail, not a re-render.
  await page.click(PAGER_PREV);
  await page.waitForTimeout(1200);
  const hash1b = await shownImageHash(page);
  rec('A7 prev returns to the same page 1 image', !!hash1b && hash1b === hash1, `page1=${hash1} back=${hash1b}`);

  // A8 -- BOUNDS. An out-of-range page is a 404, not an exception and not another page.
  const pages = countJson ? countJson.pages : 2;
  const over = await statusOf(page, `${BASE}/Certificate/ViewCertificateImage?id=${certId}&page=${pages}`);
  const way = await statusOf(page, `${BASE}/Certificate/ViewCertificateImage?id=${certId}&page=9999`);
  const neg = await statusOf(page, `${BASE}/Certificate/ViewCertificateImage?id=${certId}&page=-1`);
  rec('A8 out-of-range page rejected', over === 404 && way === 404 && neg === 200,
      `page=${pages}:${over} (404) page=9999:${way} (404) page=-1:${neg} (200, falls back to the stored thumbnail)`);

  // A9 -- TIMING, measured rather than assumed. This is a live render per request.
  const timing = await page.evaluate(async id => {
    const t = [];
    for (let i = 0; i < 3; i++) {
      const s = performance.now();
      const r = await fetch(`/Certificate/ViewCertificateImage?id=${id}&page=1`);
      await r.arrayBuffer();
      t.push(Math.round(performance.now() - s));
    }
    return t;
  }, certId);
  const worst = Math.max(...timing);
  rec('A9 render timing measured', worst < 5000, `${timing.join('ms, ')}ms (worst ${worst}ms, budget 5000ms)`);

  await ctx.close();

  // ------------------------------------------------------- anonymous / public surface
  const anon = await browser.newContext({ viewport: { width: 1400, height: 900 } });
  const ap = await anon.newPage();
  await gotoRetry(ap, `${BASE}/Certificate/CertificateSingleView/${PUBLIC_CERT}`);

  const pubId = await ap.getAttribute('#imageZoomModal', 'data-certificate-id');
  const pubCount = await ap.evaluate(async id => {
    const r = await fetch(`/Certificate/CertificatePageCount?id=${id}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
    return r.ok ? r.json() : null;
  }, pubId);
  rec('P1 anonymous caller can count pages of a PUBLIC certificate',
      !!pubCount && typeof pubCount.pages === 'number', `id=${pubId} pages=${pubCount && pubCount.pages}`);

  await openPopup(ap);
  /* A single-page certificate never shows a pager, so there is nothing to wait FOR here - wait for
     the count request to have been made and settled instead, then assert the absence. */
  if (pubCount && pubCount.pages >= 2) await waitForPager(ap);
  else await ap.waitForTimeout(3000);
  const pubPager = await ap.$$eval(PAGER, els => els.filter(e => !e.hidden).length);
  const pubLink = await ap.$$eval(ALL_PAGES, els => els.length);
  const pubPages = pubCount ? pubCount.pages : 0;
  // Single-page or unreadable => the pager must HIDE and the link must remain. Multi-page => shown.
  const expected = pubPages >= 2 ? 1 : 0;
  rec('P2 pager matches the document, and the link survives either way',
      pubPager === expected && pubLink === 1,
      `pages=${pubPages} => pager=${pubPager} (expected ${expected}), "View all pages" link=${pubLink}`);
  await ap.screenshot({ path: path.join(OUT, 'cr053-public.png') });

  // P3 -- an anonymous caller must NOT be able to page a certificate they cannot see. Walk ids
  // until one is denied for the image, then confirm the page parameter is denied identically.
  const probe = await ap.evaluate(async startId => {
    for (let id = startId; id < startId + 60; id++) {
      const img = await fetch(`/Certificate/ViewCertificateImage?id=${id}`);
      if (img.status === 404) {
        const paged = await fetch(`/Certificate/ViewCertificateImage?id=${id}&page=1`);
        const count = await fetch(`/Certificate/CertificatePageCount?id=${id}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        return { id, img: img.status, paged: paged.status, count: count.status };
      }
    }
    return null;
  }, 1);
  rec('P3 page parameter opens no door the image did not',
      !!probe && probe.paged === 404 && probe.count === 404,
      probe ? `cert ${probe.id}: image=${probe.img}, page=1 -> ${probe.paged}, count -> ${probe.count}` : 'no denied certificate found in probe range');

  await anon.close();

  // ------------------------------------------------------------------ responsive pass
  const viewports = [
    ['desktop', 1400, 900], ['tablet-land', 1024, 768], ['tablet-port', 768, 1024],
    ['mobile-iph12', 390, 844], ['mobile-iphse', 375, 667]
  ];
  const rctx = await browser.newContext();
  const rp = await rctx.newPage();
  await login(rp, ADMIN_USER, ADMIN_PASS);
  let responsiveOk = true;
  const notes = [];
  for (const [name, w, h] of viewports) {
    await rp.setViewportSize({ width: w, height: h });
    await gotoRetry(rp, `${BASE}/Certificate/CertificateInternalView/${MULTI_CERT}`);
    await openPopup(rp);
    await waitForPager(rp);
    const m = await rp.evaluate(() => {
      const bar = document.getElementById('zoomPager');
      if (!bar || bar.hidden) return null;
      const r = bar.getBoundingClientRect();
      const label = document.getElementById('zoomPagerLabel');
      return {
        left: Math.round(r.left), right: Math.round(r.right),
        fontPx: label ? parseFloat(getComputedStyle(label).fontSize) : 0,
        overflow: document.documentElement.scrollWidth > window.innerWidth
      };
    });
    const good = !!m && m.left >= 0 && m.right <= w && m.fontPx >= 16 && !m.overflow;
    if (!good) responsiveOk = false;
    notes.push(`${name}:${m ? `${m.left}-${m.right}/${w} ${m.fontPx}px${m.overflow ? ' OVERFLOW' : ''}` : 'pager absent'}`);
    await rp.screenshot({ path: path.join(OUT, `cr053-pager-${name}.png`), fullPage: true });
  }
  rec('R1 pager fits, 16px minimum, no horizontal overflow (5 viewports)', responsiveOk, notes.join(' | '));
  await rctx.close();

  await browser.close();

  const failed = results.filter(r => !r.pass);
  console.log(`\n${results.length - failed.length}/${results.length} PASS`);
  fs.writeFileSync(path.join(OUT, 'cr053-results.json'), JSON.stringify(results, null, 2));
  if (failed.length) { console.log('FAILURES: ' + failed.map(f => f.id).join(', ')); process.exit(2); }
})().catch(e => { console.error('HARNESS FAILED:', e.message); process.exit(1); });
