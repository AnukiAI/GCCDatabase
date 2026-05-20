// Final snapshot — Edit form viewport + direct API peek at GetProductTypes
// for the two category cases (Carly Test vs Chemicals) to show what would
// appear in the dropdown.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const OUT = path.join(__dirname, 'screenshots', 'qc170v5-2026-05-19T21-' +
  new Date().toISOString().slice(14, 16) + '-' +
  new Date().toISOString().slice(17, 19) + '-snapshot');
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, text) { fs.writeFileSync(path.join(OUT, `${name}.txt`), text || '(empty)'); }
async function shot(page, name) { await page.screenshot({ path: path.join(OUT, `${name}.png`) }); console.log('  shot:', name); }

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  try {
    await page.goto(`${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', 'admin');
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([
      page.waitForLoadState('networkidle'),
      page.click('button[type="submit"]')
    ]);

    // Direct API call: what does the Type dropdown contain for each category?
    const apiResult = await page.evaluate(async () => {
      const a = await (await fetch('/Certificate/GetProductTypes?categoryId=14')).json();
      const b = await (await fetch('/Certificate/GetProductTypes?categoryId=1')).json();
      return { carlyTest: a, chemicals: b };
    });
    saveText('A-GetProductTypes-API', JSON.stringify(apiResult, null, 2));
    console.log('Carly Test cat -> Type options:', JSON.stringify(apiResult.carlyTest.data));
    console.log('Chemicals cat  -> Type options:', JSON.stringify(apiResult.chemicals.data));

    // Edit form viewport
    await page.goto(`${BASE}/Certificate/Edit/26-0001`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2500);
    await page.locator('#ProductCategoryId').scrollIntoViewIfNeeded().catch(() => {});
    await shot(page, 'B-edit-form-product-section');

    // Click into the Type dropdown for a full view
    await page.locator('#ProductTypeId').click().catch(() => {});
    await page.waitForTimeout(500);
    await shot(page, 'C-edit-form-type-dropdown-open');
    await page.keyboard.press('Escape').catch(() => {});

    // Capture the cat/type/sub values from the form
    const formState = await page.evaluate(() => {
      const cat = document.querySelector('#ProductCategoryId');
      const typ = document.querySelector('#ProductTypeId');
      const sub = document.querySelector('#ProductSubcategoryId');
      return {
        category: { value: cat?.value, text: cat?.options[cat?.selectedIndex]?.text },
        type: { value: typ?.value, options: typ ? Array.from(typ.options).map(o => o.text) : [] },
        subcategory: { value: sub?.value, options: sub ? Array.from(sub.options).map(o => o.text) : [] }
      };
    });
    saveText('B-edit-form-state', JSON.stringify(formState, null, 2));
    console.log('Edit form state:', JSON.stringify(formState));

    console.log('\nOutput:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure').catch(() => {});
  } finally {
    await browser.close();
  }
})();
