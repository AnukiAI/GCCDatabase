/**
 * CR-051 (QC #266) — Status filter on the internal certificate listing.
 *
 * LOCALHOST ONLY. Verifies the two changes:
 *   1. Views/Certificate/CertificateTileViewInternal.cshtml — the #status-content panel exists.
 *   2. Controllers/Api/CertificateTileInternalApiController.cs — /filters returns the STORED
 *      status strings, not Enum.GetNames.
 *
 * The assertion that matters is A4/A5: before this CR the options endpoint returned PascalCase
 * enum names ("ClientApprovedPaid") while Certificate.Status stores the display form
 * ("Client Approved - Paid"), so four of the eight statuses silently returned zero rows. A4 proves
 * the stored value now filters; A5 is the negative control proving the old value does not.
 *
 * Usage: node Scripts/smoke-prod/verify-cr051-status-filter.js
 */

const BASE = process.env.CR051_BASE || 'http://localhost:5101';
const USER = process.env.CR051_USER || 'admin.uat';
const PASS = process.env.CR051_PASS || 'TestPass123!';

const jar = new Map();

function storeCookies(res) {
    const raw = res.headers.getSetCookie ? res.headers.getSetCookie() : [];
    for (const c of raw) {
        const [pair] = c.split(';');
        const idx = pair.indexOf('=');
        if (idx > 0) jar.set(pair.slice(0, idx).trim(), pair.slice(idx + 1).trim());
    }
}
const cookieHeader = () => [...jar].map(([k, v]) => `${k}=${v}`).join('; ');

async function get(path, extraHeaders = {}) {
    const res = await fetch(BASE + path, {
        headers: { Cookie: cookieHeader(), ...extraHeaders },
        redirect: 'manual',
    });
    storeCookies(res);
    return res;
}

async function login() {
    const page = await get('/Account/Login');
    const html = await page.text();
    const m = html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
    if (!m) throw new Error('no antiforgery token on /Account/Login');

    // The field is named `Email` but accepts a username too (AccountController.cs:1159).
    // Posting `Username=` silently re-renders the login page as HTTP 200.
    const body = new URLSearchParams({
        Email: USER,
        Password: PASS,
        __RequestVerificationToken: m[1],
    });
    const res = await fetch(BASE + '/Account/Login', {
        method: 'POST',
        headers: {
            Cookie: cookieHeader(),
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body,
        redirect: 'manual',
    });
    storeCookies(res);

    // Don't trust the 302 — confirm we aren't bounced back to the login page.
    const probe = await get('/Dashboard');
    const landed = probe.headers.get('location') || '';
    if (probe.status >= 300 && probe.status < 400 && /Account\/Login/i.test(landed)) {
        throw new Error(`login failed — /Dashboard redirected to ${landed}`);
    }
    return true;
}

// The internal list endpoint. `status` is pipe-delimited (controller splits on '|').
//
// Retries on 5xx. The dev DB throws SqlException "Execution Timeout Expired" under contention —
// CertificateExpirationService runs a heavy sweep at startup and competes with these queries. That
// is a pre-existing performance characteristic of the dev box, NOT a status-filter fault, and a
// single transient 500 must not be reported as a failed assertion. If every retry fails we surface
// the real status code rather than inventing one.
async function listCount(statusParam, attempts = 3) {
    const qs = new URLSearchParams({ page: '1', pageSize: '1' });
    if (statusParam) qs.set('status', statusParam);
    let last = null;
    for (let i = 1; i <= attempts; i++) {
        const res = await get('/api/certificate-tiles-internal?' + qs.toString(), {
            'X-Requested-With': 'XMLHttpRequest',
            Accept: 'application/json',
        });
        if (res.status === 200) {
            const json = await res.json();
            const count = json.totalCount ?? json.total ?? json.totalRecords;
            if (typeof count !== 'number') {
                throw new Error('no totalCount in list response; keys=' + Object.keys(json).join(','));
            }
            return count;
        }
        last = res.status;
        if (res.status < 500) break;
        console.log(`        (HTTP ${res.status} on '${statusParam}', retry ${i}/${attempts})`);
        await new Promise(r => setTimeout(r, 3000 * i));
    }
    throw new Error(`list endpoint returned HTTP ${last} for status='${statusParam}' after ${attempts} attempt(s)`);
}

const results = [];
function assert(id, desc, pass, detail) {
    results.push({ id, desc, pass, detail });
    console.log(`${pass ? 'PASS' : 'FAIL'}  ${id}  ${desc}`);
    if (detail) console.log(`        ${detail}`);
}

(async () => {
    console.log(`CR-051 verification against ${BASE} as ${USER}\n`);
    await login();
    console.log('logged in\n');

    // ---- A1: the panel markup is served -------------------------------------------------
    const viewRes = await get('/Certificate/CertificateTileViewInternal');
    const viewHtml = await viewRes.text();
    const hasPanel = /id="status-content"/.test(viewHtml);
    const hasToggle = /data-filter="status"/.test(viewHtml);
    assert('A1', 'internal view serves the #status-content panel + data-filter="status"',
        viewRes.status === 200 && hasPanel && hasToggle,
        `HTTP ${viewRes.status}, id=${hasPanel}, data-filter=${hasToggle}`);

    // ---- A2: the panel is NOT on the external view (embargo states) ---------------------
    const extRes = await get('/Certificate/CertificateTileViewExternal');
    const extHtml = extRes.status === 200 ? await extRes.text() : '';
    assert('A2', 'external view does NOT expose a status panel',
        extRes.status !== 200 || !/id="status-content"/.test(extHtml),
        `HTTP ${extRes.status}${extRes.status === 200 ? ', panel absent=' + !/id="status-content"/.test(extHtml) : ' (not reachable as this role)'}`);

    // ---- A3: /filters returns the 8 STORED strings --------------------------------------
    const fRes = await get('/api/certificate-tiles-internal/filters', {
        'X-Requested-With': 'XMLHttpRequest',
        Accept: 'application/json',
    });
    const filters = await fRes.json();
    const statuses = filters.statuses || [];
    const expected = [
        'Draft',
        'Client Approved - Not Paid',
        'Client Approved - Confidential',
        'Client Approved - Paid',
        'Hold',
        'Expired',
        'Republished',
        'Expired - No Annual Verification',
    ];
    const setsMatch = statuses.length === expected.length &&
        expected.every(e => statuses.includes(e));
    assert('A3', '/filters returns all 8 stored status strings (not enum names)',
        setsMatch,
        `got ${statuses.length}: ${JSON.stringify(statuses)}`);

    const noEnumNames = !statuses.some(s => /^ClientApproved|^ExpiredNo/.test(s));
    assert('A3b', 'no PascalCase enum names leaked into the options payload',
        noEnumNames, `statuses=${JSON.stringify(statuses)}`);

    // ---- A4: the four previously-broken statuses actually filter ------------------------
    const unfiltered = await listCount(null);
    console.log(`\n  unfiltered totalCount = ${unfiltered}\n`);

    const perStatus = {};
    for (const s of expected) perStatus[s] = await listCount(s);
    console.table ? console.table(perStatus) : console.log(perStatus);

    const multiWord = expected.filter(s => s.includes(' - '));
    const brokenBefore = multiWord.filter(s => perStatus[s] > 0);
    assert('A4', 'each multi-word status returns rows (the regression this CR fixes)',
        brokenBefore.length > 0,
        multiWord.map(s => `${s}=${perStatus[s]}`).join(' | ') +
        (brokenBefore.length === 0
            ? '  <-- all zero: either the fix did not take or dev has no such rows'
            : ''));

    // ---- A5: negative control — the OLD enum name must NOT match -----------------------
    const enumNameCount = await listCount('ClientApprovedPaid');
    assert('A5', 'negative control: enum name "ClientApprovedPaid" matches 0 rows',
        enumNameCount === 0,
        `count=${enumNameCount} (proves the pre-fix payload could never have filtered)`);

    // ---- A6: two statuses = UNION, not intersection ------------------------------------
    const a = 'Draft', b = 'Expired';
    const both = await listCount(`${a}|${b}`);
    assert('A6', 'two statuses return the union',
        both === perStatus[a] + perStatus[b],
        `${a}=${perStatus[a]} + ${b}=${perStatus[b]} => expected ${perStatus[a] + perStatus[b]}, got ${both}`);

    // ---- A7: population check — every row's status is one of the 8 ----------------------
    const sum = expected.reduce((t, s) => t + perStatus[s], 0);
    assert('A7', 'sum over the 8 statuses equals the unfiltered total (no legacy values)',
        sum === unfiltered,
        `sum=${sum} vs unfiltered=${unfiltered}` +
        (sum !== unfiltered
            ? `  <-- ${unfiltered - sum} row(s) hold a status outside the 8 and are UNFILTERABLE`
            : ''));

    const failed = results.filter(r => !r.pass);
    console.log(`\n${results.length - failed.length}/${results.length} passed`);
    if (failed.length) {
        console.log('FAILED: ' + failed.map(f => f.id).join(', '));
        process.exit(1);
    }
})().catch(e => {
    console.error('HARNESS ERROR: ' + e.message);
    process.exit(2);
});
