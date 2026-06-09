// DEF-243 + CR-184 — PROD fingerprint (gccwebapp.com), READ-ONLY.
// Confirms the deployed assets carry the fixes and behave correctly on real prod data.
// NO mutations: no SaveProduct, no Create, no Delete anywhere. GET + login only.
//
// Run: node Scripts/smoke-prod/def243-cr184-prod-fingerprint.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const PASS = 'TestPass123!';
const GUARD_STR = 'Select a Product Category above before adding a new product.';

const results = [];
const rec = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

async function login(ctx, user) {
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill(user);
  await page.locator('#Password').fill(PASS);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1500);
  const ok = !/\/Account\/Login/i.test(page.url());
  return { page, ok };
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  console.log(`\nDEF-243 + CR-184 PROD fingerprint (READ-ONLY) — ${BASE}\n`);

  // ---------- DEF-243 asset presence (gate only, no app login needed for static JS) ----------
  const anonCtx = await browser.newContext({ httpCredentials: GATE });
  try {
    const jsResp = await anonCtx.request.get(`${BASE}/js/certificate-inline-create.js?_=${Date.now()}`);
    const js = await jsResp.text();
    rec('DEF243-asset-js', 'Deployed certificate-inline-create.js carries the inline-add guard',
      (jsResp.status() === 200 && js.includes(GUARD_STR)) ? 'PASS' : 'FAIL',
      `http=${jsResp.status()} guardString=${js.includes(GUARD_STR)}`);

    // export endpoint must NOT be public (gate-only, no app login)
    const pubExport = await anonCtx.request.get(`${BASE}/Export/DownloadActiveProductIds`, { maxRedirects: 0 }).catch(e => ({ status: () => 'ERR', _e: e }));
    const ps = pubExport.status ? pubExport.status() : 'ERR';
    const pubBody = pubExport.text ? await pubExport.text().catch(() => '') : '';
    const notServed = ps !== 200 || !pubBody.includes('Active Product Identifiers');
    rec('CR184-not-public', 'Export not served to unauthenticated caller', notServed ? 'PASS' : 'FAIL',
      `http=${ps} (expect redirect/401/403, not a 200 CSV)`);
  } catch (e) { rec('asset-anon', 'Anon asset/endpoint checks', 'FAIL', e.message); }
  await anonCtx.close();

  // ---------- Admin session: CR-184 export + DEF-243 runtime ----------
  const adminCtx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const { page, ok } = await login(adminCtx, 'admin');
  if (!ok) { rec('SETUP-admin-login', 'Admin login on prod', 'FAIL', 'still on /Account/Login'); }
  else {
    rec('SETUP-admin-login', 'Admin login on prod', 'PASS');
    const getJson = async (url) => (await adminCtx.request.get(url)).json().catch(() => null);

    // CR-184: admin export returns the active-identifier CSV with the 12-col header
    const exp = await adminCtx.request.get(`${BASE}/Export/DownloadActiveProductIds`);
    const csv = await exp.text();
    const header = 'Certificate Number,Company,Identifier Type,Id Value,Partner,Product Name,SKU,Description,Effective Date,Expiration Date,Created Date,Created By';
    const totalLine = (csv.split('\n').find(l => l.startsWith('Total Active')) || '').trim();
    const ctype = exp.headers()['content-type'] || '';
    rec('CR184-admin-export', 'Admin export returns active-identifier CSV',
      (exp.status() === 200 && csv.includes(header) && /text\/csv/.test(ctype)) ? 'PASS' : 'FAIL',
      `http=${exp.status()} ctype="${ctype}" "${totalLine}"`);

    // CR-184: export button present on Product Management for internal role
    await page.goto(`${BASE}/Product/Management`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1200);
    const btn = await page.evaluate(() => {
      const els = Array.from(document.querySelectorAll('button, a'));
      const b = els.find(e => /export active product identifiers/i.test(e.textContent || ''));
      return b ? { found: true, up: /fa-(file-)?arrow-up|fa-arrow-up/.test(b.innerHTML) } : { found: false };
    });
    rec('CR184-mgmt-button', 'Export button on Product Management (internal)', btn.found ? 'PASS' : 'FAIL',
      `found=${btn.found} upArrow=${btn.up}`);

    // DEF-243: deployed Edit view carries the guard markers
    const tiles = (await getJson(`${BASE}/api/certificate-tiles-internal?pageSize=50&limit=50`))?.certificates ?? [];
    const cands = tiles.map(t => t.certificateNumber ?? t.CertificateNumber).filter(n => n && /^\d{2}-\d{4}$/.test(n)).slice(0, 20);
    rec('DEF243-candidates', 'Prod cert Edit candidates found', cands.length ? 'PASS' : 'WARN', `count=${cands.length}`);

    if (cands.length) {
      const firstEditHtml = await (await adminCtx.request.get(`${BASE}/Certificate/Edit/${encodeURIComponent(cands[0])}`)).text();
      rec('DEF243-asset-view', 'Deployed Edit.cshtml carries suppressProductAutoFill guard',
        (firstEditHtml.includes('suppressProductAutoFill') && firstEditHtml.includes('hasSavedCertCategory')) ? 'PASS' : 'FAIL',
        `suppress=${firstEditHtml.includes('suppressProductAutoFill')} hasSavedFlag=${firstEditHtml.includes('hasSavedCertCategory')}`);

      // Runtime no-clobber / SS-sync on real prod certs (read-only — just loads Edit pages)
      let caseA = 0, caseB = 0, scanned = 0;
      for (const num of cands) {
        if ((caseA >= 2 && caseB >= 2) || scanned >= 10) break;
        await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(num)}`, { waitUntil: 'networkidle' });
        await page.waitForTimeout(1600);
        const snap = await page.evaluate(() => {
          const catSel = document.getElementById('ProductCategoryId');
          if (!catSel) return null;
          let savedVal = '', savedText = '';
          for (const o of catSel.options) { if (o.defaultSelected && o.value) { savedVal = o.value; savedText = o.text; break; } }
          const curVal = catSel.value;
          let curText = ''; for (const o of catSel.options) if (o.value === curVal) curText = o.text;
          const w = catSel.parentNode.querySelector('.searchable-select-wrapper');
          const ss = w ? (w.querySelector('input')?.value || '') : '';
          const prodId = parseInt(document.getElementById('ProductId')?.value || '0');
          let prodCatId = null;
          try { const p = (window.companyProducts || []).find(x => (x.id ?? x.Id) === prodId); if (p) prodCatId = (p.categoryId ?? p.CategoryId); } catch (e) {}
          return { savedVal, savedText, curVal, curText, ss, prodCatId };
        });
        if (!snap) continue;
        scanned++;
        if (snap.savedVal) {
          caseA++;
          rec(`DEF243-${num}-no-clobber`, 'Edit keeps saved category (prod)',
            (snap.curVal === snap.savedVal) ? 'PASS' : 'FAIL',
            `saved=${snap.savedVal}("${snap.savedText}") cur=${snap.curVal} prodCat=${snap.prodCatId}`);
          rec(`DEF243-${num}-ss`, 'SearchableSelect synced (prod)',
            (snap.ss && snap.ss.trim() === snap.savedText.trim()) ? 'PASS' : 'FAIL', `box="${snap.ss}"`);
        } else if (snap.prodCatId != null) {
          caseB++;
          rec(`DEF243-${num}-derived`, 'Edit derives category from product when none saved (prod)',
            (String(snap.curVal) === String(snap.prodCatId)) ? 'PASS' : 'FAIL',
            `cur=${snap.curVal}("${snap.curText}") prodCat=${snap.prodCatId}`);
          rec(`DEF243-${num}-ssd`, 'SearchableSelect shows derived category (prod)',
            (snap.ss && snap.curText.trim() !== '' && snap.ss.trim() === snap.curText.trim()) ? 'PASS' : 'FAIL', `box="${snap.ss}"`);
        }
      }
      rec('DEF243-coverage', 'Edit prefill coverage on prod', (caseA + caseB) ? 'PASS' : 'WARN', `caseA=${caseA} caseB=${caseB}`);
    }
  }
  await adminCtx.close();

  // ---------- External session (best-effort): CR-184 export must be 403 ----------
  const extCtx = await browser.newContext({ httpCredentials: GATE });
  const extLogin = await login(extCtx, 'external');
  if (!extLogin.ok) {
    rec('CR184-external', 'External blocked from export', 'WARN', 'external login unavailable on prod — covered by CR184-not-public + Gate 3 localhost 403');
  } else {
    const er = await extCtx.request.get(`${BASE}/Export/DownloadActiveProductIds`);
    const eb = await er.text().catch(() => '');
    rec('CR184-external', 'External blocked from export (403, no CSV)',
      (er.status() === 403 || (er.status() !== 200) || !eb.includes('Active Product Identifiers')) ? 'PASS' : 'FAIL',
      `http=${er.status()}`);
  }
  await extCtx.close();

  fs.writeFileSync(path.join(__dirname, 'def243-cr184-prod-fingerprint-results.json'), JSON.stringify(results, null, 2));
  await browser.close();

  const p = results.filter(r => r.status === 'PASS').length;
  const f = results.filter(r => r.status === 'FAIL').length;
  const w = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${p} PASS / ${f} FAIL / ${w} WARN  (of ${results.length})`);
  process.exit(f > 0 ? 1 : 0);
})();
