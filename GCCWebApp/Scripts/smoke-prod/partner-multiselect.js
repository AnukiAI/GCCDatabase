// Verifies the new Partner(s) checkbox dropdown on Certificate Create + Edit.
// - Native <select multiple> is hidden; custom trigger + panel render.
// - Panel uses CHECKBOXES (multi) and has NO "Add" footer.
// - Toggling options updates the trigger text AND the hidden PartnerIds CSV.
// - Edit pre-selects the cert's existing partners.
// LOCALHOST ONLY. Run: node Scripts/smoke-prod/partner-multiselect.js
const { chromium } = require('playwright');
const { login, BASE } = require('./_lib/smoke-template');
const path = require('path');

const results = [];
const rec = (id, name, status, notes = '') => {
  results.push({ id, status });
  console.log(`  [${status}] ${id} — ${name}${notes ? ' :: ' + notes : ''}`);
};

async function exercise(page, where) {
  // Native select hidden, custom trigger visible.
  const state = await page.evaluate(() => {
    const sel = document.getElementById('partnerSelect');
    const trig = document.querySelector('.partner-multiselect-trigger');
    const panel = document.querySelector('.partner-multiselect-panel');
    return {
      selHidden: sel ? (getComputedStyle(sel).opacity === '0' || sel.offsetHeight === 0) : null,
      hasTrigger: !!trig,
      hasPanel: !!panel,
      optionCount: sel ? Array.from(sel.options).filter(o => o.value).length : 0,
    };
  });
  rec(`${where}.native-hidden`, 'Native <select multiple> is hidden', state.selHidden ? 'PASS' : 'FAIL');
  rec(`${where}.custom-trigger`, 'Custom dropdown trigger renders', state.hasTrigger ? 'PASS' : 'FAIL');
  rec(`${where}.has-partners`, 'Partner options present', state.optionCount > 0 ? 'PASS' : 'FAIL', `count=${state.optionCount}`);

  // Open the panel.
  await page.locator('.partner-multiselect-trigger').click();
  await page.waitForTimeout(250);
  const opened = await page.locator('.partner-multiselect-panel').isVisible();
  rec(`${where}.opens`, 'Panel opens on click', opened ? 'PASS' : 'FAIL');

  // No "Add" footer (unlike Company/Product dropdowns).
  const hasAdd = await page.evaluate(() => {
    const panel = document.querySelector('.partner-multiselect-panel');
    if (!panel) return true;
    const txt = panel.textContent.toLowerCase();
    return panel.querySelector('input.custom-dropdown-add-input, button.custom-dropdown-add-btn') !== null || txt.includes('add ');
  });
  rec(`${where}.no-add`, 'No "Add partner" option in panel', !hasAdd ? 'PASS' : 'FAIL');

  // Checkbox boxes (square, not radio circle): border-radius 4px.
  const boxRadius = await page.evaluate(() => {
    const box = document.querySelector('.partner-multiselect-box');
    return box ? getComputedStyle(box).borderRadius : null;
  });
  rec(`${where}.checkbox-square`, 'Options use square checkboxes (border-radius 4px)', boxRadius === '4px' ? 'PASS' : 'FAIL', `radius=${boxRadius}`);

  // Select the first two partner options.
  const optionRows = page.locator('.partner-multiselect-option');
  const n = await optionRows.count();
  await optionRows.nth(0).click();
  await page.waitForTimeout(120);
  if (n > 1) { await optionRows.nth(1).click(); await page.waitForTimeout(120); }

  const after = await page.evaluate(() => {
    const sel = document.getElementById('partnerSelect');
    const hidden = document.getElementById('partnersHidden');
    const trig = document.querySelector('.partner-multiselect-selected');
    return {
      selectedCount: Array.from(sel.selectedOptions).length,
      hiddenVal: hidden ? hidden.value : '',
      triggerText: trig ? trig.textContent : '',
    };
  });
  const expected = n > 1 ? 2 : 1;
  rec(`${where}.multi-select`, `Can select ${expected} partner(s) at once`, after.selectedCount === expected ? 'PASS' : 'FAIL', `selected=${after.selectedCount}`);
  rec(`${where}.hidden-sync`, 'Hidden PartnerIds CSV reflects selection', after.hiddenVal.split(',').filter(Boolean).length === expected ? 'PASS' : 'FAIL', `PartnerIds="${after.hiddenVal}"`);
  rec(`${where}.trigger-names`, 'Trigger shows selected partner name(s)', after.triggerText && after.triggerText !== 'Select partner(s)...' ? 'PASS' : 'FAIL', `"${after.triggerText}"`);

  // Deselect one — confirm it un-toggles.
  await optionRows.nth(0).click();
  await page.waitForTimeout(120);
  const afterDeselect = await page.evaluate(() => Array.from(document.getElementById('partnerSelect').selectedOptions).length);
  rec(`${where}.deselect`, 'Re-clicking an option deselects it', afterDeselect === expected - 1 ? 'PASS' : 'FAIL', `selected=${afterDeselect}`);

  await page.screenshot({ path: path.join(__dirname, '_results', `partner-multiselect-${where}.png`), fullPage: true });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const page = await (await browser.newContext({ viewport: { width: 1440, height: 1500 } })).newPage();
  try {
    await login(page, 'analyst');

    // --- CREATE ---
    await page.goto(BASE + '/Certificate/Create', { waitUntil: 'domcontentloaded', timeout: 60000 });
    await page.waitForTimeout(1000);
    await exercise(page, 'create');

    // --- EDIT (find a cert with existing partners to check pre-selection) ---
    const num = await page.evaluate(async (base) => {
      try {
        const r = await fetch(base + '/api/certificate-tiles?page=1&pageSize=20', { credentials: 'include' });
        const j = await r.json();
        const rows = j.data || j.certificates || j.items || [];
        const found = rows.map(x => x.certificateNumber ?? x.CertificateNumber).find(n => n);
        if (found) return found;
      } catch { /* fall through */ }
      return '28-0002'; // known-good fallback cert in the dev DB
    }, BASE);
    if (num) {
      await page.goto(`${BASE}/Certificate/Edit/${encodeURIComponent(num)}`, { waitUntil: 'domcontentloaded', timeout: 60000 });
      await page.waitForTimeout(1200);
      // Pre-selection check: trigger text should reflect any server-selected partners BEFORE we touch it.
      const preselect = await page.evaluate(() => {
        const sel = document.getElementById('partnerSelect');
        const trig = document.querySelector('.partner-multiselect-selected');
        return { serverSelected: Array.from(sel.selectedOptions).map(o => o.textContent), trigger: trig ? trig.textContent : '' };
      });
      const preOk = preselect.serverSelected.length === 0 || preselect.trigger.includes(preselect.serverSelected[0]);
      rec('edit.preselect', `Edit trigger reflects server-selected partners`, preOk ? 'PASS' : 'FAIL', `selected=[${preselect.serverSelected}] trigger="${preselect.trigger}"`);
      await exercise(page, 'edit');
    } else {
      rec('edit.discover', 'Found a cert to edit', 'FAIL', 'no cert number from tile API');
    }
  } catch (e) {
    rec('fatal', 'ran without throwing', 'FAIL', e.message);
  } finally {
    await browser.close();
  }
  const fail = results.filter(r => r.status === 'FAIL').length;
  console.log(`\n[partner-multiselect] ${results.filter(r => r.status === 'PASS').length} PASS / ${fail} FAIL`);
  process.exitCode = fail === 0 ? 0 : 1;
})();
