# Manual Accessibility Testing Guide

This guide complements the automated accessibility tests with manual testing procedures to ensure comprehensive ADA compliance for the GCC Web Application.

## Overview

While automated tests catch ~30-40% of accessibility issues, manual testing is essential for:
- Screen reader experience
- Keyboard navigation flows
- Cognitive accessibility
- Real user scenarios
- Context-dependent accessibility

## Required Tools

### Screen Readers
- **NVDA** (Windows) - Free, widely used
- **JAWS** (Windows) - Industry standard, paid
- **VoiceOver** (macOS) - Built-in
- **Orca** (Linux) - Free

### Browser Extensions
- **axe DevTools** - In-browser accessibility checker
- **Lighthouse** - Google's accessibility audit
- **Wave** - WebAIM's evaluation tool
- **Color Oracle** - Color blindness simulator

### Testing Tools
- **Keyboard only** - Unplug mouse
- **High contrast mode** - OS-level setting
- **Zoom tools** - Browser zoom to 200%

## Manual Testing Checklist

### 1. Keyboard Navigation Testing

#### Basic Navigation
- [ ] Tab through all interactive elements
- [ ] Ensure logical tab order
- [ ] Test Shift+Tab (reverse navigation)
- [ ] Verify focus indicators are visible
- [ ] Check focus doesn't get trapped

#### Specific Elements
- [ ] **Dropdown menus** - Open with Enter/Space, navigate with arrows
- [ ] **Modal dialogs** - Focus moves to modal, can close with Esc
- [ ] **Form controls** - All inputs accessible via keyboard
- [ ] **Data tables** - Navigate with arrow keys if supported
- [ ] **Carousel/sliders** - Control with arrow keys

#### Test Procedure
```
1. Start at top of page
2. Press Tab repeatedly
3. Note each focused element
4. Verify focus visibility
5. Test functionality with Enter/Space
6. Document any focus traps or missing elements
```

### 2. Screen Reader Testing

#### NVDA Testing (Windows)
```
1. Install NVDA (free)
2. Start NVDA with Ctrl+Alt+N
3. Navigate using screen reader commands:
   - H: Next heading
   - Shift+H: Previous heading
   - 1-6: Heading levels
   - F: Next form field
   - T: Next table
   - L: Next link
   - B: Next button
```

#### Key Screen Reader Checks
- [ ] **Page title** - Announces correctly
- [ ] **Headings** - Logical structure (H1, H2, H3)
- [ ] **Links** - Descriptive text, not "click here"
- [ ] **Buttons** - Clear purpose and action
- [ ] **Form labels** - Associated with inputs
- [ ] **Error messages** - Announced when they appear
- [ ] **Status updates** - Dynamic content changes announced
- [ ] **Images** - Alt text conveys meaning
- [ ] **Tables** - Headers announced with data

#### Certificate Page Testing
```
Test Scenario: Find and view a certificate

1. Navigate to certificates page
2. Use heading navigation (H key)
3. Find and activate search
4. Search for "organic food"
5. Navigate to first result
6. Verify certificate details are announced
7. Check if download links are accessible
```

### 3. Visual Testing

#### Color Contrast Testing
- [ ] Test with **Color Oracle** for color blindness
- [ ] Use **WebAIM Contrast Checker**
- [ ] Verify 4.5:1 ratio for normal text
- [ ] Verify 3:1 ratio for large text
- [ ] Check focus indicators meet contrast requirements

#### High Contrast Mode
```
Windows: Settings > Ease of Access > High Contrast
1. Enable high contrast
2. Navigate through application
3. Verify all content visible
4. Check focus indicators remain visible
5. Test interactive elements
```

#### Zoom Testing
```
1. Set browser zoom to 200%
2. Verify all content accessible
3. Check no horizontal scrolling required
4. Test that functionality remains intact
5. Verify responsive design works
```

### 4. Mobile Accessibility Testing

#### iOS VoiceOver Testing
```
1. Enable VoiceOver: Settings > Accessibility > VoiceOver
2. Navigate with swipe gestures
3. Test custom gestures for complex interactions
4. Verify orientation changes work
5. Check touch target sizes (44x44px minimum)
```

#### Android TalkBack Testing
```
1. Enable TalkBack: Settings > Accessibility > TalkBack
2. Navigate with explore by touch
3. Test voice commands
4. Verify gesture navigation
5. Check screen reader focus
```

### 5. Cognitive Accessibility Testing

#### Timeouts and Sessions
- [ ] No unexpected timeouts during form completion
- [ ] Warning before session expires
- [ ] Ability to extend sessions
- [ ] Data preservation during interruptions

#### Error Handling
- [ ] Clear, jargon-free error messages
- [ ] Suggestions for fixing errors
- [ ] Error prevention where possible
- [ ] Multiple ways to correct errors

#### Instructions and Help
- [ ] Clear instructions provided
- [ ] Help text available when needed
- [ ] Consistent terminology throughout
- [ ] Simple language used

### 6. Form Accessibility Testing

#### Label Association
```
Test each form field:
1. Tab to field
2. Verify label is announced
3. Check programmatic association
4. Test placeholder text (should not replace labels)
5. Verify required field indicators
```

#### Validation Testing
```
1. Submit form with errors
2. Verify error messages appear
3. Check focus moves to first error
4. Test inline validation
5. Verify success messages
```

### 7. Role-Based Accessibility Testing

Test accessibility for each user role:

#### Public Users
- [ ] Home page navigation
- [ ] Certificate search functionality
- [ ] Public certificate viewing

#### External Users
- [ ] Login process
- [ ] Certificate filtering
- [ ] Detailed certificate views

#### Analyst Users
- [ ] Dashboard accessibility
- [ ] Data entry forms
- [ ] Report generation

#### Admin Users
- [ ] User management interfaces
- [ ] System configuration pages
- [ ] Administrative reporting

## Testing Scenarios by Page

### Home Page Testing
```
Manual Test: Homepage Navigation
1. Load homepage with screen reader
2. Verify page title announces
3. Navigate main menu with arrows
4. Test search functionality
5. Check footer links accessibility
6. Verify logo is accessible
```

### Certificate Pages Testing
```
Manual Test: Certificate Search and View
1. Navigate to certificate page
2. Use screen reader to find search
3. Enter search term "organic"
4. Navigate results with screen reader
5. Select and view certificate details
6. Test download links
7. Verify related certificates section
```

### Admin Interface Testing
```
Manual Test: User Management
1. Login as admin user
2. Navigate to user management
3. Test data table navigation
4. Use keyboard to sort columns
5. Test add new user form
6. Verify validation messages
7. Test modal dialog accessibility
```

## Documentation Template

### Test Session Documentation
```
Date: [Date]
Tester: [Name]
Tool: [Screen Reader/Browser]
Page: [URL/Page Name]

Findings:
- Issue 1: [Description]
  - Severity: Critical/Major/Minor
  - Impact: [User impact]
  - Recommendation: [Fix suggestion]

- Issue 2: [Description]
  - Severity: Critical/Major/Minor
  - Impact: [User impact]
  - Recommendation: [Fix suggestion]
```

### Severity Levels
- **Critical**: Prevents task completion
- **Major**: Makes task difficult or confusing
- **Minor**: Slight inconvenience but workaround available

## Best Practices

### Testing Environment
1. Test in multiple browsers (Chrome, Firefox, Safari, Edge)
2. Use actual assistive technology devices when possible
3. Test on different operating systems
4. Include users with disabilities in testing process

### Frequency
- **New features**: Test before release
- **Regular audits**: Monthly manual testing
- **User feedback**: Document and test reported issues
- **Major updates**: Comprehensive accessibility review

### Integration with Development
1. Include accessibility testing in QA process
2. Provide developer training on accessibility
3. Create accessibility testing templates
4. Maintain accessibility issue tracking

## Common Issues to Watch For

### Navigation Issues
- Skip to main content link missing
- Focus traps in modals
- Invisible focus indicators
- Illogical tab order

### Content Issues
- Empty links or buttons
- Images without alt text
- Headings out of sequence
- Form labels not associated

### Interactive Issues
- Keyboard inaccessible dropdowns
- Missing error announcements
- Timeout issues
- Unannounced dynamic content

## Resources

### WCAG Guidelines
- [WCAG 2.1 Quick Reference](https://www.w3.org/WAI/WCAG21/quickref/)
- [Understanding WCAG 2.1](https://www.w3.org/WAI/WCAG21/Understanding/)

### Screen Reader Guides
- [NVDA User Guide](https://www.nvaccess.org/documentation/)
- [VoiceOver User Guide](https://support.apple.com/guide/voiceover/)
- [JAWS Documentation](https://support.freedomscientific.com/Teachers/Manuals/JAWS)

### Testing Tools
- [WebAIM Wave](https://wave.webaim.org/)
- [axe DevTools](https://www.deque.com/axe/devtools/)
- [Lighthouse](https://developers.google.com/web/tools/lighthouse)

## Reporting and Follow-up

### Issue Reporting
1. Use consistent issue templates
2. Include screenshots/recordings when helpful
3. Provide clear reproduction steps
4. Link to relevant WCAG criteria

### Tracking Progress
1. Maintain accessibility issue backlog
2. Prioritize by severity and user impact
3. Retest after fixes are implemented
4. Document improvements and trends

### Training and Awareness
1. Regular team training sessions
2. Share testing findings with development team
3. Create accessibility champions program
4. Maintain accessibility testing documentation