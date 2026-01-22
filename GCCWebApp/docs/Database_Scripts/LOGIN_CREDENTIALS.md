# Login Credentials - Database Authentication

## ✅ **Authentication Now Uses Database Only**

**IMPORTANT:** All authentication is now handled through the database. NO hardcoded credentials!

---

## 🔑 **Working Login Credentials**

After running the database rebuild, you can login with any of these accounts:

### **Primary Test Accounts:**

| **Email (Login)** | **Password** | **Role** | **Username** (also works) |
|-------------------|--------------|----------|---------------------------|
| **admin@gcc.com** | `TestPass123!` | Admin | admin |
| **analyst@gcc.com** | `TestPass123!` | Analyst | analyst |
| **external@company.com** | `TestPass123!` | External | external |
| **test@gcc.com** | `TestPass123!` | Public | testuser |

### **All 15 Test Users:**

All users have password: **`TestPass123!`**

| # | Email | Username | Role |
|---|-------|----------|------|
| 1 | admin@gcc.com | admin | Admin |
| 2 | analyst@gcc.com | analyst | Analyst |
| 3 | external@company.com | external | External |
| 4 | test@gcc.com | testuser | Public |
| 5 | sarah.johnson@gcc.com | sarah.johnson | Analyst |
| 6 | mike.chen@gcc.com | mike.chen | Analyst |
| 7 | lisa.rodriguez@assaabloy.com | lisa.rodriguez | External |
| 8 | david.kim@3m.com | david.kim | External |
| 9 | emma.wilson@interface.com | emma.wilson | External |
| 10 | alex.martinez@steelcase.com | alex.martinez | External |
| 11 | jennifer.lee@gcc.com | jennifer.lee | Analyst |
| 12 | robert.taylor@gcc.com | robert.taylor | Analyst |
| 13 | maria.garcia@mohawkind.com | maria.garcia | External |
| 14 | public@example.com | public.user | Public |
| 15 | partner@amazon.com | partner | External |

---

## 📝 **How to Login:**

1. Go to: https://gccwebapp.com/Account/Login
2. **Email field**: Enter email (e.g., `admin@gcc.com`) OR username (e.g., `admin`)
3. **Password field**: Enter `TestPass123!`
4. Click **Login**

### **Login Accepts Both:**
- ✅ Email: `admin@gcc.com`
- ✅ Username: `admin`

Both work! The login form requires email format (@) for HTML5 validation, so it's easier to use the email address.

---

## 🔧 **How Authentication Works:**

### **Database-Driven (NEW):**

1. User enters email/username and password
2. System queries `Users` table for matching Email OR Username
3. Verifies user is active (`IsActive=1`, `IsDeleted=0`, `Status='active'`)
4. Verifies password using BCrypt
5. Retrieves user's primary role from `Roles` table
6. Creates authentication cookie
7. Updates `LastLoginAt` timestamp

### **Files Changed:**

- `Controllers/AccountController.cs:651-698` - New ValidateUser method
  - Added ApplicationDbContext injection
  - Query database instead of hardcoded dictionary
  - Use BCrypt.Verify() for password validation
  - Include user roles in query
  - Check user status and active flags
  - Update last login timestamp

### **What Was Removed:**

❌ **NO MORE hardcoded credentials**
❌ **NO MORE test mode dictionary**
❌ **NO MORE fallback users**

All authentication goes through the database!

---

## 🔐 **Password Security:**

- **Hashing:** BCrypt with work factor 11
- **Hash Example:** `$2a$11$WJZ5XqF5KyC5Y8NyN8E9ouKbBZHBqBqPYYnJQXLgTYxNzC6gKFZ5m`
- **Verification:** `BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)`
- **Storage:** All password hashes stored in `Users.PasswordHash` column

---

## 🛠️ **Troubleshooting:**

### "Invalid email or password" Error

**Check 1: User exists in database**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT Username, Email, IsActive, Status FROM Users WHERE Email = 'admin@gcc.com'"
```
**Expected:** IsActive=1, Status='active'

**Check 2: Password hash exists**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT Username, LEN(PasswordHash) as HashLength FROM Users WHERE Email = 'admin@gcc.com'"
```
**Expected:** HashLength > 50

**Check 3: User has role assigned**
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -Q "SELECT u.Username, r.Name as RoleName FROM Users u LEFT JOIN RoleUser ru ON u.Id = ru.UserId LEFT JOIN Roles r ON ru.RoleId = r.Id WHERE u.Email = 'admin@gcc.com'"
```
**Expected:** RoleName='Admin'

---

## 📊 **Test the Changes:**

### **Quick Test:**
1. Navigate to https://gccwebapp.com/Account/Login
2. Enter:
   - Email: `admin@gcc.com`
   - Password: `TestPass123!`
3. Click Login
4. **Expected:** Redirect to Dashboard

### **Verify Roles:**
- **Admin** can access: `/Admin/UserManagement`, `/Admin/RoleManagement`
- **Analyst** can access: `/Certificate/Create`, `/Certificate/Edit`
- **External** can access: `/Certificate/CertificateTileViewExternal`
- **Public** can access: `/Certificate/CertificateTileView` (public view only)

---

## 🔄 **If Login Still Fails:**

### **Option 1: Rebuild Database**
```bash
cd "Database_Scripts"
REBUILD_COMPLETE_PRODUCTION.cmd
```
This will:
- Drop all tables
- Recreate schema
- Seed fresh data with correct password hashes

### **Option 2: Manual Password Reset**

If you need to reset a user's password to `TestPass123!`:

```sql
-- Generate hash for TestPass123!
-- Hash: $2a$11$WJZ5XqF5KyC5Y8NyN8E9ouKbBZHBqBqPYYnJQXLgTYxNzC6gKFZ5m

UPDATE Users
SET PasswordHash = '$2a$11$WJZ5XqF5KyC5Y8NyN8E9ouKbBZHBqBqPYYnJQXLgTYxNzC6gKFZ5m'
WHERE Email = 'admin@gcc.com';
```

---

## 📌 **Important Notes:**

1. **Case Insensitive:** Email/username matching is case-insensitive
2. **Either Works:** Can login with email OR username
3. **Password Exact:** Password is case-sensitive (`TestPass123!` not `testpass123!`)
4. **Active Users Only:** User must have `IsActive=1`, `IsDeleted=0`, `Status='active'`
5. **Role Required:** User must have at least one role assigned (defaults to 'Public' if none)

---

## 🎯 **Summary of Changes:**

| Before | After |
|--------|-------|
| ❌ Hardcoded credentials | ✅ Database-driven authentication |
| ❌ admin@greencirclecertified.com / Admin123! | ✅ admin@gcc.com / TestPass123! |
| ❌ Test mode dictionary | ✅ BCrypt password verification |
| ❌ Fallback users | ✅ Include user roles from database |
| ❌ No last login tracking | ✅ Updates LastLoginAt timestamp |

---

**Last Updated:** 2025-11-17
**Status:** ✅ Implemented and Tested
**Build:** ✅ Succeeded
