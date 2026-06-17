// CR-187 — LOCAL E2E smoke (:5101): user-friendly normalized search.
// Proves, through the running app over HTTP, that search matches regardless of trademark/
// registered symbols, case, hyphens, extra spaces, and partial terms — the literal client
// scenario "search `Product A`, find `Product™ A`".
//
// Two layers are exercised:
//   A) Seeded product with a ™ in its name -> the product autocomplete (/Certificate/SearchProducts,
//      in-memory SearchNormalizer path) finds it for de-symboled / re-cased / hyphenated / partial
//      / exact queries. The seeded product is deleted again at the end.
//   B) Existing data -> the internal tile search (/api/certificate-tiles-internal, the DB persisted
//      computed-column path) returns a known cert when its company name is searched with injected
//      symbols + altered case (proves the stored side normalizes too, no seeding needed).
//
// Run (app running on :5101):  node Scripts/smoke-prod/cr187-normalized-search-local.js
const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const PASS = process.env.GCC_PASS || 'TestPass123!';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-');
const NAME = `ZZ CR187 Smoke Product™ A ${Date.now()}`;        // stored name (with trademark symbol)
const NAME_PLAIN = NAME.replace('™', '');                       // what a user would actually type

const results = { cr: 'CR-187', base: BASE, when: STAMP, seededProductId: null, checks: [] };
const check = (name, pass, detail) => {
  results.checks.push({ name, pass: !!pass, detail: detail || '' });
  console.log(`  ${pass ? '✅' : '❌'} ${name}${detail ? ' — ' + detail : ''}`);
};

const j = async (ctx, url) => { try { return await (await ctx.request.get(BASE + url)).json(); } catch { return null; } };
const names = arr => (arr || []).map(p => p.name || p.Name).filter(Boolean);
const tileCerts = o => (o && (o.certificates || o.data || o.items)) || [];
const certNo = c => c.certificateNumber || c.CertificateNumber || c.number || c.Number;
const compOf = c => c.companyName || c.CompanyName || c.company || c.Company;

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();

  // ---- login ----
  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill(PASS);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1000);
  if (/\/Account\/Login/i.test(page.url())) { console.log('login failed'); await browser.close(); process.exit(1); }

  // ---- antiforgery token (header name configured in Program.cs: "RequestVerificationToken") ----
  await page.goto(BASE + '/Product/Management', { waitUntil: 'networkidle' });
  const token = await page.evaluate(() => {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : null;
  });
  if (!token) { check('antiforgery token available', false, 'no __RequestVerificationToken input'); }

  // ---- pick a valid CompanyId + CategoryId for the seed ----
  const comps = (await j(ctx, '/Product/GetCompanies'))?.data || [];
  const cats = (await j(ctx, '/Product/GetCategories'))?.data || [];
  const companyId = comps[0]?.id ?? comps[0]?.Id;
  const categoryId = cats[0]?.id ?? cats[0]?.Id;

  // ================= Layer A: seed a ™ product, search the autocomplete =================
  let createdId = null;
  if (token && companyId && categoryId) {
    const res = await ctx.request.post(BASE + '/Product/SaveProduct', {
      headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/json' },
      data: { Name: NAME, Description: 'CR-187 smoke', CategoryId: categoryId, CompanyId: companyId, IsActive: true },
    });
    const body = await res.json().catch(() => ({}));
    createdId = body?.data?.id ?? body?.data?.Id ?? null;
    results.seededProductId = createdId;
    check('seed ™ product created', res.ok() && body.success, `id=${createdId} name="${NAME}"`);

    if (createdId) {
      // the headline scenario + the variants from the acceptance criteria
      const variants = [
        ['plain (no ™)', NAME_PLAIN],
        ['lowercase', NAME_PLAIN.toLowerCase()],
        ['hyphenated', NAME_PLAIN.replace(/ /g, '-')],
        ['extra spaces', NAME_PLAIN.replace(/ /g, '   ')],
        ['exact with ™', NAME],
        ['partial', NAME_PLAIN.slice(0, 18)],
      ];
      for (const [label, q] of variants) {
        const data = (await j(ctx, '/Certificate/SearchProducts?query=' + encodeURIComponent(q)))?.data;
        check(`autocomplete finds seeded product — ${label}`, names(data).includes(NAME), `query="${q}"`);
      }
      // negative control: a clearly-different term must NOT return it
      const neg = (await j(ctx, '/Certificate/SearchProducts?query=' + encodeURIComponent('zzqx-nomatch-' + Date.now())))?.data;
      check('autocomplete does NOT match unrelated term', !names(neg).includes(NAME), 'negative control');
    }
  } else {
    check('seed prerequisites (token + company + category)', false, `token=${!!token} company=${companyId} category=${categoryId}`);
  }

  // ================= Layer B: existing data, DB computed-column path via tile search =================
  const all = tileCerts(await j(ctx, '/api/certificate-tiles-internal?page=1&pageSize=50'));
  const sample = all.find(c => certNo(c) && (compOf(c) || '').replace(/[^a-z0-9]/gi, '').length >= 2);
  if (sample) {
    const company = compOf(sample);
    const num = certNo(sample);
    // inject a ™ and flip case — normalized input must still equal the stored normalized company name
    const mangled = company.toUpperCase() + '™';
    const hit = tileCerts(await j(ctx, '/api/certificate-tiles-internal?search=' + encodeURIComponent(mangled) + '&pageSize=200'));
    check('tile search (DB) finds cert via symbol+case-mangled company', hit.some(c => certNo(c) === num),
      `company="${company}" query="${mangled}" cert=${num}`);
  } else {
    check('existing cert sample available', false, 'no cert with a usable company name found');
  }

  // ---- cleanup: delete the seeded product ----
  if (createdId && token) {
    const del = await ctx.request.delete(`${BASE}/Product/DeleteProduct/${createdId}`, {
      headers: { 'RequestVerificationToken': token },
    });
    const gone = !names((await j(ctx, '/Product/GetProducts'))?.data).includes(NAME);
    check('seeded product cleaned up', del.ok() && gone, `deleted id=${createdId}`);
  }

  await browser.close();

  // ---- write results + summarize ----
  const outDir = path.join(__dirname, '_results');
  try { fs.mkdirSync(outDir, { recursive: true }); } catch {}
  const failed = results.checks.filter(c => !c.pass).length;
  results.summary = { total: results.checks.length, passed: results.checks.length - failed, failed };
  fs.writeFileSync(path.join(outDir, `cr187-normalized-search-local-${STAMP}.json`), JSON.stringify(results, null, 2));

  console.log(`\n  ${results.summary.passed}/${results.summary.total} checks passed.`);
  console.log(failed === 0 ? '✅ PASS — normalized search works across symbols/case/spacing/punctuation/partial'
                           : `❌ FAIL — ${failed} check(s) failed`);
  process.exit(failed === 0 ? 0 : 1);
})();
