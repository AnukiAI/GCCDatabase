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
