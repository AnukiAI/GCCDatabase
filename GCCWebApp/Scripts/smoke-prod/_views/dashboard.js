// Per-view smoke — /Dashboard/Index (Tier 1 #8).
//
// Access matrix: public -> 401, external -> 403, analyst/admin -> 200.
//
// Assertions:
//   - .stats-grid present with >=5 .stat-card tiles (the page ships ~12).
//   - .tables-container with >=3 .table-section breakdown tables (ships 5).
//   - >=1 tfoot.dashboard-table-total — breakdown tables show a Total row (DEF-221).
//   - Mobile: .stats-grid collapses to a single column (DEF-114) — verified by reading
//     the computed grid-template-columns (one track on mobile).
//
// Run:  node Scripts/smoke-prod/_views/dashboard.js

const { runViewSmoke } = require('../_lib/smoke-template');

async function run() {
  return runViewSmoke({
    viewId: 'dashboard',
    url: '/Dashboard/Index',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 401, external: 403, analyst: 200, admin: 200 },
    assertions: async ({ page, viewport, record }) => {
      record('dashboard.stats-grid', 'Stats grid present',
        (await page.locator('.stats-grid').count()) > 0 ? 'PASS' : 'FAIL');

      const cards = await page.locator('.stat-card').count();
      record('dashboard.stat-cards', 'Stat tiles render (>=5)',
        cards >= 5 ? 'PASS' : 'FAIL', `stat-cards=${cards}`);

      const tables = await page.locator('.tables-container .table-section').count();
      record('dashboard.tables', 'Breakdown tables render (>=3)',
        tables >= 3 ? 'PASS' : 'FAIL', `table-sections=${tables}`);

      const totals = await page.locator('tfoot.dashboard-table-total').count();
      record('dashboard.totals', 'Breakdown tables have Total rows (DEF-221)',
        totals >= 1 ? 'PASS' : 'FAIL', `total-rows=${totals}`);

      if (viewport === 'mobile') {
        // DEF-114: stats-grid must be a single column on mobile.
        const cols = await page.evaluate(() => {
          const el = document.querySelector('.stats-grid');
          if (!el) return null;
          return getComputedStyle(el).gridTemplateColumns;
        });
        const trackCount = cols && cols !== 'none' ? cols.trim().split(/\s+/).length : 0;
        record('dashboard.mobile-single-col', 'Stats grid single column on mobile (DEF-114)',
          trackCount === 1 ? 'PASS' : 'FAIL', `grid-template-columns="${cols}" tracks=${trackCount}`);
      }
    },
  });
}

module.exports = { run };

if (require.main === module) {
  run().then((r) => {
    process.exitCode = r.fail === 0 ? 0 : 1;
  });
}
