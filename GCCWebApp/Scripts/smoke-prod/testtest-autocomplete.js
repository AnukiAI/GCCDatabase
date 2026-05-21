// Type into the SearchableSelect on /Certificate/Create and confirm testtest
// surfaces in the autocomplete dropdown list. Same probe on /Certificate/Edit.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'https://gccwebapp.com';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `testtest-autocomplete-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

function saveText(name, t) { fs.writeFileSync(path.join(OUT, `${name}.txt`), t || '(empty)'); }
async function shot(page, name, full = false) { await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: full }); console.log('  shot:', name); }

async function probeAutocomplete(page, label) {
  // Find the autocomplete input that wraps #ProductCategoryId
  const input = page.locator('select#ProductCategoryId + .searchable-select-wrapper input, .searchable-select-wrapper input[placeholder*="categor" i]').first();
  if (!(await input.count())) {
    console.log(`  [${label}] no autocomplete input found`);
    return;
  }

  await input.scrollIntoViewIfNeeded();
  await input.click();
  await input.fill('test');
  await page.waitForTimeout(500);
  await shot(page, `${label}-typed-test`);

  // Read the suggestion list
  const suggestions = await page.$$eval('.searchable-select-dropdown:visible .searchable-select-item, .searchable-select-dropdown .searchable-select-item',
    els => els.map(e => ({ value: e.getAttribute('data-value'), text: e.textContent.trim() }))).catch(() => []);
  saveText(`${label}-suggestions`, JSON.stringify(suggestions, null, 2));
  console.log(`  [${label}] suggestions for "test":`, suggestions.length, 'items');
  suggestions.forEach(s => console.log('    ', s.value, '->', s.text));
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

    console.log('\n[A] /Certificate/Create — type "test" into category autocomplete');
    await page.goto(`${BASE}/Certificate/Create`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    await probeAutocomplete(page, 'create');

    console.log('\n[B] /Certificate/Edit/26-0001 — same probe');
    await page.goto(`${BASE}/Certificate/Edit/26-0001`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);
    // Edit form may or may not use SearchableSelect on category
    const inputExists = await page.locator('.searchable-select-wrapper input').count();
    console.log('  [edit] searchable-select-wrappers on page:', inputExists);
    if (inputExists) {
      await probeAutocomplete(page, 'edit');
    } else {
      // Just confirm raw select options for completeness
      const opts = await page.$$eval('#ProductCategoryId option', els => els.map(o => ({ value: o.value, text: o.textContent.trim() })));
      saveText('edit-raw-select-options', JSON.stringify(opts, null, 2));
      const t = opts.find(o => /test\s*test/i.test(o.text));
      console.log('  [edit] raw select has testtest?', t ? 'YES' : 'NO');
    }

    console.log('\nDone. Output:', OUT);
  } catch (e) {
    console.error('FAIL:', e.message);
    await shot(page, '99-failure', true).catch(() => {});
  } finally {
    await browser.close();
  }
})();
