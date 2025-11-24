using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Repositories
{
    public class CertificateRepository : Repository<Certificate>, ICertificateRepository
    {
        public CertificateRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Certificate>> GetActiveCertificatesAsync()
        {
            return await _dbSet
                .Where(c => c.Status == "active" && c.ExpirationDate > DateTime.UtcNow)
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .ToListAsync();
        }

        public async Task<IEnumerable<Certificate>> GetExpiringCertificatesAsync(DateTime beforeDate)
        {
            return await _dbSet
                .Where(c => c.Status == "active" && c.ExpirationDate <= beforeDate && c.ExpirationDate > DateTime.UtcNow)
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
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .Include(c => c.Tags)
                .Include(c => c.CustomFieldValues)
                    .ThenInclude(cfv => cfv.Field)
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
                           (c.Country != null && c.Country.ToLower().Contains(lowerSearchTerm)))
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
                                        c.Product.Name.ToLower().Contains(lowerSearchTerm));
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
                .Include(c => c.Tags)
                .Include(c => c.CustomFieldValues)
                    .ThenInclude(cfv => cfv.Field)
                .Include(c => c.CreatedByUser)
                .Include(c => c.UpdatedByUser)
                .FirstOrDefaultAsync();
        }

        public async Task<(IEnumerable<Certificate> certificates, int totalCount)> GetAdvancedFilteredCertificatesAsync(
            CertificateSearchFilter filter)
        {
            var query = _dbSet.AsQueryable();

            // Apply search term filter
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var lowerSearchTerm = filter.SearchTerm.ToLower();
                query = query.Where(c => c.Number.ToLower().Contains(lowerSearchTerm) ||
                                        c.Company.Name.ToLower().Contains(lowerSearchTerm) ||
                                        c.Product.Name.ToLower().Contains(lowerSearchTerm) ||
                                        c.CertificationType.Name.ToLower().Contains(lowerSearchTerm) ||
                                        (c.Location != null && c.Location.ToLower().Contains(lowerSearchTerm)) ||
                                        (c.Country != null && c.Country.ToLower().Contains(lowerSearchTerm)));
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

            // New filter properties for API
            if (filter.CertificationTypeNames != null && filter.CertificationTypeNames.Any())
                query = query.Where(c => c.CertificationType != null && filter.CertificationTypeNames.Contains(c.CertificationType.Name));

            if (filter.Countries != null && filter.Countries.Any())
                query = query.Where(c => c.Country != null && filter.Countries.Contains(c.Country));

            if (filter.CompanyIds != null && filter.CompanyIds.Any())
                query = query.Where(c => filter.CompanyIds.Contains(c.CompanyId));

            var totalCount = await query.CountAsync();

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "number" => filter.SortDescending ? query.OrderByDescending(c => c.Number) : query.OrderBy(c => c.Number),
                "company" => filter.SortDescending ? query.OrderByDescending(c => c.Company.Name) : query.OrderBy(c => c.Company.Name),
                "product" => filter.SortDescending ? query.OrderByDescending(c => c.Product.Name) : query.OrderBy(c => c.Product.Name),
                "effectivedate" => filter.SortDescending ? query.OrderByDescending(c => c.EffectiveDate) : query.OrderBy(c => c.EffectiveDate),
                "expirationdate" => filter.SortDescending ? query.OrderByDescending(c => c.ExpirationDate) : query.OrderBy(c => c.ExpirationDate),
                "status" => filter.SortDescending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
                _ => filter.SortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
            };

            var certificates = await query
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .Include(c => c.Tags)
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
                .Where(c => c.Status == "active" && c.ExpirationDate <= targetDate && c.ExpirationDate > DateTime.UtcNow)
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

            // Active certificates
            stats["Active"] = await _dbSet.CountAsync(c => c.Status == "active");

            // Expired certificates
            stats["Expired"] = await _dbSet.CountAsync(c => c.ExpirationDate < now);

            // Expiring soon (within 30 days)
            var thirtyDaysFromNow = now.AddDays(30);
            stats["ExpiringSoon"] = await _dbSet.CountAsync(c => 
                c.Status == "active" && 
                c.ExpirationDate > now && 
                c.ExpirationDate <= thirtyDaysFromNow);

            // Products with active certificates
            stats["ActiveProducts"] = await _dbSet
                .Where(c => c.Status == "active")
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
                c.Status == "active" && 
                c.ExpirationDate >= startOfMonth && 
                c.ExpirationDate < startOfNextMonth);

            // Expired last month
            stats["ExpiredLastMonth"] = await _dbSet.CountAsync(c => 
                c.ExpirationDate >= startOfLastMonth && 
                c.ExpirationDate <= endOfLastMonth);

            // Expiring next month
            stats["ExpiringNextMonth"] = await _dbSet.CountAsync(c => 
                c.Status == "active" && 
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
                .Where(c => c.Status == "active" && c.CertificationType != null)
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
                .Where(c => c.Status == "active" && c.Company != null)
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
                .Where(c => c.Status == "active" && c.Product != null && c.Product.ProductType != null)
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
    }
}