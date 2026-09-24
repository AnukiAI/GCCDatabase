# Pre-deploy record — `GCCWebApp-deploy-20260922-1706.zip`

**Status: DEPLOYED to gccwebapp.com 2026-09-22 (by Shawn) — post-deploy verified; 12 register items closed out. Gate OFF on gccwebapp.com is DELIBERATE (Shawn, 2026-09-22).**
Target: TEST environment (gccwebapp.com). Built from branch `ci-regression`, uncommitted working tree.
**Supersedes** `20260921-0805` (the go-live plan's A1) and `20260921-2047`.

| | |
|---|---|
| **Artifact** | `deployment-drops/GCCWebApp-deploy-20260922-1706.zip` |
| **Size / entries** | 61.2 MB / **341** |
| **In-zip `GCCWebApp.dll`** | **md5 `ae50f49df5e7e622`**, 6,717,952 bytes |
| **Built with** | `Scripts/deploy.ps1 -SkipUploadsBackupPrompt`. The first attempt (`-1700`) aborted on a file lock while appending the post-deploy SQL, most likely OneDrive syncing the file. No zip was written, and the retry was clean. |
| **Schema** | **No new migration.** High-water `20260908175939_CR045_CertificationResultsVerbatimSort` |
| **Bundle verifier** | `Scripts/deploy/verify-bundle.py` → **failures: 0** (1 WARN: full-history script, as on every prior bundle) |
| **Redirect map** | `Scripts/cutover/verify-redirect-map.py --bundle` → **ALL CHECKS PASSED** (5,999 entries, `rewritemaps.config` present) |

> Identify this bundle by the DLL md5, not by size. `20260921-2047` is the same size.

## What it carries — Gate 0B: 12 RESOLVED-AND-WAITING, 0 UNCLASSIFIED, 0 NO-STATUS

CR-051, CR-052, CR-053, **CR-054**, DEF-430, DEF-431, DEF-432, DEF-434, DEF-436, DEF-437, DEF-438, DEF-439.
This is the 21 Sep set of 11 plus CR-054.

## CR-054 markers — checked against the in-zip bytes, with a control

| Check | This bundle | `20260921-2047` (control) |
|---|---|---|
| `wwwroot/js/certificate-create.js` contains `</i> View PDF` | yes | — |
| `…` contains `</i> All pages` | **no** | — |
| DLL contains `View PDF` (compiled Razor views) | **1** | 0 |
| DLL contains `View all pages` | **0** | 1 |
| DLL contains `Certificate Preview` (positive control) | 10 | 10 |

A first DLL search reported 0 for both the old and the new label. It had decoded the file at the
wrong byte offset. The control literal is what exposed that; the table above uses raw byte search.

## Riding along — in the working tree, not on the Gate 0B list

- **DEF-433 code fixes** (`Program.cs`, `CertificateController.cs`, `Services/FileUploadService.cs`):
  bounded `WaitAsync`, rate limiting, and ETag matched before rendering. The register says: *code
  fixed, NOT verified under load, parked 2026-09-22*, and *"a reasonable basis for shipping them in
  the bundle"*. Shipping them does **not** clear DEF-433, and the test gate must stay on.
- **Go-live A6/A7 rewrite rules** (`web.config`, `rewritemaps.config`, `GCCWebApp.csproj`):
  - The legacy deep-link rules and the `database.` canonical rule ship **enabled**.
  - **`Canonical host - gccwebapp retirement` ships `enabled="false"`.** That was checked because,
    if it were on, the test site would 301 to `db.` before DNS moves.
  - Side effect on test: `/certificate` (lowercase, exact) would 301 to the **legacy** `db.` site.
    That path 404s on the app today, so no working link is lost.

## What this bundle would UNDO

Entry-by-entry diff against `20260921-2047`: **0 entries only in the old one, 0 only in the new one.**
Six entries changed:
- CR-054's files: `GCCWebApp.dll`, `wwwroot/js/certificate-create.js`.
- Build output: `.pdb`, `staticwebassets.endpoints.json`, `migrations/README.md`.
- `apply-migrations.sql`: 6 lines differ, all generated `CreatedAt` timestamps inside an old,
  already-applied seed migration. No schema difference.

## Not checked

- **What is live on gccwebapp.com was not re-probed this session.** The 2026-09-21 record found the
  server split (wwwroot current, DLL older than DEF-439). That was not reconfirmed today.
- **`20260921-2047` has no pre-deploy record anywhere in `docs/`.** Whether it was ever uploaded is
  unknown. It was used here only as the diff baseline.
- **No smoke suite was run against this build.** CR-054 was verified on localhost (6/6). The other
  items rely on their own sessions' verification.
- **Nothing was uploaded.**

## Still owed at deploy time

1. Back up the server's `wwwroot/uploads/` before extracting.
2. Run `migrations\apply-migrations.sql` (`sqlcmd … -I -b`) **before** recycling. It is a no-op for
   schema and re-runs the two idempotent backfills.
3. Recycle the app pool.
4. Verify through the client's click path. Check four things:
   - The preview popup shows **View PDF**.
   - The emitted stylesheet links carry `?v=`, which proves the DLL moved (DEF-439).
   - `/appsettings.Production.json` does **not** return 200.
   - The site still lands on `/__gate`.
5. Close the loop: mark the 12 register statuses as deployed.

## Post-deploy verification — 2026-09-22 (Playwright + curl, GETs only; Create file-pick NOT submitted)

**gccwebapp.com is running this build.**
- **Static files:** `certificate-create.js`, `main.css` and `certificate-preview-pager.js` are
  md5-identical to the zip.
- **DLL:** stylesheets now carry `?v=` (DEF-439), and the compiled views render **View PDF**.

| Check (gccwebapp.com) | Result |
|---|---|
| Edit `26-0621`, popup | **View PDF**, aria "View certificate 26-0621 as a PDF", pager **Page 1 of 2** |
| InternalView `26-0621`, popup | same |
| Create, PDF picked (unsaved) | blob link **View PDF** |
| Public SingleView `26-0481` (anonymous) | aria "View certificate 26-0481 as a PDF"; its href returns `%PDF-` |
| Public SingleView `26-0621` | 404. It returns 404 on test.gccwebapp.com (old build) too, which fits the public-visibility rule rather than a deploy fault. **Not investigated further.** |
| `/appsettings.Production.json` | 404 |
| `/api/v1/certificates` with no key | 401 (the API is up and still demands a key) |

**The client's own example, 26-0621, now pages in the popup (Page 1 of 2).** That was the
check CR-052/053 had deferred to the test environment.

### ⚠️ FINDING — the gate on gccwebapp.com is OFF

- `/` returns **200** to an anonymous visitor.
- `/__gate` returns **404**.
- There is no TEST banner.
- `robots.txt` serves the public-directory version (`Allow: /`).

**This bundle did not cause it:** its `appsettings.Production.json` has `Enabled: true`, and its
`robots.txt` is `Disallow: /`. The live `robots.txt` differs from the zip, so both settings were
changed on the server.

`db.greencirclecertified.com` still resolves to **66.33.60.34**, the legacy host, so cutover has
**not** happened. The go-live plan keeps the gate on until then:
- **DEF-433** is unverified under load. The gate was its only mitigation.
- **CR-050** gives `test.gccwebapp.com` a gate of its own. That site is still gated (302 →
  `/__gate`). It is running the **older** build: "View all pages", and `certificate-create.js`
  still says "All pages".

**Left as found.** Whether the gate should be off is Shawn's decision. Nothing was changed on the
server.

### Not done
- The 12 register statuses have not been closed out as deployed yet.
- ExternalView was not exercised on either host.

### Close-out — 2026-09-22
- **Gate OFF: deliberate.** Shawn: *"turning the gate off was deliberate."* This is recorded in the
  go-live plan (A5) and in DEF-433.
- **8 DEFs closed:** `Scripts/deploy/close-out.js --apply`. The DEF-437 line ("deployed / not in any
  bundle") was corrected by hand.
- **4 CRs closed** (CR-051…054): heading and Status updated by hand, because close-out.js reads only
  the defect register.
- **Gate 0B afterwards:** PENDING 0, UNCLASSIFIED 0, DEPLOYED 88 → **100**.
- **Evidence behind "deployed":** a build-identity check on the live site (md5, `?v=`, compiled
  label). Only CR-053, CR-054 and DEF-439 were exercised individually on the live site. The other
  nine rest on being in the verified build.
