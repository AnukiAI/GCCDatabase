/**
 * DEF-381 regression: public tile view repeats prior batches on infinite scroll.
 *
 * Reported 2026-08-28: "when it reloads a new batch as you hit the bottom of the current, it just
 * repeats the prior" — sorted or unsorted, signed in or not, unaffected by clearing cache.
 *
 * Root cause was NOT the database and NOT the API. `renderCertificates(append)` in
 * certificate-tile-view.js built its HTML from the whole accumulated `state.certificates` array and
 * then appended that to the grid, so batch 2 re-drew certs 1-100 underneath the existing 1-50,
 * batch 3 re-drew 1-150, and so on. Tiles grew quadratically (~10k DOM nodes for 963 certs), which
 * is what made the page feel like it was reloading.
 *
 * This test asserts on the DOM, not on the API — the API was already returning clean, distinct
 * pages while the screen was full of duplicates, so a test at the API layer cannot see this bug.
 *
 * Usage: node Scripts/smoke-prod/_regression/08-tile-scroll-dupes.js [baseUrl]
 */
const { chromium } = require('playwright');

const BASE = process.argv[2] || 'http://localhost:5101';
const PATH = '/Certificate/CertificateTileView';
const BATCHES = 4;

// Localhost-only unless explicitly pointed elsewhere, so a stray run can't hammer the test env.
if (!/^https?:\/\/(localhost|127\.0\.0\.1)/.test(BASE) && !process.env.ALLOW_REMOTE) {
    console.error(`refusing to run against ${BASE} without ALLOW_REMOTE=1`);
    process.exit(2);
}

const tileCounts = async (page) => page.evaluate(() => {
    const grid = document.getElementById('certificatesGrid');
    if (!grid) return { tiles: 0, distinct: 0, shown: null };
    // Each tile carries the cert id/number; fall back to the card's text signature.
    const cards = Array.from(grid.querySelectorAll('.certificate-card'));
    const keys = cards.map(c => c.getAttribute('data-id') || c.textContent.trim().slice(0, 120));
    return {
        tiles: cards.length,
        distinct: new Set(keys).size,
        shown: document.getElementById('resultsCount')?.textContent?.trim() || null,
    };
});

(async () => {
    const browser = await chromium.launch();
    const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
    const failures = [];

    // Remote runs sit behind the test-environment gate. Without passing it, EVERY url returns
    // 200 + a ~2.5KB gate page, and the grid selector simply never appears — which reads as a
    // broken page rather than "you were never let in".
    if (!/localhost|127\.0\.0\.1/.test(BASE)) {
        const gatePass = process.env.GATE_PASS || 'Cert-Test-Access-2026!';
        await page.goto(`${BASE}/__gate?returnUrl=%2F`, { waitUntil: 'domcontentloaded', timeout: 60000 });
        const pwd = page.locator('input[type="password"]').first();
        if (await pwd.count()) {
            await pwd.fill(gatePass);
            await Promise.all([
                page.waitForLoadState('domcontentloaded'),
                page.locator('form button[type="submit"], form input[type="submit"]').first().click(),
            ]);
            console.log('passed the test-environment gate');
        }
    }

    await page.goto(BASE + PATH, { waitUntil: 'networkidle', timeout: 60000 });
    const gated = await page.locator('input[type="password"]').count();
    if (gated) {
        console.error('still on the gate page — the password was rejected; aborting rather than reporting a pass');
        await browser.close();
        process.exit(2);
    }
    await page.waitForSelector('#certificatesGrid', { timeout: 30000 });
    await page.waitForTimeout(1500);

    let prev = await tileCounts(page);
    console.log(`initial load        tiles=${prev.tiles}  distinct=${prev.distinct}  "${prev.shown}"`);
    if (prev.tiles !== prev.distinct) {
        failures.push(`initial load already has duplicates: ${prev.tiles} tiles / ${prev.distinct} distinct`);
    }

    for (let i = 1; i <= BATCHES; i++) {
        await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
        // Wait for the tile count to actually grow rather than for a fixed nap.
        await page.waitForFunction(
            (n) => document.querySelectorAll('#certificatesGrid .certificate-card').length > n,
            prev.tiles, { timeout: 20000 }
        ).catch(() => {});
        await page.waitForTimeout(900);

        const cur = await tileCounts(page);
        const grew = cur.tiles - prev.tiles;
        const ok = cur.tiles === cur.distinct;
        console.log(`after scroll ${i}      tiles=${cur.tiles} (+${grew})  distinct=${cur.distinct}  ${ok ? 'OK' : '*** DUPLICATES ***'}  "${cur.shown}"`);
        if (!ok) {
            failures.push(`scroll ${i}: ${cur.tiles} tiles rendered but only ${cur.distinct} distinct (${cur.tiles - cur.distinct} duplicated)`);
        }
        if (grew > 60) {
            failures.push(`scroll ${i}: batch added ${grew} tiles, expected <= 50 (batchSize)`);
        }
        prev = cur;
    }

    await browser.close();
    console.log('');
    if (failures.length) {
        console.log('FAIL');
        failures.forEach(f => console.log('  - ' + f));
        process.exit(1);
    }
    console.log(`PASS — ${BATCHES} scrolls, every tile on screen is unique`);
})().catch(e => { console.error(e); process.exit(1); });
