// Locate the "Invalid or unexpected token" page error seen on the internal tile/list view:
// which script, which line, and is it present on OTHER views too (i.e. pre-existing)?
// LOCALHOST ONLY.

const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';

async function login(page) {
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('#Email', 'admin');
    await page.fill('#Password', 'TestPass123!');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: 30000 }).catch(() => {}),
        page.click('button[type="submit"]'),
    ]);
}

const VIEWS = [
    ['internal tile/list', '/Certificate/CertificateTileViewInternal'],
    ['public tile',        '/Certificate/CertificateTileView'],
    ['dashboard',          '/Dashboard'],
];

(async () => {
    const browser = await chromium.launch({ headless: true });
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await ctx.newPage();
    await login(page);

    for (const [label, url] of VIEWS) {
        const errs = [];
        const onErr = e => errs.push({ msg: e.message, stack: (e.stack || '').split('\n').slice(0, 4).join(' | ') });
        page.on('pageerror', onErr);
        await page.goto(BASE + url, { waitUntil: 'domcontentloaded' });
        await page.waitForTimeout(6000);
        page.off('pageerror', onErr);

        console.log(`\n--- ${label}  (${url}) ---`);
        if (!errs.length) { console.log('    no page errors'); continue; }
        errs.forEach(e => {
            console.log(`    ${e.msg}`);
            console.log(`      stack: ${e.stack || '(none)'}`);
        });
    }

    // Which script tags exist inline on the internal view, and do any contain an obviously
    // broken literal? Print the first line of every inline script for eyeballing.
    await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    const scripts = await page.evaluate(() =>
        Array.from(document.querySelectorAll('script')).map((s, i) => ({
            i, src: s.src || '(inline)', len: s.src ? 0 : s.textContent.length,
            head: s.src ? '' : s.textContent.trim().slice(0, 70).replace(/\s+/g, ' '),
        })));
    console.log('\n--- scripts on the internal view ---');
    scripts.forEach(s => console.log(`    [${s.i}] ${s.src === '(inline)' ? `inline len=${s.len} :: ${s.head}` : s.src.replace(BASE, '')}`));

    await browser.close();
})().catch(e => { console.error('PROBE ERROR:', e); process.exit(3); });
