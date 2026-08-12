# Partner API Integration Guide — issuance log

**Purpose:** record which partner holds which revision of `docs2/exports/PARTNER_API_INTEGRATION_GUIDE.html`,
and the per-partner values that copy states. When behaviour changes, this is how we answer **"who is
holding a document that is now wrong?"** Without it that question is unanswerable — the guide carries a
version number and an issue date, but no record of recipients.

**This log is INTERNAL and must never be pasted into the guide.** A partner's copy listing the other
partners who received it would leak the partner roster to a competitor's channel.

## Why the stated values are recorded here, not just the revision

The guide's *Your Connection* panel is filled per partner. A revision number alone does not tell us
whether a given partner was told 200/hr or 2,000/hr, or presence-mode or three-value status. Record what
their copy actually said, so a later change can be assessed against what each partner was promised.

## Log

| Partner | Revision | Date issued | Issued by | Rate limit stated | Status mode stated | Endpoint scope stated | Notes |
|---|---|---|---|---|---|---|---|
| _(none yet)_ | | | | | | | Guide not yet issued to any partner |

## Before issuing a copy

1. **Confirm DEF-345 is deployed** and the guide's §2 BLOCKED block has been deleted. Until then the
   server throttles partner keys at 100/hr regardless of what the panel says.
2. Fill every `[HIGHLIGHTED PLACEHOLDER]`, including all seven rows of *Your Connection*.
3. Resolve or delete every red TBD block.
4. Verify the stated rate limit against the key actually issued — `PartnerApiKeys.RateLimitPerHour`
   (NULL = the configured platform default, currently 200/hr; it is **not** unlimited).
5. Verify the stated endpoint scope against that key's `AllowedEndpoints`. A key restricted to a subset
   returns `403` on the others, and §3 documents the full set.
6. Confirm the status mode. Presence-only is the only mode that exists today; a three-value mode requires
   CR-035, which is proposed and not built.
7. **Never paste a live API key into the document** — keys go by a separate secure channel.
8. Add a row above.

## When behaviour changes

On any change to rate limiting, status semantics, endpoint scope, or the response shape, read this log
first and decide **per partner** whether a re-issue is needed. A partner acting on a stale guide is worse
than a partner with no guide: they will build to a contract we no longer honour and the failure will
surface in their environment, not ours.

Relevant pending changes that would trigger a re-issue:

- **CR-035** — certificate status as a value (Valid / Expired / Revoked). Would change the *Status mode*
  row and §4 for any partner switched to it, and requires Amazon sign-off.
- **CR-036** — certification standard version. Additive field; would change §4's field table. Note the
  frozen-baseline contract test reports any added field as a contract change — re-baseline deliberately.
- **DEF-345** — partner rate limiting. Fixed locally, **not yet deployed**; gates the first issuance.
