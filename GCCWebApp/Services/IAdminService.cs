using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface IAdminService
    {
        // Dashboard
        Task<AdminDashboardViewModel> GetDashboardDataAsync();
        
        // User Management
        Task<List<UserViewModel>> GetUsersAsync();
        Task<UserViewModel?> GetUserByIdAsync(int id);
        Task<(bool Success, string ErrorMessage)> CreateUserAsync(CreateUserViewModel model, int userId, string username);

        /// <summary>
        /// DEF-113: bulk-create users in a single transaction. Pre-fetches existing emails,
        /// companies, partners, and roles; resolves name-based references in memory.
        /// Replaces userManagement.js#importCSV()'s per-row /Admin/CreateUser loop.
        /// Replace-all NOT supported — too risky for users (mass-deactivation = locked-out admins).
        /// Each row still pays for BCrypt password hashing (~50ms) — that's CPU-bound, not DB.
        /// </summary>
        Task<BulkImportResult> BulkImportUsersAsync(List<BulkImportUserItem> items, int userId, string username);
        Task<UserUpdateResult> UpdateUserAsync(int id, UserViewModel model, int userId, string username);
        Task<bool> DeleteUserAsync(int id, int userId, string username);
        
        // Role Management
        Task<List<RoleViewModel>> GetRolesAsync();
        Task<RoleViewModel?> GetRoleByIdAsync(int id);
        Task<bool> CreateRoleAsync(RoleViewModel model);
        Task<bool> UpdateRoleAsync(int id, RoleViewModel model);
        Task<bool> DeleteRoleAsync(int id, int userId = 0, string? username = null);
        Task<List<PermissionViewModel>> GetPermissionsAsync();
        
        // Product Types
        Task<List<ProductTypeViewModel>> GetProductTypesAsync();
        Task<ProductTypeViewModel?> GetProductTypeByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, ProductTypeViewModel? Data)> CreateProductTypeAsync(ProductTypeViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateProductTypeAsync(ProductTypeViewModel model);
        // DEF-337: userId/username optional so existing callers keep compiling; when omitted the
        // audit service resolves the acting user from the signed-in claims.
        Task<(bool Success, string Message)> DeleteProductTypeAsync(int id, int userId = 0, string? username = null);
        Task<(bool Success, string Message)> DeleteProductCategoryAsync(int id, int userId = 0, string? username = null);

        // Certificate Types
        Task<List<CertificateTypeViewModel>> GetCertificateTypesAsync();
        Task<CertificateTypeViewModel?> GetCertificateTypeByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateCertificateTypeAsync(CertificateTypeViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateCertificateTypeAsync(int id, CertificateTypeViewModel model);
        Task<bool> DeleteCertificateTypeAsync(int id, int userId = 0, string? username = null); // DEF-337
        
        // Certificate Fields
        Task<List<CertificateFieldViewModel>> GetCertificateFieldsAsync();
        Task<CertificateFieldViewModel?> GetCertificateFieldByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateCertificateFieldAsync(CertificateFieldViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateCertificateFieldAsync(int id, CertificateFieldViewModel model);
        Task<bool> DeleteCertificateFieldAsync(int id, int userId = 0, string? username = null);

        // CSF Attributes
        Task<List<CSFAttributeViewModel>> GetCSFAttributesAsync();
        Task<CSFAttributeViewModel?> GetCSFAttributeByIdAsync(int id);
        Task<bool> CreateCSFAttributeAsync(CSFAttributeViewModel model);
        Task<bool> UpdateCSFAttributeAsync(int id, CSFAttributeViewModel model);
        Task<bool> DeleteCSFAttributeAsync(int id, int userId = 0, string? username = null);

        // Tags
        Task<List<TagViewModel>> GetTagsAsync();
        Task<TagViewModel?> GetTagByIdAsync(int id);
        Task<bool> CreateTagAsync(TagViewModel model);
        Task<bool> UpdateTagAsync(int id, TagViewModel model);

        /// <summary>
        /// DEF-113: bulk-create tags in a single transaction. Pre-fetches existing
        /// (Name, TagType) pairs for in-memory dup detection. Replaces tags.js#applySelectedImport()'s
        /// per-row POST loop, which hit the localhost rate-limit cap on imports >200 rows.
        /// </summary>
        Task<BulkImportResult> BulkImportTagsAsync(List<TagViewModel> models, bool overwriteExisting = false);
        Task<bool> DeleteTagAsync(int id, int userId = 0, string? username = null); // DEF-337
        
        // Partners
        Task<List<PartnerViewModel>> GetPartnersAsync();
        Task<PartnerViewModel?> GetPartnerByIdAsync(int id);
        Task<bool> CreatePartnerAsync(PartnerViewModel model);
        Task<bool> UpdatePartnerAsync(int id, PartnerViewModel model);
        Task<bool> DeletePartnerAsync(int id, int userId = 0, string? username = null);

        // Pathways
        Task<List<PathwayViewModel>> GetPathwaysAsync();
        Task<PathwayViewModel?> GetPathwayByIdAsync(int id);
        Task<bool> CreatePathwayAsync(PathwayViewModel model);
        Task<bool> UpdatePathwayAsync(int id, PathwayViewModel model);
        Task<bool> DeletePathwayAsync(int id, int userId = 0, string? username = null);
        
        // Product Management
        Task<List<ProductViewModel>> GetProductsAsync();
        Task<ProductViewModel?> GetProductByIdAsync(int id);
        Task<bool> CreateProductAsync(ProductViewModel model);
        Task<bool> UpdateProductAsync(int id, ProductViewModel model);

        /// <summary>
        /// DEF-113: bulk-create products in a single transaction. Pre-fetches Companies,
        /// ProductTypes, and existing (Name+CompanyId) keys; replaces productManagement.js#applySelectedImport()'s
        /// per-row /Admin/CreateProduct loop. Replace-all NOT supported here — too risky for products
        /// (a typo in the import file would mass-deactivate inventory). Updates still go individually.
        /// </summary>
        Task<BulkImportResult> BulkImportProductsAsync(List<ProductViewModel> models);
        Task<bool> DeleteProductAsync(int id, int userId = 0, string? username = null);
        
        // Lookup data
        Task<List<LookupItem>> GetCompaniesAsync();
        Task<List<LookupItem>> GetRolesListAsync();
        
        // Export functionality
        Task<byte[]> ExportDataAsync(string entityType, string format);
        
        // Two-Factor Authentication Management
        Task<bool> Reset2FAAsync(int userId, int adminUserId = 0, string adminUsername = "System");
        Task<bool> Disable2FAAsync(int userId, int adminUserId = 0, string adminUsername = "System");
        Task<bool> Enable2FAAsync(int userId, string twoFactorMethod, int adminUserId = 0, string adminUsername = "System");
    }
}