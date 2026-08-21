/**
 * Click-path verification for the migrated certificate documents — runbook §8 / §11 step 3.
 *
 * Proves a VIEWER can open a document. Deliberately separate from
 * 2026-08-18-verify-pdf-links.ps1, which only proves the database and the disk agree:
 *
 *   verify-pdf-links.ps1  →  "the file the DB points at exists on the filesystem"
 *   this script           →  "the running application actually serves it, over HTTP, as a PDF"
 *
 * Files can sit on disk under the right names and still not be served — wrong folder relative to
 * the app root, App_Data not deployed, permissions. The SQL check cannot see that, and neither
 * can the linker's own summary.
 *
 * ── Why this runs BOTH signed-in and anonymous ────────────────────────────────────────────────
 * ViewCertificateFile / ViewCertificateImage are [AllowAnonymous] but ROLE-AWARE
 * (DEF-204 / DEF-208, CertificateController.cs:2127). Admin and Analyst get unrestricted access;
 * anonymous, External and Partner callers must pass status + ownership filters, and a certificate
 * that fails them returns **404 — identical to the response for a file that does not exist**.
 *
 * On 2026-08-18 an anonymous sweep of this route reported 4,655 documents "missing". Every one of
 * them was present; they were expired certificates, invisible to an anonymous caller by design.
 * The sweep was measuring public visibility, not file presence, and it understated coverage by
 * ~85%. So: file-presence checks run AUTHENTICATED, and the anonymous pass exists to assert the
 * gate still closes rather than to count anything.
 *
 * Fixtures cover the three things that break differently:
 *   - an ordinary public certificate
 *   - a CR-038 sibling PAIR sharing one number, asserted to serve DIFFERENT bytes (the regression
 *     that produced 123 shared thumbnails on dev)
 *   - 23-0971, whose preview failed to render, asserted to fall back to a generic image rather
 *     than erroring or serving another certificate's document
 *
 * Usage:  node Scripts/data-ops/2026-08-18-verify-pdf-clickpath.js
 * Exit code 0 = all passed.
 */
const crypto = require('crypto');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE_USER = process.env.GATE_USER || 'gcc';
const GATE_PASS = process.env.GATE_PASS || 'Cert-Test-Access-2026!';
const USER = process.env.LOGIN_USER || 'admin';
const PASS = process.env.LOGIN_PASS || 'TestPass123!';

// The gate accepts Basic credentials as well as its cookie, and Basic is the documented path for
// automated scripts (TestEnvironmentGateMiddleware.cs:98). Used here because it is stateless: a
// gate cookie that fails redirects to the gate page, which follows to a 200 and would be
// miscounted as "document served".
const BASIC = 'Basic ' + Buffer.from(`${GATE_USER}:${GATE_PASS}`).toString('base64');

// Ids resolved from the test database 2026-08-18. NOT stable across a reload — the clear-down
// reseeds IDENTITY, so re-resolve these if the data is ever loaded again.
const ORDINARY = { id: 1,    number: '25-8533', label: 'ordinary public cert' };
const SIBLINGS = [{ id: 319,  number: '19-0281', label: 'CR-038 sibling A' },
                  { id: 4965, number: '19-0281', label: 'CR-038 sibling B' }];
const NO_THUMB = { id: 4496, number: '23-0971', label: 'preview-render failure' };

let pass = 0, fail = 0;
function check(name, ok, detail) {
  if (ok) { pass++; console.log(`  PASS  ${name}${detail ? '  — ' + detail : ''}`); }
  else    { fail++; console.log(`  FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
}

function makeSession() {
  const jar = new Map();
  const store = (res) => {
    const set = res.headers.getSetCookie ? res.headers.getSetCookie() : [];
    for (const c of set) {
      const [pair] = c.split(';'); const i = pair.indexOf('=');
      if (i > 0) jar.set(pair.slice(0, i).trim(), pair.slice(i + 1).trim());
    }
  };
  const req = async (url, opts = {}) => {
    // Omit Cookie entirely when empty — an empty Cookie header is not the same as none.
    const headers = { Authorization: BASIC, ...(opts.headers || {}) };
    if (!('Cookie' in headers)) {
      const ck = [...jar].map(([k, v]) => `${k}=${v}`).join('; ');
      if (ck) headers.Cookie = ck;
    } else if (!headers.Cookie) delete headers.Cookie;
    const res = await fetch(url, { redirect: 'manual', ...opts, headers,
      signal: AbortSignal.timeout(120000) });
    store(res);
    return res;
  };
  return { jar, req };
}

async function getAsset(session, path) {
  let res = await session.req(`${BASE}${path}`);
  let hops = 0;
  while ([301, 302, 303, 307, 308].includes(res.status) && hops++ < 5) {
    const loc = res.headers.get('location');
    res = await session.req(loc.startsWith('http') ? loc : `${BASE}${loc}`);
  }
  const buf = Buffer.from(await res.arrayBuffer());
  return {
    status: res.status,
    type: (res.headers.get('content-type') || '').split(';')[0].trim(),
    bytes: buf.length,
    sha: crypto.createHash('sha256').update(buf).digest('hex'),
    isPdf: buf.slice(0, 5).toString('latin1') === '%PDF-',
    isPng: buf.slice(0, 8).equals(Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])),
  };
}

(async () => {
  console.log(`\nClick-path verification against ${BASE}\n`);

  // ── sign in ────────────────────────────────────────────────────────────────────────────────
  // Send ONLY the antiforgery cookie on the login POST. Including the GCC.Session cookie that the
  // login GET also sets makes the gate bounce the POST to /__gate with no auth cookie issued —
  // reproducible, and indistinguishable from a wrong password.
  const admin = makeSession();
  const lg = await admin.req(`${BASE}/Account/Login`);
  const rvt = ((await lg.text()).match(/__RequestVerificationToken[^>]*value="([^"]+)"/) || [])[1];
  if (!rvt) { console.log('FATAL: no antiforgery token on the login page'); process.exit(1); }
  const af = admin.jar.has('__RequestVerificationToken')
    ? `__RequestVerificationToken=${admin.jar.get('__RequestVerificationToken')}` : '';
  await admin.req(`${BASE}/Account/Login`, {
    method: 'POST',
    body: new URLSearchParams({ Email: USER, Password: PASS, __RequestVerificationToken: rvt }),
    headers: { 'Content-Type': 'application/x-www-form-urlencoded', Cookie: af },
  });
  check(`signed in as ${USER}`, admin.jar.has('GCC.Auth'));
  if (!admin.jar.has('GCC.Auth')) {
    console.log('FATAL: no auth cookie — every result below would be a false 404.');
    process.exit(1);
  }

  console.log('\n── documents, signed in (file presence) ──');
  const pdfs = {}, imgs = {};
  for (const c of [ORDINARY, ...SIBLINGS, NO_THUMB]) {
    const r = await getAsset(admin, `/Certificate/ViewCertificateFile/${c.id}`);
    pdfs[c.id] = r;
    check(`PDF ${c.number} (${c.label})`,
      r.status === 200 && r.type === 'application/pdf' && r.isPdf,
      `${r.status} ${r.type} ${r.bytes.toLocaleString()}B pdfMagic=${r.isPdf}`);
  }

  const [s1, s2] = SIBLINGS;
  check(`CR-038: siblings on ${s1.number} serve DIFFERENT documents`,
    pdfs[s1.id].sha !== pdfs[s2.id].sha && pdfs[s1.id].bytes > 0,
    `${pdfs[s1.id].sha.slice(0, 12)}… vs ${pdfs[s2.id].sha.slice(0, 12)}…`);

  console.log('\n── previews, signed in ──');
  for (const c of [ORDINARY, ...SIBLINGS, NO_THUMB]) {
    const r = await getAsset(admin, `/Certificate/ViewCertificateImage/${c.id}`);
    imgs[c.id] = r;
    check(`preview ${c.number} (${c.label}) served as PNG`,
      r.status === 200 && r.isPng,
      `${r.status} ${r.type} ${r.bytes.toLocaleString()}B pngMagic=${r.isPng}`);
  }

  check(`CR-038: siblings on ${s1.number} serve DIFFERENT previews`,
    imgs[s1.id].sha !== imgs[s2.id].sha,
    `${imgs[s1.id].sha.slice(0, 12)}… vs ${imgs[s2.id].sha.slice(0, 12)}…`);

  // 23-0971 has no preview. It must degrade to the generic image, NOT to another certificate's —
  // so its bytes must differ from every real preview fetched above.
  const distinct = [ORDINARY, ...SIBLINGS].every(c => imgs[c.id].sha !== imgs[NO_THUMB.id].sha);
  check(`${NO_THUMB.number}: falls back to a generic preview, not another cert's image`,
    imgs[NO_THUMB.id].status === 200 && distinct,
    `${imgs[NO_THUMB.id].bytes.toLocaleString()}B, distinct from all real previews=${distinct}`);

  // ── the role gate, anonymous ───────────────────────────────────────────────────────────────
  // Not a file-presence check. This asserts the gate still closes, and documents why an
  // anonymous sweep of this route cannot be used to count documents.
  console.log('\n── role gate, NOT signed in ──');
  const anon = makeSession();
  const pubAnon = await getAsset(anon, `/Certificate/ViewCertificateFile/${ORDINARY.id}`);
  check(`public cert ${ORDINARY.number} still served anonymously`,
    pubAnon.status === 200 && pubAnon.isPdf, `${pubAnon.status} ${pubAnon.type}`);

  const expAnon = await getAsset(anon, `/Certificate/ViewCertificateFile/${s1.id}`);
  check(`expired cert ${s1.number} correctly withheld from anonymous callers`,
    expAnon.status === 404,
    `${expAnon.status} — same response as a missing file, which is why counting anonymously is invalid`);

  const direct = await getAsset(anon, '/uploads/certificates/');
  check('direct /uploads/... is NOT browsable (DEF-208)', direct.status !== 200, `status ${direct.status}`);

  console.log(`\n=== click-path: ${pass} passed, ${fail} failed ===`);
  console.log(fail ? 'DOCUMENTS NOT SERVING CORRECTLY — see failures above.'
                   : 'DOCUMENTS SERVING CORRECTLY — viewers can open PDFs and previews, and the role gate holds.');
  process.exit(fail ? 1 : 0);
})();
