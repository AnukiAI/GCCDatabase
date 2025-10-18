# Microsoft Authenticator Quick Start Guide
## GCC Database - 5 Minute Setup

---

## What You'll Need

- [ ] Your smartphone (iPhone or Android)
- [ ] Internet connection (for initial setup only)
- [ ] Access to GCC Database account

---

## Step 1: Install Microsoft Authenticator (2 minutes)

### iPhone Users:
1. Open **App Store**
2. Search: **"Microsoft Authenticator"**
3. Tap **"Get"** → **"Install"**
4. Open the app

### Android Users:
1. Open **Google Play Store**
2. Search: **"Microsoft Authenticator"**
3. Tap **"Install"**
4. Open the app

**Alternative Apps (also work):**
- Google Authenticator
- Authy

---

## Step 2: Setup in GCC Database (2 minutes)

1. **Login** to GCC Database
   - Go to: http://localhost:5101
   - Enter email and password

2. **Navigate** to MFA Settings
   - Click your name (top right)
   - Click **"Two-Factor Authentication"**

   **OR** go directly to:
   - http://localhost:5101/Account/TwoFactorPreferences

3. **Click** the green **"Setup Now"** button
   - Under "Microsoft Authenticator" section

4. **Keep this page open** - you'll see a QR code

---

## Step 3: Scan QR Code (30 seconds)

**On Your Phone:**

1. Open **Microsoft Authenticator** app

2. Tap the **"+"** button (top right)

3. Choose **"Work or school account"** or **"Other account"**

4. Tap **"Scan QR code"**

5. **Point camera** at the QR code on your computer screen

6. **Wait** for beep/vibration (1-2 seconds)

7. **Done!** You'll see:
   ```
   GCC Database
   yourname@company.com
   482 916 ← (changes every 30 seconds)
   ```

---

## Step 4: Verify Setup (30 seconds)

**On Your Computer:**

1. Look at your phone - find the 6-digit code

2. Type the code in the verification box

3. Click **"Enable Authenticator"**

4. See success message: ✓ "Microsoft Authenticator enabled"

---

## Step 5: Test It! (1 minute)

1. **Logout** (top right)

2. **Login** again
   - Enter email and password
   - You'll see a NEW page: "Two-Factor Authentication"

3. **Look at phone** → Enter the current 6-digit code

4. Check **"Trust this device for 30 days"** (optional, for personal computer)

5. Click **"Verify Code"**

6. **Success!** You're in and protected

---

## Daily Use

### Every Login:

1. Enter password (as usual)
2. Look at phone → Enter 6-digit code from Microsoft Authenticator
3. Done!

**Tip:** The code changes every 30 seconds. Use the code before the circle completes.

---

## Troubleshooting (Quick Fixes)

### Code Not Working?

**Fix #1: Check Phone Time**
- Settings → Date & Time → Enable "Set Automatically"
- Wait 1 minute, try again

**Fix #2: Make Sure Code Hasn't Expired**
- Wait for new code to appear
- Enter code before circle timer runs out

**Fix #3: Check You're Using Correct Account**
- Look for "GCC Database" in authenticator app
- Verify email matches your login

### Lost Your Phone?

1. Click **"Use Email Code"** on login page
2. Check your email for 6-digit code
3. Login successfully
4. Setup authenticator on new phone ASAP

---

## Why This Matters

**Without MFA:**
- Stolen password = full account access
- Phishing works 100% of the time
- Password breaches expose your account

**With MFA:**
- Attacker needs password AND your phone
- 99.9% of account takeovers prevented
- Safe even if password is leaked

---

## Need Help?

**Support:** support@greencirclecertified.com
**Emergency:** [Your emergency contact]

---

## Advanced Tips

### Multiple Devices
- You can setup authenticator on tablet too
- Scan QR code on each device during setup

### Backup Codes
- Save the backup codes shown after setup
- Print or store securely (not on phone)
- Use if phone dies/lost

### Traveling?
- MFA codes work worldwide
- No internet needed for authenticator codes
- Works on airplane mode

---

**Setup Complete!** 🎉

Your account is now protected with Microsoft Authenticator.

*Estimated Total Time: 5 minutes*
*Security Improvement: 99.9%*
