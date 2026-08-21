# Shipped — August 2026

Snapshot of delivery for August 2026. Stakeholder-readable log.

Period: 2026-08-01 → ongoing

## August 2026 — at-a-glance

| | Count |
|---|---|
| **Prepared 2026-08-17 — not yet deployed** (code only, no database change) | **DEF-358 — Certificates no longer appear on the public database before their effective date begins.** Reported by the client: the public directory was showing certificates that had not yet reached their effective date. Putting a certificate into an approved status ahead of its start date is normal practice for the team — the work is finished and everything is staged so it is ready the moment the date arrives — but the site was treating "approved" as "publish now" and listing it immediately. A certificate should only be publicly visible **while it is inside its effective-to-expiration period**, and it now is. **The same question was raised about the information passed to Amazon, and it was right.** Those certificates were also going out over the Amazon partner feed, reported as active, so a certificate could be presented as a current certification weeks before it came into force. **What we found.** Everywhere the site decides whether a certificate may be shown publicly, it was checking two things — the certificate's status, and that it had not expired. It never checked that the certificate had *started*. There was no such check anywhere in the system; the start date was recorded, and used for sorting the lists, but was never used to decide visibility. The Amazon feed was the weaker of the two: it checked only the status, with no date check at either end. **Scale.** On the test environment, 9 of the 67 publicly visible certificates were affected, 6 of them also going to Amazon. It matters more than those numbers suggest, because the data prepared for go-live carries the same pattern — of the 981 certificates that would be publicly visible, **16 have future start dates** — so without this change the behaviour would have gone live with real certificates. **What changes.** A certificate appears on the public directory, and in the Amazon feed, only from its effective date through its expiration date, inclusive of both days. Nothing needs to be done to release it: a certificate staged today for a date next month **publishes itself on the morning of that date**, with no action from the team and no status change required. Certificates that pre-date this and have no start date recorded are unaffected. For the Amazon feed, a certificate that has not started is simply **left out** until its date arrives, rather than being sent with a new "pending" label — chosen deliberately so the agreed format Amazon receives is completely unchanged. **This also covers the downloadable spreadsheet.** The certificate list can be downloaded as a CSV without signing in, and that download was assembled by a different piece of code from the one behind the web pages — so on first pass it would still have contained the not-yet-started certificates the site had just stopped showing. It now matches: the public download went from 981 rows to 965, the difference being exactly the certificates whose start date has not arrived. Worth stating plainly because it was caught by our own pre-release checks rather than by review, and had it shipped, the site and the spreadsheet would have disagreed with each other. **Deliberately unchanged, at the client's direction:** the certifying company still sees its own upcoming certificates when signed in — a company needs to know its certificate is queued and ready — and staff continue to see everything, exactly as before. Only the public directory and the partner feed are affected. **One thing to expect at deployment:** if Amazon has already taken in the certificates currently showing early, they will see those drop out of the feed and then return on each certificate's effective date. **No data was at risk and nothing was deleted or altered** — the certificates themselves are untouched; this only changes when they become visible. |
| **Deployed 2026-08-04** (code only, no database change; **bundle 20260804-2230**) | **DEF-340 — Setting up the authenticator app now tells you when a code is rejected.** When switching on two-factor sign-in, you scan a QR code with your authenticator app and type in the six-digit code it shows. If that code was wrong, expired, or mistyped, the page simply reloaded and said nothing at all — no error, no explanation, the same screen you were already looking at. There was no way to tell a rejected code apart from a page that had just refreshed, so the only reasonable conclusion was that the setup screen was broken. It was not: the code was being correctly rejected, and the explanation for why was being generated every time. It just never made it onto the screen. The message was being handed off in the wrong way for the kind of page change involved, so it was dropped in transit. It now appears on the setup page, immediately, saying exactly what went wrong — the code was not six digits, or the code was not accepted, or the setup could not be completed. **There was a second effect worth mentioning, because it may explain something odd that was seen elsewhere.** Because that message was never picked up where it was meant to be, it was not discarded either — it stayed with the session and waited. The next time the **two-factor preferences** page was opened, it would appear there instead, as a red error about an authenticator code, attached to nothing the user had just done. That could be minutes or much longer after the fact. Both effects are resolved by the same change: the message is now delivered and used on the setup page itself, so it cannot resurface later out of context. **How this was found:** not by a tester and not reported by the client — it was surfaced by our automated pre-release checks while preparing this release, in a flow that is only reached when someone enrolls in two-factor sign-in *and* enters a bad code. No data was ever at risk and sign-in itself was never affected; two-factor protection was working correctly throughout, and this was purely about what the setup screen told you while you were switching it on. |
| **Deployed 2026-08-04** (code only, no database change; **bundle 20260804-1705**, superseded by 20260804-2230) | **DEF-336 — "Download Selected" no longer stalls when you select a large batch of certificates.** Selecting a large number of certificates and using **Download Selected** could leave the browser waiting with nothing happening, and on a big selection the download would never arrive at all. Nothing was ever downloaded incorrectly and no data was at risk — the request simply took longer than the browser was willing to wait. Cause: the page was fetching the selected certificates **one at a time**, and each certificate is assembled from a dozen related pieces of information (company, product, tags, partners, identifiers and so on). For a 60-certificate selection that worked out to several hundred separate trips to the database, one after another, before the ZIP file could even begin. The certificates are now fetched **in a single pass** instead, which is what the underlying database is built to do well. The permission check that decides whether you are allowed each individual certificate is **unchanged and still runs certificate-by-certificate** — only the fetching was made faster, never the security. Verified on the test environment: the download now completes promptly and returns the correct result, and the automated security check that covers this screen — which had been unable to finish at all while the stall was happening — is running green again. **Worth flagging:** this was not reported by a tester. It was found by our own automated regression suite, and the reason it matters beyond the slow download is that the stall was **preventing a security check from completing** — the check that confirms one company's user can never pull down another company's certificate files. That protection was working correctly the whole time; what we had lost was the automatic proof of it, which is now restored. |

---

## 2026-08-18 — Certificate data migration loaded into the test environment

**What was delivered:** the client's complete certificate dataset is now live in the test site,
replacing the test records that had accumulated during build and QA.

| Loaded into the test site | |
|---|---|
| Certificates | **5,702** across **5,527** certificate numbers |
| Companies | **301** |
| Products | **3,439** |
| Product identifiers (ASINs) | **16,234** |
| Publicly visible certificates | **965** |
| Records from the source workbooks that did not load | **29** — the incomplete ones, listed for the client |

**Verified, not assumed.** Every certificate and every ASIN in the client's own workbooks was
reconciled against the database **in both directions**: nothing missing that should be there,
nothing present that should not be, and no unexplained differences. Fourteen of fifteen data fields
matched exactly; the fifteenth is nine records where `The Netherlands` is stored as `Netherlands`,
which is the system's country standardisation working as intended.

**The Amazon integration was checked specifically and is unchanged.** The partner feed was called on
the live test site and compared against the frozen April reference responses — identical structure,
no change for Amazon to re-test. All nine partner API keys, including Amazon's, are intact.

**Nothing the client asked us to keep was touched.** Certification types, product categories and
types, tags, pathways, partners, API keys, roles and permissions, and the Google Analytics settings
all survived, and were checked again after the load. The client's own administrator account and the
analyst and Amazon partner logins were preserved.

**A full backup was taken immediately beforehand** and is held separately; the load itself ran inside
a single transaction.

**Certificate documents: attached and confirmed opening on the test site.** All **5,474** certificate
PDFs are matched to their certificates with previews generated — every document verified against its
original by checksum, with no mismatches, and each certificate holding its own distinct document.
The files were uploaded to the server and then checked end to end through the application itself:
documents open as genuine PDFs, previews render, and where several certificates share a number each
one correctly serves its *own* document rather than a sibling's.

One certificate's preview image failed to render (a known issue with that particular file); it
displays a generic preview instead, and we confirmed it shows the generic image rather than another
certificate's.

**Two decisions still outstanding with the client:** written confirmation of how duplicate
certificate numbers are handled, and a direction on 1,960 ASINs that carry no partner tag (currently
visible to every partner that calls the API).
