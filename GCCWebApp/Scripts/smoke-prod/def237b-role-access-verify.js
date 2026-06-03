// DEF-237 / QC #106+#148 — role-based Company Access visibility (client 5/15 feedback).
//
// Verifies the behavior the client said was "never addressed":
//   R0  Add New User (no role) -> Company Access, Partner, and note all HIDDEN
//   R1  External role  -> Company Access SHOWN, Partner hidden, note hidden
//   R2  Partner role   -> Partner SHOWN, Company Access hidden, note hidden
//   R3  Admin role     -> Company Access hidden, Partner hidden, NOTE shown
//   R4  Analyst role   -> Company Access hidden, Partner hidden, NOTE shown
//   T1  Table: an Admin/Analyst user shows "All" in the company column (not blank)
//
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/def237b-role-access-verify.js
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

const vis = (page, sel) => page.evaluate((s) => {
  const el = document.getElementById(s);
  if (!el) return 'MISSING';
  return getComputedStyle(el).display === 'none' ? 'hidden' : 'shown';
}, sel);

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(800);

    // Open Add New User
    await page.locator('button:has-text("Add User"), button:has-text("Add New User"), #add-user-btn, [onclick*="showAddUserModal"]').first().click();
    await page.waitForTimeout(500);

    // R0: nothing role-specific until a role is chosen
    rec('R0', 'No role -> all hidden',
      (await vis(page, 'company-group')) === 'hidden' && (await vis(page, 'partner-group')) === 'hidden' && (await vis(page, 'role-access-note')) === 'hidden'
        ? 'PASS' : 'FAIL',
      `company=${await vis(page,'company-group')}, partner=${await vis(page,'partner-group')}, note=${await vis(page,'role-access-note')}`);

    const setRole = async (label) => {
      await page.evaluate((r) => window.userManagement.handleRoleChange(r), label);
      await page.waitForTimeout(150);
    };

    // R1 External
    await setRole('External');
    rec('R1', 'External -> Company Access shown, note hidden',
      (await vis(page,'company-group'))==='shown' && (await vis(page,'partner-group'))==='hidden' && (await vis(page,'role-access-note'))==='hidden' ? 'PASS':'FAIL',
      `company=${await vis(page,'company-group')}, partner=${await vis(page,'partner-group')}, note=${await vis(page,'role-access-note')}`);

    // R2 Partner
    await setRole('Partner');
    rec('R2', 'Partner -> Partner shown, Company hidden, note hidden',
      (await vis(page,'partner-group'))==='shown' && (await vis(page,'company-group'))==='hidden' && (await vis(page,'role-access-note'))==='hidden' ? 'PASS':'FAIL',
      `company=${await vis(page,'company-group')}, partner=${await vis(page,'partner-group')}, note=${await vis(page,'role-access-note')}`);

    // R3 Admin
    await setRole('Admin');
    rec('R3', 'Admin -> note shown, Company + Partner hidden',
      (await vis(page,'role-access-note'))==='shown' && (await vis(page,'company-group'))==='hidden' && (await vis(page,'partner-group'))==='hidden' ? 'PASS':'FAIL',
      `company=${await vis(page,'company-group')}, partner=${await vis(page,'partner-group')}, note=${await vis(page,'role-access-note')}`);
    await page.screenshot({ path: path.join(OUT, 'def237b-admin-note.png'), fullPage: true });

    // R4 Analyst
    await setRole('Analyst');
    rec('R4', 'Analyst -> note shown, Company + Partner hidden',
      (await vis(page,'role-access-note'))==='shown' && (await vis(page,'company-group'))==='hidden' && (await vis(page,'partner-group'))==='hidden' ? 'PASS':'FAIL',
      `company=${await vis(page,'company-group')}, partner=${await vis(page,'partner-group')}, note=${await vis(page,'role-access-note')}`);

    // close modal
    await page.locator('.admin-modal-close, button:has-text("Cancel")').first().click().catch(()=>{});
    await page.waitForTimeout(300);

    // T1: table shows "All" for admin/analyst rows
    const adminCellInfo = await page.evaluate(() => {
      const um = window.userManagement;
      if (!um || !um.users) return null;
      const u = um.users.find(x => ['admin','analyst'].includes((x.role||'').toLowerCase()));
      if (!u) return { found: false };
      return { found: true, role: u.role, display: um.getCompanyAccessDisplay(u) };
    });
    if (adminCellInfo && adminCellInfo.found) {
      rec('T1', 'Admin/Analyst company column shows "All"', adminCellInfo.display === 'All' ? 'PASS' : 'FAIL',
        `role=${adminCellInfo.role}, display="${adminCellInfo.display}"`);
    } else {
      rec('T1', 'Admin/Analyst user present to check', 'WARN', 'no admin/analyst user found in table data');
    }

  } catch (err) {
    rec('ERR', 'Script error', 'FAIL', err.message);
  } finally {
    const pass = results.filter(r => r.status==='PASS').length;
    const fail = results.filter(r => r.status==='FAIL').length;
    const warn = results.filter(r => r.status==='WARN').length;
    console.log(`\n=== DEF-237b role-access verify: ${pass} PASS / ${fail} FAIL / ${warn} WARN ===`);
    fs.writeFileSync(path.join(OUT,'def237b-results.json'), JSON.stringify({pass,fail,warn,results},null,2));
    await browser.close();
    process.exit(fail>0?1:0);
  }
})();
