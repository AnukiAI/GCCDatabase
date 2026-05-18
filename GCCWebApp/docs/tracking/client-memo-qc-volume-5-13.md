# A note on quality, timing, and how we get to a clean finish line

**From:** Shawn West, Anuki Consulting
**Date:** May 14, 2026
**Re:** The QC log — what it's telling us, and what we're doing about it

---

Hi team,

I want to take a step back from the day-to-day defect cadence and talk about the bigger picture — because I know the volume of items showing up in QC is frustrating, and I want to address it directly. We've now run a careful analysis of the 5/13 log together, and I think there's a useful story in the numbers that helps explain what we're all seeing.

This memo walks through what the data shows, why the cycle has produced this shape, and what we're proposing to change for the remaining work so the next phase feels meaningfully different. None of what follows is meant to deflect — there are things we own without qualification, and I'll name them clearly.

---

## What the 5/13 log actually shows

Your team's own classification on Sheet *Change vs Defect Analysis* puts the 170 items into three buckets:

| Bucket | Count | Share |
|---|---:|---:|
| Actual defect — bugs in the code we built | 79 | 47% |
| Lack of requirements — the spec didn't answer this | 46 | 27% |
| Client preference change — refinements after seeing the build | 45 | 26% |

When you read it more strictly, only **65 of the 170 (38%)** are pure implementation defects against a clear spec. The other 105 reflect either questions the original requirements didn't anticipate or preferences your team developed once you could see the system in motion.

That distinction isn't an attempt to discount the count — every entry on that log is real, and your team's time spent on each of them is real. But it matters because the two halves of the log were produced by different mechanisms, and only one of them is fixed by *"test more before you ship."* The other needs a different cycle shape, and that's the conversation worth having for the work that's left.

---

## Why this volume showed up — four reasons, working together

### 1. Requirements emerged through builds, not on paper

The original requirements documents covered the high-level shape of the system well — auto-numbered certificate IDs, the status workflow, dashboard metrics, role mapping. What they couldn't fully define was the operational detail: what an analyst's daily flow feels like, how filters behave on the public listing, what the partner integration needs end-to-end, how cert detail reads on a phone.

Those decisions genuinely couldn't be locked from a written spec alone — your team needed to *see* a screen working before knowing what it should do. We built screens so the business could react. The reactions then became the requirements. That's a legitimate way to discover requirements; it's just expensive to run inside a testing cycle, and it shows up in the log as the 46 "lack of requirements" items — questions and clarifications about behavior the original spec didn't pin down.

### 2. Features arrived in waves — testing started before the system was complete

Standard testing assumes the system under test is feature-complete on Day 1. UAT then runs the user journeys end-to-end against a stable build.

Our engagement didn't have that shape. Features landed across many sprints — the three-level product taxonomy in February, the status workflow rewrite in March, custom fields on the analyst table in April, and so on. Each release was forward progress, but it also meant **testing started before the system was complete**. Some items your team logged against the February build were genuinely resolved by the March redesign. Some features that worked correctly under the original status workflow needed re-testing once the workflow itself changed. That's part of why the same area can appear multiple times in the log — the surface underneath the test moved between cycles.

### 3. UAT and SIT overlapped

System Integration Testing (does it all hang together?) and User Acceptance Testing (can the business actually use it?) are normally sequential — SIT first, UAT after — because UAT defects are expensive to surface when the system isn't yet fully integrated.

We didn't have the luxury of running them sequentially here. Your team needed to see builds to validate the requirements; the only way to give you something to see was to put the system in front of you; the moment it was in front of you, you were already running UAT-style scrutiny against it. That compresses the defect-finding cycle, and it's part of why the same observation can appear classified differently across the log — a single item could legitimately be a SIT issue, a UAT issue, *and* a requirements gap. The breadth of classifications on the 5/13 log (76 Bug, 42 UX, 27 Question, 17 Enhancement, 8 CR) is the signature of an engagement where SIT and UAT happened in parallel.

### 4. Each change request triggers full regression — and regression surfaces more items

This is the part I want to spend the most time on, because it's the multiplier that explains the most about the volume.

There have been roughly **27 scope-change events** across the project. Only three were formally tracked as Change Requests at the time — the rest were absorbed as "defect fixes" or "build items" inside the sprint where they came up. The largest examples are ones we've all lived through together:

- **The status workflow rewrite** (March) — moving from 14 states to 8. That single decision touched 25+ files: every API endpoint, the certificate create and edit forms, the dashboard, exports, repository statistics, the import service, the validation service, the ETL pipeline, view models, plus a SQL migration to convert existing records.
- **The three-level product taxonomy** (February) — moving from flat categorization to Category → Type → Subcategory. Data model change plus the cert create/edit flow.
- **Custom Fields system** — admin-creatable certification fields weren't in the original spec. Required an admin CRUD page, dynamic rendering on cert detail, and later extension to the table view, exports, and Partner API.
- **Partner API contract compliance** (April) — six endpoints rewritten to match the Amazon contract: status must always read "Active" for partners, public-status filtering, percentage formatting for results, and so on.

Every one of those is correct work. None of them are wrong. But each one carries a property that the original timeline didn't price in:

> **Every change request, however small, creates new opportunity for defects in two directions.**
>
> First, the change itself can be wrong — the new code has its own bugs.
> Second — and this is the bigger one — **the change can disturb code that was already tested and passing.** Touching the status workflow meant the dashboard math, the import service, the export service, the ETL pipeline, every API caller, and the JavaScript badge mappings all needed re-verification. A test that passed in February might fail in March not because the test was wrong, but because the world underneath it moved.

In practical terms, a 30-minute code change can require 4–6 hours of re-test across the surface it touches. With ~27 scope-change events, that regression cost compounds — and each regression pass is another opportunity for previously-working code to surface a defect under fresh scrutiny.

The 79 "actual defect" items in the log are a mix of *original-build bugs* and *bugs that surfaced during regression after a change request landed*. The log doesn't distinguish them, but the second category is structurally unavoidable in an engagement with this many scope-change events.

---

## What we own, plainly

I want to be specific about what's on us, before I talk about what we're proposing:

- **The 65 implementation defects against the approved spec are ours.** The dashboard math errors, the form bugs, the 14-state workflow we built when the spec actually called for 7 states, the status-leak items on the Partner API — those are all things we should have caught with more rigorous internal review and a pre-UAT pass.
- **All 8 of the Critical-severity items in the log are real defects** — none are scope-driven. We own those without qualification.
- **We should have pushed harder for written Change Requests.** Three out of twenty-seven was the wrong ratio. Going forward, every scope-change event gets a CR record, however small. That puts the regression-cost conversation on the table at the moment it's cheapest to have, not after it's already affected the timeline.

---

## What we're proposing for the work that's left

The seven CRs approved on 4/29 (CR-008 through CR-014) plus anything new that comes up — we structure the cycle differently:

**1. A design checkpoint before development starts on any CR.**
Wireframe or written spec reviewed jointly, with your team's sign-off in writing before the implementation sprint begins. This is where we catch the "requirements gap" items before they become logged defects. It feels slower; it's actually significantly faster end-to-end, because it stops the build-react-rebuild loop.

**2. A regression pass before every UAT release.**
A defined internal test pass on the full system before any build goes back to your team — not as part of UAT, but as a gate *in front of* UAT. This is where we catch the regression-cascade items before they become logged defects. It's the single biggest lever on the next QC log.

**3. Every scope-change event gets a Change Request record.**
A one-line CR entry with a statement of what's changing and why, even for small items. This creates a paper trail that lets us have honest conversations about cost and timeline at the moment the change is being considered, not after it's already produced a wave of regression items.

I think these three changes — design sign-off, internal regression pass, and tracked CRs — will produce a meaningfully different QC log on the next cycle. They're structural, not promises.

---

## Closing

You've been incredibly patient through a build cycle that ran longer than any of us wanted. The QC log is the most visible artifact of that, and I understand why the count itself feels like a quality signal. The data says something more specific — that this is an engagement where requirements and build happened together, and the log is partly what that looks like.

That's not an argument that the count is fine. It's an argument that the count alone doesn't tell the right story, and that the right response isn't *"test harder"* — it's the three structural changes above. We want to bring this to a clean finish line as much as you do. Let's talk about how we sequence the remaining CRs against those changes, and what a realistic finish-line date looks like with the new cycle in place.

Happy to walk through any of this in more detail, including the backup data behind the classifications.

— Shawn West
Anuki Consulting

---

## Appendix — the numbers

**Classification × Reason (5/13 log)**

|  | Actual defect | Lack of requirements | Client preference change | Total |
|---|---:|---:|---:|---:|
| Bug | 65 | 11 | 0 | **76** |
| UX | 9 | 1 | 32 | **42** |
| Question | 1 | 24 | 2 | **27** |
| Enhancement | 2 | 8 | 7 | **17** |
| CR | 2 | 2 | 4 | **8** |
| **Total** | **79** | **46** | **45** | **170** |

**Severity profile**

| Severity | Count | Notes |
|---|---:|---|
| Critical | 8 | All 8 are real implementation defects — Anuki owns these fully |
| Major | ~62 | Mix of defects (48), requirements gaps (~11), and scope changes (~2) |
| Minor | ~46 | Mostly requirements gaps and preference iterations |
| Cosmetic | ~54 | Largely preference and polish items |

**Scope-change events through the project**

| Type | Count |
|---|---:|
| Formally tracked CRs (CR-005, CR-006, CR-007) | 3 |
| Earlier CRs (CR-001–CR-004), partially recovered | 4 |
| Scope changes absorbed as defect fixes | ~13 |
| Newly approved CRs in flight (CR-008–CR-014) | 7 |
| **Total scope-change events** | **~27** |
