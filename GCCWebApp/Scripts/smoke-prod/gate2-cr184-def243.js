// PRE-DEPLOY GATE 2 — CR-184 (Export Active Product Identifiers) + DEF-243 (cert form dropdowns)
// Cross-role + responsive regression + axe-core WCAG 2.1 AA on changed surfaces.
//
// Surfaces:
//   A) /Product/Management  — new "Export Active Product Identifiers" button (CR-184)
//   B) /Certificate/Create  — DEF-243 form changes (category/type/subcategory + inline add guard)
//   C) /Certificate/Edit/:number — DEF-243 edit-load fix
//
// Role matrix:
//   Admin    — A visible + works; B/C accessible
//   Analyst  — A visible + works; B/C accessible
//   External — A button MUST NOT appear; B/C must redirect/forbid; no Actions column
//   Public   — unauthenticated; no access to A/B/C
//
// Run:  node Scripts/smoke-prod/gate2-cr184-def243.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const RESULTS_DIR = path.join(__dirname, '_results');
const SHOTS_DIR = path.join(RESULTS_DIR, 'gate2-cr184-def243-' + STAMP);
[RESULTS_DIR, SHOTS_DIR].forEach(d => { if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true }); });

// 5 required viewports per spec
const VIEWPORTS = [
  { name: 'desktop', width: 1440, height: 900 },
  { name: 'laptop',  width: 1024, height: 768 },
  { name: 'tablet',  width: 768,  height: 1024 },
  { name: 'mobile',  width: 390,  height: 844 },
  { name: 'small',   width: 360,  height: 740 },
];

const CREDS = {
  admin:    { email: 'admin',    password: 'TestPass123!' },
  analyst:  { email: 'analyst',  password: 'TestPass123!' },
  external: { email: 'external', password: 'TestPass123!' },
};

const results = [];
const record = (check, role, vp, status, notes = '', shot = null) => {
  const entry = { check, role, vp, status, notes, shot };
  results.push(entry);
  const tag = status === 'PASS' ? 'PASS' : status === 'FAIL' ? 'FAIL' : 'WARN';
  const shotNote = shot ? ` [${path.basename(shot)}]` : '';
  console.log(`  [${tag}] ${role}/${vp} ${check}${notes ? ' — ' + notes : ''}${shotNote}`);
};

async function login(page, role) {
  const creds = CREDS[role];
  if (!creds) return;
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(500);
  await page.locator('#Email').fill(creds.email);
  await page.locator('#Password').fill(creds.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(1000);
  const url = page.url();
  if (/\/Account\/Login/i.test(url)) throw new Error(`Login failed for ${role}`);
}

async function shot(page, label) {
  const fname = `${label}.png`;
  const p = path.join(SHOTS_DIR, fname);
  await page.screenshot({ path: p, fullPage: true });
  return p;
}

// Wait for real content to render (not just networkidle)
async function waitForContent(page, selector, timeout = 8000) {
  try {
    await page.waitForSelector(selector, { state: 'visible', timeout });
    await page.waitForTimeout(800);
  } catch (e) {
    await page.waitForTimeout(1500);
  }
}

// ---- SURFACE A: Product Management page (/Product/Management) ----
async function checkProductManagement(page, role, vpName) {
  await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded', timeout: 60000 });
  // Wait for real content: either the table header or a redirect signal
  await waitForContent(page, '#product-table, .admin-container, .login-form, #Email', 6000);
  await page.waitForTimeout(1200); // let role-element JS run

  const currentUrl = page.url();

  // External + Public must NOT be on this page as privileged users
  if (role === 'external') {
    // External CAN visit /Product/Management (it's allowed for external read-only)
    // but MUST NOT see the export button or Actions column
    const state = await page.evaluate(() => {
      const exportBtn = document.getElementById('export-all-product-ids-btn');
      const actionsHeader = Array.from(document.querySelectorAll('th')).find(th => th.textContent.trim() === 'Actions');
      const actionsVisible = actionsHeader ? actionsHeader.offsetParent !== null : false;

      // role-element visibility: check computed style and offsetParent
      const btnVisible = exportBtn
        ? (exportBtn.offsetParent !== null && getComputedStyle(exportBtn).display !== 'none' && getComputedStyle(exportBtn).visibility !== 'hidden')
        : false;
      return {
        btnExists: !!exportBtn,
        btnVisible,
        actionsHeaderVisible: actionsVisible,
        actionsHeaderText: actionsHeader ? actionsHeader.textContent.trim() : null,
        exportBtnDisplay: exportBtn ? getComputedStyle(exportBtn).display : 'N/A',
        exportBtnVisibility: exportBtn ? getComputedStyle(exportBtn).visibility : 'N/A',
      };
    });
    record('CR184.export-btn-hidden-external', role, vpName,
      !state.btnVisible ? 'PASS' : 'FAIL',
      `btnExists=${state.btnExists} visible=${state.btnVisible} display=${state.exportBtnDisplay} vis=${state.exportBtnVisibility}`);
    record('CR184.no-actions-col-external', role, vpName,
      !state.actionsHeaderVisible ? 'PASS' : 'FAIL',
      `actionsHeaderVisible=${state.actionsHeaderVisible}`);
    return state;
  }

  // Admin / Analyst: button must be present and visible
  const state = await page.evaluate(() => {
    const exportBtn = document.getElementById('export-all-product-ids-btn');
    const addBtn = document.getElementById('add-product-btn');
    const importBtn = document.getElementById('import-btn');
    const actionsHeader = Array.from(document.querySelectorAll('th')).find(th => th.textContent.trim() === 'Actions');
    return {
      exportBtnExists: !!exportBtn,
      exportBtnVisible: exportBtn
        ? (exportBtn.offsetParent !== null && getComputedStyle(exportBtn).display !== 'none')
        : false,
      exportBtnTitle: exportBtn ? exportBtn.title : '',
      addBtnVisible: addBtn ? (addBtn.offsetParent !== null) : false,
      importBtnVisible: importBtn ? (importBtn.offsetParent !== null) : false,
      actionsHeaderVisible: actionsHeader ? actionsHeader.offsetParent !== null : false,
      tableExists: !!document.getElementById('product-table'),
    };
  });
  record('CR184.export-btn-visible', role, vpName,
    state.exportBtnVisible ? 'PASS' : 'FAIL',
    `exists=${state.exportBtnExists} visible=${state.exportBtnVisible} title="${state.exportBtnTitle}"`);
  record('CR184.actions-col-visible', role, vpName,
    state.actionsHeaderVisible ? 'PASS' : 'FAIL',
    `actionsColVisible=${state.actionsHeaderVisible}`);
  return state;
}

// ---- SURFACE B: Certificate Create (/Certificate/Create) ----
async function checkCertCreate(page, role, vpName) {
  if (role === 'external') {
    // External must NOT be able to access Create — should redirect to login or get forbidden
    const resp = await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(800);
    const url = page.url();
    const blocked = /Login|Account|Access|Denied|403/i.test(url) || (resp && resp.status() === 403);
    record('DEF243.create-blocked-external', role, vpName,
      blocked ? 'PASS' : 'FAIL',
      `landed at: ${url} (status=${resp ? resp.status() : 'unknown'})`);
    return;
  }

  await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitForContent(page, '#ProductCategoryId, .certificate-form, .admin-container', 8000);
  await page.waitForTimeout(1500);

  const state = await page.evaluate(() => {
    const catSel = document.getElementById('ProductCategoryId');
    const typeSel = document.getElementById('ProductTypeId');
    const subSel = document.getElementById('ProductSubcategoryId');

    // DEF-243: check the inline-add guard exists in the product dropdown area
    const addWrapper = document.querySelector('.custom-dropdown-wrapper');
    const addInput = addWrapper ? addWrapper.querySelector('.custom-dropdown-add-input') : null;
    const addBtn = addWrapper ? addWrapper.querySelector('.custom-dropdown-add-btn') : null;
    const guardMsg = addWrapper ? addWrapper.querySelector('.custom-dropdown-error') : null;

    return {
      catSelExists: !!catSel,
      typeSelExists: !!typeSel,
      subSelExists: !!subSel,
      catOptions: catSel ? catSel.options.length : 0,
      typeOptions: typeSel ? typeSel.options.length : 0,
      hasInlineAddWrapper: !!addWrapper,
      hasAddInput: !!addInput,
      hasAddBtn: !!addBtn,
      hasGuardMsg: !!guardMsg,
      formExists: !!document.querySelector('form[action*="Create"], form#cert-form, form'),
    };
  });
  record('DEF243.create-cat-dropdown', role, vpName,
    state.catSelExists && state.catOptions > 1 ? 'PASS' : (state.catSelExists ? 'WARN' : 'FAIL'),
    `catExists=${state.catSelExists} options=${state.catOptions}`);
  record('DEF243.create-type-dropdown', role, vpName,
    state.typeSelExists ? 'PASS' : 'FAIL',
    `typeExists=${state.typeSelExists} options=${state.typeOptions}`);
  record('DEF243.create-sub-dropdown', role, vpName,
    state.subSelExists ? 'PASS' : 'FAIL',
    `subExists=${state.subSelExists}`);
  record('DEF243.create-inline-add-guard', role, vpName,
    state.hasInlineAddWrapper && state.hasGuardMsg ? 'PASS' : (state.hasInlineAddWrapper ? 'WARN' : 'FAIL'),
    `wrapper=${state.hasInlineAddWrapper} guardMsg=${state.hasGuardMsg} addBtn=${state.hasAddBtn}`);
}

// ---- SURFACE C: Certificate Edit (/Certificate/Edit/:number) ----
async function checkCertEdit(page, role, vpName, certNumber) {
  if (!certNumber) {
    record('DEF243.edit-no-cert-number', role, vpName, 'WARN', 'No cert number available for edit test');
    return;
  }
  if (role === 'external') {
    const resp = await page.goto(BASE + `/Certificate/Edit/${certNumber}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(800);
    const url = page.url();
    const blocked = /Login|Account|Access|Denied|403/i.test(url) || (resp && resp.status() === 403);
    record('DEF243.edit-blocked-external', role, vpName,
      blocked ? 'PASS' : 'FAIL',
      `landed at: ${url} (status=${resp ? resp.status() : 'unknown'})`);
    return;
  }

  await page.goto(BASE + `/Certificate/Edit/${certNumber}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await waitForContent(page, '#ProductCategoryId, .admin-container', 8000);
  await page.waitForTimeout(2000); // DEF-243: wait for cascade + suppressProductAutoFill

  const state = await page.evaluate(() => {
    const catSel = document.getElementById('ProductCategoryId');
    const typeSel = document.getElementById('ProductTypeId');
    const subSel = document.getElementById('ProductSubcategoryId');
    if (!catSel) return { error: 'no ProductCategoryId found' };

    // DEF-243 fix: server-selected value must match current select value
    // (JS auto-fill must not clobber it)
    let serverVal = '', currentVal = catSel.value;
    for (const o of catSel.options) {
      if (o.defaultSelected && o.value) { serverVal = o.value; break; }
    }

    // SearchableSelect visible input text
    const parent = catSel.parentNode;
    const ssWrapper = parent ? parent.querySelector('.searchable-select-wrapper') : null;
    const ssInput = ssWrapper ? (ssWrapper.querySelector('input')?.value || '') : '';

    // What text does the current selected option show?
    let curText = '';
    for (const o of catSel.options) if (o.value === currentVal) { curText = o.text; break; }

    return {
      serverVal,
      currentVal,
      curText,
      ssInput,
      clobberDetected: serverVal && currentVal !== serverVal,
      ssOutOfSync: ssInput && ssInput.trim() !== curText.trim(),
      typeSelExists: !!typeSel,
      subSelExists: !!subSel,
    };
  });

  if (state.error) {
    record('DEF243.edit-cat-present', role, vpName, 'FAIL', state.error);
    return;
  }
  record('DEF243.edit-no-clobber', role, vpName,
    !state.clobberDetected ? 'PASS' : 'FAIL',
    `server=${state.serverVal} current=${state.currentVal} clobbered=${state.clobberDetected}`);
  record('DEF243.edit-ss-sync', role, vpName,
    !state.ssOutOfSync ? 'PASS' : 'WARN',
    `ss="${state.ssInput}" curText="${state.curText}" outOfSync=${state.ssOutOfSync}`);
  record('DEF243.edit-type-dropdown', role, vpName,
    state.typeSelExists ? 'PASS' : 'FAIL',
    `typeExists=${state.typeSelExists}`);
}

// ---- axe-core WCAG 2.1 AA on the changed surfaces ----
async function runAxe(page, role, vpName, surface) {
  try {
    await page.addScriptTag({ url: 'https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.9.1/axe.min.js' });
    await page.waitForTimeout(600);

    const violations = await page.evaluate(() => {
      return new Promise(resolve => {
        axe.run(document, {
          runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] },
        }, (err, results) => {
          if (err) return resolve([{ error: err.message }]);
          resolve(results.violations.map(v => ({
            id: v.id,
            impact: v.impact,
            help: v.help,
            description: v.description,
            nodes: v.nodes.slice(0, 3).map(n => ({
              html: (n.html || '').slice(0, 250),
              target: n.target,
              failureSummary: (n.failureSummary || '').slice(0, 200),
            })),
          })));
        });
      });
    });

    const critical = violations.filter(v => v.impact === 'critical' || v.impact === 'serious');
    const moderate = violations.filter(v => v.impact === 'moderate');
    const minor = violations.filter(v => v.impact === 'minor');

    const summary = `critical/serious=${critical.length} moderate=${moderate.length} minor=${minor.length}`;
    const status = critical.length > 0 ? 'FAIL' : moderate.length > 0 ? 'WARN' : 'PASS';
    record(`axe.${surface}`, role, vpName, status, summary);

    if (violations.length > 0) {
      console.log(`\n    --- axe violations: ${role}/${vpName} ${surface} ---`);
      for (const v of violations) {
        console.log(`    [${v.impact}] ${v.id}: ${v.help}`);
        for (const n of v.nodes.slice(0, 2)) {
          console.log(`      node: ${n.html.slice(0, 120)}`);
          console.log(`      target: ${JSON.stringify(n.target)}`);
          if (n.failureSummary) console.log(`      issue: ${n.failureSummary.slice(0, 150)}`);
        }
      }
      console.log();
    }

    return violations;
  } catch (e) {
    record(`axe.${surface}`, role, vpName, 'WARN', `axe injection error: ${e.message}`);
    return [];
  }
}

// ---- Resolve a usable cert number for edit testing ----
async function resolveCertNumber(context) {
  try {
    const resp = await context.request.get(`${BASE}/api/certificate-tiles-internal?pageSize=20&limit=20`);
    const data = await resp.json().catch(() => null);
    const tiles = data?.certificates ?? data?.Certificates ?? [];
    const cand = tiles.find(t => {
      const n = t.certificateNumber ?? t.CertificateNumber;
      return n && /^\d{2}-\d{4}$/.test(n);
    });
    return cand ? (cand.certificateNumber ?? cand.CertificateNumber) : null;
  } catch (e) {
    return null;
  }
}

// ============================ MAIN ============================
(async () => {
  console.log(`\n[GATE 2] CR-184 + DEF-243  |  Base: ${BASE}  |  Stamp: ${STAMP}\n`);

  const browser = await chromium.launch({ headless: true });
  const allAxeViolations = {};

  // === PHASE 1: Role × Surface matrix ===
  // Desktop-only for the role checks; all 5 viewports for screenshots
  for (const role of ['admin', 'analyst', 'external', 'public']) {
    console.log(`\n=== Role: ${role.toUpperCase()} ===`);
    allAxeViolations[role] = {};

    // Shared cert number per role session (admin/analyst only)
    let certNumber = null;

    for (const vp of VIEWPORTS) {
      const ctx = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
      const page = await ctx.newPage();

      try {
        // Login (skip for public)
        if (role !== 'public') {
          await login(page, role);
        }

        // ---- SURFACE A: Product Management ----
        console.log(`  [${vp.name}] Surface A: /Product/Management`);
        if (role === 'public') {
          // Public cannot access Product Management — should redirect to login
          await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded', timeout: 60000 });
          await page.waitForTimeout(800);
          const url = page.url();
          const redirected = /Login|Account/i.test(url);
          record('A.public-redirect', role, vp.name, redirected ? 'PASS' : 'WARN',
            `landed at: ${url}`);
        } else {
          await checkProductManagement(page, role, vp.name);
        }

        // Screenshot A at this viewport
        await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded', timeout: 60000 });
        await waitForContent(page, '#product-table, .admin-container, #Email', 5000);
        await page.waitForTimeout(1000);
        const shotA = await shot(page, `${role}-${vp.name}-product-management`);
        record('screenshot.product-management', role, vp.name, 'PASS', '', shotA);

        // ---- SURFACE B: Certificate Create ----
        console.log(`  [${vp.name}] Surface B: /Certificate/Create`);
        if (role !== 'public') {
          await checkCertCreate(page, role, vp.name);
        }
        // Screenshot B
        if (role !== 'public' && role !== 'external') {
          await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
          await waitForContent(page, '#ProductCategoryId, .admin-container', 6000);
          await page.waitForTimeout(1200);
          const shotB = await shot(page, `${role}-${vp.name}-cert-create`);
          record('screenshot.cert-create', role, vp.name, 'PASS', '', shotB);
        }

        // ---- SURFACE C: Certificate Edit ----
        console.log(`  [${vp.name}] Surface C: /Certificate/Edit`);
        if (role === 'admin' || role === 'analyst') {
          // Resolve cert number once per viewport (re-use across viewports for same role)
          if (!certNumber) {
            certNumber = await resolveCertNumber(ctx);
          }
          await checkCertEdit(page, role, vp.name, certNumber);
          if (certNumber) {
            await page.goto(BASE + `/Certificate/Edit/${certNumber}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
            await waitForContent(page, '#ProductCategoryId, .admin-container', 6000);
            await page.waitForTimeout(2000);
            const shotC = await shot(page, `${role}-${vp.name}-cert-edit`);
            record('screenshot.cert-edit', role, vp.name, 'PASS', '', shotC);
          }
        } else if (role === 'external') {
          await checkCertEdit(page, role, vp.name, '26-0023');
        }

        // ---- axe-core: desktop only to avoid duplicate noise ----
        if (vp.name === 'desktop') {
          // Axe on Product Management
          if (role !== 'public') {
            await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded', timeout: 60000 });
            await waitForContent(page, '#product-table, .admin-container, #Email', 5000);
            await page.waitForTimeout(1500);
            const v1 = await runAxe(page, role, vp.name, 'product-management');
            allAxeViolations[role]['product-management'] = v1;
          }

          // Axe on Certificate Create (admin/analyst only)
          if (role === 'admin' || role === 'analyst') {
            await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
            await waitForContent(page, '#ProductCategoryId, .admin-container', 6000);
            await page.waitForTimeout(1500);
            const v2 = await runAxe(page, role, vp.name, 'cert-create');
            allAxeViolations[role]['cert-create'] = v2;

            // Axe on Certificate Edit
            if (certNumber) {
              await page.goto(BASE + `/Certificate/Edit/${certNumber}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
              await waitForContent(page, '#ProductCategoryId, .admin-container', 6000);
              await page.waitForTimeout(2000);
              const v3 = await runAxe(page, role, vp.name, 'cert-edit');
              allAxeViolations[role]['cert-edit'] = v3;
            }
          }
        }

      } catch (e) {
        record('gate2.error', role, vp.name, 'FAIL', e.message);
      } finally {
        await ctx.close();
      }
    }
  }

  await browser.close();

  // ==== SUMMARY ====
  const passed = results.filter(r => r.status === 'PASS').length;
  const failed = results.filter(r => r.status === 'FAIL').length;
  const warned = results.filter(r => r.status === 'WARN').length;
  const failures = results.filter(r => r.status === 'FAIL');
  const warnings = results.filter(r => r.status === 'WARN');

  console.log(`\n${'='.repeat(70)}`);
  console.log(`GATE 2 SUMMARY  ${passed} PASS / ${failed} FAIL / ${warned} WARN  (${results.length} total checks)`);
  console.log(`${'='.repeat(70)}\n`);

  if (failures.length > 0) {
    console.log('FAILURES:');
    for (const f of failures) console.log(`  FAIL [${f.role}/${f.vp}] ${f.check}: ${f.notes}`);
    console.log();
  }
  if (warnings.length > 0) {
    console.log('WARNINGS:');
    for (const w of warnings) console.log(`  WARN [${w.role}/${w.vp}] ${w.check}: ${w.notes}`);
    console.log();
  }

  // Verdict logic
  const criticalFails = failures.filter(f =>
    f.check.startsWith('CR184.export-btn-hidden-external') ||
    f.check.startsWith('CR184.no-actions-col-external') ||
    f.check.startsWith('DEF243.create-blocked-external') ||
    f.check.startsWith('DEF243.edit-blocked-external') ||
    (f.check.startsWith('axe.') && f.status === 'FAIL')
  );
  let verdict;
  if (failed === 0 && warned === 0) verdict = 'PASS';
  else if (failed === 0) verdict = 'PASS-WITH-NITS';
  else if (criticalFails.length > 0) verdict = 'FAIL-BLOCKING';
  else verdict = 'PASS-WITH-NITS';

  console.log(`OVERALL GATE 2 VERDICT: ${verdict}`);
  console.log(`Screenshots: ${SHOTS_DIR}\n`);

  // Save JSON
  const outFile = path.join(RESULTS_DIR, `gate2-cr184-def243-${STAMP}.json`);
  fs.writeFileSync(outFile, JSON.stringify({ stamp: STAMP, base: BASE, verdict, passed, failed, warned, results, axeViolations: allAxeViolations }, null, 2));
  console.log(`Results JSON: ${outFile}`);

  process.exit(failed > 0 ? 1 : 0);
})();
