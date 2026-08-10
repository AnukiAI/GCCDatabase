// DEF-345 — rate-limit tier + bucket-isolation spec.
//
// Until DEF-345, ApiRateLimitingMiddleware ran BEFORE authentication, so context.User was always
// empty and every /api caller fell through to AnonymousLimit (100/hr) in a bucket keyed by IP.
// Moving the middleware below UseAuthentication()/PartnerApiKeyAuthenticationMiddleware activated
// four branches that had NEVER executed in production. This spec pins each of them, and pins the
// property that actually matters: distinct callers get distinct counters.
//
// Asserted against RateLimitOptions in Program.cs:
//   anonymous 100 · authenticated (analyst) 500 · admin 5000 · partner key 200
//
// The tier is read from the X-RateLimit-Limit response header, which the middleware sets on every
// /api response before deciding whether to reject. Reading the header (rather than hammering the
// endpoint until it 429s) keeps this cheap and non-destructive — it does not burn a real allowance.
//
// Requires the dev server already running. Run:
//   node Scripts/smoke-prod/_security/def345-rate-limit-tiers.js
// Optional: DEF345_PARTNER_KEY=<gcc_partner_...> to exercise the partner branch.

const H = require('./_lib/security-harness');

const API = '/api/v1/certificates?pageSize=1';
const SPEC = 'def345-rate-limit-tiers';

// Read the rate-limit headers for a request made through a Playwright context (carries cookies).
async function limits(ctx, extraHeaders = {}) {
  const r = await ctx.request.get(H.BASE + API, {
    headers: { Accept: 'application/json', ...extraHeaders },
  });
  const h = r.headers();
  return {
    status: r.status(),
    limit: h['x-ratelimit-limit'] ? Number(h['x-ratelimit-limit']) : null,
    remaining: h['x-ratelimit-remaining'] ? Number(h['x-ratelimit-remaining']) : null,
  };
}

async function run() {
  H.guardRemote();
  console.log(`\n[security] ${SPEC} — ${H.BASE}\n`);
  const { results, record } = H.makeRecorder(SPEC);
  const ok = (c) => (c ? 'PASS' : 'FAIL');

  const browser = await H.chromium.launch();
  try {
    // ---- anonymous ---------------------------------------------------------
    const anonCtx = await H.newContext(browser);
    const anon1 = await limits(anonCtx);
    record('tiers.anon-100', 'anonymous caller gets AnonymousLimit',
      ok(anon1.limit === 100), `X-RateLimit-Limit=${anon1.limit} (expect 100)`);

    // ---- partner API key ---------------------------------------------------
    // Skip cleanly if no key is supplied, so this degrades to BLOCKED on a machine without
    // seed keys rather than reporting a misleading FAIL.
    const partnerKey = process.env.DEF345_PARTNER_KEY;
    if (!partnerKey) {
      record('tiers.partner-200', 'partner API key gets PartnerApiKeyLimit',
        'BLOCKED', 'DEF345_PARTNER_KEY not set');
      record('tiers.partner-bucket', 'partner and anonymous counters are independent',
        'BLOCKED', 'DEF345_PARTNER_KEY not set');
    } else {
      const pctx = await H.newContext(browser);
      const hdr = { 'X-API-Key': partnerKey };
      const p1 = await limits(pctx, hdr);
      record('tiers.partner-200', 'partner API key gets PartnerApiKeyLimit',
        ok(p1.status === 200 && p1.limit === 200),
        `HTTP ${p1.status}, X-RateLimit-Limit=${p1.limit} (expect 200 / 200)`);

      // The core DEF-345 property: the partner counter must be its OWN bucket, not a
      // continuation of the anonymous one. Before the fix these shared an IP-keyed counter,
      // so a partner call decremented the same counter an anonymous call had just used.
      const p2 = await limits(pctx, hdr);
      const anon2 = await limits(anonCtx);
      const partnerCounted = p2.remaining === p1.remaining - 1;
      const anonUnaffected = anon2.remaining === anon1.remaining - 1;
      record('tiers.partner-bucket', 'partner and anonymous counters are independent',
        ok(partnerCounted && anonUnaffected && p2.limit === 200 && anon2.limit === 100),
        `partner ${p1.remaining}->${p2.remaining} (limit ${p2.limit}); ` +
        `anon ${anon1.remaining}->${anon2.remaining} (limit ${anon2.limit})`);
      await pctx.close();
    }

    // ---- authenticated cookie roles ---------------------------------------
    for (const [role, expected, name] of [['analyst', 500, 'AuthenticatedLimit'], ['admin', 5000, 'AdminLimit']]) {
      const ctx = await H.newContext(browser);
      const { ok: loggedIn } = await H.login(ctx, role);
      if (!loggedIn) {
        record(`tiers.${role}-${expected}`, `${role} gets ${name}`, 'BLOCKED', `could not log in as ${role}`);
        await ctx.close();
        continue;
      }
      const r = await limits(ctx);
      record(`tiers.${role}-${expected}`, `${role} gets ${name}`,
        ok(r.limit === expected), `X-RateLimit-Limit=${r.limit} (expect ${expected})`);
      await ctx.close();
    }

    // ---- regression guard: invalid keys are no longer throttled here -------
    // Documented in DEF-345 as a known consequence of the ordering fix: the partner-auth
    // middleware short-circuits with 401 without calling _next, so the limiter never sees the
    // request and emits no headers. Recorded as WARN, not FAIL — it is a known, accepted state.
    // If the gap is ever closed this flips to PASS and says so, instead of silently going green.
    const badCtx = await H.newContext(browser);
    const bad = await limits(badCtx, { 'X-API-Key': 'gcc_partner_definitely_not_a_real_key' });
    if (bad.limit === null) {
      record('tiers.invalid-key-unthrottled', 'invalid key bypasses this limiter (known DEF-345 regression)',
        'WARN', `HTTP ${bad.status}, no X-RateLimit-* headers — UseIpRateLimiting is the only ` +
        `backstop and it is disabled in Development`);
    } else {
      record('tiers.invalid-key-unthrottled', 'invalid key IS throttled — DEF-345 regression appears closed, update the register',
        'PASS', `HTTP ${bad.status}, X-RateLimit-Limit=${bad.limit}`);
    }
    await badCtx.close();
    await anonCtx.close();
  } finally {
    await browser.close();
  }

  return H.summarize(SPEC, API, results);
}

module.exports = { run };

if (require.main === module) {
  run().then((s) => process.exit(s.fail > 0 || s.blocked > 0 ? 1 : 0));
}
