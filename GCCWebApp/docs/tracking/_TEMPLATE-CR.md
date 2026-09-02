# CR-XXX — <one-line title, the change stated as an outcome>

> **Copy this file to `docs/tracking/CR-XXX-short-slug.md` and delete these two lines.**
> Process: `docs/governance/change-request-process.md`. Re-grep the register for the next free number.

**Status:** Triage | Estimated | Sent to client | Approved | Declined | Deferred | Absorbed | Built | Delivered
**Raised:** YYYY-MM-DD (source: QC #N / client email / call / internal)
**Baseline:** Phase N baseline, section — *the line this request is measured against*
**Precedent:** [[CR-XXX]] — *a prior CR with the same shape, if one exists; copying beats re-deriving*

---

## 1. The ask — verbatim

Quote the request exactly as received. Do not paraphrase it into what you think it means; the
paraphrase is §3's job, and keeping them separate is what lets you notice you guessed wrong.

> "…"

Who said it, when, in what channel.

## 2. Discovery — what already exists

What the system does today in this area, with file and line references. This section is the
evidence for §4, and it is the part that most often shrinks the CR.

**Do this before estimating and before deciding.** Three of Phase 1's most valuable CR findings
were that the ask was much narrower than it read.

## 3. What is actually being asked

Where §1 is ambiguous, separate the readings. If several different situations are being described
as one thing, table them — this is the single highest-value move in CR triage:

| What was observed | How often | Example | Finding |
|---|---|---|---|
| | | | Already works — out of scope |
| | | | **This CR** |
| | | | Data correction, not code |

## 4. Why this is a CR, not a defect

Point at the baseline. Name the section. If the baseline covers it and the system does not do it,
stop — this is a defect, close this document and open a `DEF-XXX` row instead.

| Layer | Baseline says | System does |
|---|---|---|
| | | |

## 5. The change

### Before
### After
### Out of scope — and why

Be explicit here. The out-of-scope list is what prevents the CR growing between approval and build.

## 6. Impact on things the client will worry about

Answer these even when the answer is "none" — especially when the answer is "none", because
that is the cheapest reassurance available and it is what shrinks the client's perceived risk.

| Area | Impact |
|---|---|
| **Partner / Amazon API contract** | *(none? say so explicitly — no sign-off, no re-certification)* |
| Existing records | *(migration/backfill needed? manual re-entry?)* |
| Existing import/export files | |
| Access control and data isolation | |
| Certificate workflow and statuses | |
| Sorting, defaults, saved behaviour | |

## 7. Effort

**Estimate: N days.** A CR document without a number in this section is not finished.

| Item | Days | Notes |
|---|---|---|
| Build | | |
| Verification and regression | | *typically ≈ the build; do not omit* |
| Migration / backfill of existing records | | |
| Documentation + client one-pager | | |
| Deployment and post-deploy re-verification | | |
| **Total** | | |

**Value:** N days × agreed rate = *(amount)*
**Charging:** Bill / **Absorb** — if absorbed, the value above is still recorded. Absorbed is a
priced concession, not an absence.

## 8. Alternatives considered

Including the free one. Most CRs have a cheaper version that is 80% as good, and the client is
entitled to choose it. Name it even if you recommend against it.

| Option | Cost | Assessment |
|---|---|---|
| **A —** | | *(recommended)* |
| B — | | |
| Do nothing | 0 | *what the client lives with* |

## 9. Decisions needed from the client

Numbered, closed questions. Each one should be answerable in a sentence.

1.
2.

**The question that decides whether to build at all:** *state it here, at the top of the client's
mind.* Some CRs close for free on this question alone.

## 10. Success criteria — how the client verifies it

Written so the client can check it themselves, without us present.

- [ ]
- [ ]

## 11. Risk and test impact

What could regress; which existing suites cover it; what new coverage is needed.

---

## Gate — nothing below this line starts until the block above is complete

| Step | Done | Date |
|---|---|---|
| Numbered, register row created | ☐ | |
| §1–§9 complete, estimate present | ☐ | |
| Client one-pager generated and sent | ☐ | |
| **Decision received and recorded** | ☐ | |

**Decision:** Approved / Declined / Deferred to Phase N / Absorbed
**Decided by:** *(name, verbatim wording if directed informally)* **on** YYYY-MM-DD

---

## 12. Implementation record *(filled in after the gate)*

### Files changed
### What was built
### Verification — with numbers, and naming the environment

State which environment every figure came from. A number from dev reported as though it were the
client's environment has caused three corrections in this project.

### Defects found by this work
### Not yet done

---

## 13. Notes — internal only, never in the client one-pager

Commercial position, effort overruns, things learned about how the request arrived, anything
you would not put in front of the client.
