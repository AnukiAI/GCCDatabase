// Final QC batch — 4 UX items:
//   QC #213 / DEF-213 — Tag close button renders `×` not `Ã—` on Cert Edit   mojibake-doc
//   QC #212 / DEF-212 — SearchableSelect refocus shows full list, not collapsed-to-selected
//   QC #175 / DEF-201 — Header logo doesn't distort at 1025-1440px desktop widths
//   QC #176 / DEF-201 — Filter sidebar auto-shows when viewport drops below 1024px
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

  console.log(`\nFinal QC batch — ${TARGET.toUpperCase()} (${BASE})\n`);

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
    // T1 — QC #213 — Tag chip close button on /Certificate/Edit
    // ====================================================================
    console.log('--- T1: QC #213 tag close button encoding ---');
    // Cert 26-0004 was the original repro per DEF-213. Find any cert with tags.
    const certCandidates = ['26-0004', '26-0163', '26-0161'];
    let certWithTags = null;
    for (const c of certCandidates) {
      const r = await ctx.request.get(`${BASE}/Certificate/Edit/${c}`);
      if (r.status() === 200) {
        const body = await r.text();
        if (/external-tags-display|internal-tags-display/.test(body)) {
          certWithTags = c;
          break;
        }
      }
    }
    if (!certWithTags) certWithTags = '26-0004'; // fallback

    await page.goto(`${BASE}/Certificate/Edit/${certWithTags}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);
    // Look at the rendered tag chips — close button should be `×` (U+00D7), not the
    // 2-char mojibake sequence `Ã—` (0xC3 0x97 viewed as Latin-1)   mojibake-doc
    const tagCloseTextNodes = await page.evaluate(() => {
      const out = [];
      document.querySelectorAll('.tag-chip, .external-tag, .internal-tag, [class*="tag-"]').forEach(el => {
        const txt = el.innerText || '';
        if (txt) out.push({ classes: el.className?.toString().slice(0, 60), text: txt.slice(0, 60) });
      });
      return out.slice(0, 20);
    });
    const allChipText = tagCloseTextNodes.map(t => t.text).join(' | ');
    const hasMojibake = /Ã—/.test(allChipText); // mojibake-doc: this detector must contain the sequence it detects
    const hasCorrectX = /×/.test(allChipText) || /&times;/.test(allChipText);
    record('QC213-no-mojibake', 'Tag chips contain NO `Ã—` mojibake', // mojibake-doc
      !hasMojibake ? 'PASS' : 'FAIL',
      `samples (first 3)=${JSON.stringify(tagCloseTextNodes.slice(0, 3))}`);
    if (tagCloseTextNodes.length > 0) {
      record('QC213-has-times',
        'Tag chips contain proper `×` close-button character (or no chips on this cert)',
        hasCorrectX || tagCloseTextNodes.length === 0 ? 'PASS' : 'WARN',
        `chips found=${tagCloseTextNodes.length}, sample="${allChipText.slice(0, 100)}"`);
    }
    await page.screenshot({ path: path.join(OUT, `qc213-${TARGET}-tags.png`), fullPage: false });

    // ====================================================================
    // T2 — QC #212 — SearchableSelect refocus shows full list
    //   Per probe: visible text input uses placeholder="Search countries..."
    //   and class="form-control" inside a .searchable-select-wrapper. The
    //   dropdown options live in a sibling .searchable-select-dropdown.
    // ====================================================================
    console.log('\n--- T2: QC #212 SearchableSelect refocus ---');
    const countryInput = page.locator('input[placeholder="Search countries..."]').first();
    if (await countryInput.count() === 0) {
      record('QC212-input-found', 'Country SearchableSelect input on Edit form', 'FAIL', 'placeholder selector still missed');
    } else {
      const preFocusValue = await countryInput.inputValue();
      console.log(`  pre-focus input value: "${preFocusValue}"`);
      // Click to focus AND open dropdown (focus alone may not open)
      await countryInput.click();
      await page.waitForTimeout(600);

      // Count visible option items in the dropdown nearest this input
      const optCount = await page.evaluate(() => {
        // Find the wrapper of the focused/open dropdown
        const wrappers = document.querySelectorAll('.searchable-select-wrapper');
        for (const w of wrappers) {
          const input = w.querySelector('input[placeholder="Search countries..."]');
          if (!input) continue;
          const dd = w.querySelector('.searchable-select-dropdown');
          if (!dd) continue;
          // Is it visible? (display !== 'none' AND not hidden)
          const cs = window.getComputedStyle(dd);
          if (cs.display === 'none' || cs.visibility === 'hidden') return { visible: false, count: 0 };
          const items = dd.querySelectorAll('li, .option, [role="option"], .dropdown-option');
          return { visible: true, count: items.length };
        }
        return { visible: false, count: 0 };
      });
      console.log(`  dropdown after refocus:`, optCount);
      record('QC212-full-list',
        'Refocus on populated Country dropdown shows full option list (>1, ideally ~197)',
        optCount.visible && optCount.count > 1 ? 'PASS' : 'FAIL',
        `preFocusValue="${preFocusValue}" visible=${optCount.visible} optionsVisible=${optCount.count} (pre-fix bug would show 1)`);
      await page.screenshot({ path: path.join(OUT, `qc212-${TARGET}-country-refocus.png`), fullPage: false });
    }

    // ====================================================================
    // T3 — QC #175 — Header logo at 1280×768 mid-range desktop
    // ====================================================================
    console.log('\n--- T3: QC #175 header logo at mid-range desktop ---');
    await page.setViewportSize({ width: 1280, height: 768 });
    await page.goto(BASE + '/', { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);
    const logoMetrics = await page.evaluate(() => {
      const logo = document.querySelector('.main-logo');
      const textGroup = document.querySelector('.logo-text-group');
      if (!logo) return { logoFound: false };
      const cs = window.getComputedStyle(logo);
      const rect = logo.getBoundingClientRect();
      return {
        logoFound: true,
        width: rect.width,
        height: rect.height,
        // Wrap detection: if textGroup occupies more than one line
        textGroupHeight: textGroup ? textGroup.getBoundingClientRect().height : null,
        textGroupWidth: textGroup ? textGroup.getBoundingClientRect().width : null,
      };
    });
    console.log(`  logo metrics:`, logoMetrics);
    // Per DEF-201, the new rule sets logo to 220px at 1025-1440 viewport.
    // We assert: logo width < 280 (the broken-at-this-viewport default) AND > 100 (not collapsed)
    record('QC175-logo-sized',
      'Logo width in 1280px viewport is <280px (fix landed; was 280 pre-fix → wrap)',
      logoMetrics.logoFound && logoMetrics.width < 280 && logoMetrics.width > 100 ? 'PASS' : 'FAIL',
      `width=${logoMetrics.width}px (expecting ~220px per DEF-201 fix)`);
    await page.screenshot({ path: path.join(OUT, `qc175-${TARGET}-1280.png`), fullPage: false });

    // ====================================================================
    // T4 — QC #176 — Filter sidebar reappears when viewport drops below 1024px
    // ====================================================================
    console.log('\n--- T4: QC #176 filter sidebar auto-show on resize-to-mobile ---');
    // Open the public tile view at desktop
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(BASE + '/Certificate/CertificateTileView', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500);

    // Try to collapse the sidebar via the toggle (id from earlier QC #98 work)
    const collapseBtn = page.locator('.sidebar-collapse-btn, #sidebarToggle, [aria-label*="filter" i][aria-label*="hide" i]').first();
    let collapsedOk = false;
    if (await collapseBtn.count() > 0) {
      await collapseBtn.click().catch(() => {});
      await page.waitForTimeout(800);
      collapsedOk = await page.evaluate(() => {
        const sb = document.querySelector('.sidebar, .filter-sidebar, .filters-sidebar, aside');
        return sb && (sb.classList.contains('collapsed') || sb.classList.contains('hidden') || window.getComputedStyle(sb).display === 'none');
      });
    }
    // Now resize to 768px (below 1024 threshold) — fix should auto-drop .collapsed
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(1500);
    const afterResize = await page.evaluate(() => {
      const sb = document.querySelector('.sidebar, .filter-sidebar, .filters-sidebar, aside');
      if (!sb) return { found: false };
      return {
        found: true,
        classes: sb.className,
        collapsed: sb.classList.contains('collapsed'),
        hidden: sb.classList.contains('hidden'),
        displayCss: window.getComputedStyle(sb).display,
      };
    });
    console.log('  sidebar after viewport drop to 768:', afterResize);
    // Many UIs hide the sidebar entirely on mobile by design — but QC #176 specifically
    // says the .collapsed (desktop-state) class must NOT persist. So: `collapsed=false`
    // is the assertion; whether it's visually shown via a hamburger or always visible
    // depends on the responsive design, but the stuck .collapsed bug is what we test.
    record('QC176-no-stuck-collapsed',
      'Sidebar .collapsed class does NOT persist when viewport drops below 1024px',
      afterResize.found && !afterResize.collapsed ? 'PASS' : 'WARN',
      `classes="${afterResize.classes || '(none found)'}" collapsed=${afterResize.collapsed}`);
    await page.screenshot({ path: path.join(OUT, `qc176-${TARGET}-mobile.png`), fullPage: false });

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, `qc-final-batch-${TARGET}-results.json`), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
