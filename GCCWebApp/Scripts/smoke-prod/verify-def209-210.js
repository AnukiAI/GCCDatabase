// Verify DEF-209 + DEF-210 fixes locally.
//
// DEF-209 expectations:
//   - Login error message identical for all failure modes (valid-email-wrong-pw,
//     nonexistent-email, inactive-account, malformed-input)
//   - ForgotPassword response time near-identical for valid vs invalid email
//     (was 3008ms vs 32ms — 100x delta before async fix)
//
// DEF-210 expectations:
//   - 5 wrong-password attempts in a row → 6th attempt is rejected even with
//     CORRECT password during the lockout window
//   - After lockout window passes, fresh attempts work again
//   - Successful login resets the FailedLoginAttempts counter
//
// Uses a disposable test account so we don't lock out a real seeded user.
// Creates user via direct DB INSERT (skipping the admin UI which has its own
// flow). User is cleaned up at end.

const { chromium } = require('playwright');
const { execSync } = require('child_process');
const BASE = 'http://localhost:5101';

const TEST_EMAIL = `def210-${Date.now()}@e2e.local`;
const TEST_PASSWORD = 'TestPass123!';  // matches admin hash we clone
const TEST_USERNAME = `def210user${Date.now() % 100000}`;

function sql(query) {
    return execSync(
        `sqlcmd -S ANUKI01 -d GCCDatabase_Dev -U GCC_DevUser -P "DevP@ss2024!SQL" -I -Q "${query.replace(/"/g, '\\"').replace(/\n/g, ' ')}" -h -1 -W`,
        { encoding: 'utf8' }
    );
}

async function login(browserOrCtx, email, password) {
    // Accept either a browser (creates fresh context) or an existing context
    const ctx = typeof browserOrCtx.newContext === 'function'
        ? await browserOrCtx.newContext()
        : browserOrCtx;
    const ownContext = typeof browserOrCtx.newContext === 'function';
    const page = await ctx.newPage();
    await page.goto(`${BASE}/Account/Login`, { waitUntil: 'domcontentloaded' });
    const token = await page.evaluate(() => document.querySelector('input[name="__RequestVerificationToken"]')?.value);
    const t0 = Date.now();
    const resp = await page.request.fetch(`${BASE}/Account/Login`, {
        method: 'POST',
        maxRedirects: 0,
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        data: `Email=${encodeURIComponent(email)}&Password=${encodeURIComponent(password)}&__RequestVerificationToken=${encodeURIComponent(token || '')}`
    });
    const elapsed = Date.now() - t0;
    const body = await resp.text();
    const errMatch = body.match(/Invalid email or password\.|Login failed[^<]*|User not found|Invalid password|Account inactive/i);
    const errMsg = errMatch ? errMatch[0] : (resp.status() === 302 ? 'SUCCESS_REDIRECT' : 'NO_ERROR');
    await page.close();
    if (ownContext) await ctx.close();
    return { status: resp.status(), location: resp.headers()['location'] || '', errMsg, elapsed };
}

(async () => {
    const browser = await chromium.launch({ headless: true });
    let passes = 0, total = 0;
    const note = (msg, ok) => { total++; if (ok) passes++; console.log(`  ${ok ? '✓' : '✗'} ${msg}`); };

    // Seed test user via SQL — clone the admin's BCrypt hash so the known
    // password "TestPass123!" works for the new account too. Avoids the
    // admin-UI flow which had timing/network-idle issues.
    console.log(`Creating test user ${TEST_EMAIL} (cloning admin hash)…`);
    const TEST_USE_PASSWORD = 'TestPass123!';  // matches admin's password
    const insertSql = `
DECLARE @hash NVARCHAR(255) = (SELECT PasswordHash FROM Users WHERE Email = 'swest@anukiconsulting.com');
INSERT INTO Users (
    Username, Email, PasswordHash, Status, IsActive, IsDeleted,
    IsStaff, IsSuperuser, TwoFactorEnabled,
    RequirePasswordReset, FailedLoginAttempts, TwoFactorMethod,
    IsPhoneVerified, IsEmailVerified, MustChangePassword,
    MaxConcurrentSessions, RequirePasswordChange, MaxPasswordHistory,
    CreatedAt, UpdatedAt
)
VALUES (
    '${TEST_USERNAME}', '${TEST_EMAIL}', @hash, 'active', 1, 0,
    0, 0, 0,
    0, 0, 'Email',
    0, 1, 0,
    3, 0, 5,
    GETUTCDATE(), GETUTCDATE()
);
SELECT Id FROM Users WHERE Email = '${TEST_EMAIL}';
    `;
    const insertResult = sql(insertSql);
    console.log(`  insertResult: ${insertResult.trim()}`);
    const adminCtx = await browser.newContext();  // kept for cleanup-symmetry; unused

    // ====== DEF-209 — generic error message ======
    console.log('\n[DEF-209] Generic login error message');
    const validEmailWrongPw = await login(browser, TEST_EMAIL, 'wrongpassword');
    const nonexistentEmail   = await login(browser, 'nonexistent@e2e.local', 'anything');
    const malformedEmail     = await login(browser, 'not-an-email', 'anything');
    console.log(`  Valid-email + wrong-pw:    "${validEmailWrongPw.errMsg}"`);
    console.log(`  Nonexistent-email:         "${nonexistentEmail.errMsg}"`);
    console.log(`  Malformed-email:           "${malformedEmail.errMsg}"`);
    note('All login error messages identical (no email-enumeration differential)',
        validEmailWrongPw.errMsg === nonexistentEmail.errMsg
        && nonexistentEmail.errMsg === malformedEmail.errMsg
        && validEmailWrongPw.errMsg.includes('Invalid email or password'));

    // ====== DEF-210 — lockout ======
    console.log('\n[DEF-210] Account lockout after 5 wrong attempts');
    // We already used 1 wrong attempt above. Run 4 more.
    for (let i = 2; i <= 5; i++) {
        const r = await login(browser, TEST_EMAIL, `wrongAttempt${i}`);
        console.log(`  attempt ${i}: status=${r.status} err="${r.errMsg}"`);
    }
    // Check DB state — should be locked
    const lockState = sql(`SELECT FailedLoginAttempts, LockedUntil FROM Users WHERE Email = '${TEST_EMAIL}';`);
    console.log(`  DB after 5 failures: ${lockState.trim()}`);
    note('5 failures locked the account (FailedLoginAttempts=5, LockedUntil set)',
        /5\s+\d{4}-\d{2}-\d{2}/.test(lockState));

    // Try CORRECT password during lockout — should still be rejected
    const lockoutCorrect = await login(browser, TEST_EMAIL, TEST_USE_PASSWORD);
    console.log(`  Correct password during lockout: status=${lockoutCorrect.status} err="${lockoutCorrect.errMsg}"`);
    note('Correct password rejected during lockout (status != 302)',
        lockoutCorrect.status !== 302 && lockoutCorrect.errMsg.includes('Invalid email or password'));

    // Force-expire the lockout by setting LockedUntil to past
    sql(`UPDATE Users SET LockedUntil = DATEADD(MINUTE, -1, GETUTCDATE()), FailedLoginAttempts = 5 WHERE Email = '${TEST_EMAIL}';`);
    console.log('  Forced lockout to expire (LockedUntil = now - 1 min)…');

    // Now correct password should succeed
    const postExpiry = await login(browser, TEST_EMAIL, TEST_USE_PASSWORD);
    console.log(`  Correct password post-lockout-expiry: status=${postExpiry.status} location=${postExpiry.location.slice(0, 70)}`);
    note('Correct password works once lockout window passes (status=302 → success)',
        postExpiry.status === 302 && !postExpiry.location.includes('/Account/Login'));

    // Verify counter was reset on successful login
    const postSuccessState = sql(`SELECT FailedLoginAttempts, LockedUntil FROM Users WHERE Email = '${TEST_EMAIL}';`);
    console.log(`  DB after successful login: ${postSuccessState.trim()}`);
    note('Successful login resets FailedLoginAttempts to 0 and clears LockedUntil',
        /^0\s+NULL/.test(postSuccessState.trim()));

    // ====== DEF-209 — ForgotPassword timing ======
    console.log('\n[DEF-209] ForgotPassword timing channel');
    async function fpProbe(email) {
        const page = await browser.newContext().then(c => c.newPage());
        await page.goto(`${BASE}/Account/ForgotPassword`, { waitUntil: 'domcontentloaded' });
        const fpTok = await page.evaluate(() => document.querySelector('input[name="__RequestVerificationToken"]')?.value);
        const t0 = Date.now();
        const r = await page.request.fetch(`${BASE}/Account/ForgotPassword`, {
            method: 'POST', maxRedirects: 0,
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            data: `Email=${encodeURIComponent(email)}&__RequestVerificationToken=${encodeURIComponent(fpTok || '')}`
        });
        const elapsed = Date.now() - t0;
        await page.context().close();
        return { status: r.status(), elapsed };
    }
    const fpValid = await fpProbe(TEST_EMAIL);
    const fpInvalid = await fpProbe('definitely-not-a-real-user@e2e.local');
    console.log(`  Valid email:    status=${fpValid.status} elapsed=${fpValid.elapsed}ms`);
    console.log(`  Invalid email:  status=${fpInvalid.status} elapsed=${fpInvalid.elapsed}ms`);
    note('ForgotPassword timing difference < 500ms (was 100x — 3000ms vs 32ms before fix)',
        Math.abs(fpValid.elapsed - fpInvalid.elapsed) < 500);

    // ====== CLEANUP — hard-delete the test user ======
    console.log('\nCleanup: deleting test user…');
    sql(`DELETE FROM Users WHERE Email = '${TEST_EMAIL}';`);

    console.log(`\n${passes}/${total} pass`);
    await adminCtx.close();
    await browser.close();
    process.exit(passes === total ? 0 : 1);
})().catch(e => { console.error('FATAL', e); process.exit(2); });
