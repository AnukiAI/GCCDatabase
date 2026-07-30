// Discovery harness for QC intake (2026-07-16):
//   "Sorting Certificates by Oldest Effective Date made the screen go white, but the
//    results were still there — clicking the blank area opened a result; a refresh fixed it."
//
// Symptom (DOM present + clickable, visually blank, self-heals on refresh) => render/paint/
// layout race, NOT the sort logic. First in-browser pass (mobile-locked, bg-throttled) could
// not reproduce. This removes the blind spots:
//   - DESKTOP width (1440x900) => exercises the sticky-header table + syncScrollSizes
//   - CDP CPU throttling => widens race windows
//   - waits on the REAL API response + a stable-DOM poll (no fragile fixed sleeps)
//   - asserts the view is actually active (empty grid => INVALID, never a false PASS)
//   - ALWAYS screenshots post-sort states — the only detector for a pure paint-flush bug
//
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/cert-sort-whitescreen-discovery.js
// Env: BASE_URL (default localhost:5101), CPU_THROTTLE (default 4), HEADLESS (default 1)

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const CPU_THROTTLE = parseFloat(process.env.CPU_THROTTLE || '4');
const HEADLESS = process.env.HEADLESS !== '0';
const VIEW_URL = '/Certificate/CertificateTileViewInternal';
const CRED = { email: 'jane.analyst@anukiconsulting.com', password: 'TestPass123!' };
const API = '/api/certificate-tiles-internal';

const OUT = path.join(__dirname, '_results', 'sort-whitescreen');
const SHOTS = path.join(OUT, 'screenshots');
for (const d of [OUT, SHOTS]) if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true });

const findings = [];
const anomalies = [];
const shotsTaken = [];
function record(scenario, id, status, detail) {
  findings.push({ scenario, id, status, detail });
  const tag = status === 'FAIL' ? 'FAIL' : status === 'INVALID' ? 'INVALID' : status === 'WARN' ? 'WARN' : 'PASS';
  console.log(`  [${tag}] ${scenario} :: ${id}${detail ? ' — ' + detail : ''}`);
  if (status === 'FAIL' || status === 'INVALID') anomalies.push({ scenario, id, status, detail });
}

async function login(page) {
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('#Email').fill(CRED.email);
  await page.locator('#Password').fill(CRED.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(700);
}

async function setView(page, view) { // 'tile' | 'list'
  await page.evaluate((v) => {
    const b = document.querySelector(`button.toggle-btn[data-view="${v}"]`);
    if (b) b.click();
  }, view);
  // Wait for the target surface to be the active, populated one.
  await page.waitForFunction((v) => {
    if (v === 'list') {
      const g = document.getElementById('gridBody');
      return g && g.children.length > 0 && getComputedStyle(g).display !== 'none';
    } else {
      const g = document.getElementById('certificatesGrid');
      return g && g.children.length > 0 && getComputedStyle(g).display !== 'none';
    }
  }, view, { timeout: 15000 }).catch(() => {});
}

// Apply a sort by clicking the real custom-dropdown option, then wait for the API response
// AND a stable DOM (child count unchanged across two reads) + a rAF for syncScrollSizes.
async function applySort(page, value, { timeout = 20000 } = {}) {
  const respP = page.waitForResponse(r => r.url().includes(API), { timeout }).catch(() => null);
  await page.evaluate((v) => {
    const o = document.querySelector(`[data-sort="${v}"]`);
    if (o) o.click();
  }, value);
  await respP;
  let prev = -1, stable = 0;
  for (let i = 0; i < 50 && stable < 2; i++) {
    const n = await page.evaluate(() => {
      const l = document.getElementById('gridBody');
      const t = document.getElementById('certificatesGrid');
      const active = (l && getComputedStyle(l).display !== 'none') ? l : t;
      return active ? active.children.length : 0;
    });
    if (n > 0 && n === prev) stable++; else { stable = 0; prev = n; }
    await page.waitForTimeout(150);
  }
  await page.waitForTimeout(220); // let a rAF (syncScrollSizes) run
}

// JS-visible view of the active surface. `view` tells us which container is authoritative.
async function snapshot(page, view) {
  return page.evaluate((v) => {
    const grid = document.getElementById('certificatesGrid');
    const gb = document.getElementById('gridBody');
    const active = v === 'list' ? gb : grid;
    const header = document.getElementById('stickyHeaderRow');
    const rows = active ? active.children.length : -1;
    let firstRect = null, centerHit = 'n/a', topmostIsItem = null;
    if (active && active.children.length) {
      const item = active.children[0];
      const r = item.getBoundingClientRect();
      firstRect = { top: Math.round(r.top), h: Math.round(r.height), w: Math.round(r.width) };
      const cx = Math.round(r.left + Math.min(r.width, window.innerWidth) / 2);
      const cy = Math.round(Math.min(Math.max(r.top + r.height / 2, 5), window.innerHeight - 5));
      const el = document.elementFromPoint(cx, cy);
      centerHit = el ? (el.tagName + '.' + String(el.className || '').split(' ')[0]).slice(0, 34) : 'none';
      topmostIsItem = !!(el && (el === item || item.contains(el)));
    }
    const cs = active ? getComputedStyle(active) : null;
    return {
      view: v, rows,
      activeH: active ? active.offsetHeight : -1,
      opacity: cs ? cs.opacity : '?', vis: cs ? cs.visibility : '?', disp: cs ? cs.display : '?',
      headerH: header ? header.offsetHeight : -1,
      scrollY: Math.round(window.scrollY), docH: document.documentElement.scrollHeight, viewH: window.innerHeight,
      firstRect, centerHit, topmostIsItem,
    };
  }, view);
}

// Blank-but-populated: rows exist in DOM but they are not visible where the user looks.
function judgeBlank(s) {
  if (s.rows <= 0) return { verdict: 'INVALID', reason: `active surface has ${s.rows} rows — view not populated, test invalid` };
  const r = [];
  if (s.activeH < 40) r.push(`container height ${s.activeH}px`);
  if (s.opacity !== '' && s.opacity !== '?' && parseFloat(s.opacity) < 0.5) r.push(`opacity ${s.opacity}`);
  if (s.vis === 'hidden') r.push('visibility:hidden');
  if (s.disp === 'none') r.push('display:none');
  if (s.firstRect && s.firstRect.h < 4) r.push(`first item height ${s.firstRect.h}px`);
  // first item is within the viewport vertically but something else is painted on top of it
  if (s.firstRect && s.firstRect.top >= 0 && s.firstRect.top < s.viewH - 20 && s.topmostIsItem === false) {
    r.push(`first item covered (topmost=${s.centerHit})`);
  }
  if (s.scrollY > s.docH - 30 && s.docH > s.viewH) r.push(`scroll stranded (scrollY ${s.scrollY} / docH ${s.docH})`);
  return { verdict: r.length ? 'FAIL' : 'PASS', reason: r.join('; ') };
}

async function shot(page, name) {
  const rel = path.join('screenshots', name + '.png');
  try { await page.screenshot({ path: path.join(OUT, rel), fullPage: false }); shotsTaken.push(rel); return rel; }
  catch { return null; }
}

// ---- Scenarios -------------------------------------------------------------

async function scenarioResortLoop(page, view) {
  console.log(`\n[Scenario A/${view}] Repeated re-sort in ${view} view (CPU ${CPU_THROTTLE}x)`);
  await setView(page, view);
  const seq = ['oldest-effective', 'newest', 'oldest-effective', 'cert-type',
    'oldest-effective', 'company-az', 'oldest-effective', 'newest', 'oldest-effective'];
  let sawInvalid = false;
  for (let i = 0; i < seq.length; i++) {
    await applySort(page, seq[i]);
    const s = await snapshot(page, view);
    const j = judgeBlank(s);
    const id = `${view}#${i + 1}(${seq[i]})`;
    if (j.verdict !== 'PASS') {
      const img = await shot(page, `A-${view}-${j.verdict}-${i + 1}-${seq[i]}`);
      record(`A.resort.${view}`, id, j.verdict, `${j.reason} | rows=${s.rows} H=${s.activeH} shot=${img}`);
      if (j.verdict === 'INVALID') sawInvalid = true;
    } else {
      record(`A.resort.${view}`, id, 'PASS', `rows=${s.rows} H=${s.activeH} centerHit=${s.centerHit}`);
    }
  }
  // Always capture the final rendered state for visual review.
  await shot(page, `A-${view}-final-oldest`);
  return sawInvalid;
}

async function scenarioDeepScrollThenSort(page, view) {
  console.log(`\n[Scenario B/${view}] Deep-scroll (load all) then Oldest sort — height-collapse / stranded-scroll`);
  await setView(page, view);
  await applySort(page, 'newest');
  for (let i = 0; i < 8; i++) {
    await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
    await page.waitForTimeout(450);
  }
  const before = await snapshot(page, view);
  await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
  await page.waitForTimeout(250);
  const scrollYBefore = (await snapshot(page, view)).scrollY;
  await applySort(page, 'oldest-effective');
  const after = await snapshot(page, view);
  const j = judgeBlank(after);
  const img = await shot(page, `B-${view}-after-deepscroll-sort`);
  record(`B.deepscroll.${view}`, 'post-sort', j.verdict,
    `${j.reason ? j.reason + ' | ' : ''}scrollY ${scrollYBefore}->${after.scrollY}, docH ${before.docH}->${after.docH}, rows ${before.rows}->${after.rows}, centerHit=${after.centerHit}, shot=${img}`);
}

async function scenarioBackgroundSort(context, page, view) {
  console.log(`\n[Scenario C/${view}] Sort while tab hidden then return — reveal/animation stall`);
  await setView(page, view);
  await applySort(page, 'newest');
  const other = await context.newPage();
  await other.goto('about:blank');
  await other.bringToFront();       // `page` now document.hidden = true
  await page.waitForTimeout(150);
  // Fire the sort while hidden — don't await API (throttled); we simulate the user walking away.
  await page.evaluate(() => { const o = document.querySelector('[data-sort="oldest-effective"]'); if (o) o.click(); });
  await page.waitForTimeout(1800);
  await page.bringToFront();         // return — refresh-equivalent is NOT done; test if reveal happened on its own
  await page.waitForTimeout(400);
  const s = await snapshot(page, view);
  const j = judgeBlank(s);
  const img = await shot(page, `C-${view}-after-return`);
  record(`C.background.${view}`, 'visible-on-return', j.verdict,
    `${j.reason ? j.reason + ' | ' : ''}rows=${s.rows} H=${s.activeH} centerHit=${s.centerHit} shot=${img}`);
  await other.close();
  await page.bringToFront();
}

// Verifies the dropped-concurrent-sort fix: fire two DIFFERENT sorts back-to-back and confirm
// the rendered data reflects the SECOND (last) one — not the first. Pre-fix, the isLoading guard
// dropped the 2nd request so the data stayed on the 1st sort while the label/URL showed the 2nd.
async function scenarioRapidSupersede(page) {
  console.log(`\n[Scenario D/list] Rapid double-sort — last sort must win (dropped-sort fix)`);
  await setView(page, 'list');
  await applySort(page, 'cert-number-oldest'); // baseline (sort A)
  const fetches = [];
  const onReq = (r) => { if (r.url().includes(API)) fetches.push(r.url()); };
  page.on('request', onReq);
  // Fire A then B (B = oldest-effective) with almost no gap so B lands while A is still loading.
  await page.evaluate(() => {
    document.querySelector('[data-sort="cert-number-newest"]')?.click(); // A'
    document.querySelector('[data-sort="oldest-effective"]')?.click();   // B (last — must win)
  });
  await page.waitForTimeout(2500);
  page.off('request', onReq);
  // Ground truth: what does the API return for B, page 1?
  const expected = await page.evaluate(async (api) => {
    const r = await fetch(`${api}?sortBy=oldest-effective&page=1&pageSize=20`);
    const j = await r.json();
    return (j.certificates || []).slice(0, 5).map(c => c.number || c.certificateNumber);
  }, API);
  // What is actually rendered (first cert # cells)?
  const rendered = await page.evaluate(() => {
    const cells = [...document.querySelectorAll('#gridBody .tc-cert')].slice(0, 5).map(c => c.textContent.trim());
    return cells;
  });
  const match = JSON.stringify(expected) === JSON.stringify(rendered);
  const img = await shot(page, 'D-list-rapid-supersede');
  record('D.rapid-supersede', 'last-sort-wins', match ? 'PASS' : 'FAIL',
    `fetches=${fetches.length} | expected(B)=${JSON.stringify(expected)} rendered=${JSON.stringify(rendered)} shot=${img}`);
}

(async () => {
  if ((BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) && process.env.SMOKE_ALLOW_REMOTE !== '1') {
    console.error(`REFUSING to run against ${BASE}`); process.exit(2);
  }
  console.log(`\n[discovery] cert sort white-screen — ${BASE} — CPU ${CPU_THROTTLE}x — desktop 1440x900\n`);
  const browser = await chromium.launch({ headless: HEADLESS });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();
  const consoleErrors = [], pageErrors = [];
  page.on('console', (m) => { if (m.type() === 'error' && !/favicon|ResizeObserver loop/i.test(m.text())) consoleErrors.push(m.text()); });
  page.on('pageerror', (e) => pageErrors.push(e.message));

  try {
    await login(page);
    await page.goto(BASE + VIEW_URL, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForResponse(r => r.url().includes(API), { timeout: 30000 }).catch(() => null);
    await page.waitForTimeout(600);
    if (CPU_THROTTLE > 1) {
      const client = await context.newCDPSession(page);
      await client.send('Emulation.setCPUThrottlingRate', { rate: CPU_THROTTLE });
    }

    await scenarioResortLoop(page, 'list');
    await scenarioResortLoop(page, 'tile');
    await scenarioDeepScrollThenSort(page, 'list');
    await scenarioBackgroundSort(context, page, 'list');
    await scenarioRapidSupersede(page);
  } catch (e) {
    record('harness', 'run', 'FAIL', 'exception: ' + e.message + '\n' + (e.stack || '').split('\n').slice(0, 3).join(' | '));
  }

  if (consoleErrors.length) record('console', 'errors', 'WARN', consoleErrors.slice(0, 6).join(' || '));
  if (pageErrors.length) record('pageerror', 'errors', 'FAIL', pageErrors.slice(0, 6).join(' || '));

  const totals = {
    pass: findings.filter(f => f.status === 'PASS').length,
    warn: findings.filter(f => f.status === 'WARN').length,
    fail: findings.filter(f => f.status === 'FAIL').length,
    invalid: findings.filter(f => f.status === 'INVALID').length,
  };
  fs.writeFileSync(path.join(OUT, 'result.json'),
    JSON.stringify({ base: BASE, cpuThrottle: CPU_THROTTLE, when: new Date().toISOString(), totals, anomalies, shotsTaken, findings }, null, 2));
  console.log(`\n[discovery] done — PASS ${totals.pass} / WARN ${totals.warn} / FAIL ${totals.fail} / INVALID ${totals.invalid}`);
  console.log(`[discovery] screenshots: ${shotsTaken.length} in ${SHOTS}`);
  if (totals.fail > 0) console.log(`[discovery] >>> BLANK-STATE anomaly detected — inspect screenshots + anomalies <<<`);
  if (totals.invalid > 0) console.log(`[discovery] >>> INVALID samples — a view failed to populate; fix before trusting results <<<`);
  await browser.close();
  process.exit(0);
})();
