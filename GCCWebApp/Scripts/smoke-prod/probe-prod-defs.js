// Verify DEF-204, DEF-206, DEF-208 against prod gccwebapp.com.
// Read-only — no data changes. Uses Basic Auth gate + admin/external test creds.
//
// Expectations:
//   - DEF-208 (static-file leak): anonymous GET /uploads/certificates/{file}.pdf → 200 ← confirms leak on prod
//   - DEF-206 (internal API no role guard): authenticated external user gets all certs ← confirms on prod
//   - DEF-204 (cross-scope cert detail): external user reads other-company ExternalView ← confirms on prod

const { chromium, request } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const OUT = path.join(__dirname, 'prod-defs-' + new Date().toISOString().replace(/[:.]/g, '-'));
fs.mkdirSync(OUT, { recursive: true });

const results = [];
function record(section, name, ok, detail) {
    results.push({ section, name, ok, detail });
    const tag = ok ? '✓' : '✗';
    console.log(`  ${tag} [${section}] ${name.padEnd(64)} ${(detail || '').slice(0, 180)}`);
}

async function login(ctx, email, password) {
    const page = await ctx.newPage();
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('input[name="Email"]', email);
    await page.fill('input[name="Password"]', password);
    await Promise.all([
        page.waitForURL(u => !u.toString().includes('/Account/Login'), { timeout: 30000 }).catch(() => {}),
        page.click('button[type="submit"]')
    ]);
    return page;
}

(async () => {
    console.log(`\nProd DEF verification against ${BASE}`);
    console.log('='.repeat(72));

    const browser = await chromium.launch({ headless: true });

    // ====================== [1] DEF-208 — static-file leak on prod ======================
    console.log('\n[1] DEF-208 verification — static-file PDF/thumbnail anonymous access');
    {
        const anon = await request.newContext({ httpCredentials: GATE });
        // First, get the cert tile API to find filenames embedded in the HTML
        // Actually — we don't know real filenames on prod. Let me try a few patterns.
        // Pull the public cert tile API to get a cert number, then guess pdf path.
        let certNum = null;
        const tileResp = await anon.get(`${BASE}/api/certificate-tiles?pageSize=5`);
        if (tileResp.ok()) {
            const j = await tileResp.json();
            const items = j.certificates || j.data || [];
            if (items.length > 0) certNum = items[0].certificateNumber;
        }
        console.log(`  sample certNum from prod tile API: ${certNum}`);

        // Try anonymous static-file access with a generic guess (timestamp will vary; just sample)
        // We can't reliably guess timestamp on prod. Try to find a leaked URL from a cert detail page.
        // Or: hit a 404 to confirm directory listing is off, then 200 on a known-format guess if possible.

        // Try directory listing
        const dr = await anon.get(`${BASE}/uploads/certificates/`);
        record('1.static-prod', 'Directory listing /uploads/certificates/ on prod',
            dr.status() === 404 || dr.status() === 403, `status=${dr.status()}`);

        // Try with a known-format filename — pattern based
        // The real filenames on prod follow the same naming convention as dev.
        // Without knowing exact timestamps, probe a few candidate dates.
        if (certNum) {
            // Common formats: {certNum}_{YYYYMMDD}_{HHMMSS}.pdf
            // Just try 4 plausible dates that production deploy windows might span:
            const dates = ['20260112', '20260113', '20260115', '20260116'];
            const times = ['230836', '052459', '030000']; // arbitrary
            let foundOne = false;
            let probesRun = 0;
            outer: for (const d of dates) {
                for (const t of times) {
                    if (probesRun >= 10) break outer;
                    const url = `${BASE}/uploads/certificates/${certNum}_${d}_${t}.pdf`;
                    const r = await anon.get(url);
                    probesRun++;
                    if (r.status() === 200 && (r.headers()['content-type'] || '').includes('pdf')) {
                        foundOne = true;
                        console.log(`     ✗ LEAK: GET ${url} → 200 PDF (size=${r.headers()['content-length']})`);
                        break outer;
                    }
                }
            }
            record('1.static-prod', `Anon static-file PDF probe (${probesRun} guesses for ${certNum})`,
                !foundOne, foundOne ? 'LEAK confirmed on prod' : `tried ${probesRun} filename guesses, none matched — limited probe`);
        }

        // Also probe if the cert detail HTML page leaks the actual filename via an <img> tag
        // The thumbnail rendering on the tile view shows `/uploads/images/...`
        // Let's view the public cert tile page HTML and grep for the actual upload paths
        const tileHtmlResp = await anon.get(`${BASE}/Certificate/CertificateTileView`);
        if (tileHtmlResp.ok()) {
            const html = await tileHtmlResp.text();
            const imgUrls = [...html.matchAll(/\/uploads\/(?:images|certificates)\/[^"'\s>]+/g)].map(m => m[0]);
            const uniqueImgs = [...new Set(imgUrls)].slice(0, 5);
            console.log(`  HTML on /Certificate/CertificateTileView contains ${imgUrls.length} /uploads/* references (${uniqueImgs.length} unique)`);
            // Try fetching one to confirm anonymous access
            if (uniqueImgs.length > 0) {
                const probeUrl = BASE + uniqueImgs[0];
                const pr = await anon.get(probeUrl);
                console.log(`     anon GET ${uniqueImgs[0]}: status=${pr.status()} ct=${pr.headers()['content-type'] || ''}`);
                record('1.static-prod', `Anon static-file (from rendered HTML) access`,
                    pr.status() !== 200, `${uniqueImgs[0]}: status=${pr.status()} — if 200, DEF-208 is live on prod`);
            } else {
                record('1.static-prod', `No /uploads/* refs found in public tile HTML (good — controllers may proxy)`,
                    true, `0 references in HTML`);
            }
        }
        await anon.dispose();
    }

    // ====================== [2] DEF-206 — internal API auth gap on prod ======================
    console.log('\n[2] DEF-206 verification — /api/CertificatesApi role-guard on prod');
    {
        const extCtx = await browser.newContext({ httpCredentials: GATE });
        // Note: external test user may or may not exist on prod with same email
        const extPage = await login(extCtx, 'external.user@anukiconsulting.com', 'TestPass123!');
        const u = extPage.url();
        console.log(`  External login result URL: ${u}`);
        if (u.includes('/Account/Login')) {
            // Try another likely external account
            record('2.internal-api-prod', 'External login on prod failed',
                false, 'cannot verify DEF-206 — try a known prod external account if available');
        } else {
            const r = await extPage.request.get(`${BASE}/api/CertificatesApi?pageSize=50`, { maxRedirects: 0 });
            if (r.ok()) {
                const j = await r.json();
                const items = j.certificates || j.data || j.items || [];
                const companies = [...new Set(items.map(c => c.companyName || c.company || c.CompanyName))];
                record('2.internal-api-prod', `External GET /api/CertificatesApi on prod`,
                    items.length < 5 && companies.length <= 1,
                    `count=${items.length} distinctCompanies=${companies.length} — if many companies, DEF-206 live on prod`);
            } else {
                record('2.internal-api-prod', `External GET /api/CertificatesApi on prod (blocked at auth)`,
                    r.status() === 401 || r.status() === 403 || r.status() === 302,
                    `status=${r.status()} — if 200 = DEF-206 live on prod, if 4xx = endpoint actually requires admin on prod`);
            }
        }
        await extCtx.close();
    }

    // ====================== [3] DEF-204 — ExternalView cross-company on prod ======================
    console.log('\n[3] DEF-204 verification — ExternalView cross-company on prod');
    {
        const extCtx = await browser.newContext({ httpCredentials: GATE });
        const extPage = await login(extCtx, 'external.user@anukiconsulting.com', 'TestPass123!');
        if (extPage.url().includes('/Account/Login')) {
            record('3.extview-prod', 'External login failed; skip', false, 'cannot verify DEF-204');
        } else {
            // Find a known cert number from the public tile API
            const sample = await extPage.request.get(`${BASE}/api/certificate-tiles?pageSize=5`).then(r => r.json()).catch(() => null);
            const items = sample?.certificates || sample?.data || [];
            const certNum = items[0]?.certificateNumber;
            if (!certNum) {
                record('3.extview-prod', 'No cert numbers retrievable from prod public tile API',
                    false, 'cannot select a target cert');
            } else {
                const r = await extPage.request.get(`${BASE}/Certificate/CertificateExternalView/${certNum}`, { maxRedirects: 0 });
                if (r.ok()) {
                    const body = await r.text();
                    const hasCertNum = body.includes(certNum);
                    const hasDetailBlock = body.includes('certificate-detail') || body.includes('Certification Period');
                    record('3.extview-prod', `External GET ExternalView/${certNum} on prod`,
                        !(hasCertNum && hasDetailBlock),
                        `status=${r.status()} bodyLen=${body.length} certInBody=${hasCertNum} detailBlock=${hasDetailBlock} — if true/true = DEF-204 live on prod (whether the cert is theirs or not)`);
                } else {
                    record('3.extview-prod', `External GET ExternalView on prod`, r.status() !== 200, `status=${r.status()}`);
                }
            }
        }
        await extCtx.close();
    }

    // ====================== SUMMARY ======================
    const passes = results.filter(r => r.ok).length;
    const fails = results.filter(r => !r.ok).length;
    console.log(`\n=== PROD DEF VERIFY: PASS=${passes} FAIL=${fails} (FAIL = leak confirmed on prod) ===`);
    if (fails > 0) {
        console.log('\nLeaks confirmed on prod:');
        results.filter(r => !r.ok).forEach(r => console.log(`  [${r.section}] ${r.name}  →  ${r.detail}`));
    }
    fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(results, null, 2));
    await browser.close();
})().catch(e => { console.error('FATAL', e); process.exit(2); });
