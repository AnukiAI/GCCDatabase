# Deploy runbook — 2026-09-15 → gccwebapp.com (test) — DEF-425 partner API paging

**Bundle:** `deployment-drops/GCCWebApp-deploy-20260915-1641.zip` — **342 entries, 61.17 MB**
**In-zip `GCCWebApp.dll` md5:** `42f7efa637fd6591` (distinct from every prior bundle — checked against all 42 zips on disk)
**Verifier:** `python Scripts/deploy/verify-bundle.py deployment-drops\GCCWebApp-deploy-20260915-1641.zip --no-migrations=20260908175939_CR045_CertificationResultsVerbatimSort` → **0 failures**

> ## STATUS: BUILT AND VERIFIED — **NOT UPLOADED**
>
> The upload is a human step through Plesk. Nothing has been pushed to gccwebapp.com by this
> session. Gate 0 (back up the server's `wwwroot/uploads/`) is **owed before you extract**.

---

## ⚠️ Correction: what is actually live is **2026-09-11**, not 2026-09-08

The gate results log at the top of `pre-deploy-agent-checklist.md` presents
`GCCWebApp-deploy-20260910-2128.zip` as the newest bundle and marks it **NOT UPLOADED**. Reading only
that section, you would conclude the last live build is 09-08 and that this deploy must carry
CR-048, CR-049, DEF-419, DEF-420 and DEF-342.

**It does not. Those are already live.** `GCCWebApp-deploy-20260911-1319.zip` exists, supersedes the
09-10 bundle, is logged in a *separate* section ~1,500 lines further down under
"Bundle 2026-09-11 — CSV import/export integrity batch", and carries no status line at all.

Proven live on 2026-09-15 by two independent signals, because either alone can mislead:

| Signal | Result | What it proves |
|---|---|---|
| `GET /js/csv-parse.js` | HTTP 200, md5 `ab2688b2d562ec57` = working tree | the **wwwroot** generation is 09-11's (the file did not exist before 09-10) |
| `GET /Certificate/CertificateTileView` | markup references `csv-parse.js` | the **DLL** carries the 09-10+ compiled `_Layout` — an asset can be hand-copied, compiled Razor cannot |

**Do not "restore" the 09-10 or 09-11 bundles.** Either would be a downgrade.

---

## What this bundle actually changes — two items

| Ref | File | What it does | Risk |
|---|---|---|---|
| **DEF-425** | `Controllers/Api/v1/CertificatesController.cs` | Unique `.ThenBy(c => c.Id)` tiebreaker on all four paged endpoints, so partner paging stops repeating and dropping rows | Partner-facing; see verification below |
| *(no ref)* | `Program.cs` | Startup now logs **DB server / catalog / user** (never the password) so a missing `appsettings.Secrets.json` overlay cannot silently point the test site at live client data | Diagnostic only, try/catch wrapped |

The `Program.cs` change came from a parallel stream, not this session. It is ~29 lines, prints to
stdout at startup only, and cannot take the site down — reviewed and considered safe to ship. It has
**no DEF/CR reference**, which Gate 5 would flag; raising it here rather than inventing one.

Nothing else in the working tree is application code — the remaining diffs are docs, registers and
the payload-export script.

---

## Do you need the database step? **No.**

No pending migrations. Test is already at the CR-045 high-water
(`20260908175939_CR045_CertificationResultsVerbatimSort`) and this bundle adds none.

`deploy.ps1` still generated `migrations/apply-migrations.sql` (full history, 38 migrations, 3,236
lines) because it always does, and appended two post-deploy data scripts
(`CR-043-productid-status-backfill.sql`, `CR-171-partner-backfill.sql`).

**Do not run it.** The verifier's one WARN is exactly this: a full-history script makes SQL Server
compile every guard block before `IF NOT EXISTS` can skip it, so a block referencing a
since-dropped column aborts the whole run (DEF-386).

**TEST auto-migrate is OFF** (`AutoMigrateOnStartup is false` — asserted in the bundle by the
verifier), so extracting this zip will not apply anything on its own. That is the intended state.

---

## Pre-upload gate status

| Gate | Fires? | Result |
|---|---|---|
| 0 uploads backup | **YES** | **OUTSTANDING — human, before extract** |
| 1 UI / design guide | no | No view or CSS changes in the delta |
| 2 cross-role + responsive | no | No UI surface changed |
| 3 data scoping / RBAC | **YES** | Carried from 09-11 (PASS 57P/0F). The DEF-425 change alters row **ordering**, not the partner/company filter predicate — which is untouched above it |
| 4 Partner API contract | **YES** | **Response shape unchanged** — only `ORDER BY` changed. Functionally verified below, stronger than a shape diff |
| 5 traceability | **YES** | DEF-425 logged in the register; `Program.cs` change carries no ref (noted above) |
| 6 performance / EF | **YES** | `ORDER BY … , Id` on an indexed PK. Local paged walks returned in normal time; no new joins or includes |
| 7 pending migrations | no | None — see above |
| 8 CRUD write paths | no | Read-only endpoints; no write path touched |
| 9 static integrity | **YES** | `verify-bundle.py` → 0 failures, 1 expected WARN (full-history script) |
| 10 config points at data | no | No config change |
| 11 close-out | after upload | `node Scripts/deploy/close-out.js` |
| 12 outbound email | no | No mail path touched |

---

## Verification — what proves DEF-425 is fixed

### Already done, pre-deploy (localhost, dev DB)

`def425-verify.py` walked both partner endpoints at **pageSize=7** — deliberately small, to
manufacture **32 page boundaries per endpoint** instead of the 2 a normal `pageSize=100` pull
produces. That small-page trick is the whole reason this defect stayed latent for days.

| Endpoint | rows / distinct | vs `totalCount` | two independent walks identical? |
|---|---|---|---|
| `/api/v1/certificates` | 218 / 218 ✅ | 218 ✅ | ✅ |
| `/api/v1/certificates/gs1` | 218 / 218 ✅ | 218 ✅ | ✅ |

**The third column is the one that matters.** The defect is a race — it ran clean three times while
fully exposed. A single good walk is not evidence. Two full walks returning byte-identical ordered
lists is.

### To run AFTER the upload

```
python Scripts/data-ops/2026-09-09-amazon-payload-export.py test
```

The export now **asserts row count == distinct key count** on both endpoints and exits non-zero if
they differ — the check whose absence let this run clean on 09-09, 09-10 (twice) and 09-11.

Expected, if the data has not moved since 2026-09-15 15:07:

| Measure | Expect |
|---|---|
| certificates delivered | **212 rows / 212 distinct** (was 212 / **208**) |
| ASIN rows | **20,380** (was 20,908 — inflated by 537 duplicates, minus 9 lost) |
| distinct ASINs | **16,078** (was 16,069) |
| `25-0479`–`25-0482` | **present** (were missing) |
| `23-0925`/`0926`/`0927`/`0991` | **once each** (were twice) |
| membership findings | **0** |

The invariant — `rows == distinct` — holds regardless of how the data moves. The absolute numbers
are only valid while the underlying certificates are unchanged; re-derive them from the DB if they
disagree, rather than assuming a regression.

---

## Upload — **HUMAN**, via Plesk

1. **Gate 0 first.** Zip the server's `wwwroot/uploads/` and pull it down. Publish can overwrite the
   PDF store; this has bitten this project before (2026-04-23, and a contributor to the 2026-06-15
   all-PDFs-missing incident).
2. Upload `GCCWebApp-deploy-20260915-1641.zip` and extract over the site root.
3. **Do not** upload or run `migrations/apply-migrations.sql`.
4. Confirm `appsettings.Secrets.json` is still on the server — it is deliberately **not** in the zip,
   and the new `Program.cs` startup lines will now tell you which database the site actually bound
   to. Check them.
5. Recycle the app pool.

## Post-upload

1. `node Scripts/deploy/close-out.js`
2. `python Scripts/data-ops/2026-09-09-amazon-payload-export.py test` — expect the table above.
3. **Then tell Amazon they can re-pull.** Draft is ready at
   `docs/tracking/2026-09-15-client-email-amazon-asin-count-and-prod-endpoint.md`; it promises them a
   re-pull date, so it should go out only once step 2 is green.
4. Append to `docs/tracking/shipped-2026-09.md`.
