// Per-view smoke — /Certificate/CertificateTileViewExternal (Tier 1 #4).
//
// Access matrix: public -> 401 (redirect to login), external/analyst/admin -> 200.
//
// This view's markup is the External fork (certificate-tile-view-external.js). The
// role-sensitive removals are baked into the *view*, not toggled per role, so they
// hold for every authenticated role that loads this URL:
//   - NO "Add Certificate" button (.btn-add absent).
//   - Internal Tag column omitted (no [data-col="tc-inttag"], no .tc-inttag header).
//   - A Partner filter IS present (external-only facet).
//   - Export-to-CSV control present (#exportAllBtn).
//
// Run:  node Scripts/smoke-prod/_views/cert-tile-external.js

const { runViewSmoke } = require('../_lib/smoke-template');

async function waitForTiles(page, timeout = 9000) {
  try {
    await page.waitForFunction(
      () => document.querySelectorAll('#certificatesGrid .certificate-card').length > 0,
      { timeout }
    );
  } catch { /* may legitimately be empty for the scoped external company */ }
  return page.locator('#certificatesGrid .certificate-card').count();
}

async function run() {
  return runViewSmoke({
    viewId: 'cert-tile-external',
    url: '/Certificate/CertificateTileViewExternal',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    // Public is denied (redirect to login → 401-equiv); the others get the page.
    expectedAccess: { public: 401, external: 200, analyst: 200, admin: 200 },
    assertions: async ({ page, record }) => {
      record('cert-tile-external.grid', 'Certificates grid present',
        (await page.locator('#certificatesGrid').count()) > 0 ? 'PASS' : 'FAIL');

      // No Add Certificate button for External view.
      const addBtn = await page.locator('.btn-add').count();
      record('cert-tile-external.no-add', 'No Add Certificate button',
        addBtn === 0 ? 'PASS' : 'FAIL', `btn-add count=${addBtn}`);

      // Internal tag column must be hidden on the external surface.
      const intTag = await page.locator('[data-col="tc-inttag"], .tc-inttag').count();
      record('cert-tile-external.no-internal-tag', 'Internal Tag column hidden',
        intTag === 0 ? 'PASS' : 'FAIL', `internal-tag elements=${intTag}`);

      record('cert-tile-external.partner-filter', 'Partner filter present',
        (await page.locator('[data-filter="partner"]').count()) > 0 ? 'PASS' : 'FAIL');

      record('cert-tile-external.export', 'Export to CSV control present',
        (await page.locator('#exportAllBtn').count()) > 0 ? 'PASS' : 'FAIL');

      const cards = await waitForTiles(page);
      record('cert-tile-external.tiles-load', 'Tiles load (company-scoped)',
        cards > 0 ? 'PASS' : 'WARN', `cards=${cards}`);
    },
  });
}

module.exports = { run };

if (require.main === module) {
  run().then((r) => {
    process.exitCode = r.fail === 0 ? 0 : 1;
  });
}
