# Requirements Example: GCC Database Solution

**Purpose:** Example of a completed requirements questionnaire for reference. Shows what "good" requirements capture looks like.

---

## Document Control

| Field | Value |
|-------|-------|
| Project Name | GCC Database Solution |
| Client Name | Green Circle Certified |
| Date Completed | [Example] |
| Version | 1.0 |

---

## Section 1: Project Overview

### 1.1 Business Context

| # | Question | Answer |
|---|----------|--------|
| 1.1.1 | What problem does this solve? | Manage certification lifecycle for eco-friendly products. Currently using spreadsheets and manual processes. |
| 1.1.2 | Who requested this and why now? | Operations team. Growing certificate volume makes manual tracking unsustainable. |
| 1.1.3 | What happens if not completed? | Missed renewals, compliance issues, lost business with partners like Amazon. |
| 1.1.4 | Existing systems? | Excel spreadsheets, shared drives for PDF certificates |
| 1.1.5 | What does success look like? | All certificates tracked digitally. Partners can verify certificates via web. Analysts save 10+ hours/week. |

### 1.2 Constraints

| # | Question | Answer |
|---|----------|--------|
| 1.2.1 | Hard deadline? | Partner (Amazon) integration needed by Q2 |
| 1.2.2 | Budget range? | Mid-range |
| 1.2.3 | Regulatory requirements? | No (certifications are voluntary eco-standards) |
| 1.2.4 | Technology constraints? | Must run on Windows Server (existing hosting) |
| 1.2.5 | Branding guidelines? | Yes - Green Circle brand colors (green #92C83E, navy #002B76) |

---

## Section 2: Data Model

### 2.1 Core Entities

| Entity | Description |
|--------|-------------|
| Certificate | Core entity - the certification document |
| Company | Company that owns the certified product |
| Product | The product being certified |
| User | System users (admins, analysts, external partners) |
| CertificateProductId | Product identifiers (ASIN, UPC, etc.) |
| Partner | External partners (Amazon, Novi) |
| Tag | Labels for organizing certificates |
| CertificationType | Types of certifications offered |
| CertificateField | Custom fields per certificate type |

### 2.2 Entity: Certificate

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| CertificateNumber | String | Yes | Unique, format: YY-NNNN |
| CompanyId | FK | Yes | Link to Company |
| ProductId | FK | Yes | Link to Product |
| CertificationTypeId | FK | Yes | Link to CertificationType |
| Status | Enum | Yes | 14 possible states |
| IssueDate | Date | Yes | |
| ExpirationDate | Date | Yes | |
| Partner | Enum | Yes | Amazon, Novi, None |
| CertificationResults | String | No | Optional test results |
| PDFPath | String | No | Path to certificate PDF |
| CreatedBy | FK | Yes | Link to User |
| CreatedDate | DateTime | Yes | Auto-set |
| ModifiedDate | DateTime | Yes | Auto-update |

### 2.3 Key Relationships

```
Certificate (N) ──────── (1) Company
Certificate (N) ──────── (1) Product
Certificate (1) ──────── (N) CertificateProductId  [100s of ASINs per cert]
Certificate (N) ──────── (N) Tag                   [Many-to-many]
User (N) ──────── (N) Company                      [External users see their companies]
User (N) ──────── (N) Partner                      [External users linked to partners]
User (N) ──────── (N) Role                         [Many-to-many via UserRoles]
```

---

## Section 3: Business Rules

### 3.1 Certificate Number Generation

- Format: `YY-NNNN` (e.g., 24-0001)
- Auto-generated on creation
- Unique across all certificates
- Year resets sequence (first cert of 2025 = 25-0001)

### 3.2 Workflow States (14 States)

```
┌─────────────┐
│    Draft    │ ← Created by Analyst
└──────┬──────┘
       │ Submit
       ▼
┌─────────────┐
│  Submitted  │
└──────┬──────┘
       │ Review
       ▼
┌─────────────┐     ┌─────────────┐
│ In Review   │────→│  Rejected   │
└──────┬──────┘     └─────────────┘
       │ Approve           │ Revise
       ▼                   ▼
┌─────────────┐     ┌─────────────┐
│  Approved   │     │   Revised   │ → Back to Submitted
└──────┬──────┘     └─────────────┘
       │ Issue
       ▼
┌─────────────┐
│   Active    │ ← Public can see
└──────┬──────┘
       │ Expiration approaching (30 days)
       ▼
┌─────────────┐
│  Expiring   │
└──────┬──────┘
       │ Renew OR Expire
       ▼
┌─────────────┐     ┌─────────────┐
│   Renewed   │     │   Expired   │
└─────────────┘     └─────────────┘

Additional states: Suspended, Revoked, Archived, Cancelled, Pending Payment
```

### 3.3 Transition Rules

| From | To | Who Can | Conditions |
|------|----|---------|------------|
| Draft | Submitted | Analyst, Admin | All required fields filled |
| Submitted | In Review | Analyst, Admin | |
| In Review | Approved | Analyst, Admin | |
| In Review | Rejected | Analyst, Admin | Must provide reason |
| Approved | Active | Admin | PDF must be uploaded |
| Active | Expired | System | Auto on expiration date |

### 3.4 Visibility Rules

| Role | Can See |
|------|---------|
| Public | Only Active certificates |
| External | Active certs for their Company OR Partner |
| Analyst | All certificates |
| Admin | All certificates |

---

## Section 4: Users & Authentication

### 4.1 User Roles

| Role | Permissions |
|------|-------------|
| Admin | Full CRUD on everything, user management, system config |
| Analyst | CRUD certificates, view all data, no admin functions |
| External | View/download certificates for their company/partner only |
| Public | View Active certificates only (no login required) |

### 4.2 Authentication

| Question | Answer |
|----------|--------|
| Login identifier | Username (not email) |
| Password requirements | 8+ chars, upper, lower, number, special |
| 2FA | Yes - Email-based, optional per user |
| SSO | Future - Azure AD B2C (not MVP) |
| Session timeout | 30 minutes |
| Password reset | Self-service via email |

### 4.3 Data Visibility (External Role)

```
External User sees certificates where:
  - Certificate.Company IN User.Companies
  OR
  - Certificate.Partner IN User.Partners

External User sees products where:
  - Product.Company IN User.Companies
  OR
  - Product has certificate with Partner IN User.Partners
```

---

## Section 5: Views

### 5.1 View Inventory

| View | Access | Priority |
|------|--------|----------|
| Certificate Tile View (Public) | Public | MVP |
| Certificate Tile View (Internal) | Analyst, Admin | MVP |
| Certificate Tile View (External) | External | MVP |
| Certificate Details | Auth | MVP |
| Certificate Create/Edit | Analyst, Admin | MVP |
| Dashboard | Auth | MVP |
| User Management | Admin | MVP |
| Role Management | Admin | MVP |
| Company Management | Analyst, Admin | MVP |
| Product Management | All Auth | MVP |
| Tag Management | Analyst, Admin | MVP |
| Certificate Type Management | Admin | Phase 2 |
| Partner API Management | Admin | Phase 2 |

### 5.2 Certificate Tile View (Public) - Deep Dive

| Aspect | Requirement |
|--------|-------------|
| **Display** | |
| Data shown | Certificate tiles: Number, Company, Product, Status, Expiration |
| Format | Card/tile grid |
| Record count | Typically 100-500, max 10,000 |
| **Pagination** | |
| Style | Infinite scroll (NO pagination) |
| **Sorting** | |
| Sortable | Certificate Number, Company, Issue Date, Expiration |
| Default | Most recently issued first |
| **Filtering** | |
| Filter 1 | Certification Type - Multi-select dropdown - Database-driven |
| Filter 2 | Company - Multi-select dropdown - Database-driven |
| Filter 3 | Status - Checkbox - Database-driven |
| Filter 4 | Expiration Range - Date picker |
| Logic | AND between filters |
| Filter state in URL | Yes - shareable URLs |
| **Search** | |
| Enabled | Yes |
| Fields | Certificate number, company name, product name |
| Type | Contains, search-as-you-type |
| Autocomplete | Yes - categorized suggestions |
| **Selection** | |
| Row selection | None (public view) |
| **Actions** | |
| Row actions | View details, Download PDF |
| **Responsive** | |
| Mobile | Same layout, responsive grid |

### 5.3 Certificate Tile View (Internal/External) - Differences from Public

| Aspect | Internal | External |
|--------|----------|----------|
| Data visibility | All certificates | Filtered by company/partner |
| Statuses shown | All 14 | Only Active + Expiring |
| Row selection | Multi-select checkboxes | Multi-select checkboxes |
| Bulk actions | Download ZIP, Export CSV | Download ZIP |
| Internal Tags | Visible | Hidden |
| Add Certificate button | Yes | No |
| Edit action | Yes | No |

---

## Section 6: Forms

### 6.1 Certificate Create/Edit Form

| Field | Type | Required | Validation | Notes |
|-------|------|----------|------------|-------|
| Company | Searchable dropdown | Yes | Must exist | Create new inline |
| Product | Searchable dropdown | Yes | Must exist | Filtered by company |
| Certification Type | Dropdown | Yes | | |
| Issue Date | Date picker | Yes | Not future | |
| Expiration Date | Date picker | Yes | After issue date | |
| Partner | Dropdown | Yes | Amazon/Novi/None | |
| Certification Results | Text | No | | Optional |
| Product IDs | Repeatable group | No | | ASIN, UPC, etc. |
| Tags | Multi-select | No | | |
| Internal Tags | Multi-select | No | | Hidden from External |
| Certificate PDF | File upload | No | PDF only, 10MB max | |

### 6.2 Product ID Management (Sub-form)

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Product ID Type | Dropdown | Yes | ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| Product ID Value | Text | Yes | Validated per type |
| Bulk Import | File upload | No | CSV format, 100s of IDs |

---

## Section 7: Notifications

### 7.1 Email Notifications

| Trigger | Recipients | Template |
|---------|------------|----------|
| Certificate Expiring (30 days) | Certificate owner, Admin | Expiration warning |
| Certificate Expired | Certificate owner, Admin | Expired notice |
| Certificate Status Change | Certificate owner | Status update |
| 2FA Code | User | 2FA verification |
| Password Reset | User | Reset link |

### 7.2 In-App Notifications

| Type | Pattern |
|------|---------|
| Success (save, delete, etc.) | Toast - auto-dismiss 5s |
| Error | Toast - auto-dismiss 8s |
| Delete confirmation | Modal - requires confirmation |
| Progress (upload, bulk) | Modal with progress bar |

---

## Section 8: Import/Export

### 8.1 Import

| Entity | Format | Features |
|--------|--------|----------|
| Product IDs | CSV | Bulk import 100s per certificate |
| Users | CSV | Admin bulk import |
| Companies | CSV | Admin bulk import |
| Products | CSV | Admin bulk import |

### 8.2 Export

| Entity | Format | Notes |
|--------|--------|-------|
| Certificates | CSV only | Filtered data export |
| Certificate PDFs | ZIP | Bulk download selected |
| Users | CSV | Admin only |
| Companies | CSV | Admin only |
| Products | CSV | Admin only |

**Note:** CSV ONLY for data exports. No Excel/JSON/PDF from admin pages.

---

## Section 9: Integrations

### 9.1 Partner API

| Aspect | Requirement |
|--------|-------------|
| Type | REST API |
| Authentication | API Key per partner |
| Consumers | Amazon, Novi, future partners |
| Endpoints | GET certificates (list, single, by number) |
| Rate limiting | Per partner |
| Documentation | Swagger |

### 9.2 Future Integrations

- Azure AD B2C (SSO) - Phase 2
- Webhook notifications to partners - Phase 2

---

## Section 10: Mobile & Responsive

| Aspect | Requirement |
|--------|-------------|
| Mobile support | Yes - responsive web |
| Native app | No |
| Tablet | Same as desktop |
| Mobile layout | Responsive grid, same features |
| Offline | No |
| Touch | Standard (no special gestures) |

---

## Section 11: Security

| Aspect | Requirement |
|--------|-------------|
| HTTPS | Yes |
| Password hashing | BCrypt |
| Session management | Cookie-based |
| CSRF protection | Yes |
| XSS protection | Yes |
| SQL injection | EF Core parameterized |
| Audit logging | Login, data changes |
| 2FA | Optional, email-based |

---

## Complexity Score

| Category | Points |
|----------|--------|
| Data Model (9 entities, relationships) | 35 |
| Business Rules (14-state workflow) | 40 |
| Users (4 roles, complex visibility) | 25 |
| Views (13 views, complex filters) | 65 |
| Forms (complex certificate form) | 20 |
| Notifications | 15 |
| Import/Export | 20 |
| API | 15 |
| Security (2FA) | 10 |
| **TOTAL** | **245** |

**Category: COMPLEX (151-300)**
**Estimate: 200-500 hours**

---

## What Was Missed (Lessons Learned)

These requirements emerged DURING development, causing rework:

| Missed Requirement | Impact | Should Have Asked |
|--------------------|--------|-------------------|
| 3 separate tile views | +40 hours | "How many variations of this view?" |
| Infinite scroll (not pagination) | +8 hours | "Pagination style?" |
| Shareable URLs with filter state | +12 hours | "Must URLs be bookmarkable?" |
| Database-driven filters | +16 hours | "Where do filter options come from?" |
| Autocomplete with categories | +10 hours | "Search suggestions needed?" |
| Bulk ZIP download | +8 hours | "What bulk actions?" |
| External user data filtering | +20 hours | "Complex visibility rules?" |
| 100s of Product IDs per cert | +15 hours | "Max items in relationship?" |
| Partner API (not just Amazon) | +12 hours | "Single partner or multiple?" |
| Internal tags hidden from External | +4 hours | "Field-level visibility?" |
| **TOTAL REWORK** | **~145 hours** | |

**Conclusion:** Proper discovery would have captured these upfront, reducing a 6-month project to 3-4 months.

---

*This example shows the level of detail needed to prevent scope creep and rework.*
