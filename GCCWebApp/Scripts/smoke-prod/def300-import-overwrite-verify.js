// DEF-300 verification — CSV import overwrite path from the Internal tile view.
//
// Repro of Shawn's 2026-06-25 report: re-importing a CSV that matches existing
// certificates dead-ended on the overwrite-acknowledgement guard ("Error/OK", no way
// to proceed). The server was correct (returns requiresOverwriteAck so the client can
// prompt + retry with acknowledgeOverwrites:true); the modal JS never handled it.
//
// This script drives the REAL modal end-to-end:
//   1. export an existing cert to CSV (guaranteed matching cert#),
//   2. tweak one field (Location) so the row is detected as Modified → overwrite,
//   3. open the import modal, upload, Apply, acknowledge the overwrite,
//   4. assert the two /api/CertificateImport/Apply calls: first requiresOverwriteAck,
//      second success:true.
//
// LOCALHOST ONLY. Mutates one cert's Location on the dev DB (auto-backup is taken
// server-side; the cert# touched is printed).
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const { login, BASE } = require('./_lib/smoke-template');

if (BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) {
  console.error('REFUSING to run against a remote env. Localhost only.');
  process.exit(2);
}

// --- minimal RFC4180 CSV parse/serialize (handles quotes, commas, embedded newlines) ---
function parseCsv(text) {
  const records = [];
  let field = '', record = [], inQuotes = false;
  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (inQuotes) {
      if (c === '"') {
        if (text[i + 1] === '"') { field += '"'; i++; }
        else inQuotes = false;
      } else field += c;
    } else {
      if (c === '"') inQuotes = true;
      else if (c === ',') { record.push(field); field = ''; }
      else if (c === '\r') { /* skip */ }
      else if (c === '\n') { record.push(field); records.push(record); record = []; field = ''; }
      else field += c;
    }
  }
  if (field.length > 0 || record.length > 0) { record.push(field); records.push(record); }
  return records.filter(r => r.length > 1 || (r.length === 1 && r[0] !== ''));
}
function toCsvLine(fields) {
  return fields.map(f => `"${String(f ?? '').replace(/"/g, '""')}"`).join(',');
}

(async () => {
  const browser = await chromium.launch({ headless: process.env.SMOKE_HEADLESS !== '0' });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await ctx.newPage();

  const applyResponses = [];
  const consoleErrors = [];
  page.on('console', m => {
    if (m.type() !== 'error') return;
    const t = m.text();
    // The overwrite guard intentionally replies 400 (requiresOverwriteAck); the browser logs
    // that as a generic "Failed to load resource: ... 400". That is the expected response our
    // fix consumes, NOT a real error — exclude it so the smoke reflects actual failures only.
    if (/Failed to load resource.*400/i.test(t)) return;
    consoleErrors.push(t);
  });
  page.on('response', async (resp) => {
    if (resp.url().includes('/api/CertificateImport/Apply')) {
      let body = null;
      try { body = await resp.json(); } catch { /* non-json */ }
      applyResponses.push({ status: resp.status(), body });
    }
  });

  let fail = 0;
  const check = (name, ok, detail = '') => {
    console.log(`  [${ok ? 'PASS' : 'FAIL'}] ${name}${detail ? ' - ' + detail : ''}`);
    if (!ok) fail++;
  };

  const tmpCsv = path.join(require('os').tmpdir(), `def300-overwrite-${Date.now()}.csv`);

  try {
    await login(page, 'admin');

    // 1. Grab one existing cert id from the internal tiles API (fast — avoids the slow
    // full-table export, which can hit a 30s SQL timeout and is a separate perf issue).
    const tilesResp = await page.request.get(BASE + '/api/certificate-tiles-internal?page=1&pageSize=1', { timeout: 60000 });
    check('tiles API 200', tilesResp.status() === 200, `status ${tilesResp.status()}`);
    const tilesJson = await tilesResp.json();
    const list = tilesJson.certificates || tilesJson.data || tilesJson.items || (Array.isArray(tilesJson) ? tilesJson : []);
    const firstTile = list[0] || {};
    const certId = firstTile.id ?? firstTile.Id;
    check('tiles API returned a cert id', certId != null, `id=${certId}`);
    if (certId == null) throw new Error('No cert id from tiles API; shape=' + JSON.stringify(tilesJson).slice(0, 300));

    // Export just that one cert (admin → includes internal-tag column for full round-trip).
    const exp = await page.request.get(`${BASE}/Certificate/ExportCertificates?format=csv&ids=${certId}`, { timeout: 60000 });
    check('export endpoint 200', exp.status() === 200, `status ${exp.status()}`);
    const csvText = await exp.text();
    const rows = parseCsv(csvText);
    check('export has at least one data row', rows.length >= 2, `${rows.length - 1} data rows`);

    const header = rows[0];
    const dataRow = rows[1].slice();
    const certCol = header.findIndex(h => /^(certificate #|cert #|certificate_number|certificatenumber)$/i.test(h.trim()));
    const locCol = header.findIndex(h => /^location$/i.test(h.trim()));
    check('found Certificate # column', certCol >= 0, `index ${certCol}`);
    check('found Location column', locCol >= 0, `index ${locCol}`);
    if (certCol < 0 || locCol < 0) throw new Error('Required columns not found in export header: ' + header.join('|'));

    const certNumber = dataRow[certCol];
    const newLocation = 'DEF300 Overwrite Test ' + new Date().toISOString().slice(11, 19);
    dataRow[locCol] = newLocation;
    console.log(`  -> Using cert# ${certNumber}; setting Location to "${newLocation}"`);

    // Single-row file = overwrite exactly one existing cert.
    fs.writeFileSync(tmpCsv, toCsvLine(header) + '\r\n' + toCsvLine(dataRow) + '\r\n', 'utf8');

    // 3. Drive the modal.
    await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(1000);
    await page.locator('#openImportModalBtn').click();
    await page.waitForTimeout(300);
    await page.locator('#importFileInput').setInputFiles(tmpCsv);
    await page.waitForTimeout(200);
    await page.locator('#uploadImportBtn').click();

    // Wait for preview (step 2 visible + stats populated).
    await page.waitForSelector('#importStep2', { state: 'visible', timeout: 30000 });
    await page.waitForTimeout(800);
    const statModified = parseInt(await page.locator('#statModified').textContent(), 10) || 0;
    const statErrors = parseInt(await page.locator('#statErrors').textContent(), 10) || 0;
    check('preview shows 1 modified cert (overwrite pending)', statModified >= 1, `modified=${statModified}`);
    check('preview shows no row errors', statErrors === 0, `errors=${statErrors}`);

    // 4. Apply → "Confirm Import" (warning) → confirm.
    await page.locator('#applyImportBtn').click();
    await page.waitForSelector('[data-action="confirm"]', { state: 'visible', timeout: 10000 });
    await page.waitForTimeout(300);
    await page.locator('[data-action="confirm"]').last().click();

    // 5. Overwrite acknowledgement prompt should now appear (THE FIX). Confirm it.
    let overwritePromptSeen = false;
    try {
      await page.waitForFunction(() => {
        const t = document.body.innerText || '';
        return /Overwrite existing certificate/i.test(t);
      }, { timeout: 10000 });
      overwritePromptSeen = true;
    } catch { /* handled by assertion below */ }
    check('overwrite-acknowledgement prompt is shown (not a dead-end error)', overwritePromptSeen);

    if (overwritePromptSeen) {
      await page.waitForSelector('[data-action="confirm"]', { state: 'visible', timeout: 5000 });
      await page.waitForTimeout(300);
      await page.locator('[data-action="confirm"]').last().click();
    }

    // 6. Wait for the second Apply call to resolve.
    await page.waitForTimeout(2500);

    // --- Assert the two Apply API calls ---
    check('first Apply returned requiresOverwriteAck',
      applyResponses[0] && applyResponses[0].body && applyResponses[0].body.requiresOverwriteAck === true,
      JSON.stringify(applyResponses[0]?.body));
    const second = applyResponses[1];
    check('second Apply (after ack) succeeded',
      !!(second && second.body && second.body.success === true),
      JSON.stringify(second?.body));
    if (second && second.body && second.body.success) {
      check('server reported 1 certificate updated', second.body.certificatesUpdated >= 1,
        `updated=${second.body.certificatesUpdated}`);
      check('server reported a backup was taken', (second.body.backupCertificateCount || 0) >= 1,
        `backup=${second.body.backupCertificateCount} file=${second.body.backupFileName || 'n/a'}`);
    }

    // 7. Confirm the Location actually changed via a fresh export.
    const exp2 = await page.request.get(`${BASE}/Certificate/ExportCertificates?format=csv&ids=${certId}`, { timeout: 60000 });
    check('re-export endpoint 200', exp2.status() === 200, `status ${exp2.status()}`);
    const rows2 = parseCsv(await exp2.text());
    const updated = rows2[1]; // export-by-id → exactly one data row = our cert
    check('cert Location persisted to new value', !!updated && updated[locCol] === newLocation,
      updated ? `now "${updated[locCol]}"` : `re-export had ${rows2.length - 1} data rows`);

    check('no console errors during flow', consoleErrors.length === 0, consoleErrors.slice(0, 3).join(' | '));

  } catch (e) {
    check('script ran without throwing', false, e.message);
    try { await page.screenshot({ path: path.join(__dirname, '_results', `def300-fatal-${Date.now()}.png`), fullPage: true }); } catch {}
  } finally {
    try { fs.unlinkSync(tmpCsv); } catch {}
    await browser.close();
  }

  console.log(`\nDEF-300 overwrite verify: ${fail === 0 ? 'ALL PASS' : fail + ' FAIL'}`);
  process.exit(fail === 0 ? 0 : 1);
})();
