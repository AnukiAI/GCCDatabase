/**
 * DEF-418 REOPENED (QC #263) — layout check for the Import Certificates modal.
 *
 * The instruction list grew from 4 bullets to 7. The copy being correct is worthless if the longer
 * list pushes the file drop-zone or the action buttons out of reach — that would make the import
 * harder to use while "fixing" it, on the exact screen the client complained about.
 *
 * Measures, per viewport: is the drop-zone reachable without the modal body overflowing invisibly,
 * and are the footer buttons still on screen.
 *
 *   NODE_PATH="$(npm root -g)" node Scripts/smoke-prod/verify-def418-modal-layout.js
 */

const path = require('path');
const fs = require('fs');
const { chromium } = require('playwright');

const BASE = process.env.DEF418_BASE || 'http://localhost:5101';
const USER = process.env.DEF418_USER || 'admin.uat';
const PASS = process.env.DEF418_PASS || 'TestPass123!';
const OUT = path.join(__dirname, '_results', 'def418');

const VIEWPORTS = [
    { label: 'desktop', width: 1400, height: 900 },
    { label: 'tablet-land', width: 1024, height: 800 },
    { label: 'tablet-port', width: 768, height: 1024 },
    { label: 'mobile-iph12', width: 390, height: 844 },
    { label: 'mobile-iphse', width: 375, height: 667 },
];

const findings = [];

async function gotoRetry(page, url, attempts = 3) {
    let last;
    for (let i = 1; i <= attempts; i++) {
        try { await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 45000 }); return; }
        catch (e) { last = e; console.log(`  (nav retry ${i}/${attempts})`); await page.waitForTimeout(3000 * i); }
    }
    throw last;
}

async function login(page) {
    await gotoRetry(page, `${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', USER);
    await page.fill('input[name="Password"]', PASS);
    await page.click('button[type="submit"], input[type="submit"]', { noWaitAfter: true });
    try { await page.waitForURL(u => !/\/Account\/Login/i.test(u.toString()), { timeout: 60000 }); } catch {}
}

(async () => {
    fs.mkdirSync(OUT, { recursive: true });
    const browser = await chromium.launch();

    for (const vp of VIEWPORTS) {
        const ctx = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
        const page = await ctx.newPage();
        page.setDefaultTimeout(60000);
        page.setDefaultNavigationTimeout(60000);
        await login(page);

        await gotoRetry(page, `${BASE}/Certificate/CertificateTileViewInternal`);
        try { await page.waitForLoadState('networkidle', { timeout: 12000 }); } catch {}
        await page.waitForTimeout(600);

        // Don't just click and let a miss throw — record WHY it missed. A button that is absent,
        // hidden by a breakpoint, or simply not rendered yet are three different findings, and only
        // one of them is a defect.
        const btn = await page.evaluate(() => {
            const b = document.getElementById('openImportModalBtn');
            if (!b) return { present: false };
            const r = b.getBoundingClientRect();
            const cs = getComputedStyle(b);
            return {
                present: true,
                display: cs.display, visibility: cs.visibility,
                width: Math.round(r.width), height: Math.round(r.height),
                offscreenX: r.right < 0 || r.left > window.innerWidth,
            };
        });
        if (!btn.present || btn.width === 0) {
            findings.push({ viewport: vp.label, importButton: btn, modalOpen: false,
                note: 'import button not clickable at this viewport — see importButton for why' });
            await page.screenshot({ path: path.join(OUT, `import-modal-${vp.label}-NOBUTTON.png`), fullPage: false });
            await ctx.close();
            console.log(`SKIPPED ${vp.label}: import button not clickable (${JSON.stringify(btn)})`);
            continue;
        }
        await page.click('#openImportModalBtn');
        await page.waitForTimeout(700);

        const m = await page.evaluate(() => {
            const modal = document.getElementById('certificate-import-modal');
            const container = modal && modal.querySelector('.import-modal-container');
            const body = modal && modal.querySelector('.import-modal-body');
            const drop = document.getElementById('importDropZone');
            const bullets = modal ? modal.querySelectorAll('.import-instructions li').length : 0;
            const vh = window.innerHeight;
            const box = el => { if (!el) return null; const r = el.getBoundingClientRect();
                return { top: Math.round(r.top), bottom: Math.round(r.bottom), h: Math.round(r.height) }; };
            const dropBox = box(drop);
            const bodyBox = box(body);
            return {
                modalOpen: !!modal && getComputedStyle(modal).display !== 'none',
                bullets,
                viewportHeight: vh,
                dropZone: dropBox,
                // Reachable = either already on screen, or inside a body that can scroll to it.
                dropZoneOnScreenNow: dropBox ? (dropBox.top >= 0 && dropBox.bottom <= vh) : false,
                bodyScrollable: body ? body.scrollHeight > body.clientHeight + 1 : false,
                bodyClipped: body ? (getComputedStyle(body).overflowY === 'hidden' && body.scrollHeight > body.clientHeight + 1) : false,
                containerBottomOffscreen: (() => { const b = box(container); return b ? b.bottom > vh + 1 : null; })(),
            };
        });

        // Prove the drop zone can actually be reached, by scrolling to it and re-measuring.
        let reachable = m.dropZoneOnScreenNow;
        if (!reachable) {
            await page.evaluate(() => {
                const d = document.getElementById('importDropZone');
                if (d) d.scrollIntoView({ block: 'center' });
            });
            await page.waitForTimeout(400);
            reachable = await page.evaluate(() => {
                const d = document.getElementById('importDropZone');
                if (!d) return false;
                const r = d.getBoundingClientRect();
                return r.top >= 0 && r.bottom <= window.innerHeight;
            });
        }

        findings.push({ viewport: vp.label, importButton: btn, ...m, dropZoneReachable: reachable });
        await page.screenshot({ path: path.join(OUT, `import-modal-${vp.label}.png`), fullPage: false });
        await ctx.close();
        console.log(`captured ${vp.label}  bullets=${m.bullets} dropReachable=${reachable}`);
    }

    await browser.close();
    fs.writeFileSync(path.join(OUT, 'findings.json'), JSON.stringify(findings, null, 2));

    const bad = findings.filter(f => !f.modalOpen || f.bullets !== 7 || !f.dropZoneReachable || f.bodyClipped);
    console.log('\n' + JSON.stringify(findings, null, 2));
    console.log(bad.length ? `\nPROBLEM at: ${bad.map(b => b.viewport).join(', ')}` : '\nall viewports OK');
    console.log(`screenshots -> ${OUT}`);
    if (bad.length) process.exit(1);
})().catch(e => { console.error('ERROR: ' + e.message); process.exit(1); });
