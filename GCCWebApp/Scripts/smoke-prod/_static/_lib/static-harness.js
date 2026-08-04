// Shared harness for the static deploy-gate checks (`_static/`).
//
// SAFETY CONTRACT — read this before changing anything in here:
//   * These checks are PURE STATIC ANALYSIS. They read source files and run read-only `git`
//     queries. They never write to the project, never start the app, never touch the DB, never
//     make a network call. The ONLY write is a summary JSON under `_results/`.
//   * Nothing here can change program behaviour. A bug in this harness produces a wrong REPORT,
//     never a broken build. That is deliberate — this is hardening-phase tooling.
//
// Why a custom parser instead of a regex sweep: our own fix comments contain the very strings we
// search for. `DocumentController.cs` has `TempData["SuccessMessage"]` inside a `//` comment
// explaining why the write was removed. A naive grep counts that comment as a live writer and the
// check silently passes. Every scan below therefore runs on COMMENT-STRIPPED source.

const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

// _static/_lib -> _static -> smoke-prod -> Scripts -> <project root>
const PROJECT_ROOT = path.resolve(__dirname, '..', '..', '..', '..');
const RESULTS_DIR = path.join(PROJECT_ROOT, 'Scripts', 'smoke-prod', '_results');

// Directories that are never application source. `obj/` and `bin/` hold generated Razor C# that
// duplicates every view — scanning them double-counts every finding.
const SKIP_DIRS = new Set(['obj', 'bin', 'node_modules', '.git', '_results', 'wwwroot', 'TestResults']);

/** Recursively collect files under `dir` whose name matches `extRe`. Returns absolute paths. */
function walk(dir, extRe, acc = []) {
  if (!fs.existsSync(dir)) return acc;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      if (SKIP_DIRS.has(entry.name)) continue;
      walk(path.join(dir, entry.name), extRe, acc);
    } else if (extRe.test(entry.name)) {
      acc.push(path.join(dir, entry.name));
    }
  }
  return acc;
}

/**
 * Strip C# comments while PRESERVING every byte offset and line number (comment bodies become
 * spaces, newlines are kept). Correctly skips over:
 *   - "regular strings" with backslash escapes  -> so "http://x" is not read as a comment
 *   - @"verbatim strings" where "" is an escaped quote
 *   - 'c' char literals
 * Interpolated strings ($"...{x}...") are treated as regular strings; a `//` inside an
 * interpolation hole is vanishingly rare and would only ever cause a MISSED finding, never a
 * false one.
 */
function stripCsharpComments(src) {
  const out = src.split('');
  let i = 0;
  const n = src.length;
  const blank = (from, to) => {
    for (let k = from; k < to && k < n; k++) if (out[k] !== '\n') out[k] = ' ';
  };

  while (i < n) {
    const c = src[i];
    const next = src[i + 1];

    // @"verbatim string"
    if (c === '@' && next === '"') {
      i += 2;
      while (i < n) {
        if (src[i] === '"') {
          if (src[i + 1] === '"') { i += 2; continue; } // "" escape
          i++; break;
        }
        i++;
      }
      continue;
    }
    // "regular string" (incl. $"interpolated")
    if (c === '"') {
      i++;
      while (i < n) {
        if (src[i] === '\\') { i += 2; continue; }
        if (src[i] === '"') { i++; break; }
        if (src[i] === '\n') break; // unterminated - bail rather than eat the file
        i++;
      }
      continue;
    }
    // 'c' char literal
    if (c === "'") {
      i++;
      while (i < n) {
        if (src[i] === '\\') { i += 2; continue; }
        if (src[i] === "'") { i++; break; }
        if (src[i] === '\n') break;
        i++;
      }
      continue;
    }
    // // line comment
    if (c === '/' && next === '/') {
      let j = i;
      while (j < n && src[j] !== '\n') j++;
      blank(i, j);
      i = j;
      continue;
    }
    // /* block comment */
    if (c === '/' && next === '*') {
      const end = src.indexOf('*/', i + 2);
      const j = end === -1 ? n : end + 2;
      blank(i, j);
      i = j;
      continue;
    }
    i++;
  }
  return out.join('');
}

/**
 * Strip Razor (`@* *@`) and HTML (`<!-- -->`) comments from a .cshtml file, preserving offsets.
 * Then strip C# comments too, which covers `//` inside `@{ }` blocks.
 */
function stripRazorComments(src) {
  const out = src.split('');
  const blank = (from, to) => {
    for (let k = from; k < to && k < out.length; k++) if (out[k] !== '\n') out[k] = ' ';
  };
  const blankAll = (open, close) => {
    let idx = 0;
    while ((idx = src.indexOf(open, idx)) !== -1) {
      const end = src.indexOf(close, idx + open.length);
      const stop = end === -1 ? src.length : end + close.length;
      blank(idx, stop);
      idx = stop;
    }
  };
  blankAll('@*', '*@');
  blankAll('<!--', '-->');
  return stripCsharpComments(out.join(''));
}

/** Read a source file and return { abs, rel, raw, code, lines } where `code` is comment-stripped. */
function readSource(abs) {
  const raw = fs.readFileSync(abs, 'utf8');
  const code = abs.endsWith('.cshtml') ? stripRazorComments(raw) : stripCsharpComments(raw);
  return {
    abs,
    rel: path.relative(PROJECT_ROOT, abs).replace(/\\/g, '/'),
    raw,
    code,
    lines: code.split('\n'),
  };
}

/** 1-indexed line number for a character offset. */
function lineOf(code, offset) {
  let line = 1;
  for (let i = 0; i < offset && i < code.length; i++) if (code[i] === '\n') line++;
  return line;
}

/** Load the shared allowlist. Every entry MUST carry a `why` — an unexplained waiver is a lie. */
function loadAllowlist() {
  const file = path.join(__dirname, '..', 'allowlist.json');
  if (!fs.existsSync(file)) return {};
  try {
    return JSON.parse(fs.readFileSync(file, 'utf8'));
  } catch (e) {
    console.error(`  [allowlist] UNREADABLE (${e.message}) - treating as empty, so nothing is silently waived.`);
    return {};
  }
}

/** Read-only git. Returns '' on any failure (not a git checkout, git missing, etc.). */
function git(args) {
  try {
    return execFileSync('git', args, { cwd: PROJECT_ROOT, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
  } catch {
    return '';
  }
}

/** Per-check result recorder. FAIL blocks the gate; WARN and INFO never do. */
function recorder(specName) {
  const findings = [];
  const counts = { pass: 0, fail: 0, warn: 0, info: 0 };
  return {
    pass(msg) { counts.pass++; console.log(`  [PASS] ${msg}`); },
    fail(msg, detail) {
      counts.fail++; findings.push({ level: 'FAIL', msg, ...detail });
      console.log(`  [FAIL] ${msg}`);
      if (detail && detail.at) console.log(`         at ${detail.at}`);
      if (detail && detail.fix) console.log(`         fix: ${detail.fix}`);
    },
    warn(msg, detail) {
      counts.warn++; findings.push({ level: 'WARN', msg, ...detail });
      console.log(`  [WARN] ${msg}`);
      if (detail && detail.at) console.log(`         at ${detail.at}`);
    },
    info(msg, detail) {
      counts.info++; findings.push({ level: 'INFO', msg, ...detail });
      console.log(`  [info] ${msg}`);
    },
    summary() {
      return { spec: specName, ...counts, findings };
    },
  };
}

module.exports = {
  PROJECT_ROOT, RESULTS_DIR,
  walk, readSource, lineOf,
  stripCsharpComments, stripRazorComments,
  loadAllowlist, git, recorder,
};
