// Does the certificate detail page's inline <script> actually RUN?
//
// WHY THIS EXISTS (2026-08-25, live on the test environment):
// The DEF-369 fix added `const url = ...` to window.downloadCertificate in
// CertificateSingleView.cshtml. That function ALREADY declared `const url` further
// down for the blob object URL, at the same function scope. A duplicate `const` is a
// PARSE-time SyntaxError, so the browser discarded the ENTIRE inline <script> block:
//
//     SyntaxError: Identifier 'url' has already been declared
//
// Both onclick handlers the page renders — downloadCertificate and openImageZoom —
// were therefore never defined, and the Download and Zoom buttons did nothing at all.
// Clicking produced no network request, no error toast, no feedback whatsoever.
//
// Nothing server-side could see this. The endpoint returned a correct 837 KB PDF, the
// markup rendered the button correctly with the right arguments, and every HTTP-level
// assertion passed. The page was broken only once a JS engine parsed it. That is why
// this check runs in a REAL BROWSER and asserts the handlers are CALLABLE, rather than
// asserting that the markup mentions them.
//
// Usage: node Scripts/smoke-prod/_views/single-view-script-block.js [baseUrl] [certNumber] [cid]
'use strict';
const { chromium } = require('playwright');

const BASE = process.argv[2] || process.env.BASE || 'http://localhost:5101';
const NUMBER = process.argv[3] || '26-0475';
const CID = process.argv[4] || '3155';

// Every function the rendered markup calls via onclick must exist by load time.
const REQUIRED_HANDLERS = ['downloadCertificate', 'openImageZoom', 'closeImageZoom'];

let pass = 0, fail = 0;
const ok = (name, cond, detail = '') => {
  if (cond) { pass++; console.log(`  PASS  ${name}${detail ? ' — ' + detail : ''}`); }
  else { fail++; console.log(`  FAIL  ${name}${detail ? ' — ' + detail : ''}`); }
};

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, acceptDownloads: true });

  // Test-environment gate (no-op on localhost, required on the deployed site).
  if (/gccwebapp\.com/.test(BASE)) {
    const gate = await ctx.newPage();
    await gate.goto(`${BASE}/__gate`, { waitUntil: 'domcontentloaded' });
    await gate.fill('input[name="password"]', process.env.GATE_PASS || 'Cert-Test-Access-2026!');
    await Promise.all([gate.waitForNavigation({ waitUntil: 'domcontentloaded' }), gate.click('button[type=submit]')]);
    await gate.close();
  }

  const page = await ctx.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });

  const url = `${BASE}/Certificate/CertificateSingleView/${encodeURIComponent(NUMBER)}?cid=${CID}`;
  console.log(`\n=== ${url} ===`);
  const resp = await page.goto(url, { waitUntil: 'load' });
  ok('page returns 200', resp && resp.status() === 200, `status ${resp && resp.status()}`);

  // THE core assertion: the inline script block parsed and executed.
  ok('no page-level JS error', errors.length === 0,
     errors.length ? errors.join(' | ').slice(0, 240) : 'clean console');

  for (const fn of REQUIRED_HANDLERS) {
    const t = await page.evaluate((n) => typeof window[n], fn);
    ok(`window.${fn} is callable`, t === 'function', `typeof = ${t}`);
  }

  // Every onclick the markup renders must name a function that exists — this catches a
  // handler being renamed or dropped, not just the whole block failing.
  const handlers = await page.evaluate(() =>
    [...document.querySelectorAll('[onclick]')]
      .map((el) => (el.getAttribute('onclick').match(/^\s*([A-Za-z_$][\w$]*)\s*\(/) || [])[1])
      .filter(Boolean));
  const missing = [];
  for (const h of [...new Set(handlers)]) {
    if (await page.evaluate((n) => typeof window[n] !== 'function', h)) missing.push(h);
  }
  ok('every onclick handler is defined', missing.length === 0,
     missing.length ? `undefined: ${missing.join(', ')}` : `${new Set(handlers).size} distinct handlers`);

  // And the button must actually deliver a PDF when clicked — the user-visible outcome.
  const icon = page.locator('.action-icon.download').first();
  if (await icon.count()) {
    const [download] = await Promise.all([
      page.waitForEvent('download', { timeout: 20000 }).catch(() => null),
      icon.click(),
    ]);
    if (download) {
      const fs = require('fs');
      const p = await download.path();
      const head = p ? fs.readFileSync(p).subarray(0, 5).toString('latin1') : '';
      ok('clicking Download yields a real PDF', head.startsWith('%PDF'),
         `filename=${download.suggestedFilename()} magic=${JSON.stringify(head)}`);
    } else {
      ok('clicking Download yields a real PDF', false, 'no download event fired');
    }
  } else {
    console.log('  SKIP  no download icon rendered (certificate has no file)');
  }

  await browser.close();
  console.log(`\n${pass} passed, ${fail} failed`);
  process.exit(fail ? 1 : 0);
})().catch((e) => { console.error('ERROR:', e.message); process.exit(1); });
