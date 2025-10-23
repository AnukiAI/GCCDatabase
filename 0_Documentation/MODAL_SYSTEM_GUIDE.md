# GCC Modal & Confirmation System - Complete Guide

**Last Updated:** October 22, 2025
**Status:** ✅ Implementation Complete - Ready for Rollout

## Overview

This document provides complete guidance on using the new custom modal and confirmation system that replaces all native browser alerts (`alert()`, `confirm()`, `prompt()`).

## ⚠️ CRITICAL REQUIREMENT

**DELETE CONFIRMATIONS MUST BE MODALS, NOT TOAST NOTIFICATIONS**

Based on the design specification:
- **BEFORE deletion:** Show modal confirmation popup (`AdminCore.showConfirmation()`)
- **AFTER deletion:** Show toast notification for success/error (`AdminCore.showNotification()`)

**This is a strict requirement for all delete and destructive operations.**

## System Components

### 1. CSS Styles
- **File:** `wwwroot/css/admin/admin-core.css` (lines 377-636)
- **Features:** Animations, responsive design, accessibility support, reduced motion support

### 2. JavaScript Utilities
- **File:** `wwwroot/js/admin/admin-core.js`
- **Methods:**
  - `AdminCore.showConfirmation()` - Delete confirmations, destructive actions
  - `AdminCore.showProgress()` - File uploads, long-running operations
  - `AdminCore.showNotification()` - Toast messages (already exists)

---

## Usage Examples

### 1. Delete Confirmation Modals

**When to use:** Delete operations, destructive actions that cannot be undone

**Example - Delete Tag:**
```javascript
async function deleteTag(id, name) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete Confirmation',
        message: `Are you sure you want to delete the tag "${name}"? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        type: 'danger' // Red delete button
    });

    if (!confirmed) {
        return; // User cancelled
    }

    // Proceed with deletion
    try {
        const response = await fetch(`/Admin/DeleteTag/${id}`, { method: 'DELETE' });
        // ... handle response
    } catch (error) {
        AdminCore.showNotification('Failed to delete tag', 'error');
    }
}
```

**Example - Delete User:**
```javascript
async deleteUser(userId) {
    const user = this.users.find(u => u.id === userId);

    const confirmed = await AdminCore.showConfirmation({
        title: 'Delete Confirmation',
        message: `Are you sure you want to delete user "${user.name}"? This action cannot be undone.`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        type: 'danger'
    });

    if (confirmed) {
        // Delete user logic...
    }
}
```

---

### 2. Warning Confirmations

**When to use:** Potentially dangerous actions, security changes

**Example - Disable 2FA:**
```javascript
async function disable2FA(userId) {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Disable Two-Factor Authentication',
        message: 'Are you sure you want to disable 2FA? This will make the account less secure.',
        confirmText: 'Disable 2FA',
        cancelText: 'Keep Enabled',
        type: 'warning' // Orange/primary button
    });

    if (confirmed) {
        // Disable 2FA logic...
        AdminCore.showNotification('Two-factor authentication has been disabled', 'warning');
    }
}
```

**Example - Sign Out:**
```javascript
async function logout() {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Sign Out',
        message: 'Are you sure you want to sign out?',
        confirmText: 'Sign Out',
        cancelText: 'Stay Signed In',
        type: 'warning'
    });

    if (confirmed) {
        window.location.href = '/Account/Logout';
    }
}
```

---

### 3. Info/Success Confirmations

**When to use:** Non-destructive confirmations, acknowledgments

**Example - Export Data:**
```javascript
async function exportData() {
    const confirmed = await AdminCore.showConfirmation({
        title: 'Export Data',
        message: 'This will export all data to CSV format. Continue?',
        confirmText: 'Export',
        cancelText: 'Cancel',
        type: 'info' // Green/success button
    });

    if (confirmed) {
        // Export logic...
        AdminCore.showNotification('Export started', 'info');
    }
}
```

---

### 4. Progress Modals (File Uploads)

**When to use:** File uploads, batch operations, long-running tasks

**Example - File Upload with Progress:**
```javascript
async function uploadFile(file) {
    // Show progress modal
    const progressModal = AdminCore.showProgress({
        title: 'Uploading File',
        message: 'Your file is being uploaded. Please wait...',
        indeterminate: false, // Show percentage
        cancelText: 'Cancel Upload',
        onCancel: () => {
            // Handle upload cancellation
            xhr.abort();
        }
    });

    const xhr = new XMLHttpRequest();

    // Track upload progress
    xhr.upload.addEventListener('progress', (e) => {
        if (e.lengthComputable) {
            const percent = (e.loaded / e.total) * 100;
            progressModal.update(percent, `Uploading... ${Math.round(percent)}%`);
        }
    });

    xhr.addEventListener('load', () => {
        progressModal.close();
        AdminCore.showNotification('File uploaded successfully!', 'success');
    });

    xhr.addEventListener('error', () => {
        progressModal.close();
        AdminCore.showNotification('Upload failed. Please try again.', 'error');
    });

    // Start upload
    const formData = new FormData();
    formData.append('file', file);
    xhr.open('POST', '/api/upload');
    xhr.send(formData);
}
```

**Example - Indeterminate Progress (Unknown Duration):**
```javascript
async function processBatch() {
    const progressModal = AdminCore.showProgress({
        title: 'Processing Records',
        message: 'Processing records in background...',
        indeterminate: true, // Animated progress bar
        cancelText: 'Run in Background'
    });

    try {
        await fetch('/api/batch-process', { method: 'POST' });
        progressModal.close();
        AdminCore.showNotification('Batch processing complete!', 'success');
    } catch (error) {
        progressModal.close();
        AdminCore.showNotification('Processing failed', 'error');
    }
}
```

---

### 5. Toast Notifications (Replacing alert())

**When to use:** Validation errors, success messages, non-blocking feedback

**Validation Errors:**
```javascript
// OLD (PROHIBITED):
alert('Please enter a valid email address');

// NEW (REQUIRED):
AdminCore.showNotification('Please enter a valid email address', 'error');
```

**Success Messages:**
```javascript
// OLD (PROHIBITED):
alert('Settings saved successfully!');

// NEW (REQUIRED):
AdminCore.showNotification('Settings saved successfully!', 'success');
```

**Warnings:**
```javascript
// OLD (PROHIBITED):
alert('No data available to export');

// NEW (REQUIRED):
AdminCore.showNotification('No data available to export', 'warning');
```

**Info Messages:**
```javascript
// OLD (PROHIBITED):
alert('Your session has expired');

// NEW (REQUIRED):
AdminCore.showNotification('Your session has expired. Please log in again.', 'info', {
    duration: 0 // Don't auto-dismiss
});
```

---

## API Reference

### AdminCore.showConfirmation(options)

**Parameters:**
```javascript
{
    title: 'Confirm Action',        // Modal title
    message: 'Are you sure?',       // Modal message/description
    confirmText: 'Confirm',         // Confirm button text
    cancelText: 'Cancel',           // Cancel button text
    type: 'danger',                 // 'danger', 'warning', 'info'
    onConfirm: null,                // Optional callback when confirmed
    onCancel: null                  // Optional callback when cancelled
}
```

**Returns:** `Promise<boolean>` - Resolves to `true` if confirmed, `false` if cancelled

**Button Colors by Type:**
- `danger` → Red confirm button (for deletions)
- `warning` → Primary blue/orange confirm button (for warnings)
- `info` → Green confirm button (for info/success)

---

### AdminCore.showProgress(options)

**Parameters:**
```javascript
{
    title: 'Processing',            // Modal title
    message: 'Please wait...',      // Modal message
    indeterminate: false,           // true = animated bar, false = percentage
    cancelText: 'Cancel',           // Cancel button text
    onCancel: null                  // Optional callback when cancelled
}
```

**Returns:** Object with methods:
- `update(percent, message)` - Update progress (0-100) and optional message
- `close()` - Close the modal
- `getElement()` - Get the modal overlay element

**Example:**
```javascript
const progress = AdminCore.showProgress({ title: 'Uploading' });
progress.update(25, 'Processing...');  // 25% complete
progress.update(50, 'Halfway there!'); // 50% complete
progress.update(100, 'Complete!');     // 100% complete
progress.close(); // Close modal
```

---

### AdminCore.showNotification(message, type, options)

**Parameters:**
```javascript
message: 'Notification message',
type: 'success' | 'error' | 'warning' | 'info',
options: {
    title: 'Custom Title',          // Optional custom title
    duration: 5000,                 // Auto-dismiss after 5s (0 = manual)
    closable: true,                 // Show close button
    icon: 'fa-check-circle'        // FontAwesome icon class
}
```

**Auto-dismiss Times:**
- `success` / `info` → 5 seconds
- `error` / `warning` → 8 seconds
- `duration: 0` → Manual close only

---

## Migration Checklist

### Files with alert() - 9 instances:
- [ ] `two-factor-preferences.js` (line 95) - Phone validation
- [ ] `certificateTileViewExternal.js` (line 659) - Download error
- [ ] `certificateTileViewExternal.js` (line 687) - Export error
- [ ] `auth.js` (line 74) - Session expired
- [ ] `product-types.js` (line 754) - No data warning
- [ ] `product-types.js` (line 794) - Generic error
- [ ] `product-types.js` (line 799) - Export unavailable
- [ ] `googleAnalytics.js` (line 48) - Validation error
- [ ] `googleAnalytics.js` (line 237) - No settings warning

### Files with confirm() - 13 instances:
- [x] `tags.js` (line 284) - Delete tag ✅ **COMPLETED**
- [ ] `userManagement.js` (line 334) - Delete user
- [ ] `userManagement.js` (line 781) - Reset 2FA
- [ ] `userManagement.js` (line 814) - Disable 2FA
- [ ] `roleManagement.js` (line 397) - Delete role
- [ ] `two-factor-preferences.js` (line 109) - Disable authenticator
- [ ] `product-types.js` (line 427) - Delete product type
- [ ] `admin/userManagement.js` (line 613) - Delete user
- [ ] `product-id-manager.js` (line 320) - Delete product ID
- [ ] `admin/productManagement.js` (line 455) - Delete product
- [ ] `main.js` (line 552) - Sign out
- [ ] `admin/csfAttributes.js` (line 297) - Delete CSF attribute
- [ ] `admin/companyManagement.js` (line 239) - Delete company
- [ ] `admin/certificate-types.js` (line 373) - Delete certificate type
- [ ] `admin/certificate-fields.js` (line 301) - Delete field

---

## Design Specifications

### Colors (Matching GCC Brand)
- **Success/Confirm:** Green `#92C83E`
- **Danger/Delete:** Red `#FF5252`
- **Warning:** Yellow/Orange `#FFC107`
- **Info:** Navy Blue `#002B76`
- **Background:** White `#FFFFFF`
- **Overlay:** `rgba(0, 0, 0, 0.5)`

### Typography
- **Title:** 20px, Semibold, Navy Blue `#002B76`
- **Message:** 16px, Regular, Gray `#666`
- **Buttons:** 16px, Medium weight

### Spacing
- **Modal Padding:** 24px
- **Button Gap:** 16px
- **Mobile Padding:** 16px

### Animations
- **Show/Hide:** 300ms ease
- **Progress Bar:** Animated stripes (1s loop)
- **Button Hover:** Slight lift effect

### Accessibility
- ✅ ARIA labels for screen readers
- ✅ Keyboard navigation (Enter, Escape)
- ✅ Focus management
- ✅ High contrast mode support
- ✅ Reduced motion support (`prefers-reduced-motion`)
- ✅ Mobile responsive (stacked buttons)

---

## Testing Checklist

### Functionality
- [ ] Confirmation modals show and dismiss correctly
- [ ] Progress modals update percentage accurately
- [ ] Cancel buttons work as expected
- [ ] Overlay click closes modal
- [ ] Escape key closes modal
- [ ] Enter key confirms action
- [ ] Multiple modals don't stack improperly

### Accessibility
- [ ] Screen reader announces modal content
- [ ] Tab navigation works through buttons
- [ ] Focus returns to trigger element on close
- [ ] ARIA attributes present and correct
- [ ] Keyboard shortcuts work (Esc, Enter)

### Responsive Design
- [ ] Desktop: Side-by-side buttons, centered modal
- [ ] Tablet: Centered modal, proper spacing
- [ ] Mobile: Stacked buttons, full-width, reduced padding
- [ ] Progress bar visible and animated on all sizes

### Browser Compatibility
- [ ] Chrome/Edge (Chromium)
- [ ] Firefox
- [ ] Safari
- [ ] Mobile Safari (iOS)
- [ ] Mobile Chrome (Android)

---

## Common Patterns

### Pattern 1: Delete with Item Name
```javascript
const confirmed = await AdminCore.showConfirmation({
    title: 'Delete Confirmation',
    message: `Are you sure you want to delete "${itemName}"? This action cannot be undone.`,
    confirmText: 'Delete',
    cancelText: 'Cancel',
    type: 'danger'
});
```

### Pattern 2: Security Warning
```javascript
const confirmed = await AdminCore.showConfirmation({
    title: 'Security Warning',
    message: 'This action will reduce account security. Are you sure?',
    confirmText: 'Continue',
    cancelText: 'Cancel',
    type: 'warning'
});
```

### Pattern 3: Upload with Progress
```javascript
const progress = AdminCore.showProgress({
    title: 'Uploading File',
    message: 'Uploading...',
    indeterminate: false
});

// In upload progress handler:
xhr.upload.addEventListener('progress', (e) => {
    const percent = (e.loaded / e.total) * 100;
    progress.update(percent);
});

// On complete:
progress.close();
AdminCore.showNotification('Upload complete!', 'success');
```

---

## Best Practices

### DO:
✅ Use `showConfirmation()` for all delete/destructive actions **BEFORE** they happen
✅ Use `showNotification()` for success/error messages **AFTER** actions complete
✅ Use `showNotification()` for validation errors and warnings
✅ Use `showProgress()` for file uploads and long operations
✅ Provide clear, specific messages explaining consequences
✅ Use appropriate `type` parameter (danger, warning, info)
✅ Test on mobile devices for responsive behavior
✅ Follow the pattern: Modal → Action → Toast

### DON'T:
❌ Use native `alert()`, `confirm()`, or `prompt()`
❌ Use toast notifications for delete confirmations (MUST use modals)
❌ Use modals for success messages after deletion (use toasts)
❌ Create custom modal HTML manually (use AdminCore methods)
❌ Forget to await `showConfirmation()` (it returns a Promise)
❌ Use generic messages like "Are you sure?" without context
❌ Stack multiple modals at once
❌ Forget to close progress modals after completion

### THE GOLDEN RULE:
**Confirmation = Modal (BEFORE) → Success/Error = Toast (AFTER)**

---

## Implementation Status

**✅ Complete:**
- Modal CSS system (responsive, accessible)
- JavaScript utilities (showConfirmation, showProgress)
- Example implementation (tags.js delete)
- Documentation and usage guide

**⏳ In Progress:**
- Replacing remaining 8 alert() calls
- Replacing remaining 12 confirm() calls

**📋 Pending:**
- Full accessibility testing
- Cross-browser testing
- Mobile device testing

---

## Support & Questions

For questions about the modal system, refer to:
- **This Guide:** `/0_Documentation/MODAL_SYSTEM_GUIDE.md`
- **CLAUDE.md:** Section "User Feedback & Alert System Standardization"
- **Source Code:** `wwwroot/js/admin/admin-core.js` (lines 349-604)
- **CSS Styles:** `wwwroot/css/admin/admin-core.css` (lines 377-636)

---

**Last Updated:** October 22, 2025
**Version:** 1.0
**Status:** Ready for Production Use
