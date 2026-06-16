# CR-184 + CR-171 — Client email draft (2026-06-11)

> Combined draft for Shawn to send/edit. Covers two QC items:
> - **#184 / CR-184** — Global product-identifier export: **built + deployed + verified on the test env** (gccwebapp.com). 29/29 read-only smoke checks pass; admin export returns the 12-col CSV (~1,018 active rows), button present, external 403. Message = "ready to test" + confirm whether any missing fields should be added.
> - **#171 / CR-171** — Multi-partner certificates: **documented only, not built** (Create form still single-select). Message = the 5 sign-off questions.
>
> Field-confirmation context (what's in the export vs. available-but-excluded) is captured in §1 below. Full detail: `CR-184-product-identifiers-global-export.md`, `CR-171-multipartner-visibility.md`.

---

**Subject:** GCC — #184 product-identifier export is live for testing, plus decisions needed on #171

Hi Carly,

Two items from the QC list:

---

**1) #184 — Full product-identifier export — *live on the test site now, ready for you to try***

You asked for a way to pull product-identifier information across *all* products in one file (you noted the existing export was missing product name, SKU, and partner). That's built and deployed to the test environment.

**How to test it:**
1. Log in to the test site as an Admin or Analyst user.
2. Go to **Product Management**.
3. Click **"Export Active Product Identifiers"** (top of the page) — a CSV downloads.

It returns **every active product identifier across every certificate** (currently ~1,018 rows on the test data) with these columns:

> Certificate Number · Company · Identifier Type · Id Value · Partner · Product Name · SKU · Description · Effective Date · Expiration Date · Created Date · Created By

**Please confirm the columns are complete for your analysis.** A few fields exist in the system but we did *not* include — tell us if you want any of them added:

- **Certificate Status** and **Identifier Status** — we left these out because the export is already filtered to *active* identifiers only, so every row's status is the same. We can add them back if you'd rather see them.
- **Certification Type** (e.g. the certification standard)
- **Certification Results** (the optional test-score/threshold field)
- **Last Updated date / Updated by**
- Anything else you routinely need on this report.

Two notes on scope:
- This version is **one-click, no filter screen**. If you need to slice by partner, identifier type, or date range, that's a fast follow-up — just say the word.
- The export is **Admin/Analyst only**; external partner/company users can't access it.

---

**2) #171 — Multi-partner certificates — *need your decisions before we build***

*"Partner must be multi-select if used for partner login access."* We've scoped this but want your input first, because the decisions are really yours.

Today a certificate ties to **one** partner; you've asked for a cert to carry **more than one** (e.g. Amazon *and* Novi). The key behavior: when a cert is shared, each partner login sees the cert but **only ever sees their own name on it** — Amazon won't see Novi is attached, and vice-versa. Nothing leaks between partners.

Five questions before we build:

1. **Public view** — for anyone browsing the public list (no login), should a partner name show at all? Our recommendation: **no**.
2. **Company users** — when your company user views *their own* multi-partner cert, should they see all partner names, or none?
3. **Required?** — must every cert now have at least one partner, or is "no partner" still valid?
4. **Bulk upload** — do you need multi-partner via **CSV import** too, or is the **create/edit screen** enough for now? (Screen-only keeps this smaller.)
5. **Amazon** — the data we send Amazon stays the exact same format; Amazon just sees only their own tag. We'd want Amazon (via Kyle) to confirm they're fine receiving a cert that may be shared while only seeing their own association.

This is roughly a day or two once you confirm. It's logged as **CR-171** and on hold pending your answers.

Happy to jump on a quick call if that's easier.

Thanks,
Shawn
Anuki Consulting

---

## Internal notes (not part of the email)

- **#184 verification (2026-06-11):** `node Scripts/smoke-prod/def243-cr184-prod-fingerprint.js` → **29 PASS / 0 FAIL / 0 WARN**. Key checks: export not public (401), admin export returns `text/csv` with the 12-col header and "Total Active Product Identifiers: 1018", button present on Product Management (up-arrow), external blocked (403).
- **#184 source of truth:** `Controllers/ExportController.cs` (`DownloadActiveProductIds`, `[Authorize(Policy = "AnalystOrAdmin")]`, columns at line 283) + `Views/Product/Management.cshtml` (button + fetch handler). Results JSON: `Scripts/smoke-prod/def243-cr184-prod-fingerprint-results.json`.
- **#171 status:** not built — `Views/Certificate/Create.cshtml:205` is still a single `<select asp-for="Partner">`. Email asks the 5 questions before any build starts.
- **Available-but-excluded export fields** (basis for the §1 confirmation list): Certificate Status & Identifier Status (omitted — every row active), Certification Type, Certification Results, Updated Date / Updated By. Certificate-level fields confirmed in `Models/Certificate.cs`; identifier-level in `Models/CertificateProductId.cs`.
