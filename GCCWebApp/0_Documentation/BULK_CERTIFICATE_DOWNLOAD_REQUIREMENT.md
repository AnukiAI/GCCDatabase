# Bulk Certificate Download Requirement

## Overview
Add functionality to download multiple selected certificates as a single ZIP file from both the External and Internal Certificate Tile View pages.

## Current Behavior
- "Download Selected" button downloads certificates one at a time
- Each download has a 500ms delay between requests
- Multiple individual PDF downloads can be blocked by browser popup blockers
- No way to download all selected certificates in a single operation

## Required Behavior

### Feature: Bulk Download as ZIP
**User Story**: As an External User, Analyst, or Admin, I want to download multiple selected certificates as a single ZIP file so that I can easily manage and share multiple certificates without downloading them individually.

### Functional Requirements

#### FR-1: Download Selected as ZIP
- **Description**: When user clicks "Download Selected" button with certificates selected in table view, download all selected certificate PDFs in a single ZIP file
- **Scope**: Both External (`/Certificate/CertificateTileViewExternal`) and Internal (`/Certificate/CertificateTileViewInternal`) views
- **Prerequisites**:
  - User must be in List/Table view (not Tile view)
  - At least one certificate must be selected via checkbox
  - Selected certificates must have uploaded PDF files
- **Behavior**:
  - System creates a ZIP archive containing all selected certificate PDFs
  - Only includes certificates that have PDF files uploaded
  - Skips certificates without PDF files and logs them
  - ZIP file naming convention: `certificates_YYYY-MM-DD_HHmmss.zip`
  - Download initiates automatically via browser download
  - User receives success notification with count of downloaded certificates

#### FR-2: Certificate Filtering in ZIP
- **Description**: Only include certificates with uploaded PDF files
- **Behavior**:
  - Check each selected certificate for PDF file existence
  - Only add to ZIP if PDF file exists in storage
  - If some selected certificates have no PDF:
    - Still create ZIP with available PDFs
    - Show warning notification: "X of Y certificates downloaded. Z certificates had no PDF files."
  - If NO selected certificates have PDFs:
    - Show error notification: "None of the selected certificates have PDF files available."
    - Do not create empty ZIP

#### FR-3: ZIP File Structure
- **Structure**:
  ```
  certificates_2025-10-23_130530.zip
  ├── Certificate_24-0366.pdf
  ├── Certificate_24-0367.pdf
  ├── Certificate_24-0368.pdf
  └── ...
  ```
- **File Naming**: Each PDF uses format `Certificate_{CertificateNumber}.pdf`
- **Max Size**: Limit ZIP to reasonable size (suggest 500MB max, configurable)
- **Large Downloads**: For large selections, show progress indicator

#### FR-4: User Feedback
- **Before Download**:
  - If no certificates selected: "Please select certificates to download"
  - If selection too large: "Selection exceeds maximum download size. Please select fewer certificates."
- **During Download**:
  - Show progress modal: "Preparing ZIP file... (X of Y certificates)"
  - Prevent user from navigating away or starting another download
- **After Download**:
  - Success: "Downloaded X certificate(s) successfully"
  - Partial success: "Downloaded X of Y certificates. Z certificates had no PDF files."
  - Failure: "Failed to download certificates. Please try again."

### Technical Requirements

#### TR-1: Backend Implementation
- **Controller**: Add new endpoint to `CertificateController.cs`
  - Route: `POST /Certificate/DownloadSelectedZip`
  - Accepts: JSON array of certificate IDs
  - Returns: ZIP file stream with `application/zip` content type
- **Service**: Add method to `CertificateService.cs` or create new `BulkDownloadService.cs`
  - Method: `Task<byte[]> CreateCertificateZipAsync(List<int> certificateIds)`
  - Use `System.IO.Compression.ZipArchive` to create ZIP
  - Read PDF files from storage (file system or database)
  - Handle missing files gracefully
- **Error Handling**:
  - Catch file not found exceptions
  - Catch ZIP creation errors
  - Log all errors with certificate IDs
  - Return partial results when possible

#### TR-2: Frontend Implementation
- **External View**: Update `certificateTileViewExternal.js`
  - Modify `downloadSelected()` method (line 1125)
  - Call new `/Certificate/DownloadSelectedZip` endpoint
  - Pass array of selected certificate IDs
  - Handle ZIP file download response
- **Internal View**: Update `certificate-tile-view-internal.js`
  - Add similar `downloadSelected()` method
  - Call same endpoint
  - Handle ZIP file download response
- **Progress Indicator**: Show modal during ZIP creation
  - Display: "Preparing download..."
  - Prevent multiple simultaneous downloads
  - Auto-close on completion or error

#### TR-3: Performance Considerations
- **Streaming**: Use streaming for large ZIP files to avoid memory issues
- **Timeout**: Set appropriate timeout (suggest 5 minutes for large downloads)
- **Concurrency**: Limit concurrent ZIP creation operations (max 3 simultaneous)
- **Cleanup**: Automatically delete temporary ZIP files after download (if created on disk)

### Security Requirements

#### SR-1: Authorization
- **External Users**: Can only download certificates matching their partner assignment
- **Analysts**: Can download any certificate they have read access to
- **Admins**: Can download any certificate
- **Enforcement**: Apply same authorization checks as individual certificate download

#### SR-2: File Validation
- **PDF Validation**: Verify each file is a valid PDF before adding to ZIP
- **Path Traversal**: Sanitize certificate numbers to prevent directory traversal attacks
- **Size Limits**: Enforce maximum ZIP size to prevent DoS

### Acceptance Criteria

1. **Given** a user is on External or Internal Certificate Tile View in List mode
   **When** they select 5 certificates and click "Download Selected"
   **Then** a ZIP file containing all 5 certificate PDFs downloads automatically

2. **Given** a user selects 10 certificates where 3 have no PDF files
   **When** they click "Download Selected"
   **Then** a ZIP with 7 PDFs downloads and a warning message displays

3. **Given** a user selects certificates exceeding the size limit
   **When** they click "Download Selected"
   **Then** an error message displays and no download occurs

4. **Given** an External user selects certificates from multiple partners
   **When** they click "Download Selected"
   **Then** only certificates matching their partner assignment are downloaded

5. **Given** a download is in progress
   **When** user tries to start another download
   **Then** the system prevents the second download and shows "Download in progress"

## Implementation Priority
**Priority**: High
**Sprint**: Next available sprint
**Estimated Effort**: 8 story points (2-3 days)

## Dependencies
- PDF file storage location (file system path or database)
- Existing authorization infrastructure
- Certificate file upload functionality must be working

## Related Files
- `Controllers/CertificateController.cs` - Add ZIP download endpoint
- `Services/CertificateService.cs` - Add ZIP creation logic
- `wwwroot/js/certificateTileViewExternal.js:1125` - Update download method
- `wwwroot/js/certificate-tile-view-internal.js` - Add download method
- `Views/Certificate/CertificateTileViewExternal.cshtml` - No changes needed
- `Views/Certificate/CertificateTileViewInternal.cshtml` - No changes needed

## Testing Requirements
- Unit tests for ZIP creation logic
- Integration tests for controller endpoint
- E2E tests for both External and Internal views
- Security tests for authorization
- Performance tests for large selections (100+ certificates)

## Documentation Updates
- Update user guide with ZIP download feature
- Update API documentation with new endpoint
- Update CLAUDE.md with new requirement

---
**Created**: 2025-10-23
**Status**: Pending Implementation
**Requested By**: User
**Documented By**: Claude Code
