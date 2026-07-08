// CRUD — User create -> edit -> delete round-trip (Phase 3 / gap C1).
//   Create  POST   /Admin/CreateUser      {name, email, role, password}  [AdminOnly, antiforgery]
//   Update  PUT    /Admin/UpdateUser/{id}  {id, name, email, role, status}
//   Delete  DELETE /Admin/DeleteUser/{id}   (SOFT delete; GUARD: 'Admin'-role users cannot be deleted)
// Table: Users (unique Username+Email filtered [IsDeleted]=0). Fixture uses a NON-Admin role so it
// is deletable. Email/Name carry the fixture marker; SOFT delete so teardown hard-deletes.
//
// SAFETY: fixtures only (Email LIKE 'zz_smokefixture_user%'), verified in SQL, self-cleaning.
// Run:  node Scripts/smoke-prod/_crud/user-crud-roundtrip.js

const W = require('./_lib/write-harness');
const sqlEsc = (s) => String(s).replace(/'/g, "''");
const MARK_NAME = 'ZZ_SmokeFixture_User';
const MARK_EMAIL = 'zz_smokefixture_user';

function purge() {
  // The display name is stored in FullName (the Users.Name column is unused/NULL). Identify
  // fixtures by Email (always marked) or FullName. Remove best-effort children, then the user.
  const ids = W.sqlQuery(`SELECT Id FROM Users WHERE Email LIKE '${MARK_EMAIL}%' OR FullName LIKE '${MARK_NAME}%'`).map((r) => r[0]).filter(Boolean);
  for (const uid of ids) {
    for (const child of ['UserCompanies', 'UserPartners', 'UserRoles']) {
      try { W.sqlExec(`IF OBJECT_ID('${child}','U') IS NOT NULL DELETE FROM ${child} WHERE UserId=${Number(uid)}`); } catch {}
    }
  }
  try { W.sqlExec(`DELETE FROM Users WHERE Email LIKE '${MARK_EMAIL}%' OR FullName LIKE '${MARK_NAME}%'`); } catch {}
}

async function run() {
  W.guardRemote();
  console.log(`\n[crud] user-crud-roundtrip — ${W.BASE}\n`);
  const { results, record } = W.makeRecorder('user-crud-roundtrip');
  const tag = Date.now().toString(36);
  const name = `${MARK_NAME}_${tag}`;
  const nameEdited = `${name}_ed`;
  const email = `${MARK_EMAIL}_${tag}@example.com`;
  const browser = await W.chromium.launch({ headless: true });

  try {
    purge();
    const roleRow = W.sqlQuery(`SELECT TOP 1 Name FROM Roles WHERE Name <> 'Admin' ORDER BY CASE WHEN Name='External' THEN 0 WHEN Name='Analyst' THEN 1 ELSE 2 END`);
    if (!roleRow.length) { record('user.pre', 'discover a non-Admin role', 'BLOCKED', 'no non-Admin role found'); await browser.close(); return W.summarize('user-crud-roundtrip', null, results); }
    const role = roleRow[0][0];
    const before = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Users'), 10);

    const ctx = await W.newContext(browser);
    const admin = await W.login(ctx, 'admin');
    if (!admin.ok) { record('user.login', 'admin login', 'BLOCKED', 'login failed'); await browser.close(); return W.summarize('user-crud-roundtrip', null, results); }
    const token = await W.antiforgeryToken(admin.page, '/Admin/UserManagement');
    record('user.pre', 'role discovery + snapshot', 'PASS', `role=${role} users=${before}`);

    // CREATE
    const c = await W.jsonReq(ctx, 'POST', '/Admin/CreateUser', { name, email, role, password: 'TestPass123!' }, token);
    record('user.C1.create', 'POST /Admin/CreateUser accepted', c.status === 200 ? 'PASS' : 'FAIL', `HTTP ${c.status} ${JSON.stringify(c.json || {}).slice(0, 140)}`);
    const idRow = W.sqlQuery(`SELECT Id FROM Users WHERE Email='${sqlEsc(email)}' AND IsDeleted=0`);
    const id = idRow.length ? idRow[0][0] : null;
    record('user.C2.create-persisted', 'user row exists in DB', id ? 'PASS' : 'FAIL', id ? `id=${id}` : 'no row');

    // UPDATE (rename)
    if (id) {
      const u = await W.jsonReq(ctx, 'PUT', `/Admin/UpdateUser/${id}`, { id: Number(id), name: nameEdited, email, role, status: 'Active' }, token);
      record('user.U1.update', 'PUT /Admin/UpdateUser accepted', u.status === 200 ? 'PASS' : 'FAIL', `HTTP ${u.status} ${JSON.stringify(u.json || {}).slice(0, 100)}`);
      const now = W.sqlScalar(`SELECT FullName FROM Users WHERE Id=${Number(id)}`);
      record('user.U2.update-persisted', 'edited name persisted', now === nameEdited ? 'PASS' : 'FAIL', `db FullName="${now}" expected="${nameEdited}"`);
    }

    // DELETE (soft)
    if (id) {
      const d = await W.jsonReq(ctx, 'DELETE', `/Admin/DeleteUser/${id}`, undefined, token);
      record('user.D1.delete', 'DELETE /Admin/DeleteUser accepted', d.status === 200 ? 'PASS' : 'FAIL', `HTTP ${d.status}`);
      const soft = W.sqlScalar(`SELECT IsDeleted FROM Users WHERE Id=${Number(id)}`);
      record('user.D2.delete-softdeleted', 'user soft-deleted (IsDeleted=1)', (soft === '1' || soft === 'True') ? 'PASS' : 'FAIL', `IsDeleted=${soft}`);
      purge();
    }

    const after = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Users'), 10);
    record('user.INV', 'Users count net-zero after round-trip', after === before ? 'PASS' : 'WARN', `before=${before} after=${after}`);
  } catch (e) {
    record('user.error', 'spec ran without throwing', 'FAIL', e.message);
  } finally {
    try { purge(); } catch {}
    await browser.close();
  }
  return W.summarize('user-crud-roundtrip', null, results);
}

module.exports = { run };
if (require.main === module) run().then((s) => process.exit(s.fail || s.blocked ? 1 : 0));
