'use strict';
const { chromium } = require('playwright');

// Verify Edit route access with real certificate number
const CERT_NUM = '26-01730'; // from probe-edit4 — first live cert
const BASE = 'http://localhost:5101';

async function loginAs(ctx, user, pass) {
  const page = await ctx.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.locator('#Email').fill(user);
  await page.locator('#Password').fill(pass);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);
  return page;
}

(async () => {
  const b = await chromium.launch({ headless: true });

  const checks = [
    ['admin',    'admin',    'TestPass123!', 200],
    ['analyst',  'analyst',  'TestPass123!', 200],
    ['external', 'external', 'TestPass123!', 401], // should redirect to AccessDenied
    ['public',   null,       null,           401], // should redirect to Login
  ];

  for (const [label, user, pass, expectedStatus] of checks) {
    const ctx  = await b.newContext({ viewport: { width: 1440, height: 900 } });
    let page;
    if (user) {
      page = await loginAs(ctx, user, pass);
    } else {
      page = await ctx.newPage();
    }
    const resp = await page.goto(BASE + '/Certificate/Edit/' + CERT_NUM, {
      waitUntil: 'domcontentloaded', timeout: 30000
    });
    const finalUrl = page.url().replace(BASE, '');
    const isAuthRedirect = /\/Account\/(Login|AccessDenied)/i.test(finalUrl);
    const actual = isAuthRedirect ? 401 : resp.status();
    const pass2 = actual === expectedStatus ||
      (expectedStatus === 401 && actual === 403) ||
      (expectedStatus === 403 && actual === 401);
    console.log(`[${pass2 ? 'PASS' : 'FAIL'}] cert-edit / ${label}: expected=${expectedStatus} actual=${actual} | url=${finalUrl}`);
    await ctx.close();
  }

  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
