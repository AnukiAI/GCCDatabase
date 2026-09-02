# Client email — 2026-09-01 — count reconciliation + outstanding decisions

**Status:** DRAFT for Shawn's review before sending
**Attach:** `2026-09-01-migration-verification-workbook.xlsx`

> INTERNAL — read before sending. The "No Partner" tab is new in the attached workbook; the 8/18
> workbook did not contain it, which is why the client could not find the 4 certificates. Say so
> plainly rather than implying they missed a tab. Everything above the Subject line and below the
> "Not part of the email" heading is dropped by the converter.

**Subject:** Certificate counts from your 27 August review, and where the four untagged certificates are

Thank you — that closes two of the open items. Answers to your questions below, and the updated
verification workbook is attached. It is the same workbook as August, with **four new tabs**: No
Partner, Partner Coverage, Active - No Partner, and Amazon - No Identifiers. Every other tab is
untouched, so the figures you already have in writing still line up.

## 1. Where you can see the four certificates

You could not see them, and that is on us. In August we gave you the **number** — four certificates
carrying product identifiers but no partner — and never gave you the list. There was no tab for it
in the workbook, and there is no way to find them on the site either: **5,458 certificates have no
partner**, so filtering on that alone returns thousands. These four are the only ones that have
**identifiers and no partner**, and the site cannot express that combination as a filter.

They are now on the **No Partner** tab of the attached workbook, with a link to each on the site.

| Certificate | Company | Status | Identifiers | Reaches Amazon today |
|---|---|---|---|---|
| 26-0053 | Kohler Co. | Client Approved - Paid | **908** | No |
| 26-0101 | Kohler Co. | Client Approved - Paid | **908** | No |
| 25-8535 | Kohler Co. | Client Approved - Paid | **143** | No |
| 23-0814 | The Procter & Gamble Company | **Expired** | 1 | No |

**The decision is really about the three Kohler certificates.** They hold 1,959 of the 1,960
identifiers. The Procter & Gamble certificate expired in October 2024, and expired certificates are
excluded from the partner feed whatever their partner setting — so it makes no practical difference
either way.

For scale: of the **158** certificates that carry identifiers, **154 are associated with Amazon**
(14,275 identifiers). These four are not (1,960 identifiers, **12%** of the total).

**We need one word from you: should the three Kohler certificates be associated with Amazon?** If
yes, we set the association and their identifiers begin flowing on the next feed. If no, we leave
them exactly as they are. We will not change a partner association without your instruction.

### The wider question behind those four

We should be straight with you about how that figure was produced, because on its own it is
misleading. Those four surfaced because they carry **product identifiers** and no partner. That was
the only test applied, so it can only ever find certificates that have identifiers.

Measured by partner coverage instead, the picture is larger. Amazon is the only partner associated
with anything, and it is set on **244 certificates, 189 of them active**. That leaves **683 active
certificates carrying no partner at all** — the four above, and 680 more that were never flagged
simply because they have no identifiers to count. There is also the reverse case: **54 active
certificates are associated with Amazon but carry no product identifiers**, so Amazon receives the
certificate with no products attached.

**None of this is a loading error, and we checked before saying so.** The partner field reproduces
your source files exactly — 244 values compared, 244 matched, none different, and the remaining
5,458 blank in your files and blank in the database. Nothing was dropped.

The question is simply whether **244 is the Amazon population you intend.** Only you can answer
that. Three new tabs in the attached workbook let you see it for yourself: **Partner Coverage**
(every certificate by partner, identifiers and status), **Active - No Partner** (all 683), and
**Amazon - No Identifiers** (the 54). If 244 is right, nothing needs doing and only the Kohler
question above remains.

## 2. Duplicate names on the same identifier — closed

You are right that Amazon matches on the identifier rather than the name, and that is how the feed
works: the ASIN is the key and the product name is descriptive only. We keep the first name
encountered, which is what is loaded today. **Nothing changes and nothing further is needed.** This
item is closed.

## 3. Blank product names — please resend that tab

Understood that the values are highlighted in green, but the table did not survive the email — only
the column headings reached us. **Please send the workbook itself, or just that one tab, with the
green cells intact** and we will apply the names.

It is a small job: **11 product identifiers, across 2 certificates**, still have no product name.
Everything else already has one.

## 4. The counts from your 27 August review

Both differences reconcile exactly. **Nothing is missing that we have not already listed for you.**

**Public 964 against Active 982 — a difference of 18, which is 2 plus 16.**

The **2** are the Sofidel America certificates, 23-0358 and 25-0351, which you identified. Neither
has an expiration date in the source file, so neither could be loaded. They are two of the 29 on the
Not Loaded tab, not additional to them.

The **16** were approved and paid, but **had not yet reached their effective date** on 27 August. A
certificate becomes publicly visible on its effective date and stays visible through its expiration
date. Before its effective date it is correctly counted as active internally — your analysts staged
it deliberately — but it is not yet a live public record.

| Company | Certificates | Effective date | Where it stands now |
|---|---|---|---|
| Fleming Door Products (ASSA ABLOY) | 26-0413 through 26-0420 | 1 September 2026 | **Public since 1 September** |
| Aqua Mizer, Inc. | 26-0479, 26-0480, 26-0481 | 1 September 2026 | **Public since 1 September** |
| Charter Next Generation | 26-0224, 26-0225 | 1 September 2026 | **Public since 1 September** |
| DuPont Performance Building Solutions | 26-0330 | 11 September 2026 | Publishes 11 September |
| X-SENSE | 26-0441, 26-0442 | 21 October 2026 | Publishes 21 October |

**Thirteen of the sixteen have already gone public on their own**, with no action from anyone. The
remaining three will publish on the dates shown. No action is needed on any of them.

**Certificate export 5,702 against 5,731 sent — a difference of 29.** These are the incomplete
source records we withheld in August and listed on the **Not Loaded (29)** tab. Re-checked today and
unchanged: 13 with no expiration date, 11 with no product name, 3 missing the product name and both
dates, and 2 with no effective date. Twenty-seven came from Expired List 1 - 4.9.26 and two from
Active List 7.31.26.

## 5. Why these counts keep moving

The two figures measure different things on purpose. **Active** means approved and not yet expired.
**Public** means approved and today falls between the effective and expiration dates. Certificates
cross into and out of that window every day on their own.

Between 27 August and today alone, **108 certificates reached their expiration date** and moved to
Expired, and **13 reached their effective date** and became public. Every one of those changes is
recorded with a reason in the Audit History.

So a count taken on 27 August will not match a count taken today, and neither is wrong. When you
want to compare two sets, compare them **as of the same date** — we are glad to produce a dated
snapshot whenever you need one.

## 6. What we still need from you

| | What we need | Why it matters |
|---|---|---|
| 1 | **Should the three Kohler certificates be associated with Amazon?** | 1,959 identifiers currently reach no partner. |
| 2 | **Is 244 certificates the Amazon population you intend?** See the Partner Coverage tab. | 683 active certificates carry no partner; 54 carry Amazon but no identifiers. |
| 3 | **Expiration dates for the two Sofidel certificates**, 23-0358 and 25-0351 | They are the only live records missing from the site. |
| 4 | **The other 27 incomplete records** — supply the missing values, or confirm we leave them out permanently | Closes the 5,731 against 5,702 difference for good. All 27 are expired history. |
| 5 | **Certificate 24-1634** — your files give two certification results, 0.63 and 0.23. We loaded 0.63, matching the certificate listing. Please confirm. | The certificate is publicly visible either way. |
| 6 | **The blank product names tab**, resent with the green cells intact | 11 identifiers on 2 certificates. |

One item from the August letter is still open and is a preference rather than a blocker: where
several certificates share a certificate number, should the public site indicate that the number is
shared? Tell us either way whenever convenient.

# Not part of the email — internal notes

- Source of every figure: TEST database `gccwebap_certificates`, queried 2026-09-01. The 8/27 state
  was reconstructed by replaying `CertificateStatusHistory` backwards; it reproduces the client's
  964 / 980 exactly.
- The partner-feed exclusion for expired certs is `Controllers/Api/v1/CertificatesController.cs:183,212`
  (filters to `PublicStatuses`). That is what makes 23-0814 moot.
- Effective-date gate is DEF-358 / `Models/CertificateVisibility.cs`. Internal Active count is
  `Repositories/CertificateRepository.cs:22` — status + expiration only, no lower bound.
- Partner coverage evidence: `reconciliation-summary.csv` line 14 `Partner,244,244,0,5458` (244 compared, 244 matched, 0 mismatched, 5,458 blank both sides). Coverage grid and the two
  lists are generated by `Scripts/data-ops/2026-09-01-add-partner-tabs-to-workbook.py`.
- Do NOT attach the `DataMigrationTestDev` copy of the 8/18 workbook: its ASINs tab carries a
  hand-typed "DRAFT - still validating ASINS" note. The attached file is built from the clean copy.
