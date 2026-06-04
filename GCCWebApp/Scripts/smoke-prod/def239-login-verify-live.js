// DEF-239 — verify login works on gccwebapp.com AFTER the prod schema fix
// (added Users.UserCompanyId via the two missing migrations 2026-06-03).
//
// Before the fix EVERY login returned the generic "Invalid email or password"
// because the login query SELECTed a column that didn't exist on prod and the
// DEF-209 catch masked the SQL error. This drives a REAL login for each seeded
// test account and checks for an actual authenticated redirect, not just absence
// of the error string.
//
// LIVE TEST. Run:
//   SMOKE_REMOTE_USER=gcc SMOKE_REMOTE_PASS="Cert-Test-Access-2026!" \
//   node Scripts/smoke-prod/def239-login-verify-live.js
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = (process.env.SMOKE_REMOTE_USER && process.env.SMOKE_REMOTE_PASS)
  ? { username: process.env.SMOKE_REMOTE_USER, password: process.env.SMOKE_REMOTE_PASS }
  : { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const OUT = path.join(__dirname, '_results');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

// Seeded prod accounts (confirmed present + active in __EFMigrationsHistory check).
const ACCOUNTS = [
  { role: 'admin',    email: 'admin',    password: 'TestPass123!' },
  { role: 'analyst',  email: 'analyst',  password: 'TestPass123!' },
  { role: 'external', email: 'external', password: 'TestPass123!' },
  { role: 'partner',  email: 'partner.amazon', password: 'TestPass123!' },
];

const results = [];
const rec = (id, status, detail = '') => {
  results.push({ id, status, detail });
  console.log(`[${status}] ${id}${detail ? '  — ' + detail : ''}`);
};

(async () => {
  const browser = await chromium.launch();
  try {
    for (const acct of ACCOUNTS) {
      const ctx = await browser.newContext({
        viewport: { width: 1440, height: 900 },
        httpCredentials: GATE,
        ignoreHTTPSErrors: true,
      });
      const page = await ctx.newPage();
      try {
        await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
        await page.locator('#Email').fill(acct.email);
        await page.locator('#Password').fill(acct.password);
        await Promise.all([
          page.waitForLoadState('domcontentloaded'),
          page.locator('button[type="submit"]').first().click(),
        ]);
        await page.waitForTimeout(1500);

        const url = page.url();
        const bodyText = await page.evaluate(() => document.body.innerText);
        const hasError = /invalid email or password/i.test(bodyText);
        const stillOnLogin = /\/Account\/Login/i.test(url);
        // Success = navigated away from the login page AND no error banner.
        const ok = !stillOnLogin && !hasError;
        rec(`login:${acct.role}`, ok ? 'PASS' : 'FAIL',
          `landed=${url.replace(BASE, '') || '/'}${hasError ? ' [error banner present]' : ''}`);

        await page.screenshot({ path: path.join(OUT, `def239-login-${acct.role}.png`), fullPage: true });
      } catch (e) {
        rec(`login:${acct.role}`, 'ERROR', e.message.split('\n')[0]);
      } finally {
        await ctx.close();
      }
    }
  } finally {
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const summary = { ts: new Date().toISOString(), base: BASE, pass, total: results.length, results };
  fs.writeFileSync(path.join(OUT, 'def239-login-results.json'), JSON.stringify(summary, null, 2));
  console.log(`\n[def239] ${pass}/${results.length} PASS against ${BASE}`);
  process.exit(pass === results.length ? 0 : 1);
})();
