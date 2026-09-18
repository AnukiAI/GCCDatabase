// DEF-430 verification — internal + external tile views must END a failed load in a readable
// state instead of sitting on "Loading…" over the previous filter's tiles.
//
// A: happy path still works (status filter changes the count)      — no regression
// B: forced 500 on a filter change -> retries, then the failure panel replaces the results,
//    the count stops saying "Loading…", and the stale tiles are gone
// C: the Retry button recovers once the route is unblocked
//
// Run: NODE_PATH=<global node_modules> node Scripts/smoke-prod/verify-def430-load-failure.js
// Local dev only (localhost:5101), signed in as admin + external.
const { chromium } = require('playwright');

const BASE = 'http://localhost:5101';
const API_INTERNAL = '**/api/certificate-tiles-internal?*';
const API_EXTERNAL = '**/api/CertificateTileExternal?*';

const sleep = ms => new Promise(r => setTimeout(r, ms));
const results = [];
function record(id, desc, pass, detail) {
  results.push({ id, desc, pass, detail });
  console.log(`  ${pass ? 'PASS' : 'FAIL'}  ${id} — ${desc}${detail ? ' :: ' + detail : ''}`);
}

async function countText(page) {
  const t = await page.locator('#resultsCount').textContent().catch(() => '(none)');
  return (t || '').trim();
}

async function login(page, user, pass) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.fill('input[name="Email"]', user);
  await page.fill('input[name="Password"]', pass);
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
    page.click('button[type="submit"]')
  ]);
}

async function exercise(page, label, viewUrl, apiGlob, useStatusFilter) {
  console.log(`\n=== ${label} ===`);
  await page.goto(viewUrl, { waitUntil: 'domcontentloaded' });
  // Logged-in views open in LIST by default (CR-049), so #certificatesGrid is hidden — wait on
  // the results count, which both views own.
  await page.waitForSelector('#resultsCount', { timeout: 60000 });
  // Assert the POSITIVE terminal state, never merely "not Loading". The first version of this
  // check read `!/Loading/` and passed on the text "Retrying… (1/3)" — a load that had already
  // failed once scored a PASS. Wait for, and assert, "Showing N of N".
  //
  // Generous timeout: a cold SQL cache on this box makes the first list call take up to ~95s (an
  // environment property, not the app — see observations_log 2026-09-17).
  await page.waitForFunction(() => {
    const el = document.getElementById('resultsCount');
    return el && /Showing/.test(el.textContent);
  }, { timeout: 240000 }).catch(() => {});
  const initial = await countText(page);
  record(`${label}.A1`, 'initial load reaches "Showing N of N"', /Showing/.test(initial), initial);

  // --- A: happy path filter change
  if (useStatusFilter) {
    await page.check('#status-content input[value="Expired"]');
    await page.waitForFunction(prev => {
      const el = document.getElementById('resultsCount');
      return el && /Showing/.test(el.textContent) && el.textContent.trim() !== prev;
    }, initial, { timeout: 240000 }).catch(() => {});
    const filtered = await countText(page);
    record(`${label}.A2`, 'status filter changes the count', /Showing/.test(filtered) && filtered !== initial,
      `${initial} -> ${filtered}`);
    await page.uncheck('#status-content input[value="Expired"]');
    await sleep(2500);
  }

  // --- B: forced 500
  let blocked = 0;
  await page.route(apiGlob, route => { blocked++; route.fulfill({ status: 500, body: '{"error":"forced"}' }); });

  if (useStatusFilter) {
    await page.check('#status-content input[value="Draft"]');
  } else {
    // External view has no Status panel (CR-051 is internal-only, deliberately: the status list
    // includes the Confidential embargo state). Any filter change exercises the same path.
    await page.locator('#cert-type-content input[type="checkbox"]').first().check();
  }

  // 3 retries at 1s/2s/4s + request time; allow generous headroom.
  await page.waitForSelector('.tile-load-error', { timeout: 30000 }).catch(() => {});
  const panelVisible = await page.locator('.tile-load-error').count() > 0;
  record(`${label}.B1`, 'failure panel replaces the results', panelVisible, `${blocked} blocked calls`);

  // Again the positive state: the exact failure text, not "anything but Loading".
  const afterFail = await countText(page);
  record(`${label}.B2`, 'count reads "Could not load certificates"',
    /Could not load certificates/.test(afterFail), afterFail);

  const staleTiles = await page.locator('#certificatesGrid .certificate-card, #gridBody .table-body-row').count();
  record(`${label}.B3`, 'stale tiles cleared', staleTiles === 0, `${staleTiles} left on screen`);

  record(`${label}.B4`, 'retried before giving up (>1 call)', blocked > 1, `${blocked} attempts`);

  // --- C: retry recovers
  await page.unroute(apiGlob);
  await page.click('.tile-load-error-retry');
  // The recovered load can itself hit this box's cold-cache path, and the view will retry through
  // its own backoff before succeeding — so wait for the positive state, generously.
  await page.waitForFunction(() => {
    const el = document.getElementById('resultsCount');
    return el && /Showing/.test(el.textContent) && !document.querySelector('.tile-load-error');
  }, { timeout: 240000 }).catch(() => {});
  const recovered = await countText(page);
  const panelGone = await page.locator('.tile-load-error').count() === 0;
  record(`${label}.C1`, 'Retry recovers the list', panelGone && /Showing/.test(recovered), recovered);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  [pageerror]', String(e).slice(0, 200)));

  await login(page, 'admin', 'TestPass123!');
  await exercise(page, 'internal', `${BASE}/Certificate/CertificateTileViewInternal`, API_INTERNAL, true);

  // External view: signed in as an External-scoped account, no status filter panel there.
  const ctx2 = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page2 = await ctx2.newPage();
  page2.on('pageerror', e => console.log('  [pageerror]', String(e).slice(0, 200)));
  await login(page2, 'external', 'TestPass123!');
  await exercise(page2, 'external', `${BASE}/Certificate/CertificateTileViewExternal`, API_EXTERNAL, false);

  const failed = results.filter(r => !r.pass);
  console.log(`\n${results.length - failed.length}/${results.length} passed`);
  if (failed.length) console.log('FAILED: ' + failed.map(f => f.id).join(', '));
  await browser.close();
  process.exit(failed.length ? 1 : 0);
})();
