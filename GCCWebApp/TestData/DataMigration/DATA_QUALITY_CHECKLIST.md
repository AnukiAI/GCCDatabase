# Data Quality Checklist for GCC WebApp Migration

## Purpose
This checklist ensures source data meets quality standards before import. Complete each section and document findings.

---

## Quick Reference

| Quality Score | Action |
|---------------|--------|
| **90-100%** | Proceed with import |
| **70-89%** | Fix issues, re-validate |
| **50-69%** | Major cleanup required |
| **Below 50%** | Reject, request clean data |

---

## 1. File Structure Validation

### 1.1 Basic File Checks

| Check | Status | Notes |
|-------|--------|-------|
| File opens without errors | [ ] Pass / [ ] Fail | |
| File encoding is UTF-8 | [ ] Pass / [ ] Fail | |
| No BOM (Byte Order Mark) at start | [ ] Pass / [ ] Fail | |
| Consistent line endings (CRLF or LF) | [ ] Pass / [ ] Fail | |
| File size reasonable (<100MB per file) | [ ] Pass / [ ] Fail | |

### 1.2 CSV Structure Checks

| Check | Status | Notes |
|-------|--------|-------|
| Header row present on line 1 | [ ] Pass / [ ] Fail | |
| Consistent delimiter (comma recommended) | [ ] Pass / [ ] Fail | |
| All rows have same column count | [ ] Pass / [ ] Fail | |
| No embedded line breaks in unquoted fields | [ ] Pass / [ ] Fail | |
| Quoted fields properly escaped (`""`) | [ ] Pass / [ ] Fail | |
| No trailing delimiters | [ ] Pass / [ ] Fail | |

### 1.3 Excel Structure Checks (if applicable)

| Check | Status | Notes |
|-------|--------|-------|
| Single worksheet or clearly labeled sheets | [ ] Pass / [ ] Fail | |
| No merged cells | [ ] Pass / [ ] Fail | |
| No hidden rows or columns | [ ] Pass / [ ] Fail | |
| No formulas (values only) | [ ] Pass / [ ] Fail | |
| No conditional formatting affecting values | [ ] Pass / [ ] Fail | |

---

## 2. Column/Header Validation

### 2.1 Required Columns Present

**Companies**
| Column | Required | Present | Notes |
|--------|----------|---------|-------|
| Name | YES | [ ] Yes / [ ] No | |
| Email | No | [ ] Yes / [ ] No | |
| Url | No | [ ] Yes / [ ] No | |

**Users**
| Column | Required | Present | Notes |
|--------|----------|---------|-------|
| Name | YES | [ ] Yes / [ ] No | |
| Email | YES | [ ] Yes / [ ] No | |
| Role | YES | [ ] Yes / [ ] No | Must be: Admin, Analyst, External |
| Status | No | [ ] Yes / [ ] No | Default: Active |
| Company | No | [ ] Yes / [ ] No | Must match existing company |

**Tags**
| Column | Required | Present | Notes |
|--------|----------|---------|-------|
| Name | YES | [ ] Yes / [ ] No | |
| TagType | YES | [ ] Yes / [ ] No | Must be: internal, external |
| Description | No | [ ] Yes / [ ] No | |

**Products**
| Column | Required | Present | Notes |
|--------|----------|---------|-------|
| Name | YES | [ ] Yes / [ ] No | |
| Description | No | [ ] Yes / [ ] No | |
| Company | No | [ ] Yes / [ ] No | Must match existing company |
| ProductType | No | [ ] Yes / [ ] No | Must match existing type |
| Status | No | [ ] Yes / [ ] No | Default: Active |

### 2.2 Header Naming Conventions

| Issue | Count | Examples | Action |
|-------|-------|----------|--------|
| Non-standard capitalization | | | System handles via normalization |
| Spaces in headers | | | System handles via normalization |
| Underscores in headers | | | System handles via normalization |
| Unknown/unmapped headers | | | Will be skipped - document |

---

## 3. Data Content Validation

### 3.1 Required Field Completeness

| Entity | Field | Total Rows | Non-Empty | Empty | Completeness % |
|--------|-------|------------|-----------|-------|----------------|
| Company | Name | | | | |
| User | Name | | | | |
| User | Email | | | | |
| User | Role | | | | |
| Tag | Name | | | | |
| Tag | TagType | | | | |
| Product | Name | | | | |

**Target: 100% completeness for required fields**

### 3.2 Field Length Analysis

| Entity | Field | Max Length | Min Found | Max Found | Exceeds? |
|--------|-------|------------|-----------|-----------|----------|
| Company | Name | 255 | | | [ ] Yes / [ ] No |
| Company | Email | 255 | | | [ ] Yes / [ ] No |
| Company | Url | 500 | | | [ ] Yes / [ ] No |
| User | Name | 100 | | | [ ] Yes / [ ] No |
| User | Email | 255 | | | [ ] Yes / [ ] No |
| Tag | Name | 50 | | | [ ] Yes / [ ] No |
| Tag | Description | 500 | | | [ ] Yes / [ ] No |
| Product | Name | 100 | | | [ ] Yes / [ ] No |
| Product | Description | 500 | | | [ ] Yes / [ ] No |

### 3.3 Format Validation

**Email Fields**
| Check | Count | Examples | Valid Pattern |
|-------|-------|----------|---------------|
| Valid emails | | | `^[^@\s]+@[^@\s]+\.[^@\s]+$` |
| Invalid emails | | | Document for review |
| Empty (optional) | | | OK if optional |

**URL Fields**
| Check | Count | Examples |
|-------|-------|----------|
| Has http:// or https:// | | |
| Missing protocol | | |
| Invalid URL format | | |
| Empty (optional) | | |

**Enum Fields**
| Entity | Field | Valid Values | Invalid Count | Invalid Examples |
|--------|-------|--------------|---------------|------------------|
| User | Role | Admin, Analyst, External | | |
| User | Status | Active, Inactive | | |
| Tag | TagType | internal, external | | |
| Product | Status | Active, Inactive, Draft | | |

---

## 4. Referential Integrity

### 4.1 Foreign Key References

**Products → Companies**
| Source Company Value | Exists in System? | Action Needed |
|---------------------|-------------------|---------------|
| | [ ] Yes / [ ] No | |
| | [ ] Yes / [ ] No | |
| | [ ] Yes / [ ] No | |

**Products → Product Types**
| Source ProductType Value | Exists in System? | Action Needed |
|-------------------------|-------------------|---------------|
| | [ ] Yes / [ ] No | |
| | [ ] Yes / [ ] No | |

**Users → Companies**
| Source Company Value | Exists in System? | Action Needed |
|---------------------|-------------------|---------------|
| | [ ] Yes / [ ] No | |
| | [ ] Yes / [ ] No | |

### 4.2 Reference Data Import Order

| Step | Entity | Dependencies | Status |
|------|--------|--------------|--------|
| 1 | Companies | None | [ ] Complete |
| 2 | Product Types | None | [ ] Complete |
| 3 | Tags | None | [ ] Complete |
| 4 | Products | Companies, Product Types | [ ] Complete |
| 5 | Users | Companies (optional) | [ ] Complete |
| 6 | Certificates | Companies, Products | [ ] Complete |

---

## 5. Uniqueness & Duplicates

### 5.1 Duplicate Detection

| Entity | Unique Field(s) | Total | Unique | Duplicates | Duplicate % |
|--------|-----------------|-------|--------|------------|-------------|
| Company | Name | | | | |
| User | Email | | | | |
| Tag | Name | | | | |
| Product | Name + Company | | | | |
| Certificate | CertificateNumber | | | | |

### 5.2 Duplicate Records Found

| Record | Field | Duplicate Value | Count | Resolution |
|--------|-------|-----------------|-------|------------|
| | | | | [ ] Merge / [ ] Keep First / [ ] Manual |
| | | | | [ ] Merge / [ ] Keep First / [ ] Manual |

---

## 6. Data Consistency

### 6.1 Cross-Field Consistency

| Check | Status | Issues Found |
|-------|--------|--------------|
| Date ranges valid (start < end) | [ ] Pass / [ ] Fail | |
| Status matches related data | [ ] Pass / [ ] Fail | |
| Parent-child relationships consistent | [ ] Pass / [ ] Fail | |

### 6.2 Value Consistency

| Field | Variation Examples | Standardized Value |
|-------|-------------------|-------------------|
| Company: "Amazon" | "Amazon", "amazon", "AMAZON", "Amazon.com" | Amazon |
| Role: "Admin" | "Admin", "Administrator", "admin" | Admin |

---

## 7. Security Screening

### 7.1 Injection Attempts

| Pattern | Found | Count | Action |
|---------|-------|-------|--------|
| SQL: `'; DROP` or `--` | [ ] Yes / [ ] No | | Quarantine |
| XSS: `<script>` | [ ] Yes / [ ] No | | Sanitize |
| Path: `../` | [ ] Yes / [ ] No | | Sanitize |

### 7.2 Sensitive Data

| Check | Found | Location | Action |
|-------|-------|----------|--------|
| Passwords in clear text | [ ] Yes / [ ] No | | Remove |
| SSN/TIN patterns | [ ] Yes / [ ] No | | Review |
| Credit card patterns | [ ] Yes / [ ] No | | Remove |

---

## 8. Summary & Sign-Off

### Quality Score Calculation

| Category | Weight | Score (0-100) | Weighted |
|----------|--------|---------------|----------|
| File Structure | 10% | | |
| Column Validation | 15% | | |
| Required Fields | 25% | | |
| Format Validation | 20% | | |
| Referential Integrity | 15% | | |
| Uniqueness | 10% | | |
| Security | 5% | | |
| **TOTAL** | **100%** | | **___%** |

### Decision

| Quality Score | Recommendation | Approver |
|---------------|----------------|----------|
| [ ] 90-100% | Proceed with import | |
| [ ] 70-89% | Fix issues first | |
| [ ] 50-69% | Major cleanup | |
| [ ] <50% | Reject data | |

### Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Data Analyst | | | |
| Technical Lead | | | |
| Business Owner | | | |

---

## Appendix A: Quick Validation Commands

### PowerShell - Row Count
```powershell
(Get-Content file.csv | Measure-Object -Line).Lines - 1
```

### PowerShell - Column Count per Row
```powershell
Import-Csv file.csv | ForEach-Object { $_.PSObject.Properties.Count } | Group-Object | Sort-Object Count -Descending
```

### PowerShell - Find Duplicates
```powershell
Import-Csv file.csv | Group-Object Name | Where-Object { $_.Count -gt 1 }
```

### PowerShell - Check Max Field Length
```powershell
Import-Csv file.csv | ForEach-Object { $_.Name.Length } | Measure-Object -Maximum
```

---

*Last Updated: December 10, 2025*
