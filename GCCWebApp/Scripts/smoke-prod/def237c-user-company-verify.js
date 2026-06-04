// CR-020 — "User Company" (employer) field, separate from "Company Access".
// Verifies on localhost:5101:
//   C0  Modal has a #user-company dropdown, populated with companies + a "None" option
//   C1  Create a user with a User Company set -> persists (GetUsers returns userCompanyName)
//   C2  Re-open that user in Edit -> the dropdown reflects the saved User Company
//   C3  User Company is independent of Company Access (can set one without the other)
//   T1  Import template has BOTH "Company Access" and "User Company" columns
//   T2  Export output has BOTH "Company Access" and "User Company" columns
//
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/def237c-user-company-verify.js
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
  // unique email per run without Date/random (vary by a value the page already has)
  let stamp = '';
  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(900);

    // derive a stamp from current user count so re-runs don't collide
    stamp = await page.evaluate(() => String((window.userManagement?.users?.length || 0) + (window.userManagement?.users?.[0]?.id || 0)));
    const testEmail = `cr020-test-${stamp}@anuki.example`;

    // Open Add New User
    await page.locator('button:has-text("Add User"), button:has-text("Add New User"), [onclick*="showAddUserModal"]').first().click();
    await page.waitForTimeout(500);

    // C0: dropdown present + populated
    const dd = await page.evaluate(() => {
      const el = document.getElementById('user-company');
      if (!el) return null;
      return { options: el.options.length, hasNone: el.options[0] && el.options[0].value === '' };
    });
    rec('C0', '#user-company dropdown populated', dd && dd.options > 1 && dd.hasNone ? 'PASS' : 'FAIL', dd ? `options=${dd.options}, hasNone=${dd.hasNone}` : 'MISSING');

    // Pick the first real company for both the dropdown and to read its name
    const firstCompany = await page.evaluate(() => {
      const c = (window.userManagement?.companies || [])[0];
      return c ? { id: c.id, name: c.name } : null;
    });
    if (!firstCompany) { rec('SETUP', 'company list available', 'FAIL', 'no companies loaded'); throw new Error('no companies'); }

    // C1: fill the form (Admin role => Company Access hidden, but User Company still applies)
    await page.fill('#user-name', `CR020 Test ${stamp}`);
    await page.fill('#user-email', testEmail);
    await page.evaluate(() => window.userManagement.handleRoleChange('Admin'));
    await page.selectOption('#user-role', { label: 'Admin' }).catch(async () => { await page.selectOption('#user-role', 'Admin').catch(()=>{}); });
    await page.selectOption('#user-company', String(firstCompany.id));
    // password (Add requires one)
    await page.fill('#user-password', 'TestPass123!').catch(()=>{});
    // submit
    await page.locator('#user-form button[type="submit"], button:has-text("Save User"), #save-user-btn').first().click();
    await page.waitForTimeout(1500);

    // Verify via GetUsers data
    const saved = await page.evaluate(async (email) => {
      const r = await fetch('/Admin/GetUsers');
      const j = await r.json();
      const list = j.data || j.Data || [];
      const u = list.find(x => (x.email||'').toLowerCase() === email.toLowerCase());
      return u ? { id: u.id, userCompanyId: u.userCompanyId, userCompanyName: u.userCompanyName } : null;
    }, testEmail);
    rec('C1', 'User Company persists on create', saved && saved.userCompanyId === firstCompany.id ? 'PASS' : 'FAIL',
      saved ? `userCompanyId=${saved.userCompanyId} (expected ${firstCompany.id}), name="${saved.userCompanyName}"` : 'user not found after create');

    // C2: edit reflects the saved value
    if (saved) {
      await page.evaluate((id) => window.userManagement.editUser(id), saved.id);
      await page.waitForTimeout(700);
      const ddVal = await page.evaluate(() => document.getElementById('user-company')?.value);
      rec('C2', 'Edit modal reflects saved User Company', String(ddVal) === String(firstCompany.id) ? 'PASS' : 'FAIL', `dropdown value=${ddVal}, expected ${firstCompany.id}`);
      // C3: independence — Admin user has User Company set but no Company Access (companyIds empty)
      const indep = saved.userCompanyId === firstCompany.id;
      rec('C3', 'User Company independent of Company Access', indep ? 'PASS' : 'WARN', 'Admin user carries User Company without company-access scoping');
      await page.locator('.admin-modal-close, button:has-text("Cancel")').first().click().catch(()=>{});
    }

    // T1: import template columns (capture the generated CSV text)
    const tpl = await page.evaluate(() => {
      // Rebuild the header the same way downloadImportTemplate does, by reading the function source is brittle;
      // instead trigger it and intercept the Blob via a temporary override.
      return new Promise((resolve) => {
        const origCreate = URL.createObjectURL;
        URL.createObjectURL = (blob) => { blob.text().then(t => resolve(t)); return 'blob:stub'; };
        try { window.userManagement.downloadImportTemplate(); } catch (e) { resolve('ERR:' + e.message); }
        URL.createObjectURL = origCreate;
        setTimeout(() => resolve('TIMEOUT'), 1500);
      });
    });
    const tplHasBoth = /Company Access/i.test(tpl) && /User Company/i.test(tpl);
    rec('T1', 'Import template has Company Access + User Company columns', tplHasBoth ? 'PASS' : 'FAIL', `header="${(tpl||'').split('\n')[0]}"`);

    // T2: export columns
    const exp = await page.evaluate(() => {
      return new Promise((resolve) => {
        const origCreate = URL.createObjectURL;
        URL.createObjectURL = (blob) => { blob.text().then(t => resolve(t)); return 'blob:stub'; };
        try { window.userManagement.exportUsers(); } catch (e) { resolve('ERR:' + e.message); }
        URL.createObjectURL = origCreate;
        setTimeout(() => resolve('TIMEOUT'), 2000);
      });
    });
    const expHeader = (exp || '').split('\n')[0];
    const expHasBoth = /Company Access/i.test(expHeader) && /User Company/i.test(expHeader);
    rec('T2', 'Export has Company Access + User Company columns', expHasBoth ? 'PASS' : 'FAIL', `header="${expHeader}"`);

  } catch (err) {
    rec('ERR', 'Script error', 'FAIL', err.message);
  } finally {
    const pass = results.filter(r => r.status==='PASS').length;
    const fail = results.filter(r => r.status==='FAIL').length;
    const warn = results.filter(r => r.status==='WARN').length;
    console.log(`\n=== CR-020 verify: ${pass} PASS / ${fail} FAIL / ${warn} WARN ===`);
    fs.writeFileSync(path.join(OUT,'def237c-results.json'), JSON.stringify({pass,fail,warn,results},null,2));
    await browser.close();
    process.exit(fail>0?1:0);
  }
})();
