// Per-view smoke — /Certificate/CertificateTileViewInternal (Tier 1 #5).
//
// Access matrix: public -> 401, external -> 403, analyst/admin -> 200.
//
// Internal view markup (certificate-tile-view-internal.js):
//   - "Add Certificate" button IS present (.btn-add).
//   - Internal Tag column IS present ([data-col="tc-inttag"]).
//   - The bulk Import button (#openImportModalBtn) is ANALYST-OR-ADMIN — present for both
//     (gated by @if User.IsInRole("Admin") || User.IsInRole("Analyst") in the view, on both
//     the button ~L248 and the import modal ~L404).
//     Widened from admin-only in commit 5a94078 (2026-07-30) so the UI matches
//     CertificateImportController's long-standing [Authorize(Policy = "AnalystOrAdmin")],
//     which always permitted analysts server-side. This smoke asserted the pre-7/30 rule
//     until 2026-08-03; see observations_log.md 2026-08-03 regression run.
//
// Run:  node Scripts/smoke-prod/_views/cert-tile-internal.js

const { runViewSmoke } = require('../_lib/smoke-template');

async function waitForTiles(page, timeout = 9000) {
  try {
    await page.waitForFunction(
      () => document.querySelectorAll('#certificatesGrid .certificate-card').length > 0,
      { timeout }
    );
  } catch { /* fall through */ }
  return page.locator('#certificatesGrid .certificate-card').count();
}

async function run() {
  return runViewSmoke({
    viewId: 'cert-tile-internal',
    url: '/Certificate/CertificateTileViewInternal',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 401, external: 403, analyst: 200, admin: 200 },
    assertions: async ({ page, role, record }) => {
      record('cert-tile-internal.grid', 'Certificates grid present',
        (await page.locator('#certificatesGrid').count()) > 0 ? 'PASS' : 'FAIL');

      // Add Certificate button present on the internal surface.
      record('cert-tile-internal.add-button', 'Add Certificate button present',
        (await page.locator('.btn-add').count()) > 0 ? 'PASS' : 'FAIL');

      // Internal Tag column shown.
      const intTag = await page.locator('[data-col="tc-inttag"], .tc-inttag').count();
      record('cert-tile-internal.internal-tag', 'Internal Tag column shown',
        intTag > 0 ? 'PASS' : 'FAIL', `internal-tag elements=${intTag}`);

      // Import button is analyst-or-admin (matches the AnalystOrAdmin policy on
      // CertificateImportController). Anyone else must not see it.
      const importBtn = await page.locator('#openImportModalBtn').count();
      if (role === 'admin' || role === 'analyst') {
        record('cert-tile-internal.import-visible', `${role} sees bulk Import button`,
          importBtn > 0 ? 'PASS' : 'FAIL', `import button=${importBtn}`);
      } else {
        record('cert-tile-internal.import-hidden', 'Non-analyst/admin does NOT see Import button',
          importBtn === 0 ? 'PASS' : 'FAIL', `import button=${importBtn}`);
      }

      const cards = await waitForTiles(page);
      record('cert-tile-internal.tiles-load', 'Tiles load (all states visible)',
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
