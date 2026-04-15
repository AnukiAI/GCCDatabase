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
        Task<bool> UpdateUserAsync(int id, UserViewModel model, int userId, string username);
        Task<bool> DeleteUserAsync(int id, int userId, string username);
        
        // Role Management
        Task<List<RoleViewModel>> GetRolesAsync();
        Task<RoleViewModel?> GetRoleByIdAsync(int id);
        Task<bool> CreateRoleAsync(RoleViewModel model);
        Task<bool> UpdateRoleAsync(int id, RoleViewModel model);
        Task<bool> DeleteRoleAsync(int id);
        Task<List<PermissionViewModel>> GetPermissionsAsync();
        
        // Product Types
        Task<List<ProductTypeViewModel>> GetProductTypesAsync();
        Task<ProductTypeViewModel?> GetProductTypeByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage, ProductTypeViewModel? Data)> CreateProductTypeAsync(ProductTypeViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateProductTypeAsync(ProductTypeViewModel model);
        Task<(bool Success, string Message)> DeleteProductTypeAsync(int id);
        Task<(bool Success, string Message)> DeleteProductCategoryAsync(int id);

        // Certificate Types
        Task<List<CertificateTypeViewModel>> GetCertificateTypesAsync();
        Task<CertificateTypeViewModel?> GetCertificateTypeByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateCertificateTypeAsync(CertificateTypeViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateCertificateTypeAsync(int id, CertificateTypeViewModel model);
        Task<bool> DeleteCertificateTypeAsync(int id);
        
        // Certificate Fields
        Task<List<CertificateFieldViewModel>> GetCertificateFieldsAsync();
        Task<CertificateFieldViewModel?> GetCertificateFieldByIdAsync(int id);
        Task<(bool Success, string? ErrorMessage)> CreateCertificateFieldAsync(CertificateFieldViewModel model);
        Task<(bool Success, string? ErrorMessage)> UpdateCertificateFieldAsync(int id, CertificateFieldViewModel model);
        Task<bool> DeleteCertificateFieldAsync(int id);

        // CSF Attributes
        Task<List<CSFAttributeViewModel>> GetCSFAttributesAsync();
        Task<CSFAttributeViewModel?> GetCSFAttributeByIdAsync(int id);
        Task<bool> CreateCSFAttributeAsync(CSFAttributeViewModel model);
        Task<bool> UpdateCSFAttributeAsync(int id, CSFAttributeViewModel model);
        Task<bool> DeleteCSFAttributeAsync(int id);

        // Tags
        Task<List<TagViewModel>> GetTagsAsync();
        Task<TagViewModel?> GetTagByIdAsync(int id);
        Task<bool> CreateTagAsync(TagViewModel model);
        Task<bool> UpdateTagAsync(int id, TagViewModel model);
        Task<bool> DeleteTagAsync(int id);
        
        // Partners
        Task<List<PartnerViewModel>> GetPartnersAsync();
        Task<PartnerViewModel?> GetPartnerByIdAsync(int id);
        Task<bool> CreatePartnerAsync(PartnerViewModel model);
        Task<bool> UpdatePartnerAsync(int id, PartnerViewModel model);
        Task<bool> DeletePartnerAsync(int id);

        // Pathways
        Task<List<PathwayViewModel>> GetPathwaysAsync();
        Task<PathwayViewModel?> GetPathwayByIdAsync(int id);
        Task<bool> CreatePathwayAsync(PathwayViewModel model);
        Task<bool> UpdatePathwayAsync(int id, PathwayViewModel model);
        Task<bool> DeletePathwayAsync(int id);
        
        // Product Management
        Task<List<ProductViewModel>> GetProductsAsync();
        Task<ProductViewModel?> GetProductByIdAsync(int id);
        Task<bool> CreateProductAsync(ProductViewModel model);
        Task<bool> UpdateProductAsync(int id, ProductViewModel model);
        Task<bool> DeleteProductAsync(int id);
        
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