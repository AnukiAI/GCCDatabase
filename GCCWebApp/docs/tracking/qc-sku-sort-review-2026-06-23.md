# QC Log review item — SKU sorting on the certificate list (2026-06-23)

## Internal context (not sent)

- **Surfaced:** 6/23 review of sorting on `/Certificate/CertificateTileViewInternal`. The acceptance list asked SKU be "added as a sortable table option or the blocker documented."
- **QC classification:** Lack of Requirements / design clarification — **not a defect**. SKU was never a column on the certificate list, and the "make it sortable" ask doesn't resolve cleanly because of the data shape below.
- **The real blocker (why we're asking, not just building):** SKU on a certificate is **one-to-many**. A single certificate can carry multiple product identifiers, and SKU is one of those identifier types (a cert can have several SKU values; each marketplace identifier can also carry its own SKU attribute). So a single "SKU" column can't represent a row that has several SKUs — and "sort the list by SKU" is undefined when a row has more than one.
- **Precedent:** this is the exact situation we already hit with ASIN/UPC/GTIN/EAN (also 1-to-many per cert). We resolved those by showing a **count badge** (how many IDs the cert has) and sorting by that count, not by the value (DEF-260, shipped 6/12). The same pattern is the consistent option for SKU.
- **Where SKU lives today (all available, just not on the cert list):** Product ID Manager (per-identifier), Product Management (product record), and the CSV export. The internal cert API already passes a SKU value through (`CertificateTileInternalApiController.cs:270/360`, currently first-of-many), so the plumbing is mostly there — the open question is *what behaviour the client actually wants*, not whether we can do it.
- **Bundle opportunity:** ISBN and MPN are in the identical state (already captured, 1-to-many, not shown). If the client wants SKU surfaced, ask about these in the same pass to avoid a repeat item.
- **Recommendation if they want it on the list:** Option A (count badge, same as ASIN/UPC/GTIN/EAN) — consistent and low-risk. Value-level SKU sorting only makes sense where SKU is single-valued (Product ID Manager / export), not the cert list.
- **Placeholder:** `[name]`.

---

## Client-facing email (this is what gets sent)

**Subject:** Quick design question — SKU on the certificate list

Hi [name],

We were reviewing the sorting on the internal certificate list and want your input on one item before we build it, because there's a design choice only you can make.

The notes asked for **SKU** to be a sortable column on the certificate list. The wrinkle is that **a single certificate can have more than one SKU** (a certificate can cover multiple product identifiers, and SKU is one of those). So a single "SKU" column can't really represent — or sort — a certificate that has several SKUs. Which one would the list sort by?

We ran into the same thing with the **ASIN, UPC, GTIN, and EAN** columns, which can also have several values per certificate. There, we show a small **count badge** (how many that certificate has) and let you sort by that count. It works well and keeps the list tidy.

So for SKU, we'd like to know which of these fits how you'd actually use it:

- **Option A — match the other ID columns:** show a SKU **count badge** and sort by how many SKUs each certificate has. Consistent with ASIN/UPC/GTIN/EAN. *(Our recommendation.)*
- **Option B — sort by the SKU value:** this only makes sense where a record has a single SKU, so it really belongs on the **Product Identifier** screen / export rather than the certificate list. We can make sure SKU sorts there instead.
- **Option C — leave SKU off the certificate list:** it stays available on the product and identifier screens and in the CSV export, where the full per-SKU detail already lives.

A couple of follow-ups if you do want it on the list:
1. Internal (analyst/admin) list only, or the **external/partner** list too?
2. We capture **ISBN** and **MPN** the same way — want those handled alongside SKU, or just SKU?

No rush, and no wrong answer — we just don't want to build a SKU sort that doesn't match how you'd actually use it. Everything else in the sorting review is already done and working.

Thanks!
Shawn
