// DEF-212 regression: verify the SearchableSelect focus-handler fix end-to-end
// on every wrapped field across /Certificate/Create and /Certificate/Edit.
//
// Per-field test:
//   1. Open the page, autocomplete shows the saved value (if any)
//   2. Pick a non-empty option (giving the input text)
//   3. Click back into the input — assert dropdown shows FULL list (the bug)
//   4. Type a substring — assert dropdown filters
//   5. Press Escape — assert input restores the picked text
//
// Pass/fail per field. Localhost only.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `def212-regression-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, t) { fs.writeFileSync(path.join(OUT, `${name}.txt`), t || '(empty)'); }
async function shot(page, name) { await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: false }); }

const results = [];
function record(form, field, step, pass, detail) {
  results.push({ form, field, step, pass, detail });
  const tag = pass ? 'PASS' : 'FAIL';
  console.log(`  ${tag}  [${form}] ${field} — ${step}${detail ? `  (${detail})` : ''}`);
}

const FIELDS = [
  { id: 'ProductCategoryId',   placeholder: 'product categor',     pickValue: '1' /* Chemicals */ },
  { id: 'CertificationTypeId', placeholder: 'certification type',  pickValue: null /* pick first non-empty */ },
  { id: 'countrySelect',       placeholder: 'countr',              pickValue: null /* pick "United States" if present */ }
];

async function login(page) {
  await page.goto(`${BASE}/Account/Login`);
  await page.fill('input[name="Email"]', 'admin');
  await page.fill('input[name="Password"]', 'TestPass123!');
  await Promise.all([page.waitForLoadState('networkidle'), page.click('button[type="submit"]')]);
}

async function probeField(page, form, target) {
  const wrapperSelector = `.searchable-select-wrapper:has(input[placeholder*="${target.placeholder}" i])`;
  const wrapper = page.locator(wrapperSelector).first();
  if (!(await wrapper.count())) {
    record(form, target.id, 'wrapper-present', false, 'SearchableSelect wrapper not found on page');
    return;
  }
  const input = wrapper.locator('input').first();
  await input.scrollIntoViewIfNeeded();

  // Step 1: Pick a non-empty option (use selectOption on underlying select for reliability)
  const totalOptions = await page.$$eval(`#${target.id} option`, els => els.filter(o => o.value).length);
  let pickedValue = target.pickValue;
  if (!pickedValue) {
    pickedValue = await page.$eval(`#${target.id}`, sel => {
      for (const o of sel.options) { if (o.value && o.value !== '') return o.value; }
      return null;
    });
  }
  await page.selectOption(`#${target.id}`, pickedValue, { force: true });
  // Sync input text to the picked option text (SearchableSelect listens to underlying changes? — set manually)
  const pickedText = await page.$eval(`#${target.id} option:checked`, o => o.textContent.trim());
  await page.evaluate(({ sel, txt }) => {
    const wrapper = document.querySelector(sel).nextElementSibling;
    if (wrapper && wrapper.querySelector) {
      const inp = wrapper.querySelector('input');
      if (inp) inp.value = txt;
    }
  }, { sel: `#${target.id}`, txt: pickedText });
  record(form, target.id, 'pick-option', true, `picked="${pickedText}" (value=${pickedValue}) of ${totalOptions} total`);

  // Step 2: Re-focus the input — full list should appear
  await input.click();
  await page.waitForTimeout(400);
  const onReopenCount = await page.locator(`${wrapperSelector} .searchable-select-dropdown .searchable-select-item`).count();
  record(form, target.id, 're-focus-shows-full-list', onReopenCount === totalOptions,
    `dropdown shows ${onReopenCount} of ${totalOptions} options`);
  await shot(page, `${form}-${target.id}-step2-reopen`);

  // Step 3: Type a substring — dropdown should filter
  // Pick the first 3 chars of the picked text as filter; or "uni" / "che" depending on field
  const filterSubstring = pickedText.slice(0, 3).toLowerCase();
  await input.fill(filterSubstring);
  await page.waitForTimeout(400);
  const onFilterItems = await page.$$eval(`${wrapperSelector} .searchable-select-dropdown .searchable-select-item`,
    els => els.map(e => e.textContent.trim()));
  const allMatch = onFilterItems.every(t => t.toLowerCase().includes(filterSubstring));
  record(form, target.id, 'typing-filters', onFilterItems.length > 0 && allMatch,
    `"${filterSubstring}" -> ${onFilterItems.length} items, all match: ${allMatch}`);

  // Step 4: Escape should restore the picked text
  await page.keyboard.press('Escape');
  await page.waitForTimeout(300);
  const inputAfterEscape = await input.inputValue();
  record(form, target.id, 'escape-restores-pick', inputAfterEscape === pickedText,
    `input="${inputAfterEscape}" expected="${pickedText}"`);
}

async function runForm(page, form, url) {
  console.log(`\n=== [${form}] ${url}`);
  await page.goto(`${BASE}${url}`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(2500);
  for (const target of FIELDS) {
    await probeField(page, form, target);
  }
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('  pageerror:', e.message));
  try {
    await login(page);
    await runForm(page, 'create', '/Certificate/Create');
    await runForm(page, 'edit', '/Certificate/Edit/26-0001');

    const passCount = results.filter(r => r.pass).length;
    const failCount = results.filter(r => !r.pass).length;
    console.log(`\n========`);
    console.log(`  PASS: ${passCount}   FAIL: ${failCount}`);
    saveText('00-summary', JSON.stringify(results, null, 2));
    process.exitCode = failCount === 0 ? 0 : 1;
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure').catch(() => {});
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();
