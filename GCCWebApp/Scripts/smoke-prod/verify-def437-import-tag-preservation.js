// DEF-437 verification — CSV edit-import must not destroy tags it was never given.
//
// The defect: UpdateCertificateAsync did an unconditional `certificate.Tags.Clear()`, so an
// edit-import stripped EVERY tag (internal and external — they share one M2M) off every
// certificate it touched, even when the uploaded file carried no tag column at all. Import undo
// cannot restore collections, so the loss was permanent. It was also self-igniting: DetectChanges
// compared the cert's full tag set against the CSV's external-only column, so a tagged cert was
// ALWAYS reported Modified, which is what called the destructive apply.
//
// This script proves four things on a dedicated throwaway fixture certificate:
//   PHASE 1  create 98-9437 carrying one external + one internal tag
//   PHASE 2  import cert# + Location, NO tag columns  -> Location changes, BOTH tag sets survive
//            and the preview reports no tag change      <- the defect itself
//   PHASE 3  import cert# + External Tag only          -> external rewritten, internal PRESERVED
//            <- the partition: one column must not speak for the other
//   PHASE 4  import cert# + Internal Tag present-but-BLANK -> internal cleared, external untouched
//            <- deliberate clearing must still work (DEF-355's documented rule)
//
// Run PHASE 2 against the pre-fix binary and it fails; that is the repro.
//
// LOCALHOST ONLY. Touches exactly one synthetic cert (98-9437) and creates tags named ZZ_CI_*,
// matching the fixture markers Scripts/ci/seed-e2e.sql already cleans up. No real row is modified.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const os = require('os');
const { login, BASE } = require('./_lib/smoke-template');

if (BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) {
  console.error('REFUSING to run against a remote env. Localhost only.');
  process.exit(2);
}

const FIXTURE_CERT = '98-9437';
const EXT_TAG_A = 'ZZ_CI_EXT_437A';
const EXT_TAG_B = 'ZZ_CI_EXT_437B';
const INT_TAG_A = 'ZZ_CI_INT_437A';

// --- minimal RFC4180 CSV parse/serialize ---
function parseCsv(text) {
  const records = [];
  let field = '', record = [], inQuotes = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inQuotes) {
      if (c === '"') {
        if (text[i + 1] === '"') { field += '"'; i++; }
        else inQuotes = false;
      } else field += c;
    } else {
      if (c === '"') inQuotes = true;
      else if (c === ',') { record.push(field); field = ''; }
      else if (c === '\r') { /* skip */ }
      else if (c === '\n') { record.push(field); records.push(record); record = []; field = ''; }
      else field += c;
    }
  }
  if (field.length > 0 || record.length > 0) { record.push(field); records.push(record); }
  return records.filter(r => r.length > 1 || (r.length === 1 && r[0] !== ''));
}
const toCsvLine = fields => fields.map(f => `"${String(f ?? '').replace(/"/g, '""')}"`).join(',');

let fail = 0;
const check = (name, ok, detail = '') => {
  console.log(`  [${ok ? 'PASS' : 'FAIL'}] ${name}${detail ? ' - ' + detail : ''}`);
  if (!ok) fail++;
};

const findCol = (header, re) => header.findIndex(h => re.test(String(h).trim()));

(async () => {
  const browser = await chromium.launch({ headless: process.env.SMOKE_HEADLESS !== '0' });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await ctx.newPage();
  const tmpDir = os.tmpdir();

  // Upload a CSV, then apply it. Returns { preview, apply }.
  async function runImport(label, header, dataRow) {
    const file = path.join(tmpDir, `def437-${label}-${Date.now()}.csv`);
    fs.writeFileSync(file, toCsvLine(header) + '\r\n' + toCsvLine(dataRow) + '\r\n', 'utf8');

    const prev = await page.request.post(BASE + '/api/CertificateImport/Preview', {
      multipart: { csvFile: { name: path.basename(file), mimeType: 'text/csv', buffer: fs.readFileSync(file) } },
      timeout: 600000
    });
    const preview = await prev.json().catch(() => null);
    if (prev.status() !== 200 || !preview) {
      throw new Error(`[${label}] preview failed: HTTP ${prev.status()} ${JSON.stringify(preview).slice(0, 400)}`);
    }
    const importId = preview.importId || preview.ImportId;

    // Acknowledge both guards — this harness is deliberately driving the destructive path.
    let apply = null;
    for (const acks of [{}, { AcknowledgeOverwrites: true, AcknowledgeReassignments: true }]) {
      const res = await page.request.post(BASE + '/api/CertificateImport/Apply', {
        data: { ImportId: importId, Reason: 'DEF-437 verification', ...acks },
        timeout: 600000
      });
      apply = await res.json().catch(() => ({ raw: 'non-json', status: res.status() }));
      if (apply && apply.success) break;
    }
    fs.unlinkSync(file);
    return { preview, apply };
  }

  // Read the fixture's state straight from the database.
  //
  // Deliberately NOT through the tiles API: `?search=` is the known-slow path and exceeded 60s on
  // this box, but more importantly a read-back through the application would be partly circular —
  // it would exercise the same projection layer the fix touches. SQL is the ground truth, and this
  // is the layer the data loss actually happened at.
  function readState() {
    const conn = fs.readFileSync(path.join(__dirname, '..', '..', 'appsettings.Development.json'), 'utf8');
    const pw = conn.match(/Password=([^;"]*)/)[1];
    const sql = `SET NOCOUNT ON; SELECT c.Location, ISNULL(t.TagType,''), ISNULL(t.Name,'') ` +
                `FROM Certificates c LEFT JOIN CertificateTag ct ON ct.CertificatesId=c.Id ` +
                `LEFT JOIN Tags t ON t.Id=ct.TagsId WHERE c.Number='${FIXTURE_CERT}';`;
    const out = require('child_process').execFileSync('sqlcmd',
      ['-C', '-S', 'ANUKI01', '-d', 'GCCDatabase_Dev', '-U', 'GCC_DevUser', '-P', pw,
       '-I', '-b', '-h', '-1', '-W', '-s', '|', '-Q', sql],
      { encoding: 'utf8', timeout: 60000 });

    const lines = out.split('\n').map(l => l.trim()).filter(l => l && l.includes('|'));
    if (!lines.length) return null;
    const external = [], internal = [];
    let location = '';
    for (const line of lines) {
      const [loc, type, name] = line.split('|');
      location = loc;
      if (!name) continue;
      (type.toLowerCase() === 'internal' ? internal : external).push(name);
    }
    return { external: external.sort(), internal: internal.sort(), location };
  }
  const readTags = async () => readState();

  // Drop the fixture so PHASE 1 is a CREATE rather than an overwrite.
  //
  // This is not tidiness — it is what makes PHASE 1 reliable. An import that overwrites an existing
  // certificate first writes a pre-import safety backup, and that backup calls the same
  // single-certificate export that takes minutes on this box; it has timed out and ABORTED the
  // import outright ("the safety backup ... could not be created"). When that happens to PHASE 1
  // the fixture is never seeded, and PHASE 2 then "fails" against a certificate that had no tags to
  // lose — a false negative that looks exactly like the defect.
  //
  // Scoped to the one synthetic 98-9xxx row this script owns. Child rows go first (8 tables carry
  // an FK to Certificates); no real certificate is touched.
  function dropFixture() {
    const conn = fs.readFileSync(path.join(__dirname, '..', '..', 'appsettings.Development.json'), 'utf8');
    const pw = conn.match(/Password=([^;"]*)/)[1];
    // FK column names read from sys.foreign_key_columns, not assumed: TWO of the eight use
    // `CertificatesId` (EF's many-to-many convention) and the rest `CertificateId`. Guessing one
    // exception and hardcoding it got CertificatePartners wrong and failed the whole delete.
    const children = [
      ['CertificateDocuments', 'CertificateId'],
      ['CertificateProductIds', 'CertificateId'],
      ['CertificateTag', 'CertificatesId'],
      ['CustomFieldValues', 'CertificateId'],
      ['CertificateStatusHistory', 'CertificateId'],
      ['CertificatePartners', 'CertificatesId'],
      ['CertificateCountries', 'CertificateId'],
      ['CertificateCertificationTypeSubcategories', 'CertificateId'],
    ];
    const idExpr = `(SELECT Id FROM Certificates WHERE Number='${FIXTURE_CERT}')`;
    const sql = 'SET NOCOUNT ON; BEGIN TRAN; ' +
      children.map(([t, col]) => `DELETE FROM ${t} WHERE ${col} IN ${idExpr};`).join(' ') +
      ` DELETE FROM Certificates WHERE Number='${FIXTURE_CERT}'; COMMIT;`;
    try {
      require('child_process').execFileSync('sqlcmd',
        ['-C', '-S', 'ANUKI01', '-d', 'GCCDatabase_Dev', '-U', 'GCC_DevUser', '-P', pw,
         '-I', '-b', '-Q', sql], { encoding: 'utf8', timeout: 60000 });
    } catch (e) {
      // execFileSync puts the whole argv in e.message, which includes -P <password>. Never let
      // that reach a console or a checked-in log.
      const detail = String(e.stdout || e.stderr || '').split('\n').filter(Boolean).slice(-3).join(' | ');
      throw new Error('fixture cleanup failed: ' + (detail || 'sqlcmd returned a non-zero exit code'));
    }
  }

  try {
    await login(page, 'admin');

    // ---------- Build a valid base row from a real cert (via the tiles API) ----------
    // Deliberately NOT via /Certificate/ExportCertificates: even a single-id export exceeds 60s on
    // this dev box (the known export/search perf issue), and nothing here needs a real CSV export —
    // the tile carries every value required to compose a valid import row.
    const tiles = await page.request.get(BASE + '/api/certificate-tiles-internal?page=1&pageSize=1', { timeout: 60000 });
    const tj = await tiles.json();
    const seed = (tj.certificates || tj.data || tj.items || [])[0] || {};
    check('tiles API returned a seed cert', !!seed.certificateNumber, `seed=${seed.certificateNumber}`);
    if (!seed.certificateNumber) throw new Error('No seed cert available from tiles API');

    // Header names taken verbatim from the importer's own mapping arrays
    // (CertificateImportService.cs:77-130), so every column is guaranteed to resolve.
    const header = ['Certificate #', 'Company', 'Product Name', 'Certification Type',
                    'Status', 'Effective Date', 'Expiration Date', 'Location',
                    'External Tag', 'Internal Tag'];
    const base = [FIXTURE_CERT, seed.companyName, seed.productName, seed.certificationType,
                  seed.status, seed.effectiveDate, seed.expirationDate, 'DEF437 Fixture',
                  '', ''];
    const cNum = 0, cLoc = 7, cExt = 8, cInt = 9;
    console.log(`  -> base row from ${seed.certificateNumber}: ${seed.companyName} / ${seed.productName} / ${seed.certificationType}`);

    // ---------- PHASE 1: create the fixture with both tag types ----------
    console.log('\nPHASE 1 — create fixture ' + FIXTURE_CERT + ' with one external + one internal tag');
    dropFixture();
    check('fixture dropped before create (PHASE 1 is a create, not an overwrite)', readState() === null);

    const create = base.slice();
    create[cExt] = EXT_TAG_A;
    create[cInt] = INT_TAG_A;
    const r1 = await runImport('create', header, create);
    check('phase 1 applied', !!(r1.apply && r1.apply.success), JSON.stringify(r1.apply).slice(0, 200));

    let t = await readTags();
    check('fixture exists', t !== null);
    if (!t) throw new Error('Fixture cert not found after create');
    // Hard stop: every later phase asserts that a tag SURVIVES something. If the fixture was not
    // seeded, those assertions can only fail, and they would fail for the wrong reason — which is
    // precisely what happened on the run before this guard existed.
    check('fixture has the external tag', t.external.includes(EXT_TAG_A), `external=[${t.external}]`);
    check('fixture has the internal tag', t.internal.includes(INT_TAG_A), `internal=[${t.internal}]`);
    if (!t.external.includes(EXT_TAG_A) || !t.internal.includes(INT_TAG_A)) {
      throw new Error('ABORT: fixture was not seeded with both tag types — later phases would be false negatives');
    }

    // Company, Product Name and Certification Type are required on EVERY row, edits included
    // (CertificateImportService.cs:1409-1416), so a "only the columns I'm changing" file is not
    // actually possible — these four always travel. Worth knowing: the import help text implies
    // otherwise. Omitting them errors the row, and an errored row never reaches the apply path at
    // all, which would make every assertion below vacuously green.
    const MANDATORY = [0, 1, 2, 3]; // Certificate #, Company, Product Name, Certification Type
    const buildFile = (extraCols, overrides = {}) => {
      const keep = [...MANDATORY, ...extraCols];
      const h = keep.map(i => header[i]);
      const d = keep.map(i => (i in overrides ? overrides[i] : base[i]));
      return [h, d];
    };

    // ---------- PHASE 2: the defect — edit with NO tag columns ----------
    console.log('\nPHASE 2 — edit Location with NO tag columns present (the defect)');
    const newLoc = 'DEF437 Phase2 ' + new Date().toISOString().slice(11, 19);
    const [h2, d2] = buildFile([cLoc], { [cLoc]: newLoc });
    check('phase 2 file carries no tag column',
      !h2.some(h => /tag/i.test(h)), `header=[${h2.join('|')}]`);

    const r2 = await runImport('notagcols', h2, d2);
    check('phase 2 preview is applicable (row not errored)',
      r2.preview.canApply !== false, `canApply=${r2.preview.canApply}`);
    // The change list lives at modifiedRows[].changes (CertificateImportController.cs:315-326).
    // Assert the row IS present first: scanning an empty list for an absent entry passes for the
    // wrong reason, which is exactly what an earlier cut of this check did.
    const modRows2 = r2.preview.modifiedRows || [];
    check('phase 2 preview contains the modified row (so the scan below is meaningful)',
      modRows2.length >= 1, `modifiedRows=${modRows2.length}`);
    const tagChanges2 = modRows2.flatMap(r => r.changes || [])
      .filter(c => /^(tags|internal tags)$/i.test(String(c.fieldName ?? c.FieldName ?? '')));
    check('preview reports NO tag change (DetectChanges is gated)',
      tagChanges2.length === 0,
      tagChanges2.length ? JSON.stringify(tagChanges2).slice(0, 200) : 'no Tags entry among the reported changes');
    check('phase 2 applied', !!(r2.apply && r2.apply.success), JSON.stringify(r2.apply).slice(0, 200));

    t = await readTags();
    check('PHASE 2 — Location was updated', t.location === newLoc, `location="${t.location}"`);
    check('PHASE 2 — EXTERNAL tag survived an import with no tag column',
      t.external.includes(EXT_TAG_A), `external=[${t.external}]`);
    check('PHASE 2 — INTERNAL tag survived an import with no tag column',
      t.internal.includes(INT_TAG_A), `internal=[${t.internal}]`);

    // ---------- PHASE 3: partition — external column only ----------
    // Re-seed first. On the PRE-FIX binary phase 2 wipes both tag sets, so without this every
    // later phase inherits an empty certificate and its assertions fail (or pass) for reasons that
    // have nothing to do with what the phase is testing. Each phase must start from a known state.
    await runImport('reseed3', header, create);
    let seeded = await readTags();
    check('phase 3 re-seeded (both tag sets restored)',
      seeded.external.includes(EXT_TAG_A) && seeded.internal.includes(INT_TAG_A),
      `external=[${seeded.external}] internal=[${seeded.internal}]`);

    console.log('\nPHASE 3 — edit with ONLY the External Tag column present');
    const [h3, d3] = buildFile([cExt], { [cExt]: EXT_TAG_B });
    check('phase 3 file carries no INTERNAL tag column',
      !h3.some(h => /internal/i.test(h)), `header=[${h3.join('|')}]`);

    const r3 = await runImport('extonly', h3, d3);
    check('phase 3 preview is applicable (row not errored)',
      r3.preview.canApply !== false, `canApply=${r3.preview.canApply}`);
    check('phase 3 applied', !!(r3.apply && r3.apply.success), JSON.stringify(r3.apply).slice(0, 200));

    t = await readTags();
    check('PHASE 3 — external tag was REPLACED as instructed',
      t.external.includes(EXT_TAG_B) && !t.external.includes(EXT_TAG_A), `external=[${t.external}]`);
    check('PHASE 3 — internal tag PRESERVED (its column was absent)',
      t.internal.includes(INT_TAG_A), `internal=[${t.internal}]`);

    // ---------- PHASE 4: deliberate clear still works ----------
    await runImport('reseed4', header, create);
    seeded = await readTags();
    check('phase 4 re-seeded (both tag sets restored)',
      seeded.external.includes(EXT_TAG_A) && seeded.internal.includes(INT_TAG_A),
      `external=[${seeded.external}] internal=[${seeded.internal}]`);

    console.log('\nPHASE 4 — Internal Tag column PRESENT but blank (deliberate clear)');
    const [h4, d4] = buildFile([cInt], { [cInt]: '' });
    const r4 = await runImport('intblank', h4, d4);
    check('phase 4 preview is applicable (row not errored)',
      r4.preview.canApply !== false, `canApply=${r4.preview.canApply}`);
    check('phase 4 applied', !!(r4.apply && r4.apply.success), JSON.stringify(r4.apply).slice(0, 200));

    t = await readTags();
    check('PHASE 4 — internal tag CLEARED by a present-but-blank cell',
      !t.internal.includes(INT_TAG_A), `internal=[${t.internal}]`);
    check('PHASE 4 — external tag untouched (its column was absent)',
      t.external.includes(EXT_TAG_A), `external=[${t.external}]`);

    console.log(`\n  Fixture left in place: ${FIXTURE_CERT} (matches the '98-9%' marker seed-e2e.sql cleans).`);
  } catch (e) {
    console.error('\n  ERROR: ' + e.message);
    fail++;
  } finally {
    await browser.close();
  }

  console.log(`\n${fail === 0 ? 'DEF-437: ALL PASS' : `DEF-437: ${fail} FAILURE(S)`}`);
  process.exit(fail === 0 ? 0 : 1);
})();
