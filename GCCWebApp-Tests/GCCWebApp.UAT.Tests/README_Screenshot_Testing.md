# Full System Screenshot Testing Guide

## Overview

This comprehensive screenshot testing suite captures every page in the GCC Web Application across different user roles and device viewports. The tests are designed to provide complete visual documentation of the system for testing, documentation, and regression purposes.

## Test Structure

### Test Classes

1. **ScreenshotTestBase** - Base class with common functionality
2. **PublicPagesScreenshotTests** - Screenshots of public pages (no auth required)
3. **AuthenticationPagesScreenshotTests** - Login, signup, password reset pages
4. **AuthenticatedPagesScreenshotTests** - Pages available to authenticated users
5. **AnalystPagesScreenshotTests** - Pages requiring Analyst or Admin roles
6. **AdminPagesScreenshotTests** - Admin-only pages and access denied scenarios

### User Roles Tested

- **Public** - Unauthenticated users
- **External** - Partner users (partner@amazon.com)
- **Analyst** - Internal analysts (analyst@greencirclecertified.com)  
- **Admin** - Full admin access (admin@greencirclecertified.com)

### Viewports Tested

- **Desktop**: 1920x1080
- **Tablet**: 768x1024
- **Mobile**: 375x812

## Pages Covered

### Public Pages (40+ screenshots)
- Home page, Privacy policy
- Public certificate views
- Certificate tile views
- Individual certificate details
- Error pages

### Authentication Pages (33+ screenshots)
- Login, Sign up, Forgot password
- Password reset flows
- Email verification
- Two-factor authentication
- Account locked scenarios

### Authenticated User Pages (120+ screenshots)
- Dashboard for each user type
- Certificate listing and details
- Company management
- Product management
- Document upload

### Analyst/Admin CRUD Pages (60+ screenshots)
- Certificate create/edit/import
- Document upload forms
- Company creation

### Admin-Only Pages (84+ screenshots)
- Admin dashboard
- User management
- Role management
- System configuration
- Data management (product types, certificate fields, etc.)
- Access denied scenarios

## Running the Tests

### Quick Test (Desktop Only)
```bash
.\run-screenshot-tests-quick.cmd
```

### Full Test Suite (All Viewports)
```bash
.\run-screenshot-tests.cmd
```

### Individual Test Classes
```bash
# Public pages only
dotnet test --filter "FullyQualifiedName~PublicPagesScreenshotTests"

# Admin pages only  
dotnet test --filter "FullyQualifiedName~AdminPagesScreenshotTests"

# Authentication pages only
dotnet test --filter "FullyQualifiedName~AuthenticationPagesScreenshotTests"
```

## Output Location

Screenshots are saved to: `Screenshots/Full_System_Screenshots_[timestamp]/`

### Filename Format
`{UserRole}_{PageName}_{Width}x{Height}.png`

Examples:
- `Public_HomePage_1920x1080.png`
- `Admin_UserManagementPage_768x1024.png`
- `Analyst_CertificateCreatePage_375x812.png`

## Prerequisites

1. **Application Running**: The web application must be running on https://localhost:5001
2. **Database Setup**: Ensure database contains sample data (certificates, companies, users)
3. **Playwright Browsers**: Will be installed automatically, or run:
   ```bash
   pwsh bin\Debug\net8.0\playwright.ps1 install
   ```

## Test Credentials

The tests use these built-in test credentials:
- **Admin**: admin@greencirclecertified.com / admin123
- **Analyst**: analyst@greencirclecertified.com / analyst123  
- **External**: partner@amazon.com / partner123
- **Public**: No authentication required

## Customization

### Adding New Pages
1. Add route to appropriate test class
2. Follow naming convention: `{UserRole}_{PageName}`
3. Test across all viewports if needed

### Adding New User Roles
1. Update `UserCredentials` dictionary in `ScreenshotTestBase`
2. Create new test methods in appropriate test classes
3. Add login logic if needed

### Adding New Viewports
1. Update `TestViewports` array in `ScreenshotTestBase`
2. Tests will automatically include new viewport

## Troubleshooting

### Common Issues

1. **Application Not Running**: Ensure web app is started before tests
2. **Login Failures**: Check test credentials match database
3. **Missing Pages**: Verify routes exist in controllers
4. **Playwright Issues**: Reinstall browsers with `playwright install`

### Test Failures

- Tests include error handling and will continue on individual failures
- Check console output for specific error messages
- Failed screenshots will be logged but won't stop the test suite

## Total Screenshot Count

When all tests run successfully, expect approximately **337+ screenshots**:
- Public Pages: ~40 screenshots
- Authentication Pages: ~33 screenshots  
- Authenticated User Pages: ~120 screenshots
- Analyst/Admin CRUD Pages: ~60 screenshots
- Admin-Only Pages: ~84 screenshots

This provides comprehensive visual documentation of every page in the system across all user roles and device types.