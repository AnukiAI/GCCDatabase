# GCC Web Application Documentation

## Quick Navigation

### 📁 Core Documentation
Essential project documentation for understanding the system.

- **[Requirements](core/requirements.md)** - Complete requirements with user stories (US-001 to US-010)
- **[Requirements Gap Analysis](core/requirements-gaps.md)** - Current implementation gaps
- **[UI Gap Analysis](core/ui-gaps.md)** - Mockup vs implementation differences
- **[Issues List](core/issues-list.md)** - Active bugs and issues
- **[API Reference](core/api-reference.md)** - API documentation and testing strategy
- **[Architecture](core/architecture.md)** - System architecture overview
- **[Deployment Guide](core/deployment-guide.md)** - Deployment instructions

### 📁 Development Resources
Resources for developers working on the project.

- **[Setup Guide](development/setup-guide.md)** - Development environment setup
- **[Test Cases](development/test-cases.md)** - Manual UAT test cases
- **[Test Environment](development/test-environment-setup.md)** - Test environment configuration
- **[Test Credentials](development/test-credentials.md)** - Test user accounts
- **[Test Coverage Report](development/test-coverage-report.md)** - Current test coverage analysis
- **[Testing Strategy](development/testing-strategy.md)** - Overall testing approach
- **[Troubleshooting](development/troubleshooting.md)** - Common issues and solutions

### 📁 UI Mockups
HTML mockup files showing the intended design for all pages.

- **[View Mockups](mockups/)** - Interactive HTML mockups for all pages
  - Certificate views (tile, single, internal)
  - Dashboard and analytics
  - Admin interfaces (users, roles, products, companies)
  - Login and authentication screens

### 📁 Project Management
Active project tracking and management.

- **[TODO List](project-management/TODO.md)** - Current tasks and priorities
- **[Issue Tracker](project-management/issue-tracker.xlsx)** - Excel issue tracking

### 📁 Archive
Historical documentation and reference materials.

- **[Legacy Documents](archive/2025-08-legacy/)** - Old project documents and notes

## Quick Start for Developers

1. Read the [Setup Guide](development/setup-guide.md)
2. Review [Requirements](core/requirements.md) for feature understanding
3. Check [Issues List](core/issues-list.md) for known problems
4. Reference [UI Mockups](mockups/) for design implementation
5. Follow [Testing Strategy](development/testing-strategy.md) for quality assurance

## Important Implementation Gaps

### High Priority
- **Autocomplete Search** - Not implemented (see [UI Gap Analysis](core/ui-gaps.md))
- **Lazy Loading** - Using pagination instead of infinite scroll
- **Unique URLs** - Search state not preserved in URL
- **Excel Export** - Only basic CSV, needs proper formatting

### Known Issues
See [Issues List](core/issues-list.md) for complete list of bugs and problems.

## Test Credentials

```
Admin: admin@greencirclecertified.com / Admin123!
Analyst: analyst@greencirclecertified.com / Analyst123!
External: partner@amazon.com / Partner123!
```

See [Test Credentials](development/test-credentials.md) for complete list.

## Documentation Maintenance

This documentation structure was simplified on August 29, 2025 to:
- Reduce redundancy
- Improve organization  
- Make information easier to find
- Archive historical documents

For questions or updates, please maintain this structure and update the appropriate documents rather than creating new ones.