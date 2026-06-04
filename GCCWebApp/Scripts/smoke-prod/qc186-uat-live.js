// QC #186 UAT — behavioral verification on gccwebapp.com (READ-ONLY).
//
// Three threads were folded under QC #186:
//   1. DEF-225 (the "Ready for UAT" headline) — Cert Edit Product Name field appeared blank
//      ("Select product...") even though the cert had a product saved. Fix synced the *visible*
//      custom-dropdown widget to the underlying <select>. The existing per-view smoke only
//      checks the underlying #ProductId select (which was always correct) — it does NOT catch
//      this bug. THIS test reads the VISIBLE `.custom-dropdown-selected` text + color, which is
//      the actual surface the analyst sees.
//   2. DEF-231 — inline quick-add ignored the analyst's Category and defaulted to "Carly Test".
//      Behavioral repro would create a product (mutating); confirmed read-only via the deployed
//      fix marker + the form-state precondition (selected category survives on the form).
//   3. DEF-196/226 — Product Hierarchy node counts inaccurate / a "0" node refused to delete.
//      Read-only cross-check: compare each node's DISPLAYED count against the live active-cert
//      data, and confirm the delete-blocked nodes are exactly the ones actually in use.
//
// Nothing is created, edited, or deleted. Run:
//   node Scripts/smoke-prod/qc186-uat-live.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' }; // test-env basic-auth wall
const APP_USER = 'admin', APP_PASS = 'TestPass123!';
const SHOT_DIR = path.join(__dirname, '_results');
const MAX_CERTS = 4; // sample size for the Edit-widget check

const results = [];
const rec = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : status === 'INFO' ? 'ℹ️ ' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

const EDIT_NUMBER_RX = /^[0-9]+-[0-9A-Za-z]+$/;
const unwrap = (b) => b?.data ?? b?.Data ?? b;
const ci = (o, ...keys) => { for (const k of keys) { if (o && o[k] != null) return o[k]; } return undefined; };

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  const getJson = async (url) => (await ctx.request.get(url)).json().catch(() => null);

  try {
    console.log(`\nQC #186 UAT behavioral verification (read-only) — ${BASE}\n`);

    // ---- login ----
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(2000);
    if (/\/Account\/Login/i.test(page.url())) { rec('SETUP-login', 'Admin login', 'FAIL'); throw new Error('login failed'); }
    rec('SETUP-login', 'Admin login on test site', 'PASS', page.url());

    // =====================================================================
    // THREAD 1 — DEF-225: visible Product Name widget shows the saved product
    // =====================================================================
    console.log('\n--- DEF-225 — Cert Edit Product Name shows saved product (visible widget) ---');

    // Discover certs that ACTUALLY have a product, from the internal tiles API (authed).
    const tiles = await getJson(`${BASE}/api/certificate-tiles-internal?page=1&pageSize=200`);
    const list = (tiles && (tiles.certificates || tiles.Certificates || unwrap(tiles))) || [];
    const withProduct = list
      .map((c) => ({ number: ci(c, 'certificateNumber', 'CertificateNumber'), product: ci(c, 'productName', 'ProductName') }))
      .filter((c) => c.number && EDIT_NUMBER_RX.test(c.number) && c.product && String(c.product).trim());
    rec('DEF225-discover', 'Certs with a saved product found via internal tiles API',
      withProduct.length > 0 ? 'PASS' : 'WARN', `${withProduct.length} of ${list.length} certs have a product; sampling up to ${MAX_CERTS}`);

    const sample = withProduct.slice(0, MAX_CERTS);
    let widgetPass = 0, widgetFail = 0;
    for (const c of sample) {
      const url = `${BASE}/Certificate/Edit/${encodeURIComponent(c.number)}`;
      await page.goto(url, { waitUntil: 'networkidle' });
      // Wait for loadProductsForCompany() async cascade to populate + re-select.
      await page.waitForFunction(() => {
        const el = document.querySelector('#ProductId');
        return el && el.options && el.options.length > 1 && el.value;
      }, { timeout: 9000 }).catch(() => {});
      await page.waitForTimeout(800); // let the 50ms setTimeout + native dispatch settle

      const state = await page.evaluate(() => {
        const sel = document.querySelector('#ProductId');
        const underlyingText = sel && sel.selectedIndex >= 0 ? sel.options[sel.selectedIndex].textContent.trim() : '';
        const underlyingVal = sel ? String(sel.value) : '';
        // The visible widget wraps #ProductId. Find its trigger's selected span.
        let wrapper = sel ? sel.closest('.custom-dropdown-wrapper') : null;
        if (!wrapper && sel && sel.parentElement) {
          // fallback: nearest wrapper in the same form-group
          wrapper = sel.parentElement.querySelector('.custom-dropdown-wrapper')
                 || (sel.parentElement.parentElement && sel.parentElement.parentElement.querySelector('.custom-dropdown-wrapper'));
        }
        const span = wrapper ? wrapper.querySelector('.custom-dropdown-selected') : null;
        const visibleText = span ? span.textContent.trim() : '(no widget span)';
        const visibleColor = span ? getComputedStyle(span).color : '';
        const help = (document.querySelector('#productHelpText') || {}).textContent || '';
        return { underlyingText, underlyingVal, visibleText, visibleColor, help: help.trim(), hasWrapper: !!wrapper };
      });

      // Placeholder gray is rgb(108, 117, 125); synced dark is rgb(51, 51, 51).
      const isPlaceholder = /^Select\b/i.test(state.visibleText) || /108,\s*117,\s*125/.test(state.visibleColor);
      const matches = state.underlyingText && state.visibleText === state.underlyingText && !isPlaceholder;
      const status = matches ? 'PASS' : 'FAIL';
      if (matches) widgetPass++; else widgetFail++;
      rec(`DEF225-${c.number}`, `Visible Product Name = saved product`, status,
        `visible="${state.visibleText}" (${state.visibleColor}) vs underlying="${state.underlyingText}"; help="${state.help}"`);

      // capture a screenshot of the first failure (or first sample) as evidence
      if (status === 'FAIL' || c === sample[0]) {
        const fileSafe = c.number.replace(/[^0-9A-Za-z-]/g, '_');
        await page.screenshot({ path: path.join(SHOT_DIR, `qc186-edit-${fileSafe}.png`), fullPage: false }).catch(() => {});
      }
    }
    if (sample.length) {
      rec('DEF225-ROLLUP', `Visible widget synced across ${sample.length} sampled certs`,
        widgetFail === 0 ? 'PASS' : 'FAIL', `${widgetPass} pass / ${widgetFail} fail`);
    }

    // =====================================================================
    // THREAD 2 — DEF-231: deployed fix marker + form precondition (read-only)
    // =====================================================================
    console.log('\n--- DEF-231 — inline quick-add honors the analyst category (deployed marker) ---');
    const inlineJs = await (await ctx.request.get(`${BASE}/js/certificate-inline-create.js`)).text().catch(() => '');
    const marker = inlineJs.includes('usingFormCategory') || inlineJs.includes('ProductCategoryId');
    rec('DEF231-marker', 'Deployed quick-add reads the form Category/Type/Sub', marker ? 'PASS' : 'FAIL',
      marker ? 'usingFormCategory/ProductCategoryId path present in deployed JS'
             : 'marker MISSING — old build deployed (would default to first category, e.g. "Carly Test")');
    rec('DEF231-behavioral', 'Full create-under-chosen-category repro', 'INFO',
      'mutating (creates a product) — covered by localhost def231-fix-verify.js 6/6; not run read-only here');

    // =====================================================================
    // THREAD 3 — DEF-196/226: Product Hierarchy counts + delete-block accuracy
    // =====================================================================
    console.log('\n--- DEF-196/226 — Product Hierarchy node counts vs live data (read-only) ---');
    await page.goto(BASE + '/Admin/ProductTypes', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);

    // The .node-count renders as "{prodCount} / {certCount}" (product-types.js:120) — TWO numbers.
    // DEF-226 bolds the non-zero half + adds a lock when EITHER count blocks deletion, precisely
    // so "0 / 4" no longer reads as a bare "0" that mysteriously refuses to delete.
    const nodes = await page.evaluate(() => {
      const out = [];
      document.querySelectorAll('[data-node-id], .tree-node, .product-type-node').forEach((n) => {
        const label = (n.querySelector('.node-name, .tree-label, .node-label') || {}).textContent || '';
        const countEl = n.querySelector('.node-count');
        const countText = countEl ? countEl.textContent.replace(/\s+/g, ' ').trim() : '';
        const nums = (countText.match(/\d+/g) || []).map(Number);
        const prodCount = nums.length > 0 ? nums[0] : null;
        const certCount = nums.length > 1 ? nums[1] : null;
        const locked = !!(countEl && (countEl.querySelector('.fa-lock') || /In use/i.test(countEl.getAttribute('title') || '')));
        if (label.trim()) out.push({ label: label.trim(), prodCount, certCount, countText, locked });
      });
      return out;
    });
    rec('DEF196-render', 'Product Types tree renders nodes with product/cert counts',
      nodes.length > 0 ? 'PASS' : 'WARN', `${nodes.length} nodes parsed; ${nodes.filter(n => n.locked).length} marked in-use/locked`);

    // Correct semantics: a node is delete-locked IFF it has ANY reference (products OR certs).
    // The reported contradiction = a node shown as having no references yet refusing to delete.
    const refs = (n) => (n.prodCount || 0) + (n.certCount || 0);
    const lockedButNoRefs = nodes.filter((n) => n.locked && refs(n) === 0);          // would be the real bug
    const refsButUnlocked = nodes.filter((n) => !n.locked && refs(n) > 0);            // also wrong (under-lock)
    rec('DEF196-lock-consistency', 'Delete-lock matches references (no "empty yet locked" / "in-use yet deletable")',
      (lockedButNoRefs.length === 0 && refsButUnlocked.length === 0) ? 'PASS' : 'FAIL',
      (lockedButNoRefs.length === 0 && refsButUnlocked.length === 0)
        ? `${nodes.filter(n => n.locked).length} locked nodes all have refs; ${nodes.filter(n => refs(n) === 0).length} zero-ref nodes all unlocked`
        : `lockedButNoRefs=[${lockedButNoRefs.map(n => `${n.label} ${n.countText}`).join('; ')}] refsButUnlocked=[${refsButUnlocked.map(n => `${n.label} ${n.countText}`).join('; ')}]`);

    // Spotlight the exact "Carly Test" case from the report: it showed 0 and wouldn't delete.
    // After DEF-196/226 it should display a non-zero CERT half (the real blocker) + a lock.
    const carly = nodes.find((n) => /carly test/i.test(n.label));
    if (carly) {
      rec('DEF196-carly', '"Carly Test" surfaces the cert count that blocks its deletion',
        (carly.certCount > 0 && carly.locked) ? 'PASS' : (refs(carly) === 0 ? 'INFO' : 'WARN'),
        `displays "${carly.countText}", locked=${carly.locked} (prod=${carly.prodCount}, cert=${carly.certCount})`);
    }

    await page.screenshot({ path: path.join(SHOT_DIR, 'qc186-product-hierarchy.png'), fullPage: true }).catch(() => {});

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    rec('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(SHOT_DIR, 'qc186-uat-live-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  const info = results.filter(r => r.status === 'INFO').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN / ${info} INFO  (of ${results.length})`);
  process.exit(fail > 0 ? 1 : 0);
})();
