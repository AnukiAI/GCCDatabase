// Per-view smoke — /Certificate/Edit/{id} (Tier 1 #7).
//
// Access matrix: public -> 401, external -> 403, analyst/admin -> 200.
//
// The cert id is discovered at runtime from the public tiles API so the smoke does
// not depend on a hardcoded seed row. Falls back to id 46 (a known seeded public cert
// on localhost) if discovery fails.
//
// Edit-specific assertions (form is pre-populated — DEF-225 history):
//   - #certificateEditForm present.
//   - #Number readonly input has a non-empty value (cert number shown).
//   - #CompanyId has a selected (non-empty) value.
//   - #ProductId present with a selected value (Product Name shows saved value — DEF-225).
//   - #CertificationResults present (CR-001) and .product-manager (Product ID Manager).
//
// Run:  node Scripts/smoke-prod/_views/cert-edit.js

const { runViewSmoke, BASE } = require('../_lib/smoke-template');

async function discoverCertId() {
  try {
    const resp = await fetch(`${BASE}/api/certificate-tiles?page=1&pageSize=1`);
    const json = await resp.json();
    const id = json && json.certificates && json.certificates[0] && json.certificates[0].id;
    if (id) return id;
  } catch { /* fall through to default */ }
  return 46;
}

async function run() {
  const certId = await discoverCertId();
  console.log(`[smoke] cert-edit using certificate id=${certId}`);

  return runViewSmoke({
    viewId: 'cert-edit',
    url: `/Certificate/Edit/${certId}`,
    roles: ['public', 'external', 'analyst', 'admin'],
    viewports: ['desktop', 'tablet', 'mobile'],
    expectedAccess: { public: 401, external: 403, analyst: 200, admin: 200 },
    assertions: async ({ page, record }) => {
      record('cert-edit.form', 'Edit form present',
        (await page.locator('#certificateEditForm').count()) > 0 ? 'PASS' : 'FAIL');

      // Form pre-populated: cert number readonly input has a value.
      const number = await page.locator('#Number').inputValue().catch(() => '');
      record('cert-edit.number-populated', 'Certificate # pre-populated',
        number && number.trim() ? 'PASS' : 'FAIL', `number="${number}"`);

      // Company pre-selected.
      const companyVal = await page.locator('#CompanyId').inputValue().catch(() => '');
      record('cert-edit.company-populated', 'Company pre-selected',
        companyVal && companyVal.trim() ? 'PASS' : 'FAIL', `companyId="${companyVal}"`);

      // DEF-225: Product Name shows the saved value.
      const productPresent = await page.locator('#ProductId').count();
      const productVal = await page.locator('#ProductId').inputValue().catch(() => '');
      record('cert-edit.product-populated', 'Product Name shows saved value (DEF-225)',
        productPresent > 0 && productVal && productVal.trim() ? 'PASS' : 'FAIL',
        `productId="${productVal}"`);

      record('cert-edit.cert-results', 'Certification Results field present (CR-001)',
        (await page.locator('#CertificationResults').count()) > 0 ? 'PASS' : 'FAIL');

      record('cert-edit.product-id-manager', 'Product ID Manager renders',
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
