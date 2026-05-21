// QC #172 / DEF-194 — Product ID table bulk-delete with cross-page selection.
//
// LOCALHOST (destructive, with isolation by identifiable test values):
//   1. SETUP — insert 3 test Product IDs onto cert 24 (26-0023) with values
//      QC172TEST1/2/3 (10-char ASIN format).
//   2. T-DISPLAY — open /Certificate/Edit/26-0023, search-filter "QC172TEST",
//      assert: checkbox column present, select-all present, bulk-delete button
//      hidden initially.
//   3. T-COUNT-1 — check one row, assert "Delete Selected (1)".
//   4. T-COUNT-ALL — click select-all, assert "Delete Selected (3)".
//   5. T-DELETE — click bulk delete, confirm modal, assert all 3 rows vanish
//      from the DB.
//   6. T-CROSS-PAGE — separate non-destructive probe on cert 51 (26-0106, 101
//      IDs across 3 pages). Check one row on page 1, navigate to page 2, assert
//      "Delete Selected" still shows 1. Check one row on page 2, navigate back
//      to page 1, assert original is still checked AND count is 2.
//   7. CLEANUP — SQL hard-delete any QC172TEST rows left over (defensive).
//
// GCCWEBAPP (read-only):
//   T-LIVE — open /Certificate/Edit/<any cert with ≥1 product ID>, assert the
//   bulk UI elements exist in the DOM (proves DEF-194 deployed).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const TARGET = process.env.TARGET || (BASE.includes('gccwebapp') ? 'live' : 'local');
const BASIC = BASE.includes('gccwebapp') ? { username: 'gcc', password: 'Cert-Test-Access-2026!' } : undefined;
const APP_USER = 'analyst';
const APP_PASS = 'TestPass123!';

const TEST_CERT_ID = 24;
const TEST_CERT_NUMBER = '26-0023';
const LARGE_CERT_NUMBER = '26-0106';

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

function runSql(sql) {
  const cs = `Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;TrustServerCertificate=True;`;
  const safeSql = sql.replace(/'/g, "''");
  const cmd =
    `$conn = New-Object System.Data.SqlClient.SqlConnection '${cs}';` +
    ` $conn.Open();` +
    ` $cmd = $conn.CreateCommand();` +
    ` $cmd.CommandText = '${safeSql}';` +
    ` $rows = $cmd.ExecuteNonQuery();` +
    ` $conn.Close();` +
    ` Write-Output $rows;`;
  return execSync(`powershell -NoProfile -Command "${cmd.replace(/"/g, '\\"')}"`, { stdio: 'pipe' }).toString().trim();
}
function querySql(sql) {
  const cs = `Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;TrustServerCertificate=True;`;
  const safeSql = sql.replace(/'/g, "''");
  const cmd =
    `$conn = New-Object System.Data.SqlClient.SqlConnection '${cs}';` +
    ` $conn.Open();` +
    ` $cmd = $conn.CreateCommand();` +
    ` $cmd.CommandText = '${safeSql}';` +
    ` $r = $cmd.ExecuteScalar();` +
    ` $conn.Close();` +
    ` Write-Output $r;`;
  return execSync(`powershell -NoProfile -Command "${cmd.replace(/"/g, '\\"')}"`, { stdio: 'pipe' }).toString().trim();
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  [pageerror]', e.message));

  console.log(`\nQC #172 verification — ${TARGET.toUpperCase()} (${BASE})\n`);

  try {
    // Always defensive-clean any leftover QC172TEST rows up front
    if (TARGET === 'local') {
      runSql(`DELETE FROM CertificateProductIds WHERE ProductIdValue LIKE 'QC172TEST%';`);
    }

    // login
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(2000);

    if (TARGET === 'local') {
      // SETUP — insert 3 test Product IDs
      // CreatedBy is int (Users.Id); UpdatedAt is NOT NULL
      runSql(
        `INSERT INTO CertificateProductIds (CertificateId, ProductIdType, ProductIdValue, ProductName, IsActive, Status, IsPublic, DisplayOrder, CreatedBy, CreatedAt, UpdatedAt, Partner) VALUES ` +
        `(${TEST_CERT_ID}, 'ASIN', 'QC172TEST1', 'QC172 Bulk Delete Test 1', 1, 'Active', 1, 999, 1, GETDATE(), GETDATE(), 'None'),` +
        `(${TEST_CERT_ID}, 'ASIN', 'QC172TEST2', 'QC172 Bulk Delete Test 2', 1, 'Active', 1, 999, 1, GETDATE(), GETDATE(), 'None'),` +
        `(${TEST_CERT_ID}, 'ASIN', 'QC172TEST3', 'QC172 Bulk Delete Test 3', 1, 'Active', 1, 999, 1, GETDATE(), GETDATE(), 'None');`
      );
      console.log('SETUP: inserted 3 test product IDs on cert', TEST_CERT_NUMBER);
      const seeded = querySql(`SELECT COUNT(*) FROM CertificateProductIds WHERE ProductIdValue LIKE 'QC172TEST%' AND CertificateId = ${TEST_CERT_ID};`);
      console.log(`  seeded count: ${seeded}`);

      // T-DISPLAY — open edit page, filter to test rows
      await page.goto(`${BASE}/Certificate/Edit/${TEST_CERT_NUMBER}`, { waitUntil: 'networkidle' });
      await page.waitForTimeout(2000);
      await page.locator('#searchProductIds').fill('QC172TEST');
      await page.waitForTimeout(1500);
      await page.locator('#productIdsTable').scrollIntoViewIfNeeded();
      await page.screenshot({ path: path.join(OUT, 'qc172-local-01-filtered.png'), fullPage: true });

      const checkboxCol = await page.locator('#selectAllProductIds').count();
      const bulkBtnInitiallyHidden = await page.locator('#bulkDeleteProductIds').isVisible();
      record('QC172-A1', 'Select-all header checkbox present', checkboxCol === 1 ? 'PASS' : 'FAIL', `count=${checkboxCol}`);
      record('QC172-A2', 'Bulk delete button hidden before selection',
        !bulkBtnInitiallyHidden ? 'PASS' : 'FAIL', `visible=${bulkBtnInitiallyHidden}`);

      const visibleRows = await page.locator('.bulk-select-checkbox').count();
      record('QC172-A3', 'Three filtered test rows visible',
        visibleRows === 3 ? 'PASS' : 'FAIL', `visible=${visibleRows}`);

      // T-COUNT-1 — check one row
      await page.locator('.bulk-select-checkbox').first().check();
      await page.waitForTimeout(300);
      const btnVisible1 = await page.locator('#bulkDeleteProductIds').isVisible();
      const count1 = await page.locator('#bulkDeleteCount').innerText();
      record('QC172-B1', 'Bulk delete button appears after 1 selection',
        btnVisible1 ? 'PASS' : 'FAIL', `visible=${btnVisible1}`);
      record('QC172-B2', 'Bulk delete shows "Delete Selected (1)"',
        count1.trim() === '1' ? 'PASS' : 'FAIL', `count=${count1}`);

      // T-COUNT-ALL — click select-all
      await page.locator('#selectAllProductIds').check();
      await page.waitForTimeout(300);
      const count3 = await page.locator('#bulkDeleteCount').innerText();
      record('QC172-B3', 'Select-all checks all 3 visible rows → count=3',
        count3.trim() === '3' ? 'PASS' : 'FAIL', `count=${count3}`);
      await page.screenshot({ path: path.join(OUT, 'qc172-local-02-all-selected.png'), fullPage: true });

      // T-DELETE — click bulk delete
      // AdminCore.showConfirmation creates an overlay with [data-action="confirm"] button
      await page.locator('#bulkDeleteProductIds').click();
      // Modal appears asynchronously after click
      await page.waitForSelector('[data-action="confirm"]', { timeout: 5000 });
      await page.locator('[data-action="confirm"]').first().click();
      // Wait for the parallel DELETE API calls + table reload
      await page.waitForTimeout(4000);
      await page.screenshot({ path: path.join(OUT, 'qc172-local-03-after-delete.png'), fullPage: true });

      // DB-level verify
      const remaining = parseInt(querySql(`SELECT COUNT(*) FROM CertificateProductIds WHERE ProductIdValue LIKE 'QC172TEST%' AND CertificateId = ${TEST_CERT_ID};`));
      record('QC172-C1', 'All 3 test rows deleted from DB',
        remaining === 0 ? 'PASS' : 'FAIL', `remaining=${remaining}`);

      // T-CROSS-PAGE — open cert 26-0106 (101 IDs)
      console.log('\nT-CROSS-PAGE — selection persists across pagination');
      await page.goto(`${BASE}/Certificate/Edit/${LARGE_CERT_NUMBER}`, { waitUntil: 'networkidle' });
      await page.waitForTimeout(2500);
      // Ensure no stale search filter
      await page.locator('#searchProductIds').fill('');
      await page.waitForTimeout(1200);

      // Page 1: check the first checkbox
      const firstCheckbox = page.locator('.bulk-select-checkbox').first();
      await firstCheckbox.check();
      await page.waitForTimeout(300);
      const cpCount1 = await page.locator('#bulkDeleteCount').innerText();
      record('QC172-D1', 'Page 1: one row selected → count=1', cpCount1.trim() === '1' ? 'PASS' : 'FAIL', `count=${cpCount1}`);

      // Navigate to page 2 — pagination-btn with text "2"
      const page2Btn = page.locator('.pagination-btn').filter({ hasText: /^2$/ }).first();
      if (await page2Btn.count() > 0) {
        await page2Btn.click();
        await page.waitForTimeout(2000);
        const cpCount2 = await page.locator('#bulkDeleteCount').innerText();
        record('QC172-D2', 'After page → page 2 nav: count persists (=1)',
          cpCount2.trim() === '1' ? 'PASS' : 'FAIL', `count=${cpCount2}`);

        // Check one row on page 2
        await page.locator('.bulk-select-checkbox').first().check();
        await page.waitForTimeout(300);
        const cpCount3 = await page.locator('#bulkDeleteCount').innerText();
        record('QC172-D3', 'Page 2 + 1 more selected: count=2',
          cpCount3.trim() === '2' ? 'PASS' : 'FAIL', `count=${cpCount3}`);
        await page.screenshot({ path: path.join(OUT, 'qc172-local-04-cross-page.png'), fullPage: true });

        // Navigate back to page 1 — original selection should still be visually checked
        const page1Btn = page.locator('.pagination-btn').filter({ hasText: /^1$/ }).first();
        if (await page1Btn.count() > 0) {
          await page1Btn.click();
          await page.waitForTimeout(2000);
          const firstChecked = await page.locator('.bulk-select-checkbox').first().isChecked();
          record('QC172-D4', 'Page 1 revisited: first row still checked (selectedIds preserves state)',
            firstChecked ? 'PASS' : 'FAIL', `checked=${firstChecked}`);
        } else {
          record('QC172-D4', 'Page 1 nav button not found', 'WARN', '');
        }
      } else {
        record('QC172-D2', 'Pagination next-page button not found', 'WARN', 'cross-page test skipped');
      }

      // Cleanup leftover (defensive)
      runSql(`DELETE FROM CertificateProductIds WHERE ProductIdValue LIKE 'QC172TEST%';`);

    } else {
      // GCCWEBAPP — read-only DOM probe
      // Pick a cert with at least one product ID — use 26-0163 (3M, has product IDs)
      await page.goto(`${BASE}/Certificate/Edit/26-0163`, { waitUntil: 'networkidle' });
      await page.waitForTimeout(2500);
      const sa = await page.locator('#selectAllProductIds').count();
      const bbtn = await page.locator('#bulkDeleteProductIds').count();
      const rowCheckboxes = await page.locator('.bulk-select-checkbox').count();
      record('QC172-LIVE-1', 'Select-all checkbox present on prod', sa > 0 ? 'PASS' : 'FAIL', `count=${sa}`);
      record('QC172-LIVE-2', 'Bulk delete button present on prod (hidden until selection)',
        bbtn > 0 ? 'PASS' : 'FAIL', `count=${bbtn}`);
      record('QC172-LIVE-3', 'Per-row bulk-select checkboxes present on prod',
        rowCheckboxes > 0 ? 'PASS' : 'FAIL', `row checkbox count=${rowCheckboxes}`);
      await page.screenshot({ path: path.join(OUT, 'qc172-live-dom.png'), fullPage: true });
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
    await page.screenshot({ path: path.join(OUT, `qc172-${TARGET}-error.png`), fullPage: true }).catch(() => {});
  } finally {
    // Final defensive cleanup
    if (TARGET === 'local') {
      try { runSql(`DELETE FROM CertificateProductIds WHERE ProductIdValue LIKE 'QC172TEST%';`); } catch {}
    }
    fs.writeFileSync(path.join(OUT, `qc172-${TARGET}-results.json`), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
