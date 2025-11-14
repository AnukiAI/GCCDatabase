# Custom Fields Testing Guide

## Overview
This guide provides step-by-step instructions for testing the custom fields functionality across all certificate pages.

## Prerequisites
- Application running on http://localhost:5101
- Admin user credentials (Username: `admin`, Password: `TestPass123!`)
- Test certificates in the database

## Test Plan

### Phase 1: Admin Page Testing (Certificate Fields Management)

#### 1.1 Access the Admin Page
1. Navigate to http://localhost:5101/Admin/CertificateFields
2. If not logged in, use admin credentials
3. Verify the page loads successfully
4. Check that the page displays:
   - Page title: "Certificate Fields"
   - Search box
   - Filter controls (Certification Type, Field Type, Section)
   - Data table with columns: Name, Type, Certification Type, Section, Required, Order, Active, Actions
   - Action buttons: Export, Template, Import, Add New Certificate Field

#### 1.2 Create Test Custom Fields
Create the following test fields for testing:

**Field Set 1: Product Information Section (for "GreenCircle Certified" certification type)**
1. Click "Add New Certificate Field"
2. Create Field 1:
   - Name: `Product SKU`
   - Field Type: `text`
   - Certification Type: Select "GreenCircle Certified"
   - Section: `Product Information`
   - Display Order: `1`
   - Required: `Yes`
   - Active: `Yes`
   - Validation Rules: Leave empty
   - Help Text: `Enter the product SKU code`

3. Create Field 2:
   - Name: `Product Weight (kg)`
   - Field Type: `number`
   - Certification Type: Select "GreenCircle Certified"
   - Section: `Product Information`
   - Display Order: `2`
   - Required: `No`
   - Active: `Yes`
   - Validation Rules: Leave empty
   - Help Text: `Enter product weight in kilograms`

4. Create Field 3:
   - Name: `Manufacturing Date`
   - Field Type: `date`
   - Certification Type: Select "GreenCircle Certified"
   - Section: `Product Information`
   - Display Order: `3`
   - Required: `No`
   - Active: `Yes`
   - Validation Rules: Leave empty
   - Help Text: `Select the manufacturing date`

**Field Set 2: Compliance Details Section (for "GreenCircle Certified" certification type)**
5. Create Field 4:
   - Name: `Meets EPA Standards`
   - Field Type: `checkbox`
   - Certification Type: Select "GreenCircle Certified"
   - Section: `Compliance Details`
   - Display Order: `1`
   - Required: `No`
   - Active: `Yes`
   - Help Text: `Check if product meets EPA standards`

6. Create Field 5:
   - Name: `Certification Level`
   - Field Type: `select`
   - Certification Type: Select "GreenCircle Certified"
   - Section: `Compliance Details`
   - Display Order: `2`
   - Required: `Yes`
   - Active: `Yes`
   - Options: `Bronze,Silver,Gold,Platinum` (comma-separated)
   - Help Text: `Select the certification level`

7. Create Field 6:
   - Name: `Applicable Standards`
   - Field Type: `multi-select`
   - Certification Type: Select "GreenCircle Certified"
   - Section: `Compliance Details`
   - Display Order: `3`
   - Required: `No`
   - Active: `Yes`
   - Options: `ISO 14001,LEED,Energy Star,FSC,Green Seal` (comma-separated)
   - Help Text: `Select all applicable standards`

#### 1.3 Test CRUD Operations
1. **Read**: Verify all 6 fields appear in the table
2. **Update**: Edit one field (e.g., change "Product SKU" help text)
3. **Filter**: Test filtering by:
   - Certification Type: "GreenCircle Certified"
   - Section: "Product Information"
   - Field Type: "text"
4. **Search**: Search for "Product" - should show Product SKU and Product Weight
5. **Sort**: Click column headers to sort
6. **Export**: Click Export button, verify CSV download

**Expected Results:**
- All CRUD operations work without errors
- Filters correctly narrow down results
- Search finds relevant fields
- Sorting works on all columns
- CSV export contains all field data

---

### Phase 2: Certificate Create Page Testing

#### 2.1 Access Create Page
1. Navigate to http://localhost:5101/Certificate/Create
2. Verify admin/analyst role access

#### 2.2 Test Dynamic Field Loading
1. In the "Certification Type" dropdown, select "GreenCircle Certified"
2. **Expected**: Custom Fields section should appear below Product ID Manager
3. **Verify**:
   - Section "Product Information" displays with 3 fields (Product SKU, Product Weight, Manufacturing Date)
   - Section "Compliance Details" displays with 3 fields (Meets EPA Standards, Certification Level, Applicable Standards)
   - Fields are arranged in 2-column grid on desktop
   - Required fields show red asterisk (*)
   - Help text displays below each field

#### 2.3 Test Field Types Rendering
1. **Text Field** (Product SKU):
   - Should render as `<input type="text">`
   - Try entering: "SKU-12345"
   - Verify red asterisk shows (required field)

2. **Number Field** (Product Weight):
   - Should render as `<input type="number">`
   - Try entering: "25.5"
   - Verify accepts decimal numbers

3. **Date Field** (Manufacturing Date):
   - Should render as `<input type="date">`
   - Try selecting today's date
   - Verify date picker appears

4. **Checkbox Field** (Meets EPA Standards):
   - Should render as `<input type="checkbox">`
   - Try checking/unchecking
   - Verify toggle works

5. **Select Field** (Certification Level):
   - Should render as `<select>` dropdown
   - Verify options: Bronze, Silver, Gold, Platinum
   - Select "Gold"
   - Verify red asterisk shows (required field)

6. **Multi-Select Field** (Applicable Standards):
   - Should render as `<select multiple>`
   - Verify options: ISO 14001, LEED, Energy Star, FSC, Green Seal
   - Select multiple options (e.g., "LEED" and "Energy Star")
   - Verify multiple selections work

#### 2.4 Test Field Validation
1. Leave required field "Product SKU" empty
2. Fill in all other certificate fields
3. Click Save
4. **Expected**: Validation error appears for Product SKU
5. Fill in Product SKU: "TEST-SKU-001"
6. Leave required field "Certification Level" empty
7. Click Save
8. **Expected**: Validation error appears for Certification Level
9. Select "Certification Level": Gold
10. Click Save
11. **Expected**: Certificate saves successfully

#### 2.5 Test Responsive Design
1. Resize browser to mobile width (375px)
2. **Verify**: Custom fields section switches to 1-column layout
3. Resize to tablet width (768px)
4. **Verify**: Custom fields section switches to 1-column layout
5. Resize to desktop width (1024px+)
6. **Verify**: Custom fields section uses 2-column grid layout

**Expected Results:**
- Custom fields load dynamically when certification type changes
- All field types render correctly
- Required field validation works
- Multi-select allows multiple selections
- Responsive design adapts to screen size
- Certificate saves with custom field values

---

### Phase 3: Certificate Edit Page Testing

#### 3.1 Access Edit Page
1. Find the certificate created in Phase 2
2. Navigate to http://localhost:5101/Certificate/Edit/{id}
3. Verify admin/analyst role access

#### 3.2 Test Field Value Loading
1. **Verify** the Custom Fields section displays
2. **Verify** field values load correctly:
   - Product SKU: "TEST-SKU-001"
   - Product Weight: "25.5" (if entered)
   - Manufacturing Date: (selected date)
   - Meets EPA Standards: (checked state)
   - Certification Level: "Gold"
   - Applicable Standards: "LEED, Energy Star" (if selected)

#### 3.3 Test Field Value Updates
1. Change Product SKU to: "TEST-SKU-002"
2. Change Product Weight to: "30"
3. Change Certification Level to: "Platinum"
4. Add another standard: "ISO 14001"
5. Click Save
6. **Expected**: Changes save successfully
7. Refresh the page
8. **Verify**: All changes persisted correctly

#### 3.4 Test Clearing Values
1. Clear Product Weight (optional field)
2. Uncheck "Meets EPA Standards"
3. Click Save
4. **Expected**: Values clear successfully
5. Refresh the page
6. **Verify**: Cleared values remain empty

**Expected Results:**
- Existing field values load correctly on page load
- Field values can be updated
- Changes persist after save and refresh
- Optional fields can be cleared
- Required fields cannot be cleared

---

### Phase 4: Certificate Details Page Testing

#### 4.1 Access Details Page
1. Navigate to http://localhost:5101/Certificate/Details/{id}
2. Use the certificate from Phase 3

#### 4.2 Test Field Display
1. **Verify** "Additional Information" card appears after main certificate details
2. **Verify** sections are displayed with headers:
   - "Product Information" section header
   - "Compliance Details" section header

#### 4.3 Test Section Grouping
1. **Under "Product Information"**, verify fields display:
   - Product SKU: TEST-SKU-002
   - Product Weight (kg): 30
   - Manufacturing Date: (formatted date, e.g., "January 15, 2025")

2. **Under "Compliance Details"**, verify fields display:
   - Meets EPA Standards: (should show "Yes" or "No" based on checkbox)
   - Certification Level: Platinum
   - Applicable Standards: LEED, Energy Star, ISO 14001 (comma-separated)

#### 4.4 Test Empty Field Handling
1. Create a new certificate without filling optional custom fields
2. Navigate to Details page
3. **Verify**: Only fields with values are displayed
4. **Verify**: Empty sections are hidden (if all fields in section are empty)

#### 4.5 Test Formatted Display
1. **Verify** field formatting:
   - Text fields display as plain text
   - Number fields display numeric value
   - Date fields display formatted date (e.g., "January 15, 2025")
   - Checkbox fields display "Yes" or "No"
   - Select fields display selected option
   - Multi-select fields display comma-separated values

**Expected Results:**
- Custom fields display in read-only format
- Fields are grouped by sections
- Section headers display correctly
- Empty fields are hidden
- Field values are formatted appropriately
- Layout is clean and readable

---

### Phase 5: Cross-Browser and Performance Testing

#### 5.1 Cross-Browser Testing
Test on the following browsers:
- Chrome (latest)
- Firefox (latest)
- Edge (latest)
- Safari (if available)

**Verify** on each browser:
- Custom fields load correctly
- Field interactions work (select, checkbox, date picker)
- Validation displays properly
- Save/update operations succeed

#### 5.2 Performance Testing
1. Create a certificate type with 20+ custom fields
2. Navigate to Create page and select that certificate type
3. **Verify**: Page loads in < 2 seconds
4. Fill in all fields
5. Click Save
6. **Verify**: Save completes in < 3 seconds

#### 5.3 Error Handling
1. **Test API Failure**:
   - Stop the application server
   - Try to load custom fields on Create page
   - **Verify**: Graceful error message displays

2. **Test Invalid Data**:
   - Enter text in a number field
   - **Verify**: Browser validation prevents submission
   - **OR** Server-side validation returns appropriate error

3. **Test Network Timeout**:
   - Use browser dev tools to throttle network to "Slow 3G"
   - Load Create page with many custom fields
   - **Verify**: Loading indicator shows, fields eventually load

---

## Test Data Summary

### Test Certificate Fields Created:
| Name | Type | Certification Type | Section | Required | Order |
|------|------|-------------------|---------|----------|-------|
| Product SKU | text | GreenCircle Certified | Product Information | Yes | 1 |
| Product Weight (kg) | number | GreenCircle Certified | Product Information | No | 2 |
| Manufacturing Date | date | GreenCircle Certified | Product Information | No | 3 |
| Meets EPA Standards | checkbox | GreenCircle Certified | Compliance Details | No | 1 |
| Certification Level | select | GreenCircle Certified | Compliance Details | Yes | 2 |
| Applicable Standards | multi-select | GreenCircle Certified | Compliance Details | No | 3 |

### Test Certificate Values:
- Product SKU: `TEST-SKU-002`
- Product Weight: `30`
- Manufacturing Date: `(current date)`
- Meets EPA Standards: `unchecked`
- Certification Level: `Platinum`
- Applicable Standards: `LEED, Energy Star, ISO 14001`

---

## Checklist

### Admin Page (Certificate Fields Management)
- [ ] Page loads successfully
- [ ] Can create new custom field
- [ ] Can edit existing custom field
- [ ] Can search for fields
- [ ] Can filter by certification type
- [ ] Can filter by section
- [ ] Can filter by field type
- [ ] Can sort table columns
- [ ] Can export to CSV
- [ ] All 6 field types can be created

### Create Certificate Page
- [ ] Custom fields section appears
- [ ] Fields load when certification type is selected
- [ ] Text field renders correctly
- [ ] Number field renders correctly
- [ ] Date field renders correctly
- [ ] Checkbox field renders correctly
- [ ] Select field renders correctly
- [ ] Multi-select field renders correctly
- [ ] Required field validation works
- [ ] Fields save successfully with certificate
- [ ] Responsive layout works (desktop/tablet/mobile)

### Edit Certificate Page
- [ ] Custom fields section appears
- [ ] Existing values load correctly
- [ ] Text field updates work
- [ ] Number field updates work
- [ ] Date field updates work
- [ ] Checkbox field updates work
- [ ] Select field updates work
- [ ] Multi-select field updates work
- [ ] Changes persist after save
- [ ] Optional fields can be cleared

### Details Certificate Page
- [ ] Additional Information card appears
- [ ] Section headers display correctly
- [ ] Fields are grouped by section
- [ ] Text fields display correctly
- [ ] Number fields display correctly
- [ ] Date fields display formatted
- [ ] Checkbox fields display Yes/No
- [ ] Select fields display selected value
- [ ] Multi-select fields display comma-separated
- [ ] Empty fields are hidden

### Cross-Cutting Concerns
- [ ] Works in Chrome
- [ ] Works in Firefox
- [ ] Works in Edge
- [ ] Page load performance is acceptable
- [ ] Save performance is acceptable
- [ ] Error handling works gracefully
- [ ] No JavaScript console errors
- [ ] No server-side errors in logs

---

## Known Issues / Notes

*(Add any issues discovered during testing here)*

---

## Completion Status

**Testing Started**: _________________
**Testing Completed**: _________________
**Tested By**: _________________
**Test Result**: [ ] PASS [ ] FAIL [ ] PARTIAL

**Notes**:
