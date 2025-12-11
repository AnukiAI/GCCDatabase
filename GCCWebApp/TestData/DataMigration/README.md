# GCC WebApp Data Migration Documentation

## Overview

This folder contains comprehensive documentation to support data migration activities for the GCC WebApp. These documents help identify risks, ensure data quality, and reduce time lost on data discovery issues.

---

## Document Inventory

| Document | Purpose | When to Use |
|----------|---------|-------------|
| `DATA_MIGRATION_RISK_ASSESSMENT.md` | Comprehensive risk catalog with mitigation strategies | Before planning any migration |
| `DATA_QUALITY_CHECKLIST.md` | Step-by-step quality validation checklist | When profiling source data |
| `DATA_VALIDATION_RULES.md` | Complete field validation rules reference | When transforming/fixing data |
| `PRE_MIGRATION_AUDIT_TEMPLATE.md` | Fillable audit document for each migration | At start of each migration project |
| `NAME_MAPPING_TEMPLATE.csv` | Template for value mapping decisions | When source values differ from target |

---

## Quick Start Guide

### Step 1: Risk Assessment (Before Migration)
1. Review `DATA_MIGRATION_RISK_ASSESSMENT.md`
2. Identify which risks apply to your data
3. Prioritize Critical and High risks
4. Estimate time impact using the risk matrix

### Step 2: Data Profiling (2 Weeks Before)
1. Obtain sample source data
2. Complete `DATA_QUALITY_CHECKLIST.md`
3. Calculate quality scores
4. Document anomalies found

### Step 3: Audit & Planning (1 Week Before)
1. Fill out `PRE_MIGRATION_AUDIT_TEMPLATE.md`
2. Create name mappings using `NAME_MAPPING_TEMPLATE.csv`
3. Get Go/No-Go decision from stakeholders

### Step 4: Validation During Import
1. Reference `DATA_VALIDATION_RULES.md` for error resolution
2. Use test files from `../ImportTemplates/` to verify import works
3. Document any new issues discovered

---

## Key Data Anomalies by Entity

### Companies
| Anomaly | Risk Level | Common Cause | Prevention |
|---------|------------|--------------|------------|
| Name variations | Critical | Different source systems | Create mapping file |
| Invalid emails | Medium | Manual entry errors | Regex validation |
| Missing URLs | Low | Optional field | Accept as NULL |

### Users
| Anomaly | Risk Level | Common Cause | Prevention |
|---------|------------|--------------|------------|
| Duplicate emails | Critical | System constraint | Pre-check uniqueness |
| Invalid roles | High | Typos, old systems | Enum validation |
| Company not found | Medium | Import order | Import companies first |

### Tags
| Anomaly | Risk Level | Common Cause | Prevention |
|---------|------------|--------------|------------|
| Invalid tag type | High | Case sensitivity | Normalize to lowercase |
| Duplicate names | Medium | User error | Uniqueness check |
| Name too long | Low | 50 char limit | Truncate with notice |

### Products
| Anomaly | Risk Level | Common Cause | Prevention |
|---------|------------|--------------|------------|
| Company not found | High | Name mismatch | Use mapping file |
| ProductType invalid | Medium | Type not created | Import types first |
| Missing description | Low | Optional field | Accept as NULL |

---

## Time Impact Reference

| Data Quality Level | Prep Time | Import Time | Fix Time | Total |
|--------------------|-----------|-------------|----------|-------|
| Clean (90%+) | 2-4 hrs | 1-2 hrs | 1-2 hrs | 5-10 hrs |
| Moderate (70-89%) | 8-12 hrs | 2-4 hrs | 4-8 hrs | 18-30 hrs |
| Poor (50-69%) | 20-40 hrs | 4-8 hrs | 16-32 hrs | 48-96 hrs |
| Legacy (<50%) | 40-80 hrs | 8-16 hrs | 32-64 hrs | 96-192 hrs |

---

## Import Order (Required)

```
1. Companies        (no dependencies)
2. Product Types    (no dependencies)
3. Tags             (no dependencies)
4. Products         (depends on: Companies, ProductTypes)
5. Users            (depends on: Companies - optional)
6. Certificates     (depends on: Companies, Products)
7. ProductIds       (depends on: Certificates)
```

**Violation of this order will cause foreign key errors.**

---

## Common Migration Failures

### Top 5 Issues (by frequency)

1. **Name Mismatches** (40% of failures)
   - "Acme Corp" vs "Acme Corporation" vs "ACME"
   - Solution: Create `NAME_MAPPING_TEMPLATE.csv` entries

2. **Duplicate Keys** (25% of failures)
   - Same email, certificate number, or name
   - Solution: Pre-import deduplication

3. **Missing Reference Data** (20% of failures)
   - Product references non-existent company
   - Solution: Follow import order

4. **Format Errors** (10% of failures)
   - Invalid email, date, URL formats
   - Solution: Use validation rules reference

5. **Encoding Issues** (5% of failures)
   - UTF-8 vs Latin-1, special characters corrupted
   - Solution: Save all files as UTF-8

---

## Validation Quick Reference

### Email Pattern
```regex
^[^@\s]+@[^@\s]+\.[^@\s]+$
```

### URL Pattern
```regex
^https?://.*
```

### Date Pattern (ISO 8601)
```regex
^\d{4}-\d{2}-\d{2}$
```

### Valid Roles
- Admin
- Analyst
- External

### Valid Tag Types
- internal
- external

### Valid Status Values
- Active
- Inactive
- Draft (Products only)

---

## Testing Your Import

Use the test files in `../ImportTemplates/` to verify your import works:

```
TestData/
├── ImportTemplates/           # CSV test files for import testing
│   ├── README.md              # Test file documentation
│   ├── company_positive.csv   # Valid company records
│   ├── company_negative.csv   # Invalid company records
│   ├── user_positive.csv      # Valid user records
│   └── ... (18 test files total)
└── DataMigration/             # This folder - migration documentation
    ├── README.md              # This file
    ├── DATA_MIGRATION_RISK_ASSESSMENT.md
    ├── DATA_QUALITY_CHECKLIST.md
    ├── DATA_VALIDATION_RULES.md
    ├── PRE_MIGRATION_AUDIT_TEMPLATE.md
    └── NAME_MAPPING_TEMPLATE.csv
```

---

## Support Resources

- **Validation Rules**: See `DATA_VALIDATION_RULES.md` for complete field specifications
- **Risk Catalog**: See `DATA_MIGRATION_RISK_ASSESSMENT.md` for risk IDs and mitigations
- **Quality Scoring**: See `DATA_QUALITY_CHECKLIST.md` for scoring methodology

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-12-10 | Initial documentation set |

---

*Last Updated: December 10, 2025*
