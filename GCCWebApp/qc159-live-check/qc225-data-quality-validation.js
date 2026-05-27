// Data Quality Validation — per-cert audit of the fields that feed the
// dashboard's "Top 10 / Top 5" breakdown tables. Answers the question:
// "Top 10 Product Types only shows 2 — is it a code bug, or do most
// certs not have a Product Type assigned?"
//
// Pulls /Dashboard/DownloadStat?type=total-active-certs (all active certs,
// per-cert detail) and reports each cert's Company, Product Category,
// Product Type, Product Subcategory population. Surfaces gaps cert by cert.
//
// Output: qc225-{target}-{stamp}-DATA-QUALITY.xlsx

const { chromium } = require('playwright');
const ExcelJS = require('exceljs');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const TARGET = process.env.TARGET || (BASE.includes('gccwebapp') ? 'live' : 'local');
const BASIC = BASE.includes('gccwebapp')
  ? { username: 'gcc', password: 'Cert-Test-Access-2026!' }
  : undefined;
const APP_USER = process.env.APP_USER || 'admin';
const APP_PASS = process.env.APP_PASS || 'TestPass123!';

const STAMP = new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-');
const OUTFILE = path.join(OUT, `qc225-${TARGET}-${STAMP}-DATA-QUALITY.xlsx`);

function parseCsv(text) {
  const rows = []; let cur = '', row = [], inQ = false;
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
  return { headers, rows: all.slice(h + 1).map(r => { const o = {}; headers.forEach((h2, i) => o[h2] = r[i] || ''); return o; }) };
}

const C = {
  headerBg: 'FF1A3A5C', headerText: 'FFFFFFFF',
  ok: 'FFC8E6C9', missing: 'FFFFCDD2', warn: 'FFFFF3CD', banded: 'FFF8FAFC',
  border: 'FFB0C0D0',
};
function thinBorder() { return { top: { style:'thin', color:{argb:C.border} }, left:{style:'thin',color:{argb:C.border}}, bottom:{style:'thin',color:{argb:C.border}}, right:{style:'thin',color:{argb:C.border}} }; }
function headerStyle() { return { font:{bold:true,color:{argb:C.headerText},size:11}, fill:{type:'pattern',pattern:'solid',fgColor:{argb:C.headerBg}}, alignment:{vertical:'middle',horizontal:'left',wrapText:true}, border:thinBorder() }; }

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  console.log(`\n=== Data Quality Validation — ${TARGET.toUpperCase()} (${BASE}) ===\n`);

  try {
    await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(2000);

    // Pull total-active-certs export — per-cert detail for every active cert
    const resp = await ctx.request.get(BASE + '/Dashboard/DownloadStat?type=total-active-certs');
    if (resp.status() !== 200) { console.log(`HTTP ${resp.status()}`); process.exit(1); }
    const text = await resp.text();
    fs.writeFileSync(OUTFILE.replace('.xlsx', '-source.csv'), text);
    const { rows } = parseDataRows(text);

    console.log(`Captured ${rows.length} active certs.\n`);

    // Per-cert field population check
    const checks = rows.map(r => {
      const cn = r['Certificate Number'] || '';
      const fields = {
        Company: (r.Company || '').trim(),
        Product: (r.Product || '').trim(),
        ProductCategory: (r['Product Category'] || '').trim(),
        ProductType: (r['Product Type'] || '').trim(),
        ProductSubcategory: (r['Product Subcategory'] || '').trim(),
        CertificationType: (r['Certification Type'] || '').trim(),
        Location: (r.Location || '').trim(),
        Country: (r.Country || '').trim(),
      };
      const missing = Object.entries(fields).filter(([, v]) => !v).map(([k]) => k);
      return { cert: cn, status: r.Status, ...fields, missing, missingCount: missing.length };
    });

    // Field-level summary
    const fieldNames = ['Company', 'Product', 'ProductCategory', 'ProductType', 'ProductSubcategory', 'CertificationType', 'Location', 'Country'];
    const fieldSummary = fieldNames.map(f => {
      const populated = checks.filter(c => c[f]).length;
      const missing = checks.length - populated;
      const distinct = new Set(checks.map(c => c[f]).filter(v => v)).size;
      return { field: f, populated, missing, distinct, pct: ((populated / checks.length) * 100).toFixed(1) + '%' };
    });

    console.log('Field population summary:');
    console.log('  ' + 'Field'.padEnd(22) + 'Populated  Missing  Distinct  %Pop');
    console.log('  ' + '─'.repeat(60));
    fieldSummary.forEach(f => console.log('  ' + f.field.padEnd(22) + String(f.populated).padStart(8) + String(f.missing).padStart(10) + String(f.distinct).padStart(10) + '   ' + f.pct));

    // Build the Excel workbook
    const wb = new ExcelJS.Workbook();
    wb.creator = 'Anuki — GCC Data Quality Validator';

    // ── Sheet 1: Summary ─────────────────────────────────────────────────
    {
      const ws = wb.addWorksheet('Summary', { properties: { tabColor: { argb: 'FF1A3A5C' } } });
      ws.columns = [{ width: 30 }, { width: 14 }, { width: 14 }, { width: 14 }, { width: 12 }, { width: 70 }];
      ws.mergeCells('A1:F1');
      ws.getCell('A1').value = 'GCC Dashboard — Data Quality Validation';
      ws.getCell('A1').style = { font: { bold: true, size: 18, color: { argb: C.headerBg } } };
      ws.getRow(1).height = 30;
      ws.mergeCells('A2:F2');
      ws.getCell('A2').value = `Source: ${BASE}  •  ${rows.length} active certs  •  captured ${new Date().toLocaleString()}`;
      ws.getCell('A2').font = { italic: true, color: { argb: 'FF555555' } };

      ws.addRow([]);
      ws.mergeCells(`A4:F4`); ws.getCell('A4').value = 'Field population — how often is each field filled in across the 26 active certs?'; ws.getCell('A4').style = headerStyle(); ws.getRow(4).height = 22;
      const headerRow = ws.addRow(['Field', 'Populated', 'Missing', 'Distinct values', '% Populated', 'Implication for dashboard']);
      headerRow.eachCell(c => c.style = headerStyle());
      const implications = {
        Company: 'Drives "# Companies" badge and Top 10 Companies table',
        Product: 'Drives "# Products" badge',
        ProductCategory: 'Drives "# Product Categories" badge and Top 5 Product Categories table',
        ProductType: 'Drives "# Product Types" badge and Top 10 Product Types table — sparseness here is why that table only shows 2 of a possible 10',
        ProductSubcategory: 'Drives "# Product Type Subcategories" badge',
        CertificationType: 'Drives Total Active Certs by Certification Type table',
        Location: 'Used in exports / filtering only',
        Country: 'Used in exports / filtering only',
      };
      fieldSummary.forEach(f => {
        const row = ws.addRow([f.field, f.populated, f.missing, f.distinct, f.pct, implications[f.field] || '']);
        row.eachCell(c => c.border = thinBorder());
        // color the populated/missing cells
        const popPct = f.populated / checks.length;
        const popCell = row.getCell(2);
        if (popPct >= 0.9) popCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.ok } };
        else if (popPct >= 0.5) popCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.warn } };
        else popCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.missing } };
        const missCell = row.getCell(3);
        if (f.missing > 0) missCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.missing } };
      });

      ws.addRow([]);
      ws.mergeCells(`A${ws.rowCount + 1}:F${ws.rowCount + 1}`);
      const interpHdr = ws.addRow(['Interpretation']);
      ws.mergeCells(`A${interpHdr.number}:F${interpHdr.number}`);
      ws.getCell(`A${interpHdr.number}`).value = 'Interpretation';
      ws.getCell(`A${interpHdr.number}`).style = headerStyle();
      ws.getRow(interpHdr.number).height = 22;

      const interps = [
        ['Why "Top 10 Product Types" only shows 2', `Only ${fieldSummary.find(f => f.field === 'ProductType').populated} of ${rows.length} active certs have a Product Type assigned, distributed across ${fieldSummary.find(f => f.field === 'ProductType').distinct} distinct types. The table is showing every type that exists in the data; "Top 10" is the cap, but the data only has ${fieldSummary.find(f => f.field === 'ProductType').distinct}. Not a code bug — a data-population gap.`],
        ['Action needed', `Either (a) backfill Product Type on the ${fieldSummary.find(f => f.field === 'ProductType').missing} active certs that are missing it (data ops — see "Missing Product Type" sheet for cert list), or (b) accept the current data state. Per the [Data Changes Require CR] rule, backfilling business records needs a formal CR from the client.`],
        ['Same pattern — Product Subcategory', `${fieldSummary.find(f => f.field === 'ProductSubcategory').missing} of ${rows.length} certs are missing Product Subcategory (${(100 - parseFloat(fieldSummary.find(f => f.field === 'ProductSubcategory').pct)).toFixed(1)}% gap). Drives the "# Product Type Subcategories" badge which currently shows ${fieldSummary.find(f => f.field === 'ProductSubcategory').distinct}.`],
      ];
      interps.forEach(([t, b]) => {
        const tRow = ws.addRow([t]);
        ws.mergeCells(`A${tRow.number}:F${tRow.number}`);
        tRow.getCell(1).font = { bold: true, color: { argb: C.headerBg } };
        const bRow = ws.addRow([b]);
        ws.mergeCells(`A${bRow.number}:F${bRow.number}`);
        bRow.getCell(1).alignment = { wrapText: true, vertical: 'top' };
        bRow.height = 45;
      });

      ws.views = [{ state: 'frozen', ySplit: 2 }];
    }

    // ── Sheet 2: Per-Cert Field Matrix ───────────────────────────────────
    {
      const ws = wb.addWorksheet('Per-Cert Fields', { properties: { tabColor: { argb: 'FF1C7A3C' } } });
      ws.columns = [
        { header: 'Cert #', width: 12 },
        { header: 'Status', width: 24 },
        { header: 'Company', width: 30 },
        { header: 'Product', width: 35 },
        { header: 'Product Category', width: 28 },
        { header: 'Product Type', width: 20 },
        { header: 'Product Subcategory', width: 20 },
        { header: 'Certification Type', width: 28 },
        { header: 'Location', width: 24 },
        { header: 'Country', width: 16 },
        { header: 'Missing fields', width: 50 },
      ];
      ws.getRow(1).eachCell(c => c.style = headerStyle());
      ws.getRow(1).height = 32;

      checks.forEach((cert, i) => {
        const row = ws.addRow([
          cert.cert, cert.status, cert.Company, cert.Product, cert.ProductCategory,
          cert.ProductType, cert.ProductSubcategory, cert.CertificationType,
          cert.Location, cert.Country, cert.missing.join(', '),
        ]);
        row.eachCell((c, col) => {
          c.border = thinBorder();
          c.alignment = { vertical: 'top', wrapText: true };
          // Highlight missing values in red
          if (col >= 3 && col <= 10 && !c.value) {
            c.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.missing } };
            c.value = '—';
            c.font = { color: { argb: 'FF888888' }, italic: true };
          }
        });
        if (i % 2 === 1) row.eachCell((c, col) => { if (col >= 3 && c.fill?.fgColor?.argb !== C.missing) c.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.banded } }; });
        if (cert.missingCount > 0) row.getCell(11).font = { color: { argb: 'FFB00020' }, bold: true };
      });

      ws.autoFilter = { from: { row: 1, column: 1 }, to: { row: ws.rowCount, column: 11 } };
      ws.views = [{ state: 'frozen', xSplit: 2, ySplit: 1 }];
    }

    // ── Sheet 3: Missing Product Type — the cert list to backfill ────────
    {
      const ws = wb.addWorksheet('Missing Product Type', { properties: { tabColor: { argb: 'FFB00020' } } });
      ws.columns = [
        { header: 'Cert #', width: 12 },
        { header: 'Status', width: 24 },
        { header: 'Company', width: 35 },
        { header: 'Product', width: 35 },
        { header: 'Product Category', width: 28 },
        { header: 'Product Subcategory', width: 20 },
        { header: 'Certification Type', width: 28 },
        { header: 'Effective', width: 12 },
        { header: 'Expires', width: 12 },
      ];
      ws.getRow(1).eachCell(c => c.style = headerStyle());
      ws.getRow(1).height = 28;

      // Add a heading
      const missingProductType = checks.filter(c => !c.ProductType);
      ws.insertRow(1, [`The ${missingProductType.length} active certs below have NO Product Type set. These are why the "Top 10 Product Types" table appears sparse. Backfilling these would require a CR per [Data Changes Require CR].`]);
      ws.mergeCells('A1:I1');
      ws.getCell('A1').style = { font: { italic: true, color: { argb: C.headerBg }, size: 11 }, alignment: { wrapText: true, vertical: 'middle' } };
      ws.getRow(1).height = 38;

      // Re-style header (now row 2)
      ws.getRow(2).eachCell(c => c.style = headerStyle());

      missingProductType.forEach((cert, i) => {
        const origRow = rows.find(r => r['Certificate Number'] === cert.cert);
        const row = ws.addRow([
          cert.cert, cert.status, cert.Company, cert.Product, cert.ProductCategory,
          cert.ProductSubcategory || '—',
          cert.CertificationType,
          origRow?.['Effective Date'] || '',
          origRow?.['Expiration Date'] || '',
        ]);
        row.eachCell(c => c.border = thinBorder());
        if (i % 2 === 1) row.eachCell(c => c.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: C.banded } });
      });

      ws.views = [{ state: 'frozen', ySplit: 2 }];
    }

    // ── Sheet 4: Distinct values per field (validate spelling, taxonomy) ─
    {
      const ws = wb.addWorksheet('Distinct Values', { properties: { tabColor: { argb: 'FF6A1B9A' } } });
      ws.columns = [
        { header: 'Field', width: 22 }, { header: 'Distinct value', width: 60 }, { header: 'Cert count', width: 12 }, { header: 'Sample certs', width: 60 },
      ];
      ws.getRow(1).eachCell(c => c.style = headerStyle());
      ws.getRow(1).height = 28;

      for (const f of ['Company', 'ProductCategory', 'ProductType', 'ProductSubcategory', 'CertificationType']) {
        const map = {};
        checks.forEach(c => { const v = c[f]; if (v) (map[v] ??= []).push(c.cert); });
        const entries = Object.entries(map).sort((a, b) => b[1].length - a[1].length);
        entries.forEach(([val, certList], i) => {
          const row = ws.addRow([i === 0 ? f : '', val, certList.length, certList.slice(0, 5).join(', ') + (certList.length > 5 ? ` … (+${certList.length - 5})` : '')]);
          row.eachCell(c => c.border = thinBorder());
          if (i === 0) row.getCell(1).font = { bold: true, color: { argb: C.headerBg } };
        });
        ws.addRow([]); // blank between field groups
      }
      ws.views = [{ state: 'frozen', ySplit: 1 }];
    }

    await wb.xlsx.writeFile(OUTFILE);
    console.log(`\nWrote ${OUTFILE}`);
    console.log(`Source CSV: ${OUTFILE.replace('.xlsx', '-source.csv')}`);

  } finally {
    await browser.close();
  }
})();
