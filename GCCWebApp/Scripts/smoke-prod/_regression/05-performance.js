// Phase 5 — performance. Page-load timings for the routes each role actually lives in, plus the
// data APIs behind them.
//
// Method: each route is loaded REPEATS times in one warm session and the median reported, because
// a single sample on a shared IIS box mostly measures whether the app pool had just spun up. The
// first (cold) sample is reported separately rather than averaged in — cold start is a real user
// experience on a test box that idles out, and burying it in a mean hides both facts.
//
// Budgets are advisory: they mark what is worth looking at, not a contractual SLA.

const { BASE, HEADLESS, NAV_TIMEOUT, session, makeRecorder, chromium } = require('./_lib/regression-lib');

const REPEATS = 5;

// [path, role, budgetMs] — budgets reflect what the page does, not one global number.
const TARGETS = [
  ['/',                                          'anon',    2000],
  ['/Certificate/CertificateTileView',           'anon',    3000],
  ['/Certificate/CertificateTileViewExternal',   'external', 3000],
  ['/Certificate/CertificateTileViewExternal',   'amazon',  3000],
  ['/Certificate/CertificateTileViewInternal',   'analyst', 4000],
  ['/Certificate/Create',                        'analyst', 5000],
  ['/Dashboard/Index',                           'analyst', 6000],
  ['/Company/Management',                        'analyst', 4000],
  ['/Product/Management',                        'analyst', 5000],
  ['/Admin/UserManagement',                      'admin',   4000],
  ['/Admin/ProductManagement',                   'admin',   5000],
];

// API endpoints — the tile pages are shells; this is where the time actually goes.
const API_TARGETS = [
  ['/api/certificate-tiles?page=1&pageSize=20',                'anon',     2000],
  ['/api/certificate-tiles/filters',                           'anon',     3000],
  ['/api/certificate-tiles-internal?page=1&pageSize=20',       'analyst',  3000],
  ['/api/certificate-tiles-internal?page=50&pageSize=20',      'analyst',  3000],  // deep page
  ['/api/certificate-tiles-internal?search=sika&page=1&pageSize=20', 'analyst', 3000],
  ['/api/CertificateTileExternal?page=1&pageSize=20',          'external', 2000],
  ['/api/certificate-tiles-internal/filters',                  'analyst',  4000],
  ['/Certificate/ExportCertificates?format=csv',               'analyst', 15000],  // 5,702 rows
];

const median = (a) => { const s = [...a].sort((x, y) => x - y); return s.length % 2 ? s[(s.length - 1) / 2] : Math.round((s[s.length / 2 - 1] + s[s.length / 2]) / 2); };

(async () => {
  const { rec, save } = makeRecorder('05-performance');
  const startedAt = new Date().toISOString();
  console.log(`\n=== Phase 5: performance (${REPEATS} samples per target) ===\n`);

  const browser = await chromium.launch({ headless: HEADLESS });
  const byRole = {};
  const detail = [];

  const roles = [...new Set([...TARGETS, ...API_TARGETS].map(([, r]) => r))];
  for (const role of roles) {
    byRole[role] = await session(browser, role === 'anon' ? null : role);
  }

  // ---- Page loads ----
  for (const [route, role, budget] of TARGETS) {
    const { page } = byRole[role];
    const samples = [];
    let nav = null;
    for (let i = 0; i < REPEATS; i++) {
      const t0 = Date.now();
      await page.goto(BASE + route, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
      samples.push(Date.now() - t0);
      if (i === REPEATS - 1) {
        // Navigation Timing separates "server was slow" from "the page was heavy".
        nav = await page.evaluate(() => {
          const n = performance.getEntriesByType('navigation')[0];
          if (!n) return null;
          return { ttfb: Math.round(n.responseStart - n.requestStart), domContentLoaded: Math.round(n.domContentLoadedEventEnd), load: Math.round(n.loadEventEnd), transfer: n.transferSize };
        });
      }
    }
    const cold = samples[0], med = median(samples.slice(1).length ? samples.slice(1) : samples);
    detail.push({ kind: 'page', route, role, cold, median: med, samples, nav, budget });
    rec(`perf.page.${route}.${role}`, `${route} as ${role}`,
      med <= budget ? 'PASS' : med <= budget * 1.5 ? 'WARN' : 'FAIL',
      `median ${med}ms (cold ${cold}ms, budget ${budget}ms)` + (nav ? `, TTFB ${nav.ttfb}ms, ${(nav.transfer / 1024).toFixed(0)}KB` : ''));
  }

  // ---- API calls ----
  for (const [route, role, budget] of API_TARGETS) {
    const { page } = byRole[role];
    const samples = [];
    let status = null, bytes = 0;
    for (let i = 0; i < REPEATS; i++) {
      const t0 = Date.now();
      const r = await page.request.get(BASE + route, { failOnStatusCode: false, timeout: 120000 });
      const body = await r.body();
      samples.push(Date.now() - t0);
      status = r.status(); bytes = body.length;
    }
    const cold = samples[0], med = median(samples.slice(1).length ? samples.slice(1) : samples);
    detail.push({ kind: 'api', route, role, cold, median: med, samples, status, bytes, budget });
    rec(`perf.api.${route}.${role}`, `${route} as ${role}`,
      status !== 200 ? 'FAIL' : med <= budget ? 'PASS' : med <= budget * 1.5 ? 'WARN' : 'FAIL',
      `HTTP ${status}, median ${med}ms (cold ${cold}ms, budget ${budget}ms), ${(bytes / 1024).toFixed(0)}KB`);
  }

  for (const role of roles) await byRole[role].ctx.close();
  await browser.close();
  save({ startedAt, finishedAt: new Date().toISOString(), repeats: REPEATS, detail });
})().catch((e) => { console.error('FATAL', e); process.exit(1); });
