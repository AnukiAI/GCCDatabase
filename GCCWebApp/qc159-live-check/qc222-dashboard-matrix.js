// Dashboard data-accuracy matrix.
// Outputs a single table comparing every on-screen number to its export
// counterpart, with semantic checks (does the row content match the label).
//
//   STAT CARDS  (12 badges, /Dashboard/DownloadStat)
//   TABLES      (5 breakdowns, /Dashboard/DownloadTable + visible row sums)
//   ACTIONS     (2 "Download Expiring Certs" buttons, /Export/DownloadReport)
//
// The actions deliberately use a ROLLING window from today, while the
// expiring-this-month / expiring-next-month BADGES use CALENDAR months — so
// the two definitions diverge by design. This matrix calls that out.
//
// Env: BASE_URL, APP_USER (admin), APP_PASS

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const TARGET = process.env.TARGET || (BASE.includes('gccwebapp') ? 'live' : 'local');
const BASIC = BASE.includes('gccwebapp')
  ? { username: 'gcc', password: 'Cert-Test-Access-2026!' }
  : undefined;
const APP_USER = process.env.APP_USER || 'admin';
const APP_PASS = process.env.APP_PASS || 'TestPass123!';

const STAMP = new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-');
const PREFIX = `qc222-${TARGET}-${STAMP}`;

// Canonical filters (post DEF-220 + DEF-223)
const DASHBOARD = new Set(['Client Approved - Paid', 'Republished']);
const EXPIRED = new Set(['Expired', 'Expired - No Annual Verification']);

function ymd(d) { return d.toISOString().slice(0, 10); }
function parseDate(s) {
  if (!s) return null;
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(s);
  if (!m) return null;
  return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]));
}
function inRange(d, lo, hi) { return d && d >= lo && d <= hi; }

function getWindows() {
  const t = new Date();
  const today = new Date(t.getFullYear(), t.getMonth(), t.getDate());
  return {
    today,
    startOfMonth: new Date(today.getFullYear(), today.getMonth(), 1),
    endOfMonth: new Date(today.getFullYear(), today.getMonth() + 1, 0),
    startOfLastMonth: new Date(today.getFullYear(), today.getMonth() - 1, 1),
    endOfLastMonth: new Date(today.getFullYear(), today.getMonth(), 0),
    startOfNextMonth: new Date(today.getFullYear(), today.getMonth() + 1, 1),
    endOfNextMonth: new Date(today.getFullYear(), today.getMonth() + 2, 0),
    startOfYear: new Date(today.getFullYear(), 0, 1),
    startOfLastYear: new Date(today.getFullYear() - 1, 0, 1),
    endOfLastYear: new Date(today.getFullYear() - 1, 11, 31),
    plus30: new Date(today.getFullYear(), today.getMonth(), today.getDate() + 30),
    plus3mo: new Date(today.getFullYear(), today.getMonth() + 3, today.getDate()),
  };
}
const W = getWindows();

function parseCsv(text) {
  const rows = [];
  let cur = '', row = [], inQ = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inQ) {
      if (c === '"' && text[i+1] === '"') { cur += '"'; i++; }
      else if (c === '"') inQ = false;
      else cur += c;
    } else {
      if (c === '"') inQ = true;
      else if (c === ',') { row.push(cur); cur = ''; }
      else if (c === '\r') {}
      else if (c === '\n') { row.push(cur); rows.push(row); row = []; cur = ''; }
      else cur += c;
    }
  }
  if (cur || row.length) { row.push(cur); rows.push(row); }
  return rows.filter(r => r.length > 1 || (r.length === 1 && r[0].trim()));
}
function parseDataRows(text) {
  const all = parseCsv(text);
  let h = -1;
  for (let i = 0; i < Math.min(all.length, 10); i++) {
    const f = (all[i][0] || '').trim();
    if (f && !/report|generated|total/i.test(f) && all[i].length > 1) { h = i; break; }
  }
  if (h < 0) return { headers: [], rows: [] };
  const headers = all[h];
  return {
    headers,
    rows: all.slice(h + 1).map(r => {
      const o = {}; headers.forEach((h2, i) => o[h2] = r[i] || ''); return o;
    }),
  };
}

// Semantic rules: returns ok+failures array per row.
const STAT_RULES = {
  'total-active-certs': r => [
    { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
  ],
  'certs-published-last-month': r => [
    { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
    { ok: inRange(parseDate(r['Effective Date']), W.startOfLastMonth, W.endOfLastMonth), desc: `EffectiveDate="${r['Effective Date']}"` },
  ],
  'certs-expiring-this-month': r => [
    { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
    { ok: inRange(parseDate(r['Expiration Date']), W.startOfMonth, W.endOfMonth), desc: `ExpDate="${r['Expiration Date']}"` },
  ],
  'certs-expired-last-month': r => [
    { ok: EXPIRED.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
    { ok: inRange(parseDate(r['Expiration Date']), W.startOfLastMonth, W.endOfLastMonth), desc: `ExpDate="${r['Expiration Date']}"` },
  ],
  'certs-expiring-next-month': r => [
    { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
    { ok: inRange(parseDate(r['Expiration Date']), W.startOfNextMonth, W.endOfNextMonth), desc: `ExpDate="${r['Expiration Date']}"` },
  ],
  'certs-published-this-year': r => [
    { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
    { ok: inRange(parseDate(r['Effective Date']), W.startOfYear, W.today), desc: `EffectiveDate="${r['Effective Date']}"` },
  ],
  'certs-published-last-year': r => [
    { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
    { ok: inRange(parseDate(r['Effective Date']), W.startOfLastYear, W.endOfLastYear), desc: `EffectiveDate="${r['Effective Date']}"` },
  ],
};

// Action-button rules (rolling windows)
const ACTION_RULES = {
  'expiring-30-days': r => [
    { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
    { ok: inRange(parseDate(r['Expiration Date']), W.today, W.plus30), desc: `ExpDate="${r['Expiration Date']}" (today..today+30d)` },
  ],
  'expiring-3-months': r => [
    { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
    { ok: inRange(parseDate(r['Expiration Date']), W.today, W.plus3mo), desc: `ExpDate="${r['Expiration Date']}" (today..today+3mo)` },
  ],
};

function padR(s, n) { s = String(s); return s.length >= n ? s.slice(0, n) : s + ' '.repeat(n - s.length); }
function padL(s, n) { s = String(s); return s.length >= n ? s.slice(0, n) : ' '.repeat(n - s.length) + s; }

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 1800 } });
  const page = await ctx.newPage();

  console.log(`\n=== Dashboard matrix — ${TARGET.toUpperCase()} (${BASE}) ===`);
  console.log(`Stamp: ${STAMP}    today=${ymd(W.today)}`);
  console.log(`Windows: thisMo=${ymd(W.startOfMonth)}..${ymd(W.endOfMonth)} | lastMo=${ymd(W.startOfLastMonth)}..${ymd(W.endOfLastMonth)} | nextMo=${ymd(W.startOfNextMonth)}..${ymd(W.endOfNextMonth)} | rolling30=${ymd(W.today)}..${ymd(W.plus30)} | rolling3mo=${ymd(W.today)}..${ymd(W.plus3mo)}\n`);

  try {
    await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(2000);
    if (/\/Account\/Login/i.test(page.url())) { console.log('login failed'); process.exit(2); }

    await page.goto(BASE + '/Dashboard', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);

    // ── STAT CARDS ────────────────────────────────────────────────────────
    const cards = await page.locator('.stat-card').evaluateAll(els =>
      els.map(el => ({
        title: (el.querySelector('.stat-title')?.innerText || '').trim(),
        value: (el.querySelector('.stat-value')?.innerText || '').trim(),
        statType: el.querySelector('.stat-download-icon')?.getAttribute('data-stat-type') || null,
      })).filter(c => c.statType)
    );

    const matrix = [];

    for (const card of cards) {
      const tile = Number((card.value || '0').replace(/[^\d.-]/g, ''));
      const url = `${BASE}/Dashboard/DownloadStat?type=${encodeURIComponent(card.statType)}`;
      const resp = await ctx.request.get(url);
      const status = resp.status();
      let rowsLen = null, semFail = 0, semTotal = 0, semDetail = '';
      if (status === 200) {
        const text = await resp.text();
        fs.writeFileSync(path.join(OUT, `${PREFIX}-stat-${card.statType}.csv`), text);
        const { rows } = parseDataRows(text);
        rowsLen = rows.length;
        const rule = STAT_RULES[card.statType];
        if (rule) {
          semTotal = rows.length;
          const bad = rows.map(r => rule(r).filter(c => !c.ok)).filter(f => f.length);
          semFail = bad.length;
          if (semFail > 0) semDetail = bad.slice(0, 3).map(b => b.map(x => x.desc).join('; ')).join(' | ');
        }
      }
      matrix.push({
        kind: 'STAT', name: card.statType, title: card.title,
        onScreen: tile, export: rowsLen,
        match: status === 200 && tile === rowsLen,
        semFail, semTotal, semDetail, http: status,
      });
    }

    // ── TABLES ────────────────────────────────────────────────────────────
    const tableSections = await page.locator('.table-section').evaluateAll(els =>
      els.map(el => {
        const title = (el.querySelector('.table-title')?.innerText || '').trim();
        const tableType = el.querySelector('.table-download-icon')?.getAttribute('data-table-type') || null;
        const tfootText = (el.querySelector('tfoot td:last-child')?.innerText || '').replace(/,/g, '');
        const rows = Array.from(el.querySelectorAll('tbody tr')).map(tr =>
          Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim()));
        return { title, tableType, tfootText, rows };
      }).filter(t => t.tableType)
    );

    for (const tbl of tableSections) {
      const dataRows = tbl.rows.filter(r => r.length >= 2 && /^\d/.test(r[1].replace(/,/g, '')));
      const visibleSum = dataRows.reduce((s, r) => s + Number(r[1].replace(/,/g, '')), 0);
      const visibleRowCount = dataRows.length;
      const url = `${BASE}/Dashboard/DownloadTable?type=${encodeURIComponent(tbl.tableType)}`;
      const resp = await ctx.request.get(url);
      const status = resp.status();
      let exportRows = 0, semFail = 0;
      if (status === 200) {
        const text = await resp.text();
        fs.writeFileSync(path.join(OUT, `${PREFIX}-table-${tbl.tableType}.csv`), text);
        const { rows } = parseDataRows(text);
        exportRows = rows.length;
        if (tbl.tableType !== 'cert-status') {
          semFail = rows.filter(r => !DASHBOARD.has((r.Status || '').trim())).length;
        }
      }
      // For top-N tables, visible_sum < export is expected (visible is capped); we
      // still call out the gap for transparency.
      matrix.push({
        kind: 'TABLE', name: tbl.tableType, title: tbl.title,
        onScreen: visibleSum, export: exportRows,
        rowsShown: visibleRowCount,
        match: status === 200 && (visibleSum === exportRows),
        semFail, semTotal: exportRows, http: status,
      });
    }

    // ── ACTION BUTTONS ────────────────────────────────────────────────────
    const actions = [
      { name: 'expiring-30-days', title: 'Download Expiring Certs Report (Next 30 Days)' },
      { name: 'expiring-3-months', title: 'Download Expiring Certs Report (Next 3 Months)' },
    ];
    for (const a of actions) {
      const url = `${BASE}/Export/DownloadReport?type=${encodeURIComponent(a.name)}&format=csv`;
      const resp = await ctx.request.get(url);
      const status = resp.status();
      let exportRows = 0, semFail = 0, semTotal = 0;
      if (status === 200) {
        const text = await resp.text();
        fs.writeFileSync(path.join(OUT, `${PREFIX}-action-${a.name}.csv`), text);
        const { rows } = parseDataRows(text);
        exportRows = rows.length;
        const rule = ACTION_RULES[a.name];
        if (rule) {
          semTotal = rows.length;
          semFail = rows.filter(r => rule(r).some(c => !c.ok)).length;
        }
      }
      matrix.push({
        kind: 'ACTION', name: a.name, title: a.title,
        onScreen: '(no badge)', export: exportRows,
        match: null, // no on-screen counter to compare
        semFail, semTotal, http: status,
      });
    }

    // ── OUTPUT MATRIX ─────────────────────────────────────────────────────
    console.log(padR('KIND', 7) + padR('NAME', 42) + padL('ON-SCREEN', 11) + padL('EXPORT', 8) + '  MATCH  SEMANTIC');
    console.log('─'.repeat(95));
    for (const m of matrix) {
      const matchTag = m.match === null ? '  —  ' : (m.match ? '  ✓  ' : '  ✗  ');
      const sem = m.semTotal > 0
        ? (m.semFail === 0 ? `OK (${m.semTotal} rows)` : `${m.semFail}/${m.semTotal} ROW FAIL`)
        : '-';
      console.log(
        padR(m.kind, 7) +
        padR(m.name, 42) +
        padL(m.onScreen, 11) +
        padL(m.export, 8) +
        matchTag + sem
      );
      if (m.semDetail) console.log('         ' + m.semDetail.slice(0, 80));
    }

    // ── DIAGNOSTIC: cross-tile sanity ─────────────────────────────────────
    const find = n => matrix.find(m => m.name === n);
    console.log('\n── Cross-tile sanity ──────────────────────────────────────────');
    const expM = find('certs-expiring-this-month')?.onScreen ?? '?';
    const expN = find('certs-expiring-next-month')?.onScreen ?? '?';
    const exp30 = find('expiring-30-days')?.export ?? '?';
    const exp3 = find('expiring-3-months')?.export ?? '?';
    console.log(`  Expiring This Month BADGE  : ${expM}   (calendar [${ymd(W.startOfMonth)}..${ymd(W.endOfMonth)}])`);
    console.log(`  Expiring Next Month BADGE  : ${expN}   (calendar [${ymd(W.startOfNextMonth)}..${ymd(W.endOfNextMonth)}])`);
    console.log(`  Next 30 Days ACTION export : ${exp30}   (rolling  [${ymd(W.today)}..${ymd(W.plus30)}])`);
    console.log(`  Next 3 Months ACTION export: ${exp3}   (rolling  [${ymd(W.today)}..${ymd(W.plus3mo)}])`);
    console.log(`  NOTE: Action buttons use a ROLLING window from today; badges use CALENDAR months.`);
    console.log(`        So Next3mo export will NOT equal ThisMonth+NextMonth+MonthAfter badges.`);
    console.log(`        Also: action buttons require ExpirationDate >= today (excludes already-past dates`);
    console.log(`        in the current month that the "Expiring This Month" badge includes).`);

    fs.writeFileSync(path.join(OUT, `${PREFIX}-matrix.json`), JSON.stringify(matrix, null, 2));
    console.log(`\nMatrix JSON saved: ${PREFIX}-matrix.json`);

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
  } finally {
    await browser.close();
  }
})();
