// Per-view smoke — /Certificate/Edit/{id} (Tier 1 #7).
//
// Access matrix: public -> 401, external -> 403, analyst/admin -> 200.
//
// The Edit route is keyed by certificate NUMBER, not numeric id, via a custom route in
// Program.cs:
//     pattern:     "Certificate/Edit/{number}"
//     constraint:  number = ^[0-9]+-[0-9A-Za-z]+$   (e.g. "26-0025")
// A numeric id (or a number like "EXT-NP-002" that fails the regex) falls through to the
// default route and yields HTTP 400. So we discover a certificate NUMBER matching the
// route constraint from the public tiles API, and build the URL the way the app does
// (window.location.href = `/Certificate/Edit/${encodeURIComponent(certificateNumber)}`).
// Falls back to "26-0025" (a known seeded public cert on localhost) if discovery fails.
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

// The Edit route only accepts numbers matching this constraint (Program.cs).
const EDIT_NUMBER_RX = /^[0-9]+-[0-9A-Za-z]+$/;

async function discoverCertNumber() {
  try {
    const resp = await fetch(`${BASE}/api/certificate-tiles?page=1&pageSize=100`);
    const json = await resp.json();
    const certs = (json && json.certificates) || [];
    const match = certs.map((c) => c.certificateNumber).find((n) => n && EDIT_NUMBER_RX.test(n));
    if (match) return match;
  } catch { /* fall through to default */ }
  return '26-0025';
}

async function run() {
  const certNumber = await discoverCertNumber();
  console.log(`[smoke] cert-edit using certificate number=${certNumber}`);

  return runViewSmoke({
    viewId: 'cert-edit',
    url: `/Certificate/Edit/${encodeURIComponent(certNumber)}`,
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

      // DEF-225: Product Name shows the saved value. The Edit view renders #ProductId
      // with only a placeholder; certificate-edit's loadProductsForCompany() fetches the
      // company's products and re-selects savedProductId AFTER an async call. We must
      // distinguish two failure modes:
      //   - savedProductId IS among the loaded options but not selected  -> real DEF-225
      //     re-selection regression (FAIL).
      //   - savedProductId is NOT among the loaded options                -> the cert
      //     points to a product that isn't under its company (data inconsistency, a
      //     data-ops issue, not a code defect)                            -> WARN + flag.
      // Wait (bounded) for the cascade to settle, then inspect the select state. Re-selection
      // of savedProductId happens a tick AFTER the options populate (loadProductsForCompany
      // sets el.value once the async fetch resolves), so waiting only for options.length>1
      // races the assignment and reads value="" too early (intermittent false FAIL). Wait for
      // the value to actually equal saved; only if it never settles is it a real regression.
      try {
        await page.waitForFunction(
          () => {
            const el = document.querySelector('#ProductId');
            if (!el || !el.options || el.options.length <= 1) return false; // options not loaded yet
            const saved = (document.documentElement.innerHTML.match(/savedProductId\s*=\s*'([^']*)'/) || [])[1] || '';
            if (!saved) return true;                       // nothing to re-select
            const inOptions = Array.from(el.options).some((o) => String(o.value) === saved);
            if (!inOptions) return true;                   // saved not in list — data-ops case, decide below
            return String(el.value) === saved;             // re-selection applied
          },
          { timeout: 8000 }
        );
      } catch { /* never settled — handled below as a real FAIL */ }

      const prod = await page.evaluate(() => {
        const el = document.querySelector('#ProductId');
        const saved = (document.documentElement.innerHTML.match(/savedProductId\s*=\s*'([^']*)'/) || [])[1] || '';
        const optionValues = el ? Array.from(el.options).map((o) => String(o.value)) : [];
        return { exists: !!el, value: el ? String(el.value) : '', saved, savedInOptions: optionValues.includes(saved), optionCount: optionValues.length };
      });

      let prodStatus;
      let prodNote;
      if (!prod.exists) {
        prodStatus = 'FAIL';
        prodNote = '#ProductId not found';
      } else if (prod.value && prod.value === prod.saved) {
        prodStatus = 'PASS';
        prodNote = `productId="${prod.value}" matches saved`;
      } else if (prod.saved && !prod.savedInOptions) {
        // Saved product is not in the company's product list — bad-data row, not a code bug.
        prodStatus = 'WARN';
        prodNote = `saved product ${prod.saved} not in company product list (data inconsistency — flag for data ops)`;
      } else {
        // Saved product IS available but wasn't re-selected — genuine DEF-225 regression.
        prodStatus = 'FAIL';
        prodNote = `saved=${prod.saved} present in options but value="${prod.value}" (re-selection regression)`;
      }
      record('cert-edit.product-populated', 'Product Name shows saved value (DEF-225)', prodStatus, prodNote);

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
