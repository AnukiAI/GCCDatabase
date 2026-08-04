// DEF-319 / QC #88 (QC sheet row "Cert search view") — hover-then-Enter on the cert search box.
//
// Client report (7/30): "It is still randomly choosing an option after typing and entering as
// soon as the mouse hovers over something in dropdown. If you just type word and hit enter right
// away without exploring dropdown items seems to be ok. But not when your mouse goes over options."
//
// Cause: each .autocomplete-item had a mouseenter handler doing classList.add('selected'), and
// 'selected' is the exact class the Enter branch of the keydown handler reads. Hovering silently
// armed a selection; Enter then committed the hovered suggestion instead of the typed text.
//
// This script reproduces the gesture with a REAL mouse move (not a synthetic event), so it
// exercises the same path the client does:
//   1. type a term into .search-input
//   2. wait for .autocomplete-item suggestions
//   3. move the physical mouse over suggestion #0
//   4. check whether anything picked up the .selected class   <-- the smoking gun
//   5. press Enter
//   6. read back the committed search term
//
// PASS  = Enter commits the TYPED text.
// FAIL  = Enter commits the HOVERED suggestion (the client's bug).
//
// Usage:
//   node def319-hover-enter-verify.js                 # both targets
//   node def319-hover-enter-verify.js local           # localhost only
//   node def319-hover-enter-verify.js test            # gccwebapp.com only

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `def319-hover-enter-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

const GATE_USER = 'gcc';
const GATE_PASS = 'Cert-Test-Access-2026!';

const TARGETS = [
  { key: 'local', label: 'LOCAL DEV (working tree, 7/31 fixes applied)', base: 'http://localhost:5101', gated: false },
  { key: 'test',  label: 'TEST ENV (gccwebapp.com, deployed build)',     base: 'https://gccwebapp.com', gated: true },
];

// Terms to try until the dropdown actually returns suggestions.
const TERMS = ['test', 'cert', 'green', 'a'];

const results = [];

async function passGate(page, base) {
  // Cookie-mode gate redirects everything to /__gate. httpCredentials covers the Basic path;
  // if we still land on the gate page, fill its form.
  if (!page.url().includes('__gate')) return true;
  const pw = page.locator('input[type="password"]').first();
  if (!(await pw.count())) return false;
  await pw.fill(GATE_PASS);
  await Promise.all([
    page.waitForLoadState('networkidle').catch(() => {}),
    page.locator('button[type="submit"], input[type="submit"]').first().click(),
  ]);
  return !page.url().includes('__gate');
}

// Does the JS the SERVER actually serves still contain the mouseenter handler?
async function inspectServedJs(ctx, base) {
  const files = ['certificate-tile-view.js', 'certificate-tile-view-internal.js', 'certificate-tile-view-external.js'];
  const out = {};
  for (const f of files) {
    try {
      const res = await ctx.request.get(`${base}/js/${f}`);
      const body = await res.text();
      const isJs = (res.headers()['content-type'] || '').includes('javascript');
      // Count only real handler registrations, not the DEF-319 comment that replaced them.
      const handlers = (body.match(/addEventListener\(\s*['"]mouseenter['"]/g) || []).length;
      out[f] = {
        status: res.status(),
        bytes: body.length,
        looksLikeJs: isJs,
        mouseenterHandlers: handlers,
        hasDef319Comment: body.includes('DEF-319'),
      };
    } catch (e) {
      out[f] = { error: e.message };
    }
  }
  return out;
}

async function runTarget(browser, target) {
  const r = { target: target.key, label: target.label, base: target.base };
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    httpCredentials: target.gated ? { username: GATE_USER, password: GATE_PASS } : undefined,
  });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log('    pageerror:', e.message));

  try {
    console.log(`\n=== ${target.label} ===`);
    console.log(`    ${target.base}`);

    await page.goto(`${target.base}/Certificate/CertificateTileView`, { waitUntil: 'domcontentloaded' });
    const throughGate = await passGate(page, target.base);
    if (!throughGate) {
      r.error = 'blocked at /__gate — could not authenticate';
      console.log('    BLOCKED at gate');
      return r;
    }
    await page.waitForLoadState('networkidle').catch(() => {});

    r.servedJs = await inspectServedJs(ctx, target.base);
    console.log('    served JS:');
    for (const [f, v] of Object.entries(r.servedJs)) {
      console.log(`      ${f}: HTTP ${v.status} ${v.bytes}b js=${v.looksLikeJs} mouseenterHandlers=${v.mouseenterHandlers} DEF-319comment=${v.hasDef319Comment}`);
    }

    const input = page.locator('.search-input').first();
    if (!(await input.count())) {
      r.error = 'no .search-input on the tile view';
      console.log('    NO SEARCH INPUT FOUND');
      return r;
    }

    // Find a term that actually produces suggestions.
    let term = null, items = [];
    for (const t of TERMS) {
      await input.click();
      await input.fill('');
      await input.type(t, { delay: 60 });
      await page.waitForTimeout(900);
      items = await page.$$eval('.autocomplete-dropdown .autocomplete-item',
        els => els.map(e => ({ value: e.getAttribute('data-value'), text: e.textContent.trim() }))).catch(() => []);
      const visible = await page.locator('.autocomplete-dropdown').first().isVisible().catch(() => false);
      if (visible && items.length) { term = t; break; }
    }

    if (!term) {
      r.error = 'no term produced autocomplete suggestions — cannot exercise the hover path';
      console.log('    NO SUGGESTIONS for any of:', TERMS.join(', '));
      await page.screenshot({ path: path.join(OUT, `${target.key}-no-suggestions.png`), fullPage: true });
      return r;
    }

    r.term = term;
    r.suggestions = items;
    console.log(`    typed "${term}" -> ${items.length} suggestions; first = ${JSON.stringify(items[0])}`);
    await page.screenshot({ path: path.join(OUT, `${target.key}-1-typed.png`) });

    // --- the gesture: real mouse move over suggestion #0 ---
    const first = page.locator('.autocomplete-dropdown .autocomplete-item').first();
    const box = await first.boundingBox();
    if (!box) { r.error = 'suggestion had no bounding box'; return r; }
    await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2, { steps: 12 });
    await page.waitForTimeout(400);

    // Smoking gun: did hovering arm a selection?
    r.selectedAfterHover = await page.$$eval('.autocomplete-dropdown .autocomplete-item.selected',
      els => els.map(e => e.getAttribute('data-value'))).catch(() => []);
    r.hoveredValue = items[0].value;
    console.log(`    after hover: .selected = ${JSON.stringify(r.selectedAfterHover)} (hovered "${r.hoveredValue}")`);
    await page.screenshot({ path: path.join(OUT, `${target.key}-2-hovered.png`) });

    // --- Enter ---
    await page.keyboard.press('Enter');
    await page.waitForTimeout(1200);

    r.inputAfterEnter = await input.inputValue();
    r.urlAfterEnter = page.url();
    const u = new URL(r.urlAfterEnter);
    r.searchParam = u.searchParams.get('search');
    await page.screenshot({ path: path.join(OUT, `${target.key}-3-after-enter.png`), fullPage: true });

    const committed = r.inputAfterEnter;
    r.committedTyped = committed === term;
    r.committedHovered = committed === r.hoveredValue && r.hoveredValue !== term;
    r.verdict = r.committedHovered ? 'FAIL — hovered suggestion was committed'
              : r.committedTyped   ? 'PASS — typed text was kept'
              : `INCONCLUSIVE — input became "${committed}"`;

    console.log(`    typed "${term}" | hovered "${r.hoveredValue}" | after Enter input="${r.inputAfterEnter}" search=${r.searchParam}`);
    console.log(`    ${r.verdict}`);
  } catch (e) {
    r.error = e.message;
    console.log('    ERROR:', e.message);
    await page.screenshot({ path: path.join(OUT, `${target.key}-99-failure.png`), fullPage: true }).catch(() => {});
  } finally {
    await ctx.close();
  }
  return r;
}

(async () => {
  const only = process.argv[2];
  const targets = only ? TARGETS.filter(t => t.key === only) : TARGETS;
  const browser = await chromium.launch();
  try {
    for (const t of targets) results.push(await runTarget(browser, t));
  } finally {
    await browser.close();
  }

  console.log('\n================ SUMMARY ================');
  for (const r of results) {
    console.log(`${r.label}`);
    console.log(`  ${r.error ? 'ERROR: ' + r.error : r.verdict}`);
  }
  const file = path.join(OUT, 'results.json');
  fs.writeFileSync(file, JSON.stringify(results, null, 2));
  console.log('\nOutput:', OUT);
})();
