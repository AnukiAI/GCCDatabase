// Per-view smoke — /Certificate/Create (Tier 1 #6).
//
// Access matrix: public -> 401, external -> 403, analyst/admin -> 200.
//
// Form structure (certificateCreateForm). Note: SearchableSelect hides some native
// <select>s and renders a custom widget, but the original element stays in the DOM for
// form submit — so we assert DOM presence (count > 0), not visibility.
//   - Core dropdowns: #CompanyId, #CertificationTypeId, #Partner, #PathwayId.
//   - #CertificationResults textarea — CR-001 ("Certification Results" optional field).
//   - .product-manager — the Product ID Manager partial.
//   - >=3 form section headers (Certificate Information / PDF / Product Identifiers / ...).
//
// Run:  node Scripts/smoke-prod/_views/cert-create.js

const { runViewSmoke } = require('../_lib/smoke-template');

async function run() {
  return runViewSmoke({
    viewId: 'cert-create',
    url: '/Certificate/Create',
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 401, external: 403, analyst: 200, admin: 200 },
    assertions: async ({ page, record }) => {
      record('cert-create.form', 'Create form present',
        (await page.locator('#certificateCreateForm').count()) > 0 ? 'PASS' : 'FAIL');

      const sections = await page.locator('.form-section-header').count();
      record('cert-create.sections', 'Form section headers render (>=3)',
        sections >= 3 ? 'PASS' : 'FAIL', `sections=${sections}`);

      for (const [sel, label] of [
        ['#CompanyId', 'Company'],
        ['#CertificationTypeId', 'Certification Type'],
        ['#Partner', 'Partner'],
        ['#PathwayId', 'Pathway'],
      ]) {
        record(`cert-create.dropdown${sel}`, `${label} dropdown present`,
          (await page.locator(sel).count()) > 0 ? 'PASS' : 'FAIL');
      }

      // CR-001: optional "Certification Results" field exists.
      record('cert-create.cert-results', 'Certification Results field present (CR-001)',
        (await page.locator('#CertificationResults').count()) > 0 ? 'PASS' : 'FAIL');

      // Product ID Manager renders.
      record('cert-create.product-id-manager', 'Product ID Manager renders',
        (await page.locator('.product-manager').count()) > 0 ? 'PASS' : 'FAIL');
    },
  });
}

module.exports = { run };

if (require.main === module) {
  run().then((r) => {
    process.exitCode = r.fail === 0 ? 0 : 1;
  });
}
