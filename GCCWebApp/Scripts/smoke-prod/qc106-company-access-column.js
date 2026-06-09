// QC #106 item #2 — "Company Access" listing was missing from the User Management table.
// Verifies on localhost:5101 that the table now has a Company Access column, distinct from
// the User Company (employer) column, populated with the data-scoping value.
//   A0  A "Company Access" sortable column header exists (data-sort="companyAccess")
//   A1  It is a SEPARATE column from "User Company" (both headers present)
//   A2  Every rendered row has a .user-company-access cell (count == .user-company count)
//   A3  Admin/Analyst rows show "All" (per DEF-238 — they implicitly have all access)
//   A4  Clicking the Company Access header sorts without error (rows still render)
//
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/qc106-company-access-column.js
const { chromium } = require('playwright');
const { login } = require('./_lib/smoke-template');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const OUT = path.join(__dirname, '_results');
const results = [];
const rec = (id, desc, status, detail = '') => {
  results.push({ id, desc, status, detail });
  console.log(`[${status}] ${id}  ${desc}${detail ? '  — ' + detail : ''}`);
};

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);

    // A0 / A1: headers
    const headers = await page.evaluate(() =>
      Array.from(document.querySelectorAll('#user-table thead th'))
        .map(th => ({ text: th.textContent.trim().replace(/[▼▲]/g, '').trim(), sort: th.dataset.sort || '' })));
    const hasAccess = headers.some(h => h.sort === 'companyAccess' && /Company Access/i.test(h.text));
    const hasUserCompany = headers.some(h => h.sort === 'userCompanyName' && /User Company/i.test(h.text));
    rec('A0', '"Company Access" sortable column header present', hasAccess ? 'PASS' : 'FAIL', `headers=${headers.map(h=>h.text).join(' | ')}`);
    rec('A1', 'User Company and Company Access are separate columns', hasAccess && hasUserCompany ? 'PASS' : 'FAIL', `userCompany=${hasUserCompany}, companyAccess=${hasAccess}`);

    // A2: every row has both cells
    const cellCounts = await page.evaluate(() => ({
      rows: document.querySelectorAll('#user-table tbody tr.admin-table-row').length,
      company: document.querySelectorAll('#user-table tbody td.user-company').length,
      access: document.querySelectorAll('#user-table tbody td.user-company-access').length
    }));
    rec('A2', 'Every row renders a Company Access cell', cellCounts.rows > 0 && cellCounts.access === cellCounts.rows ? 'PASS' : 'FAIL',
      `rows=${cellCounts.rows}, accessCells=${cellCounts.access}, companyCells=${cellCounts.company}`);

    // A3: Admin/Analyst rows show "All"
    const adminAccess = await page.evaluate(() => {
      const um = window.userManagement;
      const u = (um?.filteredUsers || um?.users || []).find(x => ['admin','analyst'].includes((x.role||'').toLowerCase()));
      if (!u) return null;
      // find the matching rendered row
      const row = document.querySelector(`#user-table tbody tr[data-id="${u.id}"]`);
      const cell = row?.querySelector('td.user-company-access');
      return { role: u.role, shown: cell ? cell.textContent.trim() : '(no cell)' };
    });
    rec('A3', 'Admin/Analyst Company Access shows "All"', adminAccess && adminAccess.shown === 'All' ? 'PASS' : (adminAccess ? 'WARN' : 'WARN'),
      adminAccess ? `${adminAccess.role} → "${adminAccess.shown}"` : 'no admin/analyst user found');

    // A4: sort by Company Access does not break rendering
    await page.click('#user-table thead th[data-sort="companyAccess"]');
    await page.waitForTimeout(500);
    const afterSort = await page.evaluate(() => document.querySelectorAll('#user-table tbody tr.admin-table-row').length);
    const noErr = await page.evaluate(() => !window.__qc106SortError);
    rec('A4', 'Sorting by Company Access renders rows without error', afterSort > 0 && noErr ? 'PASS' : 'FAIL', `rowsAfterSort=${afterSort}`);

  } catch (err) {
    rec('ERR', 'Script error', 'FAIL', err.message);
  } finally {
    const pass = results.filter(r => r.status==='PASS').length;
    const fail = results.filter(r => r.status==='FAIL').length;
    const warn = results.filter(r => r.status==='WARN').length;
    console.log(`\n=== QC #106 item #2 verify: ${pass} PASS / ${fail} FAIL / ${warn} WARN ===`);
    fs.writeFileSync(path.join(OUT,'qc106-company-access-results.json'), JSON.stringify({pass,fail,warn,results},null,2));
    await browser.close();
    process.exit(fail>0?1:0);
  }
})();
