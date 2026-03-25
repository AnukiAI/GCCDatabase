# GCC Database Solution - UAT Credentials Guide

## Production Database Credentials Strategy

The production database (https://gccwebapp.com) maintains **TWO sets of test accounts**:

### 1. UAT Test Credentials (For User Acceptance Testing)

**Purpose:** Client-facing UAT testing with realistic email addresses

| Role | Email | Password | Username |
|------|-------|----------|----------|
| Admin | admin@greencirclecertified.com | Admin123! | admin.uat |
| Analyst | analyst@greencirclecertified.com | Analyst123! | analyst.uat |
| External | partner@amazon.com | Partner123! | partner.amazon |

**When to use:**
- Client UAT testing sessions
- Stakeholder demonstrations
- Acceptance testing with realistic scenarios
- Training sessions

**Login method:** Use EMAIL address

### 2. Dev/System Test Credentials (For Internal Testing)

**Purpose:** Developer and QA testing with simplified credentials

| Role | Username | Password | Email |
|------|----------|----------|-------|
| Admin | admin | TestPass123! | swest@anukiconsulting.com |
| Analyst | analyst | TestPass123! | swest@anukiconsulting.com |
| External | external | TestPass123! | swest@anukiconsulting.com |
| Public | testuser | TestPass123! | swest@anukiconsulting.com |
| (+ 10 more test accounts) | various | TestPass123! | swest@anukiconsulting.com |

**When to use:**
- Developer testing
- Automated test scripts
- QA validation
- Bug reproduction
- Internal testing

**Login method:** Use USERNAME (not email)

### 3. External Visibility Test Credentials (For Company-Scoped Testing)

**Purpose:** Testing data isolation — each external user is associated with a different company/partner to verify they only see their own data.

**Setup script:** `docs/database/scripts/SETUP_EXTERNAL_TEST_USERS.sql` (idempotent, safe to re-run)

| Username | Password | Email | Company | Partner |
|----------|----------|-------|---------|---------|
| ext-amazon | TestPass123! | ext-amazon@test.com | ExtTest-AmazonCo | Amazon |
| ext-novi | TestPass123! | ext-novi@test.com | ExtTest-NoviCo | Novi |
| ext-nopartner | TestPass123! | ext-nopartner@test.com | ExtTest-NoPartnerCo | None |
| ext-none | TestPass123! | ext-none@test.com | *(no company)* | None |

**Login method:** Use USERNAME (not email)

**When to use:**
- Verifying external users only see their own company's certificates
- Testing partner-specific filtering (Amazon vs Novi vs no partner)
- Confirming users with no company association see zero data
- Validating internal tags are hidden from external users

**Expected test results:**

| Username | Active Certs Visible | Draft/Expired Visible | Cert Numbers |
|----------|---------------------|----------------------|--------------|
| ext-amazon | 3 | No (1 draft hidden) | EXT-AMZ-001, 002, 003 |
| ext-novi | 2 | No (1 expired hidden) | EXT-NOVI-001, 002 |
| ext-nopartner | 2 | No | EXT-NP-001, 002 |
| ext-none | 0 | No | *(none — no company)* |

## Implementation

### Adding UAT Users to Production

Run this SQL script on production database:
```bash
sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i "Scripts\ADD_UAT_USERS_TO_PRODUCTION.sql"
```

### BCrypt Hashes (for reference)

**UAT Passwords:**
- Admin123!: `$2a$11$UoNvHJFDMjtDHPmZwTJAuuzkVbJ6IcAklsAwRwHFQsagn1lF4r6zO`
- Analyst123!: `$2a$11$Q.npkkcCJhA3NPmHfDhlAu3pXfQ/GQfdMSWnz9Jv5DKmDUD2XD6vO`
- Partner123!: `$2a$11$1PXM1s0bRhhOPzEf8.sArOhTiVL0ugcDhq3/erNGR2IOligjM79SW`

**Dev/Test Password:**
- TestPass123!: `$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa`

## Login Instructions

### UAT Login (Client Testing)
1. Go to: https://gccwebapp.com/Account/Login
2. Enter **EMAIL**: admin@greencirclecertified.com
3. Enter **PASSWORD**: Admin123!
4. Click Login

### Dev Login (Internal Testing)
1. Go to: https://gccwebapp.com/Account/Login
2. Enter **USERNAME**: admin (NOT email)
3. Enter **PASSWORD**: TestPass123!
4. Click Login

## Account Management

### Adding More UAT Accounts

Use the HashGenerator tool to create new password hashes:
```bash
cd Scripts/HashGenerator
dotnet run "YourPassword123!"
```

Then insert into database following the pattern in `ADD_UAT_USERS_TO_PRODUCTION.sql`

### Resetting Passwords

**Reset UAT accounts:**
- Re-run `ADD_UAT_USERS_TO_PRODUCTION.sql` (script handles updates)

**Reset all dev/test accounts:**
- Run `RESET_PRODUCTION_PASSWORDS.sql`

## Security Notes

1. **UAT credentials** use realistic email domains for client demonstrations
2. **Dev credentials** use simplified usernames for quick testing
3. Both sets can coexist in production without conflict
4. All test accounts should be clearly marked and documented
5. Consider implementing account expiration for UAT accounts after testing phases

## Troubleshooting

**UAT account not working?**
- Verify you're using the EMAIL address (not username)
- Check account is active: `SELECT * FROM Users WHERE Email = 'admin@greencirclecertified.com'`
- Verify role assignment: Check `UserRoles` table

**Dev account not working?**
- Verify you're using the USERNAME (not email)
- Password must be exactly: `TestPass123!` (case-sensitive)
- See: `Scripts/FIX_PRODUCTION_LOGIN.md` for full troubleshooting

## Files

- `ADD_UAT_USERS_TO_PRODUCTION.sql` - Adds UAT users to production
- `RESET_PRODUCTION_PASSWORDS.sql` - Resets all dev/test accounts
- `HashGenerator/` - Tool to generate BCrypt hashes
- `FIX_PRODUCTION_LOGIN.md` - Login troubleshooting guide
- `UAT_CREDENTIALS_GUIDE.md` - This document
