// QC #172 / DEF-194 — Bulk delete on Edit page, v2 (domcontentloaded-based).
// Same test plan as qc172-bulk-delete.js but avoids networkidle (Edit page on
// cert 26-0023 doesn't reach idle within 30s on dev).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const OUT = __dirname;
const BASE = 'http://localhost:5101';
const APP_USER = 'analyst';
const APP_PASS = 'TestPass123!';
const TEST_CERT_ID = 24;
const TEST_CERT_NUMBER = '26-0023';
const LARGE_CERT_NUMBER = '26-0106';

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? 'PASS' : status === 'FAIL' ? 'FAIL' : 'WARN';
  console.log(`[${tag}] ${id} ${name}${notes ? ' -- ' + notes : ''}`);
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
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  [pageerror]', e.message));

  console.log(`\nQC #172 v2 -- LOCAL (${BASE})\n`);

  try {
    // Defensive cleanup
    runSql(`DELETE FROM CertificateProductIds WHERE ProductIdValue LIKE 'QC172TEST%';`);

    // Login
    await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await page.locator('button[type="submit"]').first().click();
    await page.waitForURL(/\/(Home|Certificate|Dashboard)/i, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(1500);

    // SETUP
    runSql(
      `INSERT INTO CertificateProductIds (CertificateId, ProductIdType, ProductIdValue, ProductName, IsActive, Status, IsPublic, DisplayOrder, CreatedBy, CreatedAt, UpdatedAt, Partner) VALUES ` +
      `(${TEST_CERT_ID}, 'ASIN', 'QC172TEST1', 'QC172 Bulk Delete Test 1', 1, 'Active', 1, 999, 1, GETDATE(), GETDATE(), 'None'),` +
      `(${TEST_CERT_ID}, 'ASIN', 'QC172TEST2', 'QC172 Bulk Delete Test 2', 1, 'Active', 1, 999, 1, GETDATE(), GETDATE(), 'None'),` +
      `(${TEST_CERT_ID}, 'ASIN', 'QC172TEST3', 'QC172 Bulk Delete Test 3', 1, 'Active', 1, 999, 1, GETDATE(), GETDATE(), 'None');`
    );
    console.log(`SETUP: inserted 3 rows on ${TEST_CERT_NUMBER}`);

    // T-DISPLAY: open edit, wait for the table to exist (not networkidle).
    await page.goto(`${BASE}/Certificate/Edit/${TEST_CERT_NUMBER}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForSelector('#productIdsTable', { timeout: 30000 });
    await page.waitForSelector('#selectAllProductIds', { timeout: 15000 });
    // Give the Product ID list time to populate from /api/productids/certificate/24
    await page.waitForResponse(r => /\/api\/productids\/certificate\/\d+/.test(r.url()) && r.status() === 200, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(1500);

    // Filter to QC172TEST rows
    await page.locator('#searchProductIds').fill('QC172TEST');
    await page.waitForTimeout(1500);
    await page.locator('#productIdsTable').scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(OUT, 'qc172v2-01-filtered.png'), fullPage: true });

    const saCount = await page.locator('#selectAllProductIds').count();
    const bulkBtnVisible0 = await page.locator('#bulkDeleteProductIds').isVisible();
    record('QC172-A1', 'Select-all header checkbox present', saCount === 1 ? 'PASS' : 'FAIL', `count=${saCount}`);
    record('QC172-A2', 'Bulk delete button hidden before selection', !bulkBtnVisible0 ? 'PASS' : 'FAIL', `visible=${bulkBtnVisible0}`);

    const visibleRows = await page.locator('.bulk-select-checkbox').count();
    record('QC172-A3', 'Three filtered test rows visible', visibleRows === 3 ? 'PASS' : 'FAIL', `visible=${visibleRows}`);

    if (visibleRows < 3) {
      throw new Error(`expected 3 filtered rows, got ${visibleRows}; abort`);
    }

    // T-COUNT-1
    await page.locator('.bulk-select-checkbox').first().check();
    await page.waitForTimeout(400);
    const btn1Vis = await page.locator('#bulkDeleteProductIds').isVisible();
    const count1 = (await page.locator('#bulkDeleteCount').innerText()).trim();
    record('QC172-B1', 'Bulk delete button appears after 1 selection', btn1Vis ? 'PASS' : 'FAIL', `visible=${btn1Vis}`);
    record('QC172-B2', 'Count badge = 1', count1 === '1' ? 'PASS' : 'FAIL', `count=${count1}`);

    // T-COUNT-ALL
    await page.locator('#selectAllProductIds').check();
    await page.waitForTimeout(400);
    const count3 = (await page.locator('#bulkDeleteCount').innerText()).trim();
    record('QC172-B3', 'Select-all checks all 3 visible rows -> count=3', count3 === '3' ? 'PASS' : 'FAIL', `count=${count3}`);
    await page.screenshot({ path: path.join(OUT, 'qc172v2-02-all-selected.png'), fullPage: true });

    // T-DELETE
    await page.locator('#bulkDeleteProductIds').click();
    await page.waitForSelector('[data-action="confirm"]', { timeout: 5000 });
    await page.locator('[data-action="confirm"]').first().click();
    // Three parallel DELETEs + reload; settle.
    await page.waitForTimeout(4500);
    await page.screenshot({ path: path.join(OUT, 'qc172v2-03-after-delete.png'), fullPage: true });

    const remaining = parseInt(querySql(`SELECT COUNT(*) FROM CertificateProductIds WHERE ProductIdValue LIKE 'QC172TEST%' AND CertificateId = ${TEST_CERT_ID};`));
    record('QC172-C1', 'All 3 test rows deleted from DB', remaining === 0 ? 'PASS' : 'FAIL', `remaining=${remaining}`);

    // T-CROSS-PAGE
    console.log('\nT-CROSS-PAGE: selection persists across pagination');
    await page.goto(`${BASE}/Certificate/Edit/${LARGE_CERT_NUMBER}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForSelector('#productIdsTable', { timeout: 30000 });
    await page.waitForResponse(r => /\/api\/productids\/certificate\/\d+/.test(r.url()) && r.status() === 200, { timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(2000);
    await page.locator('#searchProductIds').fill('');
    await page.waitForTimeout(1200);

    const firstCheckbox = page.locator('.bulk-select-checkbox').first();
    await firstCheckbox.check();
    await page.waitForTimeout(300);
    const cp1 = (await page.locator('#bulkDeleteCount').innerText()).trim();
    record('QC172-D1', 'Page 1: one row selected -> count=1', cp1 === '1' ? 'PASS' : 'FAIL', `count=${cp1}`);

    const page2Btn = page.locator('.pagination-btn').filter({ hasText: /^2$/ }).first();
    if (await page2Btn.count() > 0) {
      await page2Btn.click();
      await page.waitForTimeout(2000);
      const cp2 = (await page.locator('#bulkDeleteCount').innerText()).trim();
      record('QC172-D2', 'Page 2: count persists (=1)', cp2 === '1' ? 'PASS' : 'FAIL', `count=${cp2}`);

      await page.locator('.bulk-select-checkbox').first().check();
      await page.waitForTimeout(300);
      const cp3 = (await page.locator('#bulkDeleteCount').innerText()).trim();
      record('QC172-D3', 'Page 2 + 1 more selected: count=2', cp3 === '2' ? 'PASS' : 'FAIL', `count=${cp3}`);
      await page.screenshot({ path: path.join(OUT, 'qc172v2-04-cross-page.png'), fullPage: true });

      const page1Btn = page.locator('.pagination-btn').filter({ hasText: /^1$/ }).first();
      if (await page1Btn.count() > 0) {
        await page1Btn.click();
        await page.waitForTimeout(2000);
        const firstChecked = await page.locator('.bulk-select-checkbox').first().isChecked();
        record('QC172-D4', 'Back on page 1: first row still checked', firstChecked ? 'PASS' : 'FAIL', `checked=${firstChecked}`);
      } else {
        record('QC172-D4', 'Page 1 nav button not found', 'WARN', '');
      }
    } else {
      record('QC172-D2', 'Pagination next-page button not found', 'WARN', 'cross-page test skipped');
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
    await page.screenshot({ path: path.join(OUT, 'qc172v2-error.png'), fullPage: true }).catch(() => {});
  } finally {
    try { runSql(`DELETE FROM CertificateProductIds WHERE ProductIdValue LIKE 'QC172TEST%';`); } catch {}
    fs.writeFileSync(path.join(OUT, 'qc172v2-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN (of ${results.length})`);
})();
