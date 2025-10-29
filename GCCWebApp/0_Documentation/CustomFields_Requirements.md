# CustomFields Feature - Requirements Specification

**Project**: GCC Database Solution
**Feature**: Dynamic Custom Fields for Certificates
**Version**: 1.0
**Date**: October 28, 2025
**Status**: Planning Phase

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Business Context](#business-context)
3. [Current State Analysis](#current-state-analysis)
4. [User Stories](#user-stories)
5. [Functional Requirements](#functional-requirements)
6. [Non-Functional Requirements](#non-functional-requirements)
7. [Success Criteria](#success-criteria)
8. [Appendix](#appendix)

---

## Executive Summary

### Overview
The CustomFields feature enables administrators to define and manage custom form fields without code changes, making certificate forms fully configurable. This addresses the need for flexible, certification-type-specific data collection while maintaining data integrity and user experience.

### Business Value
- **Operational Efficiency**: Add/modify fields in minutes vs. days of development
- **Regulatory Compliance**: Quickly adapt to new certification standards (ISO, EPA, etc.)
- **Type-Specific Forms**: Different certification types (CEF, CSF, RC, ES, MIR) have unique requirements
- **Future-Proof**: Easy adaptation without code deployments

### Current State (50% Complete)
**Completed:**
- Database schema (CustomFields, CustomFieldValues tables)
- Admin interface at `/Admin/CertificateFields`
- 30 pre-seeded fields across 8 categories
- Backend data loading (EF Core includes)

**Missing:**
- Dynamic field rendering in Create/Edit forms
- Custom field display in Details/Single views
- Custom field inclusion in PDF/CSV exports
- Search/filter by custom field values

### Implementation Timeline
- **Phase 1** (Week 1-2): Create/Edit Forms - 40-50 hours
- **Phase 2** (Week 3): Details Display - 16-24 hours
- **Phase 3** (Week 4): Exports - 16-24 hours
- **Phase 4** (Week 5-6): Enhancements - 24-32 hours

**Total Effort**: 96-130 hours over 5-6 weeks

---

## Business Context

### Problem Statement
Certificate forms are currently hardcoded in Razor views. Adding new fields or modifying validation requires:
1. Developer modifying view/controller code
2. Database schema changes
3. Testing and deployment
4. Potential downtime

**Impact**: 3-5 day turnaround for simple field additions, risk of errors, inflexibility.

### Solution
Dynamic custom fields system where:
- Admins define fields through UI
- Forms render automatically based on database configuration
- No code changes required for field additions
- Type-specific fields per certification type

### Stakeholders
| Role | Need | Benefit |
|------|------|---------|
| **Admins** | Manage field definitions | Control over form structure |
| **Analysts** | Enter certificate data | Clear, validated forms |
| **External Partners** | View certificate data | Consistent data presentation |
| **Developers** | Reduce maintenance burden | Focus on features, not form changes |
| **Compliance Team** | Adapt to regulations | Quick updates for new standards |

---

## Current State Analysis

### Existing Infrastructure

#### Database Schema
```sql
-- CustomField Table (Field Definitions)
CREATE TABLE CustomFields (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    FieldType NVARCHAR(50) NOT NULL,  -- text, number, date, boolean, select, multiselect, textarea
    IsRequired BIT NOT NULL DEFAULT 0,
    IsVisible BIT NOT NULL DEFAULT 1,
    IsPublic BIT NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0,
    Options NVARCHAR(MAX),           -- JSON for select options
    DefaultValue NVARCHAR(MAX),
    ValidationRules NVARCHAR(MAX),   -- JSON validation rules
    CertificateTypeId INT NOT NULL,
    FOREIGN KEY (CertificateTypeId) REFERENCES CertificationTypes(Id)
);

-- CustomFieldValue Table (Actual Values)
CREATE TABLE CustomFieldValues (
    Id INT PRIMARY KEY IDENTITY,
    CertificateId INT NOT NULL,
    FieldId INT NOT NULL,
    TextValue NVARCHAR(MAX),
    NumberValue DECIMAL(18,4),
    DateValue DATETIME2,
    BooleanValue BIT,
    FOREIGN KEY (CertificateId) REFERENCES Certificates(Id),
    FOREIGN KEY (FieldId) REFERENCES CustomFields(Id)
);
```

#### Seeded Fields (30 Total)
**Categories:**
1. Basic Certificate Information (4 fields)
   - Certificate Holder Company Name (text, required)
   - Certificate Holder Contact Email (email, required)
   - Certificate Holder Phone Number (phone, optional)
   - Certificate Holder Address (textarea, optional)

2. Product Information (6 fields)
   - Product Brand Name, Model Number, SKU, Dimensions, Weight, Color Options

3. Manufacturing Information (4 fields)
   - Facility Name, Location, Country (select), Manufacturing Date

4. Environmental and Sustainability (6 fields)
   - Impact Statement, Third-Party Certifications (multiselect), Carbon Footprint, Water Usage, Energy Consumption, Recycled Content %

5. Testing and Compliance (4 fields)
   - Test Report Number, Laboratory Name, Test Date, Compliance Standards (multiselect)

6. Additional Information (6 fields)
   - Special Notes, Internal Reference, Priority Level (select), Certificate Visibility (select), Renewal Reminder Date, Assigned Analyst

#### Admin Interface
- **URL**: `/Admin/CertificateFields`
- **Features**:
  - View all fields (7 initial display, "Show More" for rest)
  - Add new field (inline form)
  - Edit field (double-click row)
  - Delete field
  - Search/filter fields
  - Export to CSV
- **Status**: ✅ Fully functional

#### Backend Data Loading
**CertificateService.cs** (line 586-587):
```csharp
.Include(c => c.CustomFieldValues)
    .ThenInclude(cfv => cfv.Field)
```
Data is loaded but not used in UI.

### Gap Analysis

| Component | Current State | Required State | Gap |
|-----------|---------------|----------------|-----|
| **Create Form** | Hardcoded fields | Dynamic rendering | Dynamic field loading, type-specific controls |
| **Edit Form** | Hardcoded fields | Dynamic rendering | Same as Create + value population |
| **Details View** | Static display | Custom field section | Read-only field display, grouping |
| **Tile Views** | Fixed layout | Optional custom fields | Featured field display logic |
| **PDF Export** | Fixed template | Custom fields section | QuestPDF integration |
| **CSV Export** | Fixed columns | Dynamic columns | Column generation from field definitions |
| **Search/Filter** | Fixed filters | Custom field filters | Dynamic filter UI, query building |
| **Validation** | Server-only | Client + Server | JavaScript validation, FluentValidation |

---

## User Stories

### US-CF-001: Admin Manages Field Definitions
**As an** Admin
**I want to** create, edit, and delete custom field definitions
**So that** certificate forms can be customized without developer intervention

**Acceptance Criteria:**
- ✅ Can view all custom fields with search/filter
- ✅ Can add new field with name, type, validation rules, sort order
- ✅ Can edit existing field properties (except type if data exists)
- ✅ Can delete fields (soft delete with 30-day recovery)
- ✅ Can set field visibility (public/internal only)
- ✅ Can associate fields with certification types
- ✅ Can export field definitions to CSV

**Status**: ✅ COMPLETE (Admin interface exists)

---

### US-CF-002: Analyst Creates Certificate with Custom Fields
**As an** Analyst
**I want to** see and fill out custom fields when creating certificates
**So that** I can capture all required certification data

**Acceptance Criteria:**
- [ ] Custom fields appear in Create form based on selected certification type
- [ ] Fields render correctly by type (text, select, date, etc.)
- [ ] Required fields show asterisk and prevent submission if empty
- [ ] Validation rules apply (min/max length, numeric ranges, date formats)
- [ ] Field help text/tooltips display
- [ ] Fields appear in configured sort order
- [ ] Multiselect fields allow multiple selections
- [ ] Select fields show configured options
- [ ] Date fields use date picker
- [ ] Successful save stores values in CustomFieldValues table
- [ ] Form shows validation errors clearly
- [ ] Fields update dynamically when certification type changes

**Priority**: ⚠️ HIGH
**Estimated Effort**: 24-32 hours
**Dependencies**: None
**Phase**: 1

---

### US-CF-003: Analyst Edits Certificate with Custom Fields
**As an** Analyst
**I want to** edit custom field values on existing certificates
**So that** I can correct errors or update information

**Acceptance Criteria:**
- [ ] Edit form loads existing custom field values
- [ ] Previously saved values populate correctly
- [ ] Fields that were added after certificate creation show as empty
- [ ] Fields that were deleted still show existing values (read-only)
- [ ] Can update values and save successfully
- [ ] Validation applies same as Create form
- [ ] Fields appear in same order as Create form
- [ ] Changes are logged in audit trail

**Priority**: ⚠️ HIGH
**Estimated Effort**: 16-24 hours
**Dependencies**: US-CF-002
**Phase**: 1

---

### US-CF-004: User Views Custom Fields in Certificate Details
**As a** User (any role)
**I want to** see custom field values in certificate detail view
**So that** I can review all certification information

**Acceptance Criteria:**
- [ ] Custom fields display in dedicated section on details page
- [ ] Fields grouped by category (Basic Info, Product, Manufacturing, etc.)
- [ ] Only public fields visible to external partners
- [ ] All fields visible to internal users (Analyst/Admin)
- [ ] Empty fields show "Not Provided" instead of blank
- [ ] Values formatted appropriately (dates, numbers, booleans)
- [ ] Multiselect values display as comma-separated list
- [ ] Section collapsible on mobile
- [ ] Print-friendly layout

**Priority**: 🔶 MEDIUM
**Estimated Effort**: 12-16 hours
**Dependencies**: US-CF-002, US-CF-003
**Phase**: 2

---

### US-CF-005: User Sees Custom Fields in Tile Views
**As a** User viewing certificate tiles
**I want to** see key custom field values
**So that** I can make quick decisions without opening details

**Acceptance Criteria:**
- [ ] Admins can mark up to 3 fields as "featured"
- [ ] Featured fields appear in tile view below standard fields
- [ ] Values truncated to fit tile layout
- [ ] Responsive design maintained
- [ ] Performance impact < 100ms per 20 tiles
- [ ] Only public fields shown in public tile view
- [ ] Internal tile view shows all featured fields

**Priority**: 🔵 LOW
**Estimated Effort**: 8-12 hours
**Dependencies**: US-CF-004
**Phase**: 4

---

### US-CF-006: User Exports Certificates with Custom Fields
**As a** User
**I want to** export certificates to PDF/CSV including custom fields
**So that** I can share complete certification data

**Acceptance Criteria:**

**PDF Export:**
- [ ] Custom fields section appears in PDF after standard fields
- [ ] Grouped by category with headers
- [ ] Values formatted and aligned
- [ ] Page breaks handled gracefully
- [ ] Only public fields in public PDF exports
- [ ] Export includes field labels and values

**CSV Export:**
- [ ] Custom fields appear as additional columns
- [ ] Column headers use field names
- [ ] Empty values exported as empty cells
- [ ] Multiselect values exported as semicolon-separated
- [ ] Date values formatted as ISO 8601
- [ ] Bulk export performance < 30s for 1000 certificates
- [ ] Column order follows field sort order

**Priority**: 🔶 MEDIUM
**Estimated Effort**: 16-24 hours
**Dependencies**: US-CF-002, US-CF-003, US-CF-004
**Phase**: 3

---

### US-CF-007: System Handles Field Definition Changes
**As a** Developer
**I want to** handle field definition changes gracefully
**So that** existing certificate data remains accessible

**Acceptance Criteria:**
- [ ] Adding new field: Shows empty for existing certificates
- [ ] Deleting field: Soft delete, values retained 30 days
- [ ] Changing field type: Validates existing data compatibility
- [ ] Changing required status: Doesn't break existing certificates
- [ ] Changing visibility: Immediately affects display
- [ ] Reordering fields: Doesn't affect saved data
- [ ] Admin warned before incompatible changes
- [ ] Data migration tool available if needed

**Priority**: 🔵 LOW
**Estimated Effort**: 12-16 hours
**Dependencies**: US-CF-002, US-CF-003
**Phase**: 4

---

### US-CF-008: User Searches/Filters by Custom Fields
**As a** User
**I want to** search and filter certificates by custom field values
**So that** I can find specific certificates quickly

**Acceptance Criteria:**
- [ ] Advanced search includes custom field filters
- [ ] Can filter by text fields (contains, equals)
- [ ] Can filter by date ranges
- [ ] Can filter by select field options
- [ ] Can filter by numeric ranges
- [ ] Multiple custom field filters combine with AND logic
- [ ] Search results update dynamically
- [ ] Filter state saved in URL (shareable)
- [ ] Performance < 2s for 10,000 certificates

**Priority**: 🔵 LOW
**Estimated Effort**: 20-28 hours
**Dependencies**: US-CF-002, US-CF-003
**Phase**: 4

---

## Functional Requirements

### FR-001: Dynamic Field Rendering
**Requirement**: Certificate Create/Edit forms must render custom fields dynamically based on selected certification type.

**Specifications:**
1. **Field Loading**
   - Load custom fields via AJAX when certification type selected
   - Endpoint: `GET /api/CustomFields/ByCertificationType/{certTypeId}`
   - Response: JSON array of field definitions
   - Cache field definitions client-side

2. **Field Type Rendering**

   | Field Type | HTML Control | Attributes | Validation |
   |------------|--------------|------------|------------|
   | text | `<input type="text">` | maxlength, placeholder | Required, MaxLength |
   | email | `<input type="email">` | maxlength | Required, Email format |
   | phone | `<input type="tel">` | pattern | Phone format |
   | number | `<input type="number">` | min, max, step | Numeric, Range |
   | date | `<input type="date">` | min, max | Date format, Range |
   | textarea | `<textarea>` | maxlength, rows | MaxLength |
   | select | `<select>` | options from DB | Required if set |
   | multiselect | `<select multiple>` | options from DB | MinItems, MaxItems |
   | boolean | `<input type="checkbox">` | checked | N/A |

3. **Field Grouping**
   - Group fields by section (inferred from field name prefix or explicit section property)
   - Render sections with collapsible headers
   - Default sections: Basic Info, Product, Manufacturing, Environmental, Testing, Additional

4. **Sort Order**
   - Render fields in ascending SortOrder
   - Within same SortOrder, sort alphabetically by name

5. **Visibility Rules**
   - IsVisible=false: Don't render
   - IsPublic=false: Only show to Analyst/Admin roles

### FR-002: Polymorphic Value Storage
**Requirement**: Custom field values must be stored in appropriate data type columns.

**Storage Logic:**
```
- text, email, phone, textarea, select, multiselect → TextValue
- number → NumberValue
- date → DateValue
- boolean → BooleanValue
```

**Save Process:**
1. Parse form data for custom fields (name pattern: `customField_{fieldId}`)
2. Lookup field definition to determine FieldType
3. Store in correct column based on type
4. Delete existing CustomFieldValue for that Certificate+Field combination
5. Insert new CustomFieldValue record
6. Validate data type compatibility

### FR-003: Validation
**Requirement**: Custom fields must support both client-side and server-side validation.

**Validation Rules Format** (JSON in ValidationRules column):
```json
{
  "required": true,
  "minLength": 5,
  "maxLength": 100,
  "min": 0,
  "max": 999999,
  "pattern": "^[A-Z]{2}-\\d{4}$",
  "email": true,
  "phone": true,
  "dateRange": {
    "min": "2020-01-01",
    "max": "2030-12-31"
  }
}
```

**Client-Side (JavaScript):**
- Apply HTML5 validation attributes (required, pattern, min, max, maxlength)
- Custom validation for complex rules (phone, email patterns)
- Real-time validation on blur
- Display inline error messages

**Server-Side (C#):**
- FluentValidation rules generated from ValidationRules JSON
- Validate before saving CustomFieldValues
- Return ModelState errors to form
- Prevent XSS/SQL injection

### FR-004: Field Configuration Import/Export
**Requirement**: Admins can export field configurations for backup/transfer.

**Export Format (CSV):**
```
Id,Name,FieldType,IsRequired,IsVisible,IsPublic,SortOrder,Options,DefaultValue,ValidationRules,CertificateTypeId
1,"Certificate Holder Company Name",text,true,true,true,1,"","","maxLength:255",1
```

**Import Validation:**
- Check for duplicate field names per certification type
- Validate FieldType against allowed values
- Parse and validate JSON in Options/ValidationRules
- Validate CertificateTypeId exists
- Preview changes before applying

---

## Non-Functional Requirements

### NFR-001: Performance
**Requirement**: Custom fields must not significantly impact page load times.

**Metrics:**
- Page load with 30 custom fields: < 1 second
- Field rendering: < 200ms
- Form submission with custom fields: < 2 seconds
- Bulk export (1000 certificates): < 30 seconds

**Implementation:**
- Database indexes on CustomFieldValues.CertificateId, FieldId
- Cache field definitions per certification type (5 min TTL)
- Lazy load custom fields (don't load until certification type selected)
- Use partial view for field rendering (reusable component)

### NFR-002: Accessibility (WCAG 2.1 Level AA)
**Requirements:**
- All custom field inputs have associated `<label>` elements
- Required fields indicated with asterisk + `aria-required="true"`
- Validation errors have `role="alert"` and `aria-describedby`
- Select/multiselect fields have clear option labels
- Date pickers keyboard navigable
- Color contrast ratio ≥ 4.5:1
- Focus indicators visible
- Screen reader announces field type and requirements

### NFR-003: Security
**Requirements:**
- Custom field values sanitized to prevent XSS
- SQL injection prevented via parameterized queries (EF Core)
- Validation rules JSON parsed safely (no code execution)
- File upload fields (future) validated for type/size
- Role-based visibility enforced server-side
- CSRF protection on save operations (AntiForgeryToken)

### NFR-004: Maintainability
**Requirements:**
- Custom field rendering uses shared partial view
- Field type handlers in separate methods
- Validation logic reusable
- Database migrations for schema changes
- Code documentation for public methods
- Unit tests for CustomFieldService methods
- Integration tests for controller actions

### NFR-005: Scalability
**Requirements:**
- Support up to 100 custom fields per certification type
- Handle 10,000+ certificates with custom field values
- Database query optimization (avoid N+1)
- Consider field grouping/pagination if > 50 fields

### NFR-006: Usability
**Requirements:**
- Field labels clear and concise
- Help text available via tooltip/info icon
- Logical field grouping
- Responsive design (mobile, tablet, desktop)
- Inline validation (immediate feedback)
- Success/error messages clear and actionable

---

## Success Criteria

### Phase 1 Success (Create/Edit Forms)
- [ ] Analyst can create certificate with all 30 custom fields
- [ ] All field types render correctly
- [ ] Validation prevents invalid submissions
- [ ] Values save to CustomFieldValues table
- [ ] Edit form loads and updates existing values
- [ ] No performance degradation (< 1s page load)
- [ ] No accessibility violations (aXe scan)
- [ ] Zero critical bugs in 1 week of testing

### Phase 2 Success (Details Display)
- [ ] Custom fields visible in CertificateSingleView
- [ ] Grouped by category with headers
- [ ] Public/internal visibility rules enforced
- [ ] Values formatted correctly
- [ ] Responsive on mobile devices
- [ ] Print layout acceptable

### Phase 3 Success (Exports)
- [ ] PDF includes custom fields section
- [ ] CSV has dynamic custom field columns
- [ ] Export 1000 certificates in < 30 seconds
- [ ] Formatting consistent with mockups
- [ ] No data loss or corruption

### Phase 4 Success (Enhancements)
- [ ] Search/filter by at least 3 custom field types
- [ ] Featured fields appear in tile views
- [ ] Admin can manage field definitions easily
- [ ] Field changes handled gracefully
- [ ] Usage analytics tracked

### Overall Success Metrics
- **Time to Add Field**: From days to < 5 minutes
- **User Satisfaction**: ≥ 80% positive feedback
- **Bug Rate**: < 2 critical bugs/month after launch
- **Performance**: Page loads < 1s, exports < 30s for 1000 certs
- **Adoption**: ≥ 90% of analysts use custom fields within 1 month

---

## Appendix

### A. Field Type Reference

| Type | Use Case | Example | Storage Column |
|------|----------|---------|----------------|
| text | Short text | Company name | TextValue |
| email | Email address | contact@example.com | TextValue |
| phone | Phone number | +1-555-0123 | TextValue |
| number | Numeric value | 42.5 | NumberValue |
| date | Date picker | 2025-01-15 | DateValue |
| textarea | Long text | Product description | TextValue |
| select | Single choice | Priority: High/Medium/Low | TextValue |
| multiselect | Multiple choices | Certifications: LEED, Energy Star | TextValue (comma-separated) |
| boolean | Yes/No checkbox | Is renewable? | BooleanValue |

### B. Database Indexes
```sql
-- Performance optimization indexes
CREATE INDEX IX_CustomFieldValues_CertificateId ON CustomFieldValues(CertificateId);
CREATE INDEX IX_CustomFieldValues_FieldId ON CustomFieldValues(FieldId);
CREATE INDEX IX_CustomFields_CertificateTypeId ON CustomFields(CertificateTypeId);
CREATE INDEX IX_CustomFields_SortOrder ON CustomFields(CertificateTypeId, SortOrder);
```

### C. Related Documents
- **Master Design Guide**: `0_Documentation/mockups/0_0_MasterDesignGuide.html`
- **Database Schema**: Entity Framework Core migrations
- **API Documentation**: Swagger at http://localhost:5101/swagger
- **User Manual**: TBD (create after implementation)

### D. Change Log

| Date | Version | Author | Changes |
|------|---------|--------|---------|
| 2025-10-28 | 1.0 | Claude Code | Initial requirements document |

### E. Glossary
- **CustomField**: Field definition/template stored in database
- **CustomFieldValue**: Actual value for a specific certificate+field combination
- **Polymorphic Storage**: Using multiple columns (TextValue, NumberValue, etc.) to store different data types
- **Soft Delete**: Marking record as deleted without removing from database
- **Certification Type**: Type of certification (CEF, CSF, RC, ES, MIR)
- **Field Type**: Control type (text, select, date, etc.)
- **Validation Rules**: JSON-formatted constraints for field values

---

**Document Status**: APPROVED FOR IMPLEMENTATION
**Next Steps**: Proceed to Technical Design Specification
