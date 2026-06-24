// QC #164 — DATE-EXPIRY evidence: ASINs tied to a naturally-aged-out certificate
// are excluded from the Amazon partner export/badging.
//
// What this proves (the gap the 2026-05-12 EVIDENCE.md did NOT cover):
//   A cert that passes its ExpirationDate is auto-flipped to status "Expired" by the
//   real background service (CertificateExpirationBackgroundService, 1-min cadence in Dev),
//   its child product IDs cascade to "Inactive" (CR-015), and the Amazon partner API then
//   drops BOTH the certificate (cert-level PublicStatuses filter) AND every ASIN
//   (product-ID-level Status=="Active" filter).
//
// Flow (all against the LIVE app on localhost:5101, real DB GCCDatabase_Dev):
//   0. Admin login -> discover Amazon partnerId -> mint a fresh Amazon partner API key
//   1. BASELINE  : Amazon export shows cert 26-0147 with its 2 ASINs, status "Active"
//   2. AGE IT OUT: backdate cert 26-0147 ExpirationDate into the past (simulating time passing)
//   3. WAIT      : poll until the background sweep flips Status -> Expired and cascades PIDs -> Inactive
//   4. PROVE     : Amazon export no longer returns the cert; ASIN search returns nothing; by-number 404
//   5. RESTORE   : put the cert back exactly as it was; confirm it reappears in the export
//
// Every API response and DB snapshot is written to docs/tracking/screenshots/qc164/ as a receipt.
//
// Usage:  node Scripts/smoke-prod/qc164-expired-asin-exclusion.js
const { chromium, request } = require('playwright');
const { execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');
const ADMIN = { email: 'admin', password: 'TestPass123!' };
const OUT = path.join(__dirname, '..', '..', 'docs', 'tracking', 'screenshots', 'qc164');
const STAMP = '2026-06-24';

const TARGET = '26-0147';                  // Amazon cert, currently Client Approved - Paid
const TARGET_ID = 65;
const ASINS = ['B01DSSNB5G', 'B0BRNPBZJ8'];
const PAST_DATE = '2026-06-01';            // safely before BusinessToday (2026-06-23)

const SQL = ['-S', 'ANUKI01', '-d', 'GCCDatabase_Dev', '-U', 'GCC_DevUser', '-P', 'DevP@ss2024!SQL',
             '-l', '60', '-b', '-W', '-s', '|'];

const log = [];
function say(m) { console.log(m); log.push(m); }
let pass = 0, fail = 0;
function check(name, ok, detail) {
  if (ok) { pass++; say(`  PASS  ${name}${detail ? '  — ' + detail : ''}`); }
  else { fail++; say(`  FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
}

function sql(query) {
  // The local SQL instance intermittently throws login-timeout under load; retry a few times.
  let lastErr;
  for (let attempt = 1; attempt <= 5; attempt++) {
    try {
      // QUOTED_IDENTIFIER/ANSI_NULLS ON required to write the Certificates table
      // (CR-187 added indexed computed Normalized* columns).
      return execFileSync('sqlcmd', [...SQL, '-Q',
        `SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET NOCOUNT ON; ${query}`], { encoding: 'utf8' }).trim();
    } catch (e) {
      lastErr = e;
      const msg = (e.stderr || '') + (e.stdout || '');
      if (!/timeout|login/i.test(msg)) throw e; // a real SQL error — don't mask it
      Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, 4000); // synchronous 4s backoff
      say(`   (sqlcmd login timeout — retry ${attempt})`);
    }
  }
  throw lastErr;
}

// DB snapshot of the cert + its product IDs (status + expiration), as rows.
function dbSnap(label) {
  const out = sql(
    `SELECT c.Number, c.Status AS CertStatus, CONVERT(varchar(10),c.ExpirationDate,120) AS Exp, ` +
    `pid.ProductIdType AS PType, pid.ProductIdValue AS PVal, pid.Status AS PidStatus ` +
    `FROM Certificates c LEFT JOIN CertificateProductIds pid ON pid.CertificateId=c.Id ` +
    `WHERE c.Id=${TARGET_ID} ORDER BY pid.Id;`);
  say(`\n--- DB: ${label} ---`);
  say(out);
  return out;
}

function sleep(ms) { return new Promise(res => setTimeout(res, ms)); }

async function apiGet(req, key, urlPath) {
  const headers = { Accept: 'application/json' };
  if (key) headers['X-API-Key'] = key;
  // Local SQL is intermittently slow under load: a request can come back as a 5xx
  // (the endpoint's DB query hits a connection/command timeout) OR throw a client-side
  // timeout. BOTH are transient infra noise, not data. We retry with backoff, and if a
  // 5xx STILL persists we throw — never return it. That matters because callers read
  // `body.data`/`findTarget`: silently treating a 500 as "0 results" / "absent" once
  // produced a false FAIL on the baseline search (2026-06-24) and, worse, would let a
  // 500 on the post-expiry call falsely PASS the exclusion proof. A real server error
  // must abort the run loudly (FATAL → emergency restore), not masquerade as a finding.
  let lastErr, last;
  for (let attempt = 1; attempt <= 5; attempt++) {
    try {
      const r = await req.get(BASE + urlPath, { headers, timeout: 90000 });
      let body = null; try { body = await r.json(); } catch (e) {}
      last = { status: r.status(), body };
      if (r.status() < 500) return last;            // 2xx/3xx/4xx are real answers — return them
      say(`   (API GET ${r.status()} — retry ${attempt}: ${urlPath.slice(0, 60)})`);
    } catch (e) {
      lastErr = e;
      if (!/timeout/i.test(e.message)) throw e;     // a non-timeout client error is real — surface it
      say(`   (API GET timeout — retry ${attempt}: ${urlPath.slice(0, 60)})`);
    }
    if (attempt < 5) await sleep(4000);             // backoff before the next attempt
  }
  // exhausted all retries: still 5xx or never got a response — this is a genuine failure
  throw lastErr || new Error(`HTTP ${last && last.status} after 5 attempts: ${urlPath}`);
}

// Pull the target cert (if present) out of a paginated /certificates response.
function findTarget(body) {
  const data = body?.data ?? body?.Data ?? [];
  return Array.isArray(data) ? data.find(c => (c.certificateNumber || c.CertificateNumber) === TARGET) : null;
}

(async () => {
  if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  const adminReq = ctx.request;                                   // admin cookie jar
  const partnerReq = await request.newContext({ ignoreHTTPSErrors: true }); // cookie-free = how Amazon calls
  let original = null;

  try {
    // ---- 0. capture original DB state FIRST (so we can always restore) ----
    say('[0] Capture original state + mint Amazon key');
    const origRows = dbSnap('ORIGINAL (before test)');
    fs.writeFileSync(path.join(OUT, `${STAMP}_db_original.txt`), origRows + '\n');
    // parse original cert status + expiration from the first row
    const firstRow = origRows.split('\n').find(l => l.startsWith(TARGET + '|'));
    const cols = firstRow.split('|');
    original = { status: cols[1], exp: cols[2] };
    check('original cert is in an active/public status', /Approved - Paid|Republished/.test(original.status), `${original.status}, exp ${original.exp}`);

    // admin login
    for (let attempt = 1; attempt <= 3; attempt++) {
      await page.goto(`${BASE}/Account/Login`, { waitUntil: 'networkidle' });
      await page.locator('#Email').waitFor({ state: 'visible', timeout: 15000 });
      await page.locator('#Email').fill(ADMIN.email);
      await page.locator('#Password').fill(ADMIN.password);
      await page.locator('button[type="submit"]').first().click({ noWaitAfter: true });
      await page.waitForURL(u => !/\/Account\/Login/i.test(u.toString()), { timeout: 25000 }).catch(() => {});
      await page.waitForTimeout(1000);
      if (!/\/Account\/Login/i.test(page.url())) break;
      say(`   (login attempt ${attempt} did not navigate — retrying)`);
    }
    check('admin logged in', !/\/Account\/Login/i.test(page.url()), page.url());

    const partnersRes = await apiGet(adminReq, null, '/api/admin/partner/list');
    const partners = partnersRes.body?.data ?? partnersRes.body ?? [];
    const amazon = partners.find(p => (p.name || p.Name) === 'Amazon');
    if (!amazon) throw new Error('No Amazon partner found');
    const amazonId = amazon.id ?? amazon.Id;
    const genResp = await adminReq.post(`${BASE}/api/admin/partner/${amazonId}/apikey`, {
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      data: { description: 'QC164 date-expiry evidence — qc164-expired-asin-exclusion.js' }
    });
    const key = (await genResp.json().catch(() => null))?.data?.apiKey;
    check('fresh Amazon partner key minted', !!key && key.startsWith('gcc_partner_'), key ? key.slice(0, 22) + '…' : 'NONE');
    if (!key) throw new Error('No API key');

    const listUrl = (cb) => `/api/v1/certificates?includeProductIds=true&pageSize=200&_cb=${cb}`;

    // ---- 1. BASELINE: cert + ASINs present, status Active ----
    say('\n[1] BASELINE — Amazon export BEFORE expiry');
    const baseline = await apiGet(partnerReq, key, listUrl('base'));
    fs.writeFileSync(path.join(OUT, `${STAMP}_export_1_baseline.json`), JSON.stringify(baseline.body, null, 2));
    const baseCert = findTarget(baseline.body);
    check(`${TARGET} present in baseline export`, !!baseCert, baseCert ? `status=${baseCert.status}` : 'ABSENT');
    if (baseCert) {
      check(`${TARGET} status literal "Active"`, baseCert.status === 'Active', baseCert.status);
      const vals = (baseCert.productIds || []).map(p => p.value);
      for (const a of ASINS) check(`baseline export includes ASIN ${a}`, vals.includes(a), vals.join(', ') || 'none');
    }
    // ASIN search baseline
    const sBase = await apiGet(partnerReq, key, `/api/v1/certificates/search/productid?productId=${ASINS[0]}&type=ASIN&_cb=base`);
    const sBaseArr = sBase.body?.data ?? [];
    check(`ASIN ${ASINS[0]} findable via productid search (baseline)`, Array.isArray(sBaseArr) && sBaseArr.length > 0, `${sBaseArr.length} cert(s)`);

    // ---- 2. AGE IT OUT — backdate expiration into the past ----
    say('\n[2] AGE OUT — backdate ExpirationDate to ' + PAST_DATE + ' (simulating natural expiry)');
    sql(`UPDATE Certificates SET ExpirationDate='${PAST_DATE}' WHERE Id=${TARGET_ID};`);
    dbSnap('after backdating expiration (status not yet swept)');

    // ---- 3. WAIT for the real background sweep to flip status + cascade PIDs ----
    say('\n[3] WAIT — polling for CertificateExpirationBackgroundService to auto-expire');
    let swept = false;
    for (let i = 0; i < 30; i++) {
      const st = sql(`SELECT Status FROM Certificates WHERE Id=${TARGET_ID};`).split('\n').pop().trim();
      if (/Expired/i.test(st)) { swept = true; say(`   swept after ~${i * 5}s: Status='${st}'`); break; }
      await page.waitForTimeout(5000);
    }
    check('background sweep flipped cert to Expired', swept, swept ? '' : 'TIMEOUT (>150s)');
    const dbAfter = dbSnap('after background sweep (expect CertStatus=Expired, PidStatus=Inactive)');
    fs.writeFileSync(path.join(OUT, `${STAMP}_db_after_expiry.txt`), dbAfter + '\n');
    check('all child ASIN product IDs cascaded to Inactive', !/\|Active\s*$/m.test(dbAfter) && /Inactive/.test(dbAfter),
      'see db_after_expiry.txt');
    // prove it was the AUTOMATED service, not a manual edit
    const hist = sql(`SELECT TOP 1 PreviousStatus, NewStatus, ChangedByUsername, WorkflowStep FROM CertificateStatusHistory ` +
      `WHERE CertificateId=${TARGET_ID} ORDER BY Id DESC;`);
    say('\n--- CertificateStatusHistory (most recent) ---');
    say(hist);
    check('auto-expire logged by System / Auto-Expiration', /System/.test(hist) && /Auto-Expiration/.test(hist), hist.split('\n').pop());

    // ---- 4. PROVE exclusion from the Amazon export ----
    say('\n[4] PROVE — Amazon export AFTER expiry');
    const post = await apiGet(partnerReq, key, listUrl('post'));
    fs.writeFileSync(path.join(OUT, `${STAMP}_export_2_after_expiry.json`), JSON.stringify(post.body, null, 2));
    const postCert = findTarget(post.body);
    check(`${TARGET} EXCLUDED from export after expiry`, !postCert, postCert ? 'STILL PRESENT' : 'absent ✓');
    // the ASINs must not appear anywhere in the payload
    const blob = JSON.stringify(post.body);
    for (const a of ASINS) check(`ASIN ${a} absent from entire export payload`, !blob.includes(a), blob.includes(a) ? 'LEAKED' : 'gone ✓');
    // ASIN search now returns nothing
    const sPost = await apiGet(partnerReq, key, `/api/v1/certificates/search/productid?productId=${ASINS[0]}&type=ASIN&_cb=post`);
    const sPostArr = sPost.body?.data ?? [];
    check(`ASIN ${ASINS[0]} no longer found via productid search`, Array.isArray(sPostArr) && sPostArr.length === 0, `${sPostArr.length} cert(s)`);
    // by-number now 404 for the partner
    const byNum = await apiGet(partnerReq, key, `/api/v1/certificates/number/${TARGET}`);
    check(`GET /certificates/number/${TARGET} -> 404 for partner`, byNum.status === 404, `status ${byNum.status}`);

    // ---- 5. RESTORE ----
    say('\n[5] RESTORE — return cert ' + TARGET + ' to original state');
    sql(`UPDATE Certificates SET ExpirationDate='${original.exp}', Status='${original.status.replace(/'/g, "''")}' WHERE Id=${TARGET_ID};`);
    sql(`UPDATE CertificateProductIds SET Status='Active' WHERE CertificateId=${TARGET_ID};`);
    const restored = dbSnap('after restore');
    check('cert restored to original status', restored.includes(original.status) && restored.includes(original.exp), `${original.status}, ${original.exp}`);
    check('child ASIN product IDs restored to Active', !/Inactive/.test(restored), 'see snapshot');
    // confirm it reappears in the export
    const verify = await apiGet(partnerReq, key, listUrl('verify'));
    check(`${TARGET} reappears in export after restore`, !!findTarget(verify.body), findTarget(verify.body) ? 'present ✓' : 'STILL ABSENT');

  } catch (e) {
    fail++;
    say(`\nFATAL: ${e.message}`);
    // best-effort restore on failure
    try {
      if (original) {
        sql(`UPDATE Certificates SET ExpirationDate='${original.exp}', Status='${original.status.replace(/'/g, "''")}' WHERE Id=${TARGET_ID};`);
        sql(`UPDATE CertificateProductIds SET Status='Active' WHERE CertificateId=${TARGET_ID};`);
        say('   (emergency restore applied)');
      }
    } catch (e2) { say('   EMERGENCY RESTORE FAILED: ' + e2.message); }
  } finally {
    await partnerReq.dispose().catch(() => {});
    await ctx.close();
    await browser.close();
  }

  say(`\n=== QC #164 date-expiry exclusion: ${pass} passed, ${fail} failed ===`);
  fs.writeFileSync(path.join(OUT, `${STAMP}_run_log.txt`), log.join('\n') + '\n');
  process.exit(fail ? 1 : 0);
})();
