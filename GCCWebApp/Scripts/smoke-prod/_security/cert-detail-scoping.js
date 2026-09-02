// SECURITY — Certificate DETAIL-view scoping (P0, gaps C2 + C9). The tile LIST scoping is
// covered by partner-isolation.js; this closes the historically-missed adjacent surface: the
// per-certificate DETAIL pages (memory feedback_scoping_check_adjacent_endpoints — "list fixed,
// detail/single/download missed", DEF-103/164/198/204/205).
//
// Three detail views (Controllers/CertificateController.cs):
//   CertificateInternalView/{num}  [AnalystOrAdmin]  — ShowInternalData=true
//   CertificateExternalView/{num}  [ExternalAccess]  — own-company + PublicStatuses else 404
//   CertificateSingleView/{num}    [AllowAnonymous]  — anonymous sees only public+not-expired
//
// Fixtures are DISCOVERED at runtime from the tile APIs (not hardcoded cert numbers — those rot):
//   adminSet  = all certs (internal API)         extSet = the external user's authorized certs
//   ownCert   = a cert the external user may see  foreignCert = a public-status cert of ANOTHER
//                                                  company (404 must be a SCOPING denial, not status)
//   publicCert / nonPublicCert = for the anonymous status-gate.
//
// Assertions:
//   A1  external -> ExternalView(ownCert)       = 200                (allowed, own company)
//   A2  external -> ExternalView(ownCert)       0 .tag.internal      (internal tags never leak)
//   A3  external -> ExternalView(foreignCert)   = 404   *** C9 ***   (cross-company detail must NOT leak)
//   A4  anon     -> SingleView(publicCert)      = 200                (public cert visible)
//   A5  anon     -> SingleView(publicCert)      0 .tag.internal      (internal tags never leak)
//   A6  anon     -> SingleView(nonPublicCert)   = 404   *** C2 ***   (non-public gated from public)
//   A7  external -> InternalView(ownCert)       denied (302/403)     (external cannot reach internal view)
//   A8  anon     -> InternalView(ownCert)       denied (302/403/401)
//   P1  admin    -> InternalView(ownCert)       = 200                (positive control: internal view works)
//
// Read-only. Run:  node Scripts/smoke-prod/_security/cert-detail-scoping.js

const H = require('./_lib/security-harness');

const PUBLIC_STATUS = /client approved\s*-?\s*paid|republished/i;
const num = (c) => c.certificateNumber || c.CertificateNumber;
const co  = (c) => c.companyName || c.CompanyName || '';
const st  = (c) => c.status || c.Status || '';

async function pageStatus(ctx, path) { return H.status(ctx, path); }

// Navigate a logged-in/anon page and count internal-tag pills rendered in the DOM.
async function countInternalTags(page, url) {
  await page.goto(H.BASE + url, { waitUntil: 'domcontentloaded' });
  return page.locator('.tag.internal').count();
}

async function run() {
  H.guardRemote();
  console.log(`\n[security] cert-detail-scoping — ${H.BASE}\n`);
  const { results, record } = H.makeRecorder('cert-detail-scoping');
  const browser = await H.chromium.launch({ headless: true });

  try {
    // ---- Discover fixtures ----
    const adminCtx = await H.newContext(browser);
    const admin = await H.login(adminCtx, 'admin');
    if (!admin.ok) {
      record('detail.discover.admin', 'admin login for discovery', 'BLOCKED', 'admin login failed');
      await adminCtx.close(); await browser.close();
      return H.summarize('cert-detail-scoping', null, results);
    }
    const adminSet = (await H.apiJson(adminCtx, '/api/certificate-tiles-internal?page=1&pageSize=300')).certs;

    const extCtx = await H.newContext(browser);
    const ext = await H.login(extCtx, 'external');
    if (!ext.ok) {
      record('detail.discover.external', 'external login for discovery', 'BLOCKED', 'external login failed');
      await adminCtx.close(); await extCtx.close(); await browser.close();
      return H.summarize('cert-detail-scoping', null, results);
    }
    const extSet = (await H.apiJson(extCtx, '/api/CertificateTileExternal?page=1&pageSize=300')).certs;
    const extNumbers = new Set(extSet.map(num));
    const extCompanies = new Set(extSet.map(co).filter(Boolean));

    const ownCert = extSet[0] ? num(extSet[0]) : null;
    // Foreign, PUBLIC-status cert of a company the external user is NOT scoped to — so a 404
    // is unambiguously a company-scope denial, not a status gate.
    const foreignPub = adminSet.find((c) =>
      !extNumbers.has(num(c)) && !extCompanies.has(co(c)) && PUBLIC_STATUS.test(st(c)));
    const foreignCert = foreignPub ? num(foreignPub) : null;
    const publicCert = ownCert; // external's own set is public + not expired by construction
    const nonPublicRow = adminSet.find((c) => !PUBLIC_STATUS.test(st(c)));
    const nonPublicCert = nonPublicRow ? num(nonPublicRow) : null;

    console.log(`  discovered: own=${ownCert} (co=${co(extSet[0]||{})}) foreign=${foreignCert} (co=${foreignPub?co(foreignPub):'-'}, st=${foreignPub?st(foreignPub):'-'}) nonPublic=${nonPublicCert} (st=${nonPublicRow?st(nonPublicRow):'-'})`);
    record('detail.discover', 'fixtures discovered (adminSet, extSet)', 'PASS',
      `adminSet=${adminSet.length} extSet=${extSet.length} extCompanies=[${[...extCompanies].join(',')}]`);

    // ---- A1 / A2 : external own-cert view ----
    if (ownCert) {
      const s = await pageStatus(extCtx, `/Certificate/CertificateExternalView/${ownCert}`);
      record('detail.A1.ext-own-200', `external -> ExternalView(${ownCert})`,
        s === 200 ? 'PASS' : 'FAIL', `${H.fmtStatus(s)} (expect 200)`);
      const internalTags = await countInternalTags(ext.page, `/Certificate/CertificateExternalView/${ownCert}`);
      record('detail.A2.ext-no-internal-tags', 'external detail hides internal tags',
        internalTags === 0 ? 'PASS' : 'FAIL', `.tag.internal count=${internalTags} (expect 0)`);
    } else {
      record('detail.A1.ext-own', 'external own cert available', 'BLOCKED', 'external user has no visible certs');
    }

    // ---- A3 : *** C9 *** external foreign-cert detail must 404 ----
    if (foreignCert) {
      const s = await pageStatus(extCtx, `/Certificate/CertificateExternalView/${foreignCert}`);
      record('detail.A3.ext-foreign-404', `external -> ExternalView(${foreignCert}) [other company]`,
        s === 404 ? 'PASS' : 'FAIL',
        `${H.fmtStatus(s)} (expect 404)` + (s === 200 ? ' — ⚠ CROSS-COMPANY DETAIL LEAK' : ''));
    } else {
      record('detail.A3.ext-foreign', 'foreign public cert available for scoping test', 'WARN',
        'no foreign public-status cert of another company found in dataset');
    }

    // ---- A4 / A5 / A6 : anonymous single view status-gate ----
    const anonCtx = await H.newContext(browser);
    const anonPage = await anonCtx.newPage();
    if (publicCert) {
      const s = await pageStatus(anonCtx, `/Certificate/CertificateSingleView/${publicCert}`);
      record('detail.A4.anon-public-200', `anon -> SingleView(${publicCert})`,
        s === 200 ? 'PASS' : 'FAIL', `${H.fmtStatus(s)} (expect 200)`);
      const internalTags = await countInternalTags(anonPage, `/Certificate/CertificateSingleView/${publicCert}`);
      record('detail.A5.anon-no-internal-tags', 'public single view hides internal tags',
        internalTags === 0 ? 'PASS' : 'FAIL', `.tag.internal count=${internalTags} (expect 0)`);
    } else {
      record('detail.A4.anon-public', 'public cert available', 'BLOCKED', 'no public cert discovered');
    }
    if (nonPublicCert) {
      const s = await pageStatus(anonCtx, `/Certificate/CertificateSingleView/${nonPublicCert}`);
      record('detail.A6.anon-nonpublic-404', `anon -> SingleView(${nonPublicCert}) [non-public status]`,
        s === 404 ? 'PASS' : 'FAIL',
        `${H.fmtStatus(s)} (expect 404)` + (s === 200 ? ' — ⚠ NON-PUBLIC CERT EXPOSED TO PUBLIC' : ''));
    } else {
      record('detail.A6.anon-nonpublic', 'non-public cert available for status-gate test', 'WARN',
        'all certs are public status — cannot test the gate');
    }

    // ---- A7 / A8 : internal view is not reachable below Analyst ----
    if (ownCert) {
      const denied = new Set([302, 401, 403]);
      const se = await pageStatus(extCtx, `/Certificate/CertificateInternalView/${ownCert}`);
      record('detail.A7.ext-internal-denied', `external -> InternalView(${ownCert})`,
        denied.has(se) ? 'PASS' : 'FAIL', `${H.fmtStatus(se)} (expect 302/401/403)` + (se === 200 ? ' — ⚠ EXTERNAL REACHED INTERNAL VIEW' : ''));
      const sa = await pageStatus(anonCtx, `/Certificate/CertificateInternalView/${ownCert}`);
      record('detail.A8.anon-internal-denied', `anon -> InternalView(${ownCert})`,
        denied.has(sa) ? 'PASS' : 'FAIL', `${H.fmtStatus(sa)} (expect 302/401/403)`);
      // ---- P1 : positive control — admin CAN reach the internal view ----
      const sp = await pageStatus(adminCtx, `/Certificate/CertificateInternalView/${ownCert}`);
      record('detail.P1.admin-internal-200', `admin -> InternalView(${ownCert})`,
        sp === 200 ? 'PASS' : 'WARN', `${H.fmtStatus(sp)} (expect 200)`);
    }

    await anonCtx.close();
    await adminCtx.close();
    await extCtx.close();
  } catch (e) {
    record('detail.error', 'spec ran without throwing', 'FAIL', e.message);
  } finally {
    await browser.close();
  }

  return H.summarize('cert-detail-scoping', null, results);
}

module.exports = { run };
if (require.main === module) {
  run().then((s) => process.exit(s.fail || s.blocked ? 1 : 0));
}
