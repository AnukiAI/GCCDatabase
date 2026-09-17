// Gate 2 Pre-Deploy Smoke — Cross-role + Responsive Regression
//
// Verifies:
//   A. Role access on changed surfaces (DEF-294, CR-023)
//   B. Responsive: 5 viewports on cert-tile-public + cert-create (analyst)
//   C. axe-core WCAG 2.1 AA scan on cert-create (analyst) + cert-tile-public
//
// Run:  node Scripts/smoke-prod/gate2-predeploy.js
//
// Results -> Scripts/smoke-prod/_results/gate2-<stamp>.json
//           Screenshots -> Scripts/smoke-prod/_results/screenshots/gate2-*/

'use strict';

const { chromium } = require('playwright');
const fs   = require('fs');
const path = require('path');

const BASE          = process.env.BASE_URL    || 'http://localhost:5101';
const HEADLESS      = process.env.SMOKE_HEADLESS !== '0';
const NAV_TIMEOUT   = parseInt(process.env.SMOKE_NAV_TIMEOUT || '60000', 10);
const SCREENSHOTS_DIR = path.join(__dirname, '_results', 'screenshots');
const RESULTS_DIR     = path.join(__dirname, '_results');

// 5 viewports required by Gate 2.
const VIEWPORTS = [
  { name: '1440', width: 1440, height: 900 },
  { name: '1024', width: 1024, height: 768 },
  { name: '768',  width: 768,  height: 1024 },
  { name: '414',  width: 414,  height: 896 },
  { name: '375',  width: 375,  height: 667 },
];

const ROLE_CREDS = {
  public:   null,
  external: { email: 'external',  password: 'TestPass123!' },
  analyst:  { email: 'analyst',   password: 'TestPass123!' },
  admin:    { email: 'admin',     password: 'TestPass123!' },
};

// axe-core — inject from local node_modules (no network required).
const AXE_JS = fs.readFileSync(
  path.join(__dirname, 'node_modules', 'axe-core', 'axe.js'), 'utf8'
);

// ────────────────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────────────────

function ensureDirs() {
  for (const d of [RESULTS_DIR, SCREENSHOTS_DIR]) {
    if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true });
  }
}

async function login(page, role) {
  const creds = ROLE_CREDS[role];
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
  ensureDirs();
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const fname = `gate2-${label}-${stamp}.png`;
  const p = path.join(SCREENSHOTS_DIR, fname);
  try {
    await page.screenshot({ path: p, fullPage: true });
    return path.relative(path.join(__dirname, '..', '..'), p);
  } catch (e) {
    return `(screenshot failed: ${e.message})`;
  }
}

async function checkHorizontalScroll(page) {
  return page.evaluate(() => ({
    docW: document.documentElement.scrollWidth,
    winW: window.innerWidth,
  }));
}

// Run axe-core on the current page; returns array of violation objects.
async function runAxe(page) {
  await page.evaluate(AXE_JS);
  const results = await page.evaluate(async () => {
    const r = await window.axe.run(document, {
      runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa'] },
    });
    return r.violations.map((v) => ({
      id:          v.id,
      impact:      v.impact,
      description: v.description,
      helpUrl:     v.helpUrl,
      nodeCount:   v.nodes.length,
      // First affected node snippet (truncated for readability).
      sample:      v.nodes[0] ? v.nodes[0].html.slice(0, 200) : '',
    }));
  });
  return results;
}

// Determine the effective HTTP status (handles login-redirect → 401-equiv).
async function getEffectiveStatus(page, resp, testedUrl) {
  const httpStatus  = resp ? resp.status() : null;
  const finalUrl    = page.url();
  const isAuthPage  = /\/Account\/(Login|AccessDenied)/i.test(testedUrl);
  const redirectedToAuth = !isAuthPage && /\/Account\/(Login|AccessDenied)/i.test(finalUrl);
  return redirectedToAuth ? 401 : httpStatus;
}

// ────────────────────────────────────────────────────────────────────────────
// Section A — Role Access Matrix
// ────────────────────────────────────────────────────────────────────────────
//
// Surfaces & expectations:
//   /Certificate/Create         analyst=200  admin=200  external=deny  public=deny
//   /Certificate/Edit/{n}       analyst=200  admin=200  external=deny  public=deny
//   /CertificateImport          admin=200    analyst=deny  external=deny  public=deny
//   /CertificateImport/History  admin=200    analyst=deny  external=deny  public=deny

const ACCESS_MATRIX = [
  // [surface-label, url, { role: expectedStatus }]
  ['cert-create',    '/Certificate/Create',         { public: 401, external: 401, analyst: 200, admin: 200 }],
  ['cert-edit',      '/Certificate/Edit/{CERT}',    { public: 401, external: 401, analyst: 200, admin: 200 }], // {CERT} = real cert # injected at runtime; literal /Edit/1 fails the NN-XXXX route constraint and 400s (false fail)
  ['import-index',   '/CertificateImport',          { public: 401, external: 401, analyst: 401, admin: 200 }],
  ['import-history', '/CertificateImport/History',  { public: 401, external: 401, analyst: 401, admin: 200 }],
];

// Discover a real, existing cert number (NN-XXXX) so the cert-edit access check hits a
// routable URL. The literal /Certificate/Edit/1 fails the route constraint
// ^[0-9]+-[0-9A-Za-z]+$ (Program.cs) and falls through to the default route → number=null
// → BadRequest 400 (a false gate failure, not a real defect). DEF-293 gate fix, 2026-06-25.
async function discoverCertNumber(browser) {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  try {
    await login(page, 'admin');
    await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle', timeout: NAV_TIMEOUT });
    await page.waitForSelector('.certificate-card .certificate-id', { timeout: 15000 });
    return await page.evaluate(() => {
      const el = document.querySelector('.certificate-card .certificate-id');
      return el ? el.textContent.trim() : null;
    });
  } catch { return null; } finally { await ctx.close(); }
}

async function runSectionA(browser) {
  console.log('\n── Section A: Role Access Matrix ──────────────────────────────');
  const rows = []; // { surface, role, expected, actual, pass }

  const certNum = await discoverCertNumber(browser);
  console.log(`  (cert-edit uses real cert number: "${certNum || '<none found — will fall back and likely FAIL>'}")`);

  for (const [surfaceLabel, url, expectations] of ACCESS_MATRIX) {
    for (const [role, expected] of Object.entries(expectations)) {
      const ctx  = await browser.newContext({ viewport: { width: 1440, height: 900 } });
      const page = await ctx.newPage();
      try {
        await login(page, role);
        const effUrl = url.includes('{CERT}') ? url.replace('{CERT}', certNum || '1') : url;
        const resp   = await page.goto(BASE + effUrl, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
        const actual = await getEffectiveStatus(page, resp, effUrl);
        // 401 and 403 are both "denied" — treat interchangeably.
        const pass =
          actual === expected ||
          (expected === 401 && actual === 403) ||
          (expected === 403 && actual === 401);
        const icon = pass ? 'PASS' : 'FAIL';
        const finalUrl = page.url().replace(BASE, '');
        console.log(`  [${icon}] ${surfaceLabel} / ${role}: expected=${expected} actual=${actual} final=${finalUrl}`);
        rows.push({ surface: surfaceLabel, role, expected, actual, pass, finalUrl });
      } catch (e) {
        console.log(`  [FAIL] ${surfaceLabel} / ${role}: ERROR ${e.message}`);
        rows.push({ surface: surfaceLabel, role, expected, actual: 'ERROR', pass: false, error: e.message });
      } finally {
        await ctx.close();
      }
    }
  }

  const fails = rows.filter((r) => !r.pass);
  console.log(`\n  Section A: ${rows.length - fails.length} PASS / ${fails.length} FAIL\n`);
  return { rows, fails };
}

// ────────────────────────────────────────────────────────────────────────────
// Section A2 — External content-level role checks on cert tile views
// ────────────────────────────────────────────────────────────────────────────
async function runSectionA2(browser) {
  console.log('\n── Section A2: External content guards on tile views ──────────');
  const findings = [];
  const ctx  = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  try {
    await login(page, 'external');

    // Check public tile view for hidden privileged controls.
    await page.goto(BASE + '/Certificate/CertificateTileView', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
    await page.waitForTimeout(2000); // let JS render

    const addBtnPublicTile = await page.locator('.btn-add, [href*="/Certificate/Create"]').count();
    const addBtnResult = addBtnPublicTile === 0;
    findings.push({ check: 'public-tile.no-add-cert-for-external', pass: addBtnResult, notes: `add-cert elements=${addBtnPublicTile}` });
    console.log(`  [${addBtnResult ? 'PASS' : 'FAIL'}] external / public-tile: no Add Certificate button (count=${addBtnPublicTile})`);

    // Check external tile view for: no Add btn, no Actions column, no internal tags.
    await page.goto(BASE + '/Certificate/CertificateTileViewExternal', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
    await page.waitForTimeout(2000);

    const addBtnExt = await page.locator('.btn-add').count();
    findings.push({ check: 'ext-tile.no-add-cert', pass: addBtnExt === 0, notes: `btn-add=${addBtnExt}` });
    console.log(`  [${addBtnExt === 0 ? 'PASS' : 'FAIL'}] external / ext-tile: no Add Certificate button (count=${addBtnExt})`);

    // RBAC rule for external: NO Add/Edit/Delete. They MAY download/view their own certs,
    // so a download/view Actions column is allowed. (Was: forbade the whole Actions column,
    // which false-failed on the legitimate download/view column — investigated 2026-06-25,
    // see deploy-readiness-2026-06-25.md. Now check only for prohibited edit/delete controls.)
    const editDelete = await page.locator(
      '.edit-btn, .delete-btn, .action-btn.edit-btn, .action-icon.edit, ' +
      '[title="Edit Certificate"], [title="Delete Certificate"], ' +
      '[aria-label^="Edit certificate"], [aria-label^="Delete certificate"]'
    ).count();
    findings.push({ check: 'ext-tile.no-edit-delete', pass: editDelete === 0, notes: `edit/delete controls=${editDelete}` });
    console.log(`  [${editDelete === 0 ? 'PASS' : 'FAIL'}] external / ext-tile: no Edit/Delete controls (count=${editDelete})`);

    const intTag = await page.locator('[data-col="tc-inttag"], .tc-inttag, th:has-text("Internal Tag")').count();
    findings.push({ check: 'ext-tile.no-internal-tag', pass: intTag === 0, notes: `int-tag=${intTag}` });
    console.log(`  [${intTag === 0 ? 'PASS' : 'FAIL'}] external / ext-tile: no Internal Tag (count=${intTag})`);

  } catch (e) {
    findings.push({ check: 'A2.fatal', pass: false, notes: e.message });
    console.log(`  [FAIL] A2 fatal: ${e.message}`);
  } finally {
    await ctx.close();
  }

  const fails = findings.filter((f) => !f.pass);
  console.log(`\n  Section A2: ${findings.length - fails.length} PASS / ${fails.length} FAIL\n`);
  return { findings, fails };
}

// ────────────────────────────────────────────────────────────────────────────
// Section B — Responsive Screenshots (5 viewports × 2 surfaces)
// ────────────────────────────────────────────────────────────────────────────

async function runSectionB(browser) {
  console.log('\n── Section B: Responsive (5 viewports × 2 surfaces) ───────────');
  const rows = []; // { surface, viewport, pass, notes, screenshotPath }

  const surfaces = [
    // [label, url, role, waitFn]
    ['cert-tile-public', '/Certificate/CertificateTileView', 'public',
      async (page) => {
        try {
          await page.waitForFunction(
            () => document.querySelectorAll('#certificatesGrid .certificate-card').length > 0,
            { timeout: 10000 }
          );
        } catch { /* may be legitimately empty */ }
      }
    ],
    ['cert-create-analyst', '/Certificate/Create', 'analyst',
      async (page) => {
        // Wait for form inputs to be rendered (the Create form is server-rendered).
        await page.waitForSelector('form', { timeout: 10000 }).catch(() => {});
      }
    ],
  ];

  for (const [surfaceLabel, url, role, waitFn] of surfaces) {
    for (const vp of VIEWPORTS) {
      const ctx  = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
      const page = await ctx.newPage();
      try {
        await login(page, role);
        await page.goto(BASE + url, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
        if (waitFn) await waitFn(page);
        // Extra settle time for JS-driven content.
        await page.waitForTimeout(1200);

        // Horizontal-scroll check.
        const { docW, winW } = await checkHorizontalScroll(page);
        const noHScroll = docW <= winW + 2; // 2px tolerance for rounding
        const notes = `docW=${docW} winW=${winW}`;

        // Full-page screenshot — always capture for Gate 2 evidence.
        const shot = await screenshot(page, `${surfaceLabel}-${vp.name}w`);

        const pass = noHScroll;
        rows.push({ surface: surfaceLabel, viewport: vp.name, pass, notes, screenshotPath: shot });
        console.log(`  [${pass ? 'PASS' : 'FAIL'}] ${surfaceLabel} @ ${vp.name}w — ${notes} — ${shot}`);
      } catch (e) {
        rows.push({ surface: surfaceLabel, viewport: vp.name, pass: false, notes: `ERROR: ${e.message}`, screenshotPath: null });
        console.log(`  [FAIL] ${surfaceLabel} @ ${vp.name}w — ERROR: ${e.message}`);
      } finally {
        await ctx.close();
      }
    }
  }

  const fails = rows.filter((r) => !r.pass);
  console.log(`\n  Section B: ${rows.length - fails.length} PASS / ${fails.length} FAIL\n`);
  return { rows, fails };
}

// ────────────────────────────────────────────────────────────────────────────
// Section C — axe-core WCAG 2.1 AA
// ────────────────────────────────────────────────────────────────────────────

async function runSectionC(browser) {
  console.log('\n── Section C: axe-core WCAG 2.1 AA ────────────────────────────');
  const results = [];

  const targets = [
    ['cert-create',      '/Certificate/Create',               'analyst',
      async (page) => { await page.waitForSelector('form', { timeout: 10000 }).catch(() => {}); }
    ],
    ['cert-tile-public', '/Certificate/CertificateTileView',  'public',
      async (page) => {
        try {
          await page.waitForFunction(
            () => document.querySelectorAll('#certificatesGrid .certificate-card').length > 0,
            { timeout: 10000 }
          );
        } catch { /* may be legitimately empty */ }
      }
    ],
  ];

  for (const [label, url, role, waitFn] of targets) {
    const ctx  = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await ctx.newPage();
    try {
      await login(page, role);
      await page.goto(BASE + url, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
      if (waitFn) await waitFn(page);
      await page.waitForTimeout(1000);

      const violations = await runAxe(page);
      const criticals  = violations.filter((v) => v.impact === 'critical');
      const serious    = violations.filter((v) => v.impact === 'serious');
      const moderate   = violations.filter((v) => v.impact === 'moderate');
      const minor      = violations.filter((v) => v.impact === 'minor');

      console.log(`\n  [axe] ${label} — ${violations.length} violations (critical=${criticals.length} serious=${serious.length} moderate=${moderate.length} minor=${minor.length})`);
      for (const v of violations) {
        console.log(`    [${v.impact.toUpperCase()}] ${v.id}: ${v.description} (${v.nodeCount} node${v.nodeCount !== 1 ? 's' : ''})`);
        if (v.sample) console.log(`      sample: ${v.sample.slice(0, 120)}`);
      }
      results.push({ label, url, violations, criticals: criticals.length, serious: serious.length, moderate: moderate.length, minor: minor.length });
    } catch (e) {
      console.log(`  [FAIL] ${label} axe ERROR: ${e.message}`);
      results.push({ label, url, violations: [], error: e.message });
    } finally {
      await ctx.close();
    }
  }

  return results;
}

// ────────────────────────────────────────────────────────────────────────────
// Main
// ────────────────────────────────────────────────────────────────────────────

async function main() {
  ensureDirs();
  const startedAt = new Date().toISOString();
  console.log(`\n[gate2] Pre-Deploy Gate 2 — ${BASE} — ${startedAt}`);

  const browser = await chromium.launch({ headless: HEADLESS });

  const [sectionA, sectionA2, sectionB, sectionC] = await Promise.all([
    // Sections A and A2 share the browser but use independent contexts.
    // Run them sequentially to avoid login session collisions on shared test accounts.
    (async () => {
      const a  = await runSectionA(browser);
      const a2 = await runSectionA2(browser);
      return { a, a2 };
    })(),
    // Placeholder to satisfy destructuring — B and C are independent of A.
    Promise.resolve(null),
    runSectionB(browser),
    runSectionC(browser),
  ]).then(([ab, , b, c]) => [ab.a, ab.a2, b, c]);

  await browser.close();

  // ── Verdict ──────────────────────────────────────────────────────────────
  const aFails  = sectionA.fails.length;
  const a2Fails = sectionA2.fails.length;
  const bFails  = sectionB.fails.length;
  // axe critical + serious = blockers (moderate/minor = high/medium per accessibility spec).
  const cCritical = sectionC.reduce((s, r) => s + (r.criticals || 0), 0);
  const cSerious  = sectionC.reduce((s, r) => s + (r.serious   || 0), 0);

  const totalBlockers = aFails + a2Fails + bFails + cCritical + cSerious;
  const verdict = totalBlockers === 0 ? 'GATE 2 PASS' : 'GATE 2 FAIL';

  console.log('\n════════════════════════════════════════════════════════════════');
  console.log(`[gate2] ${verdict}`);
  if (totalBlockers > 0) {
    const blockerList = [];
    if (aFails  > 0) blockerList.push(`Section A: ${aFails} role-access failure(s)`);
    if (a2Fails > 0) blockerList.push(`Section A2: ${a2Fails} content-guard failure(s)`);
    if (bFails  > 0) blockerList.push(`Section B: ${bFails} responsive failure(s)`);
    if (cCritical + cSerious > 0) blockerList.push(`Section C: ${cCritical} axe-critical + ${cSerious} axe-serious violation(s)`);
    console.log('[gate2] BLOCKERS: ' + blockerList.join(' | '));
  }
  console.log('════════════════════════════════════════════════════════════════\n');

  // ── Save results ─────────────────────────────────────────────────────────
  const stamp   = startedAt.replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `gate2-${stamp}.json`);
  fs.writeFileSync(outFile, JSON.stringify({
    gate: 2, verdict, base: BASE, startedAt,
    finishedAt: new Date().toISOString(),
    sections: {
      A:  { rows: sectionA.rows,      fails: aFails  },
      A2: { findings: sectionA2.findings, fails: a2Fails },
      B:  { rows: sectionB.rows,      fails: bFails  },
      C:  sectionC,
    },
  }, null, 2));
  console.log(`[gate2] results -> ${path.relative(process.cwd(), outFile)}`);

  process.exitCode = totalBlockers === 0 ? 0 : 1;
}

main().catch((e) => {
  console.error('[gate2] FATAL:', e);
  process.exitCode = 1;
});
