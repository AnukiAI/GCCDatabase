'use strict';
const { chromium } = require('playwright');
const BASE = 'http://localhost:5101';

(async () => {
  const b   = await chromium.launch({ headless: true });
  const ctx = await b.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  // Login as external
  await page.goto(BASE + '/Account/Login', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.locator('#Email').fill('external');
  await page.locator('#Password').fill('TestPass123!');
  await Promise.all([
    page.waitForLoadState('domcontentloaded'),
    page.locator('button[type="submit"]').first().click(),
  ]);
  await page.waitForTimeout(800);

  await page.goto(BASE + '/Certificate/CertificateTileViewExternal', { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(3000);

  // Check what hides .tc-actions -- check ancestors
  const info = await page.evaluate(() => {
    const el = document.querySelector('.sticky-header-cell.tc-actions');
    if (!el) return 'not found';

    // Walk up the tree to find first hidden ancestor
    let node = el;
    const chain = [];
    while (node && node !== document.body) {
      const s = window.getComputedStyle(node);
      chain.push({
        tag: node.tagName,
        id: node.id,
        class: Array.from(node.classList).join(' '),
        display: s.display,
        visibility: s.visibility,
        opacity: s.opacity,
      });
      node = node.parentElement;
    }
    return chain;
  });

  console.log('DOM ancestry of .sticky-header-cell.tc-actions:');
  if (Array.isArray(info)) {
    for (const item of info) {
      const hidden = item.display === 'none' || item.visibility === 'hidden' || item.opacity === '0';
      console.log(`  ${item.tag}${item.id ? '#' + item.id : ''}.${item.class.replace(/ /g, '.')} | display=${item.display} vis=${item.visibility} opacity=${item.opacity}${hidden ? ' <<HIDDEN' : ''}`);
    }
  } else {
    console.log(info);
  }

  // Is there a table-view toggle? What mode is the external view in by default?
  const isTableView = await page.locator('.view-mode-table, #tableViewBtn, [data-view="table"]').count();
  const isTileView = await page.locator('.view-mode-tile, #tileViewBtn, [data-view="tile"]').count();
  const activeView = await page.evaluate(() => {
    // Check if certificatesGrid has a class indicating tile vs table mode
    const grid = document.getElementById('certificatesGrid');
    return grid ? Array.from(grid.classList).join(' ') : 'grid not found';
  });
  console.log('\nView mode: table controls=' + isTableView + ' tile controls=' + isTileView);
  console.log('Grid classes:', activeView);

  // Check if the Actions column IS visible when you switch to table view
  const tableToggle = page.locator('[data-view="table"], #tableViewBtn, .btn-table-view, .view-toggle-table').first();
  const toggleCount = await tableToggle.count();
  if (toggleCount > 0) {
    console.log('\nSwitching to table view...');
    await tableToggle.click();
    await page.waitForTimeout(1000);
    const visAfter = await page.locator('.sticky-header-cell.tc-actions').isVisible();
    console.log('.tc-actions visible after table toggle:', visAfter);
  }

  // What action buttons are in the table rows for external users?
  const actionButtons = await page.locator('.tc-actions .action-buttons, .table-body-cell.tc-actions .action-buttons').count();
  console.log('\nAction button containers in rows:', actionButtons);

  // Check body cells for tc-actions
  const bodyCells = await page.locator('.table-body-cell.tc-actions').count();
  console.log('Table body tc-actions cells:', bodyCells);
  if (bodyCells > 0) {
    const firstCellHTML = await page.locator('.table-body-cell.tc-actions').first().innerHTML();
    console.log('First tc-actions cell content:', firstCellHTML.slice(0, 300));
  }

  await ctx.close();
  await b.close();
})().catch((e) => { console.error(e.message); process.exit(1); });
