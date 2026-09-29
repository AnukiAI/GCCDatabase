/**
 * Captures the screenshots used in the Analyst Certificate Guide, from the TEST environment.
 *
 * Run:  NODE_PATH="$(npm root -g)" node capture-analyst-shots.js
 * Then: python build-guide.py ../analyst-certificate-guide.md
 *
 * SHOT DECADE: 5x. (0x-1x Accounts, 2x Partner, 4x Company.)
 *
 * SIGNED IN AS test.analyst. The guide is for analysts, so the screenshots must show the analyst's
 * page — an admin capture would show the Administration menu and teach the reader to look for a
 * menu they do not have. `shot()` refuses to save if an admin-only affordance is visible.
 *
 * NOTHING IS EVER SAVED. This runs against the client's test environment, which holds the migrated
 * certificate data. The Create and Edit forms are photographed as forms — filled where a filled
 * field teaches something, never submitted. `shot()` aborts the run if the page has navigated to a
 * saved record, and no submit button is ever clicked.
 *
 * The import PREVIEW screen is deliberately NOT captured: reaching it means uploading a CSV, which
 * writes a staged import to the client's environment. Index and History are read-only and are
 * captured; the preview is described in words in the guide until that write is approved.
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
const USER = { u: process.env.ANALYST_USER || 'test.analyst', p: process.env.ANALYST_PASS || 'GccTest2026!' };

if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
const ok = [], bad = [];

/** Blur emails, drop the test-environment banner, neutralise the signed-in account name. */
const REDACT = () => {
  document.querySelectorAll('td, span, div, a, p').forEach(el => {
    if (el.children.length) return;
    if (/@[\w.-]+\.\w+/.test(el.textContent || '')) el.style.filter = 'blur(4.5px)';
  });
  [...document.querySelectorAll('div, header, section, p')]
    .filter(el => /TEST ENVIRONMENT/i.test(el.textContent || '') && el.textContent.length < 160)
    .forEach(el => { el.style.display = 'none'; });
  document.querySelectorAll('span, div, a, p, td').forEach(el => {
    if (el.children.length) return;
    if (/^\s*(test\.\w+|admin\.uat|2fatest)/i.test(el.textContent || '')) el.textContent = 'your.account';
  });
};

/**
 * Anything only an Admin should see. An analyst has no Administration menu.
 *
 * `.role-admin-show` alone is NOT the test. Most elements carrying it also carry
 * `.role-analyst-show` — they are shown to both roles, and the first version of this guard rejected
 * six of seven pages because of one such breadcrumb. Flag only the admin-EXCLUSIVE ones.
 *
 * Note also that Create.cshtml and Edit.cshtml set `ViewData["BodyClass"] = "role-admin"`, so on
 * those two pages an analyst's <body> carries role-analyst AND role-admin. Any check that reads the
 * body class would be wrong here too.
 */
const ADMIN_AFFORDANCE_VISIBLE = () => {
  const shown = el => el && el.offsetParent !== null;
  // Breadcrumbs are decorative and their role classes are inconsistent: the Import pages carry a
  // `role-element role-admin-show` breadcrumb although CertificateImportController is
  // [Authorize(Policy = "AnalystOrAdmin")] and analysts legitimately use those pages. Scope the
  // guard to ACTIONABLE controls — a button, link, input or menu item is what would mislead a
  // reader; a mis-classed breadcrumb label would not. Narrowing, not weakening: an admin-only
  // control anywhere still fails the shot.
  const ACTIONABLE = 'button, a, input, select, textarea, [role="button"], [role="menuitem"]';
  const adminOnly = [...document.querySelectorAll('.role-admin-show')]
    .filter(el => !el.classList.contains('role-analyst-show'))
    // The Import pages put their breadcrumb in `@section Breadcrumbs` wrapped in a
    // `role-admin-show` span, and that span contains a link — so an "actionable" test alone still
    // trips on it. The layout gates `.breadcrumb-wrapper` by role on its own, and a breadcrumb
    // label cannot mislead a reader about what they can do. Exclude that container only.
    .filter(el => !el.closest('.breadcrumb-wrapper'))
    .filter(el => el.matches(ACTIONABLE) || el.querySelector(ACTIONABLE));
  const leak = adminOnly.find(shown);
  if (leak) return `admin-only control <${leak.tagName}> "${(leak.textContent || '').trim().slice(0, 40)}"`;
  if ([...document.querySelectorAll('a.nav-link, .nav-link')]
      .some(a => /administration/i.test(a.textContent || '') && shown(a))) return 'Administration menu';
  return null;
};

async function shot(page, name, opts = {}) {
  const title = await page.title();
  const url = page.url();
  if (/gate|restricted access/i.test(title)) { bad.push(`${name}: GATE PAGE — not saved`); return; }
  if (/log ?in|sign ?in/i.test(title))       { bad.push(`${name}: LOGIN PAGE — not saved`); return; }
  // Hard stop: if a form was submitted we have written to the client's environment. Fail loudly
  // rather than quietly saving a picture of the consequence.
  if (/\/Certificate\/(Details|CertificateSingleView)\//i.test(url) && /Create|Edit/i.test(name)) {
    bad.push(`${name}: PAGE NAVIGATED TO A SAVED RECORD (${url}) — a form was submitted. ABORTING.`);
    throw new Error('form submitted — aborting capture');
  }
  const leak = await page.evaluate(ADMIN_AFFORDANCE_VISIBLE);
  if (leak) { bad.push(`${name}: ADMIN AFFORDANCE VISIBLE (${leak}) — wrong role, refused to save`); return; }
  await page.evaluate(REDACT);
  await page.waitForTimeout(400);
  const f = path.join(OUT, name + '.png');
  await page.screenshot({ path: f, ...opts });
  ok.push(name);
  console.log(`  ${name}.png  ${Math.round(fs.statSync(f).size / 1024)} KB  [${title}]`);
}

/** Shoot one element, optionally capping height. */
async function clipTo(page, selector, name, maxH) {
  const el = await page.$(selector);
  if (!el) { bad.push(`${name}: selector not found (${selector})`); return; }
  const box = await el.boundingBox();
  if (!box) { bad.push(`${name}: element has no box — hidden? (${selector})`); return; }
  const pad = 14;
  await shot(page, name, {
    fullPage: true,
    clip: {
      x: Math.max(0, box.x - pad), y: Math.max(0, box.y - pad),
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
  await c.route('**/*', (route, req) => {
    route.continue(req.url().startsWith(appOrigin) ? { headers: { ...req.headers(), ...GATE } } : {});
  });
  const p = await c.newPage();

  const landed = await login(p, USER.u, USER.p);
  if (/Account\/Login/i.test(landed)) {
    console.error(`  LOGIN FAILED as ${USER.u} — still on ${landed}`); await b.close(); process.exit(1);
  }
  if (!/TileViewInternal/i.test(landed)) {
    console.error(`  WRONG LANDING PAGE for ${USER.u}: ${landed} — expected TileViewInternal`);
    await b.close(); process.exit(1);
  }
  console.log(`  signed in as ${USER.u} -> ${landed}`);

  // 1. The internal certificate list — the analyst's home page.
  await p.waitForSelector('#resultsCount', { timeout: 15000 }).catch(() => {});
  await p.waitForTimeout(3000);
  await shot(p, '50-internal-list', { fullPage: false });

  // Grab a real certificate number from the list, for the Edit shot.
  const row = await p.$('#gridBody .table-body-row');
  const certNumber = row ? await row.getAttribute('data-cert-number') : null;
  const certId = row ? await row.getAttribute('data-cert-id') : null;
  console.log(`     edit target: ${certNumber} (id ${certId})`);

  // 1b. The Status filter (CR-051), with one status ticked so the shot shows it working: the
  //     result line must change AND be non-empty, or the picture is of a checkbox that did nothing.
  //     The first run used Hold, which had 0 certificates on the test environment — "Showing 0 of 0"
  //     is technically a change and teaches nothing. Take the first working-state status that has
  //     rows, from the app's own status list.
  const statusBefore = await p.$eval('#resultsCount', e => e.textContent.trim()).catch(() => '');
  const STATUS_TRY = ['Draft', 'Hold', 'Client Approved - Not Paid', 'Expired'];
  let holdBox = null, statusPicked = null;
  for (const s of STATUS_TRY) {
    const box = await p.$(`#status-content input[name="status"][value="${s}"]`);
    if (!box) continue;
    await box.scrollIntoViewIfNeeded();
    await box.click();
    await p.waitForFunction(prev => {
      const t = (document.querySelector('#resultsCount')?.textContent || '').trim();
      return /^Showing\s+\d+\s+of\s+\d+/i.test(t) && t !== prev;
    }, statusBefore, { timeout: 20000 }).catch(() => {});
    const t = await p.$eval('#resultsCount', e => e.textContent.trim()).catch(() => '');
    if (/^Showing\s+[1-9]\d*\s+of/i.test(t)) { holdBox = box; statusPicked = s; break; }
    await box.click(); // untick, try the next
    await p.waitForTimeout(2500);
  }
  if (holdBox) { await holdBox.click(); await p.waitForTimeout(2500); } // reset; the block below re-ticks and asserts
  if (!holdBox) {
    bad.push(`58-status-filter: none of ${STATUS_TRY.join(', ')} returned any certificates`);
  } else {
    console.log(`     status picked for the shot: ${statusPicked}`);
    await holdBox.scrollIntoViewIfNeeded();
    await holdBox.click();
    const changed = await p.waitForFunction(prev => {
      const t = (document.querySelector('#resultsCount')?.textContent || '').trim();
      return /^Showing\s+\d+\s+of\s+\d+/i.test(t) && t !== prev;
    }, statusBefore, { timeout: 20000 }).then(() => true).catch(() => false);
    const statusAfter = await p.$eval('#resultsCount', e => e.textContent.trim()).catch(() => '');
    if (!changed) bad.push(`58-status-filter: result line did not change ("${statusBefore}" -> "${statusAfter}")`);
    else {
      console.log(`     status filter: "${statusBefore}" -> "${statusAfter}"`);
      // The guide says the filter shows as a tag and is kept in the address. Check both, don't assume.
      const tag = await p.$eval('#activeFilterTags', e => e.textContent.trim()).catch(() => '');
      if (!tag.includes(statusPicked)) bad.push(`58-status-filter: no "${statusPicked}" tag above results (got "${tag}")`);
      if (!/status=/i.test(p.url())) bad.push(`58-status-filter: status not in page address (${p.url()})`);
      await p.waitForTimeout(1000);
      // clipTo measures in viewport coordinates but shoots the FULL page, and the filter sidebar
      // scrolls independently of the page — two runs cropped the wrong region. Bring the panel
      // into the viewport and clip the viewport itself, so both use the same coordinates.
      const panel = await p.$('.filter-panel:has(#status-content)');
      await panel.scrollIntoViewIfNeeded();
      await p.waitForTimeout(500);
      const pb = await panel.boundingBox();
      await shot(p, '58-status-filter', {
        fullPage: false,
        clip: { x: Math.max(0, pb.x - 14), y: Math.max(0, pb.y - 14), width: pb.width + 28, height: Math.min(pb.height + 28, 900 - Math.max(0, pb.y - 14)) }
      });
    }
    await holdBox.click().catch(() => {});
    await p.waitForTimeout(2000);
  }

  // 2-4. The Create form. NEVER SUBMITTED.
  await p.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2600);
  //     Certificate Information. The auto-assigned certificate number is the whole point of this
  //     shot, and it sits below the fold — an unscrolled viewport capture stops at the Company
  //     field and shows none of it. Scroll the number field into view first.
  const numberField = await p.$('#Number, input[name="Number"]');
  if (numberField) {
    await numberField.scrollIntoViewIfNeeded();
    await p.mouse.wheel(0, -160);   // keep the section heading in frame above it
    await p.waitForTimeout(900);
  } else { bad.push('51-create-information: Number field not found — cannot frame the shot'); }
  await shot(p, '51-create-information', { fullPage: false });

  //     Tags + the PDF upload zone.
  const pdfZone = await p.$('#pdfUploadZone');
  if (pdfZone) {
    await pdfZone.scrollIntoViewIfNeeded();
    await p.waitForTimeout(900);
    await shot(p, '52-create-pdf-and-tags', { fullPage: false });
  } else { bad.push('52-create-pdf-and-tags: #pdfUploadZone not found'); }

  //     Product Identifiers + Additional Fields.
  const ids = await p.$('#custom-fields-container');
  if (ids) {
    await ids.scrollIntoViewIfNeeded();
    await p.waitForTimeout(900);
    await shot(p, '53-create-identifiers', { fullPage: false });
  } else { bad.push('53-create-identifiers: #custom-fields-container not found'); }

  // 5. The Edit form — status, Certification Results, partners. NEVER SUBMITTED.
  if (certNumber) {
    await p.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNumber)}`, { waitUntil: 'domcontentloaded' });
    await p.waitForTimeout(2800);
    const t = await p.title();
    if (/error|not found|bad request/i.test(t)) {
      bad.push(`54-edit-form: unexpected page "${t}" for ${certNumber}`);
    } else {
      const statusSel = await p.$('select[name="Status"]');
      if (statusSel) { await statusSel.scrollIntoViewIfNeeded(); await p.waitForTimeout(700); }
      await shot(p, '54-edit-form', { fullPage: false });
    }
  } else { bad.push('54-edit-form: no certificate number available from the list'); }

  // 6. Import. Read-only; NOTHING is uploaded, so the preview step is not reached.
  //    The instructions panel on this page states the import rules the guide has to be consistent
  //    with (required columns, blank-number behaviour, overwrite acknowledgement, what is created
  //    automatically). Clip the panel whole rather than taking a viewport shot that cuts it in half.
  await p.goto(`${BASE}/CertificateImport`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2400);
  await clipTo(p, '.alert.alert-info', '55-import-instructions', 0);
  await clipTo(p, '#uploadForm', '57-import-upload-form', 0);

  // 7. Import history — where a batch is reviewed and undone.
  await p.goto(`${BASE}/CertificateImport/History`, { waitUntil: 'domcontentloaded' });
  await p.waitForTimeout(2400);
  await shot(p, '56-import-history', { fullPage: false });

  // PROVE THE GUARD CAN FAIL. A role check that has never rejected anything is an untested
  // assertion, not a gate — and this one was already wrong once. Sign in as an ADMIN on the same
  // page and require it to fire. If it stays silent, every "clean" analyst shot above is worthless
  // and the run says so rather than reporting success.
  const adminCtx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  await adminCtx.route('**/*', (route, req) => {
    route.continue(req.url().startsWith(appOrigin) ? { headers: { ...req.headers(), ...GATE } } : {});
  });
  const ap = await adminCtx.newPage();
  await login(ap, process.env.ADMIN_USER || 'admin.uat', process.env.ADMIN_PASS || 'TestPass123!');
  await ap.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
  await ap.waitForTimeout(3000);
  const firedOn = await ap.evaluate(ADMIN_AFFORDANCE_VISIBLE);
  if (firedOn) {
    console.log(`\n  guard self-test PASSED — rejected the admin session: ${firedOn}`);
  } else {
    bad.push('GUARD SELF-TEST FAILED: the admin-affordance check did not fire on an ADMIN session, '
           + 'so it cannot prove the analyst shots were taken as an analyst.');
  }
  await adminCtx.close();

  await b.close();
  console.log(`\n  captured ${ok.length}`);
  if (bad.length) { console.log('  PROBLEMS:'); bad.forEach(x => console.log('   - ' + x)); process.exit(1); }
})();
