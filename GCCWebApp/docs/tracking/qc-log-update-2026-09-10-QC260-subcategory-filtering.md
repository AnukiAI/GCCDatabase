# QC Log update — QC #260, Certificate subcategory filtering (in-cell, paste-ready)

**Source workbook:** `Database QC Log_9.9.26_sw_updates.xlsx`, sheet **`GCC DB QC Log2`**, header row 5.
**Item:** ID **260** · **row 269** · Page `Filtering` · Raised by Carly · Opened 2026-09-09 · col O currently reads `New`.

| Col | Header | Who fills it |
|-----|--------|--------------|
| **J** | `Sprint 3 6/23 Notes` | **Anuki** — the cell below goes here |
| **O** | `Date Closed - GCC Confirmed Fixed` | **Client** — do not fill this in for them |
| **Q** | `Current Status - Anuki Marks Complete` | **Anuki** |

**Backing records:** DEF-413 (predicate work), DEF-414 (list/export surfaces in the same batch).
Spec: `docs/tracking/DEF-413-subcategory-filter-as-is-to-be.md`.
**Deployed to the test environment 2026-09-09** and confirmed live 2026-09-09/10.

---

## Column Q

```
Complete - ready for retest (1 question + 1 decision below)
```

## Column J — paste this

```
9/10: Fixed and live on gccwebapp.com (deployed 9/9). Both the filter checkboxes and the search box now match certificate subcategories, on the public view, on every logged-in view, and on the CSV export.

YOUR EXAMPLE, CONFIRMED ON THE LIVE SITE
Certificate 25-8071 (JW Aluminum, Certified Sustainability Facts, subcategories Recyclable Material / Recycled Content / Waste Diversion from Landfill) is now returned by all three selections:
- 'Certified Sustainability Facts' - shows
- 'Recycled Content' - shows
- 'Waste Diversion from Landfill' - shows
Typing any of those names into the search box finds it too. Same behaviour on public and login views.

WHAT THE COUNTS DO NOW (public view, live site)
- Recycled Content: was 458, now 529
- Waste Diversion from Landfill: was 46, now 151
- Certified Sustainability Facts: 103, unchanged
No filter option lost results - the change only adds.

The Export button agrees with what is on screen. Exporting a 'Recycled Content' filter returns 2,403 rows, which is the post-fix number exactly; before the fix it was 2,314.

WHAT WAS ACTUALLY WRONG
The certificate subcategory field was being saved, displayed, sorted and exported, but it was never used for matching - not by the filter checkboxes and not by the search box. It has been that way since the field was built, so it is not something a recent push broke. Nine places in the code had to be changed: the filter and the search on the public tile view, the external (login) tile view, the internal (analyst/admin) tile view, the internal list API, and the CSV export.

ONE THING TO CONFIRM
'Recycled Content' now means two things at once: certificates whose type IS Recycled Content, plus CSF certificates that carry Recycled Content as a subcategory. That is what the example asks for, and it is why the counts above went up. Please confirm that is the intent, since it widens a filter you had already signed off.

ONE DECISION WE NEED FROM YOU
Two subcategory names now match certificates but still cannot be clicked in the filter pane:
- Recyclable Material - matches 9 public certificates (this is the third subcategory on your own example cert, 25-8071)
- Zero Waste to Landfill - matches 17 public certificates
Both are findable today by typing them into the search box, so nothing is unreachable. Making them clickable means putting subcategory names back into the Certification Type filter box - which is the opposite of what you asked for on 4/30 (issue #169: "the subcategories are not meant to show up in this filter box"). We are not going to reverse that on our own. Tell us yes or no and we will raise it as a change request if yes.

Ready for retest.
```

---

## Backing evidence (for us, not for the cell)

| Claim in the cell | Where it was proven |
|---|---|
| Live on gccwebapp.com | `Scripts/smoke-prod/_def413-live.js` — 6 of 13 top-level filters returned certificates whose *own* type differs from the filter value, which is impossible pre-fix (exact-name match only) |
| Recycled Content 529 / WDL 151 on live | same run, `/api/certificate-tiles` public API |
| Export 2,403 rows | admin `ExportCertificates?certificationTypes=Recycled Content`, HTTP 200, 2,404 lines less header; dev measured 2,314 pre-fix / 2,403 post-fix |
| "never matched, not a regression" | `git log -S "CertificationTypeSubcategories.Any" --all` → 4 commits, all display/projection; no filter predicate has ever existed |
| Nine code sites | `Search/TagSearchMatch.cs` (resolvers), `Repositories/CertificateRepository.cs`, `Controllers/Api/CertificateTileExternalApiController.cs`, `Controllers/Api/CertificateTileInternalApiController.cs`, `Services/CertificateService.cs` ×3 |
| Recyclable Material / Zero Waste to Landfill not clickable | the three `GetFilterOptions` endpoints still build the list as `IsActive && ParentId == null && <ids that are some cert's OWN type>` — `CertificateTilesApiController.cs:317`, `…Internal…:595`, `…External…:654` |
| Scoping not widened | External user scoped to Sika: unscoped union for `Carbon Footprint Reduction` is 39 across other companies; that user gets exactly its own 1. Anonymous: unscoped 399 for WDL, public gets the 151 that pass the visibility gate |

**Not claimed in the cell, deliberately:** P2 (external tile) widening is verified as *scoped correctly*
but the widening itself is unproven there — no fixture company owns a subcategory certificate. P6
(partner list) has no controller caller and cannot be exercised. Neither affects what the client
tests.
