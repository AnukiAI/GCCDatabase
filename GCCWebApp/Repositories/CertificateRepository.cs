using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
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
                .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && c.ExpirationDate > DateTime.UtcNow)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetExpiringCertificatesAsync(DateTime beforeDate)
        {
            return await _dbSet
                .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && c.ExpirationDate <= beforeDate && c.ExpirationDate > DateTime.UtcNow)
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
                query = query.Where(c => c.ExpirationDate == null || c.ExpirationDate >= DateTime.UtcNow);

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

            // New filter properties for API
            if (filter.CertificationTypeNames != null && filter.CertificationTypeNames.Any())
                query = query.Where(c => c.CertificationType != null && filter.CertificationTypeNames.Contains(c.CertificationType.Name));

            if (filter.Countries != null && filter.Countries.Any())
                query = query.Where(c => c.Country != null && filter.Countries.Contains(c.Country));

            if (filter.CompanyIds != null && filter.CompanyIds.Any())
                query = query.Where(c => filter.CompanyIds.Contains(c.CompanyId));

            // Filter by product categories/types/subcategories (matches all 3 hierarchy levels)
            // Checks both Product's hierarchy and Certificate's direct fields
            if (filter.ProductCategories != null && filter.ProductCategories.Any())
                query = query.Where(c =>
                    // Product's hierarchy
                    filter.ProductCategories.Contains(c.Product.Category.Name) ||
                    (c.Product.ProductType != null && filter.ProductCategories.Contains(c.Product.ProductType.Name)) ||
                    (c.Product.ProductSubcategory != null && filter.ProductCategories.Contains(c.Product.ProductSubcategory.Name)) ||
                    // Certificate's direct fields
                    (c.ProductCategory != null && filter.ProductCategories.Contains(c.ProductCategory.Name)) ||
                    (c.ProductType != null && filter.ProductCategories.Contains(c.ProductType.Name)) ||
                    (c.ProductSubcategory != null && filter.ProductCategories.Contains(c.ProductSubcategory.Name)));

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "number" => filter.SortDescending ? query.OrderByDescending(c => c.Number) : query.OrderBy(c => c.Number),
                "company" => filter.SortDescending ? query.OrderByDescending(c => c.Company.Name) : query.OrderBy(c => c.Company.Name),
                "product" => filter.SortDescending ? query.OrderByDescending(c => c.Product.Name) : query.OrderBy(c => c.Product.Name),
                "certtype" => filter.SortDescending ? query.OrderByDescending(c => c.CertificationType.Name) : query.OrderBy(c => c.CertificationType.Name),
                "location" => filter.SortDescending ? query.OrderByDescending(c => c.Location) : query.OrderBy(c => c.Location),
                "country" => filter.SortDescending ? query.OrderByDescending(c => c.Country) : query.OrderBy(c => c.Country),
                "producttype" => filter.SortDescending
                    ? query.OrderByDescending(c => c.Product.ProductType != null ? c.Product.ProductType.Name : "")
                    : query.OrderBy(c => c.Product.ProductType != null ? c.Product.ProductType.Name : ""),
                "effectivedate" => filter.SortDescending ? query.OrderByDescending(c => c.EffectiveDate) : query.OrderBy(c => c.EffectiveDate),
                "expirationdate" => filter.SortDescending ? query.OrderByDescending(c => c.ExpirationDate) : query.OrderBy(c => c.ExpirationDate),
                "status" => filter.SortDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
                _ => filter.SortDescending ? query.OrderByDescending(c => c.Number) : query.OrderBy(c => c.Number)
            };

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
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (certificates, totalCount);
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesByStatusAsync(string status)
        {
            return await _dbSet
                .Where(c => c.Status == status)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetCertificatesExpiringInDaysAsync(int days)
        {
            var targetDate = DateTime.UtcNow.AddDays(days);
            return await _dbSet
                .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && c.ExpirationDate <= targetDate && c.ExpirationDate > DateTime.UtcNow)
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
            return await _dbSet
                .Where(c => c.EffectiveDate >= startDate && c.EffectiveDate <= endDate)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .OrderBy(c => c.EffectiveDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetCertificateStatisticsAsync()
        {
            var stats = new Dictionary<string, int>();
            var now = DateTime.UtcNow;

            // Total certificates
            stats["Total"] = await _dbSet.CountAsync();

            // Active certificates (publicly visible: Client Approved - Paid or Republished, not expired)
            var publicStatuses = CertificateStatusHelper.PublicStatuses;
            stats["Active"] = await _dbSet.CountAsync(c =>
                publicStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= now));

            // Expired certificates (status is Expired or expiration date has passed)
            stats["Expired"] = await _dbSet.CountAsync(c =>
                c.Status == CertificateStatusHelper.StatusExpired ||
                c.Status == CertificateStatusHelper.StatusExpiredNoAnnualVerification ||
                (c.ExpirationDate != null && c.ExpirationDate < now));

            // Products with active certificates
            stats["ActiveProducts"] = await _dbSet
                .Where(c => publicStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= now))
                .Select(c => c.Product.Id)
                .Distinct()
                .CountAsync();

            // Time-based statistics
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);
            var startOfNextMonth = startOfMonth.AddMonths(1);
            var endOfNextMonth = startOfNextMonth.AddMonths(1).AddDays(-1);
            var startOfYear = new DateTime(now.Year, 1, 1);
            var startOfLastYear = new DateTime(now.Year - 1, 1, 1);
            var endOfLastYear = new DateTime(now.Year - 1, 12, 31);

            // Published last month
            stats["PublishedLastMonth"] = await _dbSet.CountAsync(c => 
                c.CreatedAt >= startOfLastMonth && c.CreatedAt <= endOfLastMonth);

            // Expiring this month
            stats["ExpiringThisMonth"] = await _dbSet.CountAsync(c =>
                publicStatuses.Contains(c.Status) &&
                c.ExpirationDate >= startOfMonth &&
                c.ExpirationDate < startOfNextMonth);

            // Expired last month
            stats["ExpiredLastMonth"] = await _dbSet.CountAsync(c =>
                c.ExpirationDate >= startOfLastMonth &&
                c.ExpirationDate <= endOfLastMonth);

            // Expiring next month
            stats["ExpiringNextMonth"] = await _dbSet.CountAsync(c =>
                publicStatuses.Contains(c.Status) &&
                c.ExpirationDate >= startOfNextMonth &&
                c.ExpirationDate <= endOfNextMonth);

            // Published this year
            stats["PublishedThisYear"] = await _dbSet.CountAsync(c => 
                c.CreatedAt >= startOfYear);

            // Published last year
            stats["PublishedLastYear"] = await _dbSet.CountAsync(c => 
                c.CreatedAt >= startOfLastYear && c.CreatedAt <= endOfLastYear);

            // By status
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
                .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && c.CertificationType != null)
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
                .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && c.Company != null)
                .GroupBy(c => c.Company.Name)
                .OrderByDescending(g => g.Count())
                .Take(topCount)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            return results.Select(x => (x.Name, x.Count));
        }

        public async Task<IEnumerable<(string Name, int Count)>> GetProductTypeStatisticsAsync(int topCount = 10)
        {
            var results = await _dbSet
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductType)
                .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && c.Product != null && c.Product.ProductType != null)
                .GroupBy(c => c.Product!.ProductType!.Name)
                .OrderByDescending(g => g.Count())
                .Take(topCount)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            return results.Select(x => (x.Name, x.Count));
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
    }
}