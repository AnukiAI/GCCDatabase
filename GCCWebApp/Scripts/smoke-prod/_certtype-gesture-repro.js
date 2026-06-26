// Does the cert-type SearchableSelect commit its value for each user gesture?
// Tests: (A) click an option, (B) type full name then blur, (C) type then Tab, (D) type then Enter.
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');

async function freshForm(page) {
  await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.waitForTimeout(1200);
}
const selVal = page => page.$eval('#CertificationTypeId', s => s.value);
const inputBox = '.searchable-select-wrapper input'; // first wrapper is cert type? we target by proximity below

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1440, height: 1400 } })).newPage();
  try {
    await login(page, 'analyst');

    // Find the search input that belongs to the CertificationTypeId select (its wrapper is the next sibling).
    async function certInput() {
      return await page.evaluateHandle(() => {
        const s = document.querySelector('#CertificationTypeId');
        return s.nextElementSibling && s.nextElementSibling.querySelector('input');
      });
    }

    // (A) Click an option in the dropdown
    await freshForm(page);
    let inp = await certInput();
    await inp.asElement().click();
    await page.waitForTimeout(200);
    await inp.asElement().type('Recycled Content', { delay: 10 });
    await page.waitForTimeout(300);
    // click the matching dropdown item
    await page.locator('.searchable-select-item:has-text("Recycled Content")').first().click().catch(e => console.log('  click-item err:', e.message));
    await page.waitForTimeout(200);
    console.log('(A) click option        -> select.value =', JSON.stringify(await selVal(page)));

    // (B) Type full exact name, then blur by clicking elsewhere (NO option click)
    await freshForm(page);
    inp = await certInput();
    await inp.asElement().click();
    await inp.asElement().type('Recycled Content', { delay: 10 });
    await page.waitForTimeout(200);
    await page.locator('#Location, input[name="Location"]').first().click().catch(() => page.locator('body').click());
    await page.waitForTimeout(200);
    console.log('(B) type + blur (no click) -> select.value =', JSON.stringify(await selVal(page)));

    // (C) Type then press Tab
    await freshForm(page);
    inp = await certInput();
    await inp.asElement().click();
    await inp.asElement().type('Recycled Content', { delay: 10 });
    await page.waitForTimeout(200);
    await page.keyboard.press('Tab');
    await page.waitForTimeout(200);
    console.log('(C) type + Tab           -> select.value =', JSON.stringify(await selVal(page)));

    // (D) Type then press Enter (highlighted match)
    await freshForm(page);
    inp = await certInput();
    await inp.asElement().click();
    await inp.asElement().type('Recycled Content', { delay: 10 });
    await page.waitForTimeout(200);
    await page.keyboard.press('Enter');
    await page.waitForTimeout(200);
    console.log('(D) type + Enter         -> select.value =', JSON.stringify(await selVal(page)));

  } catch (e) {
    console.log('FATAL:', e.message);
  } finally {
    await browser.close();
  }
})();
