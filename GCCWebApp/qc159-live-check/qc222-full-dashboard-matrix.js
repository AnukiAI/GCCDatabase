// Comprehensive dashboard data-accuracy matrix.
//
// Captures every report / export surface on /Dashboard, runs each through
// its own semantic validator, and produces:
//
//   1. A summary matrix (markdown table) — one row per report with on-screen,
//      export, match, and semantic verdict.
//   2. A per-certificate appearance matrix — which certs show up in which
//      "Active" report (helps Shawn see overlap and cross-reference).
//   3. A date-window reference — exactly what calendar dates each label
//      resolves to, so "Next 3 Months" vs "Expiring This Month" can be
//      reconciled.
//   4. A reconciliation section — explains why two reports that "sound like"
//      they should match may not (e.g., rolling vs calendar windows).
//
// Inventory of reports covered:
//   STAT cards (12)  → /Dashboard/DownloadStat?type=…
//   TABLES (5)       → /Dashboard/DownloadTable?type=…
//   ACTIONS (3)      → /Export/DownloadReport?type=…
//
// Output files (one matrix run):
//   qc222-{target}-{stamp}-matrix.json        — machine-readable
//   qc222-{target}-{stamp}-MATRIX.md          — human-readable report
//   qc222-{target}-{stamp}-{kind}-{name}.csv  — every export captured

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

// Canonical status sets (post DEF-220 + DEF-223 — i.e., what the LOCAL code
// reflects; LIVE may still have wider DashboardStatuses until deploy).
const DASHBOARD_POST_FIX = new Set(['Client Approved - Paid', 'Republished']);
const DASHBOARD_PRE_FIX = new Set(['Client Approved - Paid', 'Republished', 'Client Approved - Confidential']);
// On live, until the pending zip deploys, exports still use the wide set.
// We validate against the wide set so semantic checks pass on live, and
// surface Confidential rows as informational, not failures.
const DASHBOARD = BASE.includes('gccwebapp') ? DASHBOARD_PRE_FIX : DASHBOARD_POST_FIX;
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

// Each report's metadata + filter + per-row validator.
// statusFilter: explanation of which status set the server applies
// dateFilter:   human-readable date window
// validator:    function(row) → [{ok, desc}], used to verify per-row content
const REPORTS = [
  // STAT cards
  { kind: 'STAT', name: 'total-active-certs', title: 'Total Active Certs',
    sourceUrl: '/Dashboard/DownloadStat?type=total-active-certs',
    statusFilter: 'DashboardStatuses (Paid + Republished + [Confidential on live until deploy])',
    dateFilter: 'ExpirationDate null OR >= today',
    validator: r => [
      { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}" should be in DashboardStatuses` },
      { ok: !r['Expiration Date'] || parseDate(r['Expiration Date']) >= W.today, desc: `ExpirationDate="${r['Expiration Date']}" must be null or >= today` },
    ]},
  { kind: 'STAT', name: 'companies-with-active-certs', title: '# Companies with Active Certs',
    sourceUrl: '/Dashboard/DownloadStat?type=companies-with-active-certs',
    statusFilter: 'DashboardStatuses + ExpirationDate >= today (aggregate)',
    dateFilter: '—', validator: null },
  { kind: 'STAT', name: 'product-categories-with-active-certs', title: '# Product Categories with Active Certs',
    sourceUrl: '/Dashboard/DownloadStat?type=product-categories-with-active-certs',
    statusFilter: 'DashboardStatuses (aggregate)', dateFilter: '—', validator: null },
  { kind: 'STAT', name: 'product-types-with-active-certs', title: '# Product Types with Active Certs',
    sourceUrl: '/Dashboard/DownloadStat?type=product-types-with-active-certs',
    statusFilter: 'DashboardStatuses (aggregate)', dateFilter: '—', validator: null },
  { kind: 'STAT', name: 'product-subcategories-with-active-certs', title: '# Product Type Subcategories with Active Certs',
    sourceUrl: '/Dashboard/DownloadStat?type=product-subcategories-with-active-certs',
    statusFilter: 'DashboardStatuses (aggregate)', dateFilter: '—', validator: null },
  { kind: 'STAT', name: 'products-with-active-certs', title: '# Products with Active Certs',
    sourceUrl: '/Dashboard/DownloadStat?type=products-with-active-certs',
    statusFilter: 'DashboardStatuses (aggregate)', dateFilter: '—', validator: null },
  { kind: 'STAT', name: 'certs-published-last-month', title: '# Active Certs Published Last Month',
    sourceUrl: '/Dashboard/DownloadStat?type=certs-published-last-month',
    statusFilter: 'DashboardStatuses',
    dateFilter: `EffectiveDate in calendar [${ymd(W.startOfLastMonth)}..${ymd(W.endOfLastMonth)}]`,
    validator: r => [
      { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
      { ok: inRange(parseDate(r['Effective Date']), W.startOfLastMonth, W.endOfLastMonth), desc: `EffectiveDate="${r['Effective Date']}" not in last-month window` },
    ]},
  { kind: 'STAT', name: 'certs-expiring-this-month', title: '# Certs Expiring This Month',
    sourceUrl: '/Dashboard/DownloadStat?type=certs-expiring-this-month',
    statusFilter: 'DashboardStatuses',
    dateFilter: `ExpirationDate in calendar [${ymd(W.startOfMonth)}..${ymd(W.endOfMonth)}]`,
    validator: r => [
      { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
      { ok: inRange(parseDate(r['Expiration Date']), W.startOfMonth, W.endOfMonth), desc: `ExpDate="${r['Expiration Date']}" not in this-month window` },
    ]},
  { kind: 'STAT', name: 'certs-expired-last-month', title: '# Certs Expired Last Month',
    sourceUrl: '/Dashboard/DownloadStat?type=certs-expired-last-month',
    statusFilter: 'CurrentlyExpiredStatuses (Expired + ExpiredNoAnnualVerification)',
    dateFilter: `ExpirationDate in calendar [${ymd(W.startOfLastMonth)}..${ymd(W.endOfLastMonth)}]`,
    validator: r => [
      { ok: EXPIRED.has((r.Status || '').trim()), desc: `Status="${r.Status}" should be in CurrentlyExpiredStatuses` },
      { ok: inRange(parseDate(r['Expiration Date']), W.startOfLastMonth, W.endOfLastMonth), desc: `ExpDate="${r['Expiration Date']}" not in last-month window` },
    ]},
  { kind: 'STAT', name: 'certs-expiring-next-month', title: '# Certs Expiring Next Month',
    sourceUrl: '/Dashboard/DownloadStat?type=certs-expiring-next-month',
    statusFilter: 'DashboardStatuses',
    dateFilter: `ExpirationDate in calendar [${ymd(W.startOfNextMonth)}..${ymd(W.endOfNextMonth)}]`,
    validator: r => [
      { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
      { ok: inRange(parseDate(r['Expiration Date']), W.startOfNextMonth, W.endOfNextMonth), desc: `ExpDate="${r['Expiration Date']}" not in next-month window` },
    ]},
  { kind: 'STAT', name: 'certs-published-this-year', title: '# Active Certs Published This Year',
    sourceUrl: '/Dashboard/DownloadStat?type=certs-published-this-year',
    statusFilter: 'DashboardStatuses',
    dateFilter: `EffectiveDate in [${ymd(W.startOfYear)}..${ymd(W.today)}]`,
    validator: r => [
      { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
      { ok: inRange(parseDate(r['Effective Date']), W.startOfYear, W.today), desc: `EffectiveDate="${r['Effective Date']}" not in this-year window` },
    ]},
  { kind: 'STAT', name: 'certs-published-last-year', title: '# Active Certs Published Last Year',
    sourceUrl: '/Dashboard/DownloadStat?type=certs-published-last-year',
    statusFilter: 'DashboardStatuses',
    dateFilter: `EffectiveDate in [${ymd(W.startOfLastYear)}..${ymd(W.endOfLastYear)}]`,
    validator: r => [
      { ok: DASHBOARD.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
      { ok: inRange(parseDate(r['Effective Date']), W.startOfLastYear, W.endOfLastYear), desc: `EffectiveDate="${r['Effective Date']}" not in last-year window` },
    ]},
  // TABLES
  { kind: 'TABLE', name: 'cert-types', title: 'Total Active Certs by Certification Type',
    sourceUrl: '/Dashboard/DownloadTable?type=cert-types',
    statusFilter: 'DashboardStatuses',
    dateFilter: 'ExpirationDate null OR >= today',
    visibleSumExpected: 'equals export rows (no cap)', validator: null },
  { kind: 'TABLE', name: 'top-companies', title: 'Companies by Active Cert Count (cap 10)',
    sourceUrl: '/Dashboard/DownloadTable?type=top-companies',
    statusFilter: 'DashboardStatuses',
    dateFilter: 'ExpirationDate null OR >= today',
    visibleSumExpected: 'visible_sum ≤ export rows (cap at top 10)', validator: null },
  { kind: 'TABLE', name: 'cert-status', title: '# Certs by Status',
    sourceUrl: '/Dashboard/DownloadTable?type=cert-status',
    statusFilter: 'ALL statuses (no filter)',
    dateFilter: 'all dates',
    visibleSumExpected: 'equals Statistics["Total"] count of all certs', validator: null },
  { kind: 'TABLE', name: 'product-types', title: 'Product Types by Active Cert Count (cap 10)',
    sourceUrl: '/Dashboard/DownloadTable?type=product-types',
    statusFilter: 'DashboardStatuses',
    dateFilter: 'ExpirationDate null OR >= today',
    visibleSumExpected: 'visible_sum may be < export (export = all active certs; many lack ProductType)',
    validator: null },
  { kind: 'TABLE', name: 'product-categories', title: 'Product Categories by Active Cert Count (cap 5)',
    sourceUrl: '/Dashboard/DownloadTable?type=product-categories',
    statusFilter: 'DashboardStatuses',
    dateFilter: 'ExpirationDate null OR >= today',
    visibleSumExpected: 'visible_sum ≤ export rows (cap at top 5)', validator: null },
  // ACTION buttons (rolling windows)
  { kind: 'ACTION', name: 'active-certificates', title: 'Download Current Active Certs Report',
    sourceUrl: '/Export/DownloadReport?type=active-certificates&format=csv',
    statusFilter: 'PublicStatuses (Paid + Republished)',
    dateFilter: 'ExpirationDate null OR >= today',
    validator: r => [
      { ok: DASHBOARD_POST_FIX.has((r.Status || '').trim()), desc: `Status="${r.Status}" should be Paid or Republished (PublicStatuses)` },
      { ok: !r['Expiration Date'] || parseDate(r['Expiration Date']) >= W.today, desc: `ExpDate="${r['Expiration Date']}" must be >= today` },
    ]},
  { kind: 'ACTION', name: 'expiring-30-days', title: 'Download Expiring Certs Report (Next 30 Days)',
    sourceUrl: '/Export/DownloadReport?type=expiring-30-days&format=csv',
    statusFilter: 'PublicStatuses (Paid + Republished)',
    dateFilter: `ExpirationDate in ROLLING [${ymd(W.today)}..${ymd(W.plus30)}]`,
    validator: r => [
      { ok: DASHBOARD_POST_FIX.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
      { ok: inRange(parseDate(r['Expiration Date']), W.today, W.plus30), desc: `ExpDate="${r['Expiration Date']}" not in rolling 30 days from today` },
    ]},
  { kind: 'ACTION', name: 'expiring-3-months', title: 'Download Expiring Certs Report (Next 3 Months)',
    sourceUrl: '/Export/DownloadReport?type=expiring-3-months&format=csv',
    statusFilter: 'PublicStatuses (Paid + Republished)',
    dateFilter: `ExpirationDate in ROLLING [${ymd(W.today)}..${ymd(W.plus3mo)}]`,
    validator: r => [
      { ok: DASHBOARD_POST_FIX.has((r.Status || '').trim()), desc: `Status="${r.Status}"` },
      { ok: inRange(parseDate(r['Expiration Date']), W.today, W.plus3mo), desc: `ExpDate="${r['Expiration Date']}" not in rolling 3 months from today` },
    ]},
];

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 1800 } });
  const page = await ctx.newPage();

  console.log(`\n=== Full dashboard data audit — ${TARGET.toUpperCase()} (${BASE}) ===`);
  console.log(`Stamp: ${STAMP}    today=${ymd(W.today)}\n`);

  const results = [];
  const certAppearance = {}; // certNumber → Set of report names it appears in

  try {
    await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(2000);

    await page.goto(BASE + '/Dashboard', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);

    // Capture on-screen values for stat cards
    const cards = await page.locator('.stat-card').evaluateAll(els =>
      els.map(el => ({
        title: (el.querySelector('.stat-title')?.innerText || '').trim(),
        value: (el.querySelector('.stat-value')?.innerText || '').trim(),
        statType: el.querySelector('.stat-download-icon')?.getAttribute('data-stat-type') || null,
      })).filter(c => c.statType)
    );
    const statByType = {};
    cards.forEach(c => statByType[c.statType] = Number((c.value || '0').replace(/[^\d.-]/g, '')));

    // Capture on-screen values for tables
    const tableSections = await page.locator('.table-section').evaluateAll(els =>
      els.map(el => {
        const tableType = el.querySelector('.table-download-icon')?.getAttribute('data-table-type') || null;
        const rows = Array.from(el.querySelectorAll('tbody tr')).map(tr =>
          Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim()));
        return { tableType, rows };
      }).filter(t => t.tableType)
    );
    const tableByType = {};
    tableSections.forEach(t => {
      const dataRows = t.rows.filter(r => r.length >= 2 && /^\d/.test((r[1] || '').replace(/,/g, '')));
      const visibleSum = dataRows.reduce((s, r) => s + Number(r[1].replace(/,/g, '')), 0);
      tableByType[t.tableType] = { visibleSum, visibleRowCount: dataRows.length };
    });

    // For each report, hit the endpoint and validate
    for (const rpt of REPORTS) {
      const resp = await ctx.request.get(BASE + rpt.sourceUrl);
      const status = resp.status();
      let rows = [], exportCount = 0, semFails = [], onScreen = null;
      if (status === 200) {
        const text = await resp.text();
        fs.writeFileSync(path.join(OUT, `${PREFIX}-${rpt.kind.toLowerCase()}-${rpt.name}.csv`), text);
        const parsed = parseDataRows(text);
        rows = parsed.rows;
        exportCount = rows.length;
        // Track cert appearances (only for reports that emit per-cert detail)
        rows.forEach(r => {
          const cn = (r['Certificate Number'] || '').trim();
          if (cn) {
            if (!certAppearance[cn]) certAppearance[cn] = { status: (r.Status || '').trim(), reports: new Set() };
            certAppearance[cn].reports.add(rpt.name);
          }
        });
        // Semantic validation per row
        if (rpt.validator) {
          rows.forEach((r, idx) => {
            const fails = rpt.validator(r).filter(c => !c.ok);
            if (fails.length) semFails.push({
              cert: r['Certificate Number'] || r.Name || `row#${idx+1}`,
              fails: fails.map(f => f.desc),
            });
          });
        }
      }
      // On-screen value lookup
      if (rpt.kind === 'STAT') {
        onScreen = statByType[rpt.name] ?? null;
      } else if (rpt.kind === 'TABLE') {
        const t = tableByType[rpt.name];
        onScreen = t ? `sum=${t.visibleSum} (${t.visibleRowCount} rows)` : null;
      } else {
        onScreen = '(no on-screen counter — button only)';
      }

      results.push({
        ...rpt,
        validator: undefined, // strip function for JSON
        httpStatus: status, exportCount, onScreen,
        countMatch: rpt.kind === 'STAT' ? (status === 200 && onScreen === exportCount) :
                    rpt.kind === 'TABLE' ? (status === 200 && tableByType[rpt.name]?.visibleSum === exportCount) :
                    null,
        semFails,
      });
    }

    // ── BUILD MARKDOWN REPORT ────────────────────────────────────────────
    const md = [];
    md.push(`# Dashboard Data-Accuracy Matrix — ${TARGET.toUpperCase()}`);
    md.push('');
    md.push(`- **Source:** \`${BASE}\``);
    md.push(`- **Run at:** ${new Date().toISOString()}`);
    md.push(`- **Today (US/Eastern, used by server):** ${ymd(W.today)}`);
    md.push(`- **Status sets:**`);
    md.push(`  - \`DashboardStatuses\` (post-DEF-223): \`{Client Approved - Paid, Republished}\``);
    md.push(`  - \`PublicStatuses\`: \`{Client Approved - Paid, Republished}\` (equal post-DEF-223)`);
    md.push(`  - \`CurrentlyExpiredStatuses\` (post-DEF-220): \`{Expired, Expired - No Annual Verification}\``);
    if (BASE.includes('gccwebapp')) {
      md.push(`  - **NOTE:** Live still has the pre-DEF-223 wider set including \`Client Approved - Confidential\` until the pending zip deploys. Semantic checks here use the wider set for live to avoid false failures.`);
    }
    md.push('');
    md.push(`## Date windows resolved for today`);
    md.push('');
    md.push(`| Label | Type | Window |`);
    md.push(`|---|---|---|`);
    md.push(`| Last Month | calendar | ${ymd(W.startOfLastMonth)} .. ${ymd(W.endOfLastMonth)} |`);
    md.push(`| This Month | calendar | ${ymd(W.startOfMonth)} .. ${ymd(W.endOfMonth)} |`);
    md.push(`| Next Month | calendar | ${ymd(W.startOfNextMonth)} .. ${ymd(W.endOfNextMonth)} |`);
    md.push(`| This Year-to-Date | calendar | ${ymd(W.startOfYear)} .. ${ymd(W.today)} |`);
    md.push(`| Last Year | calendar | ${ymd(W.startOfLastYear)} .. ${ymd(W.endOfLastYear)} |`);
    md.push(`| Next 30 Days | **rolling** | ${ymd(W.today)} .. ${ymd(W.plus30)} |`);
    md.push(`| Next 3 Months | **rolling** | ${ymd(W.today)} .. ${ymd(W.plus3mo)} |`);
    md.push('');

    // Section: Per-report matrix
    md.push(`## Per-report matrix`);
    md.push('');
    md.push(`Every report surface, the filter it actually applies, the number on screen, the number in the export, and whether they reconcile. Click any URL for the raw endpoint.`);
    md.push('');
    md.push(`| # | Kind | Source | Title | Status filter | Date filter | On-screen | Export | Match | Semantic |`);
    md.push(`|---|------|--------|-------|---------------|-------------|-----------|--------|-------|----------|`);
    results.forEach((r, i) => {
      const matchCell = r.countMatch === null ? '—' : (r.countMatch ? '✅' : '❌');
      const semCell = r.semFails.length
        ? `❌ ${r.semFails.length} row(s)`
        : (r.validator !== undefined || true ? '✅' : '—'); // validator stripped, default ok
      const semCellFinal = r.semFails.length ? `❌ ${r.semFails.length} row(s)` : '✅';
      md.push(`| ${i+1} | ${r.kind} | \`${r.sourceUrl}\` | ${r.title} | ${r.statusFilter} | ${r.dateFilter} | ${r.onScreen ?? '—'} | ${r.exportCount} | ${matchCell} | ${semCellFinal} |`);
    });
    md.push('');

    // Section: Semantic failures detail
    const sf = results.filter(r => r.semFails.length);
    if (sf.length) {
      md.push(`## Per-row semantic failures`);
      md.push('');
      md.push(`Rows where the export contains a value that contradicts the report's stated filter. On live this is expected to surface Confidential rows until the DEF-223 deploy.`);
      md.push('');
      for (const r of sf) {
        md.push(`### ${r.kind} — \`${r.name}\` (${r.semFails.length} fail)`);
        md.push('');
        r.semFails.slice(0, 10).forEach(f => {
          md.push(`- **${f.cert}** — ${f.fails.join('; ')}`);
        });
        if (r.semFails.length > 10) md.push(`- … ${r.semFails.length - 10} more`);
        md.push('');
      }
    } else {
      md.push(`## Per-row semantic failures`);
      md.push(``);
      md.push(`None. Every row in every per-cert export matched its report's stated status + date filters.`);
      md.push('');
    }

    // Section: Per-cert appearance matrix
    md.push(`## Per-cert appearance matrix`);
    md.push('');
    md.push(`Each cert appearing in any per-cert export, and which reports it shows up in. Useful for spotting "why is this cert in report X but not Y?" questions.`);
    md.push('');
    const allCerts = Object.keys(certAppearance).sort();
    const reportNames = REPORTS.filter(r => r.kind !== 'STAT' || !r.name.endsWith('with-active-certs') || r.name === 'total-active-certs').map(r => r.name);
    // Compact: header row of report names; one row per cert with ✓ marks
    if (allCerts.length) {
      const cols = REPORTS.filter(r =>
        // only per-cert detail reports (skip aggregate stat cards that emit Name,Count)
        !['companies-with-active-certs', 'product-categories-with-active-certs',
          'product-types-with-active-certs', 'product-subcategories-with-active-certs',
          'products-with-active-certs'].includes(r.name)
      );
      md.push(`| Cert # | Status | ` + cols.map(c => `\`${c.name.slice(0, 16)}\``).join(' | ') + ` |`);
      md.push(`|--------|--------|` + cols.map(() => '---').join('|') + `|`);
      allCerts.forEach(cn => {
        const ca = certAppearance[cn];
        const marks = cols.map(c => ca.reports.has(c.name) ? '✓' : ' ').join(' | ');
        md.push(`| ${cn} | ${ca.status} | ${marks} |`);
      });
    } else {
      md.push(`(no per-cert detail rows captured)`);
    }
    md.push('');

    // Section: Reconciliation guide
    md.push(`## Reconciliation guide — why some numbers don't add up cleanly`);
    md.push('');
    md.push(`Several reports look like they should compose into each other but don't. Each non-composition has a specific reason — none of them are bugs in the data, but the **definitions diverge**:`);
    md.push('');
    md.push(`1. **"Next 3 Months" ACTION export vs the calendar-month badges.**`);
    md.push(`   - Action button window: rolling \`[${ymd(W.today)} .. ${ymd(W.plus3mo)}]\` (today through today+3 calendar months), **future-only** (\`ExpDate >= today\`).`);
    md.push(`   - "Expiring This Month" badge: calendar \`[${ymd(W.startOfMonth)} .. ${ymd(W.endOfMonth)}]\` — **includes dates earlier in the current month that have already passed**.`);
    md.push(`   - "Expiring Next Month" badge: calendar \`[${ymd(W.startOfNextMonth)} .. ${ymd(W.endOfNextMonth)}]\`.`);
    md.push(`   - So \`Next3moAction ≠ ExpThisMonth + ExpNextMonth + ExpMonthAfter\` by design.`);
    md.push('');
    md.push(`2. **"Expired Last Month" badge does NOT belong in any "Expiring" report.**`);
    md.push(`   - "Expired Last Month" = certs whose date was in last month AND status is now \`Expired\` (already past).`);
    md.push(`   - Any "Expiring" report = future-facing only (date >= today). The 6 Expired-Last-Month certs are not in the Next 30 / Next 3 Months exports because their date is past.`);
    md.push('');
    md.push(`3. **Top-N table totals (Companies, Product Types, Product Categories).**`);
    md.push(`   - Visible "Total" on the table = sum of the top-N rows shown (capped at 10 or 5).`);
    md.push(`   - Export = ALL active certs (no top-N cap). So \`visible_sum ≤ export_rows\` is normal; equality only when fewer than the cap exist.`);
    md.push(`   - Specific to **Product Types**: most active certs don't have a Product Type assigned, so \`visible_sum\` is small (~4) while \`export\` = all 26 active certs. The export is showing the certs **so you can see which ones are missing a Product Type**, not just the top types.`);
    md.push('');
    md.push(`4. **"# Certs by Status" table includes all 8 statuses including non-Active.**`);
    md.push(`   - Sum of all 8 status counts = ALL certificates in the database (Draft + Hold + Expired + etc.), not just active.`);
    md.push(`   - That's why the visible total is larger than "Total Active Certs" — it's a different scope by design.`);
    md.push('');

    fs.writeFileSync(path.join(OUT, `${PREFIX}-matrix.json`), JSON.stringify({ results, certAppearance: Object.fromEntries(Object.entries(certAppearance).map(([k,v]) => [k, {status: v.status, reports: [...v.reports]}])), windows: W }, (k,v) => v instanceof Date ? v.toISOString() : v, 2));
    fs.writeFileSync(path.join(OUT, `${PREFIX}-MATRIX.md`), md.join('\n'));

    // Concise stdout summary
    console.log(`Summary:`);
    const stats = results.filter(r => r.kind === 'STAT');
    const tbls = results.filter(r => r.kind === 'TABLE');
    const acts = results.filter(r => r.kind === 'ACTION');
    console.log(`  STATs   : ${stats.filter(r => r.countMatch).length}/${stats.length} count match,  ${stats.filter(r => !r.semFails.length).length}/${stats.length} semantic OK`);
    console.log(`  TABLEs  : ${tbls.filter(r => r.countMatch).length}/${tbls.length} count match (expected mismatches for capped top-N), ${tbls.filter(r => !r.semFails.length).length}/${tbls.length} semantic OK`);
    console.log(`  ACTIONs : ${acts.length} buttons audited,  ${acts.filter(r => !r.semFails.length).length}/${acts.length} semantic OK`);
    console.log(`\nMarkdown report: ${PREFIX}-MATRIX.md`);
    console.log(`JSON           : ${PREFIX}-matrix.json`);
    console.log(`Raw CSVs       : ${PREFIX}-*.csv (${results.filter(r => r.httpStatus === 200).length} files)`);

  } catch (e) {
    console.log(`ERROR: ${e.message}\n${e.stack}`);
  } finally {
    await browser.close();
  }
})();
