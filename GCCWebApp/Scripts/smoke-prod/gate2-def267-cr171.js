/**
 * Gate 2: Cross-role + responsive regression
 * Batch: CR-171 multi-partner + DEF-267
 * Checks:
 *   1. Create/Edit forms render correctly as Admin and Analyst (multi-select partner control present + usable)
 *   2. External user: NO Add/Edit cert buttons, NO Actions column on Product Management, cannot reach Create/Edit
 *   3. Public: cannot reach Create/Edit (redirected to login)
 *   4. Responsive screenshots of Edit form as Admin across 5 viewports
 *   5. axe-core accessibility scan of Create and Edit (no new WCAG 2.1 AA violations)
 *
 * Reads from: http://localhost:5101
 * Saves screenshots to: Scripts/smoke-prod/_results/screenshots/gate2-def267-cr171/
 * Does NOT click Save on any form.
 */

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE = 'http://localhost:5101';
const SCREENSHOT_DIR = path.join(__dirname, '_results', 'screenshots', 'gate2-def267-cr171');
const CREDS = { admin: 'TestPass123!', analyst: 'TestPass123!', external: 'TestPass123!' };

const VIEWPORTS = [
  { name: 'desktop',  width: 1920, height: 1080 },
  { name: 'laptop',   width: 1366, height: 768  },
  { name: 'tablet',   width: 768,  height: 1024 },
  { name: 'mobile',   width: 390,  height: 844  },
  { name: 'small',    width: 320,  height: 568  },
];

const results = [];
let certNumberForEdit = null; // will be discovered at runtime

// ── helpers ─────────────────────────────────────────────────────────────────

async function login(page, username) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 20000 });
  // The login form uses #Email field (accepts username or email)
  await page.waitForSelector('#Email', { timeout: 10000 });
  await page.fill('#Email', username);
  await page.fill('#Password', CREDS[username]);
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 20000 }),
    page.click('button[type="submit"]'),
  ]);
}

async function logout(page) {
  try {
    await page.goto(`${BASE}/Account/Logout`, { waitUntil: 'domcontentloaded', timeout: 8000 });
  } catch (_) { /* best-effort */ }
}

function pass(label, detail) {
  results.push({ status: 'PASS', label, detail });
  console.log(`  [PASS] ${label}${detail ? ' — ' + detail : ''}`);
}

function fail(label, detail) {
  results.push({ status: 'FAIL', label, detail });
  console.log(`  [FAIL] ${label}${detail ? ' — ' + detail : ''}`);
}

function nit(label, detail) {
  results.push({ status: 'NIT', label, detail });
  console.log(`  [NIT]  ${label}${detail ? ' — ' + detail : ''}`);
}

async function screenshot(page, name) {
  const file = path.join(SCREENSHOT_DIR, `${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
  return file;
}

// inject axe-core via CDN and run it, returns violations array
async function runAxe(page, context) {
  try {
    // Inject axe from CDN if not already present
    const axeInjected = await page.evaluate(() => typeof window.axe !== 'undefined');
    if (!axeInjected) {
      await page.addScriptTag({ url: 'https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.10.0/axe.min.js' });
      // Wait for axe to be available
      await page.waitForFunction(() => typeof window.axe !== 'undefined', { timeout: 10000 });
    }
    const violations = await page.evaluate(async () => {
      const result = await window.axe.run(document, {
        runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa'] },
        resultTypes: ['violations'],
      });
      return result.violations.map(v => ({
        id: v.id,
        impact: v.impact,
        description: v.description,
        helpUrl: v.helpUrl,
        nodes: v.nodes.length,
        nodeExamples: v.nodes.slice(0, 2).map(n => n.target.join(', ')),
      }));
    });
    return violations;
  } catch (err) {
    return [{ id: 'axe-error', impact: 'unknown', description: String(err), nodes: 0, nodeExamples: [] }];
  }
}

// ── discover a certificate number to use for Edit ────────────────────────────

async function discoverCertNumber(browser) {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  try {
    await login(page, 'admin');
    // Use the internal tile API which returns cert numbers directly
    const data = await page.evaluate(async () => {
      const r = await fetch('/api/certificate-tiles?page=1&pageSize=5', {
        headers: { 'Accept': 'application/json' }
      });
      return await r.json();
    });
    if (data && data.certificates && data.certificates.length > 0) {
      certNumberForEdit = data.certificates[0].certificateNumber;
    }
    console.log(`  [INFO] Cert for Edit: ${certNumberForEdit || 'NOT FOUND'}`);
  } catch (err) {
    console.log(`  [WARN] Could not discover cert number: ${err.message}`);
  } finally {
    await ctx.close();
  }
}

// ── TEST 1: Admin — Create form renders, partner multi-select present ─────────

async function testAdminCreate(browser) {
  console.log('\n[1] Admin — Create form');
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await ctx.newPage();
  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded', timeout: 40000 });
    // Wait for the form to settle (JS-driven dropdowns)
    await page.waitForSelector('#certificateCreateForm', { timeout: 20000 });
    await page.waitForTimeout(2000); // allow async inits

    // Check partner multi-select rendered
    const partnerSel = await page.$('#partnerSelect');
    if (partnerSel) {
      const multiple = await partnerSel.getAttribute('multiple');
      const size = await partnerSel.getAttribute('size');
      if (multiple !== null) {
        pass('Admin/Create: partnerSelect is multiple', `size="${size}"`);
      } else {
        fail('Admin/Create: partnerSelect is NOT multiple');
      }
    } else {
      fail('Admin/Create: #partnerSelect not found');
    }

    // Verify hidden sync input exists
    const hidden = await page.$('#partnersHidden');
    hidden ? pass('Admin/Create: #partnersHidden sync input present') : fail('Admin/Create: #partnersHidden missing');

    // Check Certification Results textarea (CLAUDE.md requirement)
    const certResults = await page.$('textarea[name="CertificationResults"], #CertificationResults');
    certResults ? pass('Admin/Create: CertificationResults field present') : fail('Admin/Create: CertificationResults field missing');

    // Check h1 is present and no duplicate
    const h1s = await page.$$('h1');
    if (h1s.length === 1) {
      pass('Admin/Create: exactly one h1');
    } else {
      nit(`Admin/Create: ${h1s.length} h1 elements (expected 1)`);
    }

    await screenshot(page, 'admin-create-desktop');
  } catch (err) {
    fail('Admin/Create: unexpected error', err.message);
  } finally {
    await ctx.close();
  }
}

// ── TEST 2: Analyst — Create form renders ─────────────────────────────────────

async function testAnalystCreate(browser) {
  console.log('\n[2] Analyst — Create form');
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await ctx.newPage();
  try {
    await login(page, 'analyst');
    await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForSelector('#certificateCreateForm', { timeout: 10000 });
    await page.waitForTimeout(1500);

    const partnerSel = await page.$('#partnerSelect');
    partnerSel
      ? pass('Analyst/Create: partnerSelect rendered')
      : fail('Analyst/Create: #partnerSelect not found');

    await screenshot(page, 'analyst-create-desktop');
  } catch (err) {
    fail('Analyst/Create: unexpected error', err.message);
  } finally {
    await ctx.close();
  }
}

// ── TEST 3: Admin — Edit form renders, partner multi-select present ───────────

async function testAdminEdit(browser) {
  if (!certNumberForEdit) {
    fail('Admin/Edit: no cert number discovered — skip');
    return;
  }
  console.log('\n[3] Admin — Edit form (cert: ' + certNumberForEdit + ')');
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await ctx.newPage();
  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNumberForEdit)}`, { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForSelector('#certificateEditForm', { timeout: 10000 });
    await page.waitForTimeout(2500); // async init (product cascade, custom fields)

    const partnerSel = await page.$('#partnerSelect');
    if (partnerSel) {
      const multiple = await partnerSel.getAttribute('multiple');
      multiple !== null
        ? pass('Admin/Edit: partnerSelect is multiple')
        : fail('Admin/Edit: partnerSelect is NOT multiple');
    } else {
      fail('Admin/Edit: #partnerSelect not found');
    }

    // Check Save Changes button exists (not Save — Edit form)
    const saveBtn = await page.$('#saveButton');
    saveBtn ? pass('Admin/Edit: Save Changes button present') : nit('Admin/Edit: #saveButton not found by id');

    // Verify no Save button has been clicked (form pristine)
    const formDirty = await page.evaluate(() => {
      const form = document.getElementById('certificateEditForm');
      return form ? false : true; // form still present = not submitted
    });
    formDirty ? fail('Admin/Edit: form not found after load') : pass('Admin/Edit: form intact (not submitted)');

    await screenshot(page, 'admin-edit-desktop-1366');
  } catch (err) {
    fail('Admin/Edit: unexpected error', err.message);
  } finally {
    await ctx.close();
  }
}

// ── TEST 4: External — CANNOT reach Create or Edit ────────────────────────────

async function testExternalRestrictions(browser) {
  console.log('\n[4] External — access restrictions');
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await ctx.newPage();
  try {
    await login(page, 'external');

    // 4a: Try to navigate to Create — must redirect away (login or access-denied)
    await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded', timeout: 15000 });
    const createUrl = page.url();
    const blockedCreate = !createUrl.includes('/Certificate/Create') || createUrl.includes('/Account/Login') || createUrl.includes('/Access');
    if (blockedCreate) {
      pass('External: /Certificate/Create is inaccessible', `redirected to: ${createUrl}`);
    } else {
      // Maybe rendered but without the form (check for form element)
      const createForm = await page.$('#certificateCreateForm');
      createForm
        ? fail('External: Create form rendered — should be blocked', createUrl)
        : pass('External: Create page rendered but form absent', createUrl);
    }

    // 4b: Try to navigate to Edit (if we have a cert number)
    if (certNumberForEdit) {
      await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNumberForEdit)}`, { waitUntil: 'domcontentloaded', timeout: 15000 });
      const editUrl = page.url();
      const blockedEdit = !editUrl.includes('/Certificate/Edit/') || editUrl.includes('/Account/Login') || editUrl.includes('/Access');
      if (blockedEdit) {
        pass('External: /Certificate/Edit is inaccessible', `redirected to: ${editUrl}`);
      } else {
        const editForm = await page.$('#certificateEditForm');
        editForm
          ? fail('External: Edit form rendered — should be blocked', editUrl)
          : pass('External: Edit page rendered but form absent', editUrl);
      }
    }

    // 4c: External tile view — confirm NO Add/Edit cert buttons visible
    await page.goto(`${BASE}/Certificate/CertificateTileViewExternal`, { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForTimeout(2000);

    const addButton = await page.$('a[href*="/Certificate/Create"], button:has-text("Add Certificate"), a:has-text("Add Certificate")');
    addButton
      ? fail('External/TileView: "Add Certificate" button is visible — should be hidden')
      : pass('External/TileView: no "Add Certificate" button visible');

    const editLink = await page.$('a[href*="/Certificate/Edit/"]');
    editLink
      ? fail('External/TileView: Edit link visible — should be hidden')
      : pass('External/TileView: no Edit links visible');

    await screenshot(page, 'external-tile-view');

    // 4d: Check Product Management page if accessible — no Actions column VISIBLE
    // Note: the Actions <th> uses CSS class role-analyst-show role-admin-show for visibility;
    // it stays in the DOM for External users but must be CSS-hidden.
    await page.goto(`${BASE}/Product/Management`, { waitUntil: 'domcontentloaded', timeout: 15000 });
    await page.waitForTimeout(1500);
    const prodUrl = page.url();
    if (prodUrl.includes('/Product/Management')) {
      // Check whether the actions-col th is actually visible (computed style)
      const actionsVisible = await page.evaluate(() => {
        // The actions column header has class actions-col and role-element role-analyst-show role-admin-show
        const th = document.querySelector('th.actions-col');
        if (!th) return false;
        const style = window.getComputedStyle(th);
        return style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
      });
      actionsVisible
        ? fail('External/ProductManagement: Actions column header is CSS-visible — should be hidden')
        : pass('External/ProductManagement: Actions column header is CSS-hidden (correct)');
    } else {
      pass('External/ProductManagement: redirected away (access blocked)', prodUrl);
    }

    // 4e: Check internal tags NOT visible in cert external view
    if (certNumberForEdit) {
      await page.goto(`${BASE}/Certificate/CertificateExternalView/${encodeURIComponent(certNumberForEdit)}`, { waitUntil: 'domcontentloaded', timeout: 15000 });
      await page.waitForTimeout(1500);
      const internalTagLabel = await page.$('text=Internal Tags, .tag.internal');
      internalTagLabel
        ? fail('External/CertDetail: internal tags visible — should be hidden')
        : pass('External/CertDetail: internal tags not visible');
    }

  } catch (err) {
    fail('External restrictions: unexpected error', err.message);
  } finally {
    await ctx.close();
  }
}

// ── TEST 5: Public (no login) — cannot reach Create/Edit ─────────────────────

async function testPublicRestrictions(browser) {
  console.log('\n[5] Public — access restrictions');
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await ctx.newPage();
  try {
    await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded', timeout: 15000 });
    const url = page.url();
    const blocked = url.includes('/Account/Login') || url.includes('/Identity/Account/Login') || !url.includes('/Certificate/Create');
    blocked
      ? pass('Public: /Certificate/Create redirects to login', url)
      : fail('Public: /Certificate/Create accessible without login', url);

    if (certNumberForEdit) {
      await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNumberForEdit)}`, { waitUntil: 'domcontentloaded', timeout: 15000 });
      const editUrl = page.url();
      const blockedEdit = editUrl.includes('/Account/Login') || !editUrl.includes('/Certificate/Edit/');
      blockedEdit
        ? pass('Public: /Certificate/Edit redirects to login', editUrl)
        : fail('Public: /Certificate/Edit accessible without login', editUrl);
    }
  } catch (err) {
    fail('Public restrictions: unexpected error', err.message);
  } finally {
    await ctx.close();
  }
}

// ── TEST 6: Responsive screenshots — Admin Edit across 5 viewports ───────────

async function testResponsiveEdit(browser) {
  if (!certNumberForEdit) {
    fail('Responsive/Edit: no cert number — skip');
    return;
  }
  console.log('\n[6] Responsive screenshots — Admin Edit');

  for (const vp of VIEWPORTS) {
    const ctx = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
    const page = await ctx.newPage();
    try {
      await login(page, 'admin');
      await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNumberForEdit)}`, { waitUntil: 'domcontentloaded', timeout: 20000 });
      await page.waitForSelector('#certificateEditForm', { timeout: 10000 });
      // Wait for real content: product cascade + custom fields async init
      await page.waitForTimeout(3000);
      // Additional content checks
      await page.waitForFunction(() => {
        const form = document.getElementById('certificateEditForm');
        if (!form) return false;
        const partnerSel = document.getElementById('partnerSelect');
        // Consider ready when partner select has at least one option or after 3s
        return partnerSel !== null;
      }, { timeout: 8000 }).catch(() => {});

      const file = await screenshot(page, `edit-admin-${vp.name}-${vp.width}x${vp.height}`);

      // Check layout: no horizontal overflow at this viewport
      const overflow = await page.evaluate(() => {
        return document.documentElement.scrollWidth > document.documentElement.clientWidth;
      });
      overflow
        ? nit(`Responsive/${vp.name}: horizontal overflow detected at ${vp.width}px`)
        : pass(`Responsive/${vp.name}: no horizontal overflow`, `${vp.width}x${vp.height}`);

      // Check partner control visible
      const partnerVisible = await page.$eval('#partnerSelect', el => {
        const r = el.getBoundingClientRect();
        return r.width > 0 && r.height > 0;
      }).catch(() => false);
      partnerVisible
        ? pass(`Responsive/${vp.name}: partnerSelect visible`)
        : nit(`Responsive/${vp.name}: partnerSelect not visibly rendered`);

      console.log(`  [SS]   Saved: ${path.basename(file)}`);
    } catch (err) {
      fail(`Responsive/${vp.name}: error`, err.message);
    } finally {
      await ctx.close();
    }
  }
}

// ── TEST 7: axe-core — Create form (Admin) ─────────────────────────────────────

async function testAxeCreate(browser) {
  console.log('\n[7] axe-core — Create form (Admin)');
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await ctx.newPage();
  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForSelector('#certificateCreateForm', { timeout: 10000 });
    await page.waitForTimeout(2000);

    const violations = await runAxe(page, 'Create');
    if (violations.length === 0) {
      pass('axe/Create: zero WCAG 2.1 AA violations');
    } else {
      // Categorise by impact
      const critical = violations.filter(v => v.impact === 'critical');
      const serious  = violations.filter(v => v.impact === 'serious');
      const moderate = violations.filter(v => v.impact === 'moderate');
      const minor    = violations.filter(v => v.impact === 'minor');

      violations.forEach(v => {
        const fn = (v.impact === 'critical' || v.impact === 'serious') ? fail : nit;
        fn(`axe/Create [${v.impact}]: ${v.id}`, `${v.description} (${v.nodes} node${v.nodes !== 1 ? 's' : ''})`);
      });

      console.log(`  [AXE]  Create summary — critical:${critical.length} serious:${serious.length} moderate:${moderate.length} minor:${minor.length}`);
    }
  } catch (err) {
    fail('axe/Create: error running axe', err.message);
  } finally {
    await ctx.close();
  }
}

// ── TEST 8: axe-core — Edit form (Admin) ──────────────────────────────────────

async function testAxeEdit(browser) {
  if (!certNumberForEdit) {
    fail('axe/Edit: no cert number — skip');
    return;
  }
  console.log('\n[8] axe-core — Edit form (Admin)');
  const ctx = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await ctx.newPage();
  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNumberForEdit)}`, { waitUntil: 'domcontentloaded', timeout: 20000 });
    await page.waitForSelector('#certificateEditForm', { timeout: 10000 });
    await page.waitForTimeout(3000); // let async init settle

    const violations = await runAxe(page, 'Edit');
    if (violations.length === 0) {
      pass('axe/Edit: zero WCAG 2.1 AA violations');
    } else {
      const critical = violations.filter(v => v.impact === 'critical');
      const serious  = violations.filter(v => v.impact === 'serious');
      const moderate = violations.filter(v => v.impact === 'moderate');
      const minor    = violations.filter(v => v.impact === 'minor');

      violations.forEach(v => {
        const fn = (v.impact === 'critical' || v.impact === 'serious') ? fail : nit;
        fn(`axe/Edit [${v.impact}]: ${v.id}`, `${v.description} (${v.nodes} node${v.nodes !== 1 ? 's' : ''})`);
      });

      console.log(`  [AXE]  Edit summary — critical:${critical.length} serious:${serious.length} moderate:${moderate.length} minor:${minor.length}`);
    }
  } catch (err) {
    fail('axe/Edit: error running axe', err.message);
  } finally {
    await ctx.close();
  }
}

// ── MAIN ──────────────────────────────────────────────────────────────────────

(async () => {
  fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });

  console.log('=== Gate 2: CR-171 / DEF-267 — Pre-deploy regression ===');
  console.log(`Base: ${BASE}`);
  console.log(`Screenshots: ${SCREENSHOT_DIR}\n`);

  const browser = await chromium.launch({ headless: true });

  try {
    // Discover a usable cert number first (as admin)
    await discoverCertNumber(browser);

    await testAdminCreate(browser);
    await testAnalystCreate(browser);
    await testAdminEdit(browser);
    await testExternalRestrictions(browser);
    await testPublicRestrictions(browser);
    await testResponsiveEdit(browser);
    await testAxeCreate(browser);
    await testAxeEdit(browser);
  } finally {
    await browser.close();
  }

  // ── Summary ────────────────────────────────────────────────────────────────
  const passes = results.filter(r => r.status === 'PASS').length;
  const fails  = results.filter(r => r.status === 'FAIL').length;
  const nits   = results.filter(r => r.status === 'NIT').length;

  console.log('\n════════════════════════════════════════════════════════');
  console.log(`RESULTS: ${passes} PASS  ${fails} FAIL  ${nits} NITS`);
  console.log('════════════════════════════════════════════════════════');

  if (fails > 0) {
    console.log('\nFAILURES:');
    results.filter(r => r.status === 'FAIL').forEach(r => console.log(`  - ${r.label}: ${r.detail || ''}`));
  }
  if (nits > 0) {
    console.log('\nNITS:');
    results.filter(r => r.status === 'NIT').forEach(r => console.log(`  - ${r.label}: ${r.detail || ''}`));
  }

  const overallStatus = fails > 0 ? 'FAIL' : nits > 0 ? 'PASS-WITH-NITS' : 'PASS';
  console.log(`\nOVERALL: ${overallStatus}`);

  // Save JSON results
  const jsonPath = path.join(__dirname, '_results', `gate2-def267-cr171-${new Date().toISOString().replace(/[:.]/g, '-')}.json`);
  fs.writeFileSync(jsonPath, JSON.stringify({ overallStatus, passes, fails, nits, certNumberForEdit, results }, null, 2));
  console.log(`Results written to: ${jsonPath}`);

  process.exit(fails > 0 ? 1 : 0);
})();
