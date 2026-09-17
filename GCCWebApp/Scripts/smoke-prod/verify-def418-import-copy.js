/**
 * DEF-418 REOPENED (QC #263) — certificate-import help text.
 *
 * LOCALHOST ONLY.
 *
 * The 2026-09-10 fix corrected Views/CertificateImport/Index.cshtml only. The client works in the
 * "Import Certificates" MODAL on the certificate listing, which was never touched and is not linked
 * to that page — so she reported the item still broken and she was right.
 *
 * Every assertion below is made against RENDERED HTML from the running app, not against files on
 * disk, because "the file contains the right words" is exactly what was true last time while the
 * client still saw the wrong ones.
 *
 * Usage: node Scripts/smoke-prod/verify-def418-import-copy.js
 */

const BASE = process.env.DEF418_BASE || 'http://localhost:5101';
const USER = process.env.DEF418_USER || 'admin.uat';
const PASS = process.env.DEF418_PASS || 'TestPass123!';

const results = [];
function assert(id, desc, pass, detail) {
    results.push({ id, desc, pass, detail });
    console.log(`${pass ? 'PASS' : 'FAIL'}  ${id}  ${desc}`);
    if (detail) console.log(`        ${detail}`);
}

const jar = new Map();
const store = res => {
    for (const c of (res.headers.getSetCookie ? res.headers.getSetCookie() : [])) {
        const [pair] = c.split(';');
        const i = pair.indexOf('=');
        if (i > 0) jar.set(pair.slice(0, i).trim(), pair.slice(i + 1).trim());
    }
};
const cookie = () => [...jar].map(([k, v]) => `${k}=${v}`).join('; ');

async function get(p, attempts = 3) {
    let last = null;
    for (let i = 1; i <= attempts; i++) {
        const res = await fetch(BASE + p, { headers: { Cookie: cookie() }, redirect: 'manual' });
        store(res);
        if (res.status === 200) return { status: 200, html: await res.text() };
        last = res.status;
        if (res.status < 500) break;
        console.log(`        (HTTP ${res.status} on ${p}, retry ${i}/${attempts})`);
        await new Promise(r => setTimeout(r, 3000 * i));
    }
    return { status: last, html: '', transportFailed: true };
}

async function login() {
    const page = await get('/Account/Login');
    const tok = page.html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
    if (!tok) throw new Error('no antiforgery token');
    const res = await fetch(BASE + '/Account/Login', {
        method: 'POST',
        headers: { Cookie: cookie(), 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({ Email: USER, Password: PASS, __RequestVerificationToken: tok[1] }),
        redirect: 'manual',
    });
    store(res);
}

// Strip tags so prose assertions aren't defeated by <strong>/<code> markup splitting a phrase.
const textOf = html => html.replace(/<[^>]+>/g, ' ').replace(/&mdash;/g, '—').replace(/&amp;/g, '&')
    .replace(/&rarr;/g, '->').replace(/\s+/g, ' ');

(async () => {
    console.log(`DEF-418 (QC #263) verification against ${BASE}\n`);
    await login();

    const listing = await get('/Certificate/CertificateTileViewInternal');
    const pageDoc = await get('/CertificateImport');
    if (listing.status !== 200) throw new Error(`listing returned HTTP ${listing.status}`);
    if (pageDoc.status !== 200) throw new Error(`import page returned HTTP ${pageDoc.status}`);
    const listingText = textOf(listing.html);
    const pageText = textOf(pageDoc.html);
    console.log('fetched both surfaces\n');

    // ---- 1. the wrong claim is gone from the client's surface ----
    assert('1a', 'the MODAL no longer claims CERTIFICATE_NUMBER is required',
        !/Required:\s*CERTIFICATE_NUMBER/i.test(listingText),
        'this exact string is what the client photographed in Image 120');

    assert('1b', 'the MODAL states Certificate # is NOT required',
        /Certificate # is not required/i.test(listingText));

    // ---- 2. the client's actual question is answered on her surface ----
    for (const [id, needle, what] of [
        ['2a', /leave\s+Certificate #\s+blank/i, 'blank Certificate # = add a new certificate'],
        ['2b', /To edit an existing certificate/i, 'supplied Certificate # = edit'],
        ['2c', /acknowledgement before it is applied/i, 'overwrite acknowledgement'],
        ['2d', /backup of those certificates is saved automatically/i, 'automatic backup (CR-023 R5)'],
        ['2e', /Product Category must already exist/i, 'the one field that must pre-exist'],
    ]) {
        assert(id, `MODAL explains: ${what}`, needle.test(listingText));
    }

    // ---- 3. the two surfaces must not contradict each other ----
    // DEF-418's original finding was a page contradicting itself; two surfaces disagreeing is the
    // same failure one level up, and is what produced this reopen.
    const claims = [
        ['Certificate # not required', /Certificate # is not required/i],
        ['Product Category must pre-exist', /Product Category must already exist/i],
        ['Tags auto-created', /Product Type and Tags/i],
        ['omitted columns untouched', /left untouched/i],
    ];
    for (const [label, re] of claims) {
        const inModal = re.test(listingText);
        const inPage = re.test(pageText);
        assert(`3-${label.slice(0, 18)}`, `both surfaces agree on: ${label}`,
            inModal && inPage, `modal=${inModal}, page=${inPage}`);
    }

    // ---- 4. the DEF-421 sequencing note is discharged on the page ----
    assert('4a', 'page documents the `Subcategory` column (wired by DEF-421)',
        /Subcategory\s*—?\s*Product subcategory/i.test(pageText) || /Product subcategory\. Matched/i.test(pageText),
        'DEF-418 said this list should gain the column once DEF-421 shipped');

    assert('4b', 'page documents `Cert Subcategories`, including that it round-trips through export',
        /Cert Subcategories/i.test(pageText) && /re-import round-trip keeps it/i.test(pageText));

    assert('4c', 'both subcategory columns are described as PARENT-SCOPED, not name-only',
        /within the Product Type in the same row/i.test(pageText) &&
        /belong to the Certification Type in the same row/i.test(pageText),
        'name-only matching is the DEF-421 hierarchy trap');

    // ---- 5. what must NOT have changed ----
    assert('5a', 'the legacy SCREAMING_SNAKE aliases are still documented as accepted',
        /backward compatibility/i.test(pageText) && /CERTIFICATE_NUMBER/.test(pageText),
        'DEF-418: the aliases are real and the migration relies on them — prose only was wrong');

    const failed = results.filter(r => !r.pass);
    console.log(`\n${results.length - failed.length}/${results.length} passed`);
    if (failed.length) { console.log('FAILED: ' + failed.map(f => f.id).join(', ')); process.exit(1); }
})().catch(e => { console.error('HARNESS ERROR: ' + e.message); process.exit(2); });
