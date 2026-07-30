/**
 * DEF-312 — Import Preview "select all" must act only on the rows the user can SEE.
 *
 * Found by the Gate 1 UI audit on the 2026-07-20 deploy bundle. The CR-025 status/search
 * filter made the pre-existing blanket select-all a data hazard: filter a large import down
 * to its few error rows, click the header checkbox, and every hidden row got selected too —
 * the only cue being the Apply button's count.
 *
 * This is a pure-logic harness. It loads the real `product-id-manager.js` in a minimal DOM
 * shim and drives the actual methods, so it asserts the shipped code rather than a
 * reimplementation. No browser, no server, no DB.
 *
 * Usage: node def312-select-all-scoping.js
 */

const fs = require('fs');
const path = require('path');
const vm = require('vm');

const results = [];
function check(name, pass, detail) {
  results.push({ name, pass, detail });
  console.log(`${pass ? 'PASS' : 'FAIL'}  ${name}${detail ? ` — ${detail}` : ''}`);
}

// --- DOM shim: the file runs init code at load, so elements need a realistic surface ---
const makeEl = () => {
  const el = {
    style: {}, dataset: {}, children: [], value: '', textContent: '', innerHTML: '',
    checked: false, disabled: false, options: [], classList: {
      add() {}, remove() {}, toggle() {}, contains() { return false; },
    },
    hasAttribute() { return false; },
    setAttribute() {}, getAttribute() { return null; }, removeAttribute() {},
    addEventListener() {}, removeEventListener() {}, dispatchEvent() { return true; },
    appendChild(c) { el.children.push(c); return c; }, removeChild() {},
    querySelector: () => makeEl(), querySelectorAll: () => [],
    closest: () => null, focus() {}, click() {}, remove() {},
    insertAdjacentElement() {}, insertAdjacentHTML() {},
  };
  return el;
};
const sandbox = {
  console, setTimeout, clearTimeout, setInterval, clearInterval,
  document: {
    getElementById: () => makeEl(),
    querySelector: () => makeEl(),
    querySelectorAll: () => [],
    createElement: () => makeEl(),
    addEventListener() {},
    body: makeEl(),
    readyState: 'complete',
  },
  window: {},
  navigator: { userAgent: 'node' },
  location: { href: 'http://localhost:5101/', search: '' },
  fetch: () => Promise.reject(new Error('network disabled in this harness')),
};
sandbox.window = sandbox;
sandbox.globalThis = sandbox;

// PIM_PATH lets the harness run against an alternate copy of the file — used to prove this
// test actually FAILS against the pre-fix code (a regression test that can't fail is useless).
const pimPath = process.env.PIM_PATH
  || path.join(__dirname, '..', '..', 'wwwroot', 'js', 'product-id-manager.js');
const src = fs.readFileSync(pimPath, 'utf8');
vm.createContext(sandbox);
try {
  // `class ProductIdManager {...}` at top level is a LEXICAL binding — it never becomes a
  // property of the global object, so the sandbox can't see it without an explicit hand-off.
  vm.runInContext(
    `${src}\n;globalThis.__ProductIdManager = typeof ProductIdManager !== 'undefined' ? ProductIdManager : null;`,
    sandbox,
    { filename: 'product-id-manager.js' });
} catch (e) {
  check('load product-id-manager.js', false, e.message);
}

const ProductIdManager = sandbox.__ProductIdManager;
check('ProductIdManager class loaded', typeof ProductIdManager === 'function',
  typeof ProductIdManager);
if (typeof ProductIdManager !== 'function') {
  console.log(`\n${results.filter(r => r.pass).length}/${results.length} PASS`);
  process.exit(1);
}

/** Build a manager with a synthetic 1,000-row batch: 3 errors, rest new/modified. */
function makeManager() {
  const m = Object.create(ProductIdManager.prototype);
  m.importStatusFilter = null;
  m.importSearchTerm = '';
  m.importPreviewData = Array.from({ length: 1000 }, (_, i) => ({
    status: i < 3 ? 'error' : (i % 2 ? 'modified' : 'new'),
    selected: false,
    errors: i < 3 ? ['Invalid ASIN format'] : [],
    data: {
      productIdType: 'ASIN',
      productIdValue: `B00${String(i).padStart(6, '0')}`,
      productName: i === 500 ? 'Needle In Haystack Widget' : `Product ${i}`,
      productSku: `SKU-${i}`,
      partner: 'Amazon',
    },
  }));
  return m;
}
const selectedCount = (m) => m.importPreviewData.filter((r) => r.selected).length;

// --- 1. no filter: unchanged behaviour (every non-error row) ---
let m = makeManager();
m.toggleAllImportRows(true);
check('no filter → selects all non-error rows (behaviour unchanged)',
  selectedCount(m) === 997, `selected=${selectedCount(m)} expected=997`);

// --- 2. status filter = error: errors are never selectable, so nothing gets selected ---
m = makeManager();
m.importStatusFilter = 'error';
m.toggleAllImportRows(true);
check('filter=error → selects nothing (error rows are not importable)',
  selectedCount(m) === 0, `selected=${selectedCount(m)} expected=0`);

// --- 3. THE REGRESSION: status filter = modified must not touch the hidden 'new' rows ---
m = makeManager();
m.importStatusFilter = 'modified';
m.toggleAllImportRows(true);
const modifiedTotal = m.importPreviewData.filter((r) => r.status === 'modified').length;
const leakedNew = m.importPreviewData.filter((r) => r.status === 'new' && r.selected).length;
check('filter=modified → selects ONLY modified rows',
  selectedCount(m) === modifiedTotal, `selected=${selectedCount(m)} expected=${modifiedTotal}`);
check('filter=modified → zero hidden "new" rows selected (the DEF-312 hazard)',
  leakedNew === 0, `leaked=${leakedNew} expected=0`);

// --- 4. search filter narrows to one row ---
m = makeManager();
m.importSearchTerm = 'needle in haystack';
m.toggleAllImportRows(true);
check('search filter → selects only the matching row',
  selectedCount(m) === 1, `selected=${selectedCount(m)} expected=1`);

// --- 5. hidden rows KEEP their prior selection (we neither select nor deselect for them) ---
m = makeManager();
m.importPreviewData.forEach((r) => { if (r.status !== 'error') r.selected = true; }); // all selected
m.importStatusFilter = 'modified';
m.toggleAllImportRows(false); // user unchecks select-all while filtered to 'modified'
const newStillSelected = m.importPreviewData.filter((r) => r.status === 'new' && r.selected).length;
const modifiedStillSelected = m.importPreviewData.filter((r) => r.status === 'modified' && r.selected).length;
const newTotal = m.importPreviewData.filter((r) => r.status === 'new').length;
check('uncheck while filtered → clears visible rows only',
  modifiedStillSelected === 0, `modified still selected=${modifiedStillSelected} expected=0`);
check('uncheck while filtered → hidden rows retain their selection',
  newStillSelected === newTotal, `new still selected=${newStillSelected} expected=${newTotal}`);

// --- 6. clearing the filter restores full-batch behaviour ---
m = makeManager();
m.importStatusFilter = 'modified';
m.importSearchTerm = 'nothing matches this';
m.importStatusFilter = null;
m.importSearchTerm = '';
m.toggleAllImportRows(true);
check('filter cleared → back to full-batch select-all',
  selectedCount(m) === 997, `selected=${selectedCount(m)} expected=997`);

const failed = results.filter((r) => !r.pass);
console.log(`\n${results.length - failed.length}/${results.length} PASS`);
process.exit(failed.length ? 1 : 0);
