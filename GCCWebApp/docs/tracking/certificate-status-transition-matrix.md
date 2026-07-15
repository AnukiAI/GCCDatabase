	# Certificate Status Transition Matrix — Spec for Client Verification

**Status:** DRAFT — awaiting client sign-off
**Created:** 2026-07-13
**Purpose:** Document the status-transition rules currently enforced in production so the
client can validate every scenario. **No code changes** are made by this document.

---

## Background — why this exists and when it started

- The **8 certificate states** were client-defined (source of truth: `Models/CertificateStatus.cs`).
- The **transition rules below** (which state may follow which) were introduced by engineering
  on **2026-05-19 as DEF-203**, during a security audit.
- **Before 2026-05-19 there was NO enforcement** — the Edit form saved whatever status was
  picked. The rule engine (`CertificateWorkflowServic	e`) existed but no controller called it
  (dead code). DEF-203 wired it into the Edit save path.
- This is why the rule "appeared" recently. It is real, intentional, and ~8 weeks old at time
  of writing.

**Enforcement point:** `Controllers/CertificateController.cs` (Edit POST, ~line 1037) →
`CertificateStatusHelper.IsValidTransition()` in `Models/CertificateStatus.cs:150`.

> ⚠️ **Open governance question:** the 8 *states* were signed off by the client; the *specific
> allowed-transitions matrix* below appears to be an engineering assumption from the DEF-203
> fix. This document exists so the client can confirm or correct each rule.

---

## The 8 states

| # | Stored value | Display |
|---|---|---|
| 0 | `Draft` | Draft |
| 1 | `Client Approved - Not Paid` | Client Approved – Not Paid |
| 2 | `Client Approved - Confidential` | Client Approved – Confidential |
| 3 | `Client Approved - Paid` | Client Approved – Paid |
| 4 | `Hold` | Hold |
| 5 | `Expired` | Expired |
| 6 | `Republished` | Republished |
| 7 | `Expired - No Annual Verification` | Expired – No Annual Verification |

Publicly visible states: **Client Approved – Paid** and **Republished** only.

---

## Allowed transitions (per current from-state)

| From | Allowed next states |
|---|---|
| **Draft** | Not Paid · Confidential · Paid · Hold |
| **Client Approved – Not Paid** | Paid · Confidential · Hold · Expired |
| **Client Approved – Confidential** | Paid · Hold · Expired |
| **Client Approved – Paid** | Hold · Expired · **Republished** · Expired–No Annual Verification |
| **Hold** | Draft · Not Paid · Confidential · Paid |
| **Expired** | Republished · Draft |
| **Republished** | Expired · Expired–No Annual Verification · Hold |
| **Expired – No Annual Verification** | Draft |

---

## Full scenario grid (verify EVERY cell)

✓ = allowed today &nbsp;·&nbsp; ✗ = blocked today &nbsp;·&nbsp; — = same state (n/a)

| From ↓ \ To → | Draft | Not Paid | Confid. | Paid | Hold | Expired | Repub. | Exp-NAV |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| **Draft**            | — | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ |
| **Not Paid**         | ✗ | — | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ |
| **Confidential**     | ✗ | ✗ | — | ✓ | ✓ | ✓ | ✗ | ✗ |
| **Paid**             | ✗ | ✗ | ✗ | — | ✓ | ✓ | ✓ | ✓ |
| **Hold**             | ✓ | ✓ | ✓ | ✓ | — | ✗ | ✗ | ✗ |
| **Expired**          | ✓ | ✗ | ✗ | ✗ | ✗ | — | ✓ | ✗ |
| **Republished**      | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | — | ✓ |
| **Exp-NAV**          | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | — |

**The scenario that triggered this review:** `Draft → Republished` = ✗ (blocked). Republished
is only reachable from **Paid** or **Expired**. This is a rule to confirm, not a bug.

---

## Documented exceptions (already in code)

These bypass the matrix on purpose — confirm they are still desired:

1. **Save-time auto-expire (DEF-171):** if a cert's `ExpirationDate` is in the past when saved,
   status is forced to **Expired** regardless of the matrix.
2. **Expiry-correction revert (DEF-285 / QC #188):** if a currently-**Expired** cert has its
   `ExpirationDate` moved back to today/future, the system restores the status it held *before*
   it auto-expired (from history), bypassing the Expired→{Republished,Draft} limit.
3. **Automated background expiry:** a nightly job flips past-date active certs to **Expired**.

---

## Roles

- **Analyst / Admin:** may attempt any state (subject to the matrix above).
- **External users:** may only ever set **Draft** (creation-only).

---

## Known UX issue (separate from the rules themselves)

When a save is rejected, nothing is persisted (the cert keeps its real status), **but** the
Edit dropdown stays on the invalid choice. Re-saving fails again — feels like a lock-out.
Workaround: reset Status to its true stored value and save. *(Candidate fix, if approved:
snap the dropdown back and list allowed next states inline.)*

---

## Client sign-off

For each from-state, mark **Confirm** (rules are correct) or note the change wanted.

| From-state rule set | Confirm ✔ | Requested change / notes |
|---|:--:|---|
| Draft → Not Paid · Confidential · Paid · Hold | ☐ | |
| Not Paid → Paid · Confidential · Hold · Expired | ☐ | |
| Confidential → Paid · Hold · Expired | ☐ | |
| Paid → Hold · Expired · Republished · Exp-NAV | ☐ | |
| Hold → Draft · Not Paid · Confidential · Paid | ☐ | |
| Expired → Republished · Draft | ☐ | |
| Republished → Expired · Exp-NAV · Hold | ☐ | |
| Exp-NAV → Draft | ☐ | |
| **Draft → Republished should be allowed?** | ☐ Yes ☐ No | |
| Exceptions 1–3 (auto-expire behavior) correct? | ☐ | |

**Signed:** _______________________  **Date:** ____________
