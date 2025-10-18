# Code Review Guidelines - GCC Database Solution

## Overview
This document establishes the code review process for the GCC Database Solution project. Code reviews are mandatory for all changes and ensure code quality, security, maintainability, and knowledge sharing across the team.

## Code Review Process

### Pull Request Requirements

#### Before Creating a Pull Request
1. **Run All Tests**
   ```bash
   # Unit tests
   dotnet test GCCWebApp.Tests

   # Integration tests
   dotnet test GCCWebApp.UAT.Tests

   # API tests
   dotnet test GCCWebApp.Api.Tests

   # Security tests
   dotnet test GCCWebApp.Security.Tests
   ```

2. **Build and Lint**
   ```bash
   # Clean build
   dotnet clean
   dotnet build

   # Code formatting
   dotnet format
   ```

3. **Update Documentation**
   - Update CLAUDE.md for significant changes
   - Add/update XML documentation for public APIs
   - Update README if setup process changes

#### Pull Request Template
```markdown
## Description
Brief description of the changes and their purpose.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Security enhancement

## Testing
- [ ] I have added/updated unit tests
- [ ] I have tested the changes manually
- [ ] All existing tests pass
- [ ] I have updated integration tests if applicable

## Security Checklist
- [ ] No hardcoded credentials or secrets
- [ ] Input validation implemented
- [ ] Output encoding applied where needed
- [ ] Authorization checks in place
- [ ] SQL injection prevention verified

## Documentation
- [ ] Code comments added/updated
- [ ] XML documentation updated for public APIs
- [ ] CLAUDE.md updated if applicable
- [ ] User-facing documentation updated

## Screenshots/Evidence
Add screenshots for UI changes or test results for bug fixes.

## Related Issues
Fixes #(issue number)
```

### Review Assignment

#### Automatic Reviewers
- **All PRs**: At least 2 reviewers required
- **Security changes**: Security specialist + 1 additional reviewer
- **Database changes**: Senior developer + 1 additional reviewer
- **Frontend changes**: Frontend specialist + 1 additional reviewer

#### Review Priority Levels
1. **Critical (24 hours)**: Security fixes, production bugs
2. **High (48 hours)**: New features, performance improvements
3. **Medium (3 days)**: Refactoring, non-critical bug fixes
4. **Low (1 week)**: Documentation, minor improvements

### Review Timeline
- **Initial review**: Within review priority timeframe
- **Re-review after changes**: Within 24 hours
- **Final approval**: Within 24 hours of last change

## Code Review Checklist

### Functionality Review

#### Core Functionality
- [ ] **Code works as intended**: Features function according to requirements
- [ ] **Edge cases handled**: Boundary conditions and error scenarios covered
- [ ] **Business logic correct**: Rules and validations align with requirements
- [ ] **User experience**: UI/UX follows design guidelines and is intuitive
- [ ] **Accessibility compliance**: Minimum 16px font size, proper ARIA labels

#### Integration
- [ ] **Database operations**: Proper use of Entity Framework patterns
- [ ] **API contracts**: Request/response models follow established patterns
- [ ] **External dependencies**: Third-party integrations properly handled
- [ ] **Configuration**: New settings added to appsettings.json appropriately

### Code Quality Review

#### Structure and Design
- [ ] **Follows coding standards**: Adheres to established naming and style conventions
- [ ] **SOLID principles**: Single responsibility, proper abstraction levels
- [ ] **Design patterns**: Repository pattern, service layer, dependency injection
- [ ] **Code organization**: Logical file structure and namespace usage
- [ ] **Minimal changes**: Only necessary changes included, no scope creep

#### Readability and Maintainability
- [ ] **Clear naming**: Variables, methods, and classes have descriptive names
- [ ] **Appropriate comments**: Complex logic explained, why not just what
- [ ] **Method size**: Functions are focused and not overly complex
- [ ] **Class responsibilities**: Each class has a single, clear purpose
- [ ] **Documentation**: Public APIs have XML documentation

#### Performance and Efficiency
- [ ] **Database queries**: Efficient use of LINQ, proper indexing considered
- [ ] **Async/await**: Proper async patterns, no blocking calls
- [ ] **Memory management**: No obvious memory leaks or excessive allocations
- [ ] **Caching strategy**: Appropriate caching where beneficial
- [ ] **API performance**: Response times reasonable, pagination implemented

### Security Review

#### Authentication and Authorization
- [ ] **Role-based access**: Proper authorization attributes on controllers/actions
- [ ] **Authentication flow**: Secure login/logout mechanisms
- [ ] **Session management**: Proper session handling and timeout
- [ ] **API security**: API keys, JWT tokens properly validated

#### Input Validation and Sanitization
- [ ] **Server-side validation**: All inputs validated on server
- [ ] **SQL injection prevention**: Parameterized queries only
- [ ] **XSS prevention**: Output encoding implemented
- [ ] **CSRF protection**: Anti-forgery tokens on state-changing operations
- [ ] **File upload security**: Proper validation and sanitization

#### Data Protection
- [ ] **Sensitive data**: No credentials or secrets in code
- [ ] **Data encryption**: Sensitive data encrypted at rest and in transit
- [ ] **Logging security**: No sensitive data in logs
- [ ] **Error handling**: No sensitive information in error messages

### Testing Review

#### Test Coverage
- [ ] **Unit tests**: New code has appropriate unit test coverage
- [ ] **Integration tests**: End-to-end scenarios tested
- [ ] **Edge cases**: Boundary conditions and error scenarios tested
- [ ] **Test quality**: Tests are clear, focused, and maintainable

#### Test Implementation
- [ ] **Meaningful test names**: Tests clearly describe what they verify
- [ ] **Arrange-Act-Assert**: Clear test structure
- [ ] **Mock usage**: Appropriate use of mocks and stubs
- [ ] **Test data**: Proper test data setup and cleanup

### Frontend Review (for UI changes)

#### User Interface
- [ ] **Responsive design**: Works on mobile, tablet, and desktop
- [ ] **Cross-browser compatibility**: Tested in major browsers
- [ ] **JavaScript quality**: Clean, modern JavaScript/TypeScript
- [ ] **CSS organization**: Follows BEM methodology, uses CSS variables

#### Accessibility and Usability
- [ ] **Font size**: Minimum 16px as per requirements
- [ ] **Keyboard navigation**: All interactive elements accessible via keyboard
- [ ] **Screen reader compatibility**: Proper ARIA labels and roles
- [ ] **Color contrast**: Meets WCAG guidelines
- [ ] **Error messages**: Clear, user-friendly error messages

#### Master Design Guide Compliance
- [ ] **Design consistency**: Follows Master Design Guide patterns
- [ ] **Component reuse**: Uses existing components where appropriate
- [ ] **Branding**: Consistent with GCC visual identity
- [ ] **Navigation**: No search bar in navigation as per requirements

## Review Comments Guidelines

### Constructive Feedback
```markdown
# GOOD: Specific, actionable feedback
Consider using `ConfigureAwait(false)` here to avoid potential deadlocks:
```csharp
var result = await _service.GetDataAsync().ConfigureAwait(false);
```

# GOOD: Explaining the why
This method could benefit from input validation to prevent null reference exceptions:
```csharp
public void ProcessCertificate(Certificate certificate)
{
    ArgumentNullException.ThrowIfNull(certificate);
    // ... rest of method
}
```

# AVOID: Vague comments
"This looks wrong"
"Fix this"
```

### Comment Categories

#### 🔒 **MUST FIX** (Blocking)
- Security vulnerabilities
- Breaking functionality
- Performance issues
- Test failures

#### ⚠️ **SHOULD FIX** (High Priority)
- Code quality issues
- Missing error handling
- Accessibility problems
- Standards violations

#### 💡 **SUGGESTION** (Nice to Have)
- Performance optimizations
- Code readability improvements
- Better naming suggestions
- Refactoring opportunities

#### ❓ **QUESTION** (Clarification)
- Understanding implementation choices
- Confirming requirements
- Discussing alternatives

### Review Comment Examples

#### Security Comments
```markdown
🔒 **MUST FIX**: This endpoint is missing authorization:
```csharp
[HttpPost]
[Authorize(Roles = "Admin,Analyst")] // Add this
public async Task<IActionResult> DeleteCertificate(int id)
```

⚠️ **SHOULD FIX**: Input validation missing:
```csharp
public async Task<Certificate> GetCertificate(int id)
{
    if (id <= 0) throw new ArgumentException("Invalid ID");
    // ... rest of method
}
```
```

#### Performance Comments
```markdown
💡 **SUGGESTION**: Consider using Select() to avoid loading full entities:
```csharp
// Instead of
var certificates = await _context.Certificates.ToListAsync();

// Consider
var certificates = await _context.Certificates
    .Select(c => new CertificateListItem { Id = c.Id, Name = c.Name })
    .ToListAsync();
```
```

#### Code Quality Comments
```markdown
⚠️ **SHOULD FIX**: Method is doing too much, consider splitting:
```csharp
// Split this into multiple methods:
// - ValidateCertificate()
// - SaveCertificate()
// - NotifyUsers()
public async Task<IActionResult> CreateCertificate(...)
```
```

## Approval Process

### Approval Requirements
- **2 approvals minimum** for all PRs
- **All conversations resolved** before merge
- **All CI checks passing** (build, tests, security scans)
- **No merge conflicts** with target branch

### Special Cases

#### Hotfixes
- **1 approval required** for critical production fixes
- **Fast-track review** within 2 hours
- **Post-merge review** required within 24 hours

#### Documentation Only
- **1 approval required** for documentation-only changes
- **24-hour review window**

#### Dependency Updates
- **Automated testing** must pass
- **Security scan** required for new dependencies
- **Impact assessment** for major version updates

### Merge Strategy
```bash
# Use squash and merge for feature branches
git checkout main
git merge --squash feature/new-feature
git commit -m "feat: Add new certificate validation feature

- Implement server-side validation for certificate data
- Add client-side validation with user feedback
- Update tests for new validation rules

Closes #123"

# Use merge commit for release branches
git checkout main
git merge --no-ff release/v1.2.0
```

## Review Tools and Automation

### Required Tools
1. **GitHub Pull Requests**: Primary review platform
2. **SonarCloud**: Automated code quality analysis
3. **CodeQL**: Security vulnerability scanning
4. **Dependabot**: Dependency security alerts

### IDE Extensions
- **SonarLint**: Real-time code quality feedback
- **GitHub Copilot**: AI-powered code suggestions
- **IntelliCode**: Intelligent completions

### Automated Checks
```yaml
# .github/workflows/pr-checks.yml
name: PR Checks
on: [pull_request]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal

      - name: Security Scan
        run: dotnet run --project SecurityScanner
```

## Common Review Scenarios

### New Feature Reviews

#### Feature Checklist
- [ ] **Requirements alignment**: Feature matches user stories/wireframes
- [ ] **Design consistency**: Follows Master Design Guide
- [ ] **Testing coverage**: Comprehensive test suite
- [ ] **Documentation**: Updated user and technical docs
- [ ] **Migration path**: Database changes properly migrated

#### Example Review Flow
1. **Initial Review**: Architecture and approach
2. **Implementation Review**: Code quality and security
3. **Testing Review**: Test coverage and quality
4. **Final Review**: Integration and documentation

### Bug Fix Reviews

#### Bug Fix Checklist
- [ ] **Root cause identified**: Fix addresses underlying issue
- [ ] **Regression prevention**: Tests added to prevent recurrence
- [ ] **Side effects**: No unintended consequences
- [ ] **Documentation**: Issue resolution documented

### Refactoring Reviews

#### Refactoring Checklist
- [ ] **Behavior preservation**: Functionality unchanged
- [ ] **Test coverage**: Existing tests still pass
- [ ] **Performance impact**: No performance degradation
- [ ] **Breaking changes**: API compatibility maintained

## Review Metrics and Improvement

### Key Metrics
- **Review turnaround time**: Target < 48 hours
- **Defect escape rate**: Bugs found in production
- **Review coverage**: Percentage of code reviewed
- **Comment resolution time**: Time to address feedback

### Continuous Improvement
1. **Monthly review retrospectives**: Discuss process improvements
2. **Review training**: Regular sessions on review best practices
3. **Tool evaluation**: Assess and improve tooling
4. **Feedback collection**: Gather developer feedback on process

### Review Quality Assessment
```markdown
## Monthly Review Quality Report
- **Total PRs Reviewed**: 45
- **Average Review Time**: 36 hours
- **Critical Issues Found**: 3
- **Security Issues Found**: 1
- **Post-merge Issues**: 2 (4.4% rate)

## Areas for Improvement
- Increase security review focus
- Faster turnaround on documentation PRs
- Better async/await pattern reviews
```

## Emergency Procedures

### Critical Security Issues
1. **Immediate halt**: Stop all development activity
2. **Security assessment**: Evaluate impact and scope
3. **Hotfix process**: Fast-track security fix
4. **Post-incident review**: Learn and improve

### Production Failures
1. **Rapid response**: Emergency hotfix within 2 hours
2. **Minimal review**: Security and functionality only
3. **Follow-up review**: Comprehensive review post-deployment
4. **Root cause analysis**: Prevent future occurrences

---

## Training and Onboarding

### New Developer Onboarding
1. **Review this document**: Understand review standards
2. **Shadow experienced reviewers**: Learn by observation
3. **Practice reviews**: Start with low-risk changes
4. **Feedback sessions**: Regular one-on-ones with mentor

### Reviewer Certification
- **Code standards knowledge**: Pass coding standards quiz
- **Security awareness**: Complete security training
- **Tool proficiency**: Demonstrate review tool usage
- **Mock review**: Successfully complete practice review

---

This document should be reviewed and updated quarterly to reflect evolving best practices and team feedback.

---
*Last Updated: September 17, 2025*