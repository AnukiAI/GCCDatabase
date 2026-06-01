// Capture the full DOM around #ProductId on Cert Edit to find the visible widget
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

const BASE = 'http://localhost:5101';
const OUT = path.join(__dirname, 'screenshots', `def225-deepdom-${new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19)}`);
fs.mkdirSync(OUT, { recursive: true });

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();

  await page.goto(`${BASE}/Account/Login`);
  await page.fill('input[name="Email"]', 'analyst');
  await page.fill('input[name="Password"]', 'TestPass123!');
  await Promise.all([page.waitForLoadState('networkidle'), page.click('button[type="submit"]')]);

  await page.goto(`${BASE}/Certificate/Edit/26-0001`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(3000);

  const deepDom = await page.evaluate(() => {
    const sel = document.querySelector('#ProductId');
    if (!sel) return { error: 'no #ProductId' };

    // Walk up to find the form-group
    let container = sel;
    for (let i = 0; i < 5; i++) {
      if (container.parentElement && container.parentElement.classList.contains('form-group')) {
        container = container.parentElement;
        break;
      }
      container = container.parentElement || container;
    }

    // Get all visible non-script, non-style children of the container's parent
    const visibleSiblings = Array.from(container.children || []).map(c => ({
      tag: c.tagName,
      cls: c.className,
      id: c.id,
      visible: c.offsetParent !== null,
      offsetW: c.offsetWidth,
      offsetH: c.offsetHeight,
      text: c.textContent.trim().slice(0, 100),
      html: c.outerHTML.slice(0, 400),
    }));

    return {
      selectValue: sel.value,
      selectVisible: sel.offsetParent !== null,
      selectOffsetW: sel.offsetWidth,
      selectOffsetH: sel.offsetHeight,
      selectDisplay: window.getComputedStyle(sel).display,
      selectVisibility: window.getComputedStyle(sel).visibility,
      containerTag: container.tagName,
      containerClass: container.className,
      containerHTML: container.outerHTML.slice(0, 2500),
      siblings: visibleSiblings,
    };
  });

  fs.writeFileSync(path.join(OUT, 'deep-dom.json'), JSON.stringify(deepDom, null, 2));
  console.log('selectValue:', deepDom.selectValue);
  console.log('selectVisible:', deepDom.selectVisible, 'display:', deepDom.selectDisplay, 'visibility:', deepDom.selectVisibility, 'offsetW:', deepDom.selectOffsetW);
  console.log('container:', deepDom.containerTag, deepDom.containerClass);
  console.log('\n--- containerHTML ---');
  console.log(deepDom.containerHTML);

  await page.screenshot({ path: path.join(OUT, 'edit-full.png'), fullPage: true });

  await browser.close();
  console.log('\nOutput:', OUT);
})();
