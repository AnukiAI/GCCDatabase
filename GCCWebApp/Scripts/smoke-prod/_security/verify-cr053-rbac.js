// CR-053 RBAC — can the new pager routes be used to read another company's document?
//
// CR-053 added two reachable surfaces, both `[AllowAnonymous]` at the action with data gating
// inside via FileAccessAllowed():
//     /Certificate/ViewCertificateImage/{id}?page=N   -> a rendered PNG of page N
//     /Certificate/CertificatePageCount/{id}          -> {"pages": N}
//
// The page COUNT matters as much as the image: it discloses that a document exists and how long it
// is. This project has repeatedly fixed scoping on one endpoint while an adjacent variant kept
// leaking, and a new route is exactly that risk — so both are driven here, as three callers.
//
// Reading the code is not evidence: the check and the expectation would come from the same place.
// This drives real HTTP with real sessions.
//
// Fixtures (dev) — `external` is scoped to companies 73,339. FOUR are needed, because one cert
// cannot distinguish "refused because of company scoping" from "refused because of status".
//   OWN_MULTI        = 1321 (company 73/339, Client Approved - Paid, in force, **2 pages**)
//   OTHER_NONPUBLIC  = 1    (company 27, Expired — not public, not the external user's)
//   PUBLIC           = 597  (25-8521, Paid, in force — anonymous single view returns 200)
//
// Two earlier runs of this script failed on their own CONTROLS, and both were the test's fault:
//   * cert 1488 as the "own" control -> 404, because it is Expired: External access needs badgeable
//     status AND the validity window, not just the company. Every denial that run was unfalsifiable.
//   * cert 655 as the "own page" control -> 404 for page=1, because that PDF has ONE page and the
//     code correctly refuses `page >= pageCount`. The endpoint was right; the fixture was wrong.
// Hence the multi-page fixture and the explicit public/non-public split.

// Run: NODE_PATH=<global node_modules> node Scripts/smoke-prod/_security/verify-cr053-rbac.js
const { chromium } = require('playwright');

const BASE = 'http://localhost:5101';
const OWN = 1321;          // own company, in force, 2 pages
const OTHER = 1;           // another company, Expired => not public either
const PUBLIC_CERT = 597;   // publicly visible (anonymous single view = 200)
const results = [];

function record(id, desc, pass, detail) {
  results.push({ id, pass });
  console.log(`  ${pass ? 'PASS' : 'FAIL'}  ${id} — ${desc}${detail ? ' :: ' + detail : ''}`);
}

async function login(ctx, user, pass) {
  const page = await ctx.newPage();
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[name="Email"]', user);
  await page.fill('input[name="Password"]', pass);
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'domcontentloaded' }).catch(() => {}),
    page.click('button[type="submit"]')
  ]);
  const ok = !/Forgot your password/i.test(await page.content());
  await page.close();
  return ok;
}

// Fetch inside the browser context so the session cookie is attached.
async function probe(ctx, path) {
  const page = await ctx.newPage();
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' }).catch(() => {});
  const out = await page.evaluate(async p => {
    const r = await fetch(p, { redirect: 'manual' });
    const buf = await r.arrayBuffer().catch(() => new ArrayBuffer(0));
    const head = new Uint8Array(buf).slice(0, 8);
    return {
      status: r.status,
      type: r.headers.get('content-type') || '',
      bytes: buf.byteLength,
      // PNG magic number — proves an actual rendered page came back, not an error page
      isPng: head[0] === 0x89 && head[1] === 0x50 && head[2] === 0x4e && head[3] === 0x47,
      text: buf.byteLength < 400 ? new TextDecoder().decode(buf) : ''
    };
  }, path);
  await page.close();
  return out;
}

(async () => {
  const browser = await chromium.launch({ headless: true });

  // ---- 1. ANONYMOUS ------------------------------------------------------
  const anon = await browser.newContext();
  let r = await probe(anon, `/Certificate/CertificatePageCount/${PUBLIC_CERT}`);
  record('anon.count.public', 'CONTROL — anonymous CAN count a publicly-visible cert',
    r.status === 200 && /"pages":\s*[1-9]/.test(r.text), `HTTP ${r.status} ${r.text.trim().slice(0, 30)}`);

  r = await probe(anon, `/Certificate/CertificatePageCount/${OTHER}`);
  record('anon.count.nonpublic', 'anonymous page COUNT for a NON-public cert is refused',
    r.status !== 200 || /"pages":\s*0/.test(r.text), `HTTP ${r.status} ${r.text.trim().slice(0, 40)}`);

  r = await probe(anon, `/Certificate/ViewCertificateImage/${OTHER}?page=1`);
  record('anon.page.nonpublic', 'anonymous PAGE IMAGE for a NON-public cert is refused',
    !(r.status === 200 && r.isPng), `HTTP ${r.status}${r.isPng ? ' PNG ' + r.bytes + 'B' : ''}`);
  await anon.close();

  // ---- 2. EXTERNAL, scoped to companies 73/339 ---------------------------
  const ext = await browser.newContext();
  if (!await login(ext, 'external', 'TestPass123!')) {
    console.log('  BLOCKED — external login failed; cannot judge scoping');
    process.exit(1);
  }

  // control: its OWN certificate must still work, or a refusal proves nothing
  r = await probe(ext, `/Certificate/CertificatePageCount/${OWN}`);
  const ownCountOk = r.status === 200 && /"pages":\s*[1-9]/.test(r.text);
  record('ext.count.own', 'CONTROL — external CAN count pages of its own cert',
    ownCountOk, `HTTP ${r.status} ${r.text.trim().slice(0, 30)}`);

  r = await probe(ext, `/Certificate/ViewCertificateImage/${OWN}?page=1`);
  record('ext.page.own', 'CONTROL — external CAN render page 1 of its own cert',
    r.status === 200 && r.isPng, `HTTP ${r.status}${r.isPng ? ' PNG ' + r.bytes + 'B' : ' not-png'}`);

  // the real question
  r = await probe(ext, `/Certificate/CertificatePageCount/${OTHER}`);
  record('ext.count.other', 'external CANNOT count pages of ANOTHER company\'s cert',
    r.status !== 200 || /"pages":0/.test(r.text), `HTTP ${r.status} ${r.text.trim().slice(0, 40)}`);

  r = await probe(ext, `/Certificate/ViewCertificateImage/${OTHER}?page=1`);
  record('ext.page.other', 'external CANNOT render a page of ANOTHER company\'s cert',
    !(r.status === 200 && r.isPng), `HTTP ${r.status}${r.isPng ? ' PNG ' + r.bytes + 'B LEAK' : ''}`);

  // out-of-range and negative page indexes on a cert it IS allowed to see
  r = await probe(ext, `/Certificate/ViewCertificateImage/${OWN}?page=9999`);
  record('ext.page.oob', 'out-of-range page index is refused, not a 500',
    r.status === 404, `HTTP ${r.status}`);

  r = await probe(ext, `/Certificate/ViewCertificateImage/${OWN}?page=-1`);
  record('ext.page.negative', 'negative page index does not error',
    r.status === 200 || r.status === 404, `HTTP ${r.status}`);
  await ext.close();

  // ---- 3. ADMIN (control — the routes do work for someone) ---------------
  const admin = await browser.newContext();
  if (await login(admin, 'admin', 'TestPass123!')) {
    r = await probe(admin, `/Certificate/CertificatePageCount/${OTHER}`);
    record('admin.count.other', 'CONTROL — admin CAN count pages of any cert',
      r.status === 200 && /"pages":\s*[1-9]/.test(r.text), `HTTP ${r.status} ${r.text.trim().slice(0, 30)}`);
  } else {
    record('admin.count.other', 'CONTROL — admin login', false, 'login failed');
  }
  await admin.close();

  await browser.close();
  const failed = results.filter(x => !x.pass);
  console.log(`\n${results.length - failed.length}/${results.length} passed`);
  if (failed.length) console.log('FAILED: ' + failed.map(f => f.id).join(', '));
  process.exit(failed.length ? 1 : 0);
})();
