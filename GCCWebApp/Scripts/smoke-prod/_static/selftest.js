// SELFTEST — proves the four checks still DETECT things, using fixtures with known answers.
//
// WHY THIS EXISTS (learned the hard way while building these, twice in one sitting)
//   1. `redirect-targets`-style arg parsing swallowed the closing paren, so every action name
//      failed to parse and all 58 writes reported "unresolved". Output: a clean-looking report.
//   2. The register parser used `(.*)$` on a CRLF file. In JavaScript `\r` is a line terminator,
//      so `.` does not match it and `$` never anchors — the parser matched 0 of 247 entries and
//      printed "no entry claims fixed-but-undeployed". Output: a clean-looking report.
//
// In both cases a broken check and a healthy codebase produced the SAME output. That is the most
// dangerous failure a gate can have: it does not cry wolf, it goes quiet. This selftest makes the
// difference observable. Run it before trusting a green gate, and after touching any check.
//
// Fixtures are strings in this file - no temp files, nothing written to disk.

const assert = require('assert');
const { stripCsharpComments, stripRazorComments } = require('./_lib/static-harness');
const { parseRegister, filesIn, PENDING_RE } = require('./unshipped-fixes');

let pass = 0, fail = 0;
function check(name, fn) {
  try { fn(); pass++; console.log(`  [ok]   ${name}`); }
  catch (e) { fail++; console.log(`  [FAIL] ${name}\n         ${e.message}`); }
}

console.log('\n--- selftest: do the checks still detect anything? ------------------------');

// ---- comment stripping -------------------------------------------------------------------
check('C# line comments are blanked (our own fix comments must not count as code)', () => {
  const src = 'a();\n// TempData["SuccessMessage"] = "x";\nb();';
  const out = stripCsharpComments(src);
  assert.ok(!out.includes('SuccessMessage'), 'commented-out TempData key survived stripping');
  assert.strictEqual(out.split('\n').length, 3, 'line count changed - offsets would drift');
});

check('a // inside a string literal is NOT treated as a comment', () => {
  const src = 'var u = "http://example.com/x"; TempData["Keep"] = 1;';
  const out = stripCsharpComments(src);
  assert.ok(out.includes('TempData["Keep"]'), 'code after a URL string was wrongly blanked');
});

check('verbatim strings with "" escapes do not desynchronise the parser', () => {
  const src = 'var s = @"a ""b"" c"; TempData["After"] = 1;';
  assert.ok(stripCsharpComments(src).includes('TempData["After"]'));
});

check('Razor @* *@ comments are blanked', () => {
  const src = '@* TempData["Ghost"] is explained here *@\n<p>@TempData["Real"]</p>';
  const out = stripRazorComments(src);
  assert.ok(!out.includes('Ghost'), 'Razor comment survived');
  assert.ok(out.includes('Real'), 'live Razor code was blanked');
});

// ---- register parsing (the CRLF trap) ----------------------------------------------------
const CRLF_FIXTURE = [
  '# Register',
  '',
  '### DEF-901 — A thing that broke',
  '',
  '**Date:** 2026-08-03 | **Status:** Fixed (`Controllers/FooController.cs`) - working tree, pending next deploy bundle | **Severity:** Minor',
  '',
  'Body text.',
  '',
  '### DEF-902 — Another thing',
  '',
  '**Date:** 2026-08-03 | **Status:** Fixed and deployed 2026-08-01 | **Severity:** Minor',
  '',
].join('\r\n');

check('register parser finds entries in a CRLF file (the trap that silently zeroed this check)', () => {
  const e = parseRegister(CRLF_FIXTURE);
  assert.strictEqual(e.length, 2, `expected 2 entries, got ${e.length}`);
  assert.strictEqual(e[0].id, 'DEF-901');
  assert.ok(e[0].title.startsWith('A thing'), `title not stripped of separator: "${e[0].title}"`);
});

check('register parser also handles LF-only files', () => {
  assert.strictEqual(parseRegister(CRLF_FIXTURE.replace(/\r\n/g, '\n')).length, 2);
});

check('pending status is recognised, deployed status is not', () => {
  const e = parseRegister(CRLF_FIXTURE);
  assert.ok(PENDING_RE.test(e[0].status), 'did not recognise "working tree, pending next deploy"');
  assert.ok(!PENDING_RE.test(e[1].status), 'wrongly flagged a deployed entry as pending');
});

check('file extraction takes repo-rooted paths and ignores bare filenames', () => {
  const got = filesIn('Fixed (`Controllers/FooController.cs`, `wwwroot/js/bar.js`) and see `main.css`');
  assert.deepStrictEqual(got.sort(), ['Controllers/FooController.cs', 'wwwroot/js/bar.js'],
    `bare filenames must be ignored (they caused 3 false FAILs); got ${JSON.stringify(got)}`);
});

// ---- the checks still fire on the real tree ----------------------------------------------
// Not asserting specific findings (those change as defects are fixed) - asserting that each check
// still LOOKS at a non-trivial amount of code. A check that scans 0 files always "passes".
check('checks still scan a non-trivial codebase (guards against a silent zero-scan)', () => {
  const { buildIndex } = require('./_lib/mvc-index');
  const idx = buildIndex();
  assert.ok(idx.controllers.size >= 10, `only ${idx.controllers.size} controllers indexed - parser likely broken`);
  assert.ok(idx.viewFiles.size >= 20, `only ${idx.viewFiles.size} views found - walker likely broken`);
  let actions = 0;
  for (const c of idx.controllers.values()) actions += c.actions.size;
  assert.ok(actions >= 100, `only ${actions} actions parsed - METHOD_RE likely broken`);
});

console.log(`  => ${pass} ok / ${fail} failed`);
if (fail > 0) {
  console.log('  SELFTEST FAILED - do NOT trust a green gate until this passes.');
  process.exit(1);
}
