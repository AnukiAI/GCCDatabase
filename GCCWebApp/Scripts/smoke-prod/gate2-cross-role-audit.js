// Gate 2 (2026-07-20) — cross-role + responsive + accessibility audit of the uncommitted
// working-tree changes in GCCWebApp-deploy-20260720-1250.zip.
// REVIEW ONLY. Reads the app, writes screenshots + a JSON report under _results/gate2-20260720/.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const PWD = 'TestPass123!';
const OUT = path.join(__dirname, '_results', 'gate2-20260720');
fs.mkdirSync(OUT, { recursive: true });

const VIEWPORTS = [
  { name: 'desktop-1920', width: 1920, height: 1080 },
  { name: 'desktop-1440', width: 1440, height: 900 },
  { name: 'tablet-768', width: 768, height: 1024 },
  { name: 'mobile-390', width: 390, height: 844 },
  { name: 'mobile-360', width: 360, height: 640 },
];

const CERT_NUMBER = '26-1733';

const AXE_PATH = require.resolve('axe-core/axe.min.js');

const report = { pages: [], axe: [], externalChecks: [], errors: [] };

async function login(context, email) {
  const page = await context.newPage();
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.fill('#Email', email);
  await page.fill('#Password', PWD);
  await Promise.all([
    page.waitForLoadState('networkidle').catch(() => {}),
    page.click('button[type=submit]'),
  ]);
  await page.waitForTimeout(1000);
  const ok = !page.url().includes('/Account/Login');
  await page.close();
  return ok;
}

// Waits for REAL content: certificate cards/table rows OR an explicit empty-state message.
async function waitForRealContent(page, timeout = 20000) {
  try {
    await page.waitForFunction(() => {
      const cards = document.querySelectorAll('.certificate-card, .cert-tile, tr.certificate-row, tbody#gridBody tr');
      const empty = document.querySelector('.empty-state, #emptyState, [id*="empty" i]');
      const errorBox = document.querySelector('.loading-indicator, #loading-indicator');
      if (cards.length > 0) return true;
      if (empty && empty.offsetParent !== null && empty.textContent.trim().length > 0) return true;
      return false;
    }, { timeout });
  } catch (e) {
    return false; // caller records as a finding
  }
  return true;
}

async function shootAllViewports(context, url, slug, opts = {}) {
  const results = [];
  for (const vp of VIEWPORTS) {
    const page = await context.newPage();
    await page.setViewportSize({ width: vp.width, height: vp.height });
    let contentOk = true;
    let httpStatus = null;
    try {
      const resp = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
      httpStatus = resp ? resp.status() : null;
      if (opts.waitForContent) {
        contentOk = await waitForRealContent(page);
      } else {
        await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
      }
      await page.waitForTimeout(600);
      const shotPath = path.join(OUT, `${slug}__${vp.name}.png`);
      await page.screenshot({ path: shotPath, fullPage: true });
      results.push({ viewport: vp.name, status: httpStatus, contentOk, screenshot: shotPath, finalUrl: page.url() });
    } catch (e) {
      results.push({ viewport: vp.name, status: httpStatus, error: e.message, finalUrl: page.url() });
    } finally {
      await page.close();
    }
  }
  return results;
}

async function runAxe(context, url, slug, opts = {}) {
  const page = await context.newPage();
  await page.setViewportSize({ width: 1440, height: 900 });
  try {
    await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 });
    if (opts.waitForContent) await waitForRealContent(page);
    await page.waitForTimeout(600);
    if (opts.preAxe) await opts.preAxe(page);
    const axeSrc = fs.readFileSync(AXE_PATH, 'utf8');
    await page.addScriptTag({ content: axeSrc });
    const results = await page.evaluate(async () => {
      return await window.axe.run(document, {
        runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] },
      });
    });
    const violations = results.violations.map(v => ({
      id: v.id, impact: v.impact, help: v.help, helpUrl: v.helpUrl,
      nodes: v.nodes.map(n => ({ target: n.target, html: n.html.slice(0, 300), failureSummary: n.failureSummary })),
    }));
    fs.writeFileSync(path.join(OUT, `axe__${slug}.json`), JSON.stringify(violations, null, 2));
    return { slug, url, violationCount: violations.length, violations };
  } catch (e) {
    return { slug, url, error: e.message };
  } finally {
    await page.close();
  }
}

(async () => {
  const browser = await chromium.launch();

  // ---------- ADMIN ----------
  {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    const loggedIn = await login(ctx, 'admin');
    report.pages.push({ role: 'Admin', loggedIn });
    if (loggedIn) {
      report.pages.push({ role: 'Admin', page: 'CertificateTileViewInternal', results: await shootAllViewports(ctx, BASE + '/Certificate/CertificateTileViewInternal', 'admin__tileview-internal', { waitForContent: true }) });
      report.pages.push({ role: 'Admin', page: 'Edit', results: await shootAllViewports(ctx, BASE + '/Certificate/Edit/' + CERT_NUMBER, 'admin__edit', {}) });
      report.pages.push({ role: 'Admin', page: 'Create', results: await shootAllViewports(ctx, BASE + '/Certificate/Create', 'admin__create', {}) });
      report.pages.push({ role: 'Admin', page: 'CertificateSingleView', results: await shootAllViewports(ctx, BASE + '/Certificate/CertificateSingleView/' + CERT_NUMBER, 'admin__single-view', {}) });

      report.axe.push(await runAxe(ctx, BASE + '/Certificate/CertificateTileViewInternal', 'admin__tileview-internal', { waitForContent: true }));
      report.axe.push(await runAxe(ctx, BASE + '/Certificate/Edit/' + CERT_NUMBER, 'admin__edit'));
      report.axe.push(await runAxe(ctx, BASE + '/Certificate/Create', 'admin__create'));
      report.axe.push(await runAxe(ctx, BASE + '/Certificate/CertificateSingleView/' + CERT_NUMBER, 'admin__single-view'));
    }
    await ctx.close();
  }

  await browser.close();
  fs.writeFileSync(path.join(OUT, 'report-part1.json'), JSON.stringify(report, null, 2));
  console.log('DONE PART 1');
})();
