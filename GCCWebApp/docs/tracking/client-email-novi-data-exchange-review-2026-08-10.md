# Client email — Novi data exchange guidance review (internal cover note)

**Drafted:** 2026-08-10 · **For:** Shawn to send · **Source:** Novi Data Exchange Guidance (client-supplied, 2026-07-31), reviewed against the built partner API.

**Sendable artifact:** `client-email-novi-data-exchange-review-2026-08-10.html` — open it in a browser,
select all, paste into Outlook. Inline styles only (no `<style>` block, no external CSS), so the table
survives Outlook's HTML sanitiser. Two placeholders: `[name]`, `[your name]`.

This file is the internal record and is **not** part of the email.

## Internal cross-reference

| In the email | Tracked as |
|---|---|
| Item 1 — certificate validity expressed by presence, not a status value | **CR-035** (proposed, not built) |
| Item 2 — certification standard version | **CR-036** (proposed, not built) |
| Item 3 — product-identifier tagging convention | No CR — operational; CR-171 Model A behaviour working as designed |
| Q5 — usage pattern affects the rate limit | **DEF-345** (fixed locally, not deployed; 200/hr per partner key, per-key override now functional) |

## Framing decisions in the draft, in case you want them changed

- **Opens with what already works** (six of Novi's eight rows available) before raising scope. The
  out-of-scope conversation reads better after establishing most of it is delivered.
- **Gives a cheap exit on both CRs.** CR-035 may be unnecessary if Novi only badges; CR-036 may be
  genuinely not-applicable if GCC doesn't version its standards. Both are stated as questions, not
  quotes.
- **Names two consequences the client would otherwise meet late:** revocation means adding a ninth
  workflow state (touches analyst screens, not just the feed), and expanding status data would expose
  **confidential** certificates unless deliberately excluded. Flagged as their decision with our
  recommendation, not decided for them.
- **Says "beyond agreed scope" once, in the closing paragraph**, and pairs it with "none of this blocks
  Novi" so it reads as information rather than a bill.
- **No internal identifiers in the sendable body** — no CR/DEF numbers, file paths, field names or code.
- Novi is named throughout; the "say client, not names" convention covers the client's own people in our
  documents, not their commercial partners.

## Open item not in the email

Product-identifier **type coverage** (how many certificates actually carry GTIN or MPN rather than only
ASIN) is unverified — it needs checking on the client's database, not dev. If GTIN coverage turns out to
be thin, Novi's product-level feed degrades toward their "minimum" ledger model in practice regardless
of what the API supports. Worth confirming before Novi goes live; deliberately left out of this email so
it doesn't raise an alarm we haven't substantiated.
