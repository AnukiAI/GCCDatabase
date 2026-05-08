# Role Management Audit — 2026-05-08

**Auditor:** Security Specialist (read-only investigation)
**Scope:** Role definitions, authorization enforcement, role-based filtering, DB consistency
**Stack:** ASP.NET Core 8.0 MVC + EF Core 9.0 + SQL Server (`GCCDatabase_Dev` on `ANUKI01`)
**Method:** Static code review + read-only DB queries. **No code modified, no DB writes.**

---

## Executive Summary

- **Critical (4):** Two MVC controllers entirely missing authorization (`HomeController`, `TestController` — `TestController` exposes `IAdminService` data). Three `[AllowAnonymous]` certificate-file endpoints (`Download`, `ViewCertificateFile`, `DownloadByNumber`) serve PDFs of any cert by ID/number — including Draft, Hold, Expired, internal-only certs — with no status filter. Two API endpoints (`GetCertificate`, `GetCertificateByNumber` in `CertificatesApiController`) accept `userRole` from the **query string** as the authorization input, allowing any caller to set `?userRole=Admin`.
- **High (5):** ETLController gates on `Roles="Admin,SuperAdmin"` but the `ApiKey` scheme never injects role claims and "SuperAdmin" doesn't exist anywhere — net effect is `[Authorize(Roles=)]` enforcement is broken for API keys and the literal "SuperAdmin" is dead. `DetermineUserRole` in `AccountController` assigns Admin/Analyst/External purely by **email-domain string match** (legacy code path). Stray "Internal" role string in `CertificateController.GetUserRole()` and `MockDataGenerator`. Dev DB has a non-system role named **"test"** with `system.certtypes`/`system.fields`/`system.settings` permissions.
- **Medium (4):** `Partner` role currently has zero assignments **but is offered in the User-Management dropdown unless it's filtered (verify)**, and the seed-data note about IsActive=false for Partner is contradicted by DB (`IsActive=1`). `Partner` role description says "API-key requests get this role" — but the role on a real human user with role=Partner would also pass `[Authorize(Policy="ExternalAccess")]` and `userRole=="Partner"` checks; this is intentional per DEF-112 but easy to misuse. `data.audit` (View Audit Logs) permission still seeded but no audit-logs page exists. Inconsistent role-source pattern: some controllers use `User.HasClaim("Role", …)`, others `User.IsInRole(…)`, others `Session.GetString("UserRole")`. `[Authorize(AuthenticationSchemes="Bearer")]` JWT path on `/api/v1/Auth` lives alongside cookie/ApiKey but no controller actually relies on JWT — confirm dead config.
- **Low (5):** Documentation drift (CLAUDE.md says roles are Admin/Analyst/External/Partner but DB has Public + test too); inconsistent claim names (`"Role"` vs `ClaimTypes.Role`); session-stored role used for non-trivial decisions in `Dashboard`, `Export`, and `Document` controllers despite policies existing; hardcoded role strings throughout (no `Roles.Admin` constants); `_Layout.cshtml` reads role from session only, not claims.

**Top urgent fix:** Add `[Authorize]` (or appropriate role gate) and a `PublicStatuses` check to the three `[AllowAnonymous]` certificate-file download/view endpoints in `Controllers\CertificateController.cs` (lines 1506, 1573, 1655). Today, anyone can iterate certificate IDs/numbers and download Draft, Hold, Suspended, Revoked, or Expired PDFs without authentication.

**Total findings: 18 (4 Critical, 5 High, 4 Medium, 5 Low)**

---

## Role Inventory

### Defined in code (`Data\SeedData.cs` lines 90-102)

| Role | Defined In | Active Users (Dev DB) | Permissions Granted | Notes |
|---|---|---|---|---|
| Admin | SeedData.cs:92 | 3 | All 22 permissions | `IsSystem=1`, `IsActive=1` |
| Analyst | SeedData.cs:93 | 133 | certificate.view/create/edit, data.export/import/reports, products.view (7) | `IsSystem=1` |
| Partner | SeedData.cs:99 | **0** | certificate.view, data.export/reports, products.view (4) | `IsSystem=1`, `IsActive=1` (note: SeedData comment claims `IsActive=false`, DB row says `1`) |
| External | SeedData.cs:100 | 142 | certificate.view, data.export/reports, products.view (4) | `IsSystem=1` |
| Public | SeedData.cs:101 | 1 | certificate.view (1) | `IsSystem=1` |
| **test** | DB only — NOT in code | 1 (`test@test.com`) | system.certtypes, system.fields, system.settings (3 system permissions!) | `IsSystem=0`, `IsActive=1`. **Orphan in code.** |

### Defined-but-stale role names referenced in code (no row in DB)

| Role Name | Where | Status |
|---|---|---|
| `Internal` | `CertificateController.cs:1387` — `if (User.HasClaim("Role", "Internal")) return "Internal";` | Dead branch — no Internal role exists, no user has this claim. Bug in `GetUserRole()`. |
| `Internal` | `Scripts\MockDataGenerator.cs:593` | Dead test-data generator — generates 20% of mock users as "Internal". Will create role-claim collisions if ever run against current schema. |
| `SuperAdmin` | `Controllers\Api\ETLController.cs:11` — `[Authorize(Roles = "Admin,SuperAdmin")]` | Dead clause. No SuperAdmin role exists. Admin-only effectively. |

---

## Authorization Enforcement Map

### MVC Controllers (cookie-authenticated)

| Controller | Class-Level Auth | Action(s) Without Auth | Notes |
|---|---|---|---|
| `HomeController` | **none** | `Index`, `Privacy`, `Error` | All implicitly anonymous (Index has `[AllowAnonymous]`; Privacy and Error inherit nothing — global `AuthorizeFilter` covers them, but they probably should be `[AllowAnonymous]` for clarity). Privacy hits `View()` — likely safe. |
| `TestController` | **none** | `ProductTypes`, `GetProductTypesTest` | **Critical.** Exposes `IAdminService.GetProductTypesAsync()` to any authenticated user. Should not exist in production. |
| `AccountController` | mixed `[Authorize]`+`[AllowAnonymous]` per action | many `[AllowAnonymous]` actions for login/2FA flows | OK by design. |
| `AdminController` | `[Authorize(Policy = "AdminOnly")]` | none | Good. |
| `CertificateController` | none at class level | `CertificateTileView`, `CertificateSingleView`, `Verify`, `Download`, `ViewCertificateFile`, `DownloadByNumber`, `ExportCertificates` are `[AllowAnonymous]`; rest gated per-action by policy | **Critical.** `Download`, `ViewCertificateFile`, `DownloadByNumber` lack status filtering — see Findings F-C-002 / F-C-003. |
| `CertificateImportController` | `[Authorize(Policy = "AdminOnly")]` | none | Good. |
| `CompanyController` | `[Authorize(Policy = "AnalystOrAdmin")]` | none | Good. |
| `DashboardController` | `[Authorize(Policy = "AnalystOrAdmin")]` | none, but inline `Session.GetString("UserRole")` checks duplicate the policy | OK; redundant but defense-in-depth. |
| `DocumentController` | `[Authorize]` (any auth) | per-action policies on Upload/Delete | Good. |
| `ExportController` | per-action policies (`AnalystOrAdmin`, `AdminOnly`) | none | Good. |
| `ProductController` | `[Authorize(Policy = "ExternalAccess")]` | per-action policies for save/edit/delete | Good. |

### API Controllers

| Controller | Auth | Notes |
|---|---|---|
| `Api\v1\CertificatesController` | `[Authorize(AuthenticationSchemes = "ApiKey")]` | Partner API. Filters by partner via `User.FindFirst("partner_name")`. OK. |
| `Api\v1\BulkCertificatesController` | `[Authorize(Policy = "AnalystOrAdmin")]` | Comment says ApiKey was changed to cookie auth. OK. |
| `Api\v1\AuthController` | `[AllowAnonymous]` (class) + `[Authorize(AuthenticationSchemes = "Bearer")]` per-action | JWT login flow. OK. |
| `Api\v1\MetricsController` | `[Authorize(Policy = "AdminOnly")]` | Good. |
| `Api\v1\ApiKeysController` | `[Authorize(Policy = "AdminOnly")]` | Good. |
| `Api\v1\ProductsController` | `[Authorize(Policy = "AnalystOrAdmin")]` | Per controller comment, partners NOT allowed here. Good. |
| `Api\PartnerApiController` | `[Authorize(Policy = "AdminOnly")]` | Good. |
| `Api\ETLController` | `[Authorize(Roles = "Admin,SuperAdmin")]` | **High.** Mixes role-list with no policy — uses `ClaimTypes.Role`. Login claim in `AccountController.SignInUser()` does add `ClaimTypes.Role`, so this works for cookies — but ApiKey scheme handler does NOT add role claims, so API-key-authenticated callers (if ever attempted) would silently fail authorization. SuperAdmin literal is dead. |
| `Api\CertificatesApiController` | none at class level | per-action `[Authorize(Roles = "Admin")]`, `[Authorize(Roles = "Admin,Analyst")]`, `[Authorize(Policy = "AnalystOrAdmin")]`. **GET endpoints (`GetCertificates`, `GetCertificate`, `GetCertificateByNumber`, `DownloadCertificateFile`) are unauthenticated** but route through global `AuthorizeFilter` from `Program.cs:430-433`. Two of these accept `[FromQuery] string userRole = "Public"` and pass it to the service — see F-C-004. |
| `Api\CertificateTilesApiController` | none at class level + 5 `[AllowAnonymous]` actions | Public certificate tile view. OK by design. |
| `Api\CertificateTileExternalApiController` | `[Authorize(Policy = "ExternalAccess")]` | Good. |
| `Api\CertificateTileInternalApiController` | `[Authorize(Policy = "AnalystOrAdmin")]` | Good. |
| `Api\ProductIdApiController` | `[Authorize(Policy = "AnalystOrAdmin")]` | Good. |

### Inline role checks (hidden from `[Authorize]` static analysis)

| File:Line | Pattern | Comment |
|---|---|---|
| `Controllers\CertificateController.cs:1361-1391` | `User.HasClaim("Role", "Admin"\|"Analyst"\|"External"\|"Internal")` | Includes dead "Internal" branch. |
| `Controllers\CertificateController.cs:2090,2094` | `User.IsInRole("Admin")`, `User.IsInRole("External")` | Mixes `IsInRole` (reads `ClaimTypes.Role`) with HasClaim("Role") elsewhere. Both are populated by login (AccountController.cs:1133+1136), so this works, but it's a footgun if claim shapes ever drift. |
| `Controllers\CertificateController.cs:1838-1839` | `string.Equals(userRole, "Admin", OrdinalIgnoreCase)` | Reads via `GetUserRole()` helper. OK. |
| `Controllers\Api\CertificateTileExternalApiController.cs:98,112,129,305,317,346-371,429,441,635,640,646` | `userRole == "Partner"`, `userRole == "External"`, etc. | Heavy use throughout. |
| `Controllers\Api\CertificatesApiController.cs:118,153` | `[FromQuery] string userRole = "Public"` | **Critical — see F-C-004.** |
| `Controllers\ProductController.cs:72,86,116-125,184,198,300` | `userRole == "Partner"`, `userRole == "External"` | Filtering source-of-truth. OK as long as `GetCurrentUserRole()` is trustworthy (it reads `User.FindFirst("Role")`). |
| `Controllers\DashboardController.cs:54,120,281,328` | `userRole == "Admin"`/`"Analyst"` from session | Redundant with `[Authorize(Policy = "AnalystOrAdmin")]`. Defense-in-depth. |
| `Controllers\ExportController.cs:55,118,202` | `userRole == "Admin" \|\| userRole == "Analyst"` | Redundant with policy. |
| `Controllers\DocumentController.cs:268` | `userRole == "Admin" \|\| userRole == "Analyst"` | Redundant with policy. |
| `Middleware\ApiRateLimitingMiddleware.cs:140` | `context.User.IsInRole("Admin")` | Used to choose rate-limit tier. OK. |
| `Extensions\SwaggerExtensions.cs:430-432` | `user?.HasClaim("Role", "Admin"/"Analyst"/"External")` | Swagger gating. OK. |

---

## Findings

### Critical

#### F-C-001 — `TestController` has zero authorization, exposes admin-service data
**File:** `Controllers\TestController.cs:1-34`
**What:** The class has no `[Authorize]` attribute and no `[AllowAnonymous]` either. Global `AuthorizeFilter` in `Program.cs:430-433` requires authentication, which makes it reachable by any logged-in user (including External / Public-equivalent). It calls `_adminService.GetProductTypesAsync()` and returns the result.
**Why it matters:** External, Analyst, and any future Partner human user can hit `/Test/GetProductTypesTest` and pull the full product-type hierarchy that the admin endpoint serves. The `[Authorize(Policy="AdminOnly")]` gate on `AdminController` is bypassed by this parallel route.
**Recommendation:** Either delete the file (preferred — looks like leftover dev scaffolding) or add `[Authorize(Policy = "AdminOnly")]` at the class level and add `[Route("/admin/test")]` to make the URL match the access level.

---

#### F-C-002 — `Certificate/Download/{id}` and `Certificate/DownloadByNumber/{number}` are anonymous and don't filter by status
**File:** `Controllers\CertificateController.cs:1506-1567` (`Download`), `1655-1673` (`DownloadByNumber`)
**Code:**
```csharp
[AllowAnonymous]
public async Task<IActionResult> Download(string id)
{
    ...
    var certificate = await _certificateRepository.GetCertificateByNumberAsync(id);
    if (certificate == null) return NotFound(...);
    if (!string.IsNullOrEmpty(certificate.CertificateFile))
    {
        ...
        return File(fileBytes, contentType, fileName);
    }
}
```
**What:** Both endpoints serve any certificate's PDF by number/ID — no status check, no `PublicStatuses` filter, no auth check.
**Why it matters:** Certificate numbers are sequential (`YY-NNNN`). An anonymous attacker can iterate `/Certificate/Download/26-0001` … `/Certificate/Download/26-9999` and harvest every Draft, Hold, Suspended, Revoked, or Expired certificate PDF. Compare to `CertificateSingleView` (lines 222-238) which DOES enforce `CertificateStatusHelper.IsPubliclyVisible(...) && !isExpired` for anonymous access — that protection is absent on the download endpoints.
**Recommendation:** Add to both `Download` and `DownloadByNumber` (and `ViewCertificateFile`):
```csharp
if (User.Identity?.IsAuthenticated != true)
{
    var isPublic = CertificateStatusHelper.IsPubliclyVisible(certificate.Status);
    var isExpired = certificate.ExpirationDate < CertificateStatusHelper.BusinessToday;
    if (!isPublic || isExpired)
        return NotFound($"Certificate {id} is not currently available for public download.");
}
```
Match the pattern already used in `CertificateSingleView` for consistency.

---

#### F-C-003 — `Certificate/ViewCertificateFile/{id}` is anonymous, takes integer ID, no status filter
**File:** `Controllers\CertificateController.cs:1573-1634`
**What:** Same vulnerability as F-C-002 but takes an integer ID, which makes enumeration even simpler.
**Why it matters:** Any sequential int ID exposes the underlying PDF inline. No business rule justifies this being anonymous for non-public certs.
**Recommendation:** Same as F-C-002 — add the public-status guard for unauthenticated callers.

---

#### F-C-004 — `Api\CertificatesApiController.GetCertificate` and `GetCertificateByNumber` accept role from query string
**File:** `Controllers\Api\CertificatesApiController.cs:114-180`
**Code:**
```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<CertificateDetailsViewModel>> GetCertificate(int id,
    [FromQuery] string userRole = "Public")
{
    var certificate = await _certificateService.GetCertificateDetailsAsync(id, userRole);
    ...
}
```
**What:** The user's role is read from the URL query string, then passed to `_certificateService.GetCertificateDetailsAsync(id, userRole)`. The service uses this role to decide what data (incl. internal tags, internal-visibility custom fields) to surface.
**Why it matters:** A caller can send `?userRole=Admin` and receive an internal-perspective response. Even if the global `AuthorizeFilter` requires authentication, an authenticated External user can self-elevate by appending a query param. Combined with the `BulkSearchCertificates` endpoint at line 594 which also reads `request.UserRole ?? "Public"`, this is a privilege-escalation pattern.
**Recommendation:** Remove `userRole` from the parameter list; derive it server-side:
```csharp
var userRole = User.FindFirst("Role")?.Value
            ?? User.FindFirst(ClaimTypes.Role)?.Value
            ?? "Public";
```
Apply the same fix to `GetCertificateByNumber` (line 153) and the `BulkSearchRequest.UserRole` property at line 594.

---

### High

#### F-H-001 — ETLController uses `Roles="Admin,SuperAdmin"` — mismatched scheme, dead role literal
**File:** `Controllers\Api\ETLController.cs:11`
**Code:** `[Authorize(Roles = "Admin,SuperAdmin")]`
**What:** Two issues:
1. **SuperAdmin** is not defined anywhere — not in `SeedData.cs`, not in any policy, not in the DB. The clause is dead — only `Admin` matches.
2. The `[Authorize(Roles=…)]` form requires `ClaimTypes.Role`. Cookie auth (`AccountController.SignInUser`) DOES add that claim (line 1133), but the `ApiKeyAuthenticationHandler` (lines 119-126) does NOT — so any future API-key client of ETL endpoints is silently locked out, regardless of what scopes the key has.
**Recommendation:** Replace with `[Authorize(Policy = "AdminOnly")]` (the policy uses the custom `"Role"` claim that the login flow also sets). Drop "SuperAdmin" — it's confusing.

---

#### F-H-002 — `DetermineUserRole(email)` assigns roles from email-domain match
**File:** `Controllers\AccountController.cs:981-1004`
**Code:**
```csharp
if (email.EndsWith("@greencirclecertified.com")) return "Admin";
else if (email.EndsWith("@contractor.com") || email.EndsWith("@analyst.com")) return "Analyst";
else if (email.Contains("partner") || email.EndsWith("@amazon.com")) return "External";
else return "Public";
```
**What:** A method that infers role from email patterns. It's still in the codebase, even though `ValidateUserDetailed` (line 1017+) properly looks up roles via the DB.
**Why it matters:** If any code path ever calls `DetermineUserRole` directly (search shows it's not currently called — but the method is still callable / could be reintroduced via merge/refactor), an attacker who can register `attacker+test@greencirclecertified.com` (if registration is open or self-service) gets Admin. Even if not currently called, leaving this code around is a regression-magnet.
**Recommendation:** **Delete `DetermineUserRole`** entirely. Rely solely on `ValidateUserDetailed` → `user.Roles.FirstOrDefault()?.Name ?? "Public"`. If you keep it for any reason, gate it behind a #if DEBUG flag and add a `[Obsolete]` warning.

---

#### F-H-003 — Dev DB has unauthorized "test" role with system-level permissions
**Source:** Dev DB (`Roles` table, Id=5) and `PermissionRole`
**What:** A non-system role named `"test"` exists with these permissions: `system.certtypes`, `system.fields`, `system.settings`. One user assigned: `test@test.com`.
**Why it matters:**
- The role isn't seeded by `SeedData.cs` — someone created it manually, possibly during a debug session, and never cleaned up.
- It grants three powerful system-level permissions that aren't even granted to Analyst.
- Production DB might have the same row (need to verify).
- **Permissions are seeded but currently NOT enforced by any controller** (no codepath checks `PermissionRole` in non-Admin paths) — so today this role acts the same as `Public` because it has no role-based policy match. **However**, if/when CR-008..CR-014 introduce permission-based gates, this role becomes a backdoor.
**Recommendation:**
- Verify whether the same `"test"` role exists in production. If yes, raise a CR for cleanup.
- Add a startup-time validator: at application launch, enumerate `Roles.Where(r => !r.IsSystem)`; if any exist with system-category permissions, log a CRITICAL warning.
- Long-term: add a role-management UI policy that prevents creating non-system roles with `system.*` permissions.

---

#### F-H-004 — `GetUserRole()` in CertificateController checks for non-existent "Internal" role
**File:** `Controllers\CertificateController.cs:1387-1388`
**Code:**
```csharp
if (User.HasClaim("Role", "Internal"))
    return "Internal";
```
**What:** No "Internal" role exists in DB or seed data. The `MockDataGenerator.cs:593` references it as a 20% bucket but is unused. The login flow (`AccountController.SignInUser`) only ever stamps roles from `user.Roles.FirstOrDefault()?.Name`, which can only be Admin/Analyst/Partner/External/Public.
**Why it matters:** Dead code. Worse, the downstream consumers of `GetUserRole()` — including `Statuses` (line 666, 760) — call `CertificateStatusHelper.GetStatusesForRole(userRole)`. If `GetStatusesForRole("Internal")` exists (verify) and returns a different set than `Admin`/`Analyst`, this path is silently dead. If it returns the default, the inconsistency is harmless but still confusing.
**Recommendation:** Remove lines 1387-1388. Update `MockDataGenerator.cs:593` to remove the "Internal" bucket. Audit `CertificateStatusHelper.GetStatusesForRole` for any `case "Internal"` branch.

---

#### F-H-005 — `[Authorize(Roles = "Admin")]` and `[Authorize(Roles = "Admin,Analyst")]` mixed with `[Authorize(Policy = "AnalystOrAdmin")]` in same controller
**File:** `Controllers\Api\CertificatesApiController.cs:186, 240, 314, 353, 416, 578, 703`
**What:** Some actions use `[Authorize(Roles = "Admin")]` (relies on `ClaimTypes.Role`); others use `[Authorize(Policy = "AnalystOrAdmin")]` (relies on the custom `"Role"` claim). Login adds both, so it works today, but if someone refactors the login flow to drop the duplicate claim, half the controller will silently fail open or closed.
**Why it matters:** Inconsistency = future bug. Pick one and use it everywhere.
**Recommendation:** Standardize on policy-based authorization (`AdminOnly`, `AnalystOrAdmin`). Replace `[Authorize(Roles = "Admin")]` with `[Authorize(Policy = "AdminOnly")]` and `[Authorize(Roles = "Admin,Analyst")]` with `[Authorize(Policy = "AnalystOrAdmin")]`. ETLController in F-H-001 is part of the same fix.

---

### Medium

#### F-M-001 — Partner role: `IsActive` mismatch between code comment and DB
**File:** `Data\SeedData.cs:99` vs DB row Roles.Id=7
**What:** Code comment at SeedData.cs:94-98 says "the Roles table row is IsActive=false (DEF-112 SQL script) so the User Management role dropdown does not offer it." DB shows `IsActive=1`. So either the DEF-112 SQL was never applied to dev, or the comment is wrong.
**Why it matters:** If the User Management role dropdown filters by `IsActive=true`, Partner is selectable today — meaning an Admin can assign a human user to the Partner role, which then collides with the API-key-injected Partner claim. The code in `ProductController` and `CertificateTileExternalApiController` does branch on `userRole == "Partner"` for both cases — but the cert-listing logic assumes Partner = API key.
**Recommendation:** Decide canonically:
- If Partner is API-key-only: run `UPDATE Roles SET IsActive=0 WHERE Name='Partner'` on dev + prod, and update the seed file's `IsActive = true` to `false`.
- If Partner is dual-purpose (current seed comment ALSO says this): document that decisively and verify both code paths.
This is the kind of ambiguity that bites during UAT.

---

#### F-M-002 — `data.audit` permission ("View Audit Logs") is seeded but no audit-log page exists
**File:** `Data\SeedData.cs:61`
**What:** Permission `data.audit` is seeded and granted to Admin. There is no controller action, no view, no menu item that consumes it.
**Why it matters:** When the client lists permissions in the role-management UI, they see a permission that does nothing. This is a documented observation in MEMORY.md (QC #105).
**Recommendation:** Either (a) implement an Audit Logs read-only page (CR), or (b) remove the permission from seed data and the corresponding row from the DB. Per memory log, this was already raised — confirm with the client.

---

#### F-M-003 — Inconsistent role-source pattern: Session vs. Claim vs. IsInRole
**Files:** Multiple — see Inline checks table above
**What:** Three different ways to read the user's role coexist in the codebase:
- `User.FindFirst("Role")?.Value` (custom claim) — used in `ProductController`, `CertificatesApiController` Bulk endpoint
- `User.HasClaim("Role", "Admin")` (custom claim) — used in `CertificateController` helpers, `SwaggerExtensions`
- `User.IsInRole("Admin")` (reads `ClaimTypes.Role`) — used in `Middleware\ApiRateLimitingMiddleware`, `CertificateController.cs:2090`
- `HttpContext.Session.GetString("UserRole")` — used in `Dashboard`, `Export`, `Document`, `Home` controllers
**Why it matters:** Login adds both `ClaimTypes.Role` and `"Role"` claims (AccountController.cs:1133+1136), and the role middleware in `Program.cs:670-702` mirrors them to session. So all four sources currently agree. But:
- Session is per-request and only set if the request crosses the role middleware (i.e. NOT for `/api/*`). Any code reading session inside an API path will get null, falling back to "Public".
- A future refactor that drops the duplicate claim or the session mirror will create a silent half-broken state.
**Recommendation:** Add a single helper extension method (e.g. `User.GetRole()` returning `string`) and migrate all call sites. Centralize the claim/session/IsInRole logic in one place.

---

#### F-M-004 — Global `AuthorizeFilter` in Program.cs covers many controllers but doesn't cover unauthenticated API endpoints
**File:** `Program.cs:430-433`
**Code:**
```csharp
var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
options.Filters.Add(new AuthorizeFilter(policy));
```
**What:** This adds a global filter requiring authentication for all controllers. This is the safety net. But:
- `[AllowAnonymous]` on individual actions overrides it (correct behavior, but easy to abuse).
- Some controllers (`CertificatesApiController`) have NO class-level auth, and per-action `[Authorize(Roles = "Admin")]` only on POST/PUT/DELETE — leaving GETs covered ONLY by the global filter (i.e., "any authenticated user can read").
**Why it matters:** This isn't a per-se bug, but it means the auth posture of GET endpoints is "any logged-in user". External users CAN call `Api\CertificatesApi\GetCertificates` and get the full unfiltered cert list — there's no role gate, only the auth gate. The internal-tile `CertificateTileInternalApiController` is properly gated; this older `CertificatesApiController` is not.
**Recommendation:** Audit all `Controllers\Api\CertificatesApiController` GET endpoints. Decide whether they should require Admin/Analyst (likely yes, since the "Tile" controllers are split by role). Either retire `CertificatesApiController` (it appears to be older / superseded by `Api\v1\CertificatesController`) or add `[Authorize(Policy = "AnalystOrAdmin")]` at the class level.

---

### Low

#### F-L-001 — CLAUDE.md role list is incomplete vs. seed data
**File:** `.claude\CLAUDE.md`
**What:** CLAUDE.md says "Admin, Analyst, External, Public roles" (line referencing). The actual seeded set is Admin, Analyst, Partner, External, Public. Partner is missing from the project doc.
**Recommendation:** Update CLAUDE.md to list all five seeded roles + a note that Partner is API-key-driven.

---

#### F-L-002 — No central role constants
**File:** Throughout codebase
**What:** Strings `"Admin"`, `"Analyst"`, `"External"`, `"Partner"`, `"Public"` appear ~80+ times across controllers, services, and views. No `Roles` static class.
**Why it matters:** Typos like `"Adminn"` or `"analyst"` (lowercase) silently fail-closed. There's at least one comparison using `OrdinalIgnoreCase` (`CertificateController.cs:1838`) and many using exact-case comparison — inconsistent.
**Recommendation:** Add a `Constants\Roles.cs` static class: `public const string Admin = "Admin";` etc. Replace all string literals incrementally. Low priority but a long-term hardening.

---

#### F-L-003 — `_Layout.cshtml` reads role from session only
**File:** `Views\Shared\_Layout.cshtml:8`
**Code:** `var userRole = Context.Session.GetString("UserRole") ?? "Public";`
**Why it matters:** Same as F-M-003 — if session middleware ever fails to populate (e.g., if cookie is replayed but session expires), the layout falls back to Public and may hide nav items the user is entitled to. Better to read from `User.FindFirst("Role")?.Value` directly in views.
**Recommendation:** Switch to claim-based read in `_Layout.cshtml`. Low impact.

---

#### F-L-004 — `Partner` role in seed is `IsActive = true` and assigned 0 users — confusing
**File:** `Data\SeedData.cs:99`, `PermissionRole` rows for Partner
**What:** Partner has 4 permissions seeded but no users. The User Management form may or may not show it (verify dropdown filter logic). The role exists "for the API-key middleware to inject the claim", but the API key middleware doesn't actually require the row to exist — it's a synthetic claim. So the row is essentially documentation.
**Recommendation:** Either remove the Partner row entirely (the API key middleware doesn't read it) OR set `IsActive=false` and document that "Partner" is reserved for API-key contexts. Pick one to reduce confusion.

---

#### F-L-005 — `[Authorize(AuthenticationSchemes = "Bearer")]` JWT path may be dead
**File:** `Controllers\Api\v1\AuthController.cs:163`
**What:** JWT issuance endpoint exists. Search shows only `AuthController` itself uses Bearer scheme; no other controller declares `[Authorize(AuthenticationSchemes = "Bearer")]` for normal data access.
**Why it matters:** Maintaining an unused JWT issuance endpoint is footprint without benefit. Either remove or document use case.
**Recommendation:** Confirm with client whether JWT is needed for any future integration. If not, remove `JwtBearer` from `Program.cs:293-319` and the controller.

---

## DB Cross-Reference

### Role table contents (`Roles`)

| Id | Name | IsSystem | IsActive | Active Users | Note |
|---|---|---|---|---|---|
| 1 | Admin | 1 | 1 | 3 | Canonical |
| 2 | Analyst | 1 | 1 | 133 | Canonical |
| 3 | External | 1 | 1 | 142 | Canonical |
| 4 | Public | 1 | 1 | 1 | Canonical |
| 5 | **test** | **0** | **1** | 1 | **Orphan — not in seed code** |
| 7 | Partner | 1 | 1 | **0** | Note: Id=6 is missing — possible deleted row |

### Users without any role (active)
**Result: 0** — no orphaned users.

### Users with multiple roles
**Result: 0** — single-role per user is consistently maintained.

### Inactive users
**Result: 0** — all 280 user-role assignments are to active users.

### Mismatches between seeded permission set and assigned permissions
- Admin gets all 22 permissions ✓
- Analyst gets 7 permissions matching the seed list ✓
- External gets 4 permissions matching the seed list ✓
- Partner gets 4 permissions matching the seed list ✓ (but 0 users)
- Public gets 1 permission ✓
- **test gets 3 system-category permissions — NO seed code authorizes this** ✗

### Roles in code but not DB
- `Internal` (CertificateController.cs:1387) — never existed
- `SuperAdmin` (ETLController.cs:11) — never existed

### Roles in DB but not code
- `test` (Id=5) — manually created, not in seed

---

## Recommendations (Prioritized)

1. **Fix F-C-002 / F-C-003** — Add status filtering to `Download`, `DownloadByNumber`, `ViewCertificateFile`. Mirror the pattern already used in `CertificateSingleView`. **30-minute fix, ships before UAT.**
2. **Fix F-C-004** — Remove `[FromQuery] string userRole` from API GETs in `Api\CertificatesApiController.GetCertificate` and `GetCertificateByNumber`; derive from claims. **15-minute fix.**
3. **Delete or gate `TestController`** (F-C-001). One-line fix or file deletion. **5 minutes.**
4. **Run `DELETE FROM PermissionRole WHERE RolesId = 5; DELETE FROM UserRoles WHERE RolesId = 5; DELETE FROM Roles WHERE Id = 5;`** on dev (and check + clean prod) (F-H-003). **Requires CR per Shawn's data-change rule.** Until then, log a CRITICAL warning at startup if non-system role with system permissions exists.
5. **Replace `[Authorize(Roles = …)]` with policy attributes** in ETLController and CertificatesApiController (F-H-001, F-H-005). Standardizes claim source. **30 minutes, low risk.**
6. **Delete `DetermineUserRole(email)`** (F-H-002) — unused, dangerous regression-magnet. **10 minutes.**
7. **Remove the dead `"Internal"` role checks** (F-H-004). **10 minutes.**
8. **Decide Partner role IsActive contract** (F-M-001) — confirm whether human users may be assigned Partner. Document and align DB. **30 minutes — design decision needed first.**
9. **Add a startup-time role-set validator** (defense-in-depth for F-H-003): on app start, enumerate `Roles`, log warning for any non-system role, log CRITICAL for any role with `system.*` permissions. **15 minutes.**
10. **Centralize role string in a `Constants\Roles.cs`** (F-L-002). **One PR, low risk.** Long-term hygiene.

### Confirmation against project history (per task prompt)

- **External users must NOT see internal tags** — *Server-side enforcement confirmed*. `CertificateTileExternalApiController.cs:200` filters tags by `t.TagType == "external"` regardless of query input. `CertificateService.cs:521` and `CertificateTileInternalApiController.cs:215` are the only paths that emit `InternalTags`, and both are gated behind `AnalystOrAdmin` policy. Export at `CertificateController.cs:1838-1839` correctly excludes internal tags for non-Admin/Analyst.
- **External users CANNOT view/edit internal tags or have Add/Edit cert buttons or Actions column** — *Server-side enforcement confirmed* on the data path. Edit/Create/Delete actions are all `[Authorize(Policy = "AnalystOrAdmin")]` or `"AdminOnly"`. Views must independently hide buttons (not in scope of this audit but confirmed in `_Layout.cshtml` reads role from session).
- **Partner role is API-key-driven, not interactive login** — *Mostly correct but contradictory*. The PartnerApiKeyAuthenticationMiddleware injects the claim correctly. However, the Roles table row is `IsActive=1` (NOT 0 as the SeedData comment claims) and 0 humans currently hold it, BUT the role IS theoretically assignable. See F-M-001.
- **"Manage Workflow" permission is a placeholder** — *Confirmed and already removed* from SeedData.cs:54-55 ("Manage Workflow" was removed 2026-05-01 per client request, comment intact). However the Permissions table in dev DB no longer shows `certificate.workflow`, so dev is in sync.
- **"View Audit Logs" permission is documented but not enforced** — *Confirmed*. Permission seeded (`data.audit`), granted to Admin, but no controller checks it. See F-M-002.

---
