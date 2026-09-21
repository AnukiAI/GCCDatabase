---
org: GreenCircle Certified Database
title: Administrator Guide
slug: GCC_Administrator_Guide_v1.0
lede: How to configure the database — companies, products, certification types, tags, users and roles.
version: 1.0
issued: September 2026
applies_to: Test environment
prepared_by: Anuki Consulting
audience: Administrators
colophon: GreenCircle Certified Database — Administrator Guide, version 1.0. Prepared by Anuki Consulting, September 2026. Screenshots taken from the test environment; the live site may differ in minor visual detail. For anything not covered here, contact your system administrator.
---

> [!WARNING] About the screenshots
> Every screenshot was taken from the current build on the test site, signed in as an administrator. The companies, counts and configuration shown are whatever was on the test environment at the time.

# 1. The Administration menu

Administrators see everything an analyst sees, plus an **Administration** menu. Day-to-day certificate work is covered in the **Analyst Certificate Guide**; this guide covers the configuration behind it.

![The Administration menu.](60-administration-menu)

| Menu item | What it controls | Covered in |
|---|---|---|
| **Role Management** | The roles, and what each one may do | Section 8 |
| **User Management** | Accounts, their role, and the companies or partners they are scoped to | Section 7 |
| **Configuration/Settings → Google Analytics** | Site analytics tracking | Section 10 |
| **API Management → Partner API** | Partner API keys | Section 9 |
| **Data Management → Certificate Types** | Certification types and their subcategories | Section 4 |
| **Data Management → Product Types** | The product category hierarchy | Section 3 |
| **Data Management → Certificate Fields** | The definitions behind the Additional Fields that appear on a certificate | — |
| **Data Management → Tags** | Internal and external tags | Section 5 |
| **Data Management → Partners** | Partner organisations | Section 9 |
| **Data Management → Pathways** | Certification pathways | Section 6 |

Companies and Products sit outside this menu, under **Companies & Products** in the main navigation.

> [!NOTE] A rule that applies across every page in this guide
> Anything referenced by a product or a certificate **cannot be deleted** while that reference exists. The usage count beside each row is what the system checks. Remove or re-point the references first, then delete.

# 2. Companies

**Companies & Products → Companies** lists every certified company, with its contact details and the number of certificates held.

![Company Management.](61-companies)

A company record is the owner of its certificates and the thing an External user account is scoped to. Two consequences worth knowing:

1. **Company email and URL are shared.** These fields also appear on the certificate form, and editing them there updates this record — so a change made while editing one certificate applies to the company everywhere.
2. **A company with certificates cannot be deleted.** Reassign or remove its certificates first.

Use **Export** for a CSV of the list.

# 3. Products and the product hierarchy

**Data Management → Product Types** is where the product taxonomy lives. It has three levels.

![Product Types Management, in tree view. The counters above the tree show how many of each level exist.](62-product-types)

| Level | Example | Notes |
|---|---|---|
| **Category** | Construction / Building Products | The top level. Must exist before a product can use it |
| **Type** | Moisture Control | Sits under a category |
| **Subcategory** | — | Sits under a type. Optional |

**Tree View** shows the hierarchy and lets you expand a branch; **Table View** lists everything flat. **Expand All** and **Collapse All** act on the whole tree.

Each row carries a usage count in the form **products / certificates**, and the padlock beside it means what the rule in section 1 says: while that count is non-zero the item cannot be deleted.

To add a level, use **Add New Category** for a top-level entry, or the **+** on a row to add a child beneath it.

> [!IMPORTANT] Categories must exist before an import runs
> A bulk certificate import creates companies, products, product types and tags that it does not recognise — but **not** product categories. A row naming a category that does not exist here is rejected. Create the category first.

Individual product records are managed separately, under **Companies & Products → Products**.

# 4. Certification Types

**Data Management → Certificate Types** holds the certification types and their subcategories.

![Certificate Types Management.](63-certificate-types)

The certification type drives two things elsewhere in the system:

1. It appears on every certificate and is one of the three columns a bulk import requires.
2. It determines which **Additional Fields** appear on the certificate form.

Subcategories sit beneath a type, and a certificate may carry several of them. Both the type and its subcategories are searchable and filterable on the certificate views.

# 5. Tags

**Data Management → Tags** manages the tags that can be attached to certificates.

![Tag Management.](64-tags)

There are two kinds, and the difference is a permission, not a label:

| Tag type | Who sees it |
|---|---|
| **External** | Everyone, including company and partner accounts, and public surfaces |
| **Internal** | GreenCircle staff only — never shown to a company or partner account, and never exported to them |

> [!WARNING] Internal tags are a confidentiality boundary
> An internal tag is invisible to the certified company. Anything written in one should be safe to keep from them — but treat the tag name as internal-only content, because that is exactly what it is.

Analysts can create either kind while editing a certificate, so this page is for reviewing, renaming and tidying the set rather than being the only way to add one.

<!-- pagebreak -->

# 6. Pathways

**Data Management → Pathways** manages the certification pathways that can be assigned to a certificate.

![Pathways Management.](66-pathways)

A pathway is optional on a certificate and applies to Amazon CPF certifications. The pathway chosen on a certificate appears on the certificate form beside **Certification Results**, which is the field Amazon CPF requires.

# 7. Users

**User Management** lists every account with its role, status and whether two-factor authentication is enabled.

Creating accounts, assigning a role, scoping an account to companies or partners, two-factor authentication and password resets are covered in full by the separate **Accounts & Sign-In Guide**. Three points belong here because they are administrative rather than procedural:

1. **An account's scope is what limits its data**, not its role. An External account with no company attached sees nothing at all.
2. **Email addresses must be unique** across all accounts.
3. **Administrator accounts cannot be deleted.** Set an account to Inactive instead — it blocks sign-in without removing the record or its history.

# 8. Roles and permissions

**Role Management** lists the roles, what each may do, and how many accounts hold it.

![Role Management. The permission chips show the first few permissions; the count beside them covers the rest.](65-role-management)

| Role | Intended for |
|---|---|
| **Admin** | Full system access, including this menu |
| **Analyst** | Certificate work — create, edit, import, export |
| **External** | A certified company's own staff, scoped to that company |
| **Partner** | A partner organisation, scoped to that partner's associated certificates |
| **Public** | The public directory. No account is needed to use it |

**Add New Role** creates a role and **Export** produces a CSV of the list. Editing a role opens its permission set.

> [!CAUTION] Changing a role changes every account that holds it
> The **Users** column tells you how many accounts a change will affect. Removing a permission from Analyst applies to every analyst immediately. There is no per-account override.

**System roles cannot be deleted.** The delete control is unavailable on them, which is why the Admin row's is greyed out.

# 9. Partners and the partner API

**Data Management → Partners** manages partner organisations, and **API Management → Partner API** manages their API keys.

Both are covered end to end — including tagging certificates to a partner, generating and revoking keys, and what the partner receives — in the separate **Partner API Setup Guide**. Use that guide rather than this section; it exists so the two do not drift apart.

The one rule worth repeating here: **a partner sees only the certificates associated with that partner**, and a certificate may be associated with several partners without either partner seeing the other's association.

# 10. Google Analytics

**Configuration/Settings → Google Analytics** holds the tracking configuration for the site. Enter the measurement ID supplied by whoever manages your analytics account and save.

This affects only visitor analytics. It does not change what any account can see, and it does not touch certificate data.

# 11. The Dashboard

**Dashboard** summarises the certificate population.

![The Dashboard.](67-dashboard)

**Total Active Certs** counts certificates that are commercially in force — Client Approved - Paid, Client Approved - Confidential, and Republished — and inside their certification period. It deliberately includes Confidential certificates: those are paid, in-force work whose public listing is withheld.

Beneath it, the same population is broken down by certification type, by company, by status, by product type and by product category. The **by status** table is the one that shows the whole picture, including the statuses that are not in force.

> [!NOTE] Why the Dashboard total and the public database disagree
> They answer different questions. The Dashboard counts what is in force; the public database shows what a stranger may see, which excludes Confidential certificates and anything whose effective date has not arrived. A gap between the two figures is expected, not an error.

# 12. Troubleshooting

| Symptom | Cause | What to do |
|---|---|---|
| A category, type or tag will not delete | Something still references it | Check the usage count on the row; re-point or remove those records first |
| An import rejects rows for their category | Product categories are never created by import | Add the category in Product Types, then re-import |
| An administrator account cannot be deleted | Administrator accounts are protected | Set it to Inactive instead |
| A role will not delete | System roles are protected | Only roles you added can be removed |
| Editing a company email changed several certificates | That field belongs to the company record | Expected. Edit it deliberately |
| An External account sees nothing | It has no company attached | Attach a company in User Management |
| A partner sees fewer certificates than expected | Certificates must be associated with the partner individually | Check the partner association on the certificates themselves |
| The Dashboard total is higher than the public database | Confidential and not-yet-effective certificates are in force but not public | Expected — see section 11 |
| A company user cannot see an internal tag | Internal tags are never shown externally | Expected. Use an external tag if they should see it |

# Appendix A — What each role can see

| | Admin | Analyst | Partner | External | Public |
|---|---|---|---|---|---|
| All certificates, every status | Yes | Yes | No | No | No |
| Own company's in-force certificates | Yes | Yes | — | Yes | No |
| Partner's associated certificates | Yes | Yes | Yes | — | No |
| Public directory | Yes | Yes | Yes | Yes | Yes |
| Create and edit certificates | Yes | Yes | No | No | No |
| Bulk import and undo | Yes | Yes | No | No | No |
| Internal tags | Yes | Yes | No | No | No |
| Administration menu | Yes | No | No | No | No |
| Dashboard | Yes | Yes | No | No | No |

Confidential certificates are visible to Admin, Analyst, the owning company and its associated partners — and withheld from the public directory only.
