# GCC Admin Framework Documentation

## Overview

The GCC Admin Framework provides a comprehensive, consistent UI system for all administrative pages in the GCC Web Application. It replaces the inconsistent mixture of inline styles, external CSS files, and different component patterns with a unified design system.

## Framework Structure

### CSS Files

```
wwwroot/css/admin/
├── admin.css                 # Master file (imports all others + compatibility)
├── admin-core.css           # Design tokens, base styles, utilities
├── admin-components.css     # Buttons, forms, inputs, badges
├── admin-layout.css         # Headers, containers, grids, cards
├── admin-tables.css         # Tables, sorting, filtering, actions
├── admin-modals.css         # Modals, notifications, dropdowns
└── README.md               # This documentation
```

### JavaScript Files

```
wwwroot/js/admin/
├── admin-core.js           # Base utilities, AJAX, notifications
└── [Future expansion for specialized components]
```

## Quick Start

### For New Admin Pages

1. **Include the framework CSS:**
```html
@section Styles {
    <link href="~/css/admin/admin.css" rel="stylesheet" />
}
```

2. **Include the framework JavaScript:**
```html
@section Scripts {
    <script src="~/js/admin/admin-core.js"></script>
}
```

3. **Use the standard structure:**
```html
<div class="admin-container">
    <header class="admin-header">
        <div class="admin-header-content">
            <div class="admin-title-section">
                <h1 class="admin-page-title">Page Title</h1>
                <p class="admin-page-subtitle">Page description</p>
            </div>
            <div class="admin-actions-section">
                <div class="admin-button-group">
                    <button class="admin-btn admin-btn--secondary">
                        <i class="fas fa-download"></i> Export
                    </button>
                    <button class="admin-btn admin-btn--primary">
                        <i class="fas fa-plus"></i> Add New
                    </button>
                </div>
            </div>
        </div>
    </header>

    <!-- Optional search/filter bar -->
    <div class="admin-toolbar">
        <div class="admin-search-section">
            <div class="admin-search-box">
                <input type="text" class="admin-search-input" placeholder="Search...">
                <i class="fas fa-search admin-search-icon"></i>
            </div>
        </div>
        <div class="admin-filter-section">
            <select class="admin-filter-select">
                <option>Filter options...</option>
            </select>
        </div>
    </div>

    <!-- Main content -->
    <div class="admin-content">
        <!-- Your page content here -->
    </div>

    <!-- Footer info -->
    <div class="admin-footer-info">
        <span class="admin-count-display">Status information</span>
    </div>
</div>
```

### For Existing Admin Pages

The framework includes compatibility classes that automatically style existing elements:

- Existing `.btn` classes → automatically styled as admin buttons
- Existing `.admin-container` → automatically uses new layout system
- Existing `.table-container` → automatically styled as admin tables
- Existing `.modal` classes → automatically styled as admin modals

## Design System

### Colors

```css
/* Brand Colors */
--gcc-green-primary: #447638;
--gcc-green-secondary: #92C83E;
--gcc-blue-primary: #00AEEF;
--gcc-blue-dark: #002B76;

/* System Colors */
--admin-primary: #00AEEF (GCC Blue)
--admin-success: #92C83E (GCC Green)
--admin-warning: #ffa500
--admin-danger: #ff5252
--admin-info: #00AEEF
```

### Typography

```css
/* Font Family */
--admin-font-family-primary: 'Century Gothic', CenturyGothic, AppleGothic, sans-serif;

/* Font Sizes */
--admin-font-size-xs: 12px
--admin-font-size-sm: 14px
--admin-font-size-base: 16px
--admin-font-size-lg: 18px
--admin-font-size-xl: 20px
--admin-font-size-2xl: 24px
--admin-font-size-3xl: 32px
```

### Spacing

```css
/* 8px base unit system */
--admin-space-xs: 4px
--admin-space-sm: 8px
--admin-space-md: 16px
--admin-space-lg: 24px
--admin-space-xl: 32px
--admin-space-2xl: 48px
--admin-space-3xl: 64px
```

## Component Classes

### Buttons

```html
<!-- Basic button -->
<button class="admin-btn">Default Button</button>

<!-- Button variants -->
<button class="admin-btn admin-btn--primary">Primary Button</button>
<button class="admin-btn admin-btn--secondary">Secondary Button</button>
<button class="admin-btn admin-btn--success">Success Button</button>
<button class="admin-btn admin-btn--warning">Warning Button</button>
<button class="admin-btn admin-btn--danger">Danger Button</button>

<!-- Button sizes -->
<button class="admin-btn admin-btn--sm">Small Button</button>
<button class="admin-btn admin-btn--lg">Large Button</button>

<!-- Button with icon -->
<button class="admin-btn admin-btn--primary">
    <i class="fas fa-plus"></i> Add New
</button>

<!-- Icon-only button -->
<button class="admin-btn admin-btn--icon-only">
    <i class="fas fa-edit"></i>
</button>
```

### Forms

```html
<form class="admin-form">
    <div class="admin-form-group">
        <label class="admin-label admin-label--required">Email Address</label>
        <input type="email" class="admin-input" required>
        <span class="admin-form-help">Enter a valid email address</span>
    </div>

    <div class="admin-form-group">
        <label class="admin-label">Options</label>
        <select class="admin-select">
            <option>Choose an option</option>
        </select>
    </div>

    <div class="admin-form-group">
        <label class="admin-checkbox-label">
            <input type="checkbox" class="admin-checkbox">
            <span class="admin-checkbox-text">I agree to terms</span>
        </label>
    </div>
</form>
```

### Tables

```html
<div class="admin-table-container">
    <table class="admin-table">
        <thead class="admin-table-header">
            <tr>
                <th class="admin-table-th admin-table-th--sortable" data-sort="name">
                    Name <i class="admin-sort-icon fas fa-sort"></i>
                </th>
                <th class="admin-table-th admin-table-th--actions">Actions</th>
            </tr>
        </thead>
        <tbody class="admin-table-body">
            <tr class="admin-table-row">
                <td class="admin-table-cell">John Doe</td>
                <td class="admin-table-cell admin-table-cell--actions">
                    <div class="admin-action-group">
                        <button class="admin-action-btn admin-action-btn--edit">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="admin-action-btn admin-action-btn--delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </td>
            </tr>
        </tbody>
    </table>
</div>
```

### Cards

```html
<!-- Basic card -->
<div class="admin-card">
    <div class="admin-card-header">
        <h3 class="admin-card-title">Card Title</h3>
        <i class="fas fa-chart-bar admin-card-icon"></i>
    </div>
    <div class="admin-card-content">
        <p>Card content goes here</p>
    </div>
</div>

<!-- Stat card -->
<div class="admin-card admin-card--primary">
    <div class="admin-card-header">
        <h3 class="admin-card-title">Total Users</h3>
        <i class="fas fa-users admin-card-icon"></i>
    </div>
    <div class="admin-card-content">
        <div class="admin-card-stat">1,247</div>
        <div class="admin-card-label">Active Users</div>
    </div>
</div>

<!-- Clickable card -->
<a href="/admin/users" class="admin-card admin-card--clickable">
    <div class="admin-card-header">
        <h3 class="admin-card-title">Manage Users</h3>
        <i class="fas fa-users admin-card-icon"></i>
    </div>
    <div class="admin-card-content">
        <p>Create, edit, and manage user accounts</p>
    </div>
</a>
```

### Modals

```html
<!-- Modal HTML -->
<div class="admin-modal" id="example-modal" aria-hidden="true">
    <div class="admin-modal-overlay"></div>
    <div class="admin-modal-container">
        <div class="admin-modal-header">
            <h3 class="admin-modal-title">Modal Title</h3>
            <button class="admin-modal-close" aria-label="Close modal">
                <i class="fas fa-times"></i>
            </button>
        </div>
        <div class="admin-modal-body">
            <p>Modal content goes here</p>
        </div>
        <div class="admin-modal-footer">
            <button class="admin-btn admin-btn--secondary admin-modal-cancel">Cancel</button>
            <button class="admin-btn admin-btn--primary admin-modal-confirm">Save</button>
        </div>
    </div>
</div>

<!-- JavaScript to open modal -->
<script>
AdminCore.openModal('example-modal');
</script>
```

## JavaScript API

### Notifications

```javascript
// Show success notification
AdminCore.showNotification('Operation completed successfully!', 'success');

// Show error notification
AdminCore.showNotification('An error occurred', 'error');

// Show notification with custom options
AdminCore.showNotification('Custom message', 'info', {
    title: 'Custom Title',
    duration: 10000,
    closable: true
});
```

### AJAX

```javascript
// Simple AJAX request
AdminCore.ajax({
    url: '/api/users',
    method: 'GET',
    onSuccess: (data) => {
        console.log('Success:', data);
    },
    onError: (error) => {
        console.log('Error:', error);
    }
});

// POST request with data
AdminCore.ajax({
    url: '/api/users',
    method: 'POST',
    data: { name: 'John Doe', email: 'john@example.com' },
    onSuccess: (data) => {
        AdminCore.showNotification('User created successfully!', 'success');
    }
});
```

### Form Helpers

```javascript
// Serialize form data
const form = document.getElementById('user-form');
const data = AdminCore.serializeForm(form);

// Validate form
const errors = AdminCore.validateForm(form);
if (errors.length > 0) {
    AdminCore.showNotification(errors.join('<br>'), 'error');
}
```

### Loading States

```javascript
// Show loading state
AdminCore.showLoading('#user-table', 'Loading users...');

// Hide loading state
AdminCore.hideLoading('#user-table');
```

## Utility Classes

### Spacing

```html
<!-- Margins -->
<div class="admin-m-xs">Extra small margin</div>
<div class="admin-m-sm">Small margin</div>
<div class="admin-m-md">Medium margin</div>
<div class="admin-m-lg">Large margin</div>

<!-- Padding -->
<div class="admin-p-md">Medium padding</div>

<!-- Directional spacing -->
<div class="admin-mt-lg">Large top margin</div>
<div class="admin-mb-sm">Small bottom margin</div>
```

### Display & Layout

```html
<!-- Flexbox -->
<div class="admin-flex admin-items-center admin-justify-between">
    <span>Left content</span>
    <span>Right content</span>
</div>

<!-- Grid -->
<div class="admin-grid admin-grid--3 admin-grid--gap-md">
    <div>Item 1</div>
    <div>Item 2</div>
    <div>Item 3</div>
</div>

<!-- Text alignment -->
<div class="admin-text-center">Centered text</div>
<div class="admin-text-right">Right aligned text</div>
```

### Colors

```html
<!-- Text colors -->
<span class="admin-text-primary">Primary text</span>
<span class="admin-text-secondary">Secondary text</span>
<span class="admin-text-muted">Muted text</span>
<span class="admin-text-success">Success text</span>

<!-- Background colors -->
<div class="admin-bg-primary">Primary background</div>
<div class="admin-bg-success">Success background</div>
```

## Responsive Design

The framework is mobile-first and includes responsive breakpoints:

- **Mobile**: < 768px
- **Tablet**: 768px - 1023px
- **Desktop**: ≥ 1024px

### Responsive Classes

```html
<!-- Hide on mobile -->
<div class="admin-hidden-mobile">Desktop only</div>

<!-- Show only on mobile -->
<div class="admin-block-mobile admin-hidden-tablet admin-hidden-desktop">Mobile only</div>
```

## Migration Guide

### From Existing Admin Pages

1. **Replace CSS includes:**
   ```html
   <!-- Old -->
   <link href="~/css/admin-dashboard.css" rel="stylesheet" />

   <!-- New -->
   <link href="~/css/admin/admin.css" rel="stylesheet" />
   ```

2. **Update HTML structure:** Use new admin framework classes while keeping existing functionality

3. **Replace inline styles:** Move all inline CSS to external stylesheets or use utility classes

4. **Update JavaScript:** Use AdminCore utilities instead of custom AJAX/notification code

## Best Practices

1. **Always use the design tokens** (CSS custom properties) instead of hard-coded values
2. **Use semantic class names** that describe purpose, not appearance
3. **Follow the mobile-first approach** for responsive design
4. **Test accessibility** with screen readers and keyboard navigation
5. **Use consistent spacing** with the 8px base unit system
6. **Maintain backwards compatibility** when updating existing pages

## Browser Support

- Chrome 88+
- Firefox 85+
- Safari 14+
- Edge 88+

## Performance

- **CSS**: ~45KB minified (~8KB gzipped)
- **JavaScript**: ~12KB minified (~4KB gzipped)
- **Load time**: <100ms on average connections
- **Bundle optimization**: Uses CSS `@import` for modular loading

## Troubleshooting

### Common Issues

1. **Styles not loading**: Check CSS path and ensure admin.css is included
2. **JavaScript errors**: Verify admin-core.js is loaded before other admin scripts
3. **Mobile layout issues**: Use responsive utility classes and test on actual devices
4. **Color inconsistencies**: Use CSS custom properties instead of hard-coded colors

### Debug Mode

Add `admin-debug` class to container to visualize layout boxes:

```html
<div class="admin-container admin-debug">
    <!-- Layout boxes will be outlined -->
</div>
```

## Future Enhancements

- [ ] Dark theme support
- [ ] Advanced table features (pagination, advanced filtering)
- [ ] Drag-and-drop components
- [ ] Advanced form validation
- [ ] Animation system
- [ ] Print-optimized layouts

---

**Version**: 1.0.0
**Last Updated**: 2024-12-19
**Maintained by**: GCC Development Team