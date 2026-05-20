# QC #170 — Localhost Validation (2026-05-19)

**Purpose:** Empirically verify DEF-196 / DEF-197 fixes hold up against Carly's 5/15 retest description before the 5/19 email reply.

**Build under test:** Local `dotnet run` from current `master` (which contains DEF-196 + DEF-197). No deploy yet to gccwebapp.com.

**Setup (reversed):**
- Created `ProductCategory "Carly Test"` (id=14) and `ProductType "Carly Test Type"` (id=53, child of Carly Test category)
- Tagged Certificate `26-0001` to `ProductCategoryId=14, ProductTypeId=53` to mirror Carly's "I have a cert using my test type" condition
- Cleanup SQL ran after capture — cert restored, test rows removed

## What Carly saw on 5/15 (pre-fix)

1. Admin → Product Types: "Carly Test Type" row showed `0` for both Products and Certificates columns.
2. Clicked delete → confirmation modal → confirm → **"An error occurred while deleting. Please try again."** (generic, no path forward).
3. Filtered cert listing by Carly Test → certs returned. *Now she knew the admin count was lying, but had no way to find or unblock the dependency.*

## What localhost shows now (post-fix, 5/19)

### A. Admin → Product Types — Usage counts now correct

`screenshots/qc170v5-2026-05-19T21-03-00/02-row-carly-test-type.png`

Table headers: `["Name","Type","Parent","Products","Certificates","Sort Order","Actions"]`

| Row | Products | Certificates |
|---|---|---|
| Carly Test (Category) | 0 | **1** |
| ┗ Carly Test Type (Type) | 0 | **1** |

DEF-196 rollup is working — the `Certificates` column now counts certs that reference the type via `Certificates.ProductTypeId` (the missing path in pre-fix code). DEF-197 dedupe is also confirmed: the category shows `1`, not `2`, even though the same cert is technically referenced at both Category and Type level.

### B. Delete confirmation modal — unchanged, working

`screenshots/qc170v5-2026-05-19T21-03-00/04-confirm-modal.png`

> **Delete Confirmation**
> Are you sure you want to delete **Carly Test Type**? This action cannot be undone.
> [Delete] [Cancel]

### C. Delete error — now specific and actionable (DEF-196 core fix)

`screenshots/qc170v5-2026-05-19T21-03-00/05-error-modal.png`

> **Error**
> Cannot delete: 1 certificate(s) use this type. Edit those certificates to use a different type first, then try again.
> [OK]

Compared to pre-fix `"An error occurred while deleting. Please try again."` — the new message:

- States the count (`1 certificate(s)`)
- Names the type of blocker (`certificate(s)`, vs. `product(s)` or child types)
- Tells the user the exact next step (`Edit those certificates to use a different type first`)

### D. Cert listing filter by Carly Test Type — works

`screenshots/qc170-2026-05-19T20-46-35/07-cert-listing-filtered-carly-test-type.png` (full-page; from v1 run)

Carly's "I CAN see them in the cert listing" is correct. The DB filter behavior was never broken; only the admin page's count display was. With DEF-196 the admin page now agrees with the listing.

### E. Cert Edit form Type dropdown — cascading-as-designed (no bug)

Direct API check confirms the dropdown's behavior:

| `/Certificate/GetProductTypes?categoryId=` | Returned types |
|---|---|
| `14` (Carly Test) | `["Carly Test Type"]` (1 option) |
| `1` (Chemicals and Raw Materials) | `["Additives","Metal Alloys","Minerals","Plastics"]` (4 options) |

So when a cert's saved Category is "Carly Test", the Type dropdown is correctly filtered to "Carly Test Type" only. To pick a real type, the user **must change Category first** — this is the cascading filter behavior, not a defect. Carly read this as "broken — the dropdown won't give me alternatives." It's working as designed; the missing piece is UX guidance.

### F. Side-finding (NOT QC #170 — flag for separate triage)

On the Edit form, the saved `Certificate.ProductCategoryId` is **overwritten** in the UI by the cert's linked `Product.CategoryId`. Cert `26-0001` had `ProductCategoryId=14` in DB but the Edit form rendered Category=`1` (Chemicals) because product 77 ("Widget Pro") is in Chemicals. This is a separate UI bug — Edit.cshtml's `loadProductsForCompany` → product-change handler clobbers the cert's own FK with the product's FK on initial load. Worth a defect row but outside QC #170 scope.

## Verdict for the 5/19 email

Two of three sub-issues Carly raised on 5/15 are **fixed in code and verified end-to-end on localhost today**:

1. ✅ "0 in table while delete blocks" — fixed by DEF-196; Certificates column now reads `1` and matches delete behavior. DEF-197 confirms the rollup isn't inflated.
2. ✅ "Generic 'An error occurred'" — fixed by DEF-196; error message is now specific and tells her the exact next step.
3. ⚠️ "Edit dropdown only shows my test options" — working as designed (cascading filter). Will resolve as soon as she changes Category first. Worth one sentence of UX guidance in the email. Could also be logged as a UX enhancement CR if it keeps biting.

The fixes are still **local only — pending deploy** to gccwebapp.com. Email should commit to a deploy date, then ask for retest after.

## Screenshots & artifacts

```
Scripts/smoke-prod/screenshots/qc170v5-2026-05-19T21-03-00/
  01-row-carly-test-category.png    Usage column for Carly Test category row
  02-row-carly-test-type.png        Usage column for Carly Test Type row
  03-table-viewport.png             Admin → Product Types in table view
  04-confirm-modal.png              Delete confirmation modal
  05-error-modal.png                Server's new specific error message
  00-table-headers.txt              ["Name","Type","Parent","Products","Certificates",...]
  04-confirm-modal.txt              "Are you sure you want to delete Carly Test Type?..."
  05-error-modal-text.txt           "Cannot delete: 1 certificate(s) use this type..."

Scripts/smoke-prod/screenshots/qc170v5-2026-05-19T21-14-...-snapshot/
  A-GetProductTypes-API.txt         API result for /GetProductTypes?categoryId=14 vs 1
  B-edit-form-product-section.png   Edit form viewport
  C-edit-form-type-dropdown-open.png  Type dropdown open

Scripts/smoke-prod/qc170-setup.sql     Idempotent setup (creates Carly Test rows)
Scripts/smoke-prod/qc170-cleanup.sql   Restores cert + removes test rows
Scripts/smoke-prod/qc170-walkthrough-v5.js  Playwright walkthrough script
Scripts/smoke-prod/qc170-edit-snapshot.js   API + Edit form snapshot
```

## Suggested email shape

> Hi Carly —
>
> Quick update on QC #170 from the 5/15 retest. You were right on two of three points and I wanted to walk you through what we found before our [next call].
>
> 1. **0 in the table while delete blocks** — real bug, confirmed and fixed. The "Certificates" column was counting against the wrong FK path, so anything you tagged showed as 0. Now reads correctly: a type with 1 cert tagged shows `1` (see attached).
>
> 2. **Generic "An error occurred"** — also fixed. New message tells you exactly what's blocking and what to do:
>    > "Cannot delete: 1 certificate(s) use this type. Edit those certificates to use a different type first, then try again."
>
> 3. **Edit form dropdown only showing your test options** — this one is working as designed. The Type dropdown filters to the selected Category — if the cert's Category is "Carly Test", only "Carly Test Type" shows. To swap to a real type: **change Category at the top first**, then the Type dropdown repopulates with that category's real types.
>
> Cleanup workflow once these ship:
> - Open each cert tagged to your test rows
> - Change **Category** at the top of the form → pick a real category
> - Pick a real Type from the now-repopulated dropdown
> - Save
> - Back to Admin → Product Types — Carly Test Type will now show 0 certs and delete will succeed
>
> Both code fixes are in the build locally; targeting deploy to gccwebapp.com on [date]. Happy to walk through it live on the [date] status call once it's up.
>
> — Shawn
