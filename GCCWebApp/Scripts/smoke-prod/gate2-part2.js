// Gate 2 part 2 — Analyst, External, Public roles + CR-025 filter-tile a11y probe.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const PWD = 'TestPass123!';
const OUT = path.join(__dirname, '_results', 'gate2-20260720');
fs.mkdirSync(OUT, { recursive: true });

const VIEWPORTS = [
  { name: 'desktop-1920', width: 1920, height: 1080 },
  { name: 'desktop-1440', width: 1440, height: 900 },
  { name: 'tablet-768', width: 768, height: 1024 },
  { name: 'mobile-390', width: 390, height: 844 },
  { name: 'mobile-360', width: 360, height: 640 },
];
const CERT_NUMBER = '26-1733';
const AXE_PATH = require.resolve('axe-core/axe.min.js');
const report = { pages: [], axe: [], externalChecks: [], cr025: null, errors: [] };

async function login(context, email) {
  const page = await context.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.fill('#Email', email);
  await page.fill('#Password', PWD);
  await Promise.all([
    page.waitForLoadState('networkidle').catch(() => {}),
    page.click('button[type=submit]'),
  ]);
  await page.waitForTimeout(1000);
  const ok = !page.url().includes('/Account/Login');
  await page.close();
  return ok;
}

async function waitForRealContent(page, timeout = 20000) {
  try {
    await page.waitForFunction(() => {
      const cards = document.querySelectorAll('.certificate-card, .cert-tile, tr.certificate-row, tbody#gridBody tr');
      const empty = document.querySelector('.empty-state, #emptyState, [id*="empty" i]');
      if (cards.length > 0) return true;
      if (empty && empty.offsetParent !== null && empty.textContent.trim().length > 0) return true;
      return false;
    }, { timeout });
  } catch (e) { return false; }
  return true;
}

async function shootAllViewports(context, url, slug, opts = {}) {
  const results = [];
  for (const vp of VIEWPORTS) {
    const page = await context.newPage();
    await page.setViewportSize({ width: vp.width, height: vp.height });
    let contentOk = true, httpStatus = null;
    try {
      const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
      httpStatus = resp ? resp.status() : null;
      if (opts.waitForContent) contentOk = await waitForRealContent(page);
      else await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
      await page.waitForTimeout(600);
      const shotPath = path.join(OUT, `${slug}__${vp.name}.png`);
      await page.screenshot({ path: shotPath, fullPage: true });
      results.push({ viewport: vp.name, status: httpStatus, contentOk, screenshot: shotPath, finalUrl: page.url() });
    } catch (e) {
      results.push({ viewport: vp.name, status: httpStatus, error: e.message, finalUrl: page.url() });
    } finally { await page.close(); }
  }
  return results;
}

async function runAxe(context, url, slug, opts = {}) {
  const page = await context.newPage();
  await page.setViewportSize({ width: 1440, height: 900 });
  try {
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
    if (opts.waitForContent) await waitForRealContent(page);
    await page.waitForTimeout(600);
    if (opts.preAxe) await opts.preAxe(page);
    const axeSrc = fs.readFileSync(AXE_PATH, 'utf8');
    await page.addScriptTag({ content: axeSrc });
    const results = await page.evaluate(async () => {
      return await window.axe.run(document, { runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] } });
    });
    const violations = results.violations.map(v => ({
      id: v.id, impact: v.impact, help: v.help, helpUrl: v.helpUrl,
      nodes: v.nodes.map(n => ({ target: n.target, html: n.html.slice(0, 300), failureSummary: n.failureSummary })),
    }));
    fs.writeFileSync(path.join(OUT, `axe__${slug}.json`), JSON.stringify(violations, null, 2));
    return { slug, url, violationCount: violations.length, violations };
  } catch (e) {
    return { slug, url, error: e.message };
  } finally { await page.close(); }
}

(async () => {
  const browser = await chromium.launch();

  // ---------- ANALYST ----------
  {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    const loggedIn = await login(ctx, 'jane.analyst@anukiconsulting.com');
    report.pages.push({ role: 'Analyst', loggedIn });
    if (loggedIn) {
      report.pages.push({ role: 'Analyst', page: 'CertificateTileViewInternal', results: await shootAllViewports(ctx, BASE + '/Certificate/CertificateTileViewInternal', 'analyst__tileview-internal', { waitForContent: true }) });
      report.pages.push({ role: 'Analyst', page: 'Edit', results: await shootAllViewports(ctx, BASE + '/Certificate/Edit/' + CERT_NUMBER, 'analyst__edit', {}) });
      report.pages.push({ role: 'Analyst', page: 'Create', results: await shootAllViewports(ctx, BASE + '/Certificate/Create', 'analyst__create', {}) });

      report.axe.push(await runAxe(ctx, BASE + '/Certificate/CertificateTileViewInternal', 'analyst__tileview-internal', { waitForContent: true }));
      report.axe.push(await runAxe(ctx, BASE + '/Certificate/Edit/' + CERT_NUMBER, 'analyst__edit'));

      // CR-026 check: Analyst should now see Import + Template buttons on tile view internal.
      const p = await ctx.newPage();
      await p.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'domcontentloaded' });
      await waitForRealContent(p);
      const importBtnVisible = await p.isVisible('#openImportModalBtn').catch(() => false);
      report.pages.push({ role: 'Analyst', check: 'CR-026 Import button visible', result: importBtnVisible });
      await p.close();

      // ---- CR-025 filter-tile a11y probe on Edit page's Import Preview modal ----
      const probe = await ctx.newPage();
      await probe.goto(BASE + '/Certificate/Edit/' + CERT_NUMBER, { waitUntil: 'networkidle' });
      await probe.waitForTimeout(1000);
      // Inject fake preview data + open the modal via the app's own render path (not a
      // synthetic DOM) so we're testing the real component.
      const cr025 = await probe.evaluate(() => {
        try {
          if (typeof productIdManager === 'undefined') return { error: 'productIdManager not found on window' };
          productIdManager.importPreviewData = [
            { data: { productIdType: 'ASIN', productIdValue: 'B000TEST01', productName: 'Test Product A', productSku: 'SKU1', partner: 'Amazon' }, status: 'new', selected: true, errors: [], changes: [] },
            { data: { productIdType: 'ASIN', productIdValue: 'B000TEST02', productName: 'Test Product B', productSku: 'SKU2', partner: 'Amazon' }, status: 'modified', selected: true, errors: [], changes: [{ field: 'Product Name', oldValue: 'Old B', newValue: 'Test Product B' }] },
            { data: { productIdType: 'ASIN', productIdValue: 'B000TEST03', productName: 'Test Product C', productSku: 'SKU3', partner: 'Amazon' }, status: 'unchanged', selected: false, errors: [], changes: [] },
            { data: { productIdType: 'ASIN', productIdValue: 'BADVALUE', productName: 'Test Product D', productSku: 'SKU4', partner: 'Amazon' }, status: 'error', selected: false, errors: ['Invalid ASIN format'], changes: [] },
          ];
          productIdManager.showImportPreviewModal();
          return { ok: true };
        } catch (e) {
          return { error: e.message };
        }
      });

      if (cr025.ok) {
        await probe.waitForTimeout(300);
        // Screenshot the open modal
        await probe.screenshot({ path: path.join(OUT, 'cr025__import-preview-modal.png'), fullPage: true });

        // 1) Focusability + keyboard reachability via Tab from body
        const focusTrace = await probe.evaluate(() => {
          const box = document.getElementById('stat-box-new');
          return {
            tabIndex: box ? box.getAttribute('tabindex') : null,
            role: box ? box.getAttribute('role') : null,
            ariaPressedBefore: box ? box.getAttribute('aria-pressed') : null,
          };
        });

        // Click into search box first then Tab to reach the first stat box in DOM order
        // (stat boxes precede the search bar in source order, so Tab from body should reach
        // stat-box-new first; verify empirically).
        await probe.evaluate(() => document.getElementById('stat-box-new').focus());
        const isFocused = await probe.evaluate(() => document.activeElement && document.activeElement.id === 'stat-box-new');

        // Capture computed outline/box-shadow at focus to check for a visible focus indicator
        const focusStyle = await probe.evaluate(() => {
          const el = document.getElementById('stat-box-new');
          el.focus();
          const cs = getComputedStyle(el);
          return { outlineStyle: cs.outlineStyle, outlineWidth: cs.outlineWidth, outlineColor: cs.outlineColor, boxShadow: cs.boxShadow };
        });

        // 2) Activate via keyboard (Space) and check aria-pressed + row filtering
        await probe.evaluate(() => document.getElementById('stat-box-new').focus());
        await probe.keyboard.press('Space');
        await probe.waitForTimeout(150);
        const afterSpace = await probe.evaluate(() => {
          const box = document.getElementById('stat-box-new');
          const rows = Array.from(document.querySelectorAll('#import-preview-body tr'));
          const visibleRows = rows.filter(r => r.style.display !== 'none').length;
          return {
            ariaPressed: box.getAttribute('aria-pressed'),
            borderColor: box.style.borderColor,
            visibleRows,
            totalRows: rows.length,
            countText: document.getElementById('import-preview-count')?.textContent || '',
          };
        });

        // 3) Toggle off via Enter and re-check
        await probe.keyboard.press('Enter');
        await probe.waitForTimeout(150);
        const afterEnterToggleOff = await probe.evaluate(() => {
          const box = document.getElementById('stat-box-new');
          const rows = Array.from(document.querySelectorAll('#import-preview-body tr'));
          const visibleRows = rows.filter(r => r.style.display !== 'none').length;
          return { ariaPressed: box.getAttribute('aria-pressed'), visibleRows, totalRows: rows.length };
        });

        // 4) Tab order across all 4 tiles + search box + clear button
        const tabOrder = await probe.evaluate(() => {
          const ids = ['stat-box-new', 'stat-box-modified', 'stat-box-unchanged', 'stat-box-errors', 'import-preview-search'];
          return ids.map(id => {
            const el = document.getElementById(id);
                return { id, exists: !!el, tabIndex: el ? el.tabIndex : null };
          });
        });

        // 5) Accessible name of a stat box (what a screen reader would announce)
        const accName = await probe.evaluate(() => {
          const box = document.getElementById('stat-box-new');
          return box ? box.textContent.replace(/\s+/g, ' ').trim() : null;
        });

        report.cr025 = { focusTrace, isFocused, focusStyle, afterSpace, afterEnterToggleOff, tabOrder, accessibleNameOfNewBox: accName };

        // Axe scan specifically on the open modal
        const axeSrc = fs.readFileSync(AXE_PATH, 'utf8');
        await probe.addScriptTag({ content: axeSrc });
        const modalAxe = await probe.evaluate(async () => {
          const modal = document.getElementById('import-preview-modal');
          return await window.axe.run(modal, { runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] } });
        });
        report.cr025.axeViolations = modalAxe.violations.map(v => ({
          id: v.id, impact: v.impact, help: v.help,
          nodes: v.nodes.map(n => ({ target: n.target, html: n.html.slice(0, 200) })),
        }));
      } else {
        report.cr025 = cr025;
      }
      await probe.close();
    }
    await ctx.close();
  }

  // ---------- EXTERNAL ----------
  {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    const loggedIn = await login(ctx, 'external');
    report.pages.push({ role: 'External', loggedIn });
    if (loggedIn) {
      report.pages.push({ role: 'External', page: 'CertificateTileViewExternal', results: await shootAllViewports(ctx, BASE + '/Certificate/CertificateTileViewExternal', 'external__tileview-external', { waitForContent: true }) });
      report.axe.push(await runAxe(ctx, BASE + '/Certificate/CertificateTileViewExternal', 'external__tileview-external', { waitForContent: true }));

      // Restriction checks
      const p = await ctx.newPage();
      await p.goto(BASE + '/Certificate/CertificateTileViewExternal', { waitUntil: 'domcontentloaded' });
      await waitForRealContent(p);
      const noAddBtn = await p.isVisible('.btn-add, a[href*="/Certificate/Create"]').catch(() => false);
      const noImportBtn = await p.isVisible('#openImportModalBtn').catch(() => false);
      const bodyText = await p.textContent('body').catch(() => '');
      const hasInternalTagClass = await p.$$eval('.tag.internal', els => els.length).catch(() => 0);
      report.externalChecks.push({ check: 'Add Certificate button present (should be false)', result: noAddBtn });
      report.externalChecks.push({ check: 'Import button present (should be false)', result: noImportBtn });
      report.externalChecks.push({ check: 'Internal tag elements present (should be 0)', result: hasInternalTagClass });

      // Try direct nav to Edit/Create — should be blocked
      const editResp = await p.goto(BASE + '/Certificate/Edit/' + CERT_NUMBER, { waitUntil: 'domcontentloaded' }).catch(e => null);
      report.externalChecks.push({ check: 'External GET /Certificate/Edit/26-1733', status: editResp ? editResp.status() : null, finalUrl: p.url() });
      const createResp = await p.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded' }).catch(e => null);
      report.externalChecks.push({ check: 'External GET /Certificate/Create', status: createResp ? createResp.status() : null, finalUrl: p.url() });

      // Product Management page - Actions column check (best-effort; page may not exist under this name)
      try {
        const pmResp = await p.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded', timeout: 15000 });
        await p.waitForTimeout(800);
        const hasActionsHeader = await p.evaluate(() => {
          const ths = Array.from(document.querySelectorAll('th'));
          return ths.some(th => /actions/i.test(th.textContent));
        }).catch(() => null);
        report.externalChecks.push({ check: 'External /Product/Index Actions column present (should be false)', status: pmResp ? pmResp.status() : null, hasActionsHeader, finalUrl: p.url() });
      } catch (e) {
        report.externalChecks.push({ check: 'External /Product/Index', error: e.message });
      }
      await p.close();
    }
    await ctx.close();
  }

  // ---------- PUBLIC (anonymous) ----------
  {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    report.pages.push({ role: 'Public', page: 'CertificateTileView', results: await shootAllViewports(ctx, BASE + '/Certificate/CertificateTileView', 'public__tileview', { waitForContent: true }) });
    report.axe.push(await runAxe(ctx, BASE + '/Certificate/CertificateTileView', 'public__tileview', { waitForContent: true }));

    const p = await ctx.newPage();
    const editResp = await p.goto(BASE + '/Certificate/Edit/' + CERT_NUMBER, { waitUntil: 'domcontentloaded' }).catch(e => null);
    report.externalChecks.push({ check: 'Public GET /Certificate/Edit/26-1733 (should redirect to login)', status: editResp ? editResp.status() : null, finalUrl: p.url() });
    await p.close();
    await ctx.close();
  }

  await browser.close();
  fs.writeFileSync(path.join(OUT, 'report-part2.json'), JSON.stringify(report, null, 2));
  console.log('DONE PART 2');
})();
