// Targeted verification — DEF-253 (#2) + DEF-254. Localhost only (dev server up on :5101).
//
// DEF-253 #2 — Product Management list projection got .AsSplitQuery() (ProductController).
//   External-observable proxy: /Product/Management returns 200, the `products` list populates,
//   and the page loads in a sane time (the split-query must not have broken or slowed it).
//
// DEF-254 — editProduct() must carry over a saved Product Type / Subcategory even when that
//   value isn't under the product's Category (the ensureSavedOption helper in
//   Views/Product/Management.cshtml). Verified two ways so it holds regardless of seed data:
//     (a) REAL — any product in the local seed whose saved type isn't in its category's type
//         list (the exact cross-category repro, e.g. #347 cat4/type45 "Other"), if one exists.
//     (b) SYNTHESIZED — inject a product with a deliberately mismatched type into the page's
//         `products` array, run editProduct(), and assert the saved type is injected as an
//         option and selected (i.e. ensureSavedOption fired). Data-independent.
//
// NOT covered here: DEF-253 #7 (CertificateRepository.GetByIdAsync) is the cert Edit *POST*
// path — it mutates a cert and isn't safely probed read-only; it's exercised by the cert-edit
// save flow. This probe is scoped to the Product Management surface (#2 + DEF-254).
//
// Run:  node Scripts/smoke-prod/def253-254-verify.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const { login, BASE } = require('./_lib/smoke-template');

(async () => {
  const results = [];
  const rec = (id, name, status, notes = '') => {
    results.push({ id, name, status, notes });
    console.log(`  [${status}] ${id} ${name}${notes ? ' - ' + notes : ''}`);
  };

  if (BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) {
    console.error(`REFUSING to run against ${BASE} — this probe is localhost-only.`);
    process.exit(2);
  }

  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  try {
    await login(page, 'admin');

    const t0 = Date.now();
    const resp = await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded', timeout: 60000 });
    const status = resp ? resp.status() : 0;
    rec('def253.page', 'Product Management returns 200 (admin)',
      status === 200 ? 'PASS' : 'FAIL', `status=${status}`);

    let loaded = false;
    try {
      await page.waitForFunction(
        () => typeof products !== 'undefined' && Array.isArray(products) && products.length > 0,
        { timeout: 15000 }
      );
      loaded = true;
    } catch { /* falls through to FAIL below */ }
    const loadMs = Date.now() - t0;
    const count = await page.evaluate(
      () => (typeof products !== 'undefined' && Array.isArray(products)) ? products.length : -1
    );
    rec('def253.list', 'Product list populated (DEF-253 #2 split-query path works)',
      loaded && count > 0 ? 'PASS' : 'FAIL', `products=${count}`);
    rec('def253.timing', 'Product Management loads in reasonable time',
      loadMs < 8000 ? 'PASS' : 'WARN', `${loadMs}ms (nav + list render)`);

    // DEF-254 — cross-category carry-over, driven through the page's own editProduct().
    const r = await page.evaluate(async () => {
      const out = { hasFns: false, realCandidates: 0, real: null, synth: null, err: null };
      try {
        if (typeof editProduct !== 'function' || typeof products === 'undefined' ||
            typeof populateTypeDropdown !== 'function') {
          out.err = 'page edit functions not in scope';
          return out;
        }
        out.hasFns = true;
        const typeSel = () => document.getElementById('product-type');
        const closeModal = () => { const m = document.getElementById('product-modal'); if (m) m.style.display = 'none'; };

        // (a) REAL cross-category products: saved type not under the product's category.
        const crossCat = [];
        for (const p of products) {
          if (!p.productTypeId) continue;
          populateTypeDropdown(p.categoryId || 0);
          const inList = Array.from(typeSel().options).some(o => String(o.value) === String(p.productTypeId));
          if (!inList) crossCat.push(p);
        }
        out.realCandidates = crossCat.length;
        if (crossCat.length) {
          const p = crossCat[0];
          await editProduct(p.id);
          out.real = { id: p.id, expected: String(p.productTypeId), got: typeSel().value };
          closeModal();
        }

        // (b) SYNTHESIZED cross-category product (data-independent).
        const synthId = 999900001, synthType = 999900777;
        const base = products.find(p => p.categoryId) || {};
        products.push({
          id: synthId, name: 'SMOKE-CrossCat', description: '',
          categoryId: base.categoryId || 1, companyId: base.companyId || null,
          productTypeId: synthType, productTypeName: 'SMOKE Cross-Cat Type',
          productSubcategoryId: null, productSubcategoryName: null,
        });
        await editProduct(synthId);
        const sel = typeSel();
        out.synth = {
          expected: String(synthType),
          got: sel.value,
          injected: Array.from(sel.options).some(o => String(o.value) === String(synthType)),
        };
        closeModal();
        const idx = products.findIndex(p => p.id === synthId);
        if (idx >= 0) products.splice(idx, 1);
      } catch (e) {
        out.err = String((e && e.message) || e);
      }
      return out;
    });

    if (r.err) {
      rec('def254.fns', 'Edit-product functions reachable', 'FAIL', r.err);
    } else {
      rec('def254.synth', 'DEF-254 synthesized cross-category Type carries over on edit',
        (r.synth && r.synth.injected && r.synth.got === r.synth.expected) ? 'PASS' : 'FAIL',
        r.synth ? `injected=${r.synth.injected} got=${r.synth.got} expected=${r.synth.expected}` : 'no result');
      if (r.realCandidates > 0) {
        rec('def254.real', 'DEF-254 real cross-category product Type carries over',
          (r.real && r.real.got === r.real.expected) ? 'PASS' : 'FAIL',
          `id=${r.real.id} got=${r.real.got} expected=${r.real.expected} (${r.realCandidates} candidate(s))`);
      } else {
        rec('def254.real', 'DEF-254 real cross-category product present in seed', 'WARN',
          'none in local seed — synthesized case (above) exercises the fix');
      }
    }
  } catch (e) {
    rec('probe.fatal', 'Probe ran without throwing', 'FAIL', e.message);
  } finally {
    await ctx.close();
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\n[def253-254] ${pass} PASS / ${fail} FAIL / ${warn} WARN`);

  const outFile = path.join(__dirname, 'def253-254-verify-results.json');
  fs.writeFileSync(outFile, JSON.stringify({ base: BASE, pass, fail, warn, results }, null, 2));
  console.log(`results -> ${path.relative(process.cwd(), outFile)}`);

  process.exitCode = fail === 0 ? 0 : 1;
})();
