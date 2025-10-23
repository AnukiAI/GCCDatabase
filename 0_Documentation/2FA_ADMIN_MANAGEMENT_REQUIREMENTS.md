# Two-Factor Authentication (2FA) Admin Management Requirements

**Document Version**: 1.0
**Date**: October 22, 2025
**Status**: Required Feature - High Priority

---

## Executive Summary

Administrators require the ability to manage users' Two-Factor Authentication (2FA) settings to assist users who experience login issues, device changes, or other 2FA-related problems. This feature is critical for user support and account recovery workflows.

---

## Business Justification

### User Provisioning Model
**GCC Database follows an admin-provisioned user model:**
1. Companies request access for their employees
2. Admins create user accounts and assign roles
3. Admins configure security settings (including 2FA requirements)
4. Users receive credentials and complete initial setup

### Problem Statement
Admins need comprehensive 2FA management capabilities for:

**User Provisioning:**
- Enable/require 2FA during user creation
- Enforce 2FA policies by role (e.g., all Admins must use 2FA)
- Configure 2FA method (TOTP, Email, SMS)

**Ongoing Management:**
- View 2FA status for all users at a glance
- Help users who experience 2FA lockouts due to:
  - Lost or broken authenticator devices
  - Changed phone numbers (for SMS 2FA)
  - Uninstalled authenticator apps
  - Incorrect time synchronization on devices
  - Device factory resets

**Without comprehensive admin 2FA management tools**, organizations face:
- Inability to enforce security policies
- Extended downtime for locked-out users
- Increased support ticket volume
- Poor user experience
- Inconsistent security posture across user accounts

### Business Impact
- **User Support**: Admins can quickly resolve 2FA issues without IT intervention
- **Security**: Maintains security while allowing controlled 2FA resets
- **Compliance**: Provides audit trail for 2FA administrative actions
- **User Experience**: Reduces user frustration and downtime

---

## Functional Requirements

### US-011: Admin 2FA Management

**User Story**:
As an **Administrator**, I want to **manage users' 2FA settings** so that I can **help users who are locked out or experiencing 2FA issues**.

#### Acceptance Criteria

1. **2FA Status Visibility**
   - [ ] Admin can view whether a user has 2FA enabled
   - [ ] Admin can see which 2FA method is configured (TOTP, SMS, Email)
   - [ ] Admin can see when 2FA was last configured/updated
   - [ ] 2FA status visible in User Management table

2. **2FA Dropdown Menu** (Per User)
   - [ ] Dropdown button/icon appears in Actions column for ALL users
   - [ ] Dropdown contains options based on 2FA status:
     - **If 2FA Enabled**: View 2FA Status, Reset 2FA, Disable 2FA
     - **If 2FA Disabled**: View 2FA Status, Enable 2FA
   - [ ] Dropdown closes when clicking outside
   - [ ] Shield icon shows different colors based on status (enabled: green/yellow, disabled: gray)

3. **View 2FA Status Action**
   - [ ] Opens modal/dialog showing:
     - User name and email
     - 2FA enabled status (Yes/No)
     - Current 2FA method (TOTP/SMS/Email/None)
     - Date 2FA was configured
     - Date of last successful 2FA authentication
     - Number of backup codes remaining (if applicable)
   - [ ] Modal has "Close" button
   - [ ] Non-destructive action (read-only)

4. **Enable 2FA Action** (for users without 2FA)
   - [ ] Shows modal: "Enable 2FA for [User Name]"
   - [ ] Modal allows admin to select 2FA method:
     - TOTP (Authenticator App) - Recommended
     - Email
     - SMS (if phone number configured)
   - [ ] Checkbox: "Require 2FA setup on next login"
   - [ ] On confirmation:
     - Sets TwoFactorEnabled = true
     - Sets selected TwoFactorMethod
     - Forces user to complete 2FA setup on next login
     - Shows success notification: "2FA enabled for [User Name]"
     - Logs action to audit trail
   - [ ] User receives email with instructions for setting up 2FA

5. **Reset 2FA Action** (for users with 2FA enabled)
   - [ ] Shows confirmation modal: "Are you sure you want to reset 2FA for [User Name]? This will remove their current 2FA setup and require them to set it up again."
   - [ ] Confirmation modal has "Reset" (danger) and "Cancel" buttons
   - [ ] On confirmation:
     - Removes user's 2FA configuration (clears TwoFactorSecret)
     - Forces user to re-configure 2FA on next login
     - Shows success notification: "2FA reset successfully for [User Name]"
     - Logs action to audit trail
   - [ ] User receives email notification of 2FA reset
   - [ ] Admin receives confirmation toast notification

6. **Disable 2FA Action** (for users with 2FA enabled)
   - [ ] Shows confirmation modal: "Are you sure you want to disable 2FA for [User Name]? This will make their account less secure."
   - [ ] Confirmation modal has "Disable" (danger) and "Cancel" buttons
   - [ ] On confirmation:
     - Disables 2FA for user
     - Allows user to log in without 2FA
     - Shows success notification: "2FA disabled for [User Name]"
     - Logs action to audit trail
   - [ ] User receives email notification of 2FA disable
   - [ ] Admin receives confirmation toast notification

7. **Add/Edit User Modal Enhancement**
   - [ ] Add "Two-Factor Authentication" section to user creation modal
   - [ ] Checkbox: "Enable 2FA for this user"
   - [ ] When enabled, show 2FA method dropdown:
     - TOTP (Authenticator App) - Recommended
     - Email
     - SMS (requires phone number)
   - [ ] Checkbox: "Require 2FA setup on first login"
   - [ ] When editing existing user, show current 2FA status
   - [ ] Role-based defaults:
     - Admin role: 2FA enabled by default (can be overridden)
     - Analyst role: 2FA optional but recommended
     - External role: 2FA optional

8. **User Table Enhancement**
   - [ ] Add "2FA Status" column to User Management table (optional, can be hidden on mobile)
   - [ ] Display icon or badge indicating 2FA enabled/disabled status
   - [ ] Column is sortable
   - [ ] Filter dropdown includes "2FA Enabled" and "2FA Disabled" options

7. **Audit Trail**
   - [ ] All 2FA management actions are logged with:
     - Admin user who performed action
     - Target user affected
     - Action performed (Reset, Disable, View)
     - Timestamp
     - IP address of admin
   - [ ] Audit logs visible in Admin Dashboard or dedicated Audit Log page

8. **Permissions & Security**
   - [ ] Only users with "Admin" role can access 2FA management features
   - [ ] "Analyst" and "External" roles cannot see 2FA dropdown
   - [ ] Admin cannot reset/disable their own 2FA (prevent lockout)
   - [ ] Anti-forgery tokens required for all 2FA actions
   - [ ] HTTPS required for all 2FA management endpoints

9. **Error Handling**
   - [ ] Graceful error messages if 2FA action fails
   - [ ] Network errors show retry option
   - [ ] User not found errors handled appropriately
   - [ ] Backend validation errors displayed to admin

10. **Notification System**
    - [ ] Success notifications use `AdminCore.showNotification()` with type='success'
    - [ ] Error notifications use `AdminCore.showNotification()` with type='error'
    - [ ] Warning confirmations use custom modal (no native `confirm()`)
    - [ ] Notifications auto-dismiss after 5 seconds

---

## Technical Requirements

### Frontend Components

#### 1. User Management Table Enhancement

**Location**: `Views/Admin/UserManagement.cshtml`

Add dropdown button to Actions column:

```html
<div class="admin-action-buttons">
    <button class="admin-action-btn admin-action-btn--edit"
            onclick="userManagement.editUser(${user.id})"
            title="Edit User">
        <i class="fas fa-edit"></i>
    </button>

    <!-- NEW: 2FA Dropdown -->
    ${user.twoFactorEnabled ? `
        <div class="dropdown-container" style="display: inline-block; position: relative;">
            <button class="admin-action-btn admin-action-btn--info"
                    onclick="userManagement.toggle2FADropdown(${user.id}); event.stopPropagation();"
                    title="2FA Management">
                <i class="fas fa-shield-alt"></i>
            </button>
            <div id="dropdown-${user.id}" class="dropdown-menu">
                <button onclick="userManagement.view2FAStatus(${user.id}); event.stopPropagation();">
                    <i class="fas fa-eye"></i> View 2FA Status
                </button>
                <button onclick="userManagement.reset2FA(${user.id}); event.stopPropagation();">
                    <i class="fas fa-redo"></i> Reset 2FA
                </button>
                <button onclick="userManagement.disable2FA(${user.id}); event.stopPropagation();"
                        class="danger">
                    <i class="fas fa-ban"></i> Disable 2FA
                </button>
            </div>
        </div>
    ` : ''}

    ${user.role !== 'Admin' ? `
        <button class="admin-action-btn admin-action-btn--delete"
                onclick="userManagement.deleteUser(${user.id})"
                title="Delete User">
            <i class="fas fa-trash-alt"></i>
        </button>
    ` : `
        <span class="admin-note" title="Admin users cannot be deleted">Protected</span>
    `}
</div>
```

#### 2. CSS Styling

**Location**: `wwwroot/css/admin/admin-core.css`

Add dropdown menu styles:

```css
/* 2FA Dropdown Styles */
.dropdown-container {
    position: relative;
    display: inline-block;
}

.dropdown-menu {
    display: none;
    position: absolute;
    top: 100%;
    right: 0;
    background: white;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    min-width: 180px;
    z-index: 1000;
    margin-top: 4px;
}

.dropdown-menu.show {
    display: block;
}

.dropdown-menu button {
    display: flex;
    align-items: center;
    width: 100%;
    padding: 10px 16px;
    background: none;
    border: none;
    text-align: left;
    cursor: pointer;
    font-size: 14px;
    color: #333;
    transition: background-color 0.2s;
}

.dropdown-menu button:hover {
    background-color: #f5f5f5;
}

.dropdown-menu button.danger {
    color: #FF5252;
}

.dropdown-menu button.danger:hover {
    background-color: #ffebee;
}

.dropdown-menu button i {
    margin-right: 8px;
    width: 16px;
}

.admin-action-btn--info {
    background-color: #2196F3;
    color: white;
}

.admin-action-btn--info:hover {
    background-color: #1976D2;
}
```

#### 3. JavaScript Updates

**Location**: `wwwroot/js/userManagement.js`

The following methods already exist (lines 712-838) but need to be connected to the UI:

- `toggle2FADropdown(userId)` - Already implemented ✅
- `view2FAStatus(userId)` - Already implemented ✅
- `reset2FA(userId)` - Already implemented ✅
- `disable2FA(userId)` - Already implemented ✅
- `closeDropdownOnOutsideClick(event)` - Already implemented ✅

**Required Updates**:

1. Update `view2FAStatus()` to show data in a proper modal instead of notification
2. Replace `confirm()` calls with custom modals (notification standards compliance)
3. Update `createUserRow()` to include 2FA status and dropdown

#### 4. Backend Updates

**Location**: `Controllers/AdminController.cs`

Endpoints already exist:
- `POST /Admin/Reset2FA` - Already implemented ✅
- `POST /Admin/Disable2FA` - Already implemented ✅

**Required Updates**:

1. Add `GET /Admin/Get2FAStatus/{userId}` endpoint for detailed 2FA status
2. Add email notification triggers for 2FA reset/disable actions
3. Add audit logging for all 2FA management actions

**Location**: `Services/AdminService.cs`

Required new methods:
- `Get2FAStatusAsync(int userId)` - Returns detailed 2FA status
- `Reset2FAAsync(int userId)` - Already exists, verify audit logging
- `Disable2FAAsync(int userId)` - Already exists, verify audit logging

**Location**: `Services/IAdminService.cs`

Add interface methods:
```csharp
Task<TwoFactorStatusViewModel> Get2FAStatusAsync(int userId);
Task<bool> Reset2FAAsync(int userId);
Task<bool> Disable2FAAsync(int userId);
```

#### 5. Data Models

**Location**: `Models/AdminModels.cs`

Add new view model:

```csharp
public class TwoFactorStatusViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorMethod { get; set; } // "TOTP", "SMS", "Email", null
    public DateTime? TwoFactorConfiguredAt { get; set; }
    public DateTime? LastTwoFactorAuth { get; set; }
    public int? BackupCodesRemaining { get; set; }
}
```

Update `UserViewModel` to include 2FA status:

```csharp
public class UserViewModel
{
    // ... existing properties ...
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorMethod { get; set; }
    public DateTime? TwoFactorConfiguredAt { get; set; }
}
```

---

## UI/UX Design

### 2FA Dropdown Menu (Actions Column)

```
┌─────────────────────────────┐
│ Actions Column              │
├─────────────────────────────┤
│ [Edit] [2FA ▼] [Delete]    │  ← When 2FA enabled
│                             │
│ Dropdown appears on click:  │
│ ┌─────────────────────────┐ │
│ │ 👁 View 2FA Status      │ │
│ │ 🔄 Reset 2FA            │ │
│ │ 🚫 Disable 2FA          │ │ ← Red text
│ └─────────────────────────┘ │
└─────────────────────────────┘
```

### 2FA Status Modal

```
┌───────────────────────────────────────┐
│ 2FA Status for John Doe          [X]  │
├───────────────────────────────────────┤
│                                       │
│ Email: john.doe@example.com           │
│                                       │
│ 2FA Status: ✅ Enabled                │
│ Method: TOTP (Authenticator App)      │
│ Configured: 2025-09-15 at 10:30 AM    │
│ Last Used: 2025-10-22 at 8:15 AM      │
│ Backup Codes: 8 remaining             │
│                                       │
├───────────────────────────────────────┤
│                    [Close]            │
└───────────────────────────────────────┘
```

### Reset 2FA Confirmation Modal

```
┌───────────────────────────────────────┐
│ Reset 2FA Confirmation           [X]  │
├───────────────────────────────────────┤
│                                       │
│ Are you sure you want to reset 2FA    │
│ for John Doe?                         │
│                                       │
│ This will:                            │
│ • Remove their current 2FA setup      │
│ • Require them to reconfigure 2FA     │
│ • Send them an email notification     │
│                                       │
│ This action is logged for security.   │
│                                       │
├───────────────────────────────────────┤
│       [Reset]          [Cancel]       │
│       (Red)            (Green)        │
└───────────────────────────────────────┘
```

---

## Testing Requirements

### Unit Tests

**Location**: `GCCWebApp.Tests/Services/AdminServiceTests.cs`

- [ ] `Reset2FAAsync_ValidUser_ReturnsTrue()`
- [ ] `Reset2FAAsync_InvalidUser_ReturnsFalse()`
- [ ] `Disable2FAAsync_ValidUser_ReturnsTrue()`
- [ ] `Disable2FAAsync_InvalidUser_ReturnsFalse()`
- [ ] `Get2FAStatusAsync_ValidUser_ReturnsStatus()`
- [ ] `Get2FAStatusAsync_InvalidUser_ReturnsNull()`

### Integration Tests

**Location**: `GCCWebApp.Tests/Controllers/AdminControllerTests.cs`

- [ ] `Reset2FA_ValidUser_ReturnsSuccess()`
- [ ] `Reset2FA_UnauthorizedUser_Returns403()`
- [ ] `Disable2FA_ValidUser_ReturnsSuccess()`
- [ ] `Disable2FA_UnauthorizedUser_Returns403()`

### E2E Tests

**Location**: `GCCWebApp.UAT.Tests/Tests/Admin/UserManagement2FATests.cs`

- [ ] Admin can view 2FA dropdown for users with 2FA enabled
- [ ] Admin can view detailed 2FA status
- [ ] Admin can reset user's 2FA successfully
- [ ] Admin can disable user's 2FA successfully
- [ ] Analyst cannot see 2FA dropdown (permission denied)
- [ ] Admin cannot reset their own 2FA
- [ ] User receives email notification after 2FA reset
- [ ] Audit log records 2FA management actions

### Manual Testing Checklist

- [ ] 2FA dropdown appears only for users with 2FA enabled
- [ ] Dropdown closes when clicking outside
- [ ] View 2FA Status shows accurate information
- [ ] Reset 2FA confirmation modal appears
- [ ] Reset 2FA removes user's 2FA configuration
- [ ] User is forced to reconfigure 2FA on next login after reset
- [ ] Disable 2FA allows user to log in without 2FA
- [ ] Success notifications appear after actions
- [ ] Error notifications appear if actions fail
- [ ] Only Admin role can access 2FA management
- [ ] Audit trail logs all 2FA actions
- [ ] Email notifications sent to affected users

---

## Security Considerations

1. **Authorization**
   - Only Admin role can perform 2FA management
   - Verify user permissions on backend
   - Anti-forgery tokens required

2. **Audit Logging**
   - Log all 2FA management actions
   - Include admin user, target user, action, timestamp, IP
   - Logs should be immutable and tamper-evident

3. **Notifications**
   - Email user when their 2FA is reset or disabled
   - Include timestamp and admin name in notification
   - Provide link to immediately reconfigure 2FA

4. **Self-Service Prevention**
   - Admins cannot reset/disable their own 2FA
   - Prevents accidental lockout scenarios

5. **Rate Limiting**
   - Limit 2FA management actions per admin per hour
   - Prevent abuse or brute force attempts

---

## Implementation Priority

**Priority**: HIGH
**Estimated Effort**: 2-3 days
**Dependencies**: None

### Phase 1: UI Implementation (Day 1)
- Add 2FA dropdown to User Management table
- Update `createUserRow()` to include 2FA button
- Add CSS styling for dropdown menu
- Replace `confirm()` with custom modals

### Phase 2: Backend Implementation (Day 2)
- Add `Get2FAStatusAsync()` to AdminService
- Update endpoints to return detailed 2FA status
- Add email notification triggers
- Add audit logging

### Phase 3: Testing (Day 3)
- Write unit tests for AdminService methods
- Write integration tests for AdminController
- Write E2E tests for 2FA management flows
- Manual testing and bug fixes

---

## Success Metrics

- **User Support Efficiency**: Reduce average 2FA lockout resolution time from 24 hours to <5 minutes
- **Admin Satisfaction**: 100% of admins report feature is "useful" or "very useful"
- **Security Compliance**: 100% of 2FA management actions logged to audit trail
- **User Satisfaction**: Reduce 2FA-related support tickets by 60%

---

## Rollout Plan

1. **Development Environment**: Test 2FA management with test users
2. **Staging Environment**: Validate with product owner/admin users
3. **Production Deployment**: Release during maintenance window
4. **Documentation**: Update admin training materials
5. **Monitoring**: Track 2FA management action frequency and success rate

---

## Appendix

### Related Documents
- `CLAUDE.md` - Project guidelines and standards
- `0_Documentation/NOTIFICATION_SYSTEM_AUDIT.md` - Notification standards
- `0_Documentation/MFA_SETUP_GUIDE.md` - MFA setup instructions for users
- `0_Documentation/MFA_TESTING_WITHOUT_EMAIL.md` - Testing MFA without email

### Backend API Reference

#### Get 2FA Status
```http
GET /Admin/Get2FAStatus/{userId}
Authorization: Admin role required
Response: TwoFactorStatusViewModel
```

#### Reset 2FA
```http
POST /Admin/Reset2FA
Content-Type: application/json
Body: { "userId": 123 }
Authorization: Admin role required
Response: { "success": true, "message": "2FA reset successfully" }
```

#### Disable 2FA
```http
POST /Admin/Disable2FA
Content-Type: application/json
Body: { "userId": 123 }
Authorization: Admin role required
Response: { "success": true, "message": "2FA disabled successfully" }
```

---

**Document Control**
- Created: October 22, 2025
- Last Updated: October 22, 2025
- Owner: Development Team
- Approved By: Product Owner (Pending)
