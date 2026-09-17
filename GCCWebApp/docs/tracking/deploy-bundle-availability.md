# Deploy bundle availability

Internal inventory. **Not** a client-facing document - the stakeholder record of what shipped is `shipped-2026-MM.md`, which this deliberately does not touch.

A deploy bundle is the only way to answer "what exactly is running on the server" after the fact: you marker-check the DLL inside the zip. Two bundles have already been found identical in size and entry count while one was missing a whole fix, so the zip itself - not the log entry describing it - is the evidence. Where a bundle is gone, that check is no longer possible for the defects it carried.

Generated 2026-09-17.

| Metric | Count |
|---|---|
| Bundles cited across `docs/` | 73 |
| Present in `deployment-drops/` | 26 |
| **Cited but absent** | **47** |

## Cited but absent

`md5` is recorded where a tracking doc stated it, or where it was captured at deletion. It is enough to verify a restored copy is the real bundle.

| Bundle | md5 | Cited in |
|---|---|---|
| `GCCWebApp-FULL-EVERYTHING-20260817-2140` **(deleted 2026-09-16 - see below)** | `914c40355e28a95efd502f882fbb9203` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260504-2301` | `-` | `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260511-1054` | `-` | `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260513-0027` | `-` | `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260515-1948` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260515-2005` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260519-1044` | `-` | `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260519-1726` | `-` | `tracking/email-qc91-retest-2026-05-19.md` |
| `GCCWebApp-deploy-20260526-2054` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260526-2228` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260526-2305` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-05.md` |
| `GCCWebApp-deploy-20260603-1221` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260603-2110` | `-` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260604-1752` | `-` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260605-1234` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260605-1328` | `-` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260605-1444` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260605-1641` | `-` | `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260608-1543` | `-` | `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260609-1026` | `-` | `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260609-1146` | `-` | `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260612-1509` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260615-1737` | `-` | `tracking/change-request-register.md`, `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260616-1124` | `-` | `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260618-1027` | `-` | `tracking/change-request-register.md`, `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260619-0001` | `-` | `tracking/CR-171-multipartner-visibility.md`, `tracking/change-request-register.md`, `tracking/post-deploy-e2e-test-framework-2026-06-midbatch.md`, `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260622-1007` | `-` | `tracking/shipped-2026-06.md` |
| `GCCWebApp-deploy-20260622-1431` | `-` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260624-1345` | `-` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260625-1350` | `-` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260625-1420` | `-` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260701-1639` | `-` | `tracking/defect-register.md`, `tracking/shipped-2026-07.md` |
| `GCCWebApp-deploy-20260713-1242` | `-` | `tracking/pre-deploy-agent-checklist.md` |
| `GCCWebApp-deploy-20260713-1853` | `-` | `tracking/shipped-2026-07.md` |
| `GCCWebApp-deploy-20260720-1250` | `-` | `tracking/defect-register.md`, `tracking/pre-deploy-agent-checklist.md`, `tracking/shipped-2026-07.md` |
| `GCCWebApp-deploy-20260720-1408` | `-` | `tracking/pre-deploy-agent-checklist.md`, `tracking/shipped-2026-07.md` |
| `GCCWebApp-deploy-20260720-2037` | `-` | `tracking/change-request-register.md`, `tracking/pre-deploy-agent-checklist.md`, `tracking/shipped-2026-07.md` |
| `GCCWebApp-deploy-20260720-2137` | `-` | `tracking/pre-deploy-agent-checklist.md`, `tracking/shipped-2026-07.md` |
| `GCCWebApp-deploy-20260720-2204` | `-` | `tracking/pre-deploy-agent-checklist.md` |
| `GCCWebApp-deploy-20260720-2332` | `-` | `tracking/pre-deploy-agent-checklist.md` |
| `GCCWebApp-deploy-20260803-1422` | `-` | `tracking/pre-deploy-agent-checklist.md` |
| `GCCWebApp-deploy-20260804-1705` | `-` | `tracking/defect-register.md` |
| `GCCWebApp-deploy-20260901-1745` | `-` | `tracking/pre-deploy-agent-checklist.md` |
| `GCCWebApp-deploy-20260902` | `f2445276e8f8ee822fd458b88724447c` | `tracking/2026-09-02-deploy-runbook.md`, `tracking/pre-deploy-agent-checklist.md`, `tracking/shipped-2026-09.md` |
| `GCCWebApp-deploy-20260902-full` **(deleted 2026-09-16 - see below)** | `de72254e3fd9c42f4f56c0d035314675` | `tracking/2026-09-02-deploy-runbook.md`, `tracking/pre-deploy-agent-checklist.md`, `tracking/shipped-2026-09.md` |
| `GCCWebApp-deploy-20260908-DEF412` **(deleted 2026-09-16 - see below)** | `6549923ab9c3daea2c24dbabcfbe4c89` | `tracking/2026-09-08-deploy-runbook-CR043-CR045.md`, `tracking/2026-09-08-deploy-runbook-DEF412.md`, `tracking/pre-deploy-agent-checklist.md` |
| `GCCWebApp-deploy-20260911-1319` | `-` | `tracking/2026-09-15-deploy-runbook-DEF425.md`, `tracking/pre-deploy-agent-checklist.md` |

## Deleted 2026-09-16 in error - recovery pending

A disk-cleanup pass removed 17 deploy bundles judged unreferenced. The test used to judge them was
wrong in two ways, and **3 cited bundles were deleted as a result**:

* Reference timestamps were only extracted from strings matching `GCCWebApp-deploy-` +
  `YYYYMMDD-HHMM`, so a bundle cited as `GCCWebApp-FULL-EVERYTHING-...` was never seen as cited.
* A zip whose name carried no `HHMM` (`-20260902-full`, `-20260908-DEF412`) produced an empty
  timestamp, and the loop treated empty as *unreferenced* rather than *unknown*. **A fallback that
  defaults to delete is the wrong direction** - unknown must mean keep.

The remaining 14 deleted bundles were re-checked by full-name search and are genuinely uncited.

| Bundle | md5 captured before deletion |
|---|---|
| `GCCWebApp-FULL-EVERYTHING-20260817-2140` | `914c40355e28a95efd502f882fbb9203` |
| `GCCWebApp-deploy-20260902-full` | `de72254e3fd9c42f4f56c0d035314675` |
| `GCCWebApp-deploy-20260908-DEF412` | `6549923ab9c3daea2c24dbabcfbe4c89` |

**Recovery path.** `deployment-drops/` is inside the OneDrive-synced project folder. `rm` bypasses
the Windows Recycle Bin, but OneDrive still records the deletion, so the files should be in the
OneDrive **online** recycle bin (onedrive.com -> Recycle bin; business tenants retain 93 days,
i.e. until roughly 2026-12-18). Restore by name, then md5-verify against the table above before
treating any restored file as the original.

For `GCCWebApp-deploy-20260902-full`, the md5 captured at deletion matches the md5 recorded
independently in `shipped-2026-09.md`, so that hash is corroborated by two sources.

## Why most of these were already gone

44 of the 47 absent bundles were missing before the 2026-09-16 cleanup and were not deleted by it.
They date from 2026-05-04 to 2026-09-11 and were presumably cleared in earlier housekeeping. The
practical consequence: **for defects closed between May and August 2026, the bundle-level
verification path no longer exists.** That is not a reason to re-open them - it is a reason not to
promise that check is available when someone asks.

## Keeping this true

Re-run the inventory whenever bundles are deleted:

1. Collect every `GCCWebApp-*<8-digit-date>*.zip` name cited anywhere under `docs/` - match the
   whole filename, not a timestamp substring, because bundle names carry suffixes (`-DEF412`,
   `-full`, `-hardened`) and the naming prefix is not always `GCCWebApp-deploy-`.
2. Diff that set against `deployment-drops/`.
3. Delete only names that appear in neither the citation set nor the current
   `deploy-baseline.json`, and record an md5 for each before removing it.

