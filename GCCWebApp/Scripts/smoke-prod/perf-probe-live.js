// READ-ONLY performance probe — gccwebapp.com (test site, prod DB).
// Measures server response time (TTFB), full page load, and time-to-content for the
// paths where perf matters. GET-only; nothing is created/edited/deleted.
//
// Intensity is intentionally gentle (sequential runs + one small concurrency burst)
// because this is the shared prod-DB test box and has 503'd under load before.
//
// Run:  node Scripts/smoke-prod/perf-probe-live.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const APP_USER = 'admin', APP_PASS = 'TestPass123!';
const RUNS = 3;               // sequential samples per page
const BURST = 8;              // concurrency burst size (gentle)

const rows = [];
const fmt = (n) => (n == null ? '—' : Math.round(n) + 'ms');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // capture nav-timing for the current page
  async function navTiming(p) {
    return await p.evaluate(() => {
      const t = performance.getEntriesByType('navigation')[0];
      if (!t) return null;
      return {
        ttfb: t.responseStart,                 // request start is 0 baseline
        domContentLoaded: t.domContentLoadedEventEnd,
        load: t.loadEventEnd || t.duration,
        transferKB: Math.round((t.transferSize || 0) / 1024),
      };
    });
  }

  // measure a page over RUNS, optionally waiting for a content selector to appear
  async function measurePage(name, url, contentSelector) {
    const samples = [];
    for (let i = 0; i < RUNS; i++) {
      const t0 = Date.now();
      let status = 0;
      try {
        const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 45000 });
        status = resp ? resp.status() : 0;
      } catch (e) { samples.push({ err: e.message }); continue; }
      let contentMs = null;
      if (contentSelector) {
        try { await page.waitForSelector(contentSelector, { timeout: 20000 }); contentMs = Date.now() - t0; }
        catch { contentMs = null; }
      }
      const nav = await navTiming(page);
      samples.push({ status, wall: Date.now() - t0, contentMs, ...nav });
    }
    const ok = samples.filter(s => !s.err);
    const med = (k) => { const v = ok.map(s => s[k]).filter(x => x != null).sort((a, b) => a - b); return v.length ? v[Math.floor(v.length / 2)] : null; };
    const r = {
      name, url: url.replace(BASE, ''),
      status: ok[0]?.status,
      ttfb_med: med('ttfb'), load_med: med('load'), content_med: med('contentMs'),
      transferKB: ok[0]?.transferKB,
      errors: samples.filter(s => s.err).length,
    };
    rows.push(r);
    console.log(`  ${name.padEnd(26)} status=${r.status} ttfb=${fmt(r.ttfb_med)} load=${fmt(r.load_med)} content=${fmt(r.content_med)} ${r.transferKB ? r.transferKB + 'KB' : ''}${r.errors ? '  ERRORS:' + r.errors : ''}`);
    return r;
  }

  try {
    console.log(`\nPerformance probe — ${BASE}  (median of ${RUNS} runs)\n`);

    // --- public (unauthenticated app page, behind gate only) ---
    console.log('PUBLIC:');
    await measurePage('Public tile view', BASE + '/Certificate/CertificateTileView', '.certificate-card, .cert-tile, table');

    // --- login as admin ---
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(2000);
    const loggedIn = !/\/Account\/Login/i.test(page.url());
    console.log(`\nADMIN (login ${loggedIn ? 'ok' : 'FAILED'}):`);
    if (loggedIn) {
      await measurePage('Internal tile view', BASE + '/Certificate/CertificateTileViewInternal', '.certificate-card, table, .tile');
      await measurePage('Admin Product Types', BASE + '/Admin/ProductTypes', '.node-count, .tree-node, .product-type');
      await measurePage('Product Management', BASE + '/Product/Management', 'table, .product-row');
      await measurePage('Dashboard', BASE + '/Dashboard', '.stat, .card, canvas, table');
      await measurePage('Cert Edit (26-0166)', BASE + '/Certificate/Edit/26-0166', '#ProductId, form');
    }

    // --- gentle concurrency burst on the public tile view (server-render path) ---
    console.log(`\nCONCURRENCY (${BURST} parallel GETs, public tile view):`);
    const burstUrl = BASE + '/Certificate/CertificateTileView';
    const t0 = Date.now();
    const settled = await Promise.allSettled(
      Array.from({ length: BURST }, async () => {
        const s = Date.now();
        const r = await ctx.request.get(burstUrl, { timeout: 45000 });
        return { status: r.status(), ms: Date.now() - s };
      })
    );
    const totalMs = Date.now() - t0;
    const oks = settled.filter(s => s.status === 'fulfilled').map(s => s.value);
    const lat = oks.map(o => o.ms).sort((a, b) => a - b);
    const failed = settled.length - oks.length;
    const burst = {
      name: `concurrency x${BURST}`, total_ms: totalMs,
      min: lat[0], median: lat[Math.floor(lat.length / 2)], max: lat[lat.length - 1],
      failed, statuses: [...new Set(oks.map(o => o.status))],
    };
    rows.push(burst);
    console.log(`  ${BURST} reqs in ${totalMs}ms — min=${fmt(burst.min)} median=${fmt(burst.median)} max=${fmt(burst.max)} failed=${failed} statuses=${burst.statuses}`);

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
  } finally {
    fs.writeFileSync(path.join(__dirname, 'perf-probe-live-results.json'), JSON.stringify(rows, null, 2));
    await browser.close();
  }

  console.log(`\nResults written to perf-probe-live-results.json`);
})();
