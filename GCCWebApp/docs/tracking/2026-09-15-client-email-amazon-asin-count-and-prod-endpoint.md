# Draft reply — Amazon's 7K ASIN pull + "when is the prod endpoint ready" (2026-09-15)

Status: **DRAFT for Shawn's review. NOT SENT — awaiting your approval.**

> **✅ DEF-425 deployed and verified 2026-09-15 17:04** — 212 rows / 212 distinct certificates,
> 0 membership findings, and two independent paged walks byte-identical on both endpoints.
>
> ⚠️ **HOLD AGAIN — send only AFTER DEF-426 deploys.** The figures in this letter were updated on
> 2026-09-16 to the **post-DEF-426** feed: **20,312 associations / 16,078 distinct**. Until that
> bundle (`GCCWebApp-deploy-20260916-1023.zip`) is live, the feed still returns 20,380 and this letter
> would be wrong on arrival. **Distinct ASINs are unchanged at 16,078 and no product is lost** — all
> 68 removed associations exist on other certificates — so only the association count moved.
>
> **Notice period set 2026-09-15: two weeks.** No placeholders remain — this draft is complete and
> ready to send on your word.

Source: Amazon email — *"I was able to review and test the API pull and received a successful
response of 7K ASINs. I will proceed to code generation and data analysis, let me know when the prod
endpoint is ready for use and if there is a different set of credentials there."*

Evidence: `Scripts/data-ops/_amazon_payload/20260915-150759/` (live pull, test environment,
2026-09-15) reconciled against the database with the API's own eligibility predicate.

> **Why this needs to go out quickly.** They said they are *proceeding to code generation and data
> analysis*. The 7K is page 1 of 3 — they have **less than half the catalogue** and do not appear to
> know it. Every hour they build on it is work done against a partial set.

---

## The headline: 7K is one page, not the whole feed

Their number is almost exactly what a single un-paged call returns. We reproduced it from our own
pull:

| What they read | Certificates | ASIN rows | Distinct ASINs |
|---|---|---|---|
| **Page 1 only (what a bare call returns)** | 100 | 7,928 | **7,368** ← their "7K" |
| **The full feed** | **212** | **20,312** | **16,078** |

`PageSize` defaults to **100** and is capped at 100 (`Models/Api/PaginationParams.cs`), so
`GET /api/v1/certificates?includeProductIds=true` with no page parameter returns exactly the first
100 certificates and stops. There is no error and the response is perfectly well-formed — which is
why it reads as a successful pull.

**They are missing 112 of 212 certificates and roughly 8,700 distinct ASINs.**

### What they need to change

Every response carries a `pagination` block. Loop until `hasNext` is false:

```
GET /api/v1/certificates?includeProductIds=true&pageSize=100&pageNumber=1
GET /api/v1/certificates?includeProductIds=true&pageSize=100&pageNumber=2
GET /api/v1/certificates?includeProductIds=true&pageSize=100&pageNumber=3   → hasNext: false
```

`pagination.totalCount` (212) and `pagination.totalPages` (3) are both in the first response, so they
can also assert the total up front rather than discovering it by walking off the end.

---

## The shape of the payload: it is sent **by certificate**, with ASINs nested

This is the second way to land on a wrong count, and it is worth stating before they write the parser
rather than after.

**Neither endpoint returns one record per ASIN.** Both return one record per *certificate*, with the
ASINs nested inside it:

| Endpoint | Top-level records | Where the ASINs live |
|---|---|---|
| `/api/v1/certificates?includeProductIds=true` | **212** — one per certificate | `productIds[]` on each certificate |
| `/api/v1/certificates/gs1` | **212** — one per certificate | `products[0].additionalTradeItemId[]` |

In the GS1 feed the `products` array always holds **exactly one** entry — it is the certificate's
product, not the individual ASINs — and every ASIN for that certificate sits inside that one entry.

```json
{ "certificateNumber": "26-0520",
  "company": "...",
  "certificationResults": "...",
  "productIds": [
    { "type": "ASIN", "value": "B0F66B3WZ6", "status": "Active",
      "partner": "Amazon", "productName": "Hefty Party Perfect...", "productSku": null }
  ] }
```

So **212 top-level records is not 212 ASINs** — they must flatten the nested array to reach 20,312.

**The nesting is very uneven, which is the part that breaks naive code:**

| | |
|---|---|
| ASINs per certificate — min / median / max | **1 / 25 / 2,440** |
| Certificates with **no** ASINs | **39** of 212 |
| Largest | `24-0300` (2,440) · `26-0439` (1,127) · `23-1048` (1,106) |

**Flag the empty case explicitly.** 39 delivered certificates have an empty `productIds` array — a
certified product is not necessarily sold on Amazon — so code that assumes at least one entry will
fail on nearly one record in five.

> **Held back deliberately: `gtin` is null on all 212 GS1 products.** Every identifier is type `ASIN`
> in `additionalTradeItemId`; there are no GTIN or SKU values anywhere in the feed. In a GS1 document
> `gtin` is normally the canonical product identifier, so a strict GS1 reader could expect it
> populated. This may well be by design, but it is not captured in any requirement we can find —
> **decide internally before raising it**, since mentioning it to Amazon invites a schema
> conversation. Not included in the draft message below.

---

## Which number should they reconcile to — 20,312 or 16,078?

This is the second thing likely to cause a mismatch, and it is worth settling before they build.

- **20,312** = ASIN-to-certificate associations (rows in the feed).
- **16,078** = distinct ASIN values.

The gap is real and it is **not** a duplication fault: **0 certificates list the same ASIN twice, and
there are 0 duplicate (certificate, ASIN) pairs anywhere in the feed.** Every ASIN on a certificate is
sent exactly once for that certificate.

The difference is legitimate overlap — the same product can hold more than one certification, so it
is delivered under each certificate it belongs to:

| ASIN appears on | ASINs | Entries produced |
|---|---|---|
| 1 certificate | 12,544 | 12,544 |
| 2 certificates | 3,005 | 6,010 |
| 3 certificates | 358 | 1,074 |
| 4 certificates | 171 | 684 |
| **Total** | **16,078** | **20,312** |

> ⚠️ **Corrected 2026-09-16** (figures below are the pre-DEF-426 totals this correction was made
> against; the feed now delivers 20,312). An earlier draft said *"4,302 ASINs appear on more than one
> certificate."* **Wrong.** 4,302 is the surplus **entries** (20,380 − 16,078); the number of ASINs
> that actually repeat is **3,599**. They differ because 532 ASINs sit on three or four certificates
> and contribute two or three surplus entries each. Caught before sending, by computing the
> distribution instead of subtracting two totals. *A difference of two counts is not a count of
> things.*

So "how many ASINs" has two correct answers that will not reconcile. Worth asking which their
threshold logic uses.

All identifiers on Amazon-eligible certificates are type **ASIN**, all status **Active** — no
GTIN/SKU rows are diluting either figure.

---

## One thing we owe them: wait for our fix before they page

We found a defect in our paging while validating their number (**DEF-425** — now **deployed and
verified on test, 2026-09-15 17:04**). Ordering was not fully deterministic across page boundaries,
so on the 15:07 pull four certificates came back on two pages and four others on none — 212 rows
carrying only 208 certificates. The 17:04 re-pull returns 212 / 212 with 0 findings.

**It did not affect their 7K** (page 1 was clean; the fault sat on the page 2/3 boundary). But it
would affect them the moment they start paging, which is exactly what we are about to ask them to do.

**Decided 2026-09-15 (Shawn): we tell them.** It is a short-lived problem — the fix is built and
ships with this deploy — and it is far cheaper to say so now than to have them chase a count that
will not reconcile. It also lands well: we found it because we were checking their number, not
because they reported a fault.

**Why we did not catch it on the 2026-09-10 or 2026-09-11 pulls.** Worth having straight in case they
ask, because the honest answer is not "the data changed":

- The fault condition was **present and exposed on three earlier runs** — the same 16 certificates
  tied on `2023-11-01` straddled the same page-2/3 boundary on 09-10 (twice) and 09-11. It simply did
  not fire: the database happened to return the tied rows in the same order for both pages those
  days. It is non-deterministic, so a clean run was never evidence of correctness.
- **No check we ran could have detected it even when it did fire.** The reconciliation compares sets
  of certificate numbers, and a set collapses duplicates; `pagination.totalCount` agreed because 212
  was the correct count. Nothing distinguished "212 certificates" from "212 rows carrying 208".
- A code search for the known remedy (`.ThenBy(c => c.Id)`, from DEF-166/234) **returned hits in this
  very file** — three non-paged call sites already had it — so the gap read as already handled.

A row-count-vs-distinct-count assertion is now part of the payload export. See
`docs/tracking/ways-to-improve.md`.

---

## Their second question: the prod endpoint and credentials

**Short answer: there is no separate production endpoint or second set of credentials. The
environment they are integrating against now becomes production; only the hostname changes.**

- Same application, same database, same partner API keys. **The key they hold today keeps working** —
  nothing to re-issue, nothing to re-configure at their end beyond the base URL.
- `dbtest.greencirclecertified.com` is live today, TLS is valid for it, and it is the host partners
  integrate against (`docs/cutover/database.greencirclecertified.com-runbook.md`).

**One implementation detail worth telling them now:** `pdfDownloadUrl` is generated from the host
they call. If they persist those URLs during data analysis and the hostname later changes, the stored
links point at the old host. They should store the certificate number and build the URL at read time,
or plan to re-resolve after cutover.

### ⚠️ Do not give them a date — this is blocked on a client decision

`database.greencirclecertified.com` is **not free**: it belongs to the client's existing certificate
database. The cutover cannot run until that legacy database is retired or renamed, and the client has
not decided whether the application takes over `database.` at go-live or gets a different hostname
(runbook, scope change 2026-09-14). DNS also sits at HostMonster and we need to confirm who holds
that login.

**Suggested line to Amazon:** *"The endpoint you are testing against is the same application and
database that will serve production — your credentials carry over unchanged. The only change at
go-live is the hostname, and we will give you at least two weeks' notice before it moves."*

**Two weeks is our commitment (Shawn, 2026-09-15).** Worth noting internally: the notice period is
ours to honour, but the trigger date is not ours to set — it depends on the client retiring the
legacy `database.greencirclecertified.com`. So the fortnight starts when *they* confirm, and we
should not let the promise drift into an implied date we cannot hold.

That is true, useful, and commits to nothing we do not control.

---

## Draft message

**Subject:** Your 7K ASIN pull is page 1 of 3 - the full feed is 16,078 ASINs

**Attached:** GCC Database Partner API - Partner Integration Guide v1.2 (September 2026)

Hi [name],

Glad the pull worked. One important thing before you go further on the data analysis: **the 7K is the first page of three.**

The endpoint pages at 100 certificates per call and defaults to page 1, so a call without a page parameter returns a complete, valid response containing about 7,400 distinct ASINs - but that is 100 of 212 certificates. The full feed is **212 certificates, 20,312 ASIN associations, 16,078 distinct ASINs**. Each response carries a `pagination` block with `totalCount`, `totalPages` and `hasNext`; loop until `hasNext` is false and you will have the complete set.

This is **section 7, Pagination** of the integration guide we sent on 14 September, if it is useful to your developers.

**One correction to that section, and it is ours.** The two example calls in §7 omit `includeProductIds=true`, so copying them verbatim returns correctly paged responses containing **no product identifiers at all** — valid, well-formed, and empty of the ASINs you are after. The parameter was documented in §3, but not in the one snippet a developer is most likely to copy.

**A corrected guide (v1.2) is attached.** Section 7 reads as follows with that correction applied - reproduced in full here so your developers do not need to open the attachment:

> **7. Pagination**
>
> The API returns certificate data in pages of up to 100 certificates per request. Use the pagination object in each response to determine whether more pages are available:

```
// Page 1
GET /api/v1/certificates?includeProductIds=true&pageNumber=1&pageSize=100

// Check response
"pagination": {
  "totalCount": 250,
  "pageNumber": 1,
  "pageSize": 100,
  "totalPages": 3,
  "hasNext": true,      // more pages available
  "hasPrevious": false
}

// Page 2
GET /api/v1/certificates?includeProductIds=true&pageNumber=2&pageSize=100
```

> Loop until `hasNext` is false.

The numbers in that sample are illustrative. **For your account today the real values are `totalCount` 212 and `totalPages` 3**, so you can assert against those on the first response rather than discovering the end by walking off it.

## The shape of the feed, before you write the parser

**The feed is sent by certificate, not by ASIN.** Both endpoints return one record per certificate - 212 of them - with that certificate's ASINs nested inside:

| Endpoint | Top-level records | Where the ASINs are |
|---|---|---|
| Standard list, with includeProductIds=true | 212 - one per certificate | `productIds[]` on each certificate |
| GS1 | 212 - one per certificate | `products[0].additionalTradeItemId[]` |

On the GS1 feed the `products` array always holds **exactly one** entry - it is the certificate's product, not the individual ASINs - and every ASIN for that certificate sits inside it.

So **212 top-level records is not 212 ASINs**: you need to flatten the nested array to reach 20,312.

Two things about that nesting that are easy to miss:

- **It is very uneven** - 1 ASIN on the smallest certificate, 2,440 on the largest, median 25.
- **39 of the 212 certificates carry no ASINs at all.** That is legitimate - a certified product is not necessarily sold on Amazon - but code that assumes at least one entry will fail on nearly one record in five.

## Two things worth confirming on your side

1. **Which ASIN count your threshold logic needs** - 20,312 ASIN-to-certificate associations, or 16,078 distinct ASINs. Both are correct and they will not reconcile against each other, so it is worth fixing which one you mean now rather than after the analysis.

   To be clear about where the difference comes from, because it is not a duplication fault: every ASIN on a certificate is sent exactly once for that certificate - there are no repeated ASINs within a certificate and no repeated (certificate, ASIN) pairs anywhere in the feed. The gap is legitimate overlap, where one product holds more than one certification and is therefore delivered under each:

   | ASIN appears on | ASINs | Entries produced |
   |---|---|---|
   | 1 certificate | 12,544 | 12,544 |
   | 2 certificates | 3,005 | 6,010 |
   | 3 certificates | 358 | 1,074 |
   | 4 certificates | 171 | 684 |
   | **Total** | **16,078 distinct** | **20,312 entries** |
2. **Please re-pull whenever you are ready - the fix is already live.** While validating your number we found and fixed an issue in our paging that could repeat one certificate and skip another at a page boundary. It did not affect the single-page result you already have, but it would have affected a full paged pull. That fix was deployed and verified on 15 September, and the feed now returns 212 certificates with no repeats or gaps.

## The production endpoint

**It is the same application and database you are testing against now - your API key carries over unchanged and there is no second set of credentials.** The only change at go-live is the hostname.

That date depends on a domain retirement on our client's side, so I do not want to give you a firm one yet - but **you will have at least two weeks' notice before the hostname moves**, so there is no risk of it changing under you mid-integration.

One detail for your code generation: `pdfDownloadUrl` is built from the hostname you call. If you persist those URLs, either store the certificate number and construct the link at read time, or plan to re-resolve them after the hostname changes.

[sign-off]

---
---

# Not part of the email - internal notes

## Open questions for Shawn before this goes

1. ~~Do we tell them about DEF-425?~~ **Decided 2026-09-15: yes.** Reflected in the draft above.
2. ~~What notice period do we promise for the hostname change?~~ **Decided 2026-09-15: two weeks.**
3. **Has the client decided the go-live hostname?** The guide currently promises
   `database.greencirclecertified.com`. If that changes, the integration guide changes with it and
   Amazon needs telling twice.
4. **Which ASIN definition does Amazon's threshold use?** Their answer may become a requirement.
5. **`gtin` is null on all 212 GS1 products** - decide internally whether that is by design before
   raising it with Amazon. Deliberately excluded from the message above.
