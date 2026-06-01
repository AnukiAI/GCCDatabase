# Shipped — June 2026

Snapshot of delivery for June 2026. Stakeholder-readable log.

Period: 2026-06-01 → ongoing

## June 2026 — at-a-glance

| | Count |
|---|---|
| **Total fixes shipped this month** | **6** (DEF-225, DEF-226, DEF-227, DEF-230, DEF-231, DEF-232) |
| Deployed and verified live on gccwebapp.com | 0 |
| In working tree, queued for the next deploy | 6 |
| Investigated, closed as Working-As-Designed (no code change) | 1 (DEF-228) |
| Data-cleanup pass drafted (needs backup + CR before running) | 1 (DEF-231 fallout) |

---

## DEF-225 — Cert Edit page: Product Name field now shows the saved product on open

Reported 6/1. When an analyst opened Edit on a cert, the Product Name field appeared blank ("Select product...") even though the cert had a product saved. The hint underneath correctly said "1 product(s) found" but the field itself didn't show which one — leaving the analyst no way to know what was already on the cert without re-picking from the list.

Fixed and verified across desktop, tablet, and mobile (5 screen sizes). The saved product name now appears in dark text the moment the form loads.

---

## DEF-226 — Admin → Product Types: Usage column makes it obvious when delete will be blocked

Reported 6/1. The Usage column showed counts as a single pill like "0 / 1" — the first number is products, the second is certificates. Admins were reading just the first number, hitting Delete, and being surprised by a block message ("Cannot delete: 1 certificate(s) use this category…"). The counts were always correct; the visual didn't make the blocker stand out.

Fixed. The pill now (1) bolds whichever side is non-zero so the blocking count visually pops, (2) dims the zero side, and (3) shows a small padlock icon when either count is non-zero — a glance tells you delete will be blocked before you try.

---

## DEF-228 — Partner CSV export: investigated, confirmed working as designed (QC #182, 5/29 follow-up)

Reported 6/1. On the 5/29 retest of the DEF-198 partner export fix, the client noted that a cert flagged with an internal tag was still appearing in the partner CSV. We reproduced this against the Amazon-mapped partner login and confirmed: the *internal tag itself* (column + values) is correctly hidden from the CSV — but the *cert row* still appears when it qualifies by partner assignment and status, which is the intended behavior.

Internal tags are admin-only metadata categorization, not row-level access control. The mechanism for hiding a cert from a partner is the cert's **Status** (Draft / Hold / Confidential keep a cert off all partner-facing views; Paid / Republished publish it).

No code change. We'll add this to the next client status note so the team understands which lever controls partner visibility going forward.

---

## DEF-227 — All views now have consistent spacing between the navigation menu and the page content (QC #77)

Reported 6/1. On the Admin login, a gray separator bar sits between the top navigation menu and the page content. On the Analyst login, that separator was missing — so the navigation dropdowns appeared to overlap with the content underneath. The fix had been intended for everyone but only landed for Admin.

Fixed and verified on all four role types (Admin, Analyst, External, Public). The gray spacer now renders identically across every login. Admin still shows breadcrumb text inside that spacer; the others render it empty — same layout, no overlap.

Client also flagged a broader concern: "make sure fixes are applied to all instances where applicable." We've added a permanent reminder to our process so cross-role visual fixes get checked against all four logins before being marked complete.

---

## DEF-232 — Import templates: Tags template no longer splits across columns in Excel; Cert template special characters now render correctly (QC #148, 5/29 follow-up)

Reported 6/1 from client retest of the Sprint 3 template polish.

**Tags template.** When opening the downloaded Tags import template in Excel, the column for `TagType` was breaking into two cells — "Internal" landed in column B and "External" in column C, pushing "Description" off to column D. Fixed by changing the header separator from a comma ("Internal, External") to a slash ("Internal/External") so Excel reads the row as the intended three columns.

**Cert template.** The Status column header in the Cert import template was showing a few "random characters" (`â€"`) in place of the em dash separator. Root cause was a CSV-encoding issue: the file was saved as UTF-8 but without the marker that tells Excel "this is UTF-8" — so Excel on Windows defaulted to its older Windows-1252 encoding and the special character mis-rendered. Added the UTF-8 marker; em dash now renders correctly, and any future special characters in template headers will too.

**User Management template — Partner now supported end-to-end.** Client asked us to include "Partner" alongside Admin/Analyst/External in the Role-column hint. We did that, and also wired the partner column the rest of the way through so the template promise is honest:

- The Role hint now reads `Admin/Analyst/External/Partner`.
- A new `Partner (semicolon-separated names; required for Partner role)` column was added to the template.
- The CSV importer accepts that column, looks up each partner by name against the live Partners list, and attaches the matches to the new user (mirrors the existing `User Company` behavior).
- If a row says `Role=Partner` but the Partner column is empty, the import rejects that row with a clear message — we don't create a Partner-role user with no partner attached. Same for unknown partner names (rejected with "Partner 'X' not found").

End result: an analyst can now bulk-import Partner-role users from a CSV with the same fidelity as the Add-User UI.

---

## DEF-230 — Admin → Product Types: you can now move a type or subcategory to a different parent (QC #170, 5/29 re-open)

Reported 5/29. When editing an existing product type or subcategory, changing its parent — for example moving a type under a different category, or a subcategory under a different type — didn't take. The page saved the name and sort order but quietly discarded the move, so the item stayed exactly where it was. The client's words: *"I could not update the existing… it would not give me the other alternatives, something is broken."*

Fixed and verified on localhost across both cases: moving a top-level type between categories, and moving a subcategory between types (with the subcategory correctly inheriting its new parent's category). The existing guard rails still apply — you can't move an item under itself or under one of its own children, and duplicate names under the same parent are blocked. Queued for the next deploy.

---

## DEF-231 — Cert form "Add New product": the product now keeps the category, type, and subcategory you selected (QC #186, 5/29)

Reported 5/29 (the open-items list the client built from issues #91 and #170). When an analyst typed a new product name and used the "Add New" option on the Product Name field of a certificate, the product was being filed under the **wrong category** — the first one in the list — with no product type or subcategory, ignoring what the analyst had actually picked on the form. And because selecting that new product then copied its (wrong) category back onto the certificate, the certificate showed the wrong category too, and the product didn't carry its type/subcategory into Product Management. The client's words: *"I selected Chemicals and it shows [a test category]… Product Type/Subcategory are not appearing… it changed/removed some of my selections."*

Fixed and verified end-to-end on localhost: a product added this way now saves under exactly the category, type, and subcategory chosen on the form, and the certificate keeps those selections.

**Existing records.** This corrects the behavior going forward. Certificates and products created *before* this fix may still carry the wrong category — and because the analyst's original selection was never stored (the bug overwrote it), those can't be auto-corrected. They're being handled separately: a review list is produced from the database, and each genuine record is either re-saved through the now-corrected screen or corrected in a controlled, backed-up data pass. Any leftover test data (e.g. the "[test category]" entries) is removed in that same pass. This data cleanup is drafted and waiting on a backup + change-request approval before it runs against the live environment.

---

## Internal — Verification artifacts

- `scripts/smoke-prod/def225-def226-test.js` — Playwright test for DEF-225 + DEF-226.
- `scripts/smoke-prod/def226-setup.sql` + `def226-cleanup.sql` — idempotent test-data seed and teardown.
- `scripts/smoke-prod/def227-nav-spacing.js` — Playwright probe of breadcrumb spacer across 4 roles. Re-runnable.
- `scripts/smoke-prod/def228-partner-export.js` — Playwright probe of Amazon partner CSV export; verifies internal-tagged cert excluded post-fix.
- DEF-228 pre-fix CSV: `scripts/smoke-prod/screenshots/def228-partner-export-2026-06-01T15-28-48/partner-export.csv` (4 rows, includes 26-0001).
- DEF-228 post-fix CSV: `scripts/smoke-prod/screenshots/def228-partner-export-2026-06-01T15-35-32/partner-export.csv` (3 rows, no 26-0001).
- DEF-225/226 screenshots: `scripts/smoke-prod/screenshots/def225-def226-2026-06-01T13-33-48/`
- DEF-227 4-role verification: `scripts/smoke-prod/screenshots/def227-nav-spacing-2026-06-01T14-05-44/`
- `Scripts/smoke-prod/def230-reparent.js` — Playwright probe of the type/subcategory re-parent fix; self-contained + reversible; 11/11 PASS.
- `Scripts/smoke-prod/def231-quickadd-repro.js` — deterministic repro of the inline-quick-add mis-categorization (8/8); and `def231-fix-verify.js` — real-UI verification of the fix (6/6).
- `Scripts/data-ops/2026-06-01-product-miscategorization-IDENTIFY.sql` (read-only) + `-REMEDIATE.sql` (guarded, ROLLBACK-by-default) — the DEF-231 existing-data cleanup pass; needs backup + CR before running.
- Code-level root cause and file paths are tracked in `docs/tracking/defect-register.md` (DEF-225, DEF-226, DEF-227, DEF-230, DEF-231).
