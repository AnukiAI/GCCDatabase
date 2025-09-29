# Navigation Requirements - GCC Database Solution

## Overview
This document clarifies the navigation structure requirements for the GreenCircle Certified Database application, addressing confusion around "Certificate Management" terminology.

## Navigation Structure by Role

### Public Users (Unauthenticated)
- No navigation menu items
- Direct access to Certificate Tile View (landing page)
- Access to Certificate Details view
- Login option in header

### External Users (Partners - Amazon, Wayfair)
- **Certificates** - Access to external certificate tile view
- **Products** - Access to product management

### Analyst Users
- **Certificates** (dropdown)
  - View All Certificates
  - Create New Certificate
- **Dashboard** - Analytics and reporting
- **Companies & Products** (dropdown)
  - Companies
  - Products

### Admin Users
- **Certificates** (dropdown)
  - View All Certificates
  - Create New Certificate
- **Dashboard** - Analytics and reporting
- **Companies & Products** (dropdown)
  - Companies
  - Products
- **Administration** (dropdown)
  - Role Management
  - User Management
  - Configuration/Settings (nested)
  - API Management (nested)
  - Data Management (nested)

## Important Clarifications

### ❌ "Certificate Management" is NOT a Navigation Section
- **What it IS**: A functional area/epic describing the capability to manage certificates (CRUD operations)
- **What it's NOT**: A separate navigation menu item
- **Why this matters**: Having both "Certificates" and "Certificate Management" creates redundancy and user confusion

### ✅ Correct Terminology
- **Navigation**: "Certificates" (with Create/View options in dropdown)
- **Documentation**: "Certificate Management" refers to the overall functionality
- **User Experience**: Simple, direct access to certificate functions

## Routes and Actions

### Certificate-Related Routes
- `/Certificate/CertificateTileView` - Public main page
- `/Certificate/CertificateTileViewInternal` - Internal user view
- `/Certificate/CertificateTileViewExternal` - External partner view
- `/Certificate/Create` - Create new certificate (Analyst/Admin only)
- `/Certificate/Edit/{id}` - Edit certificate (Analyst/Admin only)
- `/Certificate/Details/{id}` - View certificate details (all users)

### Navigation Implementation
```html
<!-- Correct: Simple Certificates dropdown -->
<li class="nav-item">
    <a href="#" class="nav-link">
        Certificates
        <i class="fas fa-chevron-down dropdown-icon"></i>
    </a>
    <div class="submenu">
        <a asp-controller="Certificate" asp-action="CertificateTileViewInternal">
            View All Certificates
        </a>
        <a asp-controller="Certificate" asp-action="Create">
            Create New Certificate
        </a>
    </div>
</li>

<!-- Incorrect: Separate Certificate Management section -->
<!-- This should NOT exist -->
```

## Testing Implications

### Navigation Tests Should Verify
1. Correct menu items appear for each role
2. No "Certificate Management" section exists
3. Certificate functions are accessible through "Certificates" menu
4. External users see appropriate certificate views
5. Dropdown functionality works correctly

### Test Cases to Update
- Remove any tests looking for "Certificate Management" navigation
- Update tests to use "Certificates" navigation path
- Verify role-based menu item visibility
- Test certificate creation flow through Certificates > Create New Certificate

## Documentation Updates Required

### Files to Review and Update
- Test cases mentioning "Certificate Management" navigation
- User manuals referencing navigation structure
- Acceptance criteria for navigation features
- Any wireframes or mockups showing incorrect navigation

### Terminology Guidelines
- Use "Certificate Management" in functional/technical documentation to describe capabilities
- Use "Certificates" in user-facing interface and navigation
- Ensure consistency across all documentation

## Implementation Status

✅ **Completed**
- Removed "Certificate Management" navigation section from `_Layout.cshtml`
- Updated "Certificates" navigation to include dropdown with View/Create options
- Maintained all existing functionality while simplifying navigation

🔄 **In Progress**
- Documentation updates to reflect correct navigation structure
- Test case updates to remove incorrect navigation references

📋 **Next Steps**
- Update all test cases referencing "Certificate Management" navigation
- Review user documentation for navigation accuracy
- Conduct user acceptance testing to verify improved navigation clarity

---

*Last Updated: September 29, 2025*
*Document Version: 1.0*