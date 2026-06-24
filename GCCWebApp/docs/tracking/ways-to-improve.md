# Ways to Improve

Running list of improvement ideas surfaced during the project. Newest at top. These are
suggestions/process improvements — distinct from defects (`defect-register.md`) and change
requests (`change-request-register.md`).

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
