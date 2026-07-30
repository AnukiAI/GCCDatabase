# Change Order: Amazon API & Certification Data Enhancements

**Prepared For:** GreenCircle Certified, LLC
**Prepared By:** Anuki
**Date:** February 5, 2026
**Status:** Proposed (Updated)

---

## 1. Background

Amazon's Climate Pledge Friendly (CPF) program is requesting richer certification data. Rather than a binary "this product is certified," Amazon wants quantitative values such as *"This product uses 32% less energy than average"* and the ability to filter CPF eligibility using actual percentage thresholds.

**Amazon has confirmed** they will use the `certificationResults` field for two purposes:
1. **Customer-facing display** -- showing quantitative results (e.g., energy saving %, recycled content %) on product pages
2. **Internal CPF filtering** -- moving from binary eligibility to threshold-based filtering so Amazon can model the impact of changes before committing them

**GreenCircle has confirmed** that a Pathway field with admin-managed dropdown values is needed to provide context for certification results (e.g., for SEP certifications, the same percentage may relate to different methodologies such as Design Efficiency vs. Consumer Behavior).

This change order proposes the work required to meet these requirements while respecting GreenCircle's directive to keep the system simple and avoid cert-type-specific fields.

---

## 2. What Already Exists

| Capability | Status |
|-----------|--------|
| Certification Results field (free-text, 1,000 chars) on every certificate | Built |
| Certification Results returned in API responses (standard JSON and GS1 JSON-LD) | Built |
| Certification Type + Subcategory hierarchy (admin-managed) | Built |
| Amazon Code mapping on each Certification Type | Built (database only) |
| Product Identifiers (ASIN, UPC, GTIN, EAN) with partner tagging | Built |
| GS1 JSON-LD API endpoint for Amazon integration | Built |
| Partner API key management with automatic filtering | Built |
| Certificate PDF download via API | Built |

### Gaps

| Gap | Impact |
|-----|--------|
| **Amazon Code** exists in database but not returned in API responses | Amazon cannot use it for CPF mapping |
| **Pathway** concept does not exist | No way to categorize the methodology behind certification results (confirmed needed by GreenCircle for SEP) |

---

## 3. Proposed Changes

| # | Change | Details | Risk | DB Change |
|---|--------|---------|------|-----------|
| 1 | Expose Amazon Code in API responses | Return `amazonCode` in standard JSON and GS1 JSON-LD endpoints so Amazon can map certificates to CPF categories | Low | No |
| 2 | Add Pathway field | Admin-managed lookup table, dropdown on certificate form scoped by cert type, exposed in API. *Pending: initial values -- see Section 5* | Medium | 1 table |
| 3 | Expose Pathway in API responses | Return `pathway` value in standard JSON and GS1 JSON-LD endpoints | Low | No |
| 4 | Soft validation warning for Amazon certificates | When Partner = Amazon and Cert Results is empty, display warning (not blocker) to the analyst. *Pending: confirmation -- see Section 5* | Low | No |
| 5 | Updated API documentation | Refresh Swagger docs and Amazon integration guide with new fields | Low | No |

**Summary:** 5 changes total. 1 database migration (new Pathway table). All other changes are additive with no schema impact. All new API fields are optional -- existing consumers are unaffected.

---

## 4. Pathway Design: Admin-Managed Lookup Table

GreenCircle has confirmed the approach: an admin-managed dropdown where each value can optionally be scoped to a specific Certification Type.

| Example Pathway | Scoped To | Available For |
|----------------|-----------|---------------|
| *(values TBD)* | SEP / Energy Savings | Only SEP certs |
| General | *(all types)* | Any certificate |

**How it works:**
- Analyst selects Certification Type, then Pathway dropdown shows only relevant options
- Pathway remains optional; dropdown hidden if no pathways configured for the cert type
- Admin manages values without developer involvement
- Prevents free-text inconsistency that would degrade API quality for Amazon

**Why this approach:**
- Data consistency is high (controlled values, no variant spellings)
- Admin has full control to add/modify/remove pathway values as cert types evolve
- Clean API filterability for Amazon
- Future-proof: new cert types get their own pathways without code changes

---

## 5. Confirmed & Pending Items

### Confirmed by Email Thread (Feb 5, 2026)

| Item | Confirmed By | Detail |
|------|-------------|--------|
| Amazon will use `certificationResults` | Kyle (Amazon) | For customer-facing display and internal CPF threshold filtering |
| `certificationResults` already available in API | Development | Returned in both standard JSON and GS1 JSON-LD endpoints |
| Amazon wants energy saving % and recycled content % | Kyle (Amazon) | Specific use cases mentioned |
| Amazon is fine with partner-filtered API | Kyle (Amazon) | Only sees Amazon-tagged certificates |
| Pathway field is needed | Carly (GreenCircle) | For SEP certs, need to know which path the number relates to |
| Pathway should be admin-managed dropdown | Carly (GreenCircle) | "Pathway field with dropdowns (that Admin can control)" |
| Avoid cert-type-specific fields | Carly (GreenCircle) | "Things are always changing" -- generic approach preferred |

### Pending -- Answers Needed to Complete Changes #2-4

| # | Question | For | Blocks |
|---|----------|-----|--------|
| 1 | What are the initial Pathway values for SEP? (e.g., "Design Efficiency", "Consumer Behavior", "Operational Improvement"?) | GreenCircle | #2 |
| 2 | Are there other cert types besides SEP that need pathway values right away? | GreenCircle | #2 |
| 3 | Should Pathway be visible to external users, or internal/API-only? | GreenCircle | #2 |
| 4 | Confirm: when Partner = Amazon and Cert Results is empty, display a soft warning to the analyst? | GreenCircle | #4 |

**Change #1 and #5 can proceed immediately. Changes #2-4 require the answers above.**

---

## 6. What Is NOT Included

| Excluded Item | Reason |
|--------------|--------|
| Cert-type-specific fields | GreenCircle directive: "avoid creating multiple fields or fields specific to a certain cert type" |
| Mandatory Certification Results for all certs | Only Amazon-tagged certs may need this |
| Forced numeric validation on Certification Results | Results vary by type: percentages, levels, scores. Forcing numeric breaks legitimate data. |
| New API version (v2) | Adding optional fields to v1 is non-breaking |
| Changes to the 14-state certificate workflow | Unrelated to Amazon's request |

---

## 7. Risk Assessment

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|------------|
| Pathway design does not fit future cert types | Medium | Low | Option A scoping + "General" fallback; admin can adjust without developer |
| Validation blocks analyst workflows | High | Medium | Warnings only, never hard blockers |
| Existing certificate data incompatible | Low | Low | All new fields optional; existing records unaffected |
| API changes break current integration | High | Low | All additions are new optional fields; no removals |

---

## 8. API Fields After This Change Order

The API will return the following fields per certificate. New fields are highlighted.

| Field | Description | Status |
|-------|-------------|--------|
| Certificate Number | Unique certificate identifier (e.g., GCC-2026-001) | Existing |
| Status | Certificate status (e.g., Active, Expired) | Existing |
| Issue Date / Expiration Date | Validity period | Existing |
| Company | Company name and ID | Existing |
| Product | Product name and ID | Existing |
| Location / Country | Where the certification applies | Existing |
| Partner | Associated partner (e.g., Amazon) | Existing |
| Certification Type | Type and subcategory (e.g., Energy Savings) | Existing |
| Certification Results | Quantitative result (e.g., "32% energy savings") | Existing |
| Product Identifiers | ASIN, UPC, GTIN, EAN values | Existing |
| PDF Download | Link to download certificate PDF | Existing |
| **Amazon Code** | **CPF mapping code (e.g., `greencircle_energy_savings`)** | **New** |
| **Pathway** | **Methodology context for certification results (e.g., Design Efficiency)** | **New** |

---

*Prepared by Anuki*
