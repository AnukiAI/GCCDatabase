# User Management — internal notes

> **The procedures that used to live here are now in the Accounts & Sign-In Guide v1.1**
> (`docs/user-guide/GCC_Accounts_SignIn_Guide_v1.1.docx` / `.html`, built from
> `docs/user-guide/guide.md`). Creating a user, assigning a role, scoping to companies or
> partners, the field-by-field reference for the Add New User modal, bulk CSV import,
> two-factor setup and password reset are all covered there, with screenshots.
>
> **Edit the guide source, not this file.** This page keeps only the material that is internal
> and therefore does not belong in a document the client can forward.

Reduced to a pointer on 2026-09-22, when the same content existed in two places and had begun to
disagree. See `docs/user-guide/COVERAGE-PLAN.md`.

---

## Internal-only notes

### Security

- Passwords are hashed with BCrypt. Plaintext passwords are never stored.
- Admin actions — create, update, delete, role change — are written to the audit history.
- Never send a password by email or chat in writing. Pass it on verbally or through a password
  manager share, and require a reset on first sign-in.

### ⚠️ Pre-launch action — seed accounts

**The default seed accounts (`admin`, `analyst`, `external`, password `TestPass123!`) exist on the
development and test environments. They must be removed or re-passworded before any public
launch.**

This was the only place that requirement was written down. **It is now a gate:
`docs/tracking/pre-deploy-agent-checklist.md` — Gate 13, "Test fixture accounts are gone",
added 2026-09-22 on Shawn's instruction.**

Gate 13 is the authoritative version and covers all sixteen fixture accounts, not just the three
named above. Work from the gate; this paragraph survives only to record where the requirement
came from.

### Correction — account lockout

An earlier version of this page said a locked-out user could be freed by toggling their status
Inactive and back to Active, "because lockout is per-session". **That is not how it works.**

Lockout is time-based: five consecutive failed sign-ins set a `LockedUntil` timestamp fifteen
minutes ahead, and the account is locked while that timestamp is in the future
(`AccountController`, `User.IsLockedOut`). Toggling status does not clear it. The account unlocks
itself after fifteen minutes.

Verified against the source 2026-09-22 while writing the guide; the old instruction would have had
an administrator make two pointless edits and conclude the system was broken.
