// Regenerate a guide PDF from its HTML source.
// Headless Chromium print-to-PDF — the same path as "Print > Save as PDF" in a browser,
// which is how these exports were produced originally.
//
// Usage: node _render-template-pdf.js <input.html> <output.pdf>
const { chromium } = require('playwright');
const path = require('path');
const url = require('url');

const SRC = process.argv[2];
const OUT = process.argv[3];

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto(url.pathToFileURL(SRC).href, { waitUntil: 'networkidle' });
  await page.emulateMedia({ media: 'print' });
  await page.pdf({
    path: OUT,
    format: 'Letter',
    printBackground: true,
    margin: { top: '0.6in', bottom: '0.6in', left: '0.6in', right: '0.6in' },
  });
  await browser.close();
  console.log('written: ' + path.basename(OUT));
})().catch((e) => { console.error('RENDER ERROR:', e.message); process.exit(1); });
