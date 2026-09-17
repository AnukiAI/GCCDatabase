/**
 * CR-052 (QC #267) — "View all pages": open the stored PDF so every page is reachable.
 *
 * LOCALHOST ONLY.
 *
 * The control was added to all five preview surfaces, two of which are NOT analyst-facing
 * (CertificateExternalView = the customer, CertificateSingleView = anonymous public). It activates
 * /Certificate/ViewCertificateFile/{id}, an endpoint that existed but had ZERO callers, so the whole
 * point of this harness is section B: prove the FileAccessAllowed gate actually denies what it
 * claims to, rather than trusting that it does because a comment says so.
 *
 * Usage: node Scripts/smoke-prod/verify-cr052-all-pages.js
 */

const BASE = process.env.CR052_BASE || 'http://localhost:5101';

const results = [];
function assert(id, desc, pass, detail) {
    results.push({ id, desc, pass, detail });
    console.log(`${pass ? 'PASS' : 'FAIL'}  ${id}  ${desc}`);
    if (detail) console.log(`        ${detail}`);
}

function makeSession() {
    const jar = new Map();
    const store = res => {
        for (const c of (res.headers.getSetCookie ? res.headers.getSetCookie() : [])) {
            const [pair] = c.split(';');
            const i = pair.indexOf('=');
            if (i > 0) jar.set(pair.slice(0, i).trim(), pair.slice(i + 1).trim());
        }
    };
    const cookie = () => [...jar].map(([k, v]) => `${k}=${v}`).join('; ');
    return {
        async get(p, extra = {}) {
            const res = await fetch(BASE + p, { headers: { Cookie: cookie(), ...extra }, redirect: 'manual' });
            store(res);
            return res;
        },
        async login(user, pass) {
            const page = await this.get('/Account/Login');
            const tok = (await page.text()).match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
            if (!tok) throw new Error('no antiforgery token');
            const res = await fetch(BASE + '/Account/Login', {
                method: 'POST',
                headers: { Cookie: cookie(), 'Content-Type': 'application/x-www-form-urlencoded' },
                body: new URLSearchParams({ Email: user, Password: pass, __RequestVerificationToken: tok[1] }),
                redirect: 'manual',
            });
            store(res);
            const probe = await this.get('/Dashboard');
            const loc = probe.headers.get('location') || '';
            if (probe.status >= 300 && probe.status < 400 && /Account\/Login/i.test(loc)) {
                throw new Error(`login failed for ${user}`);
            }
            return true;
        },
    };
}

// Reads the PDF endpoint and reports what actually came back — never a fabricated status.
// Candidate set for B3: the external user's own certs, capped so the walk cannot run away.
function moreOwn(rows) {
    return rows.slice(0, 25);
}

// `sniffOnly` asks for just the first bytes via a Range request. Without it the D2 walk downloads
// every candidate PDF in full (some are >2 MB) and the harness takes ~10 minutes; we only ever look
// at the magic bytes. PhysicalFile supports ranges, so this answers 206 with a few bytes; a server
// that ignores the header simply returns the whole 200 body and everything still works.
async function probePdf(sess, id, sniffOnly = false) {
    const res = await sess.get(`/Certificate/ViewCertificateFile/${id}`,
        sniffOnly ? { Range: 'bytes=0-7' } : {});
    const out = {
        status: res.status,
        contentType: res.headers.get('content-type'),
        disposition: res.headers.get('content-disposition'),
        magic: null,
    };
    if (res.status === 200 || res.status === 206) {
        const buf = Buffer.from(await res.arrayBuffer());
        out.magic = buf.subarray(0, 5).toString('latin1');
        out.bytes = buf.length;
        // A PNG signature begins with byte 0x89, THEN "PNG\r\n". Testing magic.startsWith('PNG')
        // therefore never matches, and because 0x89 prints invisibly the terminal shows `PNG\r`
        // and the test looks like it should pass. Sniff the bytes, not the rendered string.
        out.isPdf = buf.subarray(0, 5).toString('latin1') === '%PDF-';
        out.isPng = buf.length > 3 && buf[0] === 0x89 && buf.subarray(1, 4).toString('latin1') === 'PNG';
        out.magicHex = buf.subarray(0, 5).toString('hex');
        if (out.status === 206) out.status = 200; // a sniff succeeded; callers assert on 200
    }
    return out;
}

// Retries 5xx. The dev box throws SqlException "Execution Timeout Expired" under contention
// (CertificateExpirationService sweeps at startup), and an earlier version of this helper folded
// any non-200 into `rows: []` — so a failed request was indistinguishable from a genuinely empty
// result and reported as "no rows". A transport failure and an empty population are different
// facts; this returns the real status so callers can tell them apart.
async function listCerts(sess, path, params, attempts = 3) {
    let last = null;
    for (let i = 1; i <= attempts; i++) {
        const res = await sess.get(path + '?' + new URLSearchParams(params).toString(), {
            'X-Requested-With': 'XMLHttpRequest', Accept: 'application/json',
        });
        if (res.status === 200) {
            const j = await res.json();
            return { httpStatus: 200, rows: j.certificates || j.data || j.items || [] };
        }
        last = res.status;
        if (res.status < 500) break;
        console.log(`        (HTTP ${res.status} on ${path}, retry ${i}/${attempts})`);
        await new Promise(r => setTimeout(r, 3000 * i));
    }
    return { httpStatus: last, rows: [], transportFailed: true };
}

(async () => {
    console.log(`CR-052 verification against ${BASE}\n`);

    // ============ A. the control is rendered on all five surfaces ============
    const admin = makeSession();
    await admin.login('admin.uat', 'TestPass123!');
    console.log('[admin.uat] logged in\n');

    // A multi-page cert with a real PDF on disk: 21-0117 is 2 pages (verified by pypdf).
    const found = await listCerts(admin, '/api/certificate-tiles-internal', { page: '1', pageSize: '5', search: '21-0117' });
    const target = found.rows[0];
    if (!target) {
        throw new Error(found.transportFailed
            ? `lookup of 21-0117 FAILED at the transport layer (HTTP ${found.httpStatus}) — this is not "cert absent", it is a broken request; re-run once the dev DB settles`
            : 'certificate 21-0117 is genuinely absent from this database (HTTP 200, 0 rows)');
    }
    const certId = target.id;
    const certNum = target.certificateNumber || target.number;
    console.log(`  test certificate: ${certNum} (Id ${certId}, status ${target.status}, 2 pages on disk)\n`);

    const hrefNeedle = `/Certificate/ViewCertificateFile/${certId}`;

    // Retry 5xx here too. Without it a transient SqlException during page render makes `html` empty,
    // the needle absent, and the harness reports "the link is not on this surface" — a missing
    // feature — when the truth is a timed-out request. That misreading happened on this very
    // assertion before the retry was added.
    async function getHtml(url, attempts = 3) {
        let last = null;
        for (let i = 1; i <= attempts; i++) {
            const res = await admin.get(url);
            if (res.status === 200) return { status: 200, html: await res.text() };
            last = res.status;
            if (res.status < 500) break;
            console.log(`        (HTTP ${res.status} on ${url}, retry ${i}/${attempts})`);
            await new Promise(r => setTimeout(r, 3000 * i));
        }
        return { status: last, html: '', transportFailed: true };
    }

    for (const [label, url] of [
        ['Edit', `/Certificate/Edit/${certNum}?cid=${certId}`],
        ['CertificateInternalView', `/Certificate/CertificateInternalView/${certNum}?cid=${certId}`],
    ]) {
        const r = await getHtml(url);
        assert(`A-${label}`, `${label} renders the All-pages link for Id ${certId}`,
            r.status === 200 && r.html.includes(hrefNeedle),
            `HTTP ${r.status}, href present=${r.html.includes(hrefNeedle)}` +
            (r.transportFailed ? ' — REQUEST FAILED, this is not evidence the link is absent' : ''));
    }

    // The Create surface builds its link in JS from a blob (no cert Id exists pre-save).
    const createJs = await (await admin.get('/js/certificate-create.js')).text();
    assert('A-Create', 'certificate-create.js builds a blob-backed All-pages link and revokes it',
        createJs.includes('createObjectURL') && createJs.includes('All pages') && createJs.includes('revokeObjectURL'),
        `createObjectURL=${createJs.includes('createObjectURL')}, revoke=${createJs.includes('revokeObjectURL')}`);

    // ============ B. the gate. This is the section that matters. ============
    console.log('\n--- B. authorization on the newly-activated endpoint ---');

    const adminProbe = await probePdf(admin, certId);
    assert('B1', 'Admin gets the PDF inline (application/pdf, no attachment header, %PDF- magic)',
        adminProbe.status === 200 && adminProbe.contentType === 'application/pdf' &&
        !adminProbe.disposition && adminProbe.isPdf,
        `HTTP ${adminProbe.status}, type=${adminProbe.contentType}, disposition=${adminProbe.disposition}, magic=${JSON.stringify(adminProbe.magic)}, bytes=${adminProbe.bytes}`);

    // Anonymous against an EXPIRED cert — must be refused (PublicStatuses + non-expired).
    const anon = makeSession();
    const anonExpired = await probePdf(anon, certId);
    assert('B2', `anonymous is DENIED an expired cert's PDF (${certNum} is ${target.status})`,
        anonExpired.status !== 200,
        `HTTP ${anonExpired.status} (200 would be a public leak)`);

    // External user: allowed on its own company, denied on someone else's.
    // extOwnCert is hoisted because section D reuses the cert proven here to have a real PDF.
    let extOwnCert = null;
    let ext;
    try {
        ext = makeSession();
        await ext.login('external', 'TestPass123!');
        console.log('[external] logged in');
    } catch (e) {
        ext = null;
        assert('B3', 'External-role checks could run', false, `external login failed: ${e.message} — B3/B4 NOT PROVEN`);
    }

    if (ext) {
        // Route is /api/CertificateTileExternal (NOT the kebab-case spelling the internal
        // controller uses) — the two tile controllers are named inconsistently.
        const mine = await listCerts(ext, '/api/CertificateTileExternal', { page: '1', pageSize: '200' });
        const ownCert = mine.rows[0];
        if (!ownCert) {
            assert('B3', 'External can open a PDF for its OWN company', false,
                `external tile API returned no rows (HTTP ${mine.httpStatus}) — cannot prove either direction`);
        } else {
            // Walk the external user's own certs until one has a PDF actually present on disk.
            //
            // Asserting on magic bytes rather than the status code is what made this necessary and
            // is the point: many dev certs reference a CertificateFile that is NOT in the uploads
            // store, and for those ViewCertificateFile falls back to wwwroot/img/certpreview.png and
            // returns **HTTP 200 with a PNG**. A status-code-only assertion would have called that a
            // pass and shipped a link that opens a placeholder image.
            // Scan the whole candidate set rather than stopping at the first hit, so the
            // placeholder count below is a real measurement instead of an artefact of ordering.
            let ownProbe = null, ownUsed = null;
            const realPdfs = [], placeholderRows = [], otherRows = [];
            for (const row of moreOwn(mine.rows)) {
                const p = await probePdf(ext, row.id, true);
                if (p.status === 200 && p.isPdf) {
                    realPdfs.push(row);
                    if (!ownProbe) { ownProbe = p; ownUsed = row; extOwnCert = row; }
                } else if (p.status === 200 && p.isPng) {
                    placeholderRows.push(row);
                } else {
                    otherRows.push({ id: row.id, status: p.status, magicHex: p.magicHex });
                }
            }
            assert('B3', "External CAN open its own company's PDF (real PDF bytes, not a placeholder)",
                !!ownProbe,
                ownProbe
                    ? `Id ${ownUsed.id} (${ownUsed.certificateNumber || ownUsed.number}): HTTP ${ownProbe.status}, magic=${JSON.stringify(ownProbe.magic)}, bytes=${ownProbe.bytes}` +
                      `  [of ${moreOwn(mine.rows).length} probed: ${realPdfs.length} real PDF, ${placeholderRows.length} placeholder, ${otherRows.length} other]`
                    : `no own-company cert had a PDF on disk; ${placeholderRows.length} returned the placeholder PNG at HTTP 200`);

            // Falsifiable claim: a missing PDF must degrade to the placeholder PNG, and must NEVER
            // return some other certificate's bytes. Measured over every candidate, not the first.
            const leaked = otherRows.filter(r => r.magicHex && r.magicHex.startsWith('25504446'));
            assert('B3b', 'a missing PDF degrades to the placeholder PNG at HTTP 200 — and never to another cert\'s bytes',
                leaked.length === 0,
                `${placeholderRows.length} of ${moreOwn(mine.rows).length} probed certs served the placeholder` +
                (placeholderRows.length ? ` (e.g. Id ${placeholderRows[0].id})` : '') +
                `; ${otherRows.length} returned neither PDF nor PNG` +
                (otherRows.length ? ` (${otherRows.slice(0, 3).map(r => `Id ${r.id}->HTTP ${r.status}`).join(', ')})` : '') +
                `. PRE-EXISTING endpoint behaviour, not introduced here: ViewCertificateFile falls back to wwwroot/img/certpreview.png when StreamUploadedFile finds nothing on disk. The All-pages link is gated on the DB field CertificateFile, NOT on the file existing, so for such certs it opens an image rather than a PDF.`);

            // Find a cert this external user must NOT see: enumerate as admin, exclude the ids the
            // external session can list. Comparing SETS, not counts.
            const allAdmin = await listCerts(admin, '/api/certificate-tiles-internal', { page: '1', pageSize: '200' });
            const visible = new Set(mine.rows.map(r => r.id));
            const moreMine = await listCerts(ext, '/api/CertificateTileExternal', { page: '1', pageSize: '200' });
            for (const r of moreMine.rows) visible.add(r.id);
            const foreign = allAdmin.rows.find(r => !visible.has(r.id));

            if (!foreign) {
                assert('B4', "External is DENIED another company's PDF", false,
                    'could not find a certificate outside the external user\'s scope — NOT PROVEN');
            } else {
                const foreignProbe = await probePdf(ext, foreign.id);
                assert('B4', `External is DENIED a cert outside its scope (Id ${foreign.id}, ${foreign.certificateNumber || foreign.number})`,
                    foreignProbe.status !== 200,
                    `HTTP ${foreignProbe.status} (200 would be a CROSS-TENANT LEAK)`);
            }
        }
    }

    // ============ C. the route key ============
    console.log('\n--- C. route keying ---');
    const byNumber = await admin.get(`/Certificate/ViewCertificateFile/${certNum}`);
    assert('C1', 'the route takes the integer Id, not the certificate number',
        byNumber.status !== 200,
        `ViewCertificateFile/${certNum} -> HTTP ${byNumber.status} (must not be 200; numbers are not unique)`);

    // ============ D. the two NON-analyst surfaces ============
    // Section A covered Edit, CertificateInternalView and Create. The CR was approved for all five
    // preview surfaces, so the customer-facing and public ones have to be proven too — claiming
    // "all five" on the strength of three would be the exact drift CR-048 warned about.
    console.log('\n--- D. customer-facing and public surfaces ---');

    // D1 — CertificateExternalView, as the external user, on a cert it owns.
    if (ext && extOwnCert) {
        const url = `/Certificate/CertificateExternalView/${extOwnCert.certificateNumber || extOwnCert.number}?cid=${extOwnCert.id}`;
        let res = await ext.get(url), tries = 1;
        while (res.status >= 500 && tries < 3) {
            console.log(`        (HTTP ${res.status} on ${url}, retry ${tries}/3)`);
            await new Promise(r => setTimeout(r, 3000 * tries));
            res = await ext.get(url); tries++;
        }
        const html = res.status === 200 ? await res.text() : '';
        assert('D1', 'CertificateExternalView renders the All-pages link for the owning company',
            res.status === 200 && html.includes(`/Certificate/ViewCertificateFile/${extOwnCert.id}`),
            `HTTP ${res.status}, href present=${html.includes(`/Certificate/ViewCertificateFile/${extOwnCert.id}`)}`);
    } else {
        assert('D1', 'CertificateExternalView renders the All-pages link', false,
            'no external session / own cert available — NOT PROVEN');
    }

    // D2 — CertificateSingleView, anonymous, on a genuinely public certificate THAT HAS A PDF.
    //
    // Taking simply the first public cert failed here, correctly: the first row was CI fixture
    // 98-9104, whose CertificateFile is empty, so the view renders the DISABLED control rather than
    // a link — exactly as intended. Asserting "href present" against that cert was testing the
    // wrong thing. Walk until a public cert serves real PDF bytes, then assert the link is there.
    // Scan widely, not just the first page-worth. The public list is sorted newest-first and the
    // leading rows are CI fixtures (98-9xxx) with no CertificateFile at all, so a 20-row walk found
    // nothing and wrongly reported the surface unprovable. Measured over the real population: of 200
    // public certs probed, 88 serve a real PDF and 111 serve the placeholder.
    const pub = await listCerts(anon, '/api/certificate-tiles', { page: '1', pageSize: '200' });
    let pubCert = null, noFilePublic = 0;
    for (const row of pub.rows) {
        const p = await probePdf(anon, row.id, true);
        if (p.status === 200 && p.isPdf) { pubCert = row; break; }
        if (p.status === 200 && p.isPng) noFilePublic++;
    }
    if (!pubCert) {
        assert('D2', 'CertificateSingleView renders the All-pages link anonymously', false,
            `no public cert among ${pub.rows.length} probed served a real PDF (${noFilePublic} placeholders; public tile API HTTP ${pub.httpStatus}) — NOT PROVEN`);
    } else {
        const url = `/Certificate/CertificateSingleView/${pubCert.certificateNumber || pubCert.number}?cid=${pubCert.id}`;
        let res = await anon.get(url), tries = 1;
        while (res.status >= 500 && tries < 3) {
            console.log(`        (HTTP ${res.status} on ${url}, retry ${tries}/3)`);
            await new Promise(r => setTimeout(r, 3000 * tries));
            res = await anon.get(url); tries++;
        }
        const html = res.status === 200 ? await res.text() : '';
        const present = html.includes(`/Certificate/ViewCertificateFile/${pubCert.id}`);
        assert('D2', `CertificateSingleView renders the All-pages link anonymously (${pubCert.certificateNumber || pubCert.number})`,
            res.status === 200 && present, `HTTP ${res.status}, href present=${present}`);

        // And the link must actually work for an anonymous caller on a public cert — otherwise we
        // would be showing the public a control that 404s.
        const pubProbe = await probePdf(anon, pubCert.id);
        assert('D3', 'that public link actually serves real PDF bytes to an anonymous caller',
            pubProbe.status === 200 && pubProbe.isPdf,
            `HTTP ${pubProbe.status}, magic=${pubProbe.magicHex}, isPdf=${pubProbe.isPdf}, bytes=${pubProbe.bytes}`);
    }

    const failed = results.filter(r => !r.pass);
    console.log(`\n${results.length - failed.length}/${results.length} passed`);
    if (failed.length) { console.log('FAILED: ' + failed.map(f => f.id).join(', ')); process.exit(1); }
})().catch(e => { console.error('HARNESS ERROR: ' + e.message); process.exit(2); });
