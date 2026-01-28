# UI Standards Reference

## User Feedback System

**CRITICAL**: Native `alert()`, `confirm()`, `prompt()` are STRICTLY PROHIBITED.

### Toast Notifications (Non-Blocking)
```javascript
AdminCore.showNotification(message, type)
```
- **Position**: Top-right (desktop), full-width top (mobile)
- **Colors**: Success=#92C83E, Error=#FF5252, Warning=#FFC107, Info=#002B76
- **Auto-dismiss**: Success/Info 5s, Error/Warning 8s, Critical manual
- **Use for**: Form success, save confirmations, background completion

### Modal Confirmations (Blocking)
```javascript
AdminCore.showConfirmation({ title, message, confirmText, type })
AdminCore.showProgress({ title, message })
```
- **Use for**: Delete actions, destructive operations, security changes

### Delete Pattern (CRITICAL)
```javascript
// BEFORE delete - Modal confirmation
const confirmed = await AdminCore.showConfirmation({
    title: 'Delete Confirmation',
    message: 'This action cannot be undone. Delete this item?',
    confirmText: 'Delete',
    type: 'danger'
});
if (!confirmed) return;

// Execute deletion
await deleteItem(id);

// AFTER delete - Toast notification
AdminCore.showNotification('Item deleted successfully', 'success');
```

**WRONG**:
- `if (confirm('Delete?'))` - NEVER native dialogs
- Toast for confirmations - NEVER
- Modal for success messages - NEVER

## Modal Backdrop Standard (US-015)

**ALL modals must follow this standard.**

```css
/* Backdrop */
background-color: rgba(0, 0, 0, 0.85);
backdrop-filter: blur(3px);
position: fixed; top: 0; left: 0; right: 0; bottom: 0;
z-index: 9998;

/* Container */
position: relative;
z-index: 9999;
max-width: 650px;
background-color: white;
box-shadow: 0 10px 40px rgba(0, 0, 0, 0.5);
```

**Structure**: Backdrop and container as SIBLINGS (not nested)

```html
<div id="modal-id" class="admin-modal" style="display: none;">
    <!-- Backdrop (sibling) -->
    <div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0;
         background-color: rgba(0, 0, 0, 0.85); z-index: 9998;
         backdrop-filter: blur(3px);"></div>

    <!-- Container (sibling) -->
    <div class="admin-modal-container" style="position: relative; z-index: 9999;">
        <div class="admin-modal-header">...</div>
        <div class="admin-modal-body">...</div>
        <div class="admin-modal-footer">...</div>
    </div>
</div>
```

**Reference Implementation**: `Views/Admin/PartnerAPI.cshtml:183-272`

## Button Styling

### Table Action Icons (Edit/Delete/View in rows)
```css
/* NO background - completely transparent */
.action-btn {
    background: none;  /* NEVER grey, NEVER #e3f2fd, NEVER #f5f5f5 */
    border: none;
    color: #666;
    transition: all 0.2s ease;
}

/* Hover - 10% opacity only */
.action-btn.edit:hover { color: #00AEEF; background: rgba(0, 174, 239, 0.1); transform: scale(1.1); }
.action-btn.delete:hover { color: #FF5252; background: rgba(255, 82, 82, 0.1); transform: scale(1.1); }
.action-btn.view:hover { color: #92C83E; background: rgba(146, 200, 62, 0.1); transform: scale(1.1); }
```

### Regular Buttons
| Type | Class | Background | Use |
|------|-------|------------|-----|
| Primary | `.btn-primary` | #92C83E | Save, Submit, Add New |
| Secondary/Cancel | `.btn-secondary`, `.btn-cancel` | transparent + #6c757d border | Cancel, Close, Back |
| Secondary Blue | `.btn-secondary-blue` | #00AEEF | Alternative actions |
| Tertiary | `.btn-tertiary` | transparent + #447638 border | Export, Import, Template |
| Danger | `.btn-danger` | #FF5252 | Delete ONLY (never Cancel) |

```css
/* Primary */
.btn-primary { background: #92C83E; color: white; }
.btn-primary:hover { background: #7fb835; }

/* Secondary/Cancel - STANDARD FOR ALL CANCEL BUTTONS */
.btn-secondary, .btn-cancel, .admin-btn--secondary {
    background-color: transparent;
    color: #6c757d;
    border: 1px solid #6c757d;
}
.btn-secondary:hover, .btn-cancel:hover, .admin-btn--secondary:hover {
    background-color: rgba(108, 117, 125, 0.1);
    color: #5a6268;
    border-color: #5a6268;
}

/* Secondary Blue */
.btn-secondary-blue { background: #00AEEF; color: white; }
.btn-secondary-blue:hover { background: #0095d8; }

/* Tertiary */
.btn-tertiary { background: transparent; border: 2px solid #447638; color: #447638; }
.btn-tertiary:hover { background: rgba(68, 118, 56, 0.1); }

/* Danger - DELETE ONLY, NEVER for Cancel */
.btn-danger { background: #FF5252; color: white; }
.btn-danger:hover { background: #e04545; }
```

### Cancel Button Rules (CRITICAL)
- **ALWAYS** use grey/neutral styling for Cancel buttons
- **NEVER** use red/danger styling for Cancel buttons
- Red is reserved for destructive DELETE actions only
- Cancel is a non-destructive action and must appear neutral

**Sizes**: `.btn-lg` (18px), `.btn-md` (16px default), `.btn-sm` (14px)

## Container Widths (US-016)

| Page Type | Max-Width | Class |
|-----------|-----------|-------|
| Standard Admin | 1400px | `.admin-container` |
| Auth (2-column) | 900px | `.login-container` |
| Auth (1-column) | 500px | `.auth-container` |
| Full-Width | 100% | `.admin-container--full-width` |

**Design Token**: `--admin-container-max-width: 1400px` (admin-core.css:126)

## Search Input Controls (US-017)

**ALL search input fields must use "Search" as placeholder text.**

```html
<!-- CORRECT -->
<input type="text" class="form-control" placeholder="Search" />
<input type="search" class="form-control" placeholder="Search" />

<!-- WRONG - Do NOT use these -->
<input type="text" placeholder="Search..." />           <!-- No ellipsis -->
<input type="text" placeholder="Search certificates" /> <!-- No context suffix -->
<input type="text" placeholder="Type to search..." />   <!-- No instructions -->
<input type="text" placeholder="Filter" />              <!-- Use "Search" not "Filter" -->
<input type="text" placeholder="" />                    <!-- Never empty -->
```

**Consistency Rule**: All search inputs across the application use the single word "Search" - no variations.

### Search Bar Width Standard

**ALL search bars must be 95% width of their container.**

```css
/* Standard search container width */
.search-box,
.search-bar,
.admin-search-section {
    width: 95%;
    max-width: none;
}
```

**CSS File**: `css/components/forms.css` contains the unified search bar styles.

**Exceptions**:
- Sidebar search bars: 100% of sidebar width (they are already constrained by the sidebar)

## Table Action Columns (US-018)

**Edit/Delete icons in table rows MUST stay on the same row - no wrapping.**

### Container Structure
```html
<!-- Table cell -->
<td class="actions-cell">
    <!-- Action buttons container -->
    <div class="action-buttons">
        <button class="action-btn edit" title="Edit">
            <i class="fas fa-edit"></i>
        </button>
        <button class="action-btn delete" title="Delete">
            <i class="fas fa-trash"></i>
        </button>
    </div>
</td>
```

### CSS Requirements
```css
/* Action cell - prevent wrapping, center vertically */
.actions-cell {
    white-space: nowrap;
    min-width: 80px;
    text-align: center;
    vertical-align: middle;
}

/* Action buttons container - inline-flex with NO wrap */
.action-buttons {
    display: inline-flex;
    flex-wrap: nowrap;
    align-items: center;
    justify-content: center;
    gap: 4px;
}
```

### Column Header
```html
<th class="actions-col" style="width: 10%; min-width: 80px;">Actions</th>
```

**CSS File**: `css/components/buttons.css` contains the action column styles.

**DO NOT**:
- Use `flex-wrap: wrap` in action button containers
- Use narrow column widths that force wrapping
- Stack buttons vertically in table rows (only allowed on mobile breakpoints)

## Color Palette
- Primary Green: #92C83E
- Dark Green: #447638
- Primary Blue: #00AEEF
- Navy: #002B76
- Danger Red: #FF5252 (Delete only, never Cancel)
- Warning Yellow: #FFC107
- Secondary Grey: #6c757d (Cancel, Close, Back buttons)
- Secondary Grey Hover: #5a6268
- Gray Light: #f8f9fa
- Border: #e9ecef
