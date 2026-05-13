# User Management — Admin Training Guide

**Audience:** Admin users (role = Admin) who create and manage application user accounts.
**Page:** Admin → User Management

---

## TL;DR — How a new user gets a password

| How the user was created | Where the password comes from | What the admin tells the new user |
|---|---|---|
| **Add New User (UI)** | Admin types the password into the modal | The password the admin chose |
| **Bulk Import (CSV)** | Server auto-generates; never displayed | Use **Forgot Password** on the login page (requires email to be configured) |

> **Important — email not yet configured:** until outbound email is wired up, the Forgot Password flow does not work. Use **Add New User** (not Bulk Import) so you can hand the user a password directly.

---

## 1. Add New User (single user, UI)

Use this when adding one user at a time, especially before email is configured.

**Steps:**
1. Go to **Admin → User Management**.
2. Click **+ Add New User**.
3. Fill in:
   - **Name** — full name
   - **Email** — must be unique
   - **Role** — Admin, Analyst, External, or Partner
   - **Password** — minimum 8 characters. Type a password you can give to the user.
   - **Require Password Reset** — checked by default. Leave checked to force the user to set their own password on first login *(only useful once email works — otherwise uncheck so they can log in with the password you gave them)*.
   - **Company Access** — pick which companies the user can see (External users especially).
   - **Partner** — only for the Partner role.
4. Click **Save**.

**Hand-off to the user:**
- Username = email address
- Password = whatever you typed
- URL = the application login page

---

## 2. Bulk Import (CSV)

Use this only when email is configured, or when you're comfortable having every imported user wait until email is working before they can log in.

**Steps:**
1. Click **Download Template** to get the empty CSV.
2. Fill in the columns:
   - `Name, Email, Role, Status, User Company`
   - `User Company` accepts a semicolon-separated list of company names, or the literal word `All`.
   - **There is no Password column** — passwords are not set via CSV (intentionally; previous versions allowed plaintext passwords in spreadsheets, which was a security risk).
3. Click **Import** and pick the CSV.
4. Review the import result for any error rows.

**What happens behind the scenes:**
- Each imported user gets a cryptographically random 24-character password.
- **The password is never shown to the admin.**
- Every imported user has `Require Password Reset = true` forced.
- The only way the user can log in is the **Forgot Password** flow → which requires working email.

**If email is not configured yet:** imported users cannot log in. Either configure email first, or delete each imported user and recreate via **Add New User** with a known password.

---

## 3. Editing an Existing User

You can edit:
- Name, Email, Role
- Status (Active / Inactive)
- Company and Partner assignments
- 2FA settings

You **cannot** currently change a user's password from the Edit User modal — the password field in that modal is non-functional and being tracked as a defect. To change a password today, delete the user and recreate them via **Add New User**, or wait for the defect fix.

---

## 4. Common Scenarios

### "I need to give one external user access today"
→ **Add New User**, pick role **External**, type a password, uncheck Require Password Reset, give them the password verbally.

### "I have 30 users to onboard"
→ Wait for email to be configured. Then Bulk Import + everyone gets the Forgot Password email automatically.
→ Or, if you must do it now, Add New User one at a time so you can hand each person a password.

### "I imported a user by accident and they can't log in"
→ Delete the user from User Management → recreate via **Add New User** with a password you control.

### "I created a user but they typed the wrong password three times and got locked out"
→ Edit the user → toggle Status to Inactive → save → toggle back to Active → save. (Account lockout is per-session; setting inactive/active resets the failed-attempt counter.) If that doesn't help, delete and recreate.

---

## 5. Role Reference

| Role | Sees | Can Edit | Notes |
|---|---|---|---|
| Admin | Everything | Everything | Cannot delete the seed `admin` user or the `Admin` role |
| Analyst | All certs | Create/edit certs, run workflow | No admin pages |
| External | Only certs tied to their assigned company | Read-only | No internal tags, no Add/Edit cert buttons, no Actions column on Products |
| Partner | Their Partner API key only | API access only | Used for Amazon and other external partner integrations |

---

## 6. Field-by-Field Reference (Add New User modal)

| Field | Required | Notes |
|---|---|---|
| Name | Yes | Full display name |
| Email | Yes | Must be unique. Used as the login username. |
| Role | Yes | Determines permissions. Cannot be blank. |
| Password | No, but recommended | If blank, server generates a random one (admin never sees it — same as bulk import). If provided, minimum 8 characters. |
| Require Password Reset | Default: checked | Forces a password change on first login. **Uncheck if email isn't working yet** so the user can log in with the password you typed. |
| Company Access | Conditional | Required for External users. For Admin/Analyst, you can pick specific companies or leave broad. Not shown for Partner role. |
| Partner | Only for Partner role | Pick the Partner record this API user represents (e.g., Amazon). |
| 2FA Method | Optional | Only meaningful once 2FA is enabled tenant-wide. |

---

## 7. Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| "Failed to create user" with no detail | Duplicate email | Search User Management for the email; the user already exists. |
| Imported user can't log in | Email not configured, Forgot Password doesn't send | Delete + recreate via Add New User |
| Edit password field looks like it saved but the user still can't log in with the new password | Known defect — Edit modal password field is non-functional | Delete + recreate via Add New User |
| New external user sees no certificates | Company Access wasn't assigned | Edit user → assign companies → save |
| User keeps getting locked out | Failed login lockout | Toggle Status Inactive → Active to reset the counter |

---

## 8. Security Notes

- Passwords are hashed with BCrypt. Plaintext passwords are never stored.
- Admin actions (create, update, delete, role change) are logged to the audit history.
- Do not share passwords over email or chat in writing — communicate them verbally or via a password manager share link, then require a reset on first login once email works.
- The default seed accounts (`admin`, `analyst`, `external` with password `TestPass123!`) exist in dev and test environments only. They must be removed or re-passworded before any production launch.

---

*Last updated: 2026-05-12. Author: Anuki Consulting.*
