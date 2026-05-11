# QC Log — Client Conversation Talking Points

**Prepared:** 2026-05-09
**Source:** Database QC Log_5.8.26sw.xlsx Sheet 2 (170 items, fully classified)
**Use:** Reference during client conversation about quality, timeline, and process

---

## Quick reference — the numbers

| Reason | Count | % |
|---|---:|---:|
| Actual defect (against approved spec) | 79 | 46% |
| Lack of requirements (gaps in design) | 46 | 27% |
| Client preference change (iteration on builds) | 45 | 26% |
| **Total** | **170** | **100%** |

| Severity | Count | All Critical/Major are Anuki defects? |
|---|---:|---|
| Critical | 8 | **Yes — 8 of 8 are defects, 0 LoR, 0 CPC** |
| Major | 59 | 48 defects, 11 LoR, 0 CPC |
| Minor | 46 | 17 defects, 11 LoR, 18 CPC |
| Cosmetic | 57 | 6 defects, 24 LoR, 27 CPC |

**Effort estimate for the 91 non-defect items: ~110-235 hours (3-6 weeks of dev capacity).**

---

## Opening — lead with full ownership

> *"You're right that quality wasn't where it should have been. We had 79 defects against the approved spec. Testing was thinner than it needed to be. Timeline ran longer than what we originally agreed to. I want to own all of that directly before anything else."*

**Do not** open with classification breakdowns or category percentages. The first thing the client hears is accountability.

---

## Structural explanation — only if conversation moves past the apology

> *"Looking at the data, what I see is a pattern that started early and compounded. From the first few meetings we were framed as 'behind,' but that was relative to a timeline set before the scope was actually locked. The wireframes went through eight iterations between May and July 2025 alone — each one was a rebuild against a moving target. That meant constant pressure for visible progress, and two things got squeezed every cycle: joint design reviews and thorough testing.*
>
> *That's why the QC log looks the way it does:*
>
> *- The 79 defects are mostly things more testing would have caught.*
> *- The 46 requirements gaps are mostly things joint design review would have caught earlier — most were already in the field requirements xlsx, just not surfaced at design.*
> *- The 45 preference changes are the iterating-on-the-build pattern that existed because we didn't have a stable design to test against.*
>
> *None of that excuses where we ended up. But it's why each cycle was harder than the last."*

---

## Shared accountability — split the cause without blaming

> *"This is on both sides. We agreed to early timelines that didn't account for the design discovery still ahead. We didn't push back hard enough when wireframes kept changing. We shipped builds before they were tested as thoroughly as they needed.*
>
> *On your side, the discovery cycles meant decisions were made over iterations rather than locked at design. Neither of us structured the cycle to allow real design sign-off before development."*

---

## Forward-looking proposal — the only argument worth making

> *"For the work that's left, two changes would break the cycle: a real design checkpoint with sign-off before each sprint starts, and a test pass before each UAT release. Both got squeezed under timeline pressure. If we plan for them explicitly — even at the cost of a slower-feeling cadence — the next QC log should look very different."*

---

## If pushed on specific points

### "But you have 79 defects, that's a quality problem"
> *"Yes. Some are pure code defects we should have caught — like the dashboard math errors and the form bugs. Some are defects against the spec that we should have built right the first time — like the 14-state status workflow when the spec listed 7. We own both. The question is what process change keeps the next 100 from looking the same — and that's where the design checkpoint and regression pass come in."*

### "The time and money on this has been extreme"
> *"It has been. The structural reason is that we ended up doing requirements discovery during testing instead of at design. About 110-235 hours of dev work went into clarifying requirements that came up in QC. That capacity should have been preventing defects. We didn't structure the cycles to do both."*

### "Why didn't you flag this earlier?"
> *"Honest answer: every cycle had pressure to ship visible progress, so we kept building rather than pausing for design rigor. We should have pushed back harder on that. Going forward, the design checkpoint at the start of each sprint is exactly the place to flag it."*

### "How do we know the next phase will be different?"
> *"Because we're proposing structural changes, not just promises. Design sign-off before development is a date on the calendar, not a goal. Same for the regression pass before UAT. If we don't hit them, that's visible — not buried."*

---

## Defense for specific reclassifications (if challenged)

The QC log Reason and Reason Justification columns cite specific spec rows. Strongest citations:

| ID | Cited as defect because... |
|---|---|
| 16 | Cert Upload Fields R4: auto-numbering rule explicit, all 3 spec versions identical |
| 22, 54 | Cert Upload Fields R7: 7-status taxonomy specified, Anuki built 14 |
| 23 | R7 functionality: "Database automatically moves cert to Expired when Expiration Date has passed" |
| 56 | R17: "Dropdown of preset countries" (vague — borderline) |
| 62 | R11: "Company Contact Email/URL - Open text field" |
| 67/68/70 | Dashboard Metrics R7-R20: explicit metric list with "Active Certs" qualifier |
| 106 | RoleMappings R2: User collection includes Company field |
| 135 | R7: "moves to Expired when expiration date HAS PASSED" (visible on the day) |
| 141/154/155 | Dashboard Metrics R2: "Active = Public & Confidential that have not expired" + R24-R26: "include all fields" |

**Caveat:** 3 reclassifications cite wireframes only (#65 pagination, #143 Partner role, #31 button alignment). Wireframes went through 8+ iterations — these citations are softer than the field requirements xlsx ones. If the client challenges any of these three specifically, the honest answer is *"This was specified in the wireframes, but the wireframes themselves changed multiple times — we may have built per an earlier iteration."*

---

## What NOT to say

- *"We were forced to rush"* — sounds defensive
- *"If you'd locked the requirements earlier we wouldn't have these issues"* — blame
- *"62% of the log isn't really our fault"* — undercuts ownership
- *"Scope creep made the timeline impossible"* — true but unhelpful

---

## Key principle

**Don't defend the QC count. Defend the proposal for what changes next.** The 79 defects are real. The 46 LoR items are real. The 45 CPC items are real. What you can offer is *"Here's what specifically prevents the next 100 from looking like this."* That's the conversation worth having.
