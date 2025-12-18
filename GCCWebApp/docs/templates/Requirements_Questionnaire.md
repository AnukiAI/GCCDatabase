# Comprehensive Requirements Questionnaire v2.0

**Purpose:** Capture COMPLETE project requirements BEFORE estimation to prevent scope creep, reduce rework, and deliver what the client actually needs.

**Instructions:**
1. Complete with client in discovery session(s)
2. Every "Yes" requires follow-up details
3. Use complexity scoring for realistic estimates
4. Client signs off BEFORE development begins
5. Changes after sign-off trigger formal change request

---

## Document Control

| Field | Value |
|-------|-------|
| Project Name | |
| Client Name | |
| Client Contact | |
| Date Completed | |
| Completed By | |
| Version | 2.0 |
| Sign-off Date | |

---

# PART A: PROJECT FOUNDATION

## Section 1: Project Overview

### 1.1 Business Context

| # | Question | Answer |
|---|----------|--------|
| 1.1.1 | What problem does this solve? | |
| 1.1.2 | Who requested this and why now? | |
| 1.1.3 | What happens if not completed? | |
| 1.1.4 | Existing systems this replaces/integrates with? | |
| 1.1.5 | What does success look like? Measurable? | |
| 1.1.6 | Who are the competitors? What do they do well? | |
| 1.1.7 | What's the business model? (SaaS, one-time, internal) | |

### 1.2 Constraints

| # | Question | Answer |
|---|----------|--------|
| 1.2.1 | Hard deadline? What drives it? | |
| 1.2.2 | Budget range? | |
| 1.2.3 | Regulatory requirements? (HIPAA, GDPR, SOC2, PCI) | |
| 1.2.4 | Technology constraints? (Platform, language, hosting) | |
| 1.2.5 | Branding/design guidelines to follow? | |
| 1.2.6 | Existing code/components to reuse? | |
| 1.2.7 | Team constraints? (Skills, availability) | |

### 1.3 Stakeholders

| # | Question | Answer |
|---|----------|--------|
| 1.3.1 | Primary decision maker? | |
| 1.3.2 | Who approves designs? | |
| 1.3.3 | Who approves functionality? | |
| 1.3.4 | End users? (List all types) | |
| 1.3.5 | Who maintains after launch? | |
| 1.3.6 | Who does UAT testing? | |
| 1.3.7 | Stakeholders who can block progress? | |

### 1.4 Success Metrics & KPIs

| # | Question | Answer |
|---|----------|--------|
| 1.4.1 | How will you measure success? | |
| 1.4.2 | Target metrics? (Users, transactions, revenue) | |
| 1.4.3 | Current baseline metrics? | |
| 1.4.4 | When do you expect to hit targets? | |
| 1.4.5 | What analytics/tracking needed? | |

---

## Section 2: Data Model & Entities

**CRITICAL: Define your data BEFORE building views. This section prevents major rework.**

### 2.1 Core Entities

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 2.1.1 | List ALL main entities (nouns in your system): | | |
| | Entity 1: | | +2 per entity |
| | Entity 2: | | |
| | Entity 3: | | |
| | Entity 4: | | |
| | Entity 5: | | |
| | (continue as needed) | | |

### 2.2 Per-Entity Details

**Complete for EACH entity:**

#### Entity Name: ____________________

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 2.2.1 | What fields/attributes? List all: | | +0.5 per field |
| 2.2.2 | Which fields are required? | | |
| 2.2.3 | Which fields are unique? | | +1 per unique |
| 2.2.4 | Which fields are searchable? | | +0.5 per field |
| 2.2.5 | Which fields are sortable? | | +0.5 per field |
| 2.2.6 | Which fields are filterable? | | +0.5 per field |
| 2.2.7 | Field data types? (text, number, date, file, etc.) | | |
| 2.2.8 | Field validation rules? | | +1 per rule |
| 2.2.9 | Default values? | | |
| 2.2.10 | Calculated/derived fields? | | +2 per field |
| 2.2.11 | Expected record count? (10s, 100s, 1000s, millions) | | |
| 2.2.12 | Growth rate? (records per day/month) | | |

### 2.3 Entity Relationships

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 2.3.1 | Draw relationships between entities: | | |
| | Entity A → Entity B: (1:1 / 1:N / N:N) | | +2 per relationship |
| | Entity A → Entity C: | | +3 for N:N |
| 2.3.2 | Are any relationships optional? | | |
| 2.3.3 | Cascade delete rules? | | +2 per cascade |
| 2.3.4 | Self-referencing entities? (Parent/child same type) | Yes / No | +4 if Yes |
| 2.3.5 | Polymorphic relationships? | Yes / No | +6 if Yes |
| 2.3.6 | Through/junction table data? (Extra fields on N:N) | Yes / No | +3 if Yes |

### 2.4 Data Lifecycle

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 2.4.1 | How is data created? (User entry, import, API, auto) | | |
| 2.4.2 | How is data updated? Who can update? | | |
| 2.4.3 | Soft delete or hard delete? | Soft / Hard / Both | +2 for soft |
| 2.4.4 | Data retention period? | | |
| 2.4.5 | Archival rules? | Yes / No | +4 if Yes |
| 2.4.6 | Data purge requirements? | Yes / No | +3 if Yes |

---

## Section 3: Business Rules & Logic

### 3.1 Validation Rules

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 3.1.1 | List all validation rules beyond basic field types: | | |
| | Rule 1: | | +1 per rule |
| | Rule 2: | | |
| 3.1.2 | Cross-field validations? (End > Start date) | | +2 per rule |
| 3.1.3 | Cross-entity validations? | | +3 per rule |
| 3.1.4 | Conditional required fields? | | +2 per condition |

### 3.2 Calculations & Formulas

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 3.2.1 | Auto-calculated fields? List formulas: | | +2 per formula |
| 3.2.2 | Aggregations needed? (Sum, count, avg) | | +1 per aggregation |
| 3.2.3 | Running totals/balances? | Yes / No | +4 if Yes |
| 3.2.4 | Date calculations? (Age, duration, deadlines) | | +2 per calc |
| 3.2.5 | Currency calculations? | Yes / No | +3 if Yes |
| 3.2.6 | Tax calculations? | Yes / No | +5 if Yes |
| 3.2.7 | Pricing rules/tiers? | Yes / No | +5 if Yes |

### 3.3 Automated Behaviors

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 3.3.1 | Auto-populate fields? From where? | | +2 per field |
| 3.3.2 | Auto-assign? (To user, to queue) | Yes / No | +3 if Yes |
| 3.3.3 | Auto-number/sequence generation? | Yes / No | +2 if Yes |
| 3.3.4 | Auto-status changes? When? | | +2 per trigger |
| 3.3.5 | Scheduled jobs? List them: | | +3 per job |
| 3.3.6 | Triggered actions? (On create, update, delete) | | +2 per trigger |

### 3.4 Workflows & State Machines

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 3.4.1 | Entities with status/state? | | |
| 3.4.2 | List all statuses per entity: | | +1 per status |
| 3.4.3 | Allowed transitions? (Draw state diagram) | | +2 per transition |
| 3.4.4 | Who can perform each transition? | | +1 per role check |
| 3.4.5 | Actions on transition? (Email, update fields) | | +2 per action |
| 3.4.6 | Approval workflows? | Yes / No | +8 if Yes |
| 3.4.7 | Multi-level approval? | Yes / No | +6 if Yes |
| 3.4.8 | Parallel approvals? | Yes / No | +5 if Yes |
| 3.4.9 | Rejection handling? | | +2 if complex |
| 3.4.10 | Escalation rules? | Yes / No | +5 if Yes |
| 3.4.11 | SLA tracking? | Yes / No | +6 if Yes |

### 3.5 Business Constraints

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 3.5.1 | Duplicate prevention rules? | | +2 per rule |
| 3.5.2 | Limit rules? (Max 5 items per user) | | +2 per limit |
| 3.5.3 | Date/time restrictions? (Business hours only) | | +3 if Yes |
| 3.5.4 | Geographic restrictions? | Yes / No | +4 if Yes |
| 3.5.5 | Blacklist/whitelist rules? | | +2 per list |

---

# PART B: USERS & ACCESS

## Section 4: Users & Authentication

### 4.1 User Types

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 4.1.1 | How many distinct user roles? | | +2 per role |
| 4.1.2 | List each role and permissions: | | |
| | Role 1: | | |
| | Role 2: | | |
| | Role 3: | | |
| 4.1.3 | Can users have multiple roles? | Yes / No | +3 if Yes |
| 4.1.4 | Role hierarchies? | Yes / No | +5 if Yes |
| 4.1.5 | Custom/dynamic roles? | Yes / No | +8 if Yes |
| 4.1.6 | Permission granularity? | Role / Feature / Record / Field | +3 per level |

### 4.2 Authentication

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 4.2.1 | Login identifier? | Username / Email / Phone / Employee ID | +1 each |
| 4.2.2 | Password requirements? | | +1 |
| 4.2.3 | Password expiration? | Yes / No | +2 if Yes |
| 4.2.4 | Two-Factor Authentication? | Yes / No | +5 if Yes |
| 4.2.5 | 2FA methods? | Email / SMS / Authenticator / Hardware | +2 each |
| 4.2.6 | 2FA mandatory or optional? | Mandatory / Optional / Role-based | +2 for role-based |
| 4.2.7 | Single Sign-On (SSO)? | Yes / No | +8 if Yes |
| 4.2.8 | SSO providers? | Google / Microsoft / Azure AD / SAML / LDAP | +3 each |
| 4.2.9 | Social login? | Google / Facebook / LinkedIn / Apple | +3 each |
| 4.2.10 | Passwordless login? | Yes / No | +5 if Yes |
| 4.2.11 | Magic link login? | Yes / No | +4 if Yes |
| 4.2.12 | Biometric login? | Yes / No | +6 if Yes |
| 4.2.13 | Self-service password reset? | Yes / No | +3 if Yes |
| 4.2.14 | Security questions? | Yes / No | +2 if Yes |
| 4.2.15 | Account lockout policy? | Yes / No | +2 if Yes |
| 4.2.16 | Session timeout? Duration? | | +1 |
| 4.2.17 | Concurrent session limit? | Yes / No | +3 if Yes |
| 4.2.18 | "Remember me" functionality? | Yes / No | +2 if Yes |
| 4.2.19 | Force logout capability? | Yes / No | +2 if Yes |
| 4.2.20 | Login audit trail? | Yes / No | +2 if Yes |

### 4.3 User Management

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 4.3.1 | User creation method? | Self-register / Admin / Invite / SSO-auto | +2 each |
| 4.3.2 | Email verification? | Yes / No | +2 if Yes |
| 4.3.3 | Admin approval required? | Yes / No | +3 if Yes |
| 4.3.4 | Invitation workflow? | Yes / No | +3 if Yes |
| 4.3.5 | Invite expiration? | Yes / No | +1 if Yes |
| 4.3.6 | User profile fields? List all: | | +0.5 per field |
| 4.3.7 | Profile picture/avatar? | Yes / No | +3 if Yes |
| 4.3.8 | Users edit own profile? | Yes / No | +1 if Yes |
| 4.3.9 | Admin can edit users? | Yes / No | +1 if Yes |
| 4.3.10 | Admin can impersonate? | Yes / No | +5 if Yes |
| 4.3.11 | Deactivate users (soft)? | Yes / No | +2 if Yes |
| 4.3.12 | Delete users (hard)? | Yes / No | +2 if Yes |
| 4.3.13 | Reactivate users? | Yes / No | +2 if Yes |
| 4.3.14 | Bulk user import? | Yes / No | +5 if Yes |
| 4.3.15 | Bulk user export? | Yes / No | +2 if Yes |
| 4.3.16 | User data export (GDPR)? | Yes / No | +4 if Yes |

### 4.4 Organizations & Multi-Tenancy

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 4.4.1 | Users belong to organizations/companies? | Yes / No | +5 if Yes |
| 4.4.2 | Multiple organizations per user? | Yes / No | +6 if Yes |
| 4.4.3 | Organization hierarchy? | Yes / No | +8 if Yes |
| 4.4.4 | Data isolation between orgs? | None / Soft / Hard | +5 for hard |
| 4.4.5 | Org-level admins? | Yes / No | +3 if Yes |
| 4.4.6 | Org-level settings/config? | Yes / No | +4 if Yes |
| 4.4.7 | Org-level branding? | Yes / No | +8 if Yes |
| 4.4.8 | Cross-org data sharing? | Yes / No | +6 if Yes |
| 4.4.9 | Org billing/subscriptions? | Yes / No | +10 if Yes |

### 4.5 Data Visibility & Row-Level Security

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 4.5.1 | Users see only own data? | Yes / No | +3 if Yes |
| 4.5.2 | Users see team data? | Yes / No | +4 if Yes |
| 4.5.3 | Users see org data? | Yes / No | +4 if Yes |
| 4.5.4 | Complex visibility rules? Describe: | | +5 per rule |
| 4.5.5 | Field-level visibility? (Hide columns by role) | Yes / No | +6 if Yes |
| 4.5.6 | Record-level permissions? | Yes / No | +8 if Yes |
| 4.5.7 | Data sharing rules? | Yes / No | +6 if Yes |

---

# PART C: USER INTERFACE

## Section 5: Views & Pages

**CRITICAL: This section prevents the "basic view" trap.**

### 5.1 View Inventory

| # | Question | Answer |
|---|----------|--------|
| 5.1.1 | List ALL pages/views needed: | |
| | Page 1: | |
| | Page 2: | |
| | (continue) | |
| 5.1.2 | Which are public (no login)? | |
| 5.1.3 | Which require authentication? | |
| 5.1.4 | Which are role-specific? | |
| 5.1.5 | Landing page for each role? | |

### 5.2 Per-View Deep Dive

**Complete FOR EACH VIEW:**

#### View Name: ____________________

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| **Data Display** | | | |
| 5.2.1 | What entity/entities displayed? | | |
| 5.2.2 | What fields shown? List all: | | +0.5 per field |
| 5.2.3 | Display format? | Table / Cards / List / Grid / Calendar / Timeline / Map / Mixed | +3 for mixed |
| 5.2.4 | Multiple view modes toggle? | Yes / No | +3 per mode |
| 5.2.5 | How many records typically? | | |
| 5.2.6 | Maximum records possible? | | |
| 5.2.7 | Empty state design needed? | Yes / No | +1 if Yes |
| **Pagination** | | | |
| 5.2.8 | Pagination style? | Pages / Infinite scroll / Load more / Virtual scroll / None | +3 for infinite |
| 5.2.9 | Records per page? | | |
| 5.2.10 | User can change page size? | Yes / No | +1 if Yes |
| 5.2.11 | Total count display? | Yes / No | +1 if Yes |
| **Sorting** | | | |
| 5.2.12 | Sortable columns? Which? | | +0.5 per column |
| 5.2.13 | Default sort order? | | |
| 5.2.14 | Multi-column sorting? | Yes / No | +3 if Yes |
| 5.2.15 | Remember sort preference? | Yes / No | +2 if Yes |
| **Filtering** | | | |
| 5.2.16 | Filter fields? List all: | | +1 per filter |
| 5.2.17 | Filter types? | Dropdown / Text / Date range / Checkbox / Slider / Tags | +1 per complex |
| 5.2.18 | Filter source? | Hardcoded / Database / API | +2 for dynamic |
| 5.2.19 | Multi-select filters? | Yes / No | +2 if Yes |
| 5.2.20 | Filter logic? | AND only / OR only / Configurable | +4 for configurable |
| 5.2.21 | Saved/favorite filters? | Yes / No | +5 if Yes |
| 5.2.22 | Quick filters/presets? | Yes / No | +2 if Yes |
| 5.2.23 | Advanced filter builder? | Yes / No | +8 if Yes |
| 5.2.24 | Filter state in URL? | Yes / No | +4 if Yes |
| **Search** | | | |
| 5.2.25 | Search functionality? | Yes / No | +2 if Yes |
| 5.2.26 | Search fields? | | +0.5 per field |
| 5.2.27 | Search type? | Contains / Starts with / Exact / Fuzzy | +2 for fuzzy |
| 5.2.28 | Search-as-you-type? | Yes / No | +4 if Yes |
| 5.2.29 | Search suggestions? | Yes / No | +5 if Yes |
| 5.2.30 | Categorized suggestions? | Yes / No | +3 if Yes |
| 5.2.31 | Search highlighting? | Yes / No | +2 if Yes |
| 5.2.32 | Recent searches? | Yes / No | +2 if Yes |
| 5.2.33 | Search state in URL? | Yes / No | +2 if Yes |
| **Selection & Bulk Actions** | | | |
| 5.2.34 | Row selection? | None / Single / Multiple | +2 for multiple |
| 5.2.35 | Select all scope? | Current page / All pages | +3 for all |
| 5.2.36 | Bulk actions? List: | | +3 per action |
| 5.2.37 | Confirmation for bulk actions? | Yes / No | +1 if Yes |
| 5.2.38 | Progress indicator for bulk? | Yes / No | +2 if Yes |
| **Row Actions** | | | |
| 5.2.39 | Row actions? | View / Edit / Delete / Duplicate / Other | +1 per action |
| 5.2.40 | Inline editing? | Yes / No | +8 if Yes |
| 5.2.41 | Inline cell editing? | Yes / No | +6 if Yes |
| 5.2.42 | Row expansion? | Yes / No | +5 if Yes |
| 5.2.43 | Context menu? | Yes / No | +3 if Yes |
| 5.2.44 | Drag-drop reorder? | Yes / No | +6 if Yes |
| **Visual Elements** | | | |
| 5.2.45 | Status badges? | Yes / No | +1 if Yes |
| 5.2.46 | Color coding? | Yes / No | +2 if Yes |
| 5.2.47 | Icons in data? | Yes / No | +1 if Yes |
| 5.2.48 | Thumbnails? | Yes / No | +3 if Yes |
| 5.2.49 | Progress bars? | Yes / No | +2 if Yes |
| 5.2.50 | Sparklines/mini-charts? | Yes / No | +4 if Yes |
| **Table Features** | | | |
| 5.2.51 | Sticky header? | Yes / No | +2 if Yes |
| 5.2.52 | Sticky first column? | Yes / No | +3 if Yes |
| 5.2.53 | Column resizing? | Yes / No | +4 if Yes |
| 5.2.54 | Column reordering? | Yes / No | +5 if Yes |
| 5.2.55 | Column show/hide? | Yes / No | +4 if Yes |
| 5.2.56 | Save column preferences? | Yes / No | +6 if Yes |
| 5.2.57 | Row grouping? | Yes / No | +6 if Yes |
| 5.2.58 | Subtotals? | Yes / No | +4 if Yes |
| **URL & Navigation** | | | |
| 5.2.59 | Shareable/bookmarkable URLs? | Yes / No | +3 if Yes |
| 5.2.60 | Deep link to records? | Yes / No | +2 if Yes |
| 5.2.61 | Breadcrumbs? | Yes / No | +2 if Yes |
| 5.2.62 | Back button behavior? | | +1 |
| **Responsive** | | | |
| 5.2.63 | Mobile layout? | Same / Different / Hidden | +5 for different |
| 5.2.64 | Tablet layout? | Same / Different | +3 for different |
| 5.2.65 | Touch gestures? | Yes / No | +4 if Yes |
| 5.2.66 | Minimum screen width? | | |
| **Real-time** | | | |
| 5.2.67 | Real-time updates? | Yes / No | +6 if Yes |
| 5.2.68 | Refresh button? | Yes / No | +1 if Yes |
| 5.2.69 | Auto-refresh interval? | Yes / No | +2 if Yes |

**(Duplicate for each view)**

---

## Section 6: Data Entry Forms

### 6.1 Form Inventory

| # | Question | Answer |
|---|----------|--------|
| 6.1.1 | List ALL forms: | |
| | Form 1: | |
| | Form 2: | |

### 6.2 Per-Form Deep Dive

#### Form Name: ____________________

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| **Fields** | | | |
| 6.2.1 | List fields and types: | | +0.5 per field |
| 6.2.2 | Required fields? | | +0.5 per required |
| 6.2.3 | Conditional fields? | | +3 per condition |
| 6.2.4 | Field dependencies? | | +4 per dependency |
| 6.2.5 | Read-only fields? | | +0.5 per field |
| 6.2.6 | Hidden fields? | | +0.5 per field |
| **Complex Field Types** | | | |
| 6.2.7 | File uploads? | Yes / No | +4 if Yes |
| 6.2.8 | Multiple file upload? | Yes / No | +3 if Yes |
| 6.2.9 | Drag-drop upload? | Yes / No | +2 if Yes |
| 6.2.10 | File type restrictions? | | |
| 6.2.11 | File size limit? | | |
| 6.2.12 | Image preview? | Yes / No | +2 if Yes |
| 6.2.13 | Image cropping? | Yes / No | +5 if Yes |
| 6.2.14 | Date pickers? | Yes / No | +1 per field |
| 6.2.15 | Date range pickers? | Yes / No | +2 per field |
| 6.2.16 | Time pickers? | Yes / No | +1 per field |
| 6.2.17 | DateTime pickers? | Yes / No | +2 per field |
| 6.2.18 | Rich text editor (WYSIWYG)? | Yes / No | +5 if Yes |
| 6.2.19 | Markdown editor? | Yes / No | +3 if Yes |
| 6.2.20 | Code editor? | Yes / No | +4 if Yes |
| 6.2.21 | Color picker? | Yes / No | +2 if Yes |
| 6.2.22 | Signature capture? | Yes / No | +5 if Yes |
| 6.2.23 | Address autocomplete? | Yes / No | +5 if Yes |
| 6.2.24 | Map/location picker? | Yes / No | +6 if Yes |
| 6.2.25 | Multi-select dropdown? | Yes / No | +2 per field |
| 6.2.26 | Searchable dropdown? | Yes / No | +2 per field |
| 6.2.27 | Tag input? | Yes / No | +3 per field |
| 6.2.28 | Repeatable groups? (Add another) | Yes / No | +5 per group |
| 6.2.29 | Nested forms? | Yes / No | +6 if Yes |
| 6.2.30 | Lookup/search fields? | Yes / No | +4 per field |
| **Validation** | | | |
| 6.2.31 | Client-side validation? | Yes / No | +2 if Yes |
| 6.2.32 | Server-side validation? | Yes / No | +2 if Yes |
| 6.2.33 | Real-time validation? | Yes / No | +3 if Yes |
| 6.2.34 | Custom error messages? | Yes / No | +1 if Yes |
| 6.2.35 | Error summary? | Yes / No | +1 if Yes |
| 6.2.36 | Inline errors? | Yes / No | +1 if Yes |
| 6.2.37 | Cross-field validation? | | +2 per rule |
| 6.2.38 | Async validation? (Check uniqueness) | Yes / No | +3 per field |
| **Form Behavior** | | | |
| 6.2.39 | Auto-save drafts? | Yes / No | +5 if Yes |
| 6.2.40 | Manual save draft? | Yes / No | +3 if Yes |
| 6.2.41 | Unsaved changes warning? | Yes / No | +2 if Yes |
| 6.2.42 | Multi-step wizard? | Yes / No | +3 per step |
| 6.2.43 | Non-linear wizard? | Yes / No | +3 if Yes |
| 6.2.44 | Progress indicator? | Yes / No | +2 if Yes |
| 6.2.45 | Step validation? | Yes / No | +2 if Yes |
| 6.2.46 | Review step? | Yes / No | +3 if Yes |
| 6.2.47 | Confirm before submit? | Yes / No | +1 if Yes |
| 6.2.48 | After submit action? | Redirect / Stay / Modal / Toast | |
| **Pre-population** | | | |
| 6.2.49 | Pre-populated fields? | | +2 per source |
| 6.2.50 | Copy from existing? | Yes / No | +4 if Yes |
| 6.2.51 | Import from file? | Yes / No | +6 if Yes |
| 6.2.52 | URL parameters? | Yes / No | +2 if Yes |
| 6.2.53 | Templates? | Yes / No | +4 if Yes |

---

## Section 7: User Experience States

### 7.1 Loading States

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 7.1.1 | Page loading indicator? | Yes / No | +1 if Yes |
| 7.1.2 | Skeleton screens? | Yes / No | +3 if Yes |
| 7.1.3 | Inline loading spinners? | Yes / No | +1 if Yes |
| 7.1.4 | Progress bars? | Yes / No | +2 if Yes |
| 7.1.5 | Loading messages/tips? | Yes / No | +1 if Yes |

### 7.2 Empty States

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 7.2.1 | Empty state designs? | Yes / No | +1 per view |
| 7.2.2 | Helpful empty messages? | Yes / No | +0.5 per view |
| 7.2.3 | Call-to-action in empty state? | Yes / No | +1 if Yes |
| 7.2.4 | Different empty states? (No results vs no data vs filtered) | Yes / No | +2 if Yes |

### 7.3 Error States

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 7.3.1 | Error page designs? (404, 500, etc.) | Yes / No | +1 per page |
| 7.3.2 | Friendly error messages? | Yes / No | +1 if Yes |
| 7.3.3 | Error recovery suggestions? | Yes / No | +2 if Yes |
| 7.3.4 | Retry functionality? | Yes / No | +2 if Yes |
| 7.3.5 | Offline error handling? | Yes / No | +3 if Yes |

### 7.4 Success States

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 7.4.1 | Success confirmations? | Toast / Modal / Inline / Page | +1 each |
| 7.4.2 | Success animations? | Yes / No | +2 if Yes |
| 7.4.3 | Next action suggestions? | Yes / No | +1 if Yes |

### 7.5 Onboarding & First-Time Experience

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 7.5.1 | Welcome screen? | Yes / No | +2 if Yes |
| 7.5.2 | Product tour? | Yes / No | +5 if Yes |
| 7.5.3 | Step-by-step tutorial? | Yes / No | +5 if Yes |
| 7.5.4 | Tooltips for new features? | Yes / No | +3 if Yes |
| 7.5.5 | Setup wizard? | Yes / No | +4 if Yes |
| 7.5.6 | Sample data? | Yes / No | +3 if Yes |
| 7.5.7 | Checklists/progress? | Yes / No | +3 if Yes |
| 7.5.8 | Skip onboarding option? | Yes / No | +1 if Yes |
| 7.5.9 | Replay onboarding? | Yes / No | +1 if Yes |

### 7.6 Help & Guidance

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 7.6.1 | In-app help? | Yes / No | +3 if Yes |
| 7.6.2 | Tooltips? | Yes / No | +1 if Yes |
| 7.6.3 | Help icons with popovers? | Yes / No | +2 if Yes |
| 7.6.4 | Contextual help? | Yes / No | +3 if Yes |
| 7.6.5 | Help search? | Yes / No | +4 if Yes |
| 7.6.6 | Video tutorials? | Yes / No | +4 per video |
| 7.6.7 | Keyboard shortcut help? | Yes / No | +2 if Yes |

---

## Section 8: Navigation & Layout

### 8.1 Global Navigation

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 8.1.1 | Navigation style? | Top bar / Side bar / Both / Tab bar | +2 for both |
| 8.1.2 | Collapsible sidebar? | Yes / No | +2 if Yes |
| 8.1.3 | Navigation levels? | 1 / 2 / 3+ | +2 per level |
| 8.1.4 | Mega menu? | Yes / No | +4 if Yes |
| 8.1.5 | Role-based navigation? | Yes / No | +2 if Yes |
| 8.1.6 | Dynamic navigation? | Yes / No | +4 if Yes |
| 8.1.7 | Recent items? | Yes / No | +2 if Yes |
| 8.1.8 | Favorites/pins? | Yes / No | +3 if Yes |
| 8.1.9 | Search in navigation? | Yes / No | +3 if Yes |

### 8.2 Page Layout

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 8.2.1 | Header content? | Logo / Search / User menu / Notifications / Other | +1 each |
| 8.2.2 | Footer required? | Yes / No | +1 if Yes |
| 8.2.3 | Footer content? | Links / Copyright / Social / Contact | +0.5 each |
| 8.2.4 | Sidebar content? | Navigation / Filters / Widgets / Context | +1 each |
| 8.2.5 | Resizable panels? | Yes / No | +4 if Yes |
| 8.2.6 | Split view? | Yes / No | +5 if Yes |
| 8.2.7 | Tabs within pages? | Yes / No | +2 if Yes |
| 8.2.8 | Accordions? | Yes / No | +1 if Yes |

### 8.3 Modals & Overlays

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 8.3.1 | Modal dialogs? | Yes / No | +1 per modal |
| 8.3.2 | Side panels/drawers? | Yes / No | +2 per panel |
| 8.3.3 | Full-screen modals? | Yes / No | +2 if Yes |
| 8.3.4 | Stacked modals? | Yes / No | +3 if Yes |
| 8.3.5 | Modal forms? | Yes / No | +2 per form |
| 8.3.6 | Lightbox for images? | Yes / No | +2 if Yes |

---

# PART D: FEATURES & FUNCTIONALITY

## Section 9: Search & Discovery

### 9.1 Global Search

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 9.1.1 | Global search? | Yes / No | +8 if Yes |
| 9.1.2 | Entities in global search? | | +2 per entity |
| 9.1.3 | Result grouping? | Yes / No | +3 if Yes |
| 9.1.4 | Result ranking? | Yes / No | +5 if Yes |
| 9.1.5 | Recent searches? | Yes / No | +2 if Yes |
| 9.1.6 | Saved searches? | Yes / No | +4 if Yes |
| 9.1.7 | Search filters? | Yes / No | +3 if Yes |

### 9.2 Advanced Search

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 9.2.1 | Advanced search builder? | Yes / No | +8 if Yes |
| 9.2.2 | Boolean operators? | Yes / No | +3 if Yes |
| 9.2.3 | Wildcard search? | Yes / No | +2 if Yes |
| 9.2.4 | Phrase search? | Yes / No | +2 if Yes |
| 9.2.5 | Full-text search? | Yes / No | +6 if Yes |
| 9.2.6 | Search within files? | Yes / No | +10 if Yes |
| 9.2.7 | Fuzzy/typo tolerance? | Yes / No | +4 if Yes |
| 9.2.8 | Synonyms? | Yes / No | +4 if Yes |

---

## Section 10: Reporting & Analytics

### 10.1 Dashboards

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 10.1.1 | Dashboard required? | Yes / No | +5 if Yes |
| 10.1.2 | Number of dashboards? | | +4 per dashboard |
| 10.1.3 | Role-specific dashboards? | Yes / No | +3 per role |
| 10.1.4 | Customizable dashboard? | Yes / No | +8 if Yes |
| 10.1.5 | Drag-drop widgets? | Yes / No | +5 if Yes |
| 10.1.6 | Widget library? | Yes / No | +3 if Yes |
| 10.1.7 | Save dashboard layouts? | Yes / No | +3 if Yes |
| 10.1.8 | Share dashboards? | Yes / No | +3 if Yes |
| 10.1.9 | Dashboard refresh? | Manual / Auto / Real-time | +3 for real-time |
| 10.1.10 | Date range selector? | Yes / No | +3 if Yes |
| 10.1.11 | Comparison periods? | Yes / No | +4 if Yes |

### 10.2 Charts & Visualizations

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 10.2.1 | Chart types? | Bar / Line / Pie / Donut / Area / Scatter / Gauge / Funnel / Sankey | +2 each |
| 10.2.2 | Interactive charts? | Yes / No | +3 if Yes |
| 10.2.3 | Drill-down? | Yes / No | +4 if Yes |
| 10.2.4 | Export charts? | Image / PDF | +2 each |
| 10.2.5 | Responsive charts? | Yes / No | +2 if Yes |
| 10.2.6 | Maps? | Yes / No | +10 if Yes |
| 10.2.7 | Map types? | Markers / Choropleth / Heat | +3 each |

### 10.3 Reports

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 10.3.1 | Pre-built reports? | Yes / No | +3 per report |
| 10.3.2 | Custom report builder? | Yes / No | +15 if Yes |
| 10.3.3 | Report parameters? | Yes / No | +2 if Yes |
| 10.3.4 | Report formats? | PDF / Excel / CSV / Word | +2 each |
| 10.3.5 | Report scheduling? | Yes / No | +5 if Yes |
| 10.3.6 | Email distribution? | Yes / No | +4 if Yes |
| 10.3.7 | Report templates? | Yes / No | +4 if Yes |

---

## Section 11: Notifications & Communication

### 11.1 Email Notifications

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 11.1.1 | Email notifications? | Yes / No | +3 if Yes |
| 11.1.2 | Email triggers? List all: | | +2 per trigger |
| 11.1.3 | Email templates? | Yes / No | +2 per template |
| 11.1.4 | Template editor? | Yes / No | +5 if Yes |
| 11.1.5 | Dynamic content? | Yes / No | +2 if Yes |
| 11.1.6 | Email attachments? | Yes / No | +3 if Yes |
| 11.1.7 | User preferences? | Yes / No | +4 if Yes |
| 11.1.8 | Unsubscribe? | Yes / No | +2 if Yes |
| 11.1.9 | Email provider? | SMTP / SendGrid / SES / Mailgun | |
| 11.1.10 | Track opens/clicks? | Yes / No | +4 if Yes |
| 11.1.11 | Bounce handling? | Yes / No | +3 if Yes |

### 11.2 In-App Notifications

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 11.2.1 | Notification center? | Yes / No | +5 if Yes |
| 11.2.2 | Real-time notifications? | Yes / No | +6 if Yes |
| 11.2.3 | Notification types? | Info / Success / Warning / Error | +1 each |
| 11.2.4 | Notification actions? | Yes / No | +2 if Yes |
| 11.2.5 | Mark as read? | Yes / No | +2 if Yes |
| 11.2.6 | Mark all read? | Yes / No | +1 if Yes |
| 11.2.7 | Delete notifications? | Yes / No | +1 if Yes |
| 11.2.8 | Notification history? | Yes / No | +2 if Yes |
| 11.2.9 | Badge counts? | Yes / No | +2 if Yes |
| 11.2.10 | Toast notifications? | Yes / No | +2 if Yes |
| 11.2.11 | Toast auto-dismiss? | Yes / No | +1 if Yes |
| 11.2.12 | Toast actions? | Yes / No | +2 if Yes |

### 11.3 Other Notifications

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 11.3.1 | SMS notifications? | Yes / No | +6 if Yes |
| 11.3.2 | Push notifications? | Yes / No | +8 if Yes |
| 11.3.3 | Browser notifications? | Yes / No | +4 if Yes |
| 11.3.4 | Slack integration? | Yes / No | +5 if Yes |
| 11.3.5 | Teams integration? | Yes / No | +5 if Yes |
| 11.3.6 | Webhooks? | Yes / No | +5 if Yes |

---

## Section 12: File & Document Management

### 12.1 File Uploads

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 12.1.1 | File uploads? | Yes / No | +4 if Yes |
| 12.1.2 | Max file size? | | |
| 12.1.3 | Allowed file types? | | |
| 12.1.4 | Multiple files per upload? | Yes / No | +2 if Yes |
| 12.1.5 | Drag-drop upload? | Yes / No | +2 if Yes |
| 12.1.6 | Upload progress? | Yes / No | +2 if Yes |
| 12.1.7 | Chunked upload? | Yes / No | +4 if Yes |
| 12.1.8 | Resume upload? | Yes / No | +5 if Yes |
| 12.1.9 | Virus scanning? | Yes / No | +5 if Yes |
| 12.1.10 | Storage location? | Local / S3 / Azure / GCP | +3 for cloud |
| 12.1.11 | CDN for files? | Yes / No | +3 if Yes |

### 12.2 File Management

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 12.2.1 | File listing view? | Yes / No | +2 if Yes |
| 12.2.2 | Folders/categories? | Yes / No | +4 if Yes |
| 12.2.3 | File search? | Yes / No | +3 if Yes |
| 12.2.4 | File preview? | Yes / No | +5 if Yes |
| 12.2.5 | Preview file types? | Images / PDF / Office / Video | +2 each |
| 12.2.6 | File versioning? | Yes / No | +5 if Yes |
| 12.2.7 | Version comparison? | Yes / No | +6 if Yes |
| 12.2.8 | Restore versions? | Yes / No | +3 if Yes |
| 12.2.9 | File rename? | Yes / No | +1 if Yes |
| 12.2.10 | File move? | Yes / No | +2 if Yes |
| 12.2.11 | File copy? | Yes / No | +2 if Yes |
| 12.2.12 | Bulk download? | Yes / No | +3 if Yes |
| 12.2.13 | ZIP download? | Yes / No | +4 if Yes |
| 12.2.14 | File sharing/links? | Yes / No | +4 if Yes |
| 12.2.15 | Link expiration? | Yes / No | +2 if Yes |
| 12.2.16 | Password protected links? | Yes / No | +3 if Yes |

### 12.3 Document Generation

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 12.3.1 | PDF generation? | Yes / No | +5 if Yes |
| 12.3.2 | PDF templates? | | +3 per template |
| 12.3.3 | Dynamic PDF content? | Yes / No | +3 if Yes |
| 12.3.4 | PDF with images? | Yes / No | +2 if Yes |
| 12.3.5 | PDF with tables? | Yes / No | +2 if Yes |
| 12.3.6 | PDF with charts? | Yes / No | +4 if Yes |
| 12.3.7 | Excel generation? | Yes / No | +4 if Yes |
| 12.3.8 | Word generation? | Yes / No | +4 if Yes |
| 12.3.9 | Mail merge? | Yes / No | +5 if Yes |
| 12.3.10 | E-signatures? | Yes / No | +10 if Yes |

---

## Section 13: Import & Export

### 13.1 Data Import

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 13.1.1 | Import functionality? | Yes / No | +5 if Yes |
| 13.1.2 | Import formats? | CSV / Excel / JSON / XML | +2 each |
| 13.1.3 | Import entities? | | +3 per entity |
| 13.1.4 | Import template download? | Yes / No | +2 if Yes |
| 13.1.5 | Column mapping? | Yes / No | +4 if Yes |
| 13.1.6 | Import validation? | Yes / No | +3 if Yes |
| 13.1.7 | Validation preview? | Yes / No | +3 if Yes |
| 13.1.8 | Partial import? | Yes / No | +2 if Yes |
| 13.1.9 | Error reporting? | Yes / No | +2 if Yes |
| 13.1.10 | Download error report? | Yes / No | +2 if Yes |
| 13.1.11 | Duplicate handling? | Skip / Update / Error | +2 each |
| 13.1.12 | Import history? | Yes / No | +3 if Yes |
| 13.1.13 | Undo import? | Yes / No | +5 if Yes |
| 13.1.14 | Scheduled imports? | Yes / No | +5 if Yes |
| 13.1.15 | Import from URL? | Yes / No | +3 if Yes |

### 13.2 Data Export

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 13.2.1 | Export functionality? | Yes / No | +3 if Yes |
| 13.2.2 | Export formats? | CSV / Excel / PDF / JSON / XML | +2 each |
| 13.2.3 | Export entities? | | +2 per entity |
| 13.2.4 | Column selection? | Yes / No | +2 if Yes |
| 13.2.5 | Export filtered data? | Yes / No | +2 if Yes |
| 13.2.6 | Export all data? | Yes / No | +2 if Yes |
| 13.2.7 | Large export handling? | Immediate / Background / Email | +3 for background |
| 13.2.8 | Export templates? | Yes / No | +3 if Yes |
| 13.2.9 | Scheduled exports? | Yes / No | +5 if Yes |

---

## Section 14: Integrations

### 14.1 API (Outbound)

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 14.1.1 | REST API? | Yes / No | +8 if Yes |
| 14.1.2 | GraphQL API? | Yes / No | +10 if Yes |
| 14.1.3 | API documentation? | Yes / No | +3 if Yes |
| 14.1.4 | Interactive docs (Swagger)? | Yes / No | +2 if Yes |
| 14.1.5 | API authentication? | None / API Key / OAuth / JWT | +2-5 each |
| 14.1.6 | API rate limiting? | Yes / No | +3 if Yes |
| 14.1.7 | API versioning? | Yes / No | +4 if Yes |
| 14.1.8 | API usage tracking? | Yes / No | +3 if Yes |
| 14.1.9 | API key management UI? | Yes / No | +4 if Yes |
| 14.1.10 | Webhooks (outbound)? | Yes / No | +5 if Yes |
| 14.1.11 | Webhook retry? | Yes / No | +2 if Yes |
| 14.1.12 | Webhook logs? | Yes / No | +2 if Yes |

### 14.2 Third-Party Integrations

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 14.2.1 | List integrations: | | +5-15 each |
| 14.2.2 | Payment gateway? | Stripe / PayPal / Square / Other | +10 each |
| 14.2.3 | Accounting? | QuickBooks / Xero / FreshBooks | +10 each |
| 14.2.4 | CRM? | Salesforce / HubSpot / Zoho | +10 each |
| 14.2.5 | Email marketing? | Mailchimp / Constant Contact / SendGrid | +5 each |
| 14.2.6 | Analytics? | GA4 / Mixpanel / Amplitude / Heap | +2 each |
| 14.2.7 | Error tracking? | Sentry / Bugsnag / Rollbar | +3 each |
| 14.2.8 | Calendar? | Google / Outlook / iCal | +5 each |
| 14.2.9 | Video conferencing? | Zoom / Teams / Google Meet | +6 each |
| 14.2.10 | Cloud storage? | Dropbox / Google Drive / OneDrive | +5 each |
| 14.2.11 | Social media? | Facebook / Twitter / LinkedIn / Instagram | +4 each |
| 14.2.12 | Maps? | Google Maps / Mapbox / HERE | +5 if Yes |
| 14.2.13 | Address validation? | SmartyStreets / Google / USPS | +4 if Yes |
| 14.2.14 | Background checks? | | +8 if Yes |
| 14.2.15 | ID verification? | | +10 if Yes |

### 14.3 Data Sync

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 14.3.1 | Sync with external system? | Yes / No | +6 per system |
| 14.3.2 | Sync direction? | One-way / Two-way | +4 for two-way |
| 14.3.3 | Sync frequency? | Manual / Scheduled / Real-time | +4 for real-time |
| 14.3.4 | Conflict resolution? | | +5 if needed |
| 14.3.5 | Sync monitoring? | Yes / No | +3 if Yes |
| 14.3.6 | Sync error handling? | | +3 if Yes |

---

# PART E: TECHNICAL REQUIREMENTS

## Section 15: Mobile & Responsive

### 15.1 Responsive Design

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 15.1.1 | Desktop browsers? | Chrome / Firefox / Safari / Edge / IE11 | +5 for IE11 |
| 15.1.2 | Minimum desktop width? | | |
| 15.1.3 | Tablet support? | Yes / No | +3 if Yes |
| 15.1.4 | Mobile support? | Yes / No | +5 if Yes |
| 15.1.5 | Minimum mobile width? | | |
| 15.1.6 | Same features all devices? | Yes / No | |
| 15.1.7 | Mobile-specific features? | | +3 each |
| 15.1.8 | Features hidden on mobile? | | +1 each |
| 15.1.9 | Touch-optimized? | Yes / No | +3 if Yes |

### 15.2 Progressive Web App (PWA)

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 15.2.1 | PWA required? | Yes / No | +8 if Yes |
| 15.2.2 | Add to home screen? | Yes / No | +2 if Yes |
| 15.2.3 | Offline support? | Yes / No | +10 if Yes |
| 15.2.4 | Offline features? | | +3 per feature |
| 15.2.5 | Background sync? | Yes / No | +5 if Yes |
| 15.2.6 | Service worker caching? | Yes / No | +4 if Yes |

### 15.3 Native Mobile App

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 15.3.1 | Native app required? | Yes / No | +50 if Yes |
| 15.3.2 | Platforms? | iOS / Android / Both | +20 each |
| 15.3.3 | Technology? | Native / React Native / Flutter | |
| 15.3.4 | Feature parity with web? | Full / Partial / Different | |
| 15.3.5 | Camera access? | Yes / No | +5 if Yes |
| 15.3.6 | GPS/Location? | Yes / No | +5 if Yes |
| 15.3.7 | Push notifications? | Yes / No | +5 if Yes |
| 15.3.8 | Biometric auth? | Yes / No | +4 if Yes |
| 15.3.9 | Offline mode? | Yes / No | +15 if Yes |
| 15.3.10 | Deep linking? | Yes / No | +3 if Yes |
| 15.3.11 | App store publishing? | Yes / No | +5 if Yes |

---

## Section 16: Security & Compliance

### 16.1 Data Security

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 16.1.1 | Encryption at rest? | Yes / No | +3 if Yes |
| 16.1.2 | Encryption in transit? | Yes / No | +1 if Yes |
| 16.1.3 | Field-level encryption? | Yes / No | +5 if Yes |
| 16.1.4 | PII fields? List them: | | +1 per field |
| 16.1.5 | PII masking? | Yes / No | +3 if Yes |
| 16.1.6 | Data anonymization? | Yes / No | +5 if Yes |
| 16.1.7 | Secure file storage? | Yes / No | +2 if Yes |
| 16.1.8 | Key management? | | +4 if complex |

### 16.2 Access Security

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 16.2.1 | IP whitelist/blacklist? | Yes / No | +3 if Yes |
| 16.2.2 | Geo-blocking? | Yes / No | +4 if Yes |
| 16.2.3 | CAPTCHA? Where? | | +2 per location |
| 16.2.4 | Brute force protection? | Yes / No | +3 if Yes |
| 16.2.5 | CSRF protection? | Yes / No | +2 if Yes |
| 16.2.6 | XSS protection? | Yes / No | +2 if Yes |
| 16.2.7 | SQL injection protection? | Yes / No | +2 if Yes |
| 16.2.8 | Content Security Policy? | Yes / No | +3 if Yes |
| 16.2.9 | Security headers? | Yes / No | +2 if Yes |

### 16.3 Compliance Requirements

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 16.3.1 | GDPR? | Yes / No | +8 if Yes |
| 16.3.2 | Right to be forgotten? | Yes / No | +5 if Yes |
| 16.3.3 | Data portability? | Yes / No | +4 if Yes |
| 16.3.4 | Consent management? | Yes / No | +4 if Yes |
| 16.3.5 | Cookie consent? | Yes / No | +3 if Yes |
| 16.3.6 | CCPA? | Yes / No | +6 if Yes |
| 16.3.7 | HIPAA? | Yes / No | +15 if Yes |
| 16.3.8 | SOC 2? | Yes / No | +10 if Yes |
| 16.3.9 | PCI DSS? | Yes / No | +12 if Yes |
| 16.3.10 | WCAG 2.1 Level? | A / AA / AAA | +5/10/15 |
| 16.3.11 | Industry-specific? | | +5-15 |

### 16.4 Audit & Logging

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 16.4.1 | Audit logging? | Yes / No | +5 if Yes |
| 16.4.2 | What to log? | Login / Data changes / Admin actions / All | +2 each |
| 16.4.3 | Log retention period? | | |
| 16.4.4 | Audit log search? | Yes / No | +3 if Yes |
| 16.4.5 | Audit log export? | Yes / No | +2 if Yes |
| 16.4.6 | Tamper-proof logs? | Yes / No | +5 if Yes |
| 16.4.7 | Real-time alerts? | Yes / No | +4 if Yes |

---

## Section 17: Performance & Scalability

### 17.1 Performance Requirements

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 17.1.1 | Expected concurrent users? | | |
| 17.1.2 | Peak concurrent users? | | |
| 17.1.3 | Page load time target? | | +3 if <2s |
| 17.1.4 | API response time target? | | +3 if <500ms |
| 17.1.5 | Database query targets? | | |
| 17.1.6 | Mobile performance target? | | +2 if specific |

### 17.2 Scalability

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 17.2.1 | Data volume now? | | |
| 17.2.2 | Data volume in 1 year? | | |
| 17.2.3 | Data volume in 5 years? | | |
| 17.2.4 | User growth expectation? | | |
| 17.2.5 | Auto-scaling needed? | Yes / No | +8 if Yes |
| 17.2.6 | Load balancing? | Yes / No | +5 if Yes |
| 17.2.7 | Database scaling strategy? | | +5 if complex |
| 17.2.8 | Caching strategy? | None / App / Redis / CDN | +3-5 each |

### 17.3 Availability

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 17.3.1 | Uptime SLA? | 99% / 99.9% / 99.99% | +3 per 9 |
| 17.3.2 | RTO (recovery time)? | | +2-5 |
| 17.3.3 | RPO (data loss tolerance)? | | +2-5 |
| 17.3.4 | Backup frequency? | | +1-3 |
| 17.3.5 | Backup retention? | | |
| 17.3.6 | Disaster recovery plan? | Yes / No | +5 if Yes |
| 17.3.7 | Multi-region deployment? | Yes / No | +10 if Yes |

---

## Section 18: Deployment & Infrastructure

### 18.1 Hosting

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 18.1.1 | Hosting type? | Cloud / On-premise / Hybrid | +5 for on-prem |
| 18.1.2 | Cloud provider? | AWS / Azure / GCP / Other | |
| 18.1.3 | Existing infrastructure? | | +3 if constraints |
| 18.1.4 | Containerization? | Yes / No | +4 if Yes |
| 18.1.5 | Kubernetes? | Yes / No | +8 if Yes |
| 18.1.6 | Serverless? | Yes / No | +5 if Yes |
| 18.1.7 | CDN? | Yes / No | +3 if Yes |

### 18.2 Environments

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 18.2.1 | Environments needed? | Dev / Test / Staging / Prod | +2 each |
| 18.2.2 | Environment parity? | Yes / No | +2 if Yes |
| 18.2.3 | Data refresh strategy? | | +3 if needed |
| 18.2.4 | Feature flags? | Yes / No | +5 if Yes |

### 18.3 CI/CD

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 18.3.1 | CI/CD pipeline? | Yes / No | +5 if Yes |
| 18.3.2 | CI/CD platform? | GitHub Actions / Azure DevOps / Jenkins / Other | |
| 18.3.3 | Automated testing? | Yes / No | +4 if Yes |
| 18.3.4 | Test coverage requirement? | | +2-5 |
| 18.3.5 | Automated deployments? | Yes / No | +3 if Yes |
| 18.3.6 | Blue-green deployment? | Yes / No | +6 if Yes |
| 18.3.7 | Rollback capability? | Yes / No | +3 if Yes |
| 18.3.8 | Database migrations? | Manual / Automated | +3 for automated |

### 18.4 Monitoring

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 18.4.1 | Application monitoring? | Yes / No | +3 if Yes |
| 18.4.2 | Infrastructure monitoring? | Yes / No | +3 if Yes |
| 18.4.3 | Log aggregation? | Yes / No | +3 if Yes |
| 18.4.4 | Error tracking? | Yes / No | +2 if Yes |
| 18.4.5 | Performance monitoring? | Yes / No | +3 if Yes |
| 18.4.6 | Health check endpoints? | Yes / No | +2 if Yes |
| 18.4.7 | Alerting? | Email / SMS / Slack / PagerDuty | +2 each |
| 18.4.8 | On-call rotation? | Yes / No | +3 if Yes |

---

## Section 19: SEO & Marketing

### 19.1 SEO Requirements

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 19.1.1 | SEO important? | Yes / No | +3 if Yes |
| 19.1.2 | Server-side rendering? | Yes / No | +8 if Yes |
| 19.1.3 | Meta tags management? | Yes / No | +2 if Yes |
| 19.1.4 | Dynamic meta tags? | Yes / No | +3 if Yes |
| 19.1.5 | Open Graph tags? | Yes / No | +2 if Yes |
| 19.1.6 | Twitter cards? | Yes / No | +2 if Yes |
| 19.1.7 | Structured data (JSON-LD)? | Yes / No | +3 if Yes |
| 19.1.8 | XML sitemap? | Yes / No | +2 if Yes |
| 19.1.9 | Dynamic sitemap? | Yes / No | +3 if Yes |
| 19.1.10 | Robots.txt? | Yes / No | +1 if Yes |
| 19.1.11 | Canonical URLs? | Yes / No | +2 if Yes |
| 19.1.12 | URL structure control? | Yes / No | +2 if Yes |
| 19.1.13 | Redirects management? | Yes / No | +2 if Yes |

### 19.2 Marketing Integration

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 19.2.1 | Analytics tracking? | GA4 / Adobe / Mixpanel | +2 each |
| 19.2.2 | Event tracking? | Yes / No | +2 if Yes |
| 19.2.3 | Conversion tracking? | Yes / No | +2 if Yes |
| 19.2.4 | Pixel tracking? | Facebook / LinkedIn / Google | +2 each |
| 19.2.5 | UTM parameter handling? | Yes / No | +2 if Yes |
| 19.2.6 | A/B testing? | Yes / No | +5 if Yes |
| 19.2.7 | Personalization? | Yes / No | +8 if Yes |

### 19.3 Content & Social

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 19.3.1 | Social sharing buttons? | Yes / No | +2 if Yes |
| 19.3.2 | Blog/CMS? | Yes / No | +10 if Yes |
| 19.3.3 | Landing pages? | Yes / No | +3 per page |
| 19.3.4 | QR code generation? | Yes / No | +2 if Yes |

---

## Section 20: Print & Offline

### 20.1 Print Requirements

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 20.1.1 | Print stylesheets? | Yes / No | +2 if Yes |
| 20.1.2 | Print-specific layouts? | Yes / No | +3 per page |
| 20.1.3 | Print header/footer? | Yes / No | +2 if Yes |
| 20.1.4 | Print page breaks? | Yes / No | +2 if Yes |
| 20.1.5 | Print preview? | Yes / No | +3 if Yes |
| 20.1.6 | Label printing? | Yes / No | +5 if Yes |
| 20.1.7 | Barcode printing? | Yes / No | +4 if Yes |

---

## Section 21: Design & Branding

### 21.1 Visual Design

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 21.1.1 | Existing brand guidelines? | Yes / No | -3 if Yes |
| 21.1.2 | Existing design system? | Yes / No | -5 if Yes |
| 21.1.3 | Reference websites (list 3): | | |
| 21.1.4 | Color palette provided? | Yes / No | |
| 21.1.5 | Logo files provided? | Yes / No | |
| 21.1.6 | Font preference? | | |
| 21.1.7 | Icon style? | Line / Solid / Custom | +3 for custom |
| 21.1.8 | Illustration style? | | +5 if custom |
| 21.1.9 | Photography style? | | |
| 21.1.10 | Dark mode? | Yes / No | +5 if Yes |
| 21.1.11 | Theme customization? | Yes / No | +8 if Yes |
| 21.1.12 | White-labeling? | Yes / No | +15 if Yes |

### 21.2 UI Framework

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 21.2.1 | UI framework? | Bootstrap / Tailwind / Material / Custom | +10 for custom |
| 21.2.2 | Custom components needed? | | +2 per component |
| 21.2.3 | Animation requirements? | None / Subtle / Rich | +5 for rich |
| 21.2.4 | Micro-interactions? | Yes / No | +4 if Yes |
| 21.2.5 | Accessibility (a11y)? | Yes / No | +3 if Yes |

---

## Section 22: Localization

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 22.1 | Multiple languages? | Yes / No | +8 if Yes |
| 22.2 | Languages needed? | | +3 per language |
| 22.3 | RTL support? | Yes / No | +10 if Yes |
| 22.4 | Translation management? | | +3 if needed |
| 22.5 | Multiple currencies? | Yes / No | +5 if Yes |
| 22.6 | Multiple time zones? | Yes / No | +4 if Yes |
| 22.7 | Date format localization? | Yes / No | +2 if Yes |
| 22.8 | Number format localization? | Yes / No | +2 if Yes |
| 22.9 | User language preference? | Yes / No | +2 if Yes |
| 22.10 | Auto-detect language? | Yes / No | +2 if Yes |

---

# PART F: PROJECT EXECUTION

## Section 23: Data Migration

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 23.1 | Existing data to migrate? | Yes / No | +10 if Yes |
| 23.2 | Source system(s)? | | +3 per source |
| 23.3 | Data format? | CSV / Excel / Database / API / Other | +2 each |
| 23.4 | Data volume? | | |
| 23.5 | Data quality issues? | | +5 if Yes |
| 23.6 | Data cleanup needed? | Yes / No | +5 if Yes |
| 23.7 | Data mapping required? | Yes / No | +4 if Yes |
| 23.8 | Data transformation rules? | | +2 per rule |
| 23.9 | Historical data? | Yes / No | +3 if Yes |
| 23.10 | Migration testing rounds? | | +2 per round |
| 23.11 | Cutover strategy? | Big bang / Phased | +3 for phased |
| 23.12 | Rollback plan? | Yes / No | +3 if Yes |

---

## Section 24: Testing & UAT

### 24.1 Testing Strategy

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 24.1.1 | Unit tests required? | Yes / No | +3 if Yes |
| 24.1.2 | Integration tests? | Yes / No | +4 if Yes |
| 24.1.3 | E2E tests? | Yes / No | +5 if Yes |
| 24.1.4 | Performance tests? | Yes / No | +4 if Yes |
| 24.1.5 | Security tests? | Yes / No | +4 if Yes |
| 24.1.6 | Accessibility tests? | Yes / No | +3 if Yes |
| 24.1.7 | Test coverage target? | | |
| 24.1.8 | Test data requirements? | | +2 if complex |

### 24.2 User Acceptance Testing

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 24.2.1 | UAT environment needed? | Yes / No | +2 if Yes |
| 24.2.2 | UAT testers? Who? | | |
| 24.2.3 | UAT test cases provided? | Yes / No | |
| 24.2.4 | UAT duration? | | |
| 24.2.5 | UAT sign-off process? | | |
| 24.2.6 | Bug tracking tool? | | |
| 24.2.7 | Bug priority definitions? | | |
| 24.2.8 | UAT rounds expected? | | +3 per round |

---

## Section 25: Training & Handoff

### 25.1 Documentation

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 25.1.1 | User documentation? | Yes / No | +5 if Yes |
| 25.1.2 | Admin documentation? | Yes / No | +3 if Yes |
| 25.1.3 | Technical documentation? | Yes / No | +4 if Yes |
| 25.1.4 | API documentation? | Yes / No | +3 if Yes |
| 25.1.5 | Video tutorials? | Yes / No | +5 per video |
| 25.1.6 | Documentation format? | Online / PDF / Wiki | |
| 25.1.7 | Documentation language? | | |

### 25.2 Training

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 25.2.1 | End-user training? | Yes / No | +4 if Yes |
| 25.2.2 | Admin training? | Yes / No | +3 if Yes |
| 25.2.3 | Training format? | In-person / Remote / Self-paced | |
| 25.2.4 | Training sessions count? | | +2 per session |
| 25.2.5 | Training materials? | Yes / No | +2 if Yes |
| 25.2.6 | Train-the-trainer? | Yes / No | +3 if Yes |

### 25.3 Knowledge Transfer

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 25.3.1 | Code repository handoff? | Yes / No | +1 if Yes |
| 25.3.2 | Deployment procedures? | Yes / No | +2 if Yes |
| 25.3.3 | Infrastructure documentation? | Yes / No | +2 if Yes |
| 25.3.4 | Credentials handoff? | Yes / No | +1 if Yes |
| 25.3.5 | Support contact transfer? | Yes / No | +1 if Yes |

---

## Section 26: Post-Launch Support

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 26.1 | Warranty period? | | |
| 26.2 | Warranty scope? | | |
| 26.3 | Support SLA? | | |
| 26.4 | Response time requirement? | | |
| 26.5 | Support hours? | Business / Extended / 24x7 | |
| 26.6 | Support channels? | Email / Phone / Chat / Ticket | |
| 26.7 | Escalation process? | | |
| 26.8 | Maintenance window? | | |
| 26.9 | Ongoing support contract? | Yes / No | |

---

## Section 27: Project Communication

| # | Question | Answer |
|---|----------|--------|
| 27.1 | Primary communication channel? | Email / Slack / Teams / Other |
| 27.2 | Meeting frequency? | Daily / Weekly / Bi-weekly |
| 27.3 | Meeting participants? | |
| 27.4 | Status report frequency? | |
| 27.5 | Status report format? | |
| 27.6 | Decision-making process? | |
| 27.7 | Change request process? | |
| 27.8 | Issue escalation path? | |
| 27.9 | Project management tool? | |
| 27.10 | File sharing platform? | |

---

## Section 28: Legal & Licensing

| # | Question | Answer | Complexity |
|---|----------|--------|------------|
| 28.1 | Terms of service needed? | Yes / No | +2 if Yes |
| 28.2 | Privacy policy needed? | Yes / No | +2 if Yes |
| 28.3 | Cookie policy needed? | Yes / No | +1 if Yes |
| 28.4 | Acceptable use policy? | Yes / No | +2 if Yes |
| 28.5 | EULA required? | Yes / No | +2 if Yes |
| 28.6 | Legal review required? | Yes / No | |
| 28.7 | Third-party licenses to track? | | +1 per license |
| 28.8 | Code ownership? | Client / Developer / Shared | |
| 28.9 | Open source usage rules? | | |
| 28.10 | NDA required? | Yes / No | |
| 28.11 | IP assignment? | | |

---

## Section 29: Budget & Licensing Costs

| # | Question | Answer |
|---|----------|--------|
| 29.1 | Third-party service costs? List: | |
| | - Service 1: | |
| | - Service 2: | |
| 29.2 | Hosting costs estimate? | |
| 29.3 | SSL certificate costs? | |
| 29.4 | Domain costs? | |
| 29.5 | Email service costs? | |
| 29.6 | SMS service costs? | |
| 29.7 | Payment gateway fees? | |
| 29.8 | Ongoing maintenance budget? | |
| 29.9 | Contingency budget? | |

---

## Section 30: Timeline & Phases

### 30.1 Phasing

| # | Question | Answer |
|---|----------|--------|
| 30.1.1 | Single release or phased? | |
| 30.1.2 | MVP / Phase 1 features? | |
| 30.1.3 | Phase 2 features? | |
| 30.1.4 | Phase 3 features? | |
| 30.1.5 | Future/backlog features? | |
| 30.1.6 | What can we cut if needed? | |
| 30.1.7 | What absolutely cannot be cut? | |
| 30.1.8 | Dependencies between phases? | |

### 30.2 Milestones

| # | Question | Answer |
|---|----------|--------|
| 30.2.1 | Key milestones? | |
| 30.2.2 | Hard deadlines? | |
| 30.2.3 | External dependencies? | |
| 30.2.4 | Content dependencies? | |
| 30.2.5 | Stakeholder availability? | |
| 30.2.6 | Holiday/blackout periods? | |

---

## Section 31: Risks & Concerns

| # | Question | Answer |
|---|----------|--------|
| 31.1 | What keeps you up at night? | |
| 31.2 | Past project failures? | |
| 31.3 | Biggest uncertainties? | |
| 31.4 | What would make this fail? | |
| 31.5 | What would make this succeed? | |
| 31.6 | Known technical risks? | |
| 31.7 | Known business risks? | |
| 31.8 | Stakeholder risks? | |
| 31.9 | Resource risks? | |
| 31.10 | External risks? | |

---

# PART G: SCORING & SIGN-OFF

## Section 32: Complexity Score Summary

| Category | Points |
|----------|--------|
| Section 2: Data Model | |
| Section 3: Business Rules | |
| Section 4: Users & Authentication | |
| Section 5: Views & Pages | |
| Section 6: Data Entry Forms | |
| Section 7: UX States | |
| Section 8: Navigation & Layout | |
| Section 9: Search & Discovery | |
| Section 10: Reporting & Analytics | |
| Section 11: Notifications | |
| Section 12: File Management | |
| Section 13: Import & Export | |
| Section 14: Integrations | |
| Section 15: Mobile & Responsive | |
| Section 16: Security & Compliance | |
| Section 17: Performance & Scalability | |
| Section 18: Deployment & Infrastructure | |
| Section 19: SEO & Marketing | |
| Section 20: Print & Offline | |
| Section 21: Design & Branding | |
| Section 22: Localization | |
| Section 23: Data Migration | |
| Section 24: Testing & UAT | |
| Section 25: Training & Handoff | |
| **TOTAL COMPLEXITY SCORE** | |

### Complexity-to-Effort Guide

| Score Range | Project Size | Typical Scope |
|-------------|--------------|---------------|
| 0-75 | Simple | Single feature, basic CRUD |
| 76-150 | Moderate | Small application, limited users |
| 151-300 | Complex | Full application, multiple roles |
| 301-500 | Very Complex | Enterprise application |
| 500+ | Program | Multi-phase, multi-team effort |

---

## Section 33: Sign-Off

By signing below, the client acknowledges:

1. All requirements captured accurately
2. Complexity score reviewed and understood
3. Features NOT listed are OUT OF SCOPE
4. Changes after sign-off require formal change request
5. Change requests may affect timeline and budget

| | Client | Developer |
|---|--------|-----------|
| Name | | |
| Title | | |
| Signature | | |
| Date | | |

---

## Section 34: Change Log

| Date | Version | Changed By | Description |
|------|---------|------------|-------------|
| | 2.0 | | Comprehensive expansion |

---

# APPENDICES

## Appendix A: Lessons Learned - GCC Database Solution

### A.1 The "Basic View" Trap
**What happened:** "Basic certificate view" became 3 views with infinite scroll, autocomplete, shareable URLs, database-driven filters, role-based filtering, bulk selection, ZIP downloads.
**Prevention:** Complete Section 5.2 for EVERY view without exception.

### A.2 Entity Relationship Complexity
**What happened:** Certificate → CertificateProductId (N:N), multiple product types (ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN), 100s of IDs per certificate.
**Prevention:** Section 2 must capture ALL entities and relationships upfront.

### A.3 Workflow State Explosion
**What happened:** "Simple status tracking" became 14-state workflow with transition rules, role-based permissions, and automated triggers.
**Prevention:** Draw the COMPLETE state diagram in Section 3.4 before coding.

### A.4 Role-Based Data Visibility
**What happened:** "External users see their data" required complex joins: Users ↔ Companies ↔ Certificates, Users ↔ Partners ↔ Certificates.
**Prevention:** Section 4.5 must detail EXACT visibility rules per role.

### A.5 Partner API Evolution
**What happened:** "Amazon API" became "Partner API" supporting multiple partners with individual API keys, usage tracking, and permission management.
**Prevention:** Section 14.1 must clarify if single or multi-partner from start.

### A.6 Design Iteration Cycles
**What happened:** Multiple rounds of modal styling, button placement, notification patterns, header structures - all after code was written.
**Prevention:** Get design sign-off via wireframes BEFORE coding (Section 21).

### A.7 Notification Pattern Inconsistency
**What happened:** Mix of native alerts, custom toasts, modals. Required standardization rework.
**Prevention:** Define notification standards in Section 11 upfront.

### A.8 Export Format Confusion
**What happened:** Different pages had different export formats (CSV, Excel, PDF) without clear rationale.
**Prevention:** Define export format per feature in Section 13.2.

### A.9 Mobile Assumptions
**What happened:** "Mobile support" assumed but not specified. Responsive issues discovered late.
**Prevention:** Section 15 requires explicit mobile requirements.

### A.10 Bulk Operations Scope
**What happened:** "Download certificates" evolved to "bulk select and download as ZIP" mid-project.
**Prevention:** Section 5.2.36-38 must list ALL bulk operations upfront.

---

## Appendix B: Red Flag Phrases

| Phrase | What It Usually Means | Action |
|--------|----------------------|--------|
| "Just a basic view" | Complex view with hidden requirements | Complete Section 5.2 |
| "Simple CRUD" | Workflows, validations, bulk ops, audit | Sections 3, 5, 13 |
| "Standard login" | 2FA, SSO, password policies, sessions | Section 4.2 |
| "Like [product]" | 100+ features, pick which ones | List specific features |
| "Users can filter" | 10+ filters, dynamic, saved, URL state | Section 5.2.16-24 |
| "Make it look nice" | Undefined design requirements | Section 21, get references |
| "Mobile-friendly" | Different layouts, touch, offline | Section 15 |
| "We might need..." | Scope creep incoming | In scope or out? Decide now |
| "Can we also..." | Change request | Formal CR process |
| "I assumed..." | Undocumented requirement | Document it now |
| "It should be obvious" | Hidden business logic | Section 3 |
| "Just like the old system" | Legacy complexity | Migration + feature parity |
| "Real-time" | WebSockets, scaling, complexity | Section 5.2.67, 11.2.2 |
| "For now" | Technical debt | Document future state |
| "Quick win" | Hidden dependencies | Full impact analysis |

---

## Appendix C: Question Categories by Priority

### Must Ask (Project Killers if Missed)
- Section 2: Data Model
- Section 3: Business Rules
- Section 4.1-4.2: User Roles & Auth
- Section 5.2: View Details (per view)
- Section 16.3: Compliance
- Section 30: Timeline & Phases

### Should Ask (Significant Rework if Missed)
- Section 3.4: Workflows
- Section 4.4-4.5: Multi-tenancy & Visibility
- Section 6.2: Form Details
- Section 14: Integrations
- Section 15: Mobile
- Section 23: Data Migration

### Nice to Have (Polish & Efficiency)
- Section 7: UX States
- Section 10: Dashboards
- Section 19: SEO
- Section 22: Localization

---

## Appendix D: Questionnaire Shortcuts

### For Small Projects (< 75 complexity)
Focus on: Sections 1, 2, 4.1-4.2, 5, 6, 30

### For Internal Tools
Skip: Sections 19 (SEO), 22 (Localization), most of 16.3 (Compliance)

### For MVPs
Focus on: Sections 1, 2, 4.1, 5.1, 6.1, 30 (Phase 1 only)
Mark everything else as "Phase 2+"

### For Redesigns
Add: Current pain points, what to keep, what to change
Skip: Section 23 if no data changes

---

*Document Version: 2.0*
*Created from lessons learned on GCC Database Solution project (6 months)*
*Designed to prevent scope creep and capture complete requirements upfront*
*Estimated completion time: 2-4 hours with client*
