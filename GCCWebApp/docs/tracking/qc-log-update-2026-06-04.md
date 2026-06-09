# QC Log update — 2026-06-04 (paste-ready)

Status + response text for the items worked this session. Client-facing language; paste into the QC log status + notes columns.

---

## QC #186 — Certificate Product Category / Type / Subcategory not sticking on edit
**Status:** Fixed — **deployed and verified live on gccwebapp.com (2026-06-04)**

**Response:**
Thanks for the detailed 6/4 retest — that pinpointed exactly what was happening. To be clear up front, this was a **different** problem from the "Add New product" fix we shipped on 6/1 (that one is working and stays in place). What you hit on 6/4 was on the **edit screen** itself: when a certificate opened for editing, it was quietly pulling the Category/Type/Subcategory from the *linked product* and writing them over the values you had chosen on the certificate — so if the product happened to be filed under a different category, your selection got replaced (this is why Product Category appeared to "jump to Carly Test" and Product Type went blank).

Two things were fixed:

1. **The edit screen now keeps the certificate's own Category/Type/Subcategory.** Your selections are no longer overwritten by the linked product when the page opens. (For older certificates that never had a category set on the certificate itself, it still fills the value in from the product, so nothing is left blank.)

2. **The quick "Add product" box now asks you to choose a Product Category first.** Previously, if you typed a new product name before picking a category, the product was filed under a default category ("Carly Test") with no type — which is what kept creating mis-filed products. Now it prompts *"Select a Product Category above before adding a new product,"* so anything you quick-add lands under the right hierarchy.

**Verified live on gccwebapp.com**, including on real certificates where the certificate sits under one category while its linked product sits under another — the certificate now correctly keeps its own. We specifically confirmed **26-0166** (the one you flagged) holds its category on edit.

**One thing for your side:** this fix corrects the behavior going forward. Certificates or products that were **already saved under the wrong category** before this fix aren't changed by it — cleaning those existing records up is a separate data pass we've drafted. It edits live data, so we'll take a backup and send you a short list of the affected records to confirm before we run anything. Just let us know when you'd like us to proceed with that.

---

## QC #184 — Full export of active product identifiers across all certificates
**Status:** Delivered — **deployed and verified live on gccwebapp.com (2026-06-04)**

**Response:**
You asked for a way to pull **all active product identifiers across every certificate** in one file — rather than exporting certificate-by-certificate and stitching them together. That's now a one-click button, **"Export Active Product Identifiers,"** on the **Companies & Products → Product Management** page.

It downloads a single CSV of every **active** product identifier across all certificates ("active" = the identifier's certificate is publicly live, the same rule that drives Amazon badging). Each row includes the context you'd need for this analysis:

**Certificate Number, Company, Identifier Type, Id Value, Partner, Product Name, SKU, Description, Effective Date, Expiration Date, Created Date, Created By.**

A couple of notes:
- There's **no Status column** — the file is already filtered to active identifiers, so every row is active by definition and a status column wouldn't add anything (per your 6/4 direction).
- **Who can run it:** Admin and Analyst. External/partner users don't see the button.
- This shipped as the **clean one-click version** you asked for. Optional filters (by partner, identifier type, or date range) are an easy future add if you ever want to narrow the file — just say the word.

**Verified live on gccwebapp.com**: the export returns the full active set (1,022 active identifiers on the current data) with the columns above; the button is present for Admin/Analyst and hidden from external users.

---

## One-line summary for the call
Two items this round, **both deployed and verified live on gccwebapp.com (2026-06-04)**:
- **QC #186** — fixed the edit screen so a certificate **keeps the Category/Type/Subcategory you chose** (it was being overwritten by the linked product on open), and the quick "Add product" box now requires a category so new products are filed correctly. Confirmed live, including on 26-0166. *Separate, optional:* cleaning up records already mis-filed before the fix is a data pass we'll run on your go-ahead, after a backup and a review list.
- **QC #184** — new one-click **"Export Active Product Identifiers"** on Product Management: a single CSV of every active product identifier across all certificates, with Certificate #, Company, Identifier Type/Value, Partner, Product Name, SKU, dates, and creator. Admin/Analyst only.
