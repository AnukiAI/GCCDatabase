// Does the CR-051 list-first default ever render an EMPTY table? Loads the internal view
// N times in a fresh context each time and reports what actually ended up in the DOM,
// plus console errors and the API response status, so a flake is diagnosable rather than
// just re-observed.
//
// LOCALHOST ONLY.  node Scripts/smoke-prod/probe-list-render-flake.js [runs]

const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const RUNS = parseInt(process.argv[2] || '6', 10);
const LIST_RE = /\/api\/certificate-tiles-internal(\?|$)/i;

async function login(page) {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('#Email', 'admin');
    await page.fill('#Password', 'TestPass123!');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 30000 }).catch(() => {}),
        page.click('button[type="submit"]'),
    ]);
}

(async () => {
    const browser = await chromium.launch({ headless: true });
    let empty = 0;

    for (let i = 1; i <= RUNS; i++) {
        const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
        const page = await ctx.newPage();
        const errors = [], reqs = [];
        page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
        page.on('pageerror', e => errors.push('pageerror: ' + e.message));
        page.on('response', async r => {
            if (LIST_RE.test(r.url())) reqs.push(`${r.status()} ${r.url().slice(BASE.length)}`);
        });

        await login(page);
        reqs.length = 0; errors.length = 0;
        await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
        await page.waitForTimeout(9000);

        const d = await page.evaluate(() => {
            const gb = document.getElementById('gridBody');
            const tv = document.getElementById('tableView');
            return {
                rows: document.querySelectorAll('#gridBody .table-body-row').length,
                cards: document.querySelectorAll('#certificatesGrid .certificate-card').length,
                gridBodyExists: !!gb,
                gridBodyChildren: gb ? gb.children.length : -1,
                gridBodyHtmlLen: gb ? gb.innerHTML.length : -1,
                tableViewClasses: tv ? tv.className : '(none)',
                tableViewDisplay: tv ? getComputedStyle(tv).display : '(none)',
                spinner: !!document.getElementById('initialLoadingState'),
                noResults: !!document.querySelector('.no-results, .empty-state'),
                resultsCount: (document.getElementById('tableResultsCount') || {}).textContent || '(none)',
            };
        });

        const bad = d.rows === 0;
        if (bad) empty++;
        console.log(`run ${i}: ${bad ? 'EMPTY' : 'ok   '} rows=${d.rows} cards=${d.cards} gridBodyChildren=${d.gridBodyChildren} htmlLen=${d.gridBodyHtmlLen}`);
        console.log(`        tableView="${d.tableViewClasses}" display=${d.tableViewDisplay} spinner=${d.spinner} noResults=${d.noResults}`);
        console.log(`        resultsCount="${String(d.resultsCount).trim()}" api=[${reqs.join(', ') || 'none'}] errors=${errors.length ? errors.slice(0, 2).join(' || ') : 'none'}`);
        await ctx.close();
    }

    console.log(`\n================ ${empty}/${RUNS} runs rendered an EMPTY list ================`);
    await browser.close();
    process.exit(empty === 0 ? 0 : 1);
})().catch(e => { console.error('PROBE ERROR:', e); process.exit(3); });
