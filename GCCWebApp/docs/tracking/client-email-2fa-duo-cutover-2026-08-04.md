# Client-ready email — 2FA + Duo cutover questions

**Drafted:** 2026-08-04 · **For:** Shawn to send · **Source:** `production-cutover-plan.md` §6
**Note:** paste-ready below the line. No internal defect/CR numbers, file paths or code references.
Placeholders in `[square brackets]` need filling before sending.

---

**Subject:** Two-factor authentication + Duo — what we need from you to schedule the cutover

Hi [name],

Following your note that two-factor authentication needs to be live before we cut over to
production, and that you'd like to use Duo — here's where things stand and the handful of answers we
need from you to lock in a date.

**The good news first.** Two-factor authentication is already built and running in our test
environments today: email codes, authenticator apps (Microsoft Authenticator, Google Authenticator
and similar), backup recovery codes, and a settings page where each user chooses their preferred
method. Adding Duo means connecting your existing Duo account to that, rather than building
two-factor from scratch.

**How we plan to de-risk the switch.** Before we touch production, we'd like to stand the system up
on a subdomain of your production environment and prove the whole flow there — real email delivery,
real Duo prompts, real sign-ins. Two-factor is one of the few changes that, if the delivery of codes
isn't working, prevents people from signing in at all. We would rather find that on a test subdomain
than on cutover day. Once it's proven there, enabling it in production is a small, quick change.

To do that, we need six things:

**1. Email sending details for the application**
The application needs to send sign-in codes from your mail system. Please have your IT provide the
outgoing mail server address, the username and password for the sending account, and the "from"
address you'd like codes to come from (for example, `noreply@[yourdomain]`).

This one is the gating item: **codes cannot be delivered until it's in place, so two-factor cannot be
switched on until it's in place.** Everything else can proceed in parallel.

**2. Who should Duo apply to?**
Should Duo cover your internal team only, or also the external company and partner users who access
the database?

Worth knowing before you decide: **Duo is licensed per user.** Extending it to every external company
and partner contact could increase your Duo subscription cost significantly, and that's your cost
rather than ours. A common approach is Duo for internal staff, with the built-in authenticator-app
or email codes for external users — but it's entirely your call, and we're happy either way.

**3. Should Duo replace the existing options, or sit alongside them?**
Either works. Replacing them means one consistent sign-in experience and less to maintain. Keeping
both means users can pick what suits them, at the cost of a larger system to test and support. If
you have a preference from how Duo is used elsewhere in your organisation, we'll follow it.

**4. Duo application credentials**
Once questions 2 and 3 are settled, we'll need your IT to create an application integration in your
Duo account and send us the credentials for it. We'd also ask for a **second, separate** Duo
application for the test subdomain, so that anything we register while testing never touches your
live Duo configuration.

**5. Do you want SMS text codes as an option at all?**
The system can offer codes by text message, but that needs a separate paid SMS service that isn't
currently set up. If you don't need it — most organisations using Duo and authenticator apps don't —
we'll simply remove the option, which is one less thing to configure and maintain. If you do want it,
let us know and we'll price it separately.

**6. Test data on the subdomain**
For the test subdomain, our recommendation is to point it at a **copy** of your production data
rather than the live database, so that test sign-ins and Duo device registrations can't affect real
user records. Please confirm you're happy with that approach.

**And one scheduling question:** do you have a target date in mind for the production cutover? That
lets us work backwards and tell you exactly when we'd need items 1 and 4 in hand.

Item 1 (email details) and item 4 (Duo credentials) are the two that involve your IT team, so those
are worth starting now if you're able — the rest are decisions we can talk through on our next call
if that's easier than replying in writing.

Happy to jump on a call to walk through any of this.

Best regards,
[Shawn]
Anuki Consulting
