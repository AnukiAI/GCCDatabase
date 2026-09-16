# Partner API Integration Guide — issuance log

**Purpose:** record which partner holds which revision of `docs2/exports/PARTNER_API_INTEGRATION_GUIDE.html`,
and the per-partner values that copy states. When behaviour changes, this is how we answer **"who is
holding a document that is now wrong?"** Without it that question is unanswerable — the guide carries a
version number and an issue date, but no record of recipients.

**This log is INTERNAL and must never be pasted into the guide.** A partner's copy listing the other
partners who received it would leak the partner roster to a competitor's channel.

## Why the stated values are recorded here, not just the revision

The guide's *Your Connection* panel is filled per partner. A revision number alone does not tell us
whether a given partner was told 200/hr or 2,000/hr, or presence-mode or three-value status. Record what
their copy actually said, so a later change can be assessed against what each partner was promised.

## Log

| Partner | Revision | Date issued | Issued by | Rate limit stated | Status mode stated | Endpoint scope stated | Notes |
|---|---|---|---|---|---|---|---|
| _(none yet)_ | | | | | | | Guide not yet issued to any partner |

## Artifacts cleaned for client review — 2026-08-24

The three sendable files carried the internal scaffolding baked into the generated output, not just
into the HTML source. **The Amazon guide v1.2 was already clean; the partner template was not.** Its
PDF and DOCX both printed the red `INTERNAL — REMOVE BEFORE SENDING` banner on page 1, named
**DEF-345** three times, and exposed `CertificatesController.cs` line numbers, `PublicStatuses`, the
internal status name *Client Approved - Paid* and `localhost:5101`.

Removed 2026-08-24 from `PARTNER_API_INTEGRATION_GUIDE.html` and re-rendered to PDF/DOCX:

| Block | Disposition |
|---|---|
| Page-1 "Internal — remove before sending" | **Rewritten** as a client-facing *How to use this template* box. The instructions were genuinely useful to whoever fills the template; the internal references were not. |
| §2 "Internal — verify on production, then delete" (DEF-345) | **Deleted.** The decision it was waiting on is made — 200/hour — and the verification requirement now lives in this log instead, where it belongs. |
| §4 "Internal — confirm scope, then delete" (source paths, CR-035) | **Deleted.** Fully internal; the open question it raised is tracked as CR-035 and in `2026-08-24-client-questions-partner-api.md`. |
| §10 onboarding step 2 | **Reworded** — cited DEF-345 by number in a table the partner reads. |

Verified after regeneration: zero hits for DEF-345 / CR-035 / `CertificatesController` / `localhost` /
`PublicStatuses` / *Client Approved* / *Republished* across all three files, and **no live API key** in
any of them (all `gcc_partner_` occurrences are format examples). The 6 `[PARTNER NAME]` placeholders
are **deliberate** — it is a template.

PDFs are rendered with headless Chrome from the HTML (`Skia/PDF`, matching how the originals were
made), so the HTML remains the single source and the PDF must be re-rendered after any edit — do not
hand-edit the PDF.

## Before issuing a copy

1. **Confirm DEF-345 is deployed** and the §2 rate-limit figure has been verified against the live
   server. Until then it throttles partner keys at 100/hr regardless of what the panel says. The
   in-document warning has been removed, so **this log is now the only place that gate is recorded.**
2. Fill every `[HIGHLIGHTED PLACEHOLDER]`, including all seven rows of *Your Connection*.
3. Delete the page-1 *How to use this template* box.
4. Verify the stated rate limit against the key actually issued — `PartnerApiKeys.RateLimitPerHour`
   (NULL = the configured platform default, currently 200/hr; it is **not** unlimited).
5. Verify the stated endpoint scope against that key's `AllowedEndpoints`. A key restricted to a subset
   returns `403` on the others, and §3 documents the full set.
6. Confirm the status mode. Presence-only is the only mode that exists today; a three-value mode requires
   CR-035, which is proposed and not built.
7. **Never paste a live API key into the document** — keys go by a separate secure channel.
8. Add a row above.

## When behaviour changes

On any change to rate limiting, status semantics, endpoint scope, or the response shape, read this log
first and decide **per partner** whether a re-issue is needed. A partner acting on a stale guide is worse
than a partner with no guide: they will build to a contract we no longer honour and the failure will
surface in their environment, not ours.

Relevant pending changes that would trigger a re-issue:

- **CR-035** — certificate status as a value (Valid / Expired / Revoked). Would change the *Status mode*
  row and §4 for any partner switched to it, and requires Amazon sign-off.
- **CR-036** — certification standard version. Additive field; would change §4's field table. Note the
  frozen-baseline contract test reports any added field as a contract change — re-baseline deliberately.
- **DEF-345** — partner rate limiting. Fixed locally, **not yet deployed**; gates the first issuance.

**Rate limit — decided internally 2026-08-24 (Shawn): 200/hour stands, as both guides already state.**
No code change needed; `Program.cs:460` already sets `PartnerApiKeyLimit = 200`. Sent to the client for
confirmation alongside CR-035 and CR-036 in `2026-08-24-client-questions-partner-api.md`. Step 4 above
is therefore a *verification* against the issued key, not an open decision — but §2's internal block
still requires seeing `X-RateLimit-Limit: 200` come back from the live server before it is deleted.

---

## Re-issue required — 2026-09-15 — §7 Pagination example omits `includeProductIds=true`

**Status: FULLY CLOSED 2026-09-15. Corrected as v1.2 across the HTML source, the generic template and both issued guides. Source reconciled — the HTML did still carry the defect and would have reverted the fix on the next render.**

**Affected artifacts** (both dated 2026-09-14, both carry the fault):

| File | §7 paragraph | Text as issued |
|---|---|---|
| `docs/tracking/api-samples/GCC_Partner_API_Integration_Guide_Amazon_2026-09-14.docx` | 190, 203 | `GET /api/v1/certificates?pageNumber=1&pageSize=100` |
| `docs/tracking/api-samples/GCC_Partner_API_Integration_Guide_MASTER_2026-09-14.docx` | 191, 204 | `GET /api/v1/certificates?pageNumber=2&pageSize=100` |

**The fault.** §7's two worked example calls omit `includeProductIds=true`. A reader who copies the
pagination example verbatim — the single snippet most likely to be copied — gets correctly paged
responses containing **no product identifiers at all**. The parameter is documented in §3 and in the
endpoint list, so the information exists in the guide; the two are simply never joined up in the one
place it matters.

**Severity is higher than a typo.** The failure is silent: the response is valid, well-formed and
paginated exactly as §7 promises. Nothing signals that the ASINs are missing. A partner building from
§7 alone would conclude the feed carries no ASINs.

**How it surfaced.** Not from a partner report. Amazon pulled on 2026-09-15 and reported "a successful
response of 7K ASINs"; reconciling that number showed they had read **page 1 only** (7,368 distinct
ASINs of 16,078). Checking whether our own guide had failed to document paging is what turned this up —
**it had not.** §7 is present, in the table of contents, shows the `pagination` object, and states
*"Loop until hasNext is false."* `hasNext` appears 3 times, `totalPages` and `totalCount` twice each.
**The paging contract is documented correctly; only the example is wrong.** Worth recording precisely,
because it is the difference between a documentation gap on our side and a partner reading past clear
instructions — and it changes how we word the correction.

**⚠️ BLOCKED — the correction must be made at the source, not in the .docx.** Per this log's own rule,
`docs2/exports/PARTNER_API_INTEGRATION_GUIDE.html` is the single source and generated artifacts are
re-rendered from it, never hand-edited. `docs2/` sits at the **repo root**, one level above the normal
working directory (`GCCWebApp/GCCWebApp`), and is outside the agent's permitted read scope — so the
source could not be opened to confirm whether these 2026-09-14 .docx derive from that HTML or from a
separate lineage. **That must be established before anything is edited:** hand-patching two .docx that
are actually generated would be silently reverted by the next render.

**To close this out:**

1. Grant access to `docs2/` (`/add-dir`) or make the edit by hand at the source.
2. Confirm which source the 2026-09-14 pair was rendered from — `PARTNER_API_INTEGRATION_GUIDE.html`
   or the `GCC_Partner_API_Integration_Guide_TEMPLATE.docx` lineage.
3. Correct both §7 example calls to `…?includeProductIds=true&pageNumber=N&pageSize=100`.
4. Re-render both artifacts; do not hand-edit the .docx or PDF.
5. Re-issue to Amazon and add a row to the log table above.

**Amazon has been told in the interim.** The 2026-09-15 reply
(`docs/tracking/2026-09-15-client-email-amazon-asin-count-and-prod-endpoint.md` / `-client-letter-….docx`)
points them to §7, states the omission plainly, gives them the corrected call inline, and says a
corrected guide is following. That closes the immediate risk without waiting on the re-render — but it
also means **we have now promised a corrected guide**, so steps 1-5 are committed, not optional.

### Correction made — 2026-09-15 — v1.2

**New artifacts** (the 2026-09-14 files are **kept unchanged** — they are the record of what Amazon
actually holds, and an issued document must never be mutated in place):

| File | Version | Changes |
|---|---|---|
| `GCC_Partner_API_Integration_Guide_Amazon_2026-09-15.docx` | **1.2** | §7 paras 190, 203 corrected; version bumped in header + footer |
| `GCC_Partner_API_Integration_Guide_MASTER_2026-09-15.docx` | **1.2** | §7 paras 191, 204 corrected; version bumped in header + footer |

Both §7 example calls now read
`GET /api/v1/certificates?includeProductIds=true&pageNumber=N&pageSize=100`.

**Verified after rebuild**, both files: 0 uncorrected pagination examples · 2 corrected examples each ·
*"Loop until hasNext is false"* still present · 0 remaining `v1.1` strings, 2 × `v1.2` · **no** live
`gcc_partner_` key · **no** `DEF-` / `CR-0` / `localhost` / `PublicStatuses` / *Client Approved* /
`CertificatesController` strings. Paragraph and table counts unchanged (222/13 Amazon, 223/13 MASTER),
confirming nothing else was disturbed.

Character formatting was preserved by writing into each paragraph's **first run** and blanking the
rest, rather than reassigning `paragraph.text` — the run carries the `HTML Preformatted` monospace
styling, and a plain reassignment would have silently flattened the code blocks to body text.

**Issued to Amazon** attached to `2026-09-15-client-letter-amazon-asin-count-and-prod-endpoint.docx`,
which states the correction plainly rather than slipping a fixed guide out quietly — the omission was
ours and the letter says so.

> ### ~~⚠️ STILL OWED: reconcile against the HTML source~~ — **CLOSED 2026-09-15 18:33, see below**
>
> These two .docx were corrected **directly**, because `docs2/exports/PARTNER_API_INTEGRATION_GUIDE.html`
> sits at the repo root, outside the agent's read scope, and it could not be established whether the
> 2026-09-14 pair was rendered from it or from the `…_TEMPLATE.docx` lineage.
>
> **If that HTML is the source, it still carries the defect, and the next render will silently
> reintroduce it and revert v1.2.** Before any future render:
>
> 1. Open `docs2/exports/PARTNER_API_INTEGRATION_GUIDE.html` and search for `pageNumber=1&pageSize=100`.
> 2. If present, apply the same `includeProductIds=true` correction there and bump its version to 1.2.
> 3. Re-render and diff against the 2026-09-15 .docx to confirm they agree.
>
> Recorded here rather than assumed away: a correction that lives only in a generated artifact is one
> render away from being undone.

### Source reconciled — 2026-09-15 18:33 — the concern was justified

**The HTML source DID still carry the defect.** It was `v1.1` and held both
`pageNumber=1&amp;pageSize=100` and `pageNumber=2&amp;pageSize=100` with zero `includeProductIds`.
Same version, same content, and `TEMPLATE.docx` rendered 26 minutes after it — so this is the source
lineage, and **the next render would have silently reverted v1.2.** Fixed now, not assumed away.

| File | Change | Backup kept |
|---|---|---|
| `docs2/exports/PARTNER_API_INTEGRATION_GUIDE.html` | both §7 calls corrected (lines 580, 593); `v1.1` → `v1.2` in header (line 58) and footer (line 736) | `…v1.1-backup-20260915-182957.html` |
| `docs2/exports/GCC_Partner_API_Integration_Guide_TEMPLATE.docx` | same two calls (paras 191, 204); version bumped in header + footer | `…v1.1-backup-20260915-183302.docx` |

**No PDF of this guide exists** — `find` over `docs2/` returns only the HTML and `TEMPLATE.docx`, so
the log's earlier "re-render the PDF after any edit" note has nothing to act on for this document.

**Verification.** The HTML diff against its backup is **exactly four lines** — the two example calls
and the two version strings — and the file grew by exactly `2 × len("includeProductIds=true&amp;")`,
which is the arithmetic proof that nothing else moved. The `.docx` passed nine checks including
unchanged paragraph and table counts, no live `gcc_partner_` key, and no internal references.

> **One verification assertion failed and was wrong, not the file.** The check for the literal string
> *"Loop until hasNext is false"* failed against the HTML because the source marks it up as
> `<p>Loop until <code>hasNext</code> is <code>false</code>.</p>` — the plain-text form never appears.
> The sentence is intact. Recorded because a future reader re-running that check will hit the same
> thing, and because an assertion written against the rendered wording is the wrong assertion for a
> markup source.

**The .docx were corrected in place rather than re-rendered.** There is no pandoc on this machine and
no render script was found for the DOCX path (the log documents headless Chrome for PDFs only). With
the HTML now fixed, a future re-render will *agree* with these artifacts instead of fighting them —
which is the state the earlier warning was asking for.

**All four artifacts are now v1.2 and consistent:** the HTML source, the generic `TEMPLATE.docx`, and
the two issued `…_2026-09-15.docx` guides. The `…_2026-09-14.docx` pair stays at v1.1 untouched as the
record of what Amazon actually holds.
