// QC #177 + QC #178 / DEF-199 verification.
//
// QC #177 — soft-deleted companies must not appear in the public Company filter.
// QC #178 — cert-level category override must surface in the public Category filter.
//
// LOCALHOST destructive matrix (3 snapshots, with restore):
//   T-base  → snapshot of /api/certificate-tiles/filters
//   T-mut   → after soft-deleting Acme (Id=59) + setting cert 38 ProductCategoryId=2
//   T-restore → after reverting both
//
// GCCWEBAPP read-only deploy probe:
//   Snapshot /api/certificate-tiles/filters
//   Assert every returned company is IsActive && !IsDeleted (we can't query the
//   prod DB directly from here, but every company in the response must be present
//   in the implied "non-deleted set" — sanity check the shape and that the
//   endpoint is reachable)

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const TARGET = process.env.TARGET || (BASE.includes('gccwebapp') ? 'live' : 'local');
const BASIC = BASE.includes('gccwebapp')
  ? { username: 'gcc', password: 'Cert-Test-Access-2026!' }
  : undefined;
const FILTER_URL = `${BASE}/api/certificate-tiles/filters`;

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

// Run SQL via PowerShell (single-line SQL, no here-strings)
function runSql(sql) {
  const cs = `Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;TrustServerCertificate=True;`;
  const safeSql = sql.replace(/'/g, "''"); // double single-quote for PS string literal
  const cmd =
    `$conn = New-Object System.Data.SqlClient.SqlConnection '${cs}';` +
    ` $conn.Open();` +
    ` $cmd = $conn.CreateCommand();` +
    ` $cmd.CommandText = '${safeSql}';` +
    ` [void]$cmd.ExecuteNonQuery();` +
    ` $conn.Close();`;
  execSync(`powershell -NoProfile -Command "${cmd.replace(/"/g, '\\"')}"`, { stdio: 'pipe' });
}

async function snapshot(ctx, label) {
  const resp = await ctx.request.get(FILTER_URL);
  const body = await resp.json().catch(() => null);
  fs.writeFileSync(path.join(OUT, `qc177-${TARGET}-${label}.json`), JSON.stringify(body, null, 2));
  return { status: resp.status(), body };
}

function getCompanies(body) {
  // Endpoint returns { success, data: { companies: [{id,name},...], productCategories: [...], ... } }
  const data = body?.data || body?.Data || body;
  const companies = data?.companies || data?.Companies || [];
  return companies.map(c => ({ id: c.id || c.Id, name: c.name || c.Name })).filter(c => c.id);
}
function getCategoryIds(body) {
  // productCategoriesHierarchy = [{id,name,level,types}]; productCategories is a string[] of names
  const data = body?.data || body?.Data || body;
  const cats = data?.productCategoriesHierarchy || data?.ProductCategoriesHierarchy || [];
  return cats.map(c => c.id || c.Id).filter(Boolean);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });

  console.log(`\nQC #177 + #178 verification — ${TARGET.toUpperCase()} (${BASE})\n`);

  try {
    if (TARGET === 'local') {
      // ----- T-base baseline -----
      console.log('--- T-base — baseline snapshot ---');
      const base = await snapshot(ctx, 'base');
      const baseCompanies = getCompanies(base.body);
      const baseCatIds = getCategoryIds(base.body);
      console.log(`  HTTP ${base.status}  companies=${baseCompanies.length}  categories=${baseCatIds.length}`);
      record('QC177-base-200', 'Filter endpoint returns 200', base.status === 200 ? 'PASS' : 'FAIL', `HTTP ${base.status}`);
      const acmePresent = baseCompanies.some(c => c.name === 'Acme Corporation');
      record('QC177-base-acme-in', 'Acme present in baseline (sanity)',
        acmePresent ? 'PASS' : 'WARN',
        `${baseCompanies.length} companies`);

      // ----- mutate: soft-delete Acme + override cert 38 category -----
      console.log('\n--- mutation: soft-delete Acme(59) + override cert 38 ProductCategoryId=2 ---');
      runSql(`UPDATE Companies SET IsDeleted = 1 WHERE Id = 59; UPDATE Certificates SET ProductCategoryId = 2 WHERE Id = 38;`);
      console.log('  mutations applied');

      // ----- T-mut snapshot -----
      console.log('\n--- T-mut — after mutation ---');
      const mut = await snapshot(ctx, 'mut');
      const mutCompanies = getCompanies(mut.body);
      const mutCatIds = getCategoryIds(mut.body);
      const acmeGone = !mutCompanies.some(c => c.name === 'Acme Corporation');
      const category2Surfaces = mutCatIds.includes(2);
      console.log(`  companies=${mutCompanies.length}  categories=${mutCatIds.length}  cat2-in-list=${category2Surfaces}`);

      record('QC177-mut-acme-gone', 'Acme (soft-deleted) NOT in company filter',
        acmeGone ? 'PASS' : 'FAIL',
        `companies=${mutCompanies.length} (expect ${baseCompanies.length - 1}=${baseCompanies.length - 1})`);
      record('QC177-mut-count-1-less', 'Company count drops by exactly 1',
        mutCompanies.length === baseCompanies.length - 1 ? 'PASS' : 'FAIL',
        `before=${baseCompanies.length} after=${mutCompanies.length}`);
      record('QC178-mut-cat-override', 'Cert-level Category override (id=2) surfaces in filter',
        category2Surfaces ? 'PASS' : 'FAIL',
        `cat IDs=${JSON.stringify(mutCatIds)}`);

      // ----- restore -----
      console.log('\n--- restore: un-delete Acme + clear cert 38 override ---');
      runSql(`UPDATE Companies SET IsDeleted = 0 WHERE Id = 59; UPDATE Certificates SET ProductCategoryId = NULL WHERE Id = 38;`);
      console.log('  mutations reverted');

      // ----- T-restore -----
      console.log('\n--- T-restore — after revert ---');
      const rest = await snapshot(ctx, 'restore');
      const restCompanies = getCompanies(rest.body);
      const restCatIds = getCategoryIds(rest.body);
      const acmeBack = restCompanies.some(c => c.name === 'Acme Corporation');
      const cat2Gone = !restCatIds.includes(2) || baseCatIds.includes(2);
      record('QC177-restore-acme-back', 'Acme returns to filter after restore',
        acmeBack ? 'PASS' : 'FAIL',
        `companies=${restCompanies.length}`);
      record('QC177-restore-counts-match', 'Company count matches baseline post-restore',
        restCompanies.length === baseCompanies.length ? 'PASS' : 'FAIL',
        `base=${baseCompanies.length} restore=${restCompanies.length}`);
      record('QC178-restore-cat-aligned', 'Category set matches baseline post-restore',
        JSON.stringify([...restCatIds].sort()) === JSON.stringify([...baseCatIds].sort()) ? 'PASS' : 'FAIL',
        `base=${JSON.stringify(baseCatIds.sort())} restore=${JSON.stringify(restCatIds.sort())}`);

    } else {
      // ----- LIVE deploy probe -----
      console.log('--- Live deploy probe — read-only snapshot ---');
      const live = await snapshot(ctx, 'live');
      console.log(`  HTTP ${live.status}`);
      record('QC177-live-200', 'Public filter endpoint reachable on prod', live.status === 200 ? 'PASS' : 'FAIL', `HTTP ${live.status}`);

      const liveCompanies = getCompanies(live.body);
      const liveCatIds = getCategoryIds(live.body);
      console.log(`  companies=${liveCompanies.length}  categories=${liveCatIds.length}`);
      console.log(`  companies on prod filter: ${liveCompanies.map(c => c.name).join(', ')}`);

      // The endpoint having ANY companies + categories proves the code is loading and
      // the IsActive && !IsDeleted predicate is at least not filtering everything out.
      // A stronger assertion would need DB-side truth, which we don't have on prod here.
      record('QC177-live-shape', 'Filter response carries companies + categories',
        liveCompanies.length > 0 && liveCatIds.length > 0 ? 'PASS' : 'FAIL',
        `companies=${liveCompanies.length} categories=${liveCatIds.length}`);
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, `qc177-${TARGET}-results.json`), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
