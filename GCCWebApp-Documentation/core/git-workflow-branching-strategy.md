# Git Workflow & Branching Strategy - GCC Database Solution

## Overview
This document defines the Git workflow and branching strategy for the GCC Database Solution project. Our strategy balances development velocity with code stability and supports multiple developers working simultaneously.

## Branching Strategy

### Main Branches

#### `master` (Production)
- **Purpose**: Production-ready code
- **Protection**: Protected branch with strict rules
- **Deployment**: Automatically deployed to production
- **Merge Requirements**:
  - 2+ approvals required
  - All CI checks passing
  - No direct pushes allowed

#### `develop` (Integration)
- **Purpose**: Integration branch for ongoing development
- **Protection**: Protected branch
- **Merge Requirements**:
  - 1+ approval required
  - CI checks passing
- **Deployment**: Automatically deployed to staging environment

### Supporting Branches

#### Feature Branches (`feature/`)
```bash
# Naming convention
feature/[ticket-id]-brief-description
feature/US-001-certificate-creation
feature/BUG-042-fix-search-pagination
feature/TECH-015-upgrade-entity-framework
```

**Lifecycle:**
- **Created from**: `develop`
- **Merged into**: `develop`
- **Lifetime**: Until feature is complete and merged
- **Naming**: `feature/[type]-[id]-[description]`

#### Hotfix Branches (`hotfix/`)
```bash
# Naming convention
hotfix/[version]-[brief-description]
hotfix/v1.2.1-security-vulnerability
hotfix/v1.2.2-certificate-download-bug
```

**Lifecycle:**
- **Created from**: `master`
- **Merged into**: `master` AND `develop`
- **Lifetime**: Until hotfix is deployed
- **Triggers**: Critical production issues

#### Release Branches (`release/`)
```bash
# Naming convention
release/v[major].[minor].[patch]
release/v1.3.0
release/v2.0.0
```

**Lifecycle:**
- **Created from**: `develop`
- **Merged into**: `master` AND `develop`
- **Purpose**: Final testing and bug fixes before release

## Workflow Procedures

### Feature Development Workflow

#### 1. Start New Feature
```bash
# Sync with latest develop
git checkout develop
git pull origin develop

# Create feature branch
git checkout -b feature/US-001-certificate-creation

# Push branch to remote
git push -u origin feature/US-001-certificate-creation
```

#### 2. Development Process
```bash
# Make changes and commit frequently
git add .
git commit -m "feat: Add certificate creation form validation

- Implement client-side validation for required fields
- Add server-side validation with meaningful error messages
- Update unit tests for validation scenarios

Related to US-001"

# Push changes regularly
git push origin feature/US-001-certificate-creation
```

#### 3. Keep Feature Branch Updated
```bash
# Sync with develop regularly (at least weekly)
git checkout develop
git pull origin develop
git checkout feature/US-001-certificate-creation
git merge develop

# Or use rebase for cleaner history
git rebase develop
```

#### 4. Complete Feature
```bash
# Final sync and testing
git checkout develop
git pull origin develop
git checkout feature/US-001-certificate-creation
git rebase develop

# Run all tests
dotnet test

# Push final changes
git push origin feature/US-001-certificate-creation

# Create Pull Request via GitHub UI
```

#### 5. Post-Merge Cleanup
```bash
# After PR is merged, clean up
git checkout develop
git pull origin develop
git branch -d feature/US-001-certificate-creation
git push origin --delete feature/US-001-certificate-creation
```

### Hotfix Workflow

#### 1. Create Hotfix Branch
```bash
# Start from master
git checkout master
git pull origin master

# Create hotfix branch
git checkout -b hotfix/v1.2.1-security-vulnerability
git push -u origin hotfix/v1.2.1-security-vulnerability
```

#### 2. Implement Fix
```bash
# Make minimal changes to fix issue
git add .
git commit -m "fix: Resolve SQL injection vulnerability in search

- Add parameterized queries for certificate search
- Update input validation for search parameters
- Add security tests for search functionality

Fixes SECURITY-001"

git push origin hotfix/v1.2.1-security-vulnerability
```

#### 3. Deploy and Merge
```bash
# Create PR to master (fast-track approval)
# After deployment verification:

# Merge to master
git checkout master
git merge --no-ff hotfix/v1.2.1-security-vulnerability
git tag v1.2.1
git push origin master --tags

# Merge to develop
git checkout develop
git merge --no-ff hotfix/v1.2.1-security-vulnerability
git push origin develop

# Clean up
git branch -d hotfix/v1.2.1-security-vulnerability
git push origin --delete hotfix/v1.2.1-security-vulnerability
```

### Release Workflow

#### 1. Create Release Branch
```bash
# Start from develop when ready for release
git checkout develop
git pull origin develop

# Create release branch
git checkout -b release/v1.3.0
git push -u origin release/v1.3.0
```

#### 2. Release Preparation
```bash
# Update version numbers
# Update CHANGELOG.md
# Final bug fixes only
git add .
git commit -m "chore: Prepare release v1.3.0

- Update version to 1.3.0
- Update changelog with new features
- Fix minor UI alignment issues

Release v1.3.0"

git push origin release/v1.3.0
```

#### 3. Release Deployment
```bash
# After QA approval, merge to master
git checkout master
git merge --no-ff release/v1.3.0
git tag v1.3.0
git push origin master --tags

# Merge back to develop
git checkout develop
git merge --no-ff release/v1.3.0
git push origin develop

# Clean up
git branch -d release/v1.3.0
git push origin --delete release/v1.3.0
```

## Commit Message Standards

### Conventional Commits Format
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Commit Types
- **feat**: New feature for the user
- **fix**: Bug fix for the user
- **docs**: Documentation changes
- **style**: Code formatting (no logic changes)
- **refactor**: Code restructuring (no functional changes)
- **test**: Adding or updating tests
- **chore**: Maintenance tasks (dependencies, build)
- **perf**: Performance improvements
- **security**: Security-related changes

### Examples
```bash
# Feature commit
git commit -m "feat: Add certificate search autocomplete

- Implement real-time search suggestions
- Add debounced API calls for performance
- Update UI with dropdown suggestion list

Implements US-002"

# Bug fix commit
git commit -m "fix: Resolve certificate download PDF generation

- Fix QuestPDF template rendering issue
- Add proper error handling for missing data
- Update unit tests for PDF generation service

Fixes BUG-042"

# Breaking change commit
git commit -m "feat!: Redesign authentication system

BREAKING CHANGE: Cookie authentication replaced with JWT tokens.
Migration guide available in docs/migration/v2.0.md

Implements ARCH-001"

# Hotfix commit
git commit -m "fix: Patch SQL injection vulnerability in search

- Replace string concatenation with parameterized queries
- Add input sanitization for all search parameters
- Include regression tests for security validation

Fixes SECURITY-001"
```

## Branch Protection Rules

### Master Branch Protection
```yaml
# GitHub branch protection settings
protection_rules:
  master:
    required_status_checks:
      strict: true
      contexts:
        - "ci/build"
        - "ci/test-unit"
        - "ci/test-integration"
        - "ci/security-scan"
        - "ci/sonarcloud"

    required_pull_request_reviews:
      required_approving_review_count: 2
      dismiss_stale_reviews: true
      require_code_owner_reviews: true
      restrict_review_dismissals: true

    restrictions:
      users: []
      teams: ["senior-developers", "tech-leads"]

    enforce_admins: true
    allow_force_pushes: false
    allow_deletions: false
```

### Develop Branch Protection
```yaml
protection_rules:
  develop:
    required_status_checks:
      strict: true
      contexts:
        - "ci/build"
        - "ci/test-unit"
        - "ci/test-integration"

    required_pull_request_reviews:
      required_approving_review_count: 1
      dismiss_stale_reviews: true

    allow_force_pushes: false
    allow_deletions: false
```

## Merge Strategies

### Feature Branch Merges
**Strategy**: Squash and Merge
```bash
# Squashes all feature commits into a single commit
git checkout develop
git merge --squash feature/US-001-certificate-creation
git commit -m "feat: Add certificate creation functionality

- Implement certificate creation form with validation
- Add business logic for certificate lifecycle
- Include comprehensive test coverage
- Update documentation

Implements US-001"
```

**Benefits**:
- Clean, linear history
- Single commit per feature
- Easy to revert entire features

### Release and Hotfix Merges
**Strategy**: Merge Commit (No Fast-Forward)
```bash
# Preserves branch history and merge context
git checkout master
git merge --no-ff release/v1.3.0
```

**Benefits**:
- Preserves release branch context
- Clear release boundaries in history
- Easier to track what was included in each release

## Git Configuration

### Required Git Configuration
```bash
# User identity
git config --global user.name "Your Name"
git config --global user.email "your.email@company.com"

# Default branch
git config --global init.defaultBranch main

# Editor
git config --global core.editor "code --wait"

# Line endings (Windows)
git config --global core.autocrlf true

# Line endings (Mac/Linux)
git config --global core.autocrlf input

# Pull strategy
git config --global pull.rebase false

# Push strategy
git config --global push.default simple
```

### Recommended Git Aliases
```bash
# Add useful aliases
git config --global alias.co checkout
git config --global alias.br branch
git config --global alias.ci commit
git config --global alias.st status
git config --global alias.unstage 'reset HEAD --'
git config --global alias.last 'log -1 HEAD'
git config --global alias.visual '!gitk'

# Log aliases
git config --global alias.lg "log --oneline --graph --decorate --all"
git config --global alias.ls "log --pretty=format:'%C(yellow)%h%Cred%d\\ %Creset%s%Cblue\\ [%cn]' --decorate"
git config --global alias.ll "log --pretty=format:'%C(yellow)%h%Cred%d\\ %Creset%s%Cblue\\ [%cn]' --decorate --numstat"

# Sync aliases
git config --global alias.sync-develop '!git checkout develop && git pull origin develop'
git config --global alias.sync-master '!git checkout master && git pull origin master'
```

## Best Practices

### Daily Development
1. **Start each day**: Sync with develop branch
2. **Commit frequently**: Small, logical commits
3. **Push regularly**: Don't let commits pile up locally
4. **Meaningful messages**: Follow conventional commit format
5. **Test before pushing**: Run tests locally

### Branch Management
1. **Short-lived branches**: Keep feature branches under 2 weeks
2. **Regular syncing**: Merge develop into feature branches weekly
3. **Clean up**: Delete merged branches promptly
4. **Descriptive names**: Use clear, consistent naming conventions

### Collaboration
1. **Communication**: Discuss large changes before implementing
2. **Code reviews**: Review all code before merging
3. **Documentation**: Update docs with code changes
4. **Issue tracking**: Link commits to issue numbers

## Troubleshooting Common Scenarios

### Merge Conflicts
```bash
# When conflicts occur during merge
git checkout feature/your-branch
git merge develop

# Edit conflicted files, then:
git add conflicted-file.cs
git commit -m "resolve: Merge conflicts with develop branch"
git push origin feature/your-branch
```

### Accidental Commit to Wrong Branch
```bash
# If you committed to master instead of feature branch
git checkout master
git reset --soft HEAD~1  # Undo last commit, keep changes

git checkout -b feature/correct-branch
git add .
git commit -m "feat: Your intended commit message"
```

### Reverting Changes
```bash
# Revert a commit (creates new commit undoing changes)
git revert commit-hash

# Reset to previous state (destructive)
git reset --hard commit-hash  # Use with caution!

# Revert merge commit
git revert -m 1 merge-commit-hash
```

### Large File Handling
```bash
# Use Git LFS for large files
git lfs track "*.pdf"
git lfs track "*.png"
git lfs track "*.jpg"

# Add .gitattributes file
git add .gitattributes
git commit -m "chore: Configure Git LFS for binary files"
```

## Release Management

### Version Numbering
Follow Semantic Versioning (SemVer): `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Schedule
- **Major releases**: Quarterly (v2.0.0, v3.0.0)
- **Minor releases**: Monthly (v1.1.0, v1.2.0)
- **Patch releases**: As needed (v1.1.1, v1.1.2)
- **Hotfixes**: Immediate for critical issues

### Release Checklist
- [ ] All tests passing
- [ ] Documentation updated
- [ ] CHANGELOG.md updated
- [ ] Version numbers updated
- [ ] Security scan completed
- [ ] Performance regression testing
- [ ] Backup procedures verified

## CI/CD Integration

### GitHub Actions Workflow
```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ master, develop ]
  pull_request:
    branches: [ master, develop ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3
      with:
        fetch-depth: 0

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
      uses: securecodewarrior/github-action-add-sarif@v1
      with:
        sarif-file: security-scan-results.sarif
```

### Deployment Triggers
- **Master branch**: Deploy to production
- **Develop branch**: Deploy to staging
- **Release branches**: Deploy to UAT environment
- **Tags**: Create GitHub releases

## Documentation Updates

### When to Update This Document
- Changes to branching strategy
- New merge policies
- Updated protection rules
- Tool changes (Git, GitHub, CI/CD)
- Process improvements from retrospectives

### Related Documents
- [Code Review Guidelines](code-review-guidelines.md)
- [Coding Standards Document](coding-standards-document.md)
- [CI/CD Pipeline Documentation](build-cicd-pipeline-documentation.md)

---

## Training and Onboarding

### New Developer Git Training
1. **Git fundamentals**: Basic commands and concepts
2. **Branching strategy**: Practice creating and merging branches
3. **Commit standards**: Learn conventional commit format
4. **Code review process**: Understand PR workflow
5. **Troubleshooting**: Practice conflict resolution

### Git Workflow Quick Reference Card
```bash
# Daily workflow
git checkout develop && git pull origin develop
git checkout -b feature/new-feature
# ... make changes ...
git add . && git commit -m "feat: description"
git push origin feature/new-feature
# Create PR via GitHub

# Emergency hotfix
git checkout master && git pull origin master
git checkout -b hotfix/v1.2.1-critical-fix
# ... make fix ...
git add . && git commit -m "fix: critical issue"
# Create PR to master, fast-track approval
```

This Git workflow provides structure while maintaining flexibility for different development scenarios and team sizes.

---
*Last Updated: September 17, 2025*