# Key System Backend Functions

**Updated:** 2026-08-14 · **Source:** verified against application code, not from prior documentation
**Status:** all open questions in the previous draft are now answered

---

## Certificate Number

**Format:** `YY-####` — two-digit year, hyphen, four-digit sequence. Example: `26-0001`.

- **Auto-assigned.** The system allocates the next available number in sequence; analysts do not
  type it.
- **Year rollover.** The sequence restarts at `0001` each calendar year and the prefix advances.
  The first certificate created in 2027 is `27-0001`. Nothing carries over from the prior year.
- **Beyond 9,999.** The number widens to five digits (e.g. `26-10000`) **only after all 9,999
  numbers for that year have genuinely been used**. Isolated high-numbered records — for example
  from an import or a legacy load — do not push new certificates into five digits.
- **Assigned at save, not at form open.** The number is allocated at the moment the record is
  written. If two analysts open the Create form at the same time, the first to save keeps the
  number they were shown; the second is silently assigned the next available number. **The same
  certificate number is never issued twice.**
- **Field capacity:** 50 characters.

> **Migrated records exception.** A limited set of records carried over from the prior system share
> a certificate number with another record, because those numbers were issued to more than one
> certificate before the system existed and cannot be changed after issue. These are individually
> flagged. Newly created certificates can never take an existing number.

---

## Product Identifiers

**Where changes are saved differs by page — this is the point most often misunderstood:**

| Page | Behaviour |
|------|-----------|
| **Certificate → Edit** | Product ID changes save **immediately** when you click **Save Product ID**. They do **not** wait for the certificate's Save button. |
| **Certificate → Create** | Product IDs are saved together with the certificate when you save the new record. |

**Practical guidance for analysts:** on the Edit page, complete all Product ID additions, edits and
removals as a group before leaving the page. Navigating away does not discard them — they are
already saved.

**Supported identifier types and expected formats:**

| Type | Description | Expected format |
|------|-------------|-----------------|
| ASIN | Amazon Standard Identification Number | 10 characters, letters and digits |
| UPC | Universal Product Code | 12 digits |
| GTIN | Global Trade Item Number | 8, 12, 13 or 14 digits |
| EAN | European Article Number | 13 digits |
| ISBN | International Standard Book Number | ISBN-10 or ISBN-13 |
| SKU | Stock Keeping Unit | 3–50 characters, letters/digits/hyphen/underscore |
| MPN | Manufacturer Part Number | 1–50 characters, letters/digits/hyphen/underscore/period |

Values that do not match the expected format are rejected at save with a message naming the type.

**Status.** A Product ID's status is derived automatically from its parent certificate. It is not
set by the user.

---

## Time Out

**30 minutes of inactivity.**

- The timeout is a **sliding** window — the 30-minute clock resets on every page load or action.
  It is 30 minutes of *inactivity*, not 30 minutes per login.
- Applies to both the login session and the sign-in cookie.

**What analysts need to know:** there is **no on-screen warning before the session ends, and
in-progress form data is not preserved.** A certificate Create or Edit form left open for more than
30 minutes without activity will return the user to the login page on submit, and unsaved entries on
that form are lost.

**Recommended practice:** save long certificate entries incrementally rather than composing the
entire record in one sitting.

---

## Character Limits

Values exceeding these limits are rejected at save.

| Field | Limit |
|-------|-------|
| Certificate Number | 50 |
| Status | 50 |
| Location *(certificate and company)* | 175 |
| Country | 100 |
| Partner | 100 |
| **Certification Results** | **1,000** |
| Company name | 255 |
| Product name | 255 |
| Product Category / Product Type / Certification Type name | 255 |
| Product ID value | 50 |
| Product ID — product name | 1,000 |
| Product ID — SKU | 100 |
| Product ID — description | 500 |
| Tag name | 50 |
| Contact info | 255 |
| Email address | 255 |
| Website | 500 |

**Note on error messaging.** Certification Results returns a specific message on import
("exceeds maximum length of 1000 characters"). Other fields currently return a generic validation
error rather than naming the limit. Flagged as a usability improvement; not a functional defect.

---

## Import Files

- **CSV only.** Excel (`.xlsx`), JSON and PDF are not accepted for import.
- **Always start from the template.** Download the Template from the relevant page or section for
  the type of content being imported, populate it, and upload it. Column headers must match the
  template.
- **Maximum file size: 10 MB.**

### Multiple entries in a single cell

Separate items with a **comma** — not a semicolon, pipe, or any other character.

```
Tags:            recycled, post-consumer, verified
Internal Tags:   priority-review, amazon-scope
```

Leading and trailing spaces around each item are trimmed automatically.

*(Exception: the Partner column also accepts a pipe `|` as a separator. Commas work everywhere and
are the recommended convention.)*

---

## Change log

| Date | Change |
|------|--------|
| 2026-08-14 | Time Out and Character Limits answered from code. Product Identifiers clarified — save behaviour differs between Create and Edit. Certificate Number 5-digit rule and save-time assignment confirmed as written. |
