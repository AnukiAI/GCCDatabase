# Deploy runbook — `GCCWebApp-deploy-20260917-0003.zip`

**Target:** gccwebapp.com (the pre-launch TEST environment)
**Status:** **BUILT AND GATED 2026-09-17. NOT UPLOADED.** Upload is Shawn's step.

| | |
|---|---|
| **Zip** | `deployment-drops/GCCWebApp-deploy-20260917-0003.zip` |
| **In-zip `GCCWebApp.dll` md5** | **`05bb58206cd39700`** (6,712,320 bytes) |
| **Entries / size** | 342 entries · 61.17 MB |
| **Built from** | branch `ci-regression`, HEAD `4ed13f13` + 8 uncommitted working-tree files |
| **Verifier** | `verify-bundle.py … --no-migrations=20260908175939_CR045_CertificationResultsVerbatimSort` → **0 failures**, 1 expected WARN |

> ## ⚠️ THREE BUNDLES NOW SHARE 342 entries / 61.17 MB
>
> `20260915-1641`, `20260916-1023` and this one are **identical in entry count and size**. Only the
> DLL md5 tells them apart:
>
> | Bundle | DLL md5 | State |
> |---|---|---|
> | `20260915-1641` | `42f7efa637fd6591` | superseded |
> | `20260916-1023` | `58569001f47314c2` | **currently live** |
> | **`20260917-0003`** | **`05bb58206cd39700`** | this one |
>
> **Check the hash, never the size.** A previous pair matched at 339 entries / 61.1 MB while one of
> them was missing an entire fix.

---

## What it carries

Three items from the client's 9/11 QC log. **8 files, +209 / −12** against the live build
(documentation changes in the same working tree are excluded from the publish by the pubxml and do
not ship).

| Ref | QC | Change | Files |
|---|---|---|---|
| **CR-051** | #266 | Status filter on the internal certificate listing (Admin/Analyst only) | `Controllers/Api/CertificateTileInternalApiController.cs`, `Views/Certificate/CertificateTileViewInternal.cshtml` |
| **CR-052** | #267 | "View all pages" — opens the stored PDF, on all five preview surfaces | `CertificateInternalView`, `CertificateExternalView`, `CertificateSingleView`, `Edit.cshtml`, `wwwroot/js/certificate-create.js` |
| **DEF-418** | #263 | Import Certificates **modal** copy corrected; page gains Tags + the two DEF-421 columns | `Views/Certificate/CertificateTileViewInternal.cshtml`, `Views/CertificateImport/Index.cshtml` |

**CR-051 carries a bug fix the CR did not originally describe.** The filter-options endpoint returned
PascalCase enum names (`ClientApprovedPaid`) while `Certificate.Status` stores the display form
(`Client Approved - Paid`). Four of the eight statuses would have matched nothing. Fixed in the same
change.

**DEF-418 is the *second half* of an item already marked shipped.** The 9/10 fix corrected the
standalone import page, which reached TEST in the 9/15 release. The client imports from the **modal**,
which was never touched — she rejected the item on 9/11 and was right. `DEF-418` already appears in
the deploy baseline, so **Gate 0B does not flag it**; that is a known blind spot, recorded in the
register entry. Do not let the baseline close it.

## What it does NOT undo

**DEF-426 (live since 2026-09-16) is preserved.** `Controllers/Api/v1/CertificatesController.cs` is
**unmodified against HEAD** (`git status` clean for that path) and still carries all 4 `DEF-426`
sites. Since HEAD is the tree the live bundle was built from, that file is byte-identical to live.

A bundle is a full overwrite, so this was checked rather than assumed. Nothing else in the working
tree belongs to another work stream — the parallel stream's changes were committed in `d67de8d8` /
`4ed13f13` and are included here by inheritance.

## Database

**No database step.** Newest migration in the tree is `20260908175939_CR045_…`, unchanged since the
last two deploys; none of these three changes touches a model or migration. Auto-migrate is **OFF** on
TEST.

`migrations/apply-migrations.sql` is present (full history, idempotent, 3,236 lines) and has the two
post-deploy backfills appended (`CR-043-productid-status-backfill.sql`, `CR-171-partner-backfill.sql`)
exactly as in the previous two bundles. Both are documented idempotent; re-running them is a no-op.
**Nothing needs to be run for this deploy.**

## Gates

| Gate | Result |
|---|---|
| **0 uploads backup** | **OUTSTANDING — human, before extract.** Back up the server's upload store first. |
| **0B resolved-but-unshipped** | 2 PENDING = CR-051, CR-052 — **both in this build** ✔ (DEF-418 not flagged; see above) |
| 1 / 2 UI + responsive | PASS — 5 viewports on the status filter, the all-pages control, and the import modal |
| 3 data scoping / RBAC | PASS — External denied a cert outside scope (404); anonymous denied an expired cert (404) |
| 4 Partner API contract | n/a — untouched |
| 5 traceability | PASS — every change carries a CR/DEF and a QC id |
| 6 performance / EF | n/a — no query shape change (CR-051 uses an existing, already-built filter path) |
| 7 pending migrations | n/a — none |
| 8 CRUD write paths | n/a — read and copy only |
| 9 static integrity | PASS — `verify-bundle.py` 0 failures |
| 10 config | PASS — no DataProtection keys, no dev config, no uploads in the zip (asserted independently) |
| 11 / 12 close-out, email | after upload |

**Bundle marker check — 13/13, including a negative control** that proves the search can fail:

```
CR-051  id="status-content" · data-filter="status" · GetAllStatuses          PRESENT
CR-052  ViewCertificateFile · "All pages" · createObjectURL · revokeObjectURL PRESENT
DEF-418 "Certificate # is not required" · "To edit an existing certificate"   PRESENT
        "backup of those certificates is saved automatically" · "Cert Subcategories"
DEF-418 OLD wrong line  Required:</strong> CERTIFICATE_NUMBER                 ABSENT ✔
control ZZ_NOT_IN_THIS_BUILD_MARKER                                           ABSENT ✔
```

Razor views compile into `GCCWebApp.dll`, so view copy was asserted against the DLL bytes, not
against files on disk.

## What is live right now — probed 2026-09-17, not read from the log

`node Scripts/smoke-prod/whats-deployed.js` against `https://gccwebapp.com` (read-only, gate passed):

- **DLL:** the DEF-337 batch is deployed. Control route behaves correctly (302 → login).
- **wwwroot:** 8 same / **1 differ** / 0 unreachable.

**The single difference is this bundle's own unshipped change, not a half-applied deploy.** The
script compares live against the *working tree*, which carries CR-052's edit to
`wwwroot/js/certificate-create.js`. Proven rather than assumed:

| Version | md5 |
|---|---|
| live on gccwebapp.com | `36d72002412e` |
| `20260916-1023` bundle (the live one) | `36d72002412e` |
| HEAD, converted to CRLF | **`36d72002412e`** |
| working tree (CR-052 edit) | `4a709ea39abf` |
| **`20260917-0003` bundle (this one)** | **`4a709ea39abf`** |

So **live wwwroot is fully current with the deployed bundle** and the script's "PARTIAL" verdict is a
false alarm in this context. (Reading HEAD through `git show` yields LF line endings and hashes
differently — `9b4525c481d7` — which briefly looked like a third version. It is not.)

> ### Limitation worth knowing: this probe cannot tell recent bundles apart
>
> Its DLL signal is the **DEF-337 DocumentController removal — an August-era marker**. It proves the
> live DLL is post-DEF-337 and nothing finer, so it **cannot** distinguish `20260915-1641` from
> `20260916-1023`, and it does **not** independently confirm DEF-426 is live. That claim still rests
> on the 9/16 runbook's post-deploy verification, not on this probe.
>
> **This deploy fixes that for next time.** `/js/certificate-create.js` changes with this bundle, so
> after upload it becomes a precise, cheap signal that *this specific build* landed:
>
> ```
> before: 36d72002412e   (every bundle 20260807 → 20260916)
> after:  4a709ea39abf   (20260917-0003 only)
> ```

## Upload steps

1. **Back up the server upload store** (Gate 0 — not done, and not doable from here).
2. Upload `GCCWebApp-deploy-20260917-0003.zip` to Plesk.
3. **Confirm the uploaded file's DLL md5 is `05bb58206cd39700`** before extracting — the size will
   match two older bundles.
4. Stop the app / extract over the existing files.
5. **No SQL to run.**
6. Recycle the app pool.

## Post-deploy verification — through the client's path, not an equivalent one

1. **The import modal.** Certificate listing → **Import**. The instructions must say *"Certificate #
   is not required"*. This is the exact screen the client photographed; checking the standalone
   `/CertificateImport` page instead is what caused this reopen.
2. **Status filter.** Filter panel → **Status**, below Additional. All **8** statuses, with display
   names (`Client Approved - Paid`, not `ClientApprovedPaid`). Tick two → union, not intersection.
3. **All pages.** Open a multi-page certificate and use the new control beside zoom/download.
   **Use `26-0621`** — the client's own example, which has no PDF in the dev store and therefore could
   not be exercised locally. It is the first one she will click.
4. **Confirm DEF-426 still holds:** the partner feed should still report **20,312** associations /
   16,078 distinct ASINs / 212 certificates. A regression here would mean the bundle undid it.
   **This is the only check that confirms DEF-426 is live** — `whats-deployed.js` cannot, because its
   DLL signal is an August-era marker (see above).
5. **Confirm THIS build landed**, not merely *a* build — re-run `whats-deployed.js` and require
   `/js/certificate-create.js` to have flipped `36d72002412e` → **`4a709ea39abf`**. Size and entry
   count cannot tell this bundle from the previous two; this hash can.
6. **Handler still active:** `/appsettings.Production.json` must NOT return 200.

## Afterwards

- Mark CR-051, CR-052 and DEF-418 deployed in their registers, and flip the
  `## 2026-09-16` section in `shipped-2026-09.md` from *"Built, not yet deployed"* to live with the
  date. A status written at fix-time and never updated stops the register being a deploy ledger.
- **QC #263 stays open on the client's log** until she confirms. It is hers to close, not ours.
