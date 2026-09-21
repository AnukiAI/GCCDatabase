---
name: user-guide
description: Write, update, or rebuild an end-user guide for the GCC Database — the client-facing Word/HTML documents in docs/user-guide/. Use when asked to write a guide, manual, how-to, training doc, walkthrough or "documentation for the client/analysts/external users"; to add or re-take screenshots in a guide; to update a guide after a feature changes; or to decide what guides are still missing. Covers the one-source-markdown pipeline (md to .docx + .html), the frontmatter contract, screenshot capture, and the rules that keep internal defect numbers and dev URLs out of a document that goes to the client.
---

# Building a User Guide

There is **one** way to produce a client-facing guide in this project: a single markdown source
renders to both `.docx` and `.html`. Never hand-write either output. Two formats with two copies of
the text is exactly how the previous guides drifted, which is why the pipeline exists.

```
docs/user-guide/<name>.md          <- the ONLY file you edit
docs/user-guide/_source/
    build-guide.py                 <- renders both outputs
    guide_parser.py                <- markdown dialect (see below)
    capture-screenshots.js         <- Playwright, writes shots/*.png + *.jpg
    shots/NN-name.png              <- referenced by number-prefixed name
docs/user-guide/<slug>.docx        <- generated, never edited
docs/user-guide/<slug>.html        <- generated, never edited
```

Build:

```bash
cd "docs/user-guide/_source"
python build-guide.py ../partner-api-setup-guide.md    # any source
python build-guide.py                                  # defaults to ../guide.md
```

Output basename comes from frontmatter `slug:`, else the source filename. `guide.md` has no slug
and keeps its historical names (`GCC_Accounts_SignIn_Guide_v1.0.*`) — don't "fix" that.

---

## 1. Before you write a word

**Ask who reads it.** The audience decides everything: what is in scope, what vocabulary is
allowed, and what must be hidden. The four audiences do not mix well in one document:

| Audience | Reads it to | Must never see |
|---|---|---|
| GreenCircle admin | Run the system | — |
| GreenCircle analyst | Do daily certificate work | Administration-only screens they cannot open |
| External (company) user | Find their own certificates | Internal tags, Add/Edit buttons, other companies |
| Partner (Amazon, Novi) | Wire up the API | Any other partner's data, internal endpoints |

A guide that shows an External user an admin screenshot has taught them the product is broken for
them. If two audiences genuinely need the same procedure, say so in the frontmatter `audience:`
line the way the Accounts guide does (`Sections 1-4 administrators · sections 5-9 all users`) —
don't split into near-duplicate documents.

**Check requirements before documenting behaviour.** `docs/Final Req Docs/` and
`docs/Final Req Docs/archive/`. Documenting what the build happens to do, when the requirement says
something else, converts a defect into a promise.

**Walk the screen yourself.** Every step gets performed on http://localhost:5101 or the test
environment before it is written down. A guide is a verification artifact: writing "click Save and
the toast confirms" without watching the toast is the same class of unverified claim the
`gate-check` skill exists to catch.

---

## 2. Frontmatter contract

Every field below is consumed by the renderer. Copy this block and fill it in:

```yaml
---
org: GreenCircle Certified Database
title: Accounts & Sign-In Guide
slug: GCC_Accounts_SignIn_Guide_v1.0        # omit only for the legacy guide.md
lede: How to create and manage user accounts, set up two-factor authentication, and reset a password.
version: 1.0
issued: August 2026
applies_to: Test environment
prepared_by: Anuki Consulting
audience: Sections 1-4 administrators · sections 5-9 all users
colophon: ... version 1.0. Prepared by Anuki Consulting, August 2026. Screenshots taken from the test environment; the live site may differ in minor visual detail. For anything not covered here, contact your system administrator.
---
```

- `prepared_by` is **always "Anuki Consulting"**. Never PointSolve.
- `applies_to` is **"Test environment"** until public cutover actually happens. `gccwebapp.com` is
  the pre-launch test environment, and the client's documents say "test environment", not
  "production".
- Bump `version` and the `slug`'s `_vN.N` together, and re-state it in `colophon`. Three places, one
  number — a guide whose cover says 1.1 and whose footer says 1.0 is the kind of mismatch clients
  notice first.

---

## 3. The markdown dialect

`guide_parser.py` accepts a deliberately small subset. Anything else raises `GuideError` at build
time, which is the point — the build fails rather than silently dropping content.

| Construct | Syntax |
|---|---|
| Numbered section | `# 1. Roles, and what each one can do` |
| Subsection | `## 1.1 The two checks that decide what a partner sees` |
| Figure | `![Caption text](04-user-management)` renders `shots/04-user-management.jpg` |
| Callout | `> [!NOTE] Short label` then `> body lines` |
| Table | standard pipe table |

Callout kinds: `NOTE`/`TIP` render as note · `WARNING`/`IMPORTANT` as warn · `CAUTION` as stop.
A blockquote that does **not** open with `> [!KIND] Label` is a build error — there are no plain
blockquotes.

Number your `#` sections manually and keep them contiguous. The TOC is generated from them, and the
`audience:` line refers to them by number.

---

## 4. Screenshots

Shots live in `_source/shots/` as `NN-descriptive-name.png` (plus `.jpg`, which is what actually
gets embedded). Numbers group by topic — `01-1x` accounts, `2x` partners — so a new guide should
claim its own decade rather than interleave.

Capture with Playwright, modelled on `_source/capture-screenshots.js` /
`capture-partner-shots.js`. Rules that have already cost a rebuild:

- **Take them from one environment and say which.** Mixed-environment shots in one document look
  like inconsistent software. State it in the opening `[!WARNING]` callout the way the Accounts
  guide does.
- **Blur real identifiers.** Test account emails are blurred in the existing guides. Anything that
  looks like a credential, an API key, or a live customer's data gets blurred or replaced — a key
  pasted into a screenshot is a disclosed key.
- **Shoot the state, not the page.** A screenshot of the Add User modal with empty fields teaches
  nothing; fill it the way the step describes.
- Re-capture, don't crop-edit. The next feature change re-runs the script.

---

## 5. Language rules for anything that leaves the building

These are not style preferences; each one has been a correction.

- **Say "client", never their names** in anything written down.
- **No internal identifiers.** No DEF-###, CR-###, QC row numbers, branch names, file paths,
  `localhost:5101`, database or server names. A client guide that cites a defect number tells the
  client how many defects there are.
- **No "known issue" confessions** unless the client has already been told in writing. If a
  behaviour is wrong, it is a defect to fix, not a paragraph to write.
- **Second person, imperative.** "Select **Add New User**." Not "the user should be able to...".
- **Bold the exact on-screen label** — and use the label the screen actually shows. Client-requested
  labels are immutable: if the copy looks wrong, that is a data or scoping question, not a wording
  fix.
- **State what happens after the click.** A step without its confirmation ("the list refreshes and a
  toast confirms") gives the reader no way to know they succeeded.
- 16px minimum body text, matching the app's own standard.

---

## 6. Structure that has worked

Both shipped guides follow the same spine. Reuse it.

1. **What you are setting up** — the goal in one paragraph, and the mental model the reader needs
   (the Partner guide's "the two checks that decide what a partner sees" is doing the real work of
   that document).
2. **Procedure sections**, one task each, in the order a person performs them.
3. **A test-before-you-hand-it-over section** — how the reader proves it worked.
4. **Troubleshooting** — symptom, cause, fix, in a table. Every symptom that has actually been
   reported belongs here; this section is where a support call gets prevented.
5. **Appendix** for reference tables and the panel-filling / field-by-field material.

A concept that keeps generating questions gets its own numbered subsection, not a parenthetical.
The Partner guide's §10.2 exists because the same three questions arrived repeatedly.

---

## 7. Finishing

1. Build both formats. A `GuideError` means malformed markdown — fix the source, never the output.
2. Open the `.docx` in Word, not just the HTML. Page breaks, table widths and image scaling are
   where the two formats diverge.
3. **Run the `gate-check` skill.** A guide is a document going to the client, which is squarely
   inside its trigger. The specific failure it catches here: internal notes leaking into a client
   Word document.
4. Grep the built outputs for leaks before sending:
   ```bash
   grep -oiE "DEF-[0-9]+|CR-[0-9]+|localhost|ANUKI01|PointSolve|GCCDatabase_Dev" docs/user-guide/<slug>.html | sort -u
   ```
   Grep the **output**, not the source — the source is not what you send.
5. Log the delivery in `docs/tracking/shipped-2026-MM.md`.
6. Don't commit. Shawn commits.

---

## 8. Keeping guides from rotting

- **A guide with no markdown source is unmaintainable.** `GCC_USER_GUIDE.html` is hand-written HTML
  with no source and no screenshots — it cannot be rebuilt, so every change to it is a manual HTML
  edit that drifts further. Don't create another one, and prefer porting that content into the
  pipeline over patching it.
- **One topic, one home.** `docs/training/user-management.md` and the Accounts guide's sections 1-4
  cover the same ground; `docs/tracking/2026-09-14-gcc-how-to-create-partner-api-key.md` and the
  Partner guide cover the same ground. When you find a second copy, make one of them a pointer.
- When a feature changes, the guides that show it are part of the change, not follow-up work.
  `docs/user-guide/COVERAGE-PLAN.md` maps feature to guide to status; update it in the same pass.
