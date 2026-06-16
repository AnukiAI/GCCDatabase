# CR-186 Phase 1 — Client Email

## Internal context (not sent — for our records)

**Purpose.** Tell the client why Product Type & Subcategory show blank on `/Product/Management` and ask approval to run the one-time backfill that fills them. Pairs with `CR-186-product-identity-name-category-type-subcategory.md` (§9 Phase 1 AS-BUILT).

**Key facts the email is built on:**
- Not a display bug — endpoint/join/render all correct. The product-level Type/Subcategory fields are simply empty; that data has only ever been entered on the **certificate**. Live probe 2026-06-15: 107 of 118 products blank.
- Phase 1 = (a) code write-back so new/edited certs auto-fill the product going forward (next deploy, no client action), and (b) a one-time backfill of existing products from their certs — **only where the certs agree on one type**.
- The backfill is a live-data change → needs written GCC go-ahead + full DB backup before it runs ([[feedback_data_changes_require_cr]]). Nothing has run against prod.
- A few products are linked to certs with **different** types (conflicts) — deliberately left blank, brought back to the client as a separate short decision (Phase 3), never auto-guessed.

**Keep out of the client email:** internal identifiers, table/column names, the SQL filenames, the exact 107/118 (use plain language — "most products").

**Placeholders to fill before sending:** `[name]` (GCC recipient). Sender = Shawn.

---

## Client-facing email (this is what gets sent)

**Subject:** Product Management — filling in the Product Type & Subcategory columns

Hi [name],

Following up on the Product Type and Subcategory columns showing blank on the Product Management page.

Good news first: the page itself is working correctly — it's not a display error. The reason those columns are empty is that, historically, the Product Type and Subcategory have been entered on each **certificate**, but that information was never also saved at the **product** level. So when the Product Management page goes to show it, there's nothing stored there to display.

We'd like to fix this in two parts:

1. **Going forward (automatic, nothing needed from you).** From the next update, whenever a certificate is saved, its Product Type and Subcategory will also be copied onto the product. So the columns stay filled from here on. This never overwrites anything an analyst sets directly on the product.

2. **Existing products (one-time backfill — needs your OK).** We'll fill in each existing product's Type and Subcategory from its certificates. To be safe, we only do this automatically where a product's certificates clearly point to a single, consistent value.

Because step 2 updates existing records, we'll **take a full backup first**, and I'd like your go-ahead before we run it. I can also send you the exact list of products it will fill so you can look it over beforehand.

One thing to flag: a small number of products are tied to certificates with **different** types. We won't guess on those — we'll leave them as-is and bring you a short list so you can tell us how you'd like each handled.

I've logged this as CR-186. Are you OK for us to proceed with the backfill (with the backup and a review list), and we'll include the automatic going-forward piece in the next deployment?

Thanks!
Shawn

---

## Client-facing email — VERBOSE version (fuller explanation + decisions)

> Use this version when the client wants the full "why" and the explicit choices laid out. The shorter version above is the quick-approval note.

**Subject:** Why Product Type & Subcategory show blank on Product Management — explanation + a few decisions

Hi [name],

You flagged that the **Product Type** and **Subcategory** columns on the Product Management page are showing blank for most products. I want to give you the full picture of *why* that's happening, because it's not a bug — it's a side effect of how the data has been entered — and then lay out the handful of decisions we need from you so we can fix it the way you want.

### What you're seeing, and why

There are two separate places in the system where a Product Type and Subcategory can live:

- **On the certificate.** When an analyst creates or edits a certificate, they pick the Category, Product Type, and Subcategory right there on the certificate form. This is where that information has actually been entered, day to day. It's stored on the certificate and shows correctly on the certificate views.
- **On the product record itself.** Each product *also* has its own Product Type and Subcategory fields. These are what the **Product Management** page reads and displays.

Here's the catch: those two were never linked. Entering the Type/Subcategory on a certificate did **not** copy it onto the product. The product's own fields were only ever set if someone filled them in directly on the product screen — which, in practice, almost never happened, because the natural place to enter that detail was the certificate.

So the Product Management page is doing exactly what it's supposed to: it's reading the product's own Type/Subcategory and showing them. For most products those fields are simply **empty**, so the column is blank. The information you're expecting to see does exist — it's just sitting on the certificates, one level over from where this particular page looks.

To put numbers on it: across all the products in the system today, only a small handful have their own Type/Subcategory filled in. The large majority are blank for exactly the reason above — not because anything was lost or broken, but because that detail was captured on the certificate and never mirrored onto the product.

### How we want to fix it

There are two pieces — one automatic, one that needs your sign-off:

**1. Going forward (automatic — no action from you).**
We're adding a step so that whenever a certificate is saved, its Product Type and Subcategory are also written onto the linked product (when the product doesn't already have its own value). From the next deployment on, the Product Management columns will fill in on their own as certificates are created and edited. This is careful by design: it never overwrites a value someone deliberately set on the product — it only fills in blanks.

**2. Existing products (one-time backfill — needs your approval).**
For the products that are already in the system and currently blank, we'd run a one-time pass that copies the Type/Subcategory from each product's certificates onto the product. Because this changes existing records, we'd take a **full backup first**, and we'd send you the **exact list of products it will fill** so you can review it before we commit anything. Nothing runs against the live system without your go-ahead.

### The one wrinkle — products with conflicting certificates

Most products are straightforward: all of a product's certificates point to the same Product Type, so there's one clear value to fill in.

But a smaller group of products are linked to certificates that carry **different** Product Types from one another. For those, there isn't a single obviously-correct value, and we don't want to guess — picking the wrong one would put inaccurate data in front of you (and, in some cases, in front of partners). So our recommendation is to **leave those blank for now** and hand you a short list of them as a separate, deliberate decision later (whether to split them into distinct products, choose one value each, etc.).

### Decisions we need from you

1. **The backfill itself.** Are you OK for us to fill the existing blank products from their certificates — with a full backup taken first and a review list sent to you beforehand? *(Our recommendation: yes.)*

2. **The conflicting-certificate products.** For products whose certificates disagree on the type, do you want us to:
   - **(a)** leave them blank for now and bring you a short list to decide individually *(our recommendation — most accurate, fully reversible)*, or
   - **(b)** automatically use the value from each product's most recent certificate, accepting that this may occasionally be imprecise but fills every row now?

3. **Source of truth going forward.** Are you happy for the **certificate** to be the source that auto-fills the product's Type/Subcategory (option 1 above), or would you prefer those fields be maintained separately and manually on the product screen? *(Our recommendation: let the certificate auto-fill — it matches how your team already works and keeps the page populated with no extra effort.)*

There are a couple of longer-term, optional questions this opens up too — for example, whether Product Type/Subcategory should become *required* fields, and whether two products with the same name but different Type/Subcategory should be formally treated as distinct records. Those aren't needed to fix what you're seeing now, so I'd suggest we park them and revisit only if you want to; happy to walk through them whenever.

I've logged all of this as CR-186 for the record. If you can answer the three decisions above — even just "go ahead, recommendations are fine" — we'll prepare the review list and get it moving.

Thanks!
Shawn
