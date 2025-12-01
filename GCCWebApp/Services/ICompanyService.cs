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
    }
}