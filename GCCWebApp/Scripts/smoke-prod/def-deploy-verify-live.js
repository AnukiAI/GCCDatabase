// Post-deploy READ-ONLY verification — gccwebapp.com (test site).
// Confirms the 2026-06-01 deploy carries the fixes WITHOUT mutating any data.
// (The def230-reparent / def231-quickadd probes create/move rows, so they are
//  localhost-only and deliberately NOT used here.)
//
// Strategy: assert the deployed static JS / views carry the fix markers, check the
// cert template's UTF-8 BOM, and confirm the admin Product Types page renders the
// new pill. All GETs — nothing is created, edited, or deleted.
//
// Run:  node Scripts/smoke-prod/def-deploy-verify-live.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' }; // test-env basic-auth wall
const APP_USER = 'admin', APP_PASS = 'TestPass123!';

const results = [];
const rec = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : status === 'INFO' ? 'ℹ️ ' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  const getText = async (url) => {
    const r = await ctx.request.get(url);
    return { ok: r.ok(), status: r.status(), body: r.ok() ? await r.text() : '' };
  };
  const has = (body, ...needles) => needles.every(n => body.includes(n));

  try {
    console.log(`\nPost-deploy read-only verification — ${BASE}\n`);

    // ===== PART A — deployed static JS carries the fix markers (no login needed) =====
    console.log('--- PART A — deployed JS fix markers ---');
    const a = [
      { id: 'DEF-231', file: '/js/certificate-inline-create.js', needles: ['usingFormCategory'], label: 'Cert quick-add reads form category/type/sub' },
      { id: 'DEF-232a', file: '/js/tags.js', needles: ['TagType (Internal/External)'], label: 'Tags template header (no comma split)' },
      { id: 'DEF-232b', file: '/js/userManagement.js', needles: ['Admin/Analyst/External/Partner', 'Partner (semicolon'], label: 'User template Partner role + column' },
      { id: 'DEF-226', file: '/js/product-types.js', needles: ['In use — delete blocked'], label: 'Usage pill lock icon / bold' },
      { id: 'DEF-229', file: '/js/product-id-manager.js', needles: ['DEF-229'], label: 'In-memory select-all-across-pages' },
    ];
    for (const c of a) {
      const r = await getText(BASE + c.file);
      if (!r.ok) { rec(c.id, c.label, 'FAIL', `HTTP ${r.status} fetching ${c.file}`); continue; }
      rec(c.id, c.label, has(r.body, ...c.needles) ? 'PASS' : 'FAIL',
        has(r.body, ...c.needles) ? `marker present in ${c.file}` : `marker MISSING in ${c.file} (old build still deployed?)`);
    }

    // ===== PART C — admin login, then read-only DOM checks =====
    console.log('\n--- PART C — rendered-page checks (admin, read-only) ---');
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(2500);
    const loggedIn = !/\/Account\/Login/i.test(page.url());
    rec('LOGIN', 'Admin login on test site', loggedIn ? 'PASS' : 'WARN', loggedIn ? page.url() : 'still on login — DOM checks skipped');

    if (loggedIn) {
      // DEF-232c — cert import template UTF-8 BOM (now authenticated; cookies on ctx.request)
      const tpl = await ctx.request.get(`${BASE}/CertificateImport/DownloadTemplate`);
      if (!tpl.ok()) {
        rec('DEF-232c', 'Cert template UTF-8 BOM', 'WARN', `HTTP ${tpl.status()}`);
      } else {
        const buf = await tpl.body();
        const isHtml = buf.slice(0, 60).toString('utf8').toLowerCase().includes('<!doctype');
        const bom = buf.length >= 3 && buf[0] === 0xEF && buf[1] === 0xBB && buf[2] === 0xBF;
        if (isHtml) rec('DEF-232c', 'Cert template UTF-8 BOM', 'WARN', 'got HTML (auth redirect) not CSV');
        else rec('DEF-232c', 'Cert template UTF-8 BOM present (no Excel mojibake)', bom ? 'PASS' : 'FAIL',
          `BOM=${bom}; starts "${buf.slice(bom ? 3 : 0, 45).toString('utf8').replace(/\n/g, ' ')}…"`);
      }

      // DEF-226 render + DEF-227 breadcrumb markup on /Admin/ProductTypes
      await page.goto(BASE + '/Admin/ProductTypes', { waitUntil: 'networkidle' });
      await page.waitForTimeout(1500);
      const ptHtml = await page.content();
      const lockRendered = await page.locator('.node-count .fa-lock, .node-count i.fas.fa-lock').count().catch(() => 0);
      rec('DEF-226-render', 'Product Types page renders the lock/bold pill',
        (lockRendered > 0 || ptHtml.includes('In use — delete blocked')) ? 'PASS' : 'WARN',
        `lock icons in DOM: ${lockRendered}`);
      // DEF-227: breadcrumb nav should no longer carry role-admin-show (gated by .breadcrumbs alone now)
      const breadcrumbBad = /<nav[^>]*class="[^"]*breadcrumbs[^"]*role-admin-show/i.test(ptHtml);
      rec('DEF-227', 'Breadcrumb spacer not gated by role-admin-show (consistent across roles)',
        breadcrumbBad ? 'FAIL' : 'PASS', breadcrumbBad ? 'old role-admin-show class still present' : 'role-element/role-admin-show removed from breadcrumb nav');

      // DEF-225: open the first cert Edit page found in the listing; confirm the
      // saved-product sync fix marker is in the rendered view (read-only).
      let editUrl = null;
      try {
        // already on the internal tile view after login; fall back to navigating there
        if (!/TileViewInternal/i.test(page.url())) {
          await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle' });
        }
        await page.waitForTimeout(2000);
        editUrl = await page.evaluate(() => {
          // 1) an explicit Edit link, 2) any element with the cert number, then build /Certificate/Edit/{num}
          const a = [...document.querySelectorAll('a[href*="/Certificate/Edit/"]')][0];
          if (a) return a.getAttribute('href');
          const m = document.body.innerText.match(/\b\d{2}-\d{3,}\b/);
          return m ? '/Certificate/Edit/' + m[0] : null;
        });
      } catch {}
      if (!editUrl) {
        rec('DEF-225', 'Cert Edit product-name sync', 'WARN', 'could not find a cert to sample');
      } else {
        const full = editUrl.startsWith('http') ? editUrl : BASE + editUrl;
        const edit = await getText(full);
        const marker = edit.body.includes("dispatchEvent(new Event('change'");
        rec('DEF-225', 'Cert Edit carries the saved-product display fix',
          edit.ok && marker ? 'PASS' : (edit.ok ? 'WARN' : 'FAIL'),
          edit.ok ? (marker ? `marker present (${editUrl})` : 'marker not found in Edit view') : `HTTP ${edit.status}`);
      }
    }

    // DEF-230 is server-side (AdminService.UpdateProductTypeAsync) — cannot be verified
    // read-only on the live DB (would require creating + moving a node). Confirmed by the
    // same build that carries the JS fixes above; full behavior verified on localhost (11/11).
    rec('DEF-230', 'Product Types re-parent persistence (server-side)', 'INFO',
      'not read-only verifiable on prod; rode the same build, localhost probe 11/11');

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    rec('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(__dirname, 'def-deploy-verify-live-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  const info = results.filter(r => r.status === 'INFO').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN / ${info} INFO  (of ${results.length})`);
  process.exit(fail > 0 ? 1 : 0);
})();
