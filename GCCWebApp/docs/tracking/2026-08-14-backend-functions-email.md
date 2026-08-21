# Email draft — Key System Backend Functions (answers to open items)

**Purpose:** paste-ready. Everything below the line is the email body.
**Full reference version:** `docs/tracking/2026-08-14-key-system-backend-functions.md`

---

**Subject:** Key System Backend Functions — open items answered (timeout, character limits)

Hi [Name],

Following up on the Key System Backend Functions section — the two outstanding questions are
answered below, and I've confirmed the remaining items against the application itself rather than
against earlier documentation. Two points needed a correction, noted at the end.

---

**1. Session Timeout**

The system times out after **30 minutes of inactivity**. It is a sliding window — the 30-minute
clock resets on every page load or action, so it's 30 minutes of inactivity rather than 30 minutes
per login.

One point worth including in analyst-facing material: there is **no on-screen warning before the
session ends, and in-progress form data is not preserved**. A certificate Create or Edit form left
open past the timeout will return the user to the login page on submit, and unsaved entries on that
form are lost. We'd recommend the guide advise saving longer certificate entries incrementally.

If a longer window is preferable, this is a straightforward configuration change — happy to set it
to whatever period works for your team.

---

**2. Character Limits**

| Field | Limit |
|-------|-------|
| Certificate Number | 50 |
| Status | 50 |
| Location (certificate and company) | 175 |
| Country | 100 |
| Partner | 100 |
| **Certification Results** | **1,000** |
| Company name | 255 |
| Product name | 255 |
| Product Category / Type / Certification Type | 255 |
| Product ID value | 50 |
| Product ID — product name | 1,000 |
| Product ID — SKU | 100 |
| Product ID — description | 500 |
| Tag name | 50 |
| Contact info / Email | 255 |
| Website | 500 |

Values over the limit are rejected at save. Imported files additionally have a **10 MB maximum file
size**.

---

**3. Confirmed as documented — no change needed**

*Certificate Number.* Format `YY-####` (e.g. `26-0001`), auto-assigned. The sequence restarts at
`0001` each calendar year — the first certificate of 2027 will be `27-0001`, with nothing carried
over. It widens to five digits only after all 9,999 numbers for a year are genuinely used; isolated
high-numbered records from an import will not push new certificates into five digits.

The number is allocated **at the moment of save**, not when the form is opened. If two analysts are
working simultaneously, the first to save keeps the number shown to them and the second is
automatically given the next available number. The same certificate number is never issued twice.

*Import — multiple entries in one cell.* Confirmed: separate items with a **comma**, not a
semicolon. For example: `recycled, post-consumer, verified`. Spaces around each item are trimmed
automatically. CSV only, always starting from the downloadable template.

---

**4. Two corrections to the current draft**

**Product Identifiers.** The current wording — "auto saves updates upon revision, not on save" — is
accurate on the **Edit** page only. There, Product ID changes save immediately when you click *Save
Product ID*, independent of the certificate's Save button. On the **Create** page, Product IDs are
saved together with the new certificate. As written, the instruction reads as incorrect on one of
the two pages, so we'd suggest splitting it:

> *On the Edit page, Product ID changes save immediately when you click Save Product ID — they do
> not wait for the certificate Save button. On the Create page, Product IDs are saved with the
> certificate.*

**Migrated certificate numbers.** The section states a certificate number is never assigned twice.
That holds for every certificate created in the system. It's worth adding a footnote that a limited
set of records migrated from the prior system do share a number, because those numbers were issued
to more than one certificate before this system existed and cannot be changed after issue. Those
records are individually flagged and new certificates can never take an existing number.

---

I've attached the fully updated section with these answers incorporated. Let me know if you'd like
the timeout adjusted or the Product Identifiers wording revised differently.

Best regards,
Shawn West
Anuki Consulting
