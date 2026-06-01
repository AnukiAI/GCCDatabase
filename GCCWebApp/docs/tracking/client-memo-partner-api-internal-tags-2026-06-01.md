# Client Memo — Partner CSV Export: Internal Tag Behavior Clarified

**From:** Anuki Consulting
**To:** Carly, Kyle
**Date:** 2026-06-01
**Subject:** QC #182 follow-up — what "internal tag" controls (and what it doesn't)
**Re:** QC #182 / DEF-228

---

## Why we're writing

Following up on Carly's 5/29 retest of the Partner Export fix. Row count was right ("only the 7 under that account") — but she flagged that a cert she'd marked as internal ("Chris Test") was still appearing in the Amazon CSV.

We reproduced this on the dev environment and confirmed: the **internal tag itself is correctly hidden** from the partner CSV, but the **cert row is still in the export** because it qualifies by partner-assignment and status. That's the system working as designed — and we want to make sure we're aligned on what each lever controls before assuming there's a bug to fix.

---

## What the partner CSV actually shows today

When the Amazon partner login runs Export All to CSV, a cert with an internal tag attached behaves like this:

| Element | In the CSV? | Correct? |
|---|---|---|
| The cert's row (number, status, dates, ASIN, etc.) | **Yes** | ✅ Correct — qualifies by Partner + publishable Status |
| The Internal Tag column (header) | **No** | ✅ Correct — internal column hidden for non-admin |
| The internal tag value (e.g. "Sustainable", "Chris Test") | **No** | ✅ Correct — hidden along with the column |
| The External Tag column + values | **Yes** | ✅ Correct — external tags are partner-visible |

The partner sees that the cert *exists* but cannot see the internal classification metadata Anuki has attached to it.

---

## Why this is the intended behavior

"Internal tag" was designed as **admin-only categorization metadata** — a way for the Anuki team to flag certs internally (e.g. "Q4 Review", "High Priority Client", "Annual Audit") without that categorization leaking to partners. It is not row-level access control.

The mechanism for controlling whether a partner can see a cert at all is **Status**, not Tags:

| If you want a cert to be... | Set the Status to... |
|---|---|
| Hidden from all partners + external users + public | `Draft`, `Hold`, or `Confidential` |
| Visible to mapped partner + their downstream | `Client Approved - Paid` or `Republished` |
| Hidden because it's no longer current | `Expired` or `Expired - No Annual Verification` |

So if "Chris Test" should not be visible to Amazon, the right move is to change its Status (e.g. to Confidential), not to rely on the internal tag to hide it.

---

## What we're *not* changing

- **Partner JSON API** (`/api/v1/certificates`): no change. Same internal-tag-hidden behavior as the CSV. Cert rows that qualify by partner + status continue to flow through to the integration.
- **Partner CSV export:** no change. Already behaves as described above.
- **External and public surfaces:** no change. Already behaves the same.

In other words — no code change comes out of this investigation. The only deliverable is this clarification.

---

## What we'd like from you

A quick gut check that this matches your intended design:

1. **Do you want the current behavior preserved** (internal tag = admin metadata only, status = access control), so we can close QC #182 as resolved?
2. **Or did you intend internal tags to also hide the cert row** from partners? If so, that's a real scope change — we'd document it as a CR and walk through the broader impact (Partner JSON API contract change, External/Public tile view changes) before any work.

Our recommendation is option 1 — internal tags are working as the spec intended, and the Status field gives you the access control you need. But if you want option 2, just let us know and we'll engage on scope.

Thanks,
Shawn
