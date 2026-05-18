# QC Log — Why Defect Volume Looks the Way It Does

**Prepared:** 2026-05-14
**Source:** Database QC Log_5.13.26sw.xlsx — Sheet "Change vs Defect Analysis" (170 items, fully classified by the client) + project tracking history
**Use:** Narrative for the client conversation about timeline, quality, and where the defects come from. Paste-ready for an email or share-back doc.

> **Companion docs:**
> - `qc-log-client-talking-points.md` — verbal-conversation script (open with ownership, then forward proposal)
> - `change-request-register.md` — full CR history (~27 scope-change events; only 3 formally tracked)
> - This doc — the *written* explanation: how the approach produced this defect shape

---

## What the 5/13 log actually shows

Of the **170 items** in the log, the client's own classification (Sheet *Change vs Defect Analysis*) says:

| Reason | Count | % | What it really is |
|---|---:|---:|---|
| Actual defect | 79 | 47% | Implementation bugs against the approved spec |
| Lack of requirements | 46 | 27% | Spec gaps — the original docs didn't answer the question |
| Client preference change | 45 | 26% | Iteration on builds the client saw and refined |

**Reframed:** Only **65 of 170 (38%)** are pure implementation defects — items where we built something incorrectly against a clear spec. The other **62%** are scope-driven: requirements that emerged through the build, or preferences the client refined after seeing it work.

That distinction matters for the story below, because the two halves of the log were produced by **two different mechanisms** — and only one of them is fixed by "test more before you ship."

---

## The story — four structural drivers

The defect volume isn't a single quality problem. It's the predictable output of how the project was structured. Four drivers, each one feeding the next.

### 1. Requirements emerged through builds, not upfront

The original requirements documents (archived in `docs/Final Req Docs/archive/`) defined the high-level shape — auto-numbered certificate IDs, a 7-status taxonomy, dashboard metrics, role mappings. They did not specify the operational details: what the analyst's daily workflow looks like, how filters behave on the public listing, what fields the partner integration needs, how the cert-detail layout reads on mobile.

Those details were genuinely unknowable from a written spec — the business needed to *see* a screen working before they could say what it should do. We built screens so the business could react. The reactions then became the requirements.

This is visible in the log as the **46 "Lack of requirements" items** — questions and clarifications about behavior the original spec didn't answer. The largest single bucket inside that 46 is *Questions* (24 of 27 Question-classified items) — items where the client read the build, asked "is this right?", and the spec didn't have an answer.

### 2. Functionality arrived incrementally — not all delivered at the start of testing

Standard testing process assumes the system under test is feature-complete when testing begins. UAT runs the user journeys end-to-end against a stable build.

That wasn't the shape of this engagement. Features landed across many sprints — the 3-level product taxonomy was added in February 2026, the status workflow was redefined from 14 states to 8 states in March, custom fields became visible on the analyst table in April. Each delivery added new surface to test, but it also meant **testing started before the system was complete**. Items the client logged in February as defects against the original taxonomy were later resolved by the March taxonomy redesign — and items that worked correctly under the original 14-state workflow had to be re-tested under the 8-state model.

This is one reason the log has so many entries: a single underlying decision (e.g. status taxonomy) generated rounds of testing observations as the system shifted underneath it.

### 3. UAT and SIT overlapped

System Integration Testing (SIT) verifies that components work together; User Acceptance Testing (UAT) verifies the business can use the system to do real work. They are normally sequential — SIT first, UAT after — because UAT defects are expensive to produce when the system isn't yet integrated.

Here they ran in parallel. The client needed to see the system to validate the requirements; the only way to give them something to see was to put builds in front of them; the moment a build was in front of them, they were running UAT-style scrutiny against it. We didn't have the option to complete SIT in isolation first — the business need was visible progress.

This compresses the defect-finding cycle. A single observation could legitimately be classified as a SIT defect (integration didn't work right), a UAT defect (workflow felt wrong), or a requirements gap (spec didn't say what should happen) — and very often it was *all three* in different proportions. The log reflects that: 76 items are classified Bug, 42 UX, 27 Question, 17 Enhancement, 8 CR — that breadth of classification is what overlapping SIT/UAT produces.

### 4. Each Change Request triggers full regression — and regression surfaces more defects

This is the multiplier. There have been roughly **27 scope-change events** across the project (full inventory in `change-request-register.md`). Most weren't formally tracked as CRs — they were absorbed as "defect fixes" or "build items" during the sprint. The largest examples:

- **Status workflow redefinition** (March 2026) — 14 states → 8 states. Touched 25+ files: every API controller, the certificate create/edit forms, dashboard, export, repository statistics, JavaScript badge mappings, validation service, import service, filter service, ETL pipeline, view models, plus a SQL migration to convert existing records.
- **Product taxonomy redesign** (February 2026) — flat categorization → 3-level Category → Type → Subcategory hierarchy. Data model change plus UI work across the entire cert create/edit flow.
- **Custom Fields admin system** — admin-creatable certification fields didn't exist in the original spec. Required a full admin CRUD page, dynamic rendering on cert detail, then (CR-007, April 2026) extension to the analyst table view, then (CR-012/013/014, approved April 29) extension to exports and the Partner API.
- **Partner API contract compliance** (April 2026) — six partner-facing endpoints rewritten to enforce the Amazon contract: status must be the literal "Active" for partners, public-status filtering required, percentage formatting for results. None of these requirements were in the original API spec.

Every one of those changes is correct work for the project. None of them are "wrong." But each one has a property the original timeline didn't account for:

> **Every change request, however small, creates new surface for defects in two directions.**
>
> 1. **The change itself can be wrong** — the new code has its own bugs (e.g. DEF-103, where the Partner API status-filter change shipped with two of six endpoints still leaking internal status values).
> 2. **The change can break code that was already tested and passing** — touching the status workflow meant the dashboard math, the import service, the filter service, the export service, every API caller, and the ETL pipeline all needed re-verification. A test that passed in February might fail in March not because the test was wrong, but because the world underneath it moved.

**This is what "full regression" means in cost terms.** A 30-minute code change can require 4–6 hours of re-test across the surface it touched. With 27 scope-change events, the regression cost compounds — and every regression pass is another opportunity for previously-working code to surface a new defect under fresh scrutiny.

The 79 "Actual defect" items in the log are a mix of *original-build bugs* and *bugs that appeared during regression cycles after a CR landed*. The log doesn't distinguish them, but the second category is structurally unavoidable in an engagement with this many scope-change events.

---

## The math

A simplified illustration of why volume looks the way it does:

```
Original scope:               ~100 hours of dev → ~30 defects expected at standard quality
+ Discovery-via-build:        Adds requirements gaps that get logged as defects (LoR bucket)
+ UAT/SIT overlap:            Same observation gets logged 1-3 times under different categories
+ Incremental delivery:       Same area re-tested 2-4 times as features land alongside it
+ ~27 scope-change events:    Each one triggers regression across already-tested surface
                              → Each regression pass surfaces 5-15% of the touched surface
                              → 27 events × regression rate = significant cumulative add
─────────────────────────────────────────────────────────────────────────────
Observed:                     170 logged items, 65 of which are pure original-build defects
```

A different way to read the same data: of the 105 non-pure-defect items, **45 are client-preference iterations (the build-and-react cycle working as designed)** and **46 are requirements gaps that needed a build to surface them** and **the remaining items sit in between**. None of those 91 would have appeared with the original timeline's *"finalize requirements → build to spec → test → ship"* sequence, because that sequence assumes requirements are complete at the start. They weren't.

---

## What we own, plainly

- The 65 implementation defects against the approved spec. These are ours. The dashboard math errors, the form bugs, the 14-state workflow we built when the spec actually called for 7 states (corrected in March), the status-leak bugs in the Partner API — all of these we should have caught with more rigorous design review and a pre-UAT regression pass.
- All 8 Critical-severity items in the log are real defects. Zero are scope-driven. We own those without qualification.
- The compounding effect from absorbing scope changes as "defect fixes" instead of formal CRs. We should have been pushing for written CR sign-off at every scope-change event, not just at the obvious ones. Only 3 of ~27 scope changes got that treatment.

## What the data is asking us to look at structurally

- The defect *count* doesn't tell the quality story — the **distribution** does. 38% pure defects, 27% requirements gaps, 26% preference iterations is the signature of an engagement that did requirements discovery during build, not before it.
- That distribution isn't fixed by testing harder. Testing harder catches the 38%. The other 62% is structural — it requires a different cycle shape.
- The cycle shape that produces a different distribution: **lock design before each sprint starts**, **run a regression pass before each UAT release**. Both got squeezed under the visible-progress pressure of the original timeline.

---

## Forward proposal

For the work that's left — the 7 CRs approved on 2026-04-29 (CR-008 through CR-014), plus any future scope-change events — we structure the cycle differently:

1. **Design checkpoint before development starts on any CR.** Wireframe or written spec reviewed jointly with the client; the client signs off in writing before the implementation sprint begins. This catches the requirements-gap items before they become logged defects.

2. **Regression pass before every UAT release.** A defined set of tests run on the full system before the build goes back to the client — not as part of UAT, but as a gate *in front of* UAT. This catches the regression-cascade items before they become logged defects.

3. **Every scope-change event gets a CR record, however small.** Three out of twenty-seven was the wrong ratio. Going forward, anything that changes scope gets a CR-XXX entry with a 1-line statement of what's changing and why. That puts the regression-cost conversation on the table at the moment it's cheapest to have.

The 170-item log is what we have. The number that matters is what the *next* QC log looks like — and that's the only argument worth making.

---

## Appendix — by-the-numbers tables for reference

### Classification × Reason cross-tab (5/13 log)

|  | Actual defect | Lack of requirements | Client preference change | **Total** |
|---|---:|---:|---:|---:|
| Bug | 65 | 11 | 0 | **76** |
| UX | 9 | 1 | 32 | **42** |
| Question | 1 | 24 | 2 | **27** |
| Enhancement | 2 | 8 | 7 | **17** |
| CR | 2 | 2 | 4 | **8** |
| **Total** | **79** | **46** | **45** | **170** |

### Severity × Reason (from "Work Item Status" sheet)

| Severity | Actual defect | Other reasons | Total |
|---|---:|---:|---:|
| Critical | 8 | 0 | **8** |
| Major | 48 | ~14 | **~62** |
| Minor | 17 | ~29 | **~46** |
| Cosmetic | 6 | ~48 | **~54** |

### Scope-change events ("how many CRs were there really?")

| Type | Count |
|---|---:|
| Formally tracked CRs (CR-005, CR-006, CR-007) | 3 |
| Backfilled CR slots (CR-001–CR-004), subjects partially recovered | 4 |
| Undocumented scope changes that should have been CRs | ~13 |
| Newly approved CRs in flight (CR-008–CR-014, approved 2026-04-29) | 7 |
| **Total scope-change events through the project** | **~27** |

Full register: `docs/tracking/change-request-register.md`.
