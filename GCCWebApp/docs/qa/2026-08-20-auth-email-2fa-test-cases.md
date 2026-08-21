# Manual Test Cases — Email, Password Reset, Two-Factor, Role Scoping

> **Environment:** TEST — `https://gccwebapp.com` (behind the access gate).
> **Covers:** DEF-263 (email delivery), DEF-365 (per-user 2FA + authenticator enrolment),
> DEF-366 (duplicate-email message), DEF-367 (2FA challenge names the factor).
> **Written:** 2026-08-20. Author: Anuki Consulting.
>
> Every case below is written so it can **fail**. Where a step exists only to make the result
> falsifiable — checking a "before" state, or checking that something is *absent* — that is called
> out, because those steps are the ones most often skipped and they are the ones that carry the
> proof.

---

## Accounts

All test accounts use the password **`GccTest2026!`** unless stated. Sign-in accepts **either** the
username or the email.

| Username | Email | Role | Scoped to | 2FA |
|---|---|---|---|---|
| `test.admin` | shawnmwest+admin@gmail.com | Admin | — | off |
| `test.analyst` | shawnmwest+analyst@gmail.com | Analyst | — | off |
| `test.external` | shawnmwest+external@gmail.com | External | **Sika Corporation** | off |
| `test.external2` | shawnmwest+external2@gmail.com | External | **MAPEI** | off |
| `test.amazon` | shawnmwest+amazon@gmail.com | Partner | **Amazon** | off |
| `test.novi` | shawnmwest+novi@gmail.com | Partner | **Novi** | off |
| `2fatest` | shawnmwest@gmail.com | Analyst | — | **on** (TOTP, not enrolled) |
| `admin.uat` | admin@greencirclecertified.com | Admin | — | off · password `TestPass123!` |

**All `+tag` addresses deliver to the one Gmail inbox** (Gmail subaddressing). They differ per
account only because the application requires unique email addresses.

### Three things that will waste your time if you don't know them

1. **Five failed sign-ins lock an account for ~15 minutes**, and the lockout reports
   *"Invalid email or password"* — identical to a wrong password. If sign-in starts failing after
   several attempts, **wait**; don't start changing settings.
2. **Only one 2FA code exists per user at a time.** A second sign-in invalidates the first code. If
   two codes are in the inbox, **the newest is the only valid one**.
3. `admin` no longer uses `TestPass123!` — its password was changed on 2026-08-19. Use `admin.uat`
   for admin tasks in these cases.

---

## TC-01 · Password reset delivers and actually changes the password

**Why it is written this way:** a sign-in that succeeds *after* a reset proves nothing unless you
know the password was different *before*. Step 1 establishes that.

| # | Step | Expected |
|---|---|---|
| 1 | Sign in as `test.analyst` / `GccTest2026!`, then sign out | Sign-in succeeds — the "before" state is now known |
| 2 | `/Account/ForgotPassword`, enter **shawnmwest+analyst@gmail.com**, submit | Confirmation page. **Identical wording regardless of whether the address exists** — this is deliberate (anti-enumeration), so it is *not* evidence of delivery |
| 3 | Open Gmail | Email *"Password Reset - GCC Database"* from `admin@gccwebapp.com`, arriving within ~1 min |
| 4 | Click the link, set the password to `NewPass2026!` | Success confirmation |
| 5 | Sign in with **`NewPass2026!`** | Succeeds |
| 6 | Sign out. Try signing in with the **old** `GccTest2026!` | **Rejected.** This is the load-bearing step — without it, step 5 could pass with the reset having done nothing |
| 7 | Reset the password back to `GccTest2026!` so later cases still work | — |

> **If no email arrives:** check spam first (new sending domain, no SPF/DKIM history). If it is
> genuinely absent, look in `httpdocs\logs\stdout_*.log` for `Failed to send email` — **do not**
> conclude the reset flow is broken from the UI alone; it reports success either way by design.

---

## TC-02 · An account WITHOUT 2FA is not challenged

**The negative control.** Without it, TC-03 proves nothing: a global setting could challenge
everyone, and TC-03 would still pass.

| # | Step | Expected |
|---|---|---|
| 1 | Sign in as `test.analyst` / `GccTest2026!` | Straight to the internal certificate tiles — **no 2FA page** |
| 2 | Confirm you are genuinely signed in (open any internal page) | Loads normally |

---

## TC-03 · An account WITH 2FA is challenged, and cannot get in without answering

| # | Step | Expected |
|---|---|---|
| 1 | Sign in as `2fatest` / `GccTest2026!` | Redirected to `/Account/TwoFactorAuthentication` |
| 2 | **Before entering anything**, edit the address bar to an internal page (e.g. `/Dashboard`) | **Bounced back to sign-in.** A challenge page you can navigate past is not a second factor — this step is the actual security assertion |
| 3 | Go back, enter the code (see TC-04 for where it comes from) | Signed in |

---

## TC-04 · The challenge page names the factor and the destination — DEF-367

**Previously the page read "your authenticator app or email" for everyone**, which sent testers to
the wrong factor and manufactured the lockouts described above.

| # | Step | Expected |
|---|---|---|
| 1 | Sign in as `2fatest` / `GccTest2026!` | Challenge page |
| 2 | Read the instruction line | **"Enter the 6-digit code we emailed you. It expires in 5 minutes."** |
| 3 | Read the line beneath it | **"Sent to s•••@gmail.com"** — masked, but enough to know which inbox |
| 4 | Deliberately paste a code from your authenticator app | Rejected — *correct*, this account has no enrolled authenticator |
| 5 | Take the **newest** code from Gmail and enter it | Accepted |

> Step 4 is intentional: it confirms the page's instruction is the one to trust, not the app.

---

## TC-05 · Authenticator enrolment prompt — DEF-365

**Precondition:** the account must be 2FA-enabled with **no** enrolled authenticator
(`TwoFactorMethod = 'TOTP'`, `TwoFactorSecret` NULL). `2fatest` is kept in this state; ask Anuki to
reset it if it has been enrolled.

| # | Step | Expected |
|---|---|---|
| 1 | Complete sign-in as `2fatest` using the emailed code | Land on **`/Account/SetupTotp`**, not the certificate tiles |
| 2 | Read the blue information banner | Explains 2FA is on but the authenticator is not set up, and that codes are being emailed meanwhile |
| 3 | Click away to any other page | **Allowed.** It is a prompt, not a lock — emailed codes keep working |
| 4 | Return to `/Account/SetupTotp`. **Delete any existing "GCC Database" entry in your authenticator app first** | The old entry belongs to a different account and can never produce a valid code here |
| 5 | Scan the QR code, enter the 6-digit code from the app | **Accepted**, recovery codes displayed |
| 6 | **Save the recovery codes** | They are shown once |
| 7 | Sign out, sign in again as `2fatest` | Challenged — and the instruction now reads **"…from your authenticator app"**, with **no email arriving** |
| 8 | Enter the code from the app | Signed in. The enrolment prompt does **not** reappear |

> Step 7's "no email arriving" is the proof the method actually switched. A challenge alone doesn't
> show that.

---

## TC-06 · Duplicate email is refused with a usable message — DEF-366

| # | Step | Expected |
|---|---|---|
| 1 | Sign in as `admin.uat` / `TestPass123!` → **Admin → User Management** | User list |
| 2 | Edit `test.admin`, change email to **shawnmwest@gmail.com** (already held by `2fatest`), save | **Refused**, with a message naming the address and saying to choose another or free it up first. **Not** *"Update failed. See the server log for details."* |
| 3 | Edit `test.admin` again, change **only the name**, leave the email as-is, save | **Succeeds.** Re-saving an unchanged email must not be treated as a conflict — this step guards against over-correcting the fix |
| 4 | Confirm `test.admin` still signs in | Succeeds — the refused edit changed nothing |

---

## TC-07 · External users see only their own company's data

**Test the pair.** A single scoped account can only show that it *sees* something; it can never show
it is correctly *denied* someone else's data.

| # | Step | Expected |
|---|---|---|
| 1 | Sign in as `test.external` | External tile view |
| 2 | Note several certificate numbers shown, and the company on each | All **Sika Corporation** |
| 3 | Confirm the absences: **no** Add/Edit certificate buttons, **no** internal tags, **no** Actions column on Product Management | Per the External restrictions |
| 4 | Sign out. Sign in as `test.external2` | External tile view |
| 5 | Compare against step 2 | All **MAPEI**. **None of the Sika numbers from step 2 appear** |
| 6 | While signed in as `test.external2`, open a **Sika** certificate number directly by URL (`/Certificate/CertificateSingleView/<number>` from step 2) | Not shown as owned data. Note: publishable certificates inside their validity window are legitimately public — the check is that no *scoping-restricted* view exposes it |

---

## TC-08 · Partner users see only their own partner's data

| # | Step | Expected |
|---|---|---|
| 1 | Sign in as `test.amazon` | External tile view |
| 2 | Note the certificates and product identifiers shown | Amazon-associated only |
| 3 | Sign out, sign in as `test.novi`, compare | Novi-associated only; the two sets differ |
| 4 | Confirm no co-partner leakage: a certificate shared with several partners must not reveal the *other* partners to this one | Per CR-171 / partner isolation |

---

## TC-09 · Lockout behaves and reports honestly

Run this **last** — it deliberately locks an account.

Policy verified in code (DEF-210, `AccountController.cs:1072-1073`): **5 attempts**, **15-minute**
lockout.

| # | Step | Expected |
|---|---|---|
| 1 | Sign in as `test.novi` with a deliberately wrong password, five times | Each rejected |
| 2 | Now sign in with the **correct** password `GccTest2026!` | **Still rejected** — the lockout is checked *before* the password is verified, deliberately, so a locked account cannot be probed for password correctness |
| 3 | Note the message | *"Invalid email or password"* — **indistinguishable from a wrong password.** Intentional (DEF-209 anti-enumeration), but it means the UI can never tell you an account is locked |
| 4 | Wait 15 minutes, sign in with the correct password | Succeeds. Attempts made *during* the lockout do not extend it — the counter does not increment while locked, so the wait is a genuine 15 minutes from the fifth failure |

> **Why this case exists.** The ambiguity in step 3 cost significant time on 2026-08-19/20: a
> locked test account was read as an SMTP failure, and the investigation went through mail
> transport, config files and app-pool recycles before anyone checked `Users.LockedUntil`. If
> sign-in fails repeatedly and unexpectedly, **check the lockout state before changing anything.**

---

## Automated companion

```
node Scripts/data-ops/2026-08-19-verify-2fa-per-user.js
```

Covers TC-02 and TC-03 in one run and prints the real cause of any failure (rejected credentials vs
SMTP failure vs no challenge). **Expect `4 passed, 0 failed`.** It cannot cover TC-01, TC-04 step 5,
or TC-05 — those need a code from a mailbox no script can read.

---

## Result log

| Case | Date | Tester | Result | Notes |
|---|---|---|---|---|
| TC-01 | | | | |
| TC-02 | | | | |
| TC-03 | | | | |
| TC-04 | | | | |
| TC-05 | | | | |
| TC-06 | | | | |
| TC-07 | | | | |
| TC-08 | | | | |
| TC-09 | | | | |
