# QC Log update — 2026-06-01 (paste-ready)

Status + response text for the items worked/triaged this session. Client-facing language; paste into the QC log status + notes columns.

---

## QC #170 — Product Types: counts, delete, Carly Test cleanup
**Status:** Fixed — pending deploy

**Response:**
The Product Types fixes are live as of 5/21 — the Usage column now shows both product and certificate counts, the rollup math is accurate, and a blocked delete gives a specific message naming the count and whether it's products or certificates.

Three follow-ups from your 5/29 retest are fixed and ship with the next deploy:
1. The `X / Y` Usage pill now bolds the in-use number and shows a small lock icon, so it's clear at a glance that delete will be blocked (the counts were always correct — the format made it easy to read just the first number).
2. You can now move a type or subcategory to a different parent on the Product Types page.
3. On the certificate form, the "Add New product" option now keeps the category/type/subcategory you selected — previously it filed the product under the first category in the list, which is why editing your test certs only offered the test options.

Still open (separate): removing the existing "Carly Test" entries and correcting records already saved under the wrong category is a change to live data. We've drafted that cleanup (CR-019); once approved we'll take a backup and send you a short list of affected records to confirm before finalizing.

---

## QC #186 — Open items from #91 / #170 (HIGH PRIORITY)
**Status:** Fixed — pending deploy (+ data cleanup pending approval)

**Response:**
Broke down into four threads:
1. Product Name field blank on cert Edit — fixed, pending deploy.
2. Product Type blank on cert Edit — this was a symptom of #3; resolved by it.
3. Wrong category saved ("selected Chemicals, shows [test category]") and product/type not appearing in Product Management — root cause was the "Add New product" quick-add filing products under the first category with no type/subcategory and copying that wrong category onto the cert. Fixed and verified end-to-end, pending deploy.
4. Counts not accurate — the count pill readability fix above; counts themselves were correct.

Records already created with the wrong category before this fix are corrected via the CR-019 data cleanup (pending approval + backup).

---

## QC #148 — Import template polish
**Status:** Ready for UAT — pending deploy

**Response:**
All three items addressed and verified in code:
- **Tags template:** "Internal/External" no longer splits across columns in Excel (changed the separator so Excel reads it as one cell).
- **Cert template:** the random characters in Excel are fixed (added the UTF-8 marker so Excel stops mis-decoding the special characters).
- **User template:** the Role hint now includes Partner (Admin/Analyst/External/Partner), and bulk import accepts a Partner column end-to-end.

(The User Company / Company Access note is a separate, already-settled item — "User Company" was confirmed as the desired label.)

---

## Also shipping in the same deploy (related items)
- **QC #77** — gray nav spacer now renders consistently across all four logins (Admin/Analyst/External/Public). Fixed, pending deploy.
- **QC #91** (Product Name display on Edit portion) — saved product now shows in the field on open instead of appearing blank. Fixed, pending deploy.

---

## One-line summary for the call
Engineering on #170/#186/#148 (and #77/#91) is done and batched for the next deploy. The only thing not handled by the deploy is the **live-data cleanup (CR-019)** — removing the "Carly Test" data and correcting records already saved under the wrong category — which needs your go-ahead and a backup first; we'll send a review list before finalizing.
