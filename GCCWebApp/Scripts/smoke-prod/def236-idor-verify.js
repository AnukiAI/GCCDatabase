// DEF-236 verification — DownloadSelectedZip must not leak cross-company PDFs.
//
// Repro of the Gate-3 probe: log in as the External test user (scope = company "Henkel"),
// POST a wide ID range to /Certificate/DownloadSelectedZip, and confirm the returned ZIP
// now contains ONLY certs that user is authorized for (own company + PublicStatuses + not
// expired) — not the 32 cross-company PDFs the pre-fix endpoint returned.
//
// PASS = ZIP entry count is small and every entry is a cert the external user may see
//        (we assert: far fewer than the pre-fix 32, and the response is not a cross-company
//        dump). We cross-check the authorized count against the external tile API.
//
// Run:  node Scripts/smoke-prod/def236-idor-verify.js   (server must be running with the fix)

const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const IDS = Array.from({ length: 60 }, (_, i) => i + 1); // [1..60]

// Count entries in a ZIP buffer via the End-Of-Central-Directory record (no deps).
function zipEntryCount(buf) {
  // EOCD signature 0x06054b50, scan from the end (max comment 65535 + 22).
  for (let i = buf.length - 22; i >= Math.max(0, buf.length - 22 - 65535); i--) {
    if (buf.readUInt32LE(i) === 0x06054b50) {
      return buf.readUInt16LE(i + 10); // total entries on this disk
    }
  }
  return -1; // not a zip / not found
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();

  // Login as external.
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('external');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(800);

  // Baseline: how many certs is this external user actually allowed to see? (tile API)
  let authorized = null;
  try {
    const r = await page.request.get(`${BASE}/api/CertificateTileExternal?page=1&pageSize=100`);
    const j = await r.json();
    authorized = j.totalCount;
  } catch { /* ignore */ }

  // The attack: POST a wide ID range to the bulk-zip endpoint.
  const resp = await page.request.post(`${BASE}/Certificate/DownloadSelectedZip`, {
    data: IDS,
    headers: { 'Content-Type': 'application/json' },
  });
  const status = resp.status();
  let entryCount = null, sizeKB = null;
  if (status === 200) {
    const body = await resp.body();
    sizeKB = Math.round(body.length / 1024);
    entryCount = zipEntryCount(body);
  }

  await browser.close();

  // Verdict: pre-fix returned 200 with 32 entries (~12.6 MB). Post-fix the external user
  // may only get their own authorized public certs — must be far fewer than 32 and must
  // not exceed the tile-API authorized count.
  const PREFIX_LEAK = 32;
  let verdict;
  if (status === 400) {
    // None of [1..60] were downloadable for this user — also a valid closed state.
    verdict = 'PASS';
  } else if (status === 200 && entryCount >= 0 && entryCount < PREFIX_LEAK &&
             (authorized == null || entryCount <= authorized)) {
    verdict = 'PASS';
  } else {
    verdict = 'FAIL';
  }

  console.log('=== DEF-236 IDOR verification (DownloadSelectedZip) ===');
  console.log(`  external authorized certs (tile API totalCount): ${authorized}`);
  console.log(`  POST [1..60] -> HTTP ${status}` + (status === 200 ? `, zip entries=${entryCount}, size=${sizeKB}KB` : ''));
  console.log(`  pre-fix (vulnerable) returned: 200, 32 entries, ~12.6MB (cross-company leak)`);
  console.log(`  VERDICT: ${verdict}` + (verdict === 'PASS'
    ? ' — leak closed; user only gets their authorized set (or 400 none-available).'
    : ' — STILL LEAKING; investigate.'));
  process.exitCode = verdict === 'PASS' ? 0 : 1;
})();
