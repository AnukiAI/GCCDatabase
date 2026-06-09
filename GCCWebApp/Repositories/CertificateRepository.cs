using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Extensions;
using GCCWebApp.Models;
using System.Text.RegularExpressions;

namespace GCCWebApp.Repositories
{
    public class CertificateRepository : Repository<Certificate>, ICertificateRepository
    {
        public CertificateRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Normalizes a string by removing hyphens, spaces, and special characters for flexible matching.
        /// E.g., "Len-Tex" -> "lentex", "3-Form" -> "3form"
        /// </summary>
        private static string NormalizeForSearch(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Remove hyphens, spaces, underscores and convert to lowercase
            return Regex.Replace(input.ToLower(), @"[-\s_]", "");
        }

        /// <summary>
        /// Generates search term variations for flexible matching.
        /// Returns the original term plus a normalized version (no hyphens/spaces).
        /// </summary>
        private static List<string> GetSearchTermVariations(string searchTerm)
        {
            var variations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lower = searchTerm.ToLower().Trim();

            // Add original term
            variations.Add(lower);

            // Add version without hyphens/spaces
            var normalized = NormalizeForSearch(lower);
            if (!string.IsNullOrEmpty(normalized) && normalized != lower)
                variations.Add(normalized);

            // If original has no hyphens, try adding hyphen after each capital letter position
            // This helps "LenTex" match "Len-Tex"
            if (!lower.Contains('-'))
            {
                // Try inserting hyphens at common positions (before capital letters in original)
                var withHyphens = Regex.Replace(searchTerm, @"(?<=[a-z])(?=[A-Z])", "-").ToLower();
                if (withHyphens != lower)
                    variations.Add(withHyphens);
            }

            return variations.ToList();
        }

        public async Task<IEnumerable<Certificate>> GetActiveCertificatesAsync()
        {
            return await _dbSet
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= CertificateStatusHelper.BusinessToday))
                .Include(c => c.Company)
                .Include(c => c.Product).ThenInclude(p => p!.Category)
                .Include(c => c.Product).ThenInclude(p => p!.ProductType)
                .Include(c => c.Product).ThenInclude(p => p!.ProductSubcategory)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        /// <summary>
        /// QC #141: every cert across every status with the full taxonomy nav properties loaded.
        /// Used by the dashboard "Certificates by Status" export so legacy literal statuses
        /// (e.g. "Active" used by KAO certs) are not silently dropped.
        /// </summary>
        public async Task<IEnumerable<Certificate>> GetAllCertificatesWithDetailsAsync()
        {
            return await _dbSet
                .Include(c => c.Company)
                .Include(c => c.Product).ThenInclude(p => p!.Category)
                .Include(c => c.Product).ThenInclude(p => p!.ProductType)
                .Include(c => c.Product).ThenInclude(p => p!.ProductSubcategory)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.CertificationType)
                .OrderBy(c => c.Status)
                .ThenBy(c => c.Number)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetExpiringCertificatesAsync(DateTime beforeDate)
        {
            return await _dbSet
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ExpirationDate <= beforeDate && c.ExpirationDate >= CertificateStatusHelper.BusinessToday)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByCompanyAsync(int companyId)
        {
            return await _dbSet
                .Where(c => c.CompanyId == companyId)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByProductAsync(int productId)
        {
            return await _dbSet
                .Where(c => c.ProductId == productId)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByTypeAsync(int certificateTypeId)
        {
            return await _dbSet
                .Where(c => c.CertificationTypeId == certificateTypeId)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<Certificate?> GetCertificateByNumberAsync(string certificateNumber)
        {
            return await _dbSet
                .Where(c => c.Number == certificateNumber)
                .Include(c => c.Company)
                .Include(c => c.Product).ThenInclude(p => p.Category)
                .Include(c => c.Product).ThenInclude(p => p.ProductType)
                .Include(c => c.Product).ThenInclude(p => p.ProductSubcategory)
                .Include(c => c.CertificationType)
                .Include(c => c.CertificationTypeSubcategories)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.Tags)
                .Include(c => c.Partners)
                .Include(c => c.CustomFieldValues)
                    .ThenInclude(cfv => cfv.Field)
                .Include(c => c.ProductIds)
                .Include(c => c.Pathway)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Certificate>> SearchCertificatesAsync(string searchTerm)
        {
            var lowerSearchTerm = searchTerm.ToLower();
            
            // Include navigation properties first to prevent N+1 queries
            return await _dbSet
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .Where(c => c.Number.ToLower().Contains(lowerSearchTerm) ||
                           c.Company.Name.ToLower().Contains(lowerSearchTerm) ||
                           c.Product.Name.ToLower().Contains(lowerSearchTerm) ||
                           (c.Location != null && c.Location.ToLower().Contains(lowerSearchTerm)) ||
                           (c.Country != null && c.Country.ToLower().Contains(lowerSearchTerm)) ||
                           c.Tags.Any(t => t.Name.ToLower().Contains(lowerSearchTerm)))
                .ToListAsync();
        }

        public async Task<(IEnumerable<Certificate> certificates, int totalCount)> GetPagedCertificatesAsync(
            int page, int pageSize, string? searchTerm = null, int? companyId = null, int? productId = null, string? partner = null)
        {
            var query = _dbSet.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(c => c.Number.ToLower().Contains(lowerSearchTerm) ||
                                        c.Company.Name.ToLower().Contains(lowerSearchTerm) ||
                                        c.Product.Name.ToLower().Contains(lowerSearchTerm) ||
                                        c.Tags.Any(t => t.Name.ToLower().Contains(lowerSearchTerm)));
            }

            if (companyId.HasValue)
            {
                query = query.Where(c => c.CompanyId == companyId.Value);
            }

            if (productId.HasValue)
            {
                query = query.Where(c => c.ProductId == productId.Value);
            }

            if (!string.IsNullOrEmpty(partner))
            {
                query = query.Where(c => c.Partner == partner);
            }

            var totalCount = await query.CountAsync();

            var certificates = await query
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (certificates, totalCount);
        }

        public override async Task<Certificate?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Where(c => c.Id == id)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .Include(c => c.CertificationTypeSubcategories)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.Tags)
                .Include(c => c.Partners)
                .Include(c => c.CustomFieldValues)
                    .ThenInclude(cfv => cfv.Field)
                .Include(c => c.CreatedByUser)
                .Include(c => c.UpdatedByUser)
                .Include(c => c.ProductIds)
                // DEF-253: one cert but 5 collection Includes (Subcats, Tags, Partners, CustomFieldValues,
                // ProductIds) cross-multiply into a cartesian — AsSplitQuery loads each collection
                // separately. Single-entity fetch, so no .OrderBy tiebreaker needed.
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }

        public async Task<(IEnumerable<Certificate> certificates, int totalCount)> GetAdvancedFilteredCertificatesAsync(
            CertificateSearchFilter filter)
        {
            var query = _dbSet.AsQueryable();

            // Apply search term filter with flexible matching (handles hyphens, spaces, capitalization)
            // E.g., "LenTex" matches "Len-Tex", "lentex" matches "Len-Tex", etc.
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchVariations = GetSearchTermVariations(filter.SearchTerm);
                var normalizedSearch = NormalizeForSearch(filter.SearchTerm);

                // Build flexible search: check if any variation matches OR if normalized values match
                query = query.Where(c =>
                    // Standard search with variations
                    searchVariations.Any(term =>
                        c.Number.ToLower().Contains(term) ||
                        c.Company.Name.ToLower().Contains(term) ||
                        c.Product.Name.ToLower().Contains(term) ||
                        c.CertificationType.Name.ToLower().Contains(term) ||
                        (c.Location != null && c.Location.ToLower().Contains(term)) ||
                        (c.Country != null && c.Country.ToLower().Contains(term)) ||
                        c.Tags.Any(t => t.Name.ToLower().Contains(term))) ||
                    // Flexible matching: compare normalized values (without hyphens/spaces)
                    c.Company.Name.ToLower().Replace("-", "").Replace(" ", "").Contains(normalizedSearch) ||
                    c.Product.Name.ToLower().Replace("-", "").Replace(" ", "").Contains(normalizedSearch) ||
                    (c.Location != null && c.Location.ToLower().Replace("-", "").Replace(" ", "").Contains(normalizedSearch)));
            }

            // Apply filters
            if (filter.CompanyId.HasValue)
                query = query.Where(c => c.CompanyId == filter.CompanyId.Value);

            if (filter.ProductId.HasValue)
                query = query.Where(c => c.ProductId == filter.ProductId.Value);

            if (filter.CertificationTypeId.HasValue)
                query = query.Where(c => c.CertificationTypeId == filter.CertificationTypeId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(c => c.Status == filter.Status);

            if (filter.Statuses != null && filter.Statuses.Any())
                query = query.Where(c => filter.Statuses.Contains(c.Status));

            if (filter.ExcludeExpiredByDate)
            {
                var businessToday = CertificateStatusHelper.BusinessToday;
                query = query.Where(c => c.ExpirationDate == null || c.ExpirationDate >= businessToday);
            }

            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(c => c.Location != null && c.Location.ToLower().Contains(filter.Location.ToLower()));

            if (!string.IsNullOrEmpty(filter.Country))
                query = query.Where(c => c.Country != null && c.Country.ToLower().Contains(filter.Country.ToLower()));

            if (filter.EffectiveDateFrom.HasValue)
                query = query.Where(c => c.EffectiveDate >= filter.EffectiveDateFrom.Value);

            if (filter.EffectiveDateTo.HasValue)
                query = query.Where(c => c.EffectiveDate <= filter.EffectiveDateTo.Value);

            if (filter.ExpirationDateFrom.HasValue)
                query = query.Where(c => c.ExpirationDate >= filter.ExpirationDateFrom.Value);

            if (filter.ExpirationDateTo.HasValue)
                query = query.Where(c => c.ExpirationDate <= filter.ExpirationDateTo.Value);

            if (filter.TagIds != null && filter.TagIds.Any())
                query = query.Where(c => c.Tags.Any(t => filter.TagIds.Contains(t.Id)));

            // Filter by tag names (used by tile view APIs)
            if (filter.Tags != null && filter.Tags.Any())
                query = query.Where(c => c.Tags.Any(t => filter.Tags.Contains(t.Name)));

            // Filter by additional filters (APR/LEED tags from tile view checkboxes)
            if (filter.AdditionalFilters != null && filter.AdditionalFilters.Any())
                query = query.Where(c => c.Tags.Any(t => filter.AdditionalFilters.Contains(t.Name)));

            // New filter properties for API
            if (filter.CertificationTypeNames != null && filter.CertificationTypeNames.Any())
                query = query.Where(c => c.CertificationType != null && filter.CertificationTypeNames.Contains(c.CertificationType.Name));

            if (filter.Countries != null && filter.Countries.Any())
                query = query.Where(c => c.Country != null && filter.Countries.Contains(c.Country));

            if (filter.CompanyIds != null && filter.CompanyIds.Any())
                query = query.Where(c => filter.CompanyIds.Contains(c.CompanyId));

            // QC #124: public sidebar passes company names (not IDs) via CompanyNames.
            if (filter.CompanyNames != null && filter.CompanyNames.Any())
                query = query.Where(c => c.Company != null && filter.CompanyNames.Contains(c.Company.Name));

            // DEF-192: Match the EFFECTIVE displayed category/type/subcategory at each level
            // (cert-level override if set, otherwise Product-derived). Previous implementation
            // ORed Product-derived AND cert-override across all 3 levels, so a cert whose
            // override hid the Product-derived value would still match on the hidden side and
            // appear in a result list under a category the tile did not display.
            // Display projection lives in CertificateTilesApiController.cs:186-188.
            // Hierarchy: Level 1 = ProductCategory, Level 2 = ProductType (root), Level 3 = ProductType (child).
            // Parent cascade preserved so a Level-2 sidebar pick still surfaces certs tagged at
            // the matching Level-3 child, using the same effective-value rule.
            if (filter.ProductCategories != null && filter.ProductCategories.Any())
                query = query.Where(c =>
                    // Effective Level-1 category
                    (c.ProductCategory != null && filter.ProductCategories.Contains(c.ProductCategory.Name)) ||
                    (c.ProductCategory == null && c.Product != null && filter.ProductCategories.Contains(c.Product.Category.Name)) ||
                    // Effective Level-2/3 ProductType (cert.ProductType is L2 root; cert.ProductSubcategory is L3 child)
                    (c.ProductType != null && filter.ProductCategories.Contains(c.ProductType.Name)) ||
                    (c.ProductType == null && c.Product != null && c.Product.ProductType != null && filter.ProductCategories.Contains(c.Product.ProductType.Name)) ||
                    // Parent of effective L2/L3 ProductType — lets a Level-2 sidebar pick surface its children
                    (c.ProductType != null && c.ProductType.Parent != null && filter.ProductCategories.Contains(c.ProductType.Parent.Name)) ||
                    (c.ProductType == null && c.Product != null && c.Product.ProductType != null && c.Product.ProductType.Parent != null && filter.ProductCategories.Contains(c.Product.ProductType.Parent.Name)) ||
                    // Effective Level-3 subcategory (cert.ProductSubcategory side and Product.ProductSubcategory side reference different tables)
                    (c.ProductSubcategory != null && filter.ProductCategories.Contains(c.ProductSubcategory.Name)) ||
                    (c.ProductSubcategory == null && c.Product != null && c.Product.ProductSubcategory != null && filter.ProductCategories.Contains(c.Product.ProductSubcategory.Name)) ||
                    // Parent of effective cert-side L3 subcategory
                    (c.ProductSubcategory != null && c.ProductSubcategory.Parent != null && filter.ProductCategories.Contains(c.ProductSubcategory.Parent.Name)));

            var totalCount = await query.CountAsync();

            // Apply sorting. Every sort field below is non-unique, so the .ThenBy(c => c.Id)
            // tiebreaker below is required for stable pagination across SQL Server batches
            // (per memory `feedback_ef_includes_with_projection` / DEF-166 history).
            // QC #157: text columns sort empty/blank values LAST regardless of direction, via the
            // shared OrderByTextEmptiesLast helper, so the public tile view matches the internal +
            // external views. Number / dates / Status(enum) have no blank state — kept as-is.
            IOrderedQueryable<Certificate> ordered = filter.SortBy?.ToLower() switch
            {
                "number" => filter.SortDescending ? query.OrderByDescending(c => c.Number) : query.OrderBy(c => c.Number),
                "company" => query.OrderByTextEmptiesLast(c => c.Company.Name, filter.SortDescending),
                "product" => query.OrderByTextEmptiesLast(c => c.Product.Name, filter.SortDescending),
                "certtype" => query.OrderByTextEmptiesLast(c => c.CertificationType.Name, filter.SortDescending),
                "location" => query.OrderByTextEmptiesLast(c => c.Location, filter.SortDescending),
                "country" => query.OrderByTextEmptiesLast(c => c.Country, filter.SortDescending),
                "producttype" => query.OrderByTextEmptiesLast(c => c.Product.ProductType != null ? c.Product.ProductType.Name : "", filter.SortDescending),
                "effectivedate" => filter.SortDescending ? query.OrderByDescending(c => c.EffectiveDate) : query.OrderBy(c => c.EffectiveDate),
                "expirationdate" => filter.SortDescending ? query.OrderByDescending(c => c.ExpirationDate) : query.OrderBy(c => c.ExpirationDate),
                "status" => filter.SortDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
                _ => filter.SortDescending ? query.OrderByDescending(c => c.Number) : query.OrderBy(c => c.Number)
            };
            query = ordered.ThenBy(c => c.Id);

            // DEF-234: AsSplitQuery() prevents cartesian explosion across the two collection
            // Includes (Tags, Partners). Without it, EF emits a single query that joins every
            // row of every collection — for a paged result, this stacks both query-compile and
            // execution-time cost (measured: 23.1s first hit, <200ms subsequent). The internal
            // and external tile API controllers already apply this pattern at their query sites
            // (CertificateTileInternalApiController.cs:96, CertificateTileExternalApiController.cs:85).
            // Per memory `feedback_ef_includes_with_projection` and DEF-166 history.
            //
            // The .ThenBy(c => c.Id) tiebreaker is also required: every sort field above is
            // non-unique (Number, Company.Name, etc.), so without a stable secondary sort the
            // page boundary is non-deterministic across SQL Server batches — same cert can
            // appear on page 1 and page 2, or skip entirely. Also per `feedback_ef_includes_with_projection`.
            var certificates = await query
                .Include(c => c.Company)
                .Include(c => c.Product)
                    .ThenInclude(p => p.Category)
                .Include(c => c.Product.ProductType)
                .Include(c => c.Product.ProductSubcategory)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.CertificationType)
                .Include(c => c.Tags)
                .Include(c => c.Partners)
                .AsSplitQuery()
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (certificates, totalCount);
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByStatusAsync(string status)
        {
            // QC #141: Includes expanded so DashboardController.GenerateCertificateCsv can
            // populate the Product Category / Type / Subcategory columns regardless of which
            // status the cert is in. Prior to this the cert-status export had blank values for
            // those columns even though the data existed.
            return await _dbSet
                .Where(c => c.Status == status)
                .Include(c => c.Company)
                .Include(c => c.Product).ThenInclude(p => p!.Category)
                .Include(c => c.Product).ThenInclude(p => p!.ProductType)
                .Include(c => c.Product).ThenInclude(p => p!.ProductSubcategory)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesExpiringInDaysAsync(int days)
        {
            var businessToday = CertificateStatusHelper.BusinessToday;
            var targetDate = businessToday.AddDays(days);
            return await _dbSet
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ExpirationDate <= targetDate && c.ExpirationDate >= businessToday)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .OrderBy(c => c.ExpirationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByLocationAsync(string location)
        {
            return await _dbSet
                .Where(c => c.Location != null && c.Location.ToLower().Contains(location.ToLower()))
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByCountryAsync(string country)
        {
            return await _dbSet
                .Where(c => c.Country != null && c.Country.ToLower().Contains(country.ToLower()))
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            // QC #141: same Include expansion as GetCertificatesByStatusAsync — the
            // certs-published/expired-last-month exports need the full taxonomy nav properties.
            return await _dbSet
                .Where(c => c.EffectiveDate >= startDate && c.EffectiveDate <= endDate)
                .Include(c => c.Company)
                .Include(c => c.Product).ThenInclude(p => p!.Category)
                .Include(c => c.Product).ThenInclude(p => p!.ProductType)
                .Include(c => c.Product).ThenInclude(p => p!.ProductSubcategory)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.CertificationType)
                .OrderBy(c => c.EffectiveDate)
                .ToListAsync();
        }

        /// <summary>
        /// DEF-200 (QC #180): expired-last-month export must filter on ExpirationDate, not
        /// EffectiveDate. The original code reused GetCertificatesByDateRangeAsync (which
        /// filters by EffectiveDate) so the "Certs Expired Last Month" export returned certs
        /// that became EFFECTIVE last month — a completely different set.
        /// </summary>
        public async Task<IEnumerable<Certificate>> GetCertificatesByExpirationDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(c => c.ExpirationDate >= startDate && c.ExpirationDate <= endDate)
                .Include(c => c.Company)
                .Include(c => c.Product).ThenInclude(p => p!.Category)
                .Include(c => c.Product).ThenInclude(p => p!.ProductType)
                .Include(c => c.Product).ThenInclude(p => p!.ProductSubcategory)
                .Include(c => c.ProductCategory)
                .Include(c => c.ProductType)
                .Include(c => c.ProductSubcategory)
                .Include(c => c.CertificationType)
                .OrderBy(c => c.ExpirationDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetCertificateStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();
            var today = CertificateStatusHelper.BusinessToday;

            // Total certificates
            stats["Total"] = await _dbSet.CountAsync();

            // Active certificates for dashboard reporting (includes Confidential, which is active but not public)
            var dashboardStatuses = CertificateStatusHelper.DashboardStatuses;
            stats["Active"] = await _dbSet.CountAsync(c =>
                dashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= today));

            // Expired certificates - count by STATUS only, not by expiration date
            stats["Expired"] = await _dbSet.CountAsync(c =>
                c.Status == CertificateStatusHelper.StatusExpired ||
                c.Status == CertificateStatusHelper.StatusExpiredNoAnnualVerification);

            // Active cert base filter for distinct counts (dashboard scope includes Confidential)
            var activeCertQuery = _dbSet.Where(c =>
                dashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= today));

            // Distinct companies with active certificates
            stats["CompaniesWithActiveCerts"] = await activeCertQuery
                .Where(c => c.CompanyId > 0)
                .Select(c => c.CompanyId)
                .Distinct()
                .CountAsync();

            // Distinct certification types with active certificates
            stats["CertTypesWithActiveCerts"] = await activeCertQuery
                .Where(c => c.CertificationTypeId != null)
                .Select(c => c.CertificationTypeId)
                .Distinct()
                .CountAsync();

            // Distinct product types with active certificates.
            // DEF-222: cert-direct override wins per CR-014. fromCertDirect picks every
            // cert that has the override set; fromProduct only kicks in when override
            // is null. Otherwise certs with both would be double-counted across two
            // category names (the override and the product-derived).
            var ptFromCertDirect = await activeCertQuery
                .Where(c => c.ProductType != null)
                .Select(c => c.ProductType!.Name)
                .Distinct()
                .ToListAsync();
            var ptFromProduct = await activeCertQuery
                .Where(c => c.ProductType == null && c.Product != null && c.Product.ProductType != null)
                .Select(c => c.Product!.ProductType!.Name)
                .Distinct()
                .ToListAsync();
            stats["ProductTypesWithActiveCerts"] = ptFromCertDirect.Concat(ptFromProduct).Distinct().Count();

            // Distinct product subcategories with active certificates.
            var pscFromCertDirect = await activeCertQuery
                .Where(c => c.ProductSubcategory != null)
                .Select(c => c.ProductSubcategory!.Name)
                .Distinct()
                .ToListAsync();
            var pscFromProduct = await activeCertQuery
                .Where(c => c.ProductSubcategory == null && c.Product != null && c.Product.ProductSubcategory != null)
                .Select(c => c.Product!.ProductSubcategory!.Name)
                .Distinct()
                .ToListAsync();
            stats["ProductSubcategories"] = pscFromCertDirect.Concat(pscFromProduct).Distinct().Count();

            // Distinct product categories with active certificates.
            var pcFromCertDirect = await activeCertQuery
                .Where(c => c.ProductCategory != null)
                .Select(c => c.ProductCategory!.Name)
                .Distinct()
                .ToListAsync();
            var pcFromProduct = await activeCertQuery
                .Where(c => c.ProductCategory == null && c.Product != null && c.Product.Category != null)
                .Select(c => c.Product!.Category!.Name)
                .Distinct()
                .ToListAsync();
            stats["ProductCategoriesWithActiveCerts"] = pcFromCertDirect.Concat(pcFromProduct).Distinct().Count();

            // Products with active certificates
            stats["ActiveProducts"] = await activeCertQuery
                .Where(c => c.ProductId != null)
                .Select(c => c.ProductId)
                .Distinct()
                .CountAsync();

            // Time-based statistics
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);
            var startOfNextMonth = startOfMonth.AddMonths(1);
            var endOfNextMonth = startOfNextMonth.AddMonths(1).AddDays(-1);
            var startOfYear = new DateTime(today.Year, 1, 1);
            var startOfLastYear = new DateTime(today.Year - 1, 1, 1);
            var endOfLastYear = new DateTime(today.Year - 1, 12, 31);

            // Published last month (active certs with EffectiveDate in last month)
            stats["PublishedLastMonth"] = await _dbSet.CountAsync(c =>
                dashboardStatuses.Contains(c.Status) &&
                c.EffectiveDate >= startOfLastMonth && c.EffectiveDate <= endOfLastMonth);

            // Expiring this month
            stats["ExpiringThisMonth"] = await _dbSet.CountAsync(c =>
                dashboardStatuses.Contains(c.Status) &&
                c.ExpirationDate >= startOfMonth &&
                c.ExpirationDate < startOfNextMonth);

            // Expired last month — DEF-220 (QC #180 5/26 follow-up): narrowed to
            // CurrentlyExpiredStatuses. DEF-200's EverPublishedStatuses incorrectly included
            // Republished (a re-active state) and Paid/Confidential (active). A cert with
            // a past ExpirationDate but Status=Republished/Paid is either being actively
            // extended or is a data anomaly — neither belongs in "expired last month."
            var currentlyExpired = CertificateStatusHelper.CurrentlyExpiredStatuses;
            stats["ExpiredLastMonth"] = await _dbSet.CountAsync(c =>
                currentlyExpired.Contains(c.Status) &&
                c.ExpirationDate >= startOfLastMonth &&
                c.ExpirationDate <= endOfLastMonth);

            // Expiring next month
            stats["ExpiringNextMonth"] = await _dbSet.CountAsync(c =>
                dashboardStatuses.Contains(c.Status) &&
                c.ExpirationDate >= startOfNextMonth &&
                c.ExpirationDate <= endOfNextMonth);

            // Published this year (active certs with EffectiveDate in current year up to today).
            // DEF-200 (QC #180): added `EffectiveDate <= today` cap so a future-dated cert
            // (EffectiveDate after today) no longer inflates the "published this year" count
            // ahead of actually being effective. Matches the export side, which already had
            // the cap via GetCertificatesByDateRangeAsync(startOfYear, today).
            stats["PublishedThisYear"] = await _dbSet.CountAsync(c =>
                dashboardStatuses.Contains(c.Status) &&
                c.EffectiveDate >= startOfYear &&
                c.EffectiveDate <= today);

            // Published last year (active certs with EffectiveDate in last year)
            stats["PublishedLastYear"] = await _dbSet.CountAsync(c =>
                dashboardStatuses.Contains(c.Status) &&
                c.EffectiveDate >= startOfLastYear && c.EffectiveDate <= endOfLastYear);

            // By status - all 8 statuses
            var statusCounts = await _dbSet
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => $"Status_{x.Status}", x => x.Count);

            foreach (var statusCount in statusCounts)
            {
                stats[statusCount.Key] = statusCount.Value;
            }

            return stats;
        }

        public async Task<IEnumerable<(string Name, int Count)>> GetCertificationTypeStatisticsAsync()
        {
            var results = await _dbSet
                .Include(c => c.CertificationType)
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.CertificationType != null)
                .GroupBy(c => c.CertificationType.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            return results.Select(x => (x.Name, x.Count));
        }

        public async Task<IEnumerable<(string Name, int Count)>> GetTopCompaniesByActiveCertCountAsync(int topCount = 10)
        {
            var results = await _dbSet
                .Include(c => c.Company)
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.Company != null)
                .GroupBy(c => c.Company.Name)
                .OrderByDescending(g => g.Count())
                .Take(topCount)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            return results.Select(x => (x.Name, x.Count));
        }

        public async Task<IEnumerable<(string Name, int Count)>> GetProductTypeStatisticsAsync(int topCount = 10)
        {
            // DEF-222: cert-direct first, Product-hierarchy fallback. Matches CR-014
            // override semantics + GenerateCertificateCsv resolution order.
            var fromCertDirect = await _dbSet
                .Include(c => c.ProductType)
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ProductType != null)
                .Select(c => new { CertId = c.Id, ProductTypeName = c.ProductType!.Name })
                .ToListAsync();

            var fromProduct = await _dbSet
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductType)
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ProductType == null
                    && c.Product != null && c.Product.ProductType != null)
                .Select(c => new { CertId = c.Id, ProductTypeName = c.Product!.ProductType!.Name })
                .ToListAsync();

            // Combine, deduplicate by CertId (cert-direct already won by ordering),
            // then group by name. DEF-222: cert-direct rows come first in the concat
            // so .Select(g => g.First()) preserves the override when both exist.
            var combined = fromCertDirect.Concat(fromProduct)
                .GroupBy(x => x.CertId)
                .Select(g => g.First())
                .GroupBy(x => x.ProductTypeName)
                .Select(g => (Name: g.Key, Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .Take(topCount)
                .ToList();

            return combined;
        }

        public async Task<IEnumerable<(string Name, int Count)>> GetProductCategoryStatisticsAsync(int topCount = 5)
        {
            // DEF-222 (Shawn, 2026-05-26): cert-direct override wins per CR-014 — must
            // match GenerateCertificateCsv ("cert.ProductCategory ?? cert.Product.Category")
            // and the per-cert detail view. Previously this query used Product-hierarchy
            // FIRST, so cert 26-0158 (cert-direct="Electronics", Product.Category="Carly
            // Test") was counted under "Carly Test" in the breakdown while the export
            // showed it under "Electronics" — 3 distinct categories in the data but only
            // 2 rows in the table. Flipping priority below resolves the divergence.
            var fromCertDirect = await _dbSet
                .Include(c => c.ProductCategory)
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ProductCategory != null)
                .Select(c => new { CertId = c.Id, CategoryName = c.ProductCategory!.Name })
                .ToListAsync();

            var fromProduct = await _dbSet
                .Include(c => c.Product)
                    .ThenInclude(p => p.Category)
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ProductCategory == null
                    && c.Product != null && c.Product.Category != null)
                .Select(c => new { CertId = c.Id, CategoryName = c.Product!.Category!.Name })
                .ToListAsync();

            var combined = fromCertDirect.Concat(fromProduct)
                .GroupBy(x => x.CertId)
                .Select(g => g.First())
                .GroupBy(x => x.CategoryName)
                .Select(g => (Name: g.Key, Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .Take(topCount)
                .ToList();

            return combined;
        }

        public async Task<IEnumerable<(string Name, int Count)>> GetProductSubcategoryStatisticsAsync(int topCount = 10)
        {
            // DEF-222: cert-direct first, Product-hierarchy fallback.
            var fromCertDirect = await _dbSet
                .Include(c => c.ProductSubcategory)
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ProductSubcategory != null)
                .Select(c => new { CertId = c.Id, SubcategoryName = c.ProductSubcategory!.Name })
                .ToListAsync();

            var fromProduct = await _dbSet
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductSubcategory)
                .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ProductSubcategory == null
                    && c.Product != null && c.Product.ProductSubcategory != null)
                .Select(c => new { CertId = c.Id, SubcategoryName = c.Product!.ProductSubcategory!.Name })
                .ToListAsync();

            var combined = fromCertDirect.Concat(fromProduct)
                .GroupBy(x => x.CertId)
                .Select(g => g.First())
                .GroupBy(x => x.SubcategoryName)
                .Select(g => (Name: g.Key, Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .Take(topCount)
                .ToList();

            return combined;
        }

        public async Task<IEnumerable<string>> GetDistinctCountriesAsync()
        {
            var countries = await _dbSet
                .Where(c => c.Country != null && c.Country.Trim() != "")
                .Select(c => c.Country!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
            return countries;
        }

        public new async Task<IEnumerable<Certificate>> FindAsync(System.Linq.Expressions.Expression<Func<Certificate, bool>> predicate)
        {
            return await _dbSet
                .Where(predicate)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        /// <summary>
        /// Generates the next certificate number in format YY-XXXX (e.g., "26-0001")
        /// </summary>
        public async Task<string> GenerateNextCertificateNumberAsync()
        {
            var currentYear = DateTime.Now.Year;
            var yearPrefix = (currentYear % 100).ToString("D2"); // "25", "26", etc.
            var pattern = $"{yearPrefix}-"; // e.g., "26-"

            // Find the highest certificate number for the current year
            var maxNumber = await _dbSet
                .Where(c => c.Number != null && c.Number.StartsWith(pattern))
                .Select(c => c.Number)
                .ToListAsync();

            int nextSequence = 1;

            if (maxNumber.Any())
            {
                // Parse the sequence numbers and find the max
                var sequences = maxNumber
                    .Select(n => {
                        var parts = n.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int seq))
                            return seq;
                        return 0;
                    })
                    .Where(s => s > 0);

                if (sequences.Any())
                {
                    nextSequence = sequences.Max() + 1;
                }
            }

            // Format: YY-XXXX (e.g., "26-0001")
            return $"{yearPrefix}-{nextSequence:D4}";
        }

        // DEF-193: assigns the next available certificate number AT save time and writes
        // the cert in one transaction. On UNIQUE-index conflict (another save won the race),
        // regenerates and retries. Fixes the scenario where two analysts opening the Create
        // form back-to-back both saw the same pre-filled number.
        public async Task<Certificate> AddWithGeneratedNumberAsync(Certificate certificate, int maxAttempts = 3)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                certificate.Number = await GenerateNextCertificateNumberAsync();
                try
                {
                    await _dbSet.AddAsync(certificate);
                    await _context.SaveChangesAsync();
                    return certificate;
                }
                catch (DbUpdateException ex) when (IsUniqueNumberViolation(ex))
                {
                    // Detach so the next attempt re-adds cleanly with the new number
                    _context.Entry(certificate).State = EntityState.Detached;
                    if (attempt == maxAttempts) throw;
                }
            }
            throw new InvalidOperationException("AddWithGeneratedNumberAsync: unreachable");
        }

        private static bool IsUniqueNumberViolation(DbUpdateException ex)
        {
            // SQL Server: 2601 = duplicate key in unique index, 2627 = unique constraint violation
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                return (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                    && sqlEx.Message.IndexOf("IX_Certificates_Number", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }
    }
}