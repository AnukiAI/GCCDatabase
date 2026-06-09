// Create page — selected fields must survive a server-side validation re-render.
// Repro/verify: fill Company → Product → (auto-filled Category/Type/Sub) + a cert subcategory,
// leave ExpirationDate empty to force a server ModelState error, submit, then assert the
// selections are RETAINED on the re-rendered page. FAILs before the fix, PASS after.
//
// LOCALHOST ONLY. node Scripts/smoke-prod/def-create-validation-state.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const results = [];
const rec = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  try {
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill('admin');
    await page.locator('#Password').fill('TestPass123!');
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1000);

    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'networkidle' });
    await page.waitForTimeout(900);

    // pick first real company → loads products
    const companyId = await page.evaluate(() => {
      const s = document.getElementById('CompanyId');
      for (const o of s.options) if (o.value && !o.disabled && !/add new/i.test(o.textContent)) return o.value;
      return null;
    });
    await page.evaluate(cid => { const s = document.getElementById('CompanyId'); s.value = cid; s.dispatchEvent(new Event('change', { bubbles: true })); }, companyId);
    await page.waitForFunction(() => document.getElementById('ProductId')?.options.length > 1, { timeout: 8000 }).catch(() => {});

    // pick first real product → Create auto-fills category/type/sub from the product
    const productId = await page.evaluate(() => {
      const s = document.getElementById('ProductId');
      for (const o of s.options) if (o.value) return o.value;
      return null;
    });
    await page.evaluate(pid => { const s = document.getElementById('ProductId'); s.value = pid; s.dispatchEvent(new Event('change', { bubbles: true })); }, productId);
    await page.waitForTimeout(1500); // let the auto-fill cascade settle

    // force a non-empty Product Type (+ Subcategory) to fully exercise cascade restore
    await page.evaluate(() => { const t = document.getElementById('ProductTypeId'); if (t) for (const o of t.options) if (o.value) { t.value = o.value; t.dispatchEvent(new Event('change', { bubbles: true })); break; } });
    await page.waitForTimeout(800);
    await page.evaluate(() => { const s = document.getElementById('ProductSubcategoryId'); if (s) for (const o of s.options) if (o.value) { s.value = o.value; break; } });

    // pick a certification type that HAS subcategories, then select one cert subcategory
    const certPick = await page.evaluate(() => {
      const s = document.getElementById('CertificationTypeId');
      const all = window.allCertificationTypes || [];
      for (const o of s.options) {
        if (!o.value) continue;
        const kids = all.filter(ct => ct.parentId === parseInt(o.value));
        if (kids.length) return { typeId: o.value, subId: String(kids[0].id), subName: kids[0].name };
      }
      // fallback: first cert type, no subs
      for (const o of s.options) if (o.value) return { typeId: o.value, subId: null, subName: null };
      return { typeId: null, subId: null, subName: null };
    });
    await page.evaluate(tid => { const s = document.getElementById('CertificationTypeId'); s.value = tid; s.dispatchEvent(new Event('change', { bubbles: true })); }, certPick.typeId);
    await page.waitForTimeout(600);
    if (certPick.subId) {
      // click the cert-subcategory option in the multiselect to select it
      await page.evaluate(() => { const t = document.querySelector('#subcategorySelect')?.multiSelectWrapper?.querySelector('.custom-dropdown-trigger'); if (t) t.click(); });
      await page.waitForTimeout(200);
      await page.evaluate(sid => {
        const w = document.querySelector('#subcategorySelect')?.multiSelectWrapper;
        const opt = w?.querySelector(`.custom-dropdown-option[data-value="${sid}"]`);
        if (opt) opt.click();
      }, certPick.subId);
      await page.waitForTimeout(300);
    }

    // select one external + one internal tag (JS-driven multi-selects)
    await page.evaluate(() => {
      const ex = document.getElementById('externalTagSelect');
      if (ex) for (const o of ex.options) if (o.value) { ex.value = o.value; ex.dispatchEvent(new Event('change', { bubbles: true })); break; }
      const inl = document.getElementById('internalTagSelect');
      if (inl) for (const o of inl.options) if (o.value) { inl.value = o.value; inl.dispatchEvent(new Event('change', { bubbles: true })); break; }
    });
    await page.waitForTimeout(400);

    // fill EffectiveDate, leave ExpirationDate EMPTY to force a server validation error
    await page.evaluate(() => {
      const ed = document.querySelector('[name="EffectiveDate"]'); if (ed) ed.value = '2026-01-01';
      const xd = document.querySelector('[name="ExpirationDate"]'); if (xd) xd.value = '';
      const st = document.getElementById('Status'); if (st && !st.value) { for (const o of st.options) if (o.value) { st.value = o.value; break; } }
    });

    // capture intended state BEFORE submit
    const before = await page.evaluate(() => ({
      company: document.getElementById('CompanyId')?.value,
      product: document.getElementById('ProductId')?.value,
      cat: document.getElementById('ProductCategoryId')?.value,
      type: document.getElementById('ProductTypeId')?.value,
      sub: document.getElementById('ProductSubcategoryId')?.value,
      certType: document.getElementById('CertificationTypeId')?.value,
      certSubs: (() => { try { return JSON.parse(document.getElementById('subcategoriesHidden')?.value || '[]'); } catch { return []; } })(),
      extTags: (() => { try { return JSON.parse(document.getElementById('externalTagsHidden')?.value || '[]'); } catch { return []; } })(),
      intTags: (() => { try { return JSON.parse(document.getElementById('internalTagsHidden')?.value || '[]'); } catch { return []; } })(),
    }));
    rec('SETUP', 'Form filled before submit', 'PASS', JSON.stringify(before));

    // hard navigation marker: if it's gone after submit, a fresh document really loaded
    await page.evaluate(() => { window.__navMarker = 'present'; });

    // Force a REAL server round-trip: native form.submit() bypasses jQuery-unobtrusive client
    // validation (which would otherwise block on the empty ExpirationDate) and the submit handler.
    // The server then fails ModelState (ExpirationDate required) and re-renders the Create view.
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
      page.evaluate(() => document.getElementById('certificateCreateForm').submit()),
    ]);
    await page.waitForTimeout(2500); // let the re-rendered page + any JS restoration settle

    const diag = await page.evaluate(() => ({
      navGone: typeof window.__navMarker === 'undefined',                 // true => real document reload
      valSummary: (document.querySelector('.validation-summary-errors, [asp-validation-summary], .alert-danger')?.textContent || '').trim().slice(0, 120),
      expErr: (document.querySelector('[data-valmsg-for="ExpirationDate"], span[data-valmsg-for="ExpirationDate"]')?.textContent || '').trim(),
      prodOptCount: document.getElementById('ProductId')?.options.length,
    }));
    rec('REPOST', 'Real server re-render occurred (nav marker cleared)', diag.navGone ? 'PASS' : 'FAIL',
      `navGone=${diag.navGone} expErr="${diag.expErr}" valSummary="${diag.valSummary}" #ProductId options=${diag.prodOptCount}`);

    const after = await page.evaluate(() => ({
      company: document.getElementById('CompanyId')?.value,
      product: document.getElementById('ProductId')?.value,
      cat: document.getElementById('ProductCategoryId')?.value,
      type: document.getElementById('ProductTypeId')?.value,
      sub: document.getElementById('ProductSubcategoryId')?.value,
      certType: document.getElementById('CertificationTypeId')?.value,
      certSubs: (() => { try { return JSON.parse(document.getElementById('subcategoriesHidden')?.value || '[]'); } catch { return []; } })(),
      extTags: (() => { try { return JSON.parse(document.getElementById('externalTagsHidden')?.value || '[]'); } catch { return []; } })(),
      intTags: (() => { try { return JSON.parse(document.getElementById('internalTagsHidden')?.value || '[]'); } catch { return []; } })(),
      ssCat: (() => { const w = document.getElementById('ProductCategoryId')?.parentNode?.querySelector('.searchable-select-wrapper'); return w ? w.querySelector('input')?.value : ''; })(),
    }));

    rec('R-company', 'Company retained', after.company === before.company ? 'PASS' : 'FAIL', `before=${before.company} after=${after.company}`);
    rec('R-certType', 'Certification Type retained', after.certType === before.certType ? 'PASS' : 'FAIL', `before=${before.certType} after=${after.certType}`);
    rec('R-product', 'Product retained', after.product === before.product ? 'PASS' : 'FAIL', `before=${before.product} after=${after.product}`);
    rec('R-category', 'Product Category retained', after.cat === before.cat ? 'PASS' : 'FAIL', `before=${before.cat} after=${after.cat}`);
    rec('R-type', 'Product Type retained', after.type === before.type ? 'PASS' : 'FAIL', `before=${before.type} after=${after.type}`);
    if (before.sub) rec('R-sub', 'Product Subcategory retained', after.sub === before.sub ? 'PASS' : 'FAIL', `before=${before.sub} after=${after.sub}`);
    rec('R-ss-sync', 'Category SearchableSelect display in sync', (after.cat && after.ssCat && after.ssCat.trim() !== '') ? 'PASS' : 'FAIL', `cat=${after.cat} box="${after.ssCat}"`);
    if (before.certSubs.length) rec('R-certSubs', 'Cert Subcategories retained', JSON.stringify(after.certSubs) === JSON.stringify(before.certSubs) ? 'PASS' : 'FAIL', `before=${JSON.stringify(before.certSubs)} after=${JSON.stringify(after.certSubs)}`);
    if (before.extTags.length) rec('R-extTags', 'External Tags retained', JSON.stringify(after.extTags) === JSON.stringify(before.extTags) ? 'PASS' : 'FAIL', `before=${JSON.stringify(before.extTags)} after=${JSON.stringify(after.extTags)}`);
    if (before.intTags.length) rec('R-intTags', 'Internal Tags retained', JSON.stringify(after.intTags) === JSON.stringify(before.intTags) ? 'PASS' : 'FAIL', `before=${JSON.stringify(before.intTags)} after=${JSON.stringify(after.intTags)}`);

  } catch (e) {
    rec('ERR', 'Exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(__dirname, 'def-create-validation-state-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }
  const p = results.filter(r => r.status === 'PASS').length;
  const f = results.filter(r => r.status === 'FAIL').length;
  const w = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${p} PASS / ${f} FAIL / ${w} WARN`);
  process.exit(f > 0 ? 1 : 0);
})();
