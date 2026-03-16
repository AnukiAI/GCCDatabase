# Scope Change Request: Amazon API & Certification Data Enhancements

**Prepared For:** GreenCircle Certified, LLC
**Prepared By:** PointSolve Technology / Development Team
**Date:** February 5, 2026
**Version:** 2.0
**Status:** Pending Client Approval

---

## 1. Background

Amazon has communicated expanded requirements for the Climate Pledge Friendly (CPF) integration. Based on the email exchange between Kyle Bowman (Amazon) and Carly Povilaitis (GreenCircle) from January-February 2026, the request can be distilled to two goals:

**Amazon Goal 1:** Display richer certification data to customers.
> *"This product uses X% less energy than average"*

**Amazon Goal 2:** Filter CPF eligibility using actual percentage values rather than binary yes/no.
> *"Moving us from the digital yes/no of 'this product is certified' to an analog scenario where we can see exactly why it is meeting CPF's criteria."*
> -- Kyle Bowman, Amazon

**GreenCircle Constraint:** Keep the solution simple and flexible.
> *"We want to avoid creating multiple fields or fields specific to a certain cert type since things are always changing."*
> -- Carly Povilaitis, GreenCircle

This document assesses the impact on the current system, presents design options, and outlines the work required.

---

## 2. Current System Assessment

Before proposing changes, it is important to note how much of this need the system already addresses.

### Already Built & Operational

| Capability | Notes |
|-----------|-------|
| **Certification Results field** | Free-text field (1,000 characters) on every certificate. Analysts can already enter percentage values, scores, and threshold data. |
| **Certification Type + Subcategory hierarchy** | Admin-managed parent/child types (e.g., "Energy Savings" > "Consumer Behavior"). Supports multi-select. |
| **Amazon Code mapping** | Each Certification Type has an `AmazonCode` field (e.g., `greencircle_energy_savings`) already stored in the database. |
| **Product Identifiers** | Supports multiple ASINs, UPCs, GTINs, EANs per certificate with partner tagging. |
| **GS1 JSON-LD API endpoint** | Fully compliant GS1 Web Vocabulary format, ready for Amazon integration. |
| **Partner API filtering** | Amazon's API key automatically restricts access to Amazon-tagged certificates only. |
| **API key management** | Admin can generate, revoke, and monitor partner API keys with rate limiting. |
| **Certificate PDF download** | Available via API for partner retrieval. |

### What Is Not Built

| Gap | Impact |
|-----|--------|
| **Pathway field** | No concept of "pathway" exists. This is the primary new requirement. |
| **Certification Results visibility** | The field exists on Create/Edit forms but is **not visible** in certificate tables, tile views, or CSV/Excel exports. Data goes in but cannot be reviewed without editing each certificate individually. |
| **Amazon Code in API responses** | The field exists in the database but is not returned in any API response. |

### Assessment Summary

The system already returns **20 data fields** per certificate through the API. Amazon's expanded requirements add **2 new fields**: Pathway and Amazon Code. The rest of the work is making existing data visible where it currently is not.

---

## 3. What Needs to Change

There are three categories of work, ranked by priority.

### Priority 1: Fix Data Visibility (No Schema Changes)

These items require no database changes and carry minimal risk. They address the immediate gap where data is collected but not surfaced.

| Item | Details |
|------|---------|
| Surface Certification Results in certificate tables | Currently invisible after data entry. Add column to internal and external table views. |
| Surface Certification Results in CSV/Excel exports | Currently missing from all export outputs. |
| Add Subcategory to certificate tables and exports | Improve data visibility across the board. |
| Expose Amazon Code in API responses | Field already exists on Certification Type. Add to standard and GS1 API output. |

**Risk:** Low. No schema changes. Additive only.

### Priority 2: Add Pathway Field (Core New Feature)

This is the one genuinely new feature. Pathway provides context for the Certification Results value (e.g., *"32% energy savings"* becomes meaningful when paired with *"Consumer Behavior Pathway"*).

**Three design options are presented in Section 4 below.**

**Risk:** Medium. Requires one database migration, one new UI element, and updates to API responses.

### Priority 3: Soft Validation & API Enhancements (Polish)

These items improve data quality and API usability but do not block Amazon's integration.

| Item | Details |
|------|---------|
| Soft validation warnings | When Partner = Amazon and Certification Results is empty, display a warning (not a blocker) to the analyst. |
| Add Pathway filter to API | Allow API consumers to filter certificates by pathway value. |
| Updated API documentation | Refresh Swagger documentation and Amazon integration guide. |

**Risk:** Low. No schema changes. Behavioral logic only.

---

## 4. Pathway Design Options

The Pathway field is the critical design decision. Below are three options with trade-offs.

### Option A: Standalone Pathway Lookup Table (Recommended)

**How it works:** A lightweight `Pathway` table holds admin-managed dropdown values. Each value can optionally be scoped to a specific Certification Type. The Certificate record stores a reference to the selected Pathway.

**Example data:**

| Pathway Name | Scoped To | Available For |
|-------------|-----------|---------------|
| Design Efficiency | Energy Savings | Only Energy Savings certs |
| Consumer Behavior | Energy Savings | Only Energy Savings certs |
| Operational Improvement | Energy Savings | Only Energy Savings certs |
| General | *(all types)* | Any certificate |

**How the form works:**
- Analyst selects Certification Type (existing behavior)
- Pathway dropdown populates with relevant options for that type, plus any "General" pathways
- Pathway remains optional -- only required cert types show relevant options
- If no pathways are configured for a cert type, the dropdown is hidden

**API output:**
```
"pathway": "Consumer Behavior"
```

| Pros | Cons |
|------|------|
| Flexible: Admin controls all values without developer involvement | Requires 1 new database table and 1 migration |
| Smart scoping: Analysts see only relevant pathways per cert type | Slightly more complex than a plain text field |
| Clean API output: Consistent, queryable string values | Requires a small admin management section |
| Prevents free-text inconsistency ("consumer behavior" vs "Consumer Behavior") | |
| Future-proof: New cert types get their own pathways without code changes | |
| Respects Carly's constraint: Single field, not cert-type-specific fields | |

---

### Option B: Simple String Field (No Lookup Table)

**How it works:** Add a plain `Pathway` text field (max 200 characters) directly on the Certificate. No dropdown, no lookup table. Analysts type the pathway name manually.

**API output:**
```
"pathway": "Consumer Behavior"
```

| Pros | Cons |
|------|------|
| Fastest to implement (1 field, 1 migration) | No data consistency ("consumer behavior" vs "Consumer Behavior" vs "Cons. Behavior") |
| No admin page needed | Cannot filter reliably in API (free-text variations) |
| Maximum flexibility | Analyst must remember valid pathway names |
| | Amazon receives inconsistent strings |
| | Carly would need to manage consistency through training, not the system |

---

### Option C: Extend Existing Certification Type Hierarchy

**How it works:** Treat Pathway as a third level in the existing Certification Type tree. For example:

```
Energy Savings (Level 1 - Type)
  Consumer Behavior (Level 2 - Subcategory)
    Design Efficiency Pathway (Level 3 - Pathway)
```

**API output:**
```
"certificationTypeSubcategory": "Consumer Behavior",
"pathway": "Design Efficiency Pathway"
```

| Pros | Cons |
|------|------|
| No new table -- extends existing architecture | Conflates two different concepts (what vs. how) |
| Admin already knows how to manage the Certification Type tree | Three-level hierarchy increases UI complexity |
| | Subcategory and Pathway serve different purposes (classification vs. methodology) |
| | Harder to query: Pathway buried in type hierarchy |
| | Carly specifically warned against adding complexity to cert types |
| | May require rework if pathway logic diverges from type logic in the future |

---

### Recommendation

**Option A (Standalone Pathway Lookup Table)** provides the best balance of flexibility, data quality, and simplicity. It respects the constraint of a single field while ensuring consistent data for Amazon's consumption. The scoping feature means analysts are not overwhelmed with irrelevant choices.

---

## 5. What We Are NOT Doing (Scope Boundaries)

Based on guidance from both Amazon and GreenCircle, the following are explicitly out of scope:

| Excluded Item | Reason |
|--------------|--------|
| Cert-type-specific fields | Carly: *"We want to avoid creating multiple fields or fields specific to a certain cert type"* |
| Mandatory Certification Results for all certs | Only Amazon-tagged certs may need this; non-Amazon certs should not be affected |
| Forced numeric validation on Certification Results | Results vary by type: percentages ("95.5%"), levels ("Level 2"), scores ("87/100"). Forcing numeric format breaks legitimate data. |
| New API version (v2) | Adding optional fields to v1 is non-breaking. No justification for a new version. |
| GS1 structural changes | GS1 endpoint is already fully compliant. Only addition is the new Pathway field. |
| Changes to the 14-state certificate workflow | Unrelated to Amazon's request. |

---

## 6. Risk Assessment

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|------------|
| Building features Amazon does not consume | High | Medium | Confirm with Amazon which fields they parse before development |
| Pathway design does not fit future cert types | Medium | Low | Option A's scoping and "General" fallback handles this |
| Validation blocks analyst workflows | High | Medium | Warnings only, never hard blockers |
| Existing certificate data incompatible | Low | Low | All new fields are optional; existing records unaffected |
| API changes break Amazon's current integration | High | Low | All additions are new optional fields; no removals or renames |

---

## 7. Questions Requiring Client Input

Development should not begin until these questions are answered. They directly affect the design and prevent rework.

### For Carly / GreenCircle

| # | Question | Why It Matters |
|---|----------|---------------|
| 1 | **What are the initial Pathway values?** For example, for SEP/Energy Savings: "Design Efficiency", "Consumer Behavior", "Operational Improvement"? Are there pathways for other cert types? | Determines initial data for the Pathway lookup table and whether scoping is needed immediately. |
| 2 | **Should Pathway be visible to external users?** Or is it internal-facing and API-only? | Affects whether the field appears on the external certificate table view. |
| 3 | **When Partner = Amazon, should the system warn if Certification Results is empty?** Soft warning only (analyst can proceed), or should it remain fully optional? | Determines validation behavior on the certificate form. |
| 4 | **Which design option do you prefer for Pathway?** Option A (admin dropdown with scoping), Option B (free text), or Option C (extend cert type hierarchy)? | Core architectural decision. |
| 5 | **Is the Certification Results field format acceptable as-is?** Free text that can contain "95.5% recycled content" or "UL 2809 Level 2"? Or do you need a separate numeric-only field? | If free text works, we avoid a breaking change. If numeric is required, additional design is needed. |

### For Amazon (via Carly)

| # | Question | Why It Matters |
|---|----------|---------------|
| 6 | **Which API format does Amazon prefer?** The standard JSON endpoint or the GS1 JSON-LD endpoint? Both are available. | Determines where we prioritize field additions. |
| 7 | **Does Amazon parse the `certificationResults` field today?** If not, when will they begin consuming it? | Determines urgency. If Amazon is not yet parsing it, we have time to get Pathway right. |
| 8 | **How will Amazon use the Pathway value?** Customer-facing display? Internal CPF filtering? Both? | Affects naming conventions and data format requirements. |
| 9 | **What is Amazon's timeline for needing these fields?** | Determines priority and phasing. |

---

## 8. Implementation Summary

Assuming Option A is approved and questions are answered:

| Phase | Work Items | Database Change | Risk |
|-------|-----------|----------------|------|
| **Phase 1: Visibility** | Surface Certification Results + Subcategory in table views, exports, and API | None | Low |
| **Phase 2: Pathway** | Create Pathway table, admin management, form dropdown, API exposure | 1 migration | Medium |
| **Phase 3: Polish** | Soft validation warnings, API filtering by pathway, documentation update | None | Low |

**Phases are sequential.** Phase 1 can begin immediately with no approvals needed beyond this document. Phase 2 requires answers to Questions 1, 4, and 5. Phase 3 requires answers to Questions 3, 6, 7, and 8.

---

## 9. Approval

| Role | Name | Date | Decision |
|------|------|------|----------|
| GreenCircle (Client) | | | Pending |
| Development Lead | | | Pending |
| Project Manager | | | Pending |

**Approved Pathway Design Option:** ____________________

**Notes:**

---

*This document should be reviewed and approved before any development begins. Questions in Section 7 should be routed to the appropriate parties at your earliest convenience.*

*Prepared by PointSolve Technology -- Development Team*
