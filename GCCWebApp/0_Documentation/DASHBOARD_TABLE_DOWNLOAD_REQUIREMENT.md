# Dashboard Table Download Requirement

**Priority:** HIGH
**Date Created:** 2025-10-25
**Status:** Required - Not Yet Implemented

## Overview

Each table on the Dashboard page (http://localhost:5101/Dashboard) has a download icon button. Currently, these buttons may only download the summary table data. **They must download the full certificate details for all certificates represented in that table's statistics.**

## Affected Tables

### 1. Total Active Certs by Certification Type
- **Table Location:** Dashboard > "Total Active Certs by Certification Type"
- **Download Button:** `data-table-type="cert-types"`
- **Current Behavior:** May download summary (e.g., FSC: 100, PEFC: 50)
- **Required Behavior:** Download ALL certificates of those certification types
- **Export Format:** CSV
- **Columns to Include:**
  - Certificate Number
  - Certificate Name
  - Certification Type
  - Company Name
  - Product Name
  - Issue Date
  - Expiration Date
  - Status
  - Certificate PDF URL (if available)

### 2. Top 10 Companies by Active Cert Count
- **Table Location:** Dashboard > "Top 10 Companies by Active Cert Count"
- **Download Button:** `data-table-type="top-companies"`
- **Current Behavior:** May download summary (e.g., Company A: 50, Company B: 30)
- **Required Behavior:** Download ALL certificates for those top 10 companies
- **Export Format:** CSV
- **Columns to Include:**
  - Certificate Number
  - Certificate Name
  - Company Name
  - Product Name
  - Certification Type
  - Issue Date
  - Expiration Date
  - Status
  - Certificate PDF URL (if available)

### 3. Certs by Status
- **Table Location:** Dashboard > "# Certs by Status"
- **Download Button:** `data-table-type="cert-status"`
- **Current Behavior:** May download summary (e.g., Active: 500, Expired: 50)
- **Required Behavior:** Download ALL certificates grouped by status
- **Export Format:** CSV
- **Columns to Include:**
  - Certificate Number
  - Certificate Name
  - Status
  - Company Name
  - Product Name
  - Certification Type
  - Issue Date
  - Expiration Date
  - Certificate PDF URL (if available)

### 4. Top 10 Product Types by Active Cert Count
- **Table Location:** Dashboard > "Top 10 Product Types by Active Cert Count"
- **Download Button:** `data-table-type="product-types"`
- **Current Behavior:** May download summary (e.g., Flooring: 100, Furniture: 80)
- **Required Behavior:** Download ALL certificates for those top 10 product types
- **Export Format:** CSV
- **Columns to Include:**
  - Certificate Number
  - Certificate Name
  - Product Type
  - Product Name
  - Company Name
  - Certification Type
  - Issue Date
  - Expiration Date
  - Status
  - Certificate PDF URL (if available)

### 5. Top 5 Product Categories by Active Cert Count
- **Table Location:** Dashboard > "Top 5 Product Categories by Active Cert Count"
- **Download Button:** `data-table-type="product-categories"`
- **Current Behavior:** May download summary
- **Required Behavior:** Download ALL certificates for those top 5 product categories
- **Export Format:** CSV
- **Columns to Include:**
  - Certificate Number
  - Certificate Name
  - Product Category
  - Product Name
  - Company Name
  - Certification Type
  - Issue Date
  - Expiration Date
  - Status
  - Certificate PDF URL (if available)

## Implementation Requirements

### Backend API Endpoints

Create new controller endpoints in `DashboardController.cs`:

```csharp
[HttpGet]
[Route("Dashboard/DownloadCertificationTypeData")]
public async Task<IActionResult> DownloadCertificationTypeData()
{
    // Get all certificates grouped by certification type
    // Return CSV with full certificate details
}

[HttpGet]
[Route("Dashboard/DownloadTopCompaniesData")]
public async Task<IActionResult> DownloadTopCompaniesData()
{
    // Get all certificates for top 10 companies
    // Return CSV with full certificate details
}

[HttpGet]
[Route("Dashboard/DownloadStatusData")]
public async Task<IActionResult> DownloadStatusData()
{
    // Get all certificates grouped by status
    // Return CSV with full certificate details
}

[HttpGet]
[Route("Dashboard/DownloadProductTypesData")]
public async Task<IActionResult> DownloadProductTypesData()
{
    // Get all certificates for top 10 product types
    // Return CSV with full certificate details
}

[HttpGet]
[Route("Dashboard/DownloadProductCategoriesData")]
public async Task<IActionResult> DownloadProductCategoriesData()
{
    // Get all certificates for top 5 product categories
    // Return CSV with full certificate details
}
```

### Frontend JavaScript

Update `wwwroot/js/dashboard.js` to handle table download button clicks:

```javascript
// Table download handlers
document.querySelectorAll('.table-download-icon').forEach(btn => {
    btn.addEventListener('click', function(e) {
        e.preventDefault();
        const tableType = this.getAttribute('data-table-type');

        switch(tableType) {
            case 'cert-types':
                window.location.href = '/Dashboard/DownloadCertificationTypeData';
                break;
            case 'top-companies':
                window.location.href = '/Dashboard/DownloadTopCompaniesData';
                break;
            case 'cert-status':
                window.location.href = '/Dashboard/DownloadStatusData';
                break;
            case 'product-types':
                window.location.href = '/Dashboard/DownloadProductTypesData';
                break;
            case 'product-categories':
                window.location.href = '/Dashboard/DownloadProductCategoriesData';
                break;
        }
    });
});
```

## File Naming Convention

- **Format:** `{table-name}_certificates_{YYYY-MM-DD}.csv`
- **Examples:**
  - `certification_type_certificates_2025-10-25.csv`
  - `top_companies_certificates_2025-10-25.csv`
  - `status_certificates_2025-10-25.csv`
  - `product_types_certificates_2025-10-25.csv`
  - `product_categories_certificates_2025-10-25.csv`

## User Experience

1. **Click download icon** on any table header
2. **No popup/modal** - direct CSV download
3. **Loading indicator** - Show loading state on button during download
4. **Success notification** - Toast message: "Certificates exported to CSV successfully!"
5. **File downloads immediately** - Browser's native download

## Data Scope

- **Only Active Certificates** for tables showing "Active Cert Count"
- **All Statuses** for the "Certs by Status" table (includes Active, Expired, Expiring Soon)
- **Respect User Permissions:**
  - External users: Only their company's certificates
  - Analyst users: All certificates
  - Admin users: All certificates

## Notes

- This is distinct from the stat card downloads (which have their own download icons)
- This is also distinct from the three action buttons at the bottom ("Download Expiring Certs Report", etc.)
- CSV is the ONLY export format (no Excel/JSON/PDF)
- Must include all certificate details, not just summary counts

## Related Requirements

- See `0_Documentation/docs/Requirements.md` for overall export requirements
- CSV-only export requirement applies across all admin pages
- Follows same export pattern as Tags, Users, Products, etc.

## Acceptance Criteria

- [ ] Clicking table download icon downloads CSV with full certificate data
- [ ] No popup/modal appears - direct download
- [ ] CSV includes all required columns for each table type
- [ ] File naming follows convention: `{table-name}_certificates_{date}.csv`
- [ ] Loading state shows while download is processing
- [ ] Success toast notification appears after download
- [ ] Works for all 5 dashboard tables
- [ ] Respects user role permissions (External vs Internal)
- [ ] CSV data is properly formatted (quotes, escaping, headers)
