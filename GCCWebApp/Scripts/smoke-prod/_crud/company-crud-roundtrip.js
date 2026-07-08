// CRUD — Company create -> edit -> delete round-trip (Phase 3 / gap C1).
//   Create/Update  POST   /Company/SaveCompany   {id, name}  (id:0=create, id>0=update) [AnalystOrAdmin, antiforgery]
//   Delete         DELETE /Company/DeleteCompany?id={id}                (SOFT delete: IsDeleted=1)
// Table: Companies (unique Name). Fixture marker in Name; SOFT delete so teardown hard-deletes.
//
// SAFETY: fixtures only (Name LIKE 'ZZ_SmokeFixture_Company%'), verified in SQL, self-cleaning.
// Run:  node Scripts/smoke-prod/_crud/company-crud-roundtrip.js

const W = require('./_lib/write-harness');
const sqlEsc = (s) => String(s).replace(/'/g, "''");
const MARK = 'ZZ_SmokeFixture_Company';

async function run() {
  W.guardRemote();
  console.log(`\n[crud] company-crud-roundtrip — ${W.BASE}\n`);
  const { results, record } = W.makeRecorder('company-crud-roundtrip');
  const name = `${MARK}_${Date.now().toString(36)}`;
  const nameEdited = `${name}_ed`;
  const browser = await W.chromium.launch({ headless: true });

  try {
    W.sqlExec(`DELETE FROM Companies WHERE Name LIKE '${MARK}%'`);
    const before = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Companies'), 10);

    const ctx = await W.newContext(browser);
    const admin = await W.login(ctx, 'admin');
    if (!admin.ok) { record('co.login', 'admin login', 'BLOCKED', 'login failed'); await browser.close(); return W.summarize('company-crud-roundtrip', null, results); }
    const token = await W.antiforgeryToken(admin.page, '/Company/Index');

    // CREATE
    const c = await W.jsonReq(ctx, 'POST', '/Company/SaveCompany', { id: 0, name }, token);
    record('co.C1.create', 'POST /Company/SaveCompany (create) accepted', c.status === 200 ? 'PASS' : 'FAIL', `HTTP ${c.status} ${JSON.stringify(c.json || {}).slice(0, 120)}`);
    const idRow = W.sqlQuery(`SELECT Id FROM Companies WHERE Name='${sqlEsc(name)}' AND IsDeleted=0`);
    const id = idRow.length ? idRow[0][0] : null;
    record('co.C2.create-persisted', 'company row exists in DB', id ? 'PASS' : 'FAIL', id ? `id=${id}` : 'no row');

    // UPDATE (rename)
    if (id) {
      const u = await W.jsonReq(ctx, 'POST', '/Company/SaveCompany', { id: Number(id), name: nameEdited }, token);
      record('co.U1.update', 'POST /Company/SaveCompany (update) accepted', u.status === 200 ? 'PASS' : 'FAIL', `HTTP ${u.status}`);
      const now = W.sqlScalar(`SELECT Name FROM Companies WHERE Id=${Number(id)}`);
      record('co.U2.update-persisted', 'edited name persisted', now === nameEdited ? 'PASS' : 'FAIL', `db Name="${now}" expected="${nameEdited}"`);
    }

    // DELETE (soft)
    if (id) {
      const d = await W.jsonReq(ctx, 'DELETE', `/Company/DeleteCompany?id=${id}`, undefined, token);
      record('co.D1.delete', 'DELETE /Company/DeleteCompany accepted', d.status === 200 ? 'PASS' : 'FAIL', `HTTP ${d.status}`);
      const soft = W.sqlScalar(`SELECT IsDeleted FROM Companies WHERE Id=${Number(id)}`);
      record('co.D2.delete-softdeleted', 'company soft-deleted (IsDeleted=1)', (soft === '1' || soft === 'True') ? 'PASS' : 'FAIL', `IsDeleted=${soft}`);
      // hard-remove the fixture so the count returns to baseline
      W.sqlExec(`DELETE FROM Companies WHERE Name LIKE '${MARK}%'`);
    }

    const after = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Companies'), 10);
    record('co.INV', 'Companies count net-zero after round-trip', after === before ? 'PASS' : 'WARN', `before=${before} after=${after}`);
  } catch (e) {
    record('co.error', 'spec ran without throwing', 'FAIL', e.message);
  } finally {
    try { W.sqlExec(`DELETE FROM Companies WHERE Name LIKE '${MARK}%'`); } catch {}
    await browser.close();
  }
  return W.summarize('company-crud-roundtrip', null, results);
}

module.exports = { run };
if (require.main === module) run().then((s) => process.exit(s.fail || s.blocked ? 1 : 0));
