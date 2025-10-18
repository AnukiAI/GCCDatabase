# Multi-Factor Authentication (MFA) Setup Guide
## GCC Database Solution

**Document Version:** 1.0
**Last Updated:** October 16, 2025
**Target Audience:** System Administrators & End Users

---

## Table of Contents

1. [Overview](#overview)
2. [Administrator Setup](#administrator-setup)
3. [End User Setup - Microsoft Authenticator](#end-user-setup---microsoft-authenticator)
4. [End User Setup - Email-based MFA](#end-user-setup---email-based-mfa)
5. [End User Setup - SMS Text Message](#end-user-setup---sms-text-message)
6. [Managing Your MFA Settings](#managing-your-mfa-settings)
7. [Troubleshooting](#troubleshooting)
8. [Security Best Practices](#security-best-practices)
9. [FAQs](#faqs)

---

## Overview

Multi-Factor Authentication (MFA) adds an extra layer of security to your GCC Database account by requiring two forms of verification:
1. **Something you know:** Your password
2. **Something you have:** Your phone or email access

### Available MFA Methods

| Method | Security Level | Requirements | Works Offline |
|--------|---------------|--------------|---------------|
| Microsoft/Google Authenticator (TOTP) | **Highest** | Smartphone with authenticator app | Yes |
| Email Verification | **Medium** | Access to registered email | No |
| SMS Text Message | **Medium** | Mobile phone number | No |

### Recommended Method

**Microsoft Authenticator** or **Google Authenticator** is recommended because:
- Most secure (time-based codes refresh every 30 seconds)
- Works without internet connection
- Faster than waiting for email/SMS
- Industry standard authentication method

---

## Administrator Setup

### Step 1: Enable MFA System-Wide

**Location:** `appsettings.json` or `appsettings.Production.json`

1. **Open** the configuration file in a text editor

2. **Locate** the Authentication section:

```json
{
  "Authentication": {
    "EnableTestMode": false,
    "Enable2FA": true  // ← Set to true
  },
  "Features": {
    "TwoFactorAuthentication": true,  // ← Set to true
    "RequireEmailConfirmation": false,
    "AllowGuestAccess": true
  }
}
```

3. **Save** the file

4. **Restart** the application for changes to take effect

---

### Step 2: Configure Email Service (Required for Email MFA)

**Email MFA will not work without proper email configuration.**

1. **Verify** `IEmailService` is configured in `Program.cs`

2. **Configure** email settings in `appsettings.json`:

```json
{
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-specific-password",
    "FromEmail": "noreply@greencirclecertified.com",
    "FromName": "GCC Database System",
    "EnableSsl": true
  }
}
```

3. **Test** email sending:
   - Login as a test user
   - Enable email-based MFA
   - Verify code email is received

**Common Email Providers:**

**Gmail:**
- SMTP Server: `smtp.gmail.com`
- Port: `587` (TLS) or `465` (SSL)
- Requires App-Specific Password (not regular password)
- Enable "Less Secure Apps" or use OAuth2

**Microsoft 365/Outlook:**
- SMTP Server: `smtp.office365.com`
- Port: `587`
- Use your Microsoft 365 credentials

**SendGrid (Recommended for Production):**
- SMTP Server: `smtp.sendgrid.net`
- Port: `587`
- API Key as password
- More reliable than personal email accounts

---

### Step 3: Configure SMS Service (Optional)

**Only required if SMS MFA will be used.**

The system supports SMS through third-party providers like:

- **Twilio** (Recommended)
- **TextBelt**
- **Amazon SNS**

**Example Twilio Configuration:**

```json
{
  "Sms": {
    "Provider": "Twilio",
    "AccountSid": "your-account-sid",
    "AuthToken": "your-auth-token",
    "FromPhoneNumber": "+15551234567"
  }
}
```

**Note:** SMS configuration requires code implementation in `TwoFactorService.cs` if not already present.

---

### Step 4: Set MFA Policy (Optional)

**Require MFA for specific roles:**

```json
{
  "MfaPolicy": {
    "RequireForAdmins": true,
    "RequireForAnalysts": true,
    "RequireForExternal": false,
    "AllowEmailMfa": true,
    "AllowSmsMfa": true,
    "AllowTotpMfa": true,
    "GracePeriodDays": 30
  }
}
```

---

### Step 5: Notify Users

After enabling MFA, notify all users via:

**Email Template:**

```
Subject: Important: Multi-Factor Authentication Now Available

Dear GCC Database User,

We've enabled Multi-Factor Authentication (MFA) to enhance the security
of your account.

What This Means:
- You can now add an extra layer of security to your account
- MFA is currently OPTIONAL but highly recommended
- Setup takes less than 5 minutes

Recommended: Microsoft Authenticator
The most secure method is using Microsoft Authenticator or Google
Authenticator on your smartphone.

How to Set Up MFA:
1. Login to your account
2. Navigate to: http://localhost:5101/Account/TwoFactorPreferences
3. Follow the setup wizard

Need Help?
Contact support@greencirclecertified.com

Best regards,
GCC Database Team
```

---

## End User Setup - Microsoft Authenticator

### Prerequisites

**Install an Authenticator App on your smartphone:**

**For iPhone:**
- Microsoft Authenticator (App Store)
- Google Authenticator (App Store)
- Authy (App Store)

**For Android:**
- Microsoft Authenticator (Google Play)
- Google Authenticator (Google Play)
- Authy (Google Play)

---

### Step-by-Step Setup Instructions

#### Step 1: Login to Your Account

1. Open your web browser
2. Navigate to: **http://localhost:5101** (or your production URL)
3. Click **"Login"**
4. Enter your email and password
5. Click **"Sign In"**

---

#### Step 2: Access MFA Settings

1. After logging in, navigate to:
   **http://localhost:5101/Account/TwoFactorPreferences**

   **OR**

   Click your name (top right) → **"Account Settings"** → **"Two-Factor Authentication"**

2. You should see the **Two-Factor Authentication Settings** page

---

#### Step 3: Enable Microsoft Authenticator

1. **Locate** the "Microsoft Authenticator" section
   - It will have a green "Recommended" badge
   - You'll see benefits listed: "More Secure", "No Network Required", "Works Offline"

2. **Click** the green **"Setup Now"** button

3. You'll be redirected to the **Setup TOTP** page showing:
   - A **QR Code** (black and white square pattern)
   - A **Manual Entry Code** (letters and numbers like: `ABCD EFGH IJKL MNOP`)
   - Account information: Your email address

---

#### Step 4: Scan QR Code with Your Phone

**Using Microsoft Authenticator:**

1. **Open** Microsoft Authenticator app on your phone
2. **Tap** the **"+"** button (top right)
3. **Select** "Work or school account" OR "Other account"
4. **Tap** "Scan QR code"
5. **Point** your camera at the QR code on your computer screen
6. **Wait** for the app to scan (should take 1-2 seconds)
7. **Success!** You should see:
   - Account name: "GCC Database"
   - Email: Your email address
   - A 6-digit code that changes every 30 seconds

**Using Google Authenticator:**

1. **Open** Google Authenticator app
2. **Tap** the **"+"** button (bottom right)
3. **Tap** "Scan a QR code"
4. **Point** camera at QR code
5. **Success!** Account added

**Can't Scan the QR Code?**

If your camera won't scan, use **Manual Entry** instead:

1. In your authenticator app, choose **"Enter a setup key"** or **"Manual entry"**
2. **Enter** the following:
   - **Account name:** GCC Database
   - **Your email:** (your email address)
   - **Key:** (copy the code shown below the QR code)
   - **Time-based:** Yes (or select TOTP)
3. **Save**

---

#### Step 5: Verify Setup

1. **Look** at your authenticator app
2. **Find** the GCC Database entry
3. **Note** the 6-digit code (e.g., `482 916`)
   - This code changes every 30 seconds
   - A circular timer shows time remaining

4. **Return** to your computer browser
5. **Enter** the 6-digit code in the verification box
6. **Click** "Enable Authenticator"

7. **Success Message:** "Microsoft Authenticator has been successfully enabled for your account."

8. **Save your backup codes** (if provided)
   - These are one-time codes you can use if you lose your phone
   - Print or save them securely

---

#### Step 6: Test Your Setup

1. **Click** "Logout" (top right)
2. **Navigate** to login page
3. **Enter** your email and password
4. **Click** "Sign In"

**New Step - 2FA Verification:**

5. You'll be redirected to: **"Two-Factor Authentication"** page
6. **Look** at your phone's authenticator app
7. **Enter** the current 6-digit code
8. **Optional:** Check "Trust this device for 30 days" (if using personal computer)
9. **Click** "Verify Code"

10. **Success!** You're logged in securely with MFA

---

### What Happens Next

**Every time you login:**
1. Enter your password (something you know)
2. Enter code from authenticator app (something you have)
3. Both must be correct to access your account

**Benefits:**
- Even if someone steals your password, they can't login without your phone
- Your account is now protected against 99.9% of account takeovers
- Meets compliance and security best practices

---

## End User Setup - Email-based MFA

**Email-based MFA is automatically available as a backup method.**

### When Email MFA is Used

Email MFA is used when:
- Microsoft Authenticator is NOT enabled
- You select it as your preferred backup method
- You choose "Use Email Code" during login

---

### Step-by-Step Setup

#### Step 1: Access MFA Settings

1. **Login** to your account
2. **Navigate** to: http://localhost:5101/Account/TwoFactorPreferences

---

#### Step 2: Verify Email Settings

1. **Locate** the "Backup Methods" section
2. **Verify** your email address is correct
   - Shows your registered email: `yourname@company.com`
   - Shows verification status (green checkmark if verified)

3. **Select** "Email Verification" radio button
   - Description: "Receive 6-digit codes via email"

4. **Click** "Save Preferences"

5. **Success Message:** "Your 2FA preferences have been saved successfully."

---

#### Step 3: Test Email MFA

1. **Logout** of your account
2. **Login** again with email and password
3. You'll be redirected to **"Two-Factor Authentication"** page

4. **Check your email inbox** (the email registered with your account)
   - **From:** GCC Database System (noreply@greencirclecertified.com)
   - **Subject:** "Your GCC Database 2FA Code"
   - **Received:** Within 1 minute

5. **Open the email** and find your 6-digit code
   - Example: `842 916`
   - Code is prominently displayed in the center
   - Warning: Code expires in 5 minutes

6. **Return to browser** and enter the 6-digit code

7. **Click** "Verify Code"

8. **Success!** You're logged in

---

### Email MFA Code Format

**You'll receive an email like this:**

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔐 Two-Factor Authentication
GreenCircle Certified Database
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Your verification code is:

        ┌──────────────────┐
        │   8 4 2  9 1 6   │
        └──────────────────┘

⚠️ Important:
• This code will expire in 5 minutes
• Do not share this code with anyone
• If you didn't request this code, contact support

Enter this code on the verification page to complete your login.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
This is an automated message. Please do not reply.
```

---

### Didn't Receive the Email?

**If you don't receive the code within 2 minutes:**

1. **Check your spam/junk folder**
2. **Wait 60 seconds** (rate limiting prevents immediate resend)
3. **Click** "Resend code" on the verification page
4. **Check inbox again**

**Still not receiving emails?**
- Contact your IT administrator
- Email service may not be configured
- Your email address may be incorrect

---

## End User Setup - SMS Text Message

**SMS MFA is optional and requires phone number verification.**

### Prerequisites

- Mobile phone capable of receiving SMS
- SMS service must be configured by administrator
- Phone number must be verified before use

---

### Step-by-Step Setup

#### Step 1: Add Phone Number

1. **Login** to your account
2. **Navigate** to: http://localhost:5101/Account/TwoFactorPreferences

3. **Locate** "Phone Number" field under "Backup Methods"

4. **Enter** your mobile phone number:
   - Format: `+1 (555) 123-4567`
   - Include country code: `+1` for USA/Canada
   - Example: `+44 7700 900123` for UK

5. **Click** "Save Preferences"

---

#### Step 2: Verify Phone Number

1. **Check** for verification SMS on your phone
   - **From:** GCC Database (or short code like 12345)
   - **Message:** "Your GCC Database verification code is: 482916"

2. **Enter** the verification code on the verification page

3. **Click** "Verify Phone Number"

4. **Success:** Green checkmark appears next to phone number
   - Status changes from "Not verified" to "Verified"

---

#### Step 3: Select SMS as Backup Method

1. **Select** "SMS Text Message" radio button
   - Shows your phone number: `+1 (555) 123-4567`

2. **Click** "Save Preferences"

---

#### Step 4: Test SMS MFA

1. **Logout** and **login** again
2. On 2FA verification page, **click** "Send SMS Code"
3. **Check your phone** for SMS with 6-digit code
4. **Enter** the code
5. **Click** "Verify Code"
6. **Success!** Logged in via SMS

---

### SMS Code Format

**You'll receive a text message like this:**

```
GCC Database Security Code

Your verification code is: 482916

This code expires in 5 minutes.

Do not share this code with anyone.
```

---

## Managing Your MFA Settings

### Viewing Current MFA Status

**Navigate to:** http://localhost:5101/Account/TwoFactorPreferences

**You'll see:**

**If Microsoft Authenticator is Enabled:**
```
┌─────────────────────────────────────────────┐
│ ✓ Microsoft Authenticator Enabled          │
│ Your account is protected with time-based  │
│ authentication codes                        │
│ [ACTIVE]                                    │
└─────────────────────────────────────────────┘
```

**If Using Email-based MFA:**
```
┌─────────────────────────────────────────────┐
│ ✉ Email-based 2FA                           │
│ Verification codes sent to:                 │
│ yourname@company.com                        │
│ [ACTIVE]                                    │
└─────────────────────────────────────────────┘
```

---

### Switching Between MFA Methods

**To switch from Email to Microsoft Authenticator:**

1. Navigate to MFA Settings
2. Click **"Setup Now"** under Microsoft Authenticator
3. Scan QR code with your phone
4. Enter verification code
5. Microsoft Authenticator becomes primary method
6. Email becomes backup method

**To switch from Authenticator back to Email:**

1. Navigate to MFA Settings
2. Click **"Disable"** under Microsoft Authenticator
3. Confirm the action
4. Email MFA becomes primary method

---

### Disabling MFA

**⚠️ Warning: Disabling MFA reduces your account security**

**Steps:**

1. Navigate to: http://localhost:5101/Account/TwoFactorPreferences
2. If Microsoft Authenticator is enabled:
   - Click **"Disable"** button
   - Confirm when prompted
3. Your account will revert to email-based MFA only
4. To fully disable MFA, contact your administrator

**Note:** Some organizations require MFA for certain roles. If MFA is mandatory for your role, you cannot fully disable it.

---

### Updating Your Phone Number

1. Navigate to MFA Settings
2. Update phone number in the "Phone Number" field
3. Click "Save Preferences"
4. **Verify** the new number (SMS verification code will be sent)
5. Old phone number is removed after successful verification

---

### What If I Lose My Phone?

**If you lose access to your authenticator app:**

**Option 1: Use Backup Codes** (if you saved them)
1. On 2FA verification page, click "Use Backup Code"
2. Enter one of your saved backup codes
3. Login successfully
4. Immediately setup MFA on new device

**Option 2: Use Email Backup** (if configured)
1. On 2FA verification page, click "Use Email Code"
2. Check email for verification code
3. Login successfully
4. Setup MFA on new device

**Option 3: Contact Administrator**
1. Email: support@greencirclecertified.com
2. Provide proof of identity
3. Administrator can reset your MFA
4. You'll be required to setup MFA again

---

## Troubleshooting

### Common Issues and Solutions

---

### Issue 1: Authenticator Code Not Working

**Symptoms:**
- Entering correct code but getting "Invalid code" error
- Codes always expired

**Causes & Solutions:**

**1. Time Synchronization Issue**

Your phone's time must be accurate for TOTP codes to work.

**Fix:**
- **iPhone:** Settings → General → Date & Time → Enable "Set Automatically"
- **Android:** Settings → System → Date & Time → Enable "Automatic date & time"
- Wait 1-2 minutes after changing settings
- Try a new code

**2. Entering Old Code**

Codes change every 30 seconds. Make sure you're using the current code.

**Fix:**
- Wait for new code to appear
- Enter the code before the timer runs out
- Don't use a code that's about to expire

**3. Wrong Account in Authenticator**

You may have multiple accounts in your authenticator app.

**Fix:**
- Make sure you're looking at "GCC Database" account
- Check the email address matches your login email

---

### Issue 2: Not Receiving Email Codes

**Symptoms:**
- No email arrives after requesting code
- Email arrives after 5+ minutes (expired)

**Solutions:**

**1. Check Spam/Junk Folder**
- Email may be filtered as spam
- Add `noreply@greencirclecertified.com` to safe senders

**2. Email Address Incorrect**
- Verify email address in MFA settings
- Update if wrong

**3. Email Service Not Configured**
- Contact administrator
- They need to configure SMTP settings

**4. Rate Limiting**
- Can only request new code every 60 seconds
- Wait and try again

---

### Issue 3: SMS Not Arriving

**Symptoms:**
- No SMS received after requesting code

**Solutions:**

**1. Phone Number Format**
- Must include country code: `+1 5551234567`
- No spaces, dashes, or parentheses in some systems
- Try format: `+15551234567`

**2. Carrier Delays**
- SMS can take 1-5 minutes
- Try resending after 2 minutes

**3. Phone Number Not Verified**
- Must verify phone number before use
- Check for green checkmark next to number

**4. SMS Service Not Configured**
- Contact administrator
- SMS provider may not be set up

---

### Issue 4: QR Code Won't Scan

**Symptoms:**
- Camera won't focus on QR code
- Authenticator app doesn't recognize code

**Solutions:**

**1. Use Manual Entry Instead**
- Click "Can't scan?" or look for manual entry option
- Copy the code shown below QR code
- Paste into authenticator app

**2. Increase Brightness**
- Make your computer screen brighter
- QR code needs good contrast

**3. Adjust Distance**
- Move phone closer or farther from screen
- Try different angles

**4. Try Different Authenticator App**
- Google Authenticator
- Microsoft Authenticator
- Authy

---

### Issue 5: "Trust This Device" Not Working

**Symptoms:**
- Checked "Trust this device for 30 days"
- Still asked for code on next login

**Solutions:**

**1. Cookies Disabled**
- Enable cookies in browser
- Allow cookies from the GCC Database site

**2. Private/Incognito Mode**
- Don't use private browsing if you want device remembered
- Use normal browser window

**3. Cookie Cleared**
- If you clear browser cookies, device is untrusted again
- Need to verify again

**4. Different Browser/Device**
- "Trust device" is specific to browser AND device
- Must verify on each browser/device combination

---

### Issue 6: Account Locked After Failed Attempts

**Symptoms:**
- "Too many failed attempts" message
- Cannot request new code

**Solutions:**

**1. Wait for Lockout Period**
- Typically 15 minutes
- Lockout resets automatically

**2. Contact Administrator**
- Can manually unlock your account
- Email: support@greencirclecertified.com

**3. Use Different MFA Method**
- If authenticator codes locked, try email
- If email locked, try SMS (if configured)

---

### Issue 7: Lost Backup Codes

**Symptoms:**
- Lost phone, need backup codes
- Didn't save backup codes when generated

**Solutions:**

**1. Use Email MFA**
- Most accounts have email as backup
- Click "Use Email Code" on login page

**2. Contact Administrator**
- Can generate new backup codes
- Or reset your MFA entirely

**3. Regenerate Backup Codes**
- If you can still login
- Go to MFA Settings
- Generate new set of backup codes
- **Save them securely this time!**

---

### Issue 8: Can't Access MFA Settings Page

**Symptoms:**
- 403 Forbidden error
- "Access Denied" message

**Solutions:**

**1. Not Logged In**
- Must be authenticated to access MFA settings
- Login first

**2. Session Expired**
- Login again

**3. Direct URL Access**
- Try navigating from account menu instead
- Click your name → Account Settings → MFA

---

## Security Best Practices

### DO's ✓

**DO use Microsoft/Google Authenticator**
- Most secure method available
- Works offline
- Industry standard

**DO keep backup codes secure**
- Print and store in safe place
- Don't keep them on your phone
- Treat like passwords

**DO verify it's the official login page**
- Check URL: Should be https://[your-domain]
- Look for SSL/TLS lock icon
- Beware of phishing sites

**DO use "Trust Device" on personal computers only**
- Convenient for home/personal laptop
- Saves time on repeated logins

**DO immediately report lost phones**
- Contact administrator ASAP
- They can disable your MFA temporarily
- Setup MFA on new phone

**DO setup multiple MFA methods**
- Primary: Microsoft Authenticator
- Backup: Email or SMS
- Provides redundancy if one method fails

---

### DON'Ts ✗

**DON'T share your MFA codes**
- No one legitimate will ever ask for your code
- Support staff don't need your codes
- Codes are for your use only

**DON'T use same phone for work and personal authenticators**
- If possible, separate devices
- Reduces risk if phone compromised

**DON'T screenshot/photo your QR codes**
- Security risk if phone hacked/stolen
- Setup authenticator directly

**DON'T use "Trust Device" on public/shared computers**
- Library, hotel, internet cafe computers
- Shared work computers
- Always logout completely

**DON'T ignore security warnings**
- "Unrecognized login attempt" alerts
- "MFA disabled" notifications
- Change password if you see suspicious activity

**DON'T disable MFA unless required**
- Significantly reduces account security
- May violate company security policy

---

### If You Suspect Your Account is Compromised

**Immediate Actions:**

1. **Change your password immediately**
   - Use a strong, unique password
   - Don't reuse old passwords

2. **Check recent account activity**
   - Look for unrecognized logins
   - Check audit trail if available

3. **Reset MFA**
   - Disable and re-enable authenticator
   - Generate new backup codes
   - Verify phone number again

4. **Contact security team**
   - Email: security@greencirclecertified.com
   - Report suspicious activity
   - They can review logs

5. **Enable all available MFA methods**
   - Setup authenticator if not already
   - Add backup phone number
   - Verify email address

---

## FAQs

### General Questions

**Q: Is MFA required?**
A: It depends on your role and organization policy. Admins and Analysts may be required to use MFA. External partners and public users may have it optional. Check with your administrator.

**Q: Which MFA method should I use?**
A: Microsoft Authenticator or Google Authenticator (TOTP) is recommended as the most secure option. Email is convenient as a backup.

**Q: Can I use the same authenticator for multiple accounts?**
A: Yes! Your authenticator app can store multiple accounts (work, personal, other services). Each account shows separately in the app.

**Q: Do MFA codes work if I don't have internet?**
A: Authenticator app codes (TOTP) work offline. Email and SMS codes require internet/cellular connection.

**Q: How long do codes last?**
A:
- Authenticator codes: 30 seconds (time-based)
- Email codes: 5 minutes
- SMS codes: 5 minutes
- Backup codes: One-time use (don't expire)

---

### Technical Questions

**Q: What if my phone battery dies during login?**
A: Use email MFA as backup, or wait to charge your phone. Consider keeping backup codes in a secure location (not on your phone).

**Q: Can I use MFA on multiple devices?**
A: Yes, but you need to scan the QR code on each device during setup. You can have the same account in authenticator apps on multiple phones/tablets.

**Q: What happens if I upgrade to a new phone?**
A: You'll need to setup MFA again on the new phone. Options:
1. Transfer authenticator to new phone (some apps support this)
2. Disable MFA on old phone, re-enable on new phone
3. Use backup codes temporarily while setting up new phone

**Q: Is MFA the same as 2FA?**
A: Yes, they are the same thing. MFA (Multi-Factor Authentication) and 2FA (Two-Factor Authentication) both refer to using multiple verification methods.

**Q: Can administrators see my MFA codes?**
A: No. Your codes are generated based on a secret key that only you and the server know. Administrators cannot see or generate your codes.

---

### Account Access Questions

**Q: What if I lose access to email AND phone?**
A: Contact your administrator immediately. They can reset your MFA settings after verifying your identity. This is why backup codes are important!

**Q: Can someone login if they steal just my password?**
A: No, they would also need access to your phone/email to get MFA codes. This is why MFA is so effective.

**Q: What if I accidentally disabled MFA?**
A: You can re-enable it anytime by going to MFA Settings. If it's required for your role, you may be prompted to enable it on next login.

**Q: How do I know if someone else tried to access my account?**
A: Check for:
- Unexpected MFA code emails
- Login attempt notifications
- Audit trail (if available)
- Contact administrator if suspicious

---

### Troubleshooting Questions

**Q: Why do authenticator codes sometimes work and sometimes not?**
A: Usually due to time synchronization issues. Make sure your phone's time is set to automatic. Codes are very time-sensitive (30-second window).

**Q: Can I use the same code twice?**
A: No, each code is single-use. Even if you enter it wrong, you need to wait for a new code.

**Q: Why can't I click "Resend Code" immediately?**
A: Rate limiting prevents spam. You must wait 60 seconds between resend requests. This is a security feature.

**Q: What if QR code is blurry?**
A: Use the manual entry code instead. It's shown as letters and numbers below the QR code. Enter this into your authenticator app manually.

---

## Support Contact

**For MFA Setup Assistance:**
- Email: support@greencirclecertified.com
- Phone: [Support Phone Number]
- Hours: Monday-Friday, 8 AM - 6 PM EST

**For Security Concerns:**
- Email: security@greencirclecertified.com
- Immediate: [Emergency Contact]

**Administrator Contact:**
- Email: admin@greencirclecertified.com

---

## Additional Resources

**Download Authenticator Apps:**
- Microsoft Authenticator: [iOS](https://apps.apple.com/app/microsoft-authenticator/) | [Android](https://play.google.com/store/apps/details?id=com.azure.authenticator)
- Google Authenticator: [iOS](https://apps.apple.com/app/google-authenticator/) | [Android](https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2)
- Authy: [iOS](https://apps.apple.com/app/authy/) | [Android](https://play.google.com/store/apps/details?id=com.authy.authy)

**Learn More About MFA:**
- [NIST Guidelines on MFA](https://pages.nist.gov/800-63-3/)
- [Microsoft MFA Documentation](https://www.microsoft.com/security/business/identity-access/mfa-multi-factor-authentication)
- [Google 2FA Guide](https://www.google.com/landing/2step/)

---

**Document End**

*Last Updated: October 16, 2025*
*Version: 1.0*
*For GCC Database Solution*
