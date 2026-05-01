// DEF-099 / DEF-098 verification — filter collapse + tablet sidebar
// Runs against http://localhost:5101
import { chromium } from 'playwright';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const BASE = 'http://localhost:5101';
const OUT = __dirname;

const VIEWPORTS = [
  { name: 'desktop-1920', width: 1920, height: 1080 },
  { name: 'laptop-1280', width: 1280, height: 800 },
  { name: 'tablet-1024', width: 1024, height: 768 },
  { name: 'tablet-768', width: 768, height: 1024 },
  { name: 'mobile-375', width: 375, height: 812 }
];

async function login(page, username) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[name="Email"], input[name="Username"], input[type="email"], input[type="text"]', username);
  await page.fill('input[name="Password"], input[type="password"]', 'TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.click('button[type="submit"], input[type="submit"]')
  ]);
  // Don't error if redirect lands somewhere unexpected — caller will navigate
  await page.waitForTimeout(800);
}

async function waitForFilters(page) {
  await page.waitForSelector('.filter-header', { timeout: 15000 });
  await page.waitForTimeout(500);
}

async function testFilterCollapse(page, label) {
  // Pick first filter header that has a data-filter attribute
  const header = await page.$('.filter-header[data-filter]');
  if (!header) return { ok: false, reason: 'no .filter-header[data-filter] found' };

  const filterId = await header.getAttribute('data-filter');
  const contentSel = `#${filterId}-content`;

  // Initial state
  const initiallyCollapsed = await page.$eval(contentSel, el => el.classList.contains('filter-collapsed'));
  await page.screenshot({ path: path.join(OUT, `${label}-1-initial.png`), fullPage: true });

  // Click 1
  await header.click();
  await page.waitForTimeout(400);
  const afterClick1 = await page.$eval(contentSel, el => el.classList.contains('filter-collapsed'));
  await page.screenshot({ path: path.join(OUT, `${label}-2-click1.png`), fullPage: true });

  // Click 2
  await header.click();
  await page.waitForTimeout(400);
  const afterClick2 = await page.$eval(contentSel, el => el.classList.contains('filter-collapsed'));
  await page.screenshot({ path: path.join(OUT, `${label}-3-click2.png`), fullPage: true });

  const toggled = (initiallyCollapsed !== afterClick1) && (afterClick1 !== afterClick2);
  return {
    ok: toggled,
    filterId,
    initiallyCollapsed,
    afterClick1,
    afterClick2,
    reason: toggled ? null : `expected toggle on each click — i:${initiallyCollapsed} c1:${afterClick1} c2:${afterClick2}`
  };
}

async function countHideFilterBars(page) {
  // Sidebar collapse btn + sidebar toggle tab + any "Hide Filters" text element
  const sidebarBtn = await page.$$('.sidebar-collapse-btn');
  const sidebarTab = await page.$$('.sidebar-toggle-tab');
  const visibleSidebarBtn = (await Promise.all(sidebarBtn.map(e => e.isVisible()))).filter(Boolean).length;
  const visibleSidebarTab = (await Promise.all(sidebarTab.map(e => e.isVisible()))).filter(Boolean).length;
  // Count elements containing "Hide Filters" text that are visible
  const hideFiltersCount = await page.evaluate(() => {
    const all = Array.from(document.querySelectorAll('button, a, div, span'));
    return all.filter(el => {
      const t = (el.textContent || '').trim().toLowerCase();
      if (!t.includes('hide filters')) return false;
      const r = el.getBoundingClientRect();
      const style = getComputedStyle(el);
      return r.width > 0 && r.height > 0 && style.visibility !== 'hidden' && style.display !== 'none';
    }).length;
  });
  return { visibleSidebarBtn, visibleSidebarTab, hideFiltersCount };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const results = { internal: {}, external: {}, tabletSidebar: {} };

  // === INTERNAL view (analyst) ===
  for (const vp of VIEWPORTS) {
    const context = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
    const page = await context.newPage();
    page.on('console', msg => { if (msg.type() === 'error') console.log(`  [console.error ${vp.name}]`, msg.text()); });
    try {
      await login(page, 'analyst');
      await page.goto(`${BASE}/Certificate/CertificateTileView`, { waitUntil: 'domcontentloaded' });
      await waitForFilters(page);
      const result = await testFilterCollapse(page, `internal-${vp.name}`);
      results.internal[vp.name] = result;
      const bars = await countHideFilterBars(page);
      results.tabletSidebar[`internal-${vp.name}`] = bars;
      console.log(`internal ${vp.name}:`, result.ok ? 'PASS' : 'FAIL', result.reason || `(${result.filterId})`, 'bars:', JSON.stringify(bars));
    } catch (e) {
      results.internal[vp.name] = { ok: false, reason: e.message };
      console.log(`internal ${vp.name}: ERROR`, e.message);
    } finally {
      await context.close();
    }
  }

  // === EXTERNAL view ===
  for (const vp of VIEWPORTS) {
    const context = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
    const page = await context.newPage();
    page.on('console', msg => { if (msg.type() === 'error') console.log(`  [console.error ${vp.name}]`, msg.text()); });
    try {
      await login(page, 'external');
      await page.goto(`${BASE}/Certificate/CertificateTileViewExternal`, { waitUntil: 'domcontentloaded' });
      await waitForFilters(page);
      const result = await testFilterCollapse(page, `external-${vp.name}`);
      results.external[vp.name] = result;
      const bars = await countHideFilterBars(page);
      results.tabletSidebar[`external-${vp.name}`] = bars;
      console.log(`external ${vp.name}:`, result.ok ? 'PASS' : 'FAIL', result.reason || `(${result.filterId})`, 'bars:', JSON.stringify(bars));
    } catch (e) {
      results.external[vp.name] = { ok: false, reason: e.message };
      console.log(`external ${vp.name}: ERROR`, e.message);
    } finally {
      await context.close();
    }
  }

  await browser.close();
  console.log('\n=== SUMMARY ===');
  console.log(JSON.stringify(results, null, 2));
})();
