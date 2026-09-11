# Draft follow-up — Amazon payload sent 2026-09-10 predates the CEF code fix

Status: **DRAFT for Shawn's review. Not sent.**
Context: the payload in `Scripts/data-ops/_amazon_payload/20260910-144252/` was sent to the client for
review of their same-day record edits. It was pulled at **14:43**; CR-021 was applied at **16:06**, so
the file carries the pre-fix `amazonCode` value. Corrected pull:
`Scripts/data-ops/_amazon_payload/20260910-164647/`.

---

## What is actually wrong with the file they have — verified, not assumed

Compared the sent file against the corrected one row by row and column by column.

**Exactly one field differs. Nothing else.**

| Artifact | Status |
|---|---|
| Certificate set (212 certificates) | **Identical** — same numbers, none added, none missing |
| Every field on every certificate **except `amazonCode`** | **Identical** |
| Product identifiers — 19,147 rows | **Identical** keys, same rows, same order; only `amazonCode` differs |
| `amazon-membership-reconciliation.csv` | **Byte-identical** |
| `amazon-certresults-reconciliation.csv` | **Byte-identical** |
| **`amazonCode` on Certified Environmental Facts** | **Stale** — reads `CEF`, should read `greencircle_cef` |

Affected: **78 certificates**, their **78 GS1 documents**, and **7,482 of 19,147** product-identifier
rows.

**Everything the client asked to review is correct in the file they received.** Their same-day edits
— the 11 certificates that left the feed and the 7 re-typed from Carbon Footprint Reduction — are all
accurately represented. The staleness is confined to one field carrying its old value.

> **Method note.** A first pass used `sed 's/CEF/greencircle_cef/g'` to test whether that was the only
> difference; it reported "still differs" and was **wrong** — a blanket substitution is not a diff.
> The column-aware comparison is what established the claim above. Do not characterise a file to a
> client on the strength of a text substitution.

## The important part: Amazon itself is unaffected

Amazon does not consume this file. They call the API directly with their key, and the API has been
returning `greencircle_cef` since 16:06. **Nothing needs to be re-sent to Amazon and nothing needs to
be corrected on their side.** The stale value exists only in a snapshot generated for human review.

---

## Draft note to the client

Hi Carly,

One correction on the Amazon payload I sent earlier today — worth thirty seconds of your time, and
it's good news rather than bad.

That file was generated at 2:43pm. The `greencircle_cef` change went in at 4:06pm, so the export
you have still shows the old `CEF` value in the certification code column. The export is a snapshot,
and I took it before the change rather than after.

**Everything you asked to review in it is correct.** I've compared it against a fresh export line by
line: the same 212 certificates, the same 19,147 product identifiers, and every single field matches
except the certification code. Your changes from today are all accurately reflected — the 11
certificates that came out of the feed and the 7 that moved from Carbon Footprint Reduction to
Certified Environmental Facts.

**Nothing needs to happen with Amazon.** They don't receive this file — they call our API directly,
and that has been returning `greencircle_cef` since 4:06pm. So the live feed is correct; only my
snapshot was taken too early.

I've attached the corrected export. If you'd already started reviewing the first one, you don't need
to start again — the only difference is that one column now reads `greencircle_cef` instead of `CEF`
on the 78 Certified Environmental Facts certificates.

Apologies for the extra email.

Thanks,
Shawn

---

## Before sending

- [ ] Attach `Scripts/data-ops/_amazon_payload/20260910-164647/` (not `…-144252/`)
- [ ] Confirm which files were actually sent — this note assumes the full bundle. If only
      `amazon-certificates.csv` went, say so and narrow the note accordingly.
