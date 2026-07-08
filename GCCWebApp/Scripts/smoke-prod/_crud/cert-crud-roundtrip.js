// CRUD — Certificate create -> edit -> delete round-trip (Phase 3 / gap C1). The single biggest
// coverage gap: the existing smokes only assert "the form loads" — never that a save PERSISTS or a
// delete REMOVES. This exercises the real write path (controller -> service -> EF -> DB) and VERIFIES
// persistence in SQL, then cleans up after itself.
//
// SAFETY: writes ONLY a self-created fixture cert (Location marked ZZ_SmokeFixture_CRUD_*), cloned
// from a real existing company/product/type combo so CR-186 find-or-create RESOLVES (no new product
// rows). A product-count invariant proves no pollution; teardown runs in finally.
//
// Steps / assertions:
//   PRE   discover a valid clonable combo (SQL); snapshot Products + Certificates counts
//   C1    analyst POST /Certificate/Create  -> 302 redirect (new cert number captured)
//   C2    SQL: created cert exists with the fixture marker            (CREATE persisted)
//   U1    analyst POST /Certificate/Edit/{id} with changed marker -> 302
//   U2    SQL: cert Location shows the EDITED marker                  (UPDATE persisted)
//   D1    admin   POST /Certificate/Delete/{number} -> 200/302
//   D2    SQL: cert row is gone                                       (DELETE removed)
//   INV   Products count unchanged (no find-or-create pollution) AND Certificates count net-zero
//
// Requires the dev server on :5101 and sqlcmd. Run:
//   node Scripts/smoke-prod/_crud/cert-crud-roundtrip.js

const W = require('./_lib/write-harness');

const pad = (n) => String(n).padStart(2, '0');
function ymd(d) { return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`; }
function sqlEsc(s) { return String(s).replace(/'/g, "''"); }

async function run() {
  W.guardRemote();
  console.log(`\n[crud] cert-crud-roundtrip — ${W.BASE}\n`);
  const { results, record } = W.makeRecorder('cert-crud-roundtrip');
  const runTag = ymd(new Date()) + '_' + Date.now().toString(36);
  const marker = `${W.FIXTURE_MARKER}_${runTag}`;
  const markerEdited = `${marker}_EDITED`;
  const browser = await W.chromium.launch({ headless: true });
  let createdNumber = null;

  try {
    // ---- PRE: idempotent pre-clean of any stale fixtures, discover combo, snapshot counts ----
    W.sqlExec(`DELETE FROM Certificates WHERE Location LIKE '${W.FIXTURE_MARKER}%'`);
    // Pull dims from the PRODUCT record (not a cert): ResolveProductForCertAsync matches on
    // product Name+Category+Company+Type+Subcategory, so passing a product's OWN dims makes
    // find-or-create RESOLVE to that product (no new row). Cloning a cert's dims (which can be
    // NULL while the product's aren't) would spawn a duplicate product — the pollution we must avoid.
    const combo = W.sqlQuery(
      `SELECT TOP 1 pr.CompanyId, pr.Id, pr.CategoryId,
              ISNULL(CAST(pr.ProductTypeId AS VARCHAR),''), ISNULL(CAST(pr.ProductSubcategoryId AS VARCHAR),'')
       FROM Products pr
       WHERE pr.CompanyId > 0 AND pr.IsDeleted = 0 AND pr.CategoryId > 0
       ORDER BY pr.Id DESC`);
    const certTypeRow = W.sqlQuery(`SELECT TOP 1 CertificationTypeId FROM Certificates WHERE CertificationTypeId > 0`);
    if (!combo.length || !certTypeRow.length) {
      record('crud.pre.combo', 'discover a clonable product combo', 'BLOCKED', 'no product/cert-type found');
      await browser.close();
      return W.summarize('cert-crud-roundtrip', null, results);
    }
    const [companyId, productId, categoryId, typeId, subcatId] = combo[0];
    const certTypeId = certTypeRow[0][0];
    const productsBefore = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Products'), 10);
    const certsBefore = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Certificates'), 10);
    record('crud.pre', 'clonable combo + count snapshot', 'PASS',
      `company=${companyId} product=${productId} cat=${categoryId||'-'} type=${typeId||'-'} sub=${subcatId||'-'} certType=${certTypeId} | products=${productsBefore} certs=${certsBefore}`);

    // ---- C1: CREATE (analyst) ----
    const analystCtx = await W.newContext(browser);
    const analyst = await W.login(analystCtx, 'analyst');
    if (!analyst.ok) {
      record('crud.C1.create', 'analyst login', 'BLOCKED', 'analyst login failed');
      await browser.close();
      return W.summarize('cert-crud-roundtrip', null, results);
    }
    await analyst.page.goto(W.BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded' });
    const createForm = await W.scrapeForm(analyst.page, '#certificateCreateForm');
    const today = new Date();
    const nextYear = new Date(today.getTime()); nextYear.setFullYear(today.getFullYear() + 1);
    const createFields = {
      ...createForm.fields, // carries __RequestVerificationToken, __Invariant, empty hidden fields
      CompanyId: companyId, ProductId: productId,
      ProductCategoryId: categoryId, ProductTypeId: typeId, ProductSubcategoryId: subcatId,
      CertificationTypeId: certTypeId,
      Status: 'Draft', Partner: 'None',
      EffectiveDate: ymd(today), ExpirationDate: ymd(nextYear),
      Location: marker, CertificationResults: 'ZZ smoke create', Country: '',
      action: 'save',
    };
    const createResp = await W.postForm(analystCtx, '/Certificate/Create', createFields);
    const cStatus = createResp.status();
    const loc = createResp.headers()['location'] || '';
    const m = loc.match(/CertificateInternalView\/([0-9A-Za-z-]+)/);
    createdNumber = m ? m[1] : null;
    record('crud.C1.create-302', 'analyst Create POST redirects on success',
      (cStatus === 302 && createdNumber) ? 'PASS' : 'FAIL',
      `HTTP ${cStatus}, redirect="${loc}", number=${createdNumber}` +
      (cStatus === 200 ? ' — ModelState invalid (form re-rendered)' : ''));

    // ---- C2: verify CREATE persisted ----
    if (createdNumber) {
      const row = W.sqlQuery(`SELECT Location FROM Certificates WHERE Number='${sqlEsc(createdNumber)}'`);
      const persisted = row.length && row[0][0] === marker;
      record('crud.C2.create-persisted', 'created cert exists in DB with marker',
        persisted ? 'PASS' : 'FAIL', `db Location="${row.length ? row[0][0] : '(no row)'}" expected="${marker}"`);
    }

    // ---- U1/U2: EDIT (analyst) ----
    if (createdNumber) {
      await analyst.page.goto(W.BASE + '/Certificate/Edit/' + createdNumber, { waitUntil: 'domcontentloaded' });
      const editForm = await W.scrapeForm(analyst.page, '#certificateEditForm');
      const certId = editForm.fields.Id;
      // The ProductId <select> populates asynchronously (GetProductsByCompany) and may not be
      // filled at domcontentloaded — a UI-timing concern already covered by cert-edit.js (DEF-225).
      // For this WRITE-PATH test we inject the cert's known-correct product dims so the assertion
      // isolates "does the UPDATE persist", not "did the async dropdown pre-select".
      const editFields = {
        ...editForm.fields,
        ProductId: productId, ProductCategoryId: categoryId, ProductTypeId: typeId, ProductSubcategoryId: subcatId,
        Location: markerEdited, CertificationResults: 'ZZ smoke edit', action: 'save',
      };
      const editResp = await W.postForm(analystCtx, `/Certificate/Edit/${certId}`, editFields);
      const uStatus = editResp.status();
      record('crud.U1.edit-302', 'analyst Edit POST redirects on success',
        uStatus === 302 ? 'PASS' : 'FAIL', `HTTP ${uStatus} (id=${certId})`);
      const row = W.sqlQuery(`SELECT Location FROM Certificates WHERE Number='${sqlEsc(createdNumber)}'`);
      const updated = row.length && row[0][0] === markerEdited;
      record('crud.U2.edit-persisted', 'edited value persisted in DB',
        updated ? 'PASS' : 'FAIL', `db Location="${row.length ? row[0][0] : '(no row)'}" expected="${markerEdited}"`);
    }

    // ---- D1/D2: DELETE (admin) ----
    if (createdNumber) {
      const adminCtx = await W.newContext(browser);
      const admin = await W.login(adminCtx, 'admin');
      if (!admin.ok) {
        record('crud.D1.delete', 'admin login for delete', 'BLOCKED', 'admin login failed');
      } else {
        // Grab a valid antiforgery token from any admin form (Edit page).
        await admin.page.goto(W.BASE + '/Certificate/Edit/' + createdNumber, { waitUntil: 'domcontentloaded' });
        const token = await admin.page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
        const delResp = await W.postForm(adminCtx, `/Certificate/Delete/${createdNumber}`, { __RequestVerificationToken: token });
        const dStatus = delResp.status();
        record('crud.D1.delete-ok', 'admin Delete POST accepted',
          [200, 302].includes(dStatus) ? 'PASS' : 'FAIL', `HTTP ${dStatus}`);
        const gone = parseInt(W.sqlScalar(`SELECT COUNT(*) FROM Certificates WHERE Number='${sqlEsc(createdNumber)}'`), 10) === 0;
        record('crud.D2.delete-removed', 'cert row removed from DB',
          gone ? 'PASS' : 'FAIL', gone ? 'gone' : 'STILL PRESENT');
        if (gone) createdNumber = null; // deleted cleanly; teardown no longer needs it
      }
    }

    // ---- INV: no pollution ----
    const productsAfter = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Products'), 10);
    record('crud.INV.products', 'Products count unchanged (no find-or-create pollution)',
      productsAfter === productsBefore ? 'PASS' : 'FAIL', `before=${productsBefore} after=${productsAfter}`);
    const certsAfter = parseInt(W.sqlScalar('SELECT COUNT(*) FROM Certificates'), 10);
    record('crud.INV.certs', 'Certificates count net-zero after round-trip',
      certsAfter === certsBefore ? 'PASS' : 'WARN', `before=${certsBefore} after=${certsAfter}`);
  } catch (e) {
    record('crud.error', 'spec ran without throwing', 'FAIL', e.message);
  } finally {
    // Teardown backstop: remove any fixture cert this run left behind (e.g. failed mid-flow).
    try { W.sqlExec(`DELETE FROM Certificates WHERE Location LIKE '${W.FIXTURE_MARKER}%'`); } catch {}
    await browser.close();
  }

  return W.summarize('cert-crud-roundtrip', null, results);
}

module.exports = { run };
if (require.main === module) {
  run().then((s) => process.exit(s.fail || s.blocked ? 1 : 0));
}
