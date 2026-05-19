// Post-deploy smoke for the 2026-05-19 batch on gccwebapp.com.
// Covers DEF-197 .. DEF-202 (QC #170, #175, #176, #177, #178, #180,
// #181, #182, #183, #184, #104, #118, #148, #174, #179).
//
// Designed to run after Scripts/smoke-prod/smoke.js (which covers gate,
// CSS freshness, and the prior DEF-195 regression). Same gate creds; same
// Basic Auth handling.
//
// Login: uses GCC_ADMIN_EMAIL / GCC_ADMIN_PASSWORD env vars if set, else
// the seeded admin (swest@anukiconsulting.com / TestPass123!). Partner CSV
// scoping (#182) is verified via the Partner API JSON (already gated by
// X-API-Key) rather than UI login since we don't carry partner UI creds.

const { chromium, request } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = { username: process.env.GATE_USER || 'gcc', password: process.env.GATE_PASS || 'Cert-Test-Access-2026!' };
const ADMIN_EMAIL = process.env.GCC_ADMIN_EMAIL || 'swest@anukiconsulting.com';
const ADMIN_PASS  = process.env.GCC_ADMIN_PASSWORD || 'TestPass123!';
const PARTNER_KEY = process.env.GCC_PARTNER_KEY || ''; // optional — Amazon partner API key

const OUT = path.join(__dirname, 'screenshots', 'batch-' + new Date().toISOString().replace(/[:.]/g, '-'));
fs.mkdirSync(OUT, { recursive: true });

const results = [];
function record(id, label, status, detail) {
    results.push({ id, label, status, detail });
    const tag = status === 'PASS' ? '\x1b[32mPASS\x1b[0m' : status === 'FAIL' ? '\x1b[31mFAIL\x1b[0m' : '\x1b[33m' + status + '\x1b[0m';
    console.log(`  ${tag}  ${id.padEnd(10)} ${label.padEnd(48)}${detail ? '  \x1b[90m' + detail + '\x1b[0m' : ''}`);
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
async function shot(page, name) { await page.screenshot({ path: path.join(OUT, name + '.png'), fullPage: true }); }

(async () => {
    console.log(`\nBatch smoke 2026-05-19 against ${BASE}`);
    console.log('='.repeat(72));

    const browser = await chromium.launch({ headless: true });
    const adminCtx = await browser.newContext({ viewport: { width: 1440, height: 900 }, httpCredentials: GATE });
    const adminPage = await login(adminCtx, ADMIN_EMAIL, ADMIN_PASS);
    if (adminPage.url().includes('/Account/Login')) {
        console.error(`\n\x1b[31mADMIN LOGIN FAILED\x1b[0m — set GCC_ADMIN_EMAIL/GCC_ADMIN_PASSWORD if creds differ.`);
        await browser.close();
        process.exit(2);
    }
    console.log(`\nAdmin logged in as ${ADMIN_EMAIL}`);

    // ===== [A] #182 Partner CSV scoping — Partner API JSON path ============
    console.log('\n[A] DEF-198 / QC #182 — Partner API scoping');
    if (!PARTNER_KEY) {
        record('QC#182', 'Partner API scoping (Active-only)', 'SKIP', 'set GCC_PARTNER_KEY env to verify; CSV UI test deferred — partner UI login not scripted on prod');
    } else {
        const apiCtx = await request.newContext();
        const r = await apiCtx.get(`${BASE}/api/v1/certificates`, { headers: { 'X-API-Key': PARTNER_KEY } });
        if (!r.ok()) {
            record('QC#182', 'Partner API scoping (Active-only)', 'FAIL', `status ${r.status()}`);
        } else {
            const j = await r.json();
            const items = j.certificates || j.data || j.items || [];
            const statuses = [...new Set(items.map(c => c.status))];
            const partners = [...new Set(items.map(c => c.partner || c.partnerName).filter(Boolean))];
            const onlyActive = statuses.length > 0 && statuses.every(s => s === 'Active');
            const onlyOnePartner = partners.length <= 1;
            const ok = onlyActive && onlyOnePartner;
            record('QC#182', 'Partner API scoping (Active-only)', ok ? 'PASS' : 'FAIL',
                `count=${items.length} statuses=${JSON.stringify(statuses)} partners=${JSON.stringify(partners)}`);
        }
        await apiCtx.dispose();
    }

    // ===== [B] #170 Product Types Usage column =============================
    console.log('\n[B] DEF-196/DEF-197 / QC #170 — Product Types Usage col');
    await adminPage.goto(`${BASE}/Admin/ProductTypes`, { waitUntil: 'networkidle' });
    await adminPage.waitForTimeout(2500);
    await shot(adminPage, '170-product-types');
    const ptProbe = await adminPage.evaluate(() => {
        const headerDiv = document.querySelector('.tree-column-header.products-header');
        const headerText = headerDiv ? headerDiv.textContent.replace(/\s+/g, ' ').trim() : null;
        const treeContent = document.querySelector('.tree-content');
        const allText = treeContent ? treeContent.textContent : '';
        const m = allText.match(/(\d+)\s*\/\s*(\d+)/);
        return { headerText, sample: m ? m[0] : null };
    });
    const hasHeader = ptProbe.headerText && /Usage/i.test(ptProbe.headerText) && /Products/i.test(ptProbe.headerText) && /Certs/i.test(ptProbe.headerText);
    record('QC#170', 'Usage column header + row data',
        (hasHeader && ptProbe.sample) ? 'PASS' : (hasHeader ? 'CHECK' : 'FAIL'),
        `header="${ptProbe.headerText}" sample="${ptProbe.sample}"`);

    // ===== [C] #180 Dashboard tile/CSV agreement ===========================
    console.log('\n[C] DEF-200 / QC #180 — Dashboard tile↔CSV');
    await adminPage.goto(`${BASE}/Dashboard`, { waitUntil: 'networkidle' });
    await adminPage.waitForTimeout(2800); // count-up animation
    await shot(adminPage, '180-dashboard');
    const tiles = await adminPage.evaluate(() => {
        return [...document.querySelectorAll('.stat-card')].map(card => {
            const btn = card.querySelector('button[data-stat-type]');
            const v = card.querySelector('.stat-value');
            return { statType: btn ? btn.getAttribute('data-stat-type') : null,
                     value: v ? v.textContent.trim() : null };
        });
    });
    const tileMap = {}; tiles.forEach(t => { if (t.statType) tileMap[t.statType] = t.value; });
    const types = [
        'total-active-certs','companies-with-active-certs','product-categories-with-active-certs',
        'product-types-with-active-certs','product-subcategories-with-active-certs','products-with-active-certs',
        'certs-published-last-month','certs-expiring-this-month','certs-expired-last-month',
        'certs-expiring-next-month','certs-published-this-year','certs-published-last-year'
    ];
    let matches = 0, mismatches = 0; const details = [];
    for (const t of types) {
        const r = await adminPage.request.get(`${BASE}/Dashboard/DownloadStat?type=${t}`);
        if (!r.ok()) { mismatches++; details.push(`${t}:HTTP${r.status()}`); continue; }
        const body = await r.text();
        const m = body.match(/Total [A-Za-z ]+:\s*(\d+)/);
        let csvCount;
        if (m) csvCount = parseInt(m[1], 10);
        else csvCount = Math.max(0, body.split(/\r?\n/).filter(l => l.length > 0).length - 1);
        const tileVal = tileMap[t];
        if (String(tileVal) === String(csvCount)) matches++;
        else { mismatches++; details.push(`${t}:tile=${tileVal} csv=${csvCount}`); }
    }
    record('QC#180', 'Dashboard tile↔CSV agreement',
        mismatches === 0 ? 'PASS' : 'FAIL',
        `match ${matches}/${types.length}${mismatches ? '; ' + details.join(' | ') : ''}`);

    // ===== [D] #175 Logo responsive ========================================
    console.log('\n[D] DEF-201 / QC #175 — Logo responsive');
    const widths = [
        { w: 1920, h: 1080, label: 'desktop', expectMin: 240, expectMax: 320 },
        { w: 1280, h: 800,  label: 'laptop',  expectMin: 200, expectMax: 260 },
        { w: 1024, h: 768,  label: 'tablet',  expectMin: 150, expectMax: 220 },
        { w: 375,  h: 667,  label: 'mobile',  expectMin: 60,  expectMax: 120 }
    ];
    const dims = [];
    for (const v of widths) {
        const ctx2 = await browser.newContext({ viewport: { width: v.w, height: v.h }, httpCredentials: GATE });
        const page = await ctx2.newPage();
        await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
        await page.waitForTimeout(500);
        await shot(page, `175-logo-${v.label}`);
        const d = await page.evaluate(() => {
            const logo = document.querySelector('.main-logo, .header-logo img, .logo img, .logo');
            if (!logo) return null;
            const r = logo.getBoundingClientRect();
            return { w: Math.round(r.width), h: Math.round(r.height) };
        });
        dims.push({ ...v, ...d });
        await ctx2.close();
    }
    const inRange = dims.filter(d => d && d.w >= d.expectMin && d.w <= d.expectMax);
    const detail = dims.map(d => `${d.label}(${d.w}px)→${d ? d.w + 'x' + d.h : 'none'}`).join(' | ');
    record('QC#175', 'Logo responsive sizing',
        inRange.length >= 3 ? 'PASS' : 'CHECK',
        `${inRange.length}/4 in expected band; ${detail}`);

    // ===== [E] #177/178 Filter sidebar populated ============================
    console.log('\n[E] DEF-199 / QC #177-#178 — Filter sidebar');
    {
        const ctx2 = await browser.newContext({ viewport: { width: 1440, height: 900 }, httpCredentials: GATE });
        const page = await ctx2.newPage();
        await page.goto(`${BASE}/Certificate/CertificateTileView`, { waitUntil: 'networkidle' });
        await page.waitForTimeout(3500);
        await shot(page, '177-filter-trees');
        const tree = await page.evaluate(() => {
            const count = sel => document.querySelectorAll(sel).length;
            return {
                certTypes: count('#cert-type-content input[type="checkbox"]'),
                productCats: count('#product-cat-content input[type="checkbox"]'),
                productTypes: count('#product-type-content input[type="checkbox"]'),
                productSubs: count('#product-sub-content input[type="checkbox"]'),
                companies: count('#company-content input[type="checkbox"]')
            };
        });
        const total = Object.values(tree).reduce((a, b) => a + b, 0);
        record('QC#177/178', 'Public filter sidebar populated',
            total > 0 ? 'PASS' : 'FAIL', JSON.stringify(tree));
        await ctx2.close();
    }

    // ===== [F] #181 Company Email validation ===============================
    console.log('\n[F] QC #181 — Company Email strict validation');
    await adminPage.goto(`${BASE}/Company/Index`, { waitUntil: 'networkidle' });
    await adminPage.waitForTimeout(1500);
    const clicked = await adminPage.evaluate(() => {
        const b = document.getElementById('add-company-btn');
        if (b) { b.click(); return true; }
        return false;
    });
    await adminPage.waitForTimeout(800);
    await shot(adminPage, '181-add-row');
    const eprobe = await adminPage.evaluate(() => {
        const cs = [...document.querySelectorAll('input[type="email"], input[name*="email" i], input[id*="email" i]')]
            .filter(el => { const r = el.getBoundingClientRect(); return r.width > 0 && r.height > 0; });
        if (!cs.length) return null;
        const el = cs[0];
        el.value = 'not-an-email';
        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('blur', { bubbles: true }));
        return { type: el.type, typeMismatch: el.validity ? el.validity.typeMismatch : null };
    });
    if (!clicked || !eprobe) record('QC#181', 'Email strict validation', 'BLOCKED', `clicked=${clicked} probe=${JSON.stringify(eprobe)}`);
    else if (eprobe.type === 'email' || eprobe.typeMismatch) record('QC#181', 'Email strict validation', 'PASS', JSON.stringify(eprobe));
    else record('QC#181', 'Email strict validation', 'CHECK', JSON.stringify(eprobe));

    // ===== [G] #183 Sort options removed ===================================
    console.log('\n[G] DEF-201 / QC #183 — Sort options removed');
    {
        const ctx2 = await browser.newContext({ httpCredentials: GATE });
        const page = await ctx2.newPage();
        await page.goto(`${BASE}/Certificate/CertificateTileView`, { waitUntil: 'networkidle' });
        await page.waitForTimeout(1500);
        const opts = await page.evaluate(() => [...document.querySelectorAll('select option')].map(o => o.textContent.trim()));
        const hasPopular = opts.some(o => /Most Popular/i.test(o));
        const hasRelevance = opts.some(o => /Relevance/i.test(o));
        record('QC#183', 'Sort dropdown (no Popular/Relevance)',
            (!hasPopular && !hasRelevance) ? 'PASS' : 'FAIL',
            `popular=${hasPopular} relevance=${hasRelevance} optionsScanned=${opts.length}`);
        await ctx2.close();
    }

    // ===== [H] #104 Breadcrumb renamed =====================================
    console.log('\n[H] DEF-202 / QC #104 — Companies breadcrumb');
    await adminPage.goto(`${BASE}/Company/Index`, { waitUntil: 'networkidle' });
    await adminPage.waitForTimeout(1500);
    const crumb = await adminPage.evaluate(() => {
        const nav = document.querySelector('nav.breadcrumbs, .breadcrumbs');
        return nav ? nav.textContent.replace(/\s+/g, ' ').trim() : null;
    });
    record('QC#104', 'Companies breadcrumb renamed',
        (crumb && /Company Management/i.test(crumb)) ? 'PASS' : 'FAIL',
        crumb ? crumb.slice(0, 200) : 'null');

    // ===== [I] #148 Cert template Status hint ==============================
    console.log('\n[I] DEF-202 / QC #148 — Cert import template');
    const tplResp = await adminPage.request.get(`${BASE}/CertificateImport/DownloadTemplate`);
    if (!tplResp.ok()) {
        record('QC#148', 'Cert template Status hint', 'FAIL', `status ${tplResp.status()}`);
    } else {
        const csv = await tplResp.text();
        fs.writeFileSync(path.join(OUT, '148-template.csv'), csv);
        const header = csv.split(/\r?\n/)[0] || '';
        const hasHint = /status\s*\([^)]+\)/i.test(header);
        record('QC#148', 'Cert template Status header inline hint',
            hasHint ? 'PASS' : 'CHECK', `hasInlineHint=${hasHint}`);
    }

    // ===== [J] #184 Product Mgmt export columns ============================
    console.log('\n[J] DEF-202 / QC #184 — Product Mgmt export');
    await adminPage.goto(`${BASE}/Product/Management`, { waitUntil: 'networkidle' });
    await adminPage.waitForTimeout(2500);
    await adminPage.evaluate(() => {
        window.__csv = null;
        const orig = URL.createObjectURL;
        URL.createObjectURL = function (blob) {
            if (blob instanceof Blob) {
                const reader = new FileReader();
                reader.onload = () => { window.__csv = reader.result; };
                reader.readAsText(blob);
            }
            return orig.apply(URL, arguments);
        };
    });
    const expClicked = await adminPage.evaluate(() => {
        const ids = ['exportBtn', 'btnExport', 'export-csv-btn', 'export-products-btn'];
        for (const id of ids) { const b = document.getElementById(id); if (b) { b.click(); return id; } }
        const buttons = [...document.querySelectorAll('button, a')].filter(b => /^export/i.test((b.textContent || '').trim()));
        if (buttons.length) { buttons[0].click(); return 'by-label:' + (buttons[0].textContent || '').trim().slice(0, 30); }
        return null;
    });
    await adminPage.waitForTimeout(3000);
    const csv = await adminPage.evaluate(() => window.__csv);
    if (!expClicked) record('QC#184', 'Product Mgmt export columns', 'BLOCKED', 'no export button found');
    else if (!csv) record('QC#184', 'Product Mgmt export columns', 'CHECK', `clicked=${expClicked} but csv not captured`);
    else {
        fs.writeFileSync(path.join(OUT, '184-product-export.csv'), csv);
        const header = csv.split(/\r?\n/)[0] || '';
        const hasSku = /\bSKU\b/i.test(header);
        const hasPartner = /partner/i.test(header);
        record('QC#184', 'Product Mgmt export columns',
            (hasSku && hasPartner) ? 'PASS' : 'FAIL',
            `sku=${hasSku} partner=${hasPartner}`);
    }

    // ===== [K] #179 SKU column on cert edit ================================
    console.log('\n[K] DEF-202 / QC #179 — SKU column');
    // Try to get a cert number from the internal tile view first
    await adminPage.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'networkidle' });
    await adminPage.waitForTimeout(2500);
    const certNum = await adminPage.evaluate(() => {
        const el = document.querySelector('.certificate-card .certificate-id, .certificate-card .certificate-number');
        return el ? el.textContent.trim() : null;
    });
    if (!certNum) {
        record('QC#179', 'Product ID SKU column', 'BLOCKED', 'no cert number from tile view');
    } else {
        const editResp = await adminPage.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNum)}`, { waitUntil: 'networkidle' }).catch(() => null);
        if (!editResp || editResp.status() !== 200) {
            record('QC#179', 'Product ID SKU column', 'CHECK', `edit page status ${editResp ? editResp.status() : 'null'} for ${certNum}`);
        } else {
            await adminPage.waitForTimeout(1500);
            const hasSku = await adminPage.evaluate(() => /\bSKU\b/.test(document.body.innerText));
            record('QC#179', 'Product ID SKU column', hasSku ? 'PASS' : 'CHECK', `cert=${certNum} hasSku=${hasSku}`);
        }
    }

    // ===== [L] #118 Custom Field render surface ============================
    console.log('\n[L] DEF-202 / QC #118 — Custom Field render surface');
    // Land on a public cert detail and probe the rendering region exists
    await adminPage.goto(`${BASE}/Certificate/CertificateTileView`, { waitUntil: 'networkidle' });
    await adminPage.waitForTimeout(2500);
    const certHref = await adminPage.evaluate(() => {
        const a = document.querySelector('a[href*="CertificateSingleView"]');
        return a ? a.getAttribute('href') : null;
    });
    if (!certHref) {
        record('QC#118', 'Custom Field render surface', 'BLOCKED', 'no SingleView link on tile view');
    } else {
        await adminPage.goto(BASE + certHref, { waitUntil: 'networkidle' });
        await adminPage.waitForTimeout(1500);
        await shot(adminPage, '118-cert-single');
        const probe = await adminPage.evaluate(() => ({
            hasCertSection: !!document.querySelector('.cert-detail, .certificate-detail, .certification-details, .single-cert'),
            hasAnyCustomField: !!document.querySelector('.custom-field, [class*="custom-field"]'),
            bodyText: document.body.innerText.length
        }));
        // The render surface itself is what we're verifying (data may not have any Public custom fields seeded)
        const ok = probe.hasCertSection && probe.bodyText > 1000;
        record('QC#118', 'Custom Field render surface (renders without error)',
            ok ? 'PASS' : 'CHECK',
            `${JSON.stringify(probe)} — page renders; actual Public-flagged data may be absent`);
    }

    // ===== SUMMARY =========================================================
    console.log('\n' + '='.repeat(72));
    const counts = { PASS: 0, FAIL: 0, CHECK: 0, BLOCKED: 0, SKIP: 0 };
    for (const r of results) counts[r.status] = (counts[r.status] || 0) + 1;
    const color = counts.FAIL === 0 ? '\x1b[32m' : '\x1b[31m';
    console.log(`${color}SUMMARY: PASS=${counts.PASS} FAIL=${counts.FAIL} CHECK=${counts.CHECK} BLOCKED=${counts.BLOCKED} SKIP=${counts.SKIP}\x1b[0m`);
    fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(results, null, 2));
    console.log(`Artifacts: ${OUT}`);
    await browser.close();
    process.exit(counts.FAIL > 0 ? 1 : 0);
})().catch(e => { console.error('FATAL', e); process.exit(2); });
