/**
 * DEF-381 (second half): offset-based paging must honour the exact offset.
 *
 * CertificateTilesApiController translated an offset back into a page number by integer division
 * (`Page = (offset / limit) + 1`) and the repository then did `Skip((Page - 1) * PageSize)`. Any
 * offset that was not an exact multiple of the limit silently rounded DOWN to the start of its
 * page, so the API re-served rows the caller had already seen and the tail of the list became
 * unreachable. Fixed by carrying an explicit `Skip` on CertificateSearchFilter.
 *
 * The check: build a reference ordering from aligned pages, then assert that an UNALIGNED offset
 * returns exactly reference[offset .. offset+limit]. Before the fix, offset=90 returned the rows at
 * 50..100. This is deliberately asserted against the real HTTP endpoint, not the repository.
 *
 * Usage: node Scripts/smoke-prod/_regression/09-offset-paging.js [baseUrl]
 */
const BASE = process.argv[2] || 'http://localhost:5101';
const API = '/api/certificate-tiles';
const LIMIT = 50;

if (!/^https?:\/\/(localhost|127\.0\.0\.1)/.test(BASE) && !process.env.ALLOW_REMOTE) {
    console.error(`refusing to run against ${BASE} without ALLOW_REMOTE=1`);
    process.exit(2);
}

const get = async (offset, limit = LIMIT, sort = 'cert-number-newest') => {
    const r = await fetch(`${BASE}${API}?sortBy=${sort}&offset=${offset}&limit=${limit}`);
    if (!r.ok) throw new Error(`HTTP ${r.status} at offset=${offset}`);
    return r.json();
};

(async () => {
    const failures = [];

    // Reference ordering, built from aligned pages only (this path was always correct).
    const ref = [];
    let total = 0;
    for (let off = 0; off < 400; off += LIMIT) {
        const d = await get(off);
        total = d.totalCount;
        if (!d.certificates.length) break;
        ref.push(...d.certificates.map(c => c.id));
    }
    console.log(`reference ordering: ${ref.length} ids (totalCount=${total})`);
    if (ref.length < 200) {
        console.error('not enough public certificates to exercise paging');
        process.exit(2);
    }

    // Unaligned offsets — the ones the old integer division destroyed.
    for (const off of [90, 75, 123, 149, 37]) {
        const got = (await get(off)).certificates.map(c => c.id);
        const want = ref.slice(off, off + LIMIT);
        const ok = got.length === want.length && got.every((id, i) => id === want[i]);

        // What the pre-fix code would have returned, so a regression is unmistakable.
        const alignedStart = Math.floor(off / LIMIT) * LIMIT;
        const looksRoundedDown = got.length && got[0] === ref[alignedStart];

        console.log(`offset=${String(off).padStart(3)}  ${ok ? 'OK' : '*** WRONG ROWS ***'}` +
            (ok ? '' : `  (got rows starting at reference index ${ref.indexOf(got[0])}` +
                `${looksRoundedDown ? ` — rounded DOWN to ${alignedStart}` : ''})`));
        if (!ok) failures.push(`offset=${off} returned the wrong slice`);
    }

    // The tail must be reachable from an unaligned offset.
    const tailOff = total - 7;
    const tail = (await get(tailOff)).certificates;
    console.log(`offset=${tailOff} (tail)  returned ${tail.length}, expected 7  ` +
        `${tail.length === 7 ? 'OK' : '*** WRONG ***'}`);
    if (tail.length !== 7) failures.push(`tail offset returned ${tail.length} rows, expected 7`);

    console.log('');
    if (failures.length) {
        console.log('FAIL');
        failures.forEach(f => console.log('  - ' + f));
        process.exit(1);
    }
    console.log('PASS — unaligned offsets return the exact rows requested');
})().catch(e => { console.error(e); process.exit(1); });
