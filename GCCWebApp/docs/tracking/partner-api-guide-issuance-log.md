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

## Artifacts cleaned for client review — 2026-08-24

The three sendable files carried the internal scaffolding baked into the generated output, not just
into the HTML source. **The Amazon guide v1.2 was already clean; the partner template was not.** Its
PDF and DOCX both printed the red `INTERNAL — REMOVE BEFORE SENDING` banner on page 1, named
**DEF-345** three times, and exposed `CertificatesController.cs` line numbers, `PublicStatuses`, the
internal status name *Client Approved - Paid* and `localhost:5101`.

Removed 2026-08-24 from `PARTNER_API_INTEGRATION_GUIDE.html` and re-rendered to PDF/DOCX:

| Block | Disposition |
|---|---|
| Page-1 "Internal — remove before sending" | **Rewritten** as a client-facing *How to use this template* box. The instructions were genuinely useful to whoever fills the template; the internal references were not. |
| §2 "Internal — verify on production, then delete" (DEF-345) | **Deleted.** The decision it was waiting on is made — 200/hour — and the verification requirement now lives in this log instead, where it belongs. |
| §4 "Internal — confirm scope, then delete" (source paths, CR-035) | **Deleted.** Fully internal; the open question it raised is tracked as CR-035 and in `2026-08-24-client-questions-partner-api.md`. |
| §10 onboarding step 2 | **Reworded** — cited DEF-345 by number in a table the partner reads. |

Verified after regeneration: zero hits for DEF-345 / CR-035 / `CertificatesController` / `localhost` /
`PublicStatuses` / *Client Approved* / *Republished* across all three files, and **no live API key** in
any of them (all `gcc_partner_` occurrences are format examples). The 6 `[PARTNER NAME]` placeholders
are **deliberate** — it is a template.

PDFs are rendered with headless Chrome from the HTML (`Skia/PDF`, matching how the originals were
made), so the HTML remains the single source and the PDF must be re-rendered after any edit — do not
hand-edit the PDF.

## Before issuing a copy

1. **Confirm DEF-345 is deployed** and the §2 rate-limit figure has been verified against the live
   server. Until then it throttles partner keys at 100/hr regardless of what the panel says. The
   in-document warning has been removed, so **this log is now the only place that gate is recorded.**
2. Fill every `[HIGHLIGHTED PLACEHOLDER]`, including all seven rows of *Your Connection*.
3. Delete the page-1 *How to use this template* box.
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

**Rate limit — decided internally 2026-08-24 (Shawn): 200/hour stands, as both guides already state.**
No code change needed; `Program.cs:460` already sets `PartnerApiKeyLimit = 200`. Sent to the client for
confirmation alongside CR-035 and CR-036 in `2026-08-24-client-questions-partner-api.md`. Step 4 above
is therefore a *verification* against the issued key, not an open decision — but §2's internal block
still requires seeing `X-RateLimit-Limit: 200` come back from the live server before it is deleted.
