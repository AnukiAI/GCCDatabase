// DIAGNOSTIC (local dev DB only) — root-cause the client's gccwebapp.com error:
//   "An error occurred while updating the certificate"
//   ONLY when the Status is changed and saved. Cert: 26-10126.
//
// HYPOTHESIS: CertificateStatusHistory.CertificateNumber is nvarchar(7) — sized for
// exactly "YY-NNNN" (migration 20251117211135, and still [MaxLength(7)] on the model;
// never widened). A status change is the ONLY path that inserts a status-history row
// (CertificateController.cs:1336), copying certificate.Number into that column.
// "26-10126" is 8 chars -> SQL Server "String or binary data would be truncated" ->
// caught at CertificateController.cs:1385 -> generic message. ReloadEditFormDataAsync
// does NOT touch status history, so it succeeds and we get the FRIENDLY message
// (not a 500) — matching the client's report exactly.
//
// METHOD: take a disposable dev cert, give it an 8-char number, change its status,
// observe. Then restore number + status. Restore runs in finally and is verified.
//
// SAFE: dev DB only; disposable fixture; refuses non-localhost.

const { chromium } = require('playwright');
const { execSync } = require('child_process');

const BASE = 'http://localhost:5101';
const FIXTURE = '26-1734';          // disposable seeded Draft, 0 product IDs
const LONG_NUM = '26-10126';        // 8 chars — the client's actual cert number
const ERROR_TEXT = 'An error occurred while updating the certificate';

if (!BASE.includes('localhost')) { console.error('ABORT: localhost only.'); process.exit(2); }

function sql(q) {
    return execSync(
        `sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P "DevP@ss2024!SQL" -I -Q "${q.replace(/"/g, '\\"').replace(/\n/g, ' ')}" -h -1 -W`,
        { encoding: 'utf8' }
    );
}
function scalar(q) {
    for (const line of sql(q).split(/\r?\n/)) {
        const t = line.trim();
        if (!t || /^\(\d+ rows? affected\)$/i.test(t)) continue;
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

async function postStatusChange(page, certNum, newStatus) {
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNum)}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1500);
    return await page.evaluate(async (newStatus) => {
        const sel = document.querySelector('select[name="Status"]');
        if (!sel) return { ok: false, status: null, detail: 'no Status select' };
        const form = sel.closest('form');
        sel.value = newStatus;
        const fd = new FormData(form);
        if (fd.has('certificatePdf') && fd.get('certificatePdf').size === 0) fd.delete('certificatePdf');
        if (fd.has('certificateImage') && fd.get('certificateImage').size === 0) fd.delete('certificateImage');
        const r = await fetch(form.action, { method: 'POST', credentials: 'same-origin', body: fd });
        const body = await r.text();
        return { ok: r.ok, status: r.status, body };
    }, newStatus);
}

(async () => {
    let browser, certId = null, origNum = null, origStatus = null, exitCode = 0;
    try {
        console.log(`Schema check — CertificateStatusHistory.CertificateNumber:`);
        const width = scalar(`SELECT CAST(max_length/2 AS varchar) FROM sys.columns WHERE object_id=OBJECT_ID('CertificateStatusHistory') AND name='CertificateNumber';`);
        console.log(`  nvarchar(${width})   |   '${LONG_NUM}' is ${LONG_NUM.length} chars -> ${LONG_NUM.length > parseInt(width) ? 'OVERFLOWS' : 'fits'}\n`);

        certId = parseInt(scalar(`SELECT Id FROM Certificates WHERE Number = '${FIXTURE}';`));
        origNum = scalar(`SELECT Number FROM Certificates WHERE Id = ${certId};`);
        origStatus = scalar(`SELECT Status FROM Certificates WHERE Id = ${certId};`);
        console.log(`Fixture: id=${certId}, Number='${origNum}', Status='${origStatus}'\n`);

        browser = await chromium.launch({ headless: true });
        const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
        const page = await login(ctx, 'swest@anukiconsulting.com');

        // ---- [A] CONTROL: 7-char number, change status ----
        console.log(`[A] CONTROL — number '${origNum}' (${origNum.length} chars), change status`);
        sql(`UPDATE Certificates SET Status='Draft' WHERE Id=${certId};`);
        const hA0 = parseInt(scalar(`SELECT COUNT(*) FROM CertificateStatusHistory WHERE CertificateId=${certId};`));
        const rA = await postStatusChange(page, origNum, 'Hold');
        const stA = scalar(`SELECT Status FROM Certificates WHERE Id=${certId};`);
        const hA1 = parseInt(scalar(`SELECT COUNT(*) FROM CertificateStatusHistory WHERE CertificateId=${certId};`));
        const errA = (rA.body || '').includes(ERROR_TEXT);
        console.log(`    POST=${rA.status}  friendly-error=${errA}  db Status='${stA}'  history +${hA1 - hA0}`);
        console.log(`    => ${!errA && stA === 'Hold' ? 'SAVED OK (expected)' : 'FAILED (unexpected)'}\n`);

        // ---- [B] REPRO: 8-char number, change status ----
        console.log(`[B] REPRO — rename to '${LONG_NUM}' (${LONG_NUM.length} chars), change status`);
        sql(`UPDATE Certificates SET Number='${LONG_NUM}', Status='Draft' WHERE Id=${certId};`);
        const hB0 = parseInt(scalar(`SELECT COUNT(*) FROM CertificateStatusHistory WHERE CertificateId=${certId};`));
        const rB = await postStatusChange(page, LONG_NUM, 'Hold');
        const stB = scalar(`SELECT Status FROM Certificates WHERE Id=${certId};`);
        const hB1 = parseInt(scalar(`SELECT COUNT(*) FROM CertificateStatusHistory WHERE CertificateId=${certId};`));
        const errB = (rB.body || '').includes(ERROR_TEXT);
        console.log(`    POST=${rB.status}  friendly-error=${errB}  db Status='${stB}'  history +${hB1 - hB0}`);
        console.log(`    => ${errB ? "REPRODUCED the client's exact message" : 'did not reproduce'}\n`);

        // ---- [C] CONTROL 2: 8-char number, plain edit (no status change) ----
        console.log(`[C] CONTROL 2 — still '${LONG_NUM}', edit Location only (NO status change)`);
        const rC = await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(LONG_NUM)}`, { waitUntil: 'domcontentloaded' });
        await page.waitForTimeout(1200);
        const resC = await page.evaluate(async () => {
            const loc = document.querySelector('input[name="Location"]');
            const form = loc.closest('form');
            loc.value = 'DIAG NoStatusChange';
            const fd = new FormData(form);
            if (fd.has('certificatePdf') && fd.get('certificatePdf').size === 0) fd.delete('certificatePdf');
            if (fd.has('certificateImage') && fd.get('certificateImage').size === 0) fd.delete('certificateImage');
            const r = await fetch(form.action, { method: 'POST', credentials: 'same-origin', body: fd });
            return { status: r.status, body: await r.text() };
        });
        const locC = scalar(`SELECT ISNULL(Location,'') FROM Certificates WHERE Id=${certId};`);
        const errC = (resC.body || '').includes(ERROR_TEXT);
        console.log(`    POST=${resC.status}  friendly-error=${errC}  db Location='${locC}'`);
        console.log(`    => ${!errC ? 'SAVED OK — matches "no other changes cause the error"' : 'also failed'}\n`);

        console.log('================ VERDICT ================');
        if (!errA && errB && !errC) {
            console.log(`ROOT CAUSE CONFIRMED.`);
            console.log(`CertificateStatusHistory.CertificateNumber is nvarchar(${width}); a status change is`);
            console.log(`the only path that inserts into it (CertificateController.cs:1336). An 8-char`);
            console.log(`certificate number overflows the column, SQL Server raises a truncation error,`);
            console.log(`and the catch at CertificateController.cs:1385 turns it into the generic message.`);
            console.log(`Plain edits are unaffected because they never write a status-history row.`);
        } else {
            console.log(`NOT confirmed as stated — A:${!errA} B:${errB} C:${!errC}. Investigate further.`);
        }
        console.log('=========================================\n');
    } catch (e) {
        console.error('ERROR during diagnostic:', e.message);
        exitCode = 1;
    } finally {
        if (certId && origNum) {
            console.log(`RESTORE — Number -> '${origNum}', Status -> '${origStatus}', trim test history`);
            try {
                sql(`DELETE FROM CertificateStatusHistory WHERE CertificateId=${certId} AND Reason LIKE 'User-initiated transition via Edit form%' AND ChangedAt > DATEADD(minute,-30,GETUTCDATE());`);
                sql(`UPDATE Certificates SET Number='${origNum}', Status='${origStatus}' WHERE Id=${certId};`);
                const n = scalar(`SELECT Number FROM Certificates WHERE Id=${certId};`);
                const s = scalar(`SELECT Status FROM Certificates WHERE Id=${certId};`);
                console.log(`RESTORE VERIFIED — Number='${n}', Status='${s}'`);
                if (n !== origNum || s !== origStatus) exitCode = 1;
            } catch (e) {
                console.error(`!! RESTORE FAILED: ${e.message}`);
                console.error(`!! Run manually: UPDATE Certificates SET Number='${origNum}', Status='${origStatus}' WHERE Id=${certId};`);
                exitCode = 1;
            }
        }
        if (browser) await browser.close();
        process.exit(exitCode);
    }
})();
