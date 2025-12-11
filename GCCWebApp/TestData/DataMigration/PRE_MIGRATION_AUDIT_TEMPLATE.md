# Pre-Migration Data Audit Template

## Project Information

| Field | Value |
|-------|-------|
| Migration Name | |
| Source System | |
| Target System | GCC WebApp |
| Audit Date | |
| Auditor | |
| Data Owner | |

---

## 1. Source Data Inventory

### 1.1 Files Received

| File Name | Size | Rows | Columns | Format | Received Date |
|-----------|------|------|---------|--------|---------------|
| | | | | CSV/Excel | |
| | | | | CSV/Excel | |
| | | | | CSV/Excel | |

### 1.2 Entity Summary

| Entity | File | Expected Count | Actual Count | Variance | Notes |
|--------|------|----------------|--------------|----------|-------|
| Companies | | | | | |
| Users | | | | | |
| Tags | | | | | |
| Products | | | | | |
| Certificates | | | | | |

---

## 2. Data Profiling Results

### 2.1 Company Data Profile

| Metric | Value | Expected | Status |
|--------|-------|----------|--------|
| Total Records | | | |
| Unique Names | | | |
| Duplicate Names | | | |
| With Email | | | |
| Invalid Emails | | | |
| With URL | | | |
| Invalid URLs | | | |
| Max Name Length | | <=255 | |
| Min Name Length | | >=1 | |

**Sample Records (First 5)**:
| Name | Email | URL |
|------|-------|-----|
| | | |
| | | |
| | | |
| | | |
| | | |

**Anomalies Found**:
| Issue | Count | Examples | Resolution |
|-------|-------|----------|------------|
| | | | |

### 2.2 User Data Profile

| Metric | Value | Expected | Status |
|--------|-------|----------|--------|
| Total Records | | | |
| Unique Emails | | | |
| Duplicate Emails | | | |
| Admins | | | |
| Analysts | | | |
| External Users | | | |
| Active | | | |
| Inactive | | | |
| Invalid Roles | | 0 | |
| Invalid Emails | | 0 | |

**Role Distribution**:
| Role | Count | Percentage |
|------|-------|------------|
| Admin | | |
| Analyst | | |
| External | | |
| Invalid | | |

**Anomalies Found**:
| Issue | Count | Examples | Resolution |
|-------|-------|----------|------------|
| | | | |

### 2.3 Tag Data Profile

| Metric | Value | Expected | Status |
|--------|-------|----------|--------|
| Total Records | | | |
| Unique Names | | | |
| Duplicate Names | | | |
| Internal Tags | | | |
| External Tags | | | |
| Invalid Types | | 0 | |
| Max Name Length | | <=50 | |

**Anomalies Found**:
| Issue | Count | Examples | Resolution |
|-------|-------|----------|------------|
| | | | |

### 2.4 Product Data Profile

| Metric | Value | Expected | Status |
|--------|-------|----------|--------|
| Total Records | | | |
| Unique Names | | | |
| With Company | | | |
| Without Company | | | |
| With ProductType | | | |
| Invalid Companies | | 0 | |
| Invalid ProductTypes | | 0 | |
| Max Name Length | | <=100 | |

**Company References (Top 10)**:
| Company Name | Count | Exists in System? |
|--------------|-------|-------------------|
| | | [ ] Yes / [ ] No |
| | | [ ] Yes / [ ] No |
| | | [ ] Yes / [ ] No |

**Anomalies Found**:
| Issue | Count | Examples | Resolution |
|-------|-------|----------|------------|
| | | | |

---

## 3. Reference Data Analysis

### 3.1 Company Name Mapping

| Source Value | Variations Found | Target System Match | Confidence |
|--------------|------------------|---------------------|------------|
| | | | High/Medium/Low |
| | | | High/Medium/Low |

### 3.2 Missing Reference Data

| Entity | Missing Values | Count | Action Required |
|--------|----------------|-------|-----------------|
| Company | | | [ ] Create / [ ] Map / [ ] Skip |
| ProductType | | | [ ] Create / [ ] Map / [ ] Skip |

### 3.3 Reference Data to Import First

| Entity | Count | Dependencies | Import Order |
|--------|-------|--------------|--------------|
| Companies | | None | 1 |
| ProductTypes | | None | 2 |
| Tags | | None | 3 |
| Products | | Companies, ProductTypes | 4 |
| Users | | Companies (optional) | 5 |

---

## 4. Data Quality Scoring

### 4.1 Per-Entity Scores

| Entity | Completeness | Accuracy | Consistency | Validity | Overall |
|--------|--------------|----------|-------------|----------|---------|
| Companies | /100 | /100 | /100 | /100 | /100 |
| Users | /100 | /100 | /100 | /100 | /100 |
| Tags | /100 | /100 | /100 | /100 | /100 |
| Products | /100 | /100 | /100 | /100 | /100 |

### 4.2 Scoring Criteria

**Completeness**: Required fields populated
- 100%: All required fields have values
- 75%: >95% required fields populated
- 50%: 80-95% required fields populated
- 25%: <80% required fields populated

**Accuracy**: Values match expected formats
- 100%: All values correct format
- 75%: >95% correct format
- 50%: 80-95% correct format
- 25%: <80% correct format

**Consistency**: Values follow patterns
- 100%: All values consistent
- 75%: Minor variations (case, spacing)
- 50%: Moderate variations (abbreviations)
- 25%: Major inconsistencies

**Validity**: References exist, enums valid
- 100%: All references valid
- 75%: >95% references valid
- 50%: 80-95% references valid
- 25%: <80% references valid

---

## 5. Risk Register

### 5.1 Identified Risks

| Risk ID | Description | Severity | Likelihood | Impact | Mitigation |
|---------|-------------|----------|------------|--------|------------|
| R-001 | | Critical/High/Medium/Low | High/Medium/Low | | |
| R-002 | | | | | |
| R-003 | | | | | |

### 5.2 Risk Score Matrix

| Risk ID | Severity (1-4) | Likelihood (1-4) | Score | Priority |
|---------|----------------|------------------|-------|----------|
| | | | | |

**Scoring**: Critical=4, High=3, Medium=2, Low=1
**Priority**: Score >= 9: Immediate, 6-8: High, 3-5: Medium, <3: Low

---

## 6. Data Transformation Requirements

### 6.1 Field Mappings

| Source Field | Target Field | Transformation | Notes |
|--------------|--------------|----------------|-------|
| | | Direct/Trim/Case/Parse | |
| | | | |

### 6.2 Value Mappings

| Source Value | Target Value | Entity.Field |
|--------------|--------------|--------------|
| | | |
| | | |

### 6.3 Calculated Fields

| Target Field | Calculation | Dependencies |
|--------------|-------------|--------------|
| | | |

---

## 7. Test Import Results

### 7.1 Sample Import (10% of data)

| Entity | Attempted | Succeeded | Failed | Success Rate |
|--------|-----------|-----------|--------|--------------|
| Companies | | | | % |
| Users | | | | % |
| Tags | | | | % |
| Products | | | | % |

### 7.2 Failure Analysis

| Entity | Error Message | Count | Root Cause | Fix |
|--------|---------------|-------|------------|-----|
| | | | | |

### 7.3 Validation Queries

**Record Count Match**:
```sql
-- Source count
SELECT COUNT(*) FROM [SourceTable];
-- Target count
SELECT COUNT(*) FROM [TargetTable];
-- Variance (should be 0)
```

**Spot Check Results**:
| Record ID | Field | Source Value | Target Value | Match? |
|-----------|-------|--------------|--------------|--------|
| | | | | [ ] Yes / [ ] No |

---

## 8. Effort Estimation

### 8.1 Time Estimates by Task

| Task | Estimated Hours | Actual Hours | Notes |
|------|-----------------|--------------|-------|
| Data Profiling | | | |
| Quality Fixes | | | |
| Reference Data Setup | | | |
| Test Import | | | |
| Fix Issues | | | |
| Full Import | | | |
| Validation | | | |
| User Acceptance | | | |
| **TOTAL** | | | |

### 8.2 Resource Requirements

| Resource | Role | Hours | Availability |
|----------|------|-------|--------------|
| | Data Analyst | | |
| | Technical Lead | | |
| | Business SME | | |
| | QA Tester | | |

---

## 9. Go/No-Go Checklist

### 9.1 Pre-Migration Readiness

| Checkpoint | Status | Owner | Date |
|------------|--------|-------|------|
| Data quality score >= 80% | [ ] Pass / [ ] Fail | | |
| All Critical risks mitigated | [ ] Pass / [ ] Fail | | |
| Reference data created | [ ] Pass / [ ] Fail | | |
| Test import successful | [ ] Pass / [ ] Fail | | |
| Rollback plan documented | [ ] Pass / [ ] Fail | | |
| Migration window scheduled | [ ] Pass / [ ] Fail | | |
| Stakeholder approval | [ ] Pass / [ ] Fail | | |

### 9.2 Decision

| Decision | Justification | Approver | Date |
|----------|---------------|----------|------|
| [ ] GO - Proceed with migration | | | |
| [ ] NO-GO - Issues to resolve | | | |

### 9.3 Conditions (if conditional GO)

| Condition | Owner | Deadline |
|-----------|-------|----------|
| | | |

---

## 10. Approvals

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Data Owner | | | |
| Technical Lead | | | |
| Business Owner | | | |
| Project Manager | | | |

---

## Appendix A: Data Samples

### A.1 Valid Records (3-5 examples)
*Attach or embed sample records that pass all validations*

### A.2 Invalid Records (3-5 examples)
*Attach or embed sample records that fail validations, with error details*

### A.3 Edge Cases
*Document any edge cases discovered during profiling*

---

## Appendix B: Scripts Used

### B.1 Profiling Scripts
```powershell
# Add scripts used for data profiling
```

### B.2 Transformation Scripts
```powershell
# Add scripts used for data transformation
```

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | | | Initial audit |
| | | | |

---

*Template Version: 1.0*
*Last Updated: December 10, 2025*
