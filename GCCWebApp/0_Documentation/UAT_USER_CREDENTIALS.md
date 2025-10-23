# GCC Database Solution - UAT User Credentials

```
═══════════════════════════════════════════════════════════════════════
              GCC DATABASE SOLUTION - USER ACCEPTANCE TESTING
                         TEST USER CREDENTIALS
═══════════════════════════════════════════════════════════════════════

Project:       GCC Database Solution v1.0
Environment:   UAT Testing Environment
Document Date: October 22, 2025
Classification: CONFIDENTIAL - For UAT Team Use Only

═══════════════════════════════════════════════════════════════════════
```

---

## IMPORTANT: Security Notice

**CONFIDENTIALITY:**
- This document contains sensitive test credentials
- Do NOT share these credentials outside the UAT team
- Do NOT use these credentials in production environments
- Do NOT commit this document to public repositories

**AFTER UAT:**
- All test account passwords will be reset
- Test accounts may be disabled or deleted
- API keys will be revoked

**SUPPORT:**
- For login issues, contact UAT Coordinator
- For password reset, contact IT Support
- For access questions, contact QA Lead

---

## UAT Environment Access

### Application URL

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  UAT Application URL:                                       │
│  http://localhost:5101                                      │
│                                                             │
│  OR (if staging environment):                               │
│  https://uat.greencirclecertified.com                       │
│                                                             │
│  Login Page:                                                │
│  http://localhost:5101/Account/Login                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Browser Requirements

**Supported Browsers:**
- Google Chrome 90+ (Recommended)
- Mozilla Firefox 88+
- Microsoft Edge 90+
- Safari 14+

**Screen Resolution:**
- Minimum: 1280x720
- Recommended: 1920x1080

**Network Requirements:**
- Stable internet connection
- VPN access (if required): [VPN details]

---

## Test User Accounts

### 1. Admin User Account

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃                        ADMIN ACCOUNT                        ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃                                                             ┃
┃  Account Type:    Administrator                             ┃
┃  Full Access:     YES - All features and admin pages        ┃
┃                                                             ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃  EMAIL:           admin@greencirclecertified.com            ┃
┃  PASSWORD:        Admin123!                                 ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃                                                             ┃
┃  Permissions:                                               ┃
┃  ✓ Create, Read, Update, Delete Certificates               ┃
┃  ✓ User Management (Create, Edit, Delete Users)            ┃
┃  ✓ Role Management (Create, Edit, Delete Roles)            ┃
┃  ✓ Tag Management                                           ┃
┃  ✓ Product Management                                       ┃
┃  ✓ Company Management                                       ┃
┃  ✓ Certificate Type Management                              ┃
┃  ✓ Partner Management                                       ┃
┃  ✓ Export Data (CSV, Excel)                                 ┃
┃  ✓ Dashboard Access                                         ┃
┃  ✓ API Access                                               ┃
┃                                                             ┃
┃  Use for Testing:                                           ┃
┃  • Admin functionality (User Management, Role Management)   ┃
┃  • Certificate deletion (Admin only)                        ┃
┃  • System configuration                                     ┃
┃  • Full CRUD operations                                     ┃
┃                                                             ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

**Quick Copy:**
```
Email:    admin@greencirclecertified.com
Password: Admin123!
```

---

### 2. Analyst User Account

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃                       ANALYST ACCOUNT                       ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃                                                             ┃
┃  Account Type:    Analyst                                   ┃
┃  Full Access:     NO - Certificate management only          ┃
┃                                                             ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃  EMAIL:           analyst@greencirclecertified.com          ┃
┃  PASSWORD:        Analyst123!                               ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃                                                             ┃
┃  Permissions:                                               ┃
┃  ✓ Create, Read, Update Certificates                        ┃
┃  ✗ Delete Certificates (Admin only)                         ┃
┃  ✓ Product ID Management (ASIN, UPC, etc.)                  ┃
┃  ✓ Export Data (CSV, Excel)                                 ┃
┃  ✓ Dashboard Access                                         ┃
┃  ✗ User Management (No access)                              ┃
┃  ✗ Role Management (No access)                              ┃
┃  ✗ Admin Pages (No access)                                  ┃
┃                                                             ┃
┃  Use for Testing:                                           ┃
┃  • Certificate creation and editing                         ┃
┃  • Product ID management (bulk import/export)               ┃
┃  • Search and filtering                                     ┃
┃  • Certificate export                                       ┃
┃  • Dashboard features                                       ┃
┃  • Verify NO access to admin pages (403 Forbidden)          ┃
┃                                                             ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

**Quick Copy:**
```
Email:    analyst@greencirclecertified.com
Password: Analyst123!
```

---

### 3. External Partner Account (Amazon)

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃                  EXTERNAL PARTNER ACCOUNT                   ┃
┃                      (Amazon Partner)                        ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃                                                             ┃
┃  Account Type:    External (Partner)                        ┃
┃  Full Access:     NO - Read-only, partner-filtered          ┃
┃  Partner:         Amazon                                    ┃
┃                                                             ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃  EMAIL:           partner@amazon.com                        ┃
┃  PASSWORD:        Partner123!                               ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃                                                             ┃
┃  Permissions:                                               ┃
┃  ✓ Read Certificates (Amazon-associated only)               ┃
┃  ✗ Create Certificates (No access)                          ┃
┃  ✗ Edit Certificates (No access)                            ┃
┃  ✗ Delete Certificates (No access)                          ┃
┃  ✓ Export Certificates (Amazon-associated only)             ┃
┃  ✓ Download Certificate PDFs                                ┃
┃  ✗ Dashboard (No access)                                    ┃
┃  ✗ Admin Pages (No access)                                  ┃
┃                                                             ┃
┃  Partner Filtering:                                         ┃
┃  • Only sees certificates with Partner = "Amazon"           ┃
┃  • Cannot see Wayfair or other partner certificates         ┃
┃  • API access filtered to Amazon certificates only          ┃
┃                                                             ┃
┃  Use for Testing:                                           ┃
┃  • External partner certificate viewing                     ┃
┃  • Partner-specific filtering                               ┃
┃  • Read-only access verification                            ┃
┃  • Verify cannot create/edit/delete                         ┃
┃  • Export functionality (partner-filtered)                  ┃
┃                                                             ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

**Quick Copy:**
```
Email:    partner@amazon.com
Password: Partner123!
```

---

### 4. Public User (Unauthenticated Access)

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃                      PUBLIC USER ACCESS                     ┃
┃                     (No Login Required)                      ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃                                                             ┃
┃  Account Type:    Public (Unauthenticated)                  ┃
┃  Full Access:     NO - Public view only                     ┃
┃                                                             ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃  LOGIN REQUIRED:  NO                                        ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃                                                             ┃
┃  Public URL:                                                ┃
┃  http://localhost:5101/Certificate/CertificateTileView      ┃
┃                                                             ┃
┃  Permissions:                                               ┃
┃  ✓ View Active Certificates (Tile View)                     ┃
┃  ✓ Search Certificates                                      ┃
┃  ✓ Filter Certificates (by company, type, status)           ┃
┃  ✓ View Certificate Details                                 ┃
┃  ✓ Download Certificate PDFs                                ┃
┃  ✗ Create Certificates (Login required)                     ┃
┃  ✗ Edit Certificates (Login required)                       ┃
┃  ✗ Delete Certificates (Login required)                     ┃
┃  ✗ Dashboard (Login required)                               ┃
┃  ✗ Admin Pages (Login required)                             ┃
┃                                                             ┃
┃  Use for Testing:                                           ┃
┃  • Public certificate viewing                               ┃
┃  • Search and filter functionality                          ┃
┃  • Infinite scroll                                          ┃
┃  • Shareable URLs                                           ┃
┃  • Certificate details page                                 ┃
┃  • PDF downloads                                            ┃
┃  • Verify login required for protected actions              ┃
┃                                                             ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

**Access:**
```
No login required - just navigate to:
http://localhost:5101/Certificate/CertificateTileView
```

---

## API Access Credentials

### API Key for Testing

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃                     API ACCESS CREDENTIALS                  ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃                                                             ┃
┃  API Key Type:     Amazon Partner API Key                   ┃
┃  Rate Limit:       1000 requests per hour                   ┃
┃  Partner Filter:   Amazon (only Amazon certificates)        ┃
┃                                                             ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃  X-API-Key:                                                 ┃
┃  gcc_test_amazon_partner_key_2025_UAT                       ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃                                                             ┃
┃  API Endpoint:                                              ┃
┃  http://localhost:5101/api/CertificateTileExternal/tiles    ┃
┃                                                             ┃
┃  Example Usage (curl):                                      ┃
┃  curl -H "X-API-Key: gcc_test_amazon_partner_key_2025_UAT" \┃
┃       http://localhost:5101/api/CertificateTileExternal/tiles┃
┃                                                             ┃
┃  Example Usage (Postman):                                   ┃
┃  1. Create new GET request                                  ┃
┃  2. URL: http://localhost:5101/api/CertificateTileExternal/tiles┃
┃  3. Headers:                                                ┃
┃     Key: X-API-Key                                          ┃
┃     Value: gcc_test_amazon_partner_key_2025_UAT             ┃
┃  4. Send request                                            ┃
┃                                                             ┃
┃  Expected Response:                                         ┃
┃  • Status: 200 OK                                           ┃
┃  • Body: JSON array of Amazon-associated certificates       ┃
┃                                                             ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

**Quick Copy:**
```
API Key: gcc_test_amazon_partner_key_2025_UAT
Endpoint: http://localhost:5101/api/CertificateTileExternal/tiles
Header: X-API-Key: gcc_test_amazon_partner_key_2025_UAT
```

---

### Generic Test API Key

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃                  GENERIC TEST API KEY                       ┃
┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫
┃                                                             ┃
┃  API Key Type:     Generic Test Key (No partner filter)     ┃
┃  Rate Limit:       1000 requests per hour                   ┃
┃  Partner Filter:   None (all certificates visible)          ┃
┃                                                             ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃  X-API-Key:                                                 ┃
┃  gcc_test_generic_key_2025_UAT                              ┃
┃  ─────────────────────────────────────────────────────────  ┃
┃                                                             ┃
┃  API Endpoint:                                              ┃
┃  http://localhost:5101/api/v1/certificates                  ┃
┃                                                             ┃
┃  Use for Testing:                                           ┃
┃  • API authentication                                       ┃
┃  • Rate limiting                                            ┃
┃  • General API functionality                                ┃
┃                                                             ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

**Quick Copy:**
```
API Key: gcc_test_generic_key_2025_UAT
Endpoint: http://localhost:5101/api/v1/certificates
Header: X-API-Key: gcc_test_generic_key_2025_UAT
```

---

## User Account Summary Table

| Role | Email | Password | Access Level | Can Create | Can Edit | Can Delete | Admin Access |
|------|-------|----------|--------------|------------|----------|------------|--------------|
| **Admin** | admin@greencirclecertified.com | Admin123! | Full | YES | YES | YES | YES |
| **Analyst** | analyst@greencirclecertified.com | Analyst123! | Certificate Mgmt | YES | YES | NO | NO |
| **External** | partner@amazon.com | Partner123! | Read-Only | NO | NO | NO | NO |
| **Public** | (No login) | (No login) | View-Only | NO | NO | NO | NO |

---

## Test Scenario to User Account Mapping

### Recommended Account Assignments

**Team A: Admin & Internal Users**
- Use: **Admin account** (admin@greencirclecertified.com)
- Test Scenarios: 3.1 - 3.6 (Admin scenarios)

**Team B: Analyst Users**
- Use: **Analyst account** (analyst@greencirclecertified.com)
- Test Scenarios: 2.1 - 2.6 (Analyst scenarios)

**Team C: External Users & Public**
- Use: **External account** (partner@amazon.com)
- Test Scenarios: 4.1 - 4.3 (External scenarios)
- Use: **No login** (Public access)
- Test Scenarios: 1.1 - 1.5 (Public scenarios)

**Team D: API & Performance**
- Use: **API Keys** (see above)
- Test Scenarios: 5.1 - 5.3 (API scenarios)

---

## Common Login Issues & Troubleshooting

### Issue 1: "Invalid email or password"

**Possible Causes:**
- Incorrect password (case-sensitive)
- Caps Lock is ON
- Extra spaces in email or password
- Copy-paste formatting issues

**Solution:**
- Manually type credentials (don't copy-paste)
- Verify Caps Lock is OFF
- Double-check spelling

---

### Issue 2: "Access Denied" or "403 Forbidden"

**Possible Causes:**
- Trying to access admin pages with non-admin account
- Analyst trying to delete certificates (admin only)
- External user trying to edit certificates

**Solution:**
- Use correct account for test scenario
- Refer to user account permissions table above

---

### Issue 3: "Session Expired" or "Please log in again"

**Possible Causes:**
- Session timeout (30 minutes of inactivity)
- Browser cookies cleared
- Multiple tabs with different users

**Solution:**
- Log in again
- Avoid clearing browser cookies during UAT
- Use incognito/private mode for testing multiple users

---

### Issue 4: API returns "401 Unauthorized"

**Possible Causes:**
- Missing X-API-Key header
- Incorrect API key value
- Expired API key

**Solution:**
- Verify X-API-Key header is included
- Copy API key exactly (case-sensitive)
- Contact UAT Coordinator if key is expired

---

## Password Reset Procedure

**If you need to reset a test account password:**

1. Contact UAT Coordinator
2. Provide your name and the account email
3. New temporary password will be provided
4. Update this document (if necessary)

**Emergency Password Reset:**
- Contact: [UAT Coordinator Email/Phone]
- Response Time: Within 1 hour during UAT hours

---

## Security Best Practices

### During UAT

- [ ] Do NOT share credentials via unsecured channels (e.g., public Slack, unencrypted email)
- [ ] Use encrypted file sharing (e.g., password-protected documents)
- [ ] Log out after completing test scenarios
- [ ] Clear browser cache/cookies after UAT (if using personal device)
- [ ] Report any suspicious activity to UAT Coordinator

### After UAT

- [ ] All test account passwords will be reset
- [ ] API keys will be revoked or regenerated
- [ ] This document will be archived securely
- [ ] Production accounts will use different credentials

---

## Additional Test Data

### Sample Certificate Numbers for Testing

**Active Certificates:**
- 24-0366 (ASSA ABLOY - Door Hardware)
- 24-0367 (Wayfair - Furniture)
- 24-0368 (Amazon - Electronics)

**Expired Certificates:**
- 23-1234 (Expired - for status testing)

**Draft Certificates:**
- DRAFT-001 (Draft status - for workflow testing)

### Sample Companies

- ASSA ABLOY
- Wayfair
- Amazon
- Home Depot
- Lowe's

### Sample Products

- Door Hardware
- Furniture
- Electronics
- Building Materials
- Home Goods

---

## Support Contacts

### UAT Coordinator

**Name:** [To be assigned]
**Email:** [Email]
**Phone:** [Phone]
**Teams/Slack:** [Username]

**Availability:**
- Monday - Friday: 9:00 AM - 5:00 PM
- Emergency: [Emergency contact for P0 defects]

---

### Technical Support

**Name:** [To be assigned]
**Email:** [Email]
**Phone:** [Phone]

**Availability:**
- Monday - Friday: 9:00 AM - 5:00 PM

---

### QA Lead

**Name:** [To be assigned]
**Email:** [Email]
**Phone:** [Phone]

**Availability:**
- Monday - Friday: 9:00 AM - 5:00 PM

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | October 22, 2025 | [Author] | Initial creation for UAT |

---

## Appendix: Quick Reference Card

**Print this section and keep it handy during UAT:**

```
┌───────────────────────────────────────────────────────────┐
│         GCC DATABASE SOLUTION - UAT QUICK REFERENCE       │
├───────────────────────────────────────────────────────────┤
│                                                           │
│  URL: http://localhost:5101                               │
│                                                           │
│  ADMIN:                                                   │
│  admin@greencirclecertified.com / Admin123!               │
│                                                           │
│  ANALYST:                                                 │
│  analyst@greencirclecertified.com / Analyst123!           │
│                                                           │
│  EXTERNAL:                                                │
│  partner@amazon.com / Partner123!                         │
│                                                           │
│  PUBLIC:                                                  │
│  No login required                                        │
│                                                           │
│  API KEY (Amazon):                                        │
│  gcc_test_amazon_partner_key_2025_UAT                     │
│                                                           │
│  SUPPORT:                                                 │
│  UAT Coordinator: [Phone/Email]                           │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

---

**END OF UAT USER CREDENTIALS DOCUMENT**

```
═══════════════════════════════════════════════════════════════════════
                       CONFIDENTIAL - FOR UAT TEAM USE ONLY
                          DO NOT DISTRIBUTE PUBLICLY
═══════════════════════════════════════════════════════════════════════
```
