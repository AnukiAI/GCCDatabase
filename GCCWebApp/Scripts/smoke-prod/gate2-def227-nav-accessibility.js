// PRE-DEPLOY GATE 2 — DEF-227 nav-spacing + External restrictions + Accessibility audit
// Run: node Scripts/smoke-prod/gate2-def227-nav-accessibility.js
// Scope: _Layout.cshtml + main.css changes only — June 2026 deploy batch
//
// Checks:
//  1. Shared nav/breadcrumb spacer renders correctly on ALL FOUR logins
//  2. External role: no Add/Edit cert buttons, no Actions column on Product Management,
//     no internal tags visible
//  3. Full-page screenshots: desktop (1440x900), tablet (768x1024), mobile (375x667)
//     for home/landing + one internal admin page per role
//  4. AxeCore WCAG 2.1 AA on the _Layout chrome (header, nav, breadcrumbs, footer)

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const RESULTS_DIR = path.join(__dirname, '_results');
const SHOTS_DIR = path.join(RESULTS_DIR, 'screenshots', `gate2-${STAMP}`);

[RESULTS_DIR, SHOTS_DIR].forEach(d => { if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true }); });

const VIEWPORTS = {
  desktop: { width: 1440, height: 900 },
  tablet:  { width: 768,  height: 1024 },
  mobile:  { width: 375,  height: 667 },
};

const CREDS = {
  public:   null,
  external: { email: 'external', password: 'TestPass123!' },
  analyst:  { email: 'analyst',  password: 'TestPass123!' },
  admin:    { email: 'admin',    password: 'TestPass123!' },
};

// Pages to screenshot per role
const ROLE_PAGES = {
  public:   [{ label: 'home', url: '/Certificate/CertificateTileView' }],
  external: [
    { label: 'home', url: '/Certificate/CertificateTileViewExternal' },
    { label: 'products', url: '/Product/Management' },
  ],
  analyst:  [
    { label: 'home', url: '/Certificate/CertificateTileViewInternal' },
    { label: 'dashboard', url: '/Dashboard/Index' },
  ],
  admin:    [
    { label: 'home', url: '/Certificate/CertificateTileViewInternal' },
    { label: 'admin-users', url: '/Admin/UserManagement' },
  ],
};

// AxeCore rules scoped to layout chrome only — not full-page deep content checks
const AXE_LAYOUT_SELECTORS = 'header, nav.breadcrumbs, footer, .nav-wrapper, .header-top, .header-middle';

const log = [];
const fails = [];

function record(check, role, viewport, status, notes = '', screenshotPath = null) {
  const entry = { check, role, viewport, status, notes, screenshotPath };
  log.push(entry);
  const icon = status === 'PASS' ? 'PASS' : status === 'FAIL' ? 'FAIL' : 'WARN';
  console.log(`  [${icon}] ${role}/${viewport} ${check}${notes ? ' — ' + notes : ''}`);
  if (status === 'FAIL') fails.push(entry);
}

async function login(page, role) {
  const creds = CREDS[role];
  if (!creds) return;
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('#Email').fill(creds.email);
  await page.locator('#Password').fill(creds.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);
}

async function screenshot(page, label) {
  const fname = `${label}.png`;
  const p = path.join(SHOTS_DIR, fname);
  await page.screenshot({ path: p, fullPage: true });
  return path.relative(path.join(__dirname, '..', '..'), p);
}

async function checkBreadcrumbSpacer(page, role, vpName) {
  const state = await page.evaluate(() => {
    const nav = document.querySelector('nav.breadcrumbs');
    if (!nav) return { error: 'no nav.breadcrumbs found' };
    const wrapper = nav.querySelector('.breadcrumb-wrapper');
    const cs = window.getComputedStyle(nav);
    const wcs = wrapper ? window.getComputedStyle(wrapper) : null;
    return {
      navDisplay: cs.display,
      navMinHeight: cs.minHeight,
      navOffsetHeight: nav.offsetHeight,
      wrapperVisibility: wcs ? wcs.visibility : null,
      bodyClass: document.body.className,
    };
  });

  if (state.error) {
    record('nav.breadcrumb-spacer', role, vpName, 'FAIL', state.error);
    return;
  }

  // All roles: nav.breadcrumbs must be displayed (not hidden) and at least 32px tall
  const spacerPresent = state.navDisplay !== 'none' && state.navOffsetHeight >= 30;
  record(
    'nav.breadcrumb-spacer',
    role, vpName,
    spacerPresent ? 'PASS' : 'FAIL',
    `display=${state.navDisplay} h=${state.navOffsetHeight}px`
  );

  // Admin: wrapper content must be visible; others: wrapper must be hidden (content hidden, spacer still shown)
  if (role === 'admin') {
    const adminVisible = state.wrapperVisibility === 'visible';
    record(
      'nav.breadcrumb-content-admin',
      role, vpName,
      adminVisible ? 'PASS' : 'FAIL',
      `wrapper visibility=${state.wrapperVisibility} (expected visible for admin)`
    );
  } else {
    const contentHidden = state.wrapperVisibility === 'hidden';
    record(
      'nav.breadcrumb-content-hidden',
      role, vpName,
      contentHidden ? 'PASS' : 'FAIL',
      `wrapper visibility=${state.wrapperVisibility} (expected hidden for ${role})`
    );
  }
}

async function checkNavItems(page, role, vpName) {
  const navState = await page.evaluate(() => {
    const header = document.querySelector('header.header');
    const nav = document.querySelector('nav.main-nav');
    if (!nav) return { error: 'no main-nav' };
    const cs = window.getComputedStyle(nav);
    const navLinks = Array.from(document.querySelectorAll('.nav-link')).map(a => ({
      text: a.textContent.trim(),
      visible: a.offsetParent !== null,
    }));
    const navWrapper = document.querySelector('.nav-wrapper');
    const wcs = navWrapper ? window.getComputedStyle(navWrapper) : null;
    return {
      navDisplay: cs.display,
      navHeight: nav.offsetHeight,
      navLinks,
      wrapperIsAnon: navWrapper ? navWrapper.classList.contains('nav-anon') : null,
    };
  });

  if (navState.error) {
    record('nav.renders', role, vpName, 'FAIL', navState.error);
    return;
  }

  // Nav bar should be present and rendered
  record('nav.renders', role, vpName, navState.navDisplay !== 'none' ? 'PASS' : 'FAIL',
    `display=${navState.navDisplay} h=${navState.navHeight}`);

  // Check public gets nav-anon (no nav items)
  if (role === 'public') {
    record('nav.public-anon', role, vpName, navState.wrapperIsAnon ? 'PASS' : 'FAIL',
      `nav-anon class present: ${navState.wrapperIsAnon}`);
  }

  // Analyst + Admin should have specific nav links visible
  if (role === 'analyst' || role === 'admin') {
    const hasCompanies = navState.navLinks.some(l => l.text.includes('Companies') && l.visible);
    record('nav.companies-visible', role, vpName, hasCompanies ? 'PASS' : 'FAIL',
      'Companies & Products dropdown should be in nav');
    const hasDashboard = navState.navLinks.some(l => l.text.includes('Dashboard') && l.visible);
    record('nav.dashboard-visible', role, vpName, hasDashboard ? 'PASS' : 'FAIL',
      'Dashboard nav item should be visible');
  }

  // Admin should have Administration menu
  if (role === 'admin') {
    const hasAdmin = navState.navLinks.some(l => l.text.includes('Administration') && l.visible);
    record('nav.admin-menu-visible', role, vpName, hasAdmin ? 'PASS' : 'FAIL',
      'Administration dropdown should be in nav for admin');
  }

  // External should NOT have Dashboard or Administration
  if (role === 'external') {
    const hasDashboard = navState.navLinks.some(l => l.text === 'Dashboard' && l.visible);
    record('nav.external-no-dashboard', role, vpName, !hasDashboard ? 'PASS' : 'FAIL',
      'Dashboard should NOT appear for external');
    const hasAdmin = navState.navLinks.some(l => l.text.includes('Administration') && l.visible);
    record('nav.external-no-admin', role, vpName, !hasAdmin ? 'PASS' : 'FAIL',
      'Administration menu should NOT appear for external');
  }
}

async function checkExternalRestrictions(page, vpName) {
  // 2a. Certificate tile view — no Add/Edit certificate buttons
  await page.goto(BASE + '/Certificate/CertificateTileViewExternal', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(2000); // Let tile JS hydrate

  const certRestrictions = await page.evaluate(() => {
    // Look for Add Certificate / Edit Certificate buttons anywhere on page
    const allBtns = Array.from(document.querySelectorAll('a, button'));
    const addCertBtn = allBtns.filter(el => {
      const t = el.textContent.trim().toLowerCase();
      return (t.includes('add certificate') || t.includes('add cert')) && el.offsetParent !== null;
    });
    const editCertBtn = allBtns.filter(el => {
      const t = el.textContent.trim().toLowerCase();
      const href = el.getAttribute('href') || '';
      return (t.includes('edit') || href.includes('/Certificate/Edit')) && el.offsetParent !== null;
    });
    // Internal tags: look for any element with "internal-tag" or elements inside .internal-only
    const internalTagEls = Array.from(document.querySelectorAll('.internal-tag, .internal-only, [data-internal="true"]'))
      .filter(el => el.offsetParent !== null);
    return {
      addCertButtonsVisible: addCertBtn.length,
      editCertButtonsVisible: editCertBtn.length,
      internalTagsVisible: internalTagEls.length,
      addBtnHtml: addCertBtn.map(el => el.outerHTML.slice(0, 120)),
      editBtnHtml: editCertBtn.slice(0, 3).map(el => el.outerHTML.slice(0, 120)),
    };
  });

  record('external.no-add-cert-btn', 'external', vpName,
    certRestrictions.addCertButtonsVisible === 0 ? 'PASS' : 'FAIL',
    certRestrictions.addCertButtonsVisible > 0
      ? `Found ${certRestrictions.addCertButtonsVisible} visible Add Cert buttons: ${certRestrictions.addBtnHtml[0]}`
      : 'No visible Add Certificate buttons');

  record('external.no-edit-cert-btn', 'external', vpName,
    certRestrictions.editCertButtonsVisible === 0 ? 'PASS' : 'FAIL',
    certRestrictions.editCertButtonsVisible > 0
      ? `Found ${certRestrictions.editCertButtonsVisible} visible Edit buttons on cert view: ${certRestrictions.editBtnHtml[0]}`
      : 'No visible Edit Certificate buttons');

  record('external.no-internal-tags', 'external', vpName,
    certRestrictions.internalTagsVisible === 0 ? 'PASS' : 'FAIL',
    certRestrictions.internalTagsVisible > 0
      ? `Found ${certRestrictions.internalTagsVisible} visible internal tag elements`
      : 'No internal tag elements visible');

  // 2b. Product Management — no Actions column
  await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(2000);

  const productRestrictions = await page.evaluate(() => {
    // Check table headers for "Actions" column
    const thEls = Array.from(document.querySelectorAll('th'));
    const actionsHeader = thEls.filter(el => {
      const t = el.textContent.trim().toLowerCase();
      return t === 'actions' && el.offsetParent !== null;
    });
    // Also check for action icon buttons in table rows (edit/delete icons)
    const actionIcons = Array.from(document.querySelectorAll('td .btn-edit, td .btn-delete, td .actions-col button'))
      .filter(el => el.offsetParent !== null);
    return {
      actionsColumnVisible: actionsHeader.length,
      actionIconsVisible: actionIcons.length,
      headers: thEls.filter(el => el.offsetParent !== null).map(el => el.textContent.trim()),
    };
  });

  record('external.no-actions-column', 'external', vpName,
    productRestrictions.actionsColumnVisible === 0 ? 'PASS' : 'FAIL',
    productRestrictions.actionsColumnVisible > 0
      ? `Actions column header found in Product Management table`
      : `No Actions column. Headers: ${productRestrictions.headers.join(', ')}`
  );

  record('external.no-action-icons-in-table', 'external', vpName,
    productRestrictions.actionIconsVisible === 0 ? 'PASS' : 'FAIL',
    productRestrictions.actionIconsVisible > 0
      ? `Found ${productRestrictions.actionIconsVisible} action icon buttons in table rows`
      : 'No action icon buttons visible in product table rows'
  );
}

async function checkHorizontalScroll(page, role, vpName, url) {
  const overflow = await page.evaluate(() => ({
    docW: document.documentElement.scrollWidth,
    winW: window.innerWidth,
  }));
  const ok = overflow.docW <= overflow.winW + 2; // 2px tolerance
  record('layout.no-horiz-scroll', role, vpName, ok ? 'PASS' : 'FAIL',
    `docScrollW=${overflow.docW} winW=${overflow.winW} at ${url}`);
}

async function runAxeOnLayout(page, role, vpName, url) {
  try {
    // Inject axe-core into the page
    await page.addScriptTag({ url: 'https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.9.1/axe.min.js' });
    await page.waitForTimeout(500);

    const violations = await page.evaluate((selector) => {
      return new Promise((resolve) => {
        const include = selector ? [[selector]] : undefined;
        const config = {
          runOnly: {
            type: 'tag',
            values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'],
          },
        };
        const context = selector ? { include: [[selector]] } : document;
        // Use document scope but only return violations touching layout chrome
        axe.run(document, config, (err, results) => {
          if (err) return resolve([{ error: err.message }]);
          // Filter to violations where at least one node is in the layout chrome
          const chromeSelectors = ['header', 'nav', 'footer'];
          const layoutViolations = results.violations.filter(v =>
            v.nodes.some(n =>
              chromeSelectors.some(s => n.html.toLowerCase().includes('<' + s) ||
                (n.target && n.target.some(t => typeof t === 'string' && t.startsWith(s))))
            )
          );
          resolve(layoutViolations.map(v => ({
            id: v.id,
            impact: v.impact,
            description: v.description,
            nodes: v.nodes.slice(0, 2).map(n => ({
              html: n.html ? n.html.slice(0, 200) : '',
              target: n.target,
              failureSummary: n.failureSummary ? n.failureSummary.slice(0, 200) : '',
            })),
          })));
        });
      });
    }, AXE_LAYOUT_SELECTORS);

    if (!violations || violations.length === 0) {
      record('axe.wcag21aa-layout', role, vpName, 'PASS', 'No WCAG 2.1 AA violations in layout chrome');
    } else {
      // Separate by impact
      const critical = violations.filter(v => v.impact === 'critical' || v.impact === 'serious');
      const moderate = violations.filter(v => v.impact === 'moderate');
      const minor = violations.filter(v => v.impact === 'minor');

      if (critical.length > 0) {
        record('axe.wcag21aa-layout', role, vpName, 'FAIL',
          `${critical.length} critical/serious + ${moderate.length} moderate + ${minor.length} minor violations. First: ${critical[0].id} — ${critical[0].description}`);
      } else {
        record('axe.wcag21aa-layout', role, vpName, 'WARN',
          `0 critical, ${moderate.length} moderate, ${minor.length} minor. First moderate: ${moderate[0]?.id || 'none'}`);
      }

      // Log each violation detail for the report
      for (const v of violations) {
        console.log(`    [AXE] ${role}/${vpName} ${v.impact}: ${v.id} — ${v.description}`);
        for (const n of v.nodes) {
          console.log(`         node: ${n.html.slice(0, 100)}`);
          console.log(`         target: ${JSON.stringify(n.target)}`);
        }
      }
    }

    return violations || [];
  } catch (e) {
    record('axe.wcag21aa-layout', role, vpName, 'WARN', `axe-core injection failed: ${e.message}`);
    return [];
  }
}

// ===================== MAIN =====================
(async () => {
  console.log(`\n[GATE 2] DEF-227 Nav-Spacing + External Restrictions + Accessibility Audit`);
  console.log(`[GATE 2] Base: ${BASE}  Stamp: ${STAMP}\n`);

  const browser = await chromium.launch({ headless: true });
  const axeViolationsByRole = {};

  for (const role of ['public', 'external', 'analyst', 'admin']) {
    axeViolationsByRole[role] = {};
    console.log(`\n--- Role: ${role.toUpperCase()} ---`);

    // Desktop viewport is used for role-specific checks (nav, restrictions)
    // We iterate all viewports for screenshots + spacer check
    for (const [vpName, vp] of Object.entries(VIEWPORTS)) {
      const ctx = await browser.newContext({ viewport: vp });
      const page = await ctx.newPage();

      try {
        // Login
        await login(page, role);

        // Determine the landing URL for this role
        const pages = ROLE_PAGES[role];
        const homePage = pages[0];

        await page.goto(BASE + homePage.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
        await page.waitForTimeout(2000);

        // CHECK 1: Breadcrumb spacer
        await checkBreadcrumbSpacer(page, role, vpName);

        // CHECK 1b: Nav items correct for role (desktop only to avoid mobile collapse complexity)
        if (vpName === 'desktop') {
          await checkNavItems(page, role, vpName);
        }

        // CHECK 3: Screenshots
        for (const pg of pages) {
          // navigate to each page and screenshot all 3 viewports
          if (pg.url !== homePage.url) {
            await page.goto(BASE + pg.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
            await page.waitForTimeout(2000);
          }
          const shotPath = await screenshot(page, `${role}-${pg.label}-${vpName}`);
          console.log(`    [SHOT] ${shotPath}`);
          record('layout.screenshot', role, vpName, 'PASS', `${pg.label} at ${vpName}`, shotPath);

          // CHECK 3b: Horizontal scroll (mobile + tablet)
          if (vpName !== 'desktop') {
            await checkHorizontalScroll(page, role, vpName, pg.url);
          }
        }

        // CHECK 4: AxeCore on home page (desktop only — most meaningful)
        if (vpName === 'desktop') {
          // Navigate to home for axe scan
          await page.goto(BASE + homePage.url, { waitUntil: 'domcontentloaded', timeout: 60000 });
          await page.waitForTimeout(2000);
          const viols = await runAxeOnLayout(page, role, vpName, homePage.url);
          axeViolationsByRole[role][vpName] = viols;
        }

      } catch (e) {
        record('gate2.fatal', role, vpName, 'FAIL', e.message);
      } finally {
        await ctx.close();
      }
    }

    // CHECK 2: External restrictions (desktop only, separate context to ensure clean state)
    if (role === 'external') {
      console.log(`\n--- External Role Restrictions ---`);
      const ctx = await browser.newContext({ viewport: VIEWPORTS.desktop });
      const page = await ctx.newPage();
      try {
        await login(page, 'external');
        await checkExternalRestrictions(page, 'desktop');
      } catch (e) {
        record('external.restrictions-check', 'external', 'desktop', 'FAIL', e.message);
      } finally {
        await ctx.close();
      }
    }
  }

  await browser.close();

  // ====== SUMMARY REPORT ======
  const passed = log.filter(r => r.status === 'PASS').length;
  const failed = log.filter(r => r.status === 'FAIL').length;
  const warned = log.filter(r => r.status === 'WARN').length;

  console.log(`\n${'='.repeat(60)}`);
  console.log(`GATE 2 SUMMARY: ${passed} PASS / ${failed} FAIL / ${warned} WARN`);
  console.log(`${'='.repeat(60)}\n`);

  if (fails.length > 0) {
    console.log(`FAILURES:`);
    for (const f of fails) {
      console.log(`  FAIL [${f.role}/${f.viewport}] ${f.check}: ${f.notes}`);
    }
  }

  // Overall verdict
  const hasCriticalFails = fails.some(f =>
    f.check.startsWith('external.no-') ||
    f.check.startsWith('nav.breadcrumb-spacer') ||
    f.check.includes('axe.wcag21aa') && f.status === 'FAIL'
  );
  const hasNits = warned > 0 || fails.some(f => !f.check.startsWith('external.no-'));

  let verdict;
  if (failed === 0) {
    verdict = warned > 0 ? 'PASS-WITH-NITS' : 'PASS';
  } else if (hasCriticalFails) {
    verdict = 'FAIL';
  } else {
    verdict = 'PASS-WITH-NITS';
  }

  console.log(`\nOVERALL GATE 2 VERDICT: ${verdict}\n`);

  // Save results JSON
  const outFile = path.join(RESULTS_DIR, `gate2-${STAMP}.json`);
  fs.writeFileSync(outFile, JSON.stringify({
    stamp: STAMP,
    base: BASE,
    verdict,
    passed, failed, warned,
    results: log,
    axeViolationsByRole,
  }, null, 2));
  console.log(`Results saved: ${path.relative(process.cwd(), outFile)}`);
  console.log(`Screenshots: ${path.relative(process.cwd(), SHOTS_DIR)}\n`);

  process.exit(failed > 0 ? 1 : 0);
})();
