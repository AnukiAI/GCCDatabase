// Gate 2 part 3 — DEF-310 detail views not yet covered: CertificateInternalView (Admin/Analyst),
// CertificateExternalView (External). Screenshots + axe.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const BASE = 'http://localhost:5101';
const PWD = 'TestPass123!';
const OUT = path.join(__dirname, '_results', 'gate2-20260720');
const CERT_NUMBER = '26-1733';
const AXE_PATH = require.resolve('axe-core/axe.min.js');

async function login(page, email) {
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.fill('#Email', email);
  await page.fill('#Password', PWD);
  await Promise.all([page.waitForLoadState('networkidle').catch(() => {}), page.click('button[type=submit]')]);
  await page.waitForTimeout(1000);
}

async function checkPage(browser, email, url, slug) {
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  await login(page, email);
  const resp = await page.goto(url, { waitUntil: 'networkidle', timeout: 30000 }).catch(e => null);
  await page.waitForTimeout(800);
  await page.screenshot({ path: path.join(OUT, `${slug}.png`), fullPage: true });

  const axeSrc = fs.readFileSync(AXE_PATH, 'utf8');
  await page.addScriptTag({ content: axeSrc });
  const results = await page.evaluate(async () => {
    return await window.axe.run(document, { runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] } });
  });
  const violations = results.violations.map(v => ({ id: v.id, impact: v.impact, help: v.help, count: v.nodes.length }));
  fs.writeFileSync(path.join(OUT, `axe__${slug}.json`), JSON.stringify(results.violations, null, 2));
  console.log(slug, 'status:', resp ? resp.status() : null, 'url:', page.url(), 'violations:', JSON.stringify(violations));
  await ctx.close();
}

(async () => {
  const browser = await chromium.launch();
  await checkPage(browser, 'admin', `${BASE}/Certificate/CertificateInternalView/${CERT_NUMBER}`, 'admin__internal-view');
  await checkPage(browser, 'external', `${BASE}/Certificate/CertificateExternalView/${CERT_NUMBER}`, 'external__external-view-detail');
  await browser.close();
})();
