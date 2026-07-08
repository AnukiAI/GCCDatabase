// CRUD — Product create -> edit -> delete round-trip (Phase 3 / gap C1).
//   Create/Update  POST   /Product/SaveProduct   {id, name, categoryId, companyId}  [AnalystOrAdmin, antiforgery]
//   Delete         DELETE /Product/DeleteProduct?id={id}   (HARD delete; blocked if any cert references it)
// Table: Products (Name non-unique). SaveProduct has NO find-or-create side effect (that's only in
// ImportProducts), so no pollution. Fixture product has zero certs, so it is deletable.
//
// SAFETY: fixtures only (Name LIKE 'ZZ_SmokeFixture_Product%'), verified in SQL, self-cleaning.
// Run:  node Scripts/smoke-prod/_crud/product-crud-roundtrip.js

const W = require('./_lib/write-harness');
const sqlEsc = (s) => String(s).replace(/'/g, "''");
const MARK = 'ZZ_SmokeFixture_Product';

async function run() {
  W.guardRemote();
  console.log(`\n[crud] product-crud-roundtrip — ${W.BASE}\n`);
  const { results, record } = W.makeRecorder('product-crud-roundtrip');
  const name = `${MARK}_${Date.now().toString(36)}`;
  const nameEdited = `${name}_ed`;
  const browser = await W.chromium.launch({ headless: true });

  try {
    W.sqlExec(`DELETE FROM Products WHERE Name LIKE '${MARK}%'`);
    // Valid FK values reused from an existing product (guaranteed-valid Category + Company).
    const fk = W.sqlQuery(`SELECT TOP 1 CompanyId, CategoryId FROM Products WHERE CompanyId>0 AND CategoryId>0 AND IsDeleted=0 ORDER BY Id DESC`);
    if (!fk.length) { record('prod.pre', 'discover Company+Category FKs', 'BLOCKED', 'no product with valid FKs'); await browser.close(); return W.summarize('product-crud-roundtrip', null, results); }
    const [companyId, categoryId] = fk[0];
    const before = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Products'), 10);

    const ctx = await W.newContext(browser);
    const admin = await W.login(ctx, 'admin');
    if (!admin.ok) { record('prod.login', 'admin login', 'BLOCKED', 'login failed'); await browser.close(); return W.summarize('product-crud-roundtrip', null, results); }
    const token = await W.antiforgeryToken(admin.page, '/Product/Management');
    record('prod.pre', 'FK discovery + snapshot', 'PASS', `company=${companyId} category=${categoryId} products=${before}`);

    // CREATE
    const c = await W.jsonReq(ctx, 'POST', '/Product/SaveProduct', { id: 0, name, categoryId: Number(categoryId), companyId: Number(companyId) }, token);
    record('prod.C1.create', 'POST /Product/SaveProduct (create) accepted', c.status === 200 ? 'PASS' : 'FAIL', `HTTP ${c.status} ${JSON.stringify(c.json || {}).slice(0, 120)}`);
    const idRow = W.sqlQuery(`SELECT Id FROM Products WHERE Name='${sqlEsc(name)}' AND IsDeleted=0`);
    const id = idRow.length ? idRow[0][0] : null;
    record('prod.C2.create-persisted', 'product row exists in DB', id ? 'PASS' : 'FAIL', id ? `id=${id}` : 'no row');

    // UPDATE (rename)
    if (id) {
      const u = await W.jsonReq(ctx, 'POST', '/Product/SaveProduct', { id: Number(id), name: nameEdited, categoryId: Number(categoryId), companyId: Number(companyId) }, token);
      record('prod.U1.update', 'POST /Product/SaveProduct (update) accepted', u.status === 200 ? 'PASS' : 'FAIL', `HTTP ${u.status}`);
      const now = W.sqlScalar(`SELECT Name FROM Products WHERE Id=${Number(id)}`);
      record('prod.U2.update-persisted', 'edited name persisted', now === nameEdited ? 'PASS' : 'FAIL', `db Name="${now}" expected="${nameEdited}"`);
    }

    // DELETE (hard)
    if (id) {
      const d = await W.jsonReq(ctx, 'DELETE', `/Product/DeleteProduct?id=${id}`, undefined, token);
      record('prod.D1.delete', 'DELETE /Product/DeleteProduct accepted', d.status === 200 ? 'PASS' : 'FAIL', `HTTP ${d.status}`);
      const gone = parseInt(W.sqlScalar(`SELECT COUNT(*) FROM Products WHERE Id=${Number(id)}`), 10) === 0;
      record('prod.D2.delete-removed', 'product row removed (hard delete)', gone ? 'PASS' : 'FAIL', gone ? 'gone' : 'STILL PRESENT');
    }

    const after = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Products'), 10);
    record('prod.INV', 'Products count net-zero after round-trip', after === before ? 'PASS' : 'WARN', `before=${before} after=${after}`);
  } catch (e) {
    record('prod.error', 'spec ran without throwing', 'FAIL', e.message);
  } finally {
    try { W.sqlExec(`DELETE FROM Products WHERE Name LIKE '${MARK}%'`); } catch {}
    await browser.close();
  }
  return W.summarize('product-crud-roundtrip', null, results);
}

module.exports = { run };
if (require.main === module) run().then((s) => process.exit(s.fail || s.blocked ? 1 : 0));
