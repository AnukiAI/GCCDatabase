// QC #109 — User CSV import, end-to-end through the real parse → preview → apply pipeline.
// Also exercises the CR-020 interaction (Company Access vs User Company must not cross).
//
// Verifies on localhost:5101 (admin), driving window.userManagement directly with a CSV string
// (same entry point the file-reader onload calls — avoids the OS file dialog):
//   E0  Preview builds: 1 "new" valid row + 1 "new" partner-error row
//   E1  Valid row imported -> Company Access (companyIds) = the TWO access companies
//   E2  Valid row's User Company (employer) = the THIRD company, and is NOT one of the access IDs
//   E3  Error surfacing (QC #109 fix): the Partner-no-partner row fails AND the failure detail
//       appears in the user-facing notification (not just console)
//   E4  The errored row did NOT create a user
//
// Creates throwaway users qc109-*@anuki.example on the DEV DB. LOCALHOST ONLY.
// Run: node Scripts/smoke-prod/def109-import-e2e-verify.js
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
const csvField = (v) => /[",;\n]/.test(v) ? `"${String(v).replace(/"/g, '""')}"` : v;

(async () => {
  if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Admin/UserManagement`, { waitUntil: 'networkidle', timeout: 60000 });
    await page.waitForTimeout(1000);

    // Pick 3 distinct real companies + a unique stamp (no Date/random).
    const setup = await page.evaluate(() => {
      const um = window.userManagement;
      const companies = (um?.companies || []).filter(c => c && c.id && c.name);
      const stamp = String((um?.users?.length || 0) + (um?.users?.[0]?.id || 0));
      return {
        ok: companies.length >= 3,
        a: companies[0], b: companies[1], c: companies[2],
        stamp,
      };
    });
    if (!setup.ok) { rec('SETUP', '>=3 companies available', 'FAIL', 'need 3 companies in dev DB'); throw new Error('not enough companies'); }
    const { a, b, c, stamp } = setup;
    const okEmail = `qc109-ok-${stamp}@anuki.example`;
    const errEmail = `qc109-err-${stamp}@anuki.example`;

    // Build the CSV: header names match the template (parser maps by name, not position).
    const header = ['Name', 'Email', 'Role', 'Company Access', 'User Company', 'Partner', 'Status'];
    const rowOk  = [`QC109 Access ${stamp}`, okEmail, 'External', `${a.name};${b.name}`, c.name, '', 'active'];
    const rowErr = [`QC109 PartnerErr ${stamp}`, errEmail, 'Partner', '', '', '', 'active']; // Partner role, no Partner value -> must fail
    const csv = [header, rowOk, rowErr].map(r => r.map(csvField).join(',')).join('\n');
    console.log('--- CSV under test ---\n' + csv + '\n----------------------');
    console.log(`Access companies: ${a.name}(${a.id}), ${b.name}(${b.id}) | Employer: ${c.name}(${c.id})`);

    // Stage 1: build the preview (opens the preview modal).
    const preview = await page.evaluate(async (csvText) => {
      await window.userManagement.parseAndPreviewImport(csvText);
      const d = window.userManagement.importPreviewData || [];
      return d.map(r => ({ email: r.data?.email, status: r.status, action: r.action,
        company: r.data?.company, userCompany: r.data?.userCompany, errors: r.errors }));
    }, csv);
    const okRow = preview.find(r => (r.email || '').toLowerCase() === okEmail);
    const errRow = preview.find(r => (r.email || '').toLowerCase() === errEmail);
    rec('E0', 'Preview builds both rows as new/create',
      okRow && errRow && okRow.status === 'new' && errRow.status === 'new' ? 'PASS' : 'FAIL',
      `ok=${JSON.stringify(okRow)} err=${JSON.stringify(errRow)}`);

    await page.screenshot({ path: path.join(OUT, 'def109-preview.png'), fullPage: true });

    // Stage 2: spy on the notification, then apply.
    const notes = await page.evaluate(async () => {
      const um = window.userManagement;
      const captured = [];
      const orig = um.showNotification.bind(um);
      um.showNotification = (msg, type) => { captured.push({ msg, type }); return orig(msg, type); };
      await um.applySelectedImport();
      um.showNotification = orig;
      return captured;
    });
    console.log('Notifications: ' + JSON.stringify(notes));
    await page.waitForTimeout(800);

    // E3: error detail surfaced in a user-facing notification (not console-only).
    const errNote = notes.find(n => /fail|error|partial/i.test(n.msg) && /partner/i.test(n.msg));
    rec('E3', 'Import error detail surfaced in notification (QC #109)',
      errNote ? 'PASS' : 'FAIL', `notes=${JSON.stringify(notes)}`);

    // Pull ground truth from the server.
    const dbUsers = await page.evaluate(async () => {
      const r = await fetch('/Admin/GetUsers');
      const j = await r.json();
      return (j.data || j.Data || []);
    });
    const okUser = dbUsers.find(u => (u.email || '').toLowerCase() === okEmail);
    const errUser = dbUsers.find(u => (u.email || '').toLowerCase() === errEmail);

    // E1: Company Access = the two access companies.
    const accessIds = (okUser?.companyIds || []).slice().sort((x, y) => x - y);
    const wantAccess = [a.id, b.id].sort((x, y) => x - y);
    rec('E1', 'Valid row: Company Access resolved to the two companies',
      okUser && JSON.stringify(accessIds) === JSON.stringify(wantAccess) ? 'PASS' : 'FAIL',
      `got companyIds=[${accessIds}] want=[${wantAccess}]`);

    // E2: User Company = the third company, and NOT among the access IDs.
    const employerOk = okUser && okUser.userCompanyId === c.id &&
      (okUser.userCompanyName || '').toLowerCase() === c.name.toLowerCase() &&
      !wantAccess.includes(okUser.userCompanyId);
    rec('E2', 'Valid row: User Company (employer) distinct from Company Access',
      employerOk ? 'PASS' : 'FAIL',
      `userCompanyId=${okUser?.userCompanyId} (want ${c.id}), name="${okUser?.userCompanyName}", accessIds=[${wantAccess}]`);

    // E4: the errored partner row created no user.
    rec('E4', 'Partner-error row created NO user', !errUser ? 'PASS' : 'FAIL',
      errUser ? `unexpectedly created id=${errUser.id}` : 'absent as expected');

  } catch (err) {
    rec('ERR', 'Script error', 'FAIL', err.message + '\n' + (err.stack || ''));
  } finally {
    const pass = results.filter(r => r.status === 'PASS').length;
    const fail = results.filter(r => r.status === 'FAIL').length;
    console.log(`\n=== QC #109 import E2E: ${pass} PASS / ${fail} FAIL ===`);
    fs.writeFileSync(path.join(OUT, 'def109-import-results.json'), JSON.stringify({ pass, fail, results }, null, 2));
    await browser.close();
    process.exit(fail > 0 ? 1 : 0);
  }
})();
