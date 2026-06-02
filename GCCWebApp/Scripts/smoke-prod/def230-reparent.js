// DEF-230 / QC #170 (5/29 re-open) — Product Types edit-and-move probe.
//
// Verifies that UpdateProductTypeAsync now PERSISTS a re-parent (the bug was it
// saved only Name + SortOrder and silently dropped the parent change).
//
// Two parent semantics are exercised because the edit dropdown sends different
// id kinds depending on what you're editing:
//   - top-level TYPE      → parentId is a ProductCategory.Id  (move type between categories)
//   - SUBCATEGORY         → parentId is a ProductType.Id       (move sub between types)
//
// This script MUTATES the dev DB, so it is LOCALHOST ONLY and is fully
// self-contained: it creates throwaway "ZZ_Probe_*" nodes, moves them, asserts
// the move stuck, then deletes everything it created (cleanup runs in finally,
// and also sweeps any stray ZZ_Probe_* nodes from a prior aborted run).
//
// Run:  node Scripts/smoke-prod/def230-reparent.js
//       BASE_URL=http://localhost:5101 node Scripts/smoke-prod/def230-reparent.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const OUT = __dirname;
const BASE = process.env.BASE_URL || 'http://localhost:5101';
const APP_USER = 'admin';
const APP_PASS = 'TestPass123!';
const STAMP = Date.now();
const PREFIX = 'ZZ_Probe_'; // every node this script creates carries this prefix

const LIVE = BASE.includes('gccwebapp') || BASE.includes('greencirclecertified');
// Mutating probe. Localhost by default. Against the pre-launch test env it is allowed
// ONLY with an explicit opt-in (LIVE_OK=1) — it operates exclusively on throwaway
// ZZ_Probe_* rows that it creates and deletes, never real business records.
if (LIVE && process.env.LIVE_OK !== '1') {
  console.error('REFUSING TO RUN against the live test env without LIVE_OK=1. ' +
    'This probe only touches throwaway ZZ_Probe_* rows and self-cleans; re-run with LIVE_OK=1 to allow.');
  process.exit(2);
}
const GATE = LIVE ? { username: 'gcc', password: 'Cert-Test-Access-2026!' } : undefined;

const results = [];
const record = (id, name, status, notes) => {
  results.push({ id, name, status, notes });
  const tag = status === 'PASS' ? '✅' : status === 'FAIL' ? '❌' : '⚠️ ';
  console.log(`${tag} ${id} ${name}${notes ? ' — ' + notes : ''}`);
};

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ httpCredentials: GATE, viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  let token = null;
  let cookieHeader = '';
  const created = []; // { id, kind: 'type'|'sub', name } — cleanup targets

  // --- helpers -------------------------------------------------------------
  const authHeaders = () => ({
    'Content-Type': 'application/json',
    'X-Requested-With': 'XMLHttpRequest',
    'RequestVerificationToken': token,
    'Cookie': cookieHeader,
  });

  async function getTree() {
    const resp = await ctx.request.get(`${BASE}/Admin/GetProductTypes`);
    const body = await resp.json().catch(() => null);
    const tree = body?.data || body?.Data || body || [];
    const flat = [];
    (function walk(arr, parent) {
      if (!Array.isArray(arr)) return;
      for (const n of arr) {
        const node = {
          id: n.id ?? n.Id,
          name: n.name ?? n.Name,
          isCategory: n.isCategory ?? n.IsCategory ?? false,
          parentInTree: parent ? (parent.id ?? parent.Id) : null,
          raw: n,
        };
        flat.push(node);
        walk(n.children || n.subcategories || n.Children || n.Subcategories, n);
      }
    })(tree, null);
    return { tree, flat };
  }

  // childrenOf(parentId) — direct children as they render in the tree
  const childIdsOf = (flat, parentId) =>
    flat.filter(n => n.parentInTree === parentId).map(n => n.id);

  async function createNode(name, parentId, parentIsCategory) {
    // parentIsCategory === true  → creating a top-level type under a category
    // parentIsCategory === false → creating a subcategory under a type
    const r = await ctx.request.post(`${BASE}/Admin/CreateProductType`, {
      headers: authHeaders(),
      data: { name, sortOrder: 0, parentId, isCategory: parentIsCategory },
    });
    const b = await r.json().catch(() => null);
    const ok = (b?.success ?? b?.Success) === true;
    const id = b?.data?.id ?? b?.Data?.id ?? b?.data?.Id ?? b?.Data?.Id;
    return { ok, id, message: b?.message ?? b?.Message ?? '', http: r.status() };
  }

  // Editing a type or subcategory: isCategory is ALWAYS false (the edited item
  // is never a category here). parentId is the NEW parent (category id for a
  // type, type id for a subcategory).
  async function updateNode(id, name, newParentId) {
    const r = await ctx.request.post(`${BASE}/Admin/UpdateProductType/${id}`, {
      headers: authHeaders(),
      data: { id, name, sortOrder: 0, parentId: newParentId, isCategory: false },
    });
    const b = await r.json().catch(() => null);
    return {
      ok: (b?.success ?? b?.Success) === true,
      message: b?.message ?? b?.Message ?? '',
      http: r.status(),
    };
  }

  async function deleteNode(id, isCategory = false) {
    const r = await ctx.request.post(`${BASE}/Admin/DeleteProductType/${id}?isCategory=${isCategory}`, {
      headers: authHeaders(),
    });
    const b = await r.json().catch(() => null);
    return { ok: (b?.success ?? b?.Success) === true, message: b?.message ?? b?.Message ?? '' };
  }

  try {
    console.log(`\nDEF-230 edit-and-move probe — ${BASE}\n`);

    // login as admin
    await page.goto(BASE + '/Account/Login', { waitUntil: 'networkidle' });
    await page.locator('#Email').fill(APP_USER);
    await page.locator('#Password').fill(APP_PASS);
    await Promise.all([
      page.waitForLoadState('domcontentloaded'),
      page.locator('button[type="submit"]').first().click(),
    ]);
    await page.waitForTimeout(1500);
    if (/\/Account\/Login/i.test(page.url())) {
      record('SETUP-login', 'Admin login', 'FAIL', 'still on login page');
      throw new Error('login failed');
    }

    // anti-forgery token + cookies (Create/Update are [ValidateAntiForgeryToken])
    await page.goto(BASE + '/Admin/ProductTypes', { waitUntil: 'networkidle' });
    await page.waitForTimeout(800);
    token = await page.locator('input[name="__RequestVerificationToken"]').first()
      .getAttribute('value').catch(() => null);
    cookieHeader = (await ctx.cookies()).map(c => `${c.name}=${c.value}`).join('; ');
    if (!token) { record('SETUP-token', 'Anti-forgery token', 'FAIL', 'none on /Admin/ProductTypes'); throw new Error('no token'); }
    record('SETUP', 'Login + token + tree access', 'PASS', `token len=${token.length}`);

    // need at least two categories to move a type between them
    let { flat } = await getTree();
    const categories = flat.filter(n => n.isCategory);
    if (categories.length < 2) {
      record('SETUP-cats', 'Two categories available', 'WARN',
        `found ${categories.length}; type-move test needs 2 — skipping`);
    }
    const catA = categories[0];
    const catB = categories[1];

    // =====================================================================
    // TEST 1 — move a top-level TYPE from category A to category B
    // =====================================================================
    if (catA && catB) {
      console.log('\n--- TEST 1 — move top-level type across categories ---');
      const typeName = `${PREFIX}Type_${STAMP}`;
      const c = await createNode(typeName, catA.id, true);
      if (!c.ok || !c.id) {
        record('DEF230-T1-create', 'Create probe type under cat A', 'FAIL', `http=${c.http} msg="${c.message}"`);
      } else {
        created.push({ id: c.id, kind: 'type', name: typeName });
        // confirm it starts under A
        ({ flat } = await getTree());
        const underABefore = childIdsOf(flat, catA.id).includes(c.id);
        record('DEF230-T1-create', 'Probe type created under cat A', underABefore ? 'PASS' : 'FAIL',
          `id=${c.id}, under "${catA.name}"=${underABefore}`);

        // THE MOVE: parentId = catB.id (a ProductCategory.Id)
        const u = await updateNode(c.id, typeName, catB.id);
        record('DEF230-T1-update-call', 'Update returns success', u.ok ? 'PASS' : 'FAIL',
          `http=${u.http} msg="${u.message}"`);

        // THE ASSERTION: type now renders under B and no longer under A
        ({ flat } = await getTree());
        const underB = childIdsOf(flat, catB.id).includes(c.id);
        const underA = childIdsOf(flat, catA.id).includes(c.id);
        record('DEF230-T1-moved', 'Type now under cat B, not cat A (re-parent PERSISTED)',
          (underB && !underA) ? 'PASS' : 'FAIL',
          `under "${catB.name}"=${underB}, still under "${catA.name}"=${underA}`);
      }
    }

    // =====================================================================
    // TEST 2 — move a SUBCATEGORY from type A to type B (and inherit category)
    // =====================================================================
    console.log('\n--- TEST 2 — move subcategory across parent types ---');
    const tAName = `${PREFIX}TypeA_${STAMP}`;
    const tBName = `${PREFIX}TypeB_${STAMP}`;
    const subName = `${PREFIX}Sub_${STAMP}`;
    const parentForA = catA || categories[0];
    const parentForB = catB || categories[0];
    const tA = parentForA ? await createNode(tAName, parentForA.id, true) : { ok: false };
    const tB = parentForB ? await createNode(tBName, parentForB.id, true) : { ok: false };
    if (tA.ok && tA.id) created.push({ id: tA.id, kind: 'type', name: tAName });
    if (tB.ok && tB.id) created.push({ id: tB.id, kind: 'type', name: tBName });

    if (!tA.ok || !tB.ok || !tA.id || !tB.id) {
      record('DEF230-T2-setup', 'Create two probe parent types', 'FAIL',
        `A.ok=${tA.ok} B.ok=${tB.ok}`);
    } else {
      const sub = await createNode(subName, tA.id, false); // parent is a type → isCategory:false
      if (!sub.ok || !sub.id) {
        record('DEF230-T2-create-sub', 'Create probe subcategory under type A', 'FAIL',
          `http=${sub.http} msg="${sub.message}"`);
      } else {
        created.push({ id: sub.id, kind: 'sub', name: subName });
        ({ flat } = await getTree());
        const underTABefore = childIdsOf(flat, tA.id).includes(sub.id);
        record('DEF230-T2-create-sub', 'Subcategory created under type A', underTABefore ? 'PASS' : 'FAIL',
          `id=${sub.id}, under type A=${underTABefore}`);

        // THE MOVE: parentId = tB.id (a ProductType.Id)
        const u = await updateNode(sub.id, subName, tB.id);
        record('DEF230-T2-update-call', 'Update returns success', u.ok ? 'PASS' : 'FAIL',
          `http=${u.http} msg="${u.message}"`);

        ({ flat } = await getTree());
        const underTB = childIdsOf(flat, tB.id).includes(sub.id);
        const underTA = childIdsOf(flat, tA.id).includes(sub.id);
        record('DEF230-T2-moved', 'Subcategory now under type B, not type A (re-parent PERSISTED)',
          (underTB && !underTA) ? 'PASS' : 'FAIL',
          `under B=${underTB}, still under A=${underTA}`);

        // CategoryId inheritance: type B lives under catB, so the moved sub should
        // now render in catB's subtree. (Tree groups types by CategoryId.)
        if (catA && catB) {
          const subNode = flat.find(n => n.id === sub.id);
          // walk up: sub.parentInTree === tB.id; tB.parentInTree === catB.id
          const tBNode = flat.find(n => n.id === tB.id);
          const inherited = subNode && tBNode && tBNode.parentInTree === catB.id;
          record('DEF230-T2-category-inherit', 'Moved sub inherits new parent’s category (under cat B subtree)',
            inherited ? 'PASS' : 'FAIL',
            `sub.parent=${subNode?.parentInTree} (typeB=${tB.id}), typeB.parent=${tBNode?.parentInTree} (catB=${catB.id})`);
        }

        // ---- Negative guard: self-parent is rejected -----------------------
        const selfParent = await updateNode(sub.id, subName, sub.id);
        const selfBlocked = !selfParent.ok && /cannot be its own parent/i.test(selfParent.message);
        record('DEF230-T2-self-parent', 'Self-parent rejected', selfBlocked ? 'PASS' : 'FAIL',
          `ok=${selfParent.ok} msg="${selfParent.message}"`);
      }
    }

    // =====================================================================
    // TEST 3 — duplicate-name guard on a move (type renamed into a taken name)
    // =====================================================================
    if (catA) {
      console.log('\n--- TEST 3 — duplicate-name guard on move/edit ---');
      const dupName = `${PREFIX}Dup_${STAMP}`;
      const t1 = await createNode(dupName, catA.id, true);
      const t2 = await createNode(`${PREFIX}Other_${STAMP}`, catA.id, true);
      if (t1.ok && t1.id) created.push({ id: t1.id, kind: 'type', name: dupName });
      if (t2.ok && t2.id) created.push({ id: t2.id, kind: 'type', name: `${PREFIX}Other_${STAMP}` });
      if (t1.ok && t2.ok) {
        // try to rename t2 to t1's name within the same category → must be blocked
        const dup = await updateNode(t2.id, dupName, catA.id);
        const blocked = !dup.ok && /already exists/i.test(dup.message);
        record('DEF230-T3-dup-name', 'Duplicate name under same category rejected',
          blocked ? 'PASS' : 'FAIL', `ok=${dup.ok} msg="${dup.message}"`);
      } else {
        record('DEF230-T3-dup-name', 'Duplicate-name setup', 'WARN', 'could not create both probe types');
      }
    }

  } catch (e) {
    console.log(`ERROR: ${e.message}`);
    record('ERR', 'Unexpected exception', 'FAIL', e.message);
  } finally {
    // ---- CLEANUP: delete everything we created, subs first then types -----
    // Re-fetch the tree and sweep ALL ZZ_Probe_* nodes (covers this run plus
    // any orphan from a previously aborted run). Subcategories (have a parent
    // type in the tree) are deleted before their parent types.
    try {
      const { flat } = await getTree();
      const probeNodes = flat.filter(n => typeof n.name === 'string' && n.name.startsWith(PREFIX) && !n.isCategory);
      const subs = probeNodes.filter(n => n.parentInTree !== null && !flat.find(p => p.id === n.parentInTree)?.isCategory);
      const types = probeNodes.filter(n => !subs.includes(n));
      let removed = 0, failed = 0;
      for (const n of [...subs, ...types]) {
        const d = await deleteNode(n.id, false);
        if (d.ok) removed++; else { failed++; console.log(`  cleanup: failed to delete ${n.name} (${n.id}) — ${d.message}`); }
      }
      record('DEF230-cleanup', 'Probe nodes removed', failed === 0 ? 'PASS' : 'WARN',
        `removed=${removed}, failed=${failed} (created this run=${created.length})`);
    } catch (ce) {
      record('DEF230-cleanup', 'Cleanup sweep', 'WARN', ce.message);
    }

    fs.writeFileSync(path.join(OUT, 'def230-reparent-results.json'), JSON.stringify(results, null, 2));
    await browser.close();
  }

  const pass = results.filter(r => r.status === 'PASS').length;
  const fail = results.filter(r => r.status === 'FAIL').length;
  const warn = results.filter(r => r.status === 'WARN').length;
  console.log(`\nSUMMARY: ${pass} PASS / ${fail} FAIL / ${warn} WARN  (of ${results.length})`);
  process.exit(fail > 0 ? 1 : 0);
})();
