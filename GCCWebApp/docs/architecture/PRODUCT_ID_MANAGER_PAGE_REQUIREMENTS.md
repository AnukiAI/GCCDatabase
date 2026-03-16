# Product ID Manager Page - Requirements Document

**Document Version**: 1.0
**Created**: 2025-12-23
**Status**: Requirements Gathering
**Mockup Reference**: `5_2_Product.png`

---

## 1. Overview

### 1.1 Purpose
A standalone page to view and manage all Product IDs (ASINs, UPCs, GTINs, EANs) associated with certificates across the system. This provides a centralized view for Analysts and External Partners to see product identifier data.

### 1.2 Distinction from Existing Pages

| Page | Purpose | Data Source |
|------|---------|-------------|
| `/Product/Management` | Manage standalone Products | `Products` table |
| `_ProductIdManager.cshtml` | Manage Product IDs for a single certificate | `CertificateProductIds` table (filtered by certificate) |
| **NEW: Product ID Manager Page** | View/manage ALL certificate Product IDs | `CertificateProductIds` table (all records) |

### 1.3 Target Users
- **Analysts**: Full CRUD access
- **External Partners/Clients**: View-only access (filtered by their permissions)

---

## 2. Page Layout

### 2.1 Header Section

```
+--------------------------------------------------------------------------------+
| Product ID Manager                                         [Import] [Export]   |
+--------------------------------------------------------------------------------+
| [Search certificate types...]        [All Status v] [All Partners v] [+ Add]   |
+--------------------------------------------------------------------------------+
```

**Elements:**
- **Page Title**: "Product ID Manager"
- **Search Box**: Search by certificate type, product name, company, or identifier values
- **Filter Dropdowns**:
  - Status: All Status, Active, Inactive, Pending
  - Partners: All Partners, Amazon, Wayfair, Novi, Other
- **Action Buttons**:
  - Import (CSV) - Analyst only
  - Export (CSV) - All users
  - + Add Product - Analyst only

### 2.2 Table Columns

| # | Column | Width | Sortable | Description |
|---|--------|-------|----------|-------------|
| 1 | Checkbox | 40px | No | Multi-select for bulk operations |
| 2 | Certificate # | 100px | Yes | Certificate number (e.g., 24-1605) with link |
| 3 | Product Name | 180px | Yes | Name of the product |
| 4 | Company | 150px | Yes | Company name |
| 5 | Status | 90px | Yes | Active/Inactive/Pending badge |
| 6 | Partners | 120px | Yes | Partner badges (Amazon, Wayfair, NA) |
| 7 | ASIN | 120px | Yes | Amazon Standard Identification Number |
| 8 | EAN | 120px | Yes | European Article Number |
| 9 | GTIN | 120px | Yes | Global Trade Item Number |
| 10 | UPC | 120px | Yes | Universal Product Code |
| 11 | Date Created | 100px | Yes | Creation date |
| 12 | Created By | 100px | Yes | User who created the entry |
| 13 | Actions | 80px | No | Edit/Delete icons (Analyst only) |

### 2.3 Status Badges

| Status | Color | Badge Style |
|--------|-------|-------------|
| Active | Green (#92C83E) | Solid background, white text |
| Inactive | Gray (#6c757d) | Solid background, white text |
| Pending | Orange (#FFA500) | Solid background, white text |

### 2.4 Partner Badges

| Partner | Color | Badge Style |
|---------|-------|-------------|
| Amazon | Orange (#FF9900) | Solid background, white text |
| Wayfair | Purple (#7B2D8E) | Solid background, white text |
| Novi | Blue (#002B76) | Solid background, white text |
| NA (None) | Gray (#6c757d) | Solid background, white text |

**Note**: Multiple partners can be displayed per row (side by side badges).

### 2.5 Identifier Columns Display

**Multiple Values Format** (per decision 2025-12-23):

When a cell has multiple values:
- Show first value only
- Show "+X more" badge if additional values exist: `B08X7N9MRQ [+2 more]`
- On click: Expand to show all values in popup/dropdown
- Copy button to copy all values

**Example:**
```
| ASIN                    |
|-------------------------|
| B08X7N9MRQ [+2 more]    |  <- Click reveals: B08X7N9MRQ, B09ABC123, B10DEF456
| B094H2K8LM              |  <- Single value, no badge
```

### 2.6 Inline Add Row

At the bottom of the table, show an inline add row:

```
| [ ] | [24-1609] | [Product Name] | [Company] | [Select] | [x]Amazon [x]Wayfair | [ASIN] | [EAN] | [GTIN] | [UPC] | [Date] | Current User | [Save] [Cancel] |
```

**Fields:**
- Certificate #: Auto-generated or selectable dropdown
- Product Name: Text input (required)
- Company Name: Text input or dropdown
- Status: Dropdown (Active/Inactive/Pending)
- Partners: Checkboxes (Amazon, Wayfair, Other)
- ASIN, EAN, GTIN, UPC: Text inputs
- Date Created: Auto-populated
- Created By: Auto-populated with current user

### 2.7 Footer / Infinite Scroll

**No traditional pagination** - Use infinite scroll (consistent with Certificate Tile View):

```
Showing X of Y products                                          [Loading more...]
```

**Behavior:**
- Initial load: 25 items
- Load 25 more items when user scrolls to bottom
- Show "Loading..." spinner during fetch
- Show "No more items" when all loaded

---

## 3. Access Control

### 3.1 Role-Based Permissions

| Feature | Analyst | External Partner |
|---------|---------|------------------|
| View table | Yes | Yes (filtered) |
| Search/Filter | Yes | Yes |
| Sort columns | Yes | Yes |
| Export CSV | Yes | Yes |
| Import CSV | Yes | No |
| Add Product | Yes | No |
| Edit Product | Yes | No |
| Delete Product | Yes | No |
| Bulk operations | Yes | No |

### 3.2 External Partner Data Filtering

External Partners see ONLY records where:
- Certificate belongs to a company they're associated with, OR
- Certificate has a Partner value matching their partner association

**Example**: An Amazon partner user sees only records where Partners includes "Amazon".

### 3.3 UI Element Visibility

**Hidden for External Partners:**
- Import button
- + Add Product button
- Actions column (Edit/Delete)
- Checkbox column (no bulk operations)
- Inline add row

---

## 4. Bulk Operations

### 4.1 Multi-Select

- Checkbox in header row: Select/Deselect all visible rows
- Individual checkboxes per row
- Selected count displayed: "X items selected"

### 4.2 Bulk Actions (Analyst Only)

When items are selected, show bulk action bar:

```
+--------------------------------------------------------------------------------+
| X items selected          [Change Status v] [Delete Selected] [Clear Selection]|
+--------------------------------------------------------------------------------+
```

**Bulk Status Change:**
- Dropdown: Active, Inactive, Pending
- Confirmation modal before applying
- Toast notification after completion

**Bulk Delete:**
- Confirmation modal: "Are you sure you want to delete X items? This cannot be undone."
- Toast notification after completion

---

## 5. Search & Filtering

### 5.1 Search Behavior

Search across these fields:
- Certificate Number
- Product Name
- Company Name
- ASIN, EAN, GTIN, UPC values

**Autocomplete suggestions** categorized by:
- Certificates
- Products
- Companies
- Identifiers

### 5.2 Filter Combinations

Filters can be combined:
- Status AND Partners AND Search term
- Clear All button resets all filters

### 5.3 URL State

Filters should be reflected in URL for shareable links:
```
/Product/ProductIdManager?status=active&partner=amazon&search=LED
```

---

## 6. Import/Export

### 6.1 Export (CSV)

**Filename**: `product_ids_export_YYYY-MM-DD.csv`

**Columns**:
```csv
Certificate #,Product Name,Company,Status,Partners,ASIN,EAN,GTIN,UPC,Date Created,Created By
24-1605,EcoSmart LED Bulb,EcoTech Solutions,Active,"Amazon,Wayfair",B08X7N9MRQ,0846378985421,00846378985421,846378985421,2025-04-23,Sarah Johnson
```

### 6.2 Import (CSV)

**Template Columns**:
```csv
Certificate #,Product Name,Company,Status,Partners,ASIN,EAN,GTIN,UPC
```

**Import Preview Modal:**
- Show row-by-row preview
- Highlight validation errors
- Allow selection of rows to import
- Show: New, Modified, Unchanged, Error counts

---

## 7. API Endpoints

### 7.1 Required Endpoints

| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/api/ProductIdManager/List` | GET | Get paginated list | Analyst/External |
| `/api/ProductIdManager/Filters` | GET | Get filter options | Analyst/External |
| `/api/ProductIdManager/Create` | POST | Create new entry | Analyst |
| `/api/ProductIdManager/Update/{id}` | PUT | Update entry | Analyst |
| `/api/ProductIdManager/Delete/{id}` | DELETE | Delete entry | Analyst |
| `/api/ProductIdManager/BulkDelete` | POST | Delete multiple | Analyst |
| `/api/ProductIdManager/BulkStatusChange` | POST | Change status | Analyst |
| `/api/ProductIdManager/Export` | GET | Export CSV | Analyst/External |
| `/api/ProductIdManager/Import` | POST | Import CSV | Analyst |

### 7.2 List Endpoint Parameters

```
GET /api/ProductIdManager/List
  ?page=1
  &pageSize=25
  &search=LED
  &status=active
  &partner=amazon
  &sortBy=certificateNumber
  &sortDir=asc
```

---

## 8. Data Model

### 8.1 Existing Table: CertificateProductIds

```sql
CREATE TABLE CertificateProductIds (
    Id INT PRIMARY KEY IDENTITY,
    CertificateId INT NOT NULL FK,
    ProductIdType VARCHAR(20) NOT NULL,  -- ASIN, UPC, GTIN, EAN, etc.
    ProductIdValue VARCHAR(100) NOT NULL,
    ProductName VARCHAR(255),
    ProductSku VARCHAR(50),
    ProductDescription TEXT,
    Partner VARCHAR(50),  -- Amazon, Wayfair, Novi, None
    Status VARCHAR(20) DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT GETDATE(),
    CreatedBy VARCHAR(100),
    UpdatedAt DATETIME,
    UpdatedBy VARCHAR(100)
);
```

### 8.2 View Model for Display

```csharp
public class ProductIdManagerViewModel
{
    public int Id { get; set; }
    public string CertificateNumber { get; set; }
    public int CertificateId { get; set; }
    public string ProductName { get; set; }
    public string CompanyName { get; set; }
    public string Status { get; set; }
    public List<string> Partners { get; set; }  // Multiple partners
    public string Asin { get; set; }
    public string Ean { get; set; }
    public string Gtin { get; set; }
    public string Upc { get; set; }
    public DateTime DateCreated { get; set; }
    public string CreatedBy { get; set; }
}
```

### 8.3 Archive Table (per decision 2025-12-23)

```sql
CREATE TABLE ArchivedCertificateProductIds (
    Id INT PRIMARY KEY IDENTITY,
    OriginalId INT NOT NULL,
    CertificateId INT NOT NULL,
    ProductIdType VARCHAR(20) NOT NULL,
    ProductIdValue VARCHAR(100) NOT NULL,
    ProductName VARCHAR(255),
    ProductSku VARCHAR(50),
    ProductDescription TEXT,
    Partner VARCHAR(50),
    Status VARCHAR(20),
    OriginalCreatedAt DATETIME,
    OriginalCreatedBy VARCHAR(100),
    ArchivedAt DATETIME DEFAULT GETDATE(),
    ArchivedBy VARCHAR(100),
    ArchiveReason VARCHAR(255)
);
```

**Filter:** Users can filter by "Archived" to see archived items.
**Restore:** Admin can restore archived items back to main table.

### 8.4 Data Aggregation

Since `CertificateProductIds` stores one type per row, the API must aggregate:

**Raw Data:**
```
Id=1, CertId=100, Type=ASIN, Value=B08X7N9MRQ
Id=2, CertId=100, Type=UPC, Value=846378985421
Id=3, CertId=100, Type=EAN, Value=0846378985421
```

**Aggregated for Display:**
```
CertificateNumber: 24-1605
ASIN: B08X7N9MRQ
UPC: 846378985421
EAN: 0846378985421
```

---

## 9. UI/UX Considerations

### 9.1 Responsive Design

- On mobile: Hide some columns, show expandable detail view
- Minimum viewport: 768px for full table
- Below 768px: Card layout instead of table

### 9.2 Loading States

- Skeleton loader while fetching data
- Spinner on filter/search actions
- Disabled buttons during operations

### 9.3 Error Handling

- Toast notifications for errors
- Inline validation messages
- Retry button on network errors

### 9.4 Accessibility

- Keyboard navigation for table
- Screen reader announcements
- Focus management in modals
- ARIA labels on all controls

---

## 10. Implementation Notes

### 10.1 Files to Create

| File | Purpose |
|------|---------|
| `Views/Product/ProductIdManager.cshtml` | Main page view |
| `wwwroot/js/product-id-manager-page.js` | Page JavaScript |
| `wwwroot/css/product-id-manager-page.css` | Page styles |
| `Controllers/ProductIdManagerController.cs` | MVC Controller |
| `Controllers/Api/ProductIdManagerApiController.cs` | API Controller |

### 10.2 Reusable Components

Consider reusing from existing `_ProductIdManager.cshtml`:
- Validation logic
- Import preview modal
- Export CSV generation
- Partner badge styling

### 10.3 Navigation

Add to application menu:
- Location: Under "Companies & Products" section
- Menu item: "Product ID Manager"
- Route: `/Product/ProductIdManager`

---

## 11. Open Questions - RESOLVED

| # | Question | Answer | Decision Date |
|---|----------|--------|---------------|
| 1 | Should inline add create a new Certificate if number doesn't exist? | **Show Error** - Display error message, user must select existing certificate | 2025-12-23 |
| 2 | How to handle multiple values per identifier type (e.g., 3 ASINs)? | **First value with badge** - Show first ASIN with "+2 more" badge, expandable on click | 2025-12-23 |
| 3 | Should Partners support "Other" with custom value? | **Admin-Managed List** - Partners managed in Admin section, dropdown shows active partners only | 2025-12-23 |
| 4 | Pagination: Fixed page size or user-selectable? | **Infinite Scroll** - Load more as user scrolls (consistent with Certificate Tile View) | 2025-12-23 |
| 5 | Should deleted items be soft-deleted or hard-deleted? | **Archive** - Move to archive table, visible in 'Archived' filter for recovery | 2025-12-23 |

---

## 12. Acceptance Criteria

### 12.1 Must Have (MVP)
- [ ] Table displays all certificate product IDs
- [ ] Separate columns for ASIN, EAN, GTIN, UPC
- [ ] Status and Partner badges with colors
- [ ] Search and filter functionality
- [ ] Export to CSV
- [ ] Role-based access control
- [ ] External user data filtering

### 12.2 Should Have
- [ ] Inline add row
- [ ] Multi-select checkboxes
- [ ] Bulk status change
- [ ] Bulk delete
- [ ] Import from CSV
- [ ] Shareable filter URLs

### 12.3 Nice to Have
- [ ] Column resizing
- [ ] Column visibility toggle
- [ ] Saved filter presets
- [ ] Real-time updates

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-23 | Claude | Initial requirements document |
