# GCC Database - User Guide

## Table of Contents
1. [Getting Started](#getting-started)
2. [User Roles and Permissions](#user-roles-and-permissions)
3. [Logging In](#logging-in)
4. [Dashboard Overview](#dashboard-overview)
5. [Managing Certificates](#managing-certificates)
6. [Managing Companies](#managing-companies)
7. [Managing Products](#managing-products)
8. [Search and Filter](#search-and-filter)
9. [Reports and Analytics](#reports-and-analytics)
10. [User Profile Management](#user-profile-management)
11. [Administrator Functions](#administrator-functions)
12. [Troubleshooting](#troubleshooting)
13. [Keyboard Shortcuts](#keyboard-shortcuts)
14. [FAQ](#faq)

## Getting Started

Welcome to the GCC Database system! This guide will help you navigate and use all features of the application effectively.

The system now features a **completely redesigned user interface** with modern, responsive design and enhanced user experience throughout the authentication process.

### System Requirements
- **Browser**: Chrome 90+, Firefox 88+, Safari 14+, Edge 90+
- **Screen Resolution**: 1366x768 minimum (1920x1080 recommended) - **Fully responsive design supports all screen sizes**
- **Internet Connection**: Stable broadband connection
- **JavaScript**: Must be enabled
- **Mobile Support**: ✅ **NEW** - Optimized for mobile devices with touch-friendly interface

### First Time Setup ✅ **ENHANCED**
1. Navigate to https://www.gccdatabase.com
2. **New Sign Up Experience**:
   - Modern registration form with real-time validation
   - Password strength meter and requirements checklist
   - Terms of service and privacy policy integration
   - Enhanced mobile experience
3. **Email Verification**: 
   - Improved verification email design
   - Clear instructions and next steps
   - Resend verification option with countdown
4. **Welcome Experience**:
   - New welcome page with feature highlights
   - Guided onboarding process
   - Automatic redirect to dashboard
5. Request appropriate role access from your administrator

## User Roles and Permissions

### Role Types

| Role | Description | Key Permissions |
|------|-------------|-----------------|
| **Public User** | External users with read-only access | View public certificates, Search certificates |
| **Analyst** | Internal users managing certificates | Create/Edit certificates, Manage products, Generate reports |
| **Administrator** | System administrators | All permissions, User management, System configuration |
| **External Partner** | Partner organizations | View partner-specific data, Limited editing |
| **Auditor** | Compliance and audit users | Read all data, Export reports, View audit logs |

### Permission Matrix

| Action | Public | Analyst | Admin | Partner | Auditor |
|--------|--------|---------|-------|---------|---------|
| View Certificates | ✓ | ✓ | ✓ | ✓ | ✓ |
| Create Certificates | ✗ | ✓ | ✓ | ✗ | ✗ |
| Edit Certificates | ✗ | ✓ | ✓ | Limited | ✗ |
| Delete Certificates | ✗ | ✗ | ✓ | ✗ | ✗ |
| Manage Users | ✗ | ✗ | ✓ | ✗ | ✗ |
| View Audit Logs | ✗ | ✗ | ✓ | ✗ | ✓ |
| Export Data | ✗ | ✓ | ✓ | ✓ | ✓ |

## Logging In

The GCC Database features a modern, responsive authentication experience with enhanced security and user-friendly design.

### New Login Experience ✅ **UPDATED**
The login page now features:
- **Modern Design**: Clean, professional interface with GreenCircle branding
- **Responsive Layout**: Optimized for all devices (desktop, tablet, mobile)
- **Enhanced Security**: Visual feedback and improved password security
- **Accessibility**: Better contrast, focus indicators, and keyboard navigation

### Standard Login
1. Go to https://www.gccdatabase.com/login
2. **New Interface**: Modern login form with GreenCircle green (#92C83E) and corporate blue (#002B76) branding
3. Enter your email address in the clearly labeled field
4. Enter your password with real-time security feedback
5. Click the redesigned "Sign In" button
6. **Enhanced Experience**: Loading states and smooth transitions

### Single Sign-On (SSO)
1. Click "Sign in with Microsoft" (modernized button design)
2. Enter your organization credentials
3. Complete multi-factor authentication if required
4. You'll be redirected to the dashboard

### Password Reset ✅ **ENHANCED**
1. Click "Forgot Password?" on the login page
2. **New Reset Flow**: 
   - Modern password reset form
   - Clear instructions and visual guidance
   - Email verification with enhanced design
3. Create a new password with **real-time strength validation**:
   - Visual password strength indicator
   - Requirements checklist that updates as you type
   - Minimum 8 characters with complexity requirements
4. **Success Page**: Confirmation with automatic redirect

### Two-Factor Authentication (2FA) ✅ **REDESIGNED**
1. **Enhanced 2FA Interface**: 
   - Interactive 6-digit code input with auto-advance
   - Paste support for authentication codes
   - Clear visual feedback for each digit
2. **Improved User Experience**:
   - "Trust this device for 30 days" option
   - Resend code functionality with countdown timer
   - Clear instructions and help text
3. **Accessibility**: Better keyboard navigation and screen reader support

## Dashboard Overview

The dashboard provides a quick overview of key metrics and recent activities.

### Dashboard Components

#### Statistics Panel
- **Active Certificates**: Total number of valid certificates
- **Expiring Soon**: Certificates expiring within 30 days
- **Companies**: Total registered companies
- **Products**: Total products in the system

#### Recent Activity Feed
- Latest certificate additions
- Recent updates and modifications
- System notifications
- Upcoming expirations

#### Quick Actions
- **Add Certificate**: Create new certificate
- **Search**: Quick search bar
- **Reports**: Generate standard reports
- **Import**: Bulk import certificates

### Customizing Your Dashboard
1. Click the gear icon in the top-right corner
2. Select "Dashboard Settings"
3. Choose widgets to display/hide
4. Drag widgets to rearrange
5. Click "Save Layout"

## Managing Certificates

### Viewing Certificates

#### List View
1. Navigate to Certificates → View All
2. Browse certificates in table format
3. Click column headers to sort
4. Use pagination controls at the bottom

#### Tile View
1. Click the tile icon in the view selector
2. Visual card display of certificates
3. Hover for quick details
4. Click for full information

#### Detailed View
1. Click on any certificate number
2. View complete certificate information
3. Access related documents
4. See audit history

### Creating Certificates

1. Click "Add Certificate" button
2. Fill in required fields:
   - **Certificate Number**: Unique identifier
   - **Certificate Type**: Select from dropdown
   - **Company**: Search and select
   - **Product**: Optional, select if applicable
   - **Effective Date**: Certificate start date
   - **Expiration Date**: Certificate end date

3. Add optional information:
   - Location details
   - Custom attributes
   - Supporting documents

4. Click "Save as Draft" or "Submit for Review"

### Editing Certificates

1. Navigate to the certificate detail page
2. Click "Edit" button (if you have permission)
3. Modify necessary fields
4. Add revision notes
5. Click "Save Changes"

**Note**: Some fields may be locked after certificate issuance

### Certificate Workflow

```
Draft → Pending Review → Approved → Active → Expired/Renewed
```

#### Status Definitions
- **Draft**: Initial creation, not yet submitted
- **Pending Review**: Awaiting approval
- **Approved**: Approved but not yet effective
- **Active**: Currently valid certificate
- **Expired**: Past expiration date
- **Revoked**: Manually invalidated
- **Renewed**: Replaced by new certificate

### Uploading Documents

1. In certificate edit mode, scroll to "Documents" section
2. Click "Upload Document"
3. Select file(s) from your computer
   - Supported formats: PDF, DOC, DOCX, JPG, PNG
   - Maximum size: 10MB per file
4. Add document description
5. Click "Upload"

### Bulk Import

1. Navigate to Certificates → Import
2. Download the template file
3. Fill in certificate data following the template format
4. Save as CSV or Excel file
5. Click "Choose File" and select your file
6. Review validation results
7. Fix any errors and re-upload if needed
8. Click "Import" to process

## Managing Companies

### Adding a Company

1. Navigate to Companies → Add New
2. Enter company information:
   - **Company Name**: Legal business name
   - **Tax ID**: Business registration number
   - **Address**: Complete address details
   - **Contact Information**: Primary contact details
3. Click "Save Company"

### Company Profile

Each company profile includes:
- Basic information
- Active certificates
- Products manufactured
- Contact persons
- Document library
- Activity history

### Linking Products to Companies

1. Go to company profile
2. Click "Products" tab
3. Click "Add Product"
4. Search existing products or create new
5. Establish relationship

## Managing Products

### Product Categories

Products are organized in hierarchical categories:
- Building Materials
  - Insulation
  - Flooring
  - Roofing
- Electronics
  - Consumer Electronics
  - Industrial Equipment

### Adding Products

1. Navigate to Products → Add New
2. Select company (manufacturer)
3. Choose category
4. Enter product details:
   - Product name
   - Description
   - SKU/Model number
   - Specifications
5. Click "Save Product"

### Product Certificates

View all certificates associated with a product:
1. Go to product detail page
2. Click "Certificates" tab
3. View active and historical certificates

## Search and Filter

### Basic Search

Use the search bar at the top of any page:
- Enter certificate number, company name, or product name
- Press Enter or click the search icon
- Results appear instantly

### Advanced Search

1. Click "Advanced Search" link
2. Use multiple criteria:
   - Certificate type
   - Date ranges
   - Status
   - Company
   - Product category
   - Location
3. Combine filters with AND/OR logic
4. Save search for future use

### Filter Options

On list pages, use the filter panel:
- Click "Filters" to expand options
- Select multiple filter criteria
- Click "Apply Filters"
- Clear individual filters or "Reset All"

### Saved Searches

1. After applying filters, click "Save Search"
2. Name your search
3. Access saved searches from the dropdown menu
4. Set up email alerts for new matches

## Reports and Analytics

### Standard Reports

Access pre-built reports:
1. Navigate to Reports → Standard Reports
2. Select report type:
   - Certificate Summary
   - Expiration Report
   - Company Overview
   - Product Compliance
3. Choose date range and filters
4. Click "Generate Report"

### Custom Reports

Create your own reports:
1. Go to Reports → Custom Report Builder
2. Select data source (Certificates, Companies, Products)
3. Choose fields to include
4. Add filters and grouping
5. Preview report
6. Save for future use

### Exporting Data

Export reports in multiple formats:
- **PDF**: Formatted report with charts
- **Excel**: Raw data for analysis
- **CSV**: Simple data export
- **Print**: Printer-friendly version

### Dashboard Analytics

View real-time analytics:
- Certificate trends over time
- Expiration forecasts
- Company distribution
- Product category breakdown

## User Profile Management

### Updating Your Profile

1. Click your name in the top-right corner
2. Select "My Profile"
3. Update information:
   - Personal details
   - Contact information
   - Notification preferences
   - Time zone
4. Click "Save Changes"

### Notification Settings

Configure how you receive notifications:
- **Email Notifications**:
  - Certificate expirations
  - System updates
  - Report completions
- **In-App Notifications**:
  - Real-time alerts
  - Task reminders
- **SMS Notifications** (if enabled):
  - Critical alerts only

### Security Settings

Manage your account security:
1. Change password regularly
2. Enable two-factor authentication
3. Review login history
4. Manage connected devices
5. Set up security questions

## Administrator Functions

### User Management

Administrators can:
1. Navigate to Admin → User Management
2. View all users
3. Add new users:
   - Enter email address
   - Assign role
   - Set temporary password
4. Edit user permissions
5. Deactivate/reactivate accounts
6. Reset passwords
7. View audit logs

### Role Management

1. Go to Admin → Roles
2. View existing roles
3. Create custom roles:
   - Name the role
   - Select permissions
   - Assign to users
4. Modify role permissions

### System Configuration

Configure system settings:
- **General Settings**:
  - Company information
  - Default values
  - Email templates
- **Security Settings**:
  - Password policies
  - Session timeouts
  - IP restrictions
- **Integration Settings**:
  - API configurations
  - Third-party services

### Data Management

- **Backup Management**:
  - Schedule automatic backups
  - Perform manual backups
  - Restore from backup
- **Archive Settings**:
  - Set retention policies
  - Archive old records
  - Manage storage

### Audit Logs

Review system activity:
1. Navigate to Admin → Audit Logs
2. Filter by:
   - User
   - Action type
   - Date range
   - Object type
3. Export audit reports

## Troubleshooting

### Common Issues

#### Cannot Log In
- Verify email address is correct
- Check CAPS LOCK is off
- Try password reset
- Clear browser cache
- Contact administrator if account is locked

#### Missing Data
- Check filter settings
- Verify permissions
- Refresh the page
- Log out and back in

#### Slow Performance
- Check internet connection
- Clear browser cache
- Try different browser
- Reduce number of results displayed
- Contact support if issue persists

#### Export Not Working
- Check pop-up blocker settings
- Verify export permissions
- Try smaller date range
- Use different format

### Browser Issues

Clear cache and cookies:
- **Chrome**: Settings → Privacy → Clear browsing data
- **Firefox**: Settings → Privacy & Security → Clear Data
- **Safari**: Preferences → Privacy → Manage Website Data
- **Edge**: Settings → Privacy → Clear browsing data

### Error Messages

| Error Code | Meaning | Solution |
|------------|---------|----------|
| 401 | Unauthorized | Log in again |
| 403 | Forbidden | Check permissions |
| 404 | Not found | Verify URL/data exists |
| 500 | Server error | Contact support |
| 503 | Service unavailable | Try again later |

## Keyboard Shortcuts

### Global Shortcuts
- `Ctrl + K` or `Cmd + K`: Quick search
- `Ctrl + N` or `Cmd + N`: New certificate
- `Ctrl + S` or `Cmd + S`: Save current form
- `Escape`: Close modal/dialog
- `?`: Show keyboard shortcuts

### Navigation
- `G then D`: Go to Dashboard
- `G then C`: Go to Certificates
- `G then P`: Go to Products
- `G then R`: Go to Reports

### List Views
- `J`: Next item
- `K`: Previous item
- `Enter`: Open selected item
- `Shift + Click`: Multi-select

## FAQ

### General Questions

**Q: How often is data updated?**
A: Real-time for user entries, daily for external integrations.

**Q: Can I access the system on mobile devices?**
A: Yes, the system is fully responsive and works on tablets and smartphones.

**Q: What browsers are supported?**
A: Chrome, Firefox, Safari, and Edge (latest versions).

**Q: How do I request new features?**
A: Contact your administrator or submit feedback through the Help menu.

### Certificate Questions

**Q: Can I edit an active certificate?**
A: Only certain fields can be edited. Major changes require creating a new version.

**Q: How are expiration notifications sent?**
A: Email notifications at 90, 60, 30, and 7 days before expiration.

**Q: Can I bulk update certificates?**
A: Yes, use the Import function with the update template.

### Account Questions

**Q: How do I change my email address?**
A: Contact your administrator to update your email.

**Q: What if I'm locked out?**
A: After 5 failed attempts, wait 30 minutes or contact support.

**Q: Can I have multiple roles?**
A: Yes, users can be assigned multiple roles with combined permissions.

## Support

### Getting Help
- **In-App Help**: Click the question mark icon
- **User Guide**: Access this guide from Help menu
- **Video Tutorials**: Available in the Help Center
- **Contact Support**: support@gccdatabase.com

### Reporting Issues
When reporting issues, include:
- Description of the problem
- Steps to reproduce
- Screenshot if possible
- Browser and version
- Error messages

### Training Resources
- New user orientation videos
- Role-specific training modules
- Monthly webinars
- Knowledge base articles

## Appendix

### Glossary
- **Certificate**: Official document verifying compliance
- **SKU**: Stock Keeping Unit, unique product identifier
- **2FA**: Two-Factor Authentication
- **CSV**: Comma-Separated Values file format
- **API**: Application Programming Interface

### Data Limits
- Maximum file upload: 10MB
- Maximum bulk import: 1000 records
- Search results limit: 500 per page
- Export limit: 10,000 records

### Version Information
- Application Version: 1.1.0 ✅ **UPDATED**
- UI Version: Complete Account pages redesign ✅ **NEW**
- Last Updated: August 2025
- Next Update: September 2025

---

*For additional assistance, contact support@gccdatabase.com or call +1-555-0100*