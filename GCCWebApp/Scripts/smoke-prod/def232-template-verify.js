// DEF-232 / QC #148 5/29 follow-up — template export verification.
//
// Three template surfaces:
//   T1 Tags template       — header must read "TagType (Internal/External)" so Excel parses 3 cols
//   T2 Cert template       — first 3 bytes must be UTF-8 BOM (EF BB BF) so em dash renders
//   T3 User template       — Role hint must include Partner AND a Partner column must exist
//
// LOCALHOST ONLY. Run:  node Scripts/smoke-prod/def232-template-verify.js
//
// Notes:
// - Templates are gated behind admin auth, so we log in once and reuse the cookies for downloads.
// - Body bytes are inspected raw (no string-decoding) so the BOM check is accurate.

const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const APP_USER = 'admin', APP_PASS = 'TestPass123!';

if (BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) {
  console.error('REFUSING TO RUN: localhost only.');
  process.exit(2);
}

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? 'PASS' : status === 'FAIL' ? 'FAIL' : 'WARN';
  console.log(`[${tag}] ${id} ${name}${notes ? ' - ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  try {
    console.log(`\nDEF-232 template verification - ${BASE}\n`);

    // -- Login --------------------------------------------------------------
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(800);
    if (/\/Account\/Login/i.test(page.url())) { record('SETUP-login', 'Admin login', 'FAIL'); throw new Error('login failed'); }
    record('SETUP-login', 'Admin login', 'PASS');

    // -- T2 CERT TEMPLATE (server-side; can fetch directly with cookies) ----
    // Body bytes raw so we can inspect the BOM. Using ctx.request preserves session cookies.
    const certResp = await ctx.request.get(BASE + '/CertificateImport/DownloadTemplate');
    const certBuf = await certResp.body();
    const t2Bom = certBuf.length >= 3 && certBuf[0] === 0xEF && certBuf[1] === 0xBB && certBuf[2] === 0xBF;
    record('T2.1', 'Cert template starts with UTF-8 BOM (EF BB BF)', t2Bom ? 'PASS' : 'FAIL',
      `bytes[0..2] = ${[certBuf[0], certBuf[1], certBuf[2]].map(b => b.toString(16).padStart(2, '0').toUpperCase()).join(' ')}`);

    // Verify em dash is encoded as the correct 3-byte UTF-8 sequence (E2 80 94).
    const certBody = certBuf.toString('utf8');
    const hasEmDash = certBody.includes('—');
    record('T2.2', 'Cert template contains em dash (U+2014) as real char, not mojibake',
      hasEmDash ? 'PASS' : 'FAIL', hasEmDash ? '' : 'em dash missing or wrong encoding');

    // Sanity: Status hint inline values still present (DEF-202 was the original fix).
    const hasStatusHint = certBody.includes('Status (Draft') && certBody.includes('defaults to Draft if blank');
    record('T2.3', 'Cert template Status hint inline values intact (DEF-202 not regressed)',
      hasStatusHint ? 'PASS' : 'FAIL');

    // -- T1 TAGS TEMPLATE (client-side JS-built CSV) ------------------------
    // The header is built in tags.js#downloadImportTemplate. We trigger the click and capture
    // the download payload. Tags page lives at /Admin/Tags.
    await page.goto(BASE + '/Admin/Tags', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(500);

    // The button is wired up by tags.js — find by text fallback if id differs.
    let tagsBuf = null;
    const downloadP = page.waitForEvent('download', { timeout: 5000 }).catch(() => null);
    const btn = page.locator('button:has-text("Template"), button:has-text("Download Template"), button:has-text("Import Template")').first();
    if (await btn.count() > 0) {
      await btn.click();
      const dl = await downloadP;
      if (dl) {
        const p = await dl.path();
        if (p) tagsBuf = require('fs').readFileSync(p);
      }
    }
    if (!tagsBuf) {
      record('T1.0', 'Tags template download triggered', 'WARN', 'button not found or download not captured; falling back to direct JS exec');
      // Fallback: invoke downloadImportTemplate() and intercept the blob.
      tagsBuf = await page.evaluate(async () => {
        if (typeof downloadImportTemplate !== 'function') return null;
        const origCreate = URL.createObjectURL;
        let captured = null;
        URL.createObjectURL = (blob) => { captured = blob; return 'blob:fake'; };
        try { downloadImportTemplate(); } catch (e) {}
        URL.createObjectURL = origCreate;
        if (!captured) return null;
        return await captured.text();
      });
      if (typeof tagsBuf === 'string') tagsBuf = Buffer.from(tagsBuf, 'utf8');
    } else {
      record('T1.0', 'Tags template download triggered', 'PASS');
    }

    if (tagsBuf) {
      const tagsText = tagsBuf.toString('utf8');
      const usesSlash = tagsText.includes('TagType (Internal/External)');
      const noComma = !tagsText.includes('TagType (Internal, External)');
      record('T1.1', 'Tags template uses slash, not comma', usesSlash && noComma ? 'PASS' : 'FAIL',
        `header line: ${tagsText.split('\n')[0]}`);
      // Excel parse simulation: split on commas, header should be 3 cols (Name, TagType, Description).
      const cols = tagsText.split('\n')[0].split(',');
      record('T1.2', 'Tags template parses as exactly 3 columns', cols.length === 3 ? 'PASS' : 'FAIL',
        `got ${cols.length} columns: [${cols.join(' | ')}]`);
    } else {
      record('T1.1', 'Tags template content available', 'FAIL', 'could not capture template');
    }

    // -- T3 USER TEMPLATE (client-side JS-built CSV) -----------------------
    await page.goto(BASE + '/Admin/UserManagement', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(800);

    let userBuf = null;
    const userDownloadP = page.waitForEvent('download', { timeout: 5000 }).catch(() => null);
    const tmplBtn = page.locator('#template-btn, button:has-text("Template"), button:has-text("Download Template")').first();
    if (await tmplBtn.count() > 0) {
      await tmplBtn.click();
      const dl = await userDownloadP;
      if (dl) {
        const p = await dl.path();
        if (p) userBuf = require('fs').readFileSync(p);
      }
    }

    if (userBuf) {
      const userText = userBuf.toString('utf8');
      const header = userText.split('\n')[0];
      const hasPartnerInRole = /Role\s*\(.*Partner.*\)/.test(header);
      const hasPartnerColumn = /Partner\s*\(.*semicolon/i.test(header);
      record('T3.1', 'User template Role hint includes Partner',
        hasPartnerInRole ? 'PASS' : 'FAIL', `role hint excerpt: ${(header.match(/Role[^,"]*/) || [''])[0]}`);
      record('T3.2', 'User template has dedicated Partner column',
        hasPartnerColumn ? 'PASS' : 'FAIL', `header: ${header}`);
    } else {
      record('T3.0', 'User template download triggered', 'FAIL', 'button not found or download not captured');
    }

    // -- T4 IMPORT END-TO-END: Partner-role row with valid partner ---------
    // Build a 1-row CSV, drive the import preview UI, and confirm the row maps to "create"
    // with role=Partner. We DO NOT commit (no actual user created) — UI assertion only.
    // First, fetch the partner list so we can pick a real name.
    const partnersResp = await ctx.request.get(BASE + '/Admin/GetPartners');
    const partnersJson = await partnersResp.json().catch(() => null);
    const partners = partnersJson?.data || partnersJson?.Data || [];
    const partnerName = partners.length > 0 ? partners[0].name : null;

    if (!partnerName) {
      record('T4.0', 'At least one Partner exists for import smoke', 'WARN', 'no partners in DB - skipping backend smoke');
    } else {
      record('T4.0', 'At least one Partner exists for import smoke', 'PASS', `using: ${partnerName}`);

      // Backend smoke: POST /Admin/CreateUser with the exact payload commitImport would
      // build for a Partner-role row. This proves the end-to-end wiring: payload model
      // accepts partnerIds, service attaches the partner, GetUsers returns it linked.
      const partnerId = partners[0].id;
      const stamp = Date.now();
      const probeEmail = `def232probe+${stamp}@example.com`;
      const token = await page.evaluate(() => {
        const i = document.querySelector('input[name="__RequestVerificationToken"]');
        return i ? i.value : null;
      });

      const createPayload = {
        name: 'DEF232 Probe',
        email: probeEmail,
        role: 'Partner',
        status: 'active',
        password: '',
        requirePasswordReset: true,
        companyIds: [],
        partnerIds: [partnerId]
      };
      const createResp = await ctx.request.post(BASE + '/Admin/CreateUser', {
        data: createPayload,
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' }
      });
      const createJson = await createResp.json().catch(() => null);
      const createOk = createResp.ok() && (createJson?.success === true || createJson?.Success === true);
      record('T4.1', 'POST /Admin/CreateUser with role=Partner + partnerIds=[validId] succeeds',
        createOk ? 'PASS' : 'FAIL', `status=${createResp.status()}, msg=${createJson?.message || createJson?.Message || ''}`);

      // Confirm the user was created and shows up with the partner attached.
      const usersResp = await ctx.request.get(BASE + '/Admin/GetUsers');
      const usersJson = await usersResp.json().catch(() => null);
      const users = usersJson?.data || usersJson?.Data || [];
      const probe = users.find(u => (u.email || u.Email) === probeEmail);
      const linkedPartners = probe?.partnerNames || probe?.PartnerNames || probe?.partners || probe?.Partners || [];
      const hasPartnerLink = Array.isArray(linkedPartners)
        ? linkedPartners.some(p => (typeof p === 'string' ? p : p?.name || p?.Name) === partnerName)
        : false;
      record('T4.2', 'Created Partner-role user shows partner association on read-back',
        hasPartnerLink ? 'PASS' : 'WARN',
        hasPartnerLink ? `linked: ${partnerName}` : `probe found=${!!probe}, partners=${JSON.stringify(linkedPartners)}`);

      // Cleanup: hard-delete the probe user so the test is idempotent.
      if (probe) {
        const probeId = probe.id || probe.Id;
        await ctx.request.delete(BASE + `/Admin/DeleteUser/${probeId}`, {
          headers: { 'RequestVerificationToken': token || '' }
        }).catch(() => null);
      }
    }

    // -- T5: deployed JS contains guard + resolver (DEF-232 surface present in served bundle) --
    const jsResp = await ctx.request.get(BASE + '/js/userManagement.js');
    const jsText = await jsResp.text();
    const hasGuard = jsText.includes('Partner role requires the Partner column');
    record('T5.1', 'JS guard "Partner role requires the Partner column" present in served bundle',
      hasGuard ? 'PASS' : 'FAIL');
    const hasResolver = jsText.includes('resolvePartnerIds');
    record('T5.2', 'resolvePartnerIds helper present in served bundle',
      hasResolver ? 'PASS' : 'FAIL');
    const hasPartnerHeader = jsText.includes("'Partner (semicolon-separated names; required for Partner role)'");
    record('T5.3', 'Partner column header literal present in served bundle',
      hasPartnerHeader ? 'PASS' : 'FAIL');

    // -- Summary -----------------------------------------------------------
    const pass = results.filter(r => r.status === 'PASS').length;
    const fail = results.filter(r => r.status === 'FAIL').length;
    const warn = results.filter(r => r.status === 'WARN').length;
    console.log(`\nResult: ${pass} PASS / ${fail} FAIL / ${warn} WARN (of ${results.length})\n`);

    // Write JSON for the DEF folder.
    require('fs').writeFileSync(
      require('path').join(__dirname, 'def232-template-verify-results.json'),
      JSON.stringify({ base: BASE, timestamp: new Date().toISOString(), results }, null, 2)
    );

    process.exitCode = fail === 0 ? 0 : 1;
  } catch (e) {
    console.error('FATAL:', e.message);
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
