---
org: GreenCircle Certified Database
title: Accounts & Sign-In Guide
slug: GCC_Accounts_SignIn_Guide_v1.1
lede: How to create and manage user accounts, set up two-factor authentication, and reset a password.
version: 1.1
issued: September 2026
applies_to: Test environment
prepared_by: Anuki Consulting
audience: Sections 1–4 and 10 administrators · sections 5–9 all users
colophon: GreenCircle Certified Database — Accounts & Sign-In Guide, version 1.1. Prepared by Anuki Consulting, September 2026. Screenshots taken from the test environment; the live site may differ in minor visual detail. For anything not covered here, contact your system administrator.
---

> [!WARNING] Screenshots come from the test environment
> Every screenshot in this guide was taken from the current build on the test site. Email addresses in the user list have been deliberately blurred — they are test accounts and are not part of the instructions.

# 1. Roles, and what each one can do

Every account has exactly one role. The role decides which pages the person can open and which records they can see. Choose it when you create the account — it can be changed later.

| Role | Intended for | Can see |
|---|---|---|
| **Admin** | GreenCircle staff who manage the system | Everything, including Administration and User Management |
| **Analyst** | GreenCircle staff who work on certificates | All certificates and internal views; no Administration |
| **External** | A certified company's own staff | **Only their own company's** certificates. No Add or Edit certificate buttons, no internal tags |
| **Partner** | A partner organisation such as Amazon or Novi | **Only certificates associated with that partner** |
| **Public** | Rarely used — the public directory needs no account | The public certificate directory only |

> [!NOTE] The two “company” fields are not the same thing
> **User Company** is free text describing who the person works for — a label. **Company Access** is the list of companies whose data they are allowed to see — a permission. Filling in the first does not grant any access.

# 2. Creating a user account

Only Administrators can create accounts.

1. Sign in, then open **Administration → User Management**.
2. Click **Add New User**, at the top right of the list.

![User Management. The list shows each account's role, status, last sign-in and whether two-factor authentication is on.](04-user-management)

Complete the form. Fields marked with a red asterisk are required.

![The Add New User dialog before a role has been chosen.](05-add-user)

| Field | Required? | Notes |
|---|---|---|
| **Name** | Required | The person's full name, as it should appear in the user list |
| **Email** | Required | Must be unique — no two accounts may share an address. Password-reset links and verification codes go here, so it must be a mailbox the person can open |
| **Role** | Required | See section 1. Choosing External or Partner reveals extra fields — see section 3 |
| **Status** | Required | **Active** lets the person sign in; **Inactive** blocks them without deleting the account |
| **User Company** | Optional | Free text — their employer. A label only; it grants nothing |
| **Password** | Required | Minimum 8 characters |
| **Require password reset on next login** | Optional | Recommended. The person chooses their own password the first time they sign in, so the one you typed is never their long-term password |

1. Click **Save User**.
2. Tell the person their username and temporary password. **The system does not email new accounts** — creating an account sends nothing, so you must pass the details on yourself, through a channel other than email where possible.

> [!WARNING] If the email address is already in use
> Saving is refused and a message names the address. Each address may belong to only one account. Either use a different address, or change the other account first to free it up.

# 3. Giving access to companies or partners

External and Partner accounts see nothing until they are scoped. The relevant fields appear after you choose the role — so set the role first.

## External — choose the companies

A Company Access list appears. Tick every company whose certificates this person may see. Most external users need exactly one.

![Choosing the External role reveals the Company Access list. There is a select-all control, but grant only what the person needs.](09-add-user-external)

## Partner — choose the partner

A Partner dropdown appears. Select the one organisation this account represents.

![Choosing the Partner role reveals the Partner selector.](10-add-user-partner)

> [!CAUTION] Check this before you finish
> An External or Partner account with nothing selected can sign in and will see an **empty** list. That looks like a broken system to the person using it, and it is the most common mistake when setting these accounts up. Always confirm the scope is set before you hand over the credentials.

# 4. Managing an existing user

Each row in User Management has an Actions menu at the far right.

![The Actions menu. Options vary by account — a protected account shows “Protected User” in place of Delete.](08-actions-menu)

| Option | What it does |
|---|---|
| **Edit User** | Change name, email, role, status, company or partner access. You may also set a new password here — leave the password box blank to keep the current one |
| **View 2FA Status** | Shows whether two-factor authentication is on, and by which method |
| **Enable 2FA** | Turns two-factor authentication on. See the note below |
| **Disable 2FA** | Turns it off and removes any authenticator enrolment |
| **Reset 2FA** | Clears the existing authenticator enrolment so the person can set up a new device — use this when someone loses their phone |
| **Delete User** | Removes the account. Some accounts are protected and cannot be deleted |

> [!NOTE] What “Enable 2FA” actually does
> It switches two-factor authentication on and marks the account as intending to use an authenticator app — but it cannot set up the app for them. Until the person enrols their own device, **codes are emailed to them instead**, and the next time they sign in they are shown the setup page automatically. Nobody is locked out in the meantime.

<!-- pagebreak -->

# 5. Signing in

![The sign-in page. The first box accepts either a username or an email address.](01-login)

1. Enter your **username or email address** — either works.
2. Enter your password.
3. Select **Sign In**.
4. If two-factor authentication is on for your account, you are asked for a 6-digit code next.

# 6. Two-factor authentication

Two-factor authentication asks for a short code in addition to your password, so knowing the password alone is not enough to reach your account. Two methods are available.

| Method | How the code reaches you | Notes |
|---|---|---|
| **Email** | Sent to your registered address each time you sign in | Nothing to install. Codes expire after 5 minutes |
| **Authenticator app** | Generated on your phone, no email needed | More secure and works without a signal. Recommended |

Text-message (SMS) codes are **not** available.

![The verification step. The wording tells you which method is in use — here, a code sent by email, with the destination partly masked.](03-2fa-challenge)

> [!NOTE] Read the instruction on the page
> It names the method your account is actually using. If it says the code was emailed, a code from an authenticator app will not work — and vice versa.

> [!WARNING] If you have more than one code in your inbox
> Only the **most recent** code works. Requesting or triggering another one cancels the previous. If in doubt, choose Resend code and use the newest email.

Your own settings live under Two-Factor Preferences, where you can see the current method and start authenticator setup.

![The Two-Factor Preferences page.](06-2fa-preferences)

# 7. Setting up an authenticator app

If your administrator has turned on two-factor authentication for you, this page appears automatically the next time you sign in. You can also reach it from Two-Factor Preferences at any time.

![The authenticator setup page: scan the QR code, then confirm with a 6-digit code from the app.](07-setup-authenticator)

1. Install an authenticator app on your phone if you do not have one — Microsoft Authenticator and Google Authenticator both work.
2. **Remove any earlier “GCC Database” entry** from the app first. An old entry from a previous attempt will keep producing codes, and none of them will be accepted.
3. In the app, add an account and choose to scan a QR code.
4. Scan the code on screen. If the camera will not cooperate, use the **Manual Entry Code** shown beneath it.
5. Type the 6-digit code the app displays into **6-Digit Verification Code**, then select **Enable Authenticator**.
6. **Save your recovery codes.** They are shown once and never again.

> [!CAUTION] Recovery codes are your only way back in
> If you lose the phone and have no recovery codes, an administrator must clear your enrolment with Reset 2FA before you can sign in again. Store them somewhere other than the phone itself.

From the next sign-in onward the code comes from the app and no email is sent. If an email still arrives, the setup did not complete — return to this page and finish it.

# 8. Resetting a password

## If you have forgotten your password

![The forgotten-password page.](02-forgot-password)

1. On the sign-in page, select **Forgot your password?**
2. Enter your email address and submit.
3. Open the email titled **Password Reset - GCC Database** and select the link inside. **It expires after one hour.**
4. Choose a new password and confirm it.
5. Sign in with the new password.

> [!NOTE] The confirmation message is deliberately vague
> The page shows the same wording whether or not the address belongs to an account. That is intentional — it stops the form being used to discover which addresses are registered. It also means the confirmation is not proof that an email is on its way. If nothing arrives within a few minutes, **check your spam folder first**, then confirm the address is the one on your account.

## If an administrator is resetting it for you

An Administrator can set a password directly through Edit User, without email. They should tick “Require password reset on next login” so you choose your own password immediately afterwards.

# 9. Troubleshooting

| What you see | What it usually means |
|---|---|
| **“Invalid email or password”** — and you are sure the password is right | Most often the account is **temporarily locked**. Five failed attempts lock it for 15 minutes, and the message is the same as for a wrong password. **Wait 15 minutes and try once** — repeated attempts during the lock achieve nothing. Also check you are using your username or full email address, not a shortened version of either |
| **“Invalid or expired code”** | Either the code is older than its 5-minute life, a newer code has replaced it, or it came from the wrong place — an authenticator code when the page asked for an emailed one. Re-read the instruction on the page, then use Resend code |
| The setup page rejects your authenticator code | Usually an old “GCC Database” entry still in the app. Delete every GCC entry, reload the setup page, and scan the fresh QR code |
| No email arrives | Check spam first. If it is genuinely missing, contact your administrator — the address on the account may not be the one you are watching |
| An external user signs in but sees no certificates | Their **Company Access** is almost certainly empty. An administrator can add it through Edit User |
| Lost the phone with the authenticator | Use a recovery code if you saved one. Otherwise an administrator must use **Reset 2FA**, after which you can enrol a new device |
| **Access Denied** after signing in successfully | The account signed in, but the page belongs to a role it does not have. This is not a sign-in fault — check the account's role with an administrator |

# 10. Importing users in bulk

**User Management** can create many accounts at once from a CSV, which is worth using from about a dozen people upwards.

1. Click **Download Template** to get the empty file.
2. Fill in the columns: **Name**, **Email**, **Role**, **Status**, **User Company**.
3. Click **Import** and choose your file.
4. Read the result. Rows that could not be created are listed with the reason.

**User Company** accepts several company names separated by semicolons, or the single word `All`.

> [!IMPORTANT] Imported accounts have no password you can pass on
> There is deliberately **no password column** — a spreadsheet is not a safe place for one. Each imported account gets a long random password that is never displayed, and is marked as needing a reset. The only way in is the **Forgot password** link, so every imported person must be able to receive email at the address you entered.

If someone must be given access immediately and cannot wait for email, create that one account with **Add New User** instead and hand them the password directly.

> [!NOTE] Giving an existing user a known password
> Open the account in **Edit User**, type a new password, and save. Leaving the password field blank keeps the current one. Tick **Require password reset on next login** so they replace it straight away.

<!-- pagebreak -->

<!-- include: glossary.md -->

<!-- include: getting-help.md -->
