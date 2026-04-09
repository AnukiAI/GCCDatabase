using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GCCWebApp.Models;
using GCCWebApp.Data;

namespace GCCWebApp.Services
{
    public class FilterService : IFilterService
    {
        private readonly ILogger<FilterService> _logger;
        private readonly ApplicationDbContext _context;

        public FilterService(ILogger<FilterService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<List<FilterOption>> GetAvailableFiltersAsync(string userRole)
        {
            var filters = new List<FilterOption>
            {
                new FilterOption { FilterType = "CertificationType", Text = "Certification Types", Count = await _context.CertificationTypes.CountAsync() },
                new FilterOption { FilterType = "ProductCategories", Text = "Product Categories", Count = await _context.ProductCategories.CountAsync() },
                new FilterOption { FilterType = "Companies", Text = "Companies", Count = await _context.Companies.CountAsync() },
                new FilterOption { FilterType = "Countries", Text = "Countries", Count = await _context.Certificates.Where(c => !string.IsNullOrEmpty(c.Country)).Select(c => c.Country).Distinct().CountAsync() },
                new FilterOption { FilterType = "Status", Text = "Status", Count = await _context.Certificates.Select(c => c.Status).Distinct().CountAsync() }
            };

            return filters;
        }

        public async Task<List<FilterOption>> GetFilterOptionsAsync(string filterType, string searchTerm, string userRole)
        {
            var options = new List<FilterOption>();

            try
            {
                switch (filterType.ToLower())
                {
                    case "certificationtype":
                        var query = _context.CertificationTypes.AsQueryable();
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                            query = query.Where(ct => ct.Name.Contains(searchTerm));

                        options = await query
                            .Select(ct => new FilterOption
                            {
                                Value = ct.Id.ToString(),
                                Text = ct.Name,
                                Count = ct.Certificates.Count()
                            })
                            .ToListAsync();
                        break;

                    case "companies":
                        var companyQuery = _context.Companies.AsQueryable();
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                            companyQuery = companyQuery.Where(c => c.Name.Contains(searchTerm));

                        options = await companyQuery
                            .Select(c => new FilterOption
                            {
                                Value = c.Id.ToString(),
                                Text = c.Name,
                                Count = c.Certificates.Count()
                            })
                            .ToListAsync();
                        break;

                    case "countries":
                        var countryQuery = _context.Certificates
                            .Where(c => !string.IsNullOrEmpty(c.Country))
                            .GroupBy(c => c.Country)
                            .AsQueryable();

                        if (!string.IsNullOrWhiteSpace(searchTerm))
                            countryQuery = countryQuery.Where(g => g.Key != null && g.Key.Contains(searchTerm));

                        options = await countryQuery
                            .Select(g => new FilterOption
                            {
                                Value = g.Key ?? string.Empty,
                                Text = g.Key ?? string.Empty,
                                Count = g.Count()
                            })
                            .ToListAsync();
                        break;

                    case "status":
                        options = await _context.Certificates
                            .GroupBy(c => c.Status)
                            .Select(g => new FilterOption
                            {
                                Value = g.Key,
                                Text = g.Key,
                                Count = g.Count()
                            })
                            .ToListAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options for type: {FilterType}", filterType);
            }

            return options;
        }

        public async Task<List<FilterOption>> GetProductCategoriesAsync(string userRole)
        {
            try
            {
                return await _context.ProductCategories
                    .Select(pc => new FilterOption
                    {
                        Value = pc.Id.ToString(),
                        Text = pc.Name,
                        Count = pc.Products.Sum(p => p.Certificates.Count()),
                        Children = pc.ProductTypes.Select(pt => new FilterOption
                        {
                            Value = pt.Id.ToString(),
                            Text = pt.Name,
                            Count = pt.Products.Sum(p => p.Certificates.Count()),
                            Level = 2
                        }).ToList()
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product categories");
                return new List<FilterOption>();
            }
        }

        public async Task<List<FilterOption>> GetCompaniesAsync(string searchTerm, string userRole)
        {
            try
            {
                var query = _context.Companies.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    query = query.Where(c => c.Name.Contains(searchTerm));

                return await query
                    .Select(c => new FilterOption
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Count = c.Certificates.Count()
                    })
                    .OrderBy(c => c.Text)
                    .Take(50) // Limit results for performance
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies");
                return new List<FilterOption>();
            }
        }

        public async Task<List<FilterOption>> GetCountriesAsync(string userRole)
        {
            try
            {
                return await _context.Certificates
                    .Where(c => !string.IsNullOrEmpty(c.Country))
                    .GroupBy(c => c.Country)
                    .Select(g => new FilterOption
                    {
                        Value = g.Key ?? string.Empty,
                        Text = g.Key ?? string.Empty,
                        Count = g.Count()
                    })
                    .OrderBy(f => f.Text)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting countries");
                return new List<FilterOption>();
            }
        }

        public async Task<List<FilterOption>> GetCertificationTypesAsync(string userRole)
        {
            try
            {
                return await _context.CertificationTypes
                    .Select(ct => new FilterOption
                    {
                        Value = ct.Id.ToString(),
                        Text = ct.Name,
                        Count = ct.Certificates.Count()
                    })
                    .OrderBy(f => f.Text)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certification types");
                return new List<FilterOption>();
            }
        }

        public async Task<Dictionary<string, int>> GetFilterCountsAsync(CertificateSearchFilters filters, string userRole)
        {
            try
            {
                // Include navigation properties first to prevent N+1 queries
                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .AsQueryable();

                // Apply current filters to get accurate counts
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var searchTerm = filters.SearchTerm.ToLower();
                    query = query.Where(c =>
                        c.Company.Name.ToLower().Contains(searchTerm) ||
                        c.Product.Name.ToLower().Contains(searchTerm) ||
                        c.Number.ToLower().Contains(searchTerm));
                }

                var counts = new Dictionary<string, int>
                {
                    { "Total", await query.CountAsync() },
                    { "Active", await query.CountAsync(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && c.ExpirationDate >= DateTime.UtcNow.Date) },
                    { "Expired", await query.CountAsync(c => c.Status == CertificateStatusHelper.StatusExpired || c.Status == CertificateStatusHelper.StatusExpiredNoAnnualVerification) },
                    { "Draft/Pending", await query.CountAsync(c => c.Status == CertificateStatusHelper.StatusDraft || c.Status == CertificateStatusHelper.StatusClientApprovedNotPaid) }
                };

                return counts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter counts");
                return new Dictionary<string, int>();
            }
        }
    }
}