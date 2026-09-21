# User Guide — Coverage Analysis & Plan

**As of 2026-09-20.** Phases 1-3 BUILT (see §3). Phase 4 (consolidation) remains. Inventory taken from `docs/user-guide/`, `docs/training/`, `docs/tracking/`,
and the view/controller surface of the build on branch `ci-regression`.

Companion skill: `.claude/skills/user-guide/SKILL.md` — the pipeline, frontmatter contract,
screenshot rules, and the language rules for client-facing documents.

---

## 1. What exists today

| Document | Format | Source of truth | Screenshots | Rebuildable | Audience |
|---|---|---|---|---|---|
| **Accounts & Sign-In Guide v1.0** | .docx + .html | `guide.md` | 10 | Yes | Admin (§1-4) + all users (§5-9) |
| **Company User Guide v1.0** | .docx + .html | `company-user-guide.md` | 8 | Yes | External (company) users |
| **Analyst Certificate Guide v1.0** | .docx + .html | `analyst-certificate-guide.md` | 8 | Yes | Analysts + administrators |
| **Administrator Guide v1.0** | .docx + .html | `administrator-guide.md` | 8 | Yes | Administrators |
| **Partner API Setup Guide v1.0** | .docx + .html | `partner-api-setup-guide.md` | 5 | Yes | Admin setting up a partner |
| **GCC_USER_GUIDE.html** | .html only | **none** | **0** | **No** | Unstated — reads as everyone |
| `docs/training/user-management.md` | .md | itself | 0 | n/a | Internal admin training |
| `docs/tracking/2026-09-14-gcc-how-to-create-partner-api-key.md` | .md | itself | 0 | n/a | Internal how-to |

The five versioned guides are deliverable-grade: one markdown source renders both formats, shots are
captured by Playwright, and the frontmatter carries version, audience and colophon.

### The master guide is the problem asset

`GCC_USER_GUIDE.html` is the broadest thing we have — 32 sections plus 3 appendices, roughly 13,000
words, and its content is **not stale**: it already covers Certification Results (CR-045),
Republished as the 8th state, Confidential, and Pathways.

It is also the least maintainable thing we have:

- **No markdown source.** Every change is a manual edit to 155 KB of hand-written HTML.
- **No `.docx`.** It cannot be sent as a client document in the form the other two take.
- **Zero screenshots** across all 32 sections, in a guide whose subject is a visual UI.
- **No version, no audience, no colophon** — nothing that says who it is for or what build it
  describes.

So the coverage picture is misleading at first glance: on paper the app looks ~85% documented, but
most of that documentation sits in an artifact we cannot version, illustrate, or hand over.

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

### Two topics have two homes each

| Topic | Copy A | Copy B |
|---|---|---|
| Creating and managing users | Accounts Guide §1-4 (client deliverable, screenshots) | `docs/training/user-management.md` |
| Creating a partner API key | Partner API Guide §4 (client deliverable, screenshots) | `docs/tracking/2026-09-14-gcc-how-to-create-partner-api-key.md` |

Both internal copies contain material the client deliverable does not (bulk CSV import of users;
lockout recovery). That content is worth keeping — but as one home each, not two that drift.

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

### Phase 4 — Consolidate and retire

1. **Retire `GCC_USER_GUIDE.html`.** Once Phases 1-3 land, its remaining unique content is §16
   Dashboard, §26 notifications, §28 mobile/tablet, §31 Google Analytics, §32 privacy, and the three
   appendices. Fold those into the phase guides, then delete it. Do not keep an un-rebuildable HTML
   document alongside three versioned ones — readers will find the wrong one.
2. **De-duplicate the two double-homed topics.** Move the unique internal content (user bulk CSV
   import, lockout recovery) into the Accounts Guide as an admin appendix; reduce
   `docs/training/user-management.md` and the partner-key tracking note to one-line pointers.
3. **Add a shared glossary + permissions matrix appendix** rendered into all four guides from one
   markdown include, so the role table cannot say different things in different documents.

**Estimate: 4-6 hours.**

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

| Phase | Deliverable | Estimate |
|---|---|---|
| 1 | External / Company User Guide | 6-9 hr |
| 2 | Analyst Certificate Guide | 8-12 hr |
| 3 | Administrator Guide | 8-12 hr |
| 4 | Consolidation, retirement, shared appendices | 4-6 hr |
| | **Total** | **26-39 hr** |

Phases are independent and individually shippable. Phase 1 alone closes the largest gap.
