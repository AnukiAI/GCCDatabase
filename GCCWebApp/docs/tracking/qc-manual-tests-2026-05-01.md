# Manual Test Steps — Ready-for-UAT Items Needing Visual Review

**Source:** companion to `qc-readyforuat-2026-05-01.md` (the full validation pass)
**Scope:** 13 items where Playwright couldn't reliably reach the test path; manual review required
**Best-run after:** `GCCWebApp-test-2026-05-01-0006.zip` deploys to gccwebapp.com
**Default credentials:** `analyst / TestPass123!` unless noted

**Suggested execution mode:** open Chrome DevTools (F12), pick a viewport from the device toolbar when mobile is required. Take a screenshot at each pass step so the receipts go into `docs/tracking/screenshots/`.

---

## QC #37 — Tags too close to bottom on mobile tile

**URL:** `https://gccwebapp.com/` (no login)
**Viewport:** Mobile (e.g., DevTools → iPhone 12 / 390 × 844)

**Steps:**
1. Open the public view on a mobile viewport
2. Scroll until 4–6 cert tiles are visible
3. Locate any tile with tags rendered (small pill labels at/near the bottom)

**Pass criteria:**
- Padding/margin between bottom-most tag pill and the tile bottom border is **≥ 8px** (visually clear gap)
- Tags are not flush against or overlapping the tile bottom edge
- Tile drop-shadow (if any) doesn't clip the tags

**If fail:** the fix lives in `wwwroot/css/certificate-tile-external.css` mobile breakpoint — bump `padding-bottom` on `.certificate-card` or `margin-bottom` on `.cert-tags`.

---

## QC #60 — Cert image cut off + buttons not aligned after creation

**URL:** `/Certificate/Create`
**Login:** `analyst`
**Viewport:** Desktop (1920 × 1080)

**Steps:**
1. Log in, navigate to Create cert page
2. Fill required fields: Cert # (e.g. `26-TEST01`), Company, Product, Cert Type, Effective Date, Expiration Date
3. **Upload a real PDF** (the original complaint specifically involved a cert with a PDF attachment)
4. Click **Save**
5. Navigate to the new cert's **Detail view** (not Edit)

**Pass criteria:**
- Cert PDF preview / image renders **without bottom cutoff** (you can see the bottom edge of the document)
- No `overflow: hidden` clipping
- **Zoom + Download icons are RIGHT-aligned** with the image (memory rule `feedback_cert_detail_action_icons_right_aligned.md` — NOT centered)
- Both icons clickable and functional

**If fail:** look in `Views/Certificate/Details.cshtml` for the cert-image container — likely a `max-height` constraint or `overflow: hidden` on the wrapper. **DO NOT center the icons** — right-aligned is the intended outcome.

---

## QC #80 — Product ID Manager Edit doesn't work

**URL:** `/Certificate/CertificateTileViewInternal` → click any cert with existing Product IDs → Edit
**Login:** `analyst`
**Viewport:** Desktop

**Steps:**
1. Log in, open the internal cert tile view
2. Click any cert that has Product IDs (look for ASIN/UPC values in the row)
3. On Edit, scroll to the **Product Identifiers** section (the Product ID Manager component)
4. On any existing row, click the **pencil (Edit) icon** in the actions column
5. **Inspect the open modal:**

**Pass criteria:**
- Modal opens within ~1s
- All fields are interactive (NOT grayed out, NOT `readonly`):
  - Type (dropdown: ASIN/UPC/GTIN/EAN)
  - Product ID (text)
  - Partner (dropdown)
  - ProductName, ProductSku (text)
  - Description (textarea)
- You can type into Product ID and the typed text appears
- **Status field is GONE** (per CR-015 — only the 6 fields above)
- Click Save → modal closes, the row in the list updates with new values

**If fail:** open browser DevTools → Console tab → look for JS errors on Edit click. Common causes: missing `productIdManager.editProductId(id)` function, frontend race condition, or stale modal markup. Check `wwwroot/js/product-id-manager.js`.

---

## QC #95 (4 of 7 columns) — Sort behavior on indeterminate columns

**URL:** `/Product/Management`
**Login:** `analyst`
**Viewport:** Desktop

**Steps:**
1. Log in, open Product Management
2. For each of these 4 columns, click the column header twice and observe row order:
   - **Category**
   - **Product Type**
   - **Subcategory**
   - **Status**
3. The first click should sort ascending; the second click should reverse to descending

**Pass criteria:**
- After 1st click: rows reorder (asc by that column)
- After 2nd click: rows reorder again (desc by that column)
- Sort caret in header flips direction (▲ ↔ ▼)

**Why couldn't be auto-tested:** dev DB has every product in "Chemicals and Raw Materials" with empty Product Type/Subcategory and all "Active" status — no variance to detect a sort change. **Prod data should have category/type/subcat diversity to make this testable.**

**If fail:** check `Management.cshtml` `sortMap` translation logic — column names like "Product Type" need to match the JS sort key.

---

## QC #96 — Nav appearing behind search bar (z-index)

**URL:** `/Certificate/CertificateTileViewInternal`
**Login:** `analyst`
**Viewport:** Desktop

**Steps:**
1. Log in, open internal cert tile view
2. Click into the search input at top
3. Type 1–2 characters (e.g. "Am") to trigger the search-suggestions dropdown
4. Observe whether the suggestions overlap the **main top header nav** (the bar with logo, nav links, user menu)

**Pass criteria:**
- Search-suggestions dropdown does NOT render on top of the main top nav
- If it does extend upward, the main nav stays visible and clickable

**If fail:** the fix is to bump the main top nav's `z-index` higher than the `.search-bar` (`z-index: 1001`). Look in `wwwroot/css/site.css` or `main.css` for `.navbar`/`.top-nav` rules.

---

## QC #123 — Cert #23-0375 special characters

**URL:** `https://gccwebapp.com/` (public view)
**Login:** none

**Steps:**
1. Open public view
2. In search, type `23-0375` (exact cert number)
3. Click the result tile to open cert detail

**Pass criteria:**
- Cert renders without mojibake (no `Ã©`, `Â®`, `â€™` patterns)
- TM, ®, ©, accented characters (é, ä, ñ etc.) display correctly as their proper glyphs
- Compare against the original screenshot Carly attached in the QC log

**If fail:** likely a database-encoding issue. Check the `Certificates` table column collation in the prod DB — should be `SQL_Latin1_General_CP1_CI_AS` or `Latin1_General_100_CI_AS_SC_UTF8`. Check the response Content-Type header includes `charset=utf-8`.

---

## QC #128 — LEED filter shows certs without LEED tag

**URL:** `https://gccwebapp.com/` (public view)
**Login:** none

**Steps:**
1. Open public view
2. Locate the **Additional Filters** section (or Tags filter — wherever LEED is listed)
3. Check the **LEED** checkbox/filter
4. Wait for results to refresh

**Pass criteria:**
- Only cert tiles tagged with LEED appear in the result list
- Total tile count matches the expected count of LEED-tagged certs (cross-reference with internal data)
- Other tags (e.g. APR, Recycled Content) should NOT cause non-LEED certs to leak through

**If fail:** check the public-view filter logic in `certificate-tile-view.js` — the `applyFilters()` function. The LEED tag matcher should use exact-tag membership, not substring or OR-with-other-tags.

---

## QC #130 — Remove Product Type A-Z sort option

**URL:** `https://gccwebapp.com/` (public view)
**Login:** none

**Steps:**
1. Open public view
2. Click the **Sort By** menu at top (or wherever the sort dropdown is exposed)
3. Look at the list of sort options

**Pass criteria:**
- "Product Type A-Z" / "Product Type Z-A" options are **NOT in the list**
- Reason: Product Type doesn't show on the tile, so sorting by it is meaningless from the user's perspective
- Remaining sort options should be: Newest first, Oldest first, A-Z by Cert/Company name (or similar)

**If fail:** look in `Views/Certificate/CertificateTileView.cshtml` for the sort dropdown — remove the Product Type options from the `<option>` list.

---

## QC #136 — Eff/Exp dates off-by-one in public view (PROD-ONLY)

**URL:** prod `/Certificate/Edit/{id}` then `/Certificate/Public/{number}`
**Login:** `analyst` for the edit, none for public view
**Viewport:** Desktop

**Steps:**
1. Log in as analyst on prod
2. Pick or create a cert with a **specific known date** — e.g. set Effective Date = `2026-12-15`, Expiration Date = `2027-12-15`
3. Save
4. Open the same cert in Public view (`/Certificate/Public/{cert-number}`)
5. Compare displayed dates against what was entered

**Pass criteria:**
- Public view shows **Effective: 12/15/2026, Expires: 12/15/2027** — exactly what was entered
- No ±1 day shift (NOT 12/14/2026 or 12/16/2026)
- Test with a date close to year boundary (e.g. 12/31) and around DST transitions for extra confidence

**Why prod-only:** dev's data may not surface the bug, and the timezone helper (`BusinessToday` = US Eastern) is in place — need real client-server clock interaction to confirm.

**If fail:** check date serialization in `Views/Certificate/Public.cshtml` — likely needs to use `.ToString("yyyy-MM-dd")` or a date-only format that doesn't apply timezone conversion. Frontend `new Date("2026-12-15")` interprets as UTC midnight, then formats in local TZ which can roll back a day.

---

## QC #149 — L2/L3 Product Categories don't filter

**URL:** `https://gccwebapp.com/` (public view)
**Login:** none

**Steps:**
1. Open public view
2. Locate the **Product Categories** filter (sidebar, hierarchical tree)
3. Expand a top-level (L1) category by clicking the disclosure triangle (e.g., "Construction / Building Products")
4. **Click an L2 sub-option** (e.g., "Adhesives and Sealants")
5. Wait for results to refresh
6. Note the tile count
7. Click an even deeper L3 option if available (e.g., a sub-sub-category)

**Pass criteria:**
- After clicking an L2 option, the tile list narrows to **only certs whose products fall under that subcategory**
- Tile count goes down (or stays same if all certs match)
- Same behavior at L3
- Clicking L1 alone should include all of its L2/L3 descendants

**If fail:** check `certificate-tile-view.js` `applyFilters()` — the Product Category matcher should walk the hierarchy. If only L1 IDs match, L2/L3 selections are no-ops. May need a recursive descendant lookup.

---

## QC #152 — Magnifying glass running into mobile search border

**URL:** `https://gccwebapp.com/` (public view, mobile)
**Login:** none
**Viewport:** Mobile (e.g., DevTools → iPhone 12 or Galaxy S20)

**Steps:**
1. Open public view on mobile viewport
2. If search isn't visible, tap the search affordance (hamburger menu / search icon) to reveal the input
3. Look at the search input and the magnifying-glass icon inside it

**Pass criteria:**
- Magnifying-glass icon is **clearly INSIDE** the search input box
- ≥ 8px gap from input border to the icon left edge
- Icon doesn't visually touch or overlap the rounded border
- Tappable area is comfortable (44×44px minimum touch target ideally)

**If fail:** in `wwwroot/css/main.css` (mobile media query) — bump the search input's `padding-left` to ≥ 36px and absolute-position the icon at `left: 12px` (or similar).

---

## QC #153 — Mobile cert detail: image cutoff + image-on-top order

**URL:** `https://gccwebapp.com/` (public) → tap a cert tile to open detail
**Login:** none
**Viewport:** Mobile (use a real device if possible; DevTools mobile emulation works for layout)

**Steps:**
1. Open public view on mobile
2. Tap any cert tile to open its detail
3. Scroll through the page top to bottom
4. Note the order: where does the cert image sit relative to the text content (Cert #, Company, Product, Cert Type, Dates, Description, Tags)?
5. Look at the image rendering — is it cut off at the bottom?

**Pass criteria (Carly's specific asks):**
- Cert image renders **fully** — no bottom cutoff
- Image scales to mobile width without horizontal scrolling
- **Image appears AFTER the text content sections** on mobile (so users can read details first, then scroll to image) — this is Carly's order preference for mobile only

**If fail:**
- For cutoff: same root cause as QC #60 — `max-height` constraint or `overflow: hidden` on image wrapper
- For ordering: in `Views/Certificate/Details.cshtml`, use a CSS `flex-direction: column-reverse` or `order: 2` on the image container scoped to the mobile media query. Don't change desktop ordering.

---

## QC #157 — Sort behavior on Status / Location / Partner / ASIN / UPC

**URL:** `/Certificate/CertificateTileViewInternal` (switch to LIST view)
**Login:** `analyst`
**Viewport:** Desktop

**Steps:**
1. Log in, open internal cert tile view
2. Toggle to **List/Table view** (button at top of the view)
3. For each of these 5 columns, click header twice and observe row order:
   - **Status** (Carly's complaint: "only sorts ascending")
   - **Location**
   - **Partner**
   - **ASIN**
   - **UPC**

**Pass criteria for each column:**
- 1st click sorts ascending (alphabetical for text cols, numeric where applicable)
- 2nd click flips to descending
- Sort caret in header flips direction
- Other columns unsort when a new column is clicked

**If fail:** check `certificate-tile-view-internal.js` `state.sortBy` / `state.sortDirection` logic. Status sort needs to use a status-priority mapping (not pure alphabetical) so `Active` > `Hold` > `Expired`. Location sort should respect proper alphabetical (not Unicode order). ASIN/UPC are text-but-numeric — sort as strings is fine.

---

## Wrap-up

After running these tests:

1. **Take screenshots** at each pass point — save to `docs/tracking/screenshots/2026-05-01-manual/`
2. **Update the inventory doc** (`qc-readyforuat-2026-05-01.md`) — flip each item from "NEEDS MANUAL" to PASS or FAIL
3. **For any failures:** open a DEF-XXX entry in `docs/tracking/defect-register.md` with reproduction steps + suggested fix location
4. **For client-discussion items** (#84, #59, #86, #106, #118, #145, #159 from the inventory doc): bring to the next status call

**Time estimate:** ~15-20 min per item × 13 items = **3-4 hours total**. Can be split across multiple sessions or delegated to a tester following this cheatsheet.
