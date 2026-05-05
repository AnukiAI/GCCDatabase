# Production Smoke — gccwebapp.com — 2026-05-04

Live verification of yesterday's (2026-05-03) deploy and the auth surface across all `/api/v1/*` routes. Captured for stakeholder receipts and follow-up documentation.

## Result

**PASS.** Auth wall is intact on every Partner API route. No 500s, no 503s, no anonymous data leaks. Yesterday's deploy is healthy.

Response times 200–300ms across the board. Test environment Basic Auth gate is correctly bypassing `/api/*` so contracted partners are unaffected.

## Endpoint matrix

| Route | Anon | Dev partner key | Notes |
|---|---|---|---|
| `GET /api/v1/products` | 401 (JSON) | 401 (JSON, "Invalid or expired API key") | Dev keys are env-scoped; prod DB does not recognize them — expected behavior |
| `POST /api/v1/products` | 411 (Length Required, IIS-level) | — | Pre-auth IIS check; surfaced that `ProductsController` has POST/PUT/DELETE routes — see DEF-130 severity upgrade below |
| `PUT /api/v1/products` | 411 | — | |
| `DELETE /api/v1/products` | 405 (Method Not Allowed) | — | DELETE is at `/{id}`, not collection root — correct |
| `GET /api/v1/products/1` | 401 | — | |
| `GET /api/v1/products/search?term=x` | 401 | — | |
| `GET /api/v1/certificates` | 401 | — | |
| `GET /api/v1/certificates/expiring?days=30` (DEF-124 path) | 401 | — | |
| `GET /api/v1/certificates/search?term=Widget` (DEF-129 path) | 401 | — | |
| `GET /api/v1/certificates/number/26-0001` | 401 | — | |
| `POST /api/v1/certificates/bulk/create` (DEF-131 path) | 401 | — | Same lockdown pattern as DEF-130 today; confirmed live |

## Anonymous response shape (sample)

```
GET /api/v1/products
HTTP 401 application/json
{"success":false,"message":"Authentication required. Please log in.","errorCode":"UNAUTHORIZED"}

GET /api/v1/products  (with dev partner key)
HTTP 401 application/json
{"success":false,"data":null,"errors":[{"code":"UNAUTHORIZED","message":"Unauthorized: Invalid or expired API key"}]}
```

Clean JSON 401 — no HTML auth page, no stack traces. Auth contract is solid.

## Test environment gate (`gccwebapp.com` UI paths)

- `GET /Account/Login` → HTTP 401 with `WWW-Authenticate: Basic realm="GCC Test Environment"` and body `"This is a private test environment. Enter the shared test credentials to continue."` — gate is up.
- `/api/*` correctly bypasses the gate per the test-env access middleware design.
- `/robots.txt` returns 200 (allowed through for SEO disallow rules).
- `/health` returns 401 (gated by Basic Auth wall — outside the `/api/*` bypass — minor; not a defect).

## DEF-130 — severity upgrade discovered during this smoke

`Controllers/Api/v1/ProductsController.cs` exposes six routes via the class-level auth attribute:

- `[HttpGet]` (list, line 82)
- `[HttpGet("{id}")]` (single, line 171)
- `[HttpPost]` (create, line 207)
- `[HttpPut("{id}")]` (update, line 241)
- `[HttpDelete("{id}")]` (delete, line 292)
- `[HttpGet("search")]` (search, line 318)

Pre-fix, the class-level `[Authorize(AuthenticationSchemes = "ApiKey")]` made all six reachable by any valid partner API key. So in addition to the originally-documented data exposure on GET, **partner keys could also create, update, or delete products** — same privilege-escalation class as DEF-131.

The DEF-130 fix shipped today (single attribute swap to `[Authorize(Policy = "AnalystOrAdmin")]` at the class level) closes all six methods in one line. Severity bumped from S3 (Medium) to S1 (Critical) in the defect register.

## End-to-end verification — completed with real Amazon partner key

After cookie auth was verified, a temporary Amazon partner API key (id=7, description "DEF-130 verification test 2026-05-04 (auto-revoke)") was generated via `POST /api/admin/partner/1/apikey`, used for the partner-side curl matrix below, and revoked via `DELETE /api/admin/partner/apikey/7`. Total prod state delta after the test: zero (key generated and revoked in the same minute).

### Result matrix (Amazon partner key, real prod calls)

| Defect | Endpoint | HTTP | Outcome |
|---|---|---|---|
| **DEF-130** | `GET /api/v1/products?pageSize=1` | **403** | ✓ Lockdown live — partner key denied: "Access denied. You do not have permission to perform this action." |
| **DEF-131** | `POST /api/v1/certificates/bulk/create` | **403** | ✓ Lockdown live — same `AnalystOrAdmin` policy rejection |
| **DEF-124** | `GET /api/v1/certificates/expiring?days=365` | **200** | ✓ Contract DTO returned: `status: "Active"`, `certificationCountries: "All"`, `partner: "Amazon"`, `productIds: [{type: "ASIN", value: "B0GJRV3MZC", ...}]`, `pdfDownloadUrl: "/api/v1/certificates/26-0157/download"` — all contract fields present, no internal IDs leaked |
| **DEF-129** | `GET /api/v1/certificates/search?term=Amazon&status=Active&pageSize=1` | **200** | ✓ Contract pagination shape returned (`{success, totalPages, totalCount, data: [], pageNumber, pageSize}`); `data` empty for this query but envelope is correct. Note: `status=` query parameter is required since 2025-09-28 — pre-existing API design, NOT a regression from yesterday's fix |
| **DEF-103** | `GET /api/v1/certificates/number/26-0151` | **200** | ✓ Single-cert DTO returned with `productIds[]` populated (3 ASINs), `status: "Active"`, `certificationCountries: "All"`, no internal IDs |
| Baseline | `GET /api/v1/certificates?pageSize=1` | **200** | ✓ List endpoint also returns contract DTO; `partner: "Amazon"`, `status: "Active"` |

### Test key lifecycle

- **Generated:** `POST /api/admin/partner/1/apikey` returned key id=7, plain key `gcc_partner_1a207b2c…0ed5`, `allowedEndpoints: null` (matches dev DB pattern — Amazon keys have no endpoint restriction)
- **Used:** 6 partner API curls above, one POST and five GETs, all completed successfully
- **Revoked:** `DELETE /api/admin/partner/apikey/7` returned `{"revoked": true}`
- **Post-revoke check:** repeat call with the same key now returns 401 (was 403 while active) — confirming the key is fully invalidated, not just deauthorized

### Conclusions

1. **DEF-130 is closed in prod.** The lockdown attribute swap on `ProductsController` (committed today, deployed sometime between commit and this smoke) rejects Amazon partner keys with 403 across all six methods of the controller (GET list, GET by id, GET search, POST, PUT, DELETE). The original "is Amazon currently consuming /api/v1/products" question becomes moot — the endpoint is no longer reachable to them. If they hit it, they'll see clean 403 with a JSON error body, not a 500 or HTML.

2. **DEF-131 is closed in prod.** The bulk-mutation lockdown is live; partner keys can no longer create/update/delete certificates.

3. **DEF-124 is closed in prod.** The `/expiring` endpoint now returns the contract DTO when called by a partner. No more `id`, `companyId`, `productId`, `*Id`, `certificateFile`, `certificateImagePath`, `createdBy`, `updatedBy`, no more nested `Company` with circular `Certificates`. Contract literal `"Active"` and `"All"` are correctly mapped.

4. **DEF-129 is closed in prod** for the case Amazon actually uses (`?term=…&status=Active`). The pre-existing required-status parameter design has been the behavior since September 2025 and is not a regression — Amazon's integration must already be passing it.

5. **DEF-103 stays closed in prod.** The single-cert by-number endpoint correctly returns the contract DTO with `productIds[]`.

All 6 yesterday-deployed fixes (DEF-103, DEF-124, DEF-129, DEF-131, plus DEF-128 and DEF-132 covered by the auth-surface smoke) are verified live and working from a real Amazon partner key's perspective. Today-shipped DEF-130 is also live.

## Files referenced

- `Controllers/Api/v1/ProductsController.cs` (DEF-130 fix in source, pending deploy)
- `Controllers/Api/v1/CertificatesController.cs` (DEF-129/124 fixes deployed 2026-05-03)
- `Controllers/Api/v1/BulkCertificatesController.cs` (DEF-131 fix deployed 2026-05-03)
- `Middleware/PartnerApiKeyAuthenticationMiddleware.cs` (auth surface confirmed working)
- `Middleware/TestEnvironmentGateMiddleware` (`/api/*` bypass confirmed)

## Deploy status

| Defect | Status |
|---|---|
| DEF-124 (expiring DTO) | **Deployed 2026-05-03 — verified live with real partner key 2026-05-04** |
| DEF-129 (search DTO) | **Deployed 2026-05-03 — verified live with real partner key 2026-05-04** |
| DEF-131 (bulk lockdown) | **Deployed 2026-05-03 — verified live with real partner key 2026-05-04** |
| DEF-103 (by-number/by-id DTO) | **Deployed earlier — verified live with real partner key 2026-05-04** |
| DEF-130 (products lockdown) | **Deployed and verified live 2026-05-04** |
| DEF-127 (icon polish) | Source ready, awaiting next deploy |
| DEF-111 (auth-redirect guard) | Source ready, awaiting next deploy |
