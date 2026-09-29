# User Guide — Coverage Analysis & Plan

**As of 2026-09-22.** **All four phases COMPLETE.** Five pipeline-built guides; the orphan master HTML is retired. This file is now the maintenance record, not a plan. Inventory taken from `docs/user-guide/`, `docs/training/`, `docs/tracking/`,
and the view/controller surface of the build on branch `ci-regression`.

Companion skill: `.claude/skills/user-guide/SKILL.md` — the pipeline, frontmatter contract,
screenshot rules, and the language rules for client-facing documents.

---

## 1. What exists today

| Document | Format | Source of truth | Screenshots | Rebuildable | Audience |
|---|---|---|---|---|---|
| **Accounts & Sign-In Guide v1.1** | .docx + .html | `guide.md` | 10 | Yes | Admin (§1-4, 10) + all users (§5-9) |
| **Company User Guide v1.0** | .docx + .html | `company-user-guide.md` | 8 | Yes | External (company) users |
| **Analyst Certificate Guide v1.0** | .docx + .html | `analyst-certificate-guide.md` | 8 | Yes | Analysts + administrators |
| **Administrator Guide v1.0** | .docx + .html | `administrator-guide.md` | 8 | Yes | Administrators |
| **Partner API Setup Guide v1.1** | .docx + .html | `partner-api-setup-guide.md` | 5 | Yes | Admin setting up a partner |
| `docs/training/user-management.md` | .md | itself | 0 | n/a | Internal admin training |
| `docs/tracking/2026-09-14-gcc-how-to-create-partner-api-key.md` | .md | itself | 0 | n/a | Internal how-to |

The five versioned guides are deliverable-grade: one markdown source renders both formats, shots are
captured by Playwright, and the frontmatter carries version, audience and colophon.

### The master guide was the problem asset — RETIRED 2026-09-22

`GCC_USER_GUIDE.html` was the broadest thing we had — 32 sections plus 3 appendices, roughly 13,000
words, and its content was **not stale**: it already covered Certification Results (CR-045),
Republished as the 8th state, Confidential, and Pathways.

It was also the least maintainable thing we had:

- **No markdown source.** Every change is a manual edit to 155 KB of hand-written HTML.
- **No `.docx`.** It cannot be sent as a client document in the form the other two take.
- **Zero screenshots** across all 32 sections, in a guide whose subject is a visual UI.
- **No version, no audience, no colophon** — nothing that says who it is for or what build it
  describes.

So the coverage picture was misleading at first glance: on paper the app looked ~85% documented, but
most of that documentation sat in an artifact we could not version, illustrate, or hand over.

**Deleted 2026-09-22 after all 35 of its sections were shown to have a home** — the audit is in
`2026-09-22-master-guide-disposition.md`, and the file remains recoverable from git history.

### Depth is uneven where it matters

Word counts per section of the master guide, in the order a person meets the product:

| Deep (400+ words) | Thin (under 250 words) |
|---|---|
| Uploading a certificate (2,188) | Company-user tile view (212) |
| Certificate tile view (681) | Exporting to CSV (217) |
| Certificate detail page (640) | Company Management (216) |
| 8-state workflow (608) | Product Management (212) |
| Certification Types & custom fields (554) | Product Types hierarchy (185) |
| 2FA (450) | Tag Management (180) |
| Mobile & tablet (432) | Pathways / Amazon CPF (94) |
| Partner API quick reference (402) | Searching by product identifier (82) |

The pattern: **analyst-facing certificate work is well covered; everything an External user or an
Administrator does is a stub.** Pathways at 94 words and product-identifier search at 82 are not
coverage — they are placeholders.

### Not documented anywhere

Present in the build, absent from every document:

- **CSF Attributes** (`Views/Admin/CSFAttributes.cshtml`)
- **Certificate Fields** admin page (`Views/Admin/CertificateFields.cshtml`)
- **Public certificate Single View** — the shareable-URL page an outside party actually lands on
- **GCC API admin page** (`Views/Admin/GCCAPI.cshtml`)

### Two topics had two homes each — RESOLVED 2026-09-22

| Topic | Copy A | Copy B |
|---|---|---|
| Creating and managing users | Accounts Guide §1-4 (client deliverable, screenshots) | `docs/training/user-management.md` |
| Creating a partner API key | Partner API Guide §4 (client deliverable, screenshots) | `docs/tracking/2026-09-14-gcc-how-to-create-partner-api-key.md` |

Both internal copies contained material the client deliverable did not. Resolved: the bulk CSV user
import is now section 10 of the Accounts Guide v1.1, and both internal notes are pointers.
**The lockout "recovery" in the training note turned out to be wrong** and was corrected rather than
ported — see Phase 4.

---

## 2. Coverage matrix by audience

Legend: **D** = deliverable-grade (sourced, versioned, illustrated) · **H** = in the master HTML
only · **—** = absent

| Feature area | Admin | Analyst | External | Partner |
|---|---|---|---|---|
| Signing in, 2FA, password reset | **D** | **D** | **D** | **D** |
| Creating / managing users, access scoping | **D** | — | n/a | n/a |
| Partner setup + API key lifecycle | **D** | n/a | n/a | **D** |
| Uploading a certificate | **D** | **D** | n/a | n/a |
| Editing a certificate | **D** | **D** | n/a | n/a |
| Bulk import (CSV) | **D** | **D** | n/a | n/a |
| Certificate detail page | **D** | **D** | **D** | n/a |
| 8-state workflow | **D** | **D** | n/a | n/a |
| Tile view — browse, filter, search | **D** | **D** | **D** | n/a |
| Finding your own certificates as a company | n/a | n/a | **D** | n/a |
| CSV export | **D** | **D** | **D** | n/a |
| Company Management | **D** | n/a | n/a | n/a |
| Product Management / Product Types | **D** | **D** | n/a | n/a |
| Product identifier search (ASIN/ISBN/SKU/MPN) | H (82 w) | H (82 w) | **D** — documented as NOT searchable | n/a |
| Tags | **D** | **D** | excluded by role | n/a |
| Certification Types & custom fields | **D** (types) · **—** (custom fields, see Phase 3) | **D** | n/a | n/a |
| Pathways (Amazon CPF) | **D** | **D** | n/a | n/a |
| CSF Attributes | **not documented — page is unreachable and unconsumed, see Phase 3** | — | n/a | n/a |
| Certificate Fields admin page | **listed in the menu map only — see Phase 3** | — | n/a | n/a |
| Public directory / shareable single cert | — | — | **D** (from the owner's side) | — |
| Dashboard | **D** | **D** | n/a | n/a |
| Roles & permissions | **D** | **D** | n/a | n/a |

~~**The headline gap: the External / company user — likely the largest audience by headcount at
cutover — has 212 words of documentation and no deliverable.**~~ **CLOSED 2026-09-18** by the
Company User Guide v1.0 (11 sections, 8 screenshots). Phases 2-4 remain open.

---

## 3. Plan

Four phases. Each produces one pipeline-built guide (`.md` source, `.docx` + `.html` out,
screenshots, versioned frontmatter), except Phase 4 which is consolidation.

Estimates are calibrated on the two guides already built (9-10 sections, 5-10 shots each) and
include screenshot capture, both builds, the leak grep, and the `gate-check` pass. **They are
estimates, not quotes** — confirm before any of this is started, per the estimate-before-build gate.

---

### Phase 1 — Company User Guide — **BUILT 2026-09-18, awaiting review**

**Audience:** a certified company's own staff. **Why first:** biggest audience, thinnest coverage,
and it is the guide that most reduces inbound support once external accounts go live.

**Built as 11 sections**, 8 screenshots (decade `4x`), captured as a company-scoped account on the
test environment:

1. What your account can see — the two scoping rules
2. Signing in *(cross-references the Accounts Guide rather than repeating it)*
3. Finding your certificates — list vs tile, sort, columns
4. Searching and filtering
5. The Products page
6. Reading a certificate
7. Downloading certificates — single, from the page, and bulk ZIP
8. Sharing a certificate — the Database Listing URL and what a recipient sees
9. Exporting to CSV
10. Why a certificate is not in your list
11. Troubleshooting · Appendix A — what each status means

**Two corrections the build forced**, both found by walking the screens rather than trusting the
earlier plan:

1. **There is no Share button.** The plan assumed one. The External detail page exposes a
   **Database Listing URL** row (`/Certificate/Verify/<number>`) instead. A guide written from the
   plan would have told readers to press a control that does not exist.
2. **Product identifiers are not searchable anywhere.** The plan had a whole section on searching by
   ASIN/ISBN/SKU/MPN. Neither the certificate search nor the Products search matches them — they are
   projected into results and columns only. Section 5 now says so explicitly, and this is worth
   triaging separately: the master guide's §15 describes a capability the build does not have.

**Actual: ~5 hours** against a 6-9 hour estimate, helped by the pipeline and the two existing guides
as templates. Awaiting review; not yet sent, so not in the shipped log.

---

### Phase 2 — Analyst Certificate Guide — **BUILT 2026-09-20, awaiting review**

**Audience:** analysts and administrators. **Built as 10 sections + 2 appendices**, 8 screenshots
(decade `5x`), captured as an analyst on the test environment. Sections: what you can do · the eight
statuses · creating a certificate · the certificate file · tags and product identifiers · editing ·
expiration and what happens automatically · importing from a spreadsheet · exporting · troubleshooting.
Appendix A is a field reference, Appendix B the import columns.

**Both open questions from the plan were settled without a client decision being needed:**

1. **The CSV template ships from the app, not with the guide.** There is a **Download Template**
   button on the Import page that emits headers-only. Pointing at it keeps the guide correct when
   the column set changes; a companion file would go stale silently.
2. **Pathways is out of scope here.** It is documented only as the field on the Create/Edit form
   ("for Amazon CPF applicable certs"). It remains partner-facing and its own scope is still unclear
   — see the note in Phase 3.

**What the build corrected:**

1. **There is no enforced transition path between statuses.** The matrix still exists in
   `CertificateStatusHelper.IsValidTransition`, but the client removed it from the Edit path and no
   controller invokes it there any more. Documenting the matrix would have documented dead code and
   contradicted the client's own decision. Section 2.1 says status is freely selectable.
2. **Two things change status without the analyst.** The daily sweep, and a save-time force to
   Expired when the expiration date is already past — whatever status was selected. Section 7 exists
   because "the status changed on its own" is otherwise unanswerable.
3. **The Import page states its own rules**, and they are a better second surface than the
   controller: required columns, blank-number behaviour, the overwrite acknowledgement and automatic
   backup, and that Product Category must pre-exist. The guide is written to agree with the panel
   the analyst is looking at.

**Deliberately not captured: the import PREVIEW screen.** Reaching it means uploading a CSV, which
writes a staged import to the client's test environment. Index, upload form and History are
read-only and were captured; the preview is described in words. **Ask Shawn before that write.**

**Deliberately not stated: what a present-but-blank CSV cell does.** The guide quotes the app's own
wording — "any column you leave out is left untouched" — and says nothing about a column that is
present and empty, because that behaviour is an open question with the client.

**Actual: ~4.5 hours** against an 8-12 hour estimate. The estimate assumed re-cutting the master
guide's 2,188-word upload section; in practice the Create/Edit forms and the Import instructions
panel carried most of the structure.

---

### Phase 3 — Administrator Guide — **BUILT 2026-09-20, awaiting review**

**Audience:** administrators. **Built as 12 sections + appendix**, 8 screenshots (decade `6x`),
captured as an administrator on the test environment. Sections: the Administration menu ·
companies · products and the hierarchy · certification types · tags · pathways · users · roles and
permissions · partners and the partner API · Google Analytics · the Dashboard · troubleshooting.
Appendix A is the permissions matrix.

**The menu is not shaped the way the markup reads.** Role Management and User Management sit at the
top level; everything else is nested under **Configuration/Settings**, **API Management** and
**Data Management**. Reading the view file gave the leaf actions but not the grouping — only the
opened menu did. Section 1 is built from the screenshot, not the source.

**Cross-references instead of duplication.** Users (section 7) points at the Accounts & Sign-In
Guide and Partners (section 9) at the Partner API Setup Guide, each carrying only the
administrative facts that do not belong in the other document. This is the anti-drift rule from
section 8 of the `user-guide` skill applied up front rather than retrofitted.

---

#### The two pages the plan flagged: BOTH turned out to be undocumentable as-is

The plan said "budget discovery time, or cut them to a stub and say so". Discovery found that
neither page can honestly be given a procedure, for different reasons. **Both need a decision from
Shawn before they can be documented.**

**1. Certificate Fields — binds every new field to an arbitrary certification type.**
The page is CRUD over the `CustomFields` table, which is what produces the **Additional Fields** on
the certificate form. Those fields are per certification type. But the page's view model carries no
certification type, and `CreateCertificateFieldAsync` assigns
`_context.CertificationTypes.FirstOrDefaultAsync()` — whichever type happens to be first in the
table. So a field created here attaches to an arbitrary type and appears only on that type's
certificates. The Certification Types page does not manage custom fields either, so there is **no UI
path that assigns a custom field to a chosen type**. The page IS linked in the Administration menu,
so administrators will find it. It appears in the section 1 map with a factual one-line description
and **no procedure**. Writing "use this page to add a custom field" would be an instruction to use a
broken control.

**2. CSF Attributes — unreachable, and nothing reads what it writes.**
The page is CRUD over `Tags` filtered to `TagType == "csf"`. That string appears in exactly five
places in the codebase, all inside the page's own service methods — no view, API, export or
certificate surface reads a csf tag. It is also **linked from no view at all**; it is reachable only
by typing the URL. Omitted from the guide entirely.

**Also found while capturing, neither acted on:**

- **`/Company` with no action returns a completely blank page** — no title, no content.
  `/Company/Index` is correct and is what the menu generates, so users are not affected, but any
  bookmark or hand-typed link to `/Company` yields a blank page.
- **`/Admin/GCCAPI` is linked from no view either** — same orphan shape as CSF Attributes, and
  apparently superseded by the Partner API page.

**Actual: ~4 hours** against an 8-12 hour estimate. Discovery on the two flagged pages was the bulk
of it and produced findings rather than sections, which is why the guide is 12 sections and not 10.

---

### Phase 4 — Consolidate and retire — **COMPLETE 2026-09-22**

**1. The orphan master guide is retired.** Deleted only after an audit proved every section had a
home: **33 covered, 0 missing, 2 deliberate exclusions.** The audit ran against the *built* guides,
not their sources, because the deliverables are what a reader gets. Full record in
`2026-09-22-master-guide-disposition.md`; the file is recoverable from git history.

The two deliberate exclusions:

- **"About this guide"** — document front matter. Each guide now carries its own lede, audience
  line and colophon.
- **"Notifications & toasts" — NOT ported, and this one needs a decision.** It stated a clean rule
  (green toast for success, confirmation modal before irreversible actions) that the build does not
  consistently follow — success is raised as a blocking modal in a large number of places, which is
  already a known open question. Publishing the rule would have been wrong for much of the app, so
  **nothing about notifications is documented in any guide** until that is resolved. Once it is, the
  text belongs in a shared include.

**2. Shared includes, so a common table cannot diverge.** `guide_parser.py` now supports
`<!-- include: name.md -->`, splicing `_source/_shared/*.md` before parsing — one dialect, one code
path, and a missing include file fails the build rather than rendering an empty section.

| Shared file | Included by |
|---|---|
| `glossary.md` | Accounts, Company, Analyst, Administrator |
| `getting-help.md` | Accounts, Company, Analyst, Administrator |
| `roles.md` (the permissions matrix) | Analyst, Administrator |
| `devices.md` (phone and tablet) | Company |

Verified after the build: the glossary renders **identically** in all four guides that include it —
four copies, one distinct rendering. That is the whole point of the mechanism.

**3. The mobile section was re-measured, not copied.** The old document's breakpoint claims were
checked at 1440, 1024, 768 and 390 px on the test environment. The claimed boundary holds — the
collapsed **Show Filters & Search** control is absent at 1440 and present at 1024 and below, and no
width scrolled horizontally. `_source/capture-shared-shots.js` re-runs the check and fails if a
claim stops holding.

**4. De-duplication.** `docs/training/user-management.md` and
`docs/tracking/2026-09-14-gcc-how-to-create-partner-api-key.md` are now pointers. Before reducing
them, each was checked for content the deliverable lacked — and both had some:

- The bulk CSV user import is now **section 10 of the Accounts Guide v1.1**.
- **The training note's lockout advice was wrong.** It said toggling a user Inactive and back resets
  the failed-attempt counter "because lockout is per-session". Lockout is time-based: five failures
  set a timestamp fifteen minutes ahead and the account frees itself. Toggling status does nothing.
  Corrected in place rather than ported — it would have had an administrator make two pointless
  edits and conclude the system was broken.
- ⚠️ **A pre-launch security item existed in that file and nowhere else:** the seed accounts
  (`admin`, `analyst`, `external`, a shared default password) must be removed or re-passworded
  before public launch. It was **not** in the pre-deploy checklist — verified. **Now added as
  Gate 13 ("Test fixture accounts are gone", public-launch only) on Shawn's instruction,
  2026-09-22**, covering all sixteen fixture accounts rather than the three the note named.

**5. The Accounts Guide became v1.1**, not an edit to v1.0. That guide was delivered to the client in
the August release, so the slug carries the new version and the v1.0 files are untouched on disk —
an already-issued document is a record of what the recipient holds.

**Verification:** all five guides rebuilt; **0 leak patterns** across HTML and DOCX text (one match
on the Partner guide resolved to `dbtest.greencirclecertified.com`, the partner's own base URL).

**Actual: ~3.5 hours** against a 4-6 hour estimate.

---

### Not proposed, deliberately

- **A public-visitor guide.** The public directory needs no account and no manual; if it needs one,
  that is a UX finding, not a documentation gap.
- **Re-cutting the Partner API Guide.** It is current and complete for its audience.
- **Video or interactive walkthroughs.** No request, and every re-record is a full re-shoot.

---

## 4. Maintenance rule going forward

A feature change that alters a screen in a shipped guide is not done until that guide is rebuilt.
Add the guide rebuild to the same change, and update the matrix in §2 of this file in the same pass.

The cheapest version of this: before a deploy bundle closes, grep the shipped log for changed
screens and check them against §2 here.

---

## 5. Totals

| Phase | Deliverable | Estimated | Actual |
|---|---|---|---|
| 1 | Company User Guide | 6-9 hr | ~5 hr |
| 2 | Analyst Certificate Guide | 8-12 hr | ~4.5 hr |
| 3 | Administrator Guide | 8-12 hr | ~4 hr |
| 4 | Consolidation, retirement, shared appendices | 4-6 hr | ~3.5 hr |
| | **Total** | **26-39 hr** | **~17 hr** |

All four phases are complete. The estimates were calibrated on two guides built by hand; the
pipeline and the accumulating shared material made each successive guide cheaper than the last.

**None of the five guides has been sent.** Nothing is in `shipped-2026-09.md`, and no `.docx` has
been opened in Word — page breaks, table widths and image scaling are where the two output formats
diverge, and that review is the one remaining step before any of this goes out.

---

## 6. Refresh after the 22 September release — P1 + P2, 2026-09-25

All five guides were built at 13:52 on 22 September, hours before that afternoon's release changed
screens two of them show. Gap review and priorities are in the handover doc of the same date.

**P1 — content brought in line with the live site**

| Guide | Change | Shot |
|---|---|---|
| Company | New §6.1: the preview popup, **Page 1 of N** paging, arrow keys, **View PDF**, and the no-arrows case for a document that cannot be pictured | `48-preview-pager` (new) |
| Analyst | New §1.1: the **Status** filter — multi-select, tag above results, kept in the address, respected by CSV export, staff only | `58-status-filter` (new) |
| Analyst | §4: **View PDF** on the upload screen checks the chosen file before save; preview paging on a saved certificate | — |

Both capture scripts now refuse to save these shots unless the behaviour is visible: the pager must
reach "Page 2", the popup must carry **View PDF**, the status filter must change the count to a
non-zero result, add a tag, and put `status=` in the address.

**Screenshots now come from `db.greencirclecertified.com`.** Every capture script's default moved
there. `gccwebapp.com` only redirects since 23 September, and **`test.gccwebapp.com` runs an older
build** — its popup still reads "View all pages" (observed 2026-09-25), so shots taken there would
contradict the text. No guide text named an address, so the address sweep changed nothing in the
guides themselves; `applies_to: Test environment` is kept until the cutover decision.

**P2 — first Word review. Four defects, all in the builder, all fixed at the source:**

1. **Empty contents page in every .docx.** The TOC is a Word field that shows *"Right-click and
   choose Update Field"* until Word computes it. `build-guide.py` now calls the new
   `_source/finalize-docx.ps1`, which has Word fill it in and fails loudly if it cannot.
2. **Numbered lists never restarted in Word** — the Accounts guide ran 1…26. Each list now gets its
   own numbering and starts at the number written on its first item, in both formats.
3. **A step wrapped onto a second source line was cut in two**; the remainder became a stray
   paragraph ("pop-up dialog." in the Partner guide). Continuation lines now join their step.
4. **Only `**bold**` was understood.** `code`, *italic* and ``` fenced blocks printed their markers
   — 31 lines of the Partner guide, in HTML as well as Word. All three are now rendered.

Verified: visible word streams before/after identical in all five HTML outputs (so no text was
lost or duplicated); Word's own list numbering read back for every list paragraph; leak and
marker scan clean on all ten files, with the pattern shown to fire on a file known to contain IDs.

**Still open, not done in this pass:**

- ~~**Accounts and Partner screenshots still show the red "TEST ENVIRONMENT" banner.**~~ **DONE
  2026-09-28.** Both re-captured from `db.greencirclecertified.com`, which does not render the banner.
  Both scripts now hide it anyway and refuse to save if it is still visible (guard calibrated against
  test.gccwebapp.com, where it fires). A pixel scan of all 42 shots in use finds no red banner band;
  the same scan scores a real banner page at 0.579 against a 0.05 threshold.
  - **Found while re-capturing: `07-setup-authenticator` had a scannable QR code** holding a live
    authenticator secret for the test analyst account. It was already in the Accounts guide as built.
    The QR is now blurred before capture, and the script refuses to save if it finds nothing to blur.
    Visiting that page still writes a *pending* (not enabled) secret for that account on the test
    database; the script prints an optional SQL tidy-up, not run.
  - **`03-2fa-challenge` was NOT re-taken.** Signing in as the 2FA fixture returned to the login
    page (once; not retried because failures count toward lockout on the only account that can
    receive mail). The existing shot is a sign-in page and never carried the banner. `SKIP_2FA=1`
    skips that step until the account is looked at.
  - Also fixed: `04-user-management` had been captured mid-load (spinner, empty table) — it now
    waits for rows; and the first version of the account-name scrub rewrote the **Role** badge
    "Admin" to "your.account" — narrowed, then re-captured.
- Cosmetic in Word: tables split across pages without repeating the header row; the cover details
  table has an empty header row; screenshots of full pages are small because the site banner fills
  ~40% of each.
- **Logged in `shipped-2026-09.md` on 2026-09-28** at Shawn's instruction (entry "User guides: five guides, one for each kind of user").
