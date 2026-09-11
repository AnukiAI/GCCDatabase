// Attribution probe: the FIRST internal-view load after a cold app start does not finish
// within 25s, which cascades into every later assertion. Is that cost introduced by CR-049
// (list-first) or is it the app's cold start regardless of view?
//
// Run against a JUST-RESTARTED server. Pass the mode to measure:
//    node Scripts/smoke-prod/probe-cold-start-first-paint.js tile
//    node Scripts/smoke-prod/probe-cold-start-first-paint.js list
//
// LOCALHOST ONLY.

const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const MODE = (process.argv[2] || 'list').toLowerCase();
if (!['tile', 'list'].includes(MODE)) { console.error('mode must be tile|list'); process.exit(2); }

(async () => {
    const browser = await chromium.launch({ headless: true });
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await ctx.newPage();

    const timings = [];
    page.on('response', r => {
        if (/\/api\/certificate-tiles-internal(\?|$)/i.test(r.url())) {
            timings.push({ status: r.status(), at: Date.now() });
        }
    });

    // Log in FIRST (this itself pays part of the cold start).
    const tLoginStart = Date.now();
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('#Email', 'admin');
    await page.fill('#Password', 'TestPass123!');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 60000 }).catch(() => {}),
        page.click('button[type="submit"]'),
    ]);
    const loginMs = Date.now() - tLoginStart;

    // Force the view mode under test, then load the view fresh.
    await page.evaluate(m => { try { localStorage.setItem('gcc-cert-view-internal', m); } catch (e) {} }, MODE);

    const t0 = Date.now();
    await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
    const domMs = Date.now() - t0;

    let contentMs = -1;
    try {
        await page.waitForFunction(() => {
            if (document.getElementById('initialLoadingState')) return false;
            return document.querySelectorAll('#gridBody .table-body-row').length > 0
                || document.querySelectorAll('#certificatesGrid .certificate-card').length > 0;
        }, null, { timeout: 90000 });
        contentMs = Date.now() - t0;
    } catch (e) {
        contentMs = -1;
    }

    const apiMs = timings.length ? timings[0].at - t0 : -1;
    const d = await page.evaluate(() => ({
        rows: document.querySelectorAll('#gridBody .table-body-row').length,
        cards: document.querySelectorAll('#certificatesGrid .certificate-card').length,
    }));

    console.log(`mode=${MODE}`);
    console.log(`  login round trip     : ${loginMs} ms`);
    console.log(`  domcontentloaded     : ${domMs} ms`);
    console.log(`  first API response   : ${apiMs === -1 ? 'NONE SEEN' : apiMs + ' ms'} (${timings.length} response(s))`);
    console.log(`  content on screen    : ${contentMs === -1 ? 'TIMED OUT at 90s' : contentMs + ' ms'}`);
    console.log(`  rendered             : rows=${d.rows} cards=${d.cards}`);

    await browser.close();
})().catch(e => { console.error('PROBE ERROR:', e); process.exit(3); });
