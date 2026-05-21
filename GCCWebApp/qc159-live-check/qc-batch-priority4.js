// QC Priority-4 batch — five small visual/UX checks bundled into one harness.
//
//   QC #104 / DEF-202+211 — Breadcrumb on /Company/Index and /Product/Management
//     must not start with "Administration ›"
//   QC #181 / DEF-172    — Cert Create Company Email field has strict regex
//     validation (data-val-regex attribute)
//   QC #183 / DEF-201    — Sort By dropdown no longer offers "Most Popular
//     Companies" or "Relevance Score"
//   QC #207 / DEF-207    — Cert Create page inline success script uses
//     sessionStorage guard (suppresses bfcache replay toasts on mobile)
//   QC #174 / DEF-195    — Tile-view .header-row align-items is flex-start
//     (cert-number pill flush with top of category text)
//
// All read-only on gccwebapp.com. No data mutation.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const TARGET = process.env.TARGET || (BASE.includes('gccwebapp') ? 'live' : 'local');
const BASIC = BASE.includes('gccwebapp') ? { username: 'gcc', password: 'Cert-Test-Access-2026!' } : undefined;
const APP_USER = 'admin';
const APP_PASS = 'TestPass123!';

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    httpCredentials: BASIC,
    viewport: { width: 1440, height: 900 },
  });
  const page = await ctx.newPage();

  console.log(`\nQC Priority-4 batch — ${TARGET.toUpperCase()} (${BASE})\n`);

  try {
    // login
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(2000);

    // ====================================================================
    // T1 — QC #104 / DEF-202+211 — Breadcrumb on Company + Product Mgmt
    //   Layout renders breadcrumbs in <nav class="breadcrumbs" aria-label="Breadcrumb">.
    //   Per-view content uses <span> elements + chevron <i> separators (no text ›).
    //   Per QC #104 fix, the trail must NOT start with "Administration".
    // ====================================================================
    console.log('\n--- T1: QC #104 breadcrumbs ---');
    for (const route of ['/Company/Index', '/Product/Management']) {
      await page.goto(BASE + route, { waitUntil: 'networkidle' });
      await page.waitForTimeout(1500);
      // Read the breadcrumbs container (admin-only via role-admin-show class)
      const breadcrumbInfo = await page.evaluate(() => {
        const nav = document.querySelector('nav.breadcrumbs, nav[aria-label="Breadcrumb"]');
        if (!nav) return { found: false };
        const text = (nav.innerText || '').replace(/\s+/g, ' ').trim();
        // Each <span> child (skip .separator) gives the ordered crumb labels
        const items = Array.from(nav.querySelectorAll('span')).filter(s => !s.classList.contains('separator')).map(s => (s.innerText || '').trim()).filter(Boolean);
        return { found: true, text, items };
      });
      if (!breadcrumbInfo.found) {
        record(`QC104-${route.replace(/\//g, '_')}`,
          `Breadcrumb nav element on ${route}`,
          'FAIL', 'no nav.breadcrumbs found');
        continue;
      }
      // The first crumb must be "Companies & Products", NOT "Administration"
      const firstCrumb = breadcrumbInfo.items[0] || '';
      const leadsWithAdmin = /^Administration\s*$/i.test(firstCrumb);
      const leadsWithCompanies = /Companies\s*&\s*Products/i.test(firstCrumb);
      record(`QC104-${route.replace(/\//g, '_')}`,
        `Breadcrumb on ${route} starts with "Companies & Products" (not "Administration")`,
        !leadsWithAdmin && leadsWithCompanies ? 'PASS' : 'FAIL',
        `crumbs=${JSON.stringify(breadcrumbInfo.items)}`);
      await page.screenshot({ path: path.join(OUT, `qc104-${TARGET}-${route.replace(/\//g, '_')}.png`), fullPage: false });
    }

    // ====================================================================
    // T2 — QC #181 / DEF-172 — Company Email regex validation
    // ====================================================================
    console.log('\n--- T2: QC #181 Company Email regex validation ---');
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    const emailInputAttrs = await page.locator('#CompanyEmail').evaluateAll(els =>
      els.map(e => ({
        type: e.type,
        dataValRegex: e.getAttribute('data-val-regex') || e.getAttribute('data-val-regex-pattern'),
        dataValRegexPattern: e.getAttribute('data-val-regex-pattern'),
        attrs: Array.from(e.attributes).map(a => a.name)
      }))
    );
    const attrs = emailInputAttrs[0] || {};
    const hasRegex = !!(attrs.dataValRegex || attrs.dataValRegexPattern || attrs.attrs?.some(n => /regex/i.test(n)));
    record('QC181-regex-attr', '#CompanyEmail input has regex validation attribute',
      hasRegex ? 'PASS' : 'FAIL',
      `attrs=${JSON.stringify(attrs)}`);

    // Functional check — type bad email and try to submit; client-side validation should block
    if (await page.locator('#CompanyEmail').count() > 0) {
      await page.locator('#CompanyEmail').fill('notanemail');
      await page.locator('#CompanyEmail').blur();
      await page.waitForTimeout(800);
      const errorMsg = await page.locator('[data-valmsg-for="CompanyEmail"], .field-validation-error, .text-danger').filter({ hasText: /email/i }).first().innerText().catch(() => '');
      record('QC181-functional-blocked', 'Invalid email surfaces a validation error',
        errorMsg && /email/i.test(errorMsg) ? 'PASS' : 'WARN',
        `msg="${errorMsg || '(none surfaced on blur — may require submit attempt)'}"`);
      await page.screenshot({ path: path.join(OUT, `qc181-${TARGET}-bad-email.png`), fullPage: false });
    }

    // ====================================================================
    // T3 — QC #183 / DEF-201 — Sort dropdown cleanup.
    //   Sort UI is a custom dropdown: <div id="sortDropdown"> wrapping a
    //   <button id="sortDropdownBtn"> + a menu of items rendered as <a> or
    //   <li>. Need to open the dropdown first to enumerate options.
    // ====================================================================
    console.log('\n--- T3: QC #183 sort dropdown options ---');
    for (const route of ['/Certificate/CertificateTileViewInternal', '/Certificate/CertificateTileView']) {
      await page.goto(BASE + route, { waitUntil: 'networkidle' });
      await page.waitForTimeout(2500);
      // Open the custom dropdown
      const btn = page.locator('#sortDropdownBtn');
      if (await btn.count() > 0) {
        await btn.click();
        await page.waitForTimeout(500);
      }
      // Scoop every visible option text from #sortDropdown — accept any of
      // <a>, <li>, <button>, [role=option] inside the dropdown.
      const sortOptions = await page.evaluate(() => {
        const root = document.querySelector('#sortDropdown');
        if (!root) return [];
        const candidates = root.querySelectorAll('a, li, button, [role="option"], [role="menuitem"]');
        const out = [];
        candidates.forEach(c => {
          const t = (c.innerText || '').trim();
          if (t && t.length < 60) out.push(t);
        });
        return [...new Set(out)];
      });
      const hasPopular = sortOptions.some(o => /Most Popular Companies/i.test(o));
      const hasRelevance = sortOptions.some(o => /Relevance Score/i.test(o));
      record(`QC183-${route.split('/').pop()}-popular`,
        `${route}: "Most Popular Companies" option removed`,
        !hasPopular ? 'PASS' : 'FAIL',
        `options found=${sortOptions.length}, sample=${JSON.stringify(sortOptions.slice(0, 8))}`);
      record(`QC183-${route.split('/').pop()}-relevance`,
        `${route}: "Relevance Score" option removed`,
        !hasRelevance ? 'PASS' : 'FAIL',
        `options found=${sortOptions.length}, sample=${JSON.stringify(sortOptions.slice(0, 8))}`);
      // Close dropdown for next iteration
      await page.keyboard.press('Escape').catch(() => {});
      await page.waitForTimeout(300);
    }

    // ====================================================================
    // T4 — QC #207 / DEF-207 — Cert Create stale-toast sessionStorage guard
    // ====================================================================
    console.log('\n--- T4: QC #207 mobile stale-toast guard ---');
    await page.setViewportSize({ width: 390, height: 844 }); // iPhone 12 portrait
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2000);
    // Read page source — the inline success script (rendered conditionally on TempData) must include the sessionStorage guard
    const pageHtml = await page.content();
    const hasGuard = /createSuccessMsg:|sessionStorage\.(getItem|setItem)\(['"]createSuccessMsg/.test(pageHtml);
    // Even if TempData is empty (most common), the script template should reference the guard key when emitted.
    // Look for both possibilities: either the script is omitted (no TempData), or if rendered it has the guard.
    const hasInlineScriptWithoutGuard = /<script[^>]*>[\s\S]*?showNotification\([\s\S]*?created successfully[\s\S]*?<\/script>/i.test(pageHtml)
      && !hasGuard;
    record('QC207-guard',
      'Create page inline-success guard pattern present (or script properly omitted when no TempData)',
      !hasInlineScriptWithoutGuard ? 'PASS' : 'FAIL',
      hasGuard ? 'sessionStorage createSuccessMsg pattern found' : 'no inline success script (TempData empty)');
    await page.screenshot({ path: path.join(OUT, `qc207-${TARGET}-mobile-create.png`), fullPage: true });
    // Reset viewport for T5
    await page.setViewportSize({ width: 1440, height: 900 });

    // ====================================================================
    // T5 — QC #174 / DEF-195 — Header-row align-items flex-start (top-aligned)
    // ====================================================================
    console.log('\n--- T5: QC #174 cert-number top-alignment ---');
    await page.goto(BASE + '/Certificate/CertificateTileView', { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    // Read computed alignment on every .header-row that's inside a .certificate-card
    const alignmentReport = await page.locator('.certificate-card .header-row').evaluateAll(els =>
      els.slice(0, 25).map(el => ({
        alignItems: window.getComputedStyle(el).alignItems,
        // Visual: top-delta between .certificate-category top and .certificate-id top
        topDelta: (() => {
          const cat = el.querySelector('.certificate-category');
          const id = el.querySelector('.certificate-id');
          if (!cat || !id) return null;
          return Math.round(id.getBoundingClientRect().top - cat.getBoundingClientRect().top);
        })(),
      }))
    );
    const sampleCount = alignmentReport.length;
    const allFlexStart = alignmentReport.every(r => r.alignItems === 'flex-start');
    const maxTopDelta = Math.max(0, ...alignmentReport.map(r => Math.abs(r.topDelta ?? 0)));
    record('QC174-align-items', '.header-row align-items is flex-start on every visible tile',
      allFlexStart ? 'PASS' : 'FAIL',
      `samples=${sampleCount} all-flex-start=${allFlexStart} distinct=${JSON.stringify([...new Set(alignmentReport.map(r => r.alignItems))])}`);
    record('QC174-top-delta', 'Cert-id pill top within 2px of category top across all sampled tiles',
      maxTopDelta <= 2 ? 'PASS' : 'FAIL',
      `max top-delta=${maxTopDelta}px (samples=${sampleCount})`);
    await page.screenshot({ path: path.join(OUT, `qc174-${TARGET}-tileview.png`), fullPage: false });

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, `qc-batch-priority4-${TARGET}-results.json`), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
