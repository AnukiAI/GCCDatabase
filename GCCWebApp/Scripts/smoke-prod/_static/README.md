# Static Deploy Gate (`_static/`) — Gate 9

Four **static** checks for the class of defect the compiler cannot see and a running smoke test
will not reliably reproduce. Built 2026-08-03 after DEF-318/DEF-330/DEF-331, each of which shipped
past a green build.

## Safety contract — read this first

These checks are **pure static analysis**. They:

- read source files and run **read-only** `git` queries (`diff --name-only`, `status --porcelain`, `rev-parse`);
- never start the app, never touch the database, never make a network call;
- never modify, stage, commit or check out anything;
- write exactly one file: a summary JSON under `../_results/`.

**A bug in this gate produces a wrong report, never a broken build.** That is deliberate — it was
built during hardening, where the cost of a tool breaking something is higher than the cost of it
missing something.

## Why these four

| # | Check | The defect it exists to catch | Compiler sees it? |
|---|---|---|---|
| 1 | `tempdata-pairing.js` | `TempData` written with no reader on the destination → the message reappears later on an unrelated page (**DEF-318**, all of **DEF-330**) | No — string-keyed dictionary |
| 2 | `redirect-targets.js` | `RedirectToAction("Details","Certificate")` where no such action exists → 404 after a successful save (**DEF-331**) | No — three magic strings |
| 3 | `orphan-views.js` | A partial nothing renders, which silently makes a whole controller UI-dead while its routes stay live (**DEF-331**'s root) | No — unreferenced file compiles fine |
| 4 | `unshipped-fixes.js` | The register says *fixed*; the client still sees the bug because it never shipped (**the DEF-318 re-report**) | No — not a code property at all |

## Run

```bash
node Scripts/smoke-prod/_static/run-all.js          # full gate (selftest first, then all four)
node Scripts/smoke-prod/_static/selftest.js         # just the selftest
node Scripts/smoke-prod/_static/tempdata-pairing.js # one check
```

No `npm install`, no dependencies, no running server. Exit `0` = no FAILs; `1` = at least one FAIL
or a failed selftest.

## Result semantics

- **FAIL** — provable defect. Deploy blocker.
- **WARN** — real finding that needs a human, not a blocker. Two-hop TempData flows, dead code,
  register/reality drift.
- **info** — coverage and counts. Never a finding.

WARNs are deliberately not blockers. A gate that blocks on judgement calls gets switched off.

## The selftest is fatal, and here is why

`run-all.js` runs `selftest.js` **first** and aborts the whole gate if it fails. This is not
ceremony. Both of these happened while building these checks, in one sitting:

1. An argument parser swallowed a closing paren, so every action name failed to resolve and all 58
   TempData writes reported "unresolved".
2. The register parser used `(.*)$` against a CRLF file. In JavaScript `\r` **is** a line
   terminator, so `.` does not match it and `$` never anchors — the parser matched **0 of 247**
   register entries.

In both cases the output was a clean-looking, all-clear report. **A broken check and a healthy
codebase look identical.** The selftest makes that difference observable, using fixtures with known
answers. If it fails, no result from this gate means anything.

## Check 1 — how it decides, precisely

ASP.NET Core does not discard an *unread* `TempData` entry; it re-saves it every request until
something reads that key. "Read-once" only starts counting after the first read.

| Level | Condition | Result |
|---|---|---|
| L1 | Key has writers and **zero** consuming reads anywhere | **FAIL** — provable orphan |
| L2 | A write is followed by `RedirectToAction(X)`, and neither `X`, nor its views, nor **anything else in that controller** reads the key | **FAIL** — the DEF-318 shape |
| L2′ | Same, but a *sibling* action/view in the target controller does read it | **WARN** — two-hop, ambiguous |
| L3 | Key only ever read via `Peek()` — renders but never consumes | WARN |
| L4 | Key has readers but no writer | WARN — dead render path |

**`return View(...)` is deliberately not judged.** The render→postback pattern is legitimate and
used here (AccountController writes `ForceReset*`, returns `View("ForcePasswordReset")`, and the
POST consumes them). The next request there is a form submit that cannot be identified statically,
so flagging it would be a guess. Only a **redirect** is judged, because a redirect *determines* the
next request. Suppressing this cut 11 false FAILs on the first run.

Comment stripping is not optional: `DocumentController.cs` contains `TempData["SuccessMessage"]`
inside a `//` comment explaining why the write was removed. A naive grep counts that as a live
writer and the check passes for the wrong reason. All scans run on comment-stripped source, with
line numbers preserved.

## Check 4 — how "still pending" is decided

The register keeps Status text forever, so ~143 historical entries still read *"working tree,
pending next deploy bundle"* long after shipping. An entry counts as genuinely pending only when
**both** hold:

1. its Status matches the pending pattern, **and**
2. its id appears in no *deploy* row of any `docs/tracking/shipped-*.md`.

Point 2 is subtler than it looks: shipped logs contain rows reading *"Pending deploy 2026-07-31 …
**NOT yet in a built zip**"*. Counting a mere mention as shipped hid **DEF-318 itself** on the first
run — the exact defect this check exists for. Rows are therefore classified, and only real deploy
rows count.

## Allowlist

`allowlist.json`. Every entry must carry a reason. A waiver without a stated why is a lie told to
the next person reading the report. Waivers are for *"we looked, and this is correct as-is"* —
never for *"this is noisy"*.

## Known limits (stated, not hidden)

- Check 1 does not resolve `View()` postback targets, `Redirect(url)`, or keys built from
  variables. Those are reported as coverage gaps in the `info` line, never as passes.
- Check 2 only judges statically-resolvable names. A route built from a variable is skipped.
- Check 3's controller-reachability sweep counts JS `fetch('/Controller/Action')` as a link, so an
  API-only controller is not reported dead.
- Check 4 cannot tell "committed but not deployed" from "change never happened" — both land in
  CLEAN, which is why CLEAN is a WARN asking a human, not a FAIL.
