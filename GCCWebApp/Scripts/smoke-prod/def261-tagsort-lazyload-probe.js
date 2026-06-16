// DEF-261 regression probe — directly exercises the tile API code paths the working-tree
// change touched: (a) the .Min() tag-sort path (int-tag/ext-tag), and (b) page-2 fetch
// (true infinite scroll). Logs in as admin via the same flow the smoke template uses,
// then hits the JSON API and reports HTTP status + latency. A 500 / timeout here is the
// "lazy loading not working" symptom.
const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'http://localhost:5101';

async function login(page) {
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);
}

// Hit an API URL from inside the authenticated page context (carries the auth cookie).
async function probe(page, label, url) {
  const r = await page.evaluate(async (u) => {
    const t0 = performance.now();
    try {
      const resp = await fetch(u, { headers: { 'Accept': 'application/json' } });
      const ms = Math.round(performance.now() - t0);
      let count = null, err = null;
      try { const j = await resp.json(); count = (j.certificates || j.data || j.items || []).length ?? null; }
      catch (e) { err = 'json-parse:' + e.message; }
      return { status: resp.status, ms, count, err };
    } catch (e) { return { status: -1, ms: Math.round(performance.now() - t0), err: String(e) }; }
  }, url);
  const ok = r.status === 200;
  console.log(`  [${ok ? 'PASS' : 'FAIL'}] ${label}  HTTP ${r.status}  ${r.ms}ms  cards=${r.count}${r.err ? '  ERR=' + r.err : ''}`);
  return ok;
}

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await login(page);

  const I = BASE + '/api/certificate-tiles-internal';
  const E = BASE + '/api/CertificateTileExternal';
  let pass = 0, fail = 0;
  const run = async (l, u) => { (await probe(page, l, u)) ? pass++ : fail++; };

  console.log('=== INTERNAL tile API ===');
  await run('internal default (newest) p1', `${I}?sortBy=newest&page=1&pageSize=20`);
  await run('internal LAZY page2        ', `${I}?sortBy=newest&page=2&pageSize=20`);
  await run('internal sort=int-tag  .Min', `${I}?sortBy=int-tag&page=1&pageSize=20`);
  await run('internal sort=int-tag-za .Min', `${I}?sortBy=int-tag-za&page=1&pageSize=20`);
  await run('internal sort=ext-tag  .Min', `${I}?sortBy=ext-tag&page=1&pageSize=20`);
  await run('internal sort=ext-tag-za .Min', `${I}?sortBy=ext-tag-za&page=1&pageSize=20`);

  console.log('=== EXTERNAL tile API ===');
  await run('external default (newest) p1', `${E}?sortBy=newest&page=1&pageSize=20`);
  await run('external sort=ext-tag  .Min', `${E}?sortBy=ext-tag&page=1&pageSize=20`);
  await run('external sort=ext-tag-za .Min', `${E}?sortBy=ext-tag-za&page=1&pageSize=20`);

  console.log(`\n[probe] ${pass} PASS / ${fail} FAIL`);
  await browser.close();
  process.exit(fail ? 1 : 0);
})();
