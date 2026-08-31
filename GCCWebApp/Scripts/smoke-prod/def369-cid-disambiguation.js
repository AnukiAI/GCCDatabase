// DEF-369 — verify that certificates SHARING a number are individually addressable, and that a
// wrong id is refused rather than silently serving a sibling.
//
// CR-038 allowed several certificates to hold one number (137 numbers / 312 certificates in the
// migrated dataset). Seven actions resolved by number alone, so every sibling collapsed onto one
// record: Download served a sibling's PDF, Edit LOADED a sibling for editing, and Delete would have
// permanently destroyed the wrong certificate. The fix adds an optional `cid` disambiguator; the
// number is still required and still matched, and a mismatch returns 404 with NO fallback.
//
// Every assertion here is falsifiable against the sibling set: each check names the certificate it
// expects AND the siblings it must not see. A test that only confirmed "a page loaded" would have
// passed just as happily before the fix — that is precisely how this defect survived.
//
// Usage:  node def369-cid-disambiguation.js
//         BASE_URL=http://localhost:5101  ADMIN_USER=admin  ADMIN_PASS=TestPass123!
//
// The DESTRUCTIVE path is deliberately not exercised: Delete's confirmation page (GET) is asserted,
// DeleteConfirmed (POST) is NEVER called. Its correctness rests on the same repository lookup as
// the six checked here plus DEF369_DuplicateCertificateNumberTests.
const { chromium, request } = require('playwright');
const crypto = require('crypto');
const { detectGateMode, passGate } = require('./_lib/test-env-gate');

const BASE = (process.env.BASE_URL || 'http://localhost:5101').replace(/\/$/, '');

// The fixture: one number, four certificates, four DIFFERENT PDFs. Confirmed in the dataset before
// writing this — a shared number whose siblings pointed at the SAME file could not distinguish a
// working fix from a broken one.
const NUMBER = '26-0182';
const SIBLINGS = [
  { cid: 3741, product: 'REGUPOL sonus core' },
  { cid: 3742, product: 'REGUPOL 6510S' },
  { cid: 3743, product: 'Impacta ProBase' },
  { cid: 3744, product: 'Thunder Flat' },
];
// An id that exists but belongs to a DIFFERENT number (23-0041). The number+id pair must not match.
const FOREIGN_CID = 327;
const ABSENT_CID = 999999;

let pass = 0, fail = 0;
function check(name, ok, detail) {
  if (ok) { pass++; console.log(`  PASS  ${name}${detail ? '  — ' + detail : ''}`); }
  else { fail++; console.log(`  FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
}

// Assert the page shows THIS certificate and none of its siblings. Both halves matter: presence
// alone was already true before the fix, because every sibling rendered the same one certificate.
function assertOnlyThisSibling(label, html, expected) {
  const has = (s) => html.includes(s);
  const others = SIBLINGS.filter(s => s.product !== expected).map(s => s.product);
  const leaked = others.filter(has);
  check(`${label}: shows "${expected}"`, has(expected));
  check(`${label}: shows no sibling`, leaked.length === 0,
    leaked.length ? `LEAKED ${JSON.stringify(leaked)}` : 'none of the other 3 present');
}

(async () => {
  const browser = await chromium.launch();

  let gateState;
  const mode = await detectGateMode(request, BASE);
  if (mode.mode === 'cookie') {
    const gate = await passGate(request, BASE);
    if (!gate.ok) { console.log(`FATAL: gate — ${gate.detail}`); process.exit(2); }
    gateState = gate.state;
    console.log(`  gate: passed cookie gate at ${BASE}`);
  } else {
    console.log(`  gate: ${mode.mode} (${mode.detail})`);
  }

  // Anonymous context: the public surface, with no session at all.
  const anon = await request.newContext({ ignoreHTTPSErrors: true, storageState: gateState });
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, storageState: gateState });
  const page = await ctx.newPage();

  try {
    console.log(`\n--- Public single view, anonymous (${NUMBER}, 4 siblings) ---`);
    for (const s of SIBLINGS) {
      const r = await anon.get(`${BASE}/Certificate/CertificateSingleView/${NUMBER}?cid=${s.cid}`);
      check(`cid=${s.cid}: 200`, r.status() === 200, `status ${r.status()}`);
      if (r.status() === 200) assertOnlyThisSibling(`cid=${s.cid}`, await r.text(), s.product);
    }

    console.log('\n--- Back-compat and refusal ---');
    const bare = await anon.get(`${BASE}/Certificate/CertificateSingleView/${NUMBER}`);
    check('bare number still resolves (no cid)', bare.status() === 200, `status ${bare.status()}`);
    const bareHtml = bare.status() === 200 ? await bare.text() : '';
    const bareMatches = SIBLINGS.filter(s => bareHtml.includes(s.product)).map(s => s.product);
    check('bare number returns exactly one of the siblings', bareMatches.length === 1,
      `resolved to ${JSON.stringify(bareMatches)}`);

    // The core of the fix: a mismatched pair is REFUSED, not quietly redirected to the number.
    const foreign = await anon.get(`${BASE}/Certificate/CertificateSingleView/${NUMBER}?cid=${FOREIGN_CID}`);
    check(`mismatched cid (${FOREIGN_CID} belongs to 23-0041) → 404, no fallback`,
      foreign.status() === 404, `status ${foreign.status()}`);
    const absent = await anon.get(`${BASE}/Certificate/CertificateSingleView/${NUMBER}?cid=${ABSENT_CID}`);
    check(`nonexistent cid (${ABSENT_CID}) → 404`, absent.status() === 404, `status ${absent.status()}`);

    console.log('\n--- Verify redirect carries cid ---');
    const ver = await anon.get(`${BASE}/Certificate/Verify/${NUMBER}?cid=${SIBLINGS[2].cid}`,
      { maxRedirects: 0 }).catch(e => ({ status: () => 0, headers: () => ({}), _e: e }));
    const loc = (ver.headers && ver.headers()['location']) || '';
    check('Verify 302 preserves cid', /cid=3743/i.test(loc), `location: ${loc || '(none)'}`);

    console.log('\n--- Download: each sibling must yield ITS OWN PDF ---');
    const hashes = {};
    for (const s of SIBLINGS) {
      const r = await anon.get(`${BASE}/Certificate/Download/${NUMBER}?cid=${s.cid}`);
      if (r.status() !== 200) { check(`Download cid=${s.cid}`, false, `status ${r.status()}`); continue; }
      const buf = await r.body();
      // Magic bytes, not content-type: a role-gated HTML error page is served as 200 and would
      // otherwise pass as a "downloaded PDF" (see the 4,655-missing-PDF false alarm).
      const isPdf = buf.slice(0, 4).toString('latin1') === '%PDF';
      const sha = crypto.createHash('sha256').update(buf).digest('hex').slice(0, 12);
      hashes[s.cid] = sha;
      check(`Download cid=${s.cid} is a real PDF`, isPdf, `${buf.length} bytes, sha ${sha}`);
    }
    const uniq = new Set(Object.values(hashes));
    check('all four downloads are DIFFERENT files', uniq.size === Object.keys(hashes).length,
      `${uniq.size} distinct of ${Object.keys(hashes).length} — ${JSON.stringify(hashes)}`);

    console.log('\n--- DownloadByNumber: the third download entry point ---');
    // NOTE THE URL FORM. This action's parameter is `number`, but the default route is
    // {controller}/{action}/{id?} — so a path segment binds to `id` and leaves `number` null,
    // which returns 404. It must be called with an explicit query string. Getting this wrong
    // produced four 404s that looked like a broken endpoint and hid the actual defect below.
    const byNumHashes = {};
    for (const s of SIBLINGS) {
      const r = await anon.get(`${BASE}/Certificate/DownloadByNumber?number=${encodeURIComponent(NUMBER)}&cid=${s.cid}`);
      if (r.status() !== 200) { check(`DownloadByNumber cid=${s.cid}`, false, `status ${r.status()}`); continue; }
      byNumHashes[s.cid] = crypto.createHash('sha256').update(await r.body()).digest('hex').slice(0, 12);
    }
    // The regression this guards: DownloadByNumber resolved the right certificate with `cid` and
    // then delegated as `Download(certificate.Number)` — dropping the id at the last step, so all
    // four siblings returned the SAME pdf while every status code said 200. Assert distinctness
    // explicitly; "four successful downloads" was true throughout the broken period.
    const byNumUniq = new Set(Object.values(byNumHashes));
    check('DownloadByNumber returns a DIFFERENT file per sibling', byNumUniq.size === SIBLINGS.length,
      `${byNumUniq.size} distinct of ${Object.keys(byNumHashes).length} — ${JSON.stringify(byNumHashes)}`);
    const agree = SIBLINGS.every(s => byNumHashes[s.cid] && byNumHashes[s.cid] === hashes[s.cid]);
    check('DownloadByNumber agrees with Download, per sibling', agree, JSON.stringify(byNumHashes));

    console.log('\n--- Authenticated: internal view, Edit (writes), Delete confirmation ---');
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    await page.locator('#Email').fill(process.env.ADMIN_USER || 'admin');
    await page.locator('#Password').fill(process.env.ADMIN_PASS || 'TestPass123!');
    await Promise.all([page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click()]);
    await page.waitForTimeout(700);
    const who = await ctx.request.get(`${BASE}/Dashboard`);
    check('setup: signed in as an analyst/admin', who.status() === 200 && !who.url().includes('/Account/Login'),
      `status ${who.status()}`);

    for (const s of SIBLINGS) {
      const r = await ctx.request.get(`${BASE}/Certificate/CertificateInternalView/${NUMBER}?cid=${s.cid}`);
      check(`InternalView cid=${s.cid}: 200`, r.status() === 200, `status ${r.status()}`);
      if (r.status() === 200) assertOnlyThisSibling(`InternalView cid=${s.cid}`, await r.text(), s.product);
    }

    // Edit is the write path: loading the wrong record here saves a correction onto another
    // product. Assert the form is bound to the requested id, not merely that a form rendered.
    //
    // The product name is NOT asserted from the raw HTML: #ProductId renders as an empty
    // "Select a product..." and is filled by loadProductsForCompany() over AJAX. A server-side
    // string check therefore proves nothing about it — and worse, the company <select> contains an
    // option literally named "Regupol(R) Sonus(TM) HS; Thunder Flat", so a naive substring search
    // for a sibling's product name matches unrelated markup and reports a data leak that is not
    // there. The selected product is checked in a real browser further down instead.
    for (const s of SIBLINGS) {
      const r = await ctx.request.get(`${BASE}/Certificate/Edit/${NUMBER}?cid=${s.cid}`);
      check(`Edit cid=${s.cid}: 200`, r.status() === 200, `status ${r.status()}`);
      if (r.status() !== 200) continue;
      const html = await r.text();
      const bound = new RegExp(`name="Id"[^>]*value="${s.cid}"|value="${s.cid}"[^>]*name="Id"`, 'i').test(html);
      check(`Edit cid=${s.cid}: form is bound to id ${s.cid}`, bound,
        bound ? 'hidden Id matches the requested cid' : 'hidden Id does NOT match — a save would hit another record');
    }
    const editForeign = await ctx.request.get(`${BASE}/Certificate/Edit/${NUMBER}?cid=${FOREIGN_CID}`);
    check(`Edit with mismatched cid → 404 (never loads a sibling for editing)`,
      editForeign.status() === 404, `status ${editForeign.status()}`);

    // Delete: only the REFUSAL is assertable here. The success path GET returns 500 for every
    // certificate in the system — `Views/Certificate/Delete.cshtml` does not exist — which is a
    // pre-existing defect unrelated to DEF-369 and invisible in the UI because all three delete
    // buttons POST to DeleteConfirmed instead. Asserting 200 here would fail forever for a reason
    // that has nothing to do with this fix. The destructive POST is deliberately never sent.
    const delForeign = await ctx.request.get(`${BASE}/Certificate/Delete/${NUMBER}?cid=${FOREIGN_CID}`);
    check('Delete with mismatched cid → 404 (refused before the missing view is reached)',
      delForeign.status() === 404, `status ${delForeign.status()}`);
    console.log('  SKIP  Delete confirmation success path — no Delete.cshtml exists (pre-existing, not DEF-369)');

    console.log('\n--- Client side: does the click path actually carry the id? ---');
    // The server fix is inert if the click path never sends the id — the DEF-342 lesson. This is
    // split into the two things that are each directly checkable, rather than driving the search
    // box: that widget is an autocomplete over company / product / certification type and does NOT
    // match on certificate number, so typing "26-0182" into it correctly returns nothing. A check
    // built on it would have failed forever while proving nothing about cid.
    //
    // (1) THE DATA: the tile API must hand the renderer four distinct ids for the shared number.
    const tileApi = await ctx.request.get(`${BASE}/api/certificate-tiles?search=${encodeURIComponent(NUMBER)}&pageSize=20`);
    let rows = [];
    if (tileApi.status() === 200) {
      const j = await tileApi.json();
      rows = (j.data || j.certificates || j.items || j || []).filter(r => r && r.certificateNumber === NUMBER);
    }
    const apiIds = [...new Set(rows.map(r => r.id))].sort((a, b) => a - b);
    const expected = SIBLINGS.map(s => s.cid).sort((a, b) => a - b);
    check('tile API returns every sibling of the shared number, each with its own id',
      apiIds.length === expected.length && apiIds.every((v, i) => v === expected[i]),
      `ids ${JSON.stringify(apiIds)}, expected ${JSON.stringify(expected)}`);

    // (2) THE RENDERER: on the rendered grid, NO certificate handler may be left number-only.
    // Asserted over whatever the page loads by default, so a regression in any one handler is
    // caught regardless of which certificates happen to be on screen.
    await page.goto(`${BASE}/Certificate/CertificateTileViewInternal`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(4000);
    const handlers = await page.evaluate(() => {
      const FNS = 'viewCertificateDetails|viewCertificate|viewCertificateNewTab|editCertificate|downloadCertificate';
      const withId = new RegExp(`(${FNS})\\(\\s*'[^']*'\\s*,\\s*(\\d+)\\s*\\)`);
      const anyCall = new RegExp(`(${FNS})\\(`);
      const all = [], bare = [];
      for (const el of document.querySelectorAll('[onclick]')) {
        const h = el.getAttribute('onclick') || '';
        if (!anyCall.test(h)) continue;
        const m = h.match(withId);
        if (m) all.push({ fn: m[1], id: Number(m[2]) });
        else bare.push(h.slice(0, 100));   // a call with no id = the defect, still live
      }
      return { all, bare };
    });
    check('tile grid rendered certificate handlers', handlers.all.length + handlers.bare.length > 0,
      `${handlers.all.length + handlers.bare.length} handlers on the page`);
    check('NO handler is left number-only (every one passes an id)', handlers.bare.length === 0,
      handlers.bare.length ? `${handlers.bare.length} without an id, e.g. ${handlers.bare[0]}` : 'all carry an id');
    const fns = [...new Set(handlers.all.map(h => h.fn))];
    check('view, edit and download handlers all carry the id', fns.length >= 3, `handlers: ${JSON.stringify(fns)}`);

    console.log('\n--- Edit form in a real browser: is the RIGHT product selected? ---');
    // The write path's real risk, and the one the hidden Id cannot rule out. #ProductId is filled
    // by AJAX and then preselected from savedProductId; if that preselect misses, the dropdown
    // shows a different product and saving silently rewrites a required field onto the wrong value
    // — the DEF-343 shape. Assert the SELECTED OPTION TEXT after the JS has run.
    for (const s of SIBLINGS) {
      await page.goto(`${BASE}/Certificate/Edit/${NUMBER}?cid=${s.cid}`, { waitUntil: 'domcontentloaded' });
      let selected = '';
      for (let i = 0; i < 25; i++) {           // poll rather than fixed-sleep: the AJAX fill is async
        await page.waitForTimeout(400);
        selected = await page.evaluate(() => {
          const el = document.querySelector('#ProductId');
          if (!el || el.selectedIndex < 0) return '';
          return (el.options[el.selectedIndex] || {}).text || '';
        });
        if (selected && !/select a product/i.test(selected)) break;
      }
      check(`Edit cid=${s.cid}: product selected is "${s.product}"`,
        selected.trim() === s.product,
        `dropdown shows "${selected.trim() || '(nothing)'}"`);
    }

  } catch (e) {
    fail++; console.log(`FATAL: ${e.message}`);
  } finally {
    await anon.dispose().catch(() => {});
    await ctx.close();
    await browser.close();
  }

  console.log(`\n=== DEF-369 cid disambiguation: ${pass} passed, ${fail} failed ===`);
  console.log(fail ? 'NOT VERIFIED — see failures above.'
                   : 'VERIFIED — each sibling resolves to its own record and PDF; mismatches are refused.');
  process.exit(fail ? 1 : 0);
})();
