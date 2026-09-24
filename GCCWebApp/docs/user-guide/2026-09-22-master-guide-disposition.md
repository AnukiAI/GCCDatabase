# Disposition of `GCC_USER_GUIDE.html` — retired 2026-09-22

The master guide was a single 155 KB hand-written HTML file: 32 sections plus 3 appendices, roughly
13,000 words, **no markdown source, no `.docx`, no screenshots, no version and no stated audience**.
Its content was not stale — that is what made the problem hard to see — but it could not be
rebuilt, versioned, illustrated or handed to anyone, so every change to it was a manual HTML edit.

It has been replaced by five pipeline-built guides, each rendered from one markdown source into
both `.docx` and `.html`, with screenshots and versioned front matter.

**It was deleted only after every one of its 35 sections was shown to have a home.** The audit below
was run against the *built* guides, not their sources — the deliverables are what a reader gets.
The file remains recoverable from git history.

## Audit result

**33 sections covered · 0 missing · 2 deliberate exclusions.**

| Master section | New home |
|---|---|
| 2. Who uses the platform · Appendix B — Permissions matrix | Administrator + Analyst — shared `roles.md` |
| 3. Getting started · 17. User Management · 24. Account & profile · 25. Forgotten password · 29. Two-factor · 30. Account access edge cases | Accounts & Sign-In Guide v1.1 |
| 4. Uploading · 5. Editing · 6. Bulk import · 8. The 8-state workflow | Analyst Certificate Guide |
| 7. Certificate detail · 9. Tile view · 10. Company-user tile view · 11. Exporting to CSV · 15. Product identifier search | Company User Guide |
| 12. Company Management · 13. Product Management · 14. Product Types · 16. Dashboard · 18. Role Management · 19. Tag Management · 20. Certification Types · 21. Pathways · 31. Google Analytics · 32. Privacy policy | Administrator Guide |
| 22. Partner Management · 23. Partner API Access · 27. Partner API quick reference | Partner API Setup Guide |
| 28. Mobile & tablet usage | Shared `devices.md` — **re-verified by measurement**, see below |
| Appendix A — Glossary | Shared `glossary.md`, included in four guides |
| Appendix C — Need help? | Shared `getting-help.md`, included in four guides |

## The two deliberate exclusions

**1. Section 1, "About this guide"** — front matter about the document itself. Each new guide
carries its own lede, audience line and colophon, which is the same information in the place a
reader looks for it.

**2. Section 26, "Notifications & toasts" — NOT ported, and this one needs a decision.**
The section stated a clean rule: green toast for success, yellow for warning, red for error, with a
confirmation modal before irreversible actions. The build does not consistently behave that way —
success messages are raised as a blocking modal in a large number of places, which contradicts the
documented standard and is already a known open question. Publishing "success shows a green toast"
would have been wrong for much of the application. **Nothing about notifications is documented in
any guide until that inconsistency is resolved**; once it is, the text belongs in a shared include
so all five guides describe it the same way.

## What was re-verified rather than copied

The mobile and tablet section made specific claims about layout breakpoints. Rather than trust a
document with no source, the layout was **measured at four widths** (1440, 1024, 768 and 390 px)
on the test environment. The claimed boundary holds: the collapsed **Show Filters & Search** control
is absent at 1440 px and present at 1024 px and below, and no width produced horizontal scrolling.
The check lives in `_source/capture-shared-shots.js` and fails the run if a claim stops holding.

## Where the shared material now lives

`_source/_shared/` — `glossary.md`, `roles.md`, `getting-help.md`, `devices.md`, spliced into the
guides by a `<!-- include: name.md -->` directive. A missing include file fails the build rather
than rendering an empty section. The glossary was confirmed to render **identically** in all four
guides that include it, which is the point of the mechanism: a shared table cannot say different
things in different documents.
