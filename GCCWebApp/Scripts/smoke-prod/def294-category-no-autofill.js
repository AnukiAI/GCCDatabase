// DEF-294 smoke — Cert Create/Edit no longer auto-fills Product Category from the picked product.
//
// QC #195 decision (Shawn, 2026-06-24): "Never auto-fill Category." Selecting a Product on the
// Create form must NOT populate #ProductCategoryId. On Edit, a cert's OWN saved category must
// still load (initProductCascade / server-rendered selected option) and not be wiped by the
// (now no-op) product pre-select.
//
// LOCALHOST ONLY. Run:  node Scripts/smoke-prod/def294-category-no-autofill.js

const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const NAV_TIMEOUT = parseInt(process.env.SMOKE_NAV_TIMEOUT || '60000', 10);
const CREDS = { email: 'analyst', password: 'TestPass123!' };

if (BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) {
  console.error(`REFUSING to run against ${BASE} — localhost only.`);
  process.exit(2);
}

const results = [];
const record = (id, status, notes = '') => {
  results.push({ id, status, notes });
  console.log(`  [${status}] ${id}${notes ? ' - ' + notes : ''}`);
};

async function login(page) {
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
  await page.locator('#Email').fill(CREDS.email);
  await page.locator('#Password').fill(CREDS.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);
}

// Set a native <select> value and fire the events the app's JS listens for (jQuery .change +
// native 'change'), mirroring how SearchableSelect-wrapped selects are driven elsewhere.
async function selectAndFire(page, selector, value) {
  await page.evaluate(({ selector, value }) => {
    const el = document.querySelector(selector);
    el.value = value;
    if (window.jQuery) window.jQuery(el).trigger('change');
    el.dispatchEvent(new Event('change', { bubbles: true }));
  }, { selector, value });
}

async function firstRealOptionValue(page, selector) {
  return page.evaluate((selector) => {
    const el = document.querySelector(selector);
    if (!el) return null;
    const opt = Array.from(el.options).find((o) => o.value && o.value.trim() !== '');
    return opt ? opt.value : null;
  }, selector);
}

async function run() {
  console.log(`\n[smoke] DEF-294 category-no-autofill — ${BASE}\n`);
  const browser = await chromium.launch({ headless: process.env.SMOKE_HEADLESS !== '0' });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  try {
    await login(page);

    // ===== PART 1: CREATE — product pick must NOT auto-fill Product Category =====
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
    await page.waitForSelector('#CompanyId', { timeout: NAV_TIMEOUT });

    // Baseline: category starts blank.
    const catBefore = await page.evaluate(() => document.querySelector('#ProductCategoryId')?.value ?? null);
    record('create.category-blank-initial', catBefore === '' ? 'PASS' : 'WARN', `value="${catBefore}"`);

    // Pick first real company, wait for products to load.
    const companyVal = await firstRealOptionValue(page, '#CompanyId');
    if (!companyVal) { record('create.company-available', 'FAIL', 'no company options'); }
    else {
      await selectAndFire(page, '#CompanyId', companyVal);
      // Wait until #ProductId is populated with at least one real product.
      let productVal = null;
      for (let i = 0; i < 50; i++) {
        productVal = await firstRealOptionValue(page, '#ProductId');
        if (productVal) break;
        await page.waitForTimeout(200);
      }
      if (!productVal) {
        record('create.product-available', 'WARN', `company ${companyVal} has no products — trying another`);
        // Try a couple more companies to find one with products.
        const companies = await page.evaluate(() =>
          Array.from(document.querySelector('#CompanyId').options).map((o) => o.value).filter((v) => v));
        for (const c of companies) {
          await selectAndFire(page, '#CompanyId', c);
          for (let i = 0; i < 30; i++) {
            productVal = await firstRealOptionValue(page, '#ProductId');
            if (productVal) break;
            await page.waitForTimeout(200);
          }
          if (productVal) { record('create.product-available', 'PASS', `found products under company ${c}`); break; }
        }
      } else {
        record('create.product-available', 'PASS', `company ${companyVal}`);
      }

      if (productVal) {
        // THE KEY ASSERTION: pick a product and confirm Category stays blank.
        await selectAndFire(page, '#ProductId', productVal);
        await page.waitForTimeout(1200); // give any (removed) auto-fill a chance to misbehave
        const catAfter = await page.evaluate(() => document.querySelector('#ProductCategoryId')?.value ?? null);
        record('create.no-autofill-on-product-pick', catAfter === '' ? 'PASS' : 'FAIL',
          `category after product pick = "${catAfter}" (expected blank)`);

        // Evidence screenshot: product selected, Product Category still blank.
        try {
          const shotPath = require('path').join(__dirname, '_results', 'screenshots',
            'def294-create-product-picked-category-blank.png');
          await page.screenshot({ path: shotPath, fullPage: true });
          console.log(`  [info] screenshot -> ${shotPath}`);
        } catch (e) { console.log('  [info] screenshot skipped: ' + e.message); }

        // Sanity: manual cascade still works — set a category KNOWN to have types
        // (cat 11 "Construction / Building Products" = 16 types) and confirm they load.
        const CASCADE_CAT = process.env.SMOKE_CAT_WITH_TYPES || '11';
        const hasCat = await page.evaluate((v) =>
          Array.from(document.querySelector('#ProductCategoryId').options).some((o) => o.value === v), CASCADE_CAT);
        if (hasCat) {
          await selectAndFire(page, '#ProductCategoryId', CASCADE_CAT);
          let typeOpts = 0;
          for (let i = 0; i < 30; i++) {
            typeOpts = await page.evaluate(() =>
              Array.from(document.querySelector('#ProductTypeId').options).filter((o) => o.value).length);
            if (typeOpts > 0) break;
            await page.waitForTimeout(200);
          }
          record('create.manual-cascade-works', typeOpts > 0 ? 'PASS' : 'FAIL',
            `${typeOpts} type option(s) after manually picking category ${CASCADE_CAT}`);
        } else {
          record('create.manual-cascade-works', 'WARN', `category ${CASCADE_CAT} not in dropdown for this company`);
        }
      }
    }

    // ===== PART 2: EDIT — a cert's saved category must still load =====
    // Cert 23-0358 (id 32) has a saved ProductCategoryId=1. The Edit action binds the cert
    // NUMBER (not the int id). Open its Edit form and assert the saved category is still
    // selected after the page (and the now no-op product pre-select) settle — proving the
    // saved-state restore is intact, not wiped by removing the auto-fill.
    const EDIT_CERT_NO = process.env.SMOKE_EDIT_CERT_NO || '23-0358';
    const EXPECT_CAT = process.env.SMOKE_EDIT_EXPECT_CAT || '1';
    const resp = await page.goto(`${BASE}/Certificate/Edit/${EDIT_CERT_NO}`, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
    if (!resp || resp.status() !== 200) {
      record('edit.saved-category-loads', 'WARN', `Edit/${EDIT_CERT_NO} returned ${resp ? resp.status() : 'no response'}`);
    } else if (!(await page.locator('#ProductCategoryId').count())) {
      record('edit.saved-category-loads', 'WARN', `#ProductCategoryId not present on Edit/${EDIT_CERT_NO}`);
    } else {
      await page.waitForTimeout(2000); // let initProductCascade + product pre-select settle
      const catVal = await page.evaluate(() => document.querySelector('#ProductCategoryId')?.value ?? '');
      const catText = await page.evaluate(() => {
        const el = document.querySelector('#ProductCategoryId');
        return el.options[el.selectedIndex]?.text ?? '';
      });
      record('edit.saved-category-loads', catVal === EXPECT_CAT ? 'PASS' : 'FAIL',
        `cert ${EDIT_CERT_NO}: #ProductCategoryId="${catVal}" ("${catText}"), expected "${EXPECT_CAT}"`);
    }
  } catch (e) {
    record('smoke.fatal', 'FAIL', e.message);
  } finally {
    await ctx.close();
    await browser.close();
  }

  const pass = results.filter((r) => r.status === 'PASS').length;
  const fail = results.filter((r) => r.status === 'FAIL').length;
  const warn = results.filter((r) => r.status === 'WARN').length;
  console.log(`\n[smoke] DEF-294: ${pass} PASS / ${fail} FAIL / ${warn} WARN\n`);
  process.exitCode = fail === 0 ? 0 : 1;
}

run();
