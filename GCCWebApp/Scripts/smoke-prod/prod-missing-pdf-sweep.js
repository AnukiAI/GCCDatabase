// READ-ONLY prod sweep: how many certs have a stored CertificateFile but the physical
// PDF is missing on the server (download 404). NO mutations — login + GET/HEAD only.
//
// Enumerate via the internal tile API (projection exposes PdfPath = c.CertificateFile),
// then HEAD /Certificate/Download/{number} for every cert that has a path. HEAD executes
// the GET action but suppresses the body, so present PDFs are NOT transferred.
//
// Run: node Scripts/smoke-prod/prod-missing-pdf-sweep.js
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'https://gccwebapp.com';
const GATE = { username: 'gcc', password: 'Cert-Test-Access-2026!' };
const PAGE_SIZE = parseInt(process.env.PAGE_SIZE || '100', 10);
const CONCURRENCY = parseInt(process.env.CONCURRENCY || '8', 10);

const pick = (o, keys) => { for (const k of keys) if (o[k] != null && o[k] !== '') return o[k]; return null; };

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded' });
  await page.locator('#Email').fill('admin');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]);
  await page.waitForTimeout(1200);
  if (/\/Account\/Login/i.test(page.url())) { console.log('LOGIN FAILED — aborting'); await browser.close(); process.exit(1); }
  console.log('login -> ok\n');

  // 1) Enumerate all certs via the internal tile API.
  const certs = [];
  for (let p = 1; p <= 200; p++) {
    const data = await page.evaluate(async (u) => {
      const r = await fetch(u, { headers: { Accept: 'application/json' } });
      if (!r.ok) return { err: r.status };
      return r.json();
    }, `${BASE}/api/certificate-tiles-internal?sortBy=newest&page=${p}&pageSize=${PAGE_SIZE}`);
    if (data.err) { console.log(`page ${p}: HTTP ${data.err} — stopping`); break; }
    const arr = data.certificates || data.data || data.items || [];
    if (p === 1 && arr[0]) console.log('sample keys:', Object.keys(arr[0]).join(', '), '\n');
    for (const c of arr) {
      certs.push({
        number: pick(c, ['number', 'Number', 'certificateNumber', 'CertificateNumber']),
        pdf: pick(c, ['pdfPath', 'PdfPath', 'certificateFile', 'CertificateFile']),
      });
    }
    if (arr.length < PAGE_SIZE) break;
  }
  const withPath = certs.filter((c) => c.pdf && c.number);
  console.log(`enumerated ${certs.length} certs; ${withPath.length} have a stored PDF path; ${certs.length - withPath.length} have no path\n`);

  // 2) HEAD the download for each cert that claims a file. 404 => physical file missing.
  const missing = [], present = [], other = [];
  let i = 0;
  async function worker() {
    while (i < withPath.length) {
      const c = withPath[i++];
      const res = await page.evaluate(async (u) => {
        try { const r = await fetch(u, { method: 'HEAD' }); return { s: r.status, ct: r.headers.get('content-type') || '' }; }
        catch (e) { return { s: -1, ct: String(e) }; }
      }, `${BASE}/Certificate/Download/${encodeURIComponent(c.number)}`);
      if (res.s === 200) present.push(c.number);
      else if (res.s === 404) missing.push(c.number);
      else other.push({ n: c.number, s: res.s });
      if ((present.length + missing.length + other.length) % 25 === 0)
        process.stdout.write(`  checked ${present.length + missing.length + other.length}/${withPath.length}\r`);
    }
  }
  await Promise.all(Array.from({ length: CONCURRENCY }, worker));

  console.log('\n\n========== MISSING-PDF SWEEP (prod) ==========');
  console.log(`  certs total:            ${certs.length}`);
  console.log(`  with stored PDF path:   ${withPath.length}`);
  console.log(`  PDF present (200):      ${present.length}`);
  console.log(`  PDF MISSING (404):      ${missing.length}`);
  if (other.length) console.log(`  other status:           ${other.length} -> ${JSON.stringify(other.slice(0, 10))}`);
  if (missing.length) {
    console.log('\n  MISSING cert numbers:');
    console.log('  ' + missing.sort().join(', '));
  }
  console.log('==============================================');

  const out = path.join(__dirname, '_results', 'prod-missing-pdf-sweep.json');
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, JSON.stringify({ base: BASE, total: certs.length, withPath: withPath.length, present: present.length, missing, other }, null, 2));
  console.log('  results ->', out);
  await browser.close();
})();
