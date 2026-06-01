// Reference per-view smoke — /Home/Index.
//
// Home/Index is a role-based redirect (HomeController.Index):
//   - Public      -> /Certificate/CertificateTileView
//   - External    -> /Certificate/CertificateTileViewExternal
//   - Analyst     -> /Certificate/CertificateTileViewInternal
//   - Admin       -> /Certificate/CertificateTileViewInternal
//
// What this smoke proves:
//   1. The role-based redirect lands on the right tile view for every role.
//   2. The shared layout renders correctly (logo + main nav) on each viewport.
//   3. No console/page/network errors during the redirect+landing flow.
//   4. No horizontal scroll on mobile (375px).
//
// Run:  node Scripts/smoke-prod/_views/home.js
// Env:  BASE_URL (default http://localhost:5101), SMOKE_SCREENSHOTS=on-fail|always|never

const { runViewSmoke } = require('../_lib/smoke-template');

// Each role's expected landing path(s) — multiple values are aliases. Public lands
// on '/' because the ASP.NET URL generator picks the shorter canonical alias for
// CertificateTileView (verified via `curl -I /Home/Index`: Location: /).
const EXPECTED_LANDING = {
  public:   ['/', '/Certificate/CertificateTileView'],
  external: ['/Certificate/CertificateTileViewExternal'],
  analyst:  ['/Certificate/CertificateTileViewInternal'],
  admin:    ['/Certificate/CertificateTileViewInternal'],
};

async function run() {
  return runViewSmoke({
    viewId: 'home',
    url: '/Home/Index',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    // Every role gets a 200 on the landing page — the redirect is server-side and
    // followed before navigation resolves, so resp.status() is the final page's status.
    expectedAccess: { public: 200, external: 200, analyst: 200, admin: 200 },
    assertions: async ({ page, role, viewport, record }) => {
      // 1. Role-based redirect landed on one of the expected URLs.
      const finalPath = new URL(page.url()).pathname;
      const expectedPaths = EXPECTED_LANDING[role];
      const landedRight = expectedPaths.some(
        (p) => finalPath.toLowerCase() === p.toLowerCase()
      );
      record(
        'home.redirect',
        `Role redirect lands on ${expectedPaths.join(' OR ')}`,
        landedRight ? 'PASS' : 'FAIL',
        landedRight ? '' : `landed on ${finalPath}`
      );

      // 2. Logo renders. `.logo-area` is the desktop wrapper; on mobile the same
      // element exists but may be collapsed under a hamburger.
      const logoCount = await page.locator('.logo-area, .logo-text').count();
      record('home.logo', 'Logo / brand renders', logoCount > 0 ? 'PASS' : 'FAIL');

      // 3. Main nav present. On mobile, the nav may be hidden behind a toggle —
      // we just assert the element exists in the DOM, not that it's visible.
      const navCount = await page.locator('nav.main-nav').count();
      record('home.nav', 'Main navigation present in DOM', navCount > 0 ? 'PASS' : 'FAIL');

      // 4. Mobile-only: hamburger or alternate-toggle present (mobile nav lives
      // behind a toggle in main.css). Skip for desktop/tablet.
      if (viewport === 'mobile') {
        const toggleCount = await page
          .locator('#mobile-toggle, .mobile-toggle, .navbar-toggler, [aria-label*="menu" i]')
          .count();
        record(
          'home.mobile-toggle',
          'Mobile nav toggle present',
          toggleCount > 0 ? 'PASS' : 'WARN',
          toggleCount > 0 ? '' : 'no toggle element matched — verify nav is reachable'
        );
      }

      // 5. Authenticated roles see the user/logout area; public sees a Login link.
      if (role === 'public') {
        // Login link should be reachable somewhere — direct link or in the nav.
        const loginLink = await page
          .locator('a[href*="/Account/Login" i], a:has-text("Sign In"), a:has-text("Login")')
          .count();
        record(
          'home.public-login-link',
          'Public users see a login affordance',
          loginLink > 0 ? 'PASS' : 'WARN',
          loginLink > 0 ? '' : 'no login link found — verify there is a sign-in path from the landing page'
        );
      } else {
        // Authenticated: logout form present.
        const logoutCount = await page.locator('#logoutForm, form[action*="LogoutPost" i]').count();
        record(
          'home.auth-logout',
          'Authenticated user has logout form',
          logoutCount > 0 ? 'PASS' : 'FAIL'
        );
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
