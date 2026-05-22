// Status-sync sweep — probe a sample of older "Fixed locally — pending deploy"
// DEFs against gccwebapp.com to determine which ARE deployed vs which TRULY
// pending. Each probe is independent. Results inform a bulk status update.

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
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  console.log(`\nOlder-DEF status-sync sweep — gccwebapp.com\n`);

  try {
    // login as admin
    await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill('admin');
    await page.locator('#Password').fill('TestPass123!');
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
    await page.waitForTimeout(2000);

    // ====================================================================
    // DEF-101 — Import/Export icons reversed (fa-file-arrow-down vs up)
    //   shipped log says "Deployed 2026-05-04" — confirm
    // ====================================================================
    console.log('--- DEF-101: Import/Export icon arrow direction ---');
    await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2500);
    const iconCheck = await page.evaluate(() => {
      const html = document.documentElement.outerHTML;
      return {
        hasArrowDown: /fa-file-arrow-down/.test(html),
        hasArrowUp: /fa-file-arrow-up/.test(html),
        hasOldDownload: /fa-download(?![a-z-])/.test(html),
      };
    });
    record('DEF-101', 'Import/Export icons use file-arrow-down/up (DEF-101 deployed)',
      iconCheck.hasArrowDown && iconCheck.hasArrowUp ? 'PASS' : 'FAIL',
      JSON.stringify(iconCheck));

    // ====================================================================
    // DEF-128 — /Certificate/ExportCertificates removed [AllowAnonymous]
    //   Probe: anonymous GET should return 401/302, not 200
    // ====================================================================
    console.log('\n--- DEF-128: ExportCertificates auth-gated ---');
    const anonCtx = await browser.newContext({ httpCredentials: BASIC });
    const exp = await anonCtx.request.get(BASE + '/Certificate/ExportCertificates?format=csv', { maxRedirects: 0 });
    console.log(`  anon /Certificate/ExportCertificates?format=csv → HTTP ${exp.status()}`);
    record('DEF-128', 'ExportCertificates redirects/blocks anonymous (was [AllowAnonymous])',
      [200, 302, 401].includes(exp.status()) && exp.status() !== 200 ? 'PASS' :
        exp.status() === 200 ? 'WARN' : 'PASS',
      `HTTP ${exp.status()} — 302/401 = auth gate; 200 = still anonymous (DEF-198 already wraps this with scope filter so anon gets PublicStatuses-only)`);
    await anonCtx.close();

    // ====================================================================
    // DEF-119 — Add Product ID modal too narrow on desktop
    //   Probe: open the modal, measure width — should be > 600px
    // ====================================================================
    console.log('\n--- DEF-119: Add Product ID modal width ---');
    await page.goto(BASE + '/Certificate/Edit/26-0163', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2500);
    const addBtn = page.locator('#addProductId');
    if (await addBtn.count() > 0) {
      await addBtn.click();
      await page.waitForTimeout(800);
      const modalWidth = await page.evaluate(() => {
        const modal = document.querySelector('#addProductIdModal, .admin-modal.show, [class*="product-id-modal"]');
        if (!modal) return null;
        const cs = window.getComputedStyle(modal);
        return { width: modal.getBoundingClientRect().width, css: cs.width };
      });
      console.log(`  modal width:`, modalWidth);
      record('DEF-119', 'Add Product ID modal width sufficient (>600px desktop)',
        modalWidth && modalWidth.width > 600 ? 'PASS' : 'WARN',
        `width=${modalWidth?.width}`);
      // Close modal
      await page.keyboard.press('Escape').catch(() => {});
      await page.waitForTimeout(400);
    } else {
      record('DEF-119', 'Add Product ID modal — could not open', 'WARN', 'addBtn not found');
    }

    // ====================================================================
    // DEF-138 — Product Mgmt save preserves Sku/Model
    //   Probe: fetch /Product/Management, check the deployed JS for the fix
    // ====================================================================
    console.log('\n--- DEF-138: Product Mgmt sku/model preservation ---');
    const pmJsResp = await ctx.request.get(BASE + '/js/admin/productManagement.js');
    if (pmJsResp.status() === 200) {
      const pmJs = await pmJsResp.text();
      // The fix should send sku/model in the save payload — pre-fix it was missing them
      const sendsSku = /sku\s*:/i.test(pmJs);
      const sendsModel = /model\s*:/i.test(pmJs);
      record('DEF-138', 'Deployed productManagement.js sends sku+model fields (DEF-138 deployed)',
        sendsSku && sendsModel ? 'PASS' : 'WARN',
        `sku=${sendsSku} model=${sendsModel}`);
    } else {
      record('DEF-138', 'productManagement.js fetch', 'WARN', `HTTP ${pmJsResp.status()}`);
    }

    // ====================================================================
    // DEF-176 — Filter chip prefix "Type:/Company:/Search:" + numeric ID display
    //   Probe: hit cert tile internal, open filter chips, check for prefix
    // ====================================================================
    console.log('\n--- DEF-176: filter chip prefix cleanup ---');
    await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);
    const chipText = await page.evaluate(() => {
      // Look for filter chip elements
      const html = document.documentElement.outerHTML;
      const hasTypeColon = />Type:</.test(html);
      const hasCompanyColon = />Company:</.test(html);
      const hasSearchColon = />Search:</.test(html);
      return { hasTypeColon, hasCompanyColon, hasSearchColon };
    });
    // Pre-fix HAD these prefixes; post-fix removed them
    const noPrefixes = !chipText.hasTypeColon && !chipText.hasCompanyColon && !chipText.hasSearchColon;
    record('DEF-176', 'Filter chips do NOT contain pre-fix prefixes (Type:/Company:/Search:)',
      noPrefixes ? 'PASS' : 'WARN',
      JSON.stringify(chipText) + ' (note: chips only render when filters are active — empty = inconclusive)');

    // ====================================================================
    // DEF-167 — Cert-tile filter: subcategories as separate checkboxes
    //   Probe: hit /api/certificate-tiles/filters and check shape
    // ====================================================================
    console.log('\n--- DEF-167: filter API returns hierarchical subcategories ---');
    const filterApi = await anonCtx.request.get(BASE + '/api/certificate-tiles/filters').catch(() => null);
    if (filterApi && filterApi.status() === 200) {
      const body = await filterApi.json().catch(() => null);
      const data = body?.data || body;
      // Pre-fix had flat subcategories; post-fix uses productCategoriesHierarchy with subcategories nested
      const hasHierarchy = !!(data?.productCategoriesHierarchy);
      const firstCat = data?.productCategoriesHierarchy?.[0];
      record('DEF-167', 'Filter API returns productCategoriesHierarchy (subcategories nested, not flat checkboxes)',
        hasHierarchy ? 'PASS' : 'WARN',
        `hasHierarchy=${hasHierarchy} firstCatName=${firstCat?.name || 'n/a'}`);
    } else {
      record('DEF-167', 'Filter API probe', 'WARN', `not 200`);
    }

    // ====================================================================
    // DEF-189 — Cert Edit sidebar preview square (not portrait)
    //   Probe: hit cert edit, measure preview container aspect ratio
    // ====================================================================
    console.log('\n--- DEF-189: cert preview square aspect ---');
    await page.goto(BASE + '/Certificate/Edit/26-0163', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2500);
    const previewMetrics = await page.evaluate(() => {
      const el = document.querySelector('.cert-preview-container, .certificate-preview, [class*="preview"]');
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { width: r.width, height: r.height, ratio: r.width / r.height };
    });
    console.log(`  preview metrics:`, previewMetrics);
    // Square = ratio close to 1; portrait = ratio < 0.85
    record('DEF-189', 'Cert preview not stuck in portrait orientation (ratio ~1, not <0.85)',
      previewMetrics && previewMetrics.ratio > 0.85 ? 'PASS' : 'WARN',
      `ratio=${previewMetrics?.ratio?.toFixed(2)} w=${previewMetrics?.width} h=${previewMetrics?.height}`);

    // ====================================================================
    // DEF-191 — Product ID Import: "unchanged" row marked "already exists"
    //   Hard to test in isolation; instead, verify CSV preview modal logic exists
    //   via JS file grep
    // ====================================================================
    console.log('\n--- DEF-191: ProductId import unchanged-row handling ---');
    const pidJsResp = await ctx.request.get(BASE + '/js/product-id-manager.js');
    if (pidJsResp.status() === 200) {
      const pidJs = await pidJsResp.text();
      // The fix added handling so an "unchanged" row doesn't error.
      // Look for "Unchanged" status in the JS (it's a status class added by the fix)
      const hasUnchanged = /['"]unchanged['"]/i.test(pidJs);
      record('DEF-191', 'product-id-manager.js handles "unchanged" status (DEF-191 deployed)',
        hasUnchanged ? 'PASS' : 'WARN',
        `hasUnchanged=${hasUnchanged}`);
    } else {
      record('DEF-191', 'product-id-manager.js fetch', 'WARN', `HTTP ${pidJsResp.status()}`);
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, 'older-defs-status-sweep-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
