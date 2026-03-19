using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using GCCWebApp.Models;

namespace GreenCircleCertified.Models
{
    /// <summary>
    /// Main model for the Certificate List page
    /// </summary>
    public class CertificateListViewModel
    {
        public CertificateSearchFilters Filters { get; set; } = new CertificateSearchFilters();
        public List<CertificateCardModel> Certificates { get; set; } = new List<CertificateCardModel>();
        public PaginationModel Pagination { get; set; } = new PaginationModel();
        public int TotalResults { get; set; }
        public string CurrentSort { get; set; } = "default";
        public List<SortOption> SortOptions { get; set; } = GetDefaultSortOptions();
        public List<FilterOption> AvailableFilters { get; set; } = new List<FilterOption>();

        private static List<SortOption> GetDefaultSortOptions()
        {
            return new List<SortOption>
            {
                new SortOption { Value = "default", Text = "Default", Icon = "fas fa-sort" },
                new SortOption { Value = "newest", Text = "Newest Effective Date", Icon = "fas fa-calendar-plus" },
                new SortOption { Value = "product-az", Text = "Product Name A-Z", Icon = "fas fa-sort-alpha-down" },
                new SortOption { Value = "company-az", Text = "Company A-Z", Icon = "fas fa-building" },
                new SortOption { Value = "status", Text = "Status", Icon = "fas fa-check-circle" },
                new SortOption { Value = "type", Text = "Certification Type", Icon = "fas fa-certificate" },
                new SortOption { Value = "country", Text = "Country", Icon = "fas fa-globe" }
            };
        }
    }

    /// <summary>
    /// Model for individual certificate cards
    /// </summary>
    public class CertificateCardModel
    {
        public int Id { get; set; }

        [Display(Name = "Certificate Number")]
        public required string CertificateNumber { get; set; }

        [Display(Name = "Certificate Type")]
        public required string CertificationType { get; set; }

        [Display(Name = "Company Name")]
        public required string CompanyName { get; set; }

        [Display(Name = "Product Name")]
        public required string ProductName { get; set; }

        [Display(Name = "Product Category")]
        public string? ProductCategory { get; set; }

        [Display(Name = "Effective Date")]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; }

        [Display(Name = "Expiration Date")]
        [DataType(DataType.Date)]
        public DateTime ExpirationDate { get; set; }

        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Display(Name = "Country")]
        public string? Country { get; set; }

        [Display(Name = "Status")]
        public CertificateStatus Status { get; set; }

        [Display(Name = "Badge Text")]
        public required string BadgeText { get; set; }

        [Display(Name = "Tag Type")]
        public string TagType { get; set; } = "External";

        public required string CertificateUrl { get; set; }
        public required string DetailsUrl { get; set; }

        // Computed properties
        public string FormattedCertificationPeriod => $"{EffectiveDate:M/d/yyyy} - {ExpirationDate:M/d/yyyy}";
        public string FormattedLocation => $"{Location ?? ""}, {Country ?? ""}";
        public bool IsExpiringSoon => ExpirationDate <= DateTime.Now.AddDays(30);
        public bool IsExpired => ExpirationDate < DateTime.Now;

        public string StatusText => CertificateStatusHelper.GetDisplayName(Status.ToString());

        public string StatusClass => Status switch
        {
            CertificateStatus.ClientApprovedPaid => "badge-success",
            CertificateStatus.Republished => "badge-success",
            CertificateStatus.ClientApprovedNotPaid => "badge-warning",
            CertificateStatus.ClientApprovedConfidential => "badge-warning",
            CertificateStatus.Hold => "badge-warning",
            CertificateStatus.Expired => "badge-danger",
            CertificateStatus.ExpiredNoAnnualVerification => "badge-danger",
            CertificateStatus.Draft => "badge-secondary",
            _ => "badge-primary"
        };
    }

    /// <summary>
    /// Search and filter model
    /// </summary>
    public class CertificateSearchFilters
    {
        [Display(Name = "Search Term")]
        public string SearchTerm { get; set; } = string.Empty;

        [Display(Name = "Certification Types")]
        public List<string> CertificationTypes { get; set; } = new List<string>();

        [Display(Name = "Product Categories")]
        public List<string> ProductCategories { get; set; } = new List<string>();

        [Display(Name = "Companies")]
        public List<string> Companies { get; set; } = new List<string>();

        [Display(Name = "Countries")]
        public List<string> Countries { get; set; } = new List<string>();

        [Display(Name = "Status")]
        public List<CertificateStatus> Statuses { get; set; } = new List<CertificateStatus>();

        [Display(Name = "Additional Filters")]
        public List<string> AdditionalFilters { get; set; } = new List<string>();

        [Display(Name = "Effective Date From")]
        [DataType(DataType.Date)]
        public DateTime? EffectiveDateFrom { get; set; }

        [Display(Name = "Effective Date To")]
        [DataType(DataType.Date)]
        public DateTime? EffectiveDateTo { get; set; }

        [Display(Name = "Expiration Date From")]
        [DataType(DataType.Date)]
        public DateTime? ExpirationDateFrom { get; set; }

        [Display(Name = "Expiration Date To")]
        [DataType(DataType.Date)]
        public DateTime? ExpirationDateTo { get; set; }

        public bool IsExpiringSoonOnly { get; set; }
        public bool IsRenewableEnergyOnly { get; set; }
        public bool HasCarbonFootprint { get; set; }

        // Helper method to check if any filters are applied
        public bool HasActiveFilters =>
            !string.IsNullOrEmpty(SearchTerm) ||
            CertificationTypes.Any() ||
            ProductCategories.Any() ||
            Companies.Any() ||
            Countries.Any() ||
            Statuses.Any() ||
            AdditionalFilters.Any() ||
            EffectiveDateFrom.HasValue ||
            EffectiveDateTo.HasValue ||
            ExpirationDateFrom.HasValue ||
            ExpirationDateTo.HasValue ||
            IsExpiringSoonOnly ||
            IsRenewableEnergyOnly ||
            HasCarbonFootprint;
    }

    /// <summary>
    /// Pagination model
    /// </summary>
    public class PaginationModel
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

        public string GetPaginationInfo() =>
            $"Page {CurrentPage} of {TotalPages} ({TotalItems} total certificates)";
    }

    /// <summary>
    /// Sort option model
    /// </summary>
    public class SortOption
    {
        public required string Value { get; set; }
        public required string Text { get; set; }
        public required string Icon { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Filter option model for building dynamic filter UI
    /// </summary>
    public class FilterOption
    {
        public required string FilterType { get; set; }
        public required string Value { get; set; }
        public required string Text { get; set; }
        public int Count { get; set; }
        public bool IsSelected { get; set; }
        public int Level { get; set; } = 1; // For hierarchical filters
        public List<FilterOption> Children { get; set; } = new List<FilterOption>();
        public bool HasChildren => Children.Any();
    }

    /// <summary>
    /// User role enumeration for role-based filtering
    /// </summary>
    public enum UserRole
    {
        [Display(Name = "Public")]
        Public = 1,

        [Display(Name = "External")]
        External = 2,

        [Display(Name = "Analyst")]
        Analyst = 3,

        [Display(Name = "Administrator")]
        Admin = 4
    }

    /// <summary>
    /// Certificate type enumeration
    /// </summary>
    public enum CertificationType
    {
        [Display(Name = "Certified Sustainability Facts")]
        CertifiedSustainabilityFacts = 1,

        [Display(Name = "Recycled Content")]
        RecycledContent = 2,

        [Display(Name = "Renewable Energy")]
        RenewableEnergy = 3,

        [Display(Name = "Carbon Footprint")]
        CarbonFootprint = 4,

        [Display(Name = "Chain of Custody")]
        ChainOfCustody = 5,

        [Display(Name = "Life Cycle Assessment")]
        LifeCycleAssessment = 6
    }

    /// <summary>
    /// DTO for API responses
    /// </summary>
    public class CertificateListResponse
    {
        public List<CertificateCardModel> Certificates { get; set; } = new List<CertificateCardModel>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public required string CurrentSort { get; set; }
        public required CertificateSearchFilters AppliedFilters { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Request model for certificate list API
    /// </summary>
    public class CertificateListRequest
    {
        public CertificateSearchFilters Filters { get; set; } = new CertificateSearchFilters();
        public string Sort { get; set; } = "default";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public UserRole UserRole { get; set; } = UserRole.Public;
    }

    /// <summary>
    /// Model for certificate download
    /// </summary>
    public class CertificateDownloadModel
    {
        public int CertificateId { get; set; }
        public required string CertificateNumber { get; set; }
        public required string FileName { get; set; }
        public required byte[] FileContent { get; set; }
        public string ContentType { get; set; } = "application/pdf";
        public DateTime DownloadDate { get; set; } = DateTime.UtcNow;
        public required string UserEmail { get; set; }
        public UserRole UserRole { get; set; }
    }

    /// <summary>
    /// Model for certificate details (expanded view)
    /// </summary>
    public class CertificateDetailModel : CertificateCardModel
    {
        public required string Description { get; set; }
        public List<string> AdditionalAttributes { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public required string CertificationBody { get; set; }
        public required string Standard { get; set; }
        public required string Scope { get; set; }
        public List<string> RelatedDocuments { get; set; } = new List<string>();
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public required string CreatedBy { get; set; }
        public required string LastModifiedBy { get; set; }

        // Audit trail
        public List<CertificateAuditEntry> AuditTrail { get; set; } = new List<CertificateAuditEntry>();
    }

    /// <summary>
    /// Audit entry model for certificate history
    /// </summary>
    public class CertificateAuditEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Action { get; set; }
        public required string UserName { get; set; }
        public required string Details { get; set; }
        public required string OldValue { get; set; }
        public required string NewValue { get; set; }
    }

    /// <summary>
    /// Configuration model for certificate list settings
    /// </summary>
    public class CertificateListSettings
    {
        public int DefaultPageSize { get; set; } = 20;
        public int MaxPageSize { get; set; } = 100;
        public List<int> AllowedPageSizes { get; set; } = new List<int> { 10, 20, 50, 100 };
        public string DefaultSort { get; set; } = "default";
        public bool EnablePublicAccess { get; set; } = true;
        public bool EnableDownloads { get; set; } = true;
        public bool EnableSearch { get; set; } = true;
        public bool EnableFilters { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 15;
        public bool EnableAuditLogging { get; set; } = true;
    }
}