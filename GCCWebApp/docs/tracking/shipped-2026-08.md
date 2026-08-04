# Shipped — August 2026

Snapshot of delivery for August 2026. Stakeholder-readable log.

Period: 2026-08-01 → ongoing

## August 2026 — at-a-glance

| | Count |
|---|---|
| **Pending deploy 2026-08-04** (code only, no database change; **NOT yet in a built zip — folds into the next rebuild**) | **DEF-336 — "Download Selected" no longer stalls when you select a large batch of certificates.** Selecting a large number of certificates and using **Download Selected** could leave the browser waiting with nothing happening, and on a big selection the download would never arrive at all. Nothing was ever downloaded incorrectly and no data was at risk — the request simply took longer than the browser was willing to wait. Cause: the page was fetching the selected certificates **one at a time**, and each certificate is assembled from a dozen related pieces of information (company, product, tags, partners, identifiers and so on). For a 60-certificate selection that worked out to several hundred separate trips to the database, one after another, before the ZIP file could even begin. The certificates are now fetched **in a single pass** instead, which is what the underlying database is built to do well. The permission check that decides whether you are allowed each individual certificate is **unchanged and still runs certificate-by-certificate** — only the fetching was made faster, never the security. Verified on the test environment: the download now completes promptly and returns the correct result, and the automated security check that covers this screen — which had been unable to finish at all while the stall was happening — is running green again. **Worth flagging:** this was not reported by a tester. It was found by our own automated regression suite, and the reason it matters beyond the slow download is that the stall was **preventing a security check from completing** — the check that confirms one company's user can never pull down another company's certificate files. That protection was working correctly the whole time; what we had lost was the automatic proof of it, which is now restored. |
