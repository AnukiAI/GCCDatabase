# Email Draft — QC #91 Retest Request

**Date drafted:** 2026-05-19
**Last updated:** 2026-05-19 (after live-env SIT verification)
**To:** Client (Carly)
**From:** Shawn West
**Subject:** QC #91 — Product Management fix verified on gccwebapp.com, ready for your retest
**Status:** Draft (pending Shawn's send)

---

Carly,

Quick update on QC #91 before the next round of retesting.

The fix for the Product Management symptoms you reported on 5/11 — Product Name and Type clearing on edit, "Carly Test" showing in unexpected rows, new products not appearing in the list after save — has been in our source code since 5/4–5/5 (defects DEF-134 and DEF-138 in our register) and originally went out to gccwebapp.com on 5/12. We've shipped an additional patch today (5/19) covering five more open items in the same UI surface (your QC #104, #118, #148, #179, #184). I want to be straight that when you retested QC #91 on 5/11 you were testing the pre-deploy build — the fix landed the day after — so the symptoms you saw were real on that build, but they were already corrected in source.

This time I didn't just rely on "fixed in source." Before sending this note I drove the fix end-to-end on `gccwebapp.com` itself, logged in as the analyst test account, with the following results:

- **Edit modal pre-fills correctly.** Opened an existing product that had Company, Category, Type, and Subcategory all set. Every dropdown came up with its saved value — no more blanks that would silently overwrite when you click Save.
- **Add Product persists Type and Subcategory.** Created a test product on gccwebapp.com just now (then deleted it), and confirmed via the live database response that the new row carried the chosen Product Type through correctly. This is the exact symptom from your 5/11 note — *"they are not showing in list at all"* — and it does not reproduce.
- **Sort arrows no longer overlap the column header text.** All eight sortable columns render their up/down arrow inline to the right of the header, in default state and after clicking to sort.
- **SKU values no longer disappear across saves.** This was the underlying cause of what looked like "Product Name cleared" on your end — the table renders SKU as a second line under the name, and our earlier bug was silently nulling it. That regression is closed.

I want to be candid about what went wrong on our side and what we've changed so this doesn't repeat:

1. **The original bugs were ours.** The Edit modal only pre-filled Product Name and Description, leaving Company, Category, Type, and Subcategory blank — so any Save without manually re-picking them wiped those fields to null. Separately, the Add/Edit modal had previously dropped its SKU/Model inputs (in an earlier sprint per your QC #92 request), but the server save path was still blindly assigning those fields from the request, which silently nulled any existing SKU each time a product was edited. Both are now patched and the patches are live.

2. **The deploy-vs-retest gap was a process gap on our side.** We had no signal that lined up when a defect was "fixed in source" with when it actually shipped to your test environment. We've started running a Playwright integration pass against the live build before declaring something ready for your retest, and the deploy itself now produces an idempotent migration script bundled with the code so we can no longer ship code that references a schema change that hasn't been applied yet. Today's deploy was verified post-extraction with a clean checksum against our local build artifact, and the integration pass above was run against the live URL after the IIS app pool recycle.

**One ask:** could you retest QC #91 against `https://gccwebapp.com` whenever you have a window? You're testing the same build I just verified, so if you do see the symptom reappear we have a brand-new defect on our hands and I'd want to know immediately. If it's gone, QC #91 can close.

One sub-item from your 5/11 list I'd like a clarification on before we touch it: *"won't let me access all the Product Categories"* when editing a certificate and adding a new product. Our code reads the category list directly from the active rows in the ProductCategories table, so if you're seeing fewer than expected, it's either (a) a category got marked inactive on the back end, or (b) you were looking at the filtered Product Type list (which is correctly narrowed by the Category you picked) rather than the full Category dropdown. Could you grab a screenshot of which category names you'd expect to see vs what's actually showing? That'll let us tell quickly whether it's a code defect or a data state question, without burning a sprint cycle guessing.

Thanks for the persistent testing — the SKU-null regression in particular would have hit a real customer's data if you hadn't kept pushing on QC #91 across the last three retests.

— Shawn

---

## Send checklist (for Shawn)

- [x] Deploy `GCCWebApp-deploy-20260519-1726.zip` to gccwebapp.com (apply migrations first per `migrations/README.md` in the zip)
- [x] Confirm gccwebapp.com is back up after app pool recycle — verified via static asset SHA match + live `partners` key on `/Product/GetProducts`
- [x] Live SIT pass against `gccwebapp.com` — TC-A-alt (edit pre-fill) + TC-A bonus (Type/Sub pre-fill) + TC-B (Add Product) + TC-C (sort arrows) all PASS
- [x] Test product `QC91-LIVE-1779234319868` (id=130) created during TC-B has been deleted
- [ ] Send this email

## Internal context (do NOT paste into the email)

- Full root-cause writeup lives in `defect-register.md` entries DEF-134 (5/4), DEF-138 (5/5), and DEF-202 (today).
- Per `feedback_say_client_not_names.md` — internal references use "client" not "Carly" / "GCC" / "Kyle". The salutation in this email uses "Carly" only because it is addressed to her directly.
- Live SIT artifacts:
  - Read-only checks: `C:\Users\swest\AppData\Local\Temp\qc91-sit-live-1779233783936\` (A1, A2, C1, C2)
  - Add Product check: `C:\Users\swest\AppData\Local\Temp\qc91-addproduct-live-1779234304261\` (B0–B4)
  - Cleanup confirmation: `C:\Users\swest\AppData\Local\Temp\qc91-cleanup-<ts>\`
- Test plan at `docs/tracking/qc91-test-plan-2026-05-19.md`.
