using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GCCWebApp.Models;
using GCCWebApp.Data;
using GCCWebApp.Search;

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
                new FilterOption { FilterType = "Countries", Text = "Countries", Count = await _context.CertificateCountries.Where(cc => cc.Country != "").Select(cc => cc.Country).Distinct().CountAsync() }, // CR-037
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
                        {
                            // CR-187: normalized match (case/punctuation/whitespace insensitive).
                            var nq = SearchNormalizer.Normalize(searchTerm);
                            query = query.Where(ct => ct.NormalizedName!.Contains(nq));
                        }

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
                        {
                            // CR-187: normalized match (case/punctuation/whitespace insensitive).
                            var nq = SearchNormalizer.Normalize(searchTerm);
                            companyQuery = companyQuery.Where(c => c.NormalizedName!.Contains(nq));
                        }

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
                        // CR-037: enumerate the JOIN rows, not the primaries. A certificate that
                        // lists three countries contributes to all three counts, so the number beside
                        // each checkbox matches what ticking it actually returns.
                        var countryCerts = _context.CertificateCountries
                            .Where(cc => cc.Country != "");

                        // CR-187: normalize BEFORE grouping (the group key is the raw country string,
                        // which has no computed column). Filter on NormalizedCountry first.
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            var nq = SearchNormalizer.Normalize(searchTerm);
                            countryCerts = countryCerts.Where(cc => cc.NormalizedCountry != null && cc.NormalizedCountry.Contains(nq));
                        }

                        var countryQuery = countryCerts.GroupBy(cc => cc.Country);

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
                {
                    // CR-187: normalized match (case/punctuation/whitespace insensitive).
                    var nq = SearchNormalizer.Normalize(searchTerm);
                    query = query.Where(c => c.NormalizedName!.Contains(nq));
                }

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
                // CR-037: counted off the join rows so secondary countries appear and the counts
                // agree with the membership filter.
                return await _context.CertificateCountries
                    .Where(cc => cc.Country != "")
                    .GroupBy(cc => cc.Country)
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
                // CR-187: normalized search against persisted computed columns.
                if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                {
                    var nq = SearchNormalizer.Normalize(filters.SearchTerm);
                    query = query.Where(c =>
                        c.Company.NormalizedName!.Contains(nq) ||
                        c.Product.NormalizedName!.Contains(nq) ||
                        c.NormalizedNumber!.Contains(nq));
                }

                var counts = new Dictionary<string, int>
                {
                    { "Total", await query.CountAsync() },
                    { "Active", await query.CountAsync(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && c.ExpirationDate >= CertificateStatusHelper.BusinessToday) },
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