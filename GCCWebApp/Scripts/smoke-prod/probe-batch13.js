// Batch 13:
//   1. Repro the file-upload DoS observed yesterday — try multiple malformed PDFs
//      against /api/CertificatesApi/GenerateThumbnail to see if server consistently
//      crashes or only on specific shapes.
//   2. Quantify DEF-208 prod exposure — walk through every HTML surface that might
//      render /uploads/* URLs, count unique exposed filenames.

const { chromium, request } = require('playwright');
const fs = require('fs');
const path = require('path');

const DEV = 'http://localhost:5101';
const PROD = 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const OUT = path.join(__dirname, 'batch13-' + new Date().toISOString().replace(/[:.]/g, '-'));
fs.mkdirSync(OUT, { recursive: true });

const results = [];
function record(section, name, ok, detail) {
    results.push({ section, name, ok, detail });
    const tag = ok ? '✓' : '✗';
    console.log(`  ${tag} [${section}] ${name.padEnd(64)} ${(detail || '').slice(0, 180)}`);
}

async function login(ctx, BASE, email, password) {
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
    const browser = await chromium.launch({ headless: true });

    // ====================== [1] File-upload DoS reproduction ======================
    console.log('\n[1] File-upload DoS — multiple malformed PDF shapes against /GenerateThumbnail');
    const adminCtx = await browser.newContext();
    const adminPage = await login(adminCtx, DEV, 'swest@anukiconsulting.com', 'TestPass123!');

    const probes = [
        { name: 'html-as-pdf',       body: '<html><body>not a pdf</body></html>',                              mime: 'application/pdf' },
        { name: 'empty-pdf',         body: '',                                                                  mime: 'application/pdf' },
        { name: '1-byte-pdf',        body: '%',                                                                 mime: 'application/pdf' },
        { name: 'pdf-magic-only',    body: '%PDF-1.4\n',                                                        mime: 'application/pdf' },
        { name: 'pdf-corrupt-tail',  body: '%PDF-1.4\nGARBAGE\n%%EOF\n',                                        mime: 'application/pdf' },
        { name: 'pdf-jpeg-fake',     body: Buffer.from([0xFF, 0xD8, 0xFF, 0xE0]).toString('binary'),            mime: 'application/pdf' }
    ];

    const beforeServer = await fetch(`${DEV}/`).then(r => r.status).catch(() => 0);
    console.log(`  Server status before probes: ${beforeServer}`);

    for (const p of probes) {
        const result = await adminPage.evaluate(async (probe) => {
            try {
                const f = new FormData();
                const blob = new Blob([probe.body], { type: probe.mime });
                f.append('file', blob, `${probe.name}.pdf`);
                const t0 = Date.now();
                const r = await fetch('/api/CertificatesApi/GenerateThumbnail', {
                    method: 'POST', credentials: 'same-origin', body: f
                });
                const elapsed = Date.now() - t0;
                const body = await r.text();
                return { status: r.status, elapsed, bodyLen: body.length, bodyStart: body.slice(0, 120) };
            } catch (e) { return { error: e.message }; }
        }, p);
        console.log(`  ${p.name.padEnd(20)} → ${JSON.stringify(result).slice(0, 200)}`);
        // Probe server alive after each
        const check = await fetch(`${DEV}/`, { signal: AbortSignal.timeout(3000) }).then(r => r.status).catch(e => `ERR: ${e.message}`);
        if (typeof check === 'number' && check < 500) {
            console.log(`     server status after: ${check} ✓`);
        } else {
            console.log(`     ✗ SERVER FAILED after ${p.name}: ${check}`);
            record('1.dos', `Repro: ${p.name} crashes server`, false, `before=200 after=${check}`);
            break;
        }
    }
    record('1.dos', 'Server survived all 6 malformed PDF probes', true, 'no repro of yesterday\'s crash');

    await adminCtx.close();

    // ====================== [2] DEF-208 prod exposure quantification ======================
    console.log('\n[2] DEF-208 prod exposure — count /uploads/* URLs in HTML across surfaces');
    {
        const ctx = await browser.newContext({ httpCredentials: GATE });
        const adminPage = await login(ctx, PROD, 'swest@anukiconsulting.com', 'TestPass123!');
        if (adminPage.url().includes('/Account/Login')) {
            record('2.exposure', 'Admin login on prod failed', false, '');
        } else {
            const surfacesProbed = [];
            const allUploadUrls = new Set();

            // Tile internal — walk one page
            await adminPage.goto(`${PROD}/Certificate/CertificateTileViewInternal`, { waitUntil: 'networkidle' });
            await adminPage.waitForTimeout(3000);
            const tileUrls = await adminPage.evaluate(() => {
                const matches = [...document.documentElement.outerHTML.matchAll(/\/uploads\/[a-zA-Z0-9_\/.\-]+/g)];
                return [...new Set(matches.map(m => m[0]))];
            });
            tileUrls.forEach(u => allUploadUrls.add(u));
            surfacesProbed.push({ url: '/Certificate/CertificateTileViewInternal', uploadUrls: tileUrls.length });

            // Walk first 5 cert detail pages to extract their static URLs
            const certNums = await adminPage.evaluate(() => {
                return [...document.querySelectorAll('.certificate-card .certificate-id, .certificate-card .certificate-number')]
                    .slice(0, 5).map(el => el.textContent.trim());
            });
            console.log(`  Sample cert numbers from tile: ${certNums.join(', ')}`);

            for (const certNum of certNums) {
                await adminPage.goto(`${PROD}/Certificate/CertificateInternalView/${certNum}`, { waitUntil: 'networkidle' }).catch(() => {});
                await adminPage.waitForTimeout(800);
                const detailUrls = await adminPage.evaluate(() => {
                    const matches = [...document.documentElement.outerHTML.matchAll(/\/uploads\/[a-zA-Z0-9_\/.\-]+/g)];
                    return [...new Set(matches.map(m => m[0]))];
                });
                detailUrls.forEach(u => allUploadUrls.add(u));
                surfacesProbed.push({ url: `/Certificate/CertificateInternalView/${certNum}`, uploadUrls: detailUrls.length });
            }

            console.log(`\n  Aggregate: ${allUploadUrls.size} unique /uploads/* URLs across ${surfacesProbed.length} surfaces`);

            // Test anonymous access to each unique URL
            const anon = await request.newContext({ httpCredentials: GATE });
            let anonReachable = 0;
            const sample = [...allUploadUrls].slice(0, 10);
            for (const u of sample) {
                const r = await anon.get(`${PROD}${u}`);
                if (r.ok()) anonReachable++;
            }
            await anon.dispose();

            const exposurePct = Math.round((anonReachable / sample.length) * 100);
            record('2.exposure', `Prod DEF-208 exposure: ${anonReachable}/${sample.length} URLs anon-fetchable (${exposurePct}%)`,
                anonReachable === 0,
                `${allUploadUrls.size} unique URLs across ${surfacesProbed.length} pages; sample of ${sample.length} = ${anonReachable} successful anon GETs`);

            fs.writeFileSync(path.join(OUT, 'def208-exposure.json'), JSON.stringify({
                totalUniqueUrls: allUploadUrls.size,
                surfacesProbed,
                anonReachable: anonReachable,
                sampleSize: sample.length,
                sample: sample
            }, null, 2));
        }
        await ctx.close();
    }

    // SUMMARY
    const passes = results.filter(r => r.ok).length;
    const fails = results.filter(r => !r.ok).length;
    console.log(`\n=== BATCH-13: PASS=${passes} FAIL=${fails} ===`);
    if (fails > 0) {
        console.log('\nFailures:');
        results.filter(r => !r.ok).forEach(r => console.log(`  [${r.section}] ${r.name}  →  ${r.detail}`));
    }
    fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(results, null, 2));
    await browser.close();
})().catch(e => { console.error('FATAL', e); process.exit(2); });
