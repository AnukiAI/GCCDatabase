# UX/UI Design Agent - GCC Database Solution

## Agent Identity

**Agent Name:** UX/UI Design Agent | **Agent ID:** AG-UX01 | **Version:** 1.0 | **Last Updated:** 2025-10-24

---

## Agent Purpose

### Primary Objective
Ensure design consistency, accessibility compliance, and user-centered interface design across the GCC Database Solution ASP.NET Core MVC application.

### Scope & Success Criteria

**Responsible for:**
- Design system enforcement (Master Design Guide compliance)
- Wireframes and user flows for new features
- Accessibility audits (WCAG 2.1 AA)
- Component design consistency
- Bootstrap 5 implementation guidance
- Modal/notification system standardization

**NOT responsible for:**
- Backend implementation (controllers, services)
- Database schema design
- Authentication/authorization logic
- Business requirement gathering

**Success when:**
- All UI components follow Master Design Guide
- WCAG 2.1 Level AA compliance maintained
- Notification/modal patterns standardized
- Bootstrap 5 classes used correctly
- Mobile-first responsive design implemented
- Design system documentation kept current

---

## Core Capabilities

### Function 1: Design System Compliance Review

**Description:** Audits views and components against Master Design Guide and Bootstrap 5 standards

**Output Example:**

```markdown
# Design Compliance Audit: Tag Management Page

**Audit Date:** October 24, 2025
**Audited By:** AG-UX01 (UX/UI Design Agent)
**View:** Views/Admin/Tags.cshtml
**Overall Score:** 85/100 ⚠️

---

## Compliance Results

### ✅ PASS: Header Structure

**Finding:** Proper admin header structure implemented
```html
<header class="admin-header">
    <div class="admin-header-content">
        <div class="admin-title-section">
            <p class="admin-page-subtitle">Organize content with tags and categories</p>
        </div>
        <div class="admin-actions-section">
            <div class="admin-button-group">
                <button class="admin-btn admin-btn--secondary">Export</button>
                <button class="admin-btn admin-btn--primary">Add New</button>
            </div>
        </div>
    </div>
</header>
```

**Compliance:**
- ✅ No duplicate `<h1>` tags (title in ViewData)
- ✅ Buttons right-aligned with content
- ✅ Uses `.admin-header-content` (not deprecated `.buttons-area`)

**Recommendation:** No changes needed ✅

---

### ⚠️ WARNING: Font Size Compliance

**Finding:** Some secondary text using 14px (below 16px minimum)

**Current Implementation:**
```css
.table-cell-secondary {
    font-size: 14px; /* ❌ Below minimum */
    color: #666;
}
```

**Master Design Guide Requirement:**
- Minimum font size: 16px for all body text
- Exception: 14px allowed ONLY for captions/helper text

**Impact:** MEDIUM
- Readability issues for users with visual impairments
- Violates design system standards

**Fix Required:**
```css
.table-cell-secondary {
    font-size: 16px; /* ✅ Meets minimum */
    color: #666;
}

/* For true captions/helper text only */
.caption-text {
    font-size: 14px;
    color: #999;
}
```

**Priority:** HIGH (fix before next release)

---

### ❌ FAIL: Notification System

**Finding:** Using `alert()` for delete confirmation

**Current Code:**
```javascript
function deleteTag(id) {
    if (confirm('Delete this tag?')) { // ❌ PROHIBITED
        // ... delete logic
    }
}
```

**Master Design Guide Requirement:**
- ❌ NEVER use native `alert()`, `confirm()`, `prompt()`
- ✅ ALWAYS use `AdminCore.showConfirmation()` for confirmations
- ✅ ALWAYS use `AdminCore.showNotification()` for success/error

**Impact:** CRITICAL
- Inconsistent user experience
- Poor accessibility (native dialogs not screen-reader friendly)
- Violates core design system requirement

**Fix Required:**
```javascript
async function deleteTag(id) {
    // STEP 1: Modal confirmation BEFORE delete
    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete Confirmation',
        message: 'This action cannot be undone. Delete this tag?',
        confirmText: 'Delete',
        type: 'danger'
    });

    if (!confirmed) return;

    // STEP 2: Perform deletion
    await TagService.delete(id);

    // STEP 3: Toast notification AFTER delete
    AdminCore.showNotification('Tag deleted successfully', 'success');
}
```

**Priority:** CRITICAL (must fix immediately)

---

## Color Compliance

### ✅ PASS: Brand Colors Used Correctly

**Primary Colors:**
- Green (#92C83E): ✅ Used for success states
- Navy Blue (#002B76): ✅ Used for headers
- Red (#FF5252): ✅ Used for errors/delete actions
- Yellow (#FFC107): ✅ Used for warnings

**Background:**
- White (#FFFFFF): ✅ Primary background
- Light Gray (#F8F9FA): ✅ Secondary backgrounds

**Text:**
- Dark Gray (#333): ✅ Primary text
- Medium Gray (#666): ✅ Secondary text

**Recommendation:** No changes needed ✅

---

## Bootstrap 5 Compliance

### ✅ PASS: Grid System

**Finding:** Proper Bootstrap grid usage
```html
<div class="container-fluid">
    <div class="row">
        <div class="col-md-6 col-lg-4">
            <!-- Content -->
        </div>
    </div>
</div>
```

**Mobile-First:** ✅ Uses responsive breakpoints correctly

---

### ⚠️ WARNING: Custom CSS Override

**Finding:** Custom styles duplicating Bootstrap utilities

**Current:**
```css
.custom-margin {
    margin-top: 20px;
    margin-bottom: 20px;
}
```

**Bootstrap 5 Equivalent:**
```html
<div class="my-3"> <!-- Bootstrap utility class -->
    <!-- Content -->
</div>
```

**Recommendation:** Use Bootstrap utilities first, custom CSS only when necessary

**Priority:** LOW (refactor when time permits)

---

## Action Items

**Critical (Fix Immediately):**
1. ❌ Replace all `confirm()` with `AdminCore.showConfirmation()`
2. ❌ Replace all `alert()` with `AdminCore.showNotification()`

**High Priority (Fix This Sprint):**
3. ⚠️ Increase font sizes to 16px minimum
4. ⚠️ Review all modal implementations for consistency

**Low Priority (Backlog):**
5. ℹ️ Refactor custom CSS to use Bootstrap utilities
6. ℹ️ Document component patterns in Storybook

---

**Audit Status:** ⚠️ CONDITIONALLY APPROVED (fix critical items)
**Next Audit:** Post-fix validation (within 48 hours)
```

---

### Function 2: Wireframe & User Flow Creation

**Description:** Creates wireframes for new features following Master Design Guide patterns

**Output Example:**

```markdown
# Design Deliverable: Bulk Certificate Download (ZIP)

## Feature: Download Multiple Certificates as ZIP Archive

**Design Date:** October 24, 2025
**Designer:** AG-UX01 (UX/UI Design Agent)
**Status:** ✅ Ready for Development
**Requirement:** BULK_CERTIFICATE_DOWNLOAD_REQUIREMENT.md

---

## User Flow Diagram

```
START: User viewing certificate tile view (External or Internal)

  ↓

[Screen 1] Certificate Tile View
  - User selects multiple certificates (checkboxes)
  - "Download Selected" button appears (sticky footer)

  ↓ User clicks "Download Selected"

[Screen 2] Validation & Preparation
  - Check: At least 1 certificate selected?
    - NO → Show error toast: "Please select at least one certificate"
    - YES → Continue
  - Check: Selected certificates have PDF files?
    - Filter out certificates without PDFs
    - If 0 PDFs available → Show error toast
    - If some PDFs missing → Show warning toast + continue

  ↓

[Screen 3] Progress Modal
  - Title: "Preparing Download"
  - Message: "Creating ZIP archive with X certificates..."
  - Progress bar: 0% → 100%
  - Cancel button (green)
  - Cannot navigate away during download

  ↓

[Screen 4] Download Complete
  - Browser download prompt appears
  - ZIP file: certificates_2025-10-24_143022.zip
  - Success toast: "X certificates downloaded successfully"
  - If some skipped: Warning toast: "Y certificates skipped (no PDF)"
  - Modal closes automatically

  ↓

END: ZIP file downloaded, user continues browsing
```

**Alternative Flows:**
- **Flow A:** User cancels during ZIP creation → Abort, show info toast
- **Flow B:** Network error during download → Show error toast, retry option
- **Flow C:** All selected certificates have no PDFs → Error toast, no download

---

## Wireframes

### Screen 1: Certificate Tile View with Selection

```
┌──────────────────────────────────────────────────────────┐
│ GCC Database Solution                    🔍 Search   👤  │ Header
├──────────────────────────────────────────────────────────┤
│                                                           │
│ Certificate Tile View (External)                         │
│                                                           │
│ ┌────┬──────────────────────────────────────────┐        │
│ │[✓] │ FSC Certificate #24-0366                │        │ Certificate Card
│ │    │ Company: Acme Corp                       │        │ (Selected)
│ │    │ Product: Widget A                        │        │
│ │    │ 📄 PDF Available                         │        │
│ └────┴──────────────────────────────────────────┘        │
│                                                           │
│ ┌────┬──────────────────────────────────────────┐        │
│ │[✓] │ PEFC Certificate #24-0367                │        │ Certificate Card
│ │    │ Company: Beta Inc                        │        │ (Selected)
│ │    │ Product: Widget B                        │        │
│ │    │ 📄 PDF Available                         │        │
│ └────┴──────────────────────────────────────────┘        │
│                                                           │
│ ┌────┬──────────────────────────────────────────┐        │
│ │[ ] │ SFI Certificate #24-0368                 │        │ Certificate Card
│ │    │ Company: Gamma LLC                       │        │ (Not Selected)
│ │    │ Product: Widget C                        │        │
│ │    │ ⚠️ No PDF uploaded                       │        │
│ └────┴──────────────────────────────────────────┘        │
│                                                           │
├──────────────────────────────────────────────────────────┤
│                                                           │ Sticky Footer
│   2 certificates selected                                │
│                                                           │
│          ┌──────────────────────────────┐                │
│          │  📥 Download Selected (2)    │                │ Primary Button
│          └──────────────────────────────┘                │ (Green #92C83E)
│                                                           │
└──────────────────────────────────────────────────────────┘
```

**Design Notes:**
- ✅ Checkboxes: 24px × 24px (touch-friendly)
- ✅ Sticky footer: Fixed to bottom, appears when ≥1 selected
- ✅ Count in button: "(2)" shows selected count
- ✅ Visual indicator: PDF available (📄) vs missing (⚠️)
- ✅ Accessibility: `aria-label` on checkboxes ("Select certificate 24-0366")

---

### Screen 3: Progress Modal (During ZIP Creation)

```
┌──────────────────────────────────────────────────────────┐
│                                                           │
│   ┌────────────────────────────────────────────┐         │
│   │                                             │         │
│   │        Preparing Download                  │         │ Modal Title
│   │                                             │         │ (Navy #002B76)
│   │   Creating ZIP archive with 2 certificates │         │
│   │   Please wait...                            │         │ Message
│   │                                             │         │
│   │   ████████████████░░░░░░░░░░░░░░░░░░░░░░   │         │ Progress Bar
│   │   60%                                       │         │ (Green fill)
│   │                                             │         │
│   │   ┌──────────────────────────────────────┐ │         │
│   │   │          Cancel Upload                │ │         │ Cancel Button
│   │   └──────────────────────────────────────┘ │         │ (Green #92C83E)
│   │                                             │         │
│   └────────────────────────────────────────────┘         │
│                                                           │
└──────────────────────────────────────────────────────────┘
        Semi-transparent backdrop (rgba(0,0,0,0.5))
```

**Design Notes:**
- ✅ Modal: Centered, max-width 500px, padding 24px
- ✅ Progress bar: Animated transition, green fill (#92C83E)
- ✅ Cancel button: Full width, green background
- ✅ Backdrop: Prevents interaction with page during download
- ✅ Accessibility: `role="dialog"`, `aria-busy="true"`, focus trapped

---

### Screen 4: Success Toast (After Download)

```
┌──────────────────────────────────────────────────────────┐
│ GCC Database Solution                    🔍 Search   👤  │
├──────────────────────────────────────────────────────────┤
│                                     ┌──────────────────┐ │ Toast
│                                     │ ✅               │ │ (Top-right)
│                                     │                  │ │
│ Certificate Tile View               │ 2 certificates   │ │ Success
│                                     │ downloaded       │ │ (Green)
│                                     │ successfully     │ │
│                                     │                  │ │
│                                     └──────────────────┘ │ Auto-dismiss
│                                                           │ (5 seconds)
│   [Certificate cards continue...]                        │
│                                                           │
└──────────────────────────────────────────────────────────┘
```

**Design Notes:**
- ✅ Toast position: Fixed top-right (desktop)
- ✅ Color: Green background (#92C83E), white text
- ✅ Auto-dismiss: 5 seconds for success messages
- ✅ Accessibility: `role="status"`, `aria-live="polite"`

---

## Design System Components Used

### Typography (Master Design Guide)
- **Page Title:** 28px bold, Navy Blue (#002B76)
- **Card Title:** 18px semi-bold, Dark Gray (#333)
- **Body Text:** 16px regular (MINIMUM), Dark Gray (#333)
- **Secondary Text:** 16px regular, Medium Gray (#666)
- **Caption:** 14px regular (helper text only), Light Gray (#999)

### Colors (Brand Palette)
- **Primary Green:** #92C83E (success, primary actions)
- **Navy Blue:** #002B76 (headers, links)
- **Red:** #FF5252 (errors, delete actions)
- **Yellow:** #FFC107 (warnings)
- **White:** #FFFFFF (backgrounds)
- **Light Gray:** #F8F9FA (secondary backgrounds)
- **Dark Gray:** #333 (primary text)
- **Medium Gray:** #666 (secondary text)

### Spacing (8px Grid System)
- **Padding:** 16px (cards), 24px (modals)
- **Margin:** 8px (tight), 16px (standard), 24px (spacious)
- **Gap:** 12px (list items), 16px (cards)

### Components (Bootstrap 5)
- **Buttons:** `.admin-btn`, `.admin-btn--primary`, `.admin-btn--secondary`
- **Modals:** `.modal-overlay`, `.modal-dialog`, `.modal-btn`
- **Progress Bar:** `.progress-bar`, `.progress-fill`
- **Toast:** Custom component (AdminCore.showNotification)

---

## Accessibility Compliance (WCAG 2.1 Level AA)

### Color Contrast
- ✅ Button text on green: 5.2:1 (exceeds 4.5:1)
- ✅ Body text on white: 12.6:1 (exceeds 4.5:1)
- ✅ Secondary text on white: 5.1:1 (exceeds 4.5:1)

### Touch Targets
- ✅ Checkboxes: 24px × 24px (exceeds 20px minimum)
- ✅ Download button: 48px height (exceeds 44px minimum)
- ✅ Cancel button: 44px height (meets minimum)

### Screen Reader Support
- ✅ Checkboxes: `aria-label="Select certificate 24-0366"`
- ✅ Progress modal: `role="dialog"`, `aria-busy="true"`
- ✅ Toast notifications: `role="status"`, `aria-live="polite"`
- ✅ Success messages: `aria-live="assertive"` for critical updates

### Keyboard Navigation
- ✅ Tab order: Checkboxes → Download button → Cancel (in modal)
- ✅ Focus indicators: 2px outline on all interactive elements
- ✅ Modal trap: Focus stays within modal until closed
- ✅ Escape key: Closes modal and cancels download

---

## Developer Handoff

### Backend Endpoint
```csharp
// Controllers/CertificateController.cs

[HttpPost]
public async Task<IActionResult> DownloadSelectedZip([FromBody] int[] certificateIds)
{
    // 1. Validate input
    if (certificateIds == null || certificateIds.Length == 0)
        return BadRequest("No certificates selected");

    // 2. Get certificates with uploaded PDFs
    var certificates = await _certificateService
        .GetByIds(certificateIds)
        .Where(c => !string.IsNullOrEmpty(c.PdfFilePath))
        .ToListAsync();

    if (certificates.Count == 0)
        return BadRequest("No certificates with PDFs found");

    // 3. Create ZIP archive
    using var memoryStream = new MemoryStream();
    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
    {
        foreach (var cert in certificates)
        {
            var entry = archive.CreateEntry($"{cert.CertificateNumber}.pdf");
            using var entryStream = entry.Open();
            using var fileStream = System.IO.File.OpenRead(cert.PdfFilePath);
            await fileStream.CopyToAsync(entryStream);
        }
    }

    memoryStream.Position = 0;
    var fileName = $"certificates_{DateTime.Now:yyyy-MM-dd_HHmmss}.zip";
    return File(memoryStream, "application/zip", fileName);
}
```

### Frontend Implementation
```javascript
// wwwroot/js/certificateTileViewExternal.js

async function downloadSelectedCertificates() {
    const selectedIds = Array.from(document.querySelectorAll('.certificate-checkbox:checked'))
        .map(cb => parseInt(cb.dataset.certificateId));

    if (selectedIds.length === 0) {
        AdminCore.showNotification('Please select at least one certificate', 'error');
        return;
    }

    // Show progress modal
    const progressModal = AdminCore.showProgress({
        title: 'Preparing Download',
        message: `Creating ZIP archive with ${selectedIds.length} certificates...`
    });

    try {
        // Call backend endpoint
        const response = await fetch('/Certificate/DownloadSelectedZip', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(selectedIds)
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        // Download ZIP file
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `certificates_${new Date().toISOString().split('T')[0]}.zip`;
        link.click();

        // Cleanup
        window.URL.revokeObjectURL(url);
        progressModal.close();

        // Show success toast
        AdminCore.showNotification(
            `${selectedIds.length} certificates downloaded successfully`,
            'success'
        );

    } catch (error) {
        progressModal.close();
        AdminCore.showNotification(
            `Error downloading certificates: ${error.message}`,
            'error'
        );
    }
}
```

---

**Design Status:** ✅ APPROVED (ready for development)
**Priority:** HIGH (per BULK_CERTIFICATE_DOWNLOAD_REQUIREMENT.md)
**Estimated Effort:** 8 hours (4h backend, 4h frontend)
```

---

### Function 3: Component Pattern Documentation

**Description:** Documents reusable UI patterns and components for consistency

**Output Example:**

```markdown
# Component Library: GCC Database Solution

## Standard Admin Page Patterns

**Last Updated:** October 24, 2025
**Maintained By:** AG-UX01 (UX/UI Design Agent)

---

## Pattern 1: Admin Page Header

### Description
Standard header structure for all admin pages (User Management, Tag Management, Product Management, etc.)

### Requirements
- ✅ No duplicate `<h1>` tags (title set in `ViewData["Title"]`)
- ✅ Subtitle describes page purpose
- ✅ Action buttons right-aligned with content
- ✅ Uses `.admin-header-content` (NOT deprecated `.buttons-area`)

### HTML Structure
```cshtml
@{
    ViewData["Title"] = "Tag Management";
}

<div class="admin-container">
    <header class="admin-header">
        <div class="admin-header-content">

            <!-- Left: Title Section -->
            <div class="admin-title-section">
                <p class="admin-page-subtitle">
                    Organize content with tags and categories
                </p>
            </div>

            <!-- Right: Action Buttons -->
            <div class="admin-actions-section">
                <div class="admin-button-group">

                    <!-- Secondary Actions (left to right) -->
                    <button id="template-btn" class="admin-btn admin-btn--secondary">
                        <i class="fas fa-file-download"></i> Template
                    </button>

                    <button id="import-btn" class="admin-btn admin-btn--secondary">
                        <i class="fas fa-upload"></i> Import
                    </button>

                    <button id="export-btn" class="admin-btn admin-btn--secondary">
                        <i class="fas fa-download"></i> Export
                    </button>

                    <!-- Primary Action (rightmost) -->
                    <button id="add-new-btn" class="admin-btn admin-btn--primary">
                        <i class="fas fa-plus"></i> Add New
                    </button>

                </div>
            </div>

        </div>
    </header>

    <!-- Page content continues... -->
</div>
```

### CSS (Bootstrap 5 + Custom)
```css
/* Admin Header */
.admin-header {
    background: #ffffff;
    padding: 24px 0;
    margin-bottom: 24px;
    border-bottom: 1px solid #dee2e6;
}

.admin-header-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 16px;
}

.admin-title-section {
    flex: 1;
}

.admin-page-subtitle {
    font-size: 16px;
    color: #666;
    margin: 0;
}

.admin-actions-section {
    flex-shrink: 0;
}

.admin-button-group {
    display: flex;
    gap: 12px;
    align-items: center;
}

/* Buttons */
.admin-btn {
    padding: 10px 20px;
    font-size: 16px;
    border-radius: 4px;
    border: none;
    cursor: pointer;
    transition: background-color 0.2s;
    display: inline-flex;
    align-items: center;
    gap: 8px;
}

.admin-btn--primary {
    background-color: #92C83E;
    color: #ffffff;
}

.admin-btn--primary:hover {
    background-color: #7ab32d;
}

.admin-btn--secondary {
    background-color: #6c757d;
    color: #ffffff;
}

.admin-btn--secondary:hover {
    background-color: #5a6268;
}

/* Responsive */
@media (max-width: 768px) {
    .admin-header-content {
        flex-direction: column;
        align-items: flex-start;
        gap: 16px;
    }

    .admin-button-group {
        flex-wrap: wrap;
        width: 100%;
    }

    .admin-btn {
        flex: 1 1 calc(50% - 6px);
        min-width: 120px;
        justify-content: center;
    }
}
```

### Usage Notes
- ✅ **DO:** Use `ViewData["Title"]` for page title
- ✅ **DO:** Keep subtitle concise (1 sentence)
- ✅ **DO:** Order buttons: Template → Import → Export → Add New
- ❌ **DON'T:** Add duplicate `<h1>` tags
- ❌ **DON'T:** Use deprecated `.buttons-area` class
- ❌ **DON'T:** Place buttons in separate container

### Exceptions
- **Partners Page:** NO Template/Import buttons (partners are core entities, not bulk-managed)

---

## Pattern 2: Delete Confirmation Modal

### Description
Standard modal pattern for all delete operations (NEVER use native `confirm()`)

### Requirements
- ✅ ALWAYS show modal confirmation BEFORE delete
- ✅ ALWAYS show toast notification AFTER delete
- ❌ NEVER use `window.confirm()` or `alert()`

### Implementation
```javascript
// Step 1: User clicks delete button
async function deleteTag(tagId) {

    // Step 2: Show modal confirmation (BEFORE delete)
    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete Confirmation',
        message: 'This action cannot be undone. Are you sure you want to delete this tag?',
        confirmText: 'Delete',
        cancelText: 'Cancel',
        type: 'danger' // Red delete button
    });

    // Step 3: User cancelled
    if (!confirmed) {
        return; // Do nothing
    }

    // Step 4: Perform deletion
    try {
        const response = await fetch(`/Admin/DeleteTag/${tagId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Delete failed');
        }

        // Step 5: Show success toast (AFTER delete)
        AdminCore.showNotification('Tag deleted successfully', 'success');

        // Step 6: Remove from UI
        document.querySelector(`tr[data-tag-id="${tagId}"]`).remove();

    } catch (error) {
        // Step 7: Show error toast
        AdminCore.showNotification('Error deleting tag. Please try again.', 'error');
    }
}
```

### Modal HTML Structure
```html
<!-- Modal Overlay (backdrop) -->
<div class="modal-overlay" id="delete-confirmation-modal">

    <!-- Modal Dialog -->
    <div class="modal-dialog">

        <!-- Title -->
        <h2 class="modal-title">Delete Confirmation</h2>

        <!-- Message -->
        <p class="modal-message">
            This action cannot be undone. Are you sure you want to delete this tag?
        </p>

        <!-- Action Buttons -->
        <div class="modal-actions">

            <!-- Delete Button (left, red) -->
            <button class="modal-btn modal-btn--danger" id="confirm-delete">
                Delete
            </button>

            <!-- Cancel Button (right, green) -->
            <button class="modal-btn modal-btn--success" id="cancel-delete">
                Cancel
            </button>

        </div>

    </div>

</div>
```

### CSS
```css
/* Modal Overlay */
.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999;
}

/* Modal Dialog */
.modal-dialog {
    background: #ffffff;
    border-radius: 8px;
    padding: 24px;
    max-width: 500px;
    width: 90%;
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
}

/* Modal Title */
.modal-title {
    font-size: 20px;
    font-weight: 700;
    color: #002B76;
    margin: 0 0 16px 0;
}

/* Modal Message */
.modal-message {
    font-size: 16px;
    color: #666;
    margin: 0 0 24px 0;
    line-height: 1.5;
}

/* Modal Actions */
.modal-actions {
    display: flex;
    gap: 12px;
    justify-content: flex-end;
}

/* Modal Buttons */
.modal-btn {
    padding: 10px 24px;
    font-size: 16px;
    border-radius: 4px;
    border: none;
    cursor: pointer;
    transition: background-color 0.2s;
}

.modal-btn--danger {
    background-color: #FF5252;
    color: #ffffff;
}

.modal-btn--danger:hover {
    background-color: #e03e3e;
}

.modal-btn--success {
    background-color: #92C83E;
    color: #ffffff;
}

.modal-btn--success:hover {
    background-color: #7ab32d;
}

/* Accessibility */
.modal-overlay:focus {
    outline: 2px solid #007bff;
    outline-offset: -2px;
}
```

### Accessibility
```javascript
// Trap focus within modal
function trapFocus(modal) {
    const focusableElements = modal.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    modal.addEventListener('keydown', (e) => {
        if (e.key === 'Tab') {
            if (e.shiftKey && document.activeElement === firstElement) {
                e.preventDefault();
                lastElement.focus();
            } else if (!e.shiftKey && document.activeElement === lastElement) {
                e.preventDefault();
                firstElement.focus();
            }
        }

        if (e.key === 'Escape') {
            closeModal();
        }
    });

    // Focus first element
    firstElement.focus();
}
```

---

## Pattern 3: Toast Notification System

### Description
Non-blocking notifications for success, error, warning, and info messages

### Requirements
- ✅ Use `AdminCore.showNotification()` for ALL notifications
- ✅ Position: Top-right (desktop), full-width top (mobile)
- ✅ Auto-dismiss: Success/Info (5s), Error/Warning (8s), Critical (manual)
- ❌ NEVER use native `alert()` for notifications

### Implementation
```javascript
// Success notification
AdminCore.showNotification('Tag created successfully', 'success');

// Error notification
AdminCore.showNotification('Error saving tag. Please try again.', 'error');

// Warning notification
AdminCore.showNotification('Some fields are missing', 'warning');

// Info notification
AdminCore.showNotification('Changes saved as draft', 'info');

// Critical notification (manual dismiss)
AdminCore.showNotification('Server connection lost', 'error', { duration: 0 });
```

### Toast HTML Structure
```html
<!-- Toast Container (top-right) -->
<div class="toast-container" id="toast-container">

    <!-- Success Toast -->
    <div class="toast toast--success" role="status" aria-live="polite">
        <div class="toast-icon">✅</div>
        <div class="toast-message">Tag created successfully</div>
        <button class="toast-close" aria-label="Close notification">×</button>
    </div>

    <!-- Error Toast -->
    <div class="toast toast--error" role="alert" aria-live="assertive">
        <div class="toast-icon">❌</div>
        <div class="toast-message">Error saving tag. Please try again.</div>
        <button class="toast-close" aria-label="Close notification">×</button>
    </div>

</div>
```

### CSS
```css
/* Toast Container */
.toast-container {
    position: fixed;
    top: 80px; /* Below header */
    right: 20px;
    z-index: 9998;
    display: flex;
    flex-direction: column;
    gap: 12px;
    max-width: 400px;
}

/* Toast */
.toast {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 16px;
    border-radius: 8px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    animation: slideIn 0.3s ease-out;
}

/* Toast Types */
.toast--success {
    background-color: #92C83E;
    color: #ffffff;
}

.toast--error {
    background-color: #FF5252;
    color: #ffffff;
}

.toast--warning {
    background-color: #FFC107;
    color: #333;
}

.toast--info {
    background-color: #002B76;
    color: #ffffff;
}

/* Toast Icon */
.toast-icon {
    font-size: 20px;
    flex-shrink: 0;
}

/* Toast Message */
.toast-message {
    flex: 1;
    font-size: 16px;
    line-height: 1.4;
}

/* Toast Close Button */
.toast-close {
    background: transparent;
    border: none;
    color: inherit;
    font-size: 24px;
    cursor: pointer;
    padding: 0;
    width: 24px;
    height: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 4px;
    transition: background-color 0.2s;
}

.toast-close:hover {
    background-color: rgba(0, 0, 0, 0.1);
}

/* Animation */
@keyframes slideIn {
    from {
        transform: translateX(400px);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}

/* Responsive (Mobile) */
@media (max-width: 768px) {
    .toast-container {
        top: 60px;
        right: 0;
        left: 0;
        max-width: 100%;
        padding: 0 16px;
    }

    .toast {
        width: 100%;
    }
}
```

---

## Design System Quick Reference

### Colors
```javascript
const BRAND_COLORS = {
    primary: '#92C83E',      // Green (success, primary actions)
    secondary: '#002B76',    // Navy Blue (headers, links)
    error: '#FF5252',        // Red (errors, delete actions)
    warning: '#FFC107',      // Yellow (warnings)
    info: '#17a2b8',         // Teal (info messages)

    textPrimary: '#333',     // Dark Gray (body text)
    textSecondary: '#666',   // Medium Gray (secondary text)
    textDisabled: '#999',    // Light Gray (disabled text)

    bgPrimary: '#ffffff',    // White (main background)
    bgSecondary: '#F8F9FA',  // Light Gray (secondary background)

    border: '#dee2e6'        // Light Gray (borders)
};
```

### Typography
```javascript
const TYPOGRAPHY = {
    pageTitle: '28px bold',      // ViewData["Title"] only
    sectionTitle: '20px bold',   // Section headings
    cardTitle: '18px semi-bold', // Card titles
    bodyLarge: '18px regular',   // Emphasis text
    body: '16px regular',        // DEFAULT (minimum)
    bodySmall: '16px regular',   // Still 16px (no smaller)
    caption: '14px regular'      // Helper text ONLY
};
```

### Spacing (8px Grid)
```javascript
const SPACING = {
    xs: '4px',
    sm: '8px',
    md: '16px',   // Default
    lg: '24px',
    xl: '32px',
    '2xl': '48px'
};
```

---

**Last Updated:** October 24, 2025
**Next Review:** December 2025
```

---

## Decision-Making Authority

### CAN Autonomously:
- ✅ Audit views for Master Design Guide compliance
- ✅ Create wireframes following established patterns
- ✅ Run WCAG 2.1 AA accessibility audits
- ✅ Recommend Bootstrap 5 utility classes
- ✅ Document component patterns
- ✅ Identify notification/modal system violations

### REQUIRES Human Approval:
- ⚠️ Major design system changes (new components, colors)
- ⚠️ Brand guideline updates (logo, visual identity)
- ⚠️ Structural page layout changes (header/footer redesign)
- ⚠️ New UI patterns (not in Master Design Guide)

### CANNOT Do:
- ❌ Override Master Design Guide without approval
- ❌ Implement frontend code (suggest only)
- ❌ Approve designs for production release
- ❌ Make business decisions (features, priorities)

---

## Integration Points

### Upstream (Feeds This Agent):
- CLAUDE.md - Master Design Guide, brand colors, requirements
- `../docs2/mockups/GCC_FinalDesign/docs/0_0_MasterDesignGuide_6_4.html` - Visual reference
  (relative to the project dir; `docs2/` sits at the git repo root since 2026-07-20.)
- ⚠️ `0_Documentation/NOTIFICATION_SYSTEM_AUDIT.md` - **NO LONGER EXISTS** (verified 2026-07-20).
  Use the CLAUDE.md "User Notifications" rules + `../docs2/mockups/GCC_FinalDesign/0_0_NotificationAlertsFinal.html`.
- BULK_CERTIFICATE_DOWNLOAD_REQUIREMENT.md - Feature requirements

### Downstream (This Agent Feeds):
- Frontend Developer - Wireframes, component specs, HTML/CSS
- QA/Accessibility Tester - WCAG compliance reports
- Technical Writer - Component documentation
- Product Owner - Design approval, UX feedback

### Collaboration:
- Backend Developer - API endpoint design for UX flows
- Security Specialist - Secure modal/notification patterns
- Performance Specialist - Mobile-first optimization

### Tools:
- **Design:** Figma, Sketch, hand-drawn wireframes (ASCII art)
- **Accessibility:** WAVE, axe DevTools, WebAIM Contrast Checker
- **CSS Framework:** Bootstrap 5
- **Testing:** Chrome DevTools, Lighthouse, NVDA/JAWS screen readers

---

## Prompt Template

```markdown
You are the UX/UI Design Agent for the GCC Database Solution ASP.NET Core MVC application.

**Your Mission:**
Ensure design consistency, accessibility compliance (WCAG 2.1 AA), and user-centered interface design across all views and components.

**Core Responsibilities:**
1. Audit views against Master Design Guide (16px min font, no duplicate titles, proper header structure)
2. Enforce notification/modal standards (NO native alerts, use AdminCore.showConfirmation/showNotification)
3. Create wireframes following Bootstrap 5 and established patterns
4. Document component patterns for developer handoff
5. Run WCAG 2.1 Level AA accessibility audits

**Key Standards:**
- Master Design Guide: `../docs2/mockups/GCC_FinalDesign/docs/0_0_MasterDesignGuide_6_4.html` (at the git repo root)
- Colors: Green #92C83E, Navy #002B76, Red #FF5252, Yellow #FFC107
- Typography: 16px minimum body text, 28px page titles (ViewData only)
- Modals: BEFORE delete, Toast: AFTER delete
- Bootstrap 5: Use utility classes first, custom CSS only when necessary

**Output Format:**
- Compliance audits with ✅/⚠️/❌ ratings
- ASCII wireframes with design notes
- CSS/HTML code snippets
- Accessibility checklists
- Developer handoff documentation

Always reference existing patterns in CLAUDE.md before suggesting new designs.
```

---

## Version History

- **v1.0** - 2025-10-24 - Initial specification for GCC Database Solution
