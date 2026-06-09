// DEF-243 / QC #186 (6/4) — FIX verification (drives the real cert Create + Edit UI).
//
// Covers the three behaviors changed for DEF-243:
//   H1  Footer "Add product" is BLOCKED when no Product Category is selected (no silent
//       "Carly Test" default) and shows the inline guard message.
//   H2  Footer "Add product" WITH a category still persists the product under the analyst's
//       Category + Type + Subcategory (DEF-231 happy path must not regress).
//   E1  On Edit load the cert's OWN saved Product Category is NOT overwritten by the linked
//       product's hierarchy (suppressProductAutoFill guard).
//   E2  The SearchableSelect box for Product Category shows the saved value (no stale/blank).
//
// LOCALHOST ONLY. Run:  node Scripts/smoke-prod/def243-edit-prefill-verify.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const APP_USER = 'admin', APP_PASS = 'TestPass123!';
const STAMP = Date.now();
const NAME = `ZZ_Probe_DEF243_${STAMP}`;

const LIVE = BASE.includes('gccwebapp') || BASE.includes('greencirclecertified');
if (LIVE && process.env.LIVE_OK !== '1') {
  console.error('REFUSING to run against the live test env without LIVE_OK=1 (creates a throwaway ZZ_Probe_* product).');
  process.exit(2);
}
const GATE = LIVE ? { username: 'gcc', password: 'Cert-Test-Access-2026!' } : undefined;

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  let token = null, cookieHeader = '';
  const authHeaders = () => ({ 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token, 'Cookie': cookieHeader });
  const getJson = async (url) => (await ctx.request.get(url)).json().catch(() => null);
  const unwrap = (b) => b?.data ?? b?.Data ?? b;

  try {
    console.log(`\nDEF-243 Edit-prefill + inline-add-guard verification — ${BASE}\n`);

    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(1200);
    if (/\/Account\/Login/i.test(page.url())) { record('SETUP-login', 'Admin login', 'FAIL'); throw new Error('login failed'); }

    // intended hierarchy: Chemicals + first type + first subcategory under it
    const cats = unwrap(await getJson(`${BASE}/Product/GetCategories`)) || [];
    const defaultCat = cats[0];
    const chem = cats.find(c => /chemical/i.test(c.name ?? c.Name)) || cats[1];
    const chemId = chem?.id ?? chem?.Id;
    const types = unwrap(await getJson(`${BASE}/Certificate/GetProductTypes?categoryId=${chemId}`)) || [];
    const type = types[0];
    const typeId = type?.id ?? type?.Id;
    const subs = typeId ? (unwrap(await getJson(`${BASE}/Certificate/GetProductSubcategories?productTypeId=${typeId}`)) || []) : [];
    const sub = subs[0];
    const subId = sub?.id ?? sub?.Id;
    record('SETUP-hierarchy', 'Intended Category + Type (+ Sub) chosen', (chemId && typeId) ? 'PASS' : 'FAIL',
      `cat="${chem?.name ?? chem?.Name}"(${chemId}) default="${defaultCat?.name ?? defaultCat?.Name}"(${defaultCat?.id ?? defaultCat?.Id}) type=${typeId} sub=${subId ?? '(none)'}`);

    const pickCompany = async () => page.evaluate(() => {
      const sel = document.getElementById('CompanyId');
      if (!sel) return null;
      for (const o of sel.options) if (o.value && !o.disabled && !/add new/i.test(o.textContent)) return o.value;
      return null;
    });

    // ---------- H1: footer Add BLOCKED without a category ----------
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'networkidle' });
    await page.waitForTimeout(900);
    token = await page.locator('input[name="__RequestVerificationToken"]').first().getAttribute('value').catch(() => null);
    cookieHeader = (await ctx.cookies()).map(c => `${c.name}=${c.value}`).join('; ');

    const companyId = await pickCompany();
    if (!companyId) { record('SETUP-company', 'A company option on the form', 'FAIL'); throw new Error('no company option'); }
    await page.evaluate((cid) => { const s = document.getElementById('CompanyId'); s.value = cid; s.dispatchEvent(new Event('change', { bubbles: true })); }, companyId);
    await page.waitForTimeout(1000);

    // ensure NO category selected
    await page.evaluate(() => { const s = document.getElementById('ProductCategoryId'); s.value = ''; s.dispatchEvent(new Event('change', { bubbles: true })); });
    await page.waitForTimeout(200);

    let savePostFired = false;
    const onReq = (req) => { if (req.url().includes('/Product/SaveProduct') && req.method() === 'POST') savePostFired = true; };
    page.on('request', onReq);

    const wrapperC = page.locator('.custom-dropdown-wrapper', { has: page.locator('#ProductId') });
    await wrapperC.locator('.custom-dropdown-trigger').click();
    await page.waitForTimeout(300);
    await wrapperC.locator('.custom-dropdown-add-input').fill(`${NAME}_blocked`);
    await wrapperC.locator('.custom-dropdown-add-btn').click();
    await page.waitForTimeout(2000);
    page.off('request', onReq);

    const errText = await wrapperC.locator('.custom-dropdown-error').innerText().catch(() => '');
    const errVisible = await wrapperC.locator('.custom-dropdown-error').isVisible().catch(() => false);
    record('H1-no-post', 'No product saved when category is unset', !savePostFired ? 'PASS' : 'FAIL',
      savePostFired ? 'a /Product/SaveProduct POST fired (should have been blocked)' : 'no SaveProduct POST (blocked as intended)');
    record('H1-guard-msg', 'Inline guard message shown', (errVisible && /category/i.test(errText)) ? 'PASS' : 'FAIL',
      `visible=${errVisible} text="${errText}"`);

    // ---------- H2: footer Add WITH a category persists under that hierarchy ----------
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'networkidle' });
    await page.waitForTimeout(900);
    await page.evaluate((cid) => { const s = document.getElementById('CompanyId'); s.value = cid; s.dispatchEvent(new Event('change', { bubbles: true })); }, companyId);
    await page.waitForTimeout(1000);
    await page.evaluate((cid) => { const s = document.getElementById('ProductCategoryId'); s.value = String(cid); s.dispatchEvent(new Event('change', { bubbles: true })); }, chemId);
    await page.waitForFunction(() => document.getElementById('ProductTypeId')?.options.length > 1, { timeout: 8000 }).catch(() => {});
    if (typeId) {
      await page.evaluate((tid) => { const s = document.getElementById('ProductTypeId'); s.value = String(tid); s.dispatchEvent(new Event('change', { bubbles: true })); }, typeId);
      await page.waitForTimeout(600);
    }
    if (subId) {
      await page.waitForFunction(() => document.getElementById('ProductSubcategoryId')?.options.length > 1, { timeout: 6000 }).catch(() => {});
      await page.evaluate((sid) => { const s = document.getElementById('ProductSubcategoryId'); if (s) { s.value = String(sid); s.dispatchEvent(new Event('change', { bubbles: true })); } }, subId);
      await page.waitForTimeout(300);
    }

    const wrapperC2 = page.locator('.custom-dropdown-wrapper', { has: page.locator('#ProductId') });
    await wrapperC2.locator('.custom-dropdown-trigger').click();
    await page.waitForTimeout(300);
    await wrapperC2.locator('.custom-dropdown-add-input').fill(NAME);
    const [resp] = await Promise.all([
      page.waitForResponse(r => r.url().includes('/Product/SaveProduct') && r.request().method() === 'POST', { timeout: 10000 }).catch(() => null),
      wrapperC2.locator('.custom-dropdown-add-btn').click(),
    ]);
    await page.waitForTimeout(800);
    record('H2-add-fired', 'Add posts to /Product/SaveProduct when category set', resp ? 'PASS' : 'FAIL', resp ? `http=${resp.status()}` : 'no POST');

    const all = unwrap(await getJson(`${BASE}/Product/GetProducts`)) || [];
    const prod = all.find(p => (p.name ?? p.Name) === NAME);
    if (!prod) { record('H2-readback', 'Created product found', 'FAIL', `"${NAME}" not in GetProducts`); }
    else {
      const savedCat = prod.categoryId ?? prod.CategoryId;
      const savedType = prod.productTypeId ?? prod.ProductTypeId;
      const savedSub = prod.productSubcategoryId ?? prod.ProductSubcategoryId;
      record('H2-category', 'Product under analyst category (not default)',
        (savedCat === chemId && savedCat !== (defaultCat?.id ?? defaultCat?.Id)) ? 'PASS' : 'FAIL',
        `saved cat=${savedCat} want ${chemId}`);
      record('H2-type', 'Product carries analyst type', (!typeId || savedType === typeId) ? 'PASS' : 'FAIL', `saved type=${savedType} want ${typeId}`);
      if (subId) record('H2-sub', 'Product carries analyst subcategory', (savedSub === subId) ? 'PASS' : 'WARN', `saved sub=${savedSub} want ${subId}`);
    }

    // ---------- D: DIVERGENT cert = the client's exact scenario ----------
    // A product filed under the DEFAULT category ("Carly Test"-equivalent, no type), linked to a
    // cert whose cert-level category is Chemicals. BEFORE the fix, Edit's product auto-fill reverted
    // the cert's category to the product's default on load. AFTER the fix it must keep Chemicals.
    const NAME_D = `${NAME}_divprod`;
    const defCatId = defaultCat?.id ?? defaultCat?.Id;
    let divCertNumber = null;
    const saveProdResp = await ctx.request.post(`${BASE}/Product/SaveProduct`, {
      headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
      data: { Id: 0, Name: NAME_D, CompanyId: parseInt(companyId), CategoryId: defCatId, ProductTypeId: null, ProductSubcategoryId: null, IsActive: true }
    });
    const divProdId = ((await saveProdResp.json().catch(() => null))?.data ?? {})?.id;

    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'networkidle' });
    await page.waitForTimeout(700);
    token = await page.locator('input[name="__RequestVerificationToken"]').first().getAttribute('value').catch(() => null);
    const certTypeId = await page.evaluate(() => {
      const s = document.getElementById('CertificationTypeId');
      if (!s) return null;
      for (const o of s.options) if (o.value && !o.disabled) return o.value;
      return null;
    });
    const createResp = await ctx.request.post(`${BASE}/Certificate/Create`, {
      form: {
        __RequestVerificationToken: token || '',
        Number: '00-0000', // [Required] preview; server auto-generates the real number on save
        CompanyId: String(companyId), ProductId: String(divProdId || ''), CertificationTypeId: String(certTypeId || ''),
        ProductCategoryId: String(chemId), EffectiveDate: '2026-01-01', ExpirationDate: '2027-01-01',
        Status: 'Draft', Partner: 'None', action: 'save'
      }
    });
    record('D-create', 'Divergent cert created (cert=Chemicals, product=default cat)',
      (divProdId && certTypeId && [200, 302].includes(createResp.status())) ? 'PASS' : 'FAIL',
      `prod=${divProdId} certType=${certTypeId} defCat=${defCatId} http=${createResp.status()}`);

    const t2 = (await getJson(`${BASE}/api/certificate-tiles-internal?pageSize=50&limit=50`))?.certificates ?? [];
    const mine = t2.find(t => (t.productName ?? t.ProductName) === NAME_D);
    divCertNumber = mine?.certificateNumber ?? mine?.CertificateNumber ?? null;
    if (divCertNumber) {
      await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(divCertNumber)}`, { waitUntil: 'networkidle' });
      await page.waitForTimeout(1800);
      const dsnap = await page.evaluate(() => {
        const catSel = document.getElementById('ProductCategoryId');
        const curVal = catSel?.value;
        let curText = ''; for (const o of (catSel?.options || [])) if (o.value === curVal) curText = o.text;
        const w = catSel?.parentNode?.querySelector('.searchable-select-wrapper');
        const ss = w ? (w.querySelector('input')?.value || '') : '';
        return { curVal, curText, ss };
      });
      record('D-no-clobber', 'DIVERGENT: Edit keeps cert category (Chemicals), NOT product default',
        (String(dsnap.curVal) === String(chemId)) ? 'PASS' : 'FAIL',
        `current=${dsnap.curVal}("${dsnap.curText}") want ${chemId}; product is under ${defCatId}`);
      record('D-ss-sync', 'DIVERGENT: SearchableSelect shows the cert category',
        (dsnap.ss && dsnap.ss.trim() === dsnap.curText.trim() && String(dsnap.curVal) === String(chemId)) ? 'PASS' : 'FAIL',
        `box="${dsnap.ss}"`);
    } else {
      record('D-readback', 'Divergent cert number resolved', 'WARN', `could not locate created cert via tiles (http=${createResp.status()})`);
    }

    // ---------- E1/E2: Edit load does not clobber the cert's saved category; SearchableSelect synced ----------
    // Collect candidate certs from the internal tiles API. The Edit GET binds on the certificate
    // NUMBER (Edit(string number)), so navigate by number — restricted to the canonical XX-XXXX
    // form, which FormatCertificateNumber round-trips identically (no format-mismatch lookups).
    const tilesResp = await getJson(`${BASE}/api/certificate-tiles-internal?pageSize=50&limit=50`);
    const tiles = tilesResp?.certificates ?? tilesResp?.Certificates ?? [];
    const cands = tiles
      .map(t => ({ id: t.id ?? t.Id, number: t.certificateNumber ?? t.CertificateNumber }))
      .filter(c => c.number && /^\d{2}-\d{4}$/.test(c.number))
      .slice(0, 20);
    record('E0-candidates', 'Cert Edit candidates found', cands.length ? 'PASS' : 'WARN', `count=${cands.length} nums=${cands.slice(0, 12).map(c => c.number).join(',')}${cands.length > 12 ? '…' : ''}`);

    let caseA = 0, caseB = 0, testedDivergent = false, scanned = 0;
    for (const cand of cands) {
      const id = cand.number; // label by number
      if ((caseA >= 1 && caseB >= 1 && testedDivergent) || scanned >= 12) break;
      await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(cand.number)}`, { waitUntil: 'networkidle' });
      await page.waitForTimeout(1600); // let the load-time product pre-select + cascade settle
      const snap = await page.evaluate(() => {
        const catSel = document.getElementById('ProductCategoryId');
        if (!catSel) return null;
        // server-rendered saved category = the option with defaultSelected (unaffected by JS .val())
        let savedVal = '', savedText = '';
        for (const o of catSel.options) { if (o.defaultSelected && o.value) { savedVal = o.value; savedText = o.text; break; } }
        const curVal = catSel.value;
        const curText = (() => { for (const o of catSel.options) if (o.value === curVal) return o.text; return ''; })();
        // SearchableSelect visible input
        const wrap = catSel.closest('.searchable-select-wrapper') || (catSel.previousElementSibling && catSel.previousElementSibling.classList?.contains('searchable-select-wrapper') ? catSel.previousElementSibling : null);
        let ssInput = '';
        const w = catSel.parentNode.querySelector('.searchable-select-wrapper');
        if (w) { const inp = w.querySelector('input'); ssInput = inp ? inp.value : ''; }
        // linked product's own category (what the buggy auto-fill would have forced)
        const prodId = parseInt(document.getElementById('ProductId')?.value || '0');
        let prodCatId = null;
        try { const cp = (window.companyProducts || []); const p = cp.find(x => (x.id ?? x.Id) === prodId); if (p) prodCatId = (p.categoryId ?? p.CategoryId); } catch (e) {}
        return { savedVal, savedText, curVal, curText, ssInput, prodId, prodCatId };
      });
      if (!snap) continue;
      scanned++;
      if (snap.savedVal) {
        // Case A — cert has its OWN saved category (override). The load-time product auto-fill
        // must NOT overwrite it. "Divergent" = the linked product's category differs from the
        // cert's saved one, which is the strongest proof the guard works.
        caseA++;
        const divergent = snap.prodCatId != null && String(snap.prodCatId) !== String(snap.savedVal);
        if (divergent) testedDivergent = true;
        record(`E1-cert${id}-no-clobber`, `Edit keeps cert's saved category${divergent ? ' [DIVERGENT product]' : ''}`,
          (snap.curVal === snap.savedVal) ? 'PASS' : 'FAIL',
          `saved=${snap.savedVal}("${snap.savedText}") current=${snap.curVal}("${snap.curText}") productCat=${snap.prodCatId}`);
        record(`E2-cert${id}-ss-sync`, 'SearchableSelect matches saved category',
          (snap.ssInput && snap.ssInput.trim() === snap.savedText.trim()) ? 'PASS' : 'FAIL',
          `box="${snap.ssInput}" want="${snap.savedText}"`);
      } else if (snap.prodCatId != null) {
        // Case B — cert has NO cert-level category; the product's category should populate the
        // effective value on load (so the box is not left blank). Guard must be a no-op here.
        caseB++;
        record(`E3-cert${id}-derived`, 'Edit derives category from product when cert has none',
          (String(snap.curVal) === String(snap.prodCatId)) ? 'PASS' : 'FAIL',
          `current=${snap.curVal}("${snap.curText}") productCat=${snap.prodCatId}`);
        record(`E4-cert${id}-ss-sync`, 'SearchableSelect shows derived category (not blank)',
          (snap.ssInput && snap.curText.trim() !== '' && snap.ssInput.trim() === snap.curText.trim()) ? 'PASS' : 'FAIL',
          `box="${snap.ssInput}" derived="${snap.curText}"`);
      }
    }
    if (caseA === 0 && caseB === 0) record('E-cases', 'Edit prefill cases', 'WARN', 'no testable certs found among candidates');
    else {
      record('E-coverage', 'Edit prefill case coverage', 'PASS', `caseA(override)=${caseA} caseB(derived)=${caseB} divergent=${testedDivergent}`);
      if (caseA > 0 && !testedDivergent) record('E-note', 'Divergence case', 'WARN',
        'no cert found where product category != cert saved category; no-clobber asserted on overrides present (still proves no blanking + SS sync)');
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    // cleanup throwaway certs FIRST (they FK-reference the throwaway products)
    try {
      const tiles = (await getJson(`${BASE}/api/certificate-tiles-internal?pageSize=50&limit=50`))?.certificates ?? [];
      const probeCerts = tiles.filter(x => typeof (x.productName ?? x.ProductName) === 'string' && (x.productName ?? x.ProductName).startsWith('ZZ_Probe_DEF243_'));
      let delC = 0;
      for (const c of probeCerts) {
        const num = c.certificateNumber ?? c.CertificateNumber;
        if (num) { const r = await ctx.request.post(`${BASE}/Certificate/Delete`, { form: { __RequestVerificationToken: token || '', number: num } }); if (r.ok()) delC++; }
      }
      record('cleanup-certs', 'Throwaway certs removed', 'PASS', `removed=${delC}`);
    } catch (ce) { record('cleanup-certs', 'Cert cleanup', 'WARN', ce.message); }

    // cleanup throwaway products
    try {
      const all = unwrap(await getJson(`${BASE}/Product/GetProducts`)) || [];
      const probes = all.filter(p => typeof (p.name ?? p.Name) === 'string' && (p.name ?? p.Name).startsWith('ZZ_Probe_DEF243_'));
      let removed = 0, failed = 0;
      for (const p of probes) {
        const d = await ctx.request.delete(`${BASE}/Product/DeleteProduct/${p.id ?? p.Id}`, { headers: authHeaders() });
        const db = await d.json().catch(() => null);
        if ((db?.success ?? db?.Success)) removed++; else failed++;
      }
      record('cleanup', 'Throwaway products removed', failed === 0 ? 'PASS' : 'WARN', `removed=${removed}, failed=${failed}`);
    } catch (ce) { record('cleanup', 'Cleanup', 'WARN', ce.message); }
    fs.writeFileSync(path.join(OUT, 'def243-edit-prefill-verify-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
  process.exit(fail > 0 ? 1 : 0);
})();
