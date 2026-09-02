# QC Log update — 2026-09-02 (in-cell, paste-ready) — Confidential certs / PI export / Amazon API

QC item (client): *"It appears any certs marked 'Client Paid - Confidential' are not included in the
PI export file (which I assume would also mean they would not go to Amazon). This is not accurate.
They must be in export and go to Amazon when they are active since that is how they get badged. The
Confidential piece is just for the public DB side to not show there. Make sure Client Paid -
Approved, Client Paid - Confidential, and Republished statuses would all be in PI export and passed
in API assuming within eff/exp date period."*

> **⚠️ Fill in the QC item # and Reporter before pasting** — [[feedback_def_entries_note_qc_id]].

**Backing record:** **CR-043** — `docs/tracking/change-request-register.md` and
`docs/tracking/CR-043-confidential-in-pi-export-and-api.md`. Status **PROPOSED, not built.**
All figures read live from the TEST environment (gccwebapp.com) 2026-09-02.

**This cell asks the client two questions (Q1, Q2 at the end of the Notes cell). It is not a
close-out — leave the item OPEN pending their answer.**

---

## Status cell

```
Open - confirmed and accepted as a change request (CR-043). You are right that Confidential certs are being excluded; we need two answers below before we build. Note 23-0758 is a separate issue - it expired 8/31/26.
```

## Bucket / Root Cause cells

```
Bucket: CR
Root Cause: Requirements - "Confidential" was specified to us as hidden from partners as well as the public database (we recommended exactly that on 8/24 and asked for confirmation). Your clarification changes the rule: Confidential = hidden from the public database only, still active for badging. Code matched the previous spec; no defect.
```

## Notes cell (paste as one cell; line breaks = Alt+Enter)

```
CONFIRMED - you are correct, and your example list is exactly right. Raising as CR-043.

WHAT WE FOUND
- 11 of your 12 examples are Client Approved - Confidential, inside their effective/expiration window, and are being excluded from the PI export and the Amazon API. Those 11 hold 110 product identifiers between them.
- We checked every Confidential certificate in the system, not just your examples: there are 37 in-window, and exactly 11 of them carry product identifiers. Those 11 are the 11 you listed. You did not miss any.

ONE EXCEPTION - 23-0758 IS A DIFFERENT ISSUE AND THIS CHANGE WILL NOT FIX IT
- 23-0758 is not Confidential. Its status is Expired and its expiration date was 8/31/26 - it came out of the export when it expired, two days before you wrote. That is correct behavior.
- We are flagging it now so it does not look like we missed it after the change ships. If First Quality has renewed it, it needs a renewal certificate or corrected dates to come back.

WHY THIS IS A CHANGE REQUEST RATHER THAN A BUG
- On 8/24 we asked which certificate states should be visible to partners, and we recommended that Confidential stay hidden, saying we needed that stated rather than assumed. The system was built to that recommendation.
- Your note tells us something we did not have: Confidential means "do not show it on the public database," not "it is not commercially active." Those certs are paid, in force, and need badging. That is a change to the rule, so we are tracking it as CR-043.

WHAT WILL CHANGE WHEN YOU APPROVE IT
- Client Approved - Paid, Client Approved - Confidential and Republished will all appear in the PI export and be returned by the partner API, as long as today is inside the certificate's effective/expiration window. That window rule is unchanged.
- The public database is untouched. Confidential certificates stay off it, exactly as you describe.
- Effect on today's data: 110 product identifiers added to the PI export (it currently has 16,081 rows), and 17 certificates become visible to Amazon.

ONE THING TO BE AWARE OF - PARTNER TAGGING
- Reaching Amazon needs two things: the certificate has to be active AND it has to be tagged to Amazon. Of the 37 Confidential certificates, 17 are tagged to Amazon; 20 are not tagged to any partner (mostly ASR Group / Domino Sugar entities).
- Those 20 have no product identifiers on them either, so they add nothing to the PI export and reach no partner. If you expect any of them badged, they need partner tags and product identifiers added - that is a data task we would scope separately. Tell us if so.

TWO QUESTIONS WE NEED ANSWERED TO FINISH THE SCOPE

Q1. Should a company see its OWN Confidential certificates when signed in to their account? Your wording ("just for the public DB side") suggests yes, but that is a third screen beyond the export and the API, so we do not want to assume it. Yes / No.

Q2. Should Confidential certificates count as "Active" in the internal dashboard counts and their exports? Today they do not - that was a deliberate change in May so that "Active" meant the same thing internally and publicly. Your reasoning here points the other way. This affects every dashboard tile, so we would rather ask. Yes / No.

Estimated effort once approved: about half a day, most of it re-running our partner-isolation and permissions test suite to prove that widening the status list has not widened who can see whose certificates.
```

---

## Source of each figure

- Status, effective/expiration dates, company, partner tags and active product-identifier counts for
  all 37 in-window Confidential certificates: read from `gccwebap_certificates` (TEST) 2026-09-02,
  read-only `SELECT`.
- **110** = sum of active `CertificateProductIds` across the 11 PI-bearing Confidential certs
  (8+2+14+8+2+2+65+3+2+1+3).
- **16,081** = active product-identifier rows whose parent cert is `Paid` or `Republished` and
  effective-date-reached — the PI export as it stands today.
- **17 Amazon-tagged / 20 untagged** = `CertificatePartners` join across the same 37.
- **"You did not miss any"** = set difference in both directions between the client's 12 and the
  PI-bearing Confidential population; `23-0758` is the only member of their list that is not
  Confidential, and there are zero PI-bearing Confidential certs outside their list.
- `23-0758` status/dates: same query; matches the values already reported to the client on 2026-09-02
  in `qc-log-update-2026-09-02-certs-23-0358-23-0758-25-0351.md`.
- CR-035 quotations: `docs/tracking/2026-08-24-client-questions-partner-api.md`.
