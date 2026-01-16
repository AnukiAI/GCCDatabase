using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Services.Audit;

namespace GCCWebApp.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CompanyService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
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

        public async Task<int> CreateCompanyAsync(CompanyViewModel model, int userId, string username)
        {
            // Check for duplicate company name (case-insensitive)
            var existingCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower() && !c.IsDeleted);

            if (existingCompany != null)
            {
                throw new InvalidOperationException($"A company with the name '{model.Name}' already exists.");
            }

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

            // Log audit history
            try
            {
                await _auditService.LogEntityChangeAsync(
                    company.Id.ToString(),
                    company.Name,
                    "CREATE",
                    null,
                    company,
                    userId,
                    username,
                    null,
                    "Web UI"
                );
            }
            catch (Exception ex)
            {
                // Log audit failure but don't fail the operation
                Console.WriteLine($"Failed to log audit history: {ex.Message}");
            }

            return company.Id;
        }

        public async Task<bool> UpdateCompanyAsync(int id, CompanyViewModel model, int userId, string username)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (company == null) return false;

            // Check for duplicate company name (case-insensitive), excluding current company
            var existingCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id != id && c.Name.ToLower() == model.Name.ToLower() && !c.IsDeleted);

            if (existingCompany != null)
            {
                throw new InvalidOperationException($"A company with the name '{model.Name}' already exists.");
            }

            // Capture old state for audit (detached from context)
            var oldCompany = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            company.Name = model.Name;
            company.Email = model.Email;
            company.Website = model.Url;
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log audit history
            try
            {
                await _auditService.LogEntityChangeAsync(
                    company.Id.ToString(),
                    company.Name,
                    "UPDATE",
                    oldCompany,
                    company,
                    userId,
                    username,
                    null,
                    "Web UI"
                );
            }
            catch (Exception ex)
            {
                // Log audit failure but don't fail the operation
                Console.WriteLine($"Failed to log audit history: {ex.Message}");
            }

            return true;
        }

        public async Task<bool> DeleteCompanyAsync(int id, int userId, string username)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (company == null) return false;

            // Capture company state before deletion for audit
            var companyName = company.Name;
            var oldCompany = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            // Soft delete
            company.IsDeleted = true;
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log audit history
            try
            {
                await _auditService.LogEntityChangeAsync(
                    id.ToString(),
                    companyName,
                    "DELETE",
                    oldCompany,
                    null, // Entity is soft deleted, no new state
                    userId,
                    username,
                    "Soft delete - Company marked as deleted",
                    "Web UI"
                );
            }
            catch (Exception ex)
            {
                // Log audit failure but don't fail the operation
                Console.WriteLine($"Failed to log audit history: {ex.Message}");
            }

            return true;
        }
    }
}