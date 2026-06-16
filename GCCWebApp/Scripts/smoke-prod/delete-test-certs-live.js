// One-off: delete the two throwaway test certs created on gccwebapp.com while diagnosing
// DEF-261/262. HARDCODED to exactly these two numbers — no discovery, no loops over other data.
// Uses the app's real delete endpoint (POST /Certificate/Delete/{number}) with the page's
// antiforgery token, same as the Edit page's Delete button.
//
// Run:
//   SMOKE_REMOTE_USER=gcc SMOKE_REMOTE_PASS="Cert-Test-Access-2026!" \
//   node Scripts/smoke-prod/delete-test-certs-live.js

const { chromium } = require('playwright');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = { username: process.env.SMOKE_REMOTE_USER || 'gcc', password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' };
const LOGIN = { email: process.env.APP_USER || 'admin', password: process.env.APP_PASS || 'TestPass123!' };

// Explicit allow-list. Nothing else will ever be deleted by this script.
const TARGETS = ['26-0176', '26-0177'];

const log = (s) => console.log(s);

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, httpCredentials: GATE, ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  try {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.locator('#Email').fill(LOGIN.email);
    await page.locator('#Password').fill(LOGIN.password);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1200);
    log(`Logged in as ${LOGIN.email} → ${page.url()}`);

    for (const number of TARGETS) {
      log(`\n--- ${number} ---`);
      // Open the Edit page (carries the antiforgery token) and confirm the cert exists.
      const resp = await page.goto(`${BASE}/Certificate/Edit/${number}`, { waitUntil: 'networkidle', timeout: 60000 });
      const exists = await page.evaluate(() => !!document.getElementById('certificateEditForm'));
      if (!exists) {
        log(`  SKIP — Edit page did not load a form (HTTP ${resp && resp.status()}). Cert may already be gone.`);
        continue;
      }
      // Safety: confirm the page is really for THIS number before deleting.
      const onPageNumber = await page.evaluate(() => document.getElementById('Number')?.value || '');
      if (onPageNumber.trim() !== number) {
        log(`  ABORT — Edit page shows number "${onPageNumber}", expected "${number}". Not deleting.`);
        continue;
      }
      // Fire the exact delete request the Delete button uses.
      const result = await page.evaluate(async (num) => {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const r = await fetch(`/Certificate/Delete/${encodeURIComponent(num)}`, {
          method: 'POST',
          headers: { 'Accept': 'application/json', 'RequestVerificationToken': token },
        });
        let body = null; try { body = await r.json(); } catch (_) {}
        return { status: r.status, ok: r.ok, body };
      }, number);
      log(`  DELETE → HTTP ${result.status} ok=${result.ok} success=${result.body?.success} msg=${result.body?.message || ''}`);

      // Verify it's gone.
      const verify = await page.goto(`${BASE}/Certificate/Edit/${number}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
      const stillThere = await page.evaluate(() => !!document.getElementById('certificateEditForm'));
      log(`  VERIFY → reopen Edit/${number}: HTTP ${verify && verify.status()} formPresent=${stillThere} ${stillThere ? '❌ STILL EXISTS' : '✅ gone'}`);
    }
  } catch (e) {
    log(`ERROR: ${e.message}`);
  } finally {
    await browser.close();
  }
})();
