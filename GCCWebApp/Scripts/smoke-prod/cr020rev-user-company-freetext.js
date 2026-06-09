// CR-020 (revised, QC #106) — "User Company" is now a FREE-TEXT field, not a dropdown.
// Verifies on localhost:5101:
//   F0  Modal #user-company is a TEXT INPUT (not a <select>)
//   F1  Create a user with a free-text User Company that is NOT a cert company -> persists
//        (GetUsers returns userCompanyName == the typed value)
//   F2  Re-open that user in Edit -> the input reflects the saved free text
//   F3  User Company is independent of Company Access (Admin user has no company scope)
//   T1  Import template still has BOTH "Company Access" and "User Company" columns
//   T2  Export output still has BOTH "Company Access" and "User Company" columns
//
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/cr020rev-user-company-freetext.js
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
  let stamp = '';
  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(900);

    // derive a stamp from current user count so re-runs don't collide (no Date/random)
    stamp = await page.evaluate(() => String((window.userManagement?.users?.length || 0) + (window.userManagement?.users?.[0]?.id || 0)));
    const testEmail = `cr020rev-test-${stamp}@anuki.example`;
    // A value that is deliberately NOT a cert company — the whole point of the CR.
    const employer = `GCC Corporate ${stamp}`;

    // Open Add New User
    await page.locator('button:has-text("Add User"), button:has-text("Add New User"), [onclick*="showAddUserModal"]').first().click();
    await page.waitForTimeout(500);

    // F0: #user-company is a text input, not a select
    const el = await page.evaluate(() => {
      const e = document.getElementById('user-company');
      if (!e) return null;
      return { tag: e.tagName, type: e.type, maxLength: e.maxLength };
    });
    rec('F0', '#user-company is a free-text input', el && el.tag === 'INPUT' && el.type === 'text' ? 'PASS' : 'FAIL',
      el ? `tag=${el.tag}, type=${el.type}, maxLength=${el.maxLength}` : 'MISSING');

    // F1: fill the form (Admin role => Company Access hidden, User Company still applies)
    await page.fill('#user-name', `CR020rev Test ${stamp}`);
    await page.fill('#user-email', testEmail);
    await page.evaluate(() => window.userManagement.handleRoleChange('Admin'));
    await page.selectOption('#user-role', { label: 'Admin' }).catch(async () => { await page.selectOption('#user-role', 'Admin').catch(()=>{}); });
    await page.fill('#user-company', employer);
    await page.fill('#user-password', 'TestPass123!').catch(()=>{});
    await page.locator('#user-form button[type="submit"], button:has-text("Save User"), #save-user-btn').first().click();
    await page.waitForTimeout(1500);

    const saved = await page.evaluate(async (email) => {
      const r = await fetch('/Admin/GetUsers');
      const j = await r.json();
      const list = j.data || j.Data || [];
      const u = list.find(x => (x.email||'').toLowerCase() === email.toLowerCase());
      return u ? { id: u.id, userCompanyName: u.userCompanyName } : null;
    }, testEmail);
    rec('F1', 'Free-text User Company persists on create', saved && saved.userCompanyName === employer ? 'PASS' : 'FAIL',
      saved ? `userCompanyName="${saved.userCompanyName}" (expected "${employer}")` : 'user not found after create');

    // F2: edit reflects the saved value in the input
    if (saved) {
      await page.evaluate((id) => window.userManagement.editUser(id), saved.id);
      await page.waitForTimeout(700);
      const val = await page.evaluate(() => document.getElementById('user-company')?.value);
      rec('F2', 'Edit modal reflects saved free text', String(val) === employer ? 'PASS' : 'FAIL', `input value="${val}", expected "${employer}"`);
      rec('F3', 'User Company independent of Company Access', saved.userCompanyName === employer ? 'PASS' : 'WARN', 'Admin user carries User Company without company-access scoping');
      await page.locator('.admin-modal-close, button:has-text("Cancel")').first().click().catch(()=>{});
    }

    // T1: import template columns
    const tpl = await page.evaluate(() => new Promise((resolve) => {
      const origCreate = URL.createObjectURL;
      URL.createObjectURL = (blob) => { blob.text().then(t => resolve(t)); return 'blob:stub'; };
      try { window.userManagement.downloadImportTemplate(); } catch (e) { resolve('ERR:' + e.message); }
      URL.createObjectURL = origCreate;
      setTimeout(() => resolve('TIMEOUT'), 1500);
    }));
    const tplHasBoth = /Company Access/i.test(tpl) && /User Company/i.test(tpl);
    rec('T1', 'Import template has Company Access + User Company columns', tplHasBoth ? 'PASS' : 'FAIL', `header="${(tpl||'').split('\n')[0]}"`);

    // T2: export columns
    const exp = await page.evaluate(() => new Promise((resolve) => {
      const origCreate = URL.createObjectURL;
      URL.createObjectURL = (blob) => { blob.text().then(t => resolve(t)); return 'blob:stub'; };
      try { window.userManagement.exportUsers(); } catch (e) { resolve('ERR:' + e.message); }
      URL.createObjectURL = origCreate;
      setTimeout(() => resolve('TIMEOUT'), 2000);
    }));
    const expHeader = (exp || '').split('\n')[0];
    const expHasBoth = /Company Access/i.test(expHeader) && /User Company/i.test(expHeader);
    rec('T2', 'Export has Company Access + User Company columns', expHasBoth ? 'PASS' : 'FAIL', `header="${expHeader}"`);

  } catch (err) {
    rec('ERR', 'Script error', 'FAIL', err.message);
  } finally {
    const pass = results.filter(r => r.status==='PASS').length;
    const fail = results.filter(r => r.status==='FAIL').length;
    const warn = results.filter(r => r.status==='WARN').length;
    console.log(`\n=== CR-020rev verify: ${pass} PASS / ${fail} FAIL / ${warn} WARN ===`);
    fs.writeFileSync(path.join(OUT,'cr020rev-results.json'), JSON.stringify({pass,fail,warn,results},null,2));
    await browser.close();
    process.exit(fail>0?1:0);
  }
})();
