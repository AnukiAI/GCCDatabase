# Client deploy announcement email — 2026-06-01 (draft)

**Subject:** Latest fixes are live on the test site — ready for retest

---

Hi [name],

We've deployed the latest round of fixes to the test site, and we've confirmed each one is live before handing it back to you. These cover the items from your recent testing notes (QC #148, #159, #170, #186, plus the nav-spacing item #77). A quick summary of what changed, by area:

**Certificate form**
- The **Product Name** field now shows the saved product when you open a certificate for editing (it was appearing blank even when a product was attached). *(#91/#170)*
- The **"Add New product"** option now keeps the **category, type, and subcategory you selected** — previously it filed the product under the wrong category and dropped the type/subcategory, which also carried onto the certificate. *(#186)*
- **Select-all for Product Identifiers** now works across all pages, not just the visible 50. *(#159)*

**Product Types (admin)**
- The **Usage** column now makes it obvious when a delete will be blocked — the in-use count is bolded with a small lock icon, so you can see at a glance that certificates/products are still attached. *(#170)*
- You can now **move a type or subcategory to a different parent** when editing it (the change wasn't saving before). *(#170)*

**Import templates**
- **Tags template:** "Internal/External" no longer splits across columns in Excel. *(#148)*
- **Certificate template:** the stray characters in Excel are fixed. *(#148)*
- **User template:** **Partner** is now included as a role option, and bulk import accepts a Partner column end-to-end. *(#148)*

**Across the app**
- The gray spacing between the navigation menu and page content now renders consistently on **all logins** (it was only correct on the Admin view before). *(#77)*

Whenever you get a chance, please retest these on the test site and let us know how they look.

One thing still in progress: cleaning up the **existing records** that were saved under the wrong category before today's fix (and removing the leftover "Carly Test" entries). Because the original category wasn't stored, we don't want to auto-guess — we'll send you a short list of the affected records to confirm before we make any changes. The fix deployed today stops any new records from being affected.

Thanks,
[Your name]
Anuki Consulting
