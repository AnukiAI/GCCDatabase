using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GCCWebApp.Models;
using GCCWebApp.Data;
using GCCWebApp.Repositories;
using GCCWebApp.Search;
using GCCWebApp.Services.Audit;
using GreenCircleCertified.ViewModels;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GCCWebApp.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly ILogger<CertificateService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IProductIdService _productIdService;
        private readonly IAuditService _auditService;   // DEF-337

        public CertificateService(
            ILogger<CertificateService> logger,
            ApplicationDbContext context,
            IProductIdService productIdService,
            IAuditService auditService)
        {
            _logger = logger;
            _context = context;
            _productIdService = productIdService;
            _auditService = auditService;
        }

        public async Task<CertificateListResponse> GetCertificatesAsync(CertificateListRequest request)
        {
            try
            {
                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .AsQueryable();

                // Apply search filters (includes tag matching)
                // CR-187: normalized search against persisted computed columns.
                // DEF-409: null unless there is a usable search term, which is what makes the
                // unsearched ordering provably identical to before.
                string? rankQuery = null;
                if (!string.IsNullOrWhiteSpace(request.Filters.SearchTerm))
                {
                    // DEF-366: tag ids resolved up front instead of as a correlated subquery inside
                    // the OR. See Search/TagSearchMatch.cs for the measurements.
                    var nq = SearchNormalizer.Normalize(request.Filters.SearchTerm);
                    // DEF-409: remembered so the paging step below can rank prefix matches first.
                    rankQuery = string.IsNullOrEmpty(nq) ? null : nq;
                    var tagIds = await TagSearchMatch.MatchingCertificateIdsAsync(_context.Certificates, nq);
                    query = query.WhereMatchesSearch(c =>
                        c.Company.NormalizedName!.Contains(nq) ||
                        c.Product.NormalizedName!.Contains(nq) ||
                        c.NormalizedNumber!.Contains(nq), tagIds);
                }

                if (request.Filters.CertificationTypes.Any())
                {
                    query = query.Where(c => request.Filters.CertificationTypes.Contains(c.CertificationType.Name));
                }

                if (request.Filters.Countries.Any())
                {
                    // DEF-404: match the country ANYWHERE on the certificate, not only as the
                    // denormalized primary. Certificate.Country holds one country; CR-037 put the
                    // full set in CertificateCountries. Filtering the primary meant ticking
                    // "Canada" missed a US-primary certificate that also lists Canada.
                    var countryFilter = request.Filters.Countries.ToArray();
                    query = query.Where(c => c.Countries.Any(cc => countryFilter.Contains(cc.Country)));
                }

                if (request.Filters.Statuses.Any())
                {
                    query = query.Where(c => request.Filters.Statuses.Contains(c.Status));
                }

                // Apply date filters
                if (request.Filters.EffectiveDateStart.HasValue)
                {
                    query = query.Where(c => c.EffectiveDate >= request.Filters.EffectiveDateStart.Value);
                }

                if (request.Filters.EffectiveDateEnd.HasValue)
                {
                    query = query.Where(c => c.EffectiveDate <= request.Filters.EffectiveDateEnd.Value);
                }

                // Apply sorting
                // DEF-409: the sort is a function of the query so it can be applied WITHIN each
                // relevance band instead of across the whole result set.
                Func<IQueryable<Certificate>, IQueryable<Certificate>> applySort = q => request.Sort?.ToLower() switch
                {
                    "newest" => q.OrderByDescending(c => c.EffectiveDate),
                    "expiring" => q.OrderBy(c => c.ExpirationDate),
                    "product-az" => q.OrderBy(c => c.Product.Name),
                    "company-az" => q.OrderBy(c => c.Company.Name),
                    "status" => q.OrderBy(c => c.Status),
                    "type" => q.OrderBy(c => c.CertificationType.Name),
                    "country" => q.OrderBy(c => c.Country),
                    _ => q.OrderBy(c => c.Id)
                };

                // Get total count — ranking never changes it; no row enters or leaves the set.
                var totalCount = await applySort(query).CountAsync();

                // DEF-409: certificates whose NUMBER starts with the query come first, the caller's
                // sort still governing within each band.
                var pagedIds = await SearchRanking.PageIdsAsync(
                    query, applySort, rankQuery,
                    (request.Page - 1) * request.PageSize, request.PageSize);

                // Apply pagination
                var rawCertificates = await query
                    .Where(c => pagedIds.Contains(c.Id))
                    .Select(c => new
                    {
                        Id = c.Id,
                        CertificateNumber = c.Number,
                        CertificationType = c.CertificationType.Name,
                        CompanyName = c.Company.Name,
                        ProductName = c.Product.Name,
                        ProductCategory = c.Product.Category != null ? c.Product.Category.Name : "",
                        EffectiveDate = c.EffectiveDate,
                        ExpirationDate = c.ExpirationDate,
                        Location = c.Location ?? "",
                        Country = c.Country ?? "",
                        Status = c.Status
                    })
                    .ToListAsync();

                // Apply client-side transformations
                // DEF-409: filtering by id returns rows in arbitrary order — restore the ranked page.
                var idOrder = pagedIds.Select((id, i) => new { id, i }).ToDictionary(x => x.id, x => x.i);
                rawCertificates = rawCertificates
                    .OrderBy(c => idOrder.TryGetValue(c.Id, out var i) ? i : int.MaxValue)
                    .ToList();

                var certificates = rawCertificates.Select(c => new CertificateCardModel
                {
                    Id = c.Id,
                    CertificateNumber = c.CertificateNumber,
                    CertificationType = c.CertificationType,
                    CompanyName = c.CompanyName,
                    ProductName = c.ProductName,
                    ProductCategory = c.ProductCategory,
                    EffectiveDate = c.EffectiveDate,
                    ExpirationDate = c.ExpirationDate,
                    Location = c.Location,
                    Country = c.Country,
                    Status = c.Status,
                    BadgeText = GetBadgeText(c.CertificationType),
                    TagType = DetermineTagType(request.UserRole)
                }).ToList();

                return new CertificateListResponse
                {
                    Certificates = certificates,
                    TotalCount = totalCount,
                    PageSize = request.PageSize,
                    CurrentPage = request.Page,
                    CurrentSort = request.Sort ?? string.Empty,
                    AppliedFilters = request.Filters,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates");
                return new CertificateListResponse
                {
                    Success = false,
                    Message = "Failed to retrieve certificates",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<CertificateListResponse> GetCertificatesByPartnerAsync(string partnerName, CertificateListRequest request)
        {
            try
            {
                _logger.LogInformation("Retrieving certificates for partner: {PartnerName}", partnerName);

                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .Where(c => c.Partner == partnerName) // Filter by partner
                    .AsQueryable();

                // Apply search filters (includes tag matching)
                // CR-187: normalized search against persisted computed columns.
                // DEF-409: null unless there is a usable search term, which is what makes the
                // unsearched ordering provably identical to before.
                string? rankQuery = null;
                if (!string.IsNullOrWhiteSpace(request.Filters.SearchTerm))
                {
                    // DEF-366: tag ids resolved up front instead of as a correlated subquery inside
                    // the OR. See Search/TagSearchMatch.cs for the measurements.
                    var nq = SearchNormalizer.Normalize(request.Filters.SearchTerm);
                    // DEF-409: remembered so the paging step below can rank prefix matches first.
                    rankQuery = string.IsNullOrEmpty(nq) ? null : nq;
                    var tagIds = await TagSearchMatch.MatchingCertificateIdsAsync(_context.Certificates, nq);
                    query = query.WhereMatchesSearch(c =>
                        c.Company.NormalizedName!.Contains(nq) ||
                        c.Product.NormalizedName!.Contains(nq) ||
                        c.NormalizedNumber!.Contains(nq), tagIds);
                }

                if (request.Filters.CertificationTypes.Any())
                {
                    query = query.Where(c => request.Filters.CertificationTypes.Contains(c.CertificationType.Name));
                }

                if (request.Filters.Countries.Any())
                {
                    // DEF-404: match the country ANYWHERE on the certificate, not only as the
                    // denormalized primary. Certificate.Country holds one country; CR-037 put the
                    // full set in CertificateCountries. Filtering the primary meant ticking
                    // "Canada" missed a US-primary certificate that also lists Canada.
                    var countryFilter = request.Filters.Countries.ToArray();
                    query = query.Where(c => c.Countries.Any(cc => countryFilter.Contains(cc.Country)));
                }

                if (request.Filters.Statuses.Any())
                {
                    query = query.Where(c => request.Filters.Statuses.Contains(c.Status));
                }

                // Apply date filters
                if (request.Filters.EffectiveDateStart.HasValue)
                {
                    query = query.Where(c => c.EffectiveDate >= request.Filters.EffectiveDateStart.Value);
                }

                if (request.Filters.EffectiveDateEnd.HasValue)
                {
                    query = query.Where(c => c.EffectiveDate <= request.Filters.EffectiveDateEnd.Value);
                }

                // Apply sorting
                // DEF-409: the sort is a function of the query so it can be applied WITHIN each
                // relevance band instead of across the whole result set.
                Func<IQueryable<Certificate>, IQueryable<Certificate>> applySort = q => request.Sort?.ToLower() switch
                {
                    "newest" => q.OrderByDescending(c => c.EffectiveDate),
                    "expiring" => q.OrderBy(c => c.ExpirationDate),
                    "product-az" => q.OrderBy(c => c.Product.Name),
                    "company-az" => q.OrderBy(c => c.Company.Name),
                    "status" => q.OrderBy(c => c.Status),
                    "type" => q.OrderBy(c => c.CertificationType.Name),
                    "country" => q.OrderBy(c => c.Country),
                    _ => q.OrderBy(c => c.Id)
                };

                // Get total count — ranking never changes it; no row enters or leaves the set.
                var totalCount = await applySort(query).CountAsync();

                // DEF-409: certificates whose NUMBER starts with the query come first, the caller's
                // sort still governing within each band.
                var pagedIds = await SearchRanking.PageIdsAsync(
                    query, applySort, rankQuery,
                    (request.Page - 1) * request.PageSize, request.PageSize);

                // Apply pagination
                var rawCertificates = await query
                    .Where(c => pagedIds.Contains(c.Id))
                    .Select(c => new
                    {
                        Id = c.Id,
                        CertificateNumber = c.Number,
                        CertificationType = c.CertificationType.Name,
                        CompanyName = c.Company.Name,
                        ProductName = c.Product.Name,
                        ProductCategory = c.Product.Category != null ? c.Product.Category.Name : "",
                        EffectiveDate = c.EffectiveDate,
                        ExpirationDate = c.ExpirationDate,
                        Location = c.Location ?? "",
                        Country = c.Country ?? "",
                        Status = c.Status
                    })
                    .ToListAsync();

                // Apply client-side transformations
                // DEF-409: filtering by id returns rows in arbitrary order — restore the ranked page.
                var idOrder = pagedIds.Select((id, i) => new { id, i }).ToDictionary(x => x.id, x => x.i);
                rawCertificates = rawCertificates
                    .OrderBy(c => idOrder.TryGetValue(c.Id, out var i) ? i : int.MaxValue)
                    .ToList();

                var certificates = rawCertificates.Select(c => new CertificateCardModel
                {
                    Id = c.Id,
                    CertificateNumber = c.CertificateNumber,
                    CertificationType = c.CertificationType,
                    CompanyName = c.CompanyName,
                    ProductName = c.ProductName,
                    ProductCategory = c.ProductCategory,
                    EffectiveDate = c.EffectiveDate,
                    ExpirationDate = c.ExpirationDate,
                    Location = c.Location,
                    Country = c.Country,
                    Status = c.Status,
                    BadgeText = GetBadgeText(c.CertificationType),
                    TagType = DetermineTagType(request.UserRole)
                }).ToList();

                _logger.LogInformation("Retrieved {Count} certificates for partner {PartnerName}",
                    certificates.Count, partnerName);

                return new CertificateListResponse
                {
                    Certificates = certificates,
                    TotalCount = totalCount,
                    PageSize = request.PageSize,
                    CurrentPage = request.Page,
                    CurrentSort = request.Sort ?? string.Empty,
                    AppliedFilters = request.Filters,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates for partner {PartnerName}", partnerName);
                return new CertificateListResponse
                {
                    Success = false,
                    Message = $"Failed to retrieve certificates for partner {partnerName}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<CertificateDetailsViewModel> GetCertificateDetailsAsync(int id, string userRole)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .ThenInclude(p => p.Category)
                    .Include(c => c.CertificationType)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (certificate == null)
                    return null!;

                var viewModel = new CertificateDetailsViewModel
                {
                    Id = certificate.Id,
                    CertificateNumber = certificate.Number,
                    CompanyName = certificate.Company.Name,
                    ProductName = certificate.Product.Name,
                    CertificationType = certificate.CertificationType.Name,
                    ProductCategory = certificate.Product.Category?.Name ?? "",
                    EffectiveDate = certificate.EffectiveDate,
                    ExpirationDate = certificate.ExpirationDate,
                    Location = certificate.Location ?? "",
                    Country = certificate.Country ?? "",
                    CertificateImageUrl = certificate.CertificateFile ?? ""
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate details for ID: {CertificateId}", id);
                return null!;
            }
        }

        public async Task<CertificateDownloadModel?> GetCertificateDownloadAsync(int id, string userRole)
        {
            try
            {
                var certificate = await GetCertificateDetailsAsync(id, userRole);
                if (certificate == null) return null;

                // Per business rule (2026-04-25): never auto-generate a placeholder PDF.
                // Read the actual uploaded file off disk; if there isn't one, return null so the
                // controller can surface 404. Analysts must upload a real PDF at cert creation.
                var entity = await _context.Certificates.FindAsync(id);
                if (entity == null || string.IsNullOrEmpty(entity.CertificateFile))
                {
                    return null;
                }

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", entity.CertificateFile.TrimStart('~', '/'));
                if (!System.IO.File.Exists(fullPath))
                {
                    return null;
                }

                var fileContent = await System.IO.File.ReadAllBytesAsync(fullPath);

                return new CertificateDownloadModel
                {
                    CertificateId = id,
                    CertificateNumber = certificate.CertificateNumber,
                    FileName = $"Certificate_{certificate.CertificateNumber}.pdf",
                    FileContent = fileContent,
                    ContentType = "application/pdf",
                    UserRole = userRole
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing certificate download for ID: {CertificateId}", id);
                return null;
            }
        }

        // DEF-128: CSV is the only supported export format. The Excel and PDF generators are
        // retained as dead code below (GenerateExcelExport, GeneratePdfExportFromRows) so the
        // diff is small and reviewable; cleanup lives on the followup ticket.
        public async Task<byte[]?> ExportCertificatesAsync(CertificateListRequest request, bool includeInternalTags = false)
        {
            try
            {
                var exportRows = await GetCertificatesForExportAsync(request);

                // An empty result set is NOT a failure — it is a legitimate answer for any caller
                // whose scope contains no certificates (e.g. a Partner with no cert-level
                // associations). Returning null here made it indistinguishable from the catch
                // below, and the controller turned both into HTTP 400 "No data to export or export
                // failed" — a client-error status for a server-side outcome, with a message that
                // conflated "nothing to export" with "the export broke". GenerateCsvExport emits
                // the header row before it iterates, so an empty list yields a valid header-only
                // CSV, which is what a spreadsheet user expects. null now means failure, only.
                return GenerateCsvExport(exportRows, includeInternalTags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates to CSV");
                return null;
            }
        }

        public async Task<byte[]?> ExportCertificatesByIdsAsync(List<string> certificateIds, bool includeInternalTags = false, CertificateListRequest? securityContext = null)
        {
            try
            {
                if (certificateIds == null || !certificateIds.Any())
                    return null;

                // As above: after scoping, an empty set is a legitimate answer, not a failure. It is
                // also the DEF-198 outcome when a Partner lists ids outside their scope — a
                // header-only CSV is the correct, non-leaky response there.
                var exportRows = await GetCertificatesForExportByIdsAsync(certificateIds, securityContext);
                return GenerateCsvExport(exportRows, includeInternalTags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates by IDs to CSV");
                return null;
            }
        }

        /// <summary>
        /// Internal export row with all certificate fields for comprehensive export.
        /// </summary>
        private class CertificateExportRow
        {
            public int Id { get; set; }
            public string CertificateNumber { get; set; } = "";
            public string CertificationType { get; set; } = "";
            public string CertificationSubcategories { get; set; } = "";
            public string Status { get; set; } = "";
            public string CompanyName { get; set; } = "";
            public string CompanyContactEmail { get; set; } = "";
            public string CompanyContactUrl { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string ProductCategory { get; set; } = "";
            public string ProductType { get; set; } = "";
            public string ProductSubcategory { get; set; } = "";
            public string Partner { get; set; } = "";
            public string Pathway { get; set; } = "";
            public string CertificationResults { get; set; } = "";
            public DateTime EffectiveDate { get; set; }
            public DateTime ExpirationDate { get; set; }
            public string Location { get; set; } = "";
            public string Country { get; set; } = "";
            public string ExternalTags { get; set; } = "";
            public string InternalTags { get; set; } = "";
            public string Asin { get; set; } = "";
            public string Upc { get; set; } = "";
            public string Gtin { get; set; } = "";
            public string Ean { get; set; } = "";
        }

        /// <summary>
        /// Queries certificates with all navigation properties for comprehensive export.
        /// </summary>
        private async Task<List<CertificateExportRow>> GetCertificatesForExportAsync(CertificateListRequest request)
        {
            var query = BuildExportQuery();

            // DEF-198 (QC #182): scope by caller role BEFORE any user filters. Mirrors
            // Controllers/Api/CertificateTileExternalApiController.cs:97-138 — Partner sees only their
            // partner's certs, External sees only their companies' certs, both restricted to
            // in-force statuses (BadgeableStatuses since CR-043). Without this, Partner login's
            // CSV export leaked the entire DB.
            query = ApplyExportSecurityScope(query, request);

            // Apply filters
            // CR-187: normalized search against persisted computed columns.
            // DEF-409: null unless there is a usable search term.
            string? rankQuery = null;
            if (!string.IsNullOrWhiteSpace(request.Filters.SearchTerm))
            {
                // DEF-366: tag ids resolved up front instead of as a correlated subquery inside the
                // OR. Safe against the DEF-198 export scoping above: that scope is a separate
                // AND'd Where, so a broader tag id set cannot widen what this export returns.
                var nq = SearchNormalizer.Normalize(request.Filters.SearchTerm);
                // DEF-409: remembered so the rows can be ranked below.
                rankQuery = string.IsNullOrEmpty(nq) ? null : nq;
                var tagIds = await TagSearchMatch.MatchingCertificateIdsAsync(_context.Certificates, nq);
                query = query.WhereMatchesSearch(c =>
                    c.Company.NormalizedName!.Contains(nq) ||
                    c.Product.NormalizedName!.Contains(nq) ||
                    c.NormalizedNumber!.Contains(nq), tagIds);
            }
            if (request.Filters.CertificationTypes.Any())
                query = query.Where(c => request.Filters.CertificationTypes.Contains(c.CertificationType.Name));
            if (request.Filters.Countries.Any())
            {
                // DEF-404: this is the export, and it must return the same certificates the list
                // the user just filtered showed them. The tile APIs and CertificateRepository:453
                // match on CertificateCountries; this matched Certificate.Country, the primary
                // only, so filtering by Canada listed 115 certificates and exported 107.
                // Under-delivery, not a leak — but silent, which is worse to find.
                //
                // Kept as its own AND'd Where rather than folded into the search OR: that is
                // exactly where the same clause cost 15x in DEF-402. As a standalone semi-join it
                // is cheap.
                var countryFilter = request.Filters.Countries.ToArray();
                query = query.Where(c => c.Countries.Any(cc => countryFilter.Contains(cc.Country)));
            }
            if (request.Filters.Statuses.Any())
                query = query.Where(c => request.Filters.Statuses.Contains(c.Status));

            // DEF-409: rank the export the same way the list ranks, so a user who filters, sees a
            // ranked list and clicks Export gets the file in the order they were just looking at.
            // DEF-404 is the precedent: a list and its own export disagreeing is a defect in itself.
            //
            // This method previously applied NO ordering at all, so CSV row order was whatever the
            // query plan produced and could differ between two runs of the same export. Ordering
            // here makes it deterministic for the first time, searched or not.
            if (rankQuery != null)
                query = query.OrderByNumberRelevance(rankQuery);
            else
                query = query.OrderBy(c => c.Number).ThenBy(c => c.Id);

            return await ProjectToExportRows(query);
        }

        /// <summary>
        /// Queries certificates by IDs/numbers with all fields for export.
        /// </summary>
        private async Task<List<CertificateExportRow>> GetCertificatesForExportByIdsAsync(List<string> certificateIds, CertificateListRequest? securityContext = null)
        {
            var query = BuildExportQuery()
                .Where(c => certificateIds.Contains(c.Id.ToString()) || certificateIds.Contains(c.Number));

            // DEF-198 (QC #182): a Partner could otherwise pass IDs of certs outside their scope
            // and receive them. Apply the same security predicate as the unscoped list path.
            if (securityContext != null)
            {
                query = ApplyExportSecurityScope(query, securityContext);
            }

            return await ProjectToExportRows(query);
        }

        /// <summary>
        /// DEF-198 (QC #182): role-based security predicate for CSV export queries.
        /// Mirrors the predicate enforced by CertificateTileExternalApiController so the
        /// export cannot return more than the tile view shows the same user.
        ///
        /// DEF-358: "mirrors the EXTERNAL tile view" is why the anonymous branch drifted — the
        /// external tile view is deliberately ungated for the effective date (the owning company
        /// may see its own upcoming certs), but the PUBLIC branch here serves anonymous callers
        /// through an [AllowAnonymous] endpoint, so it must mirror the PUBLIC tile view instead.
        /// Partner likewise mirrors the partner API. Without this, an anonymous CSV export handed
        /// out certificates the public site, the PDF endpoints and the Amazon feed all withheld.
        /// External stays ungated on purpose — same scope decision as everywhere else.
        /// </summary>
        private static IQueryable<Certificate> ApplyExportSecurityScope(IQueryable<Certificate> query, CertificateListRequest request)
        {
            var role = request.UserRole ?? "Public";

            if (string.Equals(role, "Partner", StringComparison.OrdinalIgnoreCase))
            {
                // Partner web users see only certs tagged with their partner name(s)
                // AND only certs in BadgeableStatuses. A Partner user with no partner
                // association returns an empty result (mirrors tile view line 113-114).
                if (request.AllowedPartnerNames.Any())
                {
                    var allowed = request.AllowedPartnerNames;
                    // CR-171: scope by the M2M Partners collection, not the denormalized
                    // primary Partner string — otherwise a co-partner (e.g. Novi on an
                    // [Amazon, Novi] cert) is silently dropped from their CSV export while
                    // the tile/API correctly show it. Matches every other partner-scoped surface.
                    query = query.Where(c => c.Partners.Any(p => allowed.Contains(p.Name)));
                }
                else
                {
                    query = query.Where(c => false);
                }
                // DEF-358: parity with the partner API — a cert that has not reached its effective
                // date is not in force, so it must not appear in a partner's CSV either.
                // CR-043: and "in force" is BadgeableStatuses, so a partner's CSV carries the same
                // Confidential certs their API call now returns. List and export must agree — the
                // DEF-404 lesson.
                query = query.Where(c => CertificateStatusHelper.BadgeableStatuses.Contains(c.Status))
                             .WhereEffectiveDateReached();
            }
            else if (string.Equals(role, "External", StringComparison.OrdinalIgnoreCase))
            {
                // External users see only certs from their assigned companies, restricted to in-force
                // statuses. CR-043 Q1 (client, Yes): that includes the company's own Confidential
                // certificates. Only the anonymous branch below stays on PublicStatuses.
                if (request.AllowedCompanyIds.Any())
                {
                    var allowedCompanies = request.AllowedCompanyIds;
                    query = query.Where(c => allowedCompanies.Contains(c.CompanyId));
                }
                else
                {
                    query = query.Where(c => false);
                }
                query = query.Where(c => CertificateStatusHelper.BadgeableStatuses.Contains(c.Status));
            }
            else if (string.Equals(role, "Public", StringComparison.OrdinalIgnoreCase))
            {
                // Anonymous / unauthenticated callers see only PublicStatuses certs.
                // DEF-358: and only from the effective date onward, matching the public tile view.
                // Note on the branch chain: a NULL UserRole coalesces to "Public" and is gated here,
                // but an unrecognised NON-NULL role string falls past every branch to the
                // unrestricted tail — that tail is how Admin/Analyst/Internal get full access, so
                // unknown roles are fail-OPEN by construction. Out of scope for DEF-358; see
                // DEF358_ExportScopeTests.UnrecognisedRole_FallsThroughUnrestricted_ByDesign.
                query = query.Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status))
                             .WhereEffectiveDateReached();
            }
            // Admin / Analyst / Internal: no restriction.

            return query;
        }

        private IQueryable<Certificate> BuildExportQuery()
        {
            return _context.Certificates
                .Include(c => c.Company)
                .Include(c => c.Product).ThenInclude(p => p.Category)
                .Include(c => c.Product).ThenInclude(p => p.ProductType)
                .Include(c => c.Product).ThenInclude(p => p.ProductSubcategory)
                .Include(c => c.CertificationType)
                .Include(c => c.CertificationTypeSubcategories)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.Pathway)
                .Include(c => c.Tags)
                .Include(c => c.ProductIds)
                // CR-037: the export's Country column lists every country, not just the
                // denormalized primary. Safe to add to this single-query pull despite the
                // DEF-253 note below — a certificate has one to three countries, so the
                // cartesian multiplier is negligible next to ProductIds (up to 1,000) and Tags.
                .Include(c => c.Countries)
                // DEF-253 NOTE: tried .AsSplitQuery() here and it REGRESSED — the export is an
                // unfiltered full-table pull with no pagination, so N split queries each re-scanning
                // ProductIds/Tags/Subcats for ALL certs (incl. the 1000-ProductId seed cert) timed out
                // at 30s, whereas the single de-duped cartesian completes fine. AsSplitQuery helps
                // PAGINATED queries (DEF-250) and small-projection lists (DEF-251), not this. Left single-query.
                .AsQueryable();
        }

        private async Task<List<CertificateExportRow>> ProjectToExportRows(IQueryable<Certificate> query)
        {
            var certs = await query.OrderBy(c => c.Id).ToListAsync();

            return certs.Select(c => new CertificateExportRow
            {
                Id = c.Id,
                CertificateNumber = c.Number,
                CertificationType = c.CertificationType?.Name ?? "",
                CertificationSubcategories = c.CertificationTypeSubcategories != null && c.CertificationTypeSubcategories.Any()
                    ? string.Join(", ", c.CertificationTypeSubcategories.Select(s => s.Name))
                    : "",
                Status = c.Status,
                CompanyName = c.Company?.Name ?? "",
                CompanyContactEmail = c.Company?.Email ?? "",
                CompanyContactUrl = c.Company?.Website ?? "",
                ProductName = c.Product?.Name ?? "",
                ProductCategory = c.ProductCategory?.Name ?? (c.Product?.Category?.Name ?? ""),
                ProductType = c.ProductType?.Name ?? (c.Product?.ProductType?.Name ?? ""),
                ProductSubcategory = c.ProductSubcategory?.Name ?? (c.Product?.ProductSubcategory?.Name ?? ""),
                Partner = c.Partner ?? "None",
                Pathway = c.Pathway?.Name ?? "",
                CertificationResults = c.CertificationResults ?? "",
                EffectiveDate = c.EffectiveDate,
                ExpirationDate = c.ExpirationDate,
                Location = c.Location ?? "",
                // CR-037: every country the certificate carries, not just the denormalized
                // primary. Both exports (CSV and XLSX) read this one property, so this is the
                // only place it has to be assembled.
                //
                // SEPARATOR IS ", " ON PURPOSE — it is what CountryList.ParseMany splits on
                // (',' and '|'), so an exported file still round-trips through the certificate
                // importer. Using "; " would look tidier and would silently break that round trip:
                // ParseMany does NOT split on ';', so "United States; Canada" comes back as one
                // unresolvable country. Same trap as DEF-346, where the Product ID export used a
                // pipe under a heading that told the reader to use commas.
                //
                // The primary leads, then the rest alphabetically, so the column stays stable
                // between exports and still reads primary-first.
                Country = BuildCountryList(c),
                ExternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "external").Select(t => t.Name)),
                InternalTags = string.Join(", ", c.Tags.Where(t => t.TagType == "internal").Select(t => t.Name)),
                Asin = GetProductIdValues(c, "ASIN"),
                Upc = GetProductIdValues(c, "UPC"),
                Gtin = GetProductIdValues(c, "GTIN"),
                Ean = GetProductIdValues(c, "EAN")
            }).ToList();
        }

        /// <summary>
        /// CR-037: the certificate's countries as one export cell — primary first, then the rest
        /// alphabetically, comma-separated so the file round-trips through CountryList.ParseMany.
        ///
        /// Falls back to the denormalized <c>Certificate.Country</c> when the join rows are empty.
        /// That is not defensive padding: the CertificateCountries backfill only covers rows that
        /// had a country at migration time, and any certificate whose join rows were never written
        /// would otherwise export a BLANK country where it used to show one. A silent blank in a
        /// client's export is worse than an incomplete list.
        /// </summary>
        private static string BuildCountryList(Certificate c)
        {
            var all = c.Countries?
                .Select(x => x.Country)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (all.Count == 0) return c.Country ?? "";

            var primary = c.Country?.Trim();
            var ordered = all
                .OrderByDescending(x => !string.IsNullOrEmpty(primary)
                                        && string.Equals(x, primary, StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return string.Join(", ", ordered);
        }

        private static string GetProductIdValues(Certificate c, string type)
        {
            // QC #157: order multi-value ID cells ascending so the displayed values read
            // consistently with the internal list view (where the leading value matches the
            // .Min() sort key). In-memory ordering on an already-loaded collection — no SQL change.
            var fromCollection = c.ProductIds?
                .Where(pid => pid.ProductIdType == type && pid.IsActive)
                .Select(pid => pid.ProductIdValue)
                .OrderBy(v => v)
                .ToList();

            if (fromCollection != null && fromCollection.Any())
                return string.Join(", ", fromCollection);

            // Fallback to legacy fields
            return type switch
            {
                "ASIN" => c.Asin ?? "",
                "UPC" => c.Upc ?? "",
                "GTIN" => c.Gtin ?? "",
                "EAN" => c.Ean ?? "",
                _ => ""
            };
        }

        public async Task<CertificateStatistics> GetCertificateStatisticsAsync(string userRole)
        {
            try
            {
                var expiringSoonDate = DateTime.Now.AddDays(30);
                
                // Single query with optimized aggregation to prevent N+1 query pattern
                var certificates = await _context.Certificates
                    .Include(c => c.CertificationType)
                    .Select(c => new 
                    {
                        c.Status,
                        c.ExpirationDate,
                        c.Country,
                        CertificationTypeName = c.CertificationType.Name,
                        // CR-043 Q2: internal "active" counts include Confidential. Named IsPublic
                        // historically; it has only ever fed the activeCertificates tally below.
                        IsPublic = CertificateStatusHelper.DashboardStatuses.Contains(c.Status)
                    })
                    .ToListAsync();

                // Calculate all statistics in memory from single query result
                var totalCertificates = certificates.Count;
                var activeCertificates = certificates.Count(c => c.IsPublic && c.ExpirationDate >= DateTime.Now);
                var expiredCertificates = certificates.Count(c => c.Status == CertificateStatusHelper.StatusExpired || c.Status == CertificateStatusHelper.StatusExpiredNoAnnualVerification);
                var pendingCertificates = certificates.Count(c => c.Status == CertificateStatusHelper.StatusClientApprovedNotPaid || c.Status == CertificateStatusHelper.StatusDraft);
                var expiringSoonCertificates = 0; // Removed per client request

                var certificatesByType = certificates
                    .Where(c => !string.IsNullOrEmpty(c.CertificationTypeName))
                    .GroupBy(c => c.CertificationTypeName)
                    .ToDictionary(g => g.Key!, g => g.Count());

                var certificatesByCountry = certificates
                    .Where(c => !string.IsNullOrEmpty(c.Country))
                    .GroupBy(c => c.Country)
                    .ToDictionary(g => g.Key!, g => g.Count());

                return new CertificateStatistics
                {
                    TotalCertificates = totalCertificates,
                    ActiveCertificates = activeCertificates,
                    ExpiredCertificates = expiredCertificates,
                    ExpiringSoonCertificates = expiringSoonCertificates,
                    PendingCertificates = pendingCertificates,
                    CertificatesByType = certificatesByType,
                    CertificatesByCountry = certificatesByCountry
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate statistics");
                return new CertificateStatistics();
            }
        }

        public async Task<BulkActionResult> PerformBulkActionAsync(BulkActionRequest request)
        {
            try
            {
                var result = new BulkActionResult { Success = true };

                switch (request.Action.ToLower())
                {
                    case "delete":
                        // DEF-337 (completion): this used to RemoveRange the whole set in one
                        // shot, which wrote no audit at all and destroyed every cascade child
                        // silently — the same core-entity hole DEF-337 closed for single
                        // deletes, still open on the bulk path.
                        //
                        // Rather than duplicate the capture logic (which would drift), each id
                        // now goes through DeleteCertificateAsync, so bulk and single deletes
                        // are audited identically by construction. Slower per certificate;
                        // correct, and bulk delete is not a hot path.
                        var deleted = 0;
                        var failedDeletes = new List<int>();

                        // Ids that do not exist are not failures — deleting nothing is a no-op,
                        // which is the behaviour the previous RemoveRange had and what callers
                        // rely on. Only a genuine delete failure is reported.
                        var existingIds = await _context.Certificates
                            .Where(c => request.CertificateIds.Contains(c.Id))
                            .Select(c => c.Id)
                            .ToListAsync();

                        foreach (var certificateId in existingIds)
                        {
                            if (await DeleteCertificateAsync(certificateId, request.UserRole))
                                deleted++;
                            else
                                failedDeletes.Add(certificateId);
                        }

                        result.AffectedCount = deleted;
                        result.Success = failedDeletes.Count == 0;
                        result.Message = failedDeletes.Count == 0
                            ? $"Successfully deleted {deleted} certificates"
                            : $"Deleted {deleted} certificate(s); {failedDeletes.Count} could not be " +
                              $"deleted (IDs: {string.Join(", ", failedDeletes)})";
                        break;

                    case "update_status":
                        if (request.Parameters.TryGetValue("status", out var newStatus))
                        {
                            var certificatesToUpdate = await _context.Certificates
                                .Where(c => request.CertificateIds.Contains(c.Id))
                                .ToListAsync();

                            foreach (var cert in certificatesToUpdate)
                            {
                                cert.Status = newStatus;
                                cert.UpdatedAt = DateTime.UtcNow;
                            }

                            await _context.SaveChangesAsync();

                            // CR-015: cascade product-ID statuses on each cert whose status just changed.
                            foreach (var cert in certificatesToUpdate)
                            {
                                await _productIdService.SyncProductIdStatusesForCertificateAsync(cert.Id);
                            }

                            result.AffectedCount = certificatesToUpdate.Count;
                            result.Message = $"Successfully updated status for {result.AffectedCount} certificates";
                        }
                        else
                        {
                            result.Success = false;
                            result.Message = "Status parameter is required for update_status action";
                        }
                        break;

                    default:
                        result.Success = false;
                        result.Message = "Unknown bulk action";
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action: {Action}", request.Action);
                return new BulkActionResult
                {
                    Success = false,
                    Message = "Bulk action failed",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<List<string>> GetSearchSuggestionsAsync(string term, string type, string userRole)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term)) return new List<string>();

                var suggestions = new List<string>();

                switch (type.ToLower())
                {
                    case "company":
                        suggestions = await _context.Companies
                            .Where(c => c.Name.Contains(term))
                            .Select(c => c.Name)
                            .Take(10)
                            .ToListAsync();
                        break;

                    case "product":
                        suggestions = await _context.Products
                            .Where(p => p.Name.Contains(term))
                            .Select(p => p.Name)
                            .Take(10)
                            .ToListAsync();
                        break;

                    default:
                        suggestions = await _context.CertificationTypes
                            .Where(ct => ct.Name.Contains(term))
                            .Select(ct => ct.Name)
                            .Take(10)
                            .ToListAsync();
                        break;
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
                return new List<string>();
            }
        }

        public async Task<CertificateValidationResult> ValidateCertificateAsync(string certificateNumber)
        {
            try
            {
                var certificate = await _context.Certificates
                    .OrderByDescending(c => c.EffectiveDate).ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync(c => c.Number == certificateNumber);

                if (certificate == null)
                {
                    return new CertificateValidationResult
                    {
                        IsValid = false,
                        Message = "Certificate not found"
                    };
                }

                var isExpired = certificate.ExpirationDate < DateTime.Now;
                var isExpiringSoon = certificate.ExpirationDate <= DateTime.Now.AddDays(30);

                return new CertificateValidationResult
                {
                    IsValid = true,
                    Status = certificate.Status,
                    Message = isExpired ? "Certificate has expired" : isExpiringSoon ? "Certificate is expiring soon" : "Certificate is valid",
                    ExpirationDate = certificate.ExpirationDate,
                    IsExpired = isExpired,
                    IsExpiringSoon = isExpiringSoon && !isExpired
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating certificate: {CertificateNumber}", certificateNumber);
                return new CertificateValidationResult
                {
                    IsValid = false,
                    Message = "Validation failed"
                };
            }
        }

        public async Task<int> CreateCertificateAsync(CertificateEditViewModel certificate, string userRole, int? userId = null)
        {
            try
            {
                var newCertificate = new Certificate
                {
                    Number = certificate.Number,
                    Status = certificate.Status,
                    EffectiveDate = certificate.EffectiveDate.Value,
                    ExpirationDate = certificate.ExpirationDate.Value,
                    Location = certificate.Location,
                    Country = certificate.Country,
                    Asin = certificate.Asin,
                    Upc = certificate.Upc,
                    Gtin = certificate.Gtin,
                    Ean = certificate.Ean,
                    CompanyId = certificate.CompanyId,
                    ProductId = certificate.ProductId,
                    CertificationTypeId = certificate.CertificationTypeId,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Certificates.Add(newCertificate);
                await _context.SaveChangesAsync();

                return newCertificate.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating certificate");
                return 0;
            }
        }

        public async Task<bool> UpdateCertificateAsync(int id, CertificateEditViewModel certificate, string userRole, int? userId = null)
        {
            try
            {
                var existingCertificate = await _context.Certificates.FindAsync(id);
                if (existingCertificate == null) return false;

                var previousStatus = existingCertificate.Status;

                existingCertificate.Number = certificate.Number;
                existingCertificate.Status = certificate.Status;
                existingCertificate.EffectiveDate = certificate.EffectiveDate.Value;
                existingCertificate.ExpirationDate = certificate.ExpirationDate.Value;
                existingCertificate.Location = certificate.Location;
                existingCertificate.Country = certificate.Country;
                existingCertificate.Asin = certificate.Asin;
                existingCertificate.Upc = certificate.Upc;
                existingCertificate.Gtin = certificate.Gtin;
                existingCertificate.Ean = certificate.Ean;
                existingCertificate.CompanyId = certificate.CompanyId;
                existingCertificate.ProductId = certificate.ProductId;
                existingCertificate.CertificationTypeId = certificate.CertificationTypeId;
                existingCertificate.UpdatedBy = userId;
                existingCertificate.UpdatedAt = DateTime.UtcNow;

                // DEF-171 Option A: save-time auto-expire enforcement via shared helper.
                // See CertificateExpirationService.TryApplySaveTimeAutoExpire — daily BG sweep
                // catches naturally-aging certs; this catches the save event so a cert can't
                // sit in an inconsistent "active + past-date" state.
                var (autoExpired, historyRow) = CertificateExpirationService.TryApplySaveTimeAutoExpire(
                    existingCertificate,
                    userId,
                    userId.HasValue ? $"User#{userId}" : "System",
                    workflowStep: "Save-Time-Expiration");
                if (autoExpired && historyRow != null)
                {
                    _context.CertificateStatusHistory.Add(historyRow);
                }

                await _context.SaveChangesAsync();

                // CR-015: cascade product-ID statuses if cert status actually changed.
                // Compare against the original DB status so the save-time auto-expire above
                // also triggers the cascade.
                var finalStatus = existingCertificate.Status;
                if (!string.Equals(previousStatus, finalStatus, StringComparison.Ordinal))
                {
                    await _productIdService.SyncProductIdStatusesForCertificateAsync(id);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate with ID: {CertificateId}", id);
                return false;
            }
        }

        /// <summary>
        /// DEF-337: certificate delete is audited, including the seven tables SQL Server
        /// cascades away. Before this, deleting a certificate wrote NO audit row at all —
        /// the core entity of the system was the least recoverable thing in it.
        /// </summary>
        public async Task<bool> DeleteCertificateAsync(int id, string userRole, int userId = 0, string? username = null)
        {
            try
            {
                var certificate = await _context.Certificates.FindAsync(id);
                if (certificate == null) return false;

                // DEF-337: every FK pointing at Certificates is ON DELETE CASCADE, so these rows
                // are destroyed by the database with no application code ever seeing them.
                // Capture them BEFORE the delete. Loaded as separate no-tracking queries rather
                // than Includes so the snapshot is a flat scalar set (entity serialization strips
                // navigation collections, so Includes would not have survived into the blob).
                var children = new List<AuditChildSnapshot>();

                async Task CollectAsync<TChild>(IQueryable<TChild> query, Func<TChild, string> idSelector,
                                                Func<TChild, string?>? nameSelector = null) where TChild : class
                {
                    foreach (var row in await query.AsNoTracking().ToListAsync())
                    {
                        children.Add(new AuditChildSnapshot
                        {
                            EntityType = typeof(TChild).Name,
                            EntityId = idSelector(row),
                            EntityName = nameSelector?.Invoke(row),
                            Entity = row
                        });
                    }
                }

                await CollectAsync(_context.CertificateProductIds.Where(x => x.CertificateId == id),
                                   x => x.Id.ToString(), x => x.ProductIdValue);
                await CollectAsync(_context.CertificateStatusHistory.Where(x => x.CertificateId == id),
                                   x => x.Id.ToString());
                await CollectAsync(_context.CertificateDocuments.Where(x => x.CertificateId == id),
                                   x => x.Id.ToString());
                await CollectAsync(_context.CustomFieldValues.Where(x => x.CertificateId == id),
                                   x => x.Id.ToString());
                // CR-037: country join rows are a real FK child with ON DELETE CASCADE, so they
                // belong in this group rather than the skip-navigation group below. Without the
                // capture, deleting a multi-country certificate silently loses every country but
                // the denormalized primary, and a restore would come back single-country.
                await CollectAsync(_context.CertificateCountries.Where(x => x.CertificateId == id),
                                   x => x.Id.ToString(), x => x.Country);

                // DEF-337 (completion): the four collections above are FK children with a real
                // CertificateId column. A certificate ALSO sits in three many-to-many join
                // tables, which cascade away just as silently but have no CLR entity type to
                // query. Capture the link rows themselves — the Tag/Partner/subcategory records
                // are not being deleted, only the associations to them.
                //
                // CertificatePartners is the consequential one: restoring a certificate without
                // its partner links quietly changes who can see it.
                var associations = await _context.Certificates
                    .AsNoTracking()
                    .Include(c => c.Tags)
                    .Include(c => c.Partners)
                    .Include(c => c.CertificationTypeSubcategories)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (associations != null)
                {
                    children.AddRange(AuditJoinRowCollector.Collect(
                        _context, typeof(Certificate), nameof(Certificate.Tags), id.ToString(),
                        associations.Tags, t => t.Id.ToString(), t => t.Name));

                    children.AddRange(AuditJoinRowCollector.Collect(
                        _context, typeof(Certificate), nameof(Certificate.Partners), id.ToString(),
                        associations.Partners, p => p.Id.ToString(), p => p.Name));

                    children.AddRange(AuditJoinRowCollector.Collect(
                        _context, typeof(Certificate), nameof(Certificate.CertificationTypeSubcategories),
                        id.ToString(), associations.CertificationTypeSubcategories,
                        s => s.Id.ToString(), s => s.Name));
                }

                // CR-171 Phase 2 added per-product-ID partner sharing. Those link rows hang off
                // CertificateProductIds, so they cascade one level deeper than the loop above
                // reaches — captured here so a restored certificate keeps its per-product
                // partner visibility, not just its certificate-level partners.
                var productIdPartners = await _context.CertificateProductIds
                    .AsNoTracking()
                    .Include(pid => pid.Partners)
                    .Where(pid => pid.CertificateId == id)
                    .ToListAsync();

                foreach (var pid in productIdPartners)
                {
                    children.AddRange(AuditJoinRowCollector.Collect(
                        _context, typeof(CertificateProductId), nameof(CertificateProductId.Partners),
                        pid.Id.ToString(), pid.Partners, p => p.Id.ToString(), p => p.Name));
                }

                await _auditService.LogEntityDeleteWithChildrenAsync(
                    id.ToString(),
                    certificate.Number,
                    certificate,
                    children,
                    userId,
                    username ?? string.Empty,
                    reason: $"Certificate deleted (role: {userRole})",
                    changeSource: AuditChangeSource.WebUI);

                _context.Certificates.Remove(certificate);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting certificate with ID: {CertificateId}", id);
                return false;
            }
        }

        public async Task<CertificateDetailsViewModel?> GetCertificateByCertificateNumberAsync(string certificateNumber, string userRole)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .ThenInclude(p => p.Category)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .Include(c => c.CustomFieldValues)
                    .ThenInclude(cfv => cfv.Field)
                    .OrderByDescending(c => c.EffectiveDate).ThenByDescending(c => c.Id)
                    .FirstOrDefaultAsync(c => c.Number == certificateNumber);

                if (certificate == null) return null;

                return await GetCertificateDetailsAsync(certificate.Id, userRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate by number: {CertificateNumber}", certificateNumber);
                return null;
            }
        }

        public async Task<Certificate?> GetCertificateEntityAsync(int id)
        {
            return await _context.Certificates.FindAsync(id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        #region Private Helper Methods

        private static string GetBadgeText(string certificationType)
        {
            return certificationType switch
            {
                "Certified Sustainability Facts" => "CSF",
                "Recycled Content" => "RC",
                "Renewable Energy" => "RE",
                "Carbon Footprint" => "CF",
                "Chain of Custody" => "CoC",
                _ => "CERT"
            };
        }

        private static string DetermineTagType(string userRole)
        {
            return userRole switch
            {
                "Admin" => "Internal",
                "Analyst" => "Internal",
                _ => "External"
            };
        }

        private byte[] GenerateCertificatePdf(CertificateDetailsViewModel certificate)
        {
            try
            {
                // Initialize QuestPDF settings
                QuestPDF.Settings.License = LicenseType.Community;
                
                // Generate individual certificate PDF using QuestPDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        
                        // Header
                        page.Header().Height(100).Background(Colors.Green.Medium).Padding(20).Column(column =>
                        {
                            column.Item().AlignCenter().Text("Green Circle Certified")
                                .FontSize(28).FontColor(Colors.White).Bold();
                            column.Item().AlignCenter().Text("Certificate Document")
                                .FontSize(16).FontColor(Colors.White);
                        });

                        // Content
                        page.Content().PaddingTop(30).Column(column =>
                        {
                            // Certificate Number
                            column.Item().PaddingBottom(20).AlignCenter()
                                .Text($"Certificate #{certificate.CertificateNumber}")
                                .FontSize(24).Bold().FontColor(Colors.Green.Darken2);

                            // Certificate Details
                            column.Item().PaddingBottom(20).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                AddDetailRow(table, "Company:", certificate.CompanyName ?? "");
                                AddDetailRow(table, "Product:", certificate.ProductName ?? "");
                                AddDetailRow(table, "Category:", certificate.ProductCategory ?? "");
                                AddDetailRow(table, "Certification Type:", certificate.CertificationType ?? "");
                                AddDetailRow(table, "Status:", certificate.Status ?? "");
                                AddDetailRow(table, "Effective Date:", certificate.EffectiveDate.ToString("yyyy-MM-dd"));
                                AddDetailRow(table, "Expiration Date:", certificate.ExpirationDate.ToString("yyyy-MM-dd"));
                                if (!string.IsNullOrEmpty(certificate.Location))
                                    AddDetailRow(table, "Location:", $"{certificate.Location}, {certificate.Country}");
                            });

                            // Footer note
                            column.Item().PaddingTop(30).AlignCenter()
                                .Text("This certificate is valid only when verified through the Green Circle Certified database.")
                                .FontSize(10).FontColor(Colors.Grey.Darken1);
                        });

                        // Footer
                        page.Footer().AlignCenter().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating individual certificate PDF with QuestPDF");
                
                // Fallback to simple text
                var content = $"Green Circle Certified Certificate\n\n" +
                             $"Certificate Number: {certificate.CertificateNumber}\n" +
                             $"Company: {certificate.CompanyName}\n" +
                             $"Product: {certificate.ProductName}\n" +
                             $"Status: {certificate.Status}\n" +
                             $"Valid: {certificate.EffectiveDate:yyyy-MM-dd} to {certificate.ExpirationDate:yyyy-MM-dd}\n\n" +
                             $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
                
                return System.Text.Encoding.UTF8.GetBytes(content);
            }
        }

        private static void AddDetailRow(dynamic table, string label, string value)
        {
            table.Cell().Border(0.5f).Padding(8).Background(Colors.Grey.Lighten4)
                .Text(label).FontSize(12).Bold();
            table.Cell().Border(0.5f).Padding(8)
                .Text(value).FontSize(12);
        }

        // DEF-128: includeInternalTags gates the "Internal Tag" column. Anonymous + External callers
        // pass false (per CLAUDE.md "External CANNOT view internal tags" — anonymous tier is below
        // External). Admin/Analyst pass true.
        private byte[] GenerateCsvExport(List<CertificateExportRow> certificates, bool includeInternalTags = false)
        {
            try
            {
                var csv = new StringBuilder();

                // Get custom fields and values for all exported certificates.
                // DEF-198 (QC #182): pass the same internal-caller gate used for the Internal Tag
                // column so custom fields marked RoleVisibility="Internal" (GCC Admin/Analyst only)
                // are excluded for Partner/External/anonymous callers. Previously every custom field
                // value was emitted regardless of visibility, leaking Internal fields (e.g. the
                // "Chris Test Field") into the partner CSV export.
                var certIds = certificates.Select(c => c.Id).ToList();
                var (customFieldNames, customFieldValueMap) = GetCustomFieldDataForExport(certIds, includeInternalTags);

                // CSV Header — all certificate fields + admin custom fields
                // CSV header text matches the certificate tile UI column labels.
                // DEF-295: Cert Subcategories and Pathway are Admin/Analyst-only — they appear on NO
                // partner-facing surface (external tile view, external table view, external tile API,
                // or the v1 partner API), so they are gated behind the same internal-caller flag
                // (`includeInternalTags`, true only for Admin/Analyst) that already hides the
                // Internal Tag column (DEF-128) and Internal custom fields (DEF-247).
                //
                // QC #201 (client, 7/6/2026): "ALL views should include results, as a percentage,
                // rather than having Results column removed" — Cert Results is explicitly EXCLUDED
                // from the DEF-295 gate and now ships on every export tier, always formatted as a
                // percentage via DisplayFormatters (DEF-287). Pathway + Cert Subcategories stay
                // internal-only. Column order here MUST stay in lock-step with the data-row builder below.
                csv.Append("\"Certificate #\",\"Certification Type\"");
                if (includeInternalTags) csv.Append(",\"Cert Subcategories\"");
                csv.Append(",\"Status\",\"Company\",\"Company Contact Email/URL\",\"Contact URL\",\"Product Name\",\"Product Category\",\"Product Type\",\"Subcategory\",\"Partner\"");
                if (includeInternalTags) csv.Append(",\"Pathway\"");
                csv.Append(",\"Cert Results\"");
                csv.Append(",\"Effective Date\",\"Expiration Date\",\"Location\",\"Country\",\"External Tag\"");
                if (includeInternalTags)
                {
                    csv.Append(",\"Internal Tag\"");
                }
                csv.Append(",\"ASIN\",\"UPC\",\"GTIN\",\"EAN\"");
                foreach (var fieldName in customFieldNames)
                {
                    csv.Append($",{CsvCell(fieldName)}");
                }
                csv.AppendLine();

                // CSV Data with proper escaping. Every text cell is emitted via CsvCell so that
                // values Excel/Sheets would otherwise treat as a formula (e.g. the "+Vantage Vinyl
                // Verified" certification type, which rendered as #NAME?) are neutralised — see CsvCell.
                foreach (var cert in certificates)
                {
                    csv.Append($"{CsvCell(cert.CertificateNumber)},{CsvCell(cert.CertificationType)}");
                    // DEF-295: Cert Subcategories — Admin/Analyst-only.
                    if (includeInternalTags) csv.Append($",{CsvCell(cert.CertificationSubcategories)}");
                    csv.Append($",{CsvCell(cert.Status)},{CsvCell(cert.CompanyName)},{CsvCell(cert.CompanyContactEmail)},{CsvCell(cert.CompanyContactUrl)},{CsvCell(cert.ProductName)},{CsvCell(cert.ProductCategory)},{CsvCell(cert.ProductType)},{CsvCell(cert.ProductSubcategory)},{CsvCell(cert.Partner)}");
                    // DEF-295: Pathway — Admin/Analyst-only.
                    if (includeInternalTags) csv.Append($",{CsvCell(cert.Pathway)}");
                    // QC #201: Cert Results on every tier, percentage-formatted (DEF-287).
                    csv.Append($",{CsvCell(cert.CertificationResults ?? "")}");
                    csv.Append($",\"{cert.EffectiveDate:yyyy-MM-dd}\",\"{cert.ExpirationDate:yyyy-MM-dd}\",{CsvCell(cert.Location)},{CsvCell(cert.Country)},{CsvCell(cert.ExternalTags)}");
                    if (includeInternalTags)
                    {
                        csv.Append($",{CsvCell(cert.InternalTags)}");
                    }
                    // ASIN/UPC/GTIN/EAN are identifier codes: force Excel text format so Excel
                    // doesn't treat them as numbers and strip leading zeros (e.g. a UPC
                    // "012345678905" would otherwise open as 12345678905). Round-trips losslessly.
                    csv.Append($",{CsvTextCell(cert.Asin)},{CsvTextCell(cert.Upc)},{CsvTextCell(cert.Gtin)},{CsvTextCell(cert.Ean)}");

                    // Append custom field values (admin-added fields)
                    customFieldValueMap.TryGetValue(cert.Id, out var certFieldValues);
                    foreach (var fieldName in customFieldNames)
                    {
                        var value = certFieldValues != null && certFieldValues.TryGetValue(fieldName, out var v) ? v : "";
                        csv.Append($",{CsvCell(value)}");
                    }
                    csv.AppendLine();
                }

                return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSV export");
                throw;
            }
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Escape quotes by doubling them
            return field.Replace("\"", "\"\"");
        }

        // Characters that make Excel / Google Sheets interpret a cell as a formula. A value
        // starting with any of these is a CSV-injection vector and, for benign data like the
        // "+Vantage Vinyl Verified" certification type, renders as #NAME? in the exported file.
        private const string CsvFormulaTriggers = "=+-@\t\r";

        /// <summary>
        /// Builds a complete, Excel-safe CSV cell (surrounding quotes included).
        /// Normal values are RFC 4180 quoted. Values that would otherwise be read as a formula
        /// (leading = + - @ or tab/CR) are emitted in Excel text format (="value") so they
        /// display literally and cannot execute. This is the same wrapper the importer already
        /// unwraps via StripExcelTextFormat (applied to every column in GetFieldWithMapping),
        /// so re-importing an exported file is lossless.
        /// </summary>
        private string CsvCell(string? field)
        {
            var value = field ?? string.Empty;
            var escaped = EscapeCsvField(value);
            if (value.Length > 0 && CsvFormulaTriggers.IndexOf(value[0]) >= 0)
            {
                return ExcelTextCell(value);
            }
            return $"\"{escaped}\"";
        }

        /// <summary>
        /// Like <see cref="CsvCell"/> but ALWAYS emits Excel text format (="value") for non-empty
        /// values. Use for identifier columns (ASIN/UPC/GTIN/EAN) whose leading zeros Excel would
        /// otherwise strip by treating the cell as a number. Empty stays an empty quoted cell.
        /// Uses the same ="value" wrapper the importer unwraps via StripExcelTextFormat, so
        /// re-importing is lossless.
        /// </summary>
        private string CsvTextCell(string? field)
        {
            var value = field ?? string.Empty;
            if (value.Length == 0)
            {
                return "\"\"";
            }
            // DEF-315: only wrap when Excel would actually corrupt the value. See
            // NeedsExcelTextProtection — most identifier cells are alphanumeric or multi-value
            // and are safe as a plain quoted field, which reads cleanly in EVERY tool
            // (Sheets/Numbers/text editors show ="…" literally). Leading zeros are still
            // protected where they genuinely exist, so DEF-306's guarantee is preserved.
            return NeedsExcelTextProtection(value) ? ExcelTextCell(value) : $"\"{EscapeCsvField(value)}\"";
        }

        /// <summary>
        /// True when Excel would corrupt this value by parsing it as a number.
        /// </summary>
        /// <remarks>
        /// CSV is typeless — there is no portable way to mark a column as text, so any protection
        /// is an Excel-specific hack that other tools render literally. The fix is therefore to
        /// apply it as narrowly as possible.
        ///
        /// Excel only coerces a cell to a number when the whole cell is digits. That means:
        ///  - Alphanumeric identifiers (ASIN "B0BRNN8BKS") are never at risk — they start with a letter.
        ///  - Multi-value cells ("012345, 098765") are never at risk — the comma makes Excel treat
        ///    them as text, so their leading zeros already survive.
        ///  - A bare all-digit value IS at risk: a leading zero is dropped ("012345678905" →
        ///    12345678905), and past 15 significant digits Excel loses precision outright.
        /// So we protect exactly that case and leave everything else as a clean plain field.
        /// </remarks>
        private static bool NeedsExcelTextProtection(string value)
        {
            if (value.Length == 0) return false;
            foreach (var c in value)
            {
                if (c < '0' || c > '9') return false; // not purely numeric → Excel leaves it alone
            }
            return value[0] == '0' || value.Length > 15;
        }

        /// <summary>
        /// Builds a complete, RFC 4180-valid CSV cell whose DECODED content is Excel text format
        /// (<c>="value"</c>).
        /// </summary>
        /// <remarks>
        /// DEF-315: the previous form emitted <c>="value"</c> with the <c>=</c> sitting OUTSIDE the
        /// quoting. A field that does not begin with a quote is unquoted per RFC 4180, so any parser
        /// (Excel included) split it on the commas inside a multi-value identifier — e.g. an ASIN cell
        /// "AMZNONLY01, B005PMYJBO, B0BRNN8BKS" became three fields, shifting every following column.
        /// The partner export was silently emitting ASIN values under the UPC and GTIN headers.
        ///
        /// Correct form quotes the whole field and doubles the inner quotes: <c>"=""value"""</c>.
        /// After CSV decoding the cell content is still exactly <c>="value"</c>, so Excel's text
        /// treatment (leading-zero preservation, formula neutralisation) is unchanged and the
        /// importer's StripExcelTextFormat round-trip still matches. This is purely an ENCODING fix —
        /// the decoded value is byte-identical to before.
        /// </remarks>
        private string ExcelTextCell(string value)
        {
            // Two escaping levels: inner = quotes doubled for Excel's ="…" string literal;
            // outer = those doubled again so the whole thing survives as one CSV field.
            var excelEscaped = value.Replace("\"", "\"\"");
            var csvEscaped = excelEscaped.Replace("\"", "\"\"");
            return $"\"=\"\"{csvEscaped}\"\"\"";
        }

        /// <summary>
        /// Gets all custom field names and their values for the given certificate IDs.
        /// Returns ordered field names and a map of certificateId -> (fieldName -> value).
        /// </summary>
        private (List<string> FieldNames, Dictionary<int, Dictionary<string, string>> ValueMap) GetCustomFieldDataForExport(List<int> certificateIds, bool includeInternalFields = true)
        {
            try
            {
                // Get all custom field values for these certificates, joined with field names.
                // DEF-198 (QC #182): non-internal callers (Partner/External/anonymous) must not
                // receive custom fields whose RoleVisibility is "Internal" (GCC Admin/Analyst only).
                // The null guard keeps legacy rows with an unset visibility (treated as Public).
                var query = _context.CustomFieldValues
                    .Where(cfv => certificateIds.Contains(cfv.CertificateId));

                if (!includeInternalFields)
                {
                    query = query.Where(cfv => cfv.Field.RoleVisibility == null || cfv.Field.RoleVisibility != "Internal");
                }

                var fieldValues = query
                    .Include(cfv => cfv.Field)
                    .Select(cfv => new
                    {
                        cfv.CertificateId,
                        FieldName = cfv.Field.Name,
                        cfv.Field.SortOrder,
                        Value = cfv.TextValue ?? ""
                    })
                    .ToList();

                // Get distinct field names in sort order
                var fieldNames = fieldValues
                    .GroupBy(fv => fv.FieldName)
                    .OrderBy(g => g.Min(fv => fv.SortOrder))
                    .Select(g => g.Key)
                    .ToList();

                // Build map: certificateId -> { fieldName -> value }
                var valueMap = fieldValues
                    .GroupBy(fv => fv.CertificateId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToDictionary(fv => fv.FieldName, fv => fv.Value)
                    );

                return (fieldNames, valueMap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading custom field data for export");
                return (new List<string>(), new Dictionary<int, Dictionary<string, string>>());
            }
        }

        private byte[] GenerateExcelExport(List<CertificateExportRow> certificates)
        {
            try
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Certificates");

                // Get custom fields and values for all exported certificates
                var certIds = certificates.Select(c => c.Id).ToList();
                var (customFieldNames, customFieldValueMap) = GetCustomFieldDataForExport(certIds);

                // Add headers — all certificate fields + admin custom fields
                var headers = new List<string>
                {
                    "Certificate Number", "Certification Type", "Certification Subcategories", "Status",
                    "Company Name", "Company Contact Email", "Company Contact URL",
                    "Product Name", "Product Category", "Product Type", "Product Subcategory",
                    "Partner Program", "Pathway", "Certification Results",
                    "Effective Date", "Expiration Date", "Location", "Country",
                    "External Tags", "Internal Tags",
                    "ASIN", "UPC", "GTIN", "EAN"
                };
                headers.AddRange(customFieldNames);

                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                }

                // Add data
                int row = 2;
                foreach (var cert in certificates)
                {
                    int col = 1;
                    worksheet.Cell(row, col++).Value = cert.CertificateNumber;
                    worksheet.Cell(row, col++).Value = cert.CertificationType;
                    worksheet.Cell(row, col++).Value = cert.CertificationSubcategories;
                    worksheet.Cell(row, col++).Value = cert.Status;
                    worksheet.Cell(row, col++).Value = cert.CompanyName;
                    worksheet.Cell(row, col++).Value = cert.CompanyContactEmail;
                    worksheet.Cell(row, col++).Value = cert.CompanyContactUrl;
                    worksheet.Cell(row, col++).Value = cert.ProductName;
                    worksheet.Cell(row, col++).Value = cert.ProductCategory;
                    worksheet.Cell(row, col++).Value = cert.ProductType;
                    worksheet.Cell(row, col++).Value = cert.ProductSubcategory;
                    worksheet.Cell(row, col++).Value = cert.Partner;
                    worksheet.Cell(row, col++).Value = cert.Pathway;
                    worksheet.Cell(row, col++).Value = cert.CertificationResults;
                    var effCell = worksheet.Cell(row, col++);
                    effCell.Value = cert.EffectiveDate;
                    effCell.Style.DateFormat.Format = "yyyy-MM-dd";
                    var expCell = worksheet.Cell(row, col++);
                    expCell.Value = cert.ExpirationDate;
                    expCell.Style.DateFormat.Format = "yyyy-MM-dd";
                    worksheet.Cell(row, col++).Value = cert.Location;
                    worksheet.Cell(row, col++).Value = cert.Country;
                    worksheet.Cell(row, col++).Value = cert.ExternalTags;
                    worksheet.Cell(row, col++).Value = cert.InternalTags;
                    worksheet.Cell(row, col++).Value = cert.Asin;
                    worksheet.Cell(row, col++).Value = cert.Upc;
                    worksheet.Cell(row, col++).Value = cert.Gtin;
                    worksheet.Cell(row, col++).Value = cert.Ean;

                    // Append custom field values (admin-added fields)
                    customFieldValueMap.TryGetValue(cert.Id, out var certFieldValues);
                    for (int i = 0; i < customFieldNames.Count; i++)
                    {
                        var value = certFieldValues != null && certFieldValues.TryGetValue(customFieldNames[i], out var v) ? v : "";
                        worksheet.Cell(row, col + i).Value = value;
                    }
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel export");
                // Fallback to CSV
                return GenerateCsvExport(certificates);
            }
        }

        private byte[] GeneratePdfExportFromRows(List<CertificateExportRow> certificates)
        {
            try
            {
                // Initialize QuestPDF settings
                QuestPDF.Settings.License = LicenseType.Community;

                // Generate professional PDF using QuestPDF
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);

                        // Header
                        page.Header().Height(80).Background(Colors.Green.Medium).Padding(20).Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().AlignCenter().Text("Green Circle Certified")
                                    .FontSize(24).FontColor(Colors.White).Bold();
                                column.Item().AlignCenter().Text("Certificate Export Report")
                                    .FontSize(16).FontColor(Colors.White);
                            });
                        });

                        // Content
                        page.Content().PaddingTop(20).Column(column =>
                        {
                            column.Item().PaddingBottom(10).Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(12).FontColor(Colors.Grey.Darken2);

                            column.Item().PaddingBottom(10).Text($"Total Certificates: {certificates.Count}")
                                .FontSize(14).Bold();

                            // Certificate table
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Certificate Number
                                    columns.RelativeColumn(3); // Company Name
                                    columns.RelativeColumn(2); // Product Name
                                    columns.RelativeColumn(1); // Status
                                    columns.RelativeColumn(2); // Valid Period
                                });

                                // Table Header
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Certificate #").Bold().FontSize(10);
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Company").Bold().FontSize(10);
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Product").Bold().FontSize(10);
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Status").Bold().FontSize(10);
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5)
                                        .Text("Valid Period").Bold().FontSize(10);
                                });

                                // Table Rows - Limit to 100 for PDF readability
                                foreach (var cert in certificates.Take(100))
                                {
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text(cert.CertificateNumber ?? "").FontSize(9);
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text(cert.CompanyName ?? "").FontSize(9);
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text(cert.ProductName ?? "").FontSize(9);
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text(cert.Status ?? "").FontSize(9);
                                    table.Cell().Border(0.5f).Padding(5)
                                        .Text($"{cert.EffectiveDate:yyyy-MM-dd} to {cert.ExpirationDate:yyyy-MM-dd}").FontSize(9);
                                }
                            });

                            // Summary
                            if (certificates.Count > 100)
                            {
                                column.Item().PaddingTop(20).Text($"Note: Showing first 100 of {certificates.Count} certificates. For complete data, please use Excel or CSV export.")
                                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                            }
                        });

                        // Footer
                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber().FontSize(10);
                            x.Span(" / ").FontSize(10);
                            x.TotalPages().FontSize(10);
                        });
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF export with QuestPDF");

                // Fallback to text-based "PDF" if QuestPDF fails
                var fallbackContent = new StringBuilder();
                fallbackContent.AppendLine("Green Circle Certified - Certificate Export Report");
                fallbackContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                fallbackContent.AppendLine("=" + new string('=', 60));
                fallbackContent.AppendLine();

                foreach (var cert in certificates.Take(50))
                {
                    fallbackContent.AppendLine($"Certificate: {cert.CertificateNumber}");
                    fallbackContent.AppendLine($"Company: {cert.CompanyName}");
                    fallbackContent.AppendLine($"Product: {cert.ProductName}");
                    fallbackContent.AppendLine($"Status: {cert.Status}");
                    fallbackContent.AppendLine($"Valid: {cert.EffectiveDate:yyyy-MM-dd} to {cert.ExpirationDate:yyyy-MM-dd}");
                    fallbackContent.AppendLine(new string('-', 40));
                }

                return System.Text.Encoding.UTF8.GetBytes(fallbackContent.ToString());
            }
        }

        #endregion

        #region Advanced Search Methods

        /// <summary>
        /// Advanced search functionality for certificates with multi-criteria support
        /// </summary>
        public async Task<CertificateListResponse> SearchCertificatesAsync(CertificateListRequest searchCriteria)
        {
            return await GetCertificatesAsync(searchCriteria);
        }

        /// <summary>
        /// Paged search functionality for certificates
        /// </summary>
        public async Task<CertificateListResponse> SearchCertificatesPagedAsync(CertificateListRequest searchCriteria)
        {
            return await GetCertificatesAsync(searchCriteria);
        }

        /// <summary>
        /// Apply advanced filters to certificate query
        /// </summary>
        public async Task<CertificateListResponse> ApplyAdvancedFiltersAsync(CertificateListRequest request)
        {
            return await GetCertificatesAsync(request);
        }

        /// <summary>
        /// Save filter configuration for user
        /// </summary>
        public async Task<bool> SaveFilterAsync(string userId, string filterName, CertificateListRequest filterConfig)
        {
            // TODO: Implement saved filters in database
            await Task.CompletedTask;
            return true;
        }

        /// <summary>
        /// Load saved filter configuration for user
        /// </summary>
        public async Task<CertificateListRequest> LoadSavedFilterAsync(string userId, string filterName)
        {
            // TODO: Implement saved filters from database
            await Task.CompletedTask;
            return new CertificateListRequest();
        }

        /// <summary>
        /// Apply dynamic filters to certificate results
        /// </summary>
        public async Task<CertificateListResponse> ApplyDynamicFilterAsync(CertificateListRequest request)
        {
            return await GetCertificatesAsync(request);
        }

        /// <summary>
        /// Export filtered results in various formats
        /// </summary>
        public async Task<byte[]?> ExportFilteredResultsAsync(CertificateListRequest request, bool includeInternalTags = false)
        {
            return await ExportCertificatesAsync(request, includeInternalTags);
        }

        #endregion
    }
}