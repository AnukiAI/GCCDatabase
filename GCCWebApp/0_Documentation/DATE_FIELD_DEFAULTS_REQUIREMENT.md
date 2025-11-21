# Certificate Date Field Defaults Requirement

## Overview
Certificate date fields (Effective Date and Expiration Date) must default to reasonable current-year values when creating new certificates.

## Business Need
When creating a new certificate, users should not need to manually correct date fields that default to year 0001 (01/01/0001). This creates confusion and increases data entry errors.

## Requirement ID
**REQ-DATE-001**: Certificate Date Field Defaults

## User Story
**As a** Certificate Manager (Admin/Analyst)
**I want** date fields to default to sensible current-year values
**So that** I don't have to manually correct invalid default dates when creating certificates

## Acceptance Criteria

### AC-1: Effective Date Default
- ✅ When opening the Certificate Create page (`/Certificate/Create`)
- ✅ The **Effective Date** field defaults to **today's date**
- ✅ The date is displayed in the user's local date format
- ✅ The user can change the date if needed

### AC-2: Expiration Date Default
- ✅ When opening the Certificate Create page
- ✅ The **Expiration Date** field defaults to **2 years from today**
- ✅ This represents typical certificate validity period
- ✅ The user can change the date if needed

### AC-3: Status Default
- ✅ The **Status** field defaults to **"Active"**

### AC-4: Date Field Validation
- ✅ Users can still manually change dates to past or future dates as needed
- ✅ Standard date validation rules still apply (required fields, date format)
- ✅ Expiration date should be after effective date (existing validation)

## Technical Implementation

### Location
**File:** `Controllers/CertificateController.cs`
**Method:** `Create()` (line 335) and `Create2()` (line 371)

### Code Change
```csharp
var model = new CertificateEditViewModel
{
    // Set default dates to current year
    EffectiveDate = DateTime.Today,
    ExpirationDate = DateTime.Today.AddYears(2), // Default 2-year validity
    Status = "active" // Default status
};
```

### Before Fix
- EffectiveDate: `01/01/0001` (DateTime.MinValue)
- ExpirationDate: `01/01/0001` (DateTime.MinValue)
- Status: `null`

### After Fix
- EffectiveDate: Current date (e.g., `11/20/2025`)
- ExpirationDate: Current date + 2 years (e.g., `11/20/2027`)
- Status: `"active"`

## Testing

### Manual Test Steps
1. Navigate to `/Certificate/Create`
2. **Verify:** Effective Date shows today's date
3. **Verify:** Expiration Date shows date 2 years from today
4. **Verify:** Status defaults to "Active"
5. **Verify:** Can manually change both dates
6. **Verify:** Form submits successfully with default dates

### Test Data
- **Expected Effective Date:** Current date (2025-11-20 at time of writing)
- **Expected Expiration Date:** 2 years later (2027-11-20 at time of writing)

## Related Issues
- **Bug:** Date fields defaulting to year 0001
- **Impact:** High - affects all certificate creation workflows
- **Priority:** High - causes user confusion and data entry errors

## Status
✅ **Implemented** - 2025-11-20

## Notes
- The 2-year default for expiration is based on typical GreenCircle certification validity periods
- Users can override these defaults as needed for special cases
- This change only affects the CREATE page, not EDIT pages (which load existing certificate dates)
