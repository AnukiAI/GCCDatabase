// Reproduce: pick a category in SearchableSelect, then re-click — does the
// dropdown show the FULL list or only the already-selected item?

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `category-reopen-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, t) { fs.writeFileSync(path.join(OUT, `${name}.txt`), t || '(empty)'); }
async function shot(page, name) { await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: false }); console.log('  shot:', name); }

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

    await page.goto(`${BASE}/Certificate/Create`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    const catInput = page.locator('.searchable-select-wrapper input[placeholder*="categor" i]').first();
    await catInput.scrollIntoViewIfNeeded();

    // [1] Pick "testtest"
    console.log('\n[1] Click input, type "test", pick testtest');
    await catInput.click();
    await catInput.fill('test');
    await page.waitForTimeout(400);
    await shot(page, '01-filtered-test');

    const optsBeforePick = await page.$$eval('.searchable-select-dropdown .searchable-select-item',
      els => els.map(e => ({ value: e.getAttribute('data-value'), text: e.textContent.trim() })));
    console.log('  filtered options:', optsBeforePick.length, JSON.stringify(optsBeforePick));

    // Click the testtest item
    const testtestItem = page.locator('.searchable-select-dropdown .searchable-select-item[data-value="12"]').first();
    await testtestItem.click();
    await page.waitForTimeout(500);
    await shot(page, '02-after-pick-testtest');

    const valueAfterPick = await catInput.inputValue();
    const underlyingValue = await page.$eval('#ProductCategoryId', s => s.value);
    console.log('  input shows:', JSON.stringify(valueAfterPick), '| underlying select value:', underlyingValue);

    // [2] Now click into the input again — what does the dropdown show?
    console.log('\n[2] Re-click the input — what shows in the dropdown?');
    await catInput.click();
    await page.waitForTimeout(500);
    await shot(page, '03-reopened-dropdown');

    const optsOnReopen = await page.$$eval('.searchable-select-dropdown .searchable-select-item',
      els => els.map(e => ({ value: e.getAttribute('data-value'), text: e.textContent.trim() })));
    console.log('  options on re-open:', optsOnReopen.length, JSON.stringify(optsOnReopen));
    saveText('04-options-on-reopen', JSON.stringify(optsOnReopen, null, 2));

    // Inspect dropdown HTML, input value, and what filter is being applied
    const state = await page.evaluate(() => {
      const input = document.querySelector('.searchable-select-wrapper input[placeholder*="categor" i]');
      const dropdown = input?.parentElement?.querySelector('.searchable-select-dropdown');
      return {
        inputValue: input?.value,
        dropdownDisplay: dropdown?.style.display,
        dropdownItemCount: dropdown?.querySelectorAll('.searchable-select-item').length,
        noResultsShown: dropdown?.innerText?.includes('No matches'),
        dropdownHtml: dropdown?.innerHTML.slice(0, 500)
      };
    });
    saveText('05-state-on-reopen', JSON.stringify(state, null, 2));
    console.log('  state on re-open:', JSON.stringify(state).slice(0, 300));

    // [3] What if user clears the input? Does the full list come back?
    console.log('\n[3] Triple-click → Delete → does full list reappear?');
    await catInput.click({ clickCount: 3 });
    await page.keyboard.press('Delete');
    await page.waitForTimeout(400);
    const optsAfterClear = await page.$$eval('.searchable-select-dropdown .searchable-select-item',
      els => els.map(e => ({ value: e.getAttribute('data-value'), text: e.textContent.trim() })));
    console.log('  options after clear:', optsAfterClear.length);
    await shot(page, '06-after-clear');

    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure').catch(() => {});
  } finally {
    await browser.close();
  }
})();
