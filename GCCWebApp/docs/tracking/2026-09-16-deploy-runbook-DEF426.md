# Deploy runbook — 2026-09-16 → gccwebapp.com (test) — DEF-426

**Bundle:** `deployment-drops/GCCWebApp-deploy-20260916-1023.zip` — **342 entries, 61.17 MB**
**In-zip `GCCWebApp.dll` md5:** `58569001f47314c2`
**Verifier:** `verify-bundle.py … --no-migrations=20260908175939_CR045_…` → **0 failures**

> ## STATUS: BUILT AND VERIFIED — **NOT UPLOADED**
> Upload is a human step through Plesk. Gate 0 (back up server `wwwroot/uploads/`) is owed first.

> ⚠️ **Same size as the previous bundle — check the hash, not the size.** Both this and
> `20260915-1641` are **342 entries / 61.17 MB**. The DLL md5 differs (`58569001f47314c2` vs
> `42f7efa637fd6591`), which is the only reliable way to tell them apart. Confirm the md5 after upload.

---

## What this bundle carries — one fix, one file

| Ref | File | Change |
|---|---|---|
| **DEF-426** | `Controllers/Api/v1/CertificatesController.cs` | Partner API now honours `pid.IsActive` at all four product-ID projection sites (standard list, search-by-product-id, GS1 builder, single-cert) |

`git diff --stat HEAD` against the live build shows **exactly one changed file, +30/−8**. Nothing else
is riding along.

**Gate 0B (`resolved-not-shipped.py`) reports 1 PENDING — DEF-426 — and it is in this build.** That is
the gate working, not a blocker. It clears once the entry is marked deployed.

---

## Do you need the database step? **No.**

No pending migrations; test is at the CR-045 high-water. `deploy.ps1` generates a full-history
`apply-migrations.sql` regardless — **do not run it** (DEF-386: SQL Server compiles every guard block
before `IF NOT EXISTS` can skip it). Auto-migrate is OFF on test.

---

## ⚠️ This changes Amazon's numbers WHILE THEY ARE TESTING — read before uploading

Amazon is testing this week and has been told the full feed is **20,380 associations / 16,078 distinct
ASINs**. After this deploys:

| Measure | Before | After | Change |
|---|---|---|---|
| ASIN associations (rows) | 20,380 | **20,312** | **−68** |
| **Distinct ASINs** | 16,078 | **16,078** | **none** |
| Certificates with ≥1 ASIN | 173 | 173 | none |
| Certificates delivered | 212 | 212 | none |

**No product disappears from Amazon's view.** All 68 deactivated ASINs on `26-0439` also appear on
other certificates — verified: **0 of the 68 exist nowhere else.** The fix removes 68 duplicate
*associations*, not 68 products. The headline figure in the client letter (16,078, including its
subject line) stays correct.

### Sequencing — this is the decision that matters

The queued Amazon letter quotes **20,380**. Whichever way round you go, the two must agree:

- **Deploy first, then send** *(recommended)* — update the letter's `20,380` → `20,312` and send after
  the post-deploy check passes. One number, correct, no follow-up needed.
- **Send first, then deploy** — the letter is correct when sent, but Amazon re-pulls into 20,312 days
  after being told 20,380, having just been told to trust our count. Needs a second note.

**Do not deploy silently mid-test.** They are actively building against this feed this week.

---

## Pre-upload gate status

| Gate | Fires? | Result |
|---|---|---|
| 0 uploads backup | **YES** | **OUTSTANDING — human, before extract** |
| 0B resolved-but-unshipped | **YES** | 1 PENDING = DEF-426, **in this build** ✔ |
| 1 UI / design guide | no | no view or CSS change |
| 2 cross-role + responsive | no | no UI surface changed |
| 3 data scoping / RBAC | **YES** | Change is *more* restrictive (adds a filter); partner/company scoping untouched |
| 4 Partner API contract | **YES** | **Response shape unchanged** — same fields, fewer rows in `productIds[]` |
| **4B cross-surface product IDs** | **YES** | **FAILS today — correct.** Must **PASS** after deploy |
| 5 traceability | **YES** | DEF-426 in register with decision, fix and root cause |
| 6 performance / EF | **YES** | One extra boolean predicate on an already-filtered collection |
| 7 pending migrations | no | none |
| 8 CRUD write paths | no | read-only endpoints |
| 9 static integrity | **YES** | **0 failures**, 1 expected WARN (full-history script) |
| 10 config points at data | no | no config change |
| 11 close-out | after upload | `node Scripts/deploy/close-out.js` |
| 12 outbound email | no | no mail path touched |

**No local functional verification was performed, and none is claimed.** `GCCDatabase_Dev` holds
**0** rows with `IsActive = 0`, so a local run passes whether or not the fix works — vacuous, the same
trap as the Novi isolation pair. The meaningful checks are all post-deploy.

---

## Upload — **HUMAN**, via Plesk

1. **Gate 0 first** — zip the server's `wwwroot/uploads/` and pull it down.
2. Upload `GCCWebApp-deploy-20260916-1023.zip`, extract over the site root.
3. **Do not** run `migrations/apply-migrations.sql`.
4. Confirm `appsettings.Secrets.json` is still present; the startup log now prints the DB it bound to.
5. Recycle the app pool.

## Post-upload — the checks that actually prove this worked

```
python Scripts/smoke-prod/cross-surface-productids.py test     # must go FAIL -> PASS
python Scripts/data-ops/2026-09-09-amazon-payload-export.py test
node Scripts/deploy/close-out.js
```

| Check | Expect |
|---|---|
| Cross-surface (Gate 4B) | **PASS** — it named `26-0439` and 68 values before the fix |
| ASIN associations | **20,312** |
| Distinct ASINs | **16,078** (unchanged) |
| Certificates delivered | **212 rows / 212 distinct** |
| Membership findings | **0** |

Then: mark DEF-426 deployed (so Gate 0B clears), extend the deploy baseline, append to
`shipped-2026-09.md`, and settle the letter sequencing above.
