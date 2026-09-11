WHAT AMAZON RECEIVES - PAYLOAD EXPORT
Retrieved 2026-09-11 13:52 from the test environment (https://gccwebapp.com)
via a live Amazon partner API key, through the same endpoints Amazon calls.

This supersedes the 2026-09-10 export.


FILES
  amazon-certificates.json              The standard list payload exactly as delivered,
                                        unmodified  (/api/v1/certificates?includeProductIds=true)
  amazon-certificates.csv               The same 208 certificates, flattened for review
  amazon-gs1.json                       The GS1 payload exactly as delivered
                                        (/api/v1/certificates/gs1)
  amazon-gs1.csv                        The same GS1 content, flattened
  amazon-product-identifiers.csv        One row per identifier (ASIN/GTIN/SKU) - 19,146 rows.
                                        This is the grain Amazon keys on; a certificate-level
                                        file alone cannot show a missing ASIN.
  amazon-certresults-reconciliation.csv Certification Results compared four ways per certificate
  amazon-membership-reconciliation.csv  Every Amazon-tagged certificate and why it is or is not
                                        in the payload
  summary.txt                           Totals and every distinct delivered value


WHAT IS IN THE PAYLOAD
  Certificates delivered ............ 208
  Product identifiers delivered ..... 19,146
  GS1 documents delivered ........... 208


CERTIFICATION RESULTS - NOTHING BROKEN
All 208 delivered values were compared against three other sources: the database as it stands
today, the pre-change snapshot taken immediately before the Certification Results change went
live on 2026-09-08, and the GS1 payload.

  All four sources byte-identical ... 208 of 208
  Mismatches ........................ 0

All 208 values are in XX% form. No bare decimals and no non-numeric values are being sent.


MEMBERSHIP - THE RIGHT SET OF CERTIFICATES
  Amazon-tagged certificates ........ 281
  Delivered ......................... 208
  Correctly withheld ................ 73
        Expired ..................... 39
        Draft ....................... 32
        Effective date not reached ... 2
  Findings .......................... 0


CHANGE SINCE THE 2026-09-10 EXPORT
The delivered count moved from 212 to 208. Four certificates had their Amazon partner tag
removed in the application on 2026-09-11 between 12:30 and 12:53:

  25-8333, 25-8334, 25-8413, 26-0519

All four are still active, in-force certificates - only the Amazon association was removed, so
they are correctly no longer sent to Amazon. No other certificate changed. Nothing was added.
