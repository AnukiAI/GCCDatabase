/**
 * CR-051 (QC #266) — visual validation of the Status filter panel.
 *
 * Follows the project Playwright standard: full-page captures, all five viewports, explicit waits
 * for AJAX-rendered content (networkidle alone captures mid-load state on these pages).
 *
 * Playwright is installed globally, not in the project, so run with NODE_PATH set:
 *   NODE_PATH="$(npm root -g)" node Scripts/smoke-prod/verify-cr051-screenshots.js
 */

const path = require('path');
const fs = require('fs');
const { chromium } = require('playwright');

const BASE = process.env.CR051_BASE || 'http://localhost:5101';
const USER = process.env.CR051_USER || 'admin.uat';
const PASS = process.env.CR051_PASS || 'TestPass123!';
const OUT = path.join(__dirname, '_results', 'cr051');

const VIEWPORTS = [
    { label: 'desktop', width: 1400, height: 900 },
    { label: 'tablet-land', width: 1024, height: 800 },
    { label: 'tablet-port', width: 768, height: 1024 },
    { label: 'mobile-iph12', width: 390, height: 844 },
    { label: 'mobile-iphse', width: 375, height: 667 },
];

const findings = [];

async function settle(page) {
    try {
        await page.waitForSelector('.certificate-card, .cert-card, .table-body-row, .empty-state', { timeout: 10000 });
    } catch { /* slow load must not fail the capture */ }
    try { await page.waitForLoadState('networkidle', { timeout: 10000 }); } catch {}
    await page.waitForTimeout(800);
}

(async () => {
    fs.mkdirSync(OUT, { recursive: true });
    const browser = await chromium.launch();

    for (const vp of VIEWPORTS) {
        const ctx = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
        const page = await ctx.newPage();

        // Login
        await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 30000 });
        await page.fill('input[name="Email"]', USER);
        await page.fill('input[name="Password"]', PASS);
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 30000 }).catch(() => {}),
            page.click('button[type="submit"], input[type="submit"]'),
        ]);

        await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded', timeout: 30000 });
        await settle(page);

        // The status panel is populated by a SEPARATE /filters XHR from the one that renders the
        // rows, so `settle()` (which waits on rows) can return while the panel still holds its
        // skeleton <li>s. The first run of this harness read 0 checkboxes on desktop and 8 on every
        // later viewport purely because desktop ran first against a cold DB — a harness race, not a
        // desktop-specific fault. Wait for the real inputs before asserting on them.
        let populateError = null;
        try {
            await page.waitForFunction(
                () => document.querySelectorAll('#status-content input[name="status"]').length > 0,
                { timeout: 20000 }
            );
        } catch (e) {
            populateError = 'status checkboxes never appeared within 20s';
        }

        // Did the panel populate with real labels (not PascalCase enum names)?
        const labels = await page.$$eval('#status-content input[name="status"]',
            els => els.map(e => e.value));
        const hasPanel = await page.$('#status-content') !== null;
        const pascal = labels.filter(l => /^ClientApproved|^ExpiredNo/.test(l));

        findings.push({
            viewport: vp.label,
            panelPresent: hasPanel,
            checkboxCount: labels.length,
            pascalCaseLeaks: pascal.length,
            populateError,
            labels: vp.label === 'desktop' ? labels : undefined,
        });

        await page.screenshot({ path: path.join(OUT, `status-filter-${vp.label}.png`), fullPage: true });

        // Desktop only: tick a multi-word status and capture the filtered state.
        if (vp.label === 'desktop') {
            const cb = await page.$('#status-content input[value="Client Approved - Paid"]');
            if (cb) {
                await cb.check();
                await settle(page);
                // settle() can return while #resultsCount still reads "Loading…" — the count is
                // written after its own XHR resolves, and a screenshot taken here shows mid-load
                // state, which is not evidence of anything. Wait for the real count.
                try {
                    await page.waitForFunction(() => {
                        const el = document.getElementById('resultsCount');
                        return el && !/loading/i.test(el.innerText);
                    }, { timeout: 25000 });
                } catch { /* recorded below as whatever it actually says */ }
                await page.waitForTimeout(400);
                const tagText = await page.$eval('#activeFilterTags', el => el.innerText.trim()).catch(() => '');
                const countText = await page.$eval('#resultsCount', el => el.innerText.trim()).catch(() => '');
                findings.push({ viewport: 'desktop-filtered', activeTag: tagText, resultsCount: countText });
                await page.screenshot({ path: path.join(OUT, 'status-filter-desktop-FILTERED.png'), fullPage: true });
            } else {
                findings.push({ viewport: 'desktop-filtered', error: 'checkbox for "Client Approved - Paid" not found' });
            }
        }

        await ctx.close();
        console.log(`captured ${vp.label}`);
    }

    await browser.close();
    fs.writeFileSync(path.join(OUT, 'findings.json'), JSON.stringify(findings, null, 2));
    console.log('\n' + JSON.stringify(findings, null, 2));
    console.log(`\nscreenshots -> ${OUT}`);
})().catch(e => { console.error('ERROR: ' + e.message); process.exit(1); });
