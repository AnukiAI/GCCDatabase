// SECURITY — IDOR on bulk download (P0). Promotes def236-idor-verify.js. The External user
// (company scope = "Henkel") POSTs a wide ID range to /Certificate/DownloadSelectedZip; the
// returned ZIP must contain ONLY certs that user is authorized for (own company + PublicStatuses
// + not expired) — not the pre-fix cross-company dump (32 entries / ~12.6 MB).
//
// PASS = HTTP 400 (none of the ids downloadable) OR HTTP 200 with entryCount < pre-fix leak
//        AND entryCount <= the user's tile-API authorized count.
//
// Read-only (a download is a read). Run:  node Scripts/smoke-prod/_security/idor-download.js

const H = require('./_lib/security-harness');

const IDS = Array.from({ length: 60 }, (_, i) => i + 1); // [1..60]
const PREFIX_LEAK = 32; // pre-fix vulnerable entry count (baseline for regression)

// Count ZIP entries via the End-Of-Central-Directory record — no deps.
function zipEntryCount(buf) {
  for (let i = buf.length - 22; i >= Math.max(0, buf.length - 22 - 65535); i--) {
    if (buf.readUInt32LE(i) === 0x06054b50) return buf.readUInt16LE(i + 10);
  }
  return -1;
}

async function run() {
  H.guardRemote();
  console.log(`\n[security] idor-download — ${H.BASE}\n`);
  const { results, record } = H.makeRecorder('idor-download');
  const browser = await H.chromium.launch({ headless: true });

  try {
    const ctx = await H.newContext(browser);
    const { ok } = await H.login(ctx, 'external');
    if (!ok) {
      record('idor.login', 'external login', 'BLOCKED', 'login failed — check external test account');
      await ctx.close();
      return H.summarize('idor-download', '/Certificate/DownloadSelectedZip', results);
    }
    record('idor.login', 'external login', 'PASS');

    // Baseline authorized count from the tile API.
    const tile = await H.apiJson(ctx, '/api/CertificateTileExternal?page=1&pageSize=100');
    const authorized = tile.total;

    // The attack: POST a wide ID range to the bulk-zip endpoint.
    const resp = await ctx.request.post(H.BASE + '/Certificate/DownloadSelectedZip', {
      data: IDS,
      headers: { 'Content-Type': 'application/json' },
    });
    const code = resp.status();
    let entryCount = null, sizeKB = null;
    if (code === 200) {
      const body = await resp.body();
      sizeKB = Math.round(body.length / 1024);
      entryCount = zipEntryCount(body);
    }

    let verdict, notes;
    if (code === 400) {
      verdict = 'PASS';
      notes = 'HTTP 400 — none of [1..60] downloadable for this user (closed state)';
    } else if (code === 200 && entryCount >= 0 && entryCount < PREFIX_LEAK &&
               (authorized == null || entryCount <= authorized)) {
      verdict = 'PASS';
      notes = `HTTP 200, zip entries=${entryCount} (< pre-fix ${PREFIX_LEAK}, <= authorized ${authorized}), ${sizeKB}KB`;
    } else {
      verdict = 'FAIL';
      notes = `HTTP ${code}, zip entries=${entryCount}, authorized=${authorized} — POSSIBLE CROSS-COMPANY LEAK`;
    }
    record('idor.bulk-zip', 'DownloadSelectedZip respects company scope', verdict, notes);
    console.log(`  (pre-fix baseline: 200, ${PREFIX_LEAK} entries, ~12.6MB cross-company dump)`);

    await ctx.close();
  } catch (e) {
    record('idor.error', 'spec ran without throwing', 'FAIL', e.message);
  } finally {
    await browser.close();
  }

  return H.summarize('idor-download', '/Certificate/DownloadSelectedZip', results);
}

module.exports = { run };
if (require.main === module) {
  run().then((s) => process.exit(s.fail || s.blocked ? 1 : 0));
}
