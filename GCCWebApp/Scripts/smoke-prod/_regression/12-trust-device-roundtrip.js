/**
 * DEF-385 round trip — the client's exact complaint, end to end:
 *
 *   1. sign in  -> challenged for 2FA
 *   2. verify the code WITH "Trust this device for 30 days" ticked
 *   3. sign out, sign in again from the same cookie jar
 *   4. must land signed in, NOT on the 2FA page
 *
 * Then the security half:
 *   5. a DIFFERENT account presenting the same cookie is still challenged
 *
 * This is the layer the defect lived at. The service unit tests pass on the old code too, because
 * the old code never called the service at all — the checkbox only widened the auth-cookie lifetime
 * via `rememberMe || trustDevice`. Only a request-level test can see that.
 *
 * LOCALHOST + DEV DATABASE ONLY. The caller (13-trust-device-setup.ps1) enrols a scratch TOTP
 * secret on a dev account and restores it afterwards; the `2fatest` fixture is never touched.
 *
 * Usage: node 12-trust-device-roundtrip.js <baseUrl> <email> <password> <base32Secret> [otherEmail] [otherPassword]
 */
const crypto = require('crypto');

const [, , BASE, EMAIL, PASSWORD, SECRET, OTHER_EMAIL, OTHER_PASSWORD] = process.argv;

if (!BASE || !EMAIL || !PASSWORD || !SECRET) {
    console.error('usage: node 12-trust-device-roundtrip.js <baseUrl> <email> <password> <base32Secret> [otherEmail] [otherPassword]');
    process.exit(2);
}
const REMOTE = !/^https?:\/\/(localhost|127\.0\.0\.1)/.test(BASE);
if (REMOTE && !process.env.ALLOW_REMOTE) {
    console.error(`refusing to run against ${BASE} without ALLOW_REMOTE=1`);
    process.exit(2);
}

// --- TOTP (RFC 6238), matching EnhancedTwoFactorService.GenerateTotpCode ------------------
function base32Decode(s) {
    const alpha = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';
    let bits = 0, value = 0;
    const out = [];
    for (const ch of s.replace(/=+$/, '').toUpperCase()) {
        const idx = alpha.indexOf(ch);
        if (idx === -1) continue;
        value = (value << 5) | idx;
        bits += 5;
        if (bits >= 8) { out.push((value >>> (bits - 8)) & 0xff); bits -= 8; }
    }
    return Buffer.from(out);
}

function totp(secretB32, atMs = Date.now()) {
    const step = Math.floor(atMs / 1000 / 30);
    const buf = Buffer.alloc(8);
    buf.writeBigInt64BE(BigInt(step));
    const hmac = crypto.createHmac('sha1', base32Decode(secretB32)).update(buf).digest();
    const off = hmac[hmac.length - 1] & 0x0f;
    const bin = ((hmac[off] & 0x7f) << 24) | ((hmac[off + 1] & 0xff) << 16) |
                ((hmac[off + 2] & 0xff) << 8) | (hmac[off + 3] & 0xff);
    return String(bin % 1000000).padStart(6, '0');
}

// --- cookie jar ---------------------------------------------------------------------------
function newJar() { return new Map(); }
const header = (jar) => [...jar.entries()].map(([k, v]) => `${k}=${v}`).join('; ');

function absorb(jar, res) {
    const raw = res.headers.getSetCookie ? res.headers.getSetCookie() : [];
    for (const c of raw) {
        const [pair] = c.split(';');
        const i = pair.indexOf('=');
        const name = pair.slice(0, i).trim();
        const value = pair.slice(i + 1).trim();
        if (value === '' || /expires=Thu, 01 Jan 1970/i.test(c)) jar.delete(name);
        else jar.set(name, value);
    }
}

async function req(jar, path, opts = {}) {
    const res = await fetch(BASE + path, {
        method: opts.method || 'GET',
        headers: {
            cookie: header(jar),
            ...(opts.form ? { 'content-type': 'application/x-www-form-urlencoded' } : {}),
        },
        body: opts.form ? new URLSearchParams(opts.form).toString() : undefined,
        redirect: 'manual',
    });
    absorb(jar, res);
    return res;
}

const rvtFrom = (html) => (html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/) || [])[1];

async function signIn(jar, email, password) {
    const page = await req(jar, '/Account/Login');
    const rvt = rvtFrom(await page.text());
    const res = await req(jar, '/Account/Login', {
        method: 'POST',
        form: { __RequestVerificationToken: rvt, Email: email, Password: password, RememberMe: 'false' },
    });
    return res;
}

/** Follows one redirect and reports whether we ended up on the 2FA challenge. */
async function landedOn2FA(jar, res) {
    let location = res.headers.get('location') || '';
    if (/TwoFactorAuthentication/i.test(location)) return true;
    if (res.status >= 300 && res.status < 400 && location) {
        const follow = await req(jar, location.startsWith('http') ? new URL(location).pathname : location);
        const body = await follow.text();
        return /Enter 6-digit verification code|Verify Code/i.test(body);
    }
    const body = await res.text();
    return /Enter 6-digit verification code|Verify Code/i.test(body);
}

/**
 * Remote runs sit behind the test-environment gate. Every jar needs its own gate cookie, otherwise
 * the login POST silently returns the gate page and reads as a bad password.
 */
async function passGate(jar) {
    if (!REMOTE) return;
    const page = await req(jar, '/__gate?returnUrl=%2F');
    const html = await page.text();
    const m = html.match(/name="formToken"\s+value="([^"]+)"/);
    if (!m) throw new Error('could not read the gate formToken');
    const res = await req(jar, '/__gate', {
        method: 'POST',
        form: { password: process.env.GATE_PASS || 'Cert-Test-Access-2026!', formToken: m[1], returnUrl: '/' },
    });
    if (res.status >= 400) throw new Error(`gate rejected the password (HTTP ${res.status})`);
}

(async () => {
    const failures = [];
    const jar = newJar();
    await passGate(jar);

    // 1. first sign-in -> must be challenged
    let res = await signIn(jar, EMAIL, PASSWORD);
    const challenged1 = await landedOn2FA(jar, res);
    console.log(`1. first sign-in challenged for 2FA: ${challenged1}  ${challenged1 ? 'OK' : '*** expected a challenge ***'}`);
    if (!challenged1) failures.push('first sign-in was not challenged for 2FA — cannot test the trust flow');

    // 2. verify with "trust this device" ticked
    const page = await req(jar, '/Account/TwoFactorAuthentication');
    const rvt = rvtFrom(await page.text());
    const code = totp(SECRET);
    res = await req(jar, '/Account/Verify2FA', {
        method: 'POST',
        form: { __RequestVerificationToken: rvt, authCode: code, TrustDevice: 'true' },
    });
    const gotCookie = jar.has('GCC.TrustedDevice');
    console.log(`2. verified with trust ticked -> GCC.TrustedDevice issued: ${gotCookie}  ${gotCookie ? 'OK' : '*** no cookie ***'}`);
    if (!gotCookie) failures.push('no GCC.TrustedDevice cookie was issued after a successful 2FA verification');

    // 3-4. drop the auth cookies, keep the trust cookie, sign in again
    for (const name of [...jar.keys()]) {
        if (name !== 'GCC.TrustedDevice') jar.delete(name);
    }
    res = await signIn(jar, EMAIL, PASSWORD);
    const challenged2 = await landedOn2FA(jar, res);
    console.log(`3. second sign-in challenged for 2FA: ${challenged2}  ${challenged2 ? '*** STILL PROMPTING — the defect ***' : 'OK (skipped)'}`);
    if (challenged2) failures.push('second sign-in was STILL challenged — trust did not suppress the 2FA step');

    // 5. the same cookie must not help a different account
    if (OTHER_EMAIL && OTHER_PASSWORD) {
        const jar2 = newJar();
        await passGate(jar2);
        jar2.set('GCC.TrustedDevice', jar.get('GCC.TrustedDevice'));
        const r = await signIn(jar2, OTHER_EMAIL, OTHER_PASSWORD);
        const otherChallenged = await landedOn2FA(jar2, r);
        console.log(`4. other account with the same cookie challenged: ${otherChallenged}  ${otherChallenged ? 'OK' : '*** LEAKED — trust crossed accounts ***'}`);
        if (!otherChallenged) failures.push('a device trusted for one user skipped 2FA for a DIFFERENT user');
    } else {
        console.log('4. cross-account check skipped (no second account supplied)');
    }

    console.log('');
    if (failures.length) {
        console.log('FAIL');
        failures.forEach(f => console.log('  - ' + f));
        process.exit(1);
    }
    console.log('PASS — trust is granted, honoured on the next sign-in, and scoped to one account');
})().catch(e => { console.error(e); process.exit(1); });
