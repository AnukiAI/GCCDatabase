// Cleaned and formatted source code file for GreenCircleCertified
// Includes Interfaces, Models, Services, Controllers, and Startup

// --- Interfaces.cs ---
using GCCWebApp.Controllers;
using GreenCircleCertified.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static GreenCircleCertified.Services.Interfaces;

namespace GreenCircleCertified.Services
{
    public interface ICertificateService
    {
        Task<CertificateListResponse> GetCertificatesAsync(CertificateListRequest request);
        Task<CertificateDetailModel> GetCertificateDetailsAsync(int id, UserRole userRole);
        Task<CertificateDownloadModel> GetCertificateDownloadAsync(int id, UserRole userRole);
        Task LogDownloadActivityAsync(CertificateDownloadModel downloadModel);
        Task<byte[]> ExportCertificatesAsync(CertificateListRequest request, string format);
        Task<CertificateStatistics> GetCertificateStatisticsAsync(UserRole userRole);
        Task<BulkActionResult> PerformBulkActionAsync(BulkActionRequest request);
        Task<List<string>> GetSearchSuggestionsAsync(string term, string type, UserRole userRole);
        Task<CertificateValidationResult> ValidateCertificateAsync(string certificateNumber);
        Task<int> CreateCertificateAsync(CertificateDetailModel certificate, UserRole userRole);
        Task<bool> UpdateCertificateAsync(CertificateDetailModel certificate, UserRole userRole);
        Task<bool> DeleteCertificateAsync(int id, UserRole userRole);
        Task<CertificateDetailModel> GetCertificateByCertificateNumberAsync(string certificateNumber, UserRole userRole);
    }

    public interface IFilterService
    {
        Task<List<FilterOption>> GetAvailableFiltersAsync(UserRole userRole);
        Task<List<FilterOption>> GetFilterOptionsAsync(string filterType, string searchTerm, UserRole userRole);
        Task<List<FilterOption>> GetProductCategoriesAsync(UserRole userRole);
        Task<List<FilterOption>> GetCompaniesAsync(string searchTerm, UserRole userRole);
        Task<List<FilterOption>> GetCountriesAsync(UserRole userRole);
        Task<List<FilterOption>> GetCertificationTypesAsync(UserRole userRole);
        string BuildFilterQuery(CertificateSearchFilters filters);
        Task<Dictionary<string, int>> GetFilterCountsAsync(CertificateSearchFilters filters, UserRole userRole);
    }

    public interface IUserService
    {
        Task<UserInfo> GetCurrentUserAsync();
        bool HasPermission(UserRole userRole, string action);
        Task LogUserActivityAsync(string action, string details, UserRole userRole);
    }

    public interface IAuditService
    {
        Task LogCertificateAccessAsync(int certificateId, UserRole userRole, string action);
        Task LogSearchActivityAsync(string searchTerm, CertificateSearchFilters filters, UserRole userRole, int resultCount);
        Task LogDownloadActivityAsync(int certificateId, UserRole userRole, string fileName);
        Task<List<CertificateAuditEntry>> GetCertificateAuditTrailAsync(int certificateId);
    }

    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task ClearByPatternAsync(string pattern);
    }

    public interface IEmailService
    {
        Task SendExpirationNotificationAsync(CertificateDetailModel certificate, List<string> recipients);
        Task SendDownloadNotificationAsync(CertificateDownloadModel download, string recipientEmail);
        Task SendBulkOperationNotificationAsync(BulkActionResult result, string recipientEmail);
    }
}

// --- Models.cs ---
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GreenCircleCertified.Models
{
    public class CertificateCardModel
    {
        public int Id { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string CertificationType { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public CertificateStatus Status { get; set; }
        public string BadgeText { get; set; } = string.Empty;
        public string TagType { get; set; } = "External";
        public string CertificateUrl { get; set; } = string.Empty;
        public string DetailsUrl { get; set; } = string.Empty;

        public string FormattedCertificationPeriod => $"{EffectiveDate:M/d/yyyy} - {ExpirationDate:M/d/yyyy}";
        public string FormattedLocation => $"{Location}, {Country}";
        public bool IsExpiringSoon => ExpirationDate <= DateTime.Now.AddDays(30);
        public bool IsExpired => ExpirationDate < DateTime.Now;

        public string StatusClass => Status switch
        {
            CertificateStatus.Active => "badge-success",
            CertificateStatus.Expired => "badge-danger",
            CertificateStatus.Pending => "badge-warning",
            CertificateStatus.Suspended => "badge-secondary",
            _ => "badge-primary"
        };
    }

    public class CertificateDetailModel : CertificateCardModel
    {
        public string Description { get; set; } = string.Empty;
        public List<string> AdditionalAttributes { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string CertificationBody { get; set; } = string.Empty;
        public string Standard { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public List<string> RelatedDocuments { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string LastModifiedBy { get; set; } = string.Empty;
        public List<CertificateAuditEntry> AuditTrail { get; set; } = new();
    }

    public enum CertificateStatus
    {
        Active = 1,
        Expired = 2,
        Pending = 3,
        Suspended = 4,
        UnderReview = 5
    }

    public enum UserRole
    {
        Public = 1,
        External = 2,
        Analyst = 3,
        Admin = 4
    }

    public class CertificateAuditEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
    }
}

// [Next section: Services.cs will follow.]
