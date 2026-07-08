// SECURITY — RBAC negative-path matrix (P0). The most security-relevant assertions in the app:
// a page that SHOULD deny must return 401/403 or redirect (302) to login — never 200. Silent
// regressions here (a gated page starts 200-ing) are privilege escalation. History: DEF-204/206/208.
//
// Also verifies POSITIVE controls (each role CAN reach its own allowed pages) so the suite isn't
// trivially green by denying everything.
//
// Denied = HTTP in {401, 403, 302}. Allowed = 200. Source of truth for the matrix:
// docs/tracking/e2e-smoke-plan.md role-to-view access table + [Authorize] attributes.
//
// Read-only. Run:  node Scripts/smoke-prod/_security/rbac-matrix.js

const H = require('./_lib/security-harness');

const DENIED = new Set([401, 403, 302]);

// role: 'public' => anonymous (no login). Others log in with the named test account.
// expect: 'deny' => must be in DENIED; 'allow' => must be 200.
const MATRIX = [
  // --- Public (anonymous) must be denied every gated surface ---
  { role: 'public',   path: '/Certificate/CertificateTileViewExternal', expect: 'deny' },
  { role: 'public',   path: '/Certificate/CertificateTileViewInternal', expect: 'deny' },
  { role: 'public',   path: '/Dashboard/Index',                         expect: 'deny' },
  { role: 'public',   path: '/Certificate/Create',                      expect: 'deny' },
  { role: 'public',   path: '/Admin/UserManagement',                    expect: 'deny' },
  { role: 'public',   path: '/Product/Management',                      expect: 'deny' },
  { role: 'public',   path: '/api/CertificateTileExternal?page=1&pageSize=5', expect: 'deny' },
  // positive control: public tile view IS public
  { role: 'public',   path: '/Certificate/CertificateTileView',         expect: 'allow' },

  // --- External: own external view yes; internal/admin/create/dashboard no ---
  { role: 'external', path: '/Certificate/CertificateTileViewExternal', expect: 'allow' },
  { role: 'external', path: '/Certificate/CertificateTileViewInternal', expect: 'deny' },
  { role: 'external', path: '/Certificate/Create',                      expect: 'deny' },
  { role: 'external', path: '/Dashboard/Index',                         expect: 'deny' },
  // External CAN view Product/Management (ProductController = [Authorize(Policy="ExternalAccess")]).
  // The restriction is the hidden ACTIONS COLUMN (verified in _views/product-mgmt.js), not page denial.
  // Write actions on this controller are separately gated [Authorize(Policy="AnalystOrAdmin")].
  { role: 'external', path: '/Product/Management',                      expect: 'allow' },
  { role: 'external', path: '/Admin/UserManagement',                    expect: 'deny' },

  // --- Analyst: internal + dashboard + create + product yes; admin no ---
  { role: 'analyst',  path: '/Certificate/CertificateTileViewInternal', expect: 'allow' },
  { role: 'analyst',  path: '/Dashboard/Index',                         expect: 'allow' },
  { role: 'analyst',  path: '/Certificate/Create',                      expect: 'allow' },
  { role: 'analyst',  path: '/Product/Management',                      expect: 'allow' },
  { role: 'analyst',  path: '/Admin/UserManagement',                    expect: 'deny' },
  { role: 'analyst',  path: '/Admin/RoleManagement',                    expect: 'deny' },
  { role: 'analyst',  path: '/Admin/Partners',                          expect: 'deny' },

  // --- Admin: full access ---
  { role: 'admin',    path: '/Admin/UserManagement',                    expect: 'allow' },
  { role: 'admin',    path: '/Certificate/CertificateTileViewInternal', expect: 'allow' },
  { role: 'admin',    path: '/Dashboard/Index',                         expect: 'allow' },
];

async function run() {
  H.guardRemote();
  console.log(`\n[security] rbac-matrix — ${H.BASE}\n`);
  const { results, record } = H.makeRecorder('rbac-matrix');
  const browser = await H.chromium.launch({ headless: true });

  // One context per role, reused across that role's rows.
  const contexts = {};
  const loginState = {};
  try {
    const roles = [...new Set(MATRIX.map((m) => m.role))];
    for (const role of roles) {
      const ctx = await H.newContext(browser);
      contexts[role] = ctx;
      if (role === 'public') { loginState[role] = true; continue; }
      const { ok } = await H.login(ctx, role);
      loginState[role] = ok;
      if (!ok) record(`rbac.${role}.login`, `${role} login`, 'BLOCKED', 'login failed — check test account');
    }

    for (const row of MATRIX) {
      const ctx = contexts[row.role];
      if (loginState[row.role] === false) {
        record(`rbac.${row.role}.${row.path}`, `${row.role} -> ${row.path}`, 'BLOCKED', 'role login failed');
        continue;
      }
      const code = await H.status(ctx, row.path);
      const denied = DENIED.has(code);
      let pass, notes;
      if (row.expect === 'deny') {
        pass = denied;
        notes = `HTTP ${code} (expect deny 401/403/302)` + (pass ? '' : ' — ⚠ NOT DENIED');
      } else {
        pass = code === 200;
        notes = `HTTP ${code} (expect allow 200)`;
      }
      // A denied-when-should-allow is a WARN (functional/test-data), NOT a security FAIL.
      // A allowed-when-should-deny is a security FAIL (the dangerous direction).
      const verdict = pass ? 'PASS' : (row.expect === 'deny' ? 'FAIL' : 'WARN');
      record(`rbac.${row.role}.${row.path}`, `${row.role} -> ${row.path}`, verdict, notes);
    }
  } finally {
    for (const c of Object.values(contexts)) { try { await c.close(); } catch {} }
    await browser.close();
  }

  return H.summarize('rbac-matrix', null, results);
}

module.exports = { run };
if (require.main === module) {
  run().then((s) => process.exit(s.fail ? 1 : 0));
}
