// POST-DEPLOY CLOSE-OUT — flip "Pending deploy" to "Deployed" in the shipped log and the defect
// register, after a bundle has actually been uploaded.
//
// THE PROBLEM THIS EXISTS TO FIX
// `pre-deploy-agent-checklist.md` runs Gates 0-10 and every one of them fires BEFORE the upload.
// Nothing fires after. So "Pending deploy" is a state the process can enter and has no way to
// leave: on 2026-08-04 every single row in shipped-2026-07.md (~50 of them) still read "Pending
// deploy", months after shipping, and check 4 (`_static/unshipped-fixes.js`) reported 54 fixed
// defects as "NOT yet in the client's hands". The code had shipped; only the paperwork had not.
// That understates delivery to the client — the exact opposite of what the shipped log is for.
//
// THE TRAP THIS SCRIPT IS BUILT AROUND
// Flipping the words "Pending deploy" -> "Deployed" is NOT enough, and a script that did only that
// would look like it worked while changing nothing. Check 4 decides "did this ship?" with TWO
// regexes, and both must stop matching:
//
//   ROW_NOT_SHIPPED_RE  /not yet in a built zip|pending deploy|pending the next deploy|not yet deployed/i
//   PENDING_RE          /working tree|pending (?:the )?next deploy|pending deploy|not yet deployed|undeployed/i
//
// A typical shipped-log row reads:
//     | **Pending deploy 2026-07-31** (code only; **NOT yet in a built zip - folds into the next rebuild**) |
// Rewrite only the first bold span and the row STILL contains "not yet in a built zip", so check 4
// still refuses to count it and the roll-up never moves. Likewise a register line reading
// "Fixed (...) - working tree, **pending next deploy bundle**" needs BOTH "working tree" and
// "pending next deploy" gone, not just one.
//
// So this script does not trust its own edit. Every rewrite is re-tested against the imported
// regexes before it is written, and anything that would still read as pending is REFUSED and
// reported as NEEDS-HUMAN rather than silently written. A no-op cannot masquerade as a success.
//
// SINGLE SOURCE OF TRUTH
// `parseRegister`, `PENDING_RE` and `shippedIds` are imported from `_static/unshipped-fixes.js`
// rather than re-implemented. If check 4's definition of "pending" changes, this follows it. Two
// private copies of that definition drifting apart is how the register got into this state.
//
// EDITS ARE SURGICAL. Only the pending/bundle-status clause is touched. Titles, client-facing
// wording and every other sentence are preserved byte-for-byte.
//
// Run:
//   node Scripts/deploy/close-out.js --date=2026-08-04 --all-pending            (dry run, default)
//   node Scripts/deploy/close-out.js --date=2026-08-04 --all-pending --apply
//   node Scripts/deploy/close-out.js --date=2026-08-04 --ids=DEF-337,DEF-338 --apply
//   node Scripts/deploy/close-out.js --date=2026-08-04 --all-pending --bundle=20260804-1705 --apply
//   ... add --include-stale to also clear register lines whose id is ALREADY in a shipped log
//       (check 4 reports these as "stale text, not a deploy risk" - 96 of them on 2026-08-04).
//
// Exit: 0 = clean (or dry run) · 1 = some entries needed a human · 2 = bad usage.

const fs = require('fs');
const path = require('path');
const { parseRegister, shippedIds, PENDING_RE } = require('../smoke-prod/_static/unshipped-fixes');

const PROJECT_ROOT = path.resolve(__dirname, '..', '..');
// CLOSEOUT_TRACKING_DIR exists so `--apply` can be exercised for real against a throwaway copy of
// the docs. A tool that rewrites the client-facing record should not have "we only ever dry-ran it"
// as its test evidence — the write path is the part that can corrupt something.
const TRACKING = process.env.CLOSEOUT_TRACKING_DIR
  ? path.resolve(process.env.CLOSEOUT_TRACKING_DIR)
  : path.join(PROJECT_ROOT, 'docs', 'tracking');
const REGISTER = path.join(TRACKING, 'defect-register.md');

// Kept in step with ROW_NOT_SHIPPED_RE in unshipped-fixes.js. Not exported there, so it is restated
// here and asserted against the real thing in selfCheck() below - if that file's copy changes and
// this one does not, the assertion fires instead of the two drifting silently.
const ROW_NOT_SHIPPED_RE = /not yet in a built zip|pending deploy|pending the next deploy|not yet deployed/i;

// ---------------------------------------------------------------------------
// args
// ---------------------------------------------------------------------------
function parseArgs(argv) {
  const a = { ids: null, date: null, bundle: null, apply: false, allPending: false, includeStale: false, keepRowDate: false };
  for (const arg of argv) {
    if (arg === '--apply') a.apply = true;
    else if (arg === '--all-pending') a.allPending = true;
    else if (arg === '--include-stale') a.includeStale = true;
    else if (arg === '--keep-row-date') a.keepRowDate = true;
    else if (arg.startsWith('--date=')) a.date = arg.slice(7).trim();
    else if (arg.startsWith('--bundle=')) a.bundle = arg.slice(9).trim();
    else if (arg.startsWith('--ids=')) {
      a.ids = new Set(arg.slice(6).split(',').map(s => s.trim().toUpperCase()).filter(Boolean));
    } else {
      console.error(`unrecognised argument: ${arg}`);
      return null;
    }
  }
  return a;
}

function usage(msg) {
  if (msg) console.error(`\n✖ ${msg}`);
  console.error(`
Usage: node Scripts/deploy/close-out.js --date=YYYY-MM-DD (--all-pending | --ids=DEF-1,CR-2) [options]

  --date=YYYY-MM-DD   REQUIRED. The date the bundle was actually uploaded. Not defaulted to today
                      on purpose: a wrong date in the client's receipts is the problem, not the fix.
  --all-pending       Act on every register entry check 4 calls genuinely undeployed.
  --ids=A,B,C         Act on these ids only.
  --bundle=ID         Record the bundle id in the shipped-log row instead of dropping the clause.
  --include-stale     Also clear register lines whose id is already in a shipped log (stale text).
  --keep-row-date     BACKFILL mode: stamp each row with ITS OWN stated pending date, not --date.
                      Use for history. Without it, a row whose own date is more than ${STALE_ROW_DAYS} days
                      before --date is refused rather than restamped.
  --apply             Write the files. WITHOUT THIS THE SCRIPT ONLY PREVIEWS.
`);
  return 2;
}

// ---------------------------------------------------------------------------
// rewrites — each returns the new text, or the input unchanged if nothing applied
// ---------------------------------------------------------------------------

/**
 * Shipped-log row. Two things must change, and the second is the one that is easy to miss:
 *   1. `**Pending deploy [date]**`      -> `**Deployed <date>**`
 *   2. the trailing bundle-status clause `; **NOT yet in a built zip - ...**` -> dropped, or
 *      replaced with the bundle id. Left in place it (a) still matches ROW_NOT_SHIPPED_RE so the
 *      row never counts as shipped, and (b) reads as a contradiction to the client - a row that
 *      says "Deployed" and "not yet in a built zip" in the same breath.
 * The `(?=\s*\))` lookahead targets the LAST bold span before the closing paren, so a row like
 * `(code + **database change**; **IN the newest bundle**)` keeps "database change" and loses only
 * the bundle clause.
 */
function rewriteRow(row, date, bundle) {
  // `[^*]*` rather than an exact `Pending deploy YYYY-MM-DD`: the bold span carries free-text
  // annotations in practice — "**Pending deploy 2026-06-22 — NEXT batch**" is a real row, and an
  // exact pattern silently skipped it as "no pending clause matched". The annotation describes
  // which future batch the row was waiting for, which is meaningless once it has shipped.
  let out = row.replace(/\*\*Pending deploy\b[^*]*\*\*/i, `**Deployed ${date}**`);
  const replacement = bundle ? `; **bundle ${bundle}**` : '';
  out = out.replace(/;\s*\*\*[^*]*\*\*(?=\s*\))/, replacement);
  return out.replace(/\(\s*;\s*/, '(').replace(/\s{2,}/g, ' ');
}

/**
 * The row's own pending date, or null. Reads the date from anywhere inside the "Pending deploy …"
 * bold span so annotated spans ("**Pending deploy 2026-06-22 — NEXT batch**") still yield a date.
 * Deliberately scoped to that span: dates elsewhere in a row are fix dates, zip timestamps and
 * client-report dates, none of which are when the thing was deployed.
 */
function rowPendingDate(row) {
  const span = /\*\*Pending deploy\b([^*]*)\*\*/i.exec(row);
  if (!span) return null;
  const d = /(\d{4}-\d{2}-\d{2})/.exec(span[1]);
  return d ? d[1] : null;
}

const daysBetween = (a, b) => Math.round((Date.parse(b) - Date.parse(a)) / 86400000);

// How far a row's own pending date may sit behind --date before this refuses to stamp it.
//
// Why this guard exists, concretely: the first dry run of --all-pending proposed rewriting
// "**Pending deploy 2026-06-05** (`GCCWebApp-deploy-20260605-1641.zip`)" to "**Deployed
// 2026-08-04**" — a row that names its own June zip, restamped two months later. That is a
// self-contradicting receipt, and it is worse than the stale text it replaces: "pending" is at
// least visibly unfinished, whereas a confident wrong date reads as fact. A close-out is for the
// bundle you just uploaded; history needs --keep-row-date, which trusts each row's own evidence.
const STALE_ROW_DAYS = 21;

/**
 * Register `**Status:**` line. Ordered most-specific first: the bold-wrapped forms have to go
 * before the bare ones, or the bare pattern eats the inside of the bold span and leaves stray `**`.
 */
function rewriteStatus(line, date) {
  let out = line;
  const subs = [
    [/\*\*pending (?:the )?next deploy bundle\.?\*\*/gi, `**deployed ${date}**`],
    [/\*\*pending (?:the )?next deploy\.?\*\*/gi, `**deployed ${date}**`],
    [/\*\*pending deploy\.?\*\*/gi, `**deployed ${date}**`],
    [/pending (?:the )?next deploy bundle/gi, `deployed ${date}`],
    [/pending (?:the )?next deploy/gi, `deployed ${date}`],
    [/pending deploy/gi, `deployed ${date}`],
    [/not yet deployed/gi, `deployed ${date}`],
    [/\bundeployed\b/gi, `deployed ${date}`],
    // "working tree" is a statement about where the fix lives, which stops being true at upload.
    // Removed with its adjacent punctuation so the sentence still reads.
    [/\s*[—–-]\s*working tree,\s*/gi, ' — '],
    [/\bworking tree,\s*/gi, ''],
    [/\bworking tree\b/gi, ''],
  ];
  for (const [re, to] of subs) out = out.replace(re, to);
  // tidy the punctuation the removals can leave behind
  return out
    .replace(/\s{2,}/g, ' ')
    .replace(/\s+([,.])/g, '$1')
    .replace(/[—–-]\s*([.|])/g, '$1')
    .replace(/,\s*\./g, '.')
    .trimEnd();
}

// ---------------------------------------------------------------------------
// file passes
// ---------------------------------------------------------------------------
const eolOf = md => (/\r\n/.test(md) ? '\r\n' : '\n');
const short = s => (s.length > 150 ? s.slice(0, 147) + '…' : s);

/**
 * Rewrite matching rows across every docs/tracking/shipped-*.md.
 *
 * ONE function for both preview and apply, gated by `write`. A separate preview twin would be two
 * copies of the same decision rule, free to drift — which is the failure this whole script exists
 * to clean up. What you see in a dry run is produced by the code that does the writing.
 */
function passShippedLogs(ids, date, bundle, write, keepRowDate) {
  const touched = [];
  const refused = [];

  for (const f of fs.readdirSync(TRACKING).filter(x => /^shipped-.*\.md$/i.test(x))) {
    const full = path.join(TRACKING, f);
    const md = fs.readFileSync(full, 'utf8');
    const eol = eolOf(md);
    const lines = md.split(/\r?\n/);
    let changed = 0;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (!ROW_NOT_SHIPPED_RE.test(line)) continue;            // already reads as shipped
      const rowIds = [...line.matchAll(/\b(DEF-\d+|CR-\d+)\b/g)].map(m => m[1]);
      if (!rowIds.some(id => ids.has(id))) continue;

      // Decide WHICH date this row gets, and refuse rather than guess.
      const own = rowPendingDate(line);
      let stamp = date;
      if (keepRowDate) {
        if (!own) {
          refused.push({ file: f, line: i + 1, ids: rowIds, why: '--keep-row-date given but this row states no date of its own', text: line });
          continue;
        }
        stamp = own;
      } else if (own && daysBetween(own, date) > STALE_ROW_DAYS) {
        refused.push({
          file: f, line: i + 1, ids: rowIds,
          why: `row's own date ${own} is ${daysBetween(own, date)} days before --date=${date} — refusing to restamp history; use --keep-row-date, or target it with --ids`,
          text: line,
        });
        continue;
      }

      const next = rewriteRow(line, stamp, bundle);
      if (next === line) {
        refused.push({ file: f, line: i + 1, ids: rowIds, why: 'no pending clause matched', text: line });
        continue;
      }
      // The whole point: prove the rewrite would now be COUNTED as shipped.
      if (ROW_NOT_SHIPPED_RE.test(next)) {
        refused.push({ file: f, line: i + 1, ids: rowIds, why: 'still reads as pending after rewrite', text: next });
        continue;
      }
      touched.push({ file: f, line: i + 1, ids: rowIds, stamp, before: line, after: next });
      lines[i] = next;
      changed++;
    }

    if (write && changed) fs.writeFileSync(full, lines.join(eol), 'utf8');
  }

  // The register pass reuses these so an id's two documents can never disagree about the date, and
  // so an id whose row was refused is not quietly marked deployed in the register instead.
  const stamps = new Map();
  for (const t of touched) for (const id of t.ids) if (!stamps.has(id)) stamps.set(id, t.stamp);
  const refusedIds = new Set(refused.flatMap(x => x.ids || []));
  return { touched, refused, stamps, refusedIds };
}

/**
 * Rewrite the `**Status:**` line of each targeted register entry.
 *
 * `stamps` carries the date each id's shipped-log row was given, so the two documents state the
 * same date. `skipIds` are ids whose row was REFUSED — marking those deployed in the register while
 * their receipt still says pending would split the record in exactly the way that caused this mess.
 */
function passRegister(ids, date, write, stamps = new Map(), skipIds = new Set()) {
  const md = fs.readFileSync(REGISTER, 'utf8');
  const eol = eolOf(md);
  const lines = md.split(/\r?\n/);
  const entries = parseRegister(md);
  const touched = [];
  const refused = [];

  for (const e of entries) {
    if (!ids.has(e.id)) continue;
    if (skipIds.has(e.id)) {
      refused.push({ id: e.id, line: e.line, why: 'its shipped-log row was refused — left pending so the two documents stay consistent', text: short(e.status) });
      continue;
    }
    if (!e.status || !PENDING_RE.test(e.status)) continue;     // already clean
    // Search forward from THIS entry's heading, never from the top of the file. Status lines are
    // formulaic and several entries share byte-identical text ("**Status:** Fixed locally —
    // pending next deploy."), so a whole-file indexOf would rewrite the first match every time:
    // one entry edited repeatedly, the rest silently skipped, and the roll-up still claiming
    // success. Bounded by the next heading so a miss cannot leak into the following entry.
    const from = e.line;                                       // e.line is 1-based -> index of the line AFTER the heading
    let end = lines.length;
    for (let j = from; j < lines.length; j++) {
      if (/^###\s+(DEF-\d+|CR-\d+)/.test(lines[j])) { end = j; break; }
    }
    let idx = -1;
    for (let j = from; j < end; j++) if (lines[j] === e.status) { idx = j; break; }
    if (idx === -1) {
      refused.push({ id: e.id, line: e.line, why: 'status line could not be located verbatim', text: short(e.status) });
      continue;
    }
    const next = rewriteStatus(e.status, stamps.get(e.id) || date);
    if (next === e.status) {
      refused.push({ id: e.id, line: idx + 1, why: 'no pending clause matched', text: short(e.status) });
      continue;
    }
    if (PENDING_RE.test(next)) {
      refused.push({ id: e.id, line: idx + 1, why: 'still reads as pending after rewrite', text: short(next) });
      continue;
    }
    touched.push({ id: e.id, line: idx + 1, before: e.status, after: next });
    lines[idx] = next;
  }

  if (write && touched.length) fs.writeFileSync(REGISTER, lines.join(eol), 'utf8');
  return { touched, refused };
}

// ---------------------------------------------------------------------------
// guard: the restated regex must still match the one check 4 uses
// ---------------------------------------------------------------------------
function selfCheck() {
  const probe = '| **Pending deploy 2026-07-31** (code only; **NOT yet in a built zip**) |';
  const ok = ROW_NOT_SHIPPED_RE.test(probe)
    && !ROW_NOT_SHIPPED_RE.test(rewriteRow(probe, '2026-08-04', null))
    && PENDING_RE.test('**Status:** Fixed — working tree, **pending next deploy bundle**')
    && !PENDING_RE.test(rewriteStatus('**Status:** Fixed — working tree, **pending next deploy bundle**', '2026-08-04'));
  if (!ok) {
    console.error('\n✖ SELF-CHECK FAILED — the rewrite no longer clears check 4\'s regexes.');
    console.error('  Either unshipped-fixes.js changed its definition of "pending", or a rewrite rule regressed.');
    console.error('  Refusing to touch the tracking docs. Fix this first.');
    return false;
  }
  return true;
}

// ---------------------------------------------------------------------------
// main
// ---------------------------------------------------------------------------
function main() {
  const a = parseArgs(process.argv.slice(2));
  if (!a) return usage();
  if (!a.date) return usage('--date is required (the date the bundle was actually uploaded).');
  if (!/^\d{4}-\d{2}-\d{2}$/.test(a.date)) return usage(`--date must be YYYY-MM-DD, got "${a.date}".`);
  if (!a.ids && !a.allPending) return usage('give --all-pending or --ids=...');
  if (!fs.existsSync(REGISTER)) return usage(`defect register not found at ${REGISTER}`);
  if (!selfCheck()) return 2;

  const md = fs.readFileSync(REGISTER, 'utf8');
  const entries = parseRegister(md);
  const shipped = shippedIds();
  const claimsPending = entries.filter(e => PENDING_RE.test(e.status));
  const genuinelyPending = claimsPending.filter(e => !shipped.has(e.id));
  const staleText = claimsPending.filter(e => shipped.has(e.id));

  let ids;
  if (a.ids) {
    ids = new Set(a.ids);
    const known = new Set(entries.map(e => e.id));
    const unknown = [...ids].filter(id => !known.has(id));
    if (unknown.length) console.log(`  note: ${unknown.length} id(s) are not in the register: ${unknown.join(', ')}`);
  } else {
    ids = new Set(genuinelyPending.map(e => e.id));
  }
  if (a.includeStale) staleText.forEach(e => ids.add(e.id));

  console.log(`\n╔${'═'.repeat(62)}╗`);
  console.log(`║  POST-DEPLOY CLOSE-OUT${a.apply ? '' : '  (DRY RUN — nothing will be written)'}`.padEnd(63) + '║');
  console.log(`╚${'═'.repeat(62)}╝`);
  console.log(`  deploy date : ${a.date}`);
  console.log(`  bundle      : ${a.bundle || '(not recorded)'}`);
  console.log(`  register    : ${entries.length} entries | ${genuinelyPending.length} genuinely undeployed | ${staleText.length} stale text`);
  console.log(`  acting on   : ${ids.size} id(s)${a.ids ? ' (explicit)' : ' (--all-pending)'}${a.includeStale ? ' + stale' : ''}`);

  const rows = passShippedLogs(ids, a.date, a.bundle, a.apply, a.keepRowDate);
  const reg = passRegister(ids, a.date, a.apply, rows.stamps, rows.refusedIds);

  console.log(`\n--- shipped log rows ${'-'.repeat(45)}`);
  if (!rows.touched.length) console.log('  (none matched)');
  for (const t of rows.touched) {
    console.log(`  ${t.file}:${t.line}  ${t.ids.join(', ')}`);
    console.log(`    -  ${short(t.before.split('|')[1] || t.before)}`);
    console.log(`    +  ${short(t.after.split('|')[1] || t.after)}`);
  }

  console.log(`\n--- register Status lines ${'-'.repeat(40)}`);
  if (!reg.touched.length) console.log('  (none matched)');
  for (const t of reg.touched) {
    console.log(`  ${t.id} (line ${t.line})`);
    console.log(`    -  ${short(t.before)}`);
    console.log(`    +  ${short(t.after)}`);
  }

  const refused = [...rows.refused, ...reg.refused];
  if (refused.length) {
    console.log(`\n--- NEEDS A HUMAN — refused, not written ${'-'.repeat(24)}`);
    for (const x of refused) {
      console.log(`  ${x.id || `${x.file}:${x.line}`}  — ${x.why}`);
      console.log(`     ${short(x.text)}`);
    }
  }

  // Register entries with no shipped-log row have nothing corroborating the date they just got —
  // the date rests entirely on the caller's assertion that this id was in this deploy. That is a
  // legitimate thing to assert, but it must not hide inside a total. Called out, not blocked.
  const uncorroborated = reg.touched.filter(t => !rows.stamps.has(t.id)).map(t => t.id);

  console.log(`\n${'#'.repeat(64)}`);
  console.log(`  shipped-log rows rewritten : ${rows.touched.length}`);
  console.log(`  register Status rewritten  : ${reg.touched.length}`);
  console.log(`  refused (need a human)     : ${refused.length}`);
  if (uncorroborated.length) {
    console.log(`\n  ⚠ ${uncorroborated.length} register line(s) stamped ${a.date} with NO shipped-log row to corroborate it.`);
    console.log(`    The date rests on your assertion that these were in this deploy — nothing else says so:`);
    console.log(`    ${uncorroborated.join(', ')}`);
    console.log(`    If any were NOT in it, re-run with --ids= naming only the ones that were.`);
  }

  if (a.apply) {
    // Recompute from disk with check 4's own functions. This is the falsifiable bit: if the edits
    // did not actually change how check 4 reads these files, this number does not move.
    delete require.cache[require.resolve('../smoke-prod/_static/unshipped-fixes')];
    const after = parseRegister(fs.readFileSync(REGISTER, 'utf8'));
    const shippedAfter = shippedIds();
    const stillPending = after.filter(e => PENDING_RE.test(e.status) && !shippedAfter.has(e.id));
    console.log(`  genuinely undeployed       : ${genuinelyPending.length} -> ${stillPending.length}`);
    console.log(`\n  Verify independently:  node Scripts/smoke-prod/_static/unshipped-fixes.js`);
  } else {
    console.log(`\n  DRY RUN — re-run with --apply to write these changes.`);
  }
  console.log(`${'#'.repeat(64)}\n`);

  return refused.length ? 1 : 0;
}

module.exports = { rewriteRow, rewriteStatus, ROW_NOT_SHIPPED_RE };
if (require.main === module) process.exit(main());
