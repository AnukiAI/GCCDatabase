// Per-view smoke — /Account/Login (Tier 1 #2).
//
// Login uses `Layout = null` (its own minimal HTML doc), so the shared nav/footer
// selectors do NOT apply here. Assertions are login-page-specific:
//   1. Email + Password fields present and wired (#Email / #Password).
//   2. A submit button exists.
//   3. Exactly ONE <h1> (DEF-104 history — login once had a duplicate-h1 regression).
//   4. No horizontal scroll on mobile (handled by the template).
//
// Only the Public persona is exercised — an authenticated session redirects away
// from /Account/Login, so testing logged-in roles here is meaningless.
//
// Run:  node Scripts/smoke-prod/_views/login.js

const { runViewSmoke } = require('../_lib/smoke-template');

async function run() {
  return runViewSmoke({
    viewId: 'login',
    url: '/Account/Login',
    roles: ['public'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 200 },
    assertions: async ({ page, record }) => {
      const email = await page.locator('#Email').count();
      record('login.email-field', 'Email input present', email > 0 ? 'PASS' : 'FAIL');

      const pwd = await page.locator('#Password').count();
      record('login.password-field', 'Password input present', pwd > 0 ? 'PASS' : 'FAIL');

      const submit = await page.locator('button[type="submit"]').count();
      record('login.submit-button', 'Submit button present', submit > 0 ? 'PASS' : 'FAIL');

      // DEF-104: exactly one <h1>. The login page has one h1 ("GreenCircle Certified
      // Database") and an h2 ("Welcome Back"); more than one h1 is the regression.
      const h1Count = await page.locator('h1').count();
      record(
        'login.single-h1',
        'Exactly one <h1> on the page',
        h1Count === 1 ? 'PASS' : 'FAIL',
        `h1 count=${h1Count}`
      );

      const container = await page.locator('.login-container').count();
      record('login.container', 'Login container renders', container > 0 ? 'PASS' : 'FAIL');
    },
  });
}

module.exports = { run };

if (require.main === module) {
  run().then((r) => {
    process.exitCode = r.fail === 0 ? 0 : 1;
  });
}
