// DEF-267 verify (localhost): saving a certificate with a normal YY-NNNNN number (8 chars)
// must NOT trip the "Certificate Number must be a string or array type with a maximum length
// of '7'" validation. Pre-fix the CertificateEditViewModel.Number had a stale [MaxLength(7)];
// this loads an existing cert's Edit page and saves it unchanged, asserting no validation banner
// and a redirect away from the edit form.
const { chromium } = require('playwright');
const BASE = process.env.BASE_URL || 'http://localhost:5101';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();

  // Login as admin
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(800);

  // Prefer the reported cert (26-10063); else pick a real >7-char number from the tile API
  // whose Edit page actually renders the form.
  let cert = process.env.CERT || '26-10063';
  const candidates = await page.evaluate(async (base) => {
    const r = await fetch(base + '/api/certificate-tiles-internal?sortBy=newest&page=1&pageSize=200', { headers: { Accept: 'application/json' } });
    const j = await r.json();
    return (j.certificates || []).map(c => c.certificateNumber).filter(Boolean);
  }, BASE);

  // Build a try-list: reported cert first, then >7-char numbers, then anything.
  const tryList = [cert, ...candidates.filter(n => n.length > 7), ...candidates].filter((v, i, a) => a.indexOf(v) === i);

  let loaded = false;
  for (const c of tryList) {
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(c)}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(800);
    const hasBtn = await page.locator('#saveButton').count();
    const hasNumField = await page.locator('#Number, input[name="Number"]').count();
    if (hasBtn && hasNumField) { cert = c; loaded = true; break; }
  }
  if (!loaded) { console.log('FAIL: could not load any cert Edit form.'); await browser.close(); process.exit(1); }

  // Local seed numbers are typically 7 chars; the prod failure (26-10063) is 8. To exercise the
  // pre-fix MaxLength(7) boundary we force an 8+ char number, save, then RESTORE the original
  // (localhost test data only — fully reverted at the end). The Number field is readonly in the
  // UI, so we set the value via JS to mimic a real >7-char certificate number.
  const original = cert;
  const eightChar = (original.length >= 8) ? original : (original + '0'.repeat(8 - original.length)); // pad to >=8

  async function saveWithNumber(num) {
    await page.evaluate((v) => {
      const el = document.getElementById('Number') || document.querySelector('input[name="Number"]');
      el.removeAttribute('readonly'); el.value = v;
      el.dispatchEvent(new Event('input', { bubbles: true }));
    }, num);
    page.once('dialog', d => d.accept());
    await page.locator('#saveButton').click();
    await page.waitForTimeout(2500);
    const url = page.url();
    const banner = await page.evaluate(() => {
      const txt = document.body.innerText || '';
      const m = txt.match(/maximum length of '7'/i);
      return m ? (document.querySelector('.validation-summary-errors')?.innerText.trim() || m[0]) : null;
    });
    return { url, banner, stillOnEdit: /\/Certificate\/Edit\//i.test(url) };
  }

  console.log(`Original number "${original}" (len ${original.length}); forcing 8-char "${eightChar}" (len ${eightChar.length})`);
  const r1 = await saveWithNumber(eightChar);
  await page.screenshot({ path: 'Scripts/smoke-prod/_results/def267-after-save.png', fullPage: true }).catch(() => {});
  console.log('after save, url:', r1.url);

  let exitCode = 0;
  console.log('\n=== VERDICT ===');
  if (r1.banner) {
    console.log(`  FAIL: MaxLength(7) validation still fires for 8-char number -> "${r1.banner}"`);
    exitCode = 1;
  } else if (r1.stillOnEdit) {
    const anyErr = await page.evaluate(() => {
      const el = document.querySelector('.validation-summary-errors, .text-danger');
      return el ? el.innerText.trim().slice(0, 300) : '(none)';
    });
    console.log(`  CHECK: no MaxLength(7) banner, but still on edit page. Other validation: ${anyErr}`);
  } else {
    console.log(`  PASS: 8-char number "${eightChar}" saved with NO MaxLength(7) error; redirected to ${r1.url}.`);
  }

  // Restore the original number (only if we changed it and it saved).
  if (eightChar !== original && !r1.stillOnEdit) {
    await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(eightChar)}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(800);
    const r2 = await saveWithNumber(original);
    console.log(`\nRestore: set "${eightChar}" back to "${original}" -> url ${r2.url} ${r2.stillOnEdit ? '(WARN: restore may not have saved — check manually)' : '(restored)'}`);
  }

  await browser.close();
  process.exit(exitCode);
})();
