// Repro: does editing a product's Type on /Product/Management UPDATE the row or ADD a new one?
// Localhost only. Adds "REPRO-EDIT-<ts>" with type A, edits it to type B, counts rows by name.
// Expected (correct): 1 row. Bug: 2+ rows. Cleans up test rows at the end.
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const NAME = 'REPRO-EDIT-' + (process.env.STAMP || 'X1'); // stamp passed in to keep it deterministic

async function pickFirstReal(page, sel) {
  return page.evaluate((s) => {
    const el = document.querySelector(s);
    const opt = Array.from(el.options).find(o => o.value && o.value !== '0');
    if (opt) { el.value = opt.value; el.dispatchEvent(new Event('change', { bubbles: true })); return { value: opt.value, text: opt.textContent.trim() }; }
    return null;
  }, sel);
}

// Dismiss any AdminCore success/alert overlay that would intercept the next click.
async function dismissOverlays(page) {
  await page.evaluate(() => {
    document.querySelectorAll('[role="alertdialog"] button, .admin-modal-overlay.show button, .gcc-modal-overlay.show button').forEach(b => {
      const t = (b.textContent || '').trim().toLowerCase();
      if (t === 'ok' || t === 'close' || b.classList.contains('admin-btn--primary')) b.click();
    });
    document.querySelectorAll('[role="alertdialog"], .gcc-modal-overlay.show').forEach(o => o.remove());
  });
  await page.waitForTimeout(250);
}

// Select a category that has >=2 real product-type options; returns the chosen typeA.
async function selectCategoryWithTypes(page) {
  const cats = await page.evaluate(() => Array.from(document.querySelectorAll('#product-category option')).map(o => o.value).filter(v => v && v !== '0'));
  for (const cv of cats) {
    await page.evaluate((v) => { const el = document.getElementById('product-category'); el.value = v; el.dispatchEvent(new Event('change', { bubbles: true })); }, cv);
    await page.waitForTimeout(700);
    const types = await page.evaluate(() => Array.from(document.querySelectorAll('#product-type option')).map(o => ({ v: o.value, t: o.textContent.trim() })).filter(o => o.v && o.v !== '0'));
    if (types.length >= 2) {
      await page.evaluate((v) => { const el = document.getElementById('product-type'); el.value = v; el.dispatchEvent(new Event('change', { bubbles: true })); }, types[0].v);
      const catText = await page.evaluate((v) => Array.from(document.querySelectorAll('#product-category option')).find(o => o.value === v)?.textContent.trim(), cv);
      return { catValue: cv, catText, typeA: types[0], allTypes: types };
    }
  }
  return null;
}

async function countByName(page, name) {
  return page.evaluate(async (n) => {
    const r = await fetch('/Product/GetProducts', { headers: { Accept: 'application/json' } });
    const j = await r.json();
    const arr = j.data || j.products || j || [];
    const rows = (Array.isArray(arr) ? arr : []).filter(p => (p.name || p.Name) === n);
    return rows.map(p => ({ id: p.id || p.Id, type: p.productTypeName || p.ProductTypeName || p.productType || '' }));
  }, name);
}

(async () => {
  const browser = await chromium.launch();
  // Pass GATE=1 to use the gccwebapp.com basic-auth site gate (prod test env).
  const ctxOpts = process.env.GATE ? { httpCredentials: { username: 'gcc', password: 'Cert-Test-Access-2026!' } } : {};
  const ctx = await browser.newContext(ctxOpts);
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(800);

  await page.goto(BASE + '/Product/Management', { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(1500);

  // ---- ADD with type A ----
  await page.locator('#add-product-btn').click();
  await page.waitForTimeout(400);
  await page.locator('#product-name').fill(NAME);
  const company = await pickFirstReal(page, '#product-company');
  const sel = await selectCategoryWithTypes(page);
  if (!sel) { console.log('NO category with >=2 types found — cannot test type change'); await browser.close(); process.exit(2); }
  const typeA = sel.typeA;
  console.log('ADD:', NAME, '| company', company?.text, '| category', sel.catText, '| typeA', typeA.t);
  await page.locator('#save-btn').click();
  await page.waitForTimeout(1500);
  await dismissOverlays(page);

  let rows = await countByName(page, NAME);
  console.log('after ADD  -> rows:', JSON.stringify(rows));

  // ---- EDIT that row, change to type B ----
  const editId = rows[0]?.id;
  await page.evaluate((id) => window.editProduct(id), editId);
  await page.waitForTimeout(900);
  const typeB = await page.evaluate((aVal) => {
    const el = document.getElementById('product-type');
    const opt = Array.from(el.options).find(o => o.value && o.value !== '0' && o.value !== aVal);
    if (opt) { el.value = opt.value; el.dispatchEvent(new Event('change', { bubbles: true })); return { value: opt.value, text: opt.textContent.trim() }; }
    return null;
  }, typeA.v);
  console.log('EDIT id', editId, '-> change type', typeA.t, '=>', typeB?.text);
  await page.waitForTimeout(400);
  await page.locator('#save-btn').click();
  await page.waitForTimeout(1500);
  await dismissOverlays(page);

  rows = await countByName(page, NAME);
  console.log('after EDIT -> rows:', JSON.stringify(rows));

  console.log('\n=== VERDICT ===');
  if (rows.length === 1) console.log('  OK on current code: edit UPDATED in place (1 row). Prod likely running older/different build.');
  else console.log(`  BUG REPRODUCED: edit created a new row — now ${rows.length} rows named "${NAME}".`);

  // ---- cleanup ----
  for (const r of rows) {
    try { await page.evaluate(async (id) => {
      const t = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      await fetch('/Product/DeleteProduct?id=' + id, { method: 'DELETE', headers: { 'RequestVerificationToken': t } });
    }, r.id); } catch {}
  }
  console.log('cleanup: deleted', rows.length, 'test row(s)');
  await browser.close();
  process.exit(rows.length === 1 ? 0 : 1);
})();
