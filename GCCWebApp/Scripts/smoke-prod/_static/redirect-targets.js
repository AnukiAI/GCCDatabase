// CHECK 2 — every MVC route reference points at an action that exists.   Catches DEF-331.
//
// THE BUG THIS EXISTS TO PREVENT
// `RedirectToAction("Details", "Certificate")` is three magic strings. The compiler validates none
// of them. DocumentController has redirected a SUCCESSFUL upload to `Certificate/Details` for as
// long as it has existed — there is no `Details` action and no `Views/Certificate/Details.cshtml`,
// so the user lands on a 404 after their work succeeded. A green build says nothing about this.
//
// WHAT IS CHECKED
//   * `RedirectToAction(...)` in controllers                      -> FAIL if the action is missing
//   * `Url.Action(...)` in controllers and views                  -> FAIL if the action is missing
//   * `asp-controller`/`asp-action` tag-helper pairs in views     -> FAIL if the action is missing
//     (a broken nav link is the same defect wearing a different hat)
//
// Only statically-resolvable references are judged. A name built from a variable is reported as
// unresolved and never as a failure — this check never guesses.
//
// FAIL = deploy block.

const path = require('path');
const { PROJECT_ROOT, walk, readSource, lineOf, loadAllowlist, recorder } = require('./_lib/static-harness');
const { buildIndex, resolveViewFile } = require('./_lib/mvc-index');

/** Split a call's argument list on commas that are at paren/brace depth 0. */
function splitArgs(s) {
  const out = [];
  let depth = 0, cur = '';
  for (const ch of s) {
    if (ch === '(' || ch === '{' || ch === '[') depth++;
    else if (ch === ')' || ch === '}' || ch === ']') depth--;
    if (ch === ',' && depth === 0) { out.push(cur); cur = ''; continue; }
    cur += ch;
  }
  if (cur.trim()) out.push(cur);
  return out.map(x => x.trim());
}

const literal = s => /^"([^"]+)"$/.exec(s || '')?.[1] ?? null;
const nameofArg = s => /^nameof\(\s*(?:\w+\.)?(\w+)\s*\)$/.exec(s || '')?.[1] ?? null;

/** Controller name implied by a file path: Controllers/XController.cs, or Views/X/Y.cshtml. */
function owningController(rel) {
  const c = /(\w+)Controller\.cs$/.exec(rel);
  if (c) return c[1];
  const v = /^Views\/([^/]+)\//.exec(rel);
  if (v && v[1] !== 'Shared') return v[1];
  return null;
}

function run() {
  console.log('\n--- check 2: redirect / route targets exist ------------------------------');
  const r = recorder('redirect-targets');
  const allow = new Set(loadAllowlist().routes || []);
  const index = buildIndex();

  const sources = [
    ...walk(path.join(PROJECT_ROOT, 'Controllers'), /\.cs$/),
    ...walk(path.join(PROJECT_ROOT, 'Views'), /\.cshtml$/),
  ].map(readSource);

  const refs = [];   // {kind, action, controller|null, rel, line}

  for (const src of sources) {
    const own = owningController(src.rel);

    // RedirectToAction( ... ) and Url.Action( ... )
    for (const m of src.code.matchAll(/\b(RedirectToAction|Url\.Action)\s*\(([^;]*?)\)\s*[;,)\]}]/g)) {
      const args = splitArgs(m[2]);
      const action = literal(args[0]) ?? nameofArg(args[0]);
      if (!action) continue;                                   // dynamic - never judged
      // 2nd arg is the controller only when it is a plain string; otherwise it's route values.
      const ctrl = literal(args[1]) ?? own;
      if (!ctrl) continue;
      refs.push({ kind: m[1], action, controller: ctrl, rel: src.rel, line: lineOf(src.code, m.index) });
    }

    // asp-action / asp-controller tag helpers (the actual nav links)
    if (src.rel.endsWith('.cshtml')) {
      for (const m of src.code.matchAll(/<[^>]*\basp-action\s*=\s*"([^"]+)"[^>]*>/g)) {
        const tag = m[0];
        const action = m[1];
        if (/[@(]/.test(action)) continue;                     // interpolated - not static
        const ctrlM = /\basp-controller\s*=\s*"([^"]+)"/.exec(tag);
        const ctrl = ctrlM ? ctrlM[1] : own;
        if (!ctrl || /[@(]/.test(ctrl)) continue;
        refs.push({ kind: 'asp-action', action, controller: ctrl, rel: src.rel, line: lineOf(src.code, m.index) });
      }
    }
  }

  console.log(`  scanned ${sources.length} files - ${refs.length} statically-resolvable route references`);

  const missingAction = [];
  const missingView = [];
  let ok = 0;

  for (const ref of refs) {
    const sig = `${ref.controller}/${ref.action}`;
    if (allow.has(sig)) continue;
    const ctrl = index.controllers.get(ref.controller);
    if (!ctrl) {
      // Controller genuinely absent (not merely un-indexed) is a hard finding.
      missingAction.push({ ...ref, why: `controller "${ref.controller}Controller" does not exist` });
      continue;
    }
    const action = ctrl.actions.get(ref.action);
    if (!action) {
      missingAction.push({ ...ref, why: `no action "${ref.action}" on ${ref.controller}Controller` });
      continue;
    }
    ok++;
    // Secondary: an action that renders a view whose file is absent throws InvalidOperationException
    // at runtime — a 500, strictly worse than the 404 above, and equally invisible to the compiler.
    for (const vn of action.views) {
      if (/[\/~\\]/.test(vn)) continue;            // path-style view name - MVC lookup not modelled
      if (!resolveViewFile(index.viewFiles, ref.controller, vn)) {
        missingView.push({ ...ref, why: `${sig} renders view "${vn}" - no file at Views/${ref.controller}/${vn}.cshtml or Views/Shared/${vn}.cshtml` });
      }
    }
  }

  if (missingAction.length === 0) {
    r.pass(`all ${ok} resolvable route references point at a real action`);
  } else {
    // Group: one finding per distinct broken target, listing every call site.
    const byTarget = new Map();
    for (const f of missingAction) {
      const k = `${f.controller}/${f.action}`;
      if (!byTarget.has(k)) byTarget.set(k, { why: f.why, sites: [] });
      byTarget.get(k).sites.push(`${f.rel}:${f.line} (${f.kind})`);
    }
    for (const [target, info] of byTarget) {
      r.fail(`route target "${target}" does not exist - ${info.why}`, {
        target, at: info.sites.join(', '),
        fix: 'point it at a real action, or delete the dead path - a user reaching this gets a 404 after a successful operation',
      });
    }
  }

  const seenView = new Set();
  for (const f of missingView) {
    if (seenView.has(f.why)) continue;
    seenView.add(f.why);
    r.fail(f.why, {
      at: `${f.rel}:${f.line}`,
      fix: 'add the view file or change the action to render an existing one - this throws InvalidOperationException (HTTP 500) when reached',
    });
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
