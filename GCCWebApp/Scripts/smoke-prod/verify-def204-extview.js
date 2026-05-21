// Verify DEF-204 ExternalView gate on dev.
// External user (Henkel+KAO) should:
//   - See their own company's certs via ExternalView (200)
//   - NOT see a cert from another company (404)
//   - NOT see a Draft cert regardless of company (404)
// Admin should see everything (200).

const { chromium } = require('playwright');
const BASE = 'http://localhost:5101';

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

    // External user (Henkel + KAO mapped per earlier session)
    const extCtx = await browser.newContext();
    const extPage = await login(extCtx, 'external.user@anukiconsulting.com');
    console.log('External login →', extPage.url());

    // Ground truth from DB:
    //   26-0001 = Acme/Amazon Paid     → Henkel External should be 404 (cross-company)
    //   26-0011 = Henkel/None Paid     → Henkel External should be 200 (their cert)
    //   26-0012 = Henkel/None Expired  → Henkel External should be 404 (expired)
    //   26-0020 = Henkel/None Paid     → Henkel External should be 200
    //   26-0026 = SuperlongCo Draft    → Henkel External should be 404 (Draft + cross-company)
    //   26-0023 = Acme/None Paid       → Henkel External should be 404 (cross-company)
    const probes = [
        { num: '26-0001', expectStatus: 404, why: 'Acme cert — Henkel External should not see other company' },
        { num: '26-0011', expectStatus: 200, why: 'Henkel/Paid — their own company\'s cert, should see' },
        { num: '26-0012', expectStatus: 404, why: 'Henkel/Expired — own company but expired, blocked' },
        { num: '26-0020', expectStatus: 200, why: 'Henkel/Paid — should see' },
        { num: '26-0026', expectStatus: 404, why: 'SuperlongCo/Draft — not their company AND Draft' },
        { num: '26-0023', expectStatus: 404, why: 'Acme/Paid — cross-company, blocked' }
    ];
    let passes = 0;
    for (const p of probes) {
        const r = await extPage.request.get(`${BASE}/Certificate/CertificateExternalView/${p.num}`, { maxRedirects: 0 });
        const ok = r.status() === p.expectStatus;
        if (ok) passes++;
        console.log(`  ${ok ? '✓' : '✗'} External ${p.num}: ${r.status()} (expected ${p.expectStatus}) — ${p.why}`);
    }
    console.log(`\nExternal probe: ${passes}/${probes.length} pass`);
    await extCtx.close();

    // Admin user — should see everything
    const adminCtx = await browser.newContext();
    const adminPage = await login(adminCtx, 'swest@anukiconsulting.com');
    for (const num of ['26-0001', '26-0026', '26-0023']) {
        const r = await adminPage.request.get(`${BASE}/Certificate/CertificateExternalView/${num}`, { maxRedirects: 0 });
        console.log(`Admin GET    /Certificate/CertificateExternalView/${num}: status=${r.status()}`);
    }
    await adminCtx.close();

    await browser.close();
})();
