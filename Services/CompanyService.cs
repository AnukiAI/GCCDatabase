using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;

        public CompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CompanyViewModel>> GetCompaniesAsync()
        {
            return await _context.Companies
                .Where(c => !c.IsDeleted)
                .Select(c => new CompanyViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email ?? string.Empty,
                    Url = c.Website ?? string.Empty
                })
                .ToListAsync();
        }

        public async Task<CompanyViewModel?> GetCompanyByIdAsync(int id)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (company == null) return null;

            return new CompanyViewModel
            {
                Id = company.Id,
                Name = company.Name,
                Email = company.Email ?? string.Empty,
                Url = company.Website ?? string.Empty
            };
        }

        public async Task<int> CreateCompanyAsync(CompanyViewModel model)
        {
            var company = new Company
            {
                Name = model.Name,
                Email = model.Email,
                Website = model.Url,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return company.Id;
        }

        public async Task<bool> UpdateCompanyAsync(int id, CompanyViewModel model)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (company == null) return false;

            company.Name = model.Name;
            company.Email = model.Email;
            company.Website = model.Url;
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCompanyAsync(int id)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (company == null) return false;

            // Soft delete
            company.IsDeleted = true;
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}