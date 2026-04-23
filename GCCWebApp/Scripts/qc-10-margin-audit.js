// QC #10: Certificates page margin audit
// Checks actual left/right whitespace at multiple viewports for all roles.
// Run: node Scripts/qc-10-margin-audit.js

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'http://localhost:5101';
const OUT_DIR = path.join(__dirname, 'screenshots', 'qc-10-' + new Date().toISOString().split('T')[0]);

const VIEWPORTS = [
    { name: '320-mobile-xs',   width: 320,  height: 800 },
    { name: '375-iphone-se',   width: 375,  height: 812 },
    { name: '414-iphone-plus', width: 414,  height: 896 },
    { name: '768-tablet',      width: 768,  height: 1024 },
    { name: '1024-ipad-land',  width: 1024, height: 768 },
    { name: '1366-laptop',     width: 1366, height: 768 },
    { name: '1440-desktop',    width: 1440, height: 900 },
    { name: '1920-fullhd',     width: 1920, height: 1080 },
    { name: '2560-ultrawide',  width: 2560, height: 1080 },
];

const ROLES = [
    { name: 'admin',    user: 'admin',    pass: 'TestPass123!', path: '/Certificate/CertificateTileViewInternal' },
    { name: 'analyst',  user: 'analyst',  pass: 'TestPass123!', path: '/Certificate/CertificateTileViewInternal' },
    { name: 'external', user: 'external', pass: 'TestPass123!', path: '/Certificate/CertificateTileViewExternal' },
];

function ensureDir(d) { if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true }); }

async function login(page, user, pass) {
    await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle' });
    await page.fill('input[name="Username"], #Username', user);
    await page.fill('input[name="Password"], #Password', pass);
    await Promise.all([
        page.waitForLoadState('networkidle'),
        page.click('button[type="submit"], input[type="submit"]'),
    ]);
    return !page.url().includes('/Account/Login');
}

async function measure(page) {
    return await page.evaluate(() => {
        const vw = window.innerWidth;
        const vh = window.innerHeight;
        const docW = document.documentElement.scrollWidth;
        const horizOverflow = docW > vw;

        function rect(sel) {
            const el = document.querySelector(sel);
            if (!el) return null;
            const r = el.getBoundingClientRect();
            const cs = getComputedStyle(el);
            return {
                sel,
                left: Math.round(r.left),
                right: Math.round(vw - r.right),
                width: Math.round(r.width),
                padLeft: parseInt(cs.paddingLeft) || 0,
                padRight: parseInt(cs.paddingRight) || 0,
                maxWidth: cs.maxWidth,
            };
        }

        const selectors = [
            '.page-container',
            '.main-content',
            '.main-container',
            '.admin-container',
            '.certificate-search-section',
            'main',
            'table',
        ];

        const measurements = selectors.map(rect).filter(Boolean);
        return { vw, vh, docW, horizOverflow, measurements };
    });
}

async function run() {
    ensureDir(OUT_DIR);
    const browser = await chromium.launch();
    const results = [];

    for (const role of ROLES) {
        const ctx = await browser.newContext();
        const page = await ctx.newPage();
        const ok = await login(page, role.user, role.pass);
        if (!ok) {
            console.log(`[${role.name}] LOGIN FAILED`);
            await ctx.close();
            continue;
        }
        console.log(`\n=== ${role.name.toUpperCase()} → ${role.path} ===`);
        for (const vp of VIEWPORTS) {
            await page.setViewportSize({ width: vp.width, height: vp.height });
            try {
                await page.goto(`${BASE_URL}${role.path}`, { waitUntil: 'networkidle', timeout: 30000 });
            } catch (e) {
                console.log(`  ${vp.name}: GOTO ERROR - ${e.message.split('\n')[0]}`);
                continue;
            }
            await page.waitForTimeout(500);

            const m = await measure(page);
            const screenshotPath = path.join(OUT_DIR, `${role.name}-${vp.name}.png`);
            await page.screenshot({ path: screenshotPath, fullPage: false });

            const pc = m.measurements.find(x => x.sel === '.page-container');
            const overflowFlag = m.horizOverflow ? ' [H-OVERFLOW]' : '';
            const gutters = pc ? `L:${pc.left}px R:${pc.right}px pad:${pc.padLeft}/${pc.padRight} max:${pc.maxWidth}` : 'NO .page-container';
            const totalGutter = pc ? pc.left + pc.right + pc.padLeft + pc.padRight : null;
            const pctGutter = totalGutter !== null ? ((totalGutter / m.vw) * 100).toFixed(1) : 'n/a';
            console.log(`  ${vp.name.padEnd(18)} vw=${m.vw} doc=${m.docW}${overflowFlag} ${gutters} TOTAL=${totalGutter}px (${pctGutter}%)`);

            results.push({ role: role.name, viewport: vp.name, vw: m.vw, ...m });
        }
        await ctx.close();
    }

    fs.writeFileSync(path.join(OUT_DIR, 'results.json'), JSON.stringify(results, null, 2));
    console.log(`\nResults: ${OUT_DIR}`);
    await browser.close();
}

run().catch(e => { console.error(e); process.exit(1); });
