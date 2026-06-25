// DEF-295 — functional check: Partner/External CSV export must NOT contain the three
// Admin/Analyst-only columns (Cert Subcategories, Pathway, Cert Results); Admin export MUST.
// Confirms the live HTTP path + real role claims (the unit tests hit the service directly).
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/_def295-export-by-role.js
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');

const ADMIN_ONLY = [/pathway/i, /cert\s*results/i, /(cert|certification)\s*subcategor/i];
const results = [];
const rec = (id, status, notes = '') => { results.push({ id, status }); console.log(`  [${status}] ${id}${notes ? ' :: ' + notes : ''}`); };

async function headerFor(page, role) {
  await login(page, role);
  const resp = await page.request.get(BASE + '/Certificate/ExportCertificates');
  if (!resp.ok()) return { status: resp.status(), header: null };
  const body = await resp.text();
  return { status: resp.status(), header: (body.split(/\r?\n/)[0] || '') };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    // External: the 3 columns must be ABSENT.
    const ext = await (await browser.newContext()).newPage();
    const e = await headerFor(ext, 'external');
    if (e.header === null) { rec('external.export-reachable', 'FAIL', `HTTP ${e.status}`); }
    else {
      rec('external.export-reachable', 'PASS', `HTTP ${e.status}`);
      const leaked = ADMIN_ONLY.filter(rx => rx.test(e.header)).map(rx => rx.source);
      rec('external.no-admin-columns', leaked.length === 0 ? 'PASS' : 'FAIL',
        leaked.length ? `LEAKED: ${leaked.join(', ')}` : 'no Pathway/Cert Results/Cert Subcategories in header');
      console.log(`     external header: ${e.header.slice(0, 200)}`);
    }

    // Admin: the 3 columns must be PRESENT (no over-strip regression).
    const adm = await (await browser.newContext()).newPage();
    const a = await headerFor(adm, 'admin');
    if (a.header === null) { rec('admin.export-reachable', 'FAIL', `HTTP ${a.status}`); }
    else {
      rec('admin.export-reachable', 'PASS', `HTTP ${a.status}`);
      const present = ADMIN_ONLY.filter(rx => rx.test(a.header)).map(rx => rx.source);
      rec('admin.has-admin-columns', present.length === ADMIN_ONLY.length ? 'PASS' : 'FAIL',
        `present: ${present.join(', ') || 'none'}`);
      console.log(`     admin header: ${a.header.slice(0, 300)}`);
    }
  } catch (err) {
    rec('exception', 'FAIL', err.message);
  } finally {
    await browser.close();
  }
  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  console.log(`\nDEF-295 EXPORT-BY-ROLE: ${pass} passed, ${fail} failed.`);
  process.exit(fail === 0 ? 0 : 1);
})();
