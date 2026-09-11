// Is the duplicate page=1 fetch on the internal tile view caused by CR-051 (list-first),
// or was it already there in tile mode? Loads the SAME page twice — once with the saved
// preference forced to 'tile' (reproduces the pre-CR-051 behaviour) and once to 'list' —
// and prints every list-endpoint request URL for each.
//
// LOCALHOST ONLY.

const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';

const LIST_RE = /\/api\/(certificate-tiles-internal|CertificateTileExternal)(\?|$)/i;

async function login(page, user) {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('#Email', user);
    await page.fill('#Password', 'TestPass123!');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 30000 }).catch(() => {}),
        page.click('button[type="submit"]'),
    ]);
}

async function measure(browser, label, url, user, prefKey, prefValue) {
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await ctx.newPage();
    const reqs = [];
    page.on('request', r => { if (LIST_RE.test(r.url())) reqs.push(r.url()); });

    await login(page, user);
    // Seed the preference on the app origin before loading the view.
    await page.evaluate(([k, v]) => { try { localStorage.setItem(k, v); } catch (e) {} }, [prefKey, prefValue]);

    reqs.length = 0;
    await page.goto(url, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(9000);

    const rows = await page.evaluate(() => ({
        rows: document.querySelectorAll('#gridBody .table-body-row').length,
        cards: document.querySelectorAll('#certificatesGrid .certificate-card').length,
    }));
    console.log(`\n--- ${label} (pref ${prefKey}=${prefValue}) ---`);
    console.log(`    rows=${rows.rows} cards=${rows.cards}  list fetches=${reqs.length}`);
    reqs.forEach((u, i) => console.log(`      ${i + 1}. ${u}`));
    await ctx.close();
    return reqs.length;
}

(async () => {
    const browser = await chromium.launch({ headless: true });
    const INT = `${BASE}/Certificate/CertificateTileViewInternal`;
    const EXT = `${BASE}/Certificate/CertificateTileViewExternal`;

    const tile = await measure(browser, 'INTERNAL as TILE (pre-CR-051 behaviour)', INT, 'admin', 'gcc-cert-view-internal', 'tile');
    const list = await measure(browser, 'INTERNAL as LIST (new default)', INT, 'admin', 'gcc-cert-view-internal', 'list');
    const extTile = await measure(browser, 'EXTERNAL as TILE (pre-CR-051 behaviour)', EXT, 'external', 'gcc-cert-view-external', 'tile');
    const extList = await measure(browser, 'EXTERNAL as LIST (new default)', EXT, 'external', 'gcc-cert-view-external', 'list');

    console.log('\n================ verdict ================');
    console.log(`internal: tile=${tile} fetches, list=${list} fetches -> ${list > tile ? 'LIST ADDS A FETCH (regression)' : 'same count, pre-existing'}`);
    console.log(`external: tile=${extTile} fetches, list=${extList} fetches -> ${extList > extTile ? 'LIST ADDS A FETCH (regression)' : 'same count, pre-existing'}`);
    await browser.close();
})().catch(e => { console.error('PROBE ERROR:', e); process.exit(3); });
