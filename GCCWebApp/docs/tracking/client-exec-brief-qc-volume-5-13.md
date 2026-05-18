# Executive Brief — QC Log Volume & Path to Finish

**From:** Shawn West, Anuki Consulting
**Date:** May 14, 2026
**Audience:** GCC executive sponsors
**Read time:** 2 minutes

---

## What the data says

The 5/13 QC log has 170 items. Your team's own classification on the *Change vs Defect Analysis* sheet sorts them as:

- **79 actual defects** (47%) — bugs against the approved spec
- **46 lack-of-requirements** (27%) — the original spec didn't answer the question
- **45 client preference changes** (26%) — refinements after seeing the build

**Reframed:** only 65 of 170 items (38%) are pure implementation defects. The other 62% are scope-driven — requirements that emerged through the build or preferences that developed once the system was visible. Every item on the log is real; the count alone, however, doesn't tell the right story.

## Why the count looks this way — four reasons working together

1. **Requirements emerged through builds.** The originals defined the system's shape; the operational detail (analyst flow, filter behavior, partner integration, mobile read) genuinely couldn't be locked from a written spec. We built screens so your team could react, and reactions became requirements.

2. **Features arrived in waves.** Testing started before the system was complete — product taxonomy in February, status workflow in March, custom fields in April. Items logged early were sometimes resolved by later redesigns; features tested under old behavior needed re-testing under new.

3. **UAT and SIT overlapped.** Your team needed to see builds to validate requirements, which meant UAT-style scrutiny was running against a system still being integrated. A single observation often qualifies as a SIT issue, a UAT issue, *and* a requirements gap simultaneously.

4. **Every change request triggers full regression — the multiplier.** ~27 scope-change events ran through the project; only 3 were formally tracked as CRs. Each one creates new defect surface in two directions: the change itself can be wrong, and it can disturb code that was already tested and passing. A 30-minute change can require 4–6 hours of re-test. With 27 events compounding, this is the largest single driver of logged-item volume.

## What we own

- All 65 implementation defects against the spec.
- All 8 Critical-severity items in the log — zero are scope-driven.
- The decision to absorb most scope changes as "defect fixes" instead of tracked CRs. 3 of 27 was the wrong ratio.

## What we're proposing for the remaining work

1. **Design checkpoint before each CR sprint** — wireframe or written spec signed off in writing before development starts. Catches requirements-gap items before they become logged defects.
2. **Internal regression pass before every UAT release** — a defined test pass on the full system, run *in front of* UAT, not as part of it. Catches regression-cascade items before they become logged defects.
3. **Every scope-change event gets a CR record** — one-line entry, however small. Puts the regression-cost conversation on the table at the moment it's cheapest to have.

## Bottom line

The next QC log can look meaningfully different — not because we'll test harder, but because we'll structure the cycle to prevent the volume rather than absorb it. We want to bring this to a clean finish line. Let's talk about sequencing the seven approved CRs against these changes and what a realistic finish-line date looks like with them in place.

— Shawn West, Anuki Consulting

---

*Full data and walkthrough available in the companion memo (`client-memo-qc-volume-5-13.md`).*
