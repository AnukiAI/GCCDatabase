// QC #170 / DEF-196 + DEF-197 verification.
//
// PART A — Display side (BOTH envs, read-only):
//   GET /Admin/GetProductTypes → assert every node has both productCount and
//   certificateCount fields. Pre-DEF-196 the JSON only had productCount.
//
// PART B — Delete pre-check side (LOCALHOST only, no actual data mutation —
//   the pre-check fires BEFORE SaveChanges so nothing is deleted):
//   - POST /Admin/DeleteProductType/49 (PET, 1 cert ref)   → "Cannot delete: 1 certificate(s) use this subcategory…"
//   - POST /Admin/DeleteProductType/48 (LLDPE, 1 product)  → "Cannot delete: 1 product(s) are assigned to this item…"
//   - POST /Admin/DeleteProductType/4  (Plastics, 6 children) → "Cannot delete: This item has 6 subcategories…"
//   - POST /Admin/DeleteProductType/99999 (nonexistent)    → "Product type not found"
//
// PART C — Gccwebapp display verification (read-only): same shape assertion as PART A.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const TARGET = process.env.TARGET || (BASE.includes('gccwebapp') ? 'live' : 'local');
const BASIC = BASE.includes('gccwebapp')
  ? { username: 'gcc', password: 'Cert-Test-Access-2026!' }
  : undefined;
const APP_USER = 'admin';
const APP_PASS = 'TestPass123!';
const RUN_DELETE_PROBES = !BASE.includes('gccwebapp'); // only on localhost

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  console.log(`\nQC #170 verification — ${TARGET.toUpperCase()} (${BASE})\n`);

  try {
    // login as admin
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(2000);
    if (/\/Account\/Login/i.test(page.url())) {
      console.log(`  login failed`); process.exit(2);
    }

    // ----- PART A — Display side -----
    console.log('--- PART A — GET /Admin/GetProductTypes JSON shape ---');
    const resp = await ctx.request.get(`${BASE}/Admin/GetProductTypes`);
    const status = resp.status();
    const body = await resp.json().catch(() => null);
    console.log(`  HTTP ${status}`);
    fs.writeFileSync(path.join(OUT, `qc170-${TARGET}-types.json`), JSON.stringify(body, null, 2));

    // The response wraps the tree in { success, data: [...] } per ApiResponse<T>
    const tree = body?.data || body?.Data || body;

    // Walk every node (could be nested under children[] / subcategories[])
    const allNodes = [];
    function walk(arr, depth = 0) {
      if (!Array.isArray(arr)) return;
      for (const n of arr) {
        allNodes.push({ depth, ...n });
        walk(n.children || n.subcategories || n.Children || n.Subcategories);
      }
    }
    walk(tree);
    console.log(`  total nodes (depth-first): ${allNodes.length}`);

    const hasProductCount = allNodes.every(n =>
      'productCount' in n || 'ProductCount' in n);
    const hasCertCount = allNodes.every(n =>
      'certificateCount' in n || 'CertificateCount' in n);

    record(`QC170-A1-${TARGET}`, 'Every product-type node carries productCount',
      hasProductCount ? 'PASS' : 'FAIL',
      `node count=${allNodes.length}`);
    record(`QC170-A2-${TARGET}`, 'Every product-type node carries certificateCount',
      hasCertCount ? 'PASS' : 'FAIL',
      `node count=${allNodes.length}`);

    // Spot-check known dev rows if local
    if (TARGET === 'local') {
      const get = (id) => allNodes.find(n => (n.id || n.Id) === id);
      const pet = get(49), lldpe = get(48), plastics = get(4);
      const cnt = (n, k) => n?.[k] ?? n?.[k[0].toUpperCase() + k.slice(1)];
      const spot = (label, node, expectProd, expectCert) => {
        if (!node) return record(`QC170-A3-${label}`, `Spot-check ${label}`, 'WARN', 'node not found');
        const okProd = cnt(node, 'productCount') === expectProd;
        const okCert = cnt(node, 'certificateCount') === expectCert;
        record(`QC170-A3-${label}`, `Spot-check ${label} counts`,
          okProd && okCert ? 'PASS' : 'FAIL',
          `prod=${cnt(node,'productCount')}/exp ${expectProd}, cert=${cnt(node,'certificateCount')}/exp ${expectCert}`);
      };
      spot('PET-49',   pet,      0, 1);  // 0 product, 1 cert
      spot('LLDPE-48', lldpe,    1, 0);  // 1 product (via ProductSubcategoryId), 0 cert
      // Plastics rollup with DEF-197 dedup: Product #346 is tagged BOTH ProductTypeId=4
      // AND ProductSubcategoryId=48, so {Plastics direct} ∪ {LLDPE} = 1 product (not 2).
      // Same applies to cert 26-0027 → 1 cert (not 2). Confirms HashSet union dedup.
      spot('Plastics-4-dedup', plastics, 1, 1);
    }

    // ----- PART B — Delete pre-check probes (LOCALHOST ONLY) -----
    if (RUN_DELETE_PROBES) {
      console.log('\n--- PART B — Delete pre-check probes (non-destructive; pre-check blocks SaveChanges) ---');

      // Need anti-forgery token from a page that emits it
      await page.goto(BASE + '/Admin/ProductTypes', { waitUntil: 'networkidle' });
      await page.waitForTimeout(1000);
      const token = await page.locator('input[name="__RequestVerificationToken"]').first().getAttribute('value').catch(() => null);
      if (!token) {
        record('QC170-B-token', 'Anti-forgery token captured', 'FAIL', 'no token on /Admin/ProductTypes');
      } else {
        const cookies = await ctx.cookies();
        const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');

        const probes = [
          { id: 49,    label: 'PET (subcat with 1 cert)',     mustMatch: /Cannot delete: 1 certificate\(s\) use this subcategory/i, kind: 'cert-blocker' },
          { id: 48,    label: 'LLDPE (subcat with 1 product)', mustMatch: /Cannot delete: 1 product\(s\) are assigned/i,             kind: 'product-blocker' },
          { id: 4,     label: 'Plastics (type with children)', mustMatch: /Cannot delete: This item has \d+ subcategor/i,            kind: 'child-blocker' },
          { id: 99999, label: 'Nonexistent ID',                mustMatch: /not found/i,                                              kind: 'not-found' },
        ];

        for (const p of probes) {
          // Fetch via raw POST with token in body
          const formBody = new URLSearchParams();
          formBody.append('__RequestVerificationToken', token);

          const r = await ctx.request.post(`${BASE}/Admin/DeleteProductType/${p.id}`, {
            headers: {
              'X-Requested-With': 'XMLHttpRequest',
              'Content-Type': 'application/x-www-form-urlencoded',
              'RequestVerificationToken': token,
              'Cookie': cookieHeader,
            },
            data: formBody.toString(),
          });
          const httpStatus = r.status();
          const respBody = await r.json().catch(() => null);
          const message = respBody?.message || respBody?.Message || '';
          const success = respBody?.success ?? respBody?.Success;
          const ok = httpStatus === 200 && success === false && p.mustMatch.test(message);
          record(`QC170-B-${p.id}`, `${p.label} → ${p.kind}`,
            ok ? 'PASS' : 'FAIL',
            `HTTP ${httpStatus}, success=${success}, msg="${message.slice(0, 110)}"`);
        }

        // Post-check: confirm we didn't accidentally delete anything by re-fetching the tree.
        // Snapshot the count BEFORE the delete probes so we don't get fooled by mutation.
        const beforeNodeCount = allNodes.length;
        const post = await ctx.request.get(`${BASE}/Admin/GetProductTypes`);
        const postBody = await post.json().catch(() => null);
        const postTree = postBody?.data || postBody?.Data || postBody;
        const postNodes = [];
        function walkPost(arr) {
          if (!Array.isArray(arr)) return;
          for (const n of arr) {
            postNodes.push(n);
            walkPost(n.children || n.subcategories || n.Children || n.Subcategories);
          }
        }
        walkPost(postTree);
        const same = postNodes.length === beforeNodeCount;
        record('QC170-B-no-destruction', 'No rows actually deleted (pre-check fired)',
          same ? 'PASS' : 'FAIL',
          `before=${beforeNodeCount} after=${postNodes.length} nodes`);
      }
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, `qc170-${TARGET}-results.json`), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
