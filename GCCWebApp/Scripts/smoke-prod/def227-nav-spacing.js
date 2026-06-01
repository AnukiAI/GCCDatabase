// DEF-227 verification: breadcrumb gray spacer renders on the tile view across
// all 4 roles (Admin, Analyst, External, Public). Pre-fix: only Admin had the
// gray bar between nav and content. Post-fix: all roles get the consistent
// 32px gray spacer; Admin still has visible breadcrumb content inside it.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `def227-nav-spacing-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

const ROLES = [
  { role: 'admin', path: '/Certificate/CertificateTileViewInternal' },
  { role: 'analyst', path: '/Certificate/CertificateTileViewInternal' },
  { role: 'external', path: '/Certificate/CertificateTileViewExternal' },
  { role: 'public', path: '/Certificate/CertificateTileView' },
];

async function login(page, role) {
  await page.goto(`${BASE}/Account/Login`);
  await page.fill('input[name="Email"]', role);
  await page.fill('input[name="Password"]', 'TestPass123!');
  await Promise.all([page.waitForLoadState('networkidle'), page.click('button[type="submit"]')]);
}

(async () => {
  const browser = await chromium.launch();
  const results = [];

  for (const r of ROLES) {
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await ctx.newPage();

    try {
      if (r.role !== 'public') {
        await login(page, r.role);
      }
      await page.goto(`${BASE}${r.path}`);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1500);

      const breadcrumbState = await page.evaluate(() => {
        const nav = document.querySelector('nav.breadcrumbs');
        if (!nav) return { error: 'no nav.breadcrumbs' };
        const wrapper = nav.querySelector('.breadcrumb-wrapper');
        const cs = window.getComputedStyle(nav);
        const wcs = wrapper ? window.getComputedStyle(wrapper) : null;
        return {
          navDisplay: cs.display,
          navVisibility: cs.visibility,
          navHeight: nav.offsetHeight,
          navBackground: cs.backgroundColor,
          wrapperVisibility: wcs ? wcs.visibility : null,
          wrapperText: wrapper ? wrapper.textContent.trim().slice(0, 100) : null,
          bodyClass: document.body.className,
        };
      });

      console.log(`[${r.role}] nav.breadcrumbs:`, JSON.stringify(breadcrumbState, null, 2));
      results.push({ role: r.role, ...breadcrumbState });

      await page.screenshot({ path: path.join(OUT, `${r.role}-tile-view.png`), fullPage: false });
    } catch (e) {
      console.error(`[${r.role}] FAIL:`, e.message);
      results.push({ role: r.role, error: e.message });
    } finally {
      await ctx.close();
    }
  }

  fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(results, null, 2));

  console.log('\n=== SUMMARY ===');
  results.forEach(r => {
    if (r.error) {
      console.log(`  ${r.role}: ERROR ${r.error}`);
      return;
    }
    const spacerPresent = r.navDisplay === 'block' && r.navHeight >= 32;
    const adminContentVisible = r.role === 'admin' ? r.wrapperVisibility === 'visible' : r.wrapperVisibility === 'hidden';
    console.log(`  ${r.role}: spacer=${spacerPresent ? 'YES' : 'NO'} (h=${r.navHeight}, display=${r.navDisplay}) wrapper=${r.wrapperVisibility} ${adminContentVisible ? '✓' : 'WRONG'}`);
  });
  console.log(`\nOutput: ${OUT}`);
  await browser.close();
})();
