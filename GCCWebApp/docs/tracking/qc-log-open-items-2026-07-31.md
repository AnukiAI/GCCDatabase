# QC Log — Open Items Report

**Source:** `Database QC Log_7.30.26.xlsx`, sheet **GCC DB QC Log2** (header row 5, 247 data rows)
**Prepared:** 2026-07-31 | **Prepared by:** Anuki Consulting
**Status:** Triage complete. **Batch 1 (DEF-316 / 317 / 318 / 319) is now BUILT and validated on
localhost** — working tree, pending the next deploy bundle. Two further defects were found while
doing it: **DEF-320** (fixed, same batch) and **DEF-321** (logged, not fixed — see §4a). Everything
else below remains triage.

---

## 1. Headline

The log shows **58 rows with Current Status = "Open."** That number overstates the real backlog by roughly 4x.

| Bucket | Count | What it means |
|---|---:|---|
| **Stale — Open but already closed** | **46** | Row has a real close date *and* client-confirmed fixed text, but the status cell was never flipped to Complete. **No work.** |
| **Live — genuinely open** | **12** | No close date, or a close date overridden by 7/30 client text saying "Not fixed" / "New". |
| **TOTAL marked Open** | 58 | |

Of the 12 live items: **8 need work from us**, **3 are blocked on the client or are pre-launch tasks**, **1 needs only a status confirmation.**

**Estimated effort for the 8: ~26–40 hours**, but ~60% of that sits in three items whose root cause is not yet known (see §4).

---

## 2. As Is / To Be — all 8 work items at a glance

Read this table with the client. "As Is" is the behavior they are seeing today; "To Be" is the
behavior we would deliver. Where "As Is" is taken from the client's own 7/30 report rather than
confirmed by us, it is marked **(client-reported, unverified)**.

| # | As Is | To Be |
|---|---|---|
| **DEF-316** | Certificate import template Partner column header reads `Partner`. A user entering two partners has no cue that comma is the separator. | Header reads `Partner (separate by comma)`. Multi-partner entry is self-documenting in the file itself. |
| **DEF-317** | The `Enter numeric certification results (e.g., 65%, 100%)` guidance renders as helper text **below** the input. The field itself is empty grey. | Same wording renders as the **placeholder inside** the input. Wording unchanged — placement only. |
| **DEF-318** | Edit a cert → Save → navigate to Certificates → Add Certificate: a success toast for the *previous* action fires on the empty create form. Survives logout/login. | The success toast fires once, on the action that caused it, and is consumed. A blank create form shows no confirmation. |
| **DEF-319** | Type a term, move the mouse over a suggestion without clicking, press Enter → the **hovered** suggestion is committed instead of the typed text. | Hover **highlights only**. Enter commits what was typed. A value is committed only by arrow-key selection or an actual click. |
| **CR-030** | A past expiration date auto-sets status to `Expired`. Correcting the date to the future **auto-reverts** status to whatever it was before (e.g. back to `Client Approved - Paid`), silently. | Status **stays `Expired`.** An alert fires: *"Expiration date was moved to the future and certificate is marked Expired. Update status accordingly if necessary."* The user sets the status deliberately. |
| **#218** | New certificates generate 7 characters (`26-10095`) despite the database holding only ~265 certs. *(client-reported, unverified)* Year-rollover behavior has never been demonstrated to the client. | New certificates generate `YY-XXXX` (6 characters) and only widen after 9,999 certs in a year. Rollover to `27-XXXX` in 2027 is **proven** with a test artifact handed to the client. |
| **#242** | Product Type "Carly Test" cannot be deleted. The usage column reports 2 certificates, but those 2 cannot be found anywhere in the UI to reassign — so the block cannot be cleared. *(client-reported, unverified)* | The usage count and the manageable record list agree. Either the 2 records are reachable and reassignable, or the count correctly reads zero and the delete succeeds. |
| **#243** | Several Product Types exist with no parent Product Category, which the hierarchy should make impossible. *(client-reported, unverified)* | No orphaned Product Types exist, **and** the write path cannot create new ones — a Type cannot be persisted without a Category. |

---

## 3. Work items — client re-flagged on 7/30

### DEF-316 — Cert import template: Partner column heading missing "(separate by comma)"
- **QC Ref:** #171 (Reporter: client, opened 5/5/26). Image 105.
- **Client text (7/30):** *"This is still not fixed. In Excel Template please add in Partner column heading (separate by comma). I want the info in () added so it's clear when multiple."*
- **Why DEF, not enhancement:** The ask arrived 7/10 as a minor copy addition and we **marked it fixed**. It was not. The defect is against our own completion claim, not against the original spec.
- **Scope:** One header string in the certificate import template generator. The multi-select Partner capability itself (deployed 6/22) is working and confirmed by the client — only the header label is outstanding.
- **Effort:** **0.5 h** including regeneration of the template and a download check.
- **Risk:** None.

### DEF-317 — "Certification Results": instruction copy rendered below the field instead of inside it
- **QC Ref:** #240 (Reporter: client, opened ~7/10). Image 109.
- **Client text (7/30):** *"The grey instruction copy in open text field is not accurate language. I wanted the copy provided added there not under box."*
- **Requested copy:** `Enter numeric certification results (e.g., 65%, 100%)` — as the **placeholder inside the input**. Label stays `(Required for Amazon CPF)`.
- **Why DEF:** We implemented the copy as helper text under the control and reported it resolved. The client specified in-field. Placement, not wording.
- **Note:** This field is the one added under the standing project requirement in `CLAUDE.md` (optional Certification Results text field for the Amazon threshold pass-through). Copy is client-specified — **do not reword it**, only relocate it.
- **Effort:** **0.5 h** (Create + Edit forms, both must match).
- **Risk:** None.

### DEF-318 — Stale "certificate added successfully" toast fires out of context
- **QC Ref:** #193 (Reporter: client, opened 6/11/26). Image 106.
- **Client repro (7/30, exact):** Open an existing cert → **Edit**, change status or any field → **Save** → click **Certificates** in the header → click **Add Certificate** → success popup for the *previous* action appears on the blank create form.
- **History:** Reported resolved 6/23 ("No popup is appearing"). It was verified against the *create-then-add-another* path; the client's path is **edit → save → navigate → add**, which was never tested. An earlier 7/2 variant survived a logout/login cycle, which points at a success flag persisted in **TempData or session**, not a page-local variable.
- **Effort:** **2–4 h**. Cheap to fix once located; the cost is auditing every place the success flag is set and confirming it is consumed exactly once.
- **Risk:** Low. Cosmetic, but it undermines confidence in save confirmations generally — a user cannot tell a real confirmation from a stale one.

### DEF-319 — Search autocomplete: hovering a suggestion commits it on Enter
- **QC Ref:** #197 (Reporter: client, opened 6/15/26). Image 88.
- **Client text (7/30):** *"It is still randomly choosing an option after typing and entering as soon as the mouse hovers over something in dropdown. If you just type word and hit enter right away without exploring dropdown items seems to be OK. But not when your mouse goes over options."*
- **Diagnosis (high confidence, from the symptom):** `mouseenter` on a suggestion sets the *active/selected* index — the same state the Enter key reads. Hover is being treated as selection. Correct behavior: hover may **highlight**, but only keyboard arrows or an actual **click** may set the value Enter commits.
- **History:** Two prior partial fixes (6/23 stopped auto-replace of typed text; a later pass fixed the type-then-Enter path). The **hover** path was never addressed. Third time on this item — worth fixing the state model, not the symptom.
- **Effort:** **3–5 h** including regression across all search boxes that share the autocomplete component.
- **Risk:** Medium — shared component; a careless fix breaks keyboard selection.

### CR-030 — Expiration date moved to future: stop auto-changing status; alert instead
- **QC Ref:** #188 (Reporter: client, opened 6/11/26). Image 86.
- **Why CR, not DEF:** **The client has reversed a decision they previously gave us.** On 6/23 we asked which of three behaviors they wanted when a past expiration date is corrected to the future. They chose *revert to the prior status*, and that is what is built and running. On 7/30 they changed it.
- **Client text (7/30), verbatim:** *"Is it possible to add a popup that alerts person? If this is possible make it say the below. **Either way if the date changes DO NOT update the status. Leave it Expired.** It will be on the user to revise as needed. ALERT: Expiration date was moved to the future and certificate is marked Expired. Update status accordingly if necessary."*
- **Scope as requested:**
  1. **Remove** the auto-revert-to-prior-status behavior. Status stays `Expired`.
  2. **Add** a warning modal on save with the client's exact copy above.
  3. Per project standard the alert must use `AdminCore.showNotification()` / `showConfirmation()` — **never a native `alert()`**.
- **Interaction to confirm before building:** CR-028 removed the status transition matrix, so the user *can* now freely set status away from Expired. That is what makes "leave it Expired, user fixes it" workable. Confirm the client understands they are accepting a manual step on every date correction.
- **Effort:** **4–6 h** including the background expiration service interaction check.
- **Risk:** Medium. Touches certificate status behavior. Also note: the **background expiration service** re-evaluates certs every 24h — confirm it will not immediately re-flip a corrected cert, and confirm the reverse case (a future-dated cert that should *not* be auto-expired).

**RESOLVED — built 2026-07-31, then widened by CR-031 on 2026-08-03. QC #188 is closed.**

- **Background-service risk above: checked and clear.** `CertificateExpirationService.cs:137` selects only `ExpirationDate < today`, and `TryApplySaveTimeAutoExpire` bails when the date is future *or* the status is already expired. The service only ever moves certificates **into** Expired — it never un-expires and never re-flips a corrected certificate. Nothing to do.
- **The client then changed the requirement a third time (7/31).** They widened the alert from "date moved past→future" to **any edit of a certificate that was already Expired**, and moved it from a post-save toast to a **pre-save confirmation**, with new copy: *"ALERT: You are updating a previously Expired certificate. Please confirm all fields are accurate including status, etc."* They also scoped it explicitly: *"only if it is originally expired"* — so setting a live certificate **to** Expired during an edit does not fire it.
- Built and browser-validated 2026-08-03. Full as-built detail, the two decisions confirmed before building, and two open questions back to the client are in **CR-031** in `change-request-register.md`.
- **Requirements-churn note for the client conversation:** QC #188 has now been specified three different ways (6/23, 7/30, 7/31) and built twice. CR-030 never reached a deploy before being superseded. Worth naming in the next client review — this is the pattern the effort log tracks, not a one-off.

---

## 4a. Found while building Batch 1

Two defects surfaced during the work that were not in the QC log. Both are recorded in
`defect-register.md`.

**DEF-320 — fixed, shipped with Batch 1.** The arrow-key highlight in the search suggestion list had
**never** been visible in any of the three tile views: the stylesheet colours `.autocomplete-item.highlighted`,
but the JavaScript has always written `.selected`, and nothing writes `.highlighted`. Only the mouse
`:hover` state was ever visible — which is plausibly *why* hover-as-selection looked acceptable when
DEF-280 was written. This had to be fixed in the same batch, because DEF-319 makes the arrow keys the
only keyboard route to a suggestion; without it the fix would have read as "keyboard nav is broken."

**DEF-321 — logged, NOT fixed.** Measured while verifying DEF-316: a certificate imported with
`Partner = "Amazon, Novi"` links both partners correctly (tile API returns `"Amazon, Novi"`), but the
**CSV export reports `Partner = "Amazon"` only**. `CertificateService.cs:588` projects the
denormalized primary `c.Partner` instead of the `Partners` collection.

Left unfixed deliberately — it is outside the agreed Batch 1 scope, and the export path is
partner-scoped (DEF-198 hardened it against cross-partner leakage), so changing what that column
emits needs its own scoping review. **There is also an open product question:** showing co-partners
in an Amazon-scoped export would contradict the partner-isolation rule, so the answer is probably
per-role — Admin/Analyst see every partner, a partner sees only itself — and may need client
sign-off. **This one is worth telling the client about**, since it undercuts the multi-partner
capability (CR-171) they signed off on.

---

## 4. Items needing investigation before they can be classified

These three cannot be given a DEF number yet. Per the project rule, **correct code operating on bad historical rows is a data issue, not a defect** — and all three have a plausible data explanation. Classification follows the repro.

### #218 — Certificate numbers generating 7 characters (`26-10095`) instead of 6
- **Client text (7/30):** *"When adding new certs it's still 7 digits total. Why? I don't think we ran out of numbers... I am ok for it to be 7 digits IF WE RUN OUT OF numbers for the current year (so after 9999 certs). Anuki please confirm you have tested that when we hit a new calendar year it changes the first 2 numbers (e.g., 2027 should have 27-XXXX)."*
- **Our 6/23 position:** behaving as designed (`YY-` + auto-increment, minimum 4 digits) — a high-numbered test cert was imported and dragged the counter up.
- **Why it is still open:** that explanation covers the *existing* `26-10095` row. It does **not** cover the client's 7/30 claim that **newly created** certs are still 7 characters. If the counter derives from `MAX(existing number)`, one imported test cert permanently poisons the sequence — that is the likely mechanism, and it is a **data** problem with a **code** aggravator.
- **Three separable deliverables:**
  1. **Verify the claim** — create a cert on the test env, record the number. ~1 h.
  2. **If confirmed:** either purge the high-numbered test rows (data ops, pre-launch env — permitted) or change the allocator to not inherit from imported outliers. 2–4 h.
  3. **Year-rollover proof** — the client explicitly asked us to demonstrate `27-XXXX` in 2027. This is a **test artifact we owe them**, independent of any fix. 2 h.
- **Also needs a client answer:** they say 7 characters is acceptable *only after 9,999 certs/year*. Confirm they accept a hard 9,999/year cap — worth stating the tradeoff plainly before we build to it.
- **Effort:** **5–7 h** total across all three.

### #242 — Cannot delete Product Type "Carly Test"; usage count says 2 but the 2 records cannot be found
- **Reported 7/30 (new).** Image 107.
- **Why this is suspicious:** DEF-196/197 (May) already fixed this exact class of bug — the usage rollup and the delete pre-check now query certificate FKs directly. If the count says 2 and the UI cannot surface those 2 to reassign, the most likely cause is that **the counting query and the listing query disagree about deleted/inactive rows** — e.g. the count includes soft-deleted or draft certs that the management grid filters out. That is the same shape as several past findings where a list was scoped but an adjacent query was not.
- **Effort:** **4–6 h** — most of it in confirming which query is right before changing either.

### #243 — Product Types existing with no parent Product Category
- **Reported 7/30 (new).** Image 108.
- **Client text:** *"I found several that had Prod Types but not Prod Category. Not sure how this would be possible given you need the category to then define the associated types. Also need to make sure that info is not clearing out again like prior issue."*
- **Two distinct questions:**
  1. **Are the orphans real, or display-only?** If the hierarchy grid fails to resolve a parent it may render blank rather than the rows actually being parentless.
  2. **Can new orphans still be created?** The client's "not clearing out again like prior issue" refers to the earlier product-field-clearing defect. This is the more important half — a data cleanup is worthless if the write path can recreate the condition.
- **Likely related:** the CR-186 product-identity split model (line item per Company + Name + Category + Type + Subcategory), whose migration is still pending client review. Check whether the find-or-create path can persist a Type without a Category.
- **Effort:** **4–6 h** investigation + cleanup scoping. **A data correction here requires a CR** per the standing data-change rule.

---

## 5. Effort summary

| Item | Type | Effort | Certainty |
|---|---|---:|---|
| DEF-316 — template Partner header | Fix | 0.5 h | Firm |
| DEF-317 — Cert Results placeholder | Fix | 0.5 h | Firm |
| DEF-318 — stale success toast | Fix | 2–4 h | Good |
| DEF-319 — autocomplete hover | Fix | 3–5 h | Good |
| CR-030 — expiration/status reversal | Change | 4–6 h | Good |
| #218 — cert number width + rollover proof | TBD | 5–7 h | **Unknown root cause** |
| #242 — Product Type delete blocker | TBD | 4–6 h | **Unknown root cause** |
| #243 — orphaned Product Types | TBD | 4–6 h | **Unknown root cause** |
| | **TOTAL** | **~24–40 h** | |

**Two one-hour wins (DEF-316, DEF-317) close two of the client's loudest 7/30 complaints.** Recommend they go in the next deploy regardless of what else is scoped.

---

## 6. Blocked / not our work

| # | Item | Status |
|---|---|---|
| **150** | Forgot-password email delivery | **Blocked on client.** Code path verified working end-to-end in dev. The live test site has no SMTP credentials configured, so reset emails never leave the server. Needs the client's email-tenant config. Related: the production email service currently falls back to a mock sender. |
| **151** | Duo MFA as an alternative to MS Authenticator | **Needs client decision.** Cannot be scoped until they confirm the factor type and tenant scope. Open since 4/7 with no answer. |
| **173** | Remove the "Partner" column from tile/table under partner login | **Pre-launch task, deliberately deferred.** Client asked us to keep it visible during testing and strip it before production. **This must not be forgotten at cutover** — recommend it becomes a line on the pre-deploy checklist rather than a QC row. |
| **170** | Product type filtering/counts | **Confirm and close.** Partially closed 5/29 with the remainder moved to #186, and #186 closed 6/17. Believed complete — needs a status flip, not work. |

---

## 7. Log hygiene — recommended before the next client review

1. **Flip 46 stale rows to Complete.** Every one has a close date and confirming client text. Leaving them Open makes the backlog look 4x its real size, which distorts every status conversation we have with the client.
2. **Resolve three duplicate IDs.** `#197`, `#238`, and `#242` each appear **twice** on different rows with unrelated content. In each pair one row is an N/A "no Anuki changes needed" note and the other is live. Any report keyed on ID will collide.
   - `#197` — N/A: product category not on public tiles (intentional) **vs. Open: search autocomplete** (DEF-319)
   - `#238` — N/A: splitting the "Doors, Door Hardware, and Accessories" category (client-side decision) **vs. Open: ASIN import preview filters** (delivered)
   - `#242` — N/A: no "newest effective date" sort on public (intentional) **vs. Open: Product Type delete blocker**
3. **Fill the classification column for rows #187 onward.** Everything from June forward has a blank `classification` and blank `Severity`, so the 5-bucket taxonomy (Bug / Enhancement / Question / UX / CR) no longer produces a usable split. This is what makes "58 open" unreadable at a glance.

---

## 8. Recommendation

**Batch 1 (next deploy) — ~7–11 h, all four are client-visible and low risk:**
DEF-316, DEF-317, DEF-318, DEF-319.

**Batch 2 — requires client input first:**
CR-030 needs sign-off that they accept a manual status step after every date correction. #218 needs their answer on the 9,999/year cap. Send both questions now so the answers are back before Batch 1 ships.

**Batch 3 — investigation-led:**
#242 and #243 together (same subsystem, likely one root cause). Timebox the diagnosis before committing to a fix estimate.

**Separately:** log hygiene in §7 costs under an hour and materially changes how the backlog reads to the client.

---

*No code was modified in producing this report. DEF-316 through DEF-319 and CR-030 are reserved but not yet entered in `defect-register.md` / `change-request-register.md` — they will be added when the work is built, per the standing register convention.*
