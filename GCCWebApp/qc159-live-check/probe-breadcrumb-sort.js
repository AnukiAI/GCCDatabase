// Probe: find actual breadcrumb selectors and sort-dropdown selectors on gccwebapp.com
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = 'https://gccwebapp.com';
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type=submit]').first().click()]);
  await page.waitForTimeout(2000);

  console.log('\n=== /Company/Index — search for breadcrumb-like elements ===');
  await page.goto(BASE + '/Company/Index', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);

  const candidates = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('nav, ol, ul, .breadcrumb, [aria-label*="breadcrumb" i], [class*="breadcrumb" i]').forEach(el => {
      const text = (el.innerText || '').trim();
      if (text && text.length < 200 && /\/|›|>|»|Home/.test(text)) {
        out.push({ tag: el.tagName, classes: el.className?.toString().slice(0, 80), text: text.slice(0, 150) });
      }
    });
    return out.slice(0, 8);
  });
  console.log(JSON.stringify(candidates, null, 2));

  console.log('\n=== /Certificate/CertificateTileView — sort dropdown ===');
  await page.goto(BASE + '/Certificate/CertificateTileView', { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);

  const selects = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('select').forEach(s => {
      out.push({ id: s.id, name: s.name, className: s.className?.toString().slice(0, 60), optionCount: s.options.length, options: Array.from(s.options).slice(0, 12).map(o => o.innerText.trim()) });
    });
    return out;
  });
  console.log(JSON.stringify(selects, null, 2));

  // Also look for "Sort" labels and adjacent controls
  const sortAreas = await page.evaluate(() => {
    const out = [];
    document.querySelectorAll('label, span, h3').forEach(el => {
      if (/sort/i.test(el.innerText || '')) {
        out.push({ tag: el.tagName, text: (el.innerText || '').trim().slice(0, 80), siblingHTML: el.parentElement?.outerHTML?.slice(0, 300) });
      }
    });
    return out.slice(0, 5);
  });
  console.log('Sort-labeled areas:', JSON.stringify(sortAreas, null, 2));

  await browser.close();
})();
