using GCCWebApp.Models;
using GreenCircleCertified.ViewModels;

namespace GCCWebApp.Services
{
    public interface ICertificateService
    {
        Task<CertificateListResponse> GetCertificatesAsync(CertificateListRequest request);
        Task<CertificateDetailsViewModel> GetCertificateDetailsAsync(int id, string userRole);
        Task<CertificateDownloadModel> GetCertificateDownloadAsync(int id, string userRole);
        Task<byte[]> ExportCertificatesAsync(CertificateListRequest request, string format);
        Task<CertificateStatistics> GetCertificateStatisticsAsync(string userRole);
        Task<BulkActionResult> PerformBulkActionAsync(BulkActionRequest request);
        Task<List<string>> GetSearchSuggestionsAsync(string term, string type, string userRole);
        Task<CertificateValidationResult> ValidateCertificateAsync(string certificateNumber);
        Task<int> CreateCertificateAsync(CertificateEditViewModel certificate, string userRole);
        Task<bool> UpdateCertificateAsync(int id, CertificateEditViewModel certificate, string userRole);
        Task<bool> DeleteCertificateAsync(int id, string userRole);
        Task<CertificateDetailsViewModel> GetCertificateByCertificateNumberAsync(string certificateNumber, string userRole);
    }

    public interface IFilterService
    {
        Task<List<FilterOption>> GetAvailableFiltersAsync(string userRole);
        Task<List<FilterOption>> GetFilterOptionsAsync(string filterType, string searchTerm, string userRole);
        Task<List<FilterOption>> GetProductCategoriesAsync(string userRole);
        Task<List<FilterOption>> GetCompaniesAsync(string searchTerm, string userRole);
        Task<List<FilterOption>> GetCountriesAsync(string userRole);
        Task<List<FilterOption>> GetCertificationTypesAsync(string userRole);
        Task<Dictionary<string, int>> GetFilterCountsAsync(CertificateSearchFilters filters, string userRole);
    }

    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task ClearByPatternAsync(string pattern);
    }
}