# GCC Database - Screenshot Test Suite
## Automated Visual Testing with Authentication

This suite captures comprehensive screenshots of the GCC Database application across all user roles for visual review and client presentations.

---

## Quick Start

### Option 1: Playwright (Recommended - Full Automation)

```bash
# 1. Install dependencies
npm install

# 2. Install Playwright browsers
npx playwright install

# 3. Run screenshot tests
npm run test:screenshots
```

### Option 2: PowerShell Script (Public Pages Only)

```powershell
# Run PowerShell script for public pages
.\run-screenshot-tests.ps1

# With mobile and tablet views
.\run-screenshot-tests.ps1 -IncludeMobile -IncludeTablet
```

---

## What Gets Tested

### User Roles Covered

| Role | Pages Tested | Screenshots | Authentication |
|------|--------------|-------------|----------------|
| **Public** | - Landing page<br>- Autocomplete<br>- Filters<br>- Certificate details<br>- Login | 8 | None required |
| **External** | - Partner-filtered view<br>- Certificate details<br>- Access denied pages | 4 | ✅ Automated |
| **Analyst** | - Dashboard<br>- Certificate list<br>- Create form<br>- Edit form<br>- Product ID manager | 6 | ✅ Automated |
| **Admin** | - Dashboard<br>- Create/Edit forms<br>- User management<br>- Role management<br>- API keys | 6 | ✅ Automated |
| **Responsive** | - Mobile (375x667)<br>- Tablet (1024x768) | 4 | Public pages |

**Total Screenshots:** ~28 per test run

---

## Screenshot Output

### Directory Structure

```
screenshots/
└── 2025-10-05T14-30-00/
    ├── public/
    │   ├── 01-tile-view-landing.png
    │   ├── 02-autocomplete-dropdown.png
    │   ├── 03-certificate-details.png
    │   ├── 04-login-page.png
    │   ├── mobile-01-tile-view.png
    │   ├── mobile-02-certificate-details.png
    │   ├── tablet-01-tile-view.png
    │   └── tablet-02-certificate-details.png
    ├── external/
    │   ├── 01-partner-filtered-view.png
    │   ├── 02-certificate-details.png
    │   ├── 03-create-access-denied.png
    │   └── 04-admin-access-denied.png
    ├── analyst/
    │   ├── 01-dashboard.png
    │   ├── 02-certificate-list.png
    │   ├── 03-create-form.png
    │   ├── 04-edit-form.png
    │   ├── 05-certificate-details.png
    │   └── 06-admin-access-denied.png
    └── admin/
        ├── 01-dashboard.png
        ├── 02-create-form.png
        ├── 03-edit-form.png
        ├── 04-user-management.png
        ├── 05-role-management.png
        └── 06-api-keys.png
```

### Screenshot Specifications

- **Format:** PNG (lossless)
- **Desktop Resolution:** 1920x1080
- **Tablet Resolution:** 1024x768
- **Mobile Resolution:** 375x667 (iPhone)
- **Capture:** Full page (scrolling)
- **Browser:** Chromium (Playwright)

---

## Playwright Test Script Details

### Authentication Flow

```javascript
// Automated login for each role
async function login(page, role) {
    const credentials = {
        admin: { email: 'admin@greencirclecertified.com', password: 'Admin123!' },
        analyst: { email: 'analyst@greencirclecertified.com', password: 'Analyst123!' },
        external: { email: 'partner@amazon.com', password: 'Partner123!' }
    };

    await page.goto('/Account/Login');
    await page.fill('input[type="email"]', credentials[role].email);
    await page.fill('input[type="password"]', credentials[role].password);
    await page.click('button[type="submit"]');
    await page.waitForLoadState('networkidle');
}
```

### Screenshot Capture

```javascript
await page.screenshot({
    path: 'screenshot.png',
    fullPage: true  // Captures entire scrollable page
});
```

### Test Coverage

1. **Public User Flow**
   - Landing page load
   - Search autocomplete interaction
   - Filter application
   - Certificate details view
   - Login page

2. **External User Flow**
   - Authenticated access
   - Partner-filtered certificate view (Amazon only)
   - Certificate details (partner access)
   - Access denied validation (Create, Admin pages)

3. **Analyst User Flow**
   - Dashboard with metrics
   - Certificate list (all partners)
   - Create certificate form
   - Edit certificate form with Product ID manager
   - Certificate details with edit button
   - Access denied validation (Admin pages)

4. **Admin User Flow**
   - Full dashboard access
   - Create/Edit certificate forms
   - User management page
   - Role management page
   - API key management

5. **Responsive Testing**
   - Mobile viewport (iPhone)
   - Tablet viewport (iPad)
   - Desktop viewport (1920x1080)

---

## Using Screenshots for Client Review

### Pre-Meeting Checklist

- [ ] Run screenshot tests: `npm run test:screenshots`
- [ ] Review all screenshots for UI issues
- [ ] Compare against Master Design Guide
- [ ] Verify all role-based access control
- [ ] Check responsive layouts
- [ ] Organize screenshots by priority
- [ ] Create presentation deck

### Client Presentation Flow

1. **Landing Page (Public View)**
   - Show `public/01-tile-view-landing.png`
   - Highlight: Infinite scroll, filter sidebar, search

2. **Search & Autocomplete**
   - Show `public/02-autocomplete-dropdown.png`
   - Highlight: Categorized suggestions, type badges

3. **Certificate Details**
   - Show `public/03-certificate-details.png`
   - Highlight: Product info, download PDF, certification period

4. **Dashboard Analytics**
   - Show `analyst/01-dashboard.png` or `admin/01-dashboard.png`
   - Highlight: 12 metrics, 5 data tables, export options

5. **Certificate Management**
   - Show `analyst/03-create-form.png`
   - Show `analyst/04-edit-form.png`
   - Highlight: Multi-select dropdowns, Product ID manager, workflow states

6. **User Management (Admin)**
   - Show `admin/04-user-management.png`
   - Highlight: Role assignment, user creation, audit trail

7. **Partner Access Control**
   - Show `external/01-partner-filtered-view.png`
   - Show `external/03-create-access-denied.png`
   - Highlight: Automatic filtering, access restrictions

8. **Responsive Design**
   - Show `public/mobile-01-tile-view.png`
   - Show `public/tablet-01-tile-view.png`
   - Highlight: Adaptive layouts, touch-friendly

---

## Troubleshooting

### Issue: Playwright not installed
```bash
npm install
npx playwright install
```

### Issue: Login fails in automated tests
- Verify test credentials in AccountController seed data
- Check if application is running on http://localhost:5101
- Review login form selectors in script

### Issue: Screenshots are blank
- Ensure application is fully loaded before capture
- Increase `waitForLoadState('networkidle')` timeout
- Check for JavaScript errors in page

### Issue: Access denied pages not captured
- Verify role-based access control is enforced
- Check authorization attributes on controllers
- Ensure session is maintained after login

---

## Advanced Usage

### Custom Screenshot Scenarios

```javascript
// Add to playwright-screenshot-tests.js

// Capture specific state (e.g., modal open)
await page.click('button.open-modal');
await page.waitForSelector('.modal.visible');
await page.screenshot({ path: 'modal-open.png' });

// Capture specific element only
await page.locator('.certificate-card').first().screenshot({
    path: 'certificate-card.png'
});

// Capture with specific viewport
await page.setViewportSize({ width: 1366, height: 768 });
await page.screenshot({ path: 'laptop-view.png' });
```

### Video Recording

```javascript
const context = await browser.newContext({
    recordVideo: {
        dir: 'videos/',
        size: { width: 1920, height: 1080 }
    }
});
```

### Comparison Testing

```javascript
// Use Playwright's built-in visual comparison
await expect(page).toHaveScreenshot('baseline.png', {
    maxDiffPixels: 100
});
```

---

## Integration with CI/CD

### GitHub Actions Example

```yaml
name: Screenshot Tests

on:
  pull_request:
    branches: [ master ]

jobs:
  screenshot-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '18'
      - run: npm install
      - run: npx playwright install --with-deps
      - run: npm run test:screenshots
      - uses: actions/upload-artifact@v3
        with:
          name: screenshots
          path: screenshots/
```

---

## Next Steps

1. **Review Screenshots**
   - Compare against COMPREHENSIVE_UI_TEST_PLAN.md
   - Validate UI/UX checklist items
   - Document any discrepancies

2. **UI Issue Tracking**
   - Create bug tickets for issues found
   - Link screenshots as evidence
   - Prioritize by severity

3. **Client Preparation**
   - Create presentation deck
   - Prepare demo environment
   - Review all test cases

4. **Regression Testing**
   - Run screenshots before each release
   - Compare against baseline
   - Track visual changes over time

---

## Files in This Suite

| File | Purpose |
|------|---------|
| `playwright-screenshot-tests.js` | Main Playwright test script (Recommended) |
| `run-screenshot-tests.ps1` | PowerShell script for public pages |
| `package.json` | NPM dependencies and scripts |
| `COMPREHENSIVE_UI_TEST_PLAN.md` | Full test plan with manual test cases |
| `SCREENSHOT_TEST_README.md` | This file |

---

**Last Updated:** 2025-10-05
**Maintainer:** GCC Development Team
**Support:** Review COMPREHENSIVE_UI_TEST_PLAN.md for detailed test cases
