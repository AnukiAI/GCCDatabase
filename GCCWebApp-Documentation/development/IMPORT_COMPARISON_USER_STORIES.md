# Import with Comparison & Update - User Stories and Requirements

**Document Version:** 1.0
**Created:** September 30, 2025
**Status:** NEW REQUIREMENT
**Priority:** HIGH

---

## Executive Summary

This document defines requirements for enhancing the existing CSV import functionality to include intelligent comparison logic that compares imported data with existing system data and performs appropriate actions (insert new, update existing, skip duplicates).

**Current State:** Import only creates new records. No comparison with existing data.
**Desired State:** Import compares against existing data and updates/inserts as needed.

---

## EPIC: Smart Import with Data Comparison

### Business Need
When importing certificate data (product IDs, certificates, companies, etc.), users need to:
1. Update existing records with new information
2. Add only new records that don't exist
3. Avoid creating duplicates
4. See a clear report of what was added, updated, or skipped

---

## User Stories

### US-IMPORT-001: Product ID Import with Comparison
**As an** Admin/Analyst
**I want to** import product IDs from CSV and automatically update existing ones
**So that** I don't create duplicates and keep data current

**Acceptance Criteria:**
- [ ] System compares each imported row against existing product IDs
- [ ] Matching logic: Same `ProductIdType` + `ProductIdValue` = match
- [ ] If match found:
  - [ ] UPDATE existing record with new data (Status, Partner, ProductName, etc.)
  - [ ] Preserve original CreatedAt, CreatedBy
  - [ ] Update UpdatedAt, UpdatedBy to current user
  - [ ] Log "Updated" in import report
- [ ] If no match found:
  - [ ] INSERT new record
  - [ ] Set CreatedAt, CreatedBy to current user
  - [ ] Log "Added" in import report
- [ ] If data exactly matches existing (no changes):
  - [ ] SKIP record (no database operation)
  - [ ] Log "Skipped (no changes)" in import report
- [ ] Import summary shows:
  - Total records processed
  - Records added (new)
  - Records updated (existing changed)
  - Records skipped (no changes)
  - Records failed (validation errors)
  - Detailed error list

**Business Rules:**
- Duplicate detection: `(ProductIdType, ProductIdValue)` combination must be unique
- Cannot update ProductIdType or ProductIdValue if it creates a duplicate with another record
- Validation rules apply to both new and updated records
- User must have Analyst or Admin role

**Priority:** HIGH
**Estimate:** 8-12 hours

---

### US-IMPORT-002: Certificate Import with Comparison
**As an** Admin/Analyst
**I want to** import certificates from CSV and update existing ones
**So that** I can bulk update certificate data without manual edits

**Acceptance Criteria:**
- [ ] System compares each imported row against existing certificates
- [ ] Matching logic: Same `CertificateNumber` = match
- [ ] If match found:
  - [ ] UPDATE all editable fields (Status, ExpirationDate, Partner, Location, etc.)
  - [ ] Preserve CreatedAt, CreatedBy, CertificateNumber
  - [ ] Update UpdatedAt, UpdatedBy
  - [ ] Log "Updated" in import report
- [ ] If no match found:
  - [ ] INSERT new certificate
  - [ ] Generate certificate number if not provided
  - [ ] Set audit fields
  - [ ] Log "Added" in import report
- [ ] If data exactly matches (no changes):
  - [ ] SKIP (no operation)
  - [ ] Log "Skipped" in import report
- [ ] Import modes supported:
  - [ ] `Update Only` - Only update existing, don't add new
  - [ ] `Add Only` - Only add new, don't update existing
  - [ ] `Add and Update` (default) - Both add and update

**Business Rules:**
- Certificate number is immutable (cannot be changed via import)
- Certificate lifecycle workflow rules apply
- Related entities (Company, Product, CertificationType) must exist
- Cannot import into "Revoked" or "Archived" status without Admin override

**Priority:** HIGH
**Estimate:** 16-20 hours

---

### US-IMPORT-003: Import Preview Mode
**As an** Admin/Analyst
**I want to** preview import changes before committing them
**So that** I can verify the import will do what I expect

**Acceptance Criteria:**
- [ ] "Preview" checkbox option on import form
- [ ] When checked, import runs in dry-run mode:
  - [ ] All validation and comparison logic executes
  - [ ] NO data is saved to database
  - [ ] Report shows what WOULD happen
- [ ] Preview report shows:
  - [ ] Records that would be added (with sample data)
  - [ ] Records that would be updated (with before/after comparison)
  - [ ] Records that would be skipped
  - [ ] Validation errors
- [ ] User can download preview report as CSV
- [ ] User can proceed with actual import after preview

**Business Rules:**
- Preview mode uses database transaction that's rolled back
- Preview execution time should be similar to actual import
- Preview shows up to 1000 records in detail (for large imports)

**Priority:** MEDIUM
**Estimate:** 6-8 hours

---

### US-IMPORT-004: Company/Product Import with Comparison
**As an** Admin
**I want to** import companies and products with update logic
**So that** I can keep master data current

**Acceptance Criteria:**
- [ ] Company import:
  - [ ] Match by `CompanyName` (case-insensitive)
  - [ ] Update: ContactInfo, Email, Website, Location, Country
  - [ ] Cannot change Company Name via import (requires manual edit)
- [ ] Product import:
  - [ ] Match by `ProductName` + `CompanyId` combination
  - [ ] Update: Description, Model, SKU, UPC, ASIN, Category, etc.
  - [ ] Can reassign to different ProductCategory
- [ ] Both support soft-delete handling:
  - [ ] If importing a soft-deleted record, restore it (IsDeleted = false)
  - [ ] Log as "Restored" in import report

**Business Rules:**
- Soft-deleted records can be restored via import
- Admin-only functionality
- Cannot delete records via import (security measure)

**Priority:** MEDIUM
**Estimate:** 10-12 hours

---

### US-IMPORT-005: Import Conflict Resolution
**As an** Admin/Analyst
**I want** clear rules for handling import conflicts
**So that** I understand what happens when data conflicts occur

**Acceptance Criteria:**
- [ ] Conflict scenarios handled:
  1. **Matching record exists with different data**
     - Action: UPDATE with imported data (imported data wins)
     - Log: "Updated: {field1}, {field2}, ..."

  2. **Duplicate detection across import file**
     - Action: SKIP duplicates within file, process first occurrence
     - Log: "Skipped: Duplicate in import file (line X)"

  3. **Validation fails on update**
     - Action: SKIP record, do not update
     - Log: "Failed: {validation error message}"

  4. **Related entity not found (Foreign key)**
     - Action: SKIP record
     - Log: "Failed: Company '{name}' not found"

  5. **Concurrent modification (optimistic locking)**
     - Action: RETRY once, then FAIL
     - Log: "Failed: Record was modified during import"

- [ ] Conflict resolution strategies configurable:
  - [ ] `ImportWins` (default) - Imported data overwrites existing
  - [ ] `ExistingWins` - Skip if exists, only add new
  - [ ] `MergeNonEmpty` - Only update fields that have values in import file

**Business Rules:**
- Default strategy is `ImportWins`
- Audit trail preserved for all updates
- Errors don't stop entire import (row-level error handling)

**Priority:** HIGH
**Estimate:** 8-10 hours

---

### US-IMPORT-006: Import Audit Trail
**As an** Admin
**I want** detailed audit logs of all import operations
**So that** I can track what data was changed and by whom

**Acceptance Criteria:**
- [ ] Each import operation creates audit record:
  - ImportId (unique identifier)
  - UserId (who performed import)
  - Timestamp
  - EntityType (Certificate, ProductId, Company, etc.)
  - FileName (original CSV filename)
  - TotalProcessed, Added, Updated, Skipped, Failed
  - Duration (execution time)
- [ ] Detailed change log per record:
  - RecordId (which certificate/product/etc.)
  - Action (Added/Updated/Skipped/Failed)
  - OldValues (JSON of previous data)
  - NewValues (JSON of imported data)
  - ChangedFields (list of field names)
  - ErrorMessage (if failed)
- [ ] Admin UI page: `/Admin/ImportHistory`
  - [ ] List all imports with summary stats
  - [ ] Filter by user, date range, entity type
  - [ ] Click to see detailed change log
  - [ ] Download original CSV file
  - [ ] Download import report

**Business Rules:**
- Import history retained for 2 years
- Detailed change logs retained for 90 days (configurable)
- Sensitive data (passwords, tokens) never logged

**Priority:** MEDIUM
**Estimate:** 12-16 hours

---

## Data Models

### ImportLog Table
```csharp
public class ImportLog
{
    public int Id { get; set; }
    public Guid ImportId { get; set; } // Unique per import operation
    public int UserId { get; set; }
    public string EntityType { get; set; } // Certificate, ProductId, etc.
    public string FileName { get; set; }
    public int TotalProcessed { get; set; }
    public int RecordsAdded { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int RecordsFailed { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime ImportedAt { get; set; }
    public bool IsPreview { get; set; } // Dry-run mode
    public string? ErrorSummary { get; set; } // Top-level errors

    // Navigation
    public User User { get; set; }
    public ICollection<ImportLogDetail> Details { get; set; }
}

public class ImportLogDetail
{
    public int Id { get; set; }
    public int ImportLogId { get; set; }
    public string EntityId { get; set; } // Certificate.Id, ProductId.Id, etc.
    public string Action { get; set; } // Added, Updated, Skipped, Failed, Restored
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string? ChangedFields { get; set; } // JSON array
    public string? ErrorMessage { get; set; }
    public int LineNumber { get; set; } // CSV line number

    // Navigation
    public ImportLog ImportLog { get; set; }
}
```

---

## API Enhancements

### Enhanced Import Endpoint
```csharp
[HttpPost("certificate/{certificateId}/product-ids/import")]
public async Task<ActionResult<ImportResult>> ImportProductIds(
    int certificateId,
    IFormFile file,
    [FromForm] ImportOptions options)
{
    // options.Mode: AddOnly, UpdateOnly, AddAndUpdate
    // options.ConflictStrategy: ImportWins, ExistingWins, MergeNonEmpty
    // options.IsPreview: true/false
    // options.ValidateOnly: true/false
}

public class ImportOptions
{
    public ImportMode Mode { get; set; } = ImportMode.AddAndUpdate;
    public ConflictStrategy Strategy { get; set; } = ConflictStrategy.ImportWins;
    public bool IsPreview { get; set; } = false;
    public bool ValidateOnly { get; set; } = false;
    public bool RestoreSoftDeleted { get; set; } = true;
}

public enum ImportMode
{
    AddOnly,        // Only insert new records
    UpdateOnly,     // Only update existing records
    AddAndUpdate    // Both (default)
}

public enum ConflictStrategy
{
    ImportWins,     // Imported data overwrites (default)
    ExistingWins,   // Keep existing, skip if exists
    MergeNonEmpty   // Only update non-empty fields
}

public class ImportResult
{
    public Guid ImportId { get; set; }
    public int TotalProcessed { get; set; }
    public int RecordsAdded { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsSkipped { get; set; }
    public int RecordsRestored { get; set; }
    public int RecordsFailed { get; set; }
    public TimeSpan Duration { get; set; }
    public List<ImportError> Errors { get; set; }
    public List<ImportDetailDto> Details { get; set; }
    public bool IsPreview { get; set; }
}

public class ImportDetailDto
{
    public string Action { get; set; } // Added, Updated, Skipped, Failed
    public string EntityId { get; set; }
    public string EntityDescription { get; set; } // e.g., "ASIN: B08N5WRWNW"
    public Dictionary<string, object>? OldValues { get; set; }
    public Dictionary<string, object>? NewValues { get; set; }
    public List<string>? ChangedFields { get; set; }
    public string? ErrorMessage { get; set; }
    public int LineNumber { get; set; }
}
```

---

## Service Layer Implementation

### IImportService Interface
```csharp
public interface IImportService<T> where T : class
{
    Task<ImportResult> ImportFromCsvAsync(
        Stream csvStream,
        ImportOptions options,
        int userId);

    Task<ImportResult> PreviewImportAsync(
        Stream csvStream,
        ImportOptions options,
        int userId);

    Task<T?> FindMatchingRecordAsync(T entity);

    bool HasChanges(T existing, T imported, ConflictStrategy strategy);

    T MergeChanges(T existing, T imported, ConflictStrategy strategy);

    Task<List<string>> ValidateEntityAsync(T entity, bool isUpdate);
}
```

### Comparison Logic Pseudocode
```csharp
public async Task<ImportResult> ImportFromCsvAsync(Stream csvStream, ImportOptions options, int userId)
{
    var result = new ImportResult();
    var importId = Guid.NewGuid();
    var items = ParseCsv(csvStream);

    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        foreach (var (item, lineNumber) in items)
        {
            try
            {
                // 1. Validate format
                var validationErrors = await ValidateEntityAsync(item, false);
                if (validationErrors.Any())
                {
                    result.RecordsFailed++;
                    result.Errors.Add(new ImportError(lineNumber, validationErrors));
                    continue;
                }

                // 2. Find existing record
                var existing = await FindMatchingRecordAsync(item);

                // 3. Determine action based on mode and existence
                if (existing == null)
                {
                    // NEW RECORD
                    if (options.Mode == ImportMode.UpdateOnly)
                    {
                        result.RecordsSkipped++;
                        result.Details.Add(new ImportDetailDto
                        {
                            Action = "Skipped",
                            EntityDescription = GetDescription(item),
                            ErrorMessage = "UpdateOnly mode - record doesn't exist"
                        });
                        continue;
                    }

                    // Check if soft-deleted
                    var softDeleted = await FindSoftDeletedAsync(item);
                    if (softDeleted != null && options.RestoreSoftDeleted)
                    {
                        // RESTORE
                        softDeleted.IsDeleted = false;
                        softDeleted.DeletedAt = null;
                        MergeChanges(softDeleted, item, options.Strategy);
                        result.RecordsRestored++;
                        result.Details.Add(CreateDetailDto(softDeleted, item, "Restored", lineNumber));
                    }
                    else
                    {
                        // INSERT
                        SetAuditFields(item, userId, true);
                        await _context.AddAsync(item);
                        result.RecordsAdded++;
                        result.Details.Add(CreateDetailDto(null, item, "Added", lineNumber));
                    }
                }
                else
                {
                    // EXISTING RECORD
                    if (options.Mode == ImportMode.AddOnly)
                    {
                        result.RecordsSkipped++;
                        result.Details.Add(new ImportDetailDto
                        {
                            Action = "Skipped",
                            EntityDescription = GetDescription(item),
                            ErrorMessage = "AddOnly mode - record already exists"
                        });
                        continue;
                    }

                    // Check if changes exist
                    if (!HasChanges(existing, item, options.Strategy))
                    {
                        // NO CHANGES
                        result.RecordsSkipped++;
                        result.Details.Add(new ImportDetailDto
                        {
                            Action = "Skipped",
                            EntityDescription = GetDescription(existing),
                            ErrorMessage = "No changes detected"
                        });
                        continue;
                    }

                    // UPDATE
                    var oldValues = CaptureCurrentValues(existing);
                    MergeChanges(existing, item, options.Strategy);
                    SetAuditFields(existing, userId, false);
                    result.RecordsUpdated++;
                    result.Details.Add(CreateDetailDto(oldValues, existing, "Updated", lineNumber));
                }
            }
            catch (Exception ex)
            {
                result.RecordsFailed++;
                result.Errors.Add(new ImportError(lineNumber, ex.Message));
            }
        }

        // Save changes
        if (!options.IsPreview)
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Log import operation
            await LogImportAsync(importId, userId, result);
        }
        else
        {
            await transaction.RollbackAsync();
        }
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw;
    }

    result.ImportId = importId;
    result.IsPreview = options.IsPreview;
    return result;
}
```

---

## CSV File Formats

### Product ID Import CSV
```csv
Type,Value,Status,Partner,ProductName,ProductSku,Description
ASIN,B08N5WRWNW,Active,Amazon,Widget Pro,WP-001,High-quality widget
UPC,123456789012,Active,None,Gadget Ultra,GU-002,Premium gadget
GTIN,12345678901234,Inactive,Wayfair,Tool Set,TS-003,Professional tools
```

### Certificate Import CSV
```csv
CertificateNumber,CompanyName,ProductName,CertificationType,Status,EffectiveDate,ExpirationDate,Partner,Location,Country
24-1605,ABC Company,Widget Pro,CSF,Active,2024-01-01,2026-01-01,Amazon,"Factory A, Building 1",USA
24-1606,XYZ Corp,Gadget Ultra,RC,Active,2024-02-01,2027-02-01,None,Manufacturing Plant,Canada
```

---

## Test Cases

### TC-IMPORT-001: Import New Product IDs
**Prerequisites:** Empty product ID list for certificate
**Steps:**
1. Navigate to certificate edit page
2. Click "Import Product IDs"
3. Upload CSV with 10 product IDs
4. Select mode: "Add and Update"
5. Click "Import"
6. Review import summary

**Expected:**
- All 10 records added
- Import summary shows: 10 added, 0 updated, 0 skipped, 0 failed
- All product IDs visible in product ID manager

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-002: Import Updates Existing Product IDs
**Prerequisites:** Certificate has 5 existing product IDs
**Steps:**
1. Export existing product IDs to CSV
2. Modify 3 records (change Status, ProductName)
3. Import modified CSV with mode "Add and Update"
4. Review import summary

**Expected:**
- Import summary shows: 0 added, 3 updated, 2 skipped (no changes), 0 failed
- Modified fields reflect new values
- CreatedAt/CreatedBy unchanged
- UpdatedAt/UpdatedBy reflect current user and time
- Detail report shows before/after for each updated record

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-003: Import Mixed (Add + Update)
**Prerequisites:** Certificate has 5 existing product IDs
**Steps:**
1. Create CSV with 10 product IDs:
   - 3 match existing (with changes)
   - 2 match existing (no changes)
   - 5 are new
2. Import with mode "Add and Update"
3. Review results

**Expected:**
- Import summary: 5 added, 3 updated, 2 skipped, 0 failed
- Total product IDs after import: 10
- Detail report clearly shows which action for each line

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-004: Import Mode - Add Only
**Prerequisites:** Certificate has 3 existing product IDs
**Steps:**
1. Create CSV with 5 product IDs (2 existing, 3 new)
2. Import with mode "Add Only"
3. Review results

**Expected:**
- Import summary: 3 added, 0 updated, 2 skipped (already exist), 0 failed
- Existing records unchanged
- Detail report shows "Skipped: AddOnly mode - record already exists"

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-005: Import Mode - Update Only
**Prerequisites:** Certificate has 3 existing product IDs
**Steps:**
1. Create CSV with 5 product IDs (3 existing with changes, 2 new)
2. Import with mode "Update Only"
3. Review results

**Expected:**
- Import summary: 0 added, 3 updated, 2 skipped (don't exist), 0 failed
- New records NOT added
- Existing records updated
- Detail report shows "Skipped: UpdateOnly mode - record doesn't exist"

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-006: Import Preview Mode
**Prerequisites:** Certificate has data
**Steps:**
1. Create import CSV
2. Check "Preview" checkbox
3. Submit import
4. Review preview report
5. Verify database unchanged
6. Run actual import without preview
7. Verify database updated

**Expected:**
- Preview shows what WOULD happen
- Database unchanged after preview
- Actual import matches preview predictions
- Preview report downloadable

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-007: Import with Validation Errors
**Prerequisites:** Valid certificate ID
**Steps:**
1. Create CSV with invalid data:
   - Invalid ASIN format (9 chars instead of 10)
   - Invalid UPC check digit
   - Empty required fields
2. Import CSV
3. Review error report

**Expected:**
- Import summary shows: 0 added, 0 updated, 0 skipped, 3 failed
- Error report lists specific validation errors per line
- Error messages clear and actionable
- Valid rows (if any) still processed

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-008: Import with Duplicates in File
**Prerequisites:** Valid certificate ID
**Steps:**
1. Create CSV with duplicate rows:
   - Line 5: ASIN B08N5WRWNW
   - Line 12: ASIN B08N5WRWNW (duplicate)
2. Import CSV
3. Review results

**Expected:**
- First occurrence processed (line 5)
- Second occurrence skipped (line 12)
- Import summary shows skipped with reason "Duplicate in import file (first seen on line 5)"

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-009: Import Conflict Strategy - Import Wins
**Prerequisites:** Certificate has product ID: ASIN B08N5WRWNW with ProductName="Old Name"
**Steps:**
1. Create CSV with same ASIN but ProductName="New Name"
2. Import with strategy "ImportWins"
3. Verify result

**Expected:**
- Product ID updated
- ProductName now shows "New Name"
- Imported data overwrote existing

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-010: Import Conflict Strategy - Existing Wins
**Prerequisites:** Certificate has product ID: ASIN B08N5WRWNW with ProductName="Old Name"
**Steps:**
1. Create CSV with same ASIN but ProductName="New Name"
2. Import with strategy "ExistingWins"
3. Verify result

**Expected:**
- Product ID NOT updated
- ProductName still shows "Old Name"
- Record skipped with reason "ExistingWins strategy - record exists"

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-011: Import Conflict Strategy - Merge Non-Empty
**Prerequisites:** Certificate has product ID: ASIN B08N5WRWNW
- ProductName="Widget"
- ProductSku="" (empty)
- Description="Original description"

**Steps:**
1. Create CSV with same ASIN:
   - ProductName="" (empty)
   - ProductSku="NEW-SKU-001"
   - Description="" (empty)
2. Import with strategy "MergeNonEmpty"
3. Verify result

**Expected:**
- Product ID updated
- ProductName still "Widget" (import was empty, kept existing)
- ProductSku now "NEW-SKU-001" (was empty, took import value)
- Description still "Original description" (import was empty, kept existing)
- Only non-empty fields from import applied

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-012: Import Restores Soft-Deleted Record
**Prerequisites:** Certificate has soft-deleted product ID (IsDeleted=true)
**Steps:**
1. Create CSV with same product ID
2. Import with RestoreSoftDeleted=true
3. Verify result

**Expected:**
- Product ID restored (IsDeleted=false)
- Import summary shows "Restored"
- Data updated with imported values
- DeletedAt/DeletedBy cleared

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-013: Large Import Performance
**Prerequisites:** Test environment
**Steps:**
1. Create CSV with 10,000 product IDs
2. Import CSV
3. Monitor performance

**Expected:**
- Import completes in < 5 minutes
- Progress indicator updates
- No timeouts or memory errors
- All records processed correctly

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-014: Import Audit Trail
**Prerequisites:** Admin user
**Steps:**
1. Perform import operation
2. Navigate to /Admin/ImportHistory
3. Find import in list
4. Click to view details
5. Review change log

**Expected:**
- Import operation logged with summary stats
- Detailed change log shows each record action
- Before/after values captured for updates
- User and timestamp recorded
- Original CSV file downloadable

**Result:** [ ] PASS [ ] FAIL

---

### TC-IMPORT-015: Import Permission Check
**Prerequisites:** External user logged in
**Steps:**
1. Navigate to certificate page
2. Look for import option
3. Try to access import API directly

**Expected:**
- Import functionality not visible
- API returns 403 Forbidden
- External users cannot import

**Result:** [ ] PASS [ ] FAIL

---

## Non-Functional Requirements

### Performance
- Import of 1,000 records: < 30 seconds
- Import of 10,000 records: < 5 minutes
- Preview mode should be within 20% of actual import time

### Security
- Only Admin/Analyst roles can import
- All imports logged with user identity
- No SQL injection via CSV content
- File size limit: 50 MB
- CSV validation before processing

### Usability
- Clear error messages for validation failures
- Progress indicator for large imports
- Downloadable import report
- Sample CSV template available
- Inline help text explaining import options

### Reliability
- Transaction rollback on critical errors
- Row-level error handling (don't fail entire import)
- Retry logic for transient errors
- Import history retained for 2 years

---

## Implementation Checklist

### Phase 1: Core Comparison Logic (Week 1)
- [ ] Create ImportLog/ImportLogDetail models
- [ ] Create database migration
- [ ] Implement IImportService interface
- [ ] Implement FindMatchingRecordAsync for ProductIds
- [ ] Implement HasChanges comparison logic
- [ ] Implement MergeChanges logic
- [ ] Add ConflictStrategy support
- [ ] Unit tests for comparison logic

### Phase 2: Import Service Enhancement (Week 2)
- [ ] Enhance ProductIdService.ImportFromCsvAsync
- [ ] Add ImportOptions parameter support
- [ ] Implement preview mode (dry-run)
- [ ] Add soft-delete restoration
- [ ] Implement duplicate detection in file
- [ ] Add detailed error reporting
- [ ] Integration tests

### Phase 3: API & UI (Week 3)
- [ ] Update ProductIdApiController import endpoint
- [ ] Add import options to UI form
- [ ] Add preview checkbox
- [ ] Show detailed import results in UI
- [ ] Add import history page (/Admin/ImportHistory)
- [ ] Add download report functionality
- [ ] UI/UX testing

### Phase 4: Extended Entities (Week 4)
- [ ] Implement import comparison for Certificates
- [ ] Implement import comparison for Companies
- [ ] Implement import comparison for Products
- [ ] Add CSV templates for each entity
- [ ] Comprehensive testing

### Phase 5: Polish & Documentation (Week 5)
- [ ] Performance optimization for large imports
- [ ] Add progress indicators
- [ ] Error message improvements
- [ ] User documentation
- [ ] Admin guide for import troubleshooting
- [ ] UAT testing

---

## Success Metrics

- 100% of imports have audit trail
- < 5% of imports result in validation errors (after initial user training)
- User satisfaction rating > 4/5 for import feature
- Zero data corruption incidents from imports
- Import feature used for > 50% of bulk data operations

---

*This document serves as the comprehensive requirements specification for the import comparison and update feature. All development work should reference this document for acceptance criteria and test cases.*