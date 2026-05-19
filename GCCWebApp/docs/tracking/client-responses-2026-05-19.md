# Client Responses — 2026-05-19

Drafts for QC items that don't need code work — both can go to the client as-is once Shawn approves the wording. Both close the QC log items on our side once sent.

---

## QC #167 — When does the system auto-expire a certificate?

**Client question (4/30):** *"I updated 25-0246 to have an expiration in the past but left the Client Approved - Paid status. It correctly removed the cert from public view since the expiration date expired (which is good). Would the system eventually auto change the status to Expired too? When and how would this work?"*

**Draft response (to client):**

Yes — the system runs a background service called `CertificateExpirationBackgroundService` that automatically transitions certificates from `Client Approved - Paid` (or `Republished` / `Client Approved - Confidential`) to `Expired` once their expiration date has passed.

Schedule:
- **Production / UAT:** every 24 hours.
- **Development:** every minute (so we can verify the flow without waiting overnight).

Conditions to flip a cert from a "live" status to `Expired`:
1. `Status` is currently in the set `{ Client Approved - Paid, Republished, Client Approved - Confidential }` (i.e., it had gone live at some point).
2. `ExpirationDate` is in the past (compared against US Eastern Time, since expirations are business dates).

If both conditions hold, the cert flips to `Expired` on the next sweep. The expiration date itself is what removes the cert from the **public** view immediately — that's a query-time check and doesn't wait for the background service. The status flip is the audit/inventory side of the same event, and is what the dashboard's "Certs Expired Last Month" tile counts.

The cert you mentioned (25-0246) will flip to `Expired` on the next nightly sweep after we deploy the next build. If you want to see it happen sooner on a specific cert during testing, let us know and we'll trigger the sweep manually.

---

## QC #150 — Confirm forgot-password works + provide individual logins for email testing

**Client ask (4/7, no retest since):** *"Anuki confirm forgot password works. We will need individual logins at some point to see emails and test."*

**(a) Forgot-password confirmation — Shawn to verify locally and add a screenshot:**

1. Visit `http://localhost:5101/Account/ForgotPassword` (or `https://gccwebapp.com/Account/ForgotPassword` once deployed).
2. Enter the email of any active user (e.g., a Carly / Kyle test account).
3. Submit — the page should show "Reset link sent" and an email should arrive at the address.
4. Open the email, click the reset link, set a new password, log in with the new credentials.

If anything in that flow misbehaves (no email, broken link, expired token), it should be re-opened as a defect with the specific step that broke. The `AccountController.ForgotPassword` / `ResetPassword` actions are already implemented and verified end-to-end during sprint 2.

**(b) Individual test logins — needs names + emails from client.**

We can create as many named test users as the client wants — typical pattern is one per real tester so each person sees emails arrive at their own inbox and can exercise the password-reset flow against their real address. Ask the client for:

- Tester name (display name on the user record)
- Email address they want emails delivered to (their actual work email is fine; the system never sends marketing or shares the address externally)
- Role they want for that login (Admin / Analyst / External / Partner)
- For External or Partner roles: which Company / Partner they should be scoped to

Once we have the list, we provision the users via the Admin → User Management page. Initial password is auto-generated and emailed to them; first login forces a password change.

Alternative if the client wants to self-serve: we add their named addresses to the seed list and they can use forgot-password to set their own initial password — no admin involvement.

---

## When to flip the QC log

- **#167:** flip to `Complete` once Shawn sends the explainer to the client.
- **#150:** flip to `Complete` once (a) forgot-password is screenshot-verified on dev/UAT and (b) the named test logins are provisioned or the seed list is updated.
