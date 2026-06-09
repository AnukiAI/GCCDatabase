// DEF-260 — verify the ASIN/UPC/GTIN/EAN columns sort by the COUNT of IDs (the count badge),
// with empty cells last in both directions. Read-only. Local or remote (gate-aware).
const { chromium } = require('playwright');
const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const ADMIN = { email: process.env.SMOKE_USER || 'admin', password: process.env.SMOKE_PASS || 'TestPass123!' };
const IS_REMOTE = !/localhost|127\.0\.0\.1/.test(BASE);
const GATE = IS_REMOTE ? { username: process.env.SMOKE_REMOTE_USER || 'gcc', password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' } : undefined;

const countIds = (v) => String(v || '').split(',').map(s => s.trim()).filter(Boolean).length;

async function fetchAll(req, sortBy) {
  const all = []; let page = 1;
  for (;;) {
    const r = await req.get(`${BASE}/api/certificate-tiles-internal?sortBy=${encodeURIComponent(sortBy)}&page=${page}&pageSize=100`);
    if (r.status() !== 200) throw new Error(`API ${sortBy} -> HTTP ${r.status()}`);
    const j = await r.json(); const arr = j.certificates || [];
    all.push(...arr); if (!j.hasMore || !arr.length || page > 50) break; page++;
  }
  return all;
}
function checkMonotonic(counts, dir) {
  const pop = counts.filter(c => c > 0);            // populated (has IDs) = the badge rows
  const tailEmptyOk = counts.slice(pop.length).every(c => c === 0) && counts.slice(0, pop.length).every(c => c > 0);
  let ok = tailEmptyOk, note = tailEmptyOk ? `${pop.length} populated lead, ${counts.length - pop.length} empty last` : 'empties not clustered last';
  for (let i = 1; i < pop.length && ok; i++) {
    if (dir === 'asc' && pop[i - 1] > pop[i]) { ok = false; note = `asc break: ${pop[i - 1]} > ${pop[i]}`; }
    if (dir === 'desc' && pop[i - 1] < pop[i]) { ok = false; note = `desc break: ${pop[i - 1]} < ${pop[i]}`; }
  }
  return { ok, note, pop };
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, httpCredentials: GATE });
  const page = await ctx.newPage();
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  const eSel = 'input[name="Email"], #Email, input[type="email"]', pSel = 'input[name="Password"], #Password, input[type="password"]';
  await page.waitForSelector(eSel, { timeout: 30000 });
  await page.locator(eSel).first().fill(ADMIN.email);
  await page.locator(pSel).first().fill(ADMIN.password);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1200);

  let pass = 0, fail = 0;
  for (const col of ['asin', 'upc', 'gtin', 'ean']) {
    for (const [sort, dir] of [[col, 'asc'], [`${col}-za`, 'desc']]) {
      const items = await fetchAll(ctx.request, sort);
      const counts = items.map(c => countIds(c[col]));
      const r = checkMonotonic(counts, dir);
      const distinct = [...new Set(r.pop)];
      console.log(`[${r.ok ? 'PASS' : 'FAIL'}] ${sort} (${dir}) — ${r.note}; distinct counts in order: [${distinct.join(', ')}]${r.pop.includes(Math.max(...r.pop, 0)) ? ` ; max=${Math.max(...r.pop, 0)}` : ''}`);
      r.ok ? pass++ : fail++;
    }
  }
  console.log(`\n[def260] ${pass} PASS / ${fail} FAIL`);
  await browser.close();
  process.exit(fail ? 1 : 0);
})().catch(e => { console.error('ERR', e.message); process.exit(1); });
