# Note to Amazon — API key rotation

**Status:** Draft for review — not sent
**Date drafted:** 2026-08-14
**Sequencing decision (Shawn, 2026-08-14):** send **after** the host cutover has completed and Amazon
has confirmed they are running against `database.greencirclecertified.com`. Do not bundle with the
host-change note — see `2026-08-14-amazon-host-change-note.md`.
**Before sending, fill:** the new key delivery channel, the overlap deadline, and the sign-off.

---

## Why this can be zero-downtime

Verified in code before drafting: keys are validated by
`FirstOrDefaultAsync(k => k.ApiKey == apiKey && k.IsActive)`
(`Services/PartnerApiKeyService.cs:93` and `:221`), and there is no constraint limiting a partner to
one active key. **A partner can therefore hold two active keys simultaneously.**

That means rotation does not need a synchronised swap. Issue the new key, let both work during an
overlap window, and revoke the old one only after Amazon confirms they have moved. Amazon is never
without a working key, so this can be scheduled at their convenience rather than ours.

---

## Email draft

**Subject:** GCC Partner API — scheduled key rotation (new key attached separately)

Hi [NAME],

Now that you are running against the new production endpoint, I would like to schedule a rotation of
your API key as a routine security measure.

**We have issued you a new key.** It is being delivered separately by [SECURE CHANNEL] rather than in
this email.

**Both keys work during the changeover.** Your existing key stays active alongside the new one, so
there is no synchronised swap and no window where your integration is without a working credential.
You can make the change whenever it suits your release schedule.

The steps on your side:

1. Update your configuration to the new key.
2. Confirm you are getting successful responses — the new key carries identical permissions, scope
   and rate limit, so nothing else changes.
3. Reply to confirm you have moved.

Once you confirm, we will revoke the old key. After that point, requests using it will return `401`.
We would like to complete this by **[DATE]**; if that window does not suit your release cycle, tell me
what does and we will work to it.

Nothing else about the integration changes — same endpoint, same paths, same payloads, same limits.

Best regards,
[SIGN-OFF]

---

## Runbook — GCC side

| # | Step | Where | Notes |
|---|------|-------|-------|
| 1 | Confirm host cutover is complete and Amazon has acknowledged | — | Gate. Do not start rotation until this is true |
| 2 | Issue a new key for the Amazon partner | Admin → Partner API Keys | Match the existing key's rate limit, allowed endpoints and permissions exactly |
| 3 | Set expiry deliberately | same | The current key was set never-expire on 2026-06-03. Consider a dated expiry on the replacement so the next rotation is forced rather than remembered |
| 4 | Deliver the new key by secure channel | — | **Never** by email body, ticket text, or embedded in a document |
| 5 | Send the note above | — | |
| 6 | Wait for Amazon's confirmation | — | Both keys active throughout; no time pressure on them |
| 7 | Revoke the old key (row Id 6) | Admin → Partner API Keys | Sets `IsActive = false` + `RevokedAt`; takes effect immediately |
| 8 | Verify the old key now returns `401` | curl against production | Falsifiable check — do not skip |
| 9 | Confirm the new key still returns `200` | curl against production | Guards against revoking the wrong row |

## Internal notes

- **The old key is compromised, not merely old.** `gcc_partner_fc8498…` was printed in plaintext in
  §2 of the v1.0/v1.1 Amazon Developer Guide PDFs, which were circulated externally. The v1.2 guide
  replaces it with a placeholder, but **every copy of the earlier PDF still carries the live key**.
  This is the reason for the rotation; the email above frames it as routine, which is a presentation
  choice, not a description of the risk.
- **Git history still contains it — confirmed, not assumed.** `docs2/exports/AMAZON_API_DEVELOPER_GUIDE.html`
  is git-tracked, and `git show HEAD:` on that path still returns the live key (the redaction is in the
  working tree, uncommitted). Removing it from the current document does not remove it from history.
  That cleanup is separate and still outstanding.
- **Three full partner keys sit in `.claude/settings.local.json`**, including this one. That file is
  **untracked and gitignored** (`.gitignore:407`), so it has not left the machine — but it is worth
  clearing when the keys are rotated rather than leaving stale live credentials on disk.
- **Smoke scripts are clean.** I checked before assuming otherwise: nothing under `Scripts/smoke-prod/`
  has the old key hardcoded, so revocation will not cause a surprise `401` in the smoke suite. The only
  places the value appears are the tracked guide, the local settings file, and these notes.
- **Do not reuse the old key value anywhere**, including in test environments or sample scripts.
