// DEF-203 verification v2: use existing fixture cert 26-0026 (Draft) as the
// subject. Snapshot status + history-row count at start, run valid + invalid
// transitions, restore at end. Less invasive than seeding a new cert; uses a
// known-good Edit page that we've verified renders the Status select.

const { chromium } = require('playwright');
const { execSync } = require('child_process');
const BASE = 'http://localhost:5101';
const TEST_CERT_NUM = '26-0026';

function sql(query) {
    return execSync(
        `sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P "DevP@ss2024!SQL" -I -Q "${query.replace(/"/g, '\\"').replace(/\n/g, ' ')}" -h -1 -W`,
        { encoding: 'utf8' }
    );
}
function getCertStatus(certId) {
    // sqlcmd -W collapses internal whitespace but keeps the row as a single line.
    // We get just the Status text plus trailing whitespace — trim and return.
    const out = sql(`SELECT Status FROM Certificates WHERE Id = ${certId};`);
    // Take the first non-empty line that isn't "(N rows affected)"
    for (const line of out.split(/\r?\n/)) {
        const t = line.trim();
        if (!t) continue;
        if (/^\(\d+ rows? affected\)$/i.test(t)) continue;
        return t;
    }
    return '';
}

async function login(ctx, email) {
    const page = await ctx.newPage();
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.fill('input[name="Email"]', email);
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([
        page.waitForURL(u => !u.toString().includes('/Account/Login'), { timeout: 30000 }).catch(() => {}),
        page.click('button[type="submit"]')
    ]);
    return page;
}

async function postEditStatus(adminPage, certNum, newStatus) {
    await adminPage.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNum)}`, { waitUntil: 'domcontentloaded' });
    await adminPage.waitForTimeout(1500);
    return await adminPage.evaluate(async (newStatus) => {
        const sel = document.querySelector('select[name="Status"]');
        if (!sel) return { ok: false, status: null, detail: 'no Status select' };
        const form = sel.closest('form');
        if (!form) return { ok: false, status: null, detail: 'no form' };
        sel.value = newStatus;
        const fd = new FormData(form);
        if (fd.has('certificatePdf') && fd.get('certificatePdf').size === 0) fd.delete('certificatePdf');
        if (fd.has('certificateImage') && fd.get('certificateImage').size === 0) fd.delete('certificateImage');
        const r = await fetch(form.action, { method: 'POST', credentials: 'same-origin', body: fd });
        return { ok: r.ok, status: r.status, url: r.url };
    }, newStatus);
}

(async () => {
    const browser = await chromium.launch({ headless: true });
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    const adminPage = await login(ctx, 'swest@anukiconsulting.com');

    // Snapshot starting state of the fixture cert
    const certIdRow = sql(`SELECT Id FROM Certificates WHERE Number = '${TEST_CERT_NUM}';`).trim();
    const certId = parseInt(certIdRow.split(/\s+/)[0]);
    const startStatus = getCertStatus(certId);
    const startHistory = parseInt(sql(`SELECT COUNT(*) FROM CertificateStatusHistory WHERE CertificateId = ${certId};`).trim());
    console.log(`Fixture: cert ${TEST_CERT_NUM} (id=${certId}), starting Status='${startStatus}', existing history rows=${startHistory}`);

    if (startStatus !== 'Draft') {
        console.log(`Setting cert to Draft for the test (will restore '${startStatus}' at end)…`);
        sql(`UPDATE Certificates SET Status = 'Draft' WHERE Id = ${certId};`);
    }

    let passes = 0, total = 0;
    const note = (msg, ok) => { total++; if (ok) passes++; console.log(`  ${ok ? '✓' : '✗'} ${msg}`); };

    // [A] Valid transitions through the matrix
    console.log('\n[A] Valid transitions (matrix-allowed)');
    const validPath = [
        { from: 'Draft', to: 'Client Approved - Paid' },
        { from: 'Client Approved - Paid', to: 'Hold' },
        { from: 'Hold', to: 'Client Approved - Paid' },
        { from: 'Client Approved - Paid', to: 'Republished' },
        { from: 'Republished', to: 'Expired' }
    ];
    for (const hop of validPath) {
        let r = await postEditStatus(adminPage, TEST_CERT_NUM, hop.to);
        let after = getCertStatus(certId);
        // Retry once if DB didn't reflect — first-hop sometimes flakes when
        // the Edit page's JS cascade hasn't fully wired up dropdown values
        // before the FormData capture. Re-loading the page and re-posting
        // resolves it reliably.
        if (after !== hop.to) {
            await adminPage.waitForTimeout(800);
            r = await postEditStatus(adminPage, TEST_CERT_NUM, hop.to);
            after = getCertStatus(certId);
        }
        note(`${hop.from.padEnd(25)} → ${hop.to.padEnd(25)} (POST=${r.status}, db=${after})`,
            after === hop.to);
    }

    // [B] Invalid transitions — reject + status unchanged
    console.log('\n[B] Invalid transitions (matrix-forbidden) — should be REJECTED');
    sql(`UPDATE Certificates SET Status = 'Draft' WHERE Id = ${certId};`);
    const invalidProbes = [
        { from: 'Draft', to: 'Republished' },
        { from: 'Draft', to: 'Expired' },
        { from: 'Draft', to: 'Expired - No Annual Verification' }
    ];
    for (const probe of invalidProbes) {
        const before = getCertStatus(certId);
        if (before !== probe.from) sql(`UPDATE Certificates SET Status = 'Draft' WHERE Id = ${certId};`);
        const r = await postEditStatus(adminPage, TEST_CERT_NUM, probe.to);
        const after = getCertStatus(certId);
        note(`${probe.from.padEnd(25)} ↛ ${probe.to.padEnd(25)} (POST=${r.status}, db=${after})`,
            after === probe.from);
    }

    // [C] Audit history rows
    console.log('\n[C] CertificateStatusHistory new rows');
    const endHistory = parseInt(sql(`SELECT COUNT(*) FROM CertificateStatusHistory WHERE CertificateId = ${certId};`).trim());
    const newRows = endHistory - startHistory;
    console.log(`  History rows added during test: ${newRows} (was ${startHistory}, now ${endHistory})`);
    note(`At least 5 new history rows for the 5 valid transitions`, newRows >= 5);

    // Restore original state
    console.log(`\nRestore: set cert back to '${startStatus}' and trim test-added history rows…`);
    sql(`UPDATE Certificates SET Status = '${startStatus}' WHERE Id = ${certId};`);
    sql(`DELETE FROM CertificateStatusHistory WHERE CertificateId = ${certId} AND ChangedAt > DATEADD(MINUTE, -30, GETUTCDATE());`);

    console.log(`\n${passes}/${total} pass`);
    await ctx.close();
    await browser.close();
    process.exit(passes === total ? 0 : 1);
})().catch(e => { console.error('FATAL', e); process.exit(2); });
