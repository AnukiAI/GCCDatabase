// E2E Smoke Template — shared infra for per-view smokes.
//
// Day 1 of the E2E Smoke Plan (docs/tracking/e2e-smoke-plan.md).
//
// Each per-view smoke (e.g. Scripts/smoke-prod/_views/home.js) calls runViewSmoke(...)
// with a viewId, URL, role list, viewport list, expectedAccess map, and an assertions
// callback. The template handles login, viewport setup, console + network error capture,
// screenshots, and JSON result aggregation.
//
// Usage:
//   const { runViewSmoke } = require('../_lib/smoke-template');
//   runViewSmoke({
//     viewId: 'home',
//     url: '/Home/Index',
//     roles: ['public', 'external', 'analyst', 'admin'],
//     viewports: ['desktop', 'tablet', 'mobile'],
//     expectedAccess: { public: 200, external: 200, analyst: 200, admin: 200 },
//     assertions: async ({ page, role, viewport, record }) => {
//       record('home.logo', 'Logo renders', (await page.locator('.logo, .nav-logo').count()) > 0 ? 'PASS' : 'FAIL');
//     },
//   });
//
// LOCALHOST ONLY by default — refuses to run against gccwebapp.com / greencirclecertified.com
// unless SMOKE_ALLOW_REMOTE=1 is set explicitly.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const SCREENSHOTS = process.env.SMOKE_SCREENSHOTS || 'on-fail'; // 'on-fail' | 'always' | 'never'
const HEADLESS = process.env.SMOKE_HEADLESS !== '0';
const ALLOW_REMOTE = process.env.SMOKE_ALLOW_REMOTE === '1';
// Navigation timeout. The local dev server can stall ~20-25s on a cold first hit
// (heavy stats pages like /Dashboard/Index plus a per-minute background sweep), which
// blew past Playwright's 30s default and produced spurious page.goto timeouts. 60s
// absorbs the cold start; override with SMOKE_NAV_TIMEOUT for slower/remote targets.
const NAV_TIMEOUT = parseInt(process.env.SMOKE_NAV_TIMEOUT || '60000', 10);

const RESULTS_DIR = path.join(__dirname, '..', '_results');
const SCREENSHOTS_DIR = path.join(__dirname, '..', '_results', 'screenshots');

const VIEWPORTS = {
  desktop: { width: 1440, height: 900 },
  tablet:  { width: 768,  height: 1024 },
  mobile:  { width: 375,  height: 667 },
};

// Test accounts per CLAUDE.md "Quick Credentials" section.
const ROLE_CREDS = {
  public:   null,
  external: { email: 'external', password: 'TestPass123!' },
  analyst:  { email: 'analyst',  password: 'TestPass123!' },
  admin:    { email: 'admin',    password: 'TestPass123!' },
};

// Console error patterns that are NOT smoke failures (known false positives).
const IGNORED_CONSOLE_PATTERNS = [
  // pageUnloading false-positive — when a fetch is aborted by navigation, the catch
  // block in tile-view scripts (per 5/2026 fix) suppresses these. Belt-and-suspenders.
  /Failed to fetch.*aborted/i,
  // ResizeObserver loop limit exceeded — Chromium noise, not a real error.
  /ResizeObserver loop/i,
  // Favicon 404 — irrelevant.
  /favicon\.ico.*404/i,
];

function isIgnoredConsoleMessage(text) {
  return IGNORED_CONSOLE_PATTERNS.some((rx) => rx.test(text));
}

async function ensureDirs() {
  for (const d of [RESULTS_DIR, SCREENSHOTS_DIR]) {
    if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true });
  }
}

async function login(page, role) {
  const creds = ROLE_CREDS[role];
  if (!creds) return; // public — no login
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
  await page.locator('#Email').fill(creds.email);
  await page.locator('#Password').fill(creds.password);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  // Best-effort wait — some logins do a redirect chain (2FA prompt for fresh devices etc.).
  await page.waitForTimeout(600);
}

async function captureScreenshot(page, viewId, role, viewport, label) {
  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const fname = `${viewId}-${role}-${viewport}-${label}-${stamp}.png`;
  const p = path.join(SCREENSHOTS_DIR, fname);
  try {
    await page.screenshot({ path: p, fullPage: true });
    return path.relative(path.join(__dirname, '..'), p);
  } catch {
    return null;
  }
}

async function runViewSmoke(opts) {
  const {
    viewId,
    url,
    roles = ['public', 'external', 'analyst', 'admin'],
    viewports = ['desktop', 'tablet', 'mobile'],
    expectedAccess = {},
    assertions,
    screenshotOn = SCREENSHOTS,
  } = opts;

  if ((BASE.includes('gccwebapp') || BASE.includes('greencirclecertified')) && !ALLOW_REMOTE) {
    console.error(`REFUSING TO RUN against ${BASE} — set SMOKE_ALLOW_REMOTE=1 to override.`);
    process.exit(2);
  }

  await ensureDirs();

  const startedAt = new Date().toISOString();
  console.log(`\n[smoke] ${viewId} (${url}) — ${BASE}`);
  console.log(`[smoke] roles=${roles.join(',')} viewports=${viewports.join(',')}\n`);

  const all = [];
  const browser = await chromium.launch({ headless: HEADLESS });

  for (const role of roles) {
    for (const vpName of viewports) {
      const vp = VIEWPORTS[vpName];
      if (!vp) {
        console.error(`Unknown viewport: ${vpName}`);
        continue;
      }
      const ctx = await browser.newContext({ viewport: vp });
      const page = await ctx.newPage();

      // Console + page error capture.
      const consoleErrors = [];
      const pageErrors = [];
      const networkErrors = [];
      page.on('console', (msg) => {
        if (msg.type() === 'error' && !isIgnoredConsoleMessage(msg.text())) {
          consoleErrors.push(msg.text());
        }
      });
      page.on('pageerror', (err) => pageErrors.push(err.message));
      page.on('response', (resp) => {
        const u = resp.url();
        const status = resp.status();
        // Skip third-party / data: / chrome-extension noise.
        if (!u.startsWith(BASE)) return;
        // 401 on protected resources is expected behavior, not a failure — the
        // expectedAccess gate covers those at the page level.
        if (status >= 400 && status !== 401 && status !== 403 && status !== 404) {
          networkErrors.push(`${status} ${u}`);
        }
      });

      const records = [];
      const record = (id, name, status, notes = '') => {
        records.push({ id, name, status, notes });
        const tag = status === 'PASS' ? 'PASS' : status === 'FAIL' ? 'FAIL' : 'WARN';
        console.log(`  [${tag}] ${role}/${vpName} ${id} ${name}${notes ? ' - ' + notes : ''}`);
      };

      let httpStatus = null;
      try {
        // Login first (no-op for public).
        await login(page, role);

        // Navigate to the view under test.
        const resp = await page.goto(BASE + url, { waitUntil: 'domcontentloaded', timeout: NAV_TIMEOUT });
        httpStatus = resp ? resp.status() : null;

        // expectedAccess gate.
        const expected = expectedAccess[role];
        if (typeof expected === 'number') {
          // Login redirect — Account/Login or Account/AccessDenied — counts as the
          // server's "you don't have access" response. Treat the *final* status code
          // as authoritative, but flag redirects to login as 401-equivalent for the
          // expectedAccess check.
          const finalUrl = page.url();
          // The "landed on login/AccessDenied => denied (401-equiv)" heuristic must NOT
          // fire when the view under test IS itself an auth page (e.g. viewId 'login',
          // url '/Account/Login') — otherwise a legitimate 200 is misread as a denial.
          const requestedIsAuthPage = /\/Account\/(Login|AccessDenied|AccountLocked|SessionExpired|ForgotPassword|ResetPassword)/i.test(url);
          const wasRedirectedToLogin =
            !requestedIsAuthPage && /\/Account\/(Login|AccessDenied)/i.test(finalUrl);
          const effectiveStatus = wasRedirectedToLogin ? 401 : httpStatus;
          const accessPass = effectiveStatus === expected ||
            // Allow 403 where 401 was specified (and vice versa) — both mean "denied".
            (expected === 401 && effectiveStatus === 403) ||
            (expected === 403 && effectiveStatus === 401);
          record(
            `${viewId}.access`,
            `Access check (expected ${expected}, got ${effectiveStatus})`,
            accessPass ? 'PASS' : 'FAIL',
            wasRedirectedToLogin ? `redirected to ${finalUrl.replace(BASE, '')}` : ''
          );
          // If access was denied and that was expected, skip body assertions.
          if (accessPass && (expected === 401 || expected === 403)) {
            await ctx.close();
            for (const r of records) all.push({ role, viewport: vpName, ...r });
            continue;
          }
          // If access was denied but we expected success, skip body assertions too —
          // the access FAIL is the headline finding.
          if (!accessPass) {
            await ctx.close();
            for (const r of records) all.push({ role, viewport: vpName, ...r });
            continue;
          }
        }

        // Mobile horizontal-scroll guard.
        if (vpName === 'mobile') {
          const overflow = await page.evaluate(() => ({
            docW: document.documentElement.scrollWidth,
            winW: window.innerWidth,
          }));
          record(
            `${viewId}.no-mobile-overflow`,
            'No horizontal scroll on mobile',
            overflow.docW <= overflow.winW + 1 ? 'PASS' : 'FAIL',
            `docW=${overflow.docW} winW=${overflow.winW}`
          );
        }

        // View-specific assertions.
        if (typeof assertions === 'function') {
          await assertions({ page, role, viewport: vpName, record, BASE });
        }

        // Console / page error rollup (last so it captures everything assertions triggered).
        if (consoleErrors.length === 0) {
          record(`${viewId}.no-console-errors`, 'No console errors', 'PASS');
        } else {
          record(
            `${viewId}.no-console-errors`,
            'No console errors',
            'FAIL',
            consoleErrors.slice(0, 3).join(' | ')
          );
        }
        if (pageErrors.length === 0) {
          record(`${viewId}.no-page-errors`, 'No uncaught page errors', 'PASS');
        } else {
          record(
            `${viewId}.no-page-errors`,
            'No uncaught page errors',
            'FAIL',
            pageErrors.slice(0, 3).join(' | ')
          );
        }
        if (networkErrors.length === 0) {
          record(`${viewId}.no-network-errors`, 'No 5xx / unexpected 4xx', 'PASS');
        } else {
          record(
            `${viewId}.no-network-errors`,
            'No 5xx / unexpected 4xx',
            'FAIL',
            networkErrors.slice(0, 3).join(' | ')
          );
        }
      } catch (e) {
        record(`${viewId}.fatal`, 'Smoke ran without throwing', 'FAIL', e.message);
      }

      // Screenshot policy.
      const anyFail = records.some((r) => r.status === 'FAIL');
      if (screenshotOn === 'always' || (screenshotOn === 'on-fail' && anyFail)) {
        const shot = await captureScreenshot(page, viewId, role, vpName, anyFail ? 'fail' : 'ok');
        if (shot) {
          for (const r of records) r.screenshot = shot;
        }
      }

      for (const r of records) all.push({ role, viewport: vpName, ...r });
      await ctx.close();
    }
  }

  await browser.close();

  // Summarize.
  const pass = all.filter((r) => r.status === 'PASS').length;
  const fail = all.filter((r) => r.status === 'FAIL').length;
  const warn = all.filter((r) => r.status === 'WARN').length;

  console.log(`\n[smoke] ${viewId}: ${pass} PASS / ${fail} FAIL / ${warn} WARN (${all.length} total)\n`);

  const stamp = startedAt.replace(/[:.]/g, '-');
  const outFile = path.join(RESULTS_DIR, `${viewId}-${stamp}.json`);
  fs.writeFileSync(
    outFile,
    JSON.stringify(
      { viewId, url, base: BASE, startedAt, finishedAt: new Date().toISOString(), pass, fail, warn, results: all },
      null,
      2
    )
  );
  console.log(`[smoke] results -> ${path.relative(process.cwd(), outFile)}`);

  // Caller may need exit code.
  return { viewId, pass, fail, warn, all, outFile };
}

module.exports = { runViewSmoke, VIEWPORTS, ROLE_CREDS, BASE };
