// QC #180 / DEF-200 — Dashboard tile counts must equal their export row counts.
//
// Walks every stat-card on /Dashboard, captures the displayed count and the
// data-stat-type, fetches /Dashboard/DownloadStat?type={type}, counts CSV
// data rows, asserts tile === export.
//
// Runs against the env passed in env var BASE_URL (default localhost).
// Also passes through TARGET to label outputs (local|live).

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

// CSV parser (same as qc182)
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

// Count data rows in a CSV that may have:
//   - 1 header row + N data rows  → return N
//   - report titles / blank lines before the header — find the row whose
//     first cell looks like a column header and count rows after it
function countDataRows(text) {
  const all = parseCsv(text);
  if (!all.length) return 0;
  // Find first row that "looks like a header": all cells non-empty, no commas, contains a letter
  // Conservative approach: assume row 0 is header unless first cell starts with "Report" or is empty.
  let headerIdx = 0;
  for (let i = 0; i < Math.min(all.length, 5); i++) {
    const first = (all[i][0] || '').trim();
    if (first && !/report|generated|^$/i.test(first) && all[i].length > 1) {
      headerIdx = i;
      break;
    }
  }
  return Math.max(0, all.length - headerIdx - 1);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    httpCredentials: BASIC,
    viewport: { width: 1440, height: 900 },
  });
  const page = await ctx.newPage();

  console.log(`\nQC #180 dashboard verification — ${TARGET.toUpperCase()} (${BASE})\n`);

  try {
    // Login as admin
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

    // Navigate to dashboard
    await page.goto(BASE + '/Dashboard', { waitUntil: 'networkidle' });
    // Wait 2500ms for the 0→N animation to finish (per DEF-200 callout)
    await page.waitForTimeout(2500);

    // Capture every stat-card
    const cards = await page.locator('.stat-card').evaluateAll(els =>
      els.map(el => ({
        title: (el.querySelector('.stat-title')?.innerText || '').trim(),
        value: (el.querySelector('.stat-value')?.innerText || '').trim(),
        statType: el.querySelector('.stat-download-icon')?.getAttribute('data-stat-type') || null,
      })).filter(c => c.statType)
    );

    console.log(`Captured ${cards.length} stat cards. Animation settled.\n`);
    await page.screenshot({ path: path.join(OUT, `qc180-${TARGET}-dashboard.png`), fullPage: true });

    const results = [];
    for (const card of cards) {
      const tileNum = Number((card.value || '0').replace(/[^\d.-]/g, ''));
      const url = `${BASE}/Dashboard/DownloadStat?type=${encodeURIComponent(card.statType)}`;
      const resp = await ctx.request.get(url);
      const status = resp.status();
      let exportRows = null;
      let csvBytes = 0;
      if (status === 200) {
        const text = await resp.text();
        csvBytes = text.length;
        exportRows = countDataRows(text);
        // Save the CSV for the record
        fs.writeFileSync(path.join(OUT, `qc180-${TARGET}-${card.statType}.csv`), text);
      }
      const pass = status === 200 && tileNum === exportRows;
      const tag = pass ? '✅' : '❌';
      console.log(`${tag} ${card.statType.padEnd(48)} title="${card.title}" tile=${tileNum} export=${exportRows} (HTTP ${status}, ${csvBytes}B)`);
      results.push({
        statType: card.statType,
        title: card.title,
        tile: tileNum,
        exportRows,
        httpStatus: status,
        pass,
      });
    }

    fs.writeFileSync(path.join(OUT, `qc180-${TARGET}-results.json`), JSON.stringify(results, null, 2));
    const pass = results.filter(r => r.pass).length;
    const fail = results.filter(r => !r.pass).length;
    console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL  (of ${results.length} stat cards)`);

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    await page.screenshot({ path: path.join(OUT, `qc180-${TARGET}-error.png`), fullPage: true }).catch(() => {});
  } finally {
    await browser.close();
  }
})();
