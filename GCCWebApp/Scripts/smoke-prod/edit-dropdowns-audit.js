// Confirm the re-focus-shows-only-selected bug on every SearchableSelect on the
// Edit form, and inventory every other select for related patterns.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `edit-audit-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, t) { fs.writeFileSync(path.join(OUT, `${name}.txt`), t || '(empty)'); }
async function shot(page, name) { await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: false }); console.log('  shot:', name); }

// SearchableSelect-wrapped IDs per the cshtml audit
const TARGETS = [
  { id: 'ProductCategoryId',    placeholder: 'product categor' },
  { id: 'CertificationTypeId',  placeholder: 'certification type' },
  { id: 'countrySelect',        placeholder: 'countr' }
];

async function probeReopenBug(page, target) {
  const sel = `#${target.id}`;
  const exists = await page.locator(sel).count();
  if (!exists) { console.log(`  [${target.id}] not present`); return null; }
  const wrapped = await page.locator(`${sel} + .searchable-select-wrapper, .searchable-select-wrapper:has(input[placeholder*="${target.placeholder}" i])`).count();
  console.log(`  [${target.id}] wrapped by SearchableSelect: ${wrapped > 0 ? 'YES' : 'NO'}`);

  if (!wrapped) {
    // Native select — count its options for baseline
    const opts = await page.$$eval(sel + ' option', els => els.length);
    return { id: target.id, type: 'native', optionCount: opts };
  }

  // Open SearchableSelect, count items in dropdown when the input still has the existing selected text
  const input = page.locator(`.searchable-select-wrapper input[placeholder*="${target.placeholder}" i]`).first();
  await input.scrollIntoViewIfNeeded();
  const initialInputText = await input.inputValue();

  await input.click();
  await page.waitForTimeout(400);
  const itemsOnReopen = await page.$$eval('.searchable-select-dropdown:has(.searchable-select-item) .searchable-select-item',
    els => els.map(e => ({ value: e.getAttribute('data-value'), text: e.textContent.trim() })))
    .catch(() => []);

  // Underlying select total
  const totalOptions = await page.$$eval(sel + ' option', els => els.filter(o => o.value).length);
  // Press Escape to close
  await page.keyboard.press('Escape').catch(() => {});

  return {
    id: target.id,
    type: 'searchable',
    inputHadText: initialInputText,
    underlyingTotalOptions: totalOptions,
    itemsShownOnReopen: itemsOnReopen.length,
    sampleItems: itemsOnReopen.slice(0, 6),
    bugPresent: initialInputText.length > 0 && itemsOnReopen.length < totalOptions
  };
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    httpCredentials: { username: 'gcc', password: 'Cert-Test-Access-2026!' }
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  pageerror:', e.message));

  try {
    await page.goto(`${BASE}/Account/Login`);
    await page.fill('input[name="Email"]', 'admin');
    await page.fill('input[name="Password"]', 'TestPass123!');
    await Promise.all([page.waitForLoadState('networkidle'), page.click('button[type="submit"]')]);

    // Use a real prod cert
    await page.goto(`${BASE}/Certificate/Edit/26-0163`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);
    await shot(page, '01-edit-loaded');

    console.log('\n[A] Probe each SearchableSelect on Edit form');
    const results = {};
    for (const t of TARGETS) {
      results[t.id] = await probeReopenBug(page, t);
      console.log('   ', t.id, '->', JSON.stringify(results[t.id]));
    }
    saveText('02-searchable-select-probes', JSON.stringify(results, null, 2));

    // Inventory ALL selects on the Edit form
    const allSelects = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('select')).map(s => {
        const wrapper = s.nextElementSibling;
        const isSearchable = wrapper && wrapper.classList && wrapper.classList.contains('searchable-select-wrapper');
        return {
          id: s.id,
          name: s.name,
          optionCount: s.options.length,
          currentValue: s.value,
          isSearchable,
          inputValueIfSearchable: isSearchable ? wrapper.querySelector('input')?.value : null,
          visible: s.offsetParent !== null || isSearchable
        };
      });
    });
    saveText('03-all-selects', JSON.stringify(allSelects, null, 2));
    console.log('\n[B] All <select> elements on Edit page:');
    allSelects.forEach(s => console.log(`    ${s.id || '(no id)'} | ${s.isSearchable ? 'SearchableSelect' : 'native'} | options=${s.optionCount} | selected="${s.currentValue}"${s.inputValueIfSearchable ? ` | input="${s.inputValueIfSearchable}"` : ''}`));

    // Also check the Create page for comparison
    console.log('\n[C] Probe the Create page same way');
    await page.goto(`${BASE}/Certificate/Create`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2500);

    // Pick a category first to give the input some text
    const catInput = page.locator('.searchable-select-wrapper input[placeholder*="product categor" i]').first();
    await catInput.click();
    await catInput.fill('Chem');
    await page.waitForTimeout(400);
    await page.locator('.searchable-select-dropdown .searchable-select-item[data-value="1"]').first().click();
    await page.waitForTimeout(500);

    // Also pick a cert type to give that input text
    const ctypeInput = page.locator('.searchable-select-wrapper input[placeholder*="certification type" i]').first();
    if (await ctypeInput.count()) {
      await ctypeInput.click();
      await ctypeInput.fill('');
      await page.waitForTimeout(200);
      // Pick the first non-empty item
      const firstItem = page.locator('.searchable-select-dropdown .searchable-select-item[data-value]').first();
      if (await firstItem.count()) {
        await firstItem.click();
        await page.waitForTimeout(400);
      }
    }

    // And country
    const countryInput = page.locator('.searchable-select-wrapper input[placeholder*="countr" i]').first();
    if (await countryInput.count()) {
      await countryInput.click();
      await countryInput.fill('United');
      await page.waitForTimeout(400);
      const firstCountryItem = page.locator('.searchable-select-dropdown .searchable-select-item[data-value]').first();
      if (await firstCountryItem.count()) { await firstCountryItem.click(); await page.waitForTimeout(400); }
    }

    const createResults = {};
    for (const t of TARGETS) {
      createResults[t.id] = await probeReopenBug(page, t);
      console.log('   ', t.id, '->', JSON.stringify(createResults[t.id]));
    }
    saveText('04-create-probes', JSON.stringify(createResults, null, 2));

    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure').catch(() => {});
  } finally {
    await browser.close();
  }
})();
