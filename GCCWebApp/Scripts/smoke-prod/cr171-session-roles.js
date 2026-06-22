// CR-171 — Session-role partner-visibility check (Playwright, LOCALHOST).
// Confirms how the cert `partner` field is scoped per logged-in role on a multi-partner
// cert (26-0146 = KAO, [Amazon, Novi], public status), via each role's tile API.
//
// Expectation per CR-171 §4-6 visibility matrix:
//   public   -> partner field absent / empty (public sees NO partner names)
//   external (company KAO) -> ALL associated  ("Amazon, Novi")
//   partner  (Amazon web login, no company)   -> OWN only ("Amazon")   [matrix row "External-Partner user"]
//   analyst  -> ALL associated  ("Amazon, Novi")
//   admin    -> ALL associated  ("Amazon, Novi")
//
// Run (after `dotnet run --urls http://localhost:5101`):
//   node Scripts/smoke-prod/cr171-session-roles.js
//
// Test users (dev DB): admin/analyst/external = TestPass123!; partner = TestPass123! (Amazon-associated, set up for this test).

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:5101';
const TARGET = '26-0146';
const EXPECT = { Amazon: true, Novi: true }; // partners actually on the cert

const ROLES = [
  { role: 'public',   login: null,                                  api: '/api/certificate-tiles?pageSize=500',          expect: 'no partner names' },
  { role: 'external', login: { e: 'external', p: 'TestPass123!' },  api: '/api/CertificateTileExternal?pageSize=500',    expect: 'Amazon, Novi (all)' },
  { role: 'partner-role(87)', login: { e: 'pat.amazon@example.com',      p: 'TestPass123!' }, api: '/api/CertificateTileExternal?pageSize=500', expect: 'Partner role, Amazon -> scoped "Amazon"' },
  { role: 'ext+partner(93)',  login: { e: 'phil.nonpartner@example.com', p: 'TestPass123!' }, api: '/api/CertificateTileExternal?pageSize=500', expect: 'External+Amazon, company=Acme -> SHOULD see Amazon-tagged 26-0146' },
  { role: 'analyst',  login: { e: 'analyst',  p: 'TestPass123!' },  api: '/api/certificate-tiles-internal?pageSize=500', expect: 'Amazon, Novi (all)' },
  { role: 'admin',    login: { e: 'admin',    p: 'TestPass123!' },  api: '/api/certificate-tiles-internal?pageSize=500', expect: 'Amazon, Novi (all)' },
];

async function login(page, creds) {
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 60000 });
  await page.locator('#Email').fill(creds.e);
  await page.locator('#Password').fill(creds.p);
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);
}

// Fetch the tile API inside the authenticated browser context (carries the auth cookie).
async function fetchTiles(page, api) {
  // Ensure the page is on the app origin (public role never logs in / navigates),
  // otherwise the in-page fetch runs from about:blank and is blocked cross-origin.
  if (!page.url().startsWith(BASE)) {
    await page.goto(BASE + '/', { waitUntil: 'domcontentloaded', timeout: 60000 });
  }
  return await page.evaluate(async (url) => {
    const r = await fetch(url, { credentials: 'include', headers: { Accept: 'application/json' } });
    let body = null; try { body = await r.json(); } catch { body = null; }
    return { status: r.status, body };
  }, BASE + api);
}

function extractList(body) {
  if (!body) return [];
  for (const k of ['data', 'Data', 'certificates', 'Certificates', 'items', 'Items', 'results']) {
    if (Array.isArray(body[k])) return body[k];
  }
  return Array.isArray(body) ? body : [];
}

function findCert(list, num) {
  return list.find((it) =>
    Object.values(it).some((v) => typeof v === 'string' && v === num)
  );
}
function partnerFieldOf(item) {
  // find the property that holds the partner string (case-insensitive 'partner')
  for (const [k, v] of Object.entries(item)) {
    if (/^partner$/i.test(k)) return { key: k, value: v };
  }
  // fallback: any key containing 'partner' that is a string
  for (const [k, v] of Object.entries(item)) {
    if (/partner/i.test(k) && typeof v === 'string') return { key: k, value: v };
  }
  return { key: '(none)', value: undefined };
}

(async () => {
  console.log(`\nCR-171 session-role partner visibility — ${BASE}  (cert ${TARGET})\n`);
  const browser = await chromium.launch({ headless: true });
  const out = [];
  for (const r of ROLES) {
    const ctx = await browser.newContext();
    const page = await ctx.newPage();
    const rec = { role: r.role, expect: r.expect };
    try {
      if (r.login) await login(page, r.login);
      const { status, body } = await fetchTiles(page, r.api);
      rec.httpStatus = status;
      const list = extractList(body);
      rec.certCount = list.length;
      const cert = findCert(list, TARGET);
      if (!cert) {
        rec.found = false;
        rec.note = status === 200 ? `cert ${TARGET} NOT in ${list.length} results` : `HTTP ${status}`;
      } else {
        rec.found = true;
        const pf = partnerFieldOf(cert);
        rec.partnerField = pf.key;
        rec.partnerValue = pf.value === undefined ? '(field absent)' : pf.value;
        rec.itemKeys = Object.keys(cert);
      }
    } catch (e) {
      rec.error = e.message;
    }
    out.push(rec);
    await ctx.close();
    const seen = rec.partnerValue !== undefined ? `partner=${JSON.stringify(rec.partnerValue)}` : (rec.note || rec.error || '');
    console.log(`  [${r.role.padEnd(8)}] http=${rec.httpStatus} found=${rec.found} ${seen}   (expect: ${r.expect})`);
  }
  await browser.close();

  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const dir = path.join(__dirname, '_results');
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  const file = path.join(dir, `cr171-session-roles-${stamp}.json`);
  fs.writeFileSync(file, JSON.stringify({ base: BASE, target: TARGET, expectedPartners: EXPECT, results: out }, null, 2));
  console.log(`\nresults -> ${path.relative(process.cwd(), file)}\n`);
})().catch((e) => { console.error(e); process.exit(1); });
