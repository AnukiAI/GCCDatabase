# Pre-Deployment Testing Guide

## 🎯 Purpose

These automated tests verify that **login works correctly** before deploying to production.

**Critical Rule:** If these tests fail, **DO NOT DEPLOY** until the issue is fixed.

---

## 🚀 Quick Start - Run Before Every Deployment

### **Option 1: Quick Smoke Test (30 seconds)**

```cmd
test-before-deploy.cmd
```

This runs the single most critical test: **Admin login must work**.

### **Option 2: Full Critical Tests (2-3 minutes)**

```cmd
test-all-critical.cmd
```

This runs all critical login tests including:
- Admin login works
- Analyst login works
- External user login works
- Authentication cookies are set
- Protected pages are accessible
- Invalid credentials are rejected

---

## ✅ What These Tests Verify

### **1. Cookie Authentication**
- ✅ Auth cookie (`GCC.Auth`) is set after successful login
- ✅ Cookie is not immediately expired
- ✅ Cookie works even without valid SSL certificate

### **2. User & Role Configuration**
- ✅ Admin user exists and is active
- ✅ Admin user has Admin role assigned
- ✅ Password hash verification works
- ✅ All test users (admin, analyst, external) can login

### **3. Redirect Behavior**
- ✅ Successful login redirects to dashboard/homepage
- ✅ Failed login shows error message
- ✅ Protected pages are accessible after login

### **4. DataProtection Keys**
- ✅ Keys persist between test runs
- ✅ Cookies remain valid after app restart

---

## 🔴 If Tests Fail

### **Common Issues & Fixes**

**Issue 1: "Authentication cookie must be set"**
```
Fix: Cookie security policy is blocking cookies
Location: Program.cs line 199
Change: CookieSecurePolicy.Always → CookieSecurePolicy.SameAsRequest
```

**Issue 2: "User not found"**
```
Fix: Database not seeded with test users
Run: dotnet ef database update
Or: Check production database has admin user
```

**Issue 3: "Invalid password"**
```
Fix: Password hash doesn't match
Check: User.PasswordHash in database
Regenerate: See docs/database/USER_SETUP.md
```

**Issue 4: "No role assigned"**
```
Fix: User has no role in UserRoles table
SQL: INSERT INTO UserRoles (UsersId, RolesId) VALUES (1, 1)
```

---

## 📋 Integration with CI/CD

### **GitHub Actions (Future)**

```yaml
- name: Pre-Deployment Tests
  run: test-before-deploy.cmd

- name: Deploy
  if: success()
  run: deploy.cmd
```

### **Manual Workflow**

1. Make code changes
2. **Run:** `test-before-deploy.cmd`
3. ✅ **If PASS:** Proceed with deployment
4. ❌ **If FAIL:** Fix issue, retest, then deploy

---

## 🧪 Test Details

### **Location**
```
GCCWebApp-Tests/GCCWebApp.Tests/Authentication/LoginIntegrationTests.cs
```

### **Run Specific Tests**

```cmd
# Run only smoke test
cd GCCWebApp-Tests\GCCWebApp.Tests
dotnet test --filter "Category=SmokeTest"

# Run all critical tests
dotnet test --filter "Category=Critical&Category=PreDeployment"

# Run specific test
dotnet test --filter "FullyQualifiedName~Login_WithValidAdminCredentials"
```

### **Test Categories**

- `SmokeTest` - Bare minimum test that must pass
- `Critical` - Core functionality tests
- `PreDeployment` - Tests that should run before deployment

---

## 📝 Adding New Login Tests

When adding new authentication features, add tests to `LoginIntegrationTests.cs`:

```csharp
[Fact]
[Trait("Category", "Critical")]
[Trait("Category", "PreDeployment")]
public async Task Login_WithNewFeature_ShouldWork()
{
    // Arrange
    // Act
    // Assert
}
```

---

## 🎯 Success Criteria

**Tests PASS when:**
- ✅ All tests show green checkmarks
- ✅ Exit code is 0
- ✅ Message says "SAFE TO DEPLOY"

**Tests FAIL when:**
- ❌ Any test shows red X
- ❌ Exit code is non-zero
- ❌ Message says "DO NOT DEPLOY"

---

## 🆘 Getting Help

**If tests fail and you can't figure out why:**

1. Check the detailed error message in test output
2. Review recent code changes (what changed since last working version?)
3. Check `docs/deployment/LOGIN_TROUBLESHOOTING.md`
4. Use TestAuth endpoint: `/Account/TestAuth?username=admin&password=TestPass123!`

---

## 📚 Related Documentation

- `docs/deployment/LOGIN_TROUBLESHOOTING.md` - Login issue diagnosis
- `docs/database/USER_SETUP.md` - Creating/managing users
- `docs/core/AUTHENTICATION.md` - How authentication works
- `.claude/CLAUDE.md` - Full project documentation

---

**Remember:** These tests exist to **prevent production issues**.

If a test fails, it means there's a real problem that would affect users. Fix it before deploying!
