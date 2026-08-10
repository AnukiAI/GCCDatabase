# Email to Novi — which fields are required for onboarding

**Drafted:** 2026-08-10 · **For:** Shawn to send, or for the client to send · **Trigger:** Shawn, 2026-08-10 — *"ask novi if standard version is required for onboarding."*

**Send path — decide before sending.** Novi is the client's commercial partner, not ours. Cleanest is
the **client sending it** (or us sending with the client copied) so Novi hears the question from the
certifying body they have the relationship with. Novi's own doc points to *"your Novi Partnerships or
Data Integration Manager"*, with `partnerships@noviconnect.com` for setup and support — use the named
manager if the client has one.

**Bundled deliberately — cut what you don't want.** You asked about standard version; questions 2 and 3
are the only other things **only Novi can answer**, and each one changes what we build or configure.
Asking now avoids a second round trip. Each is a standalone paragraph, so delete either without
rewriting anything.

**Deliberately not disclosed:** nothing about what our feed does or does not currently hold. These are
neutral requirements questions — Novi's answers determine scope, and volunteering gaps before they've
stated requirements invites work we may not need.

**Internal cross-reference (not in the email):** Q1 → CR-036 · Q2 → CR-035 · Q3 → DEF-345 (per-key rate
limit, currently 200/hour).

---

**Subject:** GreenCircle certification feed — confirming required vs optional fields before we finalise

Hi [name],

We're finalising the certification data feed for GreenCircle Certified against your data exchange
guidance, and I have three quick questions so we scope it correctly the first time.

**1. Standard version.** Your ideal structure lists certification metadata as *issuer, issue date,
expiration date, standard version*. Is **standard version required for onboarding**, or is it optional
where a certifying body doesn't version its standards that way? We want to know whether to treat it as
a gating item.

**2. Certification status.** You list status as *Valid / Expired / Revoked-or-invalid*. Does Novi need
those as distinct values on every record, or is it sufficient for the feed to contain only currently
valid certifications — so that a certification's presence confirms it, and its absence means it is not
currently valid? Knowing which you need makes a material difference to the design.

**3. How you'll consume it.** Will Novi pull the full certification set on a schedule (nightly or
weekly), or check individual brand claims as they arise? The two patterns have very different request
volumes and we'd like to size the connection correctly before you start.

Happy to jump on a short call if that's easier than writing it out.

Best regards,
[your name]
Anuki Consulting — on behalf of GreenCircle Certified
