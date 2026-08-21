/**
 * DEF-365 verification — is two-factor authentication actually driven by the per-user setting?
 *
 * WHY THIS ASSERTS BOTH DIRECTIONS IN ONE RUN
 * The defect was that login consulted only the global Authentication:Enable2FA flag and never
 * User.TwoFactorEnabled. A test that merely proves "a 2FA-enabled account gets challenged" would
 * have PASSED throughout the entire period the defect existed, because the global flag alone can
 * produce that result. The finding is only meaningful as a pair:
 *
 *   account with TwoFactorEnabled = 0  ->  signs straight in, NO challenge
 *   account with TwoFactorEnabled = 1  ->  challenged, and NO auth cookie until it is answered
 *
 * The second half matters most. A challenge page that is displayed while the session is already
 * authenticated is not a second factor at all — it is a screen the user can navigate away from.
 * So this asserts the absence of GCC.Auth, not the presence of the 2FA page.
 *
 * Usage:  node Scripts/data-ops/2026-08-19-verify-2fa-per-user.js
 *   env:  BASE_URL, GATE_USER, GATE_PASS,
 *         NO2FA_USER / NO2FA_PASS   (an account with TwoFactorEnabled = 0)
 *         WITH2FA_USER / WITH2FA_PASS (an account with TwoFactorEnabled = 1)
 * Exit code 0 = all passed.
 */
const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const BASIC = 'Basic ' + Buffer.from(
  `${process.env.GATE_USER || 'gcc'}:${process.env.GATE_PASS || 'Cert-Test-Access-2026!'}`
).toString('base64');

// Deliberately NOT the `admin` account: its password was changed on 2026-08-19 by the DEF-263
// password-reset verification, so the documented TestPass123! no longer works for it. `admin.uat`
// is an Admin-role account that still carries the documented password.
const NO_2FA   = { user: process.env.NO2FA_USER   || 'admin.uat',             pass: process.env.NO2FA_PASS   || 'TestPass123!' };
// Username `2fatest`, email shawnmwest@gmail.com. It was originally created as
// admin@gccwebapp.com — the same address as the SMTP sending mailbox — and that one address then
// meant two different things with two different passwords. The application password (below) was
// pasted into SmtpSettings:Password by mistake, producing 535 Authentication failed and an hour of
// misdiagnosis. Keep the test account and the sending mailbox on separate addresses.
const WITH_2FA = { user: process.env.WITH2FA_USER || '2fatest',               pass: process.env.WITH2FA_PASS || 'GccTest2026!' };

let pass = 0, fail = 0;
const check = (name, ok, detail) => {
  if (ok) { pass++; console.log(`  PASS  ${name}${detail ? '  — ' + detail : ''}`); }
  else    { fail++; console.log(`  FAIL  ${name}${detail ? '  — ' + detail : ''}`); }
};

/**
 * Signs in and reports what the server did. redirect:'manual' is REQUIRED: the auth cookie is set
 * on the 302 itself, and Node's fetch exposes only the final response's Set-Cookie when following
 * redirects — so 'follow' silently discards it and every login looks like a failure.
 */
async function attemptLogin({ user, pass: password }) {
  const jar = new Map();
  const absorb = r => {
    for (const c of (r.headers.getSetCookie ? r.headers.getSetCookie() : [])) {
      const [p] = c.split(';');
      const i = p.indexOf('=');
      if (i > 0) jar.set(p.slice(0, i).trim(), p.slice(i + 1).trim());
    }
  };
  const cookie = () => [...jar].map(([k, v]) => `${k}=${v}`).join('; ');

  const g = await fetch(`${BASE}/Account/Login`, { headers: { Authorization: BASIC } });
  absorb(g);
  const tok = ((await g.text()).match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/) || [])[1];
  if (!tok) throw new Error('no antiforgery token on the login page');

  const p = await fetch(`${BASE}/Account/Login`, {
    method: 'POST', redirect: 'manual',
    headers: {
      Authorization: BASIC,
      'Content-Type': 'application/x-www-form-urlencoded',
      Cookie: cookie()
    },
    body: new URLSearchParams({ __RequestVerificationToken: tok, Email: user, Password: password })
  });
  absorb(p);

  // Read the error the page actually rendered. Without this the script reports only "no redirect",
  // which conflates THREE different failures that need opposite fixes:
  //   "Invalid email or password"        -> wrong credentials, or the account is LOCKED OUT
  //   "Unable to send verification code" -> 2FA is enforced but SMTP threw
  //   (no error)                         -> something else entirely
  // Reporting them as one symptom cost hours on 2026-08-19: a locked-out account was read as an
  // SMTP fault and sent the investigation through mail transport, config files and app-pool
  // recycles before anyone looked at the actual message on the page.
  const html = await p.text().catch(() => '');
  const strip = m => m[0].replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
  const errors = [
    ...[...html.matchAll(/validation-summary-errors[\s\S]{0,400}?<\/div>/g)].map(strip),
    ...[...html.matchAll(/alert-danger[\s\S]{0,300}?<\/div>/g)].map(strip)
  ].join(' | ').slice(0, 220);

  return {
    status: p.status,
    location: p.headers.get('location') || '',
    authed: jar.has('GCC.Auth'),
    error: errors
  };
}

// NOTE: a failed run leaves failed-login attempts behind. Five in a row LOCK the account
// (Users.FailedLoginAttempts / Users.LockedUntil, ~15 minutes). So repeatedly re-running this
// against a stale password does not just fail — it locks the very account under test, and every
// subsequent run then fails for a DIFFERENT reason than the first one did. If the 2FA account
// starts reporting "Invalid email or password", check LockedUntil before changing anything.

(async () => {
  console.log(`DEF-365 — per-user 2FA enforcement against ${BASE}\n`);

  console.log(`── account WITHOUT 2FA (${NO_2FA.user}) — must NOT be challenged ──`);
  const a = await attemptLogin(NO_2FA);
  console.log(`  login -> ${a.status} ${a.location}${a.error ? '  [' + a.error + ']' : ''}`);
  check('signs in directly', a.authed, a.authed ? 'GCC.Auth issued' : 'no GCC.Auth — credentials rejected? check the password');
  check('not sent to the 2FA page', !/TwoFactorAuthentication/i.test(a.location), a.location || '(no redirect)');

  console.log(`\n── account WITH 2FA (${WITH_2FA.user}) — must be challenged ──`);
  const b = await attemptLogin(WITH_2FA);
  console.log(`  login -> ${b.status} ${b.location}${b.error ? '  [' + b.error + ']' : ''}`);
  // Distinguish "credentials rejected" from "2FA enforced but delivery failed" — they look
  // identical at the HTTP level and need opposite fixes.
  if (/Invalid email or password/i.test(b.error)) {
    check('sent to the 2FA challenge', false,
          'credentials REJECTED before 2FA — wrong password, or the account is locked out ' +
          '(check Users.LockedUntil / FailedLoginAttempts). This is NOT a 2FA or SMTP fault.');
  } else if (/Unable to send verification code/i.test(b.error)) {
    check('sent to the 2FA challenge', false,
          '2FA IS enforced, but sending the code threw — SMTP problem, not a 2FA problem. ' +
          'Check SmtpSettings and logs/stdout for "Failed to send 2FA code".');
  } else {
    check('sent to the 2FA challenge', /TwoFactorAuthentication/i.test(b.location), b.location || '(no redirect)');
  }
  // The load-bearing assertion: a challenge that has already authenticated you is decorative.
  check('NOT authenticated before answering', !b.authed,
        b.authed ? 'GCC.Auth was issued anyway — the challenge is bypassable' : 'no GCC.Auth cookie');

  console.log(`\n${pass} passed, ${fail} failed`);
  console.log(fail === 0
    ? 'PER-USER 2FA IS ENFORCED — the account setting, not just the global flag, decides.'
    : 'DEF-365 NOT fixed on this environment.');
  process.exit(fail === 0 ? 0 : 1);
})();
