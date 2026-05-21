// DEF-208 on prod — look for static-file URLs in actual rendered cert pages
const { chromium, request } = require('playwright');
const BASE = 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };

(async () => {
    const b = await chromium.launch({ headless: true });

    // Try admin login to access cert detail pages (they may render image srcs)
    const adminCtx = await b.newContext({ httpCredentials: GATE });
    const page = await adminCtx.newPage();
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('input[name="Email"]', 'swest@anukiconsulting.com');
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([
        page.waitForURL(u => !u.toString().includes('/Account/Login'), { timeout: 30000 }).catch(() => {}),
        page.click('button[type="submit"]')
    ]);
    console.log(`Logged in: ${page.url()}`);

    // Land on cert tile internal view — should have thumbnail img tags
    await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(3000);
    const tileSrcs = await page.evaluate(() => {
        const imgs = [...document.querySelectorAll('img')];
        return imgs.map(i => i.getAttribute('src')).filter(Boolean);
    });
    const uploadSrcs = tileSrcs.filter(s => s.includes('/uploads/') || s.includes('uploads/'));
    console.log(`Tile view: ${tileSrcs.length} total img src, ${uploadSrcs.length} reference /uploads/`);
    if (uploadSrcs.length > 0) console.log(`  sample: ${uploadSrcs[0]}`);

    // Try opening an individual cert detail page
    const certCard = await page.$('.certificate-card');
    if (certCard) {
        const certNum = await certCard.evaluate(el => {
            const id = el.querySelector('.certificate-id, .certificate-number');
            return id ? id.textContent.trim() : null;
        });
        if (certNum) {
            console.log(`\nOpening cert detail for ${certNum}`);
            await page.goto(`${BASE}/Certificate/CertificateInternalView/${certNum}`, { waitUntil: 'networkidle' });
            await page.waitForTimeout(2000);
            const detailSrcs = await page.evaluate(() => {
                const imgs = [...document.querySelectorAll('img')];
                return imgs.map(i => i.getAttribute('src')).filter(Boolean);
            });
            const detailUploads = detailSrcs.filter(s => s.includes('/uploads/'));
            console.log(`  Detail view: ${detailSrcs.length} img srcs, ${detailUploads.length} reference /uploads/`);
            detailUploads.forEach(u => console.log(`    ${u}`));

            // If a static URL was found, try anonymous access to confirm DEF-208 on prod
            if (detailUploads.length > 0) {
                const anon = await request.newContext({ httpCredentials: GATE });
                const probe = detailUploads[0].startsWith('/') ? BASE + detailUploads[0] : detailUploads[0];
                const r = await anon.get(probe);
                console.log(`\n  Anon GET ${detailUploads[0]}: status=${r.status()} ct=${r.headers()['content-type']}`);
                if (r.status() === 200) console.log(`  ✗ LEAK: DEF-208 confirmed on prod — anonymous can fetch /uploads/* directly`);
                else console.log(`  ✓ Anon blocked (${r.status()}) — DEF-208 may NOT be live on prod`);
                await anon.dispose();
            }
        }
    }

    await adminCtx.close();
    await b.close();
})().catch(e => { console.error('FATAL', e); process.exit(2); });
