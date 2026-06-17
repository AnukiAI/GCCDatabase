// CR-187 — seed a few special-character demo products on LOCAL (:5101) so normalized search can be
// tested live. These are throwaway test rows on the dev DB.
//   Seed:    node Scripts/smoke-prod/cr187-seed-demo-products.js
//   Clean up: node Scripts/smoke-prod/cr187-seed-demo-products.js --delete
// Demo names all start with "CR187 Demo " so they're easy to spot and remove.
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const PASS = process.env.GCC_PASS || 'TestPass123!';
const DELETE = process.argv.includes('--delete');
const TAG = 'CR187 Demo ';

const DEMO_NAMES = [
  `${TAG}Product™ A`,
  `${TAG}Product® B`,
  `${TAG}Product© C`,
  `${TAG}Eco-Friendly: Widget Pro`,
];

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext();
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill(PASS);
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(800);
  if (/\/Account\/Login/i.test(page.url())) { console.log('login failed'); await browser.close(); process.exit(1); }

  await page.goto(BASE + '/Product/Management', { waitUntil: 'networkidle' });
  const token = await page.evaluate(() => {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : null;
  });

  const getProducts = async () => ((await (await ctx.request.get(`${BASE}/Product/GetProducts`)).json()).data || []);

  if (DELETE) {
    const all = await getProducts();
    const demos = all.filter(p => (p.name || p.Name || '').startsWith(TAG));
    for (const p of demos) {
      const id = p.id ?? p.Id;
      const res = await ctx.request.delete(`${BASE}/Product/DeleteProduct/${id}`, { headers: { 'RequestVerificationToken': token } });
      console.log(`  ${res.ok() ? '🗑️ deleted' : '⚠️ failed'} #${id} "${p.name || p.Name}"`);
    }
    console.log(`\nCleanup done — removed ${demos.length} demo product(s).`);
    await browser.close();
    process.exit(0);
  }

  const comps = (await (await ctx.request.get(`${BASE}/Product/GetCompanies`)).json()).data || [];
  const cats = (await (await ctx.request.get(`${BASE}/Product/GetCategories`)).json()).data || [];
  const companyId = comps[0]?.id ?? comps[0]?.Id;
  const categoryId = cats[0]?.id ?? cats[0]?.Id;
  if (!token || !companyId || !categoryId) { console.log(`missing prereqs token=${!!token} company=${companyId} category=${categoryId}`); await browser.close(); process.exit(1); }

  const created = [];
  for (const name of DEMO_NAMES) {
    const res = await ctx.request.post(`${BASE}/Product/SaveProduct`, {
      headers: { 'RequestVerificationToken': token, 'Content-Type': 'application/json' },
      data: { Name: name, Description: 'CR-187 normalized-search demo', CategoryId: categoryId, CompanyId: companyId, IsActive: true },
    });
    const body = await res.json().catch(() => ({}));
    const id = body?.data?.id ?? body?.data?.Id ?? '?';
    created.push({ id, name, ok: res.ok() && body.success });
    console.log(`  ${res.ok() && body.success ? '✅ created' : '❌ failed'} #${id}  "${name}"`);
  }

  await browser.close();
  console.log(`\nSeeded ${created.filter(c => c.ok).length}/${DEMO_NAMES.length} demo products (company #${companyId}, category #${categoryId}).`);
  console.log('Try searching (case/symbol/hyphen insensitive):  "product a"  ·  "Product A"  ·  "ecofriendly widget"  ·  "product b"');
  console.log('Clean up later with:  node Scripts/smoke-prod/cr187-seed-demo-products.js --delete');
  process.exit(0);
})();
