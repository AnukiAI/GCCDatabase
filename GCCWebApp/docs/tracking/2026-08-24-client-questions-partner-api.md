# Client questions — Partner API documentation

**Status:** Draft for Shawn to send
**Date drafted:** 2026-08-24
**Covers:** partner API rate limit (DEF-345), CR-035 (certificate status as a value), CR-036 (certification standard version)

**Why these three together:** all three came out of the same review of the partner integration guide
against the client-supplied Novi Data Exchange Guidance (2026-07-31). All three are open questions in
the guide today, and the guide cannot be issued to a partner until at least the first is settled.

---

## Email draft

**Subject:** Partner API guides — three decisions needed before we issue them

Hi [NAME],

The partner API integration guide is written and ready, but three points need your decision before it
goes to a partner. The first is a confirmation and should be quick; the other two are scope questions
raised by Novi's data guidance, and either could be answered "no change" at no cost.

### 1. Rate limit — confirming 200 requests per hour

The developer guide Amazon holds states **200 requests per hour**, and that is the figure we have kept
in the new generic partner guide. Our position is that 200/hour is correct and should stand — it is
what was published, it is what the system is configured for, and lowering or raising it now would mean
re-issuing a document Amazon has already built against.

**Please confirm 200 requests per hour is the published limit for all partners.**

For transparency: we found earlier this month that a fault in how the limit was applied meant partner
keys were being throttled at 100/hour rather than 200. That is fixed and tested, and goes out with the
next deployment. Confirming 200 here means the fix and the documents agree, and we verify the header
on the live system before any guide is issued.

If you would rather set a different figure per partner, that is supported — the limit is stored per
key — but it needs deciding now rather than after documents are circulated.

### 2. Certificate status — should partners see expired and revoked certificates? (CR-035)

Today the partner API answers "is this certificate currently valid?" by whether the record comes back
at all. Valid certificates are returned; everything else is simply absent. That was a deliberate
design for **badging** — showing a valid certification on a product page — and it is the behaviour
Amazon signed off on.

Novi's data guidance asks for something different: a status **value** of *Valid / Expired /
Revoked*, because their use case is **claim validation** — confirming or refuting a brand's claim
against your ledger. Those are genuinely different questions, and our current contract only answers
the first.

**This is larger than adding a field, for three reasons you should weigh:**

- **There is no "revoked" state in the system.** The certificate lifecycle has eight states and none
  of them is revocation. Reporting "Revoked" means either adding a new state or repurposing an
  existing one — a change to the certificate workflow itself, with knock-on effects in the UI and
  reporting. That is your call, not a technical detail.
- **It is a disclosure decision.** Returning expired or held certificates means showing partners
  records they have never been able to see. In particular, **Client Approved – Confidential is
  paid-but-embargoed** — you deliberately chose to keep those private. Our recommendation is that
  Confidential stays hidden regardless of what else changes, but we need that stated rather than
  assumed.
- **It would affect Amazon, not just Novi.** Changing `status` from a fixed value to a variable one
  is a breaking change for every existing consumer. Amazon's integration has no reason to branch on
  it today and may reasonably treat every returned record as valid. They would need to re-certify
  their side for a change made for Novi's benefit. A per-partner opt-in flag avoids that — Amazon's
  responses stay byte-identical and only partners who ask for the new behaviour get it. That is our
  recommendation if you proceed.

**What we need from you:**

1. Do you want to proceed with this at all, or onboard Novi on the current presence/absence model?
2. If yes: which certificate states become partner-visible, and under what label? (Recommendation:
   Confidential stays hidden.)
3. Should a formal "Revoked" state be created? This is the workflow change, and the largest part.
4. Per-partner opt-in, or applied to everyone? (Recommendation: per-partner opt-in.)

**You are not blocked on this.** Novi can be onboarded today for badging-style use. It only limits
claim validation, and the guide already states plainly that a "not found" response is **not** evidence
that a certificate was revoked — so nobody can misread it in the meantime.

### 3. Certification standard version (CR-036)

Novi's guidance also asks for the **version of the standard** a certificate was granted against — for
example, certified against v2.1 of a standard rather than v1.0. There is no such field anywhere in the
system today.

**One question may close this at no cost: does GCC version its certification standards at all?**

If your certifications are not issued against numbered revisions of a published standard, the correct
answer to Novi is simply "not applicable" — one sentence in the partner guide, nothing to build. Please
answer this before the rest.

**If you do version standards,** four follow-ups shape the cost:

1. **Where is it captured?** Our recommendation is **on each certificate at the time it is issued**,
   defaulting from the certification type. The cheaper alternative — one version per certification
   type — has a serious flaw: updating a type's version would silently rewrite history, so a
   certificate issued years ago against v1.0 would start reporting v2.0. Wrong, and wrong invisibly.
2. **Free text, or a managed list of versions per certification type?** Free text is much cheaper and
   matches how Certification Results was handled. A managed list gives cleaner data and validation.
3. **Existing certificates:** leave the field blank, backfill everything, or backfill only those
   visible to partners?
4. **Where else does it appear?** API only, or also the certificate detail screen, the CSV import and
   export template, and the certificate PDF?

Unlike item 2, this one is **additive** — it adds a field without changing anything Amazon already
relies on, so it does not require them to re-certify. They would simply be notified.

---

Happy to walk through any of these on a call. Items 2 and 3 are the ones worth talking rather than
writing, since both could reasonably end in "no change".

Best regards,
[SIGN-OFF]

---

## Internal notes — not part of the email

- **Sequencing.** Question 1 gates the first issuance of the partner integration guide
  (`docs/tracking/partner-api-guide-issuance-log.md`). Questions 2 and 3 do not — they would trigger a
  **re-issue** to any partner already holding a copy, which is precisely why the issuance log exists
  and why it should be filled in from the very first send.
- **200/hour needs no code change.** `Program.cs:460` already sets `PartnerApiKeyLimit = 200`, matching
  both guides. The class default of 2000 in `ApiRateLimitingMiddleware.cs:186` is overridden at
  startup and never applies. What was broken was DEF-345 — the limiter ran before authentication, so
  no partner branch was ever reached and everything fell through to the anonymous 100/hr bucket. Fixed
  and verified on localhost 2026-08-10, **not yet deployed**.
- **Verify, don't assume, after deploy.** The guide's §2 carries an internal block saying the 200/hour
  figure is provisional until `X-RateLimit-Limit: 200` is observed coming back from the live server
  with a real partner key. Delete that block only after actually seeing the header — per
  [[feedback_verify_deployed_asset_not_symptom]], a fix being in the bundle is not evidence it is
  live.
- **Do not bundle these with the host-change note.** `2026-08-14-amazon-host-change-note.md` and
  `2026-08-14-client-message-integration-guides.md` are separate drafts still awaiting send, and both
  concern the subdomain cutover. Mixing a routine config change with open scope questions muddles a
  decision that should be easy.
- **CR-035 recommendation, stated plainly for the record:** per-partner opt-in, Confidential stays
  hidden, and out-of-scope certificates must remain indistinguishable from non-existent ones or the
  enumeration protection in [[feedback_partner_data_isolation]] is lost. That last point is not
  negotiable regardless of what the client chooses.
- **CR-036 contract test will go red on purpose.** `Scripts/smoke-prod/amazon-contract-diff.js`
  reports *any* added field as a contract change. If CR-036 is built, re-baseline deliberately rather
  than treating the red as a failure.
