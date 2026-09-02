# Shipped — September 2026

Stakeholder-readable log of what reached the test environment (gccwebapp.com) and when.

---

## 2026-09-02 — session timeout, query performance, deploy tooling, multi-country certificates

Deployed to gccwebapp.com. Bundle `GCCWebApp-deploy-20260902-full.zip`
(md5 `de72254e3fd9c42f4f56c0d035314675`), built from a full clean rebuild.

### Fixed

**Sessions were timing out early, and the warning was unreliable (DEF-390).**
The reported symptom was "the 30 minute timeout is not always followed, tends to be shorter, and
sometimes I get the alert and sometimes not." Both halves were real. The application held two
independent 30-minute clocks — one counting from the last page request, one from the last mouse
movement — and they never agreed. Because the session cookie is only renewed once more than half
its life has passed, a user could be signed out **anywhere between 15 and 30 minutes**, not 30.
The warning was driven by the second clock, so it often never appeared: it stayed silent while
someone read a page without clicking, and browsers suspend it entirely in a background tab.

Real activity now keeps the session alive on the server, and the countdown is measured against the
clock rather than a timer the browser can suspend — so a backgrounded tab reaches the right answer
the moment you return to it. "Remember me" is unaffected.

**Certificate pages could return far more data than needed (DEF-394).** A preventive change: one
detail-page query was fetching the combination of six related lists at once. Harmless at today's
data volumes, but the cost multiplies rather than adds as certificates gain more tags, countries and
product IDs — and multi-country support has just added another. Split into separate queries.
No visible change.

**A deployment setting silently did nothing (DEF-395).** The option controlling where the
application stores its encryption keys had been overridden internally, so setting it could only ever
crash startup and never actually move the keys — the cause of the 2026-08-18 outage. The setting now
works, and an unusable value falls back safely with a clear error instead of taking the site down.

### Delivered by the parallel work stream

**Multi-country certificates (CR-037).** Certificates can now hold more than one country. 1,327
existing certificates were migrated to the new structure.

**Certificate edits now write an audit trail (DEF-391).**

Also included: certification-results normalisation (CR-042), the Sofidel and "not-loaded 29"
certificate loads, and DEF-396/397.

### Testing

The maintained security suite — partner isolation, role permissions, cross-company access, download
protection, and a new check for the session fix — passes **65 of 65** with no failures.

### Known follow-ups

- Compound-country certificates (about 15) still hold their countries as a single combined string
  and need a separate, deliberate data change; it alters what the CSV and Excel exports show, so it
  is being scheduled on its own rather than bundled here.
- ~~End-to-end browser confirmation of the session-timeout fix.~~ **Done 2026-09-02** — a full
  36-minute cycle was run against the live test environment, covering four situations at once:
  sitting idle, working without changing page, leaving the tab in the background, and staying signed
  in with "remember me". All eight checks passed. The warning now appears at 25 minutes and sign-out
  follows at 30, in that order; and someone reading a page without clicking through to another one
  now stays signed in, which is the situation that previously logged people out with no warning.
- An index created by the multi-country migration is wider than SQL Server can safely guarantee
  (DEF-398). No effect on current data; queued for a follow-up.

### Incident during deploy

The application went out before its database migration, and certificate pages returned errors until
the migration was applied — the new code expects a table the migration creates. Detected and
corrected the same session. **No data was lost or altered:** certificate count was 5,731 before and
after, and a full backup was taken beforehand. The deploy runbook now states the required order
explicitly.
