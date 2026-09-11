// Verification for CR-048 (QC #261 — Edit opens in a new tab) and
// CR-049 (QC #262 — List is the default view for logged-in users).
//
// LOCALHOST ONLY. Run:  node Scripts/smoke-prod/verify-cr048-cr049.js
//
// Every assertion prints its OBSERVED value, not just PASS/FAIL, so a green run is
// falsifiable rather than self-reported.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
if (!/^http:\/\/localhost:/.test(BASE)) {
    console.error('Refusing to run against a non-localhost target: ' + BASE);
    process.exit(2);
}

const OUT = path.join(__dirname, '_results', 'cr048-cr049');
fs.mkdirSync(OUT, { recursive: true });

const results = [];
function record(id, desc, status, observed) {
    results.push({ id, desc, status, observed });
    const mark = status === 'PASS' ? 'PASS' : status === 'FAIL' ? 'FAIL' : status;
    console.log(`  [${mark}] ${id} — ${desc}\n         observed: ${observed}`);
}

const CREDS = {
    admin: { user: 'admin', pass: 'TestPass123!' },
    external: { user: 'external', pass: 'TestPass123!' },
};

async function login(page, role) {
    const c = CREDS[role];
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('#Email', c.user);
    await page.fill('#Password', c.pass);
    // NOT networkidle: the post-login landing page is the tile/list view, whose infinite
    // scroll can keep the network busy well past a networkidle window.
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 30000 }).catch(() => {}),
        page.click('button[type="submit"]'),
    ]);
    const url = page.url();
    if (/\/Account\/Login/.test(url)) throw new Error(`login failed for ${role} — still at ${url}`);
    return url;
}

// Snapshot of the view state the two CRs care about.
async function viewState(page) {
    return page.evaluate(() => {
        const btn = v => document.querySelector(`.toggle-btn[data-view="${v}"]`);
        const tile = btn('tile'), list = btn('list');
        const tableView = document.getElementById('tableView');
        const grid = document.getElementById('certificatesGrid');
        return {
            bodyTableActive: document.body.classList.contains('table-view-active'),
            tableViewActive: !!tableView && tableView.classList.contains('active'),
            tableViewDisplay: tableView ? getComputedStyle(tableView).display : '(no #tableView)',
            gridHidden: !!grid && grid.classList.contains('hidden-for-table'),
            tileActive: !!tile && tile.classList.contains('active'),
            tileAria: tile ? tile.getAttribute('aria-pressed') : '(none)',
            listActive: !!list && list.classList.contains('active'),
            listAria: list ? list.getAttribute('aria-pressed') : '(none)',
            rowCount: document.querySelectorAll('#gridBody .table-body-row, #gridBody [role="row"]').length,
            tileCardCount: document.querySelectorAll('#certificatesGrid .certificate-card').length,
            prefInternal: (() => { try { return localStorage.getItem('gcc-cert-view-internal'); } catch (e) { return '(blocked)'; } })(),
            prefExternal: (() => { try { return localStorage.getItem('gcc-cert-view-external'); } catch (e) { return '(blocked)'; } })(),
            spinnerStillPresent: !!document.getElementById('initialLoadingState'),
        };
    });
}

// Wait for the view to finish its first load: spinner gone AND either rows/cards rendered or
// an explicit empty state. Fixed sleeps were losing this race at some viewports.
async function waitForLoaded(page, timeout = 25000) {
    await page.waitForFunction(() => {
        if (document.getElementById('initialLoadingState')) return false;
        const rows = document.querySelectorAll('#gridBody .table-body-row').length;
        const cards = document.querySelectorAll('#certificatesGrid .certificate-card').length;
        const empty = document.querySelector('.no-results, .empty-state');
        return rows > 0 || cards > 0 || !!empty;
    }, null, { timeout }).catch(() => {});
}

const PREEXISTING_ERR = /Invalid or unexpected token/; // fires on untouched pages incl. anonymous Home

function attachTracking(page, bucket) {
    page.on('console', m => { if (m.type() === 'error') bucket.consoleErrors.push(m.text()); });
    page.on('pageerror', e => {
        const msg = 'pageerror: ' + e.message;
        if (PREEXISTING_ERR.test(e.message)) { (bucket.preExisting = bucket.preExisting || []).push(msg); return; }
        bucket.consoleErrors.push(msg);
    });
    page.on('request', r => {
        const u = r.url();
        if (/\/api\/(certificate-tiles-internal|CertificateTileExternal)\?/i.test(u)) bucket.certRequests.push(u);
    });
}

async function run() {
    const browser = await chromium.launch({ headless: process.env.SMOKE_HEADLESS !== '0' });
    let failures = 0;

    // WARM-UP — not an assertion. The app's FIRST certificate query after a cold start takes
    // ~38-42s on dev (measured in BOTH tile and list mode, so it is the app warming up, not
    // CR-049). Without this, the first assertion block times out and every later check
    // cascades off a page that never finished loading — a harness artifact that reads exactly
    // like a product failure.
    {
        const wctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
        const wpage = await wctx.newPage();
        const t0 = Date.now();
        await login(wpage, 'admin');
        await wpage.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
        await waitForLoaded(wpage, 120000);
        console.log(`(warm-up load: ${((Date.now() - t0) / 1000).toFixed(1)}s — excluded from results)`);
        await wctx.close();
    }

    // ── 1. INTERNAL, fresh browser state: must open in LIST ────────────────────────────────
    console.log('\n=== CR-049 · internal view · fresh state (no saved preference) ===');
    let ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    let page = await ctx.newPage();
    let bucket = { consoleErrors: [], certRequests: [] };
    attachTracking(page, bucket);

    await login(page, 'admin');
    bucket.certRequests.length = 0; // the post-login landing page is itself a tile view: don't count its fetch
    bucket.consoleErrors.length = 0;
    await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
    await waitForLoaded(page);

    let s = await viewState(page);
    record('CR049.int.default', 'Internal view opens in LIST with no saved preference',
        s.tableViewActive && s.bodyTableActive && s.gridHidden ? 'PASS' : 'FAIL',
        `tableView.active=${s.tableViewActive} display=${s.tableViewDisplay} body.table-view-active=${s.bodyTableActive} grid.hidden-for-table=${s.gridHidden}`);
    record('CR049.int.toggle', 'List toggle button is the active one, aria-pressed correct on BOTH',
        s.listActive && s.listAria === 'true' && !s.tileActive && s.tileAria === 'false' ? 'PASS' : 'FAIL',
        `list: active=${s.listActive} aria-pressed=${s.listAria} | tile: active=${s.tileActive} aria-pressed=${s.tileAria}`);
    record('CR049.int.rows', 'Table rows actually rendered (not an empty default view)',
        s.rowCount > 0 ? 'PASS' : 'FAIL', `#gridBody rows=${s.rowCount}`);
    record('CR049.int.spinner', 'Initial loading spinner cleared after load',
        s.spinnerStillPresent === false ? 'PASS' : 'FAIL', `#initialLoadingState present=${s.spinnerStillPresent}`);
    record('CR049.int.noDoubleLoad', 'Exactly ONE certificates fetch on load (no duplicate from showTableView)',
        bucket.certRequests.length === 1 ? 'PASS' : 'FAIL',
        `${bucket.certRequests.length} request(s): ${bucket.certRequests.join(' | ') || '(none)'}`);
    record('CR049.int.preexisting', 'Pre-existing page error present but NOT caused by these CRs (Home shows it too)',
        'INFO', `${(bucket.preExisting || []).length} occurrence(s): ${(bucket.preExisting || [])[0] || '(none)'}`);
    record('CR049.int.console', 'No NEW console/page errors on load',
        bucket.consoleErrors.length === 0 ? 'PASS' : 'FAIL',
        bucket.consoleErrors.length ? bucket.consoleErrors.slice(0, 4).join(' || ') : '(none)');

    // Runaway-pagination probe. The infinite-scroll sentinel is appended AFTER #tableView, so
    // list-first could in principle sit in view at load and chain-load every page. Measure it:
    // sit still for 8s and count how many list fetches actually fired.
    const reqAtSettle = bucket.certRequests.length;
    await page.waitForTimeout(8000);
    const reqAfterIdle = bucket.certRequests.length;
    const rowsAfterIdle = (await viewState(page)).rowCount;
    record('CR049.int.noRunaway', 'List-first does not chain-load pages while the user sits still',
        reqAfterIdle <= 2 ? 'PASS' : 'FAIL',
        `list fetches: ${reqAtSettle} at settle -> ${reqAfterIdle} after 8s idle; rows now ${rowsAfterIdle}`);

    // ── 2. CR-048: Edit opens a NEW TAB, list survives ─────────────────────────────────────
    console.log('\n=== CR-048 · Edit opens a new tab from the LIST view ===');
    const editBtn = page.locator('#gridBody .action-btn.edit-btn').first();
    const editCount = await page.locator('#gridBody .action-btn.edit-btn').count();
    if (editCount === 0) {
        record('CR048.list.newtab', 'Edit button present in list rows', 'FAIL', 'no .action-btn.edit-btn found in #gridBody');
    } else {
        const listUrlBefore = page.url();
        const [popup] = await Promise.all([
            ctx.waitForEvent('page', { timeout: 10000 }).catch(() => null),
            editBtn.click(),
        ]);
        await page.waitForTimeout(600); await waitForLoaded(page);
        const popupUrl = popup ? popup.url() : '(no new tab opened)';
        record('CR048.list.newtab', 'Edit opens a NEW tab pointing at /Certificate/Edit/{number}',
            popup && /\/Certificate\/Edit\//.test(popupUrl) ? 'PASS' : 'FAIL', `new tab url=${popupUrl}`);
        record('CR048.list.survives', 'Originating list tab did NOT navigate away',
            page.url() === listUrlBefore ? 'PASS' : 'FAIL', `before=${listUrlBefore}\n                   after =${page.url()}`);
        if (popup) {
            await popup.waitForLoadState('domcontentloaded').catch(() => {});
            const hasForm = await popup.locator('form').count().catch(() => 0);
            record('CR048.edit.loads', 'Edit page in the new tab renders a form (route keyed by number works)',
                hasForm > 0 ? 'PASS' : 'FAIL', `<form> count=${hasForm}, title="${await popup.title().catch(() => '?')}"`);
            await popup.close();
        }
    }

    // ── 3. CR-049: a deliberate switch to Tile is remembered ───────────────────────────────
    console.log('\n=== CR-049 · explicit Tile choice persists across reload ===');
    await page.click('.toggle-btn[data-view="tile"]');
    await page.waitForTimeout(600); await waitForLoaded(page);
    let afterClick = await viewState(page);
    record('CR049.pref.saved', 'Clicking Tile writes the preference',
        afterClick.prefInternal === 'tile' ? 'PASS' : 'FAIL', `localStorage[gcc-cert-view-internal]=${afterClick.prefInternal}`);

    await page.reload({ waitUntil: 'domcontentloaded' });
    await waitForLoaded(page);
    s = await viewState(page);
    record('CR049.pref.tileSticks', 'Reload honours the saved Tile preference (default does not override a choice)',
        !s.tableViewActive && s.tileActive && s.tileAria === 'true' && s.tileCardCount > 0 ? 'PASS' : 'FAIL',
        `tableView.active=${s.tableViewActive} tile.active=${s.tileActive} tile.aria=${s.tileAria} cards=${s.tileCardCount}`);

    console.log('\n=== CR-049 · switching back to List persists too ===');
    await page.click('.toggle-btn[data-view="list"]');
    await page.waitForTimeout(600); await waitForLoaded(page);
    await page.reload({ waitUntil: 'domcontentloaded' });
    await waitForLoaded(page);
    s = await viewState(page);
    record('CR049.pref.listSticks', 'Reload after choosing List re-opens in List',
        s.tableViewActive && s.listActive && s.rowCount > 0 ? 'PASS' : 'FAIL',
        `tableView.active=${s.tableViewActive} list.active=${s.listActive} rows=${s.rowCount} pref=${s.prefInternal}`);

    await ctx.close();

    // ── 4. EXTERNAL view, fresh state ──────────────────────────────────────────────────────
    console.log('\n=== CR-049 · external view · fresh state ===');
    ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    page = await ctx.newPage();
    bucket = { consoleErrors: [], certRequests: [] };
    attachTracking(page, bucket);
    try {
        await login(page, 'external');
        bucket.certRequests.length = 0; // same: external lands on a tile view
        bucket.consoleErrors.length = 0;
        await page.goto(`${BASE}/Certificate/CertificateTileViewExternal`, { waitUntil: 'domcontentloaded' });
        await waitForLoaded(page);
        s = await viewState(page);
        record('CR049.ext.default', 'External view opens in LIST with no saved preference',
            s.tableViewActive && s.bodyTableActive && s.gridHidden ? 'PASS' : 'FAIL',
            `tableView.active=${s.tableViewActive} body.table-view-active=${s.bodyTableActive} grid.hidden=${s.gridHidden} rows=${s.rowCount}`);
        record('CR049.ext.toggle', 'External toggle aria-pressed correct on both buttons',
            s.listActive && s.listAria === 'true' && s.tileAria === 'false' ? 'PASS' : 'FAIL',
            `list: active=${s.listActive} aria=${s.listAria} | tile: active=${s.tileActive} aria=${s.tileAria}`);
        record('CR049.ext.noDoubleLoad', 'Exactly ONE certificates fetch on external load',
            bucket.certRequests.length === 1 ? 'PASS' : 'FAIL',
            `${bucket.certRequests.length} request(s)`);
        record('CR049.ext.console', 'No console/page errors on external load',
            bucket.consoleErrors.length === 0 ? 'PASS' : 'FAIL',
            bucket.consoleErrors.length ? bucket.consoleErrors.slice(0, 4).join(' || ') : '(none)');
        record('CR049.ext.noEditBtn', 'External list rows still expose NO Edit button (RBAC unchanged)',
            (await page.locator('#gridBody .action-btn.edit-btn').count()) === 0 ? 'PASS' : 'FAIL',
            `edit buttons in external #gridBody = ${await page.locator('#gridBody .action-btn.edit-btn').count()}`);
    } catch (e) {
        record('CR049.ext.default', 'External view opens in LIST', 'BLOCKED', e.message);
    }
    await ctx.close();

    // ── 5. Viewport pass on the new default (internal, list-first) ─────────────────────────
    console.log('\n=== CR-049 · viewport pass on the new list-first default ===');
    const VIEWPORTS = [
        ['desktop-xl', 1920, 1080],
        ['desktop', 1440, 900],
        ['laptop', 1280, 800],
        ['tablet', 768, 1024],
        ['mobile', 390, 844],
    ];
    for (const [name, w, h] of VIEWPORTS) {
        const vctx = await browser.newContext({ viewport: { width: w, height: h } });
        const vpage = await vctx.newPage();
        const vb = { consoleErrors: [], certRequests: [] };
        attachTracking(vpage, vb);
        await login(vpage, 'admin');
        await vpage.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
        await waitForLoaded(vpage);
        const vs = await viewState(vpage);
        const overflow = await vpage.evaluate(() =>
            Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth));
        const shot = path.join(OUT, `internal-list-${name}-${w}x${h}.png`);
        await vpage.screenshot({ path: shot, fullPage: true });
        record(`CR049.vp.${name}`, `List default renders at ${w}x${h} with no horizontal page overflow`,
            vs.tableViewActive && vs.rowCount > 0 && overflow === 0 ? 'PASS' : 'FAIL',
            `list=${vs.tableViewActive} rows=${vs.rowCount} bodyOverflowPx=${overflow} errors=${vb.consoleErrors.length} shot=${path.basename(shot)}`);
        await vctx.close();
    }

    await browser.close();

    failures = results.filter(r => r.status !== 'PASS').length;
    const summary = {
        ranAt: new Date().toISOString(),
        base: BASE,
        total: results.length,
        passed: results.filter(r => r.status === 'PASS').length,
        failed: failures,
        results,
    };
    fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(summary, null, 2));
    console.log(`\n================ ${summary.passed}/${summary.total} PASS, ${failures} not-pass ================`);
    console.log(`results: ${path.join(OUT, 'results.json')}`);
    process.exit(failures === 0 ? 0 : 1);
}

run().catch(e => { console.error('HARNESS ERROR:', e); process.exit(3); });
