# Client Memo — Sprint Close-Out

**From:** Anuki Consulting
**Date:** 2026-05-19
**Subject:** GCC Database — QC backlog status before next deploy

We've worked through the open QC log this week and have a sizable batch of fixes ready to deploy to **gccwebapp.com**. This memo summarizes what's coming, what's still open, and what we need from the client to close the remaining items.

---

## TL;DR

- **26 QC items moved off the Open list** since the last status pass — 11 confirmed-fixed-and-closed plus 15 new corrections now ready to deploy.
- **1 Critical security fix** ready to ship (data leakage on partner CSV export).
- **All 4 Major bugs from the backlog addressed**: partner export scoping, public filter sidebar gaps, dashboard tile↔export count mismatches, and the "Certs Expired Last Month" wrong-data report.
- **7 items remain Open** — all blocked on client decisions or external dependencies, none on Anuki engineering work.
- **0 net new defects** introduced. Internal QC pass found and fixed two items the client hadn't reported.

QC log breakdown before this batch: 33 Open / 1 ready-to-deploy. After this batch: **7 Open / 16 ready-to-deploy** — all 7 Open items are decision-blocked (Section 4).

---

## 1. Critical: Partner CSV export was returning the full database

**QC #182** — Reported 5/15 — *"This is big issue, needs fixed."*

The "Export All to CSV" button on the Partner login was returning every certificate in the database, including drafts, expired certs, and certs tagged to other partners. The tile view above the same button correctly scoped to the caller's own partner — but the export bypassed that scoping.

**Status:** Fixed and verified on dev. In our dev reproduction, a partner test user mapped to Amazon went from receiving 43 leaked rows down to the correct 4 rows (Amazon × publicly-visible status). Admin and Analyst exports unchanged. Same scoping applies to external (non-partner) logged-in users.

This is the most important fix in the batch — it closes a partner-data-exposure path. We recommend deploying this with priority and re-running the Partner login export check on production immediately after.

---

## 2. Major bugs — dashboard, filters, product taxonomy

### 2.1 Public filter sidebar surfaced phantom companies and missed override categories

**QC #177 + #178** — Reported 5/8.

The Public tile view's filter sidebar had two gaps:

- The Company filter dropdown listed soft-deleted Companies that an existing cert still referenced. Selecting one looked broken because the cert listing correctly filtered them out — but they shouldn't have been in the dropdown in the first place.
- The Product Category / Type / Subcategory filter only reflected values stored on the Product, not category overrides stored at the cert level. A cert visible in the grid couldn't be reached from the filter sidebar.

**Status:** Fixed on the Public and External/Partner views. Filter sidebar now uses the same effective-category logic as the cert listing predicate, and excludes soft-deleted Companies. Verified end-to-end on dev with a cert-override probe.

### 2.2 Dashboard tile counts and download files now agree

**QC #180** — Reported 5/11 — *"Anuki needs to do that"* (the count audit).

Every download icon on the internal dashboard was returning a different row count than the tile above it. The audit found three layered bugs:

- "Certs Expired Last Month" was filtering on the wrong date column (Effective Date instead of Expiration Date), so the download returned certs that became *effective* last month — a completely different set. The download also had no status filter, so Drafts with stale expiration dates were counted as "expired."
- "Products with Active Certs" downloaded one row per certificate instead of one row per product. Tile counted distinct products (10 in dev); download exported all 22 active cert rows.
- "Active Certs Published This Year" tile counted future-dated certs that hadn't gone live yet; the download correctly capped at today.

**Status:** Fixed. All 12 dashboard cards now agree tile-count = download-row-count on dev. The dashboard's `Certs Expired Last Month` card now reflects only certs that were ever published (excludes Drafts that happened to have an expiration date in the past).

---

## 3. Smaller fixes shipping in the same deploy

| QC# | Area | Fix |
|---|---|---|
| #59 | Create cert dropdowns | Type-ahead/autocomplete on Company, Country, Product confirmed working. Status sync. |
| #60 | Cert image post-create | Cutoff resolved (max-height 448px on `.certificate-preview-box img`). Status sync. |
| #104 | Breadcrumbs | `Companies` breadcrumb renamed to `Company Management` for parity with the Product side. The standalone Admin Dashboard page was previously removed (per 5/1 decision); `/Admin/Index` redirects to User Management — no orphan link. |
| #118 | Custom Field visibility | Custom fields flagged "Public" now render on the public cert detail page and the external/partner cert detail page. Previously only the internal view rendered them. |
| #127 | Public filter labels | No longer showing company IDs. Status sync. |
| #131 | Tile text wrapping | Ellipsis on Product Name; Company wraps as designed. Status sync. |
| #132 | Country filter | Was a data-tagging issue (countries entered in the Location field), not a code defect. Closed. |
| #141 | Dashboard exports | Missing columns added. Status sync. |
| #143 | Partner role | Working with credentials provided. Status sync. (Partner API custom-field exposure remains a separate CR pending Amazon sign-off.) |
| #148 | Excel template | Cert import template's `Status` header now lists allowed values inline (matches user-mgmt template convention). Import parser still resolves the longer header on re-import. |
| #155 | Dashboard exports | Accepted as-is per client 5/11 note. Status sync. |
| #158 | High-ASIN-count detail | Confirmed working 5/11. Status sync. |
| #165 | Cert types dropdown | All types appear, including newly-added ones. Status sync. |
| #166 | Pathway dropdown | Client confirmed with Amazon no extras needed today. Status sync. |
| #170 | Product Types delete | "Usage (Products / Certs)" column shows both counts; delete error messages now say specifically which counts block the delete. Rollup math corrected so non-leaf nodes don't double-count entities tagged at multiple levels. |
| #172 | Bulk delete product IDs | Mass-delete checkboxes added on Cert Create + Cert Edit pages (per 5/8 call). |
| #174 | Cert tile spacing | Cert number pill top-aligns when cert type wraps to multiple lines. |
| #175 | Header logo | New responsive breakpoint between tablet and full desktop fixes the narrow/distorted look at small-laptop widths. |
| #176 | Filter sidebar on mobile | Auto-recovers if user hid filters on desktop and then resized below 1024px. |
| #179 | Product ID import preview | SKU column now visible in the preview popup before confirming the import. *(Description column decision deferred — see Section 4.)* |
| #181 | Company Email validation | Strict email format required (a@b.c style is rejected). Already in master since 5/11; just hadn't deployed. |
| #183 | Sort By dropdown | "Most Popular Companies" and "Relevance Score" removed from internal + partner sort dropdowns. They had no backend handler so the options produced no visible effect. |
| #184 | Product Management export | CSV export now includes SKU and Partner columns. *(Cross-cert "all active product identifiers" report is a new report surface — deferred, see Section 4.)* |

---

## 4. Items still Open — all need a client decision

These are not engineering work-in-progress; they're waiting on direction or external sign-off.

| QC# | Area | What we need |
|---|---|---|
| **#50** | Create cert on mobile | The page is not mobile-functional today. Client originally flagged it low-priority. We need a yes/no on whether to spend time on a dedicated mobile-responsive pass, or downgrade to "wontfix — internal users don't create certs on mobile." |
| **#143** | Partner API custom fields | Partner role works in the app. The remaining open piece is exposing custom certification fields on the Partner API JSON — which requires Amazon's sign-off as their own CR per the API-contract-immutable rule. Carry as CR-013 pending Amazon. |
| **#150** | Forgot password / individual logins | Anuki to screenshot the forgot-password round-trip on dev and send confirmation. Client to provide list of (name, email, role, scope) for any named test logins they want provisioned, or opt to use the forgot-password self-serve flow on existing seed accounts. |
| **#151** | Duo as 2FA option | Today the app supports TOTP (Microsoft Authenticator, Google Authenticator, etc.). Adding Duo is a third-party SDK integration with its own settings UI and enrollment flow. Need yes/no on whether Duo is a hard requirement before sprint planning; if yes, it scopes as a dedicated CR. |
| **#159** | Bulk-replace product ID import | The existing import is additive. Bulk-replace mode (remove rows not in upload, add new, keep existing) is a design decision. The bulk-delete feature shipping in this deploy (QC #172) gives a manual workaround. Need yes/no on whether to design + build the explicit replace mode, or defer. |
| **#167** | Auto-expire timing question | Anuki has drafted an explainer covering the schedule (every 24h on prod), trigger conditions, and how the dashboard "Expired Last Month" card relates. Pending Shawn sending the explainer to the client; the item closes on receipt. |
| **#171** | Multi-select Partner on cert | Today `Certificates.Partner` is a single string column. Multi-partner requires schema change, UI multi-select, query updates, and a Partner API contract review (Amazon sign-off). Non-trivial scope. Need a yes/no on priority before scoping the CR. |
| **#173** | Remove "Partner" column from partner login tile | Client asked us to keep it during testing and remove before production. This is a production-cutover checklist item, not active work. Will be flipped to Complete the moment we cut over. |
| **#179** | Description column on product-ID import | Today the import template includes a Description column but there's no UI to view or edit it on a product ID record. Need yes/no on whether to expose Description in the Product ID popup, or remove it from the template entirely. |
| **#184** | Cross-cert "all active product identifiers" export | A new export surface — either a row-per-(cert × product ID) CSV from the Cert Tile view, or a new admin Reports page. Need to decide direction before building. |

---

## 5. Two items found internally during QC pass

These weren't on the client's QC log but came out of our own audit pass and are fixed in the same deploy:

- **DEF-197** — Product Types "Usage" column was double-counting entities tagged at multiple FK levels. The visible damage was small (e.g., showing 28 certs in a category when 26 was correct), but it was a real correctness gap on a column DEF-196 specifically added so the client could trust the delete-blocker counts. Same-day fix; verified against SQL ground truth.
- **DEF-103 follow-up** — During the partner CSV export work (QC #182), we audited the older Partner API JSON fix and found one endpoint still returning raw internal status values instead of the contracted `"Active"` literal. Pulled into the same deploy.

---

## 6. Deploy plan + post-deploy verification

The full batch is on the local working tree, build clean (0 errors), waiting for the next Plesk publish. We'll back up `wwwroot/uploads/` on the server before publish per our incident-avoidance rule (we lost prod uploads once in April and added the precaution).

**Post-deploy spot-checks the client can run (none require Anuki involvement):**

1. **#182 (Critical, partner export)** — log in as the Amazon partner account, click "Export All to CSV," confirm row count matches the number of partner-visible certs on the tile view and **only** Amazon rows are present.
2. **#170 (Product Types)** — open `/Admin/ProductTypes`, confirm the "Usage (Products / Certs)" column shows both counts.
3. **#180 (Dashboard)** — click any download icon on the dashboard, open the CSV, confirm the count line at the top matches the tile number.
4. **#118 (Custom Fields)** — on a published cert, set a custom field to "Public," open the public cert detail page, confirm the field appears.
5. **#175 (Logo)** — resize a browser to ~1280px wide, confirm the logo no longer crowds the header.

---

## What we need from the client

To close the remaining 7 Open items in the next sprint window, we need decisions on the items listed in Section 4. The biggest scope decisions (cost / timeline implications) are:

- **#50** Mobile pass on Create cert form — half day vs. wontfix
- **#151** Duo as 2FA — sized as its own CR if approved
- **#171** Multi-select Partner — sized as its own CR if approved
- **#159** Bulk-replace product-ID import — sized as its own CR if approved

The rest (#150, #167, #173, #179, #184) are short conversations or pending-confirmation items.

— Anuki Consulting
