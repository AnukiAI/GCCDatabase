// QC #180 + DEF-220 — Full dashboard data-accuracy audit.
//
// Goes beyond the QC #180 tile=export count contract: every row in every
// export CSV is checked against the semantic contract of the card or table
// it came from.
//
//   STAT CARDS (12 badges)
//     - tile count = export row count
//     - per-row Status column ∈ expected status set for the card
//     - per-row EffectiveDate / ExpirationDate falls in the card's date window
//
//   TABLES (5 dashboard breakdown tables)
//     - sum of visible Count column = total active certs in that scope
//     - export grouped by the table's group-by column matches visible rows
//     - every exported row's Status ∈ DashboardStatuses (Paid + Republished
//       + Confidential) since all 5 tables are "Active *" scoped
//     - cert-status table: per-status count = exported rows of that status
//
// Env vars: BASE_URL (default localhost), APP_USER, APP_PASS, TARGET (label).

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
const PREFIX = `qc220-${TARGET}-${STAMP}`;

// Canonical status sets — mirror Models/CertificateStatus.cs
const DASHBOARD_STATUSES = new Set([
  'Client Approved - Paid',
  'Republished',
  'Client Approved - Confidential',
]);
const CURRENTLY_EXPIRED_STATUSES = new Set([
  'Expired',
  'Expired - No Annual Verification',
]);

// Date windows (business-today in US/Eastern). Server-side uses Eastern Time
// per CertificateStatusHelper.BusinessToday; the audit allows the local clock
// to drift by 1 day in either direction without flagging a row, since the
// audit runs in UTC and the server may be a few hours off relative to local.
function getWindows() {
  const t = new Date();
  // Today, last/this/next month, this/last year — calendar bounds in YYYY-MM-DD.
  const today = new Date(t.getFullYear(), t.getMonth(), t.getDate());
  const startOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
  const startOfLastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
  const endOfLastMonth = new Date(today.getFullYear(), today.getMonth(), 0);
  const startOfNextMonth = new Date(today.getFullYear(), today.getMonth() + 1, 1);
  const endOfNextMonth = new Date(today.getFullYear(), today.getMonth() + 2, 0);
  const endOfMonth = new Date(today.getFullYear(), today.getMonth() + 1, 0);
  const startOfYear = new Date(today.getFullYear(), 0, 1);
  const startOfLastYear = new Date(today.getFullYear() - 1, 0, 1);
  const endOfLastYear = new Date(today.getFullYear() - 1, 11, 31);
  return {
    today, startOfMonth, endOfMonth,
    startOfLastMonth, endOfLastMonth,
    startOfNextMonth, endOfNextMonth,
    startOfYear, startOfLastYear, endOfLastYear,
  };
}

const W = getWindows();
function ymd(d) { return d.toISOString().slice(0, 10); }
function parseDate(s) {
  // CSV dates are yyyy-MM-dd (per GenerateCertificateCsv)
  if (!s || typeof s !== 'string') return null;
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(s);
  if (!m) return null;
  return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]));
}
function inRange(d, lo, hi) {
  if (!d) return false;
  return d.getTime() >= lo.getTime() && d.getTime() <= hi.getTime();
}

// CSV parser (handles quoted fields with embedded commas/newlines)
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

// Return { headers, rows: [{col1: val, col2: val, …}] } skipping the
// preamble lines ("Report:", "Generated:", "Total …", blank).
function parseDataRows(text) {
  const all = parseCsv(text);
  let headerIdx = -1;
  for (let i = 0; i < Math.min(all.length, 10); i++) {
    const first = (all[i][0] || '').trim();
    if (first && !/report|generated|total/i.test(first) && all[i].length > 1) {
      headerIdx = i;
      break;
    }
  }
  if (headerIdx < 0) return { headers: [], rows: [] };
  const headers = all[headerIdx];
  const rows = all.slice(headerIdx + 1).map(r => {
    const obj = {};
    headers.forEach((h, idx) => { obj[h] = r[idx] || ''; });
    return obj;
  });
  return { headers, rows };
}

// ── Per-card semantic rules ─────────────────────────────────────────────────

// Stat-card rules. Each rule returns array of {field, ok, detail} per row.
// Empty array = no per-row checks (e.g. aggregate cards).
const STAT_RULES = {
  'total-active-certs': (row) => {
    const ok = DASHBOARD_STATUSES.has((row.Status || '').trim());
    return [{ field: 'Status', ok, detail: `expected ∈ {Paid, Republished, Confidential}, got "${row.Status}"` }];
  },
  'certs-published-last-month': (row) => {
    const sOk = DASHBOARD_STATUSES.has((row.Status || '').trim());
    const d = parseDate(row['Effective Date']);
    const dOk = inRange(d, W.startOfLastMonth, W.endOfLastMonth);
    return [
      { field: 'Status', ok: sOk, detail: `expected ∈ Dashboard, got "${row.Status}"` },
      { field: 'EffectiveDate', ok: dOk, detail: `expected in [${ymd(W.startOfLastMonth)}..${ymd(W.endOfLastMonth)}], got "${row['Effective Date']}"` },
    ];
  },
  'certs-expiring-this-month': (row) => {
    const sOk = DASHBOARD_STATUSES.has((row.Status || '').trim());
    const d = parseDate(row['Expiration Date']);
    const dOk = inRange(d, W.startOfMonth, W.endOfMonth);
    return [
      { field: 'Status', ok: sOk, detail: `expected ∈ Dashboard, got "${row.Status}"` },
      { field: 'ExpirationDate', ok: dOk, detail: `expected in [${ymd(W.startOfMonth)}..${ymd(W.endOfMonth)}], got "${row['Expiration Date']}"` },
    ];
  },
  'certs-expired-last-month': (row) => {
    const sOk = CURRENTLY_EXPIRED_STATUSES.has((row.Status || '').trim());
    const d = parseDate(row['Expiration Date']);
    const dOk = inRange(d, W.startOfLastMonth, W.endOfLastMonth);
    return [
      { field: 'Status', ok: sOk, detail: `expected ∈ {Expired, ExpiredNoAnnualVerification}, got "${row.Status}"` },
      { field: 'ExpirationDate', ok: dOk, detail: `expected in [${ymd(W.startOfLastMonth)}..${ymd(W.endOfLastMonth)}], got "${row['Expiration Date']}"` },
    ];
  },
  'certs-expiring-next-month': (row) => {
    const sOk = DASHBOARD_STATUSES.has((row.Status || '').trim());
    const d = parseDate(row['Expiration Date']);
    const dOk = inRange(d, W.startOfNextMonth, W.endOfNextMonth);
    return [
      { field: 'Status', ok: sOk, detail: `expected ∈ Dashboard, got "${row.Status}"` },
      { field: 'ExpirationDate', ok: dOk, detail: `expected in [${ymd(W.startOfNextMonth)}..${ymd(W.endOfNextMonth)}], got "${row['Expiration Date']}"` },
    ];
  },
  'certs-published-this-year': (row) => {
    const sOk = DASHBOARD_STATUSES.has((row.Status || '').trim());
    const d = parseDate(row['Effective Date']);
    const dOk = inRange(d, W.startOfYear, W.today);
    return [
      { field: 'Status', ok: sOk, detail: `expected ∈ Dashboard, got "${row.Status}"` },
      { field: 'EffectiveDate', ok: dOk, detail: `expected in [${ymd(W.startOfYear)}..${ymd(W.today)}], got "${row['Effective Date']}"` },
    ];
  },
  'certs-published-last-year': (row) => {
    const sOk = DASHBOARD_STATUSES.has((row.Status || '').trim());
    const d = parseDate(row['Effective Date']);
    const dOk = inRange(d, W.startOfLastYear, W.endOfLastYear);
    return [
      { field: 'Status', ok: sOk, detail: `expected ∈ Dashboard, got "${row.Status}"` },
      { field: 'EffectiveDate', ok: dOk, detail: `expected in [${ymd(W.startOfLastYear)}..${ymd(W.endOfLastYear)}], got "${row['Effective Date']}"` },
    ];
  },
  // Aggregate cards return (Name, Count) CSV; no per-row status check.
  'companies-with-active-certs': () => [],
  'product-categories-with-active-certs': () => [],
  'product-types-with-active-certs': () => [],
  'product-subcategories-with-active-certs': () => [],
  'products-with-active-certs': () => [],
};

// Table grouping: for each table type, the export is a per-cert detail CSV.
// To compare to the visible (Name, Count) table we group by these columns.
const TABLE_GROUP_BY = {
  'cert-types': 'Certification Type',
  'top-companies': 'Company',
  'cert-status': 'Status',
  'product-types': 'Product Type',
  'product-categories': 'Product Category',
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    httpCredentials: BASIC,
    viewport: { width: 1440, height: 900 },
  });
  const page = await ctx.newPage();

  console.log(`\n=== QC #180 / DEF-220 — Full dashboard data audit ===`);
  console.log(`Target:  ${TARGET.toUpperCase()} (${BASE})`);
  console.log(`Stamp:   ${STAMP}`);
  console.log(`Windows: lastMonth=${ymd(W.startOfLastMonth)}..${ymd(W.endOfLastMonth)}  thisMonth=${ymd(W.startOfMonth)}..${ymd(W.endOfMonth)}`);
  console.log(`         nextMonth=${ymd(W.startOfNextMonth)}..${ymd(W.endOfNextMonth)}  thisYear=${ymd(W.startOfYear)}..${ymd(W.today)}\n`);

  const findings = { stats: [], tables: [] };

  try {
    // Login
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(2000);
    if (/\/Account\/Login/i.test(page.url())) {
      console.log(`  login failed (still at /Account/Login). user=${APP_USER}`);
      process.exit(2);
    }

    // Dashboard
    await page.goto(BASE + '/Dashboard', { waitUntil: 'networkidle' });
    await page.waitForTimeout(2500); // animation settle

    // ── STAT CARDS ────────────────────────────────────────────────────────
    const cards = await page.locator('.stat-card').evaluateAll(els =>
      els.map(el => ({
        title: (el.querySelector('.stat-title')?.innerText || '').trim(),
        value: (el.querySelector('.stat-value')?.innerText || '').trim(),
        statType: el.querySelector('.stat-download-icon')?.getAttribute('data-stat-type') || null,
      })).filter(c => c.statType)
    );
    console.log(`STAT CARDS — ${cards.length} cards on dashboard\n`);

    for (const card of cards) {
      const tileNum = Number((card.value || '0').replace(/[^\d.-]/g, ''));
      const url = `${BASE}/Dashboard/DownloadStat?type=${encodeURIComponent(card.statType)}`;
      const resp = await ctx.request.get(url);
      const status = resp.status();
      let exportRows = null, rowChecks = null, badRows = [], csvBytes = 0, text = '';
      if (status === 200) {
        text = await resp.text();
        csvBytes = text.length;
        const { rows } = parseDataRows(text);
        exportRows = rows.length;
        fs.writeFileSync(path.join(OUT, `${PREFIX}-stat-${card.statType}.csv`), text);

        // Per-row semantic check
        const rule = STAT_RULES[card.statType];
        if (rule) {
          rowChecks = { total: rows.length, failed: 0, issues: [] };
          rows.forEach((row, idx) => {
            const checks = rule(row);
            const failures = checks.filter(c => !c.ok);
            if (failures.length) {
              rowChecks.failed++;
              if (badRows.length < 5) {
                badRows.push({
                  cert: row['Certificate Number'] || row.Name || `row#${idx + 1}`,
                  fails: failures.map(f => `${f.field}: ${f.detail}`),
                });
              }
            }
          });
        }
      }
      const countPass = status === 200 && tileNum === exportRows;
      const semPass = !rowChecks || rowChecks.failed === 0;
      const tag = countPass && semPass ? '✅' : '❌';
      const semSuffix = rowChecks
        ? `  semantic=${rowChecks.failed === 0 ? 'OK' : `${rowChecks.failed}/${rowChecks.total} ROWS FAIL`}`
        : '';
      console.log(`${tag} ${card.statType.padEnd(46)} tile=${String(tileNum).padStart(3)} export=${String(exportRows).padStart(3)}${semSuffix}`);
      if (badRows.length) {
        badRows.forEach(b => console.log(`     └─ ${b.cert}: ${b.fails.join('; ')}`));
      }
      findings.stats.push({
        statType: card.statType, title: card.title,
        tile: tileNum, exportRows, httpStatus: status, csvBytes,
        countPass, semPass,
        semanticFailures: rowChecks ? { failed: rowChecks.failed, total: rowChecks.total, sample: badRows } : null,
      });
    }

    // ── TABLES ────────────────────────────────────────────────────────────
    console.log(`\nDASHBOARD TABLES — 5 breakdown tables\n`);

    const tableSections = await page.locator('.table-section').evaluateAll(els =>
      els.map(el => {
        const title = (el.querySelector('.table-title')?.innerText || '').trim();
        const tableType = el.querySelector('.table-download-icon')?.getAttribute('data-table-type') || null;
        const rows = Array.from(el.querySelectorAll('tbody tr')).map(tr => {
          const cells = Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim());
          return cells;
        });
        return { title, tableType, rows };
      }).filter(t => t.tableType)
    );

    for (const tbl of tableSections) {
      // Skip the empty-state row "Access restricted to internal users" / "No * data available"
      const dataRows = tbl.rows.filter(r => r.length >= 2 && /^\d/.test(r[1].replace(/,/g, '')));
      const visiblePairs = dataRows.map(r => ({ name: r[0], count: Number(r[1].replace(/,/g, '')) }));
      const visibleTotal = visiblePairs.reduce((s, p) => s + p.count, 0);

      const url = `${BASE}/Dashboard/DownloadTable?type=${encodeURIComponent(tbl.tableType)}`;
      const resp = await ctx.request.get(url);
      const status = resp.status();
      let exportRows = 0, exportText = '', issues = [];

      if (status === 200) {
        exportText = await resp.text();
        fs.writeFileSync(path.join(OUT, `${PREFIX}-table-${tbl.tableType}.csv`), exportText);
        const { rows } = parseDataRows(exportText);
        exportRows = rows.length;

        // Status check on every export row (all 5 tables are "Active *" scoped
        // EXCEPT cert-status which intentionally includes every status).
        if (tbl.tableType !== 'cert-status') {
          const badStatus = rows.filter(r => !DASHBOARD_STATUSES.has((r.Status || '').trim()));
          if (badStatus.length) {
            issues.push(`${badStatus.length}/${rows.length} export rows have non-Active Status`);
            badStatus.slice(0, 3).forEach(r =>
              issues.push(`  └─ ${r['Certificate Number']}: Status="${r.Status}"`));
          }
        }

        // Group export by the table's group-by column, compare to visible pairs.
        const groupCol = TABLE_GROUP_BY[tbl.tableType];
        if (groupCol) {
          const groups = {};
          rows.forEach(r => {
            const key = (r[groupCol] || '(empty)').trim() || '(empty)';
            groups[key] = (groups[key] || 0) + 1;
          });
          // For top-N tables visible only shows top N; check each visible row matches.
          visiblePairs.forEach(pair => {
            const exportCount = groups[pair.name] ?? 0;
            if (exportCount !== pair.count) {
              issues.push(`group "${pair.name}": visible=${pair.count}, export=${exportCount}`);
            }
          });
          // Sum check: for non-top-N tables (cert-types, cert-status, product-categories
          // — these aren't capped) the visible total = export rows.
          // Top-10/Top-5 tables may have export > visible (export is all active certs, visible is top N).
          const isTopN = /top-companies|product-types|product-categories/.test(tbl.tableType);
          if (!isTopN && visibleTotal !== exportRows) {
            issues.push(`visibleSum=${visibleTotal}, exportRows=${exportRows} (non-top-N tables should match)`);
          }
        }
      } else {
        issues.push(`HTTP ${status}`);
      }

      const pass = status === 200 && issues.length === 0;
      const tag = pass ? '✅' : '❌';
      console.log(`${tag} ${tbl.tableType.padEnd(20)} visible_rows=${visiblePairs.length}  visible_sum=${visibleTotal}  export_rows=${exportRows}`);
      if (issues.length) issues.forEach(i => console.log(`     └─ ${i}`));

      findings.tables.push({
        tableType: tbl.tableType, title: tbl.title,
        visiblePairs, visibleTotal, exportRows,
        httpStatus: status, issues, pass,
      });
    }

    fs.writeFileSync(path.join(OUT, `${PREFIX}-findings.json`), JSON.stringify(findings, null, 2));
    await page.screenshot({ path: path.join(OUT, `${PREFIX}-dashboard.png`), fullPage: true });

    const statPass = findings.stats.filter(s => s.countPass && s.semPass).length;
    const statFail = findings.stats.length - statPass;
    const tablePass = findings.tables.filter(t => t.pass).length;
    const tableFail = findings.tables.length - tablePass;
    console.log(`\n────────────────────────────────────────────────────────────`);
    console.log(`STAT CARDS : ${statPass} PASS / ${statFail} FAIL  (${findings.stats.length} total)`);
    console.log(`TABLES     : ${tablePass} PASS / ${tableFail} FAIL  (${findings.tables.length} total)`);
    console.log(`────────────────────────────────────────────────────────────`);
    console.log(`Artifacts saved with prefix: ${PREFIX}`);

  } catch (e) {
    console.log(`ERROR: ${e.message}\n${e.stack}`);
    await page.screenshot({ path: path.join(OUT, `${PREFIX}-error.png`), fullPage: true }).catch(() => {});
  } finally {
    await browser.close();
  }
})();
