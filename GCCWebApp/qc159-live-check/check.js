// QC #159 live verification — does the "Replace all existing" toggle exist on gccwebapp.com?
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const OUT = __dirname;
const BASIC_USER = 'gcc';
const BASIC_PASS = 'Cert-Test-Access-2026!';
const APP_USER = 'analyst';
const APP_PASS = 'TestPass123!';
const BASE = 'https://gccwebapp.com';

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    httpCredentials: { username: BASIC_USER, password: BASIC_PASS },
    viewport: { width: 1440, height: 900 },
    ignoreHTTPSErrors: true,
  });
  const page = await ctx.newPage();
  page.on('console', msg => {
    const t = msg.type();
    if (t === 'error' || t === 'warning') console.log('  [browser]', t, msg.text().slice(0, 250));
  });
  page.on('pageerror', err => console.log('  [pageerror]', err.message));

  const log = (m) => console.log(`[${new Date().toISOString().slice(11,19)}] ${m}`);

  let outcome = { reached: 'start', toggleFound: null, notes: [] };

  try {
    log('Step 1: home page (basic auth check)');
    const home = await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 45000 });
    log(`  status=${home.status()}  url=${page.url()}`);
    await page.screenshot({ path: path.join(OUT, '01-home.png'), fullPage: true });
    outcome.reached = 'home';

    log('Step 2: navigate to login');
    if (!/login/i.test(page.url())) {
      await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
    }
    await page.screenshot({ path: path.join(OUT, '02-login.png'), fullPage: true });

    log('Step 3: sign in as analyst');
    // Local form: id="Email" accepts username; id="Password"; submit button = "Login"
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(2500);
    log(`  after login url=${page.url()}`);
    await page.screenshot({ path: path.join(OUT, '03-after-login.png'), fullPage: true });

    // If still on /Login, try email form
    if (/\/Account\/Login/i.test(page.url())) {
      log('  login bounced — retrying with email form');
      await page.locator('#Email').fill('analyst@test-gcc.local');
      await page.locator('#Password').fill(APP_PASS);
      await Promise.all([
        page.waitForLoadState('domcontentloaded'),
        page.locator('button[type="submit"]').first().click(),
      ]);
      await page.waitForTimeout(2500);
      log(`  after retry url=${page.url()}`);
      await page.screenshot({ path: path.join(OUT, '03b-after-login-retry.png'), fullPage: true });
    }
    outcome.reached = 'logged-in';

    log('Step 4: go to internal cert tile view');
    await page.goto(BASE + '/Certificate/CertificateTileViewInternal', { waitUntil: 'networkidle', timeout: 45000 });
    await page.waitForTimeout(4000);
    await page.screenshot({ path: path.join(OUT, '04-tile-view.png'), fullPage: true });

    log('Step 5: collect candidate cert NUMBERS from tile view');
    const candidateIds = await page.locator('.certificate-card .certificate-id').evaluateAll(els =>
      els.map(e => (e.innerText || '').trim()).filter(Boolean)
    );
    log(`  candidate cert numbers: ${candidateIds.slice(0, 12).join(',')}${candidateIds.length > 12 ? ',...' : ''} (total ${candidateIds.length})`);

    if (candidateIds.length === 0) {
      log('  giving up: no cert numbers to open');
      outcome.notes.push('No cert numbers discoverable from tile view');
    } else {
      // Pick a cert that has existing product IDs (carly's scenario)
      // Strategy: try first cert; if it has 0 product IDs, walk down list
      for (const certNum of candidateIds.slice(0, 6)) {
        log(`Step 6: open /Certificate/Edit/${certNum}`);
        await page.goto(`${BASE}/Certificate/Edit/${certNum}`, { waitUntil: 'networkidle', timeout: 45000 });
        await page.waitForTimeout(2500);

        const importBtn = page.locator('#importProductIds');
        const importCount = await importBtn.count();
        log(`  Import button count: ${importCount}`);
        if (importCount === 0) {
          outcome.notes.push(`Cert ${certNum}: no #importProductIds button — skip`);
          continue;
        }

        // Count existing product-id rows
        const existingRows = await page.locator('.btn-edit-product-id').count();
        log(`  existing Product ID rows: ${existingRows}`);
        if (existingRows < 1) {
          outcome.notes.push(`Cert ${certNum}: 0 existing product IDs — bad test target, trying next`);
          continue;
        }

        await importBtn.scrollIntoViewIfNeeded();
        await page.screenshot({ path: path.join(OUT, `05-cert-${certNum}-edit.png`), fullPage: true });

        log(`  clicking Import button (cert ${certNum} has ${existingRows} existing IDs)`);
        // The Import button creates a hidden file input and triggers .click() — Playwright
        // intercepts via filechooser event.
        // Headers must match parser keys: type, value (or productid), productname, productsku, partner, description
        const csv = 'Type,Value,ProductName,ProductSKU\nASIN,B00LIVECHECK,Live Check Product,SKU-LIVE-001\n';
        const tmpCsv = path.join(OUT, 'tiny.csv');
        fs.writeFileSync(tmpCsv, csv);

        const [fileChooser] = await Promise.all([
          page.waitForEvent('filechooser', { timeout: 8000 }),
          importBtn.click(),
        ]);
        await fileChooser.setFiles(tmpCsv);
        log('  CSV file provided to native chooser');
        await page.waitForTimeout(3000);
        await page.screenshot({ path: path.join(OUT, `07-cert-${certNum}-import-preview.png`), fullPage: true });

        // Assert the toggle is present
        const toggle = page.locator('#overwrite-existing-toggle');
        const label = page.locator('#overwrite-existing-label');
        const labelByText = page.locator('label:has-text("Replace all existing")');
        const toggleCount = await toggle.count();
        const labelCount = await label.count();
        const labelTextCount = await labelByText.count();
        log(`  toggle#overwrite-existing-toggle: ${toggleCount}`);
        log(`  label#overwrite-existing-label : ${labelCount}`);
        log(`  label by text "Replace all existing": ${labelTextCount}`);

        outcome.toggleFound = toggleCount > 0;
        outcome.toggleLabelById = labelCount > 0;
        outcome.toggleLabelByText = labelTextCount > 0;
        outcome.testedCert = certNum;
        outcome.existingProductIds = existingRows;
        outcome.reached = 'modal-checked';

        if (toggleCount > 0) {
          const visible = await toggle.isVisible().catch(() => false);
          log(`  toggle visible: ${visible}`);
          outcome.toggleVisible = visible;
          try {
            const lh = await label.first().elementHandle();
            if (lh) await lh.scrollIntoViewIfNeeded();
          } catch {}
          await page.screenshot({ path: path.join(OUT, `08-cert-${certNum}-toggle-context.png`), fullPage: true });

          const footer = page.locator('.admin-modal-footer').first();
          if (await footer.count() > 0) {
            await footer.screenshot({ path: path.join(OUT, `09-cert-${certNum}-footer-tight.png`) });
            const html = await footer.evaluate(el => el.outerHTML).catch(() => null);
            if (html) fs.writeFileSync(path.join(OUT, `10-cert-${certNum}-footer.html`), html);
          }

          outcome.notes.push(`Cert ${certNum}: ✅ Replace-all toggle present (visible=${visible}); footer HTML saved`);
        } else {
          outcome.notes.push(`Cert ${certNum}: ❌ Import modal opened but toggle NOT in DOM`);
        }
        break;
      }
    }

    log('DONE');
  } catch (e) {
    log(`ERROR: ${e.message}`);
    outcome.notes.push(`Exception: ${e.message}`);
    await page.screenshot({ path: path.join(OUT, 'error.png'), fullPage: true }).catch(() => {});
  } finally {
    fs.writeFileSync(path.join(OUT, 'outcome.json'), JSON.stringify(outcome, null, 2));
    await browser.close();
  }
})();
