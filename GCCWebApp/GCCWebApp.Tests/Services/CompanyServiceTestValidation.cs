using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;

namespace GCCWebApp.Tests.Services
{
    /// <summary>
    /// Validation class to verify CompanyService logic without running full test suite
    /// This helps verify our test approach is correct and the service works as expected
    /// </summary>
    public class CompanyServiceTestValidation
    {
        public static async Task<bool> ValidateCompanyServiceLogic()
        {
            try
            {
                // Create in-memory database
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new ApplicationDbContext(options);
                var service = new CompanyService(context);

                // Test 1: Create a company
                var createModel = new CompanyViewModel
                {
                    Name = "Test Company",
                    Email = "test@example.com",
                    Url = "https://test.com"
                };

                var companyId = await service.CreateCompanyAsync(createModel);
                if (companyId <= 0) return false;

                // Test 2: Retrieve the created company
                var retrieved = await service.GetCompanyByIdAsync(companyId);
                if (retrieved == null || retrieved.Name != "Test Company") return false;

                // Test 3: Update the company
                var updateModel = new CompanyViewModel
                {
                    Name = "Updated Company",
                    Email = "updated@example.com",
                    Url = "https://updated.com"
                };

                var updateResult = await service.UpdateCompanyAsync(companyId, updateModel);
                if (!updateResult) return false;

                // Test 4: Verify update
                var updatedCompany = await service.GetCompanyByIdAsync(companyId);
                if (updatedCompany == null || updatedCompany.Name != "Updated Company") return false;

                // Test 5: Get all companies
                var allCompanies = await service.GetCompaniesAsync();
                if (!allCompanies.Any(c => c.Id == companyId)) return false;

                // Test 6: Soft delete
                var deleteResult = await service.DeleteCompanyAsync(companyId);
                if (!deleteResult) return false;

                // Test 7: Verify soft delete - should not appear in GetCompaniesAsync
                var companiesAfterDelete = await service.GetCompaniesAsync();
                if (companiesAfterDelete.Any(c => c.Id == companyId)) return false;

                // Test 8: Verify soft delete - should not appear in GetCompanyByIdAsync
                var deletedCompany = await service.GetCompanyByIdAsync(companyId);
                if (deletedCompany != null) return false;

                // All tests passed
                return true;
            }
            catch (Exception ex)
            {
                // Log exception details if needed for debugging
                Console.WriteLine($"Validation failed with exception: {ex.Message}");
                return false;
            }
        }

        public static async Task<string> GetValidationReport()
        {
            var isValid = await ValidateCompanyServiceLogic();
            
            if (isValid)
            {
                return "✅ CompanyService validation PASSED - All core functionality works correctly";
            }
            else
            {
                return "❌ CompanyService validation FAILED - Check service implementation";
            }
        }
    }
}