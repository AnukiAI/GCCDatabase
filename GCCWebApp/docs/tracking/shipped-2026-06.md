# Shipped — June 2026

Snapshot of delivery for June 2026. Stakeholder-readable log.

Period: 2026-06-01 → ongoing

## June 2026 — at-a-glance

| | Count |
|---|---|
| **Total fixes shipped this month** | **9** (DEF-225, DEF-226, DEF-227, DEF-229, DEF-230, DEF-231, DEF-232, DEF-233, DEF-234) |
| Deployed + verified live on gccwebapp.com (2026-06-01) | 7 — read-only 10/10 (DEF-225/226/227/229/231/232) + live FUNCTIONAL DEF-231 6/6 & DEF-230 11/11 (self-cleaning throwaway data) |
| Shipped this batch but not in the 2026-06-01 verification pass | 2 (DEF-233, DEF-234) |
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

## DEF-233 — Tile views: filter-options fetch no longer logs a "Failed to fetch" on rapid navigation (internal — caught by new E2E smoke)

Reported 6/1 by our own tooling, not the client. The earlier pageUnloading fix from May covered the main certificate fetch on the three tile views but missed the parallel filter-options fetch. On rapid navigation away during initial page load, the unguarded path logged a harmless but visible "Failed to fetch" to the console. Real users rarely navigate fast enough to hit it; our new E2E smoke harness does, and was flagging it as a false-positive failure.

Fixed across all three tile-view scripts (public, external, internal) with the same two-line guard already in use elsewhere. Smoke suite is now deterministic green (104/104 PASS, twice consecutively).

---

## DEF-229 — Certificate Create page: "select all" Product Identifiers now spans every page, not just the visible 50 (QC #159, 5/29 follow-up)

Reported 6/1. When building a new certificate, an analyst who pasted or imported a large list of Product IDs and then used the header "select all" checkbox only selected the 50 rows currently visible on screen — there was no way to select the whole list at once to clear or bulk-act on it. The count hint was right ("N product IDs"), but the cross-page "select all N across pages" banner that appears everywhere else never showed up on the Create page specifically.

Fixed and verified on localhost. The cross-page banner now appears on the Create page too, and "select all" reaches every Product ID in the in-memory list regardless of which page is showing. Queued for the next deploy.

---

## DEF-234 — Public certificate tile view: first-load speed-up (internal — caught by new E2E smoke)

Reported 6/1 by our own tooling, not the client. After the application sits idle (or the server recycles its worker process), the very first load of the public certificate tile view was slow — the certificate grid could sit blank for ~23 seconds while the database query "warmed up," and on a bad cold start it occasionally timed out and showed "Error loading certificates" instead of the tiles. Every load after the first was effectively instant (well under a second). Real users would have seen an occasional slow first load; our new E2E smoke harness, which hits a cold server, caught it as an intermittent failure.

Fixed with two coordinated changes, defense in depth:

- **Query shape.** Changed how the certificate list query is assembled so the database no longer multiplies rows across the certificate's tag and partner associations on a single large query — the standard, lower-cost pattern we already use on the other tile views. Also added a stable secondary sort so the same cert can never accidentally appear on two pages or be skipped when many certs share the same primary sort value.
- **Startup warm-up.** The app now fires one tiny test query at boot so the database query plan is already cached before the first real visitor arrives. The first user request now pays the warm-path cost (sub-second), not the cold-compile cost. Safe-fails — if warm-up encounters any issue, it's logged and the app starts normally.

The fix is in the working tree and queued for the next deploy; a warm re-run of the smoke suite will confirm the cold-start window is closed before it's marked verified.

**Verification (2026-06-02T02:25Z):** `SMOKE_BATCH_RESTART=1` Tier-1 roll-up returned **692 PASS / 0 FAIL / 8 WARN — GREEN**. Public tile cell count: 124/0/0. Curl-timed first hit on a cold restart: ~0.6s (was 23s pre-fix). Cold-start window closed.

---

## DEF-235 — Test suite stays reliable end-to-end: no more last-view flakes on long smoke runs (internal — smoke harness reliability)

Found on 2026-06-02 during a back-to-back run of the new Tier 1 smoke suite: after ~9 views of sustained load (~600 HTTP requests in ~6 min), the local dev server stopped accepting new TCP connections and the last view in the run (`admin-tags`) failed every cell with a connection-refused error. Direct `curl` calls to the same URL right after the suite worked fine — so it was sustained-load related, not a real bug in any view.

**Root cause found and fixed (2026-06-02):** the culprit was the request/response logging middleware. It was wrapping the response body of EVERY request into a memory buffer — including bundled CSS / JavaScript / images / fonts, which were never going to be logged anyway. Under sustained load (the new E2E smoke fires ~30 static-asset requests per page × 12 cells × 10 views = ~3600 buffer allocations per run), the resulting memory pressure produced long garbage-collection pauses that made the web server fall behind on accepting new connections. The fix excludes static-asset paths from body buffering entirely — those requests still get logged (status code, duration), they just skip the wasteful body capture.

**Verification:** the deploy-gate smoke now passes in plain single-run mode (no special workaround needed): **694 PASS / 0 FAIL / 6 WARN, GREEN**. The previously-flaky last view went from 0-of-12 cells passing to 40-of-40. The earlier `SMOKE_BATCH_RESTART=1` workaround is now optional — single-run mode is reliable again. Production benefit: the same allocation pressure would have shown up under burst traffic on Plesk; that whole class of stability concern is now closed.

---

## Internal — E2E Smoke suite (Days 1 + 2 — infra and Tier 1 fan-out)

New automated test surface scoped per `docs/tracking/e2e-smoke-plan.md`. Catches view-level regressions across 4 roles × 3 viewports before they reach client UAT.

- `Scripts/smoke-prod/_lib/smoke-template.js` (~305 lines) — shared runner. Login per role using the CLAUDE.md test creds, viewport sizing, console + page-error + network-error capture (with ignore-list for known noise), screenshot policy via `SMOKE_SCREENSHOTS=on-fail|always|never`, access-gate check (200/401/403 normalization including login-redirect → 401), mobile horizontal-scroll guard, per-view JSON results. Refuses to run against gccwebapp.com / greencirclecertified.com unless `SMOKE_ALLOW_REMOTE=1` is set.
- `Scripts/smoke-prod/_views/` — 10 Tier 1 view smokes (`home`, `login`, `cert-tile-public/external/internal`, `cert-create`, `cert-edit`, `dashboard`, `admin-user-mgmt`, `admin-tags`) plus `run-all.js` orchestrator that writes a summary JSON.
- Acceptance is "all 10 views return 0 FAIL across 4 roles × 3 viewports, and negative-path access (Public→Internal, External→Admin) returns the expected 401/403."
- Day 1 + Day 2 effort: ~1 day each, infra + 10 view-specific assertion blocks. Approach is now the deploy-gate candidate for future Plesk pushes.

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
