using GCCWebApp.Controllers;
using GreenCircleCertified.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenCircleCertified.Services
{
    public class Interfaces
    {
   

    /// <summary>
    /// Service interface for certificate operations
    /// </summary>
    public interface ICertificateService
    {
        /// <summary>
        /// Get paginated list of certificates with filtering and sorting
        /// </summary>
        Task<CertificateListResponse> GetCertificatesAsync(CertificateListRequest request);

        /// <summary>
        /// Get detailed certificate information by ID
        /// </summary>
        Task<CertificateDetailModel> GetCertificateDetailsAsync(int id, UserRole userRole);

        /// <summary>
        /// Get certificate download information
        /// </summary>
        Task<CertificateDownloadModel> GetCertificateDownloadAsync(int id, UserRole userRole);

        /// <summary>
        /// Log certificate download activity
        /// </summary>
        Task LogDownloadActivityAsync(CertificateDownloadModel downloadModel);

        /// <summary>
        /// Export certificates to various formats
        /// </summary>
        Task<byte[]> ExportCertificatesAsync(CertificateListRequest request, string format);

        /// <summary>
        /// Get certificate statistics for dashboard
        /// </summary>
        Task<CertificateStatistics> GetCertificateStatisticsAsync(UserRole userRole);

        /// <summary>
        /// Perform bulk operations on certificates
        /// </summary>
        Task<BulkActionResult> PerformBulkActionAsync(BulkActionRequest request);

        /// <summary>
        /// Get search suggestions for autocomplete
        /// </summary>
        Task<List<string>> GetSearchSuggestionsAsync(string term, string type, UserRole userRole);

        /// <summary>
        /// Validate certificate by certificate number
        /// </summary>
        Task<CertificateValidationResult> ValidateCertificateAsync(string certificateNumber);

        /// <summary>
        /// Create a new certificate
        /// </summary>
        Task<int> CreateCertificateAsync(CertificateDetailModel certificate, UserRole userRole);

        /// <summary>
        /// Update an existing certificate
        /// </summary>
        Task<bool> UpdateCertificateAsync(CertificateDetailModel certificate, UserRole userRole);

        /// <summary>
        /// Delete a certificate by ID
        /// </summary>
        Task<bool> DeleteCertificateAsync(int id, UserRole userRole);

        /// <summary>
        /// Get certificate by certificate number
        /// </summary>
        Task<CertificateDetailModel> GetCertificateByCertificateNumberAsync(string certificateNumber, UserRole userRole);
    }

    /// <summary>
    /// Service interface for filter operations
    /// </summary>
    public interface IFilterService
    {
        /// <summary>
        /// Get available filters for the specified user role
        /// </summary>
        Task<List<FilterOption>> GetAvailableFiltersAsync(UserRole userRole);

        /// <summary>
        /// Get filter options for a specific filter type
        /// </summary>
        Task<List<FilterOption>> GetFilterOptionsAsync(string filterType, string searchTerm, UserRole userRole);

        /// <summary>
        /// Get hierarchical product categories for filtering
        /// </summary>
        Task<List<FilterOption>> GetProductCategoriesAsync(UserRole userRole);

        /// <summary>
        /// Get list of companies for filtering
        /// </summary>
        Task<List<FilterOption>> GetCompaniesAsync(string searchTerm, UserRole userRole);

        /// <summary>
        /// Get list of countries for filtering
        /// </summary>
        Task<List<FilterOption>> GetCountriesAsync(UserRole userRole);

        /// <summary>
        /// Get list of certification types for filtering
        /// </summary>
        Task<List<FilterOption>> GetCertificationTypesAsync(UserRole userRole);

        /// <summary>
        /// Build filter query based on applied filters
        /// </summary>
        string BuildFilterQuery(CertificateSearchFilters filters);

        /// <summary>
        /// Get filter counts for current search
        /// </summary>
        Task<Dictionary<string, int>> GetFilterCountsAsync(CertificateSearchFilters filters, UserRole userRole);
    }

    /// <summary>
    /// Service interface for user management
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Get current user information
        /// </summary>
        Task<UserInfo> GetCurrentUserAsync();

        /// <summary>
        /// Check if user has permission for specific action
        /// </summary>
        bool HasPermission(UserRole userRole, string action);

        /// <summary>
        /// Log user activity
        /// </summary>
        Task LogUserActivityAsync(string action, string details, UserRole userRole);
    }

    /// <summary>
    /// Service interface for audit logging
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Log certificate access
        /// </summary>
        Task LogCertificateAccessAsync(int certificateId, UserRole userRole, string action);

        /// <summary>
        /// Log search activity
        /// </summary>
        Task LogSearchActivityAsync(string searchTerm, CertificateSearchFilters filters, UserRole userRole, int resultCount);

        /// <summary>
        /// Log download activity
        /// </summary>
        Task LogDownloadActivityAsync(int certificateId, UserRole userRole, string fileName);

        /// <summary>
        /// Get audit trail for certificate
        /// </summary>
        Task<List<CertificateAuditEntry>> GetCertificateAuditTrailAsync(int certificateId);
    }

    /// <summary>
    /// Service interface for caching
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get cached data
        /// </summary>
        Task<T> GetAsync<T>(string key) where T : class;

        /// <summary>
        /// Set cached data
        /// </summary>
        Task SetAsync<T>(string key, T data, TimeSpan? expiration = null) where T : class;

        /// <summary>
        /// Remove cached data
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// Clear cache by pattern
        /// </summary>
        Task ClearByPatternAsync(string pattern);
    }

    /// <summary>
    /// Service interface for email notifications
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send certificate expiration notification
        /// </summary>
        Task SendExpirationNotificationAsync(CertificateDetailModel certificate, List<string> recipients);

        /// <summary>
        /// Send download notification
        /// </summary>
        Task SendDownloadNotificationAsync(CertificateDownloadModel download, string recipientEmail);

        /// <summary>
        /// Send bulk operation notification
        /// </summary>
        Task SendBulkOperationNotificationAsync(BulkActionResult result, string recipientEmail);
    }

    #region Supporting Models for Services

    /// <summary>
    /// Certificate statistics model
    /// </summary>
    public class CertificateStatistics
    {
        public int TotalCertificates { get; set; }
        public int ActiveCertificates { get; set; }
        public int ExpiredCertificates { get; set; }
        public int ExpiringSoonCertificates { get; set; }
        public int PendingCertificates { get; set; }
        public Dictionary<string, int> CertificatesByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CertificatesByCountry { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CertificatesByMonth { get; set; } = new Dictionary<string, int>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// User information model
    /// </summary>
    public class UserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public UserRole Role { get; set; }
        public bool IsAuthenticated { get; set; }
        public DateTime LastLoginDate { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Search suggestion model
    /// </summary>
    public class SearchSuggestion
    {
        public string Text { get; set; }
        public string Type { get; set; } // "company", "product", "certification_type", etc.
        public int Count { get; set; }
        public string Category { get; set; }
    }

    /// <summary>
    /// Extension method for rendering views to string (for AJAX responses)
    /// </summary>
    public static class ControllerExtensions
    {
        public static async Task<string> RenderViewAsync<TModel>(this Controller controller, string viewName, TModel model)
        {
            // Implementation would use ICompositeViewEngine to render view to string
            // This is a placeholder for the actual implementation
            return await Task.FromResult($"<!-- Rendered view: {viewName} -->");
        }
    }

        #endregion
    }
}