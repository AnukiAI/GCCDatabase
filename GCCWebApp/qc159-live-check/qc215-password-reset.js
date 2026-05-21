// DEF-215 — Password reset URL double-decode corrupted base64 tokens with `+`.
//
// Localhost-only verification. Workflow:
//   1. Pick a known raw token that contains `+`, `/`, `=` (would be corrupted pre-fix).
//   2. Write SHA256(rawToken) into Users.PasswordResetToken for a test user
//      (jane.analyst — admin/Shawn account is deliberately avoided).
//   3. URL-encode the token (so `+` becomes `%2B`) into the reset link.
//   4. GET the reset URL. Response body must NOT contain "invalid or has expired".
//   5. POST the form with a new password. Response must NOT contain "invalid or has expired";
//      should redirect to success or render success page.
//   6. Restore: blank the token + restore the original password hash.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { execSync } = require('child_process');

const OUT = __dirname;
const BASE = 'http://localhost:5101';
const TEST_EMAIL = 'jane.analyst@anukiconsulting.com';
const RAW_TOKEN = 'AAAA+BBBB+CCCC+DDDD+EEEE+FFFF+GGGG+HHHH=';  // exercises + and = (the bug chars)
// Controller uses Convert.ToBase64String(SHA256(...)) — base64, NOT hex
const TOKEN_HASH = crypto.createHash('sha256').update(RAW_TOKEN, 'utf8').digest('base64');
const NEW_PASSWORD = 'QC215Test!Pa55word'; // temporary; restored at cleanup

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

function runSql(sql) {
  const cs = `Server=ANUKI01;Database=GCCDatabase_Dev;User Id=GCC_DevUser;Password=DevP@ss2024!SQL;TrustServerCertificate=True;`;
  const safeSql = sql.replace(/'/g, "''");
  const cmd =
    `$conn = New-Object System.Data.SqlClient.SqlConnection '${cs}';` +
    ` $conn.Open();` +
    ` $cmd = $conn.CreateCommand();` +
    ` $cmd.CommandText = '${safeSql}';` +
    ` $r = $cmd.ExecuteScalar();` +
    ` $conn.Close();` +
    ` Write-Output $r;`;
  return execSync(`powershell -NoProfile -Command "${cmd.replace(/"/g, '\\"')}"`, { stdio: 'pipe' }).toString().trim();
}

(async () => {
  const browser = await chromium.launch({ headless: true });

  console.log(`\nDEF-215 password-reset verification — LOCAL (${BASE})\n`);

  // Capture original password hash for restore
  const originalHash = runSql(`SELECT PasswordHash FROM Users WHERE Email = '${TEST_EMAIL}'`);
  console.log(`SETUP: original PasswordHash for ${TEST_EMAIL} captured (${originalHash.length} chars)`);

  try {
    // SETUP — write the token hash + expiry
    runSql(
      `UPDATE Users SET PasswordResetToken = '${TOKEN_HASH}', PasswordResetTokenExpiry = DATEADD(HOUR, 1, GETUTCDATE()) WHERE Email = '${TEST_EMAIL}';`
    );
    console.log(`SETUP: PasswordResetToken set to SHA256(rawToken); token contains '+' and '=' chars`);
    console.log(`  raw token: ${RAW_TOKEN}`);

    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });

    // STEP 1 — GET reset URL with URL-encoded token (so `+` becomes %2B in the URL)
    console.log('\n--- T1: GET /Account/ResetPasswordWithToken with token containing `+` ---');
    const url = `${BASE}/Account/ResetPasswordWithToken?token=${encodeURIComponent(RAW_TOKEN)}&email=${encodeURIComponent(TEST_EMAIL)}`;
    console.log(`  URL: ${url}`);
    const getResp = await ctx.request.get(url);
    const getBody = await getResp.text();
    fs.writeFileSync(path.join(OUT, 'qc215-get-response.html'), getBody);
    const showsInvalid = /invalid or has expired/i.test(getBody);
    const hasResetForm = /<form[^>]*action="[^"]*ResetPasswordWithToken/i.test(getBody) || /name="NewPassword"/i.test(getBody);
    console.log(`  HTTP ${getResp.status()}, body ${getBody.length}B, shows-invalid=${showsInvalid}, has-reset-form=${hasResetForm}`);
    record('QC215-GET-not-invalid', 'GET handler does NOT show "invalid or has expired" for `+`-containing token',
      !showsInvalid ? 'PASS' : 'FAIL',
      `body length=${getBody.length}, showsInvalid=${showsInvalid}`);
    record('QC215-GET-form-rendered', 'GET handler renders the reset-password form',
      hasResetForm ? 'PASS' : 'FAIL',
      `hasResetForm=${hasResetForm}`);

    // STEP 2 — Extract anti-forgery token + hidden Token field, then POST
    if (!showsInvalid && hasResetForm) {
      console.log('\n--- T2: POST new password (round-trip) ---');
      // Razor HTML-encodes `+` as `&#x2B;` (and `=` as `&#x3D;`) in rendered input values.
      // Browsers decode these natively; raw HTTP POST doesn't. Decode here so the round-trip
      // mirrors what a real browser sends.
      const htmlDecode = s => s
        .replace(/&#x([0-9a-fA-F]+);/g, (_, h) => String.fromCharCode(parseInt(h, 16)))
        .replace(/&#(\d+);/g, (_, d) => String.fromCharCode(parseInt(d, 10)))
        .replace(/&amp;/g, '&')
        .replace(/&quot;/g, '"')
        .replace(/&lt;/g, '<')
        .replace(/&gt;/g, '>');
      const antiToken = (getBody.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/) || [])[1];
      const rawHiddenToken = (getBody.match(/name="Token"[^>]*value="([^"]+)"/) || [])[1] || RAW_TOKEN;
      const hiddenToken = htmlDecode(rawHiddenToken);
      const hiddenEmail = htmlDecode((getBody.match(/name="Email"[^>]*value="([^"]+)"/) || [])[1] || TEST_EMAIL);
      console.log(`  anti-forgery captured: ${antiToken ? 'yes' : 'no'}, hidden Token raw=${rawHiddenToken.length}ch decoded=${hiddenToken.length}ch`);

      const postResp = await ctx.request.post(`${BASE}/Account/ResetPasswordWithToken`, {
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        data: new URLSearchParams({
          Email: hiddenEmail,
          Token: hiddenToken,
          Password: NEW_PASSWORD,            // ViewModel field is "Password" not "NewPassword"
          ConfirmPassword: NEW_PASSWORD,
          __RequestVerificationToken: antiToken || '',
        }).toString(),
        maxRedirects: 0,
      });
      const postStatus = postResp.status();
      const postBody = await postResp.text();
      fs.writeFileSync(path.join(OUT, 'qc215-post-response.html'), postBody);
      const postShowsInvalid = /invalid or has expired/i.test(postBody);
      console.log(`  HTTP ${postStatus}, body ${postBody.length}B, shows-invalid=${postShowsInvalid}`);
      // Success is a 302 redirect OR a body containing a success indicator
      const looksSuccess = postStatus === 302 || /password.*reset.*success|successfully reset|please log in/i.test(postBody);
      record('QC215-POST-roundtrip-not-invalid',
        'POST round-trip does NOT show "invalid or has expired" (hidden-field bug fixed)',
        !postShowsInvalid ? 'PASS' : 'FAIL',
        `postShowsInvalid=${postShowsInvalid} status=${postStatus}`);
      record('QC215-POST-success',
        'POST result indicates successful password reset (302 redirect or success message)',
        looksSuccess ? 'PASS' : 'WARN',
        `status=${postStatus}, body sample="${postBody.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim().slice(0, 150)}"`);

      // Verify the password actually changed: DB hash differs from original
      const newHashInDb = runSql(`SELECT PasswordHash FROM Users WHERE Email = '${TEST_EMAIL}'`);
      record('QC215-PWHASH-changed',
        'PasswordHash actually changed in DB after successful reset',
        newHashInDb !== originalHash ? 'PASS' : 'FAIL',
        `hash differs=${newHashInDb !== originalHash}`);
    }

    await ctx.close();

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    // RESTORE: clear token + put back original PasswordHash
    if (originalHash && originalHash.length > 0) {
      runSql(`UPDATE Users SET PasswordHash = '${originalHash}', PasswordResetToken = NULL, PasswordResetTokenExpiry = NULL WHERE Email = '${TEST_EMAIL}';`);
      console.log('\nCLEANUP: restored original PasswordHash + cleared reset token');
    }
    fs.writeFileSync(path.join(OUT, `qc215-results.json`), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
})();
