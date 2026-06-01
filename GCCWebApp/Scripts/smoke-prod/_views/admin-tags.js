// Per-view smoke — /Admin/Tags (Tier 1 #10).
//
// Access matrix: admin-only. public -> 401, external -> 403, analyst -> 403, admin -> 200.
//
// Assertions (admin):
//   - #tag-table loads with the 3 data columns (Name / Type / Description) + Actions.
//   - Template / Import / Export / Add buttons present
//     (#template-btn, #import-btn, #export-btn, #add-tag-btn) — DEF-232 toolbar.
//   The Add Tag modal is constructed by tags.js on click rather than rendered statically,
//   so we assert the #add-tag-btn affordance rather than a static modal element.
//
// Run:  node Scripts/smoke-prod/_views/admin-tags.js

const { runViewSmoke } = require('../_lib/smoke-template');

async function run() {
  return runViewSmoke({
    viewId: 'admin-tags',
    url: '/Admin/Tags',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 401, external: 403, analyst: 403, admin: 200 },
    assertions: async ({ page, record }) => {
      record('admin-tags.table', 'Tag table loads',
        (await page.locator('#tag-table').count()) > 0 ? 'PASS' : 'FAIL');

      // Name / Type / Description data columns (+ Actions) — aligns with the 3-column
      // Template CSV (DEF-232). Header has 4 <th> (3 sortable data cols + Actions).
      const dataCols = await page.locator('#tag-table thead .admin-table-th--sortable').count();
      record('admin-tags.columns', 'Three data columns present (Name/Type/Description)',
        dataCols >= 3 ? 'PASS' : 'FAIL', `sortable-columns=${dataCols}`);

      for (const [sel, label] of [
        ['#template-btn', 'Template'],
        ['#import-btn', 'Import'],
        ['#export-btn', 'Export'],
        ['#add-tag-btn', 'Add New Tag'],
      ]) {
        record(`admin-tags.btn${sel}`, `${label} button present`,
          (await page.locator(sel).count()) > 0 ? 'PASS' : 'FAIL');
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
