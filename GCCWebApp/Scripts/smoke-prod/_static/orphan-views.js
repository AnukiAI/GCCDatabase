// CHECK 3 — every view/partial is reachable, and every controller has a way in.
//
// THE BUG THIS EXISTS TO PREVENT
// Nothing tells you a file has stopped being used. `Views/Shared/_CertificateDocuments.cshtml` is
// the ONLY UI entry point to DocumentController — and it is rendered by nothing. That made the
// whole controller unreachable from the UI while its routes stayed live (an unscoped
// /Document/Download, an always-null uploader id, a 404 redirect). Dead code does not announce
// itself; it just sits there accumulating risk. See DEF-331.
//
// WHAT IS CHECKED
//   A. Partials (`_Name.cshtml`) that nothing renders                     -> WARN
//   B. Non-partial views with no matching controller action               -> WARN
//   C. Controllers whose actions are never linked to from any view        -> WARN (UI-unreachable)
//
// Everything here is WARN, deliberately. Dead code is a hygiene and security-surface finding, not
// a "this deploy is broken" finding — and reachability by direct URL is real. A WARN that names
// the file is enough for a human to decide remove-vs-keep. This check never blocks a deploy.

const path = require('path');
const { PROJECT_ROOT, walk, readSource, loadAllowlist, recorder } = require('./_lib/static-harness');
const { buildIndex } = require('./_lib/mvc-index');

// Framework files that are reached by convention, never by an explicit reference.
const CONVENTION_FILES = /(_ViewStart|_ViewImports)\.cshtml$/;
const LAYOUT_FILE = /_Layout[\w.-]*\.cshtml$/;

// Controllers with no UI surface by design — API controllers are called by JS/partners, and
// Error/Home style controllers are reached by convention or by the framework.
const API_LIKE = /^(Api\/|.*ApiController)/;

function run() {
  console.log('\n--- check 3: orphan views / unreachable controllers -----------------------');
  const r = recorder('orphan-views');
  const allow = loadAllowlist().deadcode || {};
  const index = buildIndex();

  const viewSources = walk(path.join(PROJECT_ROOT, 'Views'), /\.cshtml$/).map(readSource);
  const ctrlSources = walk(path.join(PROJECT_ROOT, 'Controllers'), /\.cs$/).map(readSource);

  // Everything that could name a view: Razor render calls, tag helpers, and controller View() calls.
  const allCode = [...viewSources, ...ctrlSources].map(s => s.code).join('\n');
  const referencedNames = new Set();
  const patterns = [
    /(?:Html\.)?(?:Partial|RenderPartial|PartialAsync|RenderPartialAsync)\s*\(\s*"([^"]+)"/g,
    /<partial\s+name\s*=\s*"([^"]+)"/g,
    /(?:Html\.)?RenderSection\s*\(\s*"([^"]+)"/g,
    /\b(?:Partial)?View\s*\(\s*"([^"]+)"/g,
    /Layout\s*=\s*"([^"]+)"/g,
    /@await\s+Html\.PartialAsync\s*\(\s*"([^"]+)"/g,
    /ViewComponent\s*\(\s*"([^"]+)"/g,
  ];
  for (const re of patterns) {
    for (const m of allCode.matchAll(re)) {
      // Reference may be a bare name or a path; index by basename so both forms match.
      referencedNames.add(m[1].replace(/^~?\/?(?:Views\/)?/, '').replace(/\.cshtml$/, '').split('/').pop());
    }
  }

  // ---- A + B: view files nothing can reach ------------------------------------------------
  const orphanPartials = [];
  const orphanViews = [];

  for (const v of viewSources) {
    const base = path.basename(v.rel, '.cshtml');
    const folder = v.rel.split('/')[1];               // Views/<folder>/File.cshtml
    if (CONVENTION_FILES.test(v.rel) || LAYOUT_FILE.test(v.rel)) continue;
    if (allow[v.rel]) continue;
    if (referencedNames.has(base)) continue;

    if (base.startsWith('_')) {
      orphanPartials.push(v.rel);
      continue;
    }
    // Non-partial. MVC resolves Views/{Controller}/{Action}.cshtml first, then falls back to
    // Views/Shared/{Action}.cshtml for ANY controller — so a Shared view is reachable if *any*
    // controller has an action of that name (this is how Views/Shared/Error.cshtml is reached).
    if (folder === 'Shared') {
      let reachable = false;
      for (const c of index.controllers.values()) if (c.actions.has(base)) { reachable = true; break; }
      if (reachable) continue;
    } else {
      const ctrl = index.controllers.get(folder);
      if (ctrl && ctrl.actions.has(base)) continue;
    }
    orphanViews.push(v.rel);
  }

  if (orphanPartials.length === 0) r.pass('no orphan partials');
  for (const p of orphanPartials) {
    r.warn(`partial ${p} is rendered by nothing`, {
      at: p,
      fix: 'remove it, or wire it in - if it is the only entry point to a controller, that controller is UI-dead too',
    });
  }

  if (orphanViews.length === 0) r.pass('every non-partial view has a matching action');
  for (const p of orphanViews) {
    r.warn(`view ${p} has no matching controller action and nothing renders it by name`, { at: p });
  }

  // ---- C: controllers with no inbound UI link ---------------------------------------------
  // Build the set of controllers any view links to (asp-controller, Url.Action, /Controller/ hrefs,
  // and JS fetch paths - the tile pages call their APIs from JS, which is a real entry point).
  const linkedControllers = new Set();
  const viewAndJs = [
    ...viewSources.map(s => s.code),
    ...walk(path.join(PROJECT_ROOT, 'wwwroot', 'js'), /\.js$/).map(a => readSource(a).code),
  ].join('\n');
  for (const m of viewAndJs.matchAll(/asp-controller\s*=\s*"([^"]+)"/g)) linkedControllers.add(m[1]);
  for (const m of viewAndJs.matchAll(/Url\.Action\s*\(\s*"[^"]+"\s*,\s*"([^"]+)"/g)) linkedControllers.add(m[1]);
  for (const m of viewAndJs.matchAll(/["'`]\/(\w+)\/\w+/g)) linkedControllers.add(m[1]);

  const uiDead = [];
  for (const [name, c] of index.controllers) {
    if (API_LIKE.test(c.rel.replace('Controllers/', '')) || API_LIKE.test(name + 'ApiController')) continue;
    if (/Api/.test(c.rel)) continue;
    if (allow[c.rel]) continue;
    if (linkedControllers.has(name)) continue;
    uiDead.push({ name, rel: c.rel, actions: c.actions.size });
  }

  if (uiDead.length === 0) r.pass('every MVC controller is linked to from the UI');
  for (const d of uiDead) {
    r.warn(`${d.rel} (${d.actions} actions) is never linked to from any view or JS - UI-unreachable, but its routes still answer`, {
      at: d.rel,
      fix: 'confirm intent: remove the controller, or accept it as URL-only and review its authorization explicitly',
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
