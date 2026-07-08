// CRUD — Tag create -> edit -> delete round-trip (Phase 3 / gap C1).
// Tag is the simplest entity: no FKs, hard delete, unique Name. Admin JSON API.
//   Create  POST   /Admin/CreateTag      {name, tagType}   [AdminOnly, antiforgery]
//   Update  PUT    /Admin/UpdateTag/{id} {id, name, tagType}
//   Delete  DELETE /Admin/DeleteTag/{id}                    (hard delete)
// Table: Tags (unique Name; NormalizedName is computed — never sent). Fixture marker in Name.
//
// SAFETY: fixtures only (Name LIKE 'ZZ_SmokeFixture_Tag%'), verified in SQL, self-cleaning.
// Run:  node Scripts/smoke-prod/_crud/tag-crud-roundtrip.js

const W = require('./_lib/write-harness');
const sqlEsc = (s) => String(s).replace(/'/g, "''");
const MARK = 'ZZ_SmokeFixture_Tag';

async function run() {
  W.guardRemote();
  console.log(`\n[crud] tag-crud-roundtrip — ${W.BASE}\n`);
  const { results, record } = W.makeRecorder('tag-crud-roundtrip');
  const name = `${MARK}_${Date.now().toString(36)}`;
  const nameEdited = `${name}_ed`;
  const browser = await W.chromium.launch({ headless: true });

  try {
    W.sqlExec(`DELETE FROM Tags WHERE Name LIKE '${MARK}%'`);
    const tagsBefore = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Tags'), 10);

    const ctx = await W.newContext(browser);
    const admin = await W.login(ctx, 'admin');
    if (!admin.ok) { record('tag.login', 'admin login', 'BLOCKED', 'login failed'); await browser.close(); return W.summarize('tag-crud-roundtrip', null, results); }
    const token = await W.antiforgeryToken(admin.page, '/Admin/Tags');

    // CREATE
    const c = await W.jsonReq(ctx, 'POST', '/Admin/CreateTag', { name, tagType: 'Internal' }, token);
    record('tag.C1.create', 'POST /Admin/CreateTag accepted', c.status === 200 ? 'PASS' : 'FAIL', `HTTP ${c.status} ${JSON.stringify(c.json || {}).slice(0, 120)}`);
    const idRow = W.sqlQuery(`SELECT Id FROM Tags WHERE Name='${sqlEsc(name)}'`);
    const id = idRow.length ? idRow[0][0] : null;
    record('tag.C2.create-persisted', 'tag row exists in DB', id ? 'PASS' : 'FAIL', id ? `id=${id}` : 'no row');

    // UPDATE
    if (id) {
      const u = await W.jsonReq(ctx, 'PUT', `/Admin/UpdateTag/${id}`, { id: Number(id), name: nameEdited, tagType: 'Internal' }, token);
      record('tag.U1.update', 'PUT /Admin/UpdateTag accepted', u.status === 200 ? 'PASS' : 'FAIL', `HTTP ${u.status}`);
      const now = W.sqlScalar(`SELECT Name FROM Tags WHERE Id=${Number(id)}`);
      record('tag.U2.update-persisted', 'edited name persisted', now === nameEdited ? 'PASS' : 'FAIL', `db Name="${now}" expected="${nameEdited}"`);
    }

    // DELETE
    if (id) {
      const d = await W.jsonReq(ctx, 'DELETE', `/Admin/DeleteTag/${id}`, undefined, token);
      record('tag.D1.delete', 'DELETE /Admin/DeleteTag accepted', d.status === 200 ? 'PASS' : 'FAIL', `HTTP ${d.status}`);
      const gone = parseInt(W.sqlScalar(`SELECT COUNT(*) FROM Tags WHERE Id=${Number(id)}`), 10) === 0;
      record('tag.D2.delete-removed', 'tag row removed (hard delete)', gone ? 'PASS' : 'FAIL', gone ? 'gone' : 'STILL PRESENT');
    }

    const tagsAfter = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Tags'), 10);
    record('tag.INV', 'Tags count net-zero after round-trip', tagsAfter === tagsBefore ? 'PASS' : 'WARN', `before=${tagsBefore} after=${tagsAfter}`);
  } catch (e) {
    record('tag.error', 'spec ran without throwing', 'FAIL', e.message);
  } finally {
    try { W.sqlExec(`DELETE FROM Tags WHERE Name LIKE '${MARK}%'`); } catch {}
    await browser.close();
  }
  return W.summarize('tag-crud-roundtrip', null, results);
}

module.exports = { run };
if (require.main === module) run().then((s) => process.exit(s.fail || s.blocked ? 1 : 0));
