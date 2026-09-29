/**
 * Captures the screenshots used in the Company User Guide, from the TEST environment.
 *
 * Run:  NODE_PATH="$(npm root -g)" node capture-external-shots.js
 * Then: python build-guide.py ../company-user-guide.md
 *
 * Sibling of capture-screenshots.js (Accounts guide) and capture-partner-shots.js (Partner guide).
 * Kept separate so each guide's shots can be re-taken without re-running the others'.
 *
 * SHOT DECADE: 4x. The Accounts guide owns 0x-1x and the Partner guide owns 2x.
 *
 * SIGNED IN AS test.external (scoped to one company) AND NOTHING ELSE. This is the whole point of
 * the guide: every screenshot has to show the reader the page THEY get. An admin capture would
 * show Add/Edit buttons, internal tags and an Actions column that the reader does not have, and
 * would teach them the product is broken for them. `shot()` therefore refuses to save if any
 * admin-only affordance is visible in the DOM.
 *
 * Emails are blurred, same convention as the other two guides — these are real certified
 * companies' contact addresses, not test data.
 */
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'https://db.greencirclecertified.com';
const OUT = path.join(__dirname, 'shots');
const GATE = {
  Authorization: 'Basic ' + Buffer.from(
    `${process.env.GATE_USER || 'gcc'}:${process.env.GATE_PASS || 'Cert-Test-Access-2026!'}`
  ).toString('base64')
};
const USER = { u: process.env.EXT_USER || 'test.external', p: process.env.EXT_PASS || 'GccTest2026!' };

if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
const ok = [], bad = [];

/**
 * Blur every email address on the page, and drop the test-environment banner.
 *
 * The banner reads "TEST ENVIRONMENT — data shown here reflects production records; do not share
 * this URL publicly." It is chrome that exists only on the test site, so leaving it in would put a
 * "do not share" instruction across every page of a client deliverable while telling the reader
 * nothing about the product. The guide's own opening callout states that the screenshots come from
 * the test environment, so the fact is disclosed in words rather than in 30 pictures of a banner.
 */
const REDACT = () => {
  document.querySelectorAll('td, span, div, a, p').forEach(el => {
    if (el.children.length) return;
    if (/@[\w.-]+\.\w+/.test(el.textContent || '')) el.style.filter = 'blur(4.5px)';
  });
  [...document.querySelectorAll('div, header, section, p')]
    .filter(el => /TEST ENVIRONMENT/i.test(el.textContent || '') && el.textContent.length < 160)
    .forEach(el => { el.style.display = 'none'; });
  // The signed-in account name sits in the header of EVERY page. Left alone it puts the internal
  // test account "test.external" across a client deliverable, which greps clean (it is pixels, not
  // text) and is obvious to any reader. Replace it with a neutral placeholder.
  document.querySelectorAll('span, div, a, p, td').forEach(el => {
    if (el.children.length) return;
    if (/^\s*(test\.\w+|admin\.uat|2fatest|partner@|p\w+\.\w+@)/i.test(el.textContent || '')) {
      el.textContent = 'your.account';
    }
  });
};

/** The "Showing N of M certificates" line, as a string. Used to prove a filter actually fired. */
const resultsText = page =>
  page.$eval('#resultsCount', el => (el.textContent || '').trim()).catch(() => '');

/**
 * True if anything only an Admin or Analyst should see is rendered. The role CSS hides these by
 * class rather than omitting them, so "present in the DOM" is not enough — check that it is
 * actually laid out (offsetParent), which is what the reader would see.
 */
const ADMIN_AFFORDANCE_VISIBLE = () => {
  const shown = el => el && el.offsetParent !== null;
  if ([...document.querySelectorAll('.role-analyst-show, .role-admin-show')].some(shown)) return 'role-gated element';
  if ([...document.querySelectorAll('a.nav-link')].some(a => /dashboard|administration|companies/i.test(a.textContent || '') && shown(a))) return 'admin nav link';
  if ([...document.querySelectorAll('button, a')].some(b => /^\s*(add|edit)\s+certificate/i.test(b.textContent || '') && shown(b))) return 'Add/Edit Certificate button';
  return null;
};

async function shot(page, name, opts = {}) {
  const title = await page.title();
  if (/gate|restricted access/i.test(title)) { bad.push(`${name}: GATE PAGE — not saved`); return; }
  if (/log ?in|sign ?in/i.test(title))       { bad.push(`${name}: LOGIN PAGE — not saved`); return; }
  const leak = await page.evaluate(ADMIN_AFFORDANCE_VISIBLE);
  if (leak) { bad.push(`${name}: ADMIN AFFORDANCE VISIBLE (${leak}) — wrong role, refused to save`); return; }
  await page.evaluate(REDACT);
  await page.waitForTimeout(400);
  const f = path.join(OUT, name + '.png');
  await page.screenshot({ path: f, ...opts });
  ok.push(name);
  console.log(`  ${name}.png  ${Math.round(fs.statSync(f).size / 1024)} KB  [${title}]`);
}

/** Shoot one element, optionally capping height so a long table becomes a readable excerpt. */
async function clipTo(page, selector, name, maxH) {
  const el = await page.$(selector);
  if (!el) { bad.push(`${name}: selector not found (${selector})`); return; }
  const box = await el.boundingBox();
  if (!box) { bad.push(`${name}: element has no box — hidden? (${selector})`); return; }
  const pad = 12;
  await shot(page, name, {
    fullPage: true,
    clip: {
      x: Math.max(0, box.x - pad),
      y: Math.max(0, box.y - pad),
      width: box.width + pad * 2,
      height: maxH ? Math.min(box.height + pad * 2, maxH) : box.height + pad * 2
    }
  });
}

async function login(page, u, p) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[name="Email"]', u);
  await page.fill('input[name="Password"]', p);
  await page.click('button[type="submit"], input[type="submit"]');
  await page.waitForTimeout(1800);
  return page.url();
}

(async () => {
  const b = await chromium.launch();
  const c = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const appOrigin = new URL(BASE).origin;
  // Gate header on the app's own origin only — forcing it onto the Font Awesome CDN request makes
  // every icon render as an empty grey square (learned on the Partner guide).
  await c.route('**/*', (route, req) => {
    route.continue(req.url().startsWith(appOrigin) ? { headers: { ...req.headers(), ...GATE } } : {});
  });
  const p = await c.newPage();

  const landed = await login(p, USER.u, USER.p);
  if (/Account\/Login/i.test(landed)) {
    console.error(`  LOGIN FAILED as ${USER.u} — still on ${landed}`);
    await b.close();
    process.exit(1);
  }
  if (!/TileViewExternal/i.test(landed)) {
    console.error(`  WRONG LANDING PAGE for ${USER.u}: ${landed} — expected TileViewExternal`);
    await b.close();
    process.exit(1);
  }
  console.log(`  signed in as ${USER.u} -> ${landed}`);

  // 1. The list view as it opens (CR-049: List is the default), with the filter sidebar.
  await p.waitForSelector('#tableTotalRows, #tableResultsCount, #resultsCount', { timeout: 15000 }).catch(() => {});
  await p.waitForTimeout(2500);
  await shot(p, '40-certificates-list', { fullPage: false });

  // 2. Filter sidebar on its own — the panels are the reader's main tool.
  // 535px = the first panel and the top of the second, cut on a row boundary. The full 900px crop rendered ~990px tall in the web guide and
  // dwarfed every other screenshot (reported 2026-09-28).
  await clipTo(p, '#filterSidebar', '41-filters', 535);

  // 3. Search. `fill()` sets the value without firing the key events the debounced handler listens
  //    for, so the first attempt at this shot showed a search term typed into the box next to an
  //    UNCHANGED result count — a picture of the feature not working. Type it properly, press
  //    Enter, and refuse to save unless the result line actually changed.
  const before = await resultsText(p);
  await p.click('#certificate-search');
  await p.type('#certificate-search', 'Sikafloor', { delay: 60 });
  await p.keyboard.press('Enter');
  // Wait for a SETTLED result line that differs from the previous one. "different from before" on
  // its own is not enough: the first version of this check passed on the transient "Loading…" and
  // saved a screenshot of a spinner. Require the final "Showing N of M" shape as well.
  await p.waitForFunction(
    prev => {
      const t = (document.querySelector('#resultsCount')?.textContent || '').trim();
      return /^Showing\s+\d+\s+of\s+\d+/i.test(t) && t !== prev;
    },
    before, { timeout: 20000 }
  ).catch(() => {});
  const after = await resultsText(p);
  await p.waitForTimeout(1200);
  if (after === before || !/^Showing\s+\d+\s+of\s+\d+/i.test(after)) {
    bad.push(`42-search-results: result line "${before}" -> "${after}" — search did not settle, not saved`);
  } else {
    console.log(`     search narrowed: "${before}" -> "${after}"`);
    await shot(p, '42-search-results', { fullPage: false });
  }

  // Clear the search the same way, and wait for the full set to come back.
  await p.click('#certificate-search');
  await p.keyboard.press('Control+A');
  await p.keyboard.press('Backspace');
  await p.keyboard.press('Enter');
  await p.waitForTimeout(2500);

  // 4. Selecting rows enables Download Selected. Shoot the state, not the empty page: tick two
  //    row checkboxes so the bulk bar is populated and the button is no longer disabled.
  const boxes = await p.$$('#gridBody input[type="checkbox"]');
  for (const cb of boxes.slice(0, 2)) { await cb.click().catch(() => {}); }
  await p.waitForTimeout(900);
  await shot(p, '43-select-and-download', { fullPage: false });

  // 6. The certificate detail page — reached from the LIST view, where the certificate number is a
  //    link. (The tile cards expose icon buttons rather than an anchor, so the obvious
  //    `.certificate-card a` selector finds nothing and silently skips the shot.) Taken BEFORE the
  //    tile-view shot so the detail page is captured from a clean, unselected list.
  await p.click('[data-action="clear-selection"], .clear-selection-btn').catch(() => {});
  await p.waitForTimeout(700);
  // The list has NO anchors. Each row is a `div.table-body-row` carrying
  // `onclick="viewCertificateDetails(number, id)"`, and the cert number is styled to look like a
  // link without being one — so both `a[href*="Certificate"]` and `.certificate-card a` find
  // nothing and skip the shot silently. Click the row itself.
  //    Clicking the row calls viewCertificateDetails(), which does window.open(..., '_blank') so
  //    the reader keeps their place in the list. That means clicking it here leaves `p` on the list
  //    and silently captures the list again — which is exactly what the first attempt saved. Build
  //    the same URL the app builds and open it in a page we control. `cid` disambiguates, because
  //    CR-038 allows several certificates to share one number.
  const certRow = await p.$('#gridBody .table-body-row');
  if (!certRow) {
    bad.push('45/46: no certificate row found in the list view');
  } else {
    const num = await certRow.getAttribute('data-cert-number');
    const cid = await certRow.getAttribute('data-cert-id');
    const url = `${BASE}/Certificate/CertificateExternalView/${encodeURIComponent(num)}?cid=${cid}`;
    console.log(`     opening certificate: ${num} -> ${url}`);
    const d = await c.newPage();
    await d.goto(url, { waitUntil: 'domcontentloaded' });
    await d.waitForTimeout(3200);
    const dTitle = await d.title();
    if (/log ?in|sign ?in|error|not found/i.test(dTitle)) {
      bad.push(`45-certificate-detail: unexpected page "${dTitle}" at ${url}`);
    } else {
      await shot(d, '45-certificate-detail', { fullPage: true });

      // 7. The shareable link. There is NO Share button on the External detail page — the view
      //    markup has one, but it is not what this role gets. What the reader actually has is a
      //    "Database Listing URL" row at the foot of the card, holding a /Certificate/Verify/<number>
      //    address. Shoot that row; a guide that told them to press a Share button would be
      //    describing a control that is not there.
      const urlRow = await d.$('xpath=//*[contains(normalize-space(.), "Database Listing URL")][not(.//*[contains(normalize-space(.), "Database Listing URL")])]/ancestor-or-self::div[1]');
      if (urlRow) {
        const box = await urlRow.boundingBox();
        if (box) {
          await shot(d, '46-database-listing-url', {
            fullPage: true,
            clip: { x: Math.max(0, box.x - 16), y: Math.max(0, box.y - 16), width: box.width + 32, height: box.height + 32 }
          });
        } else {
          bad.push('46-database-listing-url: row has no box');
        }
      } else {
        bad.push('46-database-listing-url: "Database Listing URL" row not found');
      }
    }
    await d.close();
  }

  // 6b. The preview popup, turned to page 2 (CR-052/053/054). Only about a quarter of certificates
  //     have more than one page, and on a single-page one the pager is absent by design — so a shot
  //     of the first row would usually show no pager and teach nothing. Ask the app's own page-count
  //     route (the one the pager uses) for each loaded row until a multi-page certificate turns up.
  const rows = await p.$$eval('#gridBody .table-body-row', els =>
    els.map(e => ({ num: e.getAttribute('data-cert-number'), cid: e.getAttribute('data-cert-id') })));
  let multi = null;
  for (const r of rows.slice(0, 60)) {
    const pages = await p.evaluate(async id => {
      const res = await fetch(`/Certificate/CertificatePageCount?id=${encodeURIComponent(id)}`,
        { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
      if (!res.ok) return 0;
      const j = await res.json().catch(() => ({}));
      return typeof j.pages === 'number' ? j.pages : 0;
    }, r.cid).catch(() => 0);
    if (pages >= 2) { multi = { ...r, pages }; break; }
  }
  if (!multi) {
    bad.push(`48-preview-pager: none of the first ${Math.min(rows.length, 60)} rows is multi-page — pager cannot be shown`);
  } else {
    console.log(`     multi-page certificate: ${multi.num} (${multi.pages} pages)`);
    const z = await c.newPage();
    await z.goto(`${BASE}/Certificate/CertificateExternalView/${encodeURIComponent(multi.num)}?cid=${multi.cid}`,
      { waitUntil: 'domcontentloaded' });
    await z.waitForTimeout(2500);
    await z.click('.fa-search-plus').catch(() => {});
    // Wait for the pager's own label, then turn the page and wait for the label to say so. Saving
    // on the first label alone would record page 1 — indistinguishable from the old popup.
    const turned = await z.waitForFunction(() => /^Page 1 of \d+$/.test(
      (document.getElementById('zoomPagerLabel')?.textContent || '').trim()), null, { timeout: 15000 })
      .then(() => z.click('#zoomPagerNext'))
      .then(() => z.waitForFunction(() => /^Page 2 of \d+$/.test(
        (document.getElementById('zoomPagerLabel')?.textContent || '').trim()), null, { timeout: 15000 }))
      .then(() => true).catch(() => false);
    const hasViewPdf = await z.$eval('#imageZoomModal .zoom-allpages-link', a => /View PDF/.test(a.textContent)).catch(() => false);
    if (!turned) bad.push(`48-preview-pager: pager did not reach "Page 2" on ${multi.num}`);
    else if (!hasViewPdf) bad.push(`48-preview-pager: View PDF button missing in popup on ${multi.num}`);
    else {
      await z.waitForTimeout(1500); // let the page-2 image finish painting
      await shot(z, '48-preview-pager', { fullPage: false });
    }
    await z.close();
  }

  // 5. Tile view. Last, so the grid has the whole page to itself and every card has rendered —
  //    an earlier ordering caught the grid mid-render with 3 of 23 cards painted.
  await p.click('.toggle-btn[data-view="tile"]').catch(() => {});
  await p.waitForTimeout(2500);
  // The grid lazy-loads on scroll, so a capture taken straight after the toggle catches a single
  // rendered row above a page of white space — which reads as a broken grid, not a tile view.
  // Nudge the scroll to force the next batch, then return to the top for the shot.
  await p.mouse.wheel(0, 1400);
  await p.waitForTimeout(2000);
  await p.mouse.wheel(0, -1400);
  await p.waitForTimeout(1500);
  const cards = await p.$$eval('#certificatesGrid > *', els => els.length).catch(() => 0);
  console.log(`     tile cards rendered: ${cards}`);
  // 3 = one full row at 1440px, which is what the viewport shot shows; fewer means the grid failed.
  if (cards < 3) bad.push(`44-tile-view: only ${cards} cards rendered — grid did not fill, shot would look broken`);
  await shot(p, '44-tile-view', { fullPage: false });

  // 8. Products page — External sees it WITHOUT the Actions column.
  await p.goto(`${BASE}/Product/Management`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2800);
  await shot(p, '47-products', { fullPage: false });

  await b.close();
  console.log(`\n  captured ${ok.length}`);
  if (bad.length) { console.log('  PROBLEMS:'); bad.forEach(x => console.log('   - ' + x)); process.exit(1); }
})();
