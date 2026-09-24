---
name: gate-check
description: Run before anything leaves your hands — a number quoted to a client or partner, a document or file sent outward, a deploy, an edit to a generated artifact, a register entry, or a claim that something is fixed/live/verified. Catches the silent-failure class this project keeps hitting: counts that reconcile while the contents are wrong, fixes that live only in a generated file, internal notes leaking into a client Word document, "clean" test runs that prove nothing because the fault is non-deterministic, and deploy logs that disagree with the server. Use when asked to verify, reconcile, validate, ship, deploy, send, publish, re-issue, or confirm — and whenever about to state a figure or say "it's fixed".
---

# Gate Check

Every incident this document is built from had the same shape: **something looked right and was
wrong, and nothing in the process could tell the difference.** Not one was caught by a test that
failed. They were caught late, by accident, or by an outside party — or not at all.

So the checks below are not "did it work". They are *"what would this look like if it were broken,
and would I be able to tell?"* If the answer is "it would look exactly like this", the check is
worthless and you need a different one.

**Failing a gate is a success.** The cost of a gate is minutes. The cost of a miss is a partner
building against half a dataset, or a client reading our internal defect numbers.

---

## Part 1 — ALWAYS. Every task. No exceptions.

These eight are unconditional. They cost seconds and they are the ones that have actually bitten.

### 1. Never report a result you did not observe

Every number, status code and "it passed" must trace to output you actually saw in this session.
Not inferred, not remembered, not reasonably assumed.

> A harness once turned a timeout into "HTTP 300" and it was reported as a real status.
> A mechanism read from code is not a measurement.

If you did not run it, say so. "Should work" and "does work" are different claims.

### 2. Verify on the environment the work targets

Not the convenient one. Scripts default to whatever their author was using — check the target
before trusting a result.

> Three corrections in one session came from diagnosing against the wrong database.
> One reconciliation script defaults to dev; another defaults to localhost. Neither says so.

### 3. Establish source vs generated artifact before editing anything

Ask: *is this file written by hand, or produced by something?* If produced, fix the source too, or
the next render silently reverts you.

> Two `.docx` guides were corrected. Their HTML source still carried the defect and would have
> reverted the fix on the next render. Evidence that settled it: same version string, and the
> artifact's mtime was 26 minutes after the source's.

### 4. Scan anything going outward for internal content

Before any file, message or document leaves: grep it for defect/CR numbers, people's names,
file paths, internal status names, credentials, hostnames, "TODO", "internal".

**Then also assert the required content IS present.** Absence of leaks is not presence of substance.

> A partner template shipped with a red INTERNAL banner, a defect number three times, and source
> file line numbers. A Word file gets forwarded — it is the least recoverable format we produce.

### 5. Re-check a register's high-water mark immediately before appending

Numbering drifts between sessions, and heading levels are inconsistent, so a single grep pattern
undercounts.

> Proposed DEF-419. The register was already at DEF-424. The earlier read was four entries stale,
> and `^### DEF-` misses the ones written as `####`.

### 6. Scope every claim to the evidence you actually have

Superlatives and scope words — **first, never, always, all, only, every, none, none of, the whole** —
assert something about a *set*. If you have not enumerated that set, you cannot use the word. Either
enumerate it, or scope the claim to what you checked.

> "Reachable for the first time" was said after checking one settings file in one session. It
> asserted something about the project's entire history, which had never been looked at. The
> supportable claim was: *"unreachable in this session's configuration."*
>
> The tell: the sentence would need a different kind of evidence than the one you just gathered.

This is the cheapest check here and the easiest to skip, because an over-scoped claim reads as
confidence rather than as an error. Downgrade the word; do not pad it with a hedge.

### 7. Do not verify code with the code's own rule

If you derive the expected answer using the same predicate the implementation uses, agreement proves
only that the code does what the code does. Get the expectation from somewhere the implementation did
not: a second surface, the spec, the source data, a human count.

> The partner feed and our reconciliation both counted ASINs without filtering `IsActive`, because the
> reconciliation was written from the controller. They agreed exactly — 20,380 = 20,380 — while both
> were wrong, for eight days. The UI had filtered it all along; nobody had ever compared the two.

**Two surfaces answering the same question must be diffed against each other**, not each against the
database separately. That is the check that was missing.

**And prove the gate fires.** Run a new check against an input you know is bad before trusting it.
A gate that has never failed is not evidence — it is an untested assertion.

### 8. Say plainly what you did NOT check

An unstated gap reads as a cleared gate. Name it, or it does not exist.

---

## Part 2 — By work type

### A. Any number you are about to give someone

- [ ] **The number came from a command, not from prose.** A count mentioned in a paragraph is not a
      deliverable — ship the query and its output.
- [ ] **You enumerated, not sampled.** Print every distinct value or pair. `[0]` plus a count hides
      the disagreement you are looking for.
- [ ] **You logged the rows, not the tally.** A paraphrased summary once hid 6 of 15 mismatches.
- [ ] **Equal counts were not treated as equal contents.** Compare key SETS, both directions.
- [ ] **The delta closes arithmetically with no residual.** If you cannot explain the whole
      difference, you do not understand it yet.

      > `20,908 delivered − 537 sent twice + 9 lost = 20,380`. The exact closure is what turned a
      > hypothesis into a diagnosis.

- [ ] **A difference between two totals is not a count of things.** Subtracting two numbers gives
      you a *quantity*, not a *population*. If you are about to describe the gap as "N somethings",
      derive N from the distribution instead.

      > A client letter said *"4,302 ASINs appear on more than one certificate."* 4,302 was the
      > surplus **entries** (20,380 − 16,078). The ASINs that actually repeat numbered **3,599** —
      > 532 of them sit on three or four certificates and contribute two or three surplus entries
      > each. Caught only because someone asked why the two totals differed.

- [ ] **You know which definition the recipient means.** Two correct numbers that do not reconcile
      is a worse outcome than one wrong number, because nobody can tell which is which.

      > 20,380 ASIN-to-certificate associations vs 16,078 distinct ASINs — 3,599 ASINs hold more
      > than one certification. Both right. Ask before quoting.

### B. Paged, batched or chunked data

- [ ] **Row count == distinct key count.** As its own assertion, printed. This is the single cheapest
      check in this document and its absence cost five days.

      > A partner feed returned 212 rows carrying 208 certificates — 4 delivered twice, 4 dropped.
      > The membership check compared *sets* of numbers, and a set collapses the duplicates that
      > were the entire symptom. The pagination `totalCount` agreed, because 212 was correct.

- [ ] **The walk total matches the reported total.**
- [ ] **For anything non-deterministic: run it twice and require identical output.** A single clean
      run of a race proves nothing.

      > That defect ran clean on four earlier pulls *while fully exposed* — same tie group, same page
      > boundary. Those "passes" were luck and were read as correctness.

- [ ] **Force many boundaries.** Use a small page size in testing. The default page size may produce
      only one or two boundaries in the whole dataset, which is why the fault stayed latent.
- [ ] **Sort keys are unique.** `ORDER BY` on a non-unique column with `Skip/Take` is undefined —
      rows can appear on two pages or none. Always end with a unique tiebreaker.

### C. Generated artifacts and their sources

- [ ] **Source identified before editing.** Version strings and mtimes are the evidence.
- [ ] **Source fixed, not just the artifact.** Otherwise the next render reverts you.
- [ ] **An already-issued document is never mutated.** Write a new dated file; the old one is the
      record of what the recipient actually holds.
- [ ] **Diff against a backup and assert the diff is exactly the intended lines.** Byte-count
      arithmetic is stronger still: *"grew by exactly 2 × len(inserted string)"* proves nothing else
      moved.
- [ ] **Assertions match the file's own form, not the rendered wording.**

      > A check for the literal text `"Loop until hasNext is false"` failed against HTML that reads
      > `<p>Loop until <code>hasNext</code> is <code>false</code>.</p>`. The file was fine; the
      > assertion was wrong. Match the markup, or a tag-free fragment.

- [ ] **Formatting preserved.** (Editing `paragraph.text` in python-docx drops the runs and with them
      the monospace styling — write into `runs[0]` and blank the rest.)

### D. Client- or partner-facing documents

- [ ] Internal-content scan passed (Part 1, check 4).
- [ ] Required client-facing strings present.
- [ ] No live credential. Format examples only.
- [ ] Placeholders either filled or deliberately left, and *named* in the handoff.
- [ ] **Promises made are tracked.** If the document says "a corrected version is attached" or "we
      will give you notice", that is now a commitment with an owner.
- [ ] **If we got something wrong, the document says so plainly.** Quietly shipping a fixed version
      is worse than the original error.
- [ ] **Read the whole file before sending it**, including one you did not write.

### E. Deploys

- [ ] **What is actually live — probed, not read from a log.** The log is a claim.

      > The gate log showed the newest bundle as "NOT UPLOADED", implying the last live build was
      > three days older. It was wrong: a *newer* bundle had shipped and was logged in a different
      > section 1,500 lines down. Two independent probes settled it — a static asset md5 matching the
      > tree, and compiled markup in the DLL referencing it. An asset can be hand-copied; compiled
      > output cannot.

- [ ] **Everything already resolved is IN this build.** Reconcile the registers against the bundle
      *before* cutting it. A fix that is finished and unshipped is the most common miss.

      > *"We seem to miss defects in deploy. If I wanted it separate, it would be in a separate
      > build."* — the standing rule: a build carries **every** issue resolved at the time it is cut.
      > Separation is an explicit decision that gets its own build, never achieved by omission.
      >
      > Run `Scripts/deploy/resolved-not-shipped.py`. Treat UNCLASSIFIED and NO-STATUS as unshipped —
      > a fix whose status cannot be read is invisible to every later reconciliation.

- [ ] **Close the loop after the deploy.** Mark shipped entries deployed. A register whose statuses
      are written at fix-time and never updated stops being a deploy ledger, and then nothing can
      answer "what is actually out there".

- [ ] **Ask what the bundle would UNDO.** A bundle is a full overwrite, so it ships the whole tree —
      including work you did not intend to send, and *excluding* anything newer on the server.
- [ ] **Diff every server-owned setting in the zip against what is LIVE, not against the repo.**
      At minimum `wwwroot/robots.txt` (fetch it live) and the `TestEnvironmentGate` block in
      `appsettings.Production.json` (probe `/`). Anything changed by hand on the server is reverted
      by the next full-overwrite bundle, and `verify-bundle.py` cannot see it: it checks the zip
      against the repo, and here the repo was the stale side.

      > 2026-09-23: the DEF-441 hotfix zip carried `robots.txt` = `Disallow: /` and the gate
      > `Enabled: true` while the live public site had both opened by hand on 9/22. Extracting it
      > would have deindexed the public site and put the password gate back on. `verify-bundle.py`
      > reported `failures: 0`. Caught only by fetching the live `robots.txt` and asking what the
      > bundle would undo.

- [ ] **Everything riding along is accounted for.** List it. If something has no defect/CR reference,
      say so rather than inventing one.
- [ ] **Uploads/user data backed up before extracting.**
- [ ] **Migration step consciously decided** — needed or explicitly not, never assumed.
- [ ] **Serialise.** Never build while a gate measures; a publish once starved the app into a 90s
      timeout that reported as a security failure.
- [ ] **Post-deploy verification through the recipient's path**, not a path that merely exercises the
      same code.

### F. Registers and tracking

- [ ] High-water re-grepped (Part 1, check 5), across every heading level in use.
- [ ] Every fix has a register entry; every entry references its origin (QC item, partner report,
      internal).
- [ ] Status lines updated when state changes — a stale "pending deploy" on shipped work misleads
      exactly when it matters.
- [ ] Shipped/delivery log appended when something ships.
- [ ] **Distinguish a defect from a data issue.** Correct code plus bad historical rows is data ops,
      not a defect row.

---

## Part 3 — When a check fails

1. **Stop before the thing leaves.** That is the whole point of the gate.
2. **State it plainly** — what failed, whether it blocks, rough cost to fix.
3. **Question the assertion before the artifact.** Roughly half the failures in practice have been
    wrong checks, not wrong files. Confirm which before raising an alarm — but never by weakening
    the check until it passes.
4. **Fix the check too.** A gate that missed something gets a new assertion in the same session,
    while you still know what it should have caught.

---

## Part 4 — Adding to this document

**This file is meant to grow.** Add a check whenever something got through — not only when it caused
damage, but when it *could have*.

Write each entry as:

```
- [ ] **The check, as an imperative.**

      > The incident, in one or two sentences, with the real numbers.
```

**The incident line is not decoration — it is the reason the check survives.** A bare rule gets
skipped when someone is in a hurry; "212 rows carrying 208 certificates" does not.

Rules for additions:

- **Only from something that actually happened.** No speculative checks; they dilute the list.
- **A check must be falsifiable.** If it cannot fail, it is a sentence, not a gate.
- **Promote to Part 1 only if it applies to every task** and costs seconds. Part 1 stays short or it
  stops being read.
- **Prefer a command to a habit.** `rows == distinct` as a printed assertion beats "remember to check
  for duplicates".
- **Delete checks that stop earning their place.** A checklist nobody finishes protects nothing.

---

## Project-specific helpers

Concrete implementations of the mechanical checks live in the repo:

| Check | Where |
|---|---|
| Paging integrity (rows==distinct, two identical walks) | `Scripts/smoke-prod/def425-paging-determinism.py` |
| Partner payload + DB reconciliation | `Scripts/data-ops/2026-09-09-amazon-payload-export.py` |
| Bundle hygiene + content markers | `Scripts/deploy/verify-bundle.py` |
| What is actually live | `Scripts/smoke-prod/whats-deployed.js` |
| Deploy gates 0–12 | `docs/tracking/pre-deploy-agent-checklist.md` |
| Registers | `docs/tracking/defect-register.md`, `change-request-register.md` |
| Delivery log | `docs/tracking/shipped-YYYY-MM.md` |
| Accumulated lessons | `docs/tracking/ways-to-improve.md`, `observations_log.md` |
