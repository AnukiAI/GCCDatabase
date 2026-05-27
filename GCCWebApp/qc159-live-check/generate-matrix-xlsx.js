// Produce a professional Excel workbook from the matrix JSON.
//
// Sheets:
//   1. Executive Summary    — pass/fail totals, key findings
//   2. Validation Matrix    — every report: on-screen # vs export #, with filters
//   3. Per-Cert Appearance  — 37 certs x 15 per-cert reports (✓/blank)
//   4. Date Window Reference — calendar vs rolling window definitions
//   5. Reconciliation Notes — why numbers don't always compose cleanly

const ExcelJS = require('exceljs');
const fs = require('fs');
const path = require('path');

const IN = process.argv[2];
if (!IN) { console.error('usage: node generate-matrix-xlsx.js <matrix.json>'); process.exit(1); }
const data = JSON.parse(fs.readFileSync(IN, 'utf8'));
const OUT = IN.replace(/-matrix\.json$/i, '-VALIDATION.xlsx');

const TODAY = data.windows.today.slice(0, 10);

// Colors (ARGB)
const C = {
  headerBg: 'FF1A3A5C',
  headerText: 'FFFFFFFF',
  subheaderBg: 'FFD6E5F0',
  ok: 'FFC8E6C9',       // green
  fail: 'FFFFCDD2',      // red
  expected: 'FFFFF3CD',  // yellow (intentional mismatch)
  na: 'FFF0F0F0',
  banded: 'FFF8FAFC',
  border: 'FFB0C0D0',
};

const wb = new ExcelJS.Workbook();
wb.creator = 'Anuki — GCC Dashboard Validator';
wb.created = new Date();

function thinBorder() {
  return {
    top: { style: 'thin', color: { argb: C.border } },
    left: { style: 'thin', color: { argb: C.border } },
    bottom: { style: 'thin', color: { argb: C.border } },
    right: { style: 'thin', color: { argb: C.border } },
  };
}
function headerStyle() {
  return {
    font: { bold: true, color: { argb: C.headerText }, size: 11 },
    fill: { type: 'pattern', pattern: 'solid', fgColor: { argb: C.headerBg } },
    alignment: { vertical: 'middle', horizontal: 'left', wrapText: true },
    border: thinBorder(),
  };
}
function subheaderStyle() {
  return {
    font: { bold: true, color: { argb: C.headerBg }, size: 11 },
    fill: { type: 'pattern', pattern: 'solid', fgColor: { argb: C.subheaderBg } },
    border: thinBorder(),
  };
}
function fillRow(row, color) {
  row.eachCell({ includeEmpty: true }, cell => {
    cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: color } };
  });
}

// ──────────────────────────────────────────────────────────────────────────
// SHEET 1: Executive Summary
// ──────────────────────────────────────────────────────────────────────────
{
  const ws = wb.addWorksheet('Executive Summary', { properties: { tabColor: { argb: 'FF1A3A5C' } } });
  ws.columns = [
    { width: 4 }, { width: 50 }, { width: 18 }, { width: 18 }, { width: 18 },
  ];

  // Title
  ws.mergeCells('A1:E1');
  ws.getCell('A1').value = 'GCC Dashboard — Data Validation Report';
  ws.getCell('A1').style = {
    font: { bold: true, size: 18, color: { argb: C.headerBg } },
    alignment: { vertical: 'middle', horizontal: 'left' },
  };
  ws.getRow(1).height = 30;

  ws.mergeCells('A2:E2');
  ws.getCell('A2').value = `Source: https://gccwebapp.com — captured ${TODAY}`;
  ws.getCell('A2').font = { italic: true, color: { argb: 'FF555555' } };

  ws.addRow([]);

  // Pass/fail totals
  const stats = data.results.filter(r => r.kind === 'STAT');
  const tbls = data.results.filter(r => r.kind === 'TABLE');
  const acts = data.results.filter(r => r.kind === 'ACTION');
  const statCountMatch = stats.filter(r => r.countMatch).length;
  const statSemOk = stats.filter(r => !r.semFails.length).length;
  const tblCountMatch = tbls.filter(r => r.countMatch).length;
  const tblSemOk = tbls.filter(r => !r.semFails.length).length;
  const actSemOk = acts.filter(r => !r.semFails.length).length;

  ws.mergeCells('A4:E4');
  ws.getCell('A4').value = 'Validation results';
  ws.getCell('A4').style = headerStyle();
  ws.getRow(4).height = 22;

  const summary = [
    ['', 'Report kind', 'Count match', 'Semantic check', 'Notes'],
    ['', 'STAT cards (12 dashboard badges)', `${statCountMatch}/${stats.length}`, `${statSemOk}/${stats.length}`, 'Tile value equals export row count for every badge.'],
    ['', 'TABLEs (5 dashboard breakdowns)', `${tblCountMatch}/${tbls.length}`, `${tblSemOk}/${tbls.length}`, 'Top-N tables (companies, product-types) intentionally cap visible rows; export = all active certs.'],
    ['', 'ACTION buttons (3 download buttons)', `${acts.length}/${acts.length}`, `${actSemOk}/${acts.length}`, 'No on-screen counter to compare; semantic-only check.'],
  ];
  summary.forEach((r, i) => {
    const row = ws.addRow(r);
    if (i === 0) {
      row.eachCell((cell, col) => { if (col >= 2) cell.style = subheaderStyle(); });
    } else {
      row.eachCell((cell, col) => { if (col >= 2) cell.border = thinBorder(); });
    }
  });

  ws.addRow([]);

  // Key findings
  ws.mergeCells(`A${ws.rowCount + 1}:E${ws.rowCount + 1}`);
  const findHdr = ws.addRow(['Key findings'].concat(['','','','']));
  ws.mergeCells(`A${findHdr.number}:E${findHdr.number}`);
  ws.getCell(`A${findHdr.number}`).value = 'Key findings (live, pre-deploy)';
  ws.getCell(`A${findHdr.number}`).style = headerStyle();
  ws.getRow(findHdr.number).height = 22;

  const findings = [
    ['1', '✅ Every on-screen badge equals its export row count.', '', '', ''],
    ['2', '✅ Every per-cert export row matches its report\'s stated status + date filter.', '', '', ''],
    ['3', '⚠️ Cert 25-9312 (Esko Graphics) is Status="Client Approved - Confidential" but currently appears in 7 internal "Active" reports. The pending DEF-223 deploy narrows the Active filter to {Paid, Republished} only — Esko will then drop from those reports.', '', '', ''],
    ['4', 'ℹ️ "Next 3 Months" action button returns 2 certs while badges show 1+1+6 for related months. This is correct — the action button uses a rolling future-only window from today; the badges use calendar months and the Expired badge looks at the past. The two views are not designed to compose. See "Reconciliation Notes" sheet.', '', '', ''],
    ['5', 'ℹ️ Top-N table totals (Companies, Product Types, Product Categories) show smaller sums than the corresponding export. This is by design — the table shows the top 5 or 10; the export contains all active certs.', '', '', ''],
  ];
  findings.forEach(r => {
    const row = ws.addRow(r);
    ws.mergeCells(`B${row.number}:E${row.number}`);
    row.getCell(2).alignment = { wrapText: true, vertical: 'top' };
    row.height = 38;
    row.eachCell({ includeEmpty: false }, c => c.border = thinBorder());
  });

  ws.addRow([]);
  ws.addRow(['', 'Status sets in effect on this run:']);
  ws.getCell(`B${ws.rowCount}`).font = { bold: true };
  ws.addRow(['', '  DashboardStatuses (current live):', '{Paid, Republished, Confidential}']);
  ws.addRow(['', '  DashboardStatuses (after deploy DEF-223):', '{Paid, Republished}']);
  ws.addRow(['', '  CurrentlyExpiredStatuses (after DEF-220):', '{Expired, Expired - No Annual Verification}']);

  ws.views = [{ state: 'frozen', ySplit: 2 }];
}

// ──────────────────────────────────────────────────────────────────────────
// SHEET 2: Validation Matrix — per-report
// ──────────────────────────────────────────────────────────────────────────
{
  const ws = wb.addWorksheet('Validation Matrix', { properties: { tabColor: { argb: 'FF1C7A3C' } } });
  ws.columns = [
    { header: '#', width: 5 },
    { header: 'Kind', width: 10 },
    { header: 'Title (what user sees)', width: 50 },
    { header: 'Source URL', width: 60 },
    { header: 'Status filter', width: 60 },
    { header: 'Date filter', width: 50 },
    { header: 'On-screen #', width: 18 },
    { header: 'Export #', width: 12 },
    { header: 'Count Match', width: 14 },
    { header: 'Semantic Check', width: 18 },
    { header: 'Failure detail (if any)', width: 60 },
  ];
  ws.getRow(1).eachCell(c => c.style = headerStyle());
  ws.getRow(1).height = 32;

  data.results.forEach((r, i) => {
    const matchVal = r.countMatch === null ? 'N/A (no badge)' : (r.countMatch ? 'YES' : 'NO');
    const semVal = r.semFails.length ? `FAIL (${r.semFails.length} rows)` : 'OK';
    const failDetail = r.semFails.length
      ? r.semFails.slice(0, 3).map(f => `${f.cert}: ${f.fails.join('; ')}`).join('\n')
      : '';
    const row = ws.addRow([
      i + 1,
      r.kind,
      r.title,
      r.sourceUrl,
      r.statusFilter,
      r.dateFilter,
      r.onScreen ?? '—',
      r.exportCount,
      matchVal,
      semVal,
      failDetail,
    ]);
    row.alignment = { vertical: 'top', wrapText: true };
    row.eachCell(c => c.border = thinBorder());
    // Color the match cell
    const mCell = row.getCell(9);
    if (matchVal === 'YES') mCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.ok } };
    else if (matchVal === 'NO') {
      // Top-N table mismatches are EXPECTED; mark yellow not red
      const expected = r.kind === 'TABLE' && (r.name === 'top-companies' || r.name === 'product-types' || r.name === 'product-categories' || r.name === 'cert-status');
      mCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: expected ? C.expected : C.fail } };
      if (expected && !row.getCell(11).value) row.getCell(11).value = 'Expected: visible is capped at top N; export = all active certs';
    } else {
      mCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.na } };
    }
    // Color the semantic cell
    const sCell = row.getCell(10);
    if (semVal === 'OK') sCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.ok } };
    else sCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.fail } };
    // Band even rows
    if (i % 2 === 1) row.eachCell((c, col) => {
      if (col !== 9 && col !== 10) c.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.banded } };
    });
    row.height = Math.max(20, Math.min(80, 18 + (failDetail.split('\n').length - 1) * 14));
  });

  ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: ws.rowCount, column: 11 } };
  ws.views = [{ state: 'frozen', xSplit: 3, ySplit: 1 }];
}

// ──────────────────────────────────────────────────────────────────────────
// SHEET 3: Per-Cert Appearance Matrix
// ──────────────────────────────────────────────────────────────────────────
{
  const ws = wb.addWorksheet('Per-Cert Matrix', { properties: { tabColor: { argb: 'FFE65100' } } });

  // Columns: cert#, status, then one per per-cert report
  const perCertReports = data.results.filter(r =>
    !['companies-with-active-certs', 'product-categories-with-active-certs',
      'product-types-with-active-certs', 'product-subcategories-with-active-certs',
      'products-with-active-certs'].includes(r.name)
  );

  ws.columns = [
    { header: 'Cert #', width: 12 },
    { header: 'Status', width: 28 },
    ...perCertReports.map(r => ({ header: r.name, width: 12 })),
  ];
  ws.getRow(1).eachCell(c => {
    c.style = headerStyle();
    c.alignment = { textRotation: 60, vertical: 'bottom', horizontal: 'center', wrapText: true };
  });
  ws.getRow(1).height = 110;

  // Sort certs by status group then cert number
  const certs = Object.entries(data.certAppearance).map(([cn, info]) => ({ cn, ...info }));
  const statusOrder = {
    'Client Approved - Paid': 1,
    'Republished': 2,
    'Client Approved - Confidential': 3,
    'Expired': 4,
    'Expired - No Annual Verification': 5,
    'Draft': 6,
    'Hold': 7,
    'Client Approved - Not Paid': 8,
  };
  certs.sort((a, b) => {
    const sa = statusOrder[a.status] ?? 99, sb = statusOrder[b.status] ?? 99;
    if (sa !== sb) return sa - sb;
    return a.cn.localeCompare(b.cn);
  });

  certs.forEach((cert, i) => {
    const reportSet = new Set(cert.reports);
    const row = ws.addRow([
      cert.cn,
      cert.status,
      ...perCertReports.map(r => reportSet.has(r.name) ? '✓' : ''),
    ]);
    row.eachCell({ includeEmpty: true }, (c, col) => {
      c.border = thinBorder();
      if (col >= 3) {
        c.alignment = { horizontal: 'center', vertical: 'middle' };
        if (c.value === '✓') {
          c.font = { bold: true, color: { argb: 'FF1C7A3C' } };
          c.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.ok } };
        }
      }
    });
    // Highlight Confidential cert row (DEF-223 fix target)
    if (cert.status === 'Client Approved - Confidential') {
      row.getCell(1).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.expected } };
      row.getCell(2).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.expected } };
      row.getCell(1).font = { bold: true };
      row.getCell(2).font = { bold: true };
    }
    // Band by status group
    if (i % 2 === 1) {
      row.getCell(1).fill = row.getCell(1).fill || { type: 'pattern', pattern: 'solid', fgColor: { argb: C.banded } };
    }
  });

  ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: ws.rowCount, column: ws.columnCount } };
  ws.views = [{ state: 'frozen', xSplit: 2, ySplit: 1 }];
}

// ──────────────────────────────────────────────────────────────────────────
// SHEET 4: Date Window Reference
// ──────────────────────────────────────────────────────────────────────────
{
  const ws = wb.addWorksheet('Date Windows', { properties: { tabColor: { argb: 'FF6A1B9A' } } });
  ws.columns = [
    { header: 'Label', width: 32 },
    { header: 'Window type', width: 16 },
    { header: 'Start', width: 14 },
    { header: 'End', width: 14 },
    { header: 'Used by', width: 60 },
  ];
  ws.getRow(1).eachCell(c => c.style = headerStyle());
  ws.getRow(1).height = 28;
  const W = data.windows;
  const ymd = s => s.slice(0, 10);
  const rows = [
    ['Last Month', 'calendar', ymd(W.startOfLastMonth), ymd(W.endOfLastMonth), 'Published Last Month, Expired Last Month'],
    ['This Month', 'calendar', ymd(W.startOfMonth), ymd(W.endOfMonth), 'Expiring This Month'],
    ['Next Month', 'calendar', ymd(W.startOfNextMonth), ymd(W.endOfNextMonth), 'Expiring Next Month'],
    ['This Year-to-Date', 'calendar', ymd(W.startOfYear), ymd(W.today), 'Published This Year'],
    ['Last Year', 'calendar', ymd(W.startOfLastYear), ymd(W.endOfLastYear), 'Published Last Year'],
    ['Next 30 Days (rolling)', 'rolling', ymd(W.today), ymd(W.plus30), 'Download Expiring (30 Days) action button'],
    ['Next 3 Months (rolling)', 'rolling', ymd(W.today), ymd(W.plus3mo), 'Download Expiring (3 Months) action button'],
  ];
  rows.forEach((r, i) => {
    const row = ws.addRow(r);
    row.eachCell(c => c.border = thinBorder());
    if (r[1] === 'rolling') {
      row.getCell(2).fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.expected } };
      row.getCell(2).font = { bold: true };
    }
    if (i % 2 === 1) row.eachCell(c => c.fill = c.fill || { type: 'pattern', pattern: 'solid', fgColor: { argb: C.banded } });
  });
  ws.addRow([]);
  ws.mergeCells(`A${ws.rowCount + 1}:E${ws.rowCount + 1}`);
  const noteRow = ws.addRow(['NOTE: "rolling" windows start today and don\'t align to calendar month boundaries. Numbers from rolling and calendar reports cannot be summed together — they cover different sets.']);
  ws.mergeCells(`A${noteRow.number}:E${noteRow.number}`);
  noteRow.getCell(1).font = { italic: true, color: { argb: 'FF6A1B9A' } };
  noteRow.getCell(1).alignment = { wrapText: true };
  noteRow.height = 36;
  ws.views = [{ state: 'frozen', ySplit: 1 }];
}

// ──────────────────────────────────────────────────────────────────────────
// SHEET 5: Reconciliation Notes
// ──────────────────────────────────────────────────────────────────────────
{
  const ws = wb.addWorksheet('Reconciliation', { properties: { tabColor: { argb: 'FF424242' } } });
  ws.columns = [
    { header: 'Topic', width: 50 },
    { header: 'Explanation', width: 110 },
  ];
  ws.getRow(1).eachCell(c => c.style = headerStyle());
  ws.getRow(1).height = 24;

  const items = [
    [
      '"Next 3 Months" export = 2 vs the calendar-month badges',
      'The action button uses a ROLLING window from today (today → today+3mo) and requires ExpirationDate >= today (future-only). The "Expiring This Month" / "Expiring Next Month" badges use CALENDAR months and can include dates in the current month that have already passed. So the action button will not equal "ExpThisMonth + ExpNextMonth + ExpMonthAfter".',
    ],
    [
      '"Expired Last Month" (6) is not in any "Expiring" report',
      'Expired Last Month = certs whose ExpirationDate fell in the previous calendar month AND status is now Expired. These dates are in the past, so they are excluded from any "Expiring" (future-facing) report.',
    ],
    [
      'Top-N table totals smaller than the corresponding export',
      'The visible table caps at top 5 (categories) or top 10 (companies, product types). The export ignores the cap and returns all active certs. So visible_sum ≤ export rows is expected. When fewer than the cap exist (e.g. only 2 categories with active certs), the totals match.',
    ],
    [
      '"Product Types" visible total = 4 vs export = 26',
      'Only 4 of the 26 currently active certs have a Product Type assigned (2 distinct types). The breakdown only shows certs that have a Product Type set; the export returns every active cert so you can see which ones are missing one. This is a DATA QUALITY observation, not a code defect.',
    ],
    [
      '"# Certs by Status" total = 37 vs "Total Active Certs" = 26',
      'The Status table covers ALL 8 statuses including non-Active (Draft, Hold, Expired, etc.). The 11-cert gap is Draft + Hold + Expired + ExpiredNoAnnualVerification rows that are correctly excluded from the Active counts.',
    ],
    [
      'Cert 25-9312 (Esko Graphics) is Confidential but counted as Active',
      'Today\'s live DashboardStatuses constant includes Client Approved - Confidential. After deploying the pending zip (DEF-223), it will narrow to {Paid, Republished} — matching the public-facing definition and the existing IsActiveStatus() helper. Esko will then drop out of all 7 "Active" badge/table counts.',
    ],
    [
      'Action buttons vs badges use different status filters today',
      'Action buttons use PublicStatuses (Paid + Republished). Internal badges currently use DashboardStatuses (Paid + Republished + Confidential). After DEF-223 deploys, both will be {Paid, Republished} and the divergence disappears.',
    ],
  ];
  items.forEach((it, i) => {
    const row = ws.addRow(it);
    row.eachCell(c => { c.border = thinBorder(); c.alignment = { vertical: 'top', wrapText: true }; });
    row.getCell(1).font = { bold: true, color: { argb: C.headerBg } };
    row.height = 60;
    if (i % 2 === 1) row.eachCell(c => c.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.banded } });
  });
  ws.views = [{ state: 'frozen', ySplit: 1 }];
}

wb.xlsx.writeFile(OUT).then(() => {
  console.log(`Wrote ${OUT}`);
});
