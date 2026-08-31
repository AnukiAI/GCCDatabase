// Phase 1 — RBAC access matrix. Every route × every role, graded against the documented policy.
//
// Grading note: an ASP.NET Core cookie-auth denial is a 302 to /Account/Login or
// /Account/AccessDenied, which after redirect-following is HTTP 200 on a page that is not the one
// asked for. Grading the final status code alone therefore scores every denial as a success. The
// effective status below treats "landed on an auth page I did not ask for" as 401-equivalent.

const { BASE, HEADLESS, NAV_TIMEOUT, ACCOUNTS, session, makeRecorder, chromium } = require('./_lib/regression-lib');

const ALLOW = 200, DENY = 401;

// Access classes, expanded per role below. Documented source of truth is the Routes Quick Reference
// in .claude/CLAUDE.md plus the [Authorize(Policy = ...)] attributes on each controller.
const PUBLIC    = { anon: ALLOW, external: ALLOW, external2: ALLOW, amazon: ALLOW, novi: ALLOW, analyst: ALLOW, admin: ALLOW };
const EXTERNAL  = { anon: DENY,  external: ALLOW, external2: ALLOW, amazon: ALLOW, novi: ALLOW, analyst: ALLOW, admin: ALLOW };
const ANALYST   = { anon: DENY,  external: DENY,  external2: DENY,  amazon: DENY,  novi: DENY,  analyst: ALLOW, admin: ALLOW };
const ADMIN     = { anon: DENY,  external: DENY,  external2: DENY,  amazon: DENY,  novi: DENY,  analyst: DENY,  admin: ALLOW };

const ROUTES = [
  // --- Public ---
  { path: '/',                                          label: 'Home',                     policy: 'AllowAnonymous', expect: PUBLIC },
  { path: '/Account/Login',                             label: 'Login page',               policy: 'AllowAnonymous', expect: PUBLIC, authPage: true },
  { path: '/Certificate/CertificateTileView',           label: 'Public tile view',         policy: 'AllowAnonymous', expect: PUBLIC },

  // --- External and up ---
  { path: '/Certificate/CertificateTileViewExternal',   label: 'External tile view',       policy: 'ExternalAccess', expect: EXTERNAL },
  { path: '/Product/Management',                        label: 'Product Management',       policy: 'ExternalAccess', expect: EXTERNAL },

  // --- Analyst and up ---
  { path: '/Certificate/CertificateTileViewInternal',   label: 'Internal tile view',       policy: 'AnalystOrAdmin', expect: ANALYST },
  { path: '/Certificate/Create',                        label: 'Certificate Create',       policy: 'AnalystOrAdmin', expect: ANALYST },
  { path: '/Certificate/Statistics',                    label: 'Certificate Statistics',   policy: 'AnalystOrAdmin', expect: ANALYST },
  { path: '/Dashboard/Index',                           label: 'Dashboard',                policy: 'AnalystOrAdmin', expect: ANALYST },
  { path: '/Company/Management',                        label: 'Company Management',       policy: 'AnalystOrAdmin', expect: ANALYST },
  { path: '/CertificateImport/Index',                   label: 'Certificate Import',       policy: 'AnalystOrAdmin', expect: ANALYST },
  { path: '/CertificateImport/History',                 label: 'Import History',           policy: 'AnalystOrAdmin', expect: ANALYST },

  // --- Admin only ---
  { path: '/Admin/Index',                               label: 'Admin home',               policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/UserManagement',                      label: 'User Management',          policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/RoleManagement',                      label: 'Role Management',          policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/Tags',                                label: 'Tags',                     policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/Partners',                            label: 'Partners',                 policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/CertificateTypes',                    label: 'Certificate Types',        policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/ProductTypes',                        label: 'Product Types',            policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/CertificateFields',                   label: 'Certificate Fields',       policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/CSFAttributes',                       label: 'CSF Attributes',           policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/Pathways',                            label: 'Pathways',                 policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/ProductManagement',                   label: 'Admin Product Mgmt',       policy: 'AdminOnly',      expect: ADMIN },
  { path: '/Admin/GoogleAnalytics',                     label: 'Google Analytics config',  policy: 'AdminOnly',      expect: ADMIN },
];

const ROLES = ['anon', 'external', 'external2', 'amazon', 'novi', 'analyst', 'admin'];

(async () => {
  const { rec, save } = makeRecorder('01-rbac-matrix');
  const startedAt = new Date().toISOString();
  console.log(`\n=== Phase 1: RBAC matrix — ${ROUTES.length} routes x ${ROLES.length} roles ===\n`);

  const browser = await chromium.launch({ headless: HEADLESS });
  const matrix = {};   // path -> role -> { effective, ms, finalPath }
  const timings = {};  // path -> role -> ms

  for (const role of ROLES) {
    const key = role === 'anon' ? null : role;
    let s;
    try {
      s = await session(browser, key);
    } catch (e) {
      rec(`rbac.session.${role}`, `Session for ${role}`, 'FAIL', e.message);
      continue;
    }
    const { ctx, page } = s;

    for (const r of ROUTES) {
      const t0 = Date.now();
      let status = null, finalPath = '', effective = null, err = null;
      try {
        const resp = await page.goto(BASE + r.path, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
        status = resp ? resp.status() : null;
        finalPath = page.url().replace(BASE, '').split('?')[0];
        // "Bounced to an auth page I did not request" is the real denial signal.
        const bounced = !r.authPage && /^\/Account\/(Login|AccessDenied)$/i.test(finalPath);
        effective = bounced ? 401 : status;
      } catch (e) {
        err = e.message;
        effective = -1;
      }
      const ms = Date.now() - t0;

      const want = r.expect[role];
      // 401 and 403 both mean denied; the app uses a login bounce for both.
      const ok = effective === want || (want === DENY && (effective === 401 || effective === 403));

      (matrix[r.path] ||= {})[role] = { status, effective, finalPath, ms, err };
      (timings[r.path] ||= {})[role] = ms;

      rec(
        `rbac.${r.path}.${role}`,
        `${r.label} [${r.policy}] as ${role}`,
        ok ? 'PASS' : 'FAIL',
        `expected ${want === ALLOW ? 'ALLOW' : 'DENY'}, got ${effective}${finalPath && finalPath !== r.path ? ` (-> ${finalPath})` : ''}${err ? ` ERR ${err}` : ''} ${ms}ms`,
        { route: r.path, role, expected: want, effective, ms }
      );
    }
    await ctx.close();
  }

  await browser.close();
  save({ startedAt, finishedAt: new Date().toISOString(), routes: ROUTES.length, roles: ROLES, matrix, timings });
})().catch((e) => { console.error('FATAL', e); process.exit(1); });
