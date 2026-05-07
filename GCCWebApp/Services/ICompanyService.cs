using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface ICompanyService
    {
        Task<List<CompanyViewModel>> GetCompaniesAsync();
        Task<CompanyViewModel?> GetCompanyByIdAsync(int id);
        Task<int> CreateCompanyAsync(CompanyViewModel model, int userId, string username);
        Task<bool> UpdateCompanyAsync(int id, CompanyViewModel model, int userId, string username);
        Task<bool> DeleteCompanyAsync(int id, int userId, string username);

        /// <summary>
        /// DEF-113: bulk-create companies in a single transaction. Pre-fetches existing
        /// names for in-memory dup detection, batches one SaveChanges. Replaces the
        /// per-row POST loop in companyManagement.js#applySelectedImport().
        /// Skips per-row audit logging — single summary audit entry written instead.
        /// </summary>
        Task<BulkImportResult> BulkImportCompaniesAsync(List<CompanyViewModel> models, int userId, string username, bool overwriteExisting = false);
    }
}