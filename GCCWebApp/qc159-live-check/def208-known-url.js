// DEF-208 final probe — try known-leaked URLs (per the DEF entry's "Originally
// verified live on prod" example) anonymously. Pre-fix: 200 OK with image/PDF.
// Post-fix: 404.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = 'https://gccwebapp.com';
const BASIC = { username: 'gcc', password: 'Cert-Test-Access-2026!' };

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: BASIC });
  const results = [];
  const record = (id, name, status, notes) => {
    results.push({ id, name, status, notes });
    const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
    console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
  };

  try {
    // URLs that the DEF-208 entry explicitly documents as having leaked PRE-fix:
    //   "GET https://gccwebapp.com/uploads/images/26-0163_thumb_20260511_122453.png returned 200 image/png to anonymous"
    // Plus a representative spread.
    const probes = [
      '/uploads/images/26-0163_thumb_20260511_122453.png',  // DEF-208 documented leak
      '/uploads/certificates/26-0001_20260112_230836.pdf',   // probable older cert
      '/uploads/certificates/26-0163_20260511_122453.pdf',   // counterpart to the leaked image
      '/uploads/',                                            // directory listing (should always be 404)
    ];

    let any200 = 0, any404 = 0, anyOther = 0;
    for (const p of probes) {
      const r = await ctx.request.get(BASE + p);
      const status = r.status();
      const body = await r.body();
      const ct = r.headers()['content-type'] || '';
      console.log(`  ${p}  → HTTP ${status} (${body.length}B, ct=${ct})`);
      if (status === 200) any200++;
      else if (status === 404) any404++;
      else anyOther++;
    }

    record('QC208-no-static-leak',
      'No /uploads/* anonymous 200s — DEF-208 static-file gate deployed',
      any200 === 0 ? 'PASS' : 'FAIL',
      `out of ${probes.length} probes (including the explicit leaked-URL from DEF-208): 200s=${any200}, 404s=${any404}, other=${anyOther}`);

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    fs.writeFileSync(path.join(OUT, 'def208-known-url-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL`);
})();
