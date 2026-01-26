# Pending Change Request: Multi-Partner Support for Certificates

**Created:** 2026-01-23
**Updated:** 2026-01-23
**Status:** PAUSED - Views reverted to single-select dropdown, backend ready for multi-select
**Priority:** Phase 1 - User Management Enhancement

---

## Overview

Enable certificates to be associated with multiple partners (e.g., a certificate can qualify for both Amazon and Novi programs simultaneously).

**UI Clarification:** The Partner selection should remain a **dropdown** but allow **multiple selections** - NOT checkboxes as initially implemented.

---

## What Has Been Completed

### 1. Database Migration (COMPLETED)
**File:** `docs/Database_Scripts/Migrations/14_20260122_AddMultiPartnerSupport.sql`

**Changes Applied:**
- Created `CertificatePartners` join table (many-to-many)
- Added `PartnerId` column to `Companies` table (optional FK for reporting)
- Migration script executed successfully on GCCDatabase_Dev

```sql
-- CertificatePartners table structure
CREATE TABLE CertificatePartners (
    CertificatesId INT NOT NULL,
    PartnersId INT NOT NULL,
    PRIMARY KEY (CertificatesId, PartnersId)
);

-- Companies.PartnerId column
ALTER TABLE Companies ADD PartnerId INT NULL;
```

### 2. Model Changes (COMPLETED)

**Models/Company.cs**
```csharp
// Added for reporting - which partner program a company belongs to
public int? PartnerId { get; set; }
public Partner? Partner { get; set; }
```

**Models/Partner.cs**
```csharp
// Added navigation collections
public ICollection<Company> Companies { get; set; } = new List<Company>();
public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
```

**Models/Certificate.cs**
```csharp
// Added multi-partner support
public ICollection<Partner> Partners { get; set; } = new List<Partner>();
```

**Models/CertificateEditViewModel.cs**
```csharp
// Added for form submission
[Display(Name = "Partners")]
public string? PartnerIds { get; set; }  // Comma-separated list
```

### 3. DbContext Changes (COMPLETED)

**Data/ApplicationDbContext.cs**
```csharp
// Certificate-Partner many-to-many
modelBuilder.Entity<Certificate>()
    .HasMany(c => c.Partners)
    .WithMany(p => p.Certificates)
    .UsingEntity(j => j.ToTable("CertificatePartners"));

// Company-Partner optional relationship
modelBuilder.Entity<Company>()
    .HasOne(c => c.Partner)
    .WithMany(p => p.Companies)
    .HasForeignKey(c => c.PartnerId)
    .OnDelete(DeleteBehavior.SetNull)
    .IsRequired(false);
```

### 4. Repository Changes (COMPLETED)

**Repositories/IUnitOfWork.cs & UnitOfWork.cs**
- Added `IRepository<Partner> Partners` property

**Repositories/CertificateRepository.cs**
- Added `.Include(c => c.Partners)` to:
  - `GetCertificateByNumberAsync()` (line 70)
  - `GetByIdAsync()` (line 145)
  - `GetAdvancedFilteredCertificatesAsync()` (line 232)

### 5. Controller Changes (COMPLETED)

**Controllers/CertificateController.cs**

**Create POST (lines 513-531):**
```csharp
// Handle Partners (multi-partner support)
if (!string.IsNullOrWhiteSpace(model.PartnerIds))
{
    var partnerIdList = model.PartnerIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(int.Parse)
        .ToList();

    var partners = await _unitOfWork.Partners.FindAsync(p => partnerIdList.Contains(p.Id) && !p.IsDeleted);
    foreach (var partner in partners)
    {
        certificate.Partners.Add(partner);
    }
    // Set legacy field for backward compatibility
    certificate.Partner = partners.FirstOrDefault()?.Name ?? "None";
}
```

**Edit GET (lines 644-654):**
```csharp
var partnerIds = certificate.Partners?.Any() == true
    ? string.Join(",", certificate.Partners.Select(p => p.Id))
    : "";
// ... PartnerIds = partnerIds in view model
```

**Edit POST (lines 813-847):**
```csharp
// Clear existing and re-add selected partners
certificate.Partners.Clear();
if (!string.IsNullOrWhiteSpace(model.PartnerIds))
{
    // Parse and add partners...
}
```

---

## Current State (REVERTED)

**Views have been reverted to single-select Partner dropdown (2026-01-23)**

Files changed:
- `Views/Certificate/Create.cshtml` - Restored single-select Partner dropdown
- `Views/Certificate/Edit.cshtml` - Restored single-select Partner dropdown
- `wwwroot/js/certificate-create.js` - PartnerManager.init() commented out

The backend is ready for multi-partner support. When ready to implement:
- Change dropdown to multi-select
- Uncomment PartnerManager.init() or implement new multi-select JS

### Required State (FUTURE)
The Partner selection should be a **multi-select dropdown** that allows selecting multiple partners.

---

## Implementation Steps Remaining

### Step 1: Revert View Changes

**Views/Certificate/Create.cshtml** - Change back to dropdown with `multiple` attribute:
```html
<!-- REMOVE this checkbox implementation -->
<div id="partner-checkboxes" class="partner-checkbox-container">
    <span class="loading-partners">Loading partners...</span>
</div>
<input type="hidden" name="PartnerIds" id="partner-ids-hidden" value="" />

<!-- REPLACE WITH multi-select dropdown -->
<select asp-for="PartnerIds" class="form-control" id="PartnerIds" name="PartnerIds" multiple>
    @foreach (var partner in ViewBag.Partners)
    {
        <option value="@partner.Id">@partner.Name</option>
    }
</select>
```

**Views/Certificate/Edit.cshtml** - Same change as Create

### Step 2: Update Controller to Populate Partners List

**CertificateController.cs - Create GET:**
```csharp
ViewBag.Partners = await _unitOfWork.Partners
    .FindAsync(p => !p.IsDeleted && p.IsActive)
    .OrderBy(p => p.Name);
```

**CertificateController.cs - Edit GET:**
```csharp
// Same as Create, plus pre-select existing partners
ViewBag.Partners = await _unitOfWork.Partners
    .FindAsync(p => !p.IsDeleted && p.IsActive)
    .OrderBy(p => p.Name);
```

### Step 3: Update JavaScript (Optional Enhancement)

**Option A: Standard HTML multi-select**
- No JavaScript changes needed
- User holds Ctrl/Cmd to select multiple

**Option B: Enhanced multi-select with library**
- Use Select2, Choices.js, or Bootstrap-select
- Provides better UX with search, tags, etc.

```javascript
// Example with Select2
$('#PartnerIds').select2({
    placeholder: 'Select partners...',
    allowClear: true
});
```

### Step 4: Update Form Submission Handling

The `PartnerIds` field binding may need adjustment:
- HTML multi-select sends multiple values with same name
- Controller needs to handle as array or comma-separated string

**Option A: Change ViewModel property to array**
```csharp
public int[]? PartnerIds { get; set; }
```

**Option B: Keep as string, parse in JavaScript before submit**
```javascript
// Before form submit, join selected values
const selected = Array.from(document.getElementById('PartnerIds').selectedOptions)
    .map(opt => opt.value);
document.getElementById('PartnerIds-hidden').value = selected.join(',');
```

### Step 5: Remove Checkbox-Related Code

**wwwroot/js/certificate-create.js**
- Remove or comment out `PartnerManager` component (lines 890-1051)
- Remove checkbox rendering logic
- Remove checkbox event handlers

### Step 6: Update CSS (if using enhanced dropdown)

Add styling for multi-select dropdown if needed.

---

## Files Modified (Summary)

| File | Status | Action Needed |
|------|--------|---------------|
| Models/Company.cs | DONE | None |
| Models/Partner.cs | DONE | None |
| Models/Certificate.cs | DONE | None |
| Models/CertificateEditViewModel.cs | DONE | May need type change |
| Data/ApplicationDbContext.cs | DONE | None |
| Repositories/IUnitOfWork.cs | DONE | None |
| Repositories/UnitOfWork.cs | DONE | None |
| Repositories/CertificateRepository.cs | DONE | None |
| Controllers/CertificateController.cs | DONE | Update ViewBag population |
| Views/Certificate/Create.cshtml | NEEDS CHANGE | Revert to multi-select dropdown |
| Views/Certificate/Edit.cshtml | NEEDS CHANGE | Revert to multi-select dropdown |
| wwwroot/js/certificate-create.js | NEEDS CHANGE | Remove PartnerManager |
| Database Migration | DONE | Already executed |

---

## Testing Checklist

After implementing UI changes:

- [ ] Certificate Create page shows multi-select Partner dropdown
- [ ] Can select single partner
- [ ] Can select multiple partners (Ctrl+click or enhanced UI)
- [ ] Creating certificate saves all selected partners
- [ ] Certificate Edit page shows multi-select dropdown
- [ ] Existing partners are pre-selected on Edit
- [ ] Can add/remove partners on Edit
- [ ] Saving Edit updates partners correctly
- [ ] Legacy Partner string field populated for backward compatibility
- [ ] API responses include Partners array (future)
- [ ] Certificate tile views show multiple partner badges (future)

---

## Questions to Clarify Before Resuming

1. **Multi-select UI preference:**
   - Standard HTML multi-select (Ctrl+click)?
   - Enhanced dropdown with checkboxes inside?
   - Tag/chip style selector?

2. **Should "None" be an option?**
   - Or just leave empty if no partners?

3. **Partner display on certificate tiles:**
   - Show all partner badges?
   - Show count if >2 partners?

---

## Related Documentation

- observations_log.md - Entry dated 2026-01-22
- test_results/MULTI_PARTNER_TEST_REPORT.md
- docs/Database_Scripts/Migrations/14_20260122_AddMultiPartnerSupport.sql

---

**Last Updated:** 2026-01-23
**Next Action:** Clarify multi-select dropdown UI preference, then implement Step 1-5
