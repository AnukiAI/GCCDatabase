// Phase 0 — preflight: is the target reachable, how is it gated, and can all six role accounts
// authenticate? Everything downstream grades pages; running it against a gate page or a failed
// login produces confident nonsense, so this phase is a hard gate on the rest of the run.

const { BASE, HEADLESS, ACCOUNTS, detectGate, session, makeRecorder, chromium } = require('./_lib/regression-lib');

(async () => {
  const { rec, save } = makeRecorder('00-preflight');
  const startedAt = new Date().toISOString();
  console.log(`\n=== Phase 0: preflight — ${BASE} ===\n`);

  const gate = await detectGate();
  rec('pre.gate-mode', `Gate mode detected: ${gate.mode}`,
    gate.mode === 'cookie' ? 'PASS' : gate.mode === 'open' ? 'FAIL' : 'WARN', gate.detail);

  const browser = await chromium.launch({ headless: HEADLESS });
  const perAccount = {};

  for (const key of Object.keys(ACCOUNTS)) {
    const acct = ACCOUNTS[key];
    const t0 = Date.now();
    try {
      const { ctx, page, auth } = await session(browser, key);
      const ms = Date.now() - t0;
      rec(`pre.login.${key}`, `${acct.user} (${acct.role}) authenticates`, 'PASS',
        `landed ${auth.landed} in ${ms}ms`, { landed: auth.landed, ms });

      // Landing page is part of the role contract, not incidental — an Analyst dropped on the
      // external tile view is a scoping defect wearing a routing costume.
      const landedPath = auth.landed.split('?')[0];
      rec(`pre.landing.${key}`, `${acct.user} lands on ${acct.lands}`,
        landedPath.toLowerCase() === acct.lands.toLowerCase() ? 'PASS' : 'WARN',
        `expected ${acct.lands}, got ${landedPath}`);

      perAccount[key] = { landed: auth.landed, loginMs: ms };
      await ctx.close();
    } catch (e) {
      rec(`pre.login.${key}`, `${acct.user} (${acct.role}) authenticates`, 'FAIL', e.message);
      perAccount[key] = { error: e.message };
    }
  }

  await browser.close();
  save({ startedAt, finishedAt: new Date().toISOString(), gate, perAccount });
})().catch((e) => { console.error('FATAL', e); process.exit(1); });
