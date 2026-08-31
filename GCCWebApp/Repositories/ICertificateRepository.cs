using GCCWebApp.Models;

namespace GCCWebApp.Repositories
{
    public interface ICertificateRepository : IRepository<Certificate>
    {
        Task<IEnumerable<Certificate>> GetActiveCertificatesAsync();
        /// <summary>
        /// QC #141: every cert across every status with full taxonomy nav properties loaded.
        /// Used by the dashboard "Certificates by Status" export so legacy literal statuses
        /// (e.g. "Active" used by KAO certs) aren't silently dropped.
        /// </summary>
        Task<IEnumerable<Certificate>> GetAllCertificatesWithDetailsAsync();
        Task<IEnumerable<Certificate>> GetExpiringCertificatesAsync(DateTime beforeDate);
        Task<IEnumerable<Certificate>> GetCertificatesByCompanyAsync(int companyId);
        Task<IEnumerable<Certificate>> GetCertificatesByProductAsync(int productId);
        Task<IEnumerable<Certificate>> GetCertificatesByTypeAsync(int certificateTypeId);
        Task<Certificate?> GetCertificateByNumberAsync(string certificateNumber);

        /// <summary>
        /// DEF-369: resolve a certificate by number AND a specific id.
        ///
        /// CR-038 allows several certificates to share one number, so
        /// <see cref="GetCertificateByNumberAsync"/> can only ever return one of them and the rest
        /// have no address at all. This overload takes the id the caller already knows.
        ///
        /// The number is still required and still matched: the id alone is NOT trusted as a key.
        /// That keeps the public contract "a URL identifies a certificate number" intact, and stops
        /// a mismatched id from being used to reach a certificate whose number was not requested.
        /// Returns null when the pair does not match, rather than falling back to the number — a
        /// silent fallback would reintroduce exactly the wrong-certificate bug this fixes.
        /// </summary>
        Task<Certificate?> GetCertificateByNumberAndIdAsync(string certificateNumber, int certificateId);

        /// <summary>
        /// DEF-369: how many certificates share this number? Used to decide whether a bare
        /// number-only URL is ambiguous, so the view can tell the visitor there are siblings.
        /// </summary>
        Task<int> CountCertificatesByNumberAsync(string certificateNumber);

        /// <summary>
        /// CR-038: true when no certificate holds this number (optionally ignoring one id, for edits).
        /// Checks ALL certificates including the grandfathered duplicates — the filtered unique index
        /// cannot see those, so this is what stops a new certificate reusing an issued number.
        /// </summary>
        Task<bool> IsCertificateNumberAvailableAsync(string certificateNumber, int? excludeCertificateId = null);
        /// <summary>
        /// DEF-336: batched equivalent of <see cref="IRepository{T}.GetByIdAsync"/> for callers that
        /// need many certificates at once. Same Includes, one round-trip set instead of N.
        /// Calling GetByIdAsync in a loop is what timed out bulk ZIP download — with AsSplitQuery
        /// each certificate costs ~6 queries, so 60 certs was ~360 sequential round-trips.
        /// Missing IDs are simply absent from the result; callers must handle that.
        /// </summary>
        Task<IEnumerable<Certificate>> GetByIdsAsync(IEnumerable<int> ids);
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
        // DEF-200 (QC #180): expired-last-month must filter on ExpirationDate, not EffectiveDate.
        Task<IEnumerable<Certificate>> GetCertificatesByExpirationDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetCertificateStatisticsAsync();
        
        // Dashboard breakdown methods
        Task<IEnumerable<(string Name, int Count)>> GetCertificationTypeStatisticsAsync();
        Task<IEnumerable<(string Name, int Count)>> GetTopCompaniesByActiveCertCountAsync(int topCount = 10);
        Task<IEnumerable<(string Name, int Count)>> GetProductTypeStatisticsAsync(int topCount = 10);
        Task<IEnumerable<(string Name, int Count)>> GetProductCategoryStatisticsAsync(int topCount = 5);
        Task<IEnumerable<(string Name, int Count)>> GetProductSubcategoryStatisticsAsync(int topCount = 10);

        // Additional methods for API
        Task<IEnumerable<string>> GetDistinctCountriesAsync();
        new Task<IEnumerable<Certificate>> FindAsync(System.Linq.Expressions.Expression<Func<Certificate, bool>> predicate);

        // Certificate number generation.
        // asOf is a test-only clock seam (QC #218 year-rollover proof); production passes null.
        Task<string> GenerateNextCertificateNumberAsync(DateTime? asOf = null);

        // DEF-193: atomic generate-and-save with retry on unique-index conflict
        // so concurrent Create-form saves don't collide on Certificates.Number.
        Task<Certificate> AddWithGeneratedNumberAsync(Certificate certificate, int maxAttempts = 3);
    }

    public class CertificateSearchFilter
    {
        public string? SearchTerm { get; set; }
        public int? CompanyId { get; set; }
        public int? ProductId { get; set; }
        public int? CertificationTypeId { get; set; }
        public string? Status { get; set; }
        public List<string>? Statuses { get; set; }
        public bool ExcludeExpiredByDate { get; set; }
        // DEF-358: public-surface lower bound — drop certs whose EffectiveDate is in the future.
        // Paired with ExcludeExpiredByDate to give the full validity window. Public/partner
        // surfaces only; internal and external-company listings leave this false.
        public bool ExcludeNotYetEffectiveByDate { get; set; }
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

        /// <summary>
        /// DEF-381: exact row offset, for callers that page by offset rather than by page number.
        /// When set, it overrides the <see cref="Page"/>/<see cref="PageSize"/> arithmetic entirely.
        ///
        /// Why this exists: an offset can only be expressed as a page number when it is an exact
        /// multiple of the page size. `(offset / pageSize) + 1` silently rounds DOWN otherwise, so
        /// offset=90 with pageSize=50 became page 2 => Skip(50), re-serving rows 51-90 and skipping
        /// the rows the caller actually asked for. The public tile view pages by offset and advances
        /// by the count of *unique* rows it received, so a single duplicate from any cause would put
        /// it permanently on a wrong offset and it could never reach the tail of the list.
        ///
        /// Null (the default) keeps the original page-based behaviour for every existing caller.
        /// </summary>
        public int? Skip { get; set; }
        
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