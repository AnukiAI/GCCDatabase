using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface ICompanyService
    {
        Task<List<CompanyViewModel>> GetCompaniesAsync();
        Task<CompanyViewModel?> GetCompanyByIdAsync(int id);
        Task<int> CreateCompanyAsync(CompanyViewModel model);
        Task<bool> UpdateCompanyAsync(int id, CompanyViewModel model);
        Task<bool> DeleteCompanyAsync(int id);
    }
}