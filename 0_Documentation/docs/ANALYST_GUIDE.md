# GCC Database Solution - Analyst User Guide

## Overview
This guide provides instructions for Analyst users of the GCC Database Solution. Analysts have read/write access to certificates and can perform most certificate management tasks, but do not have access to administrative functions like user management.

## Login and Access
- **URL**: http://localhost:5101/Account/Login
- **Default Credentials**: analyst@greencirclecertified.com / Analyst123!
- **Role**: Analyst (certificate management access)

## Available Features

### Certificate Management
Analysts can create, edit, and manage certificates within the system.

#### Upload Certificate
1. Navigate to "Create Certificate" from the main menu
2. Fill in required certificate information:
   - Certificate number (unique identifier)
   - Company selection or create new company
   - Product information and details
   - Certificate expiration date
   - Certification body information
3. Upload the certificate PDF file
4. Add product IDs:
   - ASINs for Amazon products
   - UPCs, GTINs, EANs for retail products
   - SKUs, MPNs for internal tracking
5. Review all information for accuracy
6. Save the certificate

#### Search Certificate
1. Use the search bar on the certificate tile view
2. Search by:
   - Certificate number
   - Company name
   - Product name or description
   - Product IDs (ASIN, UPC, etc.)
3. Use filters to narrow results:
   - Date range
   - Certificate status
   - Product type
   - Company
4. Click on certificate tiles to view details

#### Edit Certificate Information
1. Locate the certificate using search
2. Click "Edit" button on certificate details page
3. Modify any required fields:
   - Update product information
   - Add or remove product IDs
   - Change expiration dates
   - Update company association
4. Upload new certificate PDF if needed
5. Save changes

#### Manage Company Information
1. Access company management from certificate creation/editing
2. Create new companies:
   - Company name and legal information
   - Contact details and address
   - Primary contact person
3. Edit existing company information
4. Associate multiple certificates with companies

### Product ID Management
Efficiently manage product identifiers for certificates.

#### Add Product IDs
1. From certificate edit page, access Product ID Manager
2. Add individual product IDs:
   - Select product type (ASIN, UPC, GTIN, etc.)
   - Enter product identifier
   - Add product description if available
3. Bulk import from CSV:
   - Download CSV template
   - Fill in product ID information
   - Upload completed CSV file
   - Review and confirm imports

#### Export Data
1. Navigate to certificate list or search results
2. Select certificates to export
3. Choose export format:
   - **CSV**: For data analysis and reporting
   - **Excel**: For formatted reports and presentations
4. Download exported file
5. Use exported data for:
   - Reporting to management
   - Compliance documentation
   - Partner data sharing

### Reporting and Analytics

#### Generate Reports
1. Access reporting section from main dashboard
2. Available report types:
   - Certificate expiration reports
   - Company certificate summaries
   - Product ID analysis
   - Monthly/quarterly activity reports
3. Set parameters:
   - Date ranges
   - Specific companies or products
   - Certificate status filters
4. Generate and download reports

#### Monitor Certificate Status
1. View dashboard for certificate overview
2. Monitor upcoming expirations
3. Track certificate renewal requirements
4. Review certificate distribution by company

## Common Workflows

### New Certificate Processing
1. Receive certificate from certification body
2. Create new certificate entry in system
3. Associate with appropriate company
4. Add all relevant product IDs
5. Upload certificate PDF
6. Verify all information accuracy
7. Save and activate certificate

### Certificate Renewal
1. Monitor expiration dates from dashboard
2. Contact companies for renewal documentation
3. Update certificate with new expiration date
4. Upload renewed certificate PDF
5. Verify product IDs are still current
6. Update any changed information

### Partner Data Updates
1. Receive product ID updates from partners
2. Locate relevant certificates
3. Update product ID lists
4. Export updated data back to partners
5. Verify accuracy of changes

## Search and Navigation Tips

### Effective Searching
- Use partial matches for company names
- Search by certificate number for exact matches
- Combine multiple search terms for better results
- Use quotation marks for exact phrase matching

### Using Filters
- Apply date filters to find certificates by timeframe
- Filter by company to see all certificates for specific clients
- Use product type filters to focus on specific categories
- Combine filters for precise results

### Quick Actions
- Bookmark frequently accessed certificates
- Use browser back button to return to search results
- Right-click links to open in new tabs
- Use keyboard shortcuts for faster navigation

## Best Practices

### Data Quality
- Always verify certificate numbers are unique
- Ensure company information is accurate and complete
- Double-check product IDs for accuracy
- Review certificate PDFs match system information

### File Management
- Use consistent naming conventions for uploaded files
- Ensure PDF files are clear and readable
- Keep file sizes reasonable for system performance
- Maintain backup copies of important documents

### Communication
- Coordinate with team members on certificate updates
- Communicate changes to relevant stakeholders
- Document any special requirements or notes
- Follow up on expiration notifications

## Troubleshooting

### Common Issues

#### Cannot Find Certificate
1. Try different search terms
2. Check spelling of company or product names
3. Use broader date ranges
4. Contact administrator if certificate should exist

#### Upload Fails
1. Check file format (PDF required for certificates)
2. Verify file size (under 10MB limit)
3. Ensure file is not corrupted
4. Try uploading from different browser

#### Product ID Import Issues
1. Verify CSV format matches template
2. Check for special characters or formatting
3. Ensure all required columns are present
4. Review error messages for specific issues

### Getting Help
- Contact system administrator for technical issues
- Refer to training materials for process questions
- Ask team lead for workflow clarification
- Use help documentation for feature guidance

---

*Last Updated: September 2025*
*Version: 1.0*
*For additional support, contact your system administrator*