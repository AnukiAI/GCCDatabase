# CR-019 — Cover Email

## Internal context (not sent — for our records)

**Purpose.** Cover note that accompanies CR-019 (`CR-019-product-miscategorization-data-cleanup.md`) when it goes to the client. The email is intentionally light-touch; this section holds the detail behind it.

**What it accompanies.** CR-019 — a one-time data-correction pass to fix product/certificate rows mis-categorized by the DEF-231 bug, plus removal of the client's "Carly Test" test data (QC #170 thread 1).

**Why the email is casual, not a formal approval request.** In the QC #186 / #170 notes the client explicitly asked for these to be updated/removed (*"PLEASE REMOVE MY CARLY TEST AND CARLY TEST TYPE OPTIONS"*; *"it changed/removed some of my selections"*). The client has already signaled the change is wanted, so a heavy "please approve this change request" framing would over-formalize something they requested. Per [[feedback_say_client_not_names]] the email stays in plain, friendly language. The formal record still lives in CR-019 + the change-request register — this email does not replace them.

**Key facts the email is built on (full detail in CR-019):**
- **Root cause / code fix:** DEF-231 — `wwwroot/js/certificate-inline-create.js::saveInlineProduct()` filed quick-added products under `GetCategories[0]` (lowest SortOrder) with null type/subcategory, ignoring the form; selecting the product then copied the wrong category onto the cert. Fixed + verified locally (`def231-quickadd-repro.js` 8/8, `def231-fix-verify.js` 6/6). Ships next deploy → stops new occurrences.
- **Data constraint:** the analyst's intended category was overwritten and is not stored anywhere → no auto-restore; remediation is identify → human-review worklist → guarded write. This is why the email promises a review list rather than a silent fix.
- **Safeguards referenced:** full DB backup before any write; ROLLBACK-by-default dry run; integrity guards; scripts committed (`Scripts/data-ops/2026-06-01-product-miscategorization-{IDENTIFY,REMEDIATE}.sql`).
- **Partner API:** no JSON shape change ([[feedback_amazon_api_immutable]]); corrected category becomes the accurate value for any partner-visible cert — mentioned for awareness only.
- **Policy basis:** [[feedback_data_changes_require_cr]] (live UPDATE/DELETE needs GCC sign-off) and [[feedback_data_issue_vs_defect]] (data op, no DEF row, date-prefixed SQL).

**Placeholders to fill before sending:** `[name]` (GCC recipient — left blank deliberately; fill with the right contact at send time). Sender filled as Shawn. Attach/link CR-019. **Send only after DEF-231 is deployed** (or note in the email that the fix lands with the next deploy, which the current draft already does).

---

## Client-facing email (casual — this is what gets sent)

**Subject:** Quick one on the product-category cleanup

Hi [name],

Good news on the category issue you flagged — we found the cause and fixed it. When a product was added on the fly from the "Add New" option on the cert form, it was getting filed under the first category in the list instead of the one you picked (and dropping the type/subcategory), which then carried onto the certificate. The fix keeps your actual selections, and it goes out with the next deploy so it won't happen again.

Since you mentioned in your notes that these can be updated, we'll also tidy up the records that already got the wrong category and pull out the "Carly Test" entries while we're in there. Two quick things on our side:

- We'll take a backup first (standard practice).
- Before we finalize, I'll send you a short list of the affected records so you can confirm the right categories — since the original pick wasn't saved anywhere, we want your eyes on it rather than guessing.

I've logged it as CR-019 on our end for the paper trail — nothing you need to do. Just shout if you'd rather we hold off on any of it.

Thanks!
Shawn
