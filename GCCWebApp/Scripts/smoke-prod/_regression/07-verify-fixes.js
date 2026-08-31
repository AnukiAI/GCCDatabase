// Post-fix verification — F-2 through F-5, driven through the real HTTP path on localhost:5101.
//
// WHY NOT JUST THE UNIT TESTS. The import unit tests call CertificateImportService directly. That
// is below the layer three of these fixes actually live at: the preview's CanApply gating, the
// "(auto-assign)" rendering, the controller's status code, and an [Authorize] attribute are all
// invisible to a service-level test. DEF-369 shipped with nine green unit tests and a broken
// controller for exactly this reason, so each fix is re-checked here through the surface a user
// touches.
//
// Runs against LOCALHOST ONLY — it creates a Partner user and imports certificates.

const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
if (!/localhost|127\.0\.0\.1/.test(BASE)) {
  console.error(`REFUSING to run against ${BASE} — this script writes data and is localhost-only.`);
  process.exit(2);
}

const ADMIN = { user: process.env.LOCAL_ADMIN || 'admin.uat', pass: process.env.LOCAL_ADMIN_PASS || 'TestPass123!' };
const REQ_TIMEOUT = parseInt(process.env.REQ_TIMEOUT || '180000', 10);
const RUN_ID = Date.now().toString(36);
const MARKER = `ZZ_FixVerify_${RUN_ID}`;
const PARTNER_USER = { username: `zz.fixverify.${RUN_ID}`, email: `zz.fixverify.${RUN_ID}@example.invalid`, password: 'GccTest2026!' };

const rows = [];
const rec = (id, name, status, notes = '') => {
  rows.push({ id, name, status, notes });
  console.log(`  [${status}] ${id} — ${name}${notes ? ' :: ' + notes : ''}`);
};

const q = (v) => `"${String(v ?? '').replace(/"/g, '""')}"`;
const ymd = (d) => d.toISOString().slice(0, 10);

async function login(page, user, pass) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill(user);
  await page.locator('#Password').fill(pass);
  await page.locator('button[type="submit"]').first().click();
  await page.waitForLoadState('domcontentloaded');
  await page.waitForTimeout(600);
  return !/\/Account\/Login/i.test(page.url());
}

async function findByMarker(page, marker) {
  const r = await page.request.get(`${BASE}/api/certificate-tiles-internal?search=${encodeURIComponent(marker)}&page=1&pageSize=50`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
  if (r.status() !== 200) return [];
  return (await r.json()).certificates || [];
}

(async () => {
  console.log(`\n=== Post-fix verification (F-2..F-5) — ${BASE} ===\n`);
  const browser = await chromium.launch({ headless: process.env.REG_HEADLESS !== '0' });
  const ctx = await browser.newContext({ acceptDownloads: true, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.setDefaultTimeout(REQ_TIMEOUT);
  page.setDefaultNavigationTimeout(REQ_TIMEOUT);
  const created = [];
  let partnerUserId = null;

  try {
    if (!(await login(page, ADMIN.user, ADMIN.pass))) throw new Error(`admin login failed for ${ADMIN.user}`);
    rec('setup.login', `Signed in as ${ADMIN.user}`, 'PASS');

    // Warm the app: first EF query against a 5,702-row clone JITs and builds the model, and every
    // later timeout would otherwise be measuring startup rather than the change under test.
    const warm0 = Date.now();
    await page.request.get(`${BASE}/api/certificate-tiles-internal?page=1&pageSize=1`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
    rec('setup.warmup', 'Application warmed', 'PASS', `${Date.now() - warm0}ms`);

    // ---------------------------------------------------------------------
    // F-3: an empty result set must be a header-only CSV, not HTTP 400
    // ---------------------------------------------------------------------
    {
      const r = await page.request.get(`${BASE}/Certificate/ExportCertificates?format=csv&searchTerm=zzz-no-such-certificate-${RUN_ID}`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
      const ct = r.headers()['content-type'] || '';
      const body = await r.text();
      const lines = body.split(/\r?\n/).filter((l) => l.trim() !== '');
      rec('F-3.status', 'Empty-scope export returns 200 with a CSV, not 400',
        r.status() === 200 && /csv/i.test(ct) ? 'PASS' : 'FAIL',
        `HTTP ${r.status()} content-type=${ct}`);
      rec('F-3.header-only', 'Empty-scope export is header-only',
        lines.length === 1 && /Certificate/i.test(lines[0]) ? 'PASS' : 'FAIL',
        `${lines.length} line(s); first="${(lines[0] || '').slice(0, 60)}"`);
    }

    // ---------------------------------------------------------------------
    // F-5: product import template is Analyst/Admin only
    // ---------------------------------------------------------------------
    {
      const asAdmin = await page.request.get(`${BASE}/Product/DownloadProductImportTemplate`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
      rec('F-5.admin-allowed', 'Admin can still download the product import template',
        asAdmin.status() === 200 && /csv/i.test(asAdmin.headers()['content-type'] || '') ? 'PASS' : 'FAIL',
        `HTTP ${asAdmin.status()}`);

      const anonCtx = await browser.newContext();
      const anonPage = await anonCtx.newPage();
      const anon = await anonPage.request.get(`${BASE}/Product/DownloadProductImportTemplate`, { failOnStatusCode: false, maxRedirects: 0, timeout: REQ_TIMEOUT });
      const denied = anon.status() === 401 || anon.status() === 403 ||
        (anon.status() >= 300 && anon.status() < 400 && /Account\/(Login|AccessDenied)/i.test(anon.headers()['location'] || ''));
      rec('F-5.anon-denied', 'Anonymous cannot download the product import template',
        denied ? 'PASS' : 'FAIL', `HTTP ${anon.status()} location=${anon.headers()['location'] || '-'}`);
      await anonCtx.close();
    }

    // ---------------------------------------------------------------------
    // F-4: import auto-numbers a blank Certificate #, and each blank row
    //      gets its OWN number (the multi-ASIN grouping trap)
    // ---------------------------------------------------------------------
    const templateText = await (await page.request.get(`${BASE}/CertificateImport/DownloadTemplate`)).text();
    const header = templateText.split(/\r?\n/)[0];
    const cols = header.replace(/^﻿/, '').split('","').map((c) => c.replace(/^"|"$/g, ''));

    const srcResp = await page.request.get(`${BASE}/api/certificate-tiles-internal?page=1&pageSize=1`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
    const src = (await srcResp.json()).certificates[0];
    if (!src) throw new Error('no certificate available on this database to clone dimensions from');

    const today = new Date(), expiry = new Date(Date.now() + 365 * 864e5);
    const cell = (col, productName) => {
      if (/^Certificate #/i.test(col)) return '';                 // the point of the test
      if (/^Certification Type/i.test(col)) return src.certificationType;
      if (/^Status/i.test(col)) return 'Draft';
      if (/^Company$/i.test(col) || col === 'Company') return src.companyName;
      if (/^Product Name/i.test(col)) return productName;
      if (/^Product Category/i.test(col)) return src.productCategory;
      if (/^Product Type/i.test(col)) return src.productType;
      if (/^Subcategory/i.test(col)) return src.productSubcategory;
      if (/^Effective Date/i.test(col)) return ymd(today);
      if (/^Expiration Date/i.test(col)) return ymd(expiry);
      if (/^Location/i.test(col)) return MARKER;
      if (/^Cert Results/i.test(col)) return 'fix verification';
      return '';
    };
    const csv = header + '\r\n' +
      cols.map((c) => q(cell(c, src.productName))).join(',') + '\r\n' +
      cols.map((c) => q(cell(c, src.productName))).join(',') + '\r\n';
    const csvPath = path.join(__dirname, '_artifacts', `fixverify-${RUN_ID}.csv`);
    fs.mkdirSync(path.dirname(csvPath), { recursive: true });
    fs.writeFileSync(csvPath, csv, 'utf8');

    await page.goto(`${BASE}/CertificateImport/Index`, { waitUntil: 'domcontentloaded' });
    await page.setInputFiles('input[name="csvFile"]', csvPath);
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'domcontentloaded', timeout: REQ_TIMEOUT }),
      page.locator('form:has(input[name="csvFile"]) button[type="submit"]').first().click({ timeout: REQ_TIMEOUT, noWaitAfter: true }),
    ]);
    await page.waitForTimeout(1200);

    const preview = await page.evaluate(() => {
      const t = document.body.innerText.replace(/\s+/g, ' ');
      const g = (label) => { const m = new RegExp('(\\d+)\\s+' + label, 'i').exec(t); return m ? parseInt(m[1], 10) : null; };
      return {
        total: g('Total Rows'), added: g('New'), errors: g('Errors'),
        cannotApply: /Cannot Apply Import/i.test(t),
        showsAutoAssign: /\(auto-assign\)/i.test(t),
        hasApplyForm: !!document.querySelector('input[name="importId"]'),
      };
    });

    rec('F-4.preview-accepts-blank', 'Preview accepts a blank Certificate # (no longer an error)',
      preview.errors === 0 && !preview.cannotApply ? 'PASS' : 'FAIL',
      `errors=${preview.errors} cannotApply=${preview.cannotApply}`);
    rec('F-4.preview-two-rows', 'Two blank rows stay TWO new certificates (multi-ASIN grouping)',
      preview.total === 2 && preview.added === 2 ? 'PASS' : 'FAIL',
      `total=${preview.total} new=${preview.added}`);
    rec('F-4.preview-placeholder', 'Preview shows "(auto-assign)" for the blank number',
      preview.showsAutoAssign ? 'PASS' : 'FAIL', `placeholder rendered=${preview.showsAutoAssign}`);
    rec('F-4.preview-applyable', 'Apply form is offered', preview.hasApplyForm ? 'PASS' : 'FAIL');

    if (preview.hasApplyForm) {
      const importId = await page.locator('input[name="importId"]').first().inputValue();
      const tok = await page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
      const ap = await page.request.post(`${BASE}/CertificateImport/Apply`, {
        form: { importId, reason: `fix verification ${RUN_ID}`, acknowledgeReassignments: 'false', acknowledgeOverwrites: 'false', __RequestVerificationToken: tok },
        failOnStatusCode: false,
      });
      rec('F-4.apply', 'Import applies', ap.status() < 400 ? 'PASS' : 'FAIL', `HTTP ${ap.status()}`);

      const hits = await findByMarker(page, MARKER);
      created.push(...hits.map((h) => h.certificateNumber));
      const numbers = hits.map((h) => h.certificateNumber);
      rec('F-4.two-certificates', 'Both rows became certificates',
        hits.length === 2 ? 'PASS' : 'FAIL', `found ${hits.length}: ${numbers.join(', ')}`);
      rec('F-4.numbers-assigned', 'Both received a generated NN-NNNN number',
        numbers.length > 0 && numbers.every((n) => /^\d{2}-\d{3,}$/.test(n)) ? 'PASS' : 'FAIL',
        numbers.join(', '));
      // The whole point: a count of 2 survives both landing on the same number.
      rec('F-4.numbers-distinct', 'The two numbers are DISTINCT',
        new Set(numbers).size === numbers.length && numbers.length === 2 ? 'PASS' : 'FAIL',
        `${numbers.length} rows -> ${new Set(numbers).size} distinct`);
    }

    // ---------------------------------------------------------------------
    // F-2: a Partner must not see not-yet-effective certificates
    // ---------------------------------------------------------------------
    {
      const partnersResp = await page.request.get(`${BASE}/Admin/GetPartners`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
      const partners = partnersResp.status() === 200 ? await partnersResp.json() : null;
      const amazon = (partners?.data || partners || []).find?.((p) => /amazon/i.test(p.name || p.Name || ''));

      if (!amazon) {
        rec('F-2', 'Partner not-yet-effective gate', 'BLOCKED', 'no Amazon partner on this database');
      } else {
        const tok = await page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
        const mk = await page.request.post(`${BASE}/Admin/CreateUser`, {
          headers: { 'Content-Type': 'application/json', RequestVerificationToken: tok },
          data: {
            username: PARTNER_USER.username, email: PARTNER_USER.email, password: PARTNER_USER.password,
            role: 'Partner', partnerIds: [amazon.id ?? amazon.Id], isActive: true, firstName: 'Fix', lastName: 'Verify',
          },
          failOnStatusCode: false,
        });
        const mkBody = await mk.text();
        if (mk.status() >= 400) {
          rec('F-2.partner-user', 'Create a temporary Partner user', 'BLOCKED', `HTTP ${mk.status()} ${mkBody.slice(0, 160)}`);
        } else {
          try { partnerUserId = JSON.parse(mkBody).id ?? JSON.parse(mkBody).userId ?? null; } catch { /* id parsed below if needed */ }
          rec('F-2.partner-user', 'Create a temporary Partner user', 'PASS', PARTNER_USER.username);

          const pCtx = await browser.newContext();
          const pPage = await pCtx.newPage();
          if (!(await login(pPage, PARTNER_USER.username, PARTNER_USER.password))) {
            rec('F-2', 'Partner not-yet-effective gate', 'BLOCKED', 'partner user could not sign in');
          } else {
            // Walk the partner's tile population.
            const tiles = [];
            let p = 1, total = null;
            while (true) {
              const r = await pPage.request.get(`${BASE}/api/CertificateTileExternal?page=${p}&pageSize=200`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
              if (r.status() !== 200) break;
              const b = await r.json();
              total ??= b.totalCount;
              const c = b.certificates || [];
              tiles.push(...c);
              if (!c.length || tiles.length >= total) break;
              p++;
            }
            const todayStr = ymd(new Date());
            const future = tiles.filter((t) => t.effectiveDate && t.effectiveDate.slice(0, 10) > todayStr);
            rec('F-2.tiles-no-future', 'Partner tile view excludes not-yet-effective certificates',
              future.length === 0 ? 'PASS' : 'FAIL',
              `${tiles.length} tiles; not-yet-effective still shown: ${future.length}` +
              (future.length ? ` e.g. ${future.slice(0, 5).map((f) => `${f.certificateNumber}@${f.effectiveDate?.slice(0, 10)}`).join(', ')}` : ''));

            // The original symptom was tile count != export count. Assert they now agree.
            const exp = await pPage.request.get(`${BASE}/Certificate/ExportCertificates?format=csv`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
            const expLines = (await exp.text()).split(/\r?\n/).filter((l) => l.trim() !== '');
            const expRows = Math.max(0, expLines.length - 1);
            rec('F-2.tiles-match-export', 'Partner tile count now agrees with the partner export',
              expRows === tiles.length ? 'PASS' : 'FAIL',
              `tiles=${tiles.length} exportRows=${expRows} delta=${tiles.length - expRows}`);
          }
          await pCtx.close();
        }
      }
    }
  } catch (e) {
    rec('fatal', 'Verification ran without throwing', 'FAIL', e.message);
  } finally {
    // ---- teardown: the imported certificates, then the temporary partner user ----
    try {
      for (const number of [...new Set(created)]) {
        await page.goto(`${BASE}/Certificate/Delete/${encodeURIComponent(number)}`, { waitUntil: 'domcontentloaded' }).catch(() => {});
        const tok = await page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
        await page.request.post(`${BASE}/Certificate/Delete/${encodeURIComponent(number)}`, {
          form: { number, __RequestVerificationToken: tok }, failOnStatusCode: false,
        }).catch(() => {});
      }
      const left = await findByMarker(page, MARKER);
      rec('teardown.certificates', 'Imported fixtures removed',
        left.length === 0 ? 'PASS' : 'FAIL',
        left.length ? `MANUAL CLEANUP REQUIRED: ${left.map((l) => l.certificateNumber).join(', ')}` : 'clean');

      const usersResp = await page.request.get(`${BASE}/Admin/GetUsers`, { failOnStatusCode: false, timeout: REQ_TIMEOUT });
      if (usersResp.status() === 200) {
        const body = await usersResp.json();
        const list = body.data || body.users || body || [];
        const mine = (Array.isArray(list) ? list : []).find((u) => (u.username || u.Username) === PARTNER_USER.username);
        const id = partnerUserId ?? mine?.id ?? mine?.Id;
        if (id) {
          const tok = await page.locator('input[name="__RequestVerificationToken"]').first().inputValue().catch(() => '');
          const del = await page.request.post(`${BASE}/Admin/DeleteUser/${id}`, {
            headers: { RequestVerificationToken: tok }, failOnStatusCode: false,
          });
          rec('teardown.partner-user', 'Temporary Partner user removed',
            del.status() < 400 ? 'PASS' : 'FAIL',
            del.status() < 400 ? PARTNER_USER.username : `MANUAL CLEANUP REQUIRED: user ${PARTNER_USER.username} (HTTP ${del.status()})`);
        } else if (mine || partnerUserId) {
          rec('teardown.partner-user', 'Temporary Partner user removed', 'FAIL', `MANUAL CLEANUP REQUIRED: ${PARTNER_USER.username}`);
        }
      }
    } catch (e) {
      rec('teardown', 'Cleanup completed', 'FAIL', `MANUAL CLEANUP may be required (${MARKER}, ${PARTNER_USER.username}): ${e.message}`);
    }

    await ctx.close();
    await browser.close();

    const pass = rows.filter((r) => r.status === 'PASS').length;
    const fail = rows.filter((r) => r.status === 'FAIL').length;
    const blocked = rows.filter((r) => r.status === 'BLOCKED').length;
    console.log(`\n[verify] ${pass} PASS / ${fail} FAIL / ${blocked} BLOCKED\n`);
    const out = path.join(__dirname, '_results', '07-verify-fixes.json');
    fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, JSON.stringify({ base: BASE, runId: RUN_ID, pass, fail, blocked, rows }, null, 2));
    process.exit(fail > 0 ? 1 : 0);
  }
})();
