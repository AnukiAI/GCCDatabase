/**
 * DEF-384: the 2FA recovery-code "Copy All" / "Download as Text" buttons emitted HALF of each code.
 *
 *   displayed (Razor, C#): code.Substring(0, 4) + '-' + code.Substring(4, 4)  -> "A1B2-C3D4"
 *   copied    (JS)       : code.substring(0, 4) + '-' + code.substring(4, 4)  -> "A1B2-"
 *
 * C# Substring is (startIndex, LENGTH). JavaScript substring is (startIndex, END). The two calls
 * look identical and mean different things, so the screen showed the whole code while the clipboard
 * and the downloaded file both silently lost the second half. A user storing the downloaded file as
 * their account-recovery backup would have been locked out.
 *
 * This test EXTRACTS the formatCode function from the view and EXECUTES it, rather than grepping
 * the source for a substring — a source-shape assertion would pass on any text that merely looks
 * right, and this is a behavioural defect (the DEF-369 lesson).
 *
 * Usage: node Scripts/smoke-prod/_regression/10-recovery-code-format.js
 */
const fs = require('fs');
const path = require('path');

const VIEW = path.join(__dirname, '..', '..', '..', 'Views', 'Account', 'ShowRecoveryCodes.cshtml');

const src = fs.readFileSync(VIEW, 'utf8');

// Pull the real function out of the view so we test what ships, not a copy of it.
const match = src.match(/function formatCode\(code\)\s*\{[\s\S]*?\n\s*\}/);
if (!match) {
    console.error('FAIL — could not find formatCode() in ' + VIEW);
    process.exit(1);
}
// eslint-disable-next-line no-new-func
const formatCode = new Function(match[0] + '; return formatCode;')();

// Codes are 8 chars from an ambiguity-free charset (EnhancedTwoFactorService.GenerateRecoveryCode).
const cases = [
    ['A1B2C3D4', 'A1B2-C3D4'],
    ['ZZZZ9999', 'ZZZZ-9999'],
    ['ABCDEFGH', 'ABCD-EFGH'],
];

const failures = [];
for (const [input, expected] of cases) {
    const got = formatCode(input);
    const ok = got === expected;
    console.log(`  formatCode(${input}) -> ${got}  ${ok ? 'OK' : `*** expected ${expected} ***`}`);
    if (!ok) failures.push(`${input}: got "${got}", expected "${expected}"`);
}

// The whole code must survive: no character of the input may be dropped.
for (const [input] of cases) {
    const roundTrip = formatCode(input).replace(/-/g, '');
    if (roundTrip !== input) {
        failures.push(`${input}: round-trip lost characters ("${roundTrip}")`);
    }
}
console.log(`  round-trip (strip dashes == original): ${failures.length ? 'FAIL' : 'OK'}`);

console.log('');
if (failures.length) {
    console.log('FAIL');
    failures.forEach(f => console.log('  - ' + f));
    process.exit(1);
}
console.log('PASS — copied/downloaded recovery codes are complete');
