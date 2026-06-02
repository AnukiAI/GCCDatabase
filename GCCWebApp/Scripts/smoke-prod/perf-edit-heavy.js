// READ-ONLY perf probe — the heaviest cert Edit page (CR-018 stress case) on gccwebapp.
// 1) Pull /api/certificate-tiles-internal once, rank certs by product-ID count.
// 2) Time the Edit page for the heaviest cert.
// 3) Time the CR-018 paged /api/productids endpoint (page 1, page 2, and a server-side sort).
// GET-only; nothing mutated.
//
// Run:  node Scripts/smoke-prod/perf-edit-heavy.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const APP_USER = 'admin', APP_PASS = 'TestPass123!';

const out = { topCerts: [], edit: null, productIdApi: [] };
const fmt = (n) => (n == null ? '—' : Math.round(n) + 'ms');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  const timedGet = async (url) => {
    const t0 = Date.now();
    const r = await ctx.request.get(url, { timeout: 60000 });
    const ms = Date.now() - t0;
    return { status: r.status(), ms, r };
  };

  try {
    console.log(`\nHeavy-Edit perf probe — ${BASE}\n`);

    // login
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(2000);
    if (/\/Account\/Login/i.test(page.url())) throw new Error('login failed');

    // 1) fetch internal tile data and rank by product-ID count
    console.log('Fetching /api/certificate-tiles-internal to rank certs by product-ID count...');
    const tiles = await timedGet(`${BASE}/api/certificate-tiles-internal?pageSize=5000`);
    console.log(`  tile API: status=${tiles.status} ${tiles.ms}ms`);
    const body = await tiles.r.json().catch(() => null);
    // unwrap to an array of cert objects regardless of envelope
    let arr = Array.isArray(body) ? body
      : body?.data ?? body?.Data ?? body?.certificates ?? body?.items ?? body?.tiles ?? null;
    if (!Array.isArray(arr)) {
      // some envelopes nest: find the first array-valued property
      const k = body && Object.keys(body).find(k => Array.isArray(body[k]));
      arr = k ? body[k] : [];
    }
    console.log(`  certs returned: ${arr.length}`);

    const lenOf = (c, ...keys) => keys.reduce((s, k) => s + ((Array.isArray(c[k]) ? c[k].length : 0)), 0);
    const ranked = arr.map(c => {
      const id = c.id ?? c.Id ?? c.certificateId;
      const number = c.rawNumber ?? c.RawNumber ?? c.number ?? c.Number;
      const allLen = Array.isArray(c.allProductIds ?? c.AllProductIds) ? (c.allProductIds ?? c.AllProductIds).length : null;
      const typed = lenOf(c, 'asinList', 'AsinList', 'upcList', 'UpcList', 'gtinList', 'GtinList', 'eanList', 'EanList');
      return { id, number, count: allLen != null ? allLen : typed };
    }).filter(c => c.id != null).sort((a, b) => b.count - a.count);

    out.topCerts = ranked.slice(0, 5);
    console.log('  Top certs by product-ID count:');
    out.topCerts.forEach((c, i) => console.log(`    ${i + 1}. cert ${c.number} (id ${c.id}) — ${c.count} product IDs`));

    const heavy = ranked[0];
    if (!heavy || heavy.count === 0) {
      console.log('\n  No cert has product IDs on this env — CR-018 stress case not reproducible here.');
    } else {
      // 2) time the Edit page for the heaviest cert
      console.log(`\nTiming Edit page for cert ${heavy.number} (${heavy.count} product IDs):`);
      const editUrl = `${BASE}/Certificate/Edit/${heavy.number}`;
      const samples = [];
      for (let i = 0; i < 3; i++) {
        const t0 = Date.now();
        const resp = await page.goto(editUrl, { waitUntil: 'domcontentloaded', timeout: 60000 });
        // wait until the product-id table/manager has rendered rows (perceived "ready")
        let ready = null;
        try { await page.waitForSelector('#productIdsTableBody tr, .product-id-row, #ProductId', { timeout: 30000 }); ready = Date.now() - t0; } catch {}
        const nav = await page.evaluate(() => {
          const t = performance.getEntriesByType('navigation')[0];
          return t ? { ttfb: Math.round(t.responseStart), load: Math.round(t.loadEventEnd || t.duration) } : {};
        });
        samples.push({ status: resp?.status(), wall: Date.now() - t0, ready, ...nav });
      }
      const med = (k) => { const v = samples.map(s => s[k]).filter(x => x != null).sort((a, b) => a - b); return v.length ? v[Math.floor(v.length / 2)] : null; };
      out.edit = { number: heavy.number, id: heavy.id, count: heavy.count, ttfb: med('ttfb'), load: med('load'), ready: med('ready'), wall: med('wall') };
      console.log(`  ttfb=${fmt(out.edit.ttfb)} load=${fmt(out.edit.load)} content-ready=${fmt(out.edit.ready)} wall=${fmt(out.edit.wall)}`);

      // 3) time the CR-018 paged product-id API
      console.log(`\nTiming paged /api/productids for cert id ${heavy.id} (CR-018):`);
      const calls = [
        { label: 'page 1, size 50', url: `${BASE}/api/productids/certificate/${heavy.id}?page=1&pageSize=50` },
        { label: 'page 2, size 50', url: `${BASE}/api/productids/certificate/${heavy.id}?page=2&pageSize=50` },
        { label: 'page 1, sort=type', url: `${BASE}/api/productids/certificate/${heavy.id}?page=1&pageSize=50&sort=type&dir=asc` },
        { label: 'search filter', url: `${BASE}/api/productids/certificate/${heavy.id}?page=1&pageSize=50&search=0` },
      ];
      for (const c of calls) {
        const g = await timedGet(c.url);
        let tc = null, items = null;
        try { const j = await g.r.json(); const d = j.data ?? j.Data ?? j; tc = d.totalCount ?? d.TotalCount; items = (d.items ?? d.Items ?? []).length; } catch {}
        out.productIdApi.push({ label: c.label, status: g.status, ms: g.ms, totalCount: tc, items });
        console.log(`  ${c.label.padEnd(20)} status=${g.status} ${fmt(g.ms)}  totalCount=${tc ?? '—'} items=${items ?? '—'}`);
      }
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    out.error = e.message;
  } finally {
    fs.writeFileSync(path.join(__dirname, 'perf-edit-heavy-results.json'), JSON.stringify(out, null, 2));
    await browser.close();
  }
  console.log(`\nResults written to perf-edit-heavy-results.json`);
})();
