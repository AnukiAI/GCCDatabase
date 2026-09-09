// READ-ONLY search-latency probe — gccwebapp.com (test site).
//
// The client reported on 2026-09-03: "when I was typing search terms in search field it was
// slower to return results than it was last time I tested. Taking 10-12 seconds to come back
// with a record."
//
// This measures the SIGNED-IN internal tile view, which is what an analyst actually uses, not
// the anonymous public one — the two hit the same controller but see 5,924 vs ~1,100 rows, so
// timing the public path proves nothing about the complaint.
//
// Every request is a GET. Nothing is created, edited or deleted.
//
//   node Scripts/smoke-prod/2026-09-03-search-latency-probe.js
//   BASE_URL=http://localhost:5101 node ... (skips the site gate automatically)

const { chromium, request } = require('playwright');
const { detectGateMode, passGate } = require('./_lib/test-env-gate');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
// `admin`'s password was reset on TEST on 2026-08-19 and only Shawn knows it; a failed login
// re-renders the login page as HTTP 200, which reads exactly like a slow/broken API.
const APP_USER = process.env.ADMIN_USER || 'admin.uat';
const APP_PASS = process.env.ADMIN_PASS || 'TestPass123!';

// Includes a term that matches NOTHING and an exact certificate number on purpose: the observed
// signature is that the FEWER rows a term matches the SLOWER it gets, which is what a full scan
// with a leading wildcard looks like. A term list of only common words would hide the worst case.
const TERMS = ['a', 'recycled', 'gaf', 'sika', 'mapei', 'membrane', '26-0166', '26-0636', 'zzzznomatch'];
const RUNS = 3;

const ms = (n) => String(Math.round(n)).padStart(6) + 'ms';

(async () => {
  // The test site gates on a COOKIE, not Basic Auth: sending httpCredentials returns HTTP 200
  // and a ~2.5KB gate page for every URL, which a timing probe would happily measure as "the
  // application" (DEF-341). Pass the gate properly and carry its storageState.
  const mode = await detectGateMode(request, BASE);
  const gate = mode.mode === 'cookie' ? await passGate(request, BASE) : { ok: true, state: undefined };
  console.log(`gate: ${mode.mode} (${mode.detail})` + (gate.ok ? '' : ` — FAILED: ${gate.detail}`));
  if (!gate.ok) process.exit(1);

  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    storageState: gate.state,
    viewport: { width: 1440, height: 900 },
  });
  const page = await ctx.newPage();

  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill(APP_USER);
  await page.locator('#Password').fill(APP_PASS);
  await Promise.all([page.waitForLoadState('networkidle'), page.locator('button[type=submit]').click()]);

  // Confirm we are actually signed in. Posting a bad password returns HTTP 200 on the login
  // page, so "the API is slow" and "I am anonymous" look identical without this.
  const who = await page.evaluate(() => document.body.innerText.slice(0, 400));
  const signedIn = !page.url().includes('/Account/Login');
  console.log(`login: ${signedIn ? 'OK as ' + APP_USER : 'FAILED — still on /Account/Login'}`);
  if (!signedIn) { console.log(who); await browser.close(); process.exit(1); }

  async function timeGet(url) {
    const t0 = Date.now();
    const r = await page.request.get(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' }, timeout: 120000 });
    const body = await r.text();
    return { ms: Date.now() - t0, status: r.status(), bytes: body.length };
  }

  const endpoints = [
    ['autocomplete', (t) => `${BASE}/api/certificate-tiles/autocomplete?term=${encodeURIComponent(t)}`],
    // The PUBLIC tile endpoint. Sees ~1,100 rows, so it is the optimistic case.
    ['tiles (public shape)', (t) => `${BASE}/api/certificate-tiles?searchTerm=${encodeURIComponent(t)}&page=1&pageSize=20`],
    // The endpoint an ANALYST actually types into — a different controller over all 5,924 rows.
    // Timing the public one instead is how a 9-second query reads as "under a second".
    ['tiles-internal', (t) => `${BASE}/api/certificate-tiles-internal?search=${encodeURIComponent(t)}&page=1&pageSize=20`],
  ];

  const results = [];
  for (const [label, build] of endpoints) {
    console.log(`\n=== ${label} (signed in as ${APP_USER}) ===`);
    console.log('  term          median     min     max   status  bytes');
    for (const term of TERMS) {
      const samples = [];
      let status = 0, bytes = 0;
      for (let i = 0; i < RUNS; i++) {
        const s = await timeGet(build(term));
        samples.push(s.ms); status = s.status; bytes = s.bytes;
      }
      samples.sort((a, b) => a - b);
      const median = samples[Math.floor(samples.length / 2)];
      results.push({ label, term, median, min: samples[0], max: samples[samples.length - 1], status });
      console.log(`  ${term.padEnd(12)}${ms(median)}${ms(samples[0])}${ms(samples[samples.length - 1])}    ${status}  ${bytes}`);
    }
  }

  // The page the client is actually typing into, end to end.
  console.log('\n=== full page load ===');
  for (const [name, url] of [
    ['TileViewInternal', `${BASE}/Certificate/CertificateTileViewInternal`],
    ['Certificate index', `${BASE}/Certificate`],
  ]) {
    const t0 = Date.now();
    const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 90000 });
    console.log(`  ${name.padEnd(20)} ${ms(Date.now() - t0)}  status ${resp && resp.status()}`);
  }

  const slow = results.filter((r) => r.median >= 5000);
  console.log(`\n${slow.length} of ${results.length} probes took 5s or more` +
    (slow.length ? ': ' + slow.map((s) => `${s.label}/${s.term}=${s.median}ms`).join(', ') : '.'));
  const worst = results.reduce((a, b) => (b.median > a.median ? b : a));
  console.log(`worst: ${worst.label} "${worst.term}" ${worst.median}ms (max sample ${worst.max}ms)`);

  await browser.close();
})().catch((e) => { console.error('FATAL:', e.message); process.exit(1); });
