// Phase 2 — data integrity and role scoping.
//
// The whole point of the paired accounts: external/external2 sit on different companies and
// amazon/novi on different partners. A scoped account can only ever prove it SEES something —
// never that it is correctly DENIED someone else's rows. So every check below compares key SETS
// between the two halves of a pair and prints BOTH directions of the difference. An equal count
// survives exactly the failure it is meant to detect.

const { BASE, HEADLESS, session, makeRecorder, chromium } = require('./_lib/regression-lib');

const PAGE_SIZE = 200;

// Walk a tile API to exhaustion. Sequential and modestly paged on purpose — the test box has died
// under bulk sweeps before.
//
// The two tile APIs do NOT share a paging contract: the internal one accepts offset/limit AND
// page/pageSize, the external one accepts page/pageSize only and silently ignores offset/limit.
// Sending offset/limit to the external API therefore returns page 1 forever, and the walk
// "completes" with duplicate rows and a plausible-looking count — so paging style is explicit here
// rather than assumed.
async function fetchAll(page, endpoint, label, style = 'page') {
  const out = [];
  let pageNo = 1, totalCount = null, pages = 0, guard = 0;
  while (guard++ < 200) {
    const q = style === 'offset'
      ? `offset=${(pageNo - 1) * PAGE_SIZE}&limit=${PAGE_SIZE}`
      : `page=${pageNo}&pageSize=${PAGE_SIZE}`;
    const url = `${BASE}${endpoint}${endpoint.includes('?') ? '&' : '?'}${q}`;
    const resp = await page.request.get(url, { failOnStatusCode: false });
    if (resp.status() !== 200) return { error: `${label}: HTTP ${resp.status()} on page ${pageNo}`, out, totalCount };
    const body = await resp.json();
    const rows = body.certificates || body.data || [];
    if (totalCount === null) totalCount = body.totalCount ?? null;
    out.push(...rows);
    pages++;
    if (rows.length === 0 || out.length >= (totalCount ?? 0)) break;
    pageNo++;
  }
  return { out, totalCount, pages };
}

// Split "no number at all" from "number shared with another row" — they are different defects and
// a single distinct-vs-total delta hides which one you have.
function numberHealth(rows) {
  const seen = new Map();
  let missing = 0;
  for (const r of rows) {
    const n = r.certificateNumber ?? r.CertificateNumber;
    if (n === null || n === undefined || String(n).trim() === '') { missing++; continue; }
    seen.set(String(n), (seen.get(String(n)) || 0) + 1);
  }
  const dupes = [...seen.entries()].filter(([, c]) => c > 1);
  return {
    missing,
    distinct: seen.size,
    duplicateNumbers: dupes.length,
    duplicateRows: dupes.reduce((s, [, c]) => s + c, 0),
    examples: dupes.slice(0, 10).map(([n, c]) => `${n}x${c}`),
  };
}

const numbersOf = (rows) => new Set(rows.map((r) => r.certificateNumber ?? r.CertificateNumber).filter(Boolean));
const companiesOf = (rows) => [...new Set(rows.map((r) => r.companyName ?? r.company ?? r.CompanyName).filter(Boolean))];
const diff = (a, b) => [...a].filter((x) => !b.has(x));

(async () => {
  const { rec, save } = makeRecorder('02-data-scoping');
  const startedAt = new Date().toISOString();
  console.log(`\n=== Phase 2: data integrity + scoping ===\n`);

  const browser = await chromium.launch({ headless: HEADLESS });
  const data = {};

  // --- Internal (Analyst/Admin) — the full population ---
  for (const role of ['admin', 'analyst']) {
    const { ctx, page } = await session(browser, role);
    const r = await fetchAll(page, '/api/certificate-tiles-internal', `internal/${role}`);
    if (r.error) rec(`data.internal.${role}`, `Internal tile API as ${role}`, 'FAIL', r.error);
    else {
      rec(`data.internal.${role}`, `Internal tile API as ${role}`, 'PASS',
        `totalCount=${r.totalCount}, fetched=${r.out.length} over ${r.pages} pages`);
      data[`internal_${role}`] = { totalCount: r.totalCount, fetched: r.out.length, numbers: numbersOf(r.out), health: numberHealth(r.out) };
    }
    await ctx.close();
  }

  // Analyst and Admin are both "internal" — they must see the identical population, not merely
  // the same tally.
  if (data.internal_admin && data.internal_analyst) {
    const a = data.internal_admin.numbers, b = data.internal_analyst.numbers;
    const onlyAdmin = diff(a, b), onlyAnalyst = diff(b, a);
    rec('data.internal.admin-eq-analyst', 'Admin and Analyst see the identical certificate set',
      onlyAdmin.length === 0 && onlyAnalyst.length === 0 ? 'PASS' : 'FAIL',
      `admin=${a.size} analyst=${b.size}; admin-only=[${onlyAdmin.slice(0, 5)}] analyst-only=[${onlyAnalyst.slice(0, 5)}]`);
  }

  // --- Public (anonymous) — visibility window applies ---
  {
    const { ctx, page } = await session(browser, null);
    const r = await fetchAll(page, '/api/certificate-tiles', 'public');
    if (r.error) rec('data.public', 'Public tile API', 'FAIL', r.error);
    else {
      data.public = { totalCount: r.totalCount, fetched: r.out.length, numbers: numbersOf(r.out), health: numberHealth(r.out) };
      rec('data.public', 'Public tile API (anonymous)', 'PASS',
        `totalCount=${r.totalCount}, fetched=${r.out.length}`);
      // DEF-358: public sees publishable status AND inside the effective-to-expiration window,
      // so it must be a strict subset of the internal population.
      if (data.internal_admin) {
        const leak = diff(data.public.numbers, data.internal_admin.numbers);
        rec('data.public.subset-of-internal', 'Public set is a subset of the internal set',
          leak.length === 0 ? 'PASS' : 'FAIL',
          leak.length ? `${leak.length} public certs absent internally: ${leak.slice(0, 5)}` : 'no leak');
        rec('data.public.narrower-than-internal', 'Public count is narrower than internal (visibility window)',
          data.public.totalCount < data.internal_admin.totalCount ? 'PASS' : 'WARN',
          `public=${data.public.totalCount} internal=${data.internal_admin.totalCount}`);
      }
    }
    await ctx.close();
  }

  // --- External + Partner scoped accounts ---
  for (const role of ['external', 'external2', 'amazon', 'novi']) {
    const { ctx, page, auth } = await session(browser, role);
    const r = await fetchAll(page, '/api/CertificateTileExternal', `external/${role}`, 'page');
    if (r.error) { rec(`data.scoped.${role}`, `External tile API as ${role}`, 'FAIL', r.error); await ctx.close(); continue; }
    const companies = companiesOf(r.out);
    data[role] = { totalCount: r.totalCount, fetched: r.out.length, numbers: numbersOf(r.out), health: numberHealth(r.out), companies, scope: auth.account.scope };
    rec(`data.scoped.${role}`, `External tile API as ${role} (scope: ${auth.account.scope})`, 'PASS',
      `totalCount=${r.totalCount}, fetched=${r.out.length}, distinct companies=${companies.length}`);
    // Enumerate the population, never sample it — printing [0] plus a count has produced a wrong
    // client-facing claim before.
    rec(`data.scoped.${role}.companies`, `Distinct companies visible to ${role}`, 'INFO',
      companies.slice(0, 25).join(' | ') + (companies.length > 25 ? ` …+${companies.length - 25}` : ''));
    await ctx.close();
  }

  // --- The pair cross-checks: the actual isolation proof ---
  const pairs = [
    ['external', 'external2', 'company'],
    ['amazon', 'novi', 'partner'],
  ];
  for (const [x, y, kind] of pairs) {
    if (!data[x] || !data[y]) continue;
    const onlyX = diff(data[x].numbers, data[y].numbers);
    const onlyY = diff(data[y].numbers, data[x].numbers);
    const shared = [...data[x].numbers].filter((n) => data[y].numbers.has(n));
    rec(`data.isolation.${x}-vs-${y}`, `${kind} isolation: ${x} (${data[x].scope}) vs ${y} (${data[y].scope})`,
      kind === 'company' ? (shared.length === 0 ? 'PASS' : 'FAIL') : (shared.length === 0 ? 'PASS' : 'WARN'),
      `${x}=${data[x].numbers.size} ${y}=${data[y].numbers.size} shared=${shared.length}` +
      (shared.length ? ` e.g. ${shared.slice(0, 5)}` : '') +
      ` | only-${x}=${onlyX.length} only-${y}=${onlyY.length}`);
  }

  // A company-scoped external must see exactly one company. More than one is a scoping breach.
  for (const role of ['external', 'external2']) {
    if (!data[role]) continue;
    const c = data[role].companies;
    rec(`data.scoped.${role}.single-company`, `${role} sees only ${data[role].scope}`,
      c.length === 1 ? 'PASS' : 'FAIL',
      `distinct companies = ${c.length}: ${c.slice(0, 10).join(' | ')}`);
  }

  // --- Certificate NUMBER, not Id, is the user-facing key (standing requirement) ---
  for (const [k, label] of [['internal_admin', 'internal'], ['public', 'public'], ['external', 'external-sika'], ['amazon', 'amazon']]) {
    if (!data[k] || !data[k].health) continue;
    const h = data[k].health;
    rec(`data.certnumber.${label}.populated`, `${label}: every row carries a certificateNumber`,
      h.missing === 0 ? 'PASS' : 'FAIL', `rows=${data[k].fetched}, blank/null numbers=${h.missing}`);
    rec(`data.certnumber.${label}.unique`, `${label}: certificate numbers are unique`,
      h.duplicateNumbers === 0 ? 'PASS' : 'FAIL',
      `${h.duplicateNumbers} numbers shared by ${h.duplicateRows} rows (distinct=${h.distinct}, rows=${data[k].fetched})` +
      (h.examples.length ? ` e.g. ${h.examples.join(', ')}` : ''));
    // A cert number that is merely the integer Id restated is the exact symptom chased before.
    const bareInts = [...data[k].numbers].filter((n) => /^\d+$/.test(String(n)));
    rec(`data.certnumber.${label}.not-bare-id`, `${label}: certificateNumber is not a bare integer Id`,
      bareInts.length === 0 ? 'PASS' : 'WARN',
      `${bareInts.length} of ${data[k].numbers.size} look like bare integers` + (bareInts.length ? ` e.g. ${bareInts.slice(0, 5)}` : ''));
  }

  // --- totalCount must describe the rows the API actually serves ---
  for (const k of Object.keys(data)) {
    const d = data[k];
    if (!d || d.totalCount == null) continue;
    rec(`data.paging.${k}`, `${k}: totalCount agrees with rows served`,
      d.fetched === d.totalCount ? 'PASS' : 'FAIL',
      `totalCount=${d.totalCount} rows walked=${d.fetched}`);
  }

  await browser.close();
  const serialisable = Object.fromEntries(Object.entries(data).map(([k, v]) => [k, { ...v, numbers: undefined, numberCount: v.numbers?.size }]));
  save({ startedAt, finishedAt: new Date().toISOString(), populations: serialisable });
})().catch((e) => { console.error('FATAL', e); process.exit(1); });
