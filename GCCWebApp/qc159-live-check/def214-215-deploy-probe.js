// Quick deploy probe for DEF-214 + DEF-215.
//
// DEF-214 fingerprints (in deployed wwwroot/js/userManagement.js):
//   - sanitizeImportField    (new helper for Sub-C)
//   - roleLookup             (new Map in buildImportPreview for Sub-B)
//   - cleanedFields          (Sub-C tracking array)
//
// DEF-215 fingerprints (in deployed Controllers/AccountController.cs — not directly fetchable,
// but the BEHAVIOR is testable: GET /Account/ResetPasswordWithToken with a bad token
// should return the "invalid or has expired" page; if a good `+`-containing token is
// constructed and the DB-side hash is set, the form should render. We can't set DB on
// prod, so instead just confirm the route still serves and the page renders without
// crashing — the fix is a 2-line removal, regression risk is essentially 0).
//
// No data mutation. Pure GET/file probes.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = 'https://gccwebapp.com';
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC });

  console.log(`\nDEF-214 + DEF-215 deploy probe — LIVE (${BASE})\n`);

  try {
    // ====================================================================
    // DEF-214 — fingerprint the deployed userManagement.js
    // ====================================================================
    console.log('--- DEF-214: fetch deployed userManagement.js + scan for fix fingerprints ---');
    const jsResp = await ctx.request.get(`${BASE}/js/userManagement.js`);
    if (jsResp.status() === 404) {
      // fallback path
      const altResp = await ctx.request.get(`${BASE}/wwwroot/js/userManagement.js`);
      console.log(`  ${BASE}/js/userManagement.js → 404; trying alt → ${altResp.status()}`);
    }
    console.log(`  ${BASE}/js/userManagement.js → HTTP ${jsResp.status()}`);
    if (jsResp.status() === 200) {
      const jsText = await jsResp.text();
      const hasSanitize = /sanitizeImportField\s*\(/.test(jsText);
      const hasRoleLookup = /roleLookup\s*=\s*new\s+Map/.test(jsText);
      const hasCleanedFields = /cleanedFields/.test(jsText);
      const hasErrorToSkip = /action\s*=\s*['"]skip['"]/.test(jsText);
      console.log(`  bytes=${jsText.length}`);
      console.log(`  fingerprints: sanitizeImportField=${hasSanitize}, roleLookup=${hasRoleLookup}, cleanedFields=${hasCleanedFields}, action=skip=${hasErrorToSkip}`);
      record('QC214-deploy-sanitize',
        'Sub-C: sanitizeImportField helper present in deployed JS',
        hasSanitize ? 'PASS' : 'FAIL',
        `present=${hasSanitize}`);
      record('QC214-deploy-roleLookup',
        'Sub-B: roleLookup Map present in deployed JS',
        hasRoleLookup ? 'PASS' : 'FAIL',
        `present=${hasRoleLookup}`);
      record('QC214-deploy-cleanedFields',
        'Sub-C tracking: cleanedFields array present in deployed JS',
        hasCleanedFields ? 'PASS' : 'FAIL',
        `present=${hasCleanedFields}`);
      record('QC214-deploy-action-skip',
        'Sub-D: error→skip remapping present in deployed JS',
        hasErrorToSkip ? 'PASS' : 'FAIL',
        `present=${hasErrorToSkip}`);
    } else {
      record('QC214-deploy-js-fetch',
        'Deployed userManagement.js fetchable',
        'FAIL', `HTTP ${jsResp.status()}`);
    }

    // ====================================================================
    // DEF-214 Sub-A — check deployed UserManagement.cshtml has the scoped <style> block
    //   This is server-rendered; we hit the page and grep for the @media (max-width: 768px)
    //   block + the col-rownum class added in the fix.
    // ====================================================================
    console.log('\n--- DEF-214 Sub-A: deployed UserManagement page has mobile-responsive style block ---');
    // Need admin login to view the page
    const page = await ctx.newPage();
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill('admin');
    await page.locator('#Password').fill('TestPass123!');
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
    await page.waitForTimeout(2000);
    await page.goto(BASE + '/Admin/UserManagement', { waitUntil: 'domcontentloaded', timeout: 45000 });
    await page.waitForTimeout(3000);
    const pageHtml = await page.content();
    const hasMobileMedia = /@media\s*\(max-width:\s*768px\)/.test(pageHtml);
    const hasImportModalRule = /\.import-modal-container/.test(pageHtml);
    const hasColRownum = /col-rownum/.test(pageHtml);
    console.log(`  hasMobileMedia=${hasMobileMedia}, hasImportModalRule=${hasImportModalRule}, hasColRownum=${hasColRownum}`);
    record('QC214-deploy-mobile-style',
      'Sub-A: @media (max-width: 768px) block present on deployed UserManagement page',
      hasMobileMedia && hasImportModalRule ? 'PASS' : 'FAIL',
      `media=${hasMobileMedia}, import-modal-container rule=${hasImportModalRule}`);
    record('QC214-deploy-col-rownum',
      'Sub-A: col-rownum class on Row <th> present in deployed page',
      hasColRownum ? 'PASS' : 'FAIL',
      `present=${hasColRownum}`);
    await page.close();

    // ====================================================================
    // DEF-215 — deploy probe: hit the reset endpoint with a known-invalid token
    //   that contains `+`. Should return the page WITHOUT crashing.
    //   The "invalid or has expired" message will appear (no DB-side token set),
    //   but the key check is: HTTP 200 and the page renders normally.
    //   We can't fully verify the fix without DB-side token setup, but we can
    //   confirm the route handler still responds and the page structure is intact.
    // ====================================================================
    console.log('\n--- DEF-215: deploy probe — hit ResetPasswordWithToken with `+`-containing token ---');
    const bogusUrl = `${BASE}/Account/ResetPasswordWithToken?token=${encodeURIComponent('AAAA+BBBB+CCCC=')}&email=${encodeURIComponent('nobody@e2e-test.invalid')}`;
    const resetResp = await ctx.request.get(bogusUrl);
    const resetBody = await resetResp.text();
    console.log(`  HTTP ${resetResp.status()}, body ${resetBody.length}B`);
    record('QC215-deploy-route-alive',
      'ResetPasswordWithToken route responds (200) — handler not crashed by fix',
      resetResp.status() === 200 ? 'PASS' : 'FAIL',
      `HTTP ${resetResp.status()}, body length=${resetBody.length}`);
    // The fix is a 2-line removal in the controller; we can't fingerprint that
    // from outside without source access. Best we can do live is confirm the
    // page renders and the GET handler doesn't crash on a token with `+`.
    // The full E2E test was already done on localhost (5/5 PASS).
    record('QC215-deploy-confidence',
      'Full E2E E2E verification was done on localhost (5/5 PASS); live shape probe confirms route still alive',
      'WARN', 'live verification of the fix itself requires DB-side token setup (not done on prod); localhost matrix stands');

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, 'def214-215-deploy-probe-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
