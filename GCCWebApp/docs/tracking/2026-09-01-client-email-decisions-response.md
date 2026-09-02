# Response — client decisions returned 1 September 2026

**Re:** "Here are the other items from the sheet that were listed as Client Decision. Are you
needing anything else from us on this?"

---

## 1. Name conflicts (same ASIN, two different names) — **CLOSED, no action**

> *"That should be ok since Amazon matches using ASIN not name."*

Agreed, and that matches how the feed works — the ASIN is the key; the name is descriptive only.
We keep the **first** name encountered, which is what is loaded today. **Nothing changes and nothing
is needed from you.** This decision is closed.

## 2. Blank product names — **we need the sheet back**

> *"Use info in green highlight below."*

Understood — but the table did not survive the email; we only received the column headers. **Please
send the workbook itself (or that one tab) with the green cells intact** and we will apply the names.

It is a small job: **11 product identifiers, across 2 certificates**, still carry no product name.
Everything else already has one.

## 3. The 4 certificates with no partner — **here they are**

> *"Where can I see the 4 referenced?"*

**You could not see them, and that is our fault** — we named the count in the August letter but never
gave you the list, and there is no tab for it in the verification workbook. There is also no way to
find them in the site: **5,458 certificates have no partner**, so filtering on "no partner" returns
thousands. The four are the only ones that have **ASINs *and* no partner**, and the site cannot
express that combination as a filter.

So here they are in full. A CSV is attached
(`2026-09-01-certificates-with-no-partner.csv`), and each is viewable directly on the site.

| Certificate | Company | Status | Valid | ASINs | Reaches Amazon today |
|---|---|---|---|---|---|
| `26-0053` | Kohler Co. | Client Approved - Paid | 20 Feb 2026 – 19 Feb 2027 | **908** | No |
| `26-0101` | Kohler Co. | Client Approved - Paid | 20 Feb 2026 – 19 Feb 2027 | **908** | No |
| `25-8535` | Kohler Co. | Client Approved - Paid | 1 Jan 2026 – 31 Dec 2026 | **143** | No |
| `23-0814` | The Procter & Gamble Company | **Expired** | 1 Nov 2023 – 31 Oct 2024 | 1 | No |

View any of them at `https://gccwebapp.com/Certificate/CertificateSingleView/26-0053` (swap in the
number). The **Partner** field on each reads *None*; the Partner column is also in the certificate
CSV export.

**The decision is really about the three Kohler certificates.** They hold **1,959 of the 1,960**
identifiers. The P&G certificate is expired and holds one ASIN — expired certificates are excluded
from the partner feed regardless of partner, so it makes no practical difference either way.

For context, of the **158** certificates that carry ASINs, **154 are associated with Amazon**
(14,275 identifiers) and these 4 are not (1,960 identifiers, **12%**).

**We need one word from you: should the three Kohler certificates be associated with Amazon?**
If yes, we set the association and their 1,959 ASINs begin flowing on the next feed. If no, we
leave them as they are. We will not change a partner association without your say-so.

---

## Are you needing anything else from us?

Yes — three things are still open:

| # | What we need | Why it matters |
|---|---|---|
| **1** | **Expiration dates for the 2 Sofidel certificates** — `23-0358`, `25-0351` | They are approved and public-eligible but cannot load without an expiration date. The only *live* records missing from the site. |
| **2** | **The other 27 incomplete records** — supply the missing values, or confirm we leave them out permanently | Closes the 5,731 vs 5,702 gap for good. All 27 are expired history. Full list is the **Not Loaded (29)** tab. |
| **3** | **Certificate `24-1634`** — your files give two certification results, **0.63** and **0.23**. We loaded 0.63, matching the certificate listing. Please confirm. | The certificate is publicly visible either way. |
| **4** | **Kohler partner association** — item 3 above | 1,959 ASINs currently reach no partner. |

Item 2 of the August letter is now answered by item 4 here. The **duplicate certificate numbers**
question from that letter — whether the public site should indicate when a number is shared by
several certificates — is also still unanswered, but it is a preference rather than a blocker; tell
us either way when convenient.
