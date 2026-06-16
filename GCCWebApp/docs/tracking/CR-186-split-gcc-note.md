# CR-186 Split Migration — GCC Approval Note

## Internal context (not sent — for our records)

**Purpose.** Get GCC's written go-ahead for the one-time CR-186 split migration on the live DB, and a decision on the 3 test products the read-only IDENTIFY surfaced. Pairs with `CR-186-product-identity-name-category-type-subcategory.md` §10 and the worklist `Scripts/smoke-prod/_results/cr186-split-identify-prod.txt`.

**Facts the note is built on (from the read-only prod IDENTIFY, 2026-06-15):**
- 118 products total. **43 real products** are single-combo → just get Type/Subcategory **set** (no split) — this is the actual fix for the blank columns.
- **3 products would split → 4 new records, and all 3 are test data:** `test` (#119), `3M Test Product` (#127), `Best Product Ever v3T` (#141).
- 72 products have no certificates → untouched.
- Recommendation: delete the 3 test products (CR-019 cleanup territory, [[feedback_data_issue_vs_defect]]) rather than split them; then the migration is almost purely the harmless "fill Type/Subcategory on real products" pass.

**Process gates (per [[feedback_data_changes_require_cr]]):** full DB backup first; migration runs ROLLBACK-by-default (verify counts, then commit). Nothing run against live except the read-only check + a ROLLBACK dry-run.

**⚠ 2026-06-15 FINDING — the "delete test products" premise was WRONG; HOLDING.** Prod investigation showed the 3 "test" products are NOT empty — they carry **7 certificates, 3 of them published `Client Approved - Paid`** (26-0163 on #127, 28-0003 + 26-0175 on #141), plus Draft/Expired ones. Deleting them would remove live public certs across 7 child tables. **Decision (Shawn): HOLD — re-confirm with GCC before any delete or split.** Prod REMEDIATE dry-run is verified (118→122, 46 set, 3 split, 4 re-pointed) and a DB export/backup exists, but **nothing committed.** The concise/detailed notes below (which recommend deleting the 3 entries) are **SUPERSEDED — do not send.** Send the **REVISED** note that follows instead.

**Before sending:** fill `[name]` (GCC recipient) and the `[To:]` line. Sender = Shawn West / Anuki Consulting. Send the FINAL version below; the short-form is interchangeable if a brief ask is preferred. Keep internal IDs / table names out of the client copy (they're only in this block).

---

## ★★ SEND THIS — REVISED 2026-06-15 (paid-cert finding) ★★

**To:** [GCC recipient]
**Subject:** One thing to confirm before we finalize Product Type & Subcategory (CR-186)

Hi [name],

A quick but important update before we run the Product Type / Subcategory fix.

During our final safety check against the live data, we found that the three entries we'd flagged as "test products" — **"test", "3M Test Product", and "Best Product Ever v3T"** — are **not empty**. Each has **real certificates attached**, and three of those certificates are in the **published "Client Approved – Paid"** state:

- **26-0163** (on "3M Test Product")
- **28-0003** (on "Best Product Ever v3T")
- **26-0175** (on "Best Product Ever v3T")

(plus a few in Draft and Expired status)

Because those are **live, published certificates** — not throwaway data — **we did not delete anything.** We want your confirmation first.

Two ways forward:

1. **If these are real**, we simply keep them. They'll display as properly-separated line items by Product Type/Subcategory like every other product — nothing removed. *(This is our recommendation unless you tell us otherwise.)*
2. **If they're genuinely test data** — including those three Paid certificates — we'll plan a separate, careful cleanup to remove them, but only with your explicit confirmation, since deleting a published certificate removes it from the public site and from any reporting.

Either way, the main fix — populating Product Type/Subcategory across your real products — is ready and just waiting on your go.

**Could you confirm whether those three products and their certificates (especially the three Paid ones above) are real or test?** Once you do, we'll finalize. Tracked on our side as CR-186.

Thank you,
Shawn West
Anuki Consulting

---

## ~~★ FINAL — READY TO SEND ★~~ (SUPERSEDED — do not send; "delete test entries" premise was wrong, see finding above)

**To:** [GCC recipient]
**Subject:** Approval needed — finalizing Product Type & Subcategory on Product Management (CR-186)

Hi [name],

We're at the final step for the **Product Type** and **Subcategory** columns on the Product Management page. Below is the complete picture — what was happening, what we're changing, exactly what it does to your data, and how we keep it safe — followed by the two things we need from you.

### 1. Why those columns looked blank

The page itself was never broken. The reason the columns were empty is *where* the information lived. Your analysts enter Product Type and Subcategory on each **certificate** — that's the natural place in the workflow. But that detail was only stored on the certificate, not copied onto the **product record** that the Product Management page reads. So the information was **present all along, just not exposed in the Product Management workflow** — it sat one level over, on the certificates.

### 2. What we're changing — and what "separate line item" means

Going forward, each product is defined by the full combination of **Company + Product Name + Category + Product Type + Subcategory**. This ensures that when the same product name has been certified under different Types or Subcategories, each unique combination is accurately represented as its **own row**, with the **correct certificate count** — instead of everything collapsing under one name.

A concrete example. Suppose **Acme Corp** has a product called **"PureFlow Water Filter"** with **four certificates**, certified across three different combinations:

- **Plastics → PET** — 2 certificates
- **Plastics → HDPE** — 1 certificate
- **Metals → Stainless Steel** — 1 certificate

Today that shows as a single "PureFlow Water Filter" row with blank Type/Subcategory and a count of 4. After this change it shows as **three rows**, and each certificate lands on the row it belongs to:

| Product | Type | Subcategory | Certificates |
|---|---|---|---|
| PureFlow Water Filter | Plastics | PET | 2 |
| PureFlow Water Filter | Plastics | HDPE | 1 |
| PureFlow Water Filter | Metals | Stainless Steel | 1 |

The counts still add up to the original four (2 + 1 + 1 = 4) — **nothing is lost**. It's simply reorganized so the page reflects how the product was actually certified. Each row is a real, manageable product record. This is already live for **new** work — when a certificate is created or edited, the system automatically files it under the matching product (creating the record if that combination is new). The step we need approval for is the **one-time pass** that organizes the products *already* in the system the same way.

### 3. Exactly what the one-time update does to your live data

We ran a **safe, read-only check** against the live database first (no changes made) so we could tell you precisely what would happen before anything runs. The result:

- **43 of your real products** are each tied to a single Type/Subcategory combination. For these, the update simply **fills in** the Type/Subcategory that's already on their certificates. Nothing is split or moved — the columns just stop being blank. This is the bulk of the value.
- **72 products** have no certificates attached. They are **left untouched**.
- **Only 3 products** are tied to certificates with *more than one* Type/Subcategory, so they're the only ones that would actually split into multiple rows — and all three are **leftover test entries**, not real catalog products:
  - "test"
  - "3M Test Product"
  - "Best Product Ever v3T"

Importantly, **none of your certificates are changed, deleted, or re-dated.** A certificate keeps all its information; the only thing that happens is each certificate gets pointed at the correctly-labeled product record, so the counts group the way the new rule intends.

### 4. The one decision we need from you

Because the only products that would split are those three test entries, splitting them would just create more test rows. We'd recommend instead simply **deleting the three test entries** as part of routine cleanup — they aren't real products. If you'd prefer we keep them and split them like everything else, that's fine too; we just want your call rather than assuming.

### 5. How we run it safely

- **Full database backup** taken immediately before anything runs, so we can restore if needed.
- The update runs in a **rehearsal mode first** — it performs all the work inside a "preview" that we can inspect (and that automatically undoes itself), so we confirm the exact numbers — 43 products updated, 3 handled per your decision, certificate counts correct — *before* we commit it for real.
- Run in a **quiet maintenance window**.
- **Your certificates, files, companies, and tags are not touched.**

### 6. What we need to proceed

1. **Approval** to run the one-time update that populates Type/Subcategory on your existing products.
2. **Confirmation** that we may **delete** the three test entries listed above — or guidance to keep and split them instead.

As soon as we have both, we'll schedule the work. It's tracked on our side as CR-186, and we'll send a short confirmation once it's done and verified.

Thank you,
Shawn West
Anuki Consulting

---

## ~~Short-form alternative~~ (SUPERSEDED — do not send; recommends deleting the test entries, premise wrong)

**To:** [GCC recipient]
**Subject:** Quick approval — making Product Type & Subcategory show correctly on Product Management (CR-186)

Hi [name],

We're ready to turn on the fix for the **Product Type** and **Subcategory** columns on the Product Management page. Two quick things before we run it — one approval, one small decision.

**What it does.** A product is identified by its **Company + Name + Category + Product Type + Subcategory**. So if the same product name has been certified under a different Product Type/Subcategory, it now shows as its **own line item**, with its own certificate count.

For example, if "Acme Corp" has a product **"PureFlow Water Filter"** with 4 certificates — 2 certified as Plastics/PET, 1 as Plastics/HDPE, and 1 as Metals/Stainless Steel — it would change from one blank-Type row of 4 into three rows (2 + 1 + 1 = the original 4). Nothing is lost; it's reorganized so the page reflects how the product was certified. Going forward this happens automatically; this approval covers the one-time pass that organizes the products already in the system.

**What we found.** We ran a safe, read-only check against the live data first. The result is reassuring:

- **43 of your real products** simply need their Type/Subcategory filled in — straightforward, no change to how they're grouped.
- **Only 3 products** would actually split into separate line items — and all three are **leftover test entries**, not real products:
  - "test"
  - "3M Test Product"
  - "Best Product Ever v3T"

**The one decision.** Rather than split those three test entries, we'd recommend simply **deleting them** as part of cleanup. They're not real catalog products. Are you OK with us removing those three? (If you'd rather we keep and split them instead, just say so.)

**How we run it safely.** Before any change we take a **full database backup**, then run the update in a mode that lets us verify the exact counts first and only commit once they match what we expect. We'd do this in a quiet maintenance window.

So, two asks:
1. **Approve** the one-time update to populate Type/Subcategory on the real products.
2. **Confirm** we can delete the three test entries above (or tell us to split them).

Once you give the go-ahead I'll schedule it. Logged on our side as CR-186.

Thank you,
Shawn West
Anuki Consulting
