// CHECK 1 — TempData writer/reader pairing.   Catches DEF-318 and all three parts of DEF-330.
//
// THE BUG THIS EXISTS TO PREVENT
// ASP.NET Core does not discard an *unread* TempData entry. It re-saves it into the cookie on
// every subsequent request until something reads that key. "Read-once" only starts counting after
// the first read. So a write whose redirect target never reads the key does not vanish — it waits,
// and surfaces later on whatever unrelated page does read that key.
//
// That is exactly what the client saw in QC #193 / Image 106: an Edit POST wrote
// TempData["SuccessMessage"], redirected to a view that reads no TempData at all, and the message
// sat in the cookie (through logout/login) until the user opened Add Certificate — the next view
// bound to that key.
//
// THE INVARIANT
//   Write TempData only when the redirect target itself reads that key.
//   No key may have a writer without a reader, or a reader without a writer.
//
// LEVELS
//   L1 (FAIL) — key has writers and ZERO consuming reads anywhere. Provable orphan. No judgement.
//   L2 (FAIL) — a specific write is followed by a redirect whose target action AND that action's
//               view both provably never read the key. This is the DEF-318 shape precisely.
//   L3 (WARN) — key is only ever read via TempData.Peek(). Peek renders but does not consume, so
//               the entry is still re-saved forever. Renders correctly, leaks anyway.
//   L4 (WARN) — key has readers but no writer. Dead render path; usually the other half of a
//               half-finished removal (this is how DEF-318's reader was left behind).
//
// FAIL = deploy block. WARN = report, never blocks.

const path = require('path');
const { PROJECT_ROOT, walk, readSource, lineOf, loadAllowlist, recorder } = require('./_lib/static-harness');
const { buildIndex, resolveViewFile } = require('./_lib/mvc-index');

const WRITE_RE = /TempData\[\s*"([^"]+)"\s*\]\s*(?:\?\?)?=(?!=)/g;      // TempData["X"] =   /  ??=
const ADD_RE = /TempData\.Add\(\s*"([^"]+)"/g;
const ANY_RE = /TempData\[\s*"([^"]+)"\s*\]/g;
const PEEK_RE = /TempData\.Peek\(\s*"([^"]+)"/g;
const REMOVE_RE = /TempData\.Remove\(\s*"([^"]+)"/g;
const TRYGET_RE = /TempData\.(?:TryGetValue|ContainsKey)\(\s*"([^"]+)"/g;

// How far past a write to look for its return statement. Writes sit immediately above their
// return in every occurrence in this codebase; 40 lines is generous headroom.
const RETURN_WINDOW_LINES = 40;

function collect(sources) {
  const writes = [];   // {key, rel, line, offset}
  const reads = [];    // {key, rel, line, kind: 'read'|'peek'|'remove'}

  for (const src of sources) {
    const record = (re, handler) => {
      re.lastIndex = 0;
      let m;
      while ((m = re.exec(src.code)) !== null) handler(m, lineOf(src.code, m.index), m.index);
    };

    const writeOffsets = new Set();
    record(WRITE_RE, (m, line, off) => {
      writes.push({ key: m[1], rel: src.rel, line, offset: off });
      writeOffsets.add(off);
    });
    record(ADD_RE, (m, line, off) => writes.push({ key: m[1], rel: src.rel, line, offset: off }));

    // Any TempData["X"] occurrence that is NOT one of the writes above is a consuming read.
    record(ANY_RE, (m, line, off) => {
      if (writeOffsets.has(off)) return;
      reads.push({ key: m[1], rel: src.rel, line, kind: 'read' });
    });
    record(PEEK_RE, (m, line) => reads.push({ key: m[1], rel: src.rel, line, kind: 'peek' }));
    record(REMOVE_RE, (m, line) => reads.push({ key: m[1], rel: src.rel, line, kind: 'remove' }));
    record(TRYGET_RE, (m, line) => reads.push({ key: m[1], rel: src.rel, line, kind: 'read' }));
  }
  return { writes, reads };
}

/** Does this blob of source read the key by any means? */
function textReadsKey(code, key) {
  if (!code) return false;
  const esc = key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const anyRead = new RegExp(
    `TempData\\[\\s*"${esc}"\\s*\\](?!\\s*(?:\\?\\?)?=(?!=))` +
    `|TempData\\.(?:Peek|Remove|TryGetValue|ContainsKey)\\(\\s*"${esc}"`
  );
  return anyRead.test(code);
}

/**
 * L2 — for one write, find its return statement and decide whether the destination can ever
 * render the key. Returns { verdict: 'ok'|'orphan'|'unresolved', why }.
 */
function proveDestination(write, srcByRel, index, viewFiles) {
  const src = srcByRel.get(write.rel);
  if (!src) return { verdict: 'unresolved', why: 'source not indexed' };

  const startLine = write.line;
  const window = src.lines.slice(startLine - 1, startLine - 1 + RETURN_WINDOW_LINES);
  // Stop at the next method signature so we never cross a method boundary.
  const stopAt = window.findIndex((l, i) => i > 0 && /^\s*(?:public|private|protected|internal)\s+/.test(l));
  const scan = (stopAt === -1 ? window : window.slice(0, stopAt)).join('\n');

  // Lazy up to the `)` that is immediately followed by `;` — so nested calls and anonymous route
  // objects are captured whole: nameof(X), new { id = c.Number }  ->  both args intact.
  // (A greedy `([^;]*)\)?` silently swallows the closing paren into the capture, which makes every
  // action name fail to parse and every write report as "unresolved" — a check that passes by
  // failing to look. Regression-guarded by `selftest.js`.)
  const ret = /return\s+(RedirectToAction|RedirectToPage|Redirect|View|PartialView)\s*\(([^;]*?)\)\s*;/.exec(scan);
  if (!ret) return { verdict: 'unresolved', why: 'no return statement found within window' };

  const [, kind, argsRaw] = ret;
  const args = argsRaw || '';

  // ------------------------------------------------------------------------------------------
  // Same-request render (`return View(...)`) is NOT judged, and that is deliberate.
  //
  // The render→postback pattern is legitimate and common in this codebase: AccountController
  // writes ForceResetEmail/Role/ReturnUrl/RememberMe, returns View("ForcePasswordReset"), and the
  // *POST* action reads them on the next request. TempData surviving exactly one request is the
  // whole point. The next request there is a form submit we cannot identify statically, so
  // flagging it would be a guess — and a gate that cries wolf gets switched off.
  //
  // Only a REDIRECT is judged, because a redirect *determines* the next request: after
  // RedirectToAction(X), the next thing rendered is X. If X cannot read the key, the entry is
  // guaranteed to outlive its intended life. That is the DEF-318 mechanism exactly, and it is
  // provable rather than inferred. L1 still catches anything read by nobody at all.
  // ------------------------------------------------------------------------------------------
  if (kind === 'View' || kind === 'PartialView') {
    return { verdict: 'unresolved', why: 'same-request View() - postback reader not statically knowable (by design)' };
  }

  if (kind !== 'RedirectToAction') {
    return { verdict: 'unresolved', why: `${kind}(...) target not statically resolvable` };
  }

  // RedirectToAction("Action" | nameof(Action) [, "Controller"] [, routeValues])
  const parts = args.split(',');
  const first = (parts[0] || '').trim();
  const second = (parts[1] || '').trim();

  const actionName =
    /^"([^"]+)"$/.exec(first)?.[1] ??
    /^nameof\(\s*(\w+)\s*\)$/.exec(first)?.[1] ?? null;
  if (!actionName) return { verdict: 'unresolved', why: `action arg "${first}" not a literal` };

  const ownCtrl = /(\w+)Controller\.cs$/.exec(write.rel)?.[1] ?? null;
  const targetCtrlName =
    /^"([^"]+)"$/.exec(second)?.[1] ??
    (second.startsWith('nameof') ? null : ownCtrl);
  if (!targetCtrlName) return { verdict: 'unresolved', why: 'controller arg not a literal' };

  const targetCtrl = index.controllers.get(targetCtrlName);
  if (!targetCtrl) return { verdict: 'unresolved', why: `controller ${targetCtrlName} not indexed` };
  const targetAction = targetCtrl.actions.get(actionName);
  if (!targetAction) {
    // redirect-targets.js reports the missing action; not this check's job.
    return { verdict: 'unresolved', why: `action ${targetCtrlName}/${actionName} not found (see check 2)` };
  }

  // Does the target ACTION body read the key? (Several auth flows read TempData in the action,
  // not the view — those are correctly paired and must not be flagged.)
  for (const b of targetAction.bodies) {
    if (textReadsKey(b.body, write.key)) {
      return { verdict: 'ok', why: `read by ${targetCtrlName}Controller.${actionName}` };
    }
  }
  // Or any view that action can render?
  const checked = [];
  for (const vn of targetAction.views) {
    const file = resolveViewFile(viewFiles, targetCtrlName, vn);
    if (!file) continue;
    checked.push(file);
    const vsrc = srcByRel.get(file);
    if (vsrc && textReadsKey(vsrc.code, write.key)) return { verdict: 'ok', why: `read by ${file}` };
  }
  if (checked.length === 0) {
    return { verdict: 'unresolved', why: `no view resolved for ${targetCtrlName}/${actionName}` };
  }

  // ------------------------------------------------------------------------------------------
  // The target itself does not read the key. Before calling that a leak, ask whether ANY action
  // or view of the target controller reads it — because the legitimate two-hop flow looks like
  // this: redirect -> render a form -> the POST reads the key. AccountController does exactly
  // that for 2FA (write 2FARole, redirect to TwoFactorAuthentication which renders the form,
  // Verify2FA POST consumes it). Unread keys are retained across requests, so it works.
  //
  // That pattern is structurally identical to DEF-318 and cannot be told apart with certainty
  // from source alone — the only difference is whether a consumer is actually *reachable* from
  // the destination. So it is reported as WARN, named precisely, rather than guessed either way.
  //
  // A leak is only asserted (FAIL) when NOTHING in the whole target controller reads the key —
  // then no navigation from that destination can consume it, and the entry is guaranteed to
  // outlive its purpose. That is DEF-330 #1 exactly: CertificateImportController wrote "Success"
  // and redirected into CertificateController, which reads it nowhere.
  // ------------------------------------------------------------------------------------------
  const siblings = [];
  for (const [aName, a] of targetCtrl.actions) {
    for (const b of a.bodies) {
      if (textReadsKey(b.body, write.key)) siblings.push(`${targetCtrlName}Controller.${aName}`);
    }
    for (const vn of a.views) {
      const file = resolveViewFile(viewFiles, targetCtrlName, vn);
      if (!file) continue;
      const vsrc = srcByRel.get(file);
      if (vsrc && textReadsKey(vsrc.code, write.key)) siblings.push(file);
    }
  }
  if (siblings.length > 0) {
    return {
      verdict: 'twohop',
      why: `redirect target ${targetCtrlName}/${actionName} (${checked.join(', ')}) does not read "${write.key}", ` +
           `but ${[...new Set(siblings)].slice(0, 3).join(', ')} does`,
    };
  }

  // FINAL FALLBACK before asserting a leak — scan the target controller's ENTIRE source, not only
  // its action bodies and views. The loop above walks `targetCtrl.actions`, so a read that lives in
  // a PRIVATE HELPER the actions call is invisible to it, and the check then states outright that
  // "NOTHING in XController" reads the key while the read sits a few hundred lines away.
  //
  // Found 2026-08-25: `"2FAMethod"` is read by `AccountController.SetTwoFactorMethodViewData()`
  // (AccountController.cs:1316) — a private method invoked by the 2FA actions to set the
  // instruction text. Extracting shared logic into a helper is ordinary refactoring and must not
  // turn a paired key into a deploy-blocking "provable orphan".
  //
  // A whole-file hit downgrades to `twohop` (WARN), never to `ok`: this proves a consumer EXISTS,
  // not that it is reachable from this destination — the same distinction the block above draws.
  for (const [rel, s] of srcByRel) {
    if (!/(?:^|[\\/])Controllers[\\/].*Controller\.cs$/.test(rel)) continue;
    if (!rel.endsWith(`${targetCtrlName}Controller.cs`)) continue;
    if (textReadsKey(s.code, write.key)) {
      return {
        verdict: 'twohop',
        why: `redirect target ${targetCtrlName}/${actionName} (${checked.join(', ')}) does not read ` +
             `"${write.key}", but ${rel} does outside an action body (private helper)`,
      };
    }
  }

  return {
    verdict: 'orphan',
    why: `redirect target ${targetCtrlName}/${actionName} (${checked.join(', ')}) never reads "${write.key}", ` +
         `and NOTHING in ${targetCtrlName}Controller does either`,
  };
}

function run() {
  console.log('\n--- check 1: TempData writer/reader pairing -------------------------------');
  const r = recorder('tempdata-pairing');
  const allow = loadAllowlist().tempdata || {};
  const index = buildIndex();

  const sources = [
    ...walk(path.join(PROJECT_ROOT, 'Controllers'), /\.cs$/),
    ...walk(path.join(PROJECT_ROOT, 'Views'), /\.cshtml$/),
    ...walk(path.join(PROJECT_ROOT, 'Services'), /\.cs$/),
    ...walk(path.join(PROJECT_ROOT, 'Middleware'), /\.cs$/),
  ].map(readSource);
  const srcByRel = new Map(sources.map(s => [s.rel, s]));

  const { writes, reads } = collect(sources);
  const l1Failed = new Set();   // keys already reported at L1 - never re-report them at L2
  const keys = new Set([...writes.map(w => w.key), ...reads.map(x => x.key)]);
  console.log(`  scanned ${sources.length} files - ${keys.size} distinct TempData keys, ${writes.length} writes, ${reads.length} reads`);

  for (const key of [...keys].sort()) {
    const w = writes.filter(x => x.key === key);
    const rd = reads.filter(x => x.key === key);
    const consuming = rd.filter(x => x.kind !== 'peek');
    const waiver = allow[key];

    // L1 — writers, and NO read of any kind anywhere. Provable orphan.
    //
    // `rd.length === 0` is load-bearing: without it this branch also swallowed every Peek-only key
    // and reported it as "written Nx but NEVER read" — a FAIL, which blocks a deploy. L3 below is
    // the intended verdict for that case (WARN), but its condition is a strict SUBSET of this one,
    // so with L1 testing only `consuming.length === 0` and then `continue`-ing, **L3 was
    // unreachable dead code and no Peek-only key could ever reach it**.
    //
    // Found 2026-08-25 when this gate blocked a bundle over `"2FAMethod"`, which is read at
    // AccountController.cs:1316 via `TempData.Peek(...)` — deliberately, and documented there, so
    // that a failed 2FA attempt re-renders the same instruction. The gate's own collector had
    // always recorded that read (PEEK_RE); the verdict logic discarded it.
    if (w.length > 0 && consuming.length === 0 && rd.length === 0) {
      const at = w.map(x => `${x.rel}:${x.line}`).join(', ');
      if (waiver) { l1Failed.add(key); r.info(`"${key}" orphan write - WAIVED: ${waiver}`, { at }); continue; }
      l1Failed.add(key);
      r.fail(
        `"${key}" is written ${w.length}x but NEVER read - the entry is re-saved into the cookie on every request, forever`,
        { key, at, fix: 'delete the write, or redirect to something that reads the key' }
      );
      continue;
    }

    // L4 — readers, no writer.
    if (w.length === 0 && rd.length > 0) {
      const at = rd.map(x => `${x.rel}:${x.line}`).join(', ');
      if (waiver) { r.info(`"${key}" reader with no writer - WAIVED: ${waiver}`, { at }); continue; }
      r.warn(`"${key}" is read but never written - dead render path (half-finished removal?)`, { key, at });
      continue;
    }

    // L3 — only ever Peek'd.
    if (w.length > 0 && consuming.length === 0 && rd.length > 0) {
      r.warn(`"${key}" is only ever read via Peek() - renders but never consumes, so it still leaks`, {
        key, at: rd.map(x => `${x.rel}:${x.line}`).join(', '),
      });
      continue;
    }

    r.pass(`"${key}" paired (${w.length} write / ${consuming.length} read)`);
  }

  // L2 — per-write destination proof.
  console.log('  -- destination proof (does each write\'s redirect target read the key?)');
  let unresolved = 0, twohop = 0;
  for (const w of writes) {
    if (allow[w.key] || l1Failed.has(w.key)) continue;   // L1 already owns this key
    const { verdict, why } = proveDestination(w, srcByRel, index, index.viewFiles);
    if (verdict === 'orphan') {
      r.fail(`${w.rel}:${w.line} writes "${w.key}" but ${why}`, {
        key: w.key, at: `${w.rel}:${w.line}`,
        fix: 'this is the DEF-318 shape: delete the write, or give the flow a dedicated key its own target reads',
      });
    } else if (verdict === 'twohop') {
      twohop++;
      r.warn(`${w.rel}:${w.line} writes "${w.key}" - ${why}`, {
        key: w.key, at: `${w.rel}:${w.line}`,
        fix: 'confirm the consumer is reachable from the redirect destination (a form that posts to it). ' +
             'If it is only reachable by unrelated navigation, this IS DEF-318 and the write must go.',
      });
    } else if (verdict === 'unresolved') {
      unresolved++;
    }
  }
  if (twohop) {
    console.log(`  note: ${twohop} two-hop write(s) above need a human read - they are the ambiguous shape, not proven leaks`);
  }
  if (unresolved) {
    r.info(`${unresolved} write(s) had a destination this check could not resolve statically - not a finding, just uncovered`);
  }

  const s = r.summary();
  console.log(`  => ${s.pass}P / ${s.fail}F / ${s.warn}W`);
  return s;
}

module.exports = { run };
if (require.main === module) {
  const s = run();
  process.exit(s.fail > 0 ? 1 : 0);
}
