// DEF-228 reproduction: Partner Amazon user does Export All to CSV; output
// is inspected for any cert that has an Internal tag attached.
//
// Setup: partner_test user (Amazon-mapped). Dev DB has cert 26-0001 with
// Status=Paid + Partner=Amazon + Internal tag "Sustainable".

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `def228-partner-export-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    acceptDownloads: true,
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log(`  pageerror: ${e.message}`));

  try {
    // Login as partner_test (Amazon-mapped)
    await page.goto(`${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', 'partner_test');
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([page.waitForLoadState('networkidle'), page.click('button[type="submit"]')]);

    console.log('Logged in. Current URL:', page.url());
    await page.screenshot({ path: path.join(OUT, '01-post-login.png'), fullPage: true });

    // Hit the export endpoint directly. Partner-side scope is enforced server-side.
    const cookies = await ctx.cookies();
    const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');

    const resp = await page.request.get(`${BASE}/Certificate/ExportCertificates?format=csv`, {
      headers: { Cookie: cookieHeader },
    });
    console.log('Export status:', resp.status());
    const csvBody = await resp.text();
    fs.writeFileSync(path.join(OUT, 'partner-export.csv'), csvBody);
    console.log('CSV bytes:', csvBody.length);

    // Parse + analyze
    const lines = csvBody.split(/\r?\n/).filter(l => l.length > 0);
    const header = lines[0];
    const rows = lines.slice(1);
    console.log('\nHeader:', header);
    console.log('Row count:', rows.length);
    console.log('\nRows:');
    rows.forEach((r, i) => console.log(`  ${i + 1}.`, r));

    // Check: does the CSV include cert 26-0001 (which has Internal tag 'Sustainable')?
    const has26_0001 = rows.some(r => r.includes('26-0001'));
    const hasSustainable = rows.some(r => /sustainable/i.test(r));
    const hasInternalTagColumn = /internal\s*tag/i.test(header);

    const findings = {
      exportStatus: resp.status(),
      rowCount: rows.length,
      headerColumns: header.split(','),
      includesInternalTagColumn: hasInternalTagColumn,
      includesCert_26_0001: has26_0001,
      anyRowContainsSustainableInternalTag: hasSustainable,
    };

    console.log('\n=== FINDINGS ===');
    console.log(JSON.stringify(findings, null, 2));
    fs.writeFileSync(path.join(OUT, 'findings.json'), JSON.stringify(findings, null, 2));

    // DEF-228 (corrected by Shawn 2026-06-01): the Internal *tag* should be excluded
    // from the export, NOT the certificate row itself. The cert remains in the export
    // when it qualifies by partner + status; the Internal Tag column/values are hidden
    // per DEF-128. This script's PASS criteria is therefore:
    //   - Internal Tag column absent from header (DEF-128)
    //   - "Sustainable" (the internal tag attached to cert 26-0001) absent from any row
    //   - Cert 26-0001 itself PRESENT (correctly in scope by partner + status)
    const passes =
      !hasInternalTagColumn &&
      !hasSustainable &&
      has26_0001;
    console.log('\n=== DEF-128 + DEF-228 working-as-designed check ===');
    console.log(`  Internal Tag column hidden:      ${!hasInternalTagColumn ? 'PASS' : 'FAIL'}`);
    console.log(`  "Sustainable" tag value hidden:  ${!hasSustainable ? 'PASS' : 'FAIL'}`);
    console.log(`  Cert 26-0001 row present:        ${has26_0001 ? 'PASS' : 'FAIL'}`);
    console.log(`  Overall:                         ${passes ? 'PASS' : 'FAIL'}`);

  } catch (e) {
    console.error('FAIL:', e.message);
    await page.screenshot({ path: path.join(OUT, '99-error.png'), fullPage: true }).catch(() => {});
  } finally {
    console.log('\nOutput:', OUT);
    await browser.close();
  }
})();
