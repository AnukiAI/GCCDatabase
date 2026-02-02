# Accessibility Quick Reference Guide
## GCC Database Solution Development Team

---

## Core Principles

1. **Keyboard First**: Everything must work with keyboard alone
2. **Screen Reader Friendly**: Use semantic HTML + ARIA when needed
3. **16px Minimum**: All text must be at least 16px
4. **Color Independent**: Don't rely on color alone to convey information
5. **Focus Visible**: Always show where keyboard focus is

---

## Quick Checklist for New Features

### Forms
- [ ] Every input has a visible `<label>` with `for` attribute
- [ ] Required fields marked with `aria-required="true"`
- [ ] Error messages linked with `aria-describedby`
- [ ] Errors use `role="alert"` or are in live region

### Buttons
- [ ] Descriptive text or `aria-label` (not just icons)
- [ ] Icon-only buttons have `aria-label`
- [ ] Icons have `aria-hidden="true"`
- [ ] Toggle buttons have `aria-pressed` state

### Images
- [ ] Meaningful images have descriptive `alt` text
- [ ] Decorative images have `alt=""` or `aria-hidden="true"`
- [ ] Logo has company name in alt text

### Modals
- [ ] Has `role="dialog"` and `aria-modal="true"`
- [ ] Has `aria-labelledby` pointing to title
- [ ] Focus moves to modal when opened
- [ ] Focus returns to trigger when closed
- [ ] Escape key closes modal
- [ ] Tab key stays within modal (focus trap)

### Tables
- [ ] Use `<table>`, `<th>`, `<td>` (not divs)
- [ ] Headers have `scope="col"` or `scope="row"`
- [ ] Has `<caption>` or `aria-label`
- [ ] Complex tables have `aria-describedby`

### Navigation
- [ ] Wrapped in `<nav>` element
- [ ] Has `aria-label` if multiple navs exist
- [ ] Current page marked with `aria-current="page"`
- [ ] Skip link at top of page

### Dynamic Content
- [ ] Loading states use `aria-live="polite"` or `role="status"`
- [ ] Errors use `aria-live="assertive"` or `role="alert"`
- [ ] Changes announced to screen readers
- [ ] Loading states have `aria-busy` attribute

---

## Common Patterns

### Search Input
```html
<div class="search-bar">
    <label for="search-input" class="admin-sr-only">Search items</label>
    <input type="text" id="search-input" placeholder="Search"
           aria-label="Search by name, company, or product">
    <i class="fas fa-search" aria-hidden="true"></i>
</div>
```

### Action Buttons
```html
<!-- Edit Button -->
<button class="action-btn edit" aria-label="Edit certificate #12345"
        onclick="editCertificate(123)">
    <i class="fas fa-edit" aria-hidden="true"></i>
</button>

<!-- Delete Button -->
<button class="action-btn delete" aria-label="Delete user John Smith"
        onclick="deleteUser(456)">
    <i class="fas fa-trash" aria-hidden="true"></i>
</button>
```

### Toggle Button (View Mode)
```html
<div class="view-toggle" role="group" aria-label="View mode">
    <button class="toggle-btn active" aria-pressed="true" aria-label="Tile view">
        <i class="fas fa-th" aria-hidden="true"></i> Tile
    </button>
    <button class="toggle-btn" aria-pressed="false" aria-label="List view">
        <i class="fas fa-list" aria-hidden="true"></i> List
    </button>
</div>
```

### Collapsible Panel
```html
<button class="filter-header" aria-expanded="true" aria-controls="filter-content">
    <span>Filter Options</span>
    <i class="fas fa-chevron-down" aria-hidden="true"></i>
</button>
<div id="filter-content" class="filter-content">
    <!-- Content here -->
</div>

<!-- Update aria-expanded when toggling -->
<script>
button.addEventListener('click', function() {
    const expanded = this.getAttribute('aria-expanded') === 'true';
    this.setAttribute('aria-expanded', !expanded);
});
</script>
```

### Form Field with Error
```html
<div class="form-group">
    <label for="email">Email <span class="text-danger" aria-label="required">*</span></label>
    <input type="email" id="email" class="form-control"
           required aria-required="true"
           aria-describedby="email-error email-help">
    <small id="email-help">We'll never share your email</small>
    <span id="email-error" class="text-danger" role="alert">Please enter a valid email</span>
</div>
```

### Loading Indicator
```html
<div id="loadingIndicator" class="loading-indicator"
     role="status" aria-live="polite" aria-atomic="true" aria-busy="false">
    <div class="loading-spinner" aria-hidden="true"></div>
    <div class="loading-text">Loading more items...</div>
</div>

<!-- Update aria-busy in JavaScript -->
<script>
loadingIndicator.setAttribute('aria-busy', 'true'); // When loading starts
loadingIndicator.setAttribute('aria-busy', 'false'); // When loading ends
</script>
```

### Screen Reader Announcements
```javascript
// Use AdminCore helper
AdminCore.announceToScreenReader('Item deleted successfully');

// Or use AccessibilityEnhancements helper
AccessibilityEnhancements.announceToScreenReader('Filter applied', 'polite');

// For critical messages, use 'assertive'
AccessibilityEnhancements.announceToScreenReader('Error: Form submission failed', 'assertive');
```

---

## ARIA Attributes Quick Reference

| Attribute | Use Case | Example |
|-----------|----------|---------|
| `aria-label` | Invisible label for element | `<button aria-label="Close">×</button>` |
| `aria-labelledby` | Points to visible label | `<div role="dialog" aria-labelledby="dialog-title">` |
| `aria-describedby` | Points to description | `<input aria-describedby="email-help">` |
| `aria-expanded` | Collapsible element state | `<button aria-expanded="true">Menu</button>` |
| `aria-pressed` | Toggle button state | `<button aria-pressed="false">Tile View</button>` |
| `aria-hidden` | Hide from screen readers | `<i class="fas fa-icon" aria-hidden="true"></i>` |
| `aria-live` | Announce dynamic changes | `<div aria-live="polite">Loading...</div>` |
| `aria-busy` | Element is loading | `<div aria-busy="true">...</div>` |
| `aria-controls` | Points to controlled element | `<button aria-controls="menu">Toggle</button>` |
| `aria-current` | Current item in set | `<a aria-current="page">Home</a>` |
| `aria-required` | Required form field | `<input aria-required="true">` |

---

## Common Mistakes to Avoid

### ❌ DON'T
```html
<!-- Missing label -->
<input type="text" placeholder="Email">

<!-- Icon-only button without label -->
<button><i class="fas fa-edit"></i></button>

<!-- Div pretending to be button -->
<div onclick="doSomething()">Click me</div>

<!-- Duplicate IDs -->
<label for="name">Name</label>
<input id="name">
<!-- Somewhere else -->
<input id="name">

<!-- Color-only status -->
<span style="color: red;">Expired</span>

<!-- Decorative image with alt -->
<img src="divider.png" alt="divider">

<!-- Native alert -->
alert('Success!');

<!-- Missing heading hierarchy -->
<h1>Page Title</h1>
<h3>Section</h3> <!-- Skipped h2 -->
```

### ✅ DO
```html
<!-- Label present -->
<label for="email">Email</label>
<input type="email" id="email" placeholder="user@example.com">

<!-- Icon button with label -->
<button aria-label="Edit certificate">
    <i class="fas fa-edit" aria-hidden="true"></i>
</button>

<!-- Semantic button -->
<button type="button" onclick="doSomething()">Click me</button>

<!-- Unique IDs -->
<label for="user-name">Name</label>
<input id="user-name">

<!-- Color + text/icon -->
<span class="badge badge-danger">
    <i class="fas fa-exclamation-circle" aria-hidden="true"></i> Expired
</span>

<!-- Decorative image -->
<img src="divider.png" alt="" aria-hidden="true">

<!-- Use AdminCore modal -->
AdminCore.showNotification('Success!', 'success');

<!-- Proper heading hierarchy -->
<h1>Page Title</h1>
<h2>Section</h2>
<h3>Subsection</h3>
```

---

## Keyboard Shortcuts

### Standard Browser Shortcuts
- **Tab**: Move to next interactive element
- **Shift+Tab**: Move to previous interactive element
- **Enter**: Activate button/link
- **Space**: Activate button, toggle checkbox
- **Escape**: Close modal/dropdown
- **Arrow keys**: Navigate within component (dropdowns, tabs, etc.)

### Application-Specific
- **Tab** (from page load): Focuses skip link
- **Escape**: Closes any open modal or dropdown
- **Arrow keys**: Navigate sort dropdown options

---

## Testing Tools

### Browser Extensions
- **axe DevTools** (Chrome/Firefox) - Automated accessibility testing
- **WAVE** (Chrome/Firefox) - Visual accessibility checker
- **Lighthouse** (Chrome) - Includes accessibility audit

### Manual Testing
1. **Keyboard only**: Unplug mouse, navigate entire page
2. **Screen reader**:
   - Windows: NVDA (free)
   - Mac: VoiceOver (built-in)
3. **Zoom**: Test at 200% and 400% zoom
4. **Color blindness**: Use browser extensions or Windows filters

### Quick Keyboard Test
1. Press Tab repeatedly - can you reach everything?
2. Can you activate buttons with Enter/Space?
3. Can you close modals with Escape?
4. Is focus always visible?
5. Does Tab order make sense?

---

## Font Size Requirements

**CRITICAL**: 16px minimum for all text

```css
/* WRONG */
.small-text { font-size: 12px; }

/* CORRECT */
.small-text { font-size: 16px; }

/* Exceptions: None - everything must be 16px+ */
```

Already enforced globally in `main.css`, but avoid inline styles that override this.

---

## Screen Reader Only Content

Use `.admin-sr-only` class for content that should only be read by screen readers:

```html
<label for="search" class="admin-sr-only">Search certificates</label>
<input id="search" type="text" placeholder="Search">
```

CSS:
```css
.admin-sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border-width: 0;
}
```

---

## Modal Accessibility Checklist

When creating a new modal:

```javascript
// 1. Create modal with proper ARIA
const modal = document.createElement('div');
modal.className = 'admin-modal-overlay';
modal.setAttribute('role', 'dialog');
modal.setAttribute('aria-modal', 'true');
modal.setAttribute('aria-labelledby', 'modal-title');

// 2. Add title with matching ID
modal.innerHTML = `
    <div class="admin-modal-dialog">
        <div class="admin-modal-header">
            <h2 id="modal-title">Dialog Title</h2>
            <button class="admin-modal-close" aria-label="Close dialog">
                <i class="fas fa-times" aria-hidden="true"></i>
            </button>
        </div>
        <div class="admin-modal-body">
            Content here
        </div>
        <div class="admin-modal-footer">
            <button class="btn btn-secondary">Cancel</button>
            <button class="btn btn-primary">Confirm</button>
        </div>
    </div>
`;

// 3. Focus trap is handled automatically by accessibility-enhancements.js

// 4. Handle Escape key
document.addEventListener('keydown', function escapeHandler(e) {
    if (e.key === 'Escape') {
        closeModal();
        document.removeEventListener('keydown', escapeHandler);
    }
});

// 5. Return focus to trigger element when closed
function closeModal() {
    modal.remove();
    triggerButton.focus();
}
```

---

## Resources

### Internal
- **Full Audit**: See `ACCESSIBILITY_AUDIT_REPORT.md`
- **Changelog**: See `ACCESSIBILITY_CHANGES.md`
- **UI Standards**: See `docs/claude-ref/UI-STANDARDS.md`

### External
- **WCAG 2.1**: https://www.w3.org/WAI/WCAG21/quickref/
- **ARIA Authoring Practices**: https://www.w3.org/WAI/ARIA/apg/
- **WebAIM**: https://webaim.org/resources/
- **MDN Accessibility**: https://developer.mozilla.org/en-US/docs/Web/Accessibility

---

## Questions?

1. Check existing patterns in codebase first
2. Review WCAG 2.1 criterion (linked in audit report)
3. Test with keyboard and screen reader
4. When in doubt, ask: "Can a keyboard-only user complete this task?"

---

**Remember**: Accessibility is not optional - it's a core requirement. Every feature must be accessible from day one.

**Last Updated**: February 1, 2026
