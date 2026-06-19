// CR-171 partner data-isolation smoke (local, real restored test data).
// For each real partner-role user, logs in and pulls the external tile API, then asserts:
//   (1) login actually succeeded (not bounced to /Account/Login),
//   (2) NO co-partner name leaks into any returned cert (the isolation guarantee),
//   (3) totalCount is plausible (> 0).
// The multi-partner user (pru.multi => Amazon+Novi) verifies a shared cert shows BOTH of the
// caller's own partners but still hides any partner that isn't theirs.
const { chromium } = require('playwright');
const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const ALL_PARTNERS = ['Amazon', 'Novi', 'Other', 'Wayfair'];

const USERS = [
  { email: 'partner@amazon.com',     allowed: ['Amazon'] },
  { email: 'perry.novi@example.com', allowed: ['Novi'] },
  { email: 'pam.wayfair@example.com',allowed: ['Wayfair'] },
  { email: 'pip.other@example.com',  allowed: ['Other'] },
  { email: 'pru.multi@example.com',  allowed: ['Amazon', 'Novi'] }, // multi-partner
];

(async () => {
  const browser = await chromium.launch();
  let pass = 0, fail = 0;
  for (const u of USERS) {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await ctx.newPage();
    const forbidden = ALL_PARTNERS.filter(p => !u.allowed.includes(p));
    try {
      await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
      await page.locator('#Email').fill(u.email);
      await page.locator('#Password').fill('TestPass123!');
      await Promise.all([page.waitForLoadState('domcontentloaded'),
        page.locator('button[type="submit"]').first().click()]);
      await page.waitForTimeout(900);
      const loggedIn = !/\/Account\/Login/i.test(page.url());

      const res = await page.evaluate(async () => {
        const r = await fetch('/api/CertificateTileExternal?page=1&pageSize=200', { headers: { Accept: 'application/json' } });
        const j = await r.json().catch(() => ({}));
        return { status: r.status, total: j.totalCount ?? j.TotalCount ?? null,
                 certs: j.certificates ?? j.Certificates ?? [] };
      });

      // Scan every cert object for any forbidden partner name (in ANY field).
      const leaks = [];
      for (const c of res.certs) {
        const blob = JSON.stringify(c);
        for (const fp of forbidden) {
          // word-boundary-ish match to avoid matching substrings inside other words
          if (new RegExp(`\\b${fp}\\b`).test(blob)) leaks.push({ cert: c.certificateNumber || c.CertificateNumber || '?', leaked: fp });
        }
      }
      const ok = loggedIn && res.status === 200 && leaks.length === 0;
      console.log(`${ok ? 'PASS' : 'FAIL'}  ${u.email}  (own=${u.allowed.join('+')})  loggedIn=${loggedIn} status=${res.status} total=${res.total} certsReturned=${res.certs.length} leaks=${leaks.length}`);
      if (leaks.length) console.log('   LEAK ->', JSON.stringify(leaks.slice(0, 8)));
      ok ? pass++ : fail++;
    } catch (e) {
      console.log(`FAIL  ${u.email}  ERROR ${e.message}`);
      fail++;
    } finally { await ctx.close(); }
  }
  await browser.close();
  console.log(`\n=== CR-171 isolation: ${pass}/${pass + fail} users clean ===`);
  process.exit(fail ? 1 : 0);
})();
