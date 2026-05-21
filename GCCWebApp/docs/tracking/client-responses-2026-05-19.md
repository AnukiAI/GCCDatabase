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

## QC #164 — Confirm cert-status-driven Product ID status cascade is working (5/11 client follow-up: "GCC has no way to verify if this is working")

**Client ask (5/11):** *"GCC has no way to verify if this is working, Anuki needs to ensure it is correct - if it's not this would cause huge issues."*

**Draft response (to client):**

Confirmed working end-to-end. Verification was completed on 2026-05-12 via Playwright against the dev environment, with SQL snapshots at each step proving the database actually flipped. Evidence package lives at `docs/tracking/screenshots/qc164/` and includes the screenshots, transition logs, and Partner API call traces. Summary:

| Test | What was checked | Result |
|---|---|---|
| A1 | Product ID Manager — no Status column, no Status filter (5 viewports: 1920, 1366, 1024, 768, 375) | PASS |
| A2 | Add / Edit Product ID modal — no Status field (5 viewports) | PASS |
| B  | Import template — no Status column in headers | PASS |
| C1 | **UP-cascade:** flipping a cert from Hold → Client Approved - Paid promoted both of its product IDs from Inactive → Active automatically | **PASS** |
| C2 | **DOWN-cascade:** flipping the same cert from Client Approved - Paid → Hold demoted both product IDs back to Inactive | **PASS** |
| D  | Partner API (`/api/v1/certificates` with the Amazon key) — only public-status certs appear in the payload; every item carries `status: "Active"` per the contract | PASS |

**How the cascade is guaranteed.** Product ID status is **never user-controlled**. It is derived from the parent certificate every time the cert is saved, imported, or auto-expired. The derivation rule is one line: `"Active"` if the cert is in a public-facing state (Client Approved - Paid / Republished), `"Inactive"` for every other state (Draft, Hold, Expired, Confidential, Not Paid, Expired - No Annual Verification, Republished → Expired).

The cascade runs from **eight different code paths** so there is no way to mutate a certificate without it firing:

1. Analyst Edit form save
2. Create-new-certificate save
3. Manual workflow transition (the dedicated status-change button)
4. Bulk action (apply a status to a set of certs)
5. CSV import — create path
6. CSV import — update path
7. Product ID import (via the per-cert importer)
8. Nightly auto-expiration background service (flips eligible certs to Expired and cascades the product IDs at the same time)

Even if a stale Status value were sent in by a script or a legacy import, the create/update service methods overwrite it with the derived value before saving. There is no path to write an incorrect Status.

**How the client can self-verify** (any of these reproduce the cascade — no Anuki involvement needed):

1. **Edit-form check.** Open any cert with product IDs (e.g., 26-0002 on dev or any production cert tagged to Amazon). Change its status to Hold, save. View the cert detail — every product ID row shows Inactive in the Partner API response. Flip the status back to Client Approved - Paid, save — product IDs flip back to Active.
2. **Partner API contract check.** Call `GET /api/v1/certificates` with the Amazon partner key. Every returned cert has `status: "Active"`. No Draft / Hold / Expired certs leak through. Product IDs under each cert inherit the same Active state.
3. **Expiration check.** Set a cert's expiration date to yesterday, leave status as Client Approved - Paid. Within 24 hours the nightly sweep flips it to Expired and re-derives product IDs as Inactive. (For a faster repro on dev, the sweep runs every minute — let us know and we'll point a tester at the dev environment.)

**One adjacent item worth flagging for transparency** (separate, not a QC #164 blocker): Anuki internal testing on 2026-05-19 found that the cert Edit form does not currently enforce the workflow transition matrix — illegal jumps like Draft → Republished are accepted without warning. This is logged as a separate defect (DEF-203) and does not affect the product-ID cascade above. The cascade fires on **every** cert save regardless of whether the transition was legal, so the product-ID statuses remain correct even in that case. We will close DEF-203 separately.

**Recommended next step for the client:** Spot-check on dev or UAT using any of the three steps above. Once confirmed, this QC item can be closed.

---

## When to flip the QC log

- **#167:** flip to `Complete` once Shawn sends the explainer to the client.
- **#150:** flip to `Complete` once (a) forgot-password is screenshot-verified on dev/UAT and (b) the named test logins are provisioned or the seed list is updated.
- **#164:** flip to `Complete` once Shawn sends the verification summary to the client and the client confirms spot-check (or after 5 business days with no response — the evidence package is sufficient on its own).
