using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class CertificateListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Sort { get; set; } = "newest";
        public string UserRole { get; set; } = "Public";
        public CertificateSearchFilters Filters { get; set; } = new CertificateSearchFilters();

        public override int GetHashCode()
        {
            return HashCode.Combine(Page, PageSize, Sort, UserRole, Filters.SearchTerm);
        }
    }

    public class CertificateSearchFilters
    {
        public string SearchTerm { get; set; } = string.Empty;
        public List<string> CertificationTypes { get; set; } = new List<string>();
        public List<string> Countries { get; set; } = new List<string>();
        public List<string> Statuses { get; set; } = new List<string>();
        public List<string> Companies { get; set; } = new List<string>();
        public List<string> ProductCategories { get; set; } = new List<string>();
        public DateTime? EffectiveDateStart { get; set; }
        public DateTime? EffectiveDateEnd { get; set; }
        public DateTime? ExpirationDateStart { get; set; }
        public DateTime? ExpirationDateEnd { get; set; }
    }

    public class CertificateListResponse
    {
        public List<CertificateCardModel> Certificates { get; set; } = new List<CertificateCardModel>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public string CurrentSort { get; set; } = string.Empty;
        public CertificateSearchFilters AppliedFilters { get; set; } = new CertificateSearchFilters();
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();

        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

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
        public string Status { get; set; } = "active";
        public string Partner { get; set; } = "None";
        public string? Asin { get; set; }
        public string? Upc { get; set; }
        public string? Gtin { get; set; }
        public string? Ean { get; set; }
        public string BadgeText { get; set; } = string.Empty;
        public string TagType { get; set; } = "External";
        public string CertificateUrl { get; set; } = string.Empty;
        public string DetailsUrl { get; set; } = string.Empty;

        public string FormattedCertificationPeriod => $"{EffectiveDate:M/d/yyyy} - {ExpirationDate:M/d/yyyy}";
        public string FormattedLocation => !string.IsNullOrEmpty(Location) ? $"{Location}, {Country}" : Country;
        public bool IsExpiringSoon => ExpirationDate <= DateTime.Now.AddDays(30);
        public bool IsExpired => ExpirationDate < DateTime.Now;

        public string StatusClass => Status.ToLower() switch
        {
            "active" => "badge-success",
            "expired" => "badge-danger",
            "pending" => "badge-warning",
            "suspended" => "badge-secondary",
            _ => "badge-primary"
        };
    }

    public class CertificateDownloadModel
    {
        public int CertificateId { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/pdf";
        public string UserRole { get; set; } = string.Empty;
    }

    public class CertificateStatistics
    {
        public int TotalCertificates { get; set; }
        public int ActiveCertificates { get; set; }
        public int ExpiredCertificates { get; set; }
        public int ExpiringSoonCertificates { get; set; }
        public int PendingCertificates { get; set; }
        public Dictionary<string, int> CertificatesByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CertificatesByCountry { get; set; } = new Dictionary<string, int>();
    }

    public class BulkActionRequest
    {
        public string Action { get; set; } = string.Empty;
        public List<int> CertificateIds { get; set; } = new List<int>();
        public string UserRole { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }

    public class BulkActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AffectedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class CertificateValidationResult
    {
        public bool IsValid { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public bool IsExpired { get; set; }
        public bool IsExpiringSoon { get; set; }
    }

    public class FilterOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Count { get; set; }
        public string FilterType { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public List<FilterOption> Children { get; set; } = new List<FilterOption>();
    }
}