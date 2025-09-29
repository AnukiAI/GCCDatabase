# GCC Database Solution - Common Tasks Training Guide

## Overview
This training guide covers the most frequently performed tasks in the GCC Database Solution. Each task includes step-by-step instructions, tips for efficiency, and troubleshooting guidance.

## Upload Certificate

### Prerequisites
- User must have Analyst or Admin role
- Certificate PDF file ready (under 10MB)
- Company information available
- Product details and IDs prepared

### Step-by-Step Process

#### 1. Navigate to Certificate Creation
1. Log in to the system
2. Click "Create Certificate" from the main menu
3. Or navigate directly to `/Certificate/Create`

#### 2. Enter Basic Information
1. **Certificate Number**: Enter unique identifier
   - Use format: YY-NNNN (e.g., 24-0001)
   - System will verify uniqueness
2. **Certificate Title**: Descriptive name for the certificate
3. **Certification Date**: Date certificate was issued
4. **Expiration Date**: When certificate expires
5. **Certification Body**: Organization that issued certificate

#### 3. Company Association
1. **Existing Company**: Select from dropdown
2. **New Company**: Click "Create New Company"
   - Enter company name and details
   - Add contact information
   - Save company information
3. Associate certificate with selected company

#### 4. Product Information
1. **Product Name**: Full product name/description
2. **Product Type**: Select appropriate category
3. **Product Details**: Additional specifications
4. **Standards**: Certification standards met

#### 5. Upload Certificate File
1. Click "Choose File" button
2. Select PDF file from your computer
3. Verify file uploads successfully
4. Preview uploaded document if needed

#### 6. Add Product IDs
1. Access Product ID Manager section
2. **Single Entry**: Add IDs one at a time
   - Select ID type (ASIN, UPC, GTIN, etc.)
   - Enter product identifier
   - Add description if needed
3. **Bulk Import**: Use CSV upload
   - Download CSV template
   - Fill in product ID information
   - Upload completed file
   - Review import results

#### 7. Review and Save
1. Review all entered information
2. Verify PDF upload completed
3. Check product ID associations
4. Click "Save Certificate"
5. Confirm success message

### Tips for Efficiency
- Prepare all information before starting
- Use bulk CSV import for multiple product IDs
- Create companies in advance if possible
- Keep certificate files organized and named consistently

---

## Search Certificate

### Basic Search Process

#### 1. Access Search Interface
1. Navigate to main certificate page
2. Use search bar at top of page
3. Or go to `/Certificate/CertificateTileView`

#### 2. Enter Search Terms
1. **Certificate Number**: For exact matches
2. **Company Name**: Full or partial names
3. **Product Name**: Product descriptions
4. **Product IDs**: ASIN, UPC, or other identifiers

#### 3. Use Autocomplete
1. Start typing in search box
2. Select from autocomplete suggestions
3. Suggestions categorized by:
   - Certificate numbers
   - Company names
   - Product names
   - Product categories

#### 4. Apply Filters
1. **Date Range**: Filter by certificate dates
2. **Company**: Select specific companies
3. **Product Type**: Filter by product categories
4. **Status**: Active, expiring, or expired certificates

### Advanced Search Techniques

#### Search Operators
- **Quotes**: Use "exact phrase" for precise matching
- **Wildcards**: Use * for partial matching
- **Multiple Terms**: Combine terms for better results

#### Filter Combinations
1. Start with broad search
2. Apply one filter at a time
3. Combine filters for precise results
4. Clear filters to start over

### Search Results

#### Understanding Results
- **Tile View**: Visual representation with key info
- **List View**: Detailed tabular format
- **Infinite Scroll**: Automatic loading of more results

#### Result Actions
1. **View Details**: Click tile or title
2. **Quick Actions**: Edit, download, share links
3. **Bulk Actions**: Select multiple for exports

---

## Create User (Admin Only)

### Prerequisites
- Admin role required
- User information prepared
- Role assignment determined

### Step-by-Step Process

#### 1. Access User Management
1. Navigate to `/Admin/UserManagement`
2. Click "Create New User" button

#### 2. Enter User Information
1. **Email Address**: Must be unique in system
2. **Full Name**: User's display name
3. **Initial Password**: Temporary password
4. **Confirm Password**: Verify password entry

#### 3. Assign Role
1. **Admin**: Full system access
2. **Analyst**: Certificate management access
3. **External**: Limited partner access
4. Select appropriate role from dropdown

#### 4. Additional Settings
1. **Account Status**: Active by default
2. **Email Verification**: Required for login
3. **Password Reset**: Force change on first login
4. **Access Restrictions**: Set if needed

#### 5. Save and Notify
1. Review all information
2. Click "Create User"
3. System sends welcome email
4. Provide login instructions to user

### User Management Best Practices
- Use meaningful email addresses
- Assign minimal required permissions
- Set strong temporary passwords
- Monitor user activity regularly
- Remove inactive accounts promptly

---

## Manage Company

### Adding New Company

#### 1. Access Company Management
1. From certificate creation page
2. Or navigate to company management section
3. Click "Add New Company"

#### 2. Enter Company Details
1. **Legal Name**: Official company name
2. **Display Name**: Name shown in system
3. **Company Type**: Classification (manufacturer, etc.)
4. **Tax ID**: Business identification number

#### 3. Contact Information
1. **Primary Contact**: Main contact person
2. **Email Address**: Company contact email
3. **Phone Number**: Primary phone
4. **Address**: Complete business address

#### 4. Additional Information
1. **Website**: Company website URL
2. **Industry**: Business category
3. **Notes**: Additional relevant information
4. **Active Status**: Enable/disable company

### Editing Existing Company

#### 1. Locate Company
1. Search for company in management interface
2. Or select from dropdown in certificate forms
3. Click "Edit" button

#### 2. Update Information
1. Modify any required fields
2. Update contact information
3. Change address if moved
4. Update notes or status

#### 3. Associate Certificates
1. View certificates linked to company
2. Add new certificate associations
3. Remove outdated associations
4. Verify all links are current

---

## Export Data

### Available Export Formats

#### CSV Export
- **Use Case**: Data analysis, spreadsheet import
- **Contents**: Tabular data with all fields
- **Best For**: Large datasets, analysis tools

#### Excel Export
- **Use Case**: Reports, presentations
- **Contents**: Formatted data with styling
- **Best For**: Business reports, sharing

#### PDF Export
- **Use Case**: Document sharing, printing
- **Contents**: Formatted certificates
- **Best For**: Official documents, archiving

### Export Process

#### 1. Select Data to Export
1. Navigate to certificate list or search results
2. Use filters to narrow data if needed
3. Select specific certificates or use "All"
4. Choose date ranges if applicable

#### 2. Choose Export Format
1. Click "Export" button
2. Select desired format from dropdown
3. Configure export options:
   - Include product IDs
   - Include company details
   - Date range restrictions
   - Field selections

#### 3. Generate and Download
1. Click "Generate Export"
2. Wait for processing completion
3. Download generated file
4. Verify export contents

### Export Best Practices
- Filter data before exporting to reduce file size
- Choose appropriate format for intended use
- Verify export contents before sharing
- Keep exports secure and delete when no longer needed

---

## Generate Report

### Available Report Types

#### Certificate Summary Report
- Overview of all certificates
- Status breakdown (active, expiring, expired)
- Company distribution
- Monthly/quarterly summaries

#### Expiration Report
- Certificates expiring in specified timeframe
- Grouped by company or product type
- Action items for renewals
- Timeline projections

#### Company Report
- All certificates for specific companies
- Company contact information
- Certificate history and trends
- Compliance status overview

#### Activity Report
- System usage statistics
- User activity summaries
- Recent changes and updates
- Performance metrics

### Report Generation Process

#### 1. Access Reporting
1. Navigate to Dashboard
2. Click "Reports" section
3. Or go to dedicated reporting page

#### 2. Select Report Type
1. Choose from available report types
2. Review report description
3. Check sample output if available

#### 3. Configure Parameters
1. **Date Range**: Set reporting period
2. **Filters**: Apply specific criteria
3. **Output Format**: Choose PDF, Excel, or CSV
4. **Detail Level**: Summary or detailed

#### 4. Generate Report
1. Click "Generate Report"
2. Wait for processing
3. Download completed report
4. Review contents for accuracy

### Report Customization
- Save frequently used report configurations
- Schedule automatic report generation
- Customize report layouts and branding
- Add custom fields or calculations

---

## Reset Password

### Self-Service Password Reset

#### 1. Access Reset Feature
1. Go to login page
2. Click "Forgot Password" link
3. Or navigate to password reset page

#### 2. Enter Email Address
1. Type your registered email address
2. Click "Send Reset Link"
3. Check email for reset instructions

#### 3. Follow Email Instructions
1. Open password reset email
2. Click provided reset link
3. Link expires in 24 hours

#### 4. Set New Password
1. Enter new password
2. Confirm password entry
3. Meet password requirements:
   - Minimum 8 characters
   - Include uppercase letter
   - Include lowercase letter
   - Include number
   - Include special character

### Admin Password Reset (Admin Only)

#### 1. Access User Management
1. Navigate to user management
2. Find user needing password reset
3. Click "Reset Password" action

#### 2. Generate New Password
1. System generates temporary password
2. Option to set custom password
3. Force password change on next login

#### 3. Communicate New Password
1. Securely share new password with user
2. Instruct user to change password immediately
3. Verify user can log in successfully

---

## Manage Permissions (Admin Only)

### Role-Based Permissions

#### Available Roles
1. **Admin**: Full system access
   - User management
   - Role configuration
   - System settings
   - All certificate functions

2. **Analyst**: Certificate management
   - Create/edit certificates
   - Company management
   - Reporting access
   - No user management

3. **External**: Limited access
   - View assigned certificates
   - Download permitted documents
   - Basic search functionality
   - No creation/editing

#### Permission Configuration

#### 1. Access Role Management
1. Navigate to `/Admin/RoleManagement`
2. Select role to configure
3. Review current permissions

#### 2. Modify Permissions
1. Enable/disable specific functions
2. Set access levels for features
3. Configure data visibility
4. Set operational restrictions

#### 3. User Assignment
1. Assign users to roles
2. Remove users from roles
3. Change user role assignments
4. Monitor role effectiveness

### Advanced Permission Settings

#### Feature-Level Permissions
- Certificate creation rights
- Company management access
- Report generation capabilities
- Export functionality limits

#### Data-Level Permissions
- Company-specific access
- Product category restrictions
- Geographic limitations
- Time-based access controls

---

## Troubleshooting Common Issues

### Login Problems
1. Verify username/email accuracy
2. Check password requirements
3. Clear browser cache/cookies
4. Try different browser
5. Contact administrator if persistent

### Upload Failures
1. Check file size (under 10MB)
2. Verify PDF format
3. Ensure file isn't corrupted
4. Try different browser
5. Check network connectivity

### Search Not Working
1. Try simpler search terms
2. Check spelling
3. Clear filters and try again
4. Use different search criteria
5. Contact support if persistent

### Performance Issues
1. Clear browser cache
2. Close unnecessary browser tabs
3. Check network connection
4. Try during off-peak hours
5. Report persistent problems

---

*This training guide covers the essential tasks performed daily in the GCC Database Solution. For additional support or advanced features, contact your system administrator or refer to role-specific guides.*

*Last Updated: September 2025*
*Version: 1.0*