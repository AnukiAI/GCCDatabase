/**
 * CR-052 (QC #267) — visual validation of the "View all pages" control.
 *
 * Two things HTML inspection cannot prove and this can:
 *   1. The Create-page link is built in JS from a blob URL after a file is chosen — it does not
 *      exist in the served markup at all.
 *   2. The detail views gained a THIRD icon in an existing flex row (.certificate-download-action).
 *      Whether that row still lays out at phone width is a rendering question, not a markup one.
 *
 * Project standard: full-page captures at all five viewports, explicit waits for AJAX content.
 * Run with NODE_PATH set, Playwright is installed globally:
 *   NODE_PATH="$(npm root -g)" node Scripts/smoke-prod/verify-cr052-screenshots.js
 */

const path = require('path');
const fs = require('fs');
const { chromium } = require('playwright');

const BASE = process.env.CR052_BASE || 'http://localhost:5101';
const USER = process.env.CR052_USER || 'admin.uat';
const PASS = process.env.CR052_PASS || 'TestPass123!';
const OUT = path.join(__dirname, '_results', 'cr052');

// A genuinely 2-page PDF from the dev uploads store (page count verified with pypdf).
const MULTIPAGE_PDF = process.env.CR052_PDF || 'C:\\GCCData\\uploads\\certificates\\21-0117_20260814_214543.pdf';

// Public cert proven to serve real PDF bytes anonymously (see verify-cr052-all-pages.js, D2/D3).
const PUBLIC_CERT = process.env.CR052_PUBLIC_CERT || '26-0481';
const PUBLIC_CERT_ID = process.env.CR052_PUBLIC_ID || '4562';

const VIEWPORTS = [
    { label: 'desktop', width: 1400, height: 900 },
    { label: 'tablet-land', width: 1024, height: 800 },
    { label: 'tablet-port', width: 768, height: 1024 },
    { label: 'mobile-iph12', width: 390, height: 844 },
    { label: 'mobile-iphse', width: 375, height: 667 },
];

const findings = [];

// The dev DB intermittently takes >30s to render a certificate page under contention, which surfaces
// as a goto timeout. Retrying distinguishes "slow box" from "broken page"; without it a transient
// stall aborts the whole capture run, as it did on the fifth viewport.
async function gotoRetry(page, url, attempts = 3) {
    let last;
    for (let i = 1; i <= attempts; i++) {
        try {
            await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 45000 });
            return true;
        } catch (e) {
            last = e;
            console.log(`  (navigation to ${url} failed, attempt ${i}/${attempts})`);
            await page.waitForTimeout(3000 * i);
        }
    }
    throw last;
}

async function settle(page) {
    try { await page.waitForLoadState('networkidle', { timeout: 12000 }); } catch {}
    await page.waitForTimeout(600);
}

// Click + waitForNavigation in a Promise.all makes page.click itself block on the navigation, so a
// slow post-login redirect on this box fails the CLICK rather than the wait. Fire the click without
// waiting, then watch for the URL to leave /Account/Login.
async function login(page) {
    await gotoRetry(page, `${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', USER);
    await page.fill('input[name="Password"]', PASS);
    await page.click('button[type="submit"], input[type="submit"]', { noWaitAfter: true });
    try {
        await page.waitForURL(u => !/\/Account\/Login/i.test(u.toString()), { timeout: 60000 });
    } catch {
        console.log('  (login redirect slow; continuing and letting the next navigation assert state)');
    }
}

(async () => {
    fs.mkdirSync(OUT, { recursive: true });
    if (!fs.existsSync(MULTIPAGE_PDF)) {
        console.log(`NOTE: multi-page fixture not found at ${MULTIPAGE_PDF} — Create check will be skipped.`);
    }
    const browser = await chromium.launch();

    for (const vp of VIEWPORTS) {
        const ctx = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
        const page = await ctx.newPage();
        page.setDefaultTimeout(60000);
        page.setDefaultNavigationTimeout(60000);
        await login(page);

        // ---- public detail view: does the 3-icon action row still lay out? ----
        await gotoRetry(page, `${BASE}/Certificate/CertificateSingleView/${PUBLIC_CERT}?cid=${PUBLIC_CERT_ID}`);
        await settle(page);

        const row = await page.$('.certificate-download-action');
        const iconBoxes = await page.$$eval('.certificate-download-action > *',
            els => els.map(e => {
                const r = e.getBoundingClientRect();
                return { tag: e.tagName, top: Math.round(r.top), left: Math.round(r.left), w: Math.round(r.width), h: Math.round(r.height) };
            }));
        // "Wrapped" = the icons do not all share a row.
        const tops = [...new Set(iconBoxes.map(b => b.top))];
        const overflowed = await page.evaluate(() =>
            document.documentElement.scrollWidth > document.documentElement.clientWidth + 1);

        findings.push({
            viewport: vp.label,
            actionRowPresent: !!row,
            iconCount: iconBoxes.length,
            distinctRowTops: tops.length,
            wrapped: tops.length > 1,
            horizontalPageOverflow: overflowed,
            boxes: vp.label === 'mobile-iphse' ? iconBoxes : undefined,
        });

        await page.screenshot({ path: path.join(OUT, `allpages-public-${vp.label}.png`), fullPage: true });

        // ---- Create page: blob-backed link appears only after a file is chosen ----
        if (vp.label === 'desktop' && fs.existsSync(MULTIPAGE_PDF)) {
            await gotoRetry(page, `${BASE}/Certificate/Create`);
            await settle(page);

            const before = await page.$$eval('#pdfPreviewContainer a', els => els.length);
            await page.setInputFiles('#certificatePdf', MULTIPAGE_PDF);
            await page.waitForTimeout(1200);

            const link = await page.$('#pdfPreviewContainer a');
            const href = link ? await link.getAttribute('href') : null;
            const text = link ? (await link.innerText()).trim() : null;

            findings.push({
                viewport: 'create-after-file-select',
                linksBeforeSelect: before,
                linkAppeared: !!link,
                hrefIsBlob: !!href && href.startsWith('blob:'),
                linkText: text,
            });
            await page.screenshot({ path: path.join(OUT, 'allpages-create-desktop.png'), fullPage: true });
        }

        await ctx.close();
        console.log(`captured ${vp.label}`);
    }

    await browser.close();
    fs.writeFileSync(path.join(OUT, 'findings.json'), JSON.stringify(findings, null, 2));
    console.log('\n' + JSON.stringify(findings, null, 2));
    console.log(`\nscreenshots -> ${OUT}`);
})().catch(e => { console.error('ERROR: ' + e.message); process.exit(1); });
