# Certificate count reconciliation — the 8/27 review

**Date:** 1 September 2026
**Re:** "Counts did not align" — Public 964 / Active 982 / DB Export 980, and Cert Export 5,702 vs 5,731 sent

---

## Short answer

**Nothing is missing that we have not already listed for you, and nothing is wrong.**

Both gaps reconcile exactly:

| Gap you saw | Size | What it is |
|---|---|---|
| Active 982 → Public 964 | **18** | **2** Sofidel certificates never loaded (you identified these) + **16** certificates that were approved but **had not yet reached their effective date** on 27 August |
| Sent 5,731 → Loaded 5,702 | **29** | The 29 incomplete source records we listed on **8/18** — still awaiting your decision |

The 2 Sofidel certificates are **part of** the 29, not additional. Counting each gap once, the
complete list of records not on the site is **29 — the same 29 as in August.**

---

## 1. The 18-certificate difference between Active and Public

We rebuilt the database exactly as it stood on **27 August** and reproduced your numbers to the row:

| Measure | You counted | We reproduce |
|---|---|---|
| Public database | 964 | **964** |
| Active certificates / DB export | 980 | **980** |

### The 2 — Sofidel America

`23-0358` and `25-0351`. Neither has an expiration date in the source file, so neither could be
loaded. Both are on the **Not Loaded (29)** tab. Give us the two expiration dates and they load
and go public the same day.

### The 16 — approved, but not yet started

These certificates were approved and paid, but their **effective date had not yet arrived** on
27 August. A certificate becomes publicly visible on its effective date and stays visible through
its expiration date. Before its effective date it is correctly counted as Active internally — your
analysts staged it deliberately — but it is not yet a live public record, so the public database
does not show it.

| Company | Certificates | Effective date | Status now |
|---|---|---|---|
| Fleming Door Products (ASSA ABLOY) | `26-0413` … `26-0420` (8) | 1 Sep 2026 | **Public since 1 Sep** |
| Aqua Mizer, Inc. | `26-0479`, `26-0480`, `26-0481` | 1 Sep 2026 | **Public since 1 Sep** |
| Charter Next Generation | `26-0224`, `26-0225` | 1 Sep 2026 | **Public since 1 Sep** |
| DuPont Performance Building Solutions | `26-0330` | 11 Sep 2026 | Publishes 11 Sep |
| X-SENSE | `26-0441`, `26-0442` | 21 Oct 2026 | Publishes 21 Oct |

**13 of the 16 have already gone public on their own**, with no action from anyone — the system
re-checks the window daily. The remaining 3 will publish on the dates shown. As of today the public
count is **869** and only **3** certificates are waiting on an effective date.

This is the behaviour agreed on **17 August**: a certificate staged early must not publish before
its start date, on the public site or through the Amazon partner feed. The same rule is why the
public count will keep moving day to day, in both directions.

---

## 2. The 29-certificate difference in the total

`5,731 − 5,702 = 29`. These are the records we **withheld** on 18 August because your source files
were missing a value the system requires, and we did not want to invent one. Nothing has changed
since — re-verified today:

| What is missing | Records |
|---|---|
| No expiration date | 13 |
| No product name | 11 |
| No product name, no effective date, no expiration date | 3 |
| No effective date | 2 |

**27 came from `Expired List 1 - 4.9.26`; 2 from `Active List 7.31.26`.** 27 are expired history;
the 2 active ones are the Sofidel pair above.

The full list — certificate number, company, product, and the exact field missing from each — is
the **Not Loaded (29)** tab of the verification workbook sent on 18 August.

**This is decision #1 from that email and it is still open:** can you supply the missing values, or
should these records be left out permanently? Answer it and this gap closes for good. The 2 Sofidel
certificates are the ones worth doing first — they are live records, not history.

---

## 3. Why the numbers keep moving

The two counts measure different things on purpose:

- **Active** = approved and not yet expired.
- **Public** = approved, **and** today falls between the effective and expiration dates.

Every day, certificates cross into or out of that window on their own. Between 27 August and today
alone, **108 certificates reached their expiration date** and moved to Expired, and **13 reached
their effective date** and went public. Every one of those changes is recorded with a reason in the
**Audit History**.

So a count taken on 27 August will never match a count taken today, and neither is wrong. When you
want to compare two sets, compare them **as of the same date** — we can produce a dated snapshot on
request.

---

## What we need from you

| # | Decision | Effect |
|---|---|---|
| **1** | The **2 Sofidel** expiration dates (`23-0358`, `25-0351`) | They load and go public immediately |
| **2** | The other **27** incomplete records — supply the missing values, or leave them out permanently | Closes the 5,731 vs 5,702 gap for good |

No action is needed on the 16 not-yet-effective certificates. 13 have already published themselves;
the remaining 3 will publish on 11 September and 21 October.
