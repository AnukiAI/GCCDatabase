# UI Design Review Screenshot Testing

## Overview

The UI Design Review Screenshot Testing suite captures comprehensive screenshots of all pages in the GCC Web Application for design consistency review. This testing process supports ongoing UI/UX quality assurance and helps identify design inconsistencies across different pages and user roles.

## Purpose

- **Design Consistency Auditing**: Capture visual snapshots of all pages to compare spacing, padding, colors, and typography
- **Baseline Documentation**: Create reference screenshots for future visual regression testing
- **Stakeholder Review**: Provide visual documentation for design reviews and approvals
- **Issue Identification**: Identify inconsistent UI patterns across different pages and roles

## Quick Start

### Prerequisites

1. Ensure the GCC Web Application is running:
   ```bash
   cd GCCWebApp
   dotnet run --urls "http://localhost:5101"
   ```

2. Playwright browsers must be installed (one-time setup):
   ```bash
   cd GCCWebApp.UAT.Tests
   ./install-playwright.cmd
   ```

### Run UI Design Review Screenshots

**Option 1: Use the convenient batch file (Recommended)**
```bash
cd GCCWebApp.UAT.Tests
./run-ui-design-review-screenshots.cmd
```

**Option 2: Use dotnet test command**
```bash
cd GCCWebApp.UAT.Tests
dotnet test --filter "Category=UIDesignReview"
```

**Option 3: Run in headed mode (visual browser)**
```bash
set TEST_HEADLESS=false
dotnet test --filter "Category=UIDesignReview"
```

## Screenshot Coverage

The UI Design Review captures **20 key pages** across all user roles:

### Public Pages (3 screenshots)
1. Certificate Tile View (Public)
2. Login Page
3. Certificate Details (Public)

### Admin Pages (13 screenshots)
4. Dashboard (Admin)
5. Certificate Tile View (Internal)
6. Create Certificate
7. Edit Certificate
8. Single Certificate View
9. Companies Page
10. Product Management
11. User Management
12. Role Management
13. Certificate Types
14. Product Types
15. Certificate Fields
16. Partners

### Analyst Pages (2 screenshots)
17. Dashboard (Analyst)
18. Certificate Tile View (Analyst)

### External Pages (2 screenshots)
19. Certificate Tile View (External)
20. Product Management (External)

## Screenshot Storage

### Location
Screenshots are saved to:
```
GCCWebApp.UAT.Tests/Screenshots/UI_Design_Review_{timestamp}/
```

### Naming Convention
```
{timestamp} = yyyy-MM-dd_HH-mm-ss

Example: UI_Design_Review_2025-10-06_14-30-45/
```

### Screenshot Files
Each screenshot is named descriptively:
- `01-Public-Certificate-Tile-View.png`
- `02-Login-Page.png`
- `03-Public-Certificate-Details.png`
- `04-Admin-Dashboard.png`
- ... and so on

## Design Standards Reference

The screenshots should be reviewed against the design standards documented in:

**Formal Deliverable:**
```
GCCWebApp/0_Documentation/Deliverables/UI_DESIGN_CONSISTENCY_REVIEW.html
```

**Markdown Reference:**
```
GCCWebApp/UI-DESIGN-REVIEW.md
```

### Key Standards to Check

#### Color Palette
- **Navy Blue:** `#002876` (Headers, primary text)
- **Ocean Blue:** `#00AEEF` (Interactive elements, buttons)
- **Lime Green:** `#92C83E` (Success states)
- **Forest Green:** `#447638` (Borders, secondary)
- **Off-White:** `#f9f9f9` (Page backgrounds)
- **White:** `#ffffff` (Card backgrounds)

#### Spacing System
- **XS:** 4px
- **SM:** 8px
- **MD:** 16px
- **LG:** 24px
- **XL:** 32px

#### Component Standards
- **Card Padding:** 24-28px
- **Card Gap:** 20-24px
- **Table Row Padding:** 16-20px
- **Button Padding:** 16-18px vertical
- **Input Height:** 44px minimum (touch-friendly)
- **Border Radius (Cards):** 12px
- **Border Radius (Buttons):** 8px

## Review Process

### Step 1: Capture Screenshots
```bash
./run-ui-design-review-screenshots.cmd
```

### Step 2: Review Screenshots
1. Open the generated screenshots folder
2. Review each screenshot against the design standards
3. Compare similar pages across different roles (e.g., Admin vs Analyst dashboard)
4. Note any inconsistencies in spacing, padding, colors, or typography

### Step 3: Document Issues
Update the formal deliverable:
```
0_Documentation/Deliverables/UI_DESIGN_CONSISTENCY_REVIEW.html
```

Add new rows to the "Issues Identified & Resolution Status" table:
- Issue ID
- Description
- Page(s) Affected
- Priority (Critical, High, Medium, Low)
- Status (Fixed, In Progress, Pending)

### Step 4: Create Fix List
1. Prioritize issues (Critical → High → Medium → Low)
2. Group related issues (e.g., all card padding issues)
3. Identify files that need to be modified

### Step 5: Implement Fixes
1. Update CSS files or inline styles
2. Test changes locally
3. Re-run screenshots to verify fixes
4. Update issue status in deliverable

### Step 6: Regression Testing
- Run screenshots again after fixes
- Compare before/after screenshots
- Ensure fixes don't introduce new inconsistencies

## Test Configuration

### User Credentials
The tests use these test accounts:
- **Public:** (no credentials needed)
- **External:** `external@company.com` / `TestPass123!`
- **Analyst:** `analyst@gcc.com` / `TestPass123!`
- **Admin:** `admin@gcc.com` / `TestPass123!`

### Viewport Settings
- **Desktop:** 1920x1080 (default)
- Future: Can be extended to test Tablet (768x1024) and Mobile (375x812)

### Browser Settings
- **Browser:** Chromium (via Playwright)
- **Headless Mode:** Yes (default) - faster execution
- **Headed Mode:** Set `TEST_HEADLESS=false` to see browser

## Advanced Usage

### Run Specific Pages Only

**Only Public Pages:**
```bash
dotnet test --filter "Category=UIDesignReview&Category=Public"
```

**Only Admin Pages:**
```bash
dotnet test --filter "Category=UIDesignReview&Category=Admin"
```

**Only Dashboard:**
```bash
dotnet test --filter "Category=UIDesignReview&Category=Dashboard"
```

### Run in Headed Mode (Visual)
```bash
set TEST_HEADLESS=false
./run-ui-design-review-screenshots.cmd
```

### Custom Base URL
```bash
set TEST_BASE_URL=http://localhost:8080
dotnet test --filter "Category=UIDesignReview"
```

### Verbose Logging
```bash
dotnet test --filter "Category=UIDesignReview" --logger "console;verbosity=detailed"
```

## Integration with CI/CD

### Automated Nightly Run
Add to your CI/CD pipeline to capture screenshots nightly:

```yaml
# Example GitHub Actions workflow
name: UI Design Review Screenshots
on:
  schedule:
    - cron: '0 2 * * *'  # Run at 2 AM daily
  workflow_dispatch:      # Allow manual trigger

jobs:
  screenshots:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Run application
        run: |
          cd GCCWebApp
          dotnet run --urls "http://localhost:5101" &
          sleep 10
      - name: Run UI Design Review Screenshots
        run: |
          cd GCCWebApp.UAT.Tests
          dotnet test --filter "Category=UIDesignReview"
      - name: Upload Screenshots
        uses: actions/upload-artifact@v3
        with:
          name: ui-screenshots
          path: GCCWebApp.UAT.Tests/Screenshots/UI_Design_Review_*
```

## Troubleshooting

### Application Not Running
```
Error: Unable to connect to http://localhost:5101
```

**Solution:** Start the application first:
```bash
cd GCCWebApp
dotnet run --urls "http://localhost:5101"
```

### Playwright Not Installed
```
Error: Playwright browsers are not installed
```

**Solution:** Install Playwright:
```bash
cd GCCWebApp.UAT.Tests
./install-playwright.cmd
```

### Screenshots Folder Not Found
**Location:** Check the console output - it shows the exact path where screenshots are saved.

### Test Timeout
If tests time out, increase the timeout in `ScreenshotTestBase.cs` or ensure the application is responsive.

## Files Created

### Test File
```
GCCWebApp.UAT.Tests/Tests/Screenshots/UIDesignReviewScreenshotTests.cs
```

### Batch Script
```
GCCWebApp.UAT.Tests/run-ui-design-review-screenshots.cmd
```

### Documentation
```
GCCWebApp.UAT.Tests/README_UI_Design_Review_Testing.md (this file)
GCCWebApp/0_Documentation/Deliverables/UI_DESIGN_CONSISTENCY_REVIEW.html
GCCWebApp/UI-DESIGN-REVIEW.md
```

## Benefits

### For Developers
- Quickly identify UI inconsistencies
- Visual regression testing
- Ensure design standards compliance

### For Designers
- Review actual implementation vs. mockups
- Provide visual feedback on design issues
- Track design consistency over time

### For QA
- Comprehensive visual testing coverage
- Automated screenshot capture
- Evidence for bug reports

### For Product Managers
- Visual documentation of all pages
- Progress tracking for UI improvements
- Stakeholder review materials

## Maintenance

### Updating the Test Suite

**Add New Pages:**
1. Open `UIDesignReviewScreenshotTests.cs`
2. Add new test method following the naming convention:
   ```csharp
   [Test, Order(21)]
   [Category("Admin")]
   public async Task Capture_21_NewAdminPage()
   {
       await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/NewPage");
       // ... screenshot logic
   }
   ```

**Update Page URLs:**
- Modify the `GotoAsync()` URLs if routes change

**Update Credentials:**
- Modify credentials in `ScreenshotTestBase.cs` if test accounts change

## Related Documentation

- **Master Design Guide:** `GCCWebApp/0_Documentation/mockups/0_0_MasterDesignGuide.html`
- **Requirements:** `GCCWebApp/0_Documentation/docs/Requirements.md`
- **Test Cases:** `GCCWebApp/0_Documentation/docs/MANUAL_UAT_TEST_CASES.md`
- **Screenshot Testing Guide:** `GCCWebApp.UAT.Tests/README_Screenshot_Testing.md`

---

**Last Updated:** October 6, 2025
**Version:** 1.0
**Maintained By:** GCC Development Team
