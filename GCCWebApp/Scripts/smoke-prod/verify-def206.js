// Verify DEF-206 fix — /api/CertificatesApi class-level [AnalystOrAdmin].
//
// Before:
//   anon       → 401
//   external   → 200 with 43 certs across 10+ companies (LEAK)
//   partner    → 200 with 11 certs incl. Drafts (LEAK + contract divergence)
//   analyst    → 200
//   admin      → 200
//
// After (expected):
//   anon       → 401
//   external   → 403 (class-level [AnalystOrAdmin] denies)
//   partner    → 403 (same; partners must use /api/v1/certificates contracted endpoint)
//   analyst    → 200
//   admin      → 200

const { chromium, request } = require('playwright');
const BASE = 'http://localhost:5101';
const PARTNER_KEY = 'gcc_partner_95961557dde1e196be074634cef252caec32c4b08e1e58dcfa63e43bbdbb63c7';

async function login(ctx, email) {
    const page = await ctx.newPage();
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('input[name="Email"]', email);
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([
        page.waitForURL(u => !u.toString().includes('/Account/Login'), { timeout: 30000 }).catch(() => {}),
        page.click('button[type="submit"]')
    ]);
    return page;
}

(async () => {
    const browser = await chromium.launch({ headless: true });
    let passes = 0;
    const expected = [];

    // Anonymous
    const anon = await request.newContext();
    let r = await anon.fetch(`${BASE}/api/CertificatesApi?pageSize=10`, { method: 'GET', maxRedirects: 0 });
    expected.push({ persona: 'anon       ', want: 401, got: r.status() });
    // Partner X-API-Key
    r = await anon.fetch(`${BASE}/api/CertificatesApi?pageSize=10`, { method: 'GET', maxRedirects: 0, headers: { 'X-API-Key': PARTNER_KEY } });
    expected.push({ persona: 'partner-key', want: 403, got: r.status() });
    await anon.dispose();

    // External
    const extCtx = await browser.newContext();
    await login(extCtx, 'external.user@anukiconsulting.com');
    r = await extCtx.pages()[0].request.fetch(`${BASE}/api/CertificatesApi?pageSize=10`, { method: 'GET', maxRedirects: 0 });
    expected.push({ persona: 'external   ', want: 403, got: r.status() });
    await extCtx.close();

    // Analyst (should be allowed)
    const anaCtx = await browser.newContext();
    await login(anaCtx, 'jane.analyst@anukiconsulting.com');
    r = await anaCtx.pages()[0].request.fetch(`${BASE}/api/CertificatesApi?pageSize=10`, { method: 'GET', maxRedirects: 0 });
    expected.push({ persona: 'analyst    ', want: 200, got: r.status() });
    await anaCtx.close();

    // Admin (should be allowed)
    const admCtx = await browser.newContext();
    await login(admCtx, 'swest@anukiconsulting.com');
    r = await admCtx.pages()[0].request.fetch(`${BASE}/api/CertificatesApi?pageSize=10`, { method: 'GET', maxRedirects: 0 });
    expected.push({ persona: 'admin      ', want: 200, got: r.status() });
    // While we have admin, also verify by-id and by-number still work
    let r2 = await admCtx.pages()[0].request.fetch(`${BASE}/api/CertificatesApi/2`, { method: 'GET', maxRedirects: 0 });
    expected.push({ persona: 'admin /2   ', want: 200, got: r2.status() });
    r2 = await admCtx.pages()[0].request.fetch(`${BASE}/api/CertificatesApi/by-number/26-0001`, { method: 'GET', maxRedirects: 0 });
    expected.push({ persona: 'admin /by-#', want: 200, got: r2.status() });
    await admCtx.close();

    // External attempting by-id and by-number — should also be 403 now
    const extCtx2 = await browser.newContext();
    await login(extCtx2, 'external.user@anukiconsulting.com');
    r = await extCtx2.pages()[0].request.fetch(`${BASE}/api/CertificatesApi/2`, { method: 'GET', maxRedirects: 0 });
    expected.push({ persona: 'ext /2     ', want: 403, got: r.status() });
    r = await extCtx2.pages()[0].request.fetch(`${BASE}/api/CertificatesApi/by-number/26-0001`, { method: 'GET', maxRedirects: 0 });
    expected.push({ persona: 'ext /by-#  ', want: 403, got: r.status() });
    await extCtx2.close();

    for (const e of expected) {
        const ok = e.got === e.want;
        if (ok) passes++;
        console.log(`  ${ok ? '✓' : '✗'} ${e.persona} → status=${e.got} (expected ${e.want})`);
    }
    console.log(`\n${passes}/${expected.length} pass`);
    await browser.close();
})();
