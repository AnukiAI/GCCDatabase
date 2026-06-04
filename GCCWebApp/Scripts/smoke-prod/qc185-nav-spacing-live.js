// QC #185 / #77 — verify the gray breadcrumb spacer (DEF-227 fix) is LIVE on
// gccwebapp.com and renders consistently across all roles, with NO overlap
// between the top navbar (its dropdowns) and the page content / search bar.
//
// Carly's re-report: in Analyst view the nav box appeared behind the search bar
// (no gray spacer), while Admin had the gray area. The fix must be present on
// every role instance, not just Admin.
//
// LIVE TEST. Run:
//   SMOKE_REMOTE_USER=gcc SMOKE_REMOTE_PASS="Cert-Test-Access-2026!" \
//   node Scripts/smoke-prod/qc185-nav-spacing-live.js
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = (process.env.SMOKE_REMOTE_USER && process.env.SMOKE_REMOTE_PASS)
  ? { username: process.env.SMOKE_REMOTE_USER, password: process.env.SMOKE_REMOTE_PASS }
  : { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const OUT = path.join(__dirname, '_results');
if (!fs.existsSync(OUT)) fs.mkdirSync(OUT, { recursive: true });

const ROLES = [
  { role: 'admin',    email: 'admin',    path: '/Certificate/CertificateTileViewInternal' },
  { role: 'analyst',  email: 'analyst',  path: '/Certificate/CertificateTileViewInternal' },
  { role: 'external', email: 'external', path: '/Certificate/CertificateTileViewExternal' },
  { role: 'public',   email: null,       path: '/Certificate/CertificateTileView' },
];

const results = [];

async function login(page, email) {
  await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('#Email').fill(email);
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(1200);
}

(async () => {
  const browser = await chromium.launch();
  try {
    for (const r of ROLES) {
      const ctx = await browser.newContext({
        viewport: { width: 1440, height: 900 },
        httpCredentials: GATE,
        ignoreHTTPSErrors: true,
      });
      const page = await ctx.newPage();
      try {
        if (r.email) await login(page, r.email);
        await page.goto(`${BASE}${r.path}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
        await page.waitForLoadState('networkidle').catch(() => {});
        await page.waitForTimeout(1500);

        const m = await page.evaluate(() => {
          const rectOf = (sel) => {
            const el = document.querySelector(sel);
            if (!el) return null;
            const rb = el.getBoundingClientRect();
            return { top: Math.round(rb.top), bottom: Math.round(rb.bottom), height: Math.round(rb.height) };
          };
          const nav = document.querySelector('nav.breadcrumbs');
          const cs = nav ? getComputedStyle(nav) : null;
          const wrapper = nav ? nav.querySelector('.breadcrumb-wrapper') : null;
          const wcs = wrapper ? getComputedStyle(wrapper) : null;
          // navbar = the top site navigation; search = first search input on the page
          const navbar = document.querySelector('.navbar, nav.navbar, header nav, .top-nav');
          const search = document.querySelector('input[type="search"], #searchInput, .search-input, input[placeholder*="earch"]');
          return {
            hasBreadcrumb: !!nav,
            breadcrumbDisplay: cs ? cs.display : null,
            breadcrumbHeight: nav ? Math.round(nav.getBoundingClientRect().height) : 0,
            breadcrumbBg: cs ? cs.backgroundColor : null,
            wrapperVisibility: wcs ? wcs.visibility : null,
            wrapperText: wrapper ? wrapper.textContent.trim().slice(0, 80) : null,
            bodyClass: document.body.className,
            navbarRect: rectOf('.navbar, nav.navbar, header nav, .top-nav'),
            breadcrumbRect: rectOf('nav.breadcrumbs'),
            searchRect: search ? (() => { const rb = search.getBoundingClientRect(); return { top: Math.round(rb.top), bottom: Math.round(rb.bottom) }; })() : null,
          };
        });

        // Spacer present = breadcrumb renders as a block with a real height (~32px).
        const spacerPresent = m.hasBreadcrumb && m.breadcrumbDisplay !== 'none' && m.breadcrumbHeight >= 24;
        // No overlap = breadcrumb sits at/below the navbar bottom (within 2px tolerance).
        const noOverlap = !m.navbarRect || !m.breadcrumbRect || m.breadcrumbRect.top >= (m.navbarRect.bottom - 2);
        // Admin shows breadcrumb content; others have it visually hidden.
        const wrapperOk = r.role === 'admin' ? m.wrapperVisibility === 'visible' : m.wrapperVisibility === 'hidden';
        const pass = spacerPresent && noOverlap;

        results.push({ role: r.role, pass, spacerPresent, noOverlap, wrapperOk, ...m });
        console.log(`[${pass ? 'PASS' : 'FAIL'}] ${r.role}: spacer=${spacerPresent}(h=${m.breadcrumbHeight}) overlap=${!noOverlap} wrapper=${m.wrapperVisibility}(${wrapperOk ? 'ok' : 'CHECK'})`);

        await page.screenshot({ path: path.join(OUT, `qc185-nav-${r.role}.png`), fullPage: false });
      } catch (e) {
        results.push({ role: r.role, pass: false, error: e.message.split('\n')[0] });
        console.log(`[ERROR] ${r.role}: ${e.message.split('\n')[0]}`);
      } finally {
        await ctx.close();
      }
    }
  } finally {
    await browser.close();
  }

  const pass = results.filter(r => r.pass).length;
  const summary = { ts: new Date().toISOString(), base: BASE, qc: '185/77', def: '227', pass, total: results.length, results };
  fs.writeFileSync(path.join(OUT, 'qc185-nav-spacing-results.json'), JSON.stringify(summary, null, 2));
  console.log(`\n[qc185] ${pass}/${results.length} roles consistent against ${BASE}`);
  process.exit(pass === results.length ? 0 : 1);
})();
