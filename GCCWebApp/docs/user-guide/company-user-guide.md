---
org: GreenCircle Certified Database
title: Company User Guide
slug: GCC_Company_User_Guide_v1.0
lede: How to find, read, download and share your company's certificates.
version: 1.0
issued: September 2026
applies_to: Test environment
prepared_by: Anuki Consulting
audience: Company users · sections 1–2 explain what your account can see
colophon: GreenCircle Certified Database — Company User Guide, version 1.0. Prepared by Anuki Consulting, September 2026. Screenshots taken from the test environment; the live site may differ in minor visual detail. For anything not covered here, contact your GreenCircle representative.
---

> [!WARNING] About the screenshots
> Every screenshot was taken from the current build on the test site, using an example company account. The company name, product names and certificate numbers you see in the pictures are examples — your own account shows your own company's certificates in the same places.

# 1. What your account can see

Your account is tied to your company. Two rules decide what appears, and together they explain almost every question about a missing certificate.

**Rule one — your company only.** You see your own company's certificates and nothing else. There is no setting that widens this, and no way to search across other companies.

**Rule two — certificates that are currently in force.** A certificate has a status, and only three statuses are in force:

| Status | Meaning | In your list? |
|---|---|---|
| **Client Approved - Paid** | Certified and published | Yes |
| **Client Approved - Confidential** | Certified, but not listed on the public database | Yes |
| **Republished** | Re-issued with a fresh certification period | Yes |
| **Draft** · **Client Approved - Not Paid** · **Hold** | Still in progress | No |
| **Expired** · **Expired - No Annual Verification** | Past its certification period | No |

A certificate also drops out of your list once its **expiration date** has passed.

> [!NOTE] Upcoming certificates do appear
> A certificate whose effective date is still in the future is shown in your list as soon as it is approved, so you can see what is coming. It is not yet visible to the public — see section 8.

Your account cannot add or edit certificates, and it does not show GreenCircle's internal notes or tags. Those are deliberate.

# 2. Signing in

Go to the database address supplied by GreenCircle and sign in with your email address (or username) and password.

Creating accounts, two-factor authentication, and resetting a forgotten password are covered in the separate **Accounts & Sign-In Guide**. This guide starts from the point where you are signed in.

Once signed in you have two menu items, and only two: **Certificates** and **Products**.

# 3. Finding your certificates

Signing in takes you straight to your certificate list.

![Your certificate list. The count above the table tells you how many certificates you have in total, and how many are currently loaded.](40-certificates-list)

There are two ways to look at the same certificates, and the buttons at the top right switch between them.

**List** is the default. It is a table, it shows the most information at once, and it is the only view where you can select several certificates.

**Tile** shows each certificate as a card with its certification type, status, certification period and location.

![Tile view. Each card carries the certificate number in the top right.](44-tile-view)

The list loads more certificates as you scroll, so the count reads "Showing 20 of 23" until you reach the end.

**Sort By**, at the top right, reorders the list. The options are grouped — by date, by certificate, alphabetically, by location, and a small advanced group for product type and status.

**Columns**, above the table, controls which columns the table shows. Click it, tick or untick what you need, and use **Reset** to return to the standard set. Your choice is remembered the next time you sign in.

To open a certificate, click anywhere on its row. It opens in a **new browser tab**, so your place in the list is not lost.

# 4. Searching and filtering

The **Search** box at the top of the left sidebar searches across your certificates. It matches the certificate number, the certification type, your company name, the product name, the product category, the location and the country — and also matches tags, countries and certification subcategories attached to the certificate.

Type your term and press **Enter**. The count above the table updates to show how many certificates matched.

![A search for a product name. The count has narrowed from 23 certificates to 2.](42-search-results)

Below the search box are the filter panels. Tick as many as you need; they combine, so ticking two certification types shows certificates in either, while adding a country narrows that result further.

![The filter panels. Each one lists only the values that actually occur in your certificates, so a panel with nothing in it means none of your certificates use it.](41-filters)

| Panel | Filters on |
|---|---|
| **Certification Type** | The type of certification, and its subcategories where they exist |
| **Product Categories** | The product category, and the product types beneath it |
| **Company** | Your company. It appears for consistency with the rest of the database |
| **Countries** | Every country recorded on the certificate, not only the main one |
| **Additional** | Tags applied to the certificate |
| **Partners** | Partner programmes the certificate is associated with |

Applied filters appear as tags above the results so you can see what is narrowing the list. **Clear Filters**, beside the Filters heading, removes them all at once. Clearing the filters does not clear the search box — clear that separately.

> [!TIP] An empty filter panel is an answer
> The panels are built from your own certificates. If the **Partners** panel is empty, none of your certificates is associated with a partner programme.

# 5. The Products page

**Products** in the top menu lists the products behind your certificates, with their product category, type and any product identifiers recorded against them.

![The Products page.](47-products)

The search box at the top of the page matches the product name, your company name, the product category, the product type and subcategory, and the product's SKU and model.

The **IDs** column shows how many identifiers are recorded against each product. Open the product to read them.

> [!NOTE] Identifiers are shown, not searched
> Identifiers such as ASIN, UPC, GTIN and EAN are recorded against certificates and displayed here, but neither this search nor the certificate search in section 4 matches on them. To find the certificate behind an identifier, ask your GreenCircle representative.

You can view products here but not change them. Product records are maintained by GreenCircle.

<!-- pagebreak -->

# 6. Reading a certificate

Opening a certificate shows everything recorded against it, with the certificate image beside it.

![A certificate. The blue band carries the certification type, your company name and the certificate number.](45-certificate-detail)

| Section | What it contains |
|---|---|
| **Certification Details** | Product category, product type, product subcategory, and the location the certificate covers |
| **Certification Period** | The effective date and the expiration date |
| **Certification Results** | The result recorded by the analyst, where one applies. Shown exactly as it was entered |
| **Contact Information** | Your company's email address and website, where recorded. A dash means nothing is recorded |
| **Custom Fields** | Any additional fields that apply to this certification type |
| **Database Listing URL** | The shareable public address for this certificate — see section 8 |

Sections that do not apply to a certificate are not shown, so two certificates may have a different number of sections.

Beneath the certificate image are two buttons: a magnifier to open the preview, and a download arrow to save the PDF.

## 6.1 The preview, and certificates with more than one page

Click the magnifier, or the certificate image itself, to open the preview. It shows the certificate at a readable size over the page.

![The preview on a two-page certificate, turned to page 2. The page counter and View PDF stay in view as you scroll.](48-preview-pager)

If the certificate has more than one page, a bar under the preview reads **Page 1 of 2** (or however many pages there are). Use the **<** and **>** arrows, or the left and right arrow keys, to turn the pages. The preview stays open while you do.

On a single-page certificate there are no arrows and no page counter. That is expected; there is nothing to turn to.

**View PDF** opens the certificate document itself in a new browser tab, with every page and your browser's own search, zoom and print.

To close the preview, click anywhere outside the certificate or the **×** in the corner.

> [!NOTE] If the preview offers only View PDF
> A small number of documents cannot be shown as a picture — for example, a PDF that is password-protected. For those the page arrows do not appear. Use the download arrow to save the file instead.

# 7. Downloading certificates

There are three ways to get a PDF, and they all produce the same file.

**One certificate, from the list.** Click the download icon in the **Actions** column of its row.

**One certificate, from its own page.** Click the download arrow beneath the certificate image.

**Several at once.** Switch to **List** view, tick the checkbox at the left of each certificate you want, then click **Download Selected**. A bar above the table confirms how many you have chosen, and **Download Selected** turns green once at least one is ticked. The certificates arrive as a single ZIP file.

![Two certificates selected. The bar confirms the count, and Clear Selection empties it.](43-select-and-download)

The checkbox in the table header selects every certificate currently loaded. Because the list loads as you scroll, that means the ones you have scrolled to — scroll to the end first if you want all of them.

> [!NOTE] If a certificate downloads but others in the same batch do not
> A ZIP contains only the certificates your account may download. Anything outside your company, or no longer in force, is left out rather than failing the whole download.

# 8. Sharing a certificate

Each certificate has a public address, shown as **Database Listing URL** at the foot of its page. Copy that link and send it to a customer, a specifier or an auditor — they do not need an account, and they see a public listing for that one certificate.

![The Database Listing URL. This is the link to share.](46-database-listing-url)

What the recipient sees is decided independently of what you see, and it is stricter:

| Certificate | In your list | At the shared link |
|---|---|---|
| Client Approved - Paid, in its certification period | Yes | Yes |
| **Client Approved - Confidential** | Yes | **No** — confidential certificates are deliberately not listed publicly |
| Republished, in its certification period | Yes | Yes |
| Approved, but the **effective date has not arrived** | Yes | **No** — not until that date |
| Past its expiration date | No | No |

When a link is not available publicly, the recipient sees the message *"Certificate … is not currently available for public viewing."* The link is not broken and does not need re-issuing — it starts working on the effective date, or when the certificate stops being confidential.

> [!WARNING] Check the effective date before you send a link
> This is the most common reason a shared link appears not to work. The certificate is in your list because it is approved; the public listing waits for the effective date shown in **Certification Period**.

# 9. Exporting to CSV

**Export All to CSV**, above the table, downloads everything currently shown as a spreadsheet file. Filters and searches apply, so narrow the list first and the export matches it.

The file opens in Excel or any spreadsheet program. The columns follow the table, so use **Columns** to choose what you want before exporting.

CSV is the only export format. There is no Excel, PDF or JSON export.

<!-- pagebreak -->

# 10. Why a certificate is not in your list

Work down this list in order. The first two causes account for nearly all cases.

1. **It is not yet approved.** A certificate in Draft, Hold, or Client Approved - Not Paid does not appear. Your GreenCircle representative can tell you where it is.
2. **It has expired.** Once the expiration date has passed the certificate leaves your list. Check the certification period on your copy of it.
3. **A filter or search is still applied.** Look above the results for filter tags, click **Clear Filters**, and empty the search box. These are remembered as you move around the site.
4. **It belongs to a different company record.** Companies with several legal entities may hold certificates under more than one. Ask your representative which company your account is tied to.
5. **You have not scrolled far enough.** The count reads "Showing 20 of 23" until the rest load.

If none of these explains it, contact your GreenCircle representative with the certificate number and the product name.

# 11. Troubleshooting

| Symptom | Cause | What to do |
|---|---|---|
| A certificate is missing from your list | Status, expiry, or a filter still applied | Work through section 10 |
| A shared link shows *"not currently available for public viewing"* | The certificate is confidential, or its effective date has not arrived | Check the certification period. The link starts working on the effective date — see section 8 |
| **Download Selected** is grey and does nothing | Nothing is ticked, or you are in Tile view | Switch to **List** and tick at least one checkbox |
| Selecting everything with the header checkbox misses certificates | Only the certificates loaded so far are selected | Scroll to the end of the list first |
| Searching an ASIN, UPC or GTIN finds nothing | Identifiers are displayed but not searchable | Search by product name instead, or ask your GreenCircle representative — see section 5 |
| The certificate opened in a different tab and you lost your place | Certificates always open in a new tab, by design | Close that tab to return to your list, which is untouched |
| A CSV opens with the wrong columns | The export follows the table's current columns | Set **Columns** first, then export |
| No **Add** or **Edit** buttons anywhere | Company accounts are read-only | Certificates are created and amended by GreenCircle |
| Signed out unexpectedly | The session has a time limit | Sign in again; nothing is lost |

# Appendix A — What each status means

| Status | Meaning for your company |
|---|---|
| **Draft** | Being prepared. Not yet approved and not visible to you |
| **Client Approved - Not Paid** | Approved, awaiting payment. Not yet in force |
| **Client Approved - Paid** | In force, and listed on the public database |
| **Client Approved - Confidential** | In force, and deliberately not listed publicly |
| **Hold** | Paused. Not in force |
| **Republished** | Re-issued with a fresh certification period, and in force |
| **Expired** | Past its expiration date |
| **Expired - No Annual Verification** | Lapsed because the annual verification was not completed |

The status of each certificate is shown in the **Status** column of the list, and on each tile.

<!-- pagebreak -->

<!-- include: devices.md -->

<!-- include: glossary.md -->

<!-- include: getting-help.md -->
