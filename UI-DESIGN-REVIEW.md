# GCC Web Application - UI Design Review
**Date:** October 6, 2025
**Purpose:** Review UI consistency across all views - spacing, padding, backgrounds, and design patterns

## Design Standards (From Master Design Guide)

### Color Palette
- **Navy Blue:** `#002876` (Primary headers, text)
- **Ocean Blue:** `#00AEEF` (Interactive elements, buttons, accents)
- **Lime Green:** `#92C83E` (Success states, progress)
- **Forest Green:** `#447638` (Borders, secondary elements)
- **Off-White:** `#f9f9f9` (Backgrounds)
- **White:** `#ffffff` (Card backgrounds, content areas)

### Typography
- **Font Family:** Century Gothic (primary), CenturyGothic, AppleGothic, sans-serif
- **Minimum Font Size:** 16px (accessibility requirement)
- **Line Height:** 1.6 (body text)

### Spacing System
- **XS:** 4px
- **SM:** 8px
- **MD:** 16px
- **LG:** 24px
- **XL:** 32px

### Border Radius
- **SM:** 4px
- **MD:** 8px
- **LG:** 12px

---

## Pages to Review

### 1. **Public Certificate Tile View** (`/Certificate/CertificateTileView`)
**URL:** http://localhost:5101/Certificate/CertificateTileView

**Current Observations:**
- [ ] Header background color
- [ ] Page title spacing
- [ ] Search bar styling and spacing
- [ ] Filter panel (if visible) spacing
- [ ] Certificate tile card spacing
- [ ] Certificate tile padding
- [ ] Gap between tiles
- [ ] Footer spacing
- [ ] Overall page background

**Expected Standards:**
- Background: `#f9f9f9` (off-white)
- Card backgrounds: `#ffffff` (white)
- Card padding: 24-28px
- Gap between tiles: 20px
- Border radius: 12px
- Shadow: `0 2px 8px rgba(0, 0, 0, 0.06)`

---

### 2. **Login Page** (`/Account/Login`)
**URL:** http://localhost:5101/Account/Login

**Current Observations:**
- [ ] Page background
- [ ] Login form container background
- [ ] Form padding
- [ ] Input field spacing
- [ ] Button styling (size, padding, color)
- [ ] Logo/header spacing
- [ ] Overall vertical spacing

**Expected Standards:**
- Page background: `#f9f9f9`
- Form container: white with shadow
- Form padding: 32-40px
- Input fields: 16px font, proper padding
- Button: Ocean Blue (#00AEEF), 16-18px padding

---

### 3. **Dashboard** (`/Dashboard/Index`) ⚠️ ISSUE IDENTIFIED
**URL:** http://localhost:5101/Dashboard/Index
**Auth:** Admin or Analyst

**Current Observations:**
- [x] **BACKGROUND ISSUE:** Teal/turquoise gradient visible behind content
- [ ] Stat card spacing and padding
- [ ] Gap between stat cards
- [ ] Table section spacing
- [ ] Table padding
- [ ] Action button spacing

**Expected Standards:**
- Page background: `#f9f9f9`
- Dashboard container: `#ffffff` (white) **← FIX APPLIED**
- Stat cards: white background, 24-28px padding
- Gap between cards: 20px
- Tables: proper header/row padding (16-20px)

**Fix Applied:**
- Added `background-color: #ffffff;` to `.dashboard-container` in `Views/Dashboard/Index.cshtml:14`

---

### 4. **Internal Certificate Tile View** (`/Certificate/CertificateTileViewInternal`)
**URL:** http://localhost:5101/Certificate/CertificateTileViewInternal
**Auth:** Admin or Analyst

**Current Observations:**
- [ ] Matches public view styling?
- [ ] Additional filters/controls spacing
- [ ] Edit/Delete button spacing on tiles
- [ ] Breadcrumb spacing (if visible for Admin)

---

### 5. **Create Certificate** (`/Certificate/Create`)
**URL:** http://localhost:5101/Certificate/Create
**Auth:** Admin or Analyst

**Current Observations:**
- [ ] Form container background
- [ ] Form section spacing
- [ ] Input field spacing (vertical gaps)
- [ ] Label to input spacing
- [ ] Button group spacing
- [ ] Multi-step indicator (if present)
- [ ] Product ID Manager modal spacing

**Expected Standards:**
- Form background: white
- Section spacing: 24-32px
- Field spacing: 16-20px
- Label to input: 8px
- Button padding: 16px 28px

---

### 6. **Edit Certificate** (`/Certificate/Edit/{id}`)
**URL:** http://localhost:5101/Certificate/Edit/{id}
**Auth:** Admin or Analyst

**Current Observations:**
- [ ] Same as Create form?
- [ ] Additional status/workflow controls spacing
- [ ] Product ID list spacing

---

### 7. **Single Certificate View** (`/Certificate/CertificateSingleView/{id}`)
**URL:** http://localhost:5101/Certificate/CertificateSingleView/{id}
**Auth:** Admin or Analyst

**Current Observations:**
- [ ] Header section spacing
- [ ] Field display spacing
- [ ] Section breaks/dividers
- [ ] Download button spacing
- [ ] Product ID section spacing

---

### 8. **Companies List** (`/Company/Index`)
**URL:** http://localhost:5101/Company/Index
**Auth:** Admin or Analyst

**Current Observations:**
- [ ] Table header spacing
- [ ] Table row padding
- [ ] Search/filter controls spacing
- [ ] Action button spacing
- [ ] Pagination (if present)

---

### 9. **Product Management** (`/Product/Management`)
**URL:** http://localhost:5101/Product/Management
**Auth:** Admin, Analyst, or External

**Current Observations:**
- [ ] Product list/grid spacing
- [ ] Card vs. table layout
- [ ] Filter controls spacing
- [ ] Add product button

---

### 10. **User Management** (`/Admin/UserManagement`)
**URL:** http://localhost:5101/Admin/UserManagement
**Auth:** Admin only

**Current Observations:**
- [ ] Table layout
- [ ] Row padding
- [ ] Action buttons (Edit/Delete) spacing
- [ ] Add user button
- [ ] Modal popup (if present) padding

---

### 11. **Role Management** (`/Admin/RoleManagement`)
**URL:** http://localhost:5101/Admin/RoleManagement
**Auth:** Admin only

**Current Observations:**
- [ ] Role cards or table layout
- [ ] Permission checkboxes spacing
- [ ] Section spacing

---

### 12. **Certificate Types** (`/Admin/CertificateTypes`)
**URL:** http://localhost:5101/Admin/CertificateTypes
**Auth:** Admin only

**Current Observations:**
- [ ] List/table layout
- [ ] Add/Edit form spacing
- [ ] Modal or inline editing

---

### 13. **Product Types** (`/Admin/ProductTypes`)
**URL:** http://localhost:5101/Admin/ProductTypes
**Auth:** Admin only

**Current Observations:**
- [ ] Similar to Certificate Types?
- [ ] Hierarchy display (categories/subcategories)
- [ ] Spacing for nested items

---

### 14. **Certificate Fields** (`/Admin/CertificateFields`)
**URL:** http://localhost:5101/Admin/CertificateFields
**Auth:** Admin only

**Current Observations:**
- [ ] Field list layout
- [ ] Field type indicators
- [ ] Required/Optional badges spacing

---

### 15. **Partners** (`/Admin/Partners`)
**URL:** http://localhost:5101/Admin/Partners
**Auth:** Admin only

**Current Observations:**
- [ ] Partner list layout
- [ ] Logo/branding display
- [ ] API key section spacing

---

## Common Elements to Review Across All Pages

### Header
- [ ] Logo sizing and spacing
- [ ] Navigation bar item spacing
- [ ] User profile dropdown spacing
- [ ] Consistent height across pages

### Navigation
- [ ] Menu item padding
- [ ] Dropdown menu spacing
- [ ] Active state styling
- [ ] Mobile menu (hamburger) spacing

### Footer
- [ ] Disclaimer text spacing
- [ ] Link spacing
- [ ] Overall padding

### Buttons
- [ ] Primary button: Ocean Blue (#00AEEF), 16-18px padding
- [ ] Secondary button styling
- [ ] Danger/Delete button: Red with proper spacing
- [ ] Button groups: 12-16px gap

### Forms
- [ ] Input field height (44px minimum for touch)
- [ ] Input padding (12-16px)
- [ ] Label to input gap (8px)
- [ ] Field to field gap (16-20px)
- [ ] Error message spacing

### Tables
- [ ] Header background and padding
- [ ] Row padding (16-20px)
- [ ] Column spacing
- [ ] Hover state styling
- [ ] Striped rows (if applicable)

### Cards
- [ ] Card padding: 24-28px
- [ ] Card gap in grids: 20-24px
- [ ] Shadow consistency
- [ ] Border radius: 12px

### Modals/Popups
- [ ] Modal overlay opacity
- [ ] Modal content padding: 24-32px
- [ ] Modal max-width
- [ ] Close button spacing
- [ ] Modal header/footer spacing

---

## Issues Found

### Critical Issues
1. **Dashboard Background** - Teal gradient visible behind white content
   - **Status:** ✅ FIXED - Added white background to `.dashboard-container`
   - **File:** `Views/Dashboard/Index.cshtml:14`

### Medium Priority
- [ ] TBD after screenshot review

### Low Priority
- [ ] TBD after screenshot review

---

## Next Steps

1. ✅ Open all key pages in browser
2. ⏳ Take screenshots of each page
3. ⏳ Compare screenshots against Master Design Guide
4. ⏳ Document specific spacing/padding inconsistencies
5. ⏳ Create prioritized fix list
6. ⏳ Implement fixes
7. ⏳ Test and verify

---

## Screenshot Checklist

### Public Views
- [ ] Certificate Tile View (Public)
- [ ] Certificate Details (Public)
- [ ] Login Page

### Admin Views
- [ ] Dashboard
- [ ] Certificate Tile View (Internal)
- [ ] Create Certificate
- [ ] Edit Certificate
- [ ] Single Certificate View
- [ ] Companies List
- [ ] Product Management
- [ ] User Management
- [ ] Role Management
- [ ] Certificate Types
- [ ] Product Types
- [ ] Certificate Fields
- [ ] Partners

### Analyst Views
- [ ] Dashboard (Analyst)
- [ ] Certificate Views (Analyst)

### External Views
- [ ] Certificate Tile View (External)
- [ ] Product Management (External)

### Modals/Popups
- [ ] Filter panel (expanded)
- [ ] Product ID Manager modal
- [ ] Add/Edit user modal
- [ ] Delete confirmation modal
- [ ] Any other popups

---

## Design Consistency Goals

1. **Spacing Uniformity**
   - All cards use consistent 24-28px padding
   - All grids use 20-24px gaps
   - All tables use 16-20px row padding

2. **Color Consistency**
   - Primary actions: Ocean Blue (#00AEEF)
   - Danger actions: Red (#FF4757)
   - Success: Lime Green (#92C83E)
   - Text: Navy Blue (#002876) or #333

3. **Typography Consistency**
   - All body text: 16px minimum
   - All headings: Proper hierarchy (24px, 20px, 18px)
   - Line height: 1.6 for readability

4. **Component Consistency**
   - All buttons: Same height, padding, border-radius
   - All inputs: Same height, padding, border styling
   - All cards: Same shadow, border-radius, padding

---

## Additional Notes

- **Accessibility:** Ensure all touch targets are 44px minimum
- **Mobile Responsiveness:** Check spacing on mobile breakpoints (768px, 480px)
- **Print Styling:** Verify certificate print view spacing
- **Browser Compatibility:** Test in Chrome, Firefox, Edge
