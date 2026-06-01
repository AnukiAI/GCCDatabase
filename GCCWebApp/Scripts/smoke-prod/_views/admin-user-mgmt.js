// Per-view smoke — /Admin/UserManagement (Tier 1 #9).
//
// Access matrix: admin-only. public -> 401, external -> 403, analyst -> 403, admin -> 200.
//
// Assertions (admin):
//   - #user-table loads.
//   - Template / Import / Export / Add buttons present
//     (#template-btn, #import-btn, #export-btn, #add-user-btn) — DEF-232 toolbar.
//   - #user-modal (Add User modal) exists in the DOM.
//
// Run:  node Scripts/smoke-prod/_views/admin-user-mgmt.js

const { runViewSmoke } = require('../_lib/smoke-template');

async function run() {
  return runViewSmoke({
    viewId: 'admin-user-mgmt',
    url: '/Admin/UserManagement',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 401, external: 403, analyst: 403, admin: 200 },
    assertions: async ({ page, record }) => {
      record('admin-user-mgmt.table', 'User table loads',
        (await page.locator('#user-table').count()) > 0 ? 'PASS' : 'FAIL');

      for (const [sel, label] of [
        ['#template-btn', 'Template'],
        ['#import-btn', 'Import'],
        ['#export-btn', 'Export'],
        ['#add-user-btn', 'Add New User'],
      ]) {
        record(`admin-user-mgmt.btn${sel}`, `${label} button present`,
          (await page.locator(sel).count()) > 0 ? 'PASS' : 'FAIL');
      }

      record('admin-user-mgmt.modal', 'Add User modal present in DOM',
        (await page.locator('#user-modal').count()) > 0 ? 'PASS' : 'FAIL');
    },
  });
}

module.exports = { run };

if (require.main === module) {
  run().then((r) => {
    process.exitCode = r.fail === 0 ? 0 : 1;
  });
}
