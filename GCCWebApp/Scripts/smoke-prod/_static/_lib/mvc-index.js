// Builds an index of the app's MVC surface: every controller, its action methods, and the views
// each action renders. Shared by `tempdata-pairing.js` (needs "does the redirect target read this
// key?") and `redirect-targets.js` (needs "does this action exist?").
//
// Read-only. Parses comment-stripped source; see the safety contract in `static-harness.js`.

const fs = require('fs');
const path = require('path');
const { PROJECT_ROOT, walk, readSource, lineOf } = require('./static-harness');

/**
 * Find the index just past the `}` that closes the `{` at or after `from`.
 * Skips braces inside string/char literals. Input is already comment-stripped.
 * Returns -1 if unbalanced (truncated file, parse drift) — callers must handle it.
 */
function matchBrace(code, from) {
  let i = code.indexOf('{', from);
  if (i === -1) return -1;
  let depth = 0;
  const n = code.length;
  while (i < n) {
    const c = code[i];
    if (c === '@' && code[i + 1] === '"') {
      i += 2;
      while (i < n) {
        if (code[i] === '"') { if (code[i + 1] === '"') { i += 2; continue; } i++; break; }
        i++;
      }
      continue;
    }
    if (c === '"' || c === "'") {
      const q = c; i++;
      while (i < n) {
        if (code[i] === '\\') { i += 2; continue; }
        if (code[i] === q || code[i] === '\n') { i++; break; }
        i++;
      }
      continue;
    }
    if (c === '{') depth++;
    else if (c === '}') { depth--; if (depth === 0) return i + 1; }
    i++;
  }
  return -1;
}

// A method signature: optional attributes, an access modifier, optional async, a return type, a
// name, and an opening paren. Anchored to a line start so it does not match calls.
const METHOD_RE =
  /^[ \t]*(?:public|private|protected|internal)[ \t]+(?:static[ \t]+)?(?:async[ \t]+)?([\w<>,.\[\]?]+(?:<[^>()\n]*>)?)[ \t]+(\w+)[ \t]*\(/gm;

const NON_ACTION_RETURN_TYPES = new Set(['void', 'bool', 'string', 'int', 'Task']);

/**
 * @returns {{
 *   controllers: Map<string, {name, rel, actions: Map<string, {name, routeName, rel, line, body, bodyStart, views: string[]}>}>,
 *   viewFiles: Set<string>            // repo-relative, forward slashes
 * }}
 */
function buildIndex() {
  const controllers = new Map();
  const controllerFiles = walk(path.join(PROJECT_ROOT, 'Controllers'), /Controller\.cs$/);

  for (const abs of controllerFiles) {
    const src = readSource(abs);
    const classMatch = /class\s+(\w+)Controller\b/.exec(src.code);
    if (!classMatch) continue;
    const name = classMatch[1];

    const actions = new Map();
    METHOD_RE.lastIndex = 0;
    let m;
    while ((m = METHOD_RE.exec(src.code)) !== null) {
      const [, returnType, methodName] = m;
      if (methodName === name + 'Controller') continue;              // constructor
      if (NON_ACTION_RETURN_TYPES.has(returnType)) continue;         // helper, not an action
      const bodyStart = src.code.indexOf(')', m.index);
      const end = matchBrace(src.code, bodyStart);
      if (end === -1) continue;                                      // expression-bodied or unbalanced
      const body = src.code.slice(bodyStart, end);

      // Attributes sit on the lines immediately above the signature.
      const above = src.code.slice(Math.max(0, m.index - 400), m.index);
      if (/\[NonAction\]/.test(above.split('\n').slice(-6).join('\n'))) continue;
      const actionNameAttr = /\[ActionName\("([^"]+)"\)\]/.exec(above.split('\n').slice(-6).join('\n'));

      const routeName = actionNameAttr ? actionNameAttr[1] : methodName;
      const line = lineOf(src.code, m.index);

      // Which views can this action render?
      const views = new Set();
      // return View("Name" ...)  /  return PartialView("Name" ...)
      for (const v of body.matchAll(/\b(?:Partial)?View\(\s*"([^"]+)"/g)) views.add(v[1]);
      // return View() / View(model) -> convention: Views/{Controller}/{Action}.cshtml
      if (/\b(?:Partial)?View\(\s*(?:\)|[A-Za-z_])/.test(body)) views.add(routeName);

      // Overloads (GET + POST Upload) share a name — merge rather than overwrite.
      const existing = actions.get(routeName);
      if (existing) {
        existing.views = [...new Set([...existing.views, ...views])];
        existing.bodies.push({ body, bodyStart, line, rel: src.rel });
      } else {
        actions.set(routeName, {
          name: methodName, routeName, rel: src.rel, line,
          views: [...views],
          bodies: [{ body, bodyStart, line, rel: src.rel }],
        });
      }
    }

    controllers.set(name, { name, rel: src.rel, abs, actions });
  }

  const viewFiles = new Set(
    walk(path.join(PROJECT_ROOT, 'Views'), /\.cshtml$/)
      .map(a => path.relative(PROJECT_ROOT, a).replace(/\\/g, '/'))
  );

  return { controllers, viewFiles };
}

/**
 * Resolve an action's view name to an actual file, honouring MVC's lookup order:
 * Views/{Controller}/{name}.cshtml then Views/Shared/{name}.cshtml.
 * Returns the repo-relative path, or null if neither exists.
 */
function resolveViewFile(viewFiles, controllerName, viewName) {
  const own = `Views/${controllerName}/${viewName}.cshtml`;
  if (viewFiles.has(own)) return own;
  const shared = `Views/Shared/${viewName}.cshtml`;
  if (viewFiles.has(shared)) return shared;
  return null;
}

module.exports = { buildIndex, resolveViewFile, matchBrace };
