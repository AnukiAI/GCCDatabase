# Certificate Field Management - Requirements & Flows

## Overview
The Certificate Field Management page allows Admins to configure and manage custom fields that appear on certificates. This provides flexibility to add certification-specific attributes without modifying the core database schema.

**Route:** `/Admin/CertificateFields`
**Access:** Admin only
**Mockup:** `0_Documentation/mockups/7_5_1_CertificateFieldManagement.html`

---

## Business Requirements

### Purpose
- **Configuration Management**: Enable admins to define custom fields for different certification types
- **Data Collection**: Capture certification-specific framework (CSF) attributes and other variable data
- **Flexibility**: Support future certification types without code changes
- **Validation**: Enforce data quality through field-level validation rules

### User Roles
- **Admin**: Full CRUD access to certificate fields
- **Analyst**: Read-only view (optional, not in mockup)
- **External/Public**: No access

---

## Functional Requirements

### FR-1: View Certificate Fields
**As an** Admin
**I want to** view all certificate fields in a table
**So that** I can see what fields are currently configured

**Acceptance Criteria:**
- Display all fields in a searchable/filterable table
- Group fields by section (Core Certificate Information, Company Information, Product Information, etc.)
- Show field metadata: Section, Field Name, Control Type, Required status, Notes
- Display field count in footer ("Showing X of Y fields")
- Default sort: Section alphabetically, then by display order

**Columns:**
1. **Section** (15% width) - Grouping category
2. **Field Name** (18% width) - Display name of the field
3. **Control Type** (12% width) - UI control (Text, Dropdown, Multi-Select, Date, File, etc.)
4. **Required** (12% width) - Required/Optional/Auto-generated
5. **Notes** (28% width) - Description/help text
6. **Actions** (15% width) - Edit/Delete/Reorder buttons

---

### FR-2: Search/Filter Fields
**As an** Admin
**I want to** search certificate fields
**So that** I can quickly find specific fields

**Acceptance Criteria:**
- Search box filters by: Field Name, Section, Control Type, Notes
- Real-time filtering (as you type)
- Case-insensitive search
- Search icon visible in search box
- Clear search functionality

---

### FR-3: Add New Certificate Field
**As an** Admin
**I want to** add custom certificate fields
**So that** I can capture additional certification-specific data

**Trigger:** Click "Add New Field" button (green, top-right)

**Add Field Modal/Form Fields:**
1. **Section** (Dropdown, Required)
   - Options: Core Certificate Information, Company Information, Product Information, Product Identifiers, Tagging & Classification, File Management, Record Management, Custom
   - Or free text to create new section

2. **Field Name** (Text, Required)
   - Max 100 characters
   - Must be unique within certification type
   - Auto-generates database column name (sanitized)

3. **Certification Type** (Dropdown, Required)
   - Which certification type(s) this field applies to
   - Options: All, Certified Environmental Facts, Sustainability Facts, Recycled Content, etc.
   - Multi-select capability

4. **Control Type** (Dropdown, Required)
   - Text (single line)
   - Textarea (multi-line)
   - Dropdown (single select)
   - Multi-Select
   - Date
   - Number
   - Checkbox (Boolean)
   - File Upload
   - System Generated (read-only, auto-populated)

5. **Required** (Radio, Required)
   - Required
   - Optional
   - Auto-generated (system fills automatically)

6. **Default Value** (Text, Optional)
   - Pre-populate field with this value on certificate creation

7. **Validation Rules** (Optional)
   - Min/Max length (for text)
   - Min/Max value (for numbers)
   - Date range restrictions
   - Regex pattern
   - Custom validation message

8. **Options** (for Dropdown/Multi-Select only)
   - Comma-separated or line-by-line list of options
   - Example: "Option 1, Option 2, Option 3"

9. **Display Order** (Number, Optional)
   - Sort order within section (default: 999)

10. **Visibility** (Checkboxes)
    - ☑ Show on Create form
    - ☑ Show on Edit form
    - ☑ Show on Details/Single view
    - ☑ Show to Public (External users)
    - ☑ Show in Tile view

11. **Notes/Help Text** (Textarea, Optional)
    - Max 500 characters
    - Displayed as tooltip or help text near field

**Validation:**
- Field Name must be unique per certification type
- Control Type must match data type (e.g., can't have "Dropdown" with no options)
- Required fields cannot have blank default values

**Success:**
- Show success message: "Field '[Field Name]' added successfully"
- Refresh table to show new field
- Close modal

**Errors:**
- Display validation errors inline
- "Field name already exists for this certification type"
- "Options are required for Dropdown/Multi-Select fields"

---

### FR-4: Edit Certificate Field
**As an** Admin
**I want to** edit existing certificate fields
**So that** I can update field configuration as requirements change

**Trigger:** Click "Edit" icon in Actions column

**Edit Field Modal:**
- Same form as Add Field
- Pre-populated with existing values
- Cannot change Certification Type if field has existing data (show warning)
- Cannot change Control Type if it would cause data loss (e.g., Dropdown → Number)

**Validation:**
- Same as Add Field
- Additional: "This field has X existing values. Changing the control type may result in data loss."

**Success:**
- Show success message: "Field '[Field Name]' updated successfully"
- Refresh table

---

### FR-5: Delete Certificate Field
**As an** Admin
**I want to** delete certificate fields
**So that** I can remove unused or deprecated fields

**Trigger:** Click "Delete" icon in Actions column

**Confirmation Modal:**
```
Delete Field: [Field Name]

⚠️ Warning: This will permanently delete this field and all associated data.

• [X] certificates currently use this field
• This action cannot be undone

Are you sure you want to delete this field?

[Cancel] [Delete Field]
```

**Business Rules:**
- System/core fields cannot be deleted (Certificate #, Status, Effective Date, etc.)
- Show count of certificates using this field
- Require additional confirmation if > 10 certificates use the field
- Soft delete preferred (mark IsDeleted=true) to preserve data

**Success:**
- Show success message: "Field '[Field Name]' deleted successfully"
- Remove from table

**Errors:**
- "Cannot delete system field"
- "Cannot delete field: referenced by X certificates"

---

### FR-6: Reorder Fields
**As an** Admin
**I want to** change field display order
**So that** fields appear in logical sequence on forms

**Implementation Options:**

**Option A: Drag-and-Drop** (Preferred)
- Drag field row to new position
- Auto-renumber SortOrder values
- Show visual indicator during drag
- Save immediately on drop

**Option B: Up/Down Arrows**
- Up/Down arrow buttons in Actions column
- Move field one position at a time
- Save immediately on click

**Business Rules:**
- Can only reorder within same section
- System fields maintain fixed positions

---

## Current Field Inventory

Based on your list and the mockup, here are the certificate fields:

### Core Certificate Information
1. **Certificate #** - Text (Optional) - Auto-generated if blank
2. **Certification Type** - Dropdown (Required) - Type of certification
3. **CSF Attributes** - Multi-Select (Optional) - Certification framework attributes
4. **Status** - Dropdown (Required) - Lifecycle status (14 states)
5. **Effective Date** - Date (Required) - When certification becomes effective
6. **Expiration Date** - Date (Required) - When certification expires

### Company Information
7. **Company** - Dropdown/Autocomplete (Required) - Company receiving certification
8. **Company Contact Email** - Text (Optional) - Email contact
9. **Company Contact URL** - Text (Optional) - Website URL

### Product Information
10. **Product Name** - Text (Required) - Name of certified product
11. **Product Category** - Dropdown (Required) - Main category
12. **Product Type** - Dropdown (Required) - Specific type within category
13. **Product Type Subcategory** - Dropdown (Required) - Further classification
14. **Location** - Text (Optional) - Manufacturing/certification location
15. **Country** - Dropdown (Required) - Country of origin

### Product Identifiers
16. **ASIN** - Text (Optional) - Amazon Standard Identification Number
17. **UPC** - Text (Optional) - Universal Product Code
18. **GTIN** - Text (Optional) - Global Trade Item Number
19. **EAN** - Text (Optional) - European Article Number

### Tagging & Classification
20. **External Tag** - Multi-Select (Optional) - External tags
21. **Internal Tag** - Multi-Select (Optional) - Internal tags

### Partner Information
22. **Partner** - Multi-Select (Required) - Amazon/Wayfair/None/Other

### File Management
23. **Certificate Upload** - File (Optional) - Upload PDF certificate
24. **Preview** - System Generated (Auto) - Certificate image/thumbnail

### Record Management (Auto-generated)
25. **Record Created Date** - Date (Auto) - Creation timestamp
26. **Record Last Updated Date** - Date (Auto) - Last modification timestamp

---

## Data Model

### Existing Models (✅ Already Implemented)

**CustomField** (`Models/CustomField.cs`)
```csharp
public class CustomField
{
    public int Id { get; set; }
    public string Name { get; set; }              // Field Name
    public string FieldType { get; set; }         // text/number/date/boolean/select
    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; }
    public bool IsPublic { get; set; }
    public int SortOrder { get; set; }
    public string? Options { get; set; }          // JSON for dropdown options
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }  // JSON validation
    public int CertificateTypeId { get; set; }

    // Navigation
    public CertificationType CertificationType { get; set; }
    public ICollection<CustomFieldValue> CustomFieldValues { get; set; }
}
```

**CustomFieldValue** (`Models/CustomFieldValue.cs`)
```csharp
public class CustomFieldValue
{
    public int Id { get; set; }
    public int CertificateId { get; set; }
    public int FieldId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public bool? BooleanValue { get; set; }

    // Navigation
    public Certificate Certificate { get; set; }
    public CustomField Field { get; set; }
}
```

### Required Enhancements

Add to **CustomField** model:
```csharp
[MaxLength(100)]
public string? Section { get; set; }              // Grouping (Core, Company, Product, etc.)

[MaxLength(500)]
public string? Notes { get; set; }                // Help text/description

public bool ShowOnCreateForm { get; set; } = true;
public bool ShowOnEditForm { get; set; } = true;
public bool ShowOnDetailsView { get; set; } = true;
public bool ShowInTileView { get; set; } = false;

public bool IsSystemField { get; set; } = false;  // Prevents deletion
public bool IsDeleted { get; set; } = false;      // Soft delete

public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime? UpdatedAt { get; set; }
public int? CreatedBy { get; set; }               // User ID
public int? UpdatedBy { get; set; }               // User ID
```

---

## Technical Implementation

### Controller: AdminController
**Route:** `/Admin/CertificateFields`

**Actions Needed:**
```csharp
// GET: /Admin/CertificateFields
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> CertificateFields()

// GET: /Admin/CertificateFields/GetFields
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> GetFields(string? search = null, int? certificationTypeId = null)

// POST: /Admin/CertificateFields/Create
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> CreateField(CustomFieldViewModel model)

// POST: /Admin/CertificateFields/Edit/{id}
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> EditField(int id, CustomFieldViewModel model)

// POST: /Admin/CertificateFields/Delete/{id}
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> DeleteField(int id)

// POST: /Admin/CertificateFields/Reorder
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> ReorderFields(List<FieldOrderViewModel> fields)
```

### View: `/Views/Admin/CertificateFields.cshtml`

**Layout:**
- Header with title and "Add New Field" button
- Search/filter bar
- Responsive table (desktop) / cards (mobile)
- Modal for Add/Edit field
- Confirmation modal for delete

**JavaScript:**
- Real-time search filtering
- AJAX for CRUD operations
- Drag-and-drop reordering
- Form validation
- Toast notifications for success/errors

---

## User Flows

### Flow 1: Add New Field

```
1. Admin navigates to /Admin/CertificateFields
2. Page loads with existing fields table
3. Admin clicks "Add New Field" button
4. Modal opens with Add Field form
5. Admin fills in:
   - Section: "Product Information"
   - Field Name: "Product Weight"
   - Certification Type: "All"
   - Control Type: "Number"
   - Required: "Optional"
   - Validation Rules: Min=0, Max=10000
   - Notes: "Weight in pounds"
6. Admin clicks "Save"
7. System validates:
   - Field name is unique ✓
   - Control type matches data type ✓
   - Validation rules are valid JSON ✓
8. System saves CustomField record
9. Modal closes
10. Success toast: "Field 'Product Weight' added successfully"
11. Table refreshes showing new field
12. Field now appears on certificate Create/Edit forms
```

### Flow 2: Edit Existing Field

```
1. Admin searches for "ASIN"
2. Table filters to show ASIN field
3. Admin clicks Edit icon for ASIN
4. Modal opens pre-filled with ASIN field data
5. Admin changes:
   - Required: Optional → Required
   - Notes: "Amazon Standard Identification Number - Required for Amazon partners"
6. Admin clicks "Update"
7. System checks for data impact:
   - 156 certificates have ASIN values ✓
   - No data loss from this change ✓
8. System updates CustomField record
9. System logs audit trail (UpdatedBy, UpdatedAt)
10. Modal closes
11. Success toast: "Field 'ASIN' updated successfully"
12. Table refreshes
13. ASIN field now shows "Required" in table
```

### Flow 3: Delete Unused Field

```
1. Admin finds "Obsolete Field X"
2. Admin clicks Delete icon
3. Confirmation modal opens:
   "Delete Field: Obsolete Field X
    ⚠️ Warning: 0 certificates use this field
    This action cannot be undone.
    [Cancel] [Delete Field]"
4. Admin clicks "Delete Field"
5. System soft-deletes field (IsDeleted=true)
6. Modal closes
7. Success toast: "Field deleted successfully"
8. Table refreshes (field removed)
```

### Flow 4: Delete Field with Data

```
1. Admin tries to delete "Product Name"
2. Admin clicks Delete icon
3. Confirmation modal opens:
   "Delete Field: Product Name
    ⚠️ Warning: This is a SYSTEM FIELD
    • 1,234 certificates use this field
    • This action cannot be undone
    [Cancel] [Delete Field]"
4. Admin clicks "Delete Field"
5. System validates:
   - IsSystemField = true → BLOCKED
6. Error modal: "Cannot delete system field 'Product Name'"
7. Delete operation cancelled
```

---

## Edge Cases & Business Rules

### 1. System Fields Protection
**Fields that cannot be deleted:**
- Certificate #
- Certification Type
- Status
- Effective Date
- Expiration Date
- Company
- Product Name
- Record Created Date
- Record Last Updated Date

**Implementation:**
- Mark these fields with `IsSystemField = true`
- Hide delete button for system fields
- Show lock icon instead

### 2. Data Type Changes
**Allowed:**
- Text → Textarea (preserve data)
- Number → Text (convert to string)
- Optional → Required (if all existing values are filled)

**Not Allowed:**
- Dropdown → Number (would lose data)
- Date → Boolean (incompatible types)
- Required → Optional (always allowed)

**Implementation:**
- Check compatibility matrix before allowing change
- Show warning if data loss possible
- Require explicit confirmation

### 3. Field with Existing Data
When editing field used by certificates:
- Show count: "This field is used by 456 certificates"
- Warn about impact of changes
- Consider migration strategy for breaking changes

### 4. Dropdown Options Modification
When editing dropdown options:
- New options: Safe to add
- Removing options: Check if any certificates use removed option
- Warn: "Option 'X' is used by 23 certificates. Remove anyway?"

---

## UI/UX Specifications

### Colors (Master Design Guide)
- **Primary Blue**: #002B76 (headers)
- **Action Green**: #92C83E (Add button)
- **Secondary Green**: #447638 (hover states)
- **Text**: #333333
- **Border**: #EAEAEA
- **Background**: #F9F9F9

### Typography
- **Font**: Century Gothic
- **H1**: 24px, weight 500
- **Body**: 16px
- **Table headers**: 15px, weight 500

### Buttons
- **Primary (Add New Field)**: Green background, white text
- **Edit**: Blue icon, no background
- **Delete**: Red icon, no background
- **Hover**: Subtle background change

### Table
- **Row hover**: Light gray background (#F5F5F5)
- **Alternating rows**: Optional zebra striping
- **Column alignment**: Left for text, center for actions
- **Responsive**: Stack on mobile (<768px)

### Modals
- **Overlay**: Semi-transparent black (#00000080)
- **Modal**: White, centered, max-width 600px
- **Close**: X button top-right
- **Footer buttons**: Right-aligned, Cancel (gray), Save (green)

---

## Testing Requirements

### Unit Tests
- CustomField CRUD operations
- Validation rules enforcement
- Soft delete functionality
- Field reordering logic

### Integration Tests
- Add field → appears on Create form
- Edit field → updates on all forms
- Delete field → removes from UI
- System field protection

### UAT Test Cases
1. Add text field, verify on Create form
2. Add dropdown with options, verify options display
3. Make optional field required, verify validation
4. Delete unused field, verify removal
5. Try to delete system field, verify error
6. Reorder fields, verify new order persists
7. Search fields by name, verify results
8. Edit field notes, verify tooltip updates

---

## Migration Strategy

### Phase 1: Core Fields (Already Done)
- ✅ Database models exist (CustomField, CustomFieldValue)
- ✅ Core certificate fields hardcoded in Create/Edit forms

### Phase 2: Dynamic Field Rendering
- Read CustomField records from database
- Render form fields dynamically based on CustomField configuration
- Apply validation rules from CustomField.ValidationRules

### Phase 3: Field Management UI
- Implement /Admin/CertificateFields page
- CRUD operations for CustomField
- Search/filter/reorder functionality

### Phase 4: Migration of Hardcoded Fields
- Create CustomField records for existing hardcoded fields
- Mark as IsSystemField=true
- Migrate to dynamic rendering

---

## Dependencies

**Models:** ✅ Already exist
- `CustomField.cs`
- `CustomFieldValue.cs`

**Needs Creation:**
- `CustomFieldViewModel.cs` (for forms)
- `FieldOrderViewModel.cs` (for reordering)

**Controller:** Partial
- `AdminController.cs` exists
- Needs CertificateFields actions

**View:** ❌ Needs creation
- `/Views/Admin/CertificateFields.cshtml`

**JavaScript:** ❌ Needs creation
- `/wwwroot/js/certificate-field-management.js`

**CSS:** ✅ Can reuse
- Master Design Guide styles
- Table styles from existing admin pages

---

## Acceptance Criteria (Overall)

**This feature is COMPLETE when:**

1. ✅ Admin can view all certificate fields in a table
2. ✅ Admin can search/filter fields by name, section, type
3. ✅ Admin can add new custom fields with all configuration options
4. ✅ Admin can edit existing fields (with data impact warnings)
5. ✅ Admin can delete non-system fields (with confirmation)
6. ✅ Admin can reorder fields within sections
7. ✅ System fields are protected from deletion
8. ✅ New fields automatically appear on Create/Edit forms
9. ✅ Field validation rules are enforced on certificate forms
10. ✅ Audit trail tracks all field changes (CreatedBy, UpdatedBy, timestamps)
11. ✅ Mobile responsive design
12. ✅ All CRUD operations have success/error feedback
13. ✅ Unit tests cover all business logic
14. ✅ UAT tests verify end-to-end functionality

---

## Priority & Timeline

**Priority:** Medium (Phase 2 feature)
**Complexity:** Medium
**Estimated Effort:** 16-24 hours
**Dependencies:** CustomField models already exist

**Breakdown:**
- Backend (8-10 hours):
  - AdminController actions (3 hours)
  - ViewModels (1 hour)
  - Service layer for field management (2 hours)
  - Validation logic (2 hours)
  - Unit tests (2 hours)

- Frontend (8-10 hours):
  - CertificateFields.cshtml view (3 hours)
  - Add/Edit modal (2 hours)
  - JavaScript (search, AJAX, drag-drop) (3 hours)
  - CSS/responsive design (1 hour)
  - Integration testing (1 hour)

- Documentation (2-4 hours):
  - User guide
  - Admin training materials
  - API documentation

---

## Open Questions

1. **Should Analysts have read-only access** to view field configuration?
   - Current design: Admin only
   - Consider: Analyst view-only for documentation purposes

2. **How to handle field name changes?**
   - Option A: Don't allow (preserve data integrity)
   - Option B: Allow with warning + data migration

3. **Multi-language support?**
   - Field labels in different languages?
   - Future consideration

4. **Version history for field changes?**
   - Track all modifications to field configuration?
   - Could be useful for compliance/audit

5. **Import/Export field configurations?**
   - For backup or migration between environments?
   - JSON export of all CustomField records?

---

**Last Updated:** October 6, 2025
**Status:** Requirements Documented - Ready for Implementation
**Mockup:** `/0_Documentation/mockups/7_5_1_CertificateFieldManagement.html`
