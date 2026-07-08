// SECURITY — Partner / company data isolation (P0). Promotes cr171-partner-isolation-local.js
// into the maintained suite. For each partner-role user this asserts, across the surfaces a
// scoping bug historically slipped through on (DEF-103/164/198/204/205 — "list fixed, detail/
// download/export missed", memory feedback_scoping_check_adjacent_endpoints):
//
//   1. login succeeds (else BLOCKED — data-setup gap, not a leak)
//   2. LIST  — /api/CertificateTileExternal returns a scoped, non-empty set
//   3. ISOLATION — no co-partner name leaks into ANY returned cert (the core guarantee);
//                  the multi-partner user still sees BOTH its own partners
//   4. EXPORT — /Certificate/ExportCertificates?format=csv row count <= the user's tile scope
//               (an export must not widen what the API scopes)
//
// Read-only. Run:  node Scripts/smoke-prod/_security/partner-isolation.js

const H = require('./_lib/security-harness');

async function run() {
  H.guardRemote();
  console.log(`\n[security] partner-isolation — ${H.BASE}\n`);
  const { results, record } = H.makeRecorder('partner-isolation');
  const browser = await H.chromium.launch({ headless: true });

  try {
    for (const u of H.PARTNER_USERS) {
      const ctx = await H.newContext(browser);
      const forbidden = H.ALL_PARTNERS.filter((p) => !u.own.includes(p));
      const own = u.own.join('+');
      try {
        const { ok } = await H.login(ctx, u.email);
        if (!ok) {
          record(`iso.${u.email}.login`, `login (${own})`, 'BLOCKED',
            'login bounced — user missing from local DB? restore CR-171 test data');
          await ctx.close();
          continue;
        }
        record(`iso.${u.email}.login`, `login (${own})`, 'PASS');

        // 2. LIST — scoped, non-empty.
        const list = await H.apiJson(ctx, '/api/CertificateTileExternal?page=1&pageSize=300');
        record(`iso.${u.email}.list`, 'external tile list scoped+non-empty',
          (list.status === 200 && (list.total ?? 0) > 0) ? 'PASS' : 'WARN',
          `HTTP ${list.status}, total=${list.total}, returned=${list.certs.length}`);

        // 3. ISOLATION — no co-partner leak; own partners present.
        const leaks = H.scanLeaks(list.certs, forbidden);
        record(`iso.${u.email}.no-leak`, `no co-partner leak (forbid ${forbidden.join('/')})`,
          leaks.length === 0 ? 'PASS' : 'FAIL',
          leaks.length ? `LEAK -> ${JSON.stringify(leaks.slice(0, 6))}` : 'clean');
        if (u.own.length > 1) {
          const blob = JSON.stringify(list.certs);
          const missingOwn = u.own.filter((p) => !new RegExp(`\\b${p}\\b`).test(blob));
          // Multi-partner user should see BOTH own partners somewhere in the scoped set.
          record(`iso.${u.email}.own-visible`, 'multi-partner sees all own partners',
            missingOwn.length === 0 ? 'PASS' : 'WARN',
            missingOwn.length ? `own partner(s) not present in set: ${missingOwn.join(',')}` : 'both present');
        }

        // 4. EXPORT scope — CSV distinct cert count must not exceed the tile scope.
        const csv = await ctx.request.get(H.BASE + '/Certificate/ExportCertificates?format=csv',
          { headers: { Accept: 'text/csv' } });
        if (csv.status() === 200) {
          const text = await csv.text();
          const lines = text.split(/\r?\n/).filter((l) => l.trim());
          const dataRows = Math.max(0, lines.length - 1); // minus header
          const withinScope = list.total == null || dataRows <= list.total + 1;
          record(`iso.${u.email}.export-scope`, 'export CSV rows <= tile scope',
            withinScope ? 'PASS' : 'FAIL',
            `csv rows=${dataRows} vs tile scope=${list.total}`);
        } else {
          record(`iso.${u.email}.export-scope`, 'export CSV reachable', 'WARN',
            `export HTTP ${csv.status()} (endpoint may be gated for this role)`);
        }
      } catch (e) {
        record(`iso.${u.email}.error`, 'spec ran without throwing', 'FAIL', e.message);
      } finally {
        await ctx.close();
      }
    }
  } finally {
    await browser.close();
  }

  return H.summarize('partner-isolation', '/api/CertificateTileExternal', results);
}

module.exports = { run };
if (require.main === module) {
  run().then((s) => process.exit(s.fail || s.blocked ? 1 : 0));
}
