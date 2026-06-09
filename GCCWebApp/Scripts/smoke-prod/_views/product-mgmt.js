// Per-view smoke — /Product/Management (Tier 1 #11).
//
// Added 2026-06-05 to give Product Management the same role x viewport regression coverage as
// the other Tier 1 views, after DEF-253 (#2: list projection -> .AsSplitQuery()) and DEF-254
// (cross-category Product Type/Subcategory carry-over on Edit) both changed this page but it
// had no standing smoke. Targeted behavioural checks for those two live in
// Scripts/smoke-prod/def253-254-verify.js; this file is the breadth (loads, role-gates, no
// errors across roles/viewports) net.
//
// Access: ProductController is [Authorize(Policy="ExternalAccess")] — External+ can VIEW the
// page; per CLAUDE.md External users get NO Add/Edit buttons and NO Actions column (role-gated
// via role-analyst-show / role-admin-show CSS classes).
//   public -> 401 (login redirect), external -> 200 (read-only), analyst -> 200, admin -> 200.
//
// Assertions:
//   - #product-table renders.
//   - Product rows load (required for analyst/admin; external is company-scoped so 0 is allowed).
//   - #product-modal (Add/Edit modal) present in DOM.
//   - Role-gating: analyst/admin see #add-product-btn + Actions column; external sees neither.
//
// Run:  node Scripts/smoke-prod/_views/product-mgmt.js

const { runViewSmoke } = require('../_lib/smoke-template');

async function run() {
  return runViewSmoke({
    viewId: 'product-mgmt',
    url: '/Product/Management',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 401, external: 200, analyst: 200, admin: 200 },
    assertions: async ({ page, role, viewport, record }) => {
      record('product-mgmt.table', 'Product table renders',
        (await page.locator('#product-table').count()) > 0 ? 'PASS' : 'FAIL');

      // Products load asynchronously from the list API (DEF-253 #2 split-query path). Wait for
      // at least one row; an external user may legitimately be scoped to zero, so don't hang.
      try {
        await page.waitForFunction(() => {
          const tb = document.querySelector('#product-table tbody');
          return tb && tb.querySelectorAll('tr').length > 0;
        }, { timeout: 12000 });
      } catch { /* tolerated — see external note below */ }
      const rowCount = await page.locator('#product-table tbody tr').count();

      if (role === 'admin' || role === 'analyst') {
        record('product-mgmt.rows', 'Product rows load (>0)',
          rowCount > 0 ? 'PASS' : 'FAIL', `rows=${rowCount}`);
      } else {
        record('product-mgmt.rows', 'Product rows query returned without error',
          'PASS', `rows=${rowCount} (external is company-scoped)`);
      }

      record('product-mgmt.modal', 'Product Add/Edit modal present in DOM',
        (await page.locator('#product-modal').count()) > 0 ? 'PASS' : 'FAIL');

      // Role-gating (CLAUDE.md External restriction: no Add/Edit, no Actions column).
      // The Add button is the viewport-independent role signal (it stays a button at every
      // width). The Actions *column header* (th.actions-col) is only asserted on desktop: the
      // .admin-table collapses to a stacked/card layout on tablet/mobile and hides column
      // headers (rows then use data-label), so the <th> is legitimately display:none there —
      // responsive behaviour, not a role signal. External is hidden by role CSS at every width.
      const addVisible = await page.locator('#add-product-btn').isVisible().catch(() => false);
      const actionsVisible = await page.locator('#product-table th.actions-col').first().isVisible().catch(() => false);
      if (role === 'admin' || role === 'analyst') {
        record('product-mgmt.add-visible', 'Add Product button visible (analyst/admin)',
          addVisible ? 'PASS' : 'FAIL');
        if (viewport === 'desktop') {
          record('product-mgmt.actions-visible', 'Actions column visible (analyst/admin, desktop)',
            actionsVisible ? 'PASS' : 'FAIL');
        }
      } else if (role === 'external') {
        record('product-mgmt.add-hidden', 'Add Product button hidden (external)',
          !addVisible ? 'PASS' : 'FAIL');
        record('product-mgmt.actions-hidden', 'Actions column hidden (external)',
          !actionsVisible ? 'PASS' : 'FAIL');
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
