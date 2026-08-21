using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    /// <summary>
    /// Outcome of AdminService.UpdateUserAsync.
    ///
    /// Previously the method returned bool, which collapsed "no such user" and "the save threw"
    /// into a single false. AdminController then Json()'d every outcome as HTTP 200, so a failed
    /// update was indistinguishable from a successful one to any status-code-based caller — the
    /// CRUD smoke suite recorded PASS on the 200 and only caught the problem one step later when
    /// the row had not changed. Callers need the distinction to pick a status code.
    /// </summary>
    public enum UserUpdateResult
    {
        Success,

        /// <summary>No non-deleted user with that id. Maps to 404.</summary>
        NotFound,

        /// <summary>The update threw; details are in the server log. Maps to 500.</summary>
        Failed,

        /// <summary>
        /// DEF-366: another live user already owns the requested email. Maps to 409 with a message
        /// naming the address, so the admin can act on it. Previously this case had no result of
        /// its own: the email was assigned, the database's unique index threw on save, and it came
        /// back as <see cref="Failed"/> — a 500 reading "Update failed. See the server log for
        /// details." The user was told nothing they could act on, for an entirely ordinary and
        /// foreseeable mistake. CreateUserAsync has always reported this properly; only the update
        /// path did not.
        /// </summary>
        DuplicateEmail
    }

    // User Management Models
    public class UserViewModel
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        // QC #181 alignment: stricter than [EmailAddress] (which accepts "a@b"). Requires
        // name@domain.tld with a 2+ char TLD. Matches the pattern used in CertificateEditViewModel
        // and CompanyViewModel.
        [RegularExpression(
            @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Enter a valid email address (e.g., name@example.com).")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public List<int> CompanyIds { get; set; } = new List<int>();

        public List<int> PartnerIds { get; set; } = new List<int>();

        // CR-020 (revised QC #106): the user's own company/employer as free text. Distinct from
        // CompanyIds, which is the data-access scope. Label/display only.
        public string? UserCompanyName { get; set; }

        public DateTime? LastLogin { get; set; }

        public bool RequirePasswordReset { get; set; }

        // DEF-187: Edit User modal password field was previously dead UI — the JS sent
        // userData.password but UserViewModel had no Password property, so the model
        // binder silently dropped it and UpdateUserAsync never touched user.PasswordHash.
        // Nullable + no [Required]: blank/null = "no change", non-blank = set new password.
        // Min-length validation lives in the controller (matches CreateUserAsync's 8-char rule).
        [StringLength(100)]
        public string? Password { get; set; }

        // 2FA Fields
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorMethod { get; set; }

        // For display purposes
        public List<string> CompanyNames { get; set; } = new List<string>();
        public List<string> PartnerNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// DEF-113: payload row for /Admin/BulkImportUsers. Uses name-based references
    /// (CompanyNames as strings) — service-side resolves to FK IDs against pre-fetched
    /// dictionaries to avoid one DB roundtrip per row.
    /// </summary>
    public class BulkImportUserItem
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public bool RequirePasswordReset { get; set; } = true;
        public List<string> CompanyNames { get; set; } = new();
        // DEF-232: Partner-role users created via CSV import. Mirrors CompanyNames
        // (semicolon-separated names → resolved to Partner entities in the service).
        public List<string> PartnerNames { get; set; } = new();
        // CR-020: the user's own company/employer (single name → resolved to UserCompanyId).
        public string? UserCompanyName { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        // QC #181 alignment: stricter than [EmailAddress]. Matches the pattern used in
        // CertificateEditViewModel, CompanyViewModel, and UserViewModel above.
        [RegularExpression(
            @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Enter a valid email address (e.g., name@example.com).")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = string.Empty;
        
        // DEF-186: Password is no longer [Required] at the model-binder layer.
        // Bulk-import callers send "" so AdminService.CreateUserAsync can generate
        // a cryptographically random one and force RequirePasswordReset=true.
        // Minimum-length enforcement moved to the service (only when a password is
        // explicitly provided by the caller; generated passwords are always 24 chars).
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;
        
        public List<int> CompanyIds { get; set; } = new List<int>();

        public List<int> PartnerIds { get; set; } = new List<int>();

        // CR-020 (revised QC #106): the user's own company/employer as free text.
        public string? UserCompanyName { get; set; }

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

        // QC #170: Certificates referencing this category/type/subcategory.
        // Counts via Certificates.ProductCategoryId / ProductTypeId / ProductSubcategoryId
        // — independent of Products. Rolled up the same way as ProductCount.
        public int CertificateCount { get; set; }

        public List<ProductTypeViewModel> Children { get; set; } = new List<ProductTypeViewModel>();

        public string ParentName { get; set; } = string.Empty;

        /// <summary>
        /// QC #117: Distinguishes root-level ProductCategory nodes from ProductType nodes.
        /// When true, Id refers to ProductCategories table; when false, Id refers to ProductTypes table.
        /// </summary>
        public bool IsCategory { get; set; }
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