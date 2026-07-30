// DIAGNOSTIC (local dev DB only) — reproduce the client's gccwebapp.com error:
//   "An error occurred while updating the certificate"  on every cert Edit save.
//
// Hypothesis under test: prod's schema is behind the deployed DLL. Production
// deliberately never auto-migrates (Program.cs:922-926), and the CR-015 always-cascade
// SyncProductIdStatusesForCertificateAsync -> ProductIdService does
// `.Include(p => p.Partners)` on product IDs. If the DLL maps CertificateProductIdPartners
// and the database lacks it, EF throws on EVERY save and CertificateController.cs:1385-1388
// swallows it into that generic message.
//
// Method: baseline a plain (non-status) edit, then RENAME the table away, retry the same
// edit, then rename it back. Restore runs in a finally block and is verified before exit.
//
// SAFE: dev DB (GCCDatabase_Dev) only; rename is reversible and touches no business data.
// Refuses to run against anything but localhost.

const { chromium } = require('playwright');
const { execSync } = require('child_process');

const BASE = 'http://localhost:5101';
// MUST be a cert that HAS product IDs — otherwise the CR-015 cascade never queries
// CertificateProductIds/.Include(Partners) and the experiment proves nothing. (First run
// used 26-1734, which has 0 product IDs, and produced a false "not confirmed".)
// 23-0753 has 7. Avoid 26-0146 — it's an Amazon contract-diff baseline cert.
const TEST_CERT_NUM = '23-0753';
const TABLE = 'CertificateProductIdPartners';
const HIDDEN = 'CertificateProductIdPartners_DIAGHIDDEN';
const ERROR_TEXT = 'An error occurred while updating the certificate';

if (!BASE.includes('localhost')) { console.error('ABORT: localhost only.'); process.exit(2); }

function sql(query) {
    return execSync(
        `sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P "DevP@ss2024!SQL" -I -Q "${query.replace(/"/g, '\\"').replace(/\n/g, ' ')}" -h -1 -W`,
        { encoding: 'utf8' }
    );
}
function tableExists(name) {
    const out = sql(`SELECT COUNT(*) FROM sys.tables WHERE name = '${name}';`);
    for (const line of out.split(/\r?\n/)) {
        const t = line.trim();
        if (/^\d+$/.test(t)) return parseInt(t) > 0;
    }
    return false;
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

// Plain edit: change ONLY Location (no status change) — the exact scenario Shawn confirmed
// fails on prod ("every edit fails").
async function postPlainEdit(page, certNum, newLocation) {
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(certNum)}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(1500);
    return await page.evaluate(async (newLocation) => {
        const loc = document.querySelector('input[name="Location"]');
        if (!loc) return { ok: false, status: null, detail: 'no Location input' };
        const form = loc.closest('form');
        if (!form) return { ok: false, status: null, detail: 'no form' };
        loc.value = newLocation;
        const fd = new FormData(form);
        if (fd.has('certificatePdf') && fd.get('certificatePdf').size === 0) fd.delete('certificatePdf');
        if (fd.has('certificateImage') && fd.get('certificateImage').size === 0) fd.delete('certificateImage');
        const r = await fetch(form.action, { method: 'POST', credentials: 'same-origin', body: fd });
        const body = await r.text();
        return { ok: r.ok, status: r.status, url: r.url, body };
    }, newLocation);
}

(async () => {
    let browser, renamed = false, exitCode = 0;
    try {
        if (!tableExists(TABLE)) {
            console.error(`ABORT: ${TABLE} not present in dev DB — cannot run this experiment.`);
            process.exit(2);
        }

        browser = await chromium.launch({ headless: true });
        const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
        const page = await login(ctx, 'swest@anukiconsulting.com');

        const idOut = sql(`SELECT Id FROM Certificates WHERE Number = '${TEST_CERT_NUM}';`).trim();
        const certId = parseInt(idOut.split(/\s+/)[0]);
        const origLoc = sql(`SELECT ISNULL(Location,'') FROM Certificates WHERE Id = ${certId};`)
            .split(/\r?\n/).map(s => s.trim()).find(s => s && !/^\(\d+ rows? affected\)$/i.test(s)) || '';
        console.log(`Fixture: cert ${TEST_CERT_NUM} (id=${certId}), Location='${origLoc}'\n`);

        // ---- [1] BASELINE: schema intact ----
        console.log(`[1] BASELINE — ${TABLE} PRESENT`);
        const r1 = await postPlainEdit(page, TEST_CERT_NUM, 'DIAG Baseline City');
        const err1 = (r1.body || '').includes(ERROR_TEXT);
        const loc1 = sql(`SELECT ISNULL(Location,'') FROM Certificates WHERE Id = ${certId};`)
            .split(/\r?\n/).map(s => s.trim()).find(s => s && !/^\(\d+ rows? affected\)$/i.test(s)) || '';
        console.log(`    POST=${r1.status}  generic-error-shown=${err1}  db Location='${loc1}'`);
        console.log(`    => ${!err1 && loc1 === 'DIAG Baseline City' ? 'EDIT SUCCEEDED (as expected)' : 'EDIT FAILED (unexpected!)'}\n`);

        // ---- [2] REPRODUCE: hide the table ----
        console.log(`[2] REPRODUCE — renaming ${TABLE} -> ${HIDDEN}`);
        sql(`EXEC sp_rename '${TABLE}', '${HIDDEN}';`);
        renamed = true;
        console.log(`    ${TABLE} exists now: ${tableExists(TABLE)} (should be false)`);

        const r2 = await postPlainEdit(page, TEST_CERT_NUM, 'DIAG Repro City');
        const err2 = (r2.body || '').includes(ERROR_TEXT);
        const loc2 = sql(`SELECT ISNULL(Location,'') FROM Certificates WHERE Id = ${certId};`)
            .split(/\r?\n/).map(s => s.trim()).find(s => s && !/^\(\d+ rows? affected\)$/i.test(s)) || '';
        console.log(`    POST=${r2.status}  generic-error-shown=${err2}  db Location='${loc2}'`);
        console.log(`    => ${err2 ? 'REPRODUCED the client error' : 'did NOT reproduce'}\n`);

        // ---- VERDICT ----
        // Three distinct outcomes — do NOT collapse 500 into "did not reproduce".
        // A 500 means the exception escaped the controller's catch: ReloadEditFormDataAsync
        // (called INSIDE that catch) hits the same missing table and throws again. That is a
        // materially different failure mode from the client's friendly message, and it is
        // what rules the missing-table hypothesis OUT.
        const saved2 = loc2 === 'DIAG Repro City';
        console.log('================ VERDICT ================');
        if (err1 || loc1 !== 'DIAG Baseline City') {
            console.log(`INCONCLUSIVE: baseline edit itself failed — fix the fixture first.`);
        } else if (err2) {
            console.log(`CONFIRMED: a missing ${TABLE} produces exactly the client's message`);
            console.log(`on a plain (non-status) edit. Consistent with "every edit fails".`);
        } else if (r2.status >= 500) {
            console.log(`RULED OUT: without ${TABLE} the save fails with HTTP ${r2.status} (raw error page),`);
            console.log(`NOT the client's friendly message. The exception escapes the catch at`);
            console.log(`CertificateController.cs:1385 because ReloadEditFormDataAsync re-throws on the`);
            console.log(`same missing table. The client's error therefore comes from something that`);
            console.log(`throws inside the try but leaves the reload path working.`);
        } else if (!saved2) {
            console.log(`ODD: POST=${r2.status}, no error message, but the edit did not persist.`);
        } else {
            console.log(`NOT CONFIRMED: edit still succeeded without ${TABLE}.`);
        }
        console.log('=========================================\n');

        // restore Location
        sql(`UPDATE Certificates SET Location = '${origLoc.replace(/'/g, "''")}' WHERE Id = ${certId};`);
    } catch (e) {
        console.error('ERROR during diagnostic:', e.message);
        exitCode = 1;
    } finally {
        if (renamed) {
            console.log(`RESTORE — renaming ${HIDDEN} -> ${TABLE}`);
            try {
                sql(`EXEC sp_rename '${HIDDEN}', '${TABLE}';`);
            } catch (e) {
                console.error(`!! RESTORE FAILED: ${e.message}`);
                console.error(`!! Run manually:  EXEC sp_rename '${HIDDEN}', '${TABLE}';`);
                exitCode = 1;
            }
        }
        const ok = tableExists(TABLE);
        console.log(`RESTORE VERIFIED — ${TABLE} present: ${ok}`);
        if (!ok) exitCode = 1;
        if (browser) await browser.close();
        process.exit(exitCode);
    }
})();
