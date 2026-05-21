// DEF-208 targeted probe — discover a real /uploads/ filename via authenticated
// access, then attempt to fetch it anonymously. Pre-fix: 200 with file body.
// Post-fix: 404.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = 'https://gccwebapp.com';
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };

(async () => {
  const browser = await chromium.launch({ headless: true });
  const results = [];
  const record = (id, name, status, notes) => {
    results.push({ id, name, status, notes });
    const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
    console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
  };

  try {
    // 1) Authenticated discovery — find at least one /uploads/ path
    console.log('--- Authenticated discovery: find /uploads/ paths ---');
    const authCtx = await browser.newContext({ httpCredentials: BASIC });
    const authPage = await authCtx.newPage();
    await authPage.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await authPage.locator('#Email').fill('admin');
    await authPage.locator('#Password').fill('TestPass123!');
    await Promise.all([authPage.waitForLoadState('domcontentloaded'), authPage.locator('button[type=submit]').first().click()]);
    await authPage.waitForTimeout(2000);

    // Hit cert edit pages for known cert numbers and grep for /uploads/ paths
    const certNums = ['26-0163', '26-0161', '26-0162', '26-0146', '26-0001'];
    const found = new Set();
    for (const cn of certNums) {
      const r = await authCtx.request.get(`${BASE}/Certificate/Edit/${cn}`);
      if (r.status() !== 200) continue;
      const html = await r.text();
      const matches = html.match(/\/uploads\/[^"'\s)]+\.(pdf|png|jpg|jpeg)/gi) || [];
      matches.forEach(m => found.add(m));
      if (found.size >= 4) break;
    }
    const uploadPaths = [...found];
    console.log(`  discovered ${uploadPaths.length} /uploads/ paths`);
    uploadPaths.slice(0, 6).forEach(u => console.log(`    ${u}`));
    await authPage.close();
    await authCtx.close();

    if (uploadPaths.length === 0) {
      record('QC208-discovery', 'Discover real /uploads/ filename via authenticated access', 'FAIL', 'no upload paths found in cert edit HTML');
    } else {
      record('QC208-discovery', 'Discovered real /uploads/ filenames via authenticated access', 'PASS',
        `count=${uploadPaths.length}, sample=${uploadPaths[0]}`);

      // 2) Anonymous probe of each discovered path
      console.log('\n--- Anonymous probe of /uploads/ paths (DEF-208 deploy check) ---');
      const anonCtx = await browser.newContext({ httpCredentials: BASIC });
      let any200 = 0, any404 = 0, anyForbid = 0;
      for (const p of uploadPaths.slice(0, 6)) {
        const fullUrl = p.startsWith('http') ? p : BASE + p;
        const r = await anonCtx.request.get(fullUrl);
        const status = r.status();
        const body = await r.body();
        const ct = r.headers()['content-type'] || '';
        console.log(`  anon ${fullUrl} → HTTP ${status} (${body.length}B, ${ct})`);
        if (status === 200) any200++;
        else if (status === 404) any404++;
        else if (status === 401 || status === 403) anyForbid++;
      }

      // The fix: ALL /uploads/* should 404 to anonymous (or redirect to login).
      // ANY 200 with file body = the static-file leak is still live.
      record('QC208-no-anon-leak',
        'No anonymous 200 OK file responses on /uploads/* paths (DEF-208 deployed)',
        any200 === 0 ? 'PASS' : 'FAIL',
        `out of ${Math.min(uploadPaths.length, 6)} probes: 200s=${any200}, 404s=${any404}, 401/403s=${anyForbid}`);

      await anonCtx.close();
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, 'def208-uploads-probe-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN`);
})();
