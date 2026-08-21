#!/usr/bin/env node
/**
 * Mojibake guard — DEF-133's prevention recommendation, finally implemented.
 *
 * WHY THIS EXISTS
 * DEF-133: `▶`, `•`, `—`, `→` were baked into 4 Razor views as mojibake and shipped to users on
 * Product Management. DEF-213: the tag-chip close button rendered as literal `Ã—` instead of `×` on   mojibake-doc
 * the cert Edit page, also live. Both had the same cause — a UTF-8 file opened as Windows-1252 and
 * re-saved — and both were caught by a human noticing garbage on screen, which is the expensive way.
 * DEF-133 closed with: "add a pre-commit hook or CI check that greps Views/*.cshtml for the
 * canonical mojibake byte sequences and fails the commit. Cheap to wire up; would have caught this
 * at the moment of corruption."
 *
 * SCOPE — user-facing render surfaces ONLY:
 *   Views/**\/*.cshtml       (DEF-133's original recommendation)
 *   wwwroot/js/**\/*.js      (widened: DEF-213's `&times;` convention lives here, and these strings
 *                             are injected straight into the DOM, so corruption is equally visible)
 *
 * DELIBERATELY NOT SCANNED: docs/tracking/*.md, observations_log.md, and the rest of the docs tree.
 * Those files contain mojibake ON PURPOSE — DEF-133/213/232/307 and the Burt's Bees investigation
 * quote the corrupted sequences as evidence, e.g. "renders as mojibaked `Ã—` instead of `×`".   mojibake-doc
 * "Cleaning" them would make the defects unstateable. Quoted mojibake is documentation; mojibake in
 * a render surface is a bug. This guard only polices the second kind.
 *
 * USAGE
 *   node Scripts/ci/check-encoding.js          # scan; exit 1 on any finding
 *   node Scripts/ci/check-encoding.js --self-test
 *       Plants each known sequence in a temp file, asserts the detector fires on every one, and
 *       asserts a clean file passes. Proves the guard can actually fail — a green check that cannot
 *       go red is worse than no check.
 *
 * Run from the app directory (GCCWebApp/GCCWebApp) or pass --root=<dir>.
 */

'use strict';

const fs = require('fs');
const path = require('path');
const os = require('os');

/**
 * Canonical mojibake signatures. Each is the Windows-1252 misreading of a real UTF-8 sequence, so
 * every entry is 2+ characters — bare lead bytes (`Ã`, `Â`, `â`) alone would false-positive on
 * legitimate accented text. Sourced from the DEF-133 list plus the sequences that actually shipped.
 *
 * Every entry below carries the `mojibake-doc` marker that .githooks/pre-commit honours. This
 * table IS the signature list, so it has to hold the sequences verbatim; without the marker the
 * hook flags its own detector and this guard could never be committed. Same exemption the QC
 * script qc159-live-check/qc-final-batch.js already uses. The marker is per-LINE, so genuine
 * corruption anywhere else in this file still trips the hook.
 */
const SIGNATURES = [
  { seq: 'Ã¢â‚¬', means: 'double-encoded UTF-8 (mojibake of mojibake) — see the Burt’s Bees case' }, // mojibake-doc
  { seq: 'â€™',  means: '’ right single quote' }, // mojibake-doc
  { seq: 'â€œ',  means: '“ left double quote' }, // mojibake-doc
  { seq: 'â€¢',  means: '• bullet' }, // mojibake-doc
  { seq: 'â€¦',  means: '… ellipsis' }, // mojibake-doc
  { seq: 'â€',   means: '— em dash / – en dash / ” right double quote' }, // mojibake-doc
  { seq: 'â„¢',  means: '™ trademark' }, // mojibake-doc
  { seq: 'â„',   means: '™ trademark (partial)' }, // mojibake-doc
  { seq: 'â†',   means: '→ arrow' }, // mojibake-doc
  { seq: 'â–',   means: '▶ / ■ geometric shape' }, // mojibake-doc
  { seq: 'Ã—',   means: '× multiplication sign (DEF-213 — use &times;)' }, // mojibake-doc
  { seq: 'Â®',   means: '® registered' }, // mojibake-doc
  { seq: 'Â©',   means: '© copyright' }, // mojibake-doc
  { seq: 'Â°',   means: '° degree' }, // mojibake-doc
  { seq: 'Â ', means: 'non-breaking space' }, // mojibake-doc
];

const TARGETS = [
  { dir: path.join('Views'), ext: '.cshtml' },
  { dir: path.join('wwwroot', 'js'), ext: '.js' },
];

function walk(dir, ext, out) {
  let entries;
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return out; // directory absent — not this guard's problem
  }
  for (const e of entries) {
    const full = path.join(dir, e.name);
    if (e.isDirectory()) walk(full, ext, out);
    else if (e.isFile() && e.name.endsWith(ext) && !e.name.endsWith('.min' + ext)) out.push(full);
  }
  return out;
}

/** Returns [{file, line, column, seq, means, excerpt}] for every signature hit. */
function scanFile(file) {
  const findings = [];
  const text = fs.readFileSync(file, 'utf8');
  const lines = text.split(/\r?\n/);

  lines.forEach((line, i) => {
    for (const { seq, means } of SIGNATURES) {
      let from = 0;
      for (;;) {
        const at = line.indexOf(seq, from);
        if (at === -1) break;
        findings.push({
          file,
          line: i + 1,
          column: at + 1,
          seq,
          means,
          excerpt: line.slice(Math.max(0, at - 40), at + seq.length + 40).trim(),
        });
        from = at + seq.length;
      }
      // Only report the first signature that matches at a given spot; longer sequences
      // (Ã¢â‚¬) are listed before their shorter prefixes so the most specific wins.   mojibake-doc
      if (findings.some(f => f.line === i + 1)) break;
    }
  });

  return findings;
}

function scan(root) {
  const files = [];
  for (const t of TARGETS) files.push(...walk(path.join(root, t.dir), t.ext, []));
  const findings = [];
  for (const f of files) findings.push(...scanFile(f));
  return { files, findings };
}

function selfTest() {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'encguard-'));
  let failures = 0;

  for (const { seq, means } of SIGNATURES) {
    const f = path.join(tmp, 'planted.cshtml');
    fs.writeFileSync(f, `<div>before ${seq} after</div>\n`, 'utf8');
    const hits = scanFile(f);
    if (hits.length === 0) {
      console.error(`  SELF-TEST FAIL: detector did not fire on ${JSON.stringify(seq)} (${means})`);
      failures++;
    }
  }

  const clean = path.join(tmp, 'clean.cshtml');
  fs.writeFileSync(clean, '<div>— • → ▶ × ® proper UTF-8</div>\n', 'utf8');
  const cleanHits = scanFile(clean);
  if (cleanHits.length !== 0) {
    console.error(`  SELF-TEST FAIL: detector fired on a CLEAN file: ${JSON.stringify(cleanHits)}`);
    failures++;
  }

  fs.rmSync(tmp, { recursive: true, force: true });

  if (failures) {
    console.error(`\nSELF-TEST FAILED (${failures} problem(s)) — the guard is not trustworthy.`);
    return 1;
  }
  console.log(`Self-test passed: all ${SIGNATURES.length} signatures detected, clean file passes.`);
  return 0;
}

function main() {
  const args = process.argv.slice(2);
  const rootArg = args.find(a => a.startsWith('--root='));
  const root = rootArg ? rootArg.slice('--root='.length) : process.cwd();

  if (args.includes('--self-test')) process.exit(selfTest());

  const { files, findings } = scan(root);

  if (findings.length === 0) {
    console.log(`Encoding guard: OK — ${files.length} render-surface files scanned, no mojibake.`);
    console.log('  (Views/**/*.cshtml + wwwroot/js/**/*.js. Tracking docs are excluded by design —');
    console.log('   they quote mojibake as evidence for DEF-133/213/232/307.)');
    process.exit(0);
  }

  console.error(`Encoding guard: FAILED — ${findings.length} mojibake sequence(s) in ${new Set(findings.map(f => f.file)).size} file(s).\n`);
  for (const f of findings) {
    console.error(`  ${f.file}:${f.line}:${f.column}`);
    console.error(`    found ${JSON.stringify(f.seq)} — should be ${f.means}`);
    console.error(`    ${f.excerpt}`);
    console.error('');
  }
  console.error('These are user-facing render surfaces: this text reaches the browser as-is.');
  console.error('Fix by re-saving the file as UTF-8, or use an HTML entity (&times;, &bull;, &mdash;).');
  console.error('See DEF-133 and DEF-213 in docs/tracking/defect-register.md.');
  process.exit(1);
}

main();
