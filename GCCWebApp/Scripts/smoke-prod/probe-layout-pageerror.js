// Is the "Invalid or unexpected token" page error specific to the certificate views (i.e.
// possibly introduced by CR-050/CR-051), or does it fire on pages neither CR touches?
// LOCALHOST ONLY.

const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';

(async () => {
    const browser = await chromium.launch({ headless: true });
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const page = await ctx.newPage();

    for (const [label, url] of [['login (anonymous, untouched)', '/Account/Login'],
                                ['home  (anonymous, untouched)', '/']]) {
        const errs = [];
        const onErr = e => errs.push(e.message);
        page.on('pageerror', onErr);
        await page.goto(BASE + url, { waitUntil: 'domcontentloaded' }).catch(e => errs.push('NAV: ' + e.message));
        await page.waitForTimeout(4000);
        page.off('pageerror', onErr);
        console.log(`${label.padEnd(32)} -> ${errs.length ? errs.join(' || ') : 'no page errors'}`);
    }

    await browser.close();
})().catch(e => { console.error('PROBE ERROR:', e); process.exit(3); });
