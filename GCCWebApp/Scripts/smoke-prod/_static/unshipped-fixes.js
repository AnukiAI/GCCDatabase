// CHECK 4 — reconcile "the register says this is fixed" against "the code and the shipped log agree".
//
// THE FAILURE THIS EXISTS TO PREVENT
// This is the one that actually cost us with the client. DEF-318 was root-caused and fixed on
// 2026-07-31. On 2026-08-03 the client re-reported it — because the fix was still sitting
// uncommitted in the working tree. Nothing anywhere compared "register says Fixed, pending next
// deploy bundle" against what was really in HEAD. Gates 0-8 all assess a BUNDLE; none of them
// assess the gap between "we fixed it" and "they can see it".
//
// HOW "STILL PENDING" IS DECIDED
// The register keeps its Status text forever, so ~143 historical entries still read "working tree,
// pending next deploy bundle" long after they shipped. Matching on that text alone produces a
// report nobody will read. So an entry counts as genuinely pending only when BOTH hold:
//   (a) its Status says working-tree / pending-deploy, AND
//   (b) its id appears in NO docs/tracking/shipped-*.md log.
// Entries failing (b) are stale register text — counted in one line, never spammed one-per-entry.
//
// CLASSIFICATION of the genuinely-pending set:
//   MODIFIED - files differ from HEAD. Real, present, uncommitted. Ships only if the bundle is
//              published from the WORKING TREE; lost by any clean checkout.          -> WARN
//   CLEAN    - files identical to HEAD. Either committed-but-undeployed, or the change is not
//              there at all. Both need a human.                                      -> WARN
//   MISSING  - the register names a repo path that does not exist.                   -> FAIL
//
// Read-only: `git diff --name-only`, `git status --porcelain`, `git rev-parse`. Nothing is
// written, staged, committed or checked out.

const fs = require('fs');
const path = require('path');
const { PROJECT_ROOT, git, loadAllowlist, recorder } = require('./_lib/static-harness');

const TRACKING = path.join(PROJECT_ROOT, 'docs', 'tracking');
const REGISTER = path.join(TRACKING, 'defect-register.md');
const PENDING_RE = /working tree|pending (?:the )?next deploy|pending deploy|not yet deployed|undeployed/i;

// Only backticked paths rooted in a real source directory are treated as file references. Bare
// names like `main.css` appear in prose all over the register and cannot be resolved reliably —
// guessing at them produced three false FAILs on the first run.
const SOURCE_ROOTS = /^(Controllers|Views|Services|Repositories|Models|Data|Middleware|wwwroot|Scripts|Migrations|docs)\//;

/** Parse `### DEF-XXX — title` blocks and their `**Status:**` line. */
function parseRegister(md) {
  const entries = [];
  // Split on CRLF *or* LF. This file is CRLF, and in JavaScript `\r` is a line terminator, so `.`
  // does not match it — meaning a pattern ending `(.*)$` matches NOTHING on a CRLF line. That is
  // not hypothetical: it silently made this check report "0 entries, all clear" against a register
  // holding 247 of them. A broken check and a clean codebase look identical unless you test it —
  // hence selftest.js.
  const lines = md.split(/\r?\n/);
  let cur = null;
  for (let i = 0; i < lines.length; i++) {
    // Match the id, then take the rest of the line and strip separators in code. Deliberately does
    // not encode the dash: pinning a parser to one dash character is the same class of trap.
    const h = /^###\s+(DEF-\d+|CR-\d+)(.*)$/.exec(lines[i]);
    if (h) {
      if (cur) entries.push(cur);
      cur = { id: h[1], title: h[2].replace(/^[\s‐-―-]+/, '').trim(), line: i + 1, status: '', body: '' };
      continue;
    }
    if (!cur) continue;
    cur.body += lines[i] + '\n';
    if (!cur.status && /\*\*Status:\*\*/.test(lines[i])) cur.status = lines[i];
  }
  if (cur) entries.push(cur);
  return entries;
}

/** Backticked repo-rooted paths in a blob of markdown. */
function filesIn(text) {
  const out = new Set();
  for (const m of text.matchAll(/`([^`]+)`/g)) {
    const raw = m[1].trim().replace(/^\.?\//, '');
    if (/[ ,`]/.test(raw)) continue;
    if (!SOURCE_ROOTS.test(raw)) continue;
    if (!/\.(cs|cshtml|js|css|json|sql|md)$/i.test(raw)) continue;
    out.add(raw);
  }
  return [...out];
}

// A shipped log row is NOT proof of shipping. shipped-2026-07.md contains rows reading
// "Pending deploy 2026-07-31 … NOT yet in a built zip — folds into the next rebuild". Treating a
// mere mention as "shipped" hid DEF-318 — the exact defect this check exists for — on the first
// run. So each row is classified, and only rows that describe an actual deploy count.
const ROW_NOT_SHIPPED_RE = /not yet in a built zip|pending deploy|pending the next deploy|not yet deployed/i;

/** DEF/CR ids that a shipped-*.md log says actually went out (pending rows excluded). */
function shippedIds() {
  const ids = new Set();
  if (!fs.existsSync(TRACKING)) return ids;
  for (const f of fs.readdirSync(TRACKING)) {
    if (!/^shipped-.*\.md$/i.test(f)) continue;
    const md = fs.readFileSync(path.join(TRACKING, f), 'utf8');
    for (const row of md.split(/\r?\n/)) {
      if (ROW_NOT_SHIPPED_RE.test(row)) continue;          // row says it has NOT gone out
      for (const m of row.matchAll(/\b(DEF-\d+|CR-\d+)\b/g)) ids.add(m[1]);
    }
  }
  return ids;
}

function run() {
  console.log('\n--- check 4: register "fixed" vs code reality ----------------------------');
  const r = recorder('unshipped-fixes');
  const allow = new Set(loadAllowlist().unshipped || []);

  if (!fs.existsSync(REGISTER)) {
    r.warn(`defect register not found at ${path.relative(PROJECT_ROOT, REGISTER)} - check skipped`);
    return r.summary();
  }
  if (!git(['rev-parse', '--git-dir'])) {
    r.warn('not a git checkout (or git unavailable) - cannot reconcile against HEAD');
    return r.summary();
  }

  const entries = parseRegister(fs.readFileSync(REGISTER, 'utf8'));
  const shipped = shippedIds();
  const claimsPending = entries.filter(e => PENDING_RE.test(e.status) && !allow.has(e.id));
  const stale = claimsPending.filter(e => shipped.has(e.id));
  const pending = claimsPending.filter(e => !shipped.has(e.id));

  console.log(`  register ${entries.length} entries | ${claimsPending.length} say pending | ${stale.length} already in a shipped log | ${pending.length} genuinely undeployed`);

  // Read-only git state.
  const modified = new Set(git(['diff', '--name-only', 'HEAD']).split('\n').map(s => s.trim()).filter(Boolean));
  const untracked = new Set(
    git(['status', '--porcelain', '--untracked-files=all']).split('\n')
      .filter(l => l.startsWith('??')).map(l => l.slice(3).trim())
  );
  // Register paths are project-relative (Controllers/…); git reports repo-relative
  // (GCCWebApp/Controllers/…) because the project sits one level below the repo root.
  const prefixRaw = git(['rev-parse', '--show-prefix']).trim();
  const prefix = prefixRaw && !prefixRaw.endsWith('/') ? prefixRaw + '/' : prefixRaw;
  const isDirty = p => modified.has(prefix + p) || modified.has(p) || untracked.has(prefix + p) || untracked.has(p);

  const rows = [];
  let unverifiable = 0;

  for (const e of pending) {
    // The **Status:** line is the AUTHORITATIVE statement of which files a fix touched. Body prose
    // is only a fallback for entries whose Status names none — and it is never trusted enough to
    // FAIL on, because an entry's body legitimately names paths that do NOT exist: DEF-333's body
    // says "there was no `Views/Shared/VerifyPhone.cshtml`", describing the absence that WAS the
    // bug. Failing on that is the check misreading a correct entry. Body paths therefore feed the
    // MODIFIED/CLEAN classification only.
    const authoritative = filesIn(e.status);
    const files = authoritative.length ? authoritative : filesIn(e.body);
    if (files.length === 0) { unverifiable++; rows.push({ id: e.id, state: 'UNVERIFIABLE', files: [] }); continue; }

    const missing = authoritative.filter(f => !fs.existsSync(path.join(PROJECT_ROOT, f)));
    if (missing.length) {
      r.fail(`${e.id} names ${missing.length} repo path(s) that do not exist: ${missing.join(', ')}`, {
        id: e.id, at: `docs/tracking/defect-register.md:${e.line}`,
        fix: 'the register describes a change against a path that is not there - correct the entry or restore the file',
      });
      rows.push({ id: e.id, state: 'MISSING', files: missing });
      continue;
    }
    const dirty = files.filter(isDirty);
    rows.push({ id: e.id, state: dirty.length ? 'MODIFIED' : 'CLEAN', files: dirty.length ? dirty : files });
  }

  const mod = rows.filter(x => x.state === 'MODIFIED');
  const clean = rows.filter(x => x.state === 'CLEAN');

  // One finding, not one per defect: this bucket is usually stale register prose, and a wall of
  // near-identical WARNs is how a report stops being read.
  if (clean.length) {
    r.warn(`${clean.length} defect(s) say "pending deploy" and appear in no deploy row, yet none of their files differ from HEAD - most likely shipped with stale Status text: ${clean.map(x => x.id).join(', ')}`, {
      ids: clean.map(x => x.id),
      fix: 'for each: either it is committed-but-undeployed (say so in the Status), or it shipped and the Status needs updating. Cheap to clear, and it is what makes the MODIFIED list above trustworthy.',
    });
  }

  if (mod.length) {
    console.log('  -- uncommitted work backing a genuinely-undeployed entry:');
    for (const x of mod) console.log(`     ${x.id.padEnd(9)} ${x.files.join(', ')}`);
    r.warn(`${mod.length} defect(s) exist ONLY in the working tree - publish the bundle from THIS tree, not from a clean checkout of HEAD`, {
      ids: mod.map(x => x.id),
      fix: 'commit them, or confirm the publish source - a clean clone silently drops every one of these',
    });
  }

  if (stale.length) {
    r.info(`${stale.length} register Status line(s) still say "pending deploy" for defects that appear in a shipped log - stale text, not a deploy risk (${stale.slice(0, 6).map(e => e.id).join(', ')}${stale.length > 6 ? ', …' : ''})`, {
      ids: stale.map(e => e.id),
    });
  }
  if (unverifiable) {
    r.info(`${unverifiable} genuinely-pending entr(ies) name no repo-rooted file path, so their code state cannot be verified`, {
      ids: rows.filter(x => x.state === 'UNVERIFIABLE').map(x => x.id),
    });
  }

  if (pending.length === 0) {
    r.pass('every register entry marked fixed is either shipped or accounted for');
  } else {
    console.log(`\n  ROLL-UP - ${pending.length} fixed defect(s) NOT yet in the client's hands:`);
    console.log(`     ${rows.map(x => `${x.id}(${x.state})`).join('  ')}`);
  }

  const s = r.summary();
  console.log(`  => ${s.pass}P / ${s.fail}F / ${s.warn}W`);
  return s;
}

module.exports = { run, parseRegister, filesIn, shippedIds, PENDING_RE };   // parser exported for selftest.js
if (require.main === module) {
  const s = run();
  process.exit(s.fail > 0 ? 1 : 0);
}
