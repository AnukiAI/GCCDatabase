---
org: GreenCircle Certified Database
title: Analyst Certificate Guide
slug: GCC_Analyst_Certificate_Guide_v1.0
lede: How to create, edit, publish and import certificates, and what the system does on its own.
version: 1.0
issued: September 2026
applies_to: Test environment
prepared_by: Anuki Consulting
audience: Analysts and administrators
colophon: GreenCircle Certified Database — Analyst Certificate Guide, version 1.0. Prepared by Anuki Consulting, September 2026. Screenshots taken from the test environment; the live site may differ in minor visual detail. For anything not covered here, contact your system administrator.
---

> [!WARNING] About the screenshots
> Every screenshot was taken from the current build on the test site, signed in as an analyst. Company names, product names and certificate numbers in the pictures are whatever happened to be on screen at the time.

# 1. What you can do

Analysts and administrators share the certificate screens. Signing in takes you to the internal certificate list, and the menu gives you **Certificates**, **Dashboard**, and **Companies & Products**. Administrators additionally get **Administration**; everything in this guide is available to both roles.

![The internal certificate list. This is the working view — every certificate, every status, with the filters and columns you choose.](50-internal-list)

The internal list differs from what a company or partner sees in one important way: **it shows every certificate in every status**, including Draft, Hold and Expired. The company's own view shows only certificates that are in force. Section 2 explains which is which.

## 1.1 Filtering by status

The last box in the filter panel, below **Additional**, is **Status**. It lists all eight statuses. Tick one to see only certificates in that status; tick several to see all of them together. The result count updates as you tick.

![The Status filter with Draft ticked. The box scrolls to show the remaining statuses.](58-status-filter)

The status filter works with everything else on the list. It shows as a tag above the results, it is kept in the page address so a link you share opens with the same filter, and **Export All to CSV** exports only what it is showing.

> [!NOTE] Staff only
> The Status filter appears only on the internal list. Company and partner accounts do not have it, because their lists already contain only certificates that are in force.

# 2. The eight statuses

Status is the single most consequential field on a certificate. It decides whether the certificate appears on the public database, whether it reaches a partner feed, and whether the certified company can see it at all.

| Status | In force? | On the public database? | Company sees it? |
|---|---|---|---|
| **Draft** | No | No | No |
| **Client Approved - Not Paid** | No | No | No |
| **Client Approved - Confidential** | **Yes** | **No** — deliberately embargoed | **Yes** |
| **Client Approved - Paid** | **Yes** | Yes | **Yes** |
| **Hold** | No | No | No |
| **Republished** | **Yes** | Yes | **Yes** |
| **Expired** | No | No | No |
| **Expired - No Annual Verification** | No | No | No |

> [!IMPORTANT] Confidential is an embargo, not a pause
> A Confidential certificate is paid, in force and badgeable. It is withheld from the anonymous public listing and from nothing else — the certified company sees it, and it still counts as active. Do not use Confidential to park work that is not ready; use Hold or Draft for that.

## 2.1 There is no enforced path between statuses

You may move a certificate from any status to any other. The system does not restrict the order, and it will not stop you going from Draft straight to Republished.

Two things still happen automatically, and they are covered in section 7: a certificate whose expiration date has passed is forced to **Expired** when you save it, and **every status change is recorded** against the certificate whether you made it or the system did.

# 3. Creating a certificate

Open **Certificates**, then **Create Certificate**.

![The top of the Create Certificate form. The certification number is assigned for you.](51-create-information)

## 3.1 The certificate number is assigned, not typed

**Certification #** is filled in and read-only. The number shown is a preview of the next available number — the final number is assigned when you save, and if another analyst saves first you are given the next one automatically. You never need to work out the next number yourself, and you cannot collide with a colleague who is creating a certificate at the same moment.

## 3.2 Fields that do more than they look like

Three fields have effects beyond the certificate in front of you.

| Field | What it also does |
|---|---|
| **Company Email**, **Company URL** | Pre-filled from the company record. **Editing them here updates the company record**, so the change applies to that company's other certificates too |
| **Product Name** | Selecting a product links the certificate to a product record. Product Category, Type and Subcategory are **not** auto-filled from it — set them deliberately |
| **Status** | Decides publication immediately on save. See section 2 |

**Certification Results** is free text and is stored and displayed exactly as you type it. The field is required for Amazon CPF certificates and optional otherwise. Nothing reformats it, so type the value in the form it should appear in.

**Partner(s)** may hold more than one partner. Each partner sees only its own association — adding two partners does not show either of them the other.

**Country** may also hold more than one value, and **Location** is the free-text "City, State" line.

# 4. The certificate file

A **PDF is required** to create a certificate. Drop it on the upload zone or browse for it; the preview image is generated from it automatically.

![The PDF upload zone, with the tag panel beside it.](52-create-pdf-and-tags)

> [!NOTE] The preview is blank until you save
> The preview panel shows "Certificate Unavailable" while you are still filling the form. That is expected — the image is generated from the PDF after the certificate is saved, not while you are typing.

**Check the file before you save.** Once you have chosen a PDF, its name appears under the upload zone with a **View PDF** button. That opens the file you have just chosen in a new browser tab, every page of it, so you can confirm it is the right document — especially a multi-page one — before anything is saved.

To replace a file later, open the certificate in **Edit** and upload a new PDF over the old one.

**On a saved certificate**, clicking the certificate image opens the preview. A certificate with more than one page shows **Page 1 of N** with arrows to turn the pages, and **View PDF** opens the whole document in a new tab. This is the same preview that companies see; the Company User Guide describes it in full.

# 5. Tags, and product identifiers

**External Tags** are visible to the certified company and on public surfaces. **Internal Tags** are for GreenCircle only and are never shown to a company or partner account. Both are free to create as you type.

![Product Identifiers and Additional Fields, at the foot of the form.](53-create-identifiers)

**Product Identifiers** holds the ASINs, UPCs, GTINs and other identifiers for this certificate. They belong to the certificate rather than to the product, so two certificates on the same product can carry different identifiers.

> [!NOTE] Identifiers follow the certificate
> An identifier's active state follows its certificate. When the certificate stops being in force, its identifiers stop being reported with it — you do not need to deactivate them separately.

**Additional Fields** shows the custom fields that apply to the certification type you chose. Choosing a different type changes which fields appear.

<!-- pagebreak -->

# 6. Editing a certificate

Open a certificate from the list and choose **Edit**. The form is the Create form with the values filled in.

![The Edit form. Status, partners, certification results and pathway sit together.](54-edit-form)

Certificate numbers are the key to a certificate, so **Edit is reached by number**. The number field itself is read-only; a number that is already issued to another certificate is refused if something tries to move a certificate onto it.

> [!WARNING] Editing a certificate that is already expired
> If the certificate was **Expired** when you opened the form, you are asked to confirm before the save goes through. This is deliberate — it is the point at which to decide whether you meant to revise an expired record.

**Changing the dates does not change the status.** If you correct an expiration date on an expired certificate, the status stays Expired until you change it yourself. The system will not infer what you meant.

# 7. Expiration, and what happens without you

Two things happen automatically. Knowing them prevents most "the status changed on its own" questions.

1. **A daily sweep expires certificates whose expiration date has passed.** The status moves to Expired and the change is recorded.
2. **Saving a certificate whose expiration date is in the past forces it to Expired**, whatever status you selected on the form. If you intend a certificate to be in force, its expiration date has to be today or later.

A certificate is live **through** its expiration date and expires the day after — an expiration date of 30 June means the certificate is still valid on 30 June.

The effective date behaves the same way at the other end: a certificate is live **on** its effective date. A certificate approved ahead of time sits in the certified company's own list straight away, but does not appear on the public database until its effective date arrives.

Every status change is recorded against the certificate — who changed it, when, and from what to what — whether a person or the daily sweep made it.

# 8. Importing certificates from a spreadsheet

**Certificates → Import** takes a CSV and creates or updates certificates in bulk. The page states the rules, and they are worth reading before the first import of the day.

![The import rules, as shown on the Import page.](55-import-instructions)

## 8.1 The template

**Download Template** gives you the column headers with no sample rows. Use it rather than building a file by hand — the headers are what the importer matches on, and several columns carry their allowed values in the header text.

Only three columns are required: **Company**, **Product Name** and **Certification Type**.

## 8.2 Adding versus updating

This is decided entirely by the **Certificate #** column.

| Certificate # | Result |
|---|---|
| **Blank** | A new certificate is created and the next available number is assigned. Each blank row gets its own number |
| **Filled in** | That certificate is updated. Only the columns present in your file are changed — any column you leave out is left untouched |

![The upload step.](57-import-upload-form)

Upload the file, and the system shows you a preview of what it is about to do before anything is written. **If any row would update an existing certificate you must tick an acknowledgement**, and a backup of those certificates is saved automatically first.

Company, Product, Product Type and Tags are created if they do not already exist. **Product Category must already exist** — an unrecognised category rejects the row rather than inventing one.

Files are CSV only, up to 10MB.

## 8.3 If an import goes wrong

![Import History. Each batch can be reviewed and undone.](56-import-history)

**Import History** lists every batch with what it did. A batch can be undone from here, which reverses the rows it wrote.

> [!TIP] Import in small batches the first time
> A batch is the unit you can undo. A hundred-row file that is half wrong is one decision; ten ten-row files are ten smaller ones.

# 9. Exporting to CSV

**Export All to CSV** on the certificate list exports what the list is currently showing, so filter and search first and the export follows. The columns follow your current column selection.

The export and the import template share a column set deliberately: you can export, edit in a spreadsheet, and re-import the same file to apply the changes in bulk.

CSV is the only export format.

# 10. Troubleshooting

| Symptom | Cause | What to do |
|---|---|---|
| A certificate saved as Expired although you chose another status | Its expiration date is in the past | Correct the expiration date, then set the status again |
| A status you set reverted overnight | The daily sweep expired it on its expiration date | Extend the expiration date if the certificate is still in force |
| A company cannot see a certificate you published | It is in a status that is not in force, or its expiration has passed | Check section 2 |
| A certificate is in force but missing from the public database | Its status is Confidential, or its effective date has not arrived | Both are intended. See sections 2 and 7 |
| Editing a company email changed other certificates | That field writes to the company record | Edit the company record deliberately, or leave it alone |
| An import row was rejected for its category | Product Category must already exist | Create the category first, then re-import |
| An import updated certificates you did not intend | A Certificate # was present in those rows | Undo the batch from Import History, then re-import with the column blank |
| The preview is empty on a certificate you are creating | The image is generated after save | Save, then reopen |
| A number you expected was skipped | Numbers are assigned at save; another analyst saved first | Nothing to do — the number is still unique and sequential |

# Appendix A — Create and Edit field reference

| Field | Required? | Notes |
|---|---|---|
| **Company** | Required | Selecting it pre-fills the email and URL |
| **Company Email** · **Company URL** | Optional | Pre-filled from the company. Edits here update the company record |
| **Certification #** | Assigned | Read-only. Final number set at save |
| **Certification Type** | Required | Determines which Additional Fields appear |
| **Certificate Subcategories** | Optional | Several may be chosen |
| **Product Name** | Required | Links the certificate to a product record |
| **Product Category** · **Type** · **Subcategory** | Category required | Not auto-filled from the product — set them deliberately |
| **Effective Date** | Required | The certificate is live on this date |
| **Expiration Date** | Required | The certificate is live through this date |
| **Status** | Required | See section 2. Freely selectable |
| **Partner(s)** | Optional | Several may be chosen; each partner sees only its own |
| **Certification Results** | Required for Amazon CPF | Free text, stored and shown exactly as typed |
| **Pathway** | Optional | For Amazon CPF applicable certificates |
| **Location** | Optional | Free text, "City, State" |
| **Country** | Optional | Several may be chosen |
| **External Tags** | Optional | Visible to the company and publicly |
| **Internal Tags** | Optional | GreenCircle only — never shown externally |
| **Certificate PDF** | Required to create | The preview image is generated from it |
| **Product Identifiers** | Optional | ASIN, UPC, GTIN and others. Held against the certificate |

# Appendix B — Import columns

The template carries these headers, in this order. Certificate #, and everything below Company, may be left out of your file.

| Column | Notes |
|---|---|
| Certificate # | Blank creates; filled updates |
| Certification Type | Required |
| Cert Subcategories | |
| Status | Defaults to Draft if blank |
| Company | Required |
| Company Email · Company URL | |
| Product Name | Required |
| Product Category | Must already exist |
| Product Type · Subcategory | Created if new |
| Partner | Separate several with commas |
| Pathway | |
| Cert Results | |
| Effective Date · Expiration Date | |
| Location | |
| Country | Separate several with commas |
| External Tag · Internal Tag | Created if new |
| ASIN · UPC · GTIN · EAN | |

<!-- pagebreak -->

<!-- include: roles.md -->

<!-- include: glossary.md -->

<!-- include: getting-help.md -->
