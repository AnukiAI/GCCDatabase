# Ways to Improve

Running list of improvement ideas surfaced during the project. Newest at top. These are
suggestions/process improvements — distinct from defects (`defect-register.md`) and change
requests (`change-request-register.md`).

## 2026-07-20 — surfaced during CR-028 (remove the status transition matrix)

1. **Get engineer-invented business rules signed off BEFORE they ship — not after a client hits them.** The status transition matrix was added in a 2026-05-19 *security* pass, but "which status may follow which" is a *business* rule, not a security control. It shipped without client review; the client then hit it live (*"I was unaware we had any in place"*), we wrote a fix (DEF-305) and a sign-off sheet, and after review the client had us **remove the whole thing** (CR-028). Net: build the matrix → build DEF-305 UX around it → build the sign-off docs → tear it all out. **Rule going forward:** when a hardening/refactor pass adds a rule that *constrains what a user may do* (not just what an attacker may do), flag it as a requirement decision and get a one-line client confirmation before it ships. Cheaper than the round-trip. Ties to the standing efficiency note — *ask before building something the client may not want.*
2. **When removing an enforcement guard, audit every OTHER branch that keyed off it.** Deleting the Edit-POST matrix block wasn't enough: the QC#188 expiry-restore branch *also* tested `!IsValidTransition(...)`, and left as-is it would have silently overridden an analyst's explicit status choice once the matrix was gone. Removing a rule means re-reading every `if` that referenced it, not just the obvious one. (Mirrors [[feedback_scoping_check_adjacent_endpoints]] — the fix is never only where you first look.)
3. **Keep behavior-assertion smokes honest when behavior flips.** `verify-def203.js` asserted the matrix *blocked* bad transitions; after CR-028 that assertion is exactly backwards. A regression test that encodes a now-reversed rule is worse than none (it fails green-gates for the wrong reason). Repurposed it in the same change — but the general rule: a CR that changes a behavior must update the test that pinned the old behavior, in the same diff.

## 2026-06-19 — surfaced during the full regression run

1. **Make test fixtures drift-proof (no hardcoded ids).** The Tier 2 seed broke twice on
   hardcoded ids that shifted in the dev DB (`@certTypeId=1` removed; `@ownCompanyId=53`
   went from Henkel → Domtar; the `external` user actually maps to KAO/259). Fixed by
   deriving both by lookup. **Rule going forward:** seed/teardown SQL should resolve ids by
   stable keys (name, username→CompanyUser link, `IsActive` filters), never literal ids.
2. **Harden tile-view smokes against the load race.** `cert-edit` and the Tier 2 Internal
   view both flaked because assertions read DOM state before async tile/option loads
   settled. cert-edit is now fixed with a `waitForFunction` on the settled value; the
   Internal-view smoke (`cert-state-matrix.js`) should get the same treatment — wait for the
   specific card to render (bounded) before asserting shown/hidden, so a slow cell can't
   masquerade as a visibility bug.
3. **Fix DEF-234 (cold-start tile-API latency) — it is the root of nearly every E2E flake.**
   The first `/api/certificate-tiles` hit after a server (re)start runs ~20-23s (EF
   query-compile + cold cache), which trips both Playwright nav timeouts and the JS-side
   `AbortSignal.timeout`. Every "flaky" FAIL in this run chains back to it. Pre-warming the
   tile projection at startup (and profiling `AsSplitQuery` per the EF-includes rule) would
   remove the whole flake class and let the suite gate deploys without retry logic.
4. **Keep view smokes synced to UI changes (CR traceability).** The `#Partner` → CR-171
   `#partnerSelect` drift sat undetected because the smoke wasn't updated when the Create
   form changed. Consider a lightweight checklist item on UI-touching CRs: "does any
   `_views/*.js` assertion reference a selector this CR renames/removes?"
5. **Coverage gaps the current suite still does NOT cover** (documented in
   `e2e-smoke-plan.md`, restated as a backlog): field-level admin CRUD permutations,
   wrong-but-renderable logic bugs (the DEF-220 class), full WCAG keyboard audit,
   performance/load, and cross-browser (Chromium only). These remain UAT/targeted-probe
   territory until explicitly scoped.

## 2026-06-24 — surfaced during the QC #198 / DEF-286 upload-storage work

- **Centralise duplicated "resolver" logic the moment a second copy appears.** The
  logical→physical upload-path resolution was copy-pasted across 5+ call sites; DEF-262, 264,
  and 279 were three separate defects that each fixed one copy and missed the neighbours. The
  cost of those three defects far exceeded the cost of the single `IUploadPathResolver` that
  DEF-286 should have been from the start. Heuristic: when the same non-trivial logic lands in
  a second place, extract it then — don't wait for the divergence bug.
- **Dead config keys are a trap.** `FileStorage:StoragePath` sat in the production template
  for months looking like it controlled upload location, but nothing read it — operators could
  "configure" it with zero effect. Either wire a config key or delete it; a key that lies is
  worse than no key.
- **Treat "inside the deploy folder" as non-persistent.** `App_Data/uploads` looked safe but a
  Plesk publish wipes it — the root of a recurring incident that was being managed with a manual
  "zip before deploy" ritual. Anything that must survive deploys belongs on a path outside the
  deploy folder, resolved by config. Audit other write locations (logs, generated files, caches)
  for the same assumption.
- **Test infra: persisted computed columns don't exist under the InMemory provider.** The
  CR-187 `NormalizedX` search columns are SQL computed columns, so ~18 unit/integration tests
  that filter on them fail under EF InMemory (the column is null). This made it hard to tell my
  change's failures from the baseline. Worth either (a) a SQLite-backed test fixture for the
  search/normalization tests, or (b) a clearly-labelled `[Trait("RequiresRelational")]` skip so
  the baseline failure set is unambiguous.

- **Column/field-level visibility keeps leaking one surface at a time.** The partner export has
  now needed three separate scoping fixes for the SAME confidentiality rule: Internal Tag column
  (DEF-128), Internal custom fields (DEF-247), and three first-class schema columns —
  Cert Subcategories / Pathway / Cert Results (DEF-295). Each was a hand-maintained column list
  in `GenerateCsvExport` that drifted from "what a partner is actually allowed to see." The real
  fix is to stop hand-listing: derive the export's partner column set from a single declared
  source (the same field-visibility metadata the external tile API / v1 partner API project), so
  adding a new certificate column can't silently appear in the partner CSV. Until then, every new
  export column needs an explicit "is this partner-visible?" review. See
  [[feedback_scoping_check_adjacent_endpoints]].

## 2026-06-25 — CSS verified only against stale dev hash → shipped a tile regression
**What happened:** DEF-293 tile-condensing CSS was marked "verified uniform on localhost," deployed, and immediately regressed the live tiles. The dev env serves a stale `asp-append-version` hash, so the "before/after" screenshots had been captured against OLD CSS — the real render was never seen until after it broke prod.
**Improve:**
1. **Never sign off CSS from the stale-hash dev path.** Always verify with a FRESH Playwright context (no cache) hitting the actual stylesheet, and assert the new rule is present (e.g. grep the served CSS for a marker string) before claiming "verified."
2. **Measure, don't eyeball.** Capture card heights programmatically (`getBoundingClientRect`) so "uniform/condensed" is a number, not an impression.
3. **CSS deploys carry a Plesk-overwrite risk.** Plesk extract can skip existing files; the `?v=` cache-bust changes the URL, not the served file. For CSS changes, confirm the file is actually overwritten on the server (or delete-first), or the change silently no-ops on prod.

## 2026-06-25 — Type-ahead widget discarded the typed value unless mouse-clicked (DEF-299)
**What happened:** The `SearchableSelect` custom dropdown (Certification Type, Product Category, etc.) only committed its value to the underlying `<select>` when the user clicked a row with the mouse. Type-then-Enter / Tab / click-away silently left the field empty, so the form posted an empty `[Required]` int and the server rejected it with the cryptic "the value '' is invalid." It survived "hours of testing" because automated/manual checks that set the value programmatically (or always mouse-clicked) never exercised the keyboard gesture real users default to.
**Improve:**
1. **Custom form controls must behave like the native element for EVERY commit gesture, not just the happy mouse path.** Any widget that proxies a `<select>`/`<input>` needs explicit tests for click, Enter, Tab, and blur — keyboard-then-blur is the common case, not the edge case.
2. **Smoke the full happy-path submit, not just the presence of `required` attributes.** `cert-create-validations.js` checked that fields are marked required but never did a complete fill-and-submit, so it couldn't catch a field that posts empty. Add a "fill everything via the real UI gestures and expect a 200/redirect" path to the create/edit smokes.
3. **A `[Required]` non-nullable value type yields the raw ".NET value '' is invalid" message** rather than the friendly controller guard — when you see that wording, suspect an empty/uncommitted POST upstream (often a JS-driven control), not a server validation-rule problem.

## 2026-06-25 — Server "you must acknowledge X" guards need a client acknowledge path, not a dead-end error (DEF-300)
**What happened:** The CSV-import API returns 400 + `requiresOverwriteAck` / `requiresReassignmentAck` so the client can confirm a destructive action and re-submit with the flag set. The Internal tile-view import modal never implemented the client half — it posted with no ack flags and rendered the guard message as a terminal "Error/OK" dialog, so overwriting an existing certificate was impossible from that screen (infinite Apply→Error loop). The full-page MVC flow handled it; the modal was a parallel implementation that drifted.
**Improve:**
1. **When the server adds a "requires acknowledgement" response, every client that calls that endpoint must handle the flag — grep for all callers.** Here two UIs hit `/api/CertificateImport/Apply`; only one was updated when the CR-023 R5 guard landed. A 400 carrying a `requiresXAck` field is a *prompt-and-retry* contract, not an error.
2. **Two import UIs (modal + full-page) duplicate the apply/ack logic.** They will keep drifting; consider collapsing the modal onto the same code path, or at minimum a shared smoke that drives the modal's overwrite-acknowledge round-trip (none existed, which is why this shipped).
3. **Round-trip imports silently mutate data via tag handling.** While tracing DEF-300: `DetectChanges` ignores internal tags (so a clean re-import always shows "Modified"), and an applied overwrite clears+re-adds external tags only (dropping internal tags). Import change-detection and apply must treat the internal+external tag sets symmetrically — track and fix separately.

## 2026-07-17 — Conventionally-routed action params must match the route token name, or add an explicit route (DEF-308)
**What happened:** `CertificateController.PreviewThumbnail(string fileName)` was only reachable via the default route `{controller}/{action}/{id?}`, whose trailing token is `id`. The link (`/Certificate/PreviewThumbnail/{file}`) bound its segment to `id`, so `fileName` was always null → the guard returned `NotFound()` → the Create-page PDF preview rendered as a broken image, for everyone, the whole time the feature existed. It went unnoticed until a client reported it because the happy path (Edit, which uses `ViewCertificateImage(int id)`) works and nobody visually checked the *pre-save* preview.
**Improve:**
1. **Any action that takes a route value from a path segment must either name the parameter `id` (the default token) or ship an explicit `MapControllerRoute`/attribute route naming the segment.** This app already had the pattern for `Certificate/Edit/{number}` and `.../Delete/{number}`; `PreviewThumbnail` was added (DEF-208) without one. Grep for controller actions whose single string/int param is *not* named `id` and confirm each has a matching route.
2. **A "returns NotFound on any failure" endpoint hides the difference between 'file missing' and 'never bound the argument'.** Both were 404, so the routing bug looked like a missing-thumbnail. Consider logging *why* (null arg vs File.Exists=false) at Debug on these serve-file endpoints so the next mismatch is one log line, not a repro session.
3. **Pre-save / draft-state UI has no automated coverage.** The smoke suites drive saved certs; the Create-form preview-before-save path had zero tests, which is exactly why a 100%-repro bug shipped. A single smoke that uploads a PDF on `/Certificate/Create` and asserts the sidebar `<img>` actually loads (naturalWidth>0) would have caught it.

## 2026-07-20 — File-serving endpoints need an explicit cache policy; "intermittent" is a symptom with a short list of causes (DEF-310)
**What happened:** `ViewCertificateImage` / `ViewCertificateFile` returned `PhysicalFile(...)` with no `Cache-Control` header. Per RFC 9111 a 200 with `Last-Modified` and no freshness directive gets *heuristic* freshness (~10% of the document's age), so replacing a document on an old certificate served the stale copy for days while replacing on a fresh one worked instantly. The client and CP both experienced it, both failed to reproduce it on a second attempt, and it sat unexplained as "sometimes doesn't take."
**Improve:**
1. **Every endpoint that streams a mutable file must state a cache policy explicitly — never inherit the default.** The default is not "don't cache"; it's "let the browser guess," and the guess scales with file age, which is the worst possible behaviour for a replace-in-place feature. Audit the remaining file-serving actions (`PreviewThumbnail` still has no policy) and give each one an intentional header.
2. **Stable URLs + mutable content = always add a version token.** `/Certificate/ViewCertificateImage/{id}` never changes but its bytes do. The `?v=` token now derives from the stored path; the same pattern should be the default for any future `id`-addressed file route. Headers alone don't cover intermediaries that ignore revalidation.
3. **Watch for role-dependent header behaviour when diagnosing caching.** Anonymous responses here inherit `no-cache,no-store` from the cookie-auth handler; authenticated ones got nothing. Testing anonymously would have "proved" the bug didn't exist. **Reproduce in the same auth state as the reporter** — for analyst-reported defects that means logged in, always.
4. **"It worked the second time" is diagnostic, not noise.** It narrows to a very short list: caching, a race, or a silently-swallowed retry-on-failure. Enumerating those three up front would have gone straight to the answer instead of starting from the save path. Worth making the first question on any intermittent report.
5. **Don't mistake "the seeded data can't show this" for "this can't be verified."** No *seeded* certificate's `CertificateImagePath` resolves to a file on the dev box, so every image request 404s — from which I concluded the replace round trip was unverifiable locally and shipped the fix with that gap flagged. Wrong: creating a fresh certificate through the UI produces a real PDF and thumbnail on disk, and the whole flow then verifies end-to-end (`def310-replace-document-verify.js`, 10/10). **When fixture data blocks a check, generate the fixture instead of downgrading the claim.**
6. **Assert on rendered pixels, not just bytes or URLs.** The DEF-310 harness generates two PDFs reading "DOCUMENT ALPHA" and "DOCUMENT BRAVO" so the thumbnail swap is visually provable, and saves the served image for eyeballing. A hash change alone would also have been produced by a re-encode of the *same* document — the visual check is what makes it conclusive.
7. **Two Create-form automation traps, both silent.** `CertificationTypeId` is `display:none` behind the `SearchableSelect` widget, so Playwright's `selectOption` refuses to drive it (set via DOM + dispatch `change`). And `validateCertDateRange()` calls `preventDefault()` when Effective == Expiration with **no visible error and no server-side POST** — it presents as a dead Save button. Both cost real cycles. Worth a shared helper in `_lib/` so the next Create-form smoke doesn't rediscover them.

## 2026-07-20 — A private module object that view scripts reach via `window.X.Y || {}` is a silent split-brain bug (DEF-311)
**What happened:** `certificate-create.js` kept the real tag arrays inside a private `TagManager` closure and exposed only `removeTag` (bound to it), not `TagManager` itself. `Edit.cshtml` (and `Create.cshtml`'s post-validation restore) populated tags via `window.CertificateCreate.TagManager = window.CertificateCreate.TagManager || { … }` — because `TagManager` was unexposed, that quietly created a *second, throwaway* object. The loader wrote to one array; `removeTag` read the other. No error, no console warning — the remove button just silently did nothing, and Save reported success on an unchanged list.
**Improve:**
1. **If a view is expected to reach a module's internal state, that state must be an explicit, single export — never something a `|| {}` fallback can fabricate.** The `x = x || {}` idiom turns a missing export into a plausible-but-wrong empty object instead of a loud failure. Prefer an exposed method (`TagManager.loadExisting(ids)`) over letting the view poke arrays directly.
2. **The bug only bit *pre-populated* state (Edit's saved tags, Create's re-rendered-after-error tags), never the happy path (add-then-remove in one session).** Same shape as the pre-save-preview gap (DEF-308) and the validation-restore state-loss (DEF-246): our tests drive fresh interaction, not "state that was rehydrated from the server." Any field that can be loaded/restored needs a remove/clear test on the *loaded* instance, not just a freshly-added one.
3. **"Save says success but nothing changed" is a class, not a one-off.** A hidden input that is only written by a JS handler will silently post its stale server value if that handler no-ops. When a control edits a hidden field, a test should assert the hidden field's value actually changed — not just that the visible pill/row updated.

## 2026-07-31 — Partner onboarding is a repeatable process, not a per-partner build

Surfaced while reviewing Novi's data-exchange guidance against the Amazon integration.

1. **Keep partner docs generic by default, specialize at the cover page.** The Amazon guide had
   Amazon-specific content woven through ~a third of the body, so the second partner meant a rewrite
   rather than a reprint. `docs2/exports/PARTNER_API_INTEGRATION_GUIDE.html` is now the reusable
   template; partner-specific facts are confined to highlighted placeholders. Next partner should be
   a fill-in-the-blanks exercise.
2. **Never put a live credential in a document.** The Amazon guide shipped with a production key on
   page 2. The generic template states explicitly that keys are delivered by secure channel only, and
   carries an internal pre-send block reminding the author. Worth making this a standing rule for any
   partner-facing collateral.
3. **Documentation drifts from the contract — verify docs against code, not memory.** Two errors were
   carried in the Amazon guide: the error envelope shape and the rate limit (200/hr documented vs
   2000/hr default in code). A short doc-vs-code check should be part of any partner guide revision,
   the same way the Amazon contract diff is a pre-deploy gate.
4. **Partner tagging is the real onboarding lead time, not the key.** Issuing a key takes seconds;
   deciding and applying which certificates a partner may see is a business decision that gates the
   entire integration. Raise it at the first partner conversation, not at go-live.
5. **The active-only status filter is an Amazon assumption baked into a shared contract.** Badging
   partners want active-only; validation partners need Expired/Revoked to refute a false claim. If a
   second partner needs the expanded set, prefer a per-key or per-partner behavior flag over forking
   the endpoint — and treat any change as a CR, since the Amazon contract is frozen.

## 2026-09-03 — Incremental loads onto a live database are a different job from the first migration

Surfaced running the September top-up (193 certificates, 1,870 ASINs) through tooling written for
the one-time August go-live load.

1. **A loader written for an empty database quietly stops being correct.** The August generator
   asserted `AllowsDuplicateNumber` counts across the *whole table*, which balances only when the
   table starts empty; on the 2026-09-02 top-up a correct load rolled back. Same shape here with
   `DisplayOrder`, which restarted at 1 per certificate and would have collided on a certificate
   that already had product identifiers. **Every "count the table" assertion in a loader should be
   scoped to the rows that load is responsible for**, from the first version, not after it bites.
2. **Resolve the client's data against the client's own system, not against our reading of it.**
   Three certificates looked like unauthorised UPDATEs to live records. One fetch of the client's
   public production API settled it in seconds: their system already showed the renewed dates, so
   the spreadsheet was right and our database was stale. The same fetch supplied every field for two
   certificates missing from the drop entirely. **A client-facing system of record is a cheaper
   tie-breaker than an email round-trip, and it is evidence rather than inference.**
3. **Ask the blocking questions in one batch, before building, not as they arise.** 79% of the ASIN
   file pointed at two certificates that did not exist. Discovering that at load time would have cost
   the deadline; discovering it during a 20-minute analysis pass cost nothing, and the answer arrived
   while the loader was being written.
4. **A design decision already settled in code but never written down gets re-deferred every time.**
   `ProductSubcategoryId` was left NULL in August "pending a design decision" — but the decision was
   visible in `CertificateController.GetProductSubcategories` all along (it FKs to `ProductTypes`,
   not to the empty `ProductSubcategories` table). Two loads shipped without a field the client
   supplies. **When a loader skips a field "pending a decision", check whether the running
   application has already made it.**
5. **A case-insensitive database will silently reuse a near-match, and that is usually right.** Two
   product names differed from the sheet in capitalisation only; the loader matched the existing
   records rather than creating twins the application itself cannot distinguish. The verifier should
   report that as information, not fail on it — a check that fails on correct behaviour trains people
   to ignore it.
6. **Measure the endpoint the user actually uses.** The reported 10-12 second search measured as
   under a second on the public tile endpoint and 8-10 seconds on the internal one — same feature,
   different controller, 1,100 rows versus 5,924. **A performance measurement is worthless without
   naming the role and the route it was taken on.**

## 2026-09-03 (later) — verification that fails on correct work is worse than no verification

Both of the day's false alarms were in the CHECKS, not in the work being checked.

1. **A 404 from the wrong route key is indistinguishable from "the file is missing."** The PDF
   round-trip check reported **0 of 12 documents served** against 96 uploads that were all perfect.
   `ViewCertificateFile`/`ViewCertificateImage` take an `int id`, not a certificate number — the
   exception to this project's number-keyed-routes rule, which is documented in CLAUDE.md for the
   *edit* routes and not for these. **Any check whose failure mode is a 404 should assert a
   known-good control passes** before believing a whole batch failed.
2. **A dictionary keyed by a non-unique key silently deletes rows from your report.** The country
   audit printed "5,749 certificates" for a 5,924-row table, because both its lookups were keyed by
   certificate NUMBER and CR-038 lets several records share one. It read like a load had just
   dropped 175 rows. **Print row counts and distinct-key counts as separate figures whenever a
   key is not unique** — and CR-038 means no certificate-number key in this codebase ever is.
3. **A "known follow-up" left unrun becomes an inconsistency the moment new data arrives.** The
   compound-country split was deliberately deferred on 2026-09-02 with good reason. One load later,
   10 new certificates use the split shape and 15 old ones do not, so the database now holds two
   shapes for the same concept — a worse state than either choice. **When deferring a data fix, note
   what will make the deferral more expensive, not just why it is safe today.**
4. **Prefer the application's own API to hand-placing files and hand-writing SQL.** Attaching 96
   PDFs through `POST /api/certificates/{number}/upload-pdf` picked up four behaviours for free that
   the August hand-placement reimplemented — path resolution (DEF-286), naming, preview rendering
   (DEF-262) and setting both columns atomically — and needed no server access at all. Check for an
   existing endpoint before writing a migration path around one.

## 2026-09-04 — "are we sure that's the best solution?" was worth more than the whole diagnosis

The client-visible search fix was found only because Shawn refused the first answer. Four things
went wrong before that, and they are all the same kind of wrong.

1. **A plausible mechanism is not a measured one.** "Leading-wildcard LIKE is non-sargable" is
   true, expert-sounding, and accounted for **195 ms of an 8,000 ms** request. It was never tested
   against the database — it was reasoned from the code. **5,924 rows is small; if a number seems
   too large for the data volume, the explanation is probably not the one you can see.**
2. **Verify against the artifact you actually measured.** The endpoint timed was
   `/api/certificate-tiles-internal`. The predicate read — and confidently ruled out — was in
   `CertificateService.cs`, which that endpoint does not call. The controller's own predicate
   carried the CR-037 clause that turned out to be the entire cause. A "ruled out" written into the
   defect register from the wrong file is worse than no entry, because the next person believes it.
3. **A fix that costs user-visible behaviour deserves the most scepticism, not the least.**
   Prefix-matching certificate numbers would have been shipped, would have made `0636` stop finding
   `26-0636`, and would have saved almost nothing. It reached a client letter before it was caught.
4. **A measurement harness can be the bug.** The first test of the real fix showed no improvement —
   because it resolved ids into a `DECLARE @ids TABLE`, and a table variable estimates 1 row and
   wrecks the plan. With a literal `IN` list, as the code actually emits, it was 194 ms.
   **When a fix "does not work", check the test before abandoning the fix.**

And one that is not about being wrong: **the same defect keeps coming back under a new feature.**
DEF-366 pulled the tag match out of the search OR; CR-037 put the country match straight back in,
in the same shape, three months later. The fix existed, was documented, and was in the very file the
new code called. **When a class of bug gets its own helper, the helper's doc comment should say
"any new collection match belongs here" — and a code review of a feature that adds a searchable
collection should ask whether it used it.**

## 2026-09-04 — a warning in a code comment is evidence of a decision, not of current behaviour

The session's third stale claim. All three reached a client-facing document.

1. **A doc comment describes the world when it was written.** The compound-country split script
   warned that after running it the exports would show only the primary country. True on 2026-09-01.
   `BuildCountryList` landed afterwards and made it false — but the comment stayed, and I carried it
   into a defect note, the shipped log and a client letter without once downloading the export.
   **Before repeating a limitation from a comment, spend the ninety seconds to observe it.**
2. **The deployed artifact is the only authority on deployed behaviour.** Reading the repository
   could have shown the fix existed but never that it was live. One HTTP request settled both.
3. **Never invent an input and report its rejection as a defect.** The export audit's first run
   flagged four endpoints returning HTTP 400. The `type` parameter is a closed switch; I had made
   the values up. Transcribe enumerated inputs from the code, then a rejection means something.
4. **"Same size" is a lead worth pulling.** Six stat exports came back within 20 bytes of each other.
   They were meant to differ, and three sorted by the wrong dimension — two produced byte-identical
   files under different titles. A row-count check would have passed all six; comparing the files to
   each other is what found it.
5. **Widening a filter needs proof, not reasoning.** Removing three null-guards looked obviously safe
   because the FKs are NOT NULL. Confirming that with a query — 0 rows violate them — cost one
   command and turned "obviously" into "measured".

---

## 2026-09-04 — Shared design tokens keep getting shadowed by page-local `:root` overrides

DEF-406 was not a colour decision gone wrong; it was `--auth-left-gradient` being defined once
correctly in `wwwroot/css/auth.css` and then re-declared inline in three separate views. The shared
token was doing its job — three pages were opted out of it, silently, and nobody could see the drift
without opening all four files side by side.

**The pattern to watch:** a `:root { --token: ... }` block inside a view's `<style>` element. It
looks like a "page-specific override" (the comment in each file literally said so) but it overrides
the *global* token for that page, so the page stops tracking the design system permanently.

**Suggested check, cheap to run before any UI batch:**

```
grep -rn ":root" Views/ | grep -v "^Views/Shared/"
```

A page-scoped class (`.login-left { background: ... }`) would have been visible as an override;
re-declaring the token was not.

**Refinement after doing it:** that grep over-reports. `SignUp.cshtml` keeps a legitimate `:root`
block for *sizing* tokens (container width, logo height, header font size) — page-specific sizing is
a reasonable use. The smell is specifically a view redefining a **shared theme/colour** token. Grep
the token names, not `:root`:

```
grep -rn "auth-left-gradient\|--gcc-" Views/
```

**And a sharper lesson fell out of it — one about me, not the CSS.** Chasing the last override
surfaced that `Views/Account/SignUp.cshtml` has no action, no route and no inbound link, so
`/Account/SignUp` 404s. I wrote it up as orphan markup and proposed deleting it. **It is not dead —
the client asked for that screen to be built ahead of a future self-registration feature, and it is
deliberately parked and deliberately unreachable.**

The general point: *unreferenced is not the same as unwanted.* "No caller" is evidence about the
code; it says nothing about intent. Deliberately-parked work is invisible in the code by
construction — it looks exactly like the thing you are trying to delete. A static sweep for "views
with no corresponding action" would have flagged this file every time and been wrong every time.

**What to do instead:** when something scans as dead, the cheap move is to ask the owner before
proposing removal — and when the answer is "keep it", write the intent **into the file**, not only
into a tracking doc. `SignUp.cshtml` now carries a `DELIBERATELY PARKED — DO NOT DELETE, DO NOT WIRE
UP` header for exactly that reason, so the next sweep (human or agent) reads the reason at the point
of temptation rather than re-litigating it. Intent that lives only in a register does not travel
with the file.

**Related:** DEF-406, [[feedback_one_modal_design_gcc_modal]] — same shape of problem as hand-rolled
modals: a second copy of something that already had one source of truth.

---

## A checked-in file is a photograph, not a measurement (2026-09-04)

Two separate false findings in one session, same root cause: a file in the repo read as if it were
the current state of the system.

1. **`_regression/_artifacts/cert-export-admin.csv`** — I reported 9 certificates still holding
   compound country strings. The artifact was captured **2026-09-03 17:53 UTC**, before the split
   ran. The live database has zero. What made it convincing was that the *same file* held correctly
   split rows for other certificates, so it read as a half-finished job rather than a stale snapshot.
2. **`_regression/_results/02-data-scoping.json`** — phase 3 graded today's export against a
   population count from the previous day and reported four FAILs with a 193-row delta. Every delta
   is zero when both numbers come from the same day (DEF-408).

**The habit to build:** before quoting a file under `_artifacts/` or `_results/` as evidence, read
its own timestamp — `finishedAt` in the results JSON, the file mtime for artifacts — and say the
timestamp out loud in the finding. If it is not from the current run, re-measure before reporting.

**The harness change that would enforce it (DEF-408):** a phase that consumes another phase's output
should refuse a stale one — WARN "snapshot stale, re-run phase 2" instead of grading against it.
Bonus trap in the same loop: a missing population is skipped with `continue`, so the check silently
disappears rather than reporting NOT-RUN. **A check that vanishes reads as a pass.**

**Related, and this is the third instance:** the same stale-source error put "exports show only the
primary country" into a client letter — that claim came from a script doc comment written the day
*before* the export code was fixed. Corrected in place 2026-09-04, deliberately not deleted, because
the wrong claim was already out. See [[feedback_verify_deployed_asset_not_symptom]] and DEF-404.


## 2026-09-08 — the .NET test suite cannot be used as a gate in its current state

**Finding.** `GCCWebApp.Api.Tests` reports **89 failures out of 839** before any change is made. The
bulk is `Integration.PartnerApiScopingTests` and `MultiPartnerApiIntegrationTests`, which return an
empty payload for *every* partner — so they fail identically whether partner scoping works or not.

**Why it matters, in cost terms.** It cost most of an hour on CR-043 alone. A red suite cannot answer
"did I break anything?", so the only honest method is to capture a pre-change baseline, diff the
failing *set*, and reason about each difference by hand. That is a per-change tax on every future
piece of work, and it grows: the more failures accumulate, the less anyone trusts the number, and the
easier it becomes for a real regression to hide inside the noise. A suite that always fails is worse
than no suite, because it costs the same to run and tells you nothing.

**What it is not.** Not a product defect. These are fixtures and harness setup, not application code —
the same 89 fail on a clean checkout.

**Suggested approach, cheapest first.**

1. **Triage in one pass, don't fix in one pass.** Split the 89 into (a) fixtures that never got
   updated when the code moved — the `DEF-404` country-filter fixture found today is exactly this
   shape, one line to fix; (b) integration tests whose `WebApplicationFactory` or seed no longer
   stands up; (c) anything that is a real bug hiding in the pile. Only (c) is urgent.
2. **Fix the harness before the assertions.** If the partner integration factory returns empty for
   everyone, one root cause is probably worth ~24 of the failures.
3. **Then make green the standard** and add the suite to the pre-deploy checklist as a gate. Until it
   is green it cannot be one, which is why it is currently absent from Gates 0–10.

**Rough cost.** Half a day to triage and categorise all 89 with a one-line cause each; the fix effort
is unknown until that is done, but the integration-factory cluster looks like a single root cause.

**Related pattern already logged:** [[feedback_test_below_the_defect_layer]] — a green test at the
wrong layer proves nothing. This is the mirror: a red test at any layer proves nothing either.

---

## Verification scripts mint partner API keys and never revoke them (2026-09-09)

**What it is.** Ten Amazon partner API keys (`PartnerApiKeys` ids 8–17) are **active** on the test
environment right now, every one of them created by a verification script that mints a key, uses it
for a few seconds, and exits. `amazon-contract-diff` alone has left five. None of them revokes what it
creates, so the count only ever goes up — five more were added in a single hour of work on 2026-09-09
before the new export script was taught to clean up after itself.

**Why it matters.** A partner API key is a credential that reads the whole Amazon dataset. This is a
pre-launch environment, so the exposure today is limited — but the *habit* travels to production with
the scripts, and there is no way to tell an abandoned key from a live integration key by looking at
the table. Id 6 is the key published in the Amazon Developer Guide PDF and genuinely must stay active;
ids 8–17 look exactly like it.

**The fix is small.** `2026-09-09-amazon-payload-export.py` shows the shape: capture `data.id` from
the mint response and `DELETE /api/admin/partner/apikey/{id}` as soon as the pulls finish — before the
file-writing steps, so a later error still cannot leave a credential behind. Roughly four lines per
script.

**Suggested action.**

1. Revoke ids 8–17 (Shawn's call — they are not obviously in use, but revoking is one-way).
2. Add self-revoke to `amazon-contract-diff.js`, `def358-api-effective-date.js`,
   `cr171-multipartner-isolation.js` and any other script that calls `/apikey`.
3. Consider having `GenerateApiKey` accept an expiry so a forgotten key dies on its own — the
   `ExpiresAt` column already exists and is unused by every one of these scripts.

**Rough cost.** An hour for steps 1–2. Step 3 is a small CR, only worth doing if partner keys are ever
minted by anyone other than a test script.

---

## Cold start: the first INTERNAL certificate query was 38-42 s twice, 1.7 s later — unresolved (found 2026-09-10)

> **CORRECTED the same evening, before this was acted on.** The section below originally described
> this as the app's cold start and as the experience of every user after an app-pool recycle.
> **Follow-up measurement does not support that.** On a freshly restarted process the **public**
> tile endpoint — the only one with a client-side abort ceiling — returned in **0.8 s**, and the
> internal endpoint returned in **1.7 s** minutes later. The 38-42 s figures are real and were
> reproduced twice back-to-back, but they are **not reliably reproducible** and the cause is not
> established. Read what follows as an unexplained intermittent, not a known cold-start cost, and
> **do not spend half a day on it before reproducing it.**
>
> **It does not gate a deploy.** Gate 6 asks that cold time clear any client-side
> `AbortSignal.timeout(...)` with margin. The only such ceiling is 30 s on the **public** view
> (`certificate-tile-view.js:260`), and that endpoint measured 0.8 s cold. The internal and external
> views have **no** abort ceiling, so a slow first load there degrades to a long spinner, not an
> error.

**What was measured.** On a freshly started dev server, the first request to
`/api/certificate-tiles-internal` returned in **37.9 s** with the view in tile mode and **42.3 s** in
list mode. Same single request, same 6,019-certificate dataset; the gap between the two is variance
in the warm-up, not a difference between the views. Every subsequent load in the same session renders
in a couple of seconds.

**Why it surfaced now.** It broke a verification run. The first assertion block waited 25 s, gave up
on a page that had not finished loading, and every later assertion cascaded off that dead page — six
red results that were not real. It reads exactly like a product failure, and it cost real time to
attribute. Measuring **both** view modes on a cold server is what separated "the app is warming up"
from "the change we just made is slow".

**Why it might still matter.** *If* it reproduces, it is the experience of the first analyst to open
the certificate list after an app-pool recycle — over half a minute of spinner with no way to tell
it from a hang. That "if" is doing real work: the same endpoint was 1.7 s on a later attempt, and the
public list was 0.8 s cold. Both slow readings were taken while the machine was also running builds
and several headless Chromium instances, which is the most likely confound and the first thing to
rule out.

**Suggested action.**

0. **Reproduce it first.** It did not reproduce on the follow-up attempt. Restart the app with
   nothing else running on the machine and time the first authenticated
   `/api/certificate-tiles-internal?page=1&pageSize=20`. If it comes back in seconds, close this
   item as a measurement taken under load rather than a real defect.
1. Only if it reproduces: measure it on the **test environment**, not dev — dev shares a machine
   with builds, so the absolute number is not trustworthy. The shape (first-request-only) will be.
2. If it reproduces, the usual suspects in order: EF model building on first query (an EF compiled
   model moves this to build time), the absence of a warm-up ping after recycle, and the cold plan
   for the list query itself.
3. Cheapest partial mitigation regardless of cause: an IIS/Plesk **Application Initialization**
   warm-up hitting the certificate list endpoint after a recycle, so the cost lands on the server
   rather than on the first analyst.

**Rough cost.** Ten minutes to attempt a clean reproduction — do that before anything else. Half a
day to diagnose properly on test *only if it reproduces*. The warm-up ping is well under an hour and
is cheap insurance either way.

**Related, seen at the same time, not chased:** a `pageerror: Invalid or unexpected token` fires on
the internal certificate view, the public tile view, and the **anonymous Home page**. All three
certificate JS bundles pass `node --check`, so it is a Razor-rendered inline script — most likely
unescaped data reaching a `<script>` block. It is silent to users today, but a page-level syntax
error means whatever follows it in that block never runs. Worth half an hour to locate.
---

## The certificate import template emits two columns the importer throws away — RESOLVED

> **RESOLVED 2026-09-10 as DEF-421** — both columns are now read, resolved parent-scoped and
> written, verified end-to-end against dev. Item 3 below (the generic "unconsumed column" warning)
> is the part that is **still open**, and it is the one that stops this class recurring; the audit
> script written for DEF-421 is the seed for it. Item 1's decision went to "wire both up". The
> same audit also turned up **DEF-422** (a `Company Email` column with no `Company` column imports
> the email address as the company name) — raised, not fixed.

**Found 2026-09-10 while fixing DEF-418.**

`DownloadTemplate` (`Controllers/CertificateImportController.cs:468`) emits 24 columns. Two of them
are never read by the importer:

- **`Cert Subcategories`** (column 3)
- **`Subcategory`** (column 11)

There is no mapping array for either in `CertificateImportService.cs` — every other column in the
template has one — and the service comments say so outright at `:866`: *"Subcategory still resolves
as null (no column yet)"*.

**Why it matters.** An analyst downloads the template, sees a `Subcategory` column, fills it in for
several hundred rows, imports, and the values are **silently discarded**. Nothing warns, nothing
rejects, and the preview screen has nothing to show because the field was never parsed. This is the
same shape as DEF-347, where a `COUNTRY`-headed file silently imported blank countries for months —
and that one was only found by reading the mapping arrays, not by anything failing.

The risk is highest precisely now: the client has just started asking how to use the template for
edits (QC #263), so template-driven editing is about to get more use, not less.

**Suggested action.**

1. Decide which way it goes. `Subcategory` is probably worth wiring up — the CR-186 product-identity
   resolution at `CertificateImportService.cs:1250-1290` already takes a `subcatId` parameter, so the
   plumbing exists and only the column-read and lookup are missing. `Cert Subcategories` needs a
   product decision first: it is a certificate-level multi-value field, so it needs a separator
   convention like Partner and Country already have.
2. If either is **not** going to be wired up, remove it from the template. A column that silently
   does nothing is worse than no column.
3. Whichever way: add a preview-time warning for any template column the parser does not consume.
   That generalises the fix and would have caught DEF-347 too.

**Rough cost.** `Subcategory` wire-up, roughly half a day including a preview-diff check. Dropping
either column from the template, under an hour. The generic "unconsumed column" warning, half a day,
and it is the one that stops this recurring.

## Regression harness — found during the 2026-09-10 full run

1. **A refusal must not exit 0.** `_regression/08` and `09` print `refusing to run against ... without
   ALLOW_REMOTE=1` and then exit 0. Any chain runner that branches on exit code scores that as a pass.
   Exit non-zero on refusal.
2. **Phases that take a base URL should not silently default to localhost.** `08`/`09` read
   `argv[2] || 'http://localhost:5101'`, so a run aimed at the test environment grades localhost and
   says `PASS`. Either require the base explicitly or print the resolved target in the result line.
3. **Never grade an admin API call by HTTP status alone.** `/Admin/CreateUser` returns 200 with
   `success:false` on validation failure. `07-verify-fixes.js` checks `status >= 400` and therefore
   reports PASS for a user it never created. Parse the body's `success` flag — the controller comments
   already warn about exactly this.
4. **Keep test payloads in step with the view models.** That same call sends `username` / `firstName` /
   `lastName` / `isActive`, none of which exist on `CreateUserViewModel`, and omits the `[Required]`
   `name`. A payload-shape assertion in the harness would have caught it the day the model changed.
5. **Teardown failure should be loud and re-runnable.** `07` left certs 26-0659 / 26-0660 in the dev DB
   with only a FAIL line. Worth a `--cleanup-only` mode that sweeps by marker.
6. **One runner for the whole suite.** There is no `_regression/run-all.js`; the phase list lives in
   operators' heads, which is how 08/09 got run against the wrong target. `_security`, `_views` and
   `_crud` each have one.
---

## A defect register that lags the code can hold up a release

**Found 2026-09-11.** Process, not code. No fix, a suggestion.

DEF-420 was fixed and committed on 2026-09-10. Its register entry still read **"RAISED — not fixed"**
the next day. On the strength of that line I recommended holding a deployment to fix work that was
already done. The error was caught only because the fix was attempted and the code turned out to be
already correct.

This is structural, not carelessness. Several streams commit to this repo in a day; the code is
updated in the same commit as the work, and the register is updated by whoever remembers. The
**Status** field is the one readers trust most and the one that decays fastest.

**Suggested action.**

1. **Cheapest, do this one:** at the top of the defect register, state that a Status line is
   provisional and must be confirmed against `git log -1 -- <file>` before it is quoted in a release
   decision. Ten minutes, and it would have prevented this.
2. Make it mechanical: a pre-deploy gate step that lists every entry whose Status says
   "RAISED"/"not fixed" while the files it names have changed since the entry's date. That is a
   ~20-line script over the register and `git log`, and it belongs next to Gate 9, which already does
   shipped-vs-fixed reconciliation. **~1-2 hrs.**
3. Cheaper habit with most of the benefit: when a fix is committed, put the DEF number in the commit
   message. `git log --grep=DEF-420` then answers "is this fixed?" in one command. Today's commits
   are titled "updates", so that question can only be answered by reading diffs.

**Rough cost.** Item 1 is ten minutes and is worth doing today. Item 3 is free and compounding.
Item 2 is the real fix and can wait for the next quiet gate cycle.

**Why it is worth money.** The failure mode is not a wrong document — it is a delayed release, or a
second deploy a few days later for work that was already in the first bundle.

---

## From the 2026-09-11 post-deploy regression run

**1. A fixture-drift failure is indistinguishable from a broken login — make the suite say which.**
`test.external` failed to authenticate and reported *"login failed for external: landed
/Account/Login"* against a page reading *"Invalid email or password"*. The account was fine; only its
`Username` had drifted. It cost four diagnostic round-trips to establish that. `regression-lib`'s
`login()` already knows the account's email — when a username login fails it should retry with the
email and, if that succeeds, report **FIXTURE DRIFT: username no longer matches, email works**
rather than a generic FAIL. Same shape as the `assertAccountsMatchTarget()` guard already in that
file, which exists for exactly this class of mistake.

**2. "Full regression" needs a manifest that states which phases are remote-eligible.**
Of thirteen phase files, three (07, 11, 12) are localhost-only *by design* and one suite
(`_security`) would return fabricated PASSes if aimed at TEST. Nothing in the directory says so — it
is discoverable only by reading each file's header. A short table in `_regression/README.md`
(phase → local-only / remote-OK / gate mode) would stop a future run either skipping real coverage
or reporting green on a suite that never actually ran.

**3. The security suite's gate mode is wrong for the only remote target that exists.**
`_security/_lib` passes the test-env gate with **Basic auth**; gccwebapp.com is **Cookie** mode.
The failure is silent — a 200 gate page for every URL, so every assertion "passes". `regression-lib`
already has a correct `passGate()`. Porting it into `_security/_lib` would make the P0 security gate
runnable against the deployed environment, which is currently the one place it cannot run.

**4. Import Undo is the only import path with no live coverage.**
`imp.undo` has WARNed for several runs: the batchId is not exposed in the History page markup, so the
harness cannot invoke Undo. Rendering it as a `data-batch-id` attribute is a small view change and
would close the gap. Low priority, but it is a destructive path with zero live assertions.

**5. Anonymous 302s prove nothing about a config leak.**
Checking `/appsettings.Production.json` without a gate cookie returns 302 for a leaked file and 302
for a protected one alike. The pre-deploy checklist's leak check should specify **behind the gate**,
where the answer is an unambiguous 404.

---

## 2026-09-14 - Stop issuing partner documents with blanks in them

**Trigger.** Client on the Integration Guide template: *"I don't know how I would fill in some of
missing items when I take the template and try and apply to Novi for example."* Eight of her fifteen
comments are the same question asked in eight places - "where would this info come from?"

**Diagnosis.** We handed the client a template whose per-partner values live in a system only we
administer. That is not a documentation problem, it is a handoff problem: the blanks can only be
filled from the API key record, and today only we create keys.

**Change.** Fill the *Your Connection* panel at the moment the key is issued and send back a completed
guide, rather than a template. Two benefits beyond convenience:

1. The document can never disagree with the key that was actually issued - which is the exact failure
   the template's own "how to use this" box warns about (a key scoped to a subset of endpoints
   returns 403 on the others while the guide claims "All").
2. It removes a step the client cannot perform, instead of documenting it.

**Wider version of the same lesson.** Only four of the panel's eight rows genuinely vary per partner.
The other four were written as `[PLACEHOLDER]` anyway, which is what made the document look
unfillable. **A placeholder for a constant is a bug in the document.** Before shipping any template,
separate "varies" from "looks like it varies".

## 2026-09-14 - A client reporting a broken link is not automatically our defect

Client reported a page in the Integration Guide with no content. It resolved to the *directory index*
of a GS1 US GitHub Pages site - GS1's own placeholder. The two URLs actually referenced
(`cert_schema.json`, `cert_context.json`) both return full content, and neither is a link we chose:
they are `@context` and `$schema` values the GS1 data model requires inside the payload, which Word
auto-linked.

Had we "fixed" this we would have changed a partner-facing contract to solve someone else's missing
index page. **Check what the URL actually is and who owns it before treating a client's link report
as a defect.** The right fix was a one-line pointer to the upstream repository.

## 2026-09-15 — A set comparison cannot see a duplicate

DEF-425 delivered four certificates twice and dropped four others. The Amazon payload export ran its
full reconciliation over that payload and reported **zero findings**, because its membership check
compares *sets* of certificate numbers in both directions — and a set silently collapses the
duplicates that were the whole symptom. The `pagination.totalCount` cross-check passed too: the count
was a correct 212 while the row *identities* were wrong.

Two habits this should change:

1. **Assert row count == distinct key count** on any paged pull, as its own line. One cheap
   assertion would have caught this on 2026-09-10 rather than five days later.
2. **Page-boundary bugs need a repeated pull, not a clean one.** The defect is non-deterministic;
   a single good result is weak evidence. Pull the same page twice and compare the row sets.

This generalises beyond the Amazon feed — it applies to every export and reconciliation script in
`Scripts/data-ops/` that walks a paged API.

## 2026-09-15 — Scope words are claims about sets

*First, never, always, all, only, every, none.* Each asserts something about a population. Using one
without having enumerated that population is an unverified claim wearing the costume of a verified
one — which is worse than a hedge, because it reads as extra rigour.

**The test, applied before the sentence leaves:** would this claim need a *different kind* of
evidence than the evidence I just gathered? "Reachable for the first time" needs configuration
history; what had actually been gathered was one file's current contents.

**The fix is to downgrade the word, not to add a hedge.** "Unreachable in this session's
configuration" is both true and more useful than "possibly the first time".

Now a Part 1 always-check in the `gate-check` skill, and carried forward to future projects via
`~/.claude/skills/lessons-learned/CARRY-FORWARD.md`.

## 2026-09-15 — Write a role check as "is this an insider", never "is this an outsider"

Every permission cell in the role matrix comes down to a check in code, and the check can be written
two ways that look equivalent and are not:

- **Closed set (safe):** `Admin || Analyst` — name who is allowed, exclude everyone else by
  construction. A role added later is denied by default.
- **Open set (unsafe):** enumerate Admin / Analyst / External and let the last `else` catch the rest.
  A role added later inherits that `else`.

DEF-198 is that failure already spent: Partner was not in the enumeration, fell through to
`"Public"`, and the export path treated `"Public"` as unrestricted. The bug was not a missing rule —
the rule existed and was documented. It was that the check was written as an open set.

**Two habits:**

1. **Grep the shape, not the rule, when auditing a matrix cell.** "Does the code implement the rule"
   is the wrong question if the implementation is an enumeration — it implements it for today's roles
   only. Ask instead what a role not in the list would get.
2. **A hardcoded permissive flag must justify itself at the call site.** `includeInternalTags: true`
   is correct in an `AnalystOrAdmin`-gated action and catastrophic two refactors later. If the reason
   it's safe lives in an attribute above the method, it should be restated in a comment at the flag —
   or, better, be derived rather than hardcoded.

This generalises past internal tags to every cell of the role matrix, and it is the same class as the
External-umbrella confusion: **External is an umbrella over Company and Partner, not a peer of
Admin/Analyst** — so any check that treats "External" as one value in a flat list of roles is already
an open set even before a new role is added.


---

## 2026-09-16 - Un-ignoring is a publish

From the fresh-clone setup work. Three habits:

1. **Audit ignore rules with `git check-ignore -v` against real paths, never by reading patterns.**
   `**/Archive/` was written to exclude superseded code and had been eating 130 documentation files,
   including the entire original requirements archive, for months. Ignored files never appear in
   `git status`, so there is no passive signal - the gap is only visible if you go looking. A quick
   standing check: compare `find <dir> -type f | wc -l` against `git ls-files <dir> | wc -l` for any
   directory that is supposed to be fully tracked.

2. **Scan contents before un-ignoring anything, and stage before you commit so the scan has
   something to read.** An ignore rule has been protecting against everything that matched it, not
   just the thing it was written for. Removing it publishes all of that at once. Here, 21 of 133
   files carried a plaintext production database credential. `git add` then grep the staged set for
   credential shapes; the commit is the last reversible moment, and only while it is unpushed.

3. **When a setup fails, separate "the artifact is absent" from "the environment cannot reach it."**
   The reported symptom was missing seed data; the cause was a committed connection string pointing
   at another machine. Check whether the file is tracked *before* concluding it was never committed -
   `git ls-files <path>` answers it in one call and stops the search going the wrong direction.
