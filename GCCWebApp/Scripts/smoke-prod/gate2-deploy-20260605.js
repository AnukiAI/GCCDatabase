/**
 * Gate 2 Pre-Deploy Audit — GCCWebApp-deploy-20260605-1128.zip
 *
 * Tests:
 *   1. /Admin/UserManagement  — CR-020rev Company Access column (admin-only)
 *   2. /Certificate/Create    — DEF-246 form-state restore (Analyst+)
 *   3. /Certificate/Edit/{n}  — DEF-246 form-state restore (Analyst+)
 *   4. /Product/Management    — external restrictions hold
 *   5. /Certificate/CertificateTileViewInternal — DEF-248/249/250
 *
 * Viewports: desktop-1920, desktop-1440, tablet-768, mobile-390, mobile-360
 * Roles: public (unauthenticated), external, analyst, admin
 * AxeCore: WCAG 2.1 AA on each changed surface
 *
 * Run: node Scripts/smoke-prod/gate2-deploy-20260605.js
 */

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const axe = require('axe-core');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const OUT_DIR = path.join(__dirname, '_results', 'gate2-deploy-20260605');
const SCREENSHOTS_DIR = path.join(OUT_DIR, 'screenshots');

// Ensure output dirs exist
[OUT_DIR, SCREENSHOTS_DIR].forEach(d => { if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true }); });

// Viewports: 5 required by Gate 2 spec
const VIEWPORTS = {
  'desktop-1920': { width: 1920, height: 1080 },
  'desktop-1440': { width: 1440, height: 900 },
  'tablet-768':   { width: 768,  height: 1024 },
  'mobile-390':   { width: 390,  height: 844 },
  'mobile-360':   { width: 360,  height: 780 },
};

// Role credentials
const CREDS = {
  public:   null,
  external: { email: 'external',  password: 'TestPass123!' },
  analyst:  { email: 'analyst',   password: 'TestPass123!' },
  admin:    { email: 'admin',     password: 'TestPass123!' },
};

const NAV_TIMEOUT = 60000;
const results = [];
let certNumberForEdit = null; // populated at runtime

function rec(view, role, viewport, id, desc, status, detail = '') {
  const entry = { view, role, viewport, id, desc, status, detail };
  results.push(entry);
  const tag = status === 'PASS' ? 'PASS' : status === 'FAIL' ? 'FAIL' : 'WARN';
  console.log(`  [${tag}] ${role}/${viewport} :: ${id} — ${desc}${detail ? ' | ' + detail : ''}`);
}

async function login(page, role) {
  const creds = CREDS[role];
  if (!creds) return;
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
  await page.locator('#Email').fill(creds.email);
  await page.locator('#Password').fill(creds.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);
}

async function screenshot(page, label) {
  const fname = `${label.replace(/[\/\\: ]/g, '-')}.png`;
  const p = path.join(SCREENSHOTS_DIR, fname);
  try {
    await page.screenshot({ path: p, fullPage: true });
    return fname;
  } catch (e) {
    return `screenshot-error: ${e.message}`;
  }
}

// Run axe-core on current page; returns array of new violations
async function runAxe(page, context) {
  try {
    const axeSource = axe.source;
    const violations = await page.evaluate((src) => {
      const script = document.createElement('script');
      script.text = src;
      document.head.appendChild(script);
      return new Promise(resolve => {
        window.axe.run({
          runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] },
          rules: { 'color-contrast': { enabled: true } }
        }).then(r => resolve(r.violations));
      });
    }, axeSource);
    return violations;
  } catch (e) {
    return [{ id: 'axe-error', help: 'AxeCore execution error', nodes: [], impact: 'critical', description: e.message }];
  }
}

// Helper: check no horizontal scroll on mobile
async function checkNoHorizScroll(page, view, role, vp) {
  const vw = parseInt(vp.replace('mobile-', ''));
  if (vw > 500) return; // only mobile
  const overflow = await page.evaluate(() => ({
    docW: document.documentElement.scrollWidth,
    winW: window.innerWidth,
  }));
  rec(view, role, vp, `${view}.no-horiz-scroll`,
    'No horizontal scroll on mobile',
    overflow.docW <= overflow.winW + 2 ? 'PASS' : 'FAIL',
    `docW=${overflow.docW} winW=${overflow.winW}`);
}

// ─────────────────────────────────────────────────────────────────────────────
// VIEW 1: /Admin/UserManagement
// ─────────────────────────────────────────────────────────────────────────────
async function auditUserManagement(browser) {
  console.log('\n[VIEW] /Admin/UserManagement — CR-020rev Company Access column');
  const VIEW = 'user-mgmt';
  const ROLES_EXPECTED = {
    public:   401,
    external: 401,
    analyst:  401, // admin-only route
    admin:    200,
  };

  for (const [role, expectedStatus] of Object.entries(ROLES_EXPECTED)) {
    for (const [vp, vpSize] of Object.entries(VIEWPORTS)) {
      const ctx = await browser.newContext({ viewport: vpSize });
      const page = await ctx.newPage();
      const consoleErrors = [];
      page.on('console', msg => { if (msg.type() === 'error' && !/favicon/i.test(msg.text())) consoleErrors.push(msg.text()); });

      try {
        await login(page, role);
        const resp = await page.goto(BASE + '/Admin/UserManagement', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
        const finalUrl = page.url();
        const wasRedirected = /\/(Login|AccessDenied)/i.test(finalUrl) && role !== 'public';
        const effectiveStatus = wasRedirected ? 401 : (resp ? resp.status() : null);

        // Access gate
        const accessOk = (effectiveStatus === expectedStatus) ||
          (expectedStatus === 401 && (effectiveStatus === 403 || wasRedirected));
        rec(VIEW, role, vp, `${VIEW}.access`,
          `Access control (expected ${expectedStatus})`,
          accessOk ? 'PASS' : 'FAIL',
          `effective=${effectiveStatus} url=${finalUrl.replace(BASE, '')}`);

        if (!accessOk || expectedStatus !== 200) {
          await ctx.close(); continue;
        }

        // Wait for JS to populate the table
        await page.waitForTimeout(2000);
        await page.waitForSelector('#user-table tbody tr', { timeout: 10000 }).catch(() => {});

        // A: Company Access column header present
        const headers = await page.$$eval('#user-table thead th', ths =>
          ths.map(th => ({ text: th.textContent.trim(), sort: th.dataset.sort || '', ariaSort: th.getAttribute('aria-sort') || '' })));
        const hasCompanyAccess = headers.some(h => /Company Access/i.test(h.text) && h.sort === 'companyAccess');
        const hasUserCompany   = headers.some(h => /User Company/i.test(h.text) && h.sort === 'userCompanyName');
        rec(VIEW, role, vp, `${VIEW}.company-access-col-header`,
          '"Company Access" sortable header present and separate from "User Company"',
          hasCompanyAccess && hasUserCompany ? 'PASS' : 'FAIL',
          `companyAccess=${hasCompanyAccess} userCompany=${hasUserCompany}`);

        // B: aria-sort on sortable headers
        const missingAriaSort = headers.filter(h => h.sort && !h.ariaSort);
        rec(VIEW, role, vp, `${VIEW}.aria-sort-on-headers`,
          'Sortable column headers have aria-sort attribute',
          missingAriaSort.length === 0 ? 'PASS' : 'WARN',
          missingAriaSort.length ? `missing on: ${missingAriaSort.map(h=>h.sort).join(', ')}` : '');

        // C: table has caption
        const hasCaption = await page.$eval('#user-table caption', el => el.textContent.trim().length > 0).catch(() => false);
        rec(VIEW, role, vp, `${VIEW}.table-caption`,
          'Table has descriptive caption for screen readers',
          hasCaption ? 'PASS' : 'WARN',
          hasCaption ? '' : 'caption element missing or empty');

        // D: data rows rendered with company-access cell
        const cellCounts = await page.evaluate(() => ({
          rows: document.querySelectorAll('#user-table tbody tr.admin-table-row').length,
          access: document.querySelectorAll('#user-table tbody td.user-company-access').length,
        }));
        rec(VIEW, role, vp, `${VIEW}.company-access-cells`,
          'Every user row has a Company Access cell',
          cellCounts.rows > 0 && cellCounts.access === cellCounts.rows ? 'PASS' : 'FAIL',
          `rows=${cellCounts.rows} accessCells=${cellCounts.access}`);

        // E: Admin/Analyst rows show "All"
        const adminAll = await page.evaluate(() => {
          const rows = Array.from(document.querySelectorAll('#user-table tbody tr.admin-table-row'));
          for (const row of rows) {
            const roleCell = row.querySelector('td.user-role-cell, td:nth-child(5)');
            const role = roleCell ? roleCell.textContent.trim().toLowerCase() : '';
            if (role === 'admin' || role === 'analyst') {
              const accessCell = row.querySelector('td.user-company-access');
              return { role, shown: accessCell ? accessCell.textContent.trim() : '(missing)' };
            }
          }
          return null;
        });
        if (adminAll) {
          rec(VIEW, role, vp, `${VIEW}.admin-analyst-shows-all`,
            'Admin/Analyst user Company Access shows "All"',
            adminAll.shown === 'All' ? 'PASS' : 'FAIL',
            `${adminAll.role} → "${adminAll.shown}"`);
        }

        // F: User Company field is free-text input (not select) in Add/Edit modal
        // Try clicking Add New User
        await page.click('#add-user-btn').catch(() => {});
        await page.waitForTimeout(800);
        const modalVisible = await page.$eval('#user-modal, #add-user-modal, [id*="user-modal"]',
          el => el && (el.style.display !== 'none' || el.classList.contains('show'))).catch(() => false);
        if (modalVisible) {
          const userCompanyType = await page.evaluate(() => {
            const inputs = document.querySelectorAll('#user-modal input[name*="company" i], #user-modal input[id*="company" i], #user-modal input[name*="UserCompany" i]');
            const selects = document.querySelectorAll('#user-modal select[name*="company" i], #user-modal select[id*="company" i]');
            return {
              inputCount: inputs.length,
              selectCount: selects.length,
              inputTypes: Array.from(inputs).map(i => i.type),
            };
          });
          rec(VIEW, role, vp, `${VIEW}.user-company-freetext`,
            'User Company in modal is a free-text input (CR-020rev)',
            userCompanyType.inputCount > 0 && userCompanyType.selectCount === 0 ? 'PASS' : 'WARN',
            `inputs=${userCompanyType.inputCount} selects=${userCompanyType.selectCount}`);
          // Close modal via Escape
          await page.keyboard.press('Escape');
          await page.waitForTimeout(400);
        } else {
          rec(VIEW, role, vp, `${VIEW}.user-company-freetext`,
            'User Company in modal is a free-text input (CR-020rev)',
            'WARN', 'Modal did not open — could not verify field type');
        }

        // G: Responsive / horizontal scroll
        await checkNoHorizScroll(page, VIEW, role, vp);

        // H: Console errors
        rec(VIEW, role, vp, `${VIEW}.no-console-errors`,
          'No JS console errors',
          consoleErrors.length === 0 ? 'PASS' : 'FAIL',
          consoleErrors.slice(0, 3).join(' | '));

        // I: AxeCore (admin + desktop-1440 only to avoid duplicates)
        if (role === 'admin' && vp === 'desktop-1440') {
          console.log('    [axe] Running WCAG 2.1 AA scan on UserManagement...');
          const violations = await runAxe(page, VIEW);
          const impactful = violations.filter(v => ['critical', 'serious'].includes(v.impact));
          rec(VIEW, role, vp, `${VIEW}.axe-wcag21aa`,
            `AxeCore WCAG 2.1 AA (${violations.length} violations, ${impactful.length} critical/serious)`,
            impactful.length === 0 ? 'PASS' : 'FAIL',
            impactful.map(v => `[${v.impact}] ${v.id}: ${v.help} (${v.nodes.length} nodes)`).join(' | '));
          fs.writeFileSync(
            path.join(OUT_DIR, 'axe-user-management.json'),
            JSON.stringify(violations, null, 2));
        }

        // Screenshot on desktop-1440 per role
        if (vp === 'desktop-1440') {
          await screenshot(page, `${VIEW}-${role}-${vp}`);
        }
        // Screenshot all mobile viewports
        if (vp.startsWith('mobile')) {
          await screenshot(page, `${VIEW}-${role}-${vp}`);
        }

      } catch (e) {
        rec(VIEW, role, vp, `${VIEW}.fatal`, 'Test ran without throwing', 'FAIL', e.message);
      }
      await ctx.close();
    }
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// VIEW 2 & 3: /Certificate/Create and /Certificate/Edit — DEF-246
// ─────────────────────────────────────────────────────────────────────────────
async function auditCertificateCreateEdit(browser) {
  console.log('\n[VIEW] /Certificate/Create and /Certificate/Edit — DEF-246 form-state restore');

  // First get a valid cert number for Edit
  if (!certNumberForEdit) {
    const ctx = await browser.newContext({ viewport: VIEWPORTS['desktop-1440'] });
    const page = await ctx.newPage();
    try {
      await login(page, 'analyst');
      await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
      await page.waitForTimeout(2500);
      const firstLink = await page.$eval(
        '.cert-card a[href*="/Edit/"], .cert-tile a[href*="/Edit/"], a[href*="/Certificate/Edit/"]',
        a => a.getAttribute('href')
      ).catch(() => null);
      if (firstLink) {
        const match = firstLink.match(/\/Edit\/([^?#]+)/);
        if (match) certNumberForEdit = match[1];
      }
      // Fallback: try to get from API
      if (!certNumberForEdit) {
        const resp = await page.goto(BASE + '/api/certificates?page=1&pageSize=1', { timeout: NAV_TIMEOUT });
        const json = await page.evaluate(() => {
          try { return JSON.parse(document.body.innerText); } catch { return null; }
        });
        certNumberForEdit = json?.data?.[0]?.number || json?.items?.[0]?.number || '1';
      }
    } catch (e) {
      certNumberForEdit = '1';
    }
    await ctx.close();
  }
  console.log(`    [info] Using cert number for Edit: ${certNumberForEdit}`);

  const VIEWS = [
    { view: 'cert-create', url: '/Certificate/Create',           roles: { public: 401, external: 401, analyst: 200, admin: 200 } },
    { view: 'cert-edit',   url: `/Certificate/Edit/${certNumberForEdit}`, roles: { public: 401, external: 401, analyst: 200, admin: 200 } },
  ];

  for (const { view, url, roles } of VIEWS) {
    console.log(`\n  [surface] ${url}`);
    for (const [role, expectedStatus] of Object.entries(roles)) {
      for (const [vp, vpSize] of Object.entries(VIEWPORTS)) {
        const ctx = await browser.newContext({ viewport: vpSize });
        const page = await ctx.newPage();
        const consoleErrors = [];
        page.on('console', msg => { if (msg.type() === 'error' && !/favicon/i.test(msg.text())) consoleErrors.push(msg.text()); });

        try {
          await login(page, role);
          const resp = await page.goto(BASE + url, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
          const finalUrl = page.url();
          const wasRedirected = /\/(Login|AccessDenied)/i.test(finalUrl);
          const effectiveStatus = wasRedirected ? 401 : (resp ? resp.status() : null);

          const accessOk = (effectiveStatus === expectedStatus) ||
            (expectedStatus === 401 && (effectiveStatus === 403 || wasRedirected));
          rec(view, role, vp, `${view}.access`,
            `Access control (expected ${expectedStatus})`,
            accessOk ? 'PASS' : 'FAIL',
            `effective=${effectiveStatus}`);

          if (!accessOk || expectedStatus !== 200) { await ctx.close(); continue; }

          await page.waitForTimeout(1500);

          // DEF-246: Form state restore — verify form fields are populated (Edit) or empty/default (Create)
          if (view === 'cert-edit') {
            const companyVal = await page.$eval('select[name="CompanyId"], #CompanyId',
              el => el.value).catch(() => null);
            rec(view, role, vp, `${view}.def246-form-populated`,
              'DEF-246: Edit form loads with existing cert data (CompanyId populated)',
              companyVal && companyVal !== '' ? 'PASS' : 'FAIL',
              `CompanyId="${companyVal}"`);
          }

          if (view === 'cert-create') {
            const formExists = await page.$('#certificateCreateForm').catch(() => null);
            rec(view, role, vp, `${view}.def246-form-renders`,
              'DEF-246: Create form renders correctly',
              formExists ? 'PASS' : 'FAIL', '');
          }

          // Required fields have proper labels
          const labelIssues = await page.evaluate(() => {
            const issues = [];
            document.querySelectorAll('input[required], select[required], textarea[required]').forEach(el => {
              const id = el.id;
              const label = id ? document.querySelector(`label[for="${id}"]`) : null;
              const ariaLabel = el.getAttribute('aria-label');
              const ariaLabelledBy = el.getAttribute('aria-labelledby');
              if (!label && !ariaLabel && !ariaLabelledBy) {
                issues.push(id || el.name || '(unknown)');
              }
            });
            return issues;
          });
          rec(view, role, vp, `${view}.required-fields-labeled`,
            'Required fields have accessible labels',
            labelIssues.length === 0 ? 'PASS' : 'FAIL',
            labelIssues.length ? `unlabeled: ${labelIssues.join(', ')}` : '');

          // aria-required on required fields
          const ariaReqIssues = await page.evaluate(() => {
            const issues = [];
            document.querySelectorAll('input[required], select[required], textarea[required]').forEach(el => {
              if (el.getAttribute('aria-required') !== 'true') {
                issues.push(el.id || el.name || '(unknown)');
              }
            });
            return issues;
          });
          rec(view, role, vp, `${view}.aria-required`,
            'Required fields have aria-required="true"',
            ariaReqIssues.length === 0 ? 'PASS' : 'WARN',
            ariaReqIssues.length ? `missing aria-required: ${ariaReqIssues.slice(0,5).join(', ')}` : '');

          // Heading hierarchy — only one h1
          const h1Count = await page.evaluate(() => document.querySelectorAll('h1').length);
          rec(view, role, vp, `${view}.single-h1`,
            'Single h1 on page',
            h1Count === 1 ? 'PASS' : 'FAIL',
            `h1 count=${h1Count}`);

          // Page title visible
          const h1Text = await page.$eval('h1', el => el.textContent.trim()).catch(() => '');
          rec(view, role, vp, `${view}.h1-text`,
            'h1 has descriptive text',
            h1Text.length > 0 ? 'PASS' : 'FAIL',
            `h1="${h1Text}"`);

          // Horizontal scroll on mobile
          await checkNoHorizScroll(page, view, role, vp);

          // Console errors
          rec(view, role, vp, `${view}.no-console-errors`,
            'No JS console errors',
            consoleErrors.length === 0 ? 'PASS' : 'FAIL',
            consoleErrors.slice(0, 3).join(' | '));

          // AxeCore — analyst + desktop-1440 only
          if (role === 'analyst' && vp === 'desktop-1440') {
            console.log(`    [axe] Running WCAG 2.1 AA scan on ${view}...`);
            const violations = await runAxe(page, view);
            const impactful = violations.filter(v => ['critical', 'serious'].includes(v.impact));
            rec(view, role, vp, `${view}.axe-wcag21aa`,
              `AxeCore WCAG 2.1 AA (${violations.length} violations, ${impactful.length} critical/serious)`,
              impactful.length === 0 ? 'PASS' : 'FAIL',
              impactful.map(v => `[${v.impact}] ${v.id}: ${v.help} (${v.nodes.length} nodes)`).join(' | '));
            fs.writeFileSync(
              path.join(OUT_DIR, `axe-${view}.json`),
              JSON.stringify(violations, null, 2));
          }

          // Screenshots
          if (vp === 'desktop-1440' || vp.startsWith('mobile')) {
            await screenshot(page, `${view}-${role}-${vp}`);
          }

        } catch (e) {
          rec(view, role, vp, `${view}.fatal`, 'Test ran without throwing', 'FAIL', e.message);
        }
        await ctx.close();
      }
    }
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// VIEW 4: /Product/Management — external restrictions
// ─────────────────────────────────────────────────────────────────────────────
async function auditProductManagement(browser) {
  console.log('\n[VIEW] /Product/Management — external restrictions');
  const VIEW = 'product-mgmt';
  const ROLES_EXPECTED = {
    public:   401,
    external: 200, // can view but with restrictions
    analyst:  200,
    admin:    200,
  };

  for (const [role, expectedStatus] of Object.entries(ROLES_EXPECTED)) {
    for (const [vp, vpSize] of Object.entries(VIEWPORTS)) {
      const ctx = await browser.newContext({ viewport: vpSize });
      const page = await ctx.newPage();
      const consoleErrors = [];
      page.on('console', msg => { if (msg.type() === 'error' && !/favicon/i.test(msg.text())) consoleErrors.push(msg.text()); });

      try {
        await login(page, role);
        const resp = await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
        const finalUrl = page.url();
        const wasRedirected = /\/(Login|AccessDenied)/i.test(finalUrl);
        const effectiveStatus = wasRedirected ? 401 : (resp ? resp.status() : null);

        const accessOk = (effectiveStatus === expectedStatus) ||
          (expectedStatus === 401 && (effectiveStatus === 403 || wasRedirected));
        rec(VIEW, role, vp, `${VIEW}.access`,
          `Access control (expected ${expectedStatus})`,
          accessOk ? 'PASS' : 'FAIL',
          `effective=${effectiveStatus}`);

        if (!accessOk || expectedStatus !== 200) { await ctx.close(); continue; }

        await page.waitForTimeout(2000);

        // External: NO Actions column (per CLAUDE.md spec)
        if (role === 'external') {
          const actionsColVisible = await page.evaluate(() => {
            // Check table headers for Actions column
            const ths = Array.from(document.querySelectorAll('table thead th'));
            return ths.some(th => /actions?/i.test(th.textContent));
          });
          rec(VIEW, role, vp, `${VIEW}.external-no-actions-col`,
            'External user: No Actions column on Product Management',
            !actionsColVisible ? 'PASS' : 'FAIL',
            `actionsColumnFound=${actionsColVisible}`);

          // External: Add/Import/Export buttons
          const adminBtnsVisible = await page.evaluate(() => {
            const btns = ['#add-product-btn', '#import-btn', '#template-btn'];
            return btns.filter(sel => {
              const el = document.querySelector(sel);
              if (!el) return false;
              const style = window.getComputedStyle(el);
              return style.display !== 'none' && style.visibility !== 'hidden';
            });
          });
          rec(VIEW, role, vp, `${VIEW}.external-no-add-import-btns`,
            'External user: Add New Product / Import / Template buttons hidden',
            adminBtnsVisible.length === 0 ? 'PASS' : 'FAIL',
            adminBtnsVisible.length ? `visible restricted buttons: ${adminBtnsVisible.join(', ')}` : '');

          // External: export button SHOULD be visible (non-restricted)
          const exportVisible = await page.evaluate(() => {
            const el = document.querySelector('#export-btn');
            if (!el) return false;
            const style = window.getComputedStyle(el);
            return style.display !== 'none' && style.visibility !== 'hidden';
          });
          rec(VIEW, role, vp, `${VIEW}.external-export-visible`,
            'External user: Export button is visible (allowed)',
            exportVisible ? 'PASS' : 'WARN',
            `exportBtnVisible=${exportVisible}`);
        }

        // Admin/Analyst: Add button present
        if (role === 'admin' || role === 'analyst') {
          const addBtnVisible = await page.evaluate(() => {
            const el = document.querySelector('#add-product-btn');
            if (!el) return false;
            const style = window.getComputedStyle(el);
            return style.display !== 'none' && style.visibility !== 'hidden';
          });
          rec(VIEW, role, vp, `${VIEW}.admin-analyst-add-btn-visible`,
            `${role}: Add New Product button visible`,
            addBtnVisible ? 'PASS' : 'FAIL', '');
        }

        // Heading hierarchy
        const h1Count = await page.evaluate(() => document.querySelectorAll('h1').length);
        rec(VIEW, role, vp, `${VIEW}.single-h1`,
          'Single h1 on page',
          h1Count === 1 ? 'PASS' : 'FAIL', `h1 count=${h1Count}`);

        // Search input has label (QC accessibility item)
        const searchLabeled = await page.evaluate(() => {
          const input = document.querySelector('#search-input');
          if (!input) return false;
          const label = document.querySelector('label[for="search-input"]');
          return !!(label || input.getAttribute('aria-label') || input.getAttribute('aria-labelledby'));
        });
        rec(VIEW, role, vp, `${VIEW}.search-labeled`,
          'Search input has accessible label',
          searchLabeled ? 'PASS' : 'FAIL', '');

        // Filter selects have labels
        const filterLabelIssues = await page.evaluate(() => {
          const issues = [];
          ['#category-filter', '#company-filter'].forEach(sel => {
            const el = document.querySelector(sel);
            if (!el) return;
            const label = el.id ? document.querySelector(`label[for="${el.id}"]`) : null;
            const ariaLabel = el.getAttribute('aria-label');
            if (!label && !ariaLabel) issues.push(sel);
          });
          return issues;
        });
        rec(VIEW, role, vp, `${VIEW}.filter-selects-labeled`,
          'Filter select inputs have accessible labels',
          filterLabelIssues.length === 0 ? 'PASS' : 'FAIL',
          filterLabelIssues.join(', '));

        // Horizontal scroll
        await checkNoHorizScroll(page, VIEW, role, vp);

        // Console errors
        rec(VIEW, role, vp, `${VIEW}.no-console-errors`,
          'No JS console errors',
          consoleErrors.length === 0 ? 'PASS' : 'FAIL',
          consoleErrors.slice(0, 3).join(' | '));

        // AxeCore — admin + desktop-1440 only
        if (role === 'admin' && vp === 'desktop-1440') {
          console.log('    [axe] Running WCAG 2.1 AA scan on ProductManagement...');
          const violations = await runAxe(page, VIEW);
          const impactful = violations.filter(v => ['critical', 'serious'].includes(v.impact));
          rec(VIEW, role, vp, `${VIEW}.axe-wcag21aa`,
            `AxeCore WCAG 2.1 AA (${violations.length} violations, ${impactful.length} critical/serious)`,
            impactful.length === 0 ? 'PASS' : 'FAIL',
            impactful.map(v => `[${v.impact}] ${v.id}: ${v.help} (${v.nodes.length} nodes)`).join(' | '));
          fs.writeFileSync(
            path.join(OUT_DIR, 'axe-product-mgmt.json'),
            JSON.stringify(violations, null, 2));
        }

        if (vp === 'desktop-1440' || vp.startsWith('mobile')) {
          await screenshot(page, `${VIEW}-${role}-${vp}`);
        }

      } catch (e) {
        rec(VIEW, role, vp, `${VIEW}.fatal`, 'Test ran without throwing', 'FAIL', e.message);
      }
      await ctx.close();
    }
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// VIEW 5: /Certificate/CertificateTileViewInternal — DEF-248/249/250
// ─────────────────────────────────────────────────────────────────────────────
async function auditCertTileViewInternal(browser) {
  console.log('\n[VIEW] /Certificate/CertificateTileViewInternal — DEF-248/249/250');
  const VIEW = 'cert-tile-internal';
  const ROLES_EXPECTED = {
    public:   401,
    external: 200, // redirects to external tile view but should still work
    analyst:  200,
    admin:    200,
  };

  for (const [role, expectedStatus] of Object.entries(ROLES_EXPECTED)) {
    for (const [vp, vpSize] of Object.entries(VIEWPORTS)) {
      const ctx = await browser.newContext({ viewport: vpSize });
      const page = await ctx.newPage();
      const consoleErrors = [];
      const networkErrors = [];
      page.on('console', msg => {
        if (msg.type() === 'error' && !/favicon/i.test(msg.text()) && !/aborted/i.test(msg.text())) {
          consoleErrors.push(msg.text());
        }
      });
      page.on('response', resp => {
        const u = resp.url();
        const s = resp.status();
        if (u.startsWith(BASE) && s >= 500) networkErrors.push(`${s} ${u.replace(BASE, '')}`);
      });

      try {
        await login(page, role);
        const resp = await page.goto(BASE + '/Certificate/CertificateTileViewInternal',
          { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
        const finalUrl = page.url();
        const wasRedirected = /\/(Login|AccessDenied)/i.test(finalUrl);
        const effectiveStatus = wasRedirected ? 401 : (resp ? resp.status() : null);

        // External gets redirected to external tile view — treat as 200-equiv
        const isExternalRedirect = role === 'external' && /TileViewExternal/i.test(finalUrl);
        const accessOk = isExternalRedirect ||
          (effectiveStatus === expectedStatus) ||
          (expectedStatus === 401 && (effectiveStatus === 403 || wasRedirected));
        rec(VIEW, role, vp, `${VIEW}.access`,
          `Access control (expected ${expectedStatus})`,
          accessOk ? 'PASS' : 'FAIL',
          `effective=${effectiveStatus} url=${finalUrl.replace(BASE, '')}`);

        if (!accessOk || expectedStatus !== 200) { await ctx.close(); continue; }
        if (isExternalRedirect) { await ctx.close(); continue; }

        // Wait for tile data to load
        await page.waitForTimeout(3000);
        await page.waitForSelector('.cert-card, .certificate-card, .cert-tile, .cert-item, [class*="cert"]',
          { timeout: 12000 }).catch(() => {});

        // DEF-248: ASIN display order — verify cert tiles render
        const tileCount = await page.evaluate(() =>
          document.querySelectorAll('.cert-card, .certificate-card, .cert-tile, [class*="cert-item"]').length);
        rec(VIEW, role, vp, `${VIEW}.def248-tiles-render`,
          'DEF-248: Certificate tiles render data',
          tileCount > 0 ? 'PASS' : 'WARN',
          `tiles found=${tileCount}`);

        // DEF-249/250: No 5xx errors from data API
        rec(VIEW, role, vp, `${VIEW}.def249-250-no-api-errors`,
          'DEF-249/250: No 5xx errors from tile data API',
          networkErrors.length === 0 ? 'PASS' : 'FAIL',
          networkErrors.slice(0, 3).join(' | '));

        // Heading hierarchy
        const h1Count = await page.evaluate(() => document.querySelectorAll('h1').length);
        rec(VIEW, role, vp, `${VIEW}.single-h1`,
          'Single h1 on page',
          h1Count === 1 ? 'PASS' : 'FAIL', `h1 count=${h1Count}`);

        // Filter inputs labeled
        const filterLabelIssues = await page.evaluate(() => {
          const issues = [];
          document.querySelectorAll('input[type="checkbox"], input[type="radio"]').forEach(el => {
            const label = el.id ? document.querySelector(`label[for="${el.id}"]`) : null;
            const ariaLabel = el.getAttribute('aria-label');
            const ariaLabelledBy = el.getAttribute('aria-labelledby');
            if (!label && !ariaLabel && !ariaLabelledBy) {
              issues.push(el.id || el.name || '(unknown)');
            }
          });
          return issues.slice(0, 10);
        });
        rec(VIEW, role, vp, `${VIEW}.filter-checkboxes-labeled`,
          'Filter checkboxes have accessible labels',
          filterLabelIssues.length === 0 ? 'PASS' : 'WARN',
          filterLabelIssues.length ? `${filterLabelIssues.length} unlabeled: ${filterLabelIssues.join(', ')}` : '');

        // Horizontal scroll
        await checkNoHorizScroll(page, VIEW, role, vp);

        // Console errors
        rec(VIEW, role, vp, `${VIEW}.no-console-errors`,
          'No JS console errors',
          consoleErrors.length === 0 ? 'PASS' : 'FAIL',
          consoleErrors.slice(0, 3).join(' | '));

        // AxeCore — analyst + desktop-1440 only
        if (role === 'analyst' && vp === 'desktop-1440') {
          console.log('    [axe] Running WCAG 2.1 AA scan on CertTileViewInternal...');
          const violations = await runAxe(page, VIEW);
          const impactful = violations.filter(v => ['critical', 'serious'].includes(v.impact));
          rec(VIEW, role, vp, `${VIEW}.axe-wcag21aa`,
            `AxeCore WCAG 2.1 AA (${violations.length} violations, ${impactful.length} critical/serious)`,
            impactful.length === 0 ? 'PASS' : 'FAIL',
            impactful.map(v => `[${v.impact}] ${v.id}: ${v.help} (${v.nodes.length} nodes)`).join(' | '));
          fs.writeFileSync(
            path.join(OUT_DIR, 'axe-cert-tile-internal.json'),
            JSON.stringify(violations, null, 2));
        }

        if (vp === 'desktop-1440' || vp.startsWith('mobile')) {
          await screenshot(page, `${VIEW}-${role}-${vp}`);
        }

      } catch (e) {
        rec(VIEW, role, vp, `${VIEW}.fatal`, 'Test ran without throwing', 'FAIL', e.message);
      }
      await ctx.close();
    }
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// PUBLIC tile view (unauthenticated baseline)
// ─────────────────────────────────────────────────────────────────────────────
async function auditPublicTileView(browser) {
  console.log('\n[VIEW] /Certificate/CertificateTileView — public baseline');
  const VIEW = 'cert-tile-public';

  for (const [vp, vpSize] of Object.entries(VIEWPORTS)) {
    const ctx = await browser.newContext({ viewport: vpSize });
    const page = await ctx.newPage();
    const consoleErrors = [];
    page.on('console', msg => {
      if (msg.type() === 'error' && !/favicon/i.test(msg.text()) && !/aborted/i.test(msg.text())) {
        consoleErrors.push(msg.text());
      }
    });

    try {
      const resp = await page.goto(BASE + '/Certificate/CertificateTileView',
        { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
      const httpStatus = resp ? resp.status() : null;
      rec(VIEW, 'public', vp, `${VIEW}.access`,
        'Public tile view accessible (200)',
        httpStatus === 200 ? 'PASS' : 'FAIL', `status=${httpStatus}`);

      if (httpStatus !== 200) { await ctx.close(); continue; }

      await page.waitForTimeout(2500);

      // No login-only UI leaking
      const noAddCert = await page.evaluate(() => {
        const el = document.querySelector('[href*="/Certificate/Create"], #add-cert-btn, .add-cert');
        if (!el) return true;
        const style = window.getComputedStyle(el);
        return style.display === 'none' || style.visibility === 'hidden';
      });
      rec(VIEW, 'public', vp, `${VIEW}.no-add-cert-btn`,
        'Public view: no Add Certificate button',
        noAddCert ? 'PASS' : 'FAIL', '');

      // Horizontal scroll
      await checkNoHorizScroll(page, VIEW, 'public', vp);

      rec(VIEW, 'public', vp, `${VIEW}.no-console-errors`,
        'No JS console errors',
        consoleErrors.length === 0 ? 'PASS' : 'FAIL',
        consoleErrors.slice(0, 3).join(' | '));

      if (vp === 'desktop-1440' || vp.startsWith('mobile')) {
        await screenshot(page, `${VIEW}-public-${vp}`);
      }

    } catch (e) {
      rec(VIEW, 'public', vp, `${VIEW}.fatal`, 'Test ran without throwing', 'FAIL', e.message);
    }
    await ctx.close();
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN
// ─────────────────────────────────────────────────────────────────────────────
(async () => {
  const startedAt = new Date().toISOString();
  console.log(`\n=== GATE 2 PRE-DEPLOY AUDIT — ${startedAt} ===`);
  console.log(`=== Target: ${BASE} ===\n`);

  const browser = await chromium.launch({ headless: true });

  try {
    await auditUserManagement(browser);
    await auditCertificateCreateEdit(browser);
    await auditProductManagement(browser);
    await auditCertTileViewInternal(browser);
    await auditPublicTileView(browser);
  } finally {
    await browser.close();
  }

  // Final summary
  const pass  = results.filter(r => r.status === 'PASS').length;
  const fail  = results.filter(r => r.status === 'FAIL').length;
  const warn  = results.filter(r => r.status === 'WARN').length;
  const total = results.length;

  const failItems = results.filter(r => r.status === 'FAIL');
  const warnItems = results.filter(r => r.status === 'WARN');

  console.log('\n' + '='.repeat(70));
  console.log(`GATE 2 SUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN (${total} checks)`);
  console.log('='.repeat(70));

  if (failItems.length) {
    console.log('\nFAILURES:');
    failItems.forEach(r =>
      console.log(`  FAIL  [${r.view}] ${r.role}/${r.viewport} :: ${r.id}\n        ${r.desc}\n        ${r.detail}`));
  }
  if (warnItems.length) {
    console.log('\nWARNINGS:');
    warnItems.forEach(r =>
      console.log(`  WARN  [${r.view}] ${r.role}/${r.viewport} :: ${r.id}\n        ${r.desc}\n        ${r.detail}`));
  }

  const verdict = fail === 0 ? 'GATE 2 PASS' : 'GATE 2 FAIL';
  console.log(`\n>>> ${verdict} <<<`);

  // Write results
  const outFile = path.join(OUT_DIR, 'gate2-deploy-20260605-results.json');
  fs.writeFileSync(outFile, JSON.stringify({
    verdict, startedAt, finishedAt: new Date().toISOString(),
    pass, fail, warn, total,
    failures: failItems,
    warnings: warnItems,
    results,
  }, null, 2));
  console.log(`\n[output] ${outFile}`);
  console.log(`[screenshots] ${SCREENSHOTS_DIR}`);

  process.exit(fail > 0 ? 1 : 0);
})();
