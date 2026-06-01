// DEF-225 + DEF-226 reproduction test on localhost:5101
//
// DEF-225: Cert Edit shows blank Product Name autocomplete even though
//          "1 product(s) found" hint shows the products loaded.
//          Route is /Certificate/Edit/{number} where number = "26-0001".
//
// DEF-226: Admin -> Product Types: Usage column counts surprise the admin
//          when a cert is tagged at Category-FK only (no Type-FK). Tree
//          uses .tree-node > .node-content with .node-label, .node-count,
//          .action-btn.delete[data-id][data-is-category]. Delete posts to
//          /Admin/DeleteProductType/{id}?isCategory=true|false.

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const STAMP = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
const OUT = path.join(__dirname, 'screenshots', `def225-def226-${STAMP}`);
fs.mkdirSync(OUT, { recursive: true });

const VIEWPORTS = [
  { name: 'desktop-1920', width: 1920, height: 1080 },
  { name: 'desktop-1440', width: 1440, height: 900 },
  { name: 'tablet-1024', width: 1024, height: 768 },
  { name: 'mobile-768', width: 768, height: 1024 },
  { name: 'mobile-390', width: 390, height: 844 },
];

function saveText(name, t) {
  fs.writeFileSync(path.join(OUT, `${name}.txt`), typeof t === 'string' ? t : JSON.stringify(t, null, 2));
}
async function shot(page, name) {
  await page.screenshot({ path: path.join(OUT, `${name}.png`), fullPage: true });
  console.log('  shot:', name);
}

async function login(page, role) {
  await page.goto(`${BASE}/Account/Login`);
  await page.fill('input[name="Email"]', role);
  await page.fill('input[name="Password"]', 'TestPass123!');
  await Promise.all([page.waitForLoadState('networkidle'), page.click('button[type="submit"]')]);
}

async function testDef225(browser) {
  console.log('\n=== DEF-225: Cert Edit Product Name autocomplete ===');
  const results = { defect: 'DEF-225', viewports: [] };

  for (const vp of VIEWPORTS) {
    console.log(`\n[${vp.name}] ${vp.width}x${vp.height}`);
    const ctx = await browser.newContext({ viewport: { width: vp.width, height: vp.height } });
    const page = await ctx.newPage();
    page.on('pageerror', e => console.log(`  pageerror: ${e.message}`));

    try {
      await login(page, 'analyst');

      // Pick any cert with a Product. 26-0001 = cert Id=2, has ProductId=77 (Widget Pro).
      await page.goto(`${BASE}/Certificate/Edit/26-0001`);
      await page.waitForLoadState('networkidle');
      // Edit page uses setTimeout(50) on the pre-select; wait generously.
      await page.waitForTimeout(2500);

      await shot(page, `def225-${vp.name}-01-full`);

      const state = await page.evaluate(() => {
        const sel = document.querySelector('#ProductId');
        if (!sel) return { error: 'no #ProductId select found' };
        const selectedOption = sel.options[sel.selectedIndex];
        // The visible widget is .custom-dropdown-wrapper. The #ProductId form-group
        // contains both the <select> and the wrapper. Find the wrapper in the same form-group.
        const formGroup = sel.closest('.form-group');
        const wrapper = formGroup ? formGroup.querySelector('.custom-dropdown-wrapper') : null;
        const trigger = wrapper ? wrapper.querySelector('.custom-dropdown-trigger') : null;
        const display = wrapper ? wrapper.querySelector('.custom-dropdown-selected') : null;
        const helpText = document.querySelector('#productHelpText');
        return {
          selectValue: sel.value,
          selectedText: selectedOption ? selectedOption.textContent.trim() : null,
          selectOptionCount: sel.options.length,
          wrapperFound: !!wrapper,
          triggerText: trigger ? trigger.textContent.trim() : null,
          displayText: display ? display.textContent.trim() : null,
          displayColor: display ? window.getComputedStyle(display).color : null,
          selectDisplay: window.getComputedStyle(sel).display,
          selectWidth: sel.offsetWidth,
          helpText: helpText ? helpText.textContent.trim() : null,
        };
      });

      console.log('  Product field state:', JSON.stringify(state, null, 2));
      saveText(`def225-${vp.name}-state`, state);

      // DEFECT detection: underlying select has Widget Pro selected, but the
      // visible custom-dropdown-selected label still reads "Select product..."
      let defectReproduced = false;
      let detail = null;
      if (state.selectValue && state.selectedText && state.wrapperFound) {
        const display = (state.displayText || '').trim();
        // Bug: display reads the placeholder ("Select product...") despite select having a real value
        if (display && !display.includes(state.selectedText) && /select.*product/i.test(display)) {
          defectReproduced = true;
          detail = `select.value=${state.selectValue} (${state.selectedText}) but visible label="${display}"`;
        }
      }

      results.viewports.push({ viewport: vp.name, state, defectReproduced, detail });

      const productEl = page.locator('#ProductId').first();
      if (await productEl.count()) {
        await productEl.scrollIntoViewIfNeeded();
        await shot(page, `def225-${vp.name}-02-product-section`);
      }
    } catch (e) {
      console.error(`  FAIL ${vp.name}:`, e.message);
      results.viewports.push({ viewport: vp.name, error: e.message });
      await shot(page, `def225-${vp.name}-99-error`).catch(() => {});
    } finally {
      await ctx.close();
    }
  }

  return results;
}

async function testDef226(browser) {
  console.log('\n=== DEF-226: Admin Product Types Usage counts + delete ===');
  const results = { defect: 'DEF-226' };

  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  page.on('pageerror', e => console.log(`  pageerror: ${e.message}`));
  page.on('dialog', d => d.accept());

  try {
    await login(page, 'admin');
    await page.goto(`${BASE}/Admin/ProductTypes`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    await shot(page, 'def226-01-product-types-page');

    // Extract tree-node data
    const nodes = await page.evaluate(() => {
      return Array.from(document.querySelectorAll('.tree-node')).map(tn => {
        const content = tn.querySelector('.node-content');
        const label = tn.querySelector('.node-label');
        const count = tn.querySelector('.node-count');
        const editBtn = tn.querySelector('.action-btn.edit');
        const deleteBtn = tn.querySelector('.action-btn.delete');
        const isCategoryNode = content && content.classList.contains('category-node');
        const isTypeNode = content && content.classList.contains('type-node');
        const isSubcatNode = content && content.classList.contains('subcategory-node');
        return {
          id: tn.dataset.id,
          name: label ? label.textContent.trim() : null,
          countText: count ? count.textContent.trim() : null,
          isCategoryNode, isTypeNode, isSubcatNode,
          deleteIsCategory: deleteBtn ? deleteBtn.dataset.isCategory : null,
          deleteDataId: deleteBtn ? deleteBtn.dataset.id : null,
          deleteDataType: deleteBtn ? deleteBtn.dataset.type : null,
        };
      });
    });
    console.log(`  Tree nodes captured: ${nodes.length}`);
    saveText('def226-02-all-nodes', nodes);

    const testNode = nodes.find(n => n.name === 'DEF226-CatOnly');
    if (!testNode) {
      results.error = 'DEF226-CatOnly not in tree';
      saveText('def226-03-target-found', { error: results.error, nodes });
      return results;
    }
    console.log(`  DEF226-CatOnly: count="${testNode.countText}" id=${testNode.deleteDataId} is_category=${testNode.deleteIsCategory}`);
    results.targetNode = testNode;

    // Direct fetch the delete endpoint with the right params - that's what UI would do
    const deleteResult = await page.evaluate(async (target) => {
      const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
      const token = tokenInput ? tokenInput.value : null;
      const headers = { 'Accept': 'application/json' };
      if (token) headers['RequestVerificationToken'] = token;
      const resp = await fetch(`/Admin/DeleteProductType/${target.deleteDataId}?isCategory=${target.deleteIsCategory}`, {
        method: 'POST',
        headers,
      });
      let body = null;
      try { body = await resp.json(); } catch { body = await resp.text().catch(() => null); }
      return { status: resp.status, body };
    }, testNode);
    console.log('  Delete response:', JSON.stringify(deleteResult, null, 2));
    saveText('def226-04-delete-response', deleteResult);
    results.deleteResponse = deleteResult;

    // Validate the contradiction: delete blocks (status non-200 or success:false with message)
    // while the tree-node count showed "0 / 1" (0 products / 1 cert). We expect:
    // - delete BLOCKED with a specific "1 certificate(s)" message
    // - count text WAS "0 / 1" -- the "0" is products, the "1" is certs
    results.analysis = {
      countShownToAdmin: testNode.countText,
      deleteBlocked: deleteResult.status !== 200 && deleteResult.status !== 204,
      blockMessage: deleteResult.body && (deleteResult.body.message || deleteResult.body.error || String(deleteResult.body).slice(0, 200)),
      hypothesis: 'Count "0 / 1" means 0 products, 1 cert. If admin reads ONLY the products half and tries to delete the category, the cert-FK-only tag blocks it. DEF-226 hypothesis is that the surprise is the admin not realizing the second number is the blocker.',
    };

  } catch (e) {
    console.error('  FAIL DEF-226:', e.message);
    results.error = e.message;
    await shot(page, 'def226-99-error').catch(() => {});
  } finally {
    await ctx.close();
  }

  return results;
}

(async () => {
  const browser = await chromium.launch();
  try {
    const def225 = await testDef225(browser);
    const def226 = await testDef226(browser);

    saveText('00-summary', { def225, def226 });

    console.log('\n=== SUMMARY ===');
    const reproCount = def225.viewports.filter(v => v.defectReproduced).length;
    console.log(`DEF-225 reproduced in ${reproCount}/${def225.viewports.length} viewports`);
    def225.viewports.forEach(v => {
      if (v.state) {
        console.log(`  ${v.viewport}: select="${v.state.selectValue}" (${v.state.selectedText}), wrapperInput="${v.state.wrapperInputValue}", display="${v.state.wrapperDisplayText}", hint="${v.state.helpText}" -> ${v.defectReproduced ? 'REPRODUCED' : 'OK'}`);
      } else if (v.error) {
        console.log(`  ${v.viewport}: ERROR ${v.error}`);
      }
    });
    console.log('\nDEF-226 analysis:');
    console.log(JSON.stringify(def226.analysis, null, 2));
    console.log(`\nAll output in: ${OUT}`);
  } finally {
    await browser.close();
  }
})();
