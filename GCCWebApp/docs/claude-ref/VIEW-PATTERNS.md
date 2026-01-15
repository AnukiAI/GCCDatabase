# View Patterns Reference

## Admin Page Structure (US-016 Hybrid Layout)

### Complete Template
```cshtml
@{ ViewData["Title"] = "Page Name"; }  <!-- Rendered by _Layout.cshtml -->

<div class="admin-container">
    <header class="admin-header">
        <div class="admin-header-content">
            <div class="admin-title-section">
                <p class="admin-page-subtitle">Page description text</p>
            </div>
            <div class="admin-actions-section">
                <div class="admin-button-group">
                    <button id="template-btn" class="admin-btn admin-btn--secondary">
                        <i class="fas fa-file-download"></i> Template
                    </button>
                    <button id="import-btn" class="admin-btn admin-btn--secondary">
                        <i class="fas fa-upload"></i> Import
                    </button>
                    <button id="export-btn" class="admin-btn admin-btn--secondary">
                        <i class="fas fa-download"></i> Export
                    </button>
                    <button id="add-btn" class="admin-btn admin-btn--primary">
                        <i class="fas fa-plus"></i> Add New
                    </button>
                </div>
            </div>
        </div>
    </header>

    <div class="admin-body-content">
        <div class="admin-toolbar">
            <div class="admin-search-section">
                <div class="admin-search-box">
                    <label for="search-input" class="admin-sr-only">Search items</label>
                    <input type="text" id="search-input" class="admin-search-input"
                           placeholder="Search items..." aria-label="Search items">
                    <i class="fas fa-search admin-search-icon"></i>
                </div>
            </div>
            <div class="admin-filter-section">
                <div class="admin-filter-group">
                    <label for="status-filter" class="admin-sr-only">Filter by status</label>
                    <select id="status-filter" class="admin-filter-select" aria-label="Filter by status">
                        <option value="all">All Status</option>
                        <option value="active">Active</option>
                        <option value="inactive">Inactive</option>
                    </select>
                </div>
            </div>
        </div>

        <div class="admin-content">
            <div class="admin-table-container">
                <table class="admin-table">
                    <thead>
                        <tr>
                            <th>Column 1</th>
                            <th>Column 2</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody id="table-body">
                        <!-- Dynamic content -->
                    </tbody>
                </table>
            </div>
        </div>

        <div class="admin-footer-info">
            <span class="admin-count-display">Showing X of Y items</span>
        </div>
    </div>
</div>
```

### Common Mistakes

**DON'T**:
- Add `<h1 class="admin-page-title">` in view (creates duplicate - _Layout handles it)
- Use `.buttons-area` class (deprecated)
- Place buttons directly under `.admin-header`
- Use inline `max-width` overrides

**DO**:
- Use `ViewData["Title"]` for page title
- Use `.admin-header-content` → `.admin-title-section` + `.admin-actions-section`
- Wrap toolbar + content + footer in `.admin-body-content`
- Add `.admin-sr-only` labels for accessibility

## Table Action Icons

```html
<td class="actions-cell">
    <div class="action-buttons">
        <button class="action-btn edit" title="Edit" onclick="editItem(@item.Id)">
            <i class="fas fa-edit"></i>
        </button>
        <button class="action-btn delete" title="Delete" onclick="deleteItem(@item.Id)">
            <i class="fas fa-trash"></i>
        </button>
        <button class="action-btn view" title="View" onclick="viewItem(@item.Id)">
            <i class="fas fa-eye"></i>
        </button>
    </div>
</td>
```

**Role-based visibility**: External users should NOT see Actions column on Product Management.

## Import Template Pattern

All admin pages with import have "Template" button (EXCEPTION: Partners page - NO Template/Import).

```javascript
downloadImportTemplate() {
    const templateData = [{ Field1: 'Example 1', Field2: 'Example 2' }];
    const headers = ['Field1', 'Field2'];
    const csvContent = [
        headers.join(','),
        ...templateData.map(row =>
            headers.map(h => (row[h] || '').includes(',') ? `"${row[h]}"` : row[h]).join(',')
        )
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `entity_import_template_${new Date().toISOString().split('T')[0]}.csv`;
    link.click();

    AdminCore.showNotification('Template downloaded', 'success');
}
```

## Certificate Tile View Specifics

### Public/External/Internal Views
- **NO PAGINATION** - Infinite scroll only
- **Database-driven filters** - All filter options loaded from DB
- **Bulk ZIP download** - External/Internal views
- Autocomplete search with categorized suggestions
- Shareable URLs for filtered searches

### External View Restrictions
- Internal Tag column hidden
- Add/Edit buttons hidden
- Only associated company/partner data visible

### CSS Reference
- Unified styling: `wwwroot/css/certificatetileview.css`
- Filter panel: 10px checkbox-label spacing, 8px scrollbars
- Table: 14px/10px padding, zebra striping (#fff/#fafafa)
- Monospace: certificate numbers and dates
- Row height: 52px minimum
- Status badges: Active=#92C83E, Draft=#FFC107, Expired=#FF5252

## Certificate Details Page

### Preview Container
```cshtml
<div class="certificate-preview-box">
    <!-- 200x200 consistent container -->
    @if (hasValidThumbnail)
    {
        <img src="@snapshotPath" alt="Certificate preview" class="preview-image" />
    }
    else
    {
        <div class="preview-placeholder">Certificate Unavailable</div>
    }
</div>
```

- Valid thumbnail: path contains `_thumb_` or `uploads/images/`
- Click-to-zoom opens full-size modal
- Download button only if PDF exists
