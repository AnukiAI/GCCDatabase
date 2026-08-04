# Production Cutover Plan — 2FA enablement + Duo

**Opened:** 2026-08-04 · **Owner:** Anuki Consulting · **Status:** Active — pre-cutover
**Driver:** client requirement, 2026-08-04 — *two-factor authentication must be enabled prior to production cutover, and the client wants **Duo***.

**Approach set by Shawn, 2026-08-04:** the client **already has email and a Duo tenant**. So this is
configuration and integration, not capability-building. Everything below is to be **proven on a
subdomain of the existing production environment before cutover**, to take risk and delay out of
go-live rather than discovering it on the day.

Duo itself is tracked as **CR-033** (scope deliberately not yet fixed — see §4).

---

## 1. The one thing that must not happen

**Enabling 2FA against the current production configuration locks out 100% of users, including
every admin, with no self-service way back in.** This is not a prediction; each link is verified in
the code and the deployed config:

| # | Link | Evidence |
|---|---|---|
| 1 | `Enable2FA=true` challenges **every** user. There is no per-role or per-user exemption in the login path. | `Controllers/AccountController.cs:203-215` |
| 2 | Any user who has not chosen a method defaults to **Email**. | `Services/EnhancedTwoFactorService.cs:22` |
| 3 | Production `SmtpSettings.Host` / `.Username` / `.Password` are all **empty strings**, so the DI factory selects **`MockEmailService`** — the code is generated, logged, and never sent. | `appsettings.Production.json:92-99` + `Program.cs:505-515` |
| 4 | Production has no `SmsSettings` override, so it inherits **`Provider: "mock"`** — SMS is not delivered either. | `appsettings.json:77` |

And there is no recovery route: **TOTP and recovery codes can only be set up while logged in**, which
is impossible once step 1 fires. The failure is total and self-sealing.

> **Gate: do not flip `Enable2FA` in production until §2 is complete and §3 has been rehearsed.**

### 1a. The second lockout vector — SMS, which is easy to miss

Even after email is fixed, **the SMS option is a live trap**. The 2FA settings page offers SMS as
soon as a user has saved a phone number (`TwoFactorPreferences.cshtml:594`). If they select it while
`SmsSettings:Provider` is still `"mock"`, their preference is stored and **every future login sends
their code to a mock provider** — locking them out at the *next* login, not the current one, which
makes it look unrelated to anything they did.

**Decide before cutover — one of:**
- (a) configure a real SMS provider (Twilio credentials already scaffolded: `SmsSettings:Twilio`), **or**
- (b) hide/disable the SMS option in the UI until a provider is live.

Leaving it as-is is not an option. This is a **cutover blocker**, not a nice-to-have.

---

## 2. 2FA go-live prerequisites (in order)

| # | Activity | Depends on | Status |
|---|---|---|---|
| 2.1 | Obtain the client's SMTP host / username / password / from-address and populate `appsettings.Production.json → SmtpSettings`. Client already has email — this is credentials, not procurement. | client | ☐ |
| 2.2 | Verify `EmailService` (not `MockEmailService`) is selected. The factory requires **all three** of Host, Username, Password non-empty — one blank silently reverts to mock. Confirm from startup logs, not from the config file. | 2.1 | ☐ |
| 2.3 | Send a real 2FA code to a real mailbox on the subdomain and confirm receipt, including spam placement and the from-name. | 2.2 | ☐ |
| 2.4 | Resolve the SMS trap per §1a — configure Twilio **or** hide the SMS option. | decision | ☐ |
| 2.5 | Audit user records for missing/invalid email addresses **before** the flip. Any user without a deliverable address is locked out the moment 2FA turns on. Read-only query; no data changes. | — | ☐ |
| 2.6 | Enrol every admin on **TOTP/authenticator** first. TOTP has **no delivery dependency** — it works if email and SMS both fail, which makes it the safest break-glass. Store their recovery codes per §3. | 2.3 | ☐ |
| 2.7 | Deploy the pending 2FA fixes: **DEF-333** (missing `VerifyPhone.cshtml` → HTTP 500 in SMS setup) and **DEF-334** (orphan `2FAMethod` TempData write). Both are in the working tree, undeployed. | deploy bundle | ☐ |
| 2.8 | Pilot on the subdomain (§5) with a small real user set before touching production. | 2.1–2.7 | ☐ |

---

## 3. Break-glass and rollback — agree this before the flip, not during

- **Rollback is a config flip:** set `Authentication:Enable2FA` to `false` and restart. **Confirm at
  cutover that someone with Plesk access is available and awake** — if 2FA delivery fails and nobody
  can edit the config, the outage lasts as long as it takes to find that person.
- **Break-glass account:** at least one admin enrolled on **TOTP**, with recovery codes printed or
  stored outside the application. Email/SMS break-glass is circular — if delivery is the thing that
  broke, it cannot rescue you.
- **Confirm the rollback actually works on the subdomain** before relying on it in production.

---

## 4. Duo — scope deliberately still open (CR-033)

Per Shawn 2026-08-04: **added to cutover activities and tracked; scope not decided yet.**

**Established fact that shapes every option:** **Azure AD B2C is not in use.** Production config is
all placeholder tokens (`{AZURE_AD_B2C_CLIENT_ID}` …), so `isAzureAdB2CConfigured` is false and the
app runs on its **own cookie authentication** with the custom 2FA service
(`Program.cs:159-170, 249`). The project guide's "Azure AD B2C" description is out of date.

Consequence: there is **no external identity provider to attach Duo to**. Duo would be integrated at
the **application level** — Duo Universal Prompt (OIDC redirect) inserted into the app's own login
flow — unless the client wants to stand up a real IdP first, which is a materially larger project
and not a cutover-timeframe activity.

**Known:** the client has an **existing Duo tenant**, so we request an application integration from
their IT (client id / client secret / API hostname) rather than procuring anything.

**Still to decide (client):**
1. **Who does Duo cover?** Internal GCC staff only, or external company/partner users too? **Duo is
   licensed per user** — extending it to every external user is a recurring cost the client owns.
2. **Does Duo replace the built-in methods, or sit alongside them?** Replacing is one code path and
   simpler to operate; alongside is friendlier but doubles the surface to build and regression-test.
3. **Who owns enrolment** for users who are not on the client's own directory?

---

## 5. Subdomain pilot on production infrastructure

**Why:** it exercises the *real* SMTP relay, the *real* Duo tenant, and real network/TLS conditions.
Those are exactly the things that fail on cutover day and cannot be reproduced locally or on
`gccwebapp.com`.

**Decisions needed before it is stood up:**

| Decision | Why it matters |
|---|---|
| **Which database does the subdomain point at?** | Pointing at the live production DB means pilot logins, 2FA enrolments and Duo device registrations write to **real user records**. Recommend a **restored copy**, with the pilot users re-created there. If the client wants it against live data, that needs explicit sign-off — this is a data change ([[feedback_data_changes_require_cr]]). |
| **Search-engine exclusion** | A subdomain of the real production domain is publicly resolvable. Apply the noindex/robots handling before DNS goes live — see the SEO deindex playbook. Do not repeat the indexing cleanup. |
| **Access restriction** | Keep it behind the existing access gate so it is not a second public front door to client data. |
| **Duo integration keys** | A separate Duo *application* for the subdomain, so pilot enrolments can be discarded without touching production Duo config. |

**Exit criteria — the pilot is only "passed" when all of these hold:**
- a real user receives a real emailed code and completes login;
- an admin completes TOTP enrolment and logs in with authenticator only;
- Duo prompt completes for at least one internal user;
- the SMS decision from §1a is proven (either a real code arrives, or the option is genuinely absent);
- **rollback is exercised** — `Enable2FA=false`, restart, confirm normal login returns.

---

## 6. Open questions for the client

1. SMTP credentials for the application (host, username, password, from-address).
2. Duo: who is in scope (internal only / everyone), and does it replace or supplement the built-in methods?
3. Duo application integration keys from their IT, plus a separate application for the subdomain pilot.
4. SMS: do they want it as a delivery option at all? If not, we hide it and the §1a risk disappears.
5. Subdomain pilot: which database, and is a restored copy acceptable (recommended) or do they want live data?
6. Target cutover date, so the pilot window can be worked backwards from it.

---

## 7. Change log

| Date | Note |
|---|---|
| 2026-08-04 | Opened. Client requirement captured: 2FA enabled before cutover, Duo wanted. Lockout blocker (§1) and SMS trap (§1a) identified and verified in code + deployed config. Duo logged as CR-033, scope intentionally open. Subdomain-on-production pilot adopted as the de-risking approach. |
