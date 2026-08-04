# Test Suite Triage — the 62 failing tests

**Date:** 2026-07-31 | **By:** Anuki Consulting
**Suite:** `GCCWebApp.Api.Tests` — **709 tests, 647 passing, 62 failing**
**Scope:** triage only. No production code changed. Two speculative fixture fixes were tried, measured, disproved, and reverted.

---

## 1. The headline is not the 62 failures

The 62 red tests are loud and pre-existing. The real finding is quieter and worse:

> **The partner-isolation suite currently cannot detect a data leak.** Roughly 34 of its tests
> pass *vacuously* — they assert "partner X does not see partner Y's data" against a result set
> that is empty for everyone. They would keep passing if isolation broke completely.

That is a false green on the control that protects [[feedback_partner_data_isolation]] — the single
most sensitive behavior in the product, and the thing DEF-103 and DEF-198 were built to guarantee.

---

## 2. Not caused by the current work — measured twice

| Run | Passed | Failed |
|---|---:|---:|
| Baseline (Batch 1–3 source changes stashed) | 640 | **62** |
| With Batch 1–3 changes applied | 647 | **62** |

Same 62, same class distribution; the +7 are the new DEF-322 certificate-numbering tests. Verified by
stashing only the source files and re-running, then diffing failing test names — **identical sets**.

---

## 3. Where the failures live

| Test class | Failures | Area |
|---|---:|---|
| `Integration.PartnerApiScopingTests` | 26 | Partner API scoping / isolation |
| `Unit.Controllers.CertificateTileExternalApiControllerTests` | 10 | External tile API |
| `Integration.MultiPartnerApiIntegrationTests` | 8 | Multi-partner (CR-171) |
| `Unit.Services.ProductIdServiceTests` | 5 | Product identifiers |
| `Unit.Services.CertificateServiceTests` | 5 | Certificate service |
| `Unit.Services.AdminServiceTests` | 3 | Admin service |
| `Unit.Repositories.QC_CertificateRepositoryTests` | 2 | Repository |
| `Unit.Controllers.CertificatesControllerTests` | 1 | |
| `Unit.Controllers.CertificateTilesApiControllerTests` | 1 | |
| `Integration.ApiIntegrationTests` | 1 | |

**44 of 62 (71%) sit in partner/external scoping.**

---

## 4. The diagnostic pattern — this is the important part

Splitting `PartnerApiScopingTests` by outcome makes the cause unmistakable:

**FAILING — every test that asserts data IS returned**
`Amazon_GetCertificates_ReturnsTwoCertificates` · `Novi_GetCertificates_ReturnsOneCertificate` ·
`Other_GetCertificates_ReturnsOneCertificate` · `Amazon_GetCertificatesWithProductIds_IncludesProductIdentifiers` ·
`Amazon_GetCertificates_AmazonCodeFieldPopulated` · `Partner_GetCertificates_ReturnsExpectedCount(…)` …

**PASSING — every test that asserts data is ABSENT**
`Amazon_GetCertificates_DoesNotReturnNoviOrOtherCertificates` · `Partner_GetCertificates_NeverReturnsNonePartner(…)` ·
`Novi_SearchByAmazonAsin_ReturnsEmptyDueToScoping` · `Partner_GetCertificates_AllReturnedCertsMatchPartner(…)` ·
`NoApiKey_GetCertificates_ReturnsUnauthorized` …

**The API returns an empty set to every caller.** Positive assertions fail; negative assertions are
satisfied trivially by emptiness. `AllReturnedCertsMatchPartner` is the clearest example — it iterates
the returned certificates and checks each one's partner. Over zero certificates that is a no-op.

**Fail-closed, so this is not evidence of a live leak** — nothing is being over-shared. But it does
mean the suite is not currently *proving* anything about isolation.

---

## 5. Two hypotheses tested and disproved

Both were plausible, both were measured against failing-test-name diffs, both changed **nothing**
(34 failed → 34 failed, identical names). Reverted rather than left in the tree.

**Hypothesis 1 — legacy status literal.** `PartnerScopingFactory` seeds all 5 certificates with
`Status = "Active"`, which is **not** one of the 8 workflow statuses and **not** in
`CertificateStatusHelper.PublicStatuses` (`Client Approved - Paid`, `Republished`). Since partner reads
are filtered to `PublicStatuses`, every seeded certificate should be invisible. Changing them to
`Client Approved - Paid` fixed nothing — so the data is not reaching the query at all, and the status
filter is not the (only) gate.
*Still true and still worth correcting eventually — "Active" is a legacy literal that shouldn't be in a fixture.*

**Hypothesis 2 — EF InMemory store root.** The factory seeds through
`services.BuildServiceProvider()`, a second container. EF Core's InMemory provider keys stores by
*(name, `InMemoryDatabaseRoot`)*, so a matching database name alone does not guarantee a shared store.
Passing an explicit shared `InMemoryDatabaseRoot` fixed nothing either.

**What this rules in.** Since neither the status filter nor the store root is responsible, the next
candidates are the request pipeline rather than the data: partner API-key authentication inside
`WebApplicationFactory` (several failures surface as **401 Unauthorized**, and `ValidateApiKeyAsync`
does a plaintext comparison that ought to match the fixture), or the `Testing` environment branch in
`Program.cs` short-circuiting middleware the tests depend on.

---

## 5a. UPDATE — vacuous passes fixed (DEF-326)

Recommendation 1 below has been **done**. `PartnerApiScopingTests` now treats an empty result set as a
failure wherever emptiness would make an assertion meaningless:

- `RequireNonEmptyData(doc, because)` helper, used in place of a bare `GetProperty("data")` in every
  absence-asserting test.
- **Positive controls** on the two tests that legitimately expect empty — each now proves the endpoint
  returns the caller's **own** product ID first, so the empty cross-partner result means something.
- `GetCertificateFieldNames` no longer returns an empty list on empty data (that made the
  "all partners get the same response shape" test compare three empty lists).
- Two GS1 loops guarded; the GTIN test also counts the GTINs it actually examined.

| | Before | After |
|---|---:|---:|
| Passing | 647 | **633** |
| Failing | 62 | **76** |
| `PartnerApiScopingTests` failures | 26 | **40** |

**14 vacuous passes became honest failures.** The count going up is the intended result — the suite now
reports partner isolation as *unverified* rather than *verified*. All other test classes unchanged.

Six tests still pass and should: three are genuinely data-independent (auth / input validation) and
`PaginationMetadataPresent` legitimately checks the response envelope, which is valid on an empty page.

---

## 6. Recommendation

**This is not a deploy blocker.** The failures pre-date this work, they fail closed, and the Batch 1–3
changes are independently validated (unit tests, SQL ground-truth checks, and browser verification).

**It is a real gap, and the vacuous passes are the part to fix first.** Priority order:

1. **Make the isolation tests fail when they prove nothing.** Any test asserting absence should first
   assert the result set is non-empty — otherwise emptiness masquerades as correctness. This is a
   small change and it converts 34 misleading greens into an honest signal.
2. **Find why the fixture returns nothing.** Start by capturing one response body and the app log from
   a single `PartnerApiScopingTests` run; the 401s point at the API-key path through
   `WebApplicationFactory`.
3. **Then re-check hypothesis 1**, which may become load-bearing once data flows.
4. **Triage the remaining 18** (ProductIdService, CertificateService, AdminService, repository) separately —
   not yet examined.

**Interim assurance:** the maintained P0 security gate in `Scripts/smoke-prod/_security/` runs against a
real deployment rather than this fixture, so partner isolation still has live coverage. Worth running it
before the next deploy given this suite's blind spot.

---

*No production code was modified during this triage. The two fixture experiments were reverted after
measurement; `GCCWebApp.Api.Tests` is unchanged apart from the new DEF-322 test file.*
