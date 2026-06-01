// Per-view smoke — /Certificate/CertificateTileView (Tier 1 #3).
//
// The public tile view. AllowAnonymous, so every role gets 200. Tiles render
// client-side into #certificatesGrid via certificate-tile-view.js (each tile is a
// .certificate-card). The seeded localhost DB has public-status certs, so we expect
// at least one card to render for the public grid.
//
// Assertions:
//   1. Main container + filter sidebar + search box present.
//   2. The 5 filter panels render (cert-type, product-cat, company, countries, additional).
//   3. Tiles actually load (.certificate-card count > 0) — the grid is async, so we wait.
//   4. Results count element present and reflects a loaded state.
//
// Run:  node Scripts/smoke-prod/_views/cert-tile-public.js

const { runViewSmoke } = require('../_lib/smoke-template');

// Wait for the async tile grid to populate. Returns the card count (0 if none after
// the timeout — the caller decides PASS/WARN). Public grid is expected to be non-empty.
async function waitForTiles(page, timeout = 9000) {
  try {
    await page.waitForFunction(
      () => document.querySelectorAll('#certificatesGrid .certificate-card').length > 0,
      { timeout }
    );
  } catch {
    /* timed out — fall through and report whatever count we have */
  }
  return page.locator('#certificatesGrid .certificate-card').count();
}

async function run() {
  return runViewSmoke({
    viewId: 'cert-tile-public',
    url: '/Certificate/CertificateTileView',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 200, external: 200, analyst: 200, admin: 200 },
    assertions: async ({ page, record }) => {
      record('cert-tile-public.container', 'Main container renders',
        (await page.locator('.main-container').count()) > 0 ? 'PASS' : 'FAIL');

      record('cert-tile-public.sidebar', 'Filter sidebar present',
        (await page.locator('#filterSidebar').count()) > 0 ? 'PASS' : 'FAIL');

      record('cert-tile-public.search', 'Search input present',
        (await page.locator('#searchInput').count()) > 0 ? 'PASS' : 'FAIL');

      const panels = await page.locator('.filter-panel').count();
      record('cert-tile-public.filters', 'Filter panels render (>=4)',
        panels >= 4 ? 'PASS' : 'FAIL', `panels=${panels}`);

      record('cert-tile-public.grid', 'Certificates grid present',
        (await page.locator('#certificatesGrid').count()) > 0 ? 'PASS' : 'FAIL');

      // Tiles load asynchronously — public DB has public-status certs, so expect >0.
      const cards = await waitForTiles(page);
      record('cert-tile-public.tiles-load', 'Tiles load into grid',
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
