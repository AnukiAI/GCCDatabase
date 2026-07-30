# Client Email — Certificate Status Workflow (FINAL / send-ready)

*Fill in [Name] and the recipient address. Reflects work as of 2026-07-13: the fix is
built, tested, and staged in the next deployment bundle (not yet on the live site).*

---

**To:** [client contact]
**From:** Shawn West, Anuki Consulting
**Subject:** Certificate status change — the "invalid transition" message, and how we've handled it

---

Hi [Name],

Thanks for flagging the message you hit while editing a certificate (moving one from **Draft** straight to **Republished**). I want to explain what it is, why it's there, and the two things we've done about it — one already built, one that needs a quick review from your side.

**What you saw.** The system blocked the change with *"Invalid status transition from 'Draft' to 'Republished'."* That's not a bug — it's a workflow rule doing its job. The application enforces an order for how a certificate moves through its lifecycle and only allows sensible next steps from wherever a certificate currently sits. From **Draft**, the allowed next steps are Client Approved – Not Paid, Confidential, Paid, or Hold. **Republished** is only reachable after a certificate has been Paid (or has Expired), because "republished" means a previously-live certificate is being re-issued — so a jump straight from Draft to Republished is intentionally not permitted.

**Where these rules came from.** These guardrails were added during a security-hardening pass earlier this year. Before that, the edit screen would save any status with no checks — which risked certificates ending up in states that don't reflect reality. The rules protect the integrity of your certificate data and what your partners (including Amazon) see publicly.

**Fix #1 — done (no action needed from you).** You mentioned you then couldn't re-save or recreate the record. That was a genuine rough edge: when the change was rejected, the screen kept showing your invalid choice, so saving again just hit the same wall — it felt like being locked out (nothing was ever saved or damaged). We've corrected this, and it's built, tested, and staged in the next update. Now:

- the status field resets to the certificate's real current status, so a normal save works immediately, and
- the message tells you exactly which statuses you can move to from where you are, instead of asking you to "check the rules."

**Fix #2 — a quick review from you (the important one).** The eight certificate *statuses* were defined by your team. The specific *rules for which status can follow which* were set by our engineers during that security pass and, as far as our records show, were never formally reviewed with you. Rather than assume they're all correct, we've written up the complete set of rules — every allowed and blocked step — in a one-page sheet with a sign-off checklist. The exact case you hit (Draft → Republished) has a Yes/No line so you can tell us whether it should actually be allowed. Could you (or the right person on your side) confirm each rule or mark what you'd like changed? Once we have your answers, we'll adjust the workflow to match exactly what you want. *(Sheet attached / to follow.)*

Nothing is broken and no data was affected — the system is being appropriately careful. We just want the rules to reflect your business, not our best guess.

Happy to walk through any of this on a quick call.

Best,
Shawn West
Anuki Consulting

---

*Attachment to include: the transition-matrix sign-off sheet — ready to send in two formats: `certificate-status-transition-matrix.pdf` (print/read) and `certificate-status-transition-matrix.docx` (client can type answers directly into the sign-off table). Both in `docs/tracking/`; source in the matching `.html`.*
