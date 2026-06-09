// DEF-259 focused probe — proves numeric ASIN values sort NATURALLY (1,2,...,10,...,1000)
// on the internal Cert List, not lexically (1,10,1000,2,9). Run against localhost AFTER
// seeding the QC157TEST numeric ASINs. Read-only (no data changes).
const { chromium } = require('playwright');
const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const ADMIN = { email: process.env.SMOKE_USER || 'admin', password: process.env.SMOKE_PASS || 'TestPass123!' };
// Test-env HTTP Basic gate — only for non-localhost (e.g. gccwebapp.com). Read-only probe.
const IS_REMOTE = !/localhost|127\.0\.0\.1/.test(BASE);
const GATE = IS_REMOTE
  ? { username: process.env.SMOKE_REMOTE_USER || 'gcc', password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' }
  : undefined;

async function fetchAll(req, sortBy) {
  const all = []; let page = 1;
  for (;;) {
    const r = await req.get(`${BASE}/api/certificate-tiles-internal?sortBy=${encodeURIComponent(sortBy)}&page=${page}&pageSize=100`);
    if (r.status() !== 200) throw new Error(`API ${sortBy} -> HTTP ${r.status()}`);
    const j = await r.json();
    const arr = j.certificates || [];
    all.push(...arr);
    if (!j.hasMore || arr.length === 0 || page > 50) break;
    page++;
  }
  return all;
}
// Only the seeded numeric cells (purely digits, length<=4) so the alphanumeric B0… noise is filtered out.
const isNumericCell = (s) => /^[\d,\s]+$/.test(String(s || '').trim()) && String(s).trim().length > 0;

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, httpCredentials: GATE });
  const page = await ctx.newPage();
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  console.log('login page url:', page.url(), '| title:', await page.title());
  const emailSel = 'input[name="Email"], #Email, input[type="email"]';
  const passSel = 'input[name="Password"], #Password, input[type="password"]';
  await page.waitForSelector(emailSel, { timeout: 30000 });
  await page.locator(emailSel).first().fill(ADMIN.email);
  await page.locator(passSel).first().fill(ADMIN.password);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1200);
  console.log('after-login url:', page.url());

  for (const sort of ['asin', 'asin-za']) {
    const items = await fetchAll(ctx.request, sort);
    const numeric = items.map(c => String(c.asin || '').trim()).filter(isNumericCell);
    console.log(`\n=== sortBy=${sort} — numeric ASIN cells in returned order ===`);
    console.log(numeric.join('  |  '));
    // position of the all-blank vs the big 1000 value relative to single digits
    const lead = numeric.map(s => s.split(',')[0].trim());
    console.log(`leading-value sequence: [${lead.join(', ')}]`);
  }
  await browser.close();
})().catch(e => { console.error('ERR', e.message); process.exit(1); });
