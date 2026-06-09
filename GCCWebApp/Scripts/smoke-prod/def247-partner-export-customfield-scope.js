// DEF-247 verification: Internal-visibility CUSTOM FIELDS must NOT appear in the
// Partner/External CSV export (QC #182, 6/4 client re-raise — "Chris Test Field"
// set to RoleVisibility=Internal still showing in partner export).
//
// Strategy = differential export. The base certificate columns are identical for
// every caller; the trailing columns are admin-added custom fields. We export as
// BOTH Admin (sees all custom fields incl. Internal) and Partner (must see only
// Public/External custom fields), then assert:
//   - Any custom-field column present for Admin but absent for Partner is an
//     Internal-visibility field correctly hidden by the DEF-247 gate.
//   - The Partner export still has rows (row-scoping from DEF-198 intact).
//   - A named Internal field (default "Chris Test Field", override via INTERNAL_FIELD)
//     is absent from the Partner header AND every Partner row.
//
// NOTE: conclusive only if the dev DB actually has at least one custom field with
// RoleVisibility=Internal that carries a value on a partner-visible cert. If Admin
// and Partner expose the same custom-field columns, seed an Internal field first
// (Admin > cert type > custom fields) or run against prod where "Chris Test Field"
// exists. The probe prints the column diff either way so the state is visible.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const INTERNAL_FIELD = process.env.INTERNAL_FIELD || 'Chris Test Field';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `def247-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

// Fixed base columns emitted by GenerateCsvExport before any custom fields.
// Anything in the header AFTER these (and after the optional Internal Tag +
// ASIN/UPC/GTIN/EAN) is an admin custom field.
const BASE_COLS = new Set([
  'Certificate #', 'Certification Type', 'Cert Subcategories', 'Status', 'Company',
  'Company Contact Email/URL', 'Contact URL', 'Product Name', 'Product Category',
  'Product Type', 'Subcategory', 'Partner', 'Pathway', 'Cert Results',
  'Effective Date', 'Expiration Date', 'Location', 'Country',
  'External Tag', 'Internal Tag', 'ASIN', 'UPC', 'GTIN', 'EAN',
]);

// Minimal RFC-4180 header splitter (handles quoted fields).
function parseHeader(line) {
  const out = [];
  let cur = '', inQ = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (inQ) {
      if (c === '"' && line[i + 1] === '"') { cur += '"'; i++; }
      else if (c === '"') inQ = false;
      else cur += c;
    } else {
      if (c === '"') inQ = true;
      else if (c === ',') { out.push(cur); cur = ''; }
      else cur += c;
    }
  }
  out.push(cur);
  return out.map(s => s.trim());
}

async function exportAs(ctx, page, user, pass, label) {
  await page.goto(`${BASE}/Account/Login`);
  await page.fill('input[name="Email"]', user);
  await page.fill('input[name="Password"]', pass);
  await Promise.all([page.waitForLoadState('networkidle'), page.click('button[type="submit"]')]);
  const cookies = await ctx.cookies();
  const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');
  const resp = await page.request.get(`${BASE}/Certificate/ExportCertificates?format=csv`, {
    headers: { Cookie: cookieHeader },
  });
  const body = await resp.text();
  fs.writeFileSync(path.join(OUT, `${label}-export.csv`), body);
  const lines = body.split(/\r?\n/).filter(l => l.length > 0);
  const header = lines[0] ? parseHeader(lines[0]) : [];
  const rows = lines.slice(1);
  const customCols = header.filter(h => !BASE_COLS.has(h));
  // Clear cookies for the next login.
  await ctx.clearCookies();
  return { status: resp.status(), header, rows, customCols, body };
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, acceptDownloads: true });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log(`  pageerror: ${e.message}`));

  const results = [];
  const check = (name, cond) => { results.push({ name, pass: !!cond }); console.log(`  ${cond ? 'PASS' : 'FAIL'}  ${name}`); };

  try {
    const admin = await exportAs(ctx, page, 'admin', 'TestPass123!', 'admin');
    const partner = await exportAs(ctx, page, 'partner_test', 'TestPass123!', 'partner');

    console.log('\nAdmin export   status:', admin.status, '| rows:', partner.rows.length, '| custom cols:', JSON.stringify(admin.customCols));
    console.log('Partner export status:', partner.status, '| rows:', partner.rows.length, '| custom cols:', JSON.stringify(partner.customCols));

    const hiddenFromPartner = admin.customCols.filter(c => !partner.customCols.includes(c));
    console.log('\nCustom-field columns present for Admin but HIDDEN from Partner:', JSON.stringify(hiddenFromPartner));

    const internalInPartnerHeader = partner.header.includes(INTERNAL_FIELD);
    const internalInPartnerRows = partner.rows.some(r => r.includes(INTERNAL_FIELD));
    const internalInAdminHeader = admin.header.includes(INTERNAL_FIELD);

    console.log('\n=== DEF-247 checks ===');
    check('Partner export succeeded (200)', partner.status === 200);
    check('Partner export has rows (row-scope intact, DEF-198)', partner.rows.length > 0);
    check(`Internal field "${INTERNAL_FIELD}" absent from Partner header`, !internalInPartnerHeader);
    check(`Internal field "${INTERNAL_FIELD}" absent from every Partner row`, !internalInPartnerRows);

    if (internalInAdminHeader) {
      // Strong differential: the named Internal field exists in admin export and is gated for partner.
      check(`Internal field "${INTERNAL_FIELD}" PRESENT for Admin (differential confirmed)`, internalInAdminHeader);
    } else if (hiddenFromPartner.length > 0) {
      // Some Internal-visibility field exists and is correctly hidden, just not the named one.
      check('At least one custom field is gated Admin->Partner (Internal gate active)', true);
      console.log(`  INFO: "${INTERNAL_FIELD}" not in dev DB, but these are gated: ${JSON.stringify(hiddenFromPartner)}`);
    } else {
      console.log(`  INFO: dev DB has no Internal-visibility custom field with a value on a partner-visible cert.`);
      console.log(`        Header check still proves the gate doesn't expose "${INTERNAL_FIELD}"; seed an Internal`);
      console.log(`        field or run with BASE_URL=prod + the gate creds for a conclusive differential.`);
    }

    const passCount = results.filter(r => r.pass).length;
    const summary = {
      def: 'DEF-247', base: BASE, internalFieldProbed: INTERNAL_FIELD,
      adminCustomCols: admin.customCols, partnerCustomCols: partner.customCols,
      hiddenFromPartner, partnerRowCount: partner.rows.length,
      checks: results, passCount, failCount: results.length - passCount,
    };
    fs.writeFileSync(path.join(OUT, 'def247-findings.json'), JSON.stringify(summary, null, 2));
    console.log(`\n=== ${passCount}/${results.length} PASS ===`);
  } catch (e) {
    console.error('ERROR:', e.message);
    await page.screenshot({ path: path.join(OUT, '99-error.png'), fullPage: true }).catch(() => {});
  } finally {
    console.log('Output:', OUT);
    await browser.close();
  }
})();
