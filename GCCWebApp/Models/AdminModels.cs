using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    // User Management Models
    public class UserViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = string.Empty;
        
        public string Status { get; set; } = "active";
        
        public List<int> CompanyIds { get; set; } = new List<int>();
        
        public List<int> PartnerIds { get; set; } = new List<int>();
        
        public DateTime? LastLogin { get; set; }
        
        public bool RequirePasswordReset { get; set; }

        // 2FA Fields
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorMethod { get; set; }

        // For display purposes
        public List<string> CompanyNames { get; set; } = new List<string>();
        public List<string> PartnerNames { get; set; } = new List<string>();
    }

    public class CreateUserViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
        
        public List<int> CompanyIds { get; set; } = new List<int>();
        
        public List<int> PartnerIds { get; set; } = new List<int>();
        
        public bool RequirePasswordReset { get; set; } = true;
    }

    // Role Management Models
    public class RoleViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string Description { get; set; } = string.Empty;
        
        public List<string> Permissions { get; set; } = new List<string>();
        
        public int UserCount { get; set; }
        
        public bool IsSystem { get; set; } = false;
    }

    public class PermissionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Google Analytics Models
    public class GoogleAnalyticsViewModel
    {
        [Required]
        [StringLength(20)]
        public string TrackingId { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = false;

        public string? CustomScript { get; set; }

        public bool AnonymizeIp { get; set; } = true;

        public bool EnhancedLinkAttribution { get; set; } = false;

        public bool DebugMode { get; set; } = false;

        public DateTime? LastUpdated { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    // Product Types Models
    public class ProductTypeViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public int? ParentId { get; set; }
        
        public int Level { get; set; }
        
        public int SortOrder { get; set; }
        
        public string? Description { get; set; }
        
        public int ProductCount { get; set; }
        
        public List<ProductTypeViewModel> Children { get; set; } = new List<ProductTypeViewModel>();
        
        public string ParentName { get; set; } = string.Empty;
    }

    // Certificate Types Models
    public class CertificateTypeViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int CertificateCount { get; set; }

        /// <summary>
        /// Parent certification type ID (null for top-level types)
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Hierarchy level (1 = parent/top-level, 2 = subcategory/child)
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// Parent type name for display purposes
        /// </summary>
        public string? ParentName { get; set; }
    }

    // Certificate Fields Models
    public class CertificateFieldViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string FieldType { get; set; } = string.Empty;

        public bool IsRequired { get; set; }

        public bool IsVisible { get; set; } = true;

        public int SortOrder { get; set; }

        [StringLength(500)]
        public string Options { get; set; } = string.Empty;

        [StringLength(255)]
        public string DefaultValue { get; set; } = string.Empty;

        [StringLength(500)]
        public string ValidationRules { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Section { get; set; }

        /// <summary>
        /// Role-based visibility: "Public", "External", "Internal"
        /// </summary>
        [StringLength(20)]
        public string RoleVisibility { get; set; } = "Public";
    }

    // CSF Attributes Models
    public class CSFAttributeViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
    }

    // Tags Models
    public class TagViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string TagType { get; set; } = string.Empty; // Internal, External
        
        public int ProductCount { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }

    // Partners Models
    public class PartnerViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string Website { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string ContactEmail { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        public int SortOrder { get; set; } = 0;
        
        public DateTime CreatedDate { get; set; }
        
        public int UserCount { get; set; }
    }

    // Pathways Models
    public class PathwayViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedDate { get; set; }

        public int CertificateCount { get; set; }
    }

    // Product Management Models
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public int? ProductTypeId { get; set; }

        public string ProductTypeName { get; set; } = string.Empty;

        public int? CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }

        public int CertificateCount { get; set; }

        // Direct identifier fields from Product entity
        public string? Model { get; set; }
        public string? Sku { get; set; }
        public string? Upc { get; set; }
        public string? Asin { get; set; }

        public List<ProductIdentifierViewModel> ProductIdentifiers { get; set; } = new List<ProductIdentifierViewModel>();

        // Identifiers from CertificateProductIds (via Certificate relationship)
        public List<CertificateProductIdExportViewModel> CertificateProductIds { get; set; } = new List<CertificateProductIdExportViewModel>();
    }

    public class ProductIdentifierViewModel
    {
        public int Id { get; set; }
        public string IdentifierType { get; set; } = string.Empty;
        public string IdentifierValue { get; set; } = string.Empty;
    }

    public class CertificateProductIdExportViewModel
    {
        public string ProductIdType { get; set; } = string.Empty;
        public string ProductIdValue { get; set; } = string.Empty;
    }

    // Amazon API Models
    public class AmazonAPISettingsViewModel
    {
        [Required]
        [StringLength(100)]
        public string AccessKeyId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string SecretAccessKey { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Region { get; set; } = "us-east-1";
        
        [Required]
        [StringLength(100)]
        public string MarketplaceId { get; set; } = string.Empty;
        
        public bool IsEnabled { get; set; } = true;
        
        public DateTime? LastSync { get; set; }
        
        public string LastSyncStatus { get; set; } = string.Empty;
        
        public int SyncIntervalHours { get; set; } = 24;
    }

    // Common response models
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    // Lookup models for dropdowns
    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCompanies { get; set; }
        public int TotalCertificates { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingCertificates { get; set; }
        public DateTime LastDataUpdate { get; set; }
        public List<string> RecentActivity { get; set; } = new List<string>();
    }
}