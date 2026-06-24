// QC #200 (DEF-282) + QC #195/#189 (DEF-278) — LIVE verification on gccwebapp.com
// after the 2026-06-24 redeploy. Gives visual ground-truth before any prod SQL runs.
//
// ─ #200 (DEF-282) — "Invalid Date" on imported/in-memory Product IDs ────────────
//   On the cert Create page a not-yet-saved Product ID row has no createdAt. The
//   JS render path (product-id-manager.js:553 -> formatDate/formatDateTime) must
//   show Created = "—" (NOT "Invalid Date") and Created By = "—". This adds one
//   in-memory row via the Add Product ID modal and reads those two cells.
//   EXPECT: the fix is already deployed (the live JS has the guard) -> PASS.
//
// ─ #195/#189 (DEF-278) — Product Category shows "—" until the backfill runs ──────
//   The cert DETAIL views render `cert.ProductCategory?.Name ?? "—"` with NO
//   fallback to the linked product's category. Pre-existing/imported certs have
//   ProductCategoryId = NULL until 2026-06-23-cert-category-backfill-UPDATE.sql is
//   run on prod, so their detail page shows "—".
//   IMPORTANT: the tile/list API STILL falls back to the product category
//   (CertificateTileInternalApiController.cs:248), so the LIST looks correct — the
//   regression is only visible on the DETAIL page. This samples N internal certs,
//   opens each detail page, and tallies how many show "—".
//   EXPECT (backfill NOT yet run): several/most detail pages show "—".
//
// LIVE TEST. Run from repo root:
//   SMOKE_REMOTE_USER=gcc SMOKE_REMOTE_PASS="Cert-Test-Access-2026!" \
//   node Scripts/smoke-prod/qc195-200-live-verify.js
//
// Optional env: BASE_URL (default https://gccwebapp.com),
//   APP_USER / APP_PASS (default analyst / TestPass123!),
//   SAMPLE (default 8) number of detail pages to sample for #195.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = {
  username: process.env.SMOKE_REMOTE_USER || 'gcc',
  password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!',
};
const APP = {
  email: process.env.APP_USER || 'analyst',
  password: process.env.APP_PASS || 'TestPass123!',
};
const SAMPLE = parseInt(process.env.SAMPLE || '8', 10);
const NAV = 60000;

const OUT = path.join(__dirname, '_results');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

const results = [];
const rec = (id, status, detail = '') => {
  results.push({ id, status, detail });
  console.log(`[${status}] ${id}${detail ? '  — ' + detail : ''}`);
};

async function login(page) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: NAV });
  await page.locator('#Email').fill(APP.email);
  await page.locator('#Password').fill(APP.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(1200);
  if (/\/Account\/Login/i.test(page.url())) {
    throw new Error(`login failed for ${APP.email} — still on /Account/Login`);
  }
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    httpCredentials: GATE,
    ignoreHTTPSErrors: true,
  });
  const page = await ctx.newPage();

  // Surface uncaught client errors that might mask a render.
  const pageErrors = [];
  page.on('pageerror', (e) => pageErrors.push(e.message.split('\n')[0]));

  try {
    await login(page);
    rec('login:analyst', 'PASS', `landed=${page.url().replace(BASE, '') || '/'}`);

    // ── PART A — #200 (DEF-282): in-memory Product ID row must not say "Invalid Date"
    try {
      await page.goto(`${BASE}/Certificate/Create`, { waitUntil: 'domcontentloaded', timeout: NAV });
      await page.locator('#addProductId').waitFor({ state: 'visible', timeout: 15000 });
      await page.locator('#addProductId').click();
      await page.locator('#productIdModal').waitFor({ state: 'visible', timeout: 8000 });
      await page.locator('#productIdType').selectOption('ASIN');
      await page.locator('#productIdValue').fill('B0SMOKE777'); // 10-char alphanumeric ASIN
      await page.locator('#saveProductId').click();

      // New row renders via the JS path (the one DEF-282 guards).
      const createdCell = page.locator('#productIdsTableBody .created-date').first();
      await createdCell.waitFor({ state: 'visible', timeout: 8000 });
      const created = (await createdCell.innerText()).trim();
      const createdBy = (await page.locator('#productIdsTableBody .created-by').first().innerText()).trim();

      await page.screenshot({ path: path.join(OUT, 'qc200-create-productid-row.png'), fullPage: true });

      const bad = /invalid date/i.test(created) || /invalid date/i.test(createdBy);
      rec('qc200:in-memory-date', bad ? 'FAIL' : 'PASS',
        `Created="${created}"  CreatedBy="${createdBy}"  (expect "—" / "—", FAIL if "Invalid Date")`);
    } catch (e) {
      await page.screenshot({ path: path.join(OUT, 'qc200-ERROR.png'), fullPage: true }).catch(() => {});
      rec('qc200:in-memory-date', 'ERROR', e.message.split('\n')[0]);
    }

    // ── PART B — #195/#189 (DEF-278): detail pages show "—" until backfill runs
    try {
      const api = await page.evaluate(async () => {
        try {
          const r = await fetch('/api/certificate-tiles-internal?page=1&pageSize=30');
          if (!r.ok) return { error: `HTTP ${r.status}` };
          const d = await r.json();
          const list = (d.certificates || [])
            .map((c) => ({
              number: c.certificateNumber || c.number || c.rawNumber,
              apiCategory: c.productCategory || null, // list value (has product fallback)
            }))
            .filter((c) => c.number);
          return { list, total: d.totalCount };
        } catch (e) {
          return { error: e.message };
        }
      });

      if (api.error || !api.list || api.list.length === 0) {
        rec('qc195:detail-category', 'ERROR', `could not list certs: ${api.error || 'empty'}`);
      } else {
        const sample = api.list.slice(0, SAMPLE);
        const rows = [];
        let blanks = 0;
        let shotCount = 0;
        for (const c of sample) {
          await page.goto(`${BASE}/Certificate/CertificateInternalView/${encodeURIComponent(c.number)}`,
            { waitUntil: 'domcontentloaded', timeout: NAV });
          const detailCat = await page.evaluate(() => {
            const fields = [...document.querySelectorAll('.certificate-field')];
            for (const f of fields) {
              const strong = f.querySelector('strong');
              if (strong && /product category/i.test(strong.textContent)) {
                const span = f.querySelector('span');
                return span ? span.textContent.trim() : '';
              }
            }
            return null; // field not found
          });
          const isBlank = detailCat === '—' || detailCat === '' || detailCat === null;
          if (isBlank) blanks++;
          rows.push({ number: c.number, listCategory: c.apiCategory, detailCategory: detailCat, blank: isBlank });
          // Screenshot the first couple for visual proof.
          if (shotCount < 2) {
            await page.screenshot({
              path: path.join(OUT, `qc195-detail-${c.number.replace(/[^\w-]/g, '_')}.png`),
              fullPage: true,
            });
            shotCount++;
          }
        }

        console.log('\n  cert#         list category            -> detail page shows');
        for (const r of rows) {
          console.log(`  ${String(r.number).padEnd(12)} ${String(r.listCategory || '∅').padEnd(24)} -> ${r.blank ? '"—"  (BLANK)' : '"' + r.detailCategory + '"'}`);
        }

        // The smoke PASSES when it runs cleanly; the headline is the blank tally,
        // which is the evidence for whether the backfill is still needed.
        const verdict = blanks > 0
          ? `${blanks}/${rows.length} detail pages show "—" -> backfill SQL still needed`
          : `0/${rows.length} blank -> categories present (backfill already applied or all certs have own category)`;
        rec('qc195:detail-category', 'PASS', verdict);
      }
    } catch (e) {
      rec('qc195:detail-category', 'ERROR', e.message.split('\n')[0]);
    }

    if (pageErrors.length) rec('client:page-errors', 'WARN', pageErrors.slice(0, 3).join(' | '));
  } catch (e) {
    rec('fatal', 'ERROR', e.message.split('\n')[0]);
  } finally {
    await ctx.close();
    await browser.close();
  }

  const summary = { ts: new Date().toISOString(), base: BASE, results };
  fs.writeFileSync(path.join(OUT, 'qc195-200-results.json'), JSON.stringify(summary, null, 2));
  const failed = results.some((r) => r.status === 'FAIL' || r.status === 'ERROR');
  console.log(`\n[qc195-200] done against ${BASE}. Screenshots + JSON in Scripts/smoke-prod/_results/`);
  process.exit(failed ? 1 : 0);
})();
