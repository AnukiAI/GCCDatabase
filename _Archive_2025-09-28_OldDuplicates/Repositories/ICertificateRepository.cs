using GCCWebApp.Models;

namespace GCCWebApp.Repositories
{
    public interface ICertificateRepository : IRepository<Certificate>
    {
        Task<IEnumerable<Certificate>> GetActiveCertificatesAsync();
        Task<IEnumerable<Certificate>> GetExpiringCertificatesAsync(DateTime beforeDate);
        Task<IEnumerable<Certificate>> GetCertificatesByCompanyAsync(int companyId);
        Task<IEnumerable<Certificate>> GetCertificatesByProductAsync(int productId);
        Task<IEnumerable<Certificate>> GetCertificatesByTypeAsync(int certificateTypeId);
        Task<Certificate?> GetCertificateByNumberAsync(string certificateNumber);
        Task<IEnumerable<Certificate>> SearchCertificatesAsync(string searchTerm);
        Task<(IEnumerable<Certificate> certificates, int totalCount)> GetPagedCertificatesAsync(
            int page, int pageSize, string? searchTerm = null, int? companyId = null, int? productId = null, string? partner = null);
        
        // Advanced filtering methods
        Task<(IEnumerable<Certificate> certificates, int totalCount)> GetAdvancedFilteredCertificatesAsync(
            CertificateSearchFilter filter);
        Task<IEnumerable<Certificate>> GetCertificatesByStatusAsync(string status);
        Task<IEnumerable<Certificate>> GetCertificatesExpiringInDaysAsync(int days);
        Task<IEnumerable<Certificate>> GetCertificatesByLocationAsync(string location);
        Task<IEnumerable<Certificate>> GetCertificatesByCountryAsync(string country);
        Task<IEnumerable<Certificate>> GetCertificatesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetCertificateStatisticsAsync();
        
        // Dashboard breakdown methods
        Task<IEnumerable<(string Name, int Count)>> GetCertificationTypeStatisticsAsync();
        Task<IEnumerable<(string Name, int Count)>> GetTopCompaniesByActiveCertCountAsync(int topCount = 10);
        Task<IEnumerable<(string Name, int Count)>> GetProductTypeStatisticsAsync(int topCount = 10);
        
        // Additional methods for API
        Task<IEnumerable<string>> GetDistinctCountriesAsync();
        new Task<IEnumerable<Certificate>> FindAsync(System.Linq.Expressions.Expression<Func<Certificate, bool>> predicate);
    }

    public class CertificateSearchFilter
    {
        public string? SearchTerm { get; set; }
        public int? CompanyId { get; set; }
        public int? ProductId { get; set; }
        public int? CertificationTypeId { get; set; }
        public string? Status { get; set; }
        public string? Partner { get; set; }
        public string? Location { get; set; }
        public string? Country { get; set; }
        public DateTime? EffectiveDateFrom { get; set; }
        public DateTime? EffectiveDateTo { get; set; }
        public DateTime? ExpirationDateFrom { get; set; }
        public DateTime? ExpirationDateTo { get; set; }
        public List<int>? TagIds { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortDescending { get; set; } = true;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        // Additional filter properties for API
        public List<string>? CertificationTypeNames { get; set; }
        public List<string>? Countries { get; set; }
        public List<int>? CompanyIds { get; set; }
        public List<string>? CompanyNames { get; set; }
        public List<string>? ProductCategories { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? AdditionalFilters { get; set; }
    }
}