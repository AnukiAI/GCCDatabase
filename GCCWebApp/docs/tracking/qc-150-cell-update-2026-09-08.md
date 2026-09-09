# QC #150 (row 155) — resolution summary, 2026-09-08

**Workbook:** `Database QC Log_9.3.26_sw_updates.xlsx` · sheet `GCC DB QC Log2` · **row 155**
**Page:** Sign In / Forgot Password · **Raised by:** Carly · opened 4/7/26 · Images 116, 117

| Cell | Set it to |
|---|---|
| **Column J** | **append** the block below under the existing text |
| **Column C** (`Status`) | `Resolved` — currently `Pre-Prod` |
| **Column Q** | `Complete` |
| **Column O** | currently `See Edits Needed & Questions` — leave it; the client clears it |

Three edits and two questions. All three edits are live; both questions are answered, and one of them
is a real go-live task rather than a code change.

---

## Column J — paste as one append (line breaks = Alt+Enter)

```
9/8: All three edits are live on gccwebapp.com, and both questions are answered below.

THE THREE EDITS
1. Expiry wording (image 116) - the confirmation screen and the email now both say 1 hour, which is what the system actually enforces. The 24-hour wording is gone.
2. Background green (image 117) - the Forgot Password and Reset Password screens now use the same lighter green as the Sign In screen, so the logo no longer blends into it.
3. "Access your sustainability certification database." has been removed from the Forgot Password screens.

To review 1: the expiry wording appears on the screen you land on after submitting the form, and in the email itself - so submit a reset to see both.

YOUR QUESTIONS
Will the sending address change to the production domain at go-live? Yes. It is a configuration setting rather than anything built into the code, so it changes without a code release. The one dependency is that the mailbox or alias has to exist on the production domain before we point at it - we have that on the go-live list so it is done in the right order.

Will the links point to the production URL at go-live? Yes, and this one needs nothing from either of us. The reset link is built from whatever address the site is being served on, so the moment the site answers on the production domain the links follow automatically. Nothing to change and nothing to remember.

ON THE MESSAGE LANDING IN SPAM
Thanks for calling it out. Mail is currently sent from the test domain, which has no sending reputation of its own - that is the usual reason for a first-time message being filtered and for the short delay. Setting up the standard sender records for the production domain at go-live is what improves it, and we will include that with the mailbox setup above. Worth re-checking placement once the production domain is live.

Ready for review whenever suits.
```

---

## Internal notes (do NOT paste)

- **Backing records:** DEF-405 (expiry wording), DEF-406 (auth screen gradient), DEF-407 (tagline
  removal) — all shipped 2026-09-04 and present in the 2026-09-08 bundle.
- **Verified live 2026-09-08, not assumed:**
  - Tagline **absent** from `/Account/ForgotPassword` and `/Account/ResetPassword` (both HTTP 200,
    through the test gate; `/Account/Login` returns a different page size, which is how we know the
    gate was passed and these are the real pages rather than the gate page — DEF-341).
  - `/css/auth.css` serves `--auth-left-gradient: linear-gradient(135deg, #92C83E 0%, #447638 100%)`,
    the lighter sign-in green, and the bundle verifier's DEF-406 check ("no view redeclares the
    gradient") passed.
  - The 1-hour copy lives in `Views/Account/ForgotPasswordConfirmation.cshtml:81` and in the email
    body at `AccountController.cs:547`. **Both say 1 hour.**
- **`/Account/ForgotPasswordConfirmation` returns 404 to a direct GET** — it is rendered as a view
  from the POST (`return View("ForgotPasswordConfirmation")`, `AccountController.cs:581`), not a
  routable action. That is by design; a probe hitting it directly is testing the probe. The client is
  told to submit the form rather than given a URL that will 404 on them.
- **The link-follows-the-domain answer is verified in code**, not assumed: the reset URL is built as
  `$"{Request.Scheme}://{Request.Host}/Account/ResetPasswordWithToken?…"`
  (`AccountController.cs:530`), so it tracks whatever host serves the request.
- **The sender address is a genuine go-live dependency, not a code change.**
  `appsettings.Production.json` carries `FromEmail: admin@gccwebapp.com`. Per
  `appsettings.Secrets.template.json`, the mailbox or alias must exist *before* the value is changed,
  or outbound mail fails silently. This belongs on the go-live checklist.
- **Deliberately not raised with the client:** SPF/DKIM/DMARC by name. "Standard sender records" says
  the same thing without turning a UAT row into a DNS conversation; the detail belongs with whoever
  sets up the production domain.
