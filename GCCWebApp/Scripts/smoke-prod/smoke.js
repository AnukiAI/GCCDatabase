// Post-deploy smoke runner for gccwebapp.com.
//
// Goal: 60 seconds of operational confidence after every Plesk deploy.
// Catches the class of failure DEF-195's first deploy hit — files uploaded
// but never replaced on the server, so the site loaded fine but the fix
// wasn't actually live. Asserts file *content* (hashes, computed CSS) and
// *freshness* (Last-Modified), not just "page loads."
//
// Usage:
//   npm install
//   node smoke.js
//   node smoke.js --base https://gccwebapp.com    # explicit base
//   VERBOSE=1 node smoke.js                       # show per-check details
//
// Defaults to gccwebapp.com (prelaunch test env). When greencirclecertified.com
// goes live, pass --base or set BASE_URL env var.
//
// Sections:
//   [1] Gate behavior — Basic Auth challenge + bypass for static/api routes
//   [2] UI surfaces  — pages render behind the gate, cards visible, no 500s
//   [3] DEF-195      — multi-viewport visual: cert-id pill flush with first line
//                      of multi-line categories (regression: client QC #174)
//   [4] CSS sanity   — fix is in deployed file, Last-Modified is recent
//                      (catches "deploy didn't carry" failures)
//   [5] Role-gated   — admin/internal/external/create routes return non-500
//                      (302 to login expected for unauth requests)
//
// Exit codes:  0 = all pass, 1 = at least one fail, 2 = runner errored.

const { chromium, request } = require('playwright');
const fs = require('fs');
const path = require('path');

// ---- Config ---------------------------------------------------------------

function parseArgs() {
  const args = process.argv.slice(2);
  const cfg = {
    base: process.env.BASE_URL || 'https://gccwebapp.com',
    gateUser: process.env.GATE_USER || 'gcc',
    gatePass: process.env.GATE_PASS || 'Cert-Test-Access-2026!',
    screenshotDir: process.env.SCREENSHOT_DIR || path.join(__dirname, 'screenshots'),
    verbose: !!process.env.VERBOSE
  };
  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--base' && args[i + 1]) cfg.base = args[++i];
    if (args[i] === '--user' && args[i + 1]) cfg.gateUser = args[++i];
    if (args[i] === '--pass' && args[i + 1]) cfg.gatePass = args[++i];
    if (args[i] === '--verbose' || args[i] === '-v') cfg.verbose = true;
  }
  cfg.base = cfg.base.replace(/\/$/, '');
  return cfg;
}

const CFG = parseArgs();
fs.mkdirSync(CFG.screenshotDir, { recursive: true });

// ---- Result tracking ------------------------------------------------------

const results = [];
function record(section, name, pass, detail) {
  results.push({ section, name, pass, detail });
  const tag = pass ? '\x1b[32mPASS\x1b[0m' : '\x1b[31mFAIL\x1b[0m';
  console.log(`  ${tag}  ${name}${detail && (CFG.verbose || !pass) ? `  \x1b[90m${detail}\x1b[0m` : ''}`);
}
function section(n, title) {
  console.log(`\n[${n}] ${title}`);
}

// ---- Helpers --------------------------------------------------------------

const VIEWPORTS = {
  desktop: { width: 1440, height: 900 },
  tablet:  { width: 1024, height: 768 },
  mobile:  { width: 390,  height: 844 }
};

function gateCreds() {
  return { username: CFG.gateUser, password: CFG.gatePass };
}

async function withPage(viewport, fn) {
  const browser = await chromium.launch();
  try {
    const ctx = await browser.newContext({ viewport, httpCredentials: gateCreds() });
    const page = await ctx.newPage();
    return await fn(page, ctx);
  } finally {
    await browser.close();
  }
}

// ---- Main -----------------------------------------------------------------

(async () => {
  console.log(`\nPost-deploy smoke against ${CFG.base}`);
  console.log('='.repeat(72));

  // ===== [1] GATE BEHAVIOR (anonymous) =====================================
  section(1, 'Gate behavior — anonymous requests');
  const anon = await request.newContext();

  let r = await anon.get(`${CFG.base}/robots.txt`);
  const robotsText = await r.text();
  record(1, 'robots.txt bypasses gate + serves Disallow',
    r.status() === 200 && /Disallow:\s*\//i.test(robotsText),
    `status=${r.status()}, body="${robotsText.slice(0, 60).replace(/\s+/g, ' ')}"`);

  r = await anon.get(`${CFG.base}/favicon.ico`);
  const favAuth = r.headers()['www-authenticate'] || '';
  record(1, 'favicon.ico bypasses gate (no Basic challenge regardless of file existence)',
    r.status() !== 401 || !/basic/i.test(favAuth),
    `status=${r.status()}, www-authenticate="${favAuth || 'none'}"`);

  r = await anon.get(`${CFG.base}/`);
  const rootAuth = r.headers()['www-authenticate'] || '';
  record(1, 'Root requires Basic Auth challenge',
    r.status() === 401 && /basic/i.test(rootAuth),
    `status=${r.status()}, www-authenticate="${rootAuth}"`);

  r = await anon.get(`${CFG.base}/api/v1/certificates`);
  const apiAuth = r.headers()['www-authenticate'] || '';
  record(1, 'Partner API bypasses Basic gate, requires X-API-Key',
    r.status() === 401 && !/basic/i.test(apiAuth),
    `status=${r.status()}, www-authenticate="${apiAuth || 'none'}"`);

  r = await anon.get(`${CFG.base}/api/v1/certificates`, {
    headers: { 'X-API-Key': 'gcc_partner_bogus_smoke_test_key_should_be_rejected' }
  });
  record(1, 'Partner API rejects bogus key with 401', r.status() === 401, `status=${r.status()}`);

  await anon.dispose();

  // ===== [2] UI SURFACES (behind gate) =====================================
  section(2, 'UI surfaces — behind Basic Auth gate');
  let renderedCertNum = null;

  await withPage(VIEWPORTS.desktop, async (page, ctx) => {
    let resp = await page.goto(`${CFG.base}/`, { waitUntil: 'domcontentloaded' });
    record(2, 'Site root loads after Basic Auth',
      resp.status() < 400, `status=${resp.status()}, final=${page.url()}`);

    resp = await page.goto(`${CFG.base}/Account/Login`, { waitUntil: 'domcontentloaded' });
    const pwd = await page.locator('input[type=password]').count();
    record(2, 'Login page renders password field',
      resp.status() === 200 && pwd > 0, `status=${resp.status()}, password fields=${pwd}`);

    resp = await page.goto(`${CFG.base}/Certificate/CertificateTileView`,
      { waitUntil: 'domcontentloaded' });
    await page.waitForSelector('.certificate-card', { timeout: 20000 }).catch(() => {});
    const cardCount = await page.locator('.certificate-card').count();
    record(2, 'CertificateTileView renders cards',
      resp.status() === 200 && cardCount > 0, `status=${resp.status()}, cards=${cardCount}`);

    // Pick a cert number for the detail check
    renderedCertNum = await page.locator('.certificate-card .certificate-id')
      .first().textContent().catch(() => null);
    if (renderedCertNum) {
      const num = renderedCertNum.trim();
      resp = await page.goto(`${CFG.base}/Certificate/CertificateSingleView/${encodeURIComponent(num)}`,
        { waitUntil: 'domcontentloaded' });
      record(2, `Cert detail loads for ${num} (DEF-103 regression — public cert reachable)`,
        resp.status() === 200, `status=${resp.status()}`);
    } else {
      record(2, 'Cert detail loads', false, 'could not extract a cert number from rendered tiles');
    }
  });

  // ===== [3] DEF-195 VISUAL — MULTI-VIEWPORT ==============================
  section(3, 'DEF-195 — cert-id pill alignment on multi-line categories');
  for (const [name, vp] of Object.entries(VIEWPORTS)) {
    await withPage(vp, async (page) => {
      await page.goto(`${CFG.base}/Certificate/CertificateTileView`,
        { waitUntil: 'domcontentloaded' });
      await page.waitForSelector('.certificate-card', { timeout: 20000 }).catch(() => {});

      const stats = await page.evaluate(() => {
        const rows = Array.from(document.querySelectorAll('.certificate-card .header-row'));
        const aligns = rows.map(r => getComputedStyle(r).alignItems);
        // Detect column-mode (mobile) vs row-mode (desktop/tablet).
        // The mobile media query at main.css:3693 stacks the header-row
        // vertically, so the cert-id sits BELOW the category by design —
        // "drift" is intentional and the assertion doesn't apply.
        const firstRow = rows[0];
        const flexDirection = firstRow ? getComputedStyle(firstRow).flexDirection : 'row';
        const isColumnMode = flexDirection.startsWith('column');
        const wrapped = [];
        rows.forEach(row => {
          const cat = row.querySelector('.certificate-category');
          const id = row.querySelector('.certificate-id');
          if (!cat || !id) return;
          const lh = parseFloat(getComputedStyle(cat).lineHeight) || 18;
          const lines = Math.round(cat.getBoundingClientRect().height / lh);
          if (lines >= 2) {
            wrapped.push({
              lines,
              drift: Math.round(id.getBoundingClientRect().top - cat.getBoundingClientRect().top)
            });
          }
        });
        return { count: rows.length, allFlexStart: aligns.every(a => a === 'flex-start'),
                 sample: aligns[0], wrapped, isColumnMode, flexDirection };
      });

      record(3, `[${name} ${vp.width}px] .header-row computed align-items: flex-start`,
        stats.allFlexStart, `n=${stats.count}, sample=${stats.sample}, dir=${stats.flexDirection}`);

      if (stats.isColumnMode) {
        // Column mode means stacked-by-design — drift assertion not applicable.
        // Just record that we detected the responsive switch so failures get
        // attributed correctly when the layout changes.
        record(3, `[${name} ${vp.width}px] header-row is stacked (mobile responsive layout, drift not applicable)`,
          true, `flex-direction=${stats.flexDirection}, ${stats.wrapped.length} wrapped tiles`);
      } else if (stats.wrapped.length > 0) {
        const maxDrift = Math.max(...stats.wrapped.map(w => w.drift));
        record(3, `[${name} ${vp.width}px] multi-line categories — pill drift ≤ 1px`,
          maxDrift <= 1, `${stats.wrapped.length} wrapped tiles, max drift=${maxDrift}px`);
      } else if (CFG.verbose) {
        console.log(`     (no multi-line categories rendered at ${vp.width}px)`);
      }

      const shotFile = path.join(CFG.screenshotDir,
        `tileview_${name}_${vp.width}.png`);
      await page.screenshot({ path: shotFile, fullPage: true });
    });
  }

  // ===== [4] CSS SANITY — content + freshness ==============================
  section(4, 'CSS sanity — deployed content + freshness');
  const ctxForCss = await request.newContext({ httpCredentials: gateCreds() });

  const cssChecks = [
    {
      url: '/css/main.css',
      // Match the desktop block specifically (the one that wasn't in a media query).
      // body.theme-option4 .header-row { ... align-items: flex-start ... position: relative ... }
      pattern: /body\.theme-option4\s+\.header-row\s*\{[^}]*align-items:\s*flex-start[^}]*position:\s*relative/m,
      label: 'main.css contains DEF-195 desktop theme-option4 fix'
    },
    {
      url: '/css/certificatetileview.css',
      pattern: /\.certificate-card\s+\.header-row\s*\{[^}]*align-items:\s*flex-start/m,
      label: 'certificatetileview.css contains .certificate-card .header-row flex-start'
    }
  ];

  for (const check of cssChecks) {
    const resp = await ctxForCss.get(`${CFG.base}${check.url}`);
    const body = await resp.text();
    record(4, check.label, check.pattern.test(body), `size=${body.length}B`);

    // Freshness check — Last-Modified must be within 14 days of now.
    // Catches the "deploy didn't carry" class of failure that bit us on
    // DEF-195's first attempt. If you genuinely haven't touched a CSS file
    // in over 2 weeks this will false-alarm; bump the window if it becomes
    // noisy on real long-stable files.
    const lm = resp.headers()['last-modified'];
    if (lm) {
      const lmDate = new Date(lm);
      const ageDays = (Date.now() - lmDate.getTime()) / (1000 * 60 * 60 * 24);
      record(4, `${check.url} Last-Modified within last 14 days (catches "deploy didn't carry")`,
        ageDays <= 14, `Last-Modified=${lm}, age=${ageDays.toFixed(1)}d`);
    } else {
      record(4, `${check.url} Last-Modified header present`, false, 'header missing');
    }
  }

  await ctxForCss.dispose();

  // ===== [5] ROLE-GATED PAGES — no 500s ====================================
  section(5, 'Role-gated pages reachable (302 to login expected, 500 = bad)');
  const ctxRoles = await request.newContext({ httpCredentials: gateCreds() });

  const protectedPaths = [
    '/Admin/UserManagement',
    '/Admin/ProductTypes',          // DEF-196 area
    '/Admin/Companies',
    '/Admin/PartnerApiKeys',
    '/Certificate/CertificateTileViewInternal',
    '/Certificate/CertificateTileViewExternal',
    '/Certificate/Create'
  ];

  for (const p of protectedPaths) {
    const resp = await ctxRoles.get(`${CFG.base}${p}`, { maxRedirects: 0, failOnStatusCode: false });
    record(5, `${p} (no server error)`, resp.status() < 500, `status=${resp.status()}`);
  }

  await ctxRoles.dispose();

  // ===== SUMMARY ===========================================================
  console.log('\n' + '='.repeat(72));
  const passed = results.filter(r => r.pass).length;
  const failed = results.filter(r => !r.pass).length;
  const color = failed === 0 ? '\x1b[32m' : '\x1b[31m';
  console.log(`${color}SUMMARY: ${passed}/${results.length} passed, ${failed} failed\x1b[0m`);
  console.log(`Screenshots: ${CFG.screenshotDir}`);

  if (failed > 0) {
    console.log('\nFailures:');
    results.filter(r => !r.pass).forEach(r =>
      console.log(`  [${r.section}] ${r.name}  →  ${r.detail || '(no detail)'}`));
    process.exit(1);
  }
  process.exit(0);
})().catch(e => {
  console.error('\n\x1b[31mSmoke runner errored:\x1b[0m', e);
  process.exit(2);
});
