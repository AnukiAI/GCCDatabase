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

        public async Task<BulkImportResult> BulkImportCompaniesAsync(List<CompanyViewModel> models, int userId, string username, bool overwriteExisting = false)
        {
            // DEF-113: companion to DEF-112's bulk endpoint pattern.
            // Pre-fetch existing names once → in-memory dup detection → batched SaveChanges.
            // Replaces companyManagement.js#applySelectedImport()'s per-row POST loop, which
            // hit AspNetCoreRateLimit's localhost 200/min cap on imports >200 rows.
            var result = new BulkImportResult { TotalProcessed = models.Count };
            var now = DateTime.UtcNow;

            var existingCompanies = await _context.Companies
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            var existingByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in existingCompanies)
            {
                existingByName[c.Name] = c.Id;
            }

            if (overwriteExisting)
            {
                var importNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var m in models)
                {
                    if (!string.IsNullOrWhiteSpace(m.Name)) importNames.Add(m.Name.Trim());
                }
                var toDeactivate = await _context.Companies
                    .Where(c => !c.IsDeleted && c.IsActive)
                    .ToListAsync();
                foreach (var c in toDeactivate)
                {
                    if (!importNames.Contains(c.Name))
                    {
                        c.IsActive = false;
                        c.UpdatedAt = now;
                    }
                }
            }

            var addedCount = 0;
            foreach (var model in models)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Name))
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"(blank) - Name is required");
                        continue;
                    }
                    var name = model.Name.Trim();
                    if (existingByName.ContainsKey(name))
                    {
                        if (overwriteExisting)
                        {
                            // Already exists. In overwrite mode we keep the existing row; count as success.
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"{name} - A company with this name already exists");
                        }
                        continue;
                    }

                    var entity = new Company
                    {
                        Name = name,
                        Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                        Website = string.IsNullOrWhiteSpace(model.Url) ? null : model.Url.Trim(),
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsActive = true,
                        IsDeleted = false
                    };

                    _context.Companies.Add(entity);
                    existingByName[name] = 0; // prevent within-batch dup
                    addedCount++;
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"{model.Name} - {ex.Message}");
                }
            }

            try
            {
                if (_context.ChangeTracker.HasChanges())
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Database save failed: {ex.Message}");
                result.ErrorCount += result.SuccessCount;
                result.SuccessCount = 0;
                return result;
            }

            // Single summary audit entry — per-row audit on bulk imports adds N more saves
            // and is rarely useful for compliance review. The shipped log is the source of truth
            // for bulk-import decisions; per-row audit returns when individual /Company/SaveCompany
            // is called.
            if (addedCount > 0)
            {
                try
                {
                    await _auditService.LogEntityChangeAsync(
                        "BULK",
                        $"BulkImport ({addedCount} companies)",
                        "CREATE",
                        null,
                        new { Count = addedCount, OverwriteExisting = overwriteExisting },
                        userId,
                        username,
                        $"Bulk import: {addedCount} new, {result.ErrorCount} errors",
                        "Web UI"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to log bulk-import audit history: {ex.Message}");
                }
            }

            return result;
        }
    }
}