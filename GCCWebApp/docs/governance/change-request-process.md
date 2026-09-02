# Change Request Process — Phase 2 onward

**Owner:** Anuki Consulting · **Applies from:** the Phase 2 baseline (see Part A)
**Templates:** `docs/tracking/_TEMPLATE-CR.md` (internal) · `docs/tracking/_TEMPLATE-CR-client-onepager.md` (client)

---

## The one failure this exists to prevent

> Work gets **built** before anyone decides whether it is **billable**.

That is the whole problem, stated plainly. Everything below is machinery for putting a decision
in front of a build.

Phase 1 has 42 numbered CRs and a register section literally titled *"Undocumented work that
should have been formal CRs."* The pattern was never that scope changes went unnoticed — CR-037's
own document says, in §4, *"Ask this before anything below is estimated."* The pattern is that the
question got asked, the answer was slow, and the build happened anyway because building felt
cheaper than waiting.

So this process has exactly one hard rule and the rest is paperwork to support it:

**No build starts on a CR-classified request until it has a number, an estimate, and a recorded
decision. "Absorbed" is a valid decision. Silence is not.**

---

# Part A — The phase boundary (once per phase)

Run this once, before any Phase 2 development. It takes a day and it is the highest-leverage
day in the phase.

## A1. Write the baseline

The baseline is the artifact that makes "is this a CR?" answerable. Without one, every argument
about scope is two people recalling different conversations.

Produce `docs/governance/phase-2-baseline.md` containing:

| Section | Content |
|---|---|
| **In scope** | Every deliverable in the fixed price, itemized to the feature level — not "admin screens" but each screen |
| **Explicitly out** | Things discussed and *not* included. This section is the one that saves you |
| **Assumptions** | What the price assumes about data, environments, third parties, client response time |
| **Baseline date + version** | Everything after this date is measured against it |

Two rules learned the expensive way:

- **Itemize admin surface at proposal time.** Phase 1's admin scope — user management, role
  management, product types, certificate types, certificate fields, partner API access, tags,
  partners, dashboard analytics — grew well past kickoff scope because it was priced as
  "and we'll figure out the admin later."
- **Name the delivery model in writing.** Phase 1's core friction was not technical: the client
  expected a finished product handed over; the build was iterative. Neither side named it. Put
  one sentence in the baseline saying which one Phase 2 is, and what the client will see at each
  checkpoint.

## A2. Fix the commercial terms

These are contract terms, not process notes. They go in the agreement, not just this file.

1. **Discovery is a separate, billed deliverable** with its own sign-off gate before development
   starts. Phase 1 absorbed ~8 months of design/wireframe alignment inside the build container.
2. **Staffing is stated.** If the price assumes one person, the timeline is calibrated to one
   person and the client is told so. If the client is buying team velocity, the price staffs a team.
3. **CR rate and turnaround are pre-agreed** — a day rate and a "we respond to a CR within N
   business days" commitment, so pricing a CR is arithmetic rather than a negotiation each time.
4. **A standing absorption allowance**, if you want one: e.g. "up to X days per phase absorbed at
   Anuki's discretion for small courtesies." This makes goodwill a visible, bounded budget instead
   of an unlimited silent one.

## A3. Gate

Development does not start until the baseline is signed. That signature is what converts every
later request into either "in the price" or "a CR" — a five-second answer instead of an argument.

---

# Part B — Per-request triage (every request)

Every incoming item — QC log row, email, call, hallway comment — gets classified before it gets
worked. Classification takes minutes; misclassification costs days.

## B1. The five buckets

These are the buckets already in use in the QC log. Do not invent new ones.

| Bucket | Test | Billable? | Artifact |
|---|---|---|---|
| **Bug** | Baseline says X, system does Y | No — fix it | `DEF-XXX` row |
| **Data** | Code is right, a stored value is wrong | Usually no; a bulk correction may be | Data-ops script + note, no DEF row |
| **Question** | Needs an answer, not a change | No | Reply; log if it reveals a gap |
| **UX** | Works as specified, specification is awkward | Judgement — small polish no, redesign yes | DEF or CR per size |
| **CR** | Baseline does not cover it | **Yes — price it** | `CR-XXX` doc + register row |

**The one test that settles most arguments:** *point at the line in the baseline that this
request says was not delivered.* If you can, it's a Bug. If you cannot, it's a CR. "The client
assumed it" is not a line in the baseline.

Two traps this project has already hit:

- **New requirements arrive dressed as defects.** A single QC log entry once carried 44 items, of
  which 39 were new requirements, not defects. Triage per *item*, never per log row.
- **Correct code + wrong stored data is not a defect.** It is a data task, and if it is a bulk
  correction to business records it needs its own CR before anything is written.

## B2. The gate

Once an item is classified **CR**, stop. In order:

```
  1. NUMBER      Next free CR-XXX. Re-grep the register; never trust a remembered high-water mark.
  2. DOC         Copy _TEMPLATE-CR.md → docs/tracking/CR-XXX-short-slug.md. Fill §1–§6.
  3. ESTIMATE    A number, in days. A CR doc without one is not finished.
  4. ─── STOP ───  Nothing is built past this line.
  5. ONE-PAGER   Generate the client page from _TEMPLATE-CR-client-onepager.md. Send it.
  6. DECISION    Approved / Declined / Deferred / Absorbed. Recorded in the register, with a date.
  7. BUILD       Only now.
```

**Discovery is allowed above the line; implementation is not.** Reading the code to find out what
a change would touch is how you produce a defensible estimate, and it is not billable-scope work.
Writing the feature is. If you find yourself "just trying something to see if it's hard," you have
crossed the line — that is what happened on CR-037.

## B3. When the gate may be skipped

Rarely, and always recorded:

- **Production is down or data is at risk.** Fix it, then write the CR retroactively the same day
  with the decision recorded as "built under incident, priced after."
- **The client explicitly directs a build before pricing.** Allowed — but record *who* directed it
  and *when*, verbatim, in the CR doc. CR-037's register entry does exactly this, and that is the
  one thing it got right.

---

# Part C — Pricing, including the zero-cost case

**This is the part that changes Phase 2's economics.**

## C1. Absorbed is a price, not an absence

A CR you choose not to bill is still priced, then discounted to zero, and logged:

| Field | Example |
|---|---|
| Estimated effort | 2.5 days |
| Rate | *(agreed day rate)* |
| Value | *(effort × rate)* |
| **Charged** | **£0 / $0 — absorbed** |
| Reason | Goodwill, late in phase, small relative to relationship |

Do this and by the end of the phase you have a single defensible number: **total value absorbed.**
That number is the strongest thing you can put in front of a client at a phase renewal or a
scope dispute — it is concrete, it is itemized, and it was agreed line by line at the time.

Absorb work silently and that number is zero, no matter how much you gave away. Phase 1's absorbed
CRs are, financially, indistinguishable from work that never happened.

## C2. Estimate honestly, including the invisible half

Phase 1's estimates were good on build and blind on everything around it. A CR estimate covers:

- build
- **verification** — CR-037's testing found two real defects and cost roughly as much as the feature
- regression checks on adjacent surfaces
- migration/backfill for existing records
- documentation and the client one-pager
- deployment and post-deploy re-verification

If an estimate is only the build, it is wrong by roughly half.

## C3. Say what it does *not* touch

Every CR one-pager carries an "unchanged" section. It is the cheapest anxiety-reduction available
and it prevents the client pricing risk you already eliminated. CR-037's genuinely valuable
commercial fact was *"the partner API is byte-identical, so no partner sign-off and no
re-certification"* — that is worth stating loudly every time it is true.

---

# Part D — Artifacts and where they live

| Artifact | Path | Audience | When |
|---|---|---|---|
| Phase baseline | `docs/governance/phase-2-baseline.md` | Both, signed | Once, before dev |
| This process | `docs/governance/change-request-process.md` | Internal | Standing |
| Internal CR doc | `docs/tracking/CR-XXX-slug.md` | Internal | At classification |
| CR register row | `docs/tracking/change-request-register.md` | Internal, canonical | At classification |
| Client one-pager | published page + `.md` source | Client, signed | Before build |
| Shipped log entry | `docs/tracking/shipped-YYYY-MM.md` | Client-readable | On delivery |

**The register is canonical.** The per-CR document is the working record; the register row is the
fact. Always re-grep both registers for the current high-water number — a remembered one has been
wrong before, and Phase 1 carries nine duplicate DEF numbers because of it.

---

# Part E — Rhythm

| Cadence | Action |
|---|---|
| **Per request** | Triage into a bucket. Minutes, not days. |
| **Weekly** | Sweep new CRs: numbered, estimated, one-pager sent? Anything sitting above the gate for over a week is either chased or declined. |
| **Per delivery** | Register status updated; shipped log appended. |
| **Per phase close** | Total value delivered, total value absorbed, count of CRs by outcome. This is the renewal conversation. |

---

## Appendix — CR-037 as the worked example

Held up deliberately, because it went right and wrong in instructive proportions.

**What it did well.** Investigated before estimating, and found the ask was ~2% of what it first
appeared — three different situations were being read as one, and only one needed code. Named the
one question that decided whether to build at all. Confirmed the partner API was untouched, which
removed the client's largest risk. Recorded the client's build direction verbatim. Tested hard
enough to find two real defects, including an export that was silently emitting incomplete data.

**What it cost.** The document said *"Ask this before anything below is estimated"* — and the
build ran anyway, roughly 2–2.5 days across 25 files, plus verification. It was then classified
zero-cost. The value was delivered and the record of that value is a sentence in a register.

**The single change that would have fixed it:** the gate in §B2 — the one-pager goes out, the
decision comes back, *then* the build starts. Same work, same possible outcome of absorbing it,
but the absorption becomes a recorded, quantified concession instead of an invisible one.
