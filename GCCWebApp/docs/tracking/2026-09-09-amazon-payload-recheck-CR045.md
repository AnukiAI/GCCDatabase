# Amazon payload re-check after CR-045 — 2026-09-09

**Ask (client, via Shawn):** *"I also want to have someone check all the Amazon certs again once
you change the Cert Results field to ensure those didn't break. create file of what is sent to
amazon. json and csv"*

**Verdict: nothing broke.** 223 certificates delivered, every Certification Results value identical
across four independent sources, and the delivered SET is exactly what the partner rules predict —
no certificate missing, none delivered that should not be.

**Script:** `Scripts/data-ops/2026-09-09-amazon-payload-export.py test`
**Files:** `Scripts/data-ops/_amazon_payload/20260909-094724/`
**Target:** gccwebapp.com (test environment), pulled through a real Amazon partner API key.

---

## The files for the reviewer

| File | Grain | Rows | What it is |
|---|---|---|---|
| `amazon-certificates.json` | certificate | 223 | The standard-list payload **exactly as delivered**, unedited |
| `amazon-certificates.csv` | certificate | 223 | The same, flattened for Excel |
| `amazon-product-identifiers.csv` | identifier | 19,003 | One row per ASIN/GTIN — the grain Amazon actually keys on |
| `amazon-gs1.json` | certificate | 223 | The GS1-format payload as delivered |
| `amazon-gs1.csv` | certificate | 223 | The same, flattened |
| `amazon-certresults-reconciliation.csv` | certificate | 223 | The proof: four sources side by side |
| `amazon-membership-reconciliation.csv` | certificate | 295 | Every Amazon-tagged certificate and why it is in or out |
| `summary.txt` | — | — | The numbers below, machine-generated |

Both endpoints Amazon consumes are covered: `/api/v1/certificates?includeProductIds=true` (standard)
and `/api/v1/certificates/gs1` (GS1). A file from only one of them would leave the other unchecked,
and the GS1 one is where Certification Results travels as `certificationInfo.value`.

---

## Check 1 — did any value change?

For each of the 223 delivered certificates, four strings compared:

1. `Certificates.CertificationResults` **now**
2. the same column in `CertResults_bak_CR045_20260908` — the snapshot taken **before** the CR-045
   deploy
3. `certificationResults` in the standard-list payload
4. `certificationInfo.value` in the GS1 payload

| | |
|---|---|
| certificates compared | 223 |
| all four sources byte-identical | **223** |
| mismatches | **0** |

**The pre-change snapshot is what makes this falsifiable.** Comparing the API to the database only
shows they agree *today* — which would be equally true if both had changed together. The snapshot is
the fixed point.

Delivered values: 223 populated, **all** in `XX%` form, **0** bare decimals, **0** non-numeric, 0
empty. Every distinct value is enumerated in `summary.txt` rather than sampled.

## Check 2 — is the right SET of certificates being sent?

A payload where every value is correct can still be missing certificates, and check 1 would pass
regardless. So the delivered set was predicted from the database and compared.

The API's partner filter (`Controllers/Api/v1/CertificatesController.cs:178`) is
**Amazon-tagged AND badgeable status AND effective date reached.** There is deliberately no
expiration predicate — Expired is a status, not a date test.

| | |
|---|---|
| Amazon-tagged certificates in the database | 295 |
| delivered | **223** |
| correctly withheld — Expired | 39 |
| correctly withheld — Draft | 31 |
| correctly withheld — effective date not yet reached | 2 |
| eligible but missing, or delivered but ineligible | **0** |

295 = 223 + 39 + 31 + 2, with nothing unaccounted for. The 19 Confidential certificates CR-043 added
are among the 223.

---

## Column J follow-up for QC #251 — paste as one append (line breaks = Alt+Enter)

```
9/9: Re your note about having someone check the Amazon certificates - done, and we have put what Amazon receives into a file so your reviewer can see it directly rather than take our word for it.

WHAT WE CHECKED
Every certificate currently going to Amazon: 223 of them, pulled through a real Amazon API key against both feeds Amazon uses - the standard feed and the GS1 feed. For each one we compared four things side by side: the value in the database today, the value as it stood before the field change, the value in the standard feed, and the value in the GS1 feed.

All four match on all 223. Nothing moved.

We also checked that the right certificates are being sent, not just that the values are right - a correct value on a missing certificate would still be a problem. 295 certificates carry the Amazon tag: 223 are sent, 39 are Expired, 31 are still in Draft, and 2 have not reached their effective date yet. That accounts for all 295, with nothing unexplained. The 19 confidential ones from the other change are among the 223.

THE FILE
Attached, one folder, seven files:
- amazon-certificates.json / .csv - the 223 certificates exactly as Amazon receives them
- amazon-gs1.json / .csv - the same in the GS1 format
- amazon-product-identifiers.csv - one row per ASIN, 19,003 of them, which is the level Amazon actually matches on
- amazon-certresults-reconciliation.csv - the four-way comparison above, one row per certificate, so the check can be re-read rather than trusted
- summary.txt - the counts, plus every distinct Certification Results value being sent

The CSVs open straight into Excel. The JSON files are the literal API response if your reviewer or Amazon wants to see the raw shape.

Happy to re-run this at any point - it is a script now, so it takes a couple of minutes and can be repeated after any future load.
```

---

## Internal notes

- **Three defects in the harness were found and fixed before any result was trusted**, each of which
  would have produced a confident wrong answer:
  1. **A partner API call from a signed-in session returns 401.**
     `PartnerApiKeyAuthenticationMiddleware` opens with *"skip if already authenticated (cookie/JWT
     takes precedence)"*, so an admin cookie on the same session makes the middleware stand aside and
     the controller's `[Authorize(AuthenticationSchemes = "ApiKey")]` then rejects the cookie
     identity. The body is empty, so it reads exactly like a bad API key. The script now uses two
     sessions: one signed in to mint the key, one gate-only to make the calls.
  2. **137 certificate numbers carry more than one row** on this dataset
     (`Certificates.AllowsDuplicateNumber`). A plain `number → value` dictionary silently keeps
     whichever row was read last, which could hide a genuine disagreement. The script now collects
     every value per number and reports a number whose rows disagree as its own finding. Verified:
     **0** numbers disagree, and **0** duplicated numbers appear in the Amazon set, so keying by
     number is safe here — but it is now checked rather than assumed.
  3. **Row counts drift mid-run** — the certificate table went 6,019 → 6,022 while these queries ran,
     because Drafts were being created on test at the time (26-0659…26-0665, 13:34–13:42). Drafts are
     excluded by status, so the payload is unaffected, but a count quoted from one query and compared
     against another will not tie out. The script reports rows *and* distinct numbers separately.
- **Stale comment removed** at `Controllers/Api/v1/CertificatesController.cs:304`, which still
  described the deleted percentage formatter as if it ran. Replaced with a CR-045 do-not-reintroduce
  note.
- **API keys now clean up after themselves.** The export mints an Amazon key and revokes it as soon
  as both pulls finish. Five keys minted during this work (ids 18–22) were revoked.
  **Left alone, and worth a decision:** ids 8–17 are ten *still-active* Amazon keys left behind by
  earlier verification scripts (`amazon-contract-diff`, `def358-api-effective-date`, `QC retest …`),
  none of which revoke what they mint. Id 6 is the key published in the Amazon Developer Guide PDF
  and must stay. These are not mine to revoke — recommend revoking 8–17 and adding self-revoke to the
  smoke scripts, but that is Shawn's call.
- **19,003 identifiers delivered vs 18,935 rows in the 2026-09-08 PI export** — different surfaces
  with different rules (the PI export is an admin report), and two batch loads have landed since. Not
  a discrepancy between them, but do not quote the two figures as if they measure the same thing.
- Reading the export in Excel: the CSVs are UTF-8 with BOM so they open directly. `idValue` holds
  ASINs — Excel will not mangle them, but a column of long digit strings (GTIN/UPC) should be
  imported as text if the reviewer re-saves the file.
