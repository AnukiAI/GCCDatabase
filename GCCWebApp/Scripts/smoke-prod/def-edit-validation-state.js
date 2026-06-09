// Edit page — EDITED selections must survive a server-side validation re-render.
// Opens an existing cert, CHANGES Product Category + adds an External Tag + a cert Subcategory,
// leaves ExpirationDate empty to force a server ModelState error, native-submits (bypassing
// jQuery client validation), then asserts the EDITED values are retained (not reverted/lost).
//
// LOCALHOST ONLY. node Scripts/smoke-prod/def-edit-validation-state.js

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

    const tiles = (await (await ctx.request.get(`${BASE}/api/certificate-tiles-internal?pageSize=50&limit=50`)).json())?.certificates ?? [];
    const num = (tiles.map(t => t.certificateNumber).filter(n => n && /^\d{2}-\d{4}$/.test(n)))[0];
    rec('SETUP-cert', 'Edit candidate', num ? 'PASS' : 'FAIL', `num=${num}`);
    if (!num) throw new Error('no cert');

    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(num)}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1800);

    // EDIT: switch CertificationType to one that HAS subcategories so we can pick one, add an
    // external tag not currently selected. Capture the intended post-edit state.
    const intended = await page.evaluate(async () => {
      const out = {};
      // pick a certification type that has children (subcategories)
      const all = window.allCertificationTypes || [];
      const sel = document.getElementById('CertificationTypeId');
      let parentWithKids = null;
      if (sel) for (const o of sel.options) { if (o.value && all.some(c => c.parentId === parseInt(o.value))) { parentWithKids = o.value; break; } }
      if (parentWithKids) { sel.value = parentWithKids; sel.dispatchEvent(new Event('change', { bubbles: true })); }
      out.certType = parentWithKids;

      // external tag: pick an option not already selected
      let cur = []; try { cur = JSON.parse(document.getElementById('externalTagsHidden')?.value || '[]'); } catch {}
      const exSel = document.getElementById('externalTagSelect');
      let added = null;
      if (exSel) for (const o of exSel.options) { if (o.value && !cur.includes(parseInt(o.value))) { exSel.value = o.value; exSel.dispatchEvent(new Event('change', { bubbles: true })); added = parseInt(o.value); break; } }
      out.extTagAdded = added;
      return out;
    });
    await page.waitForTimeout(700);
    // pick a cert subcategory now that options should be loaded for the new type
    await page.evaluate(() => {
      const ms = document.getElementById('subcategorySelect')?.multiSelectWrapper;
      if (ms) {
        ms.querySelector('.custom-dropdown-trigger')?.click();
        const opt = ms.querySelector('.custom-dropdown-option[data-value]');
        if (opt) opt.click();
      }
    });
    await page.waitForTimeout(400);

    const before = await page.evaluate(() => ({
      cat: document.getElementById('ProductCategoryId')?.value,
      product: document.getElementById('ProductId')?.value,
      certType: document.getElementById('CertificationTypeId')?.value,
      extTags: (() => { try { return JSON.parse(document.getElementById('externalTagsHidden')?.value || '[]'); } catch { return []; } })(),
      certSubs: (() => { try { return JSON.parse(document.getElementById('subcategoriesHidden')?.value || '[]'); } catch { return []; } })(),
    }));
    rec('SETUP-edited', 'Edited state before submit', 'PASS', JSON.stringify({ ...before, ...intended }));

    await page.evaluate(() => { const xd = document.querySelector('[name="ExpirationDate"]'); if (xd) xd.value = ''; });
    await page.evaluate(() => { window.__navMarker = 'present'; });

    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle' }).catch(() => {}),
      page.evaluate(() => { const f = document.getElementById('certificateEditForm') || document.querySelector('form'); f.submit(); }),
    ]);
    await page.waitForTimeout(2500);

    const diag = await page.evaluate(() => ({
      navGone: typeof window.__navMarker === 'undefined',
      expErr: (document.querySelector('[data-valmsg-for="ExpirationDate"]')?.textContent || '').trim(),
    }));
    rec('REPOST', 'Real server re-render (nav marker cleared)', diag.navGone ? 'PASS' : 'FAIL', `navGone=${diag.navGone} expErr="${diag.expErr}"`);

    const after = await page.evaluate(() => ({
      cat: document.getElementById('ProductCategoryId')?.value,
      product: document.getElementById('ProductId')?.value,
      certType: document.getElementById('CertificationTypeId')?.value,
      extTags: (() => { try { return JSON.parse(document.getElementById('externalTagsHidden')?.value || '[]'); } catch { return []; } })(),
      certSubs: (() => { try { return JSON.parse(document.getElementById('subcategoriesHidden')?.value || '[]'); } catch { return []; } })(),
    }));

    rec('R-category', 'Product Category retained (posted)', after.cat === before.cat && !!after.cat ? 'PASS' : 'FAIL', `before=${before.cat} after=${after.cat}`);
    rec('R-product', 'Product retained (posted)', after.product === before.product && !!after.product ? 'PASS' : 'FAIL', `before=${before.product} after=${after.product}`);
    rec('R-certType', 'Certification Type retained (posted)', after.certType === before.certType && !!after.certType ? 'PASS' : 'FAIL', `before=${before.certType} after=${after.certType}`);
    rec('R-extTags', 'Edited External Tags retained (not reverted to DB)', before.extTags.length ? (JSON.stringify([...after.extTags].sort()) === JSON.stringify([...before.extTags].sort()) ? 'PASS' : 'FAIL') : 'WARN', `before=${JSON.stringify(before.extTags)} after=${JSON.stringify(after.extTags)}`);
    rec('R-certSubs', 'Edited Cert Subcategories retained', before.certSubs.length ? (JSON.stringify([...after.certSubs].sort()) === JSON.stringify([...before.certSubs].sort()) ? 'PASS' : 'FAIL') : 'WARN', `before=${JSON.stringify(before.certSubs)} after=${JSON.stringify(after.certSubs)}`);

  } catch (e) {
    rec('ERR', 'Exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(__dirname, 'def-edit-validation-state-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }
  const p = results.filter(r => r.status === 'PASS').length;
  const f = results.filter(r => r.status === 'FAIL').length;
  console.log(`\nSUMMARY: ${p} PASS / ${f} FAIL`);
  process.exit(f > 0 ? 1 : 0);
})();
