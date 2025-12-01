using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Services.Audit;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GCCWebApp.Tests.Services
{
    public class CompanyServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly CompanyService _service;
        private const int TestUserId = 1;
        private const string TestUsername = "TestUser";

        public CompanyServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _auditServiceMock = new Mock<IAuditService>();
            _service = new CompanyService(_context, _auditServiceMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var companies = new List<Company>
            {
                new Company
                {
                    Id = 1,
                    Name = "Active Company 1",
                    Email = "active1@example.com",
                    Website = "https://active1.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    IsDeleted = false
                },
                new Company
                {
                    Id = 2,
                    Name = "Active Company 2",
                    Email = "active2@example.com",
                    Website = "https://active2.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3),
                    IsDeleted = false
                },
                new Company
                {
                    Id = 3,
                    Name = "Deleted Company",
                    Email = "deleted@example.com",
                    Website = "https://deleted.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    IsDeleted = true
                },
                new Company
                {
                    Id = 4,
                    Name = "Company with Nulls",
                    Email = null,
                    Website = null,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    IsDeleted = false
                }
            };

            _context.Companies.AddRange(companies);
            _context.SaveChanges();
        }

        #region GetCompaniesAsync Tests

        [Fact]
        public async Task GetCompaniesAsync_ShouldReturnOnlyActiveCompanies()
        {
            // Act
            var result = await _service.GetCompaniesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().NotContain(c => c.Name == "Deleted Company");
            result.Should().OnlyContain(c => !string.IsNullOrEmpty(c.Name));
        }

        [Fact]
        public async Task GetCompaniesAsync_ShouldMapPropertiesCorrectly()
        {
            // Act
            var result = await _service.GetCompaniesAsync();

            // Assert
            var company1 = result.FirstOrDefault(c => c.Id == 1);
            company1.Should().NotBeNull();
            company1.Name.Should().Be("Active Company 1");
            company1.Email.Should().Be("active1@example.com");
            company1.Url.Should().Be("https://active1.com");
        }

        [Fact]
        public async Task GetCompaniesAsync_ShouldHandleNullValuesWithEmptyStrings()
        {
            // Act
            var result = await _service.GetCompaniesAsync();

            // Assert
            var companyWithNulls = result.FirstOrDefault(c => c.Id == 4);
            companyWithNulls.Should().NotBeNull();
            companyWithNulls.Email.Should().Be(string.Empty);
            companyWithNulls.Url.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GetCompaniesAsync_WhenNoCompanies_ShouldReturnEmptyList()
        {
            // Arrange
            await ClearAllCompanies();

            // Act
            var result = await _service.GetCompaniesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetCompanyByIdAsync Tests

        [Fact]
        public async Task GetCompanyByIdAsync_ValidActiveId_ShouldReturnCompany()
        {
            // Act
            var result = await _service.GetCompanyByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Active Company 1");
            result.Email.Should().Be("active1@example.com");
            result.Url.Should().Be("https://active1.com");
        }

        [Fact]
        public async Task GetCompanyByIdAsync_DeletedCompanyId_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetCompanyByIdAsync(3);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCompanyByIdAsync_NonExistentId_ShouldReturnNull()
        {
            // Act
            var result = await _service.GetCompanyByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetCompanyByIdAsync_ShouldHandleNullFieldsCorrectly()
        {
            // Act
            var result = await _service.GetCompanyByIdAsync(4);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(string.Empty);
            result.Url.Should().Be(string.Empty);
        }

        #endregion

        #region CreateCompanyAsync Tests

        [Fact]
        public async Task CreateCompanyAsync_ValidModel_ShouldCreateCompany()
        {
            // Arrange
            var model = new CompanyViewModel
            {
                Name = "New Test Company",
                Email = "newtest@example.com",
                Url = "https://newtest.com"
            };

            // Act
            var resultId = await _service.CreateCompanyAsync(model, TestUserId, TestUsername);

            // Assert
            resultId.Should().BeGreaterThan(0);

            var createdCompany = await _context.Companies.FindAsync(resultId);
            createdCompany.Should().NotBeNull();
            createdCompany.Name.Should().Be(model.Name);
            createdCompany.Email.Should().Be(model.Email);
            createdCompany.Website.Should().Be(model.Url);
            createdCompany.IsDeleted.Should().BeFalse();
            createdCompany.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            createdCompany.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task CreateCompanyAsync_ShouldReturnNewCompanyId()
        {
            // Arrange
            var model = new CompanyViewModel
            {
                Name = "Another New Company",
                Email = "another@example.com",
                Url = "https://another.com"
            };

            // Act
            var resultId = await _service.CreateCompanyAsync(model, TestUserId, TestUsername);

            // Assert
            resultId.Should().BeGreaterThan(0);
            var company = await _context.Companies.FindAsync(resultId);
            company.Id.Should().Be(resultId);
        }

        [Fact]
        public async Task CreateCompanyAsync_WithMinimalData_ShouldCreateCompany()
        {
            // Arrange
            var model = new CompanyViewModel
            {
                Name = "Minimal Company",
                Email = string.Empty,
                Url = string.Empty
            };

            // Act
            var resultId = await _service.CreateCompanyAsync(model, TestUserId, TestUsername);

            // Assert
            resultId.Should().BeGreaterThan(0);
            var company = await _context.Companies.FindAsync(resultId);
            company.Name.Should().Be("Minimal Company");
            company.Email.Should().Be(string.Empty);
            company.Website.Should().Be(string.Empty);
        }

        [Fact]
        public async Task CreateCompanyAsync_MultipleCalls_ShouldCreateUniqueCompanies()
        {
            // Arrange
            var model1 = new CompanyViewModel
            {
                Name = "Company One",
                Email = "one@example.com",
                Url = "https://one.com"
            };

            var model2 = new CompanyViewModel
            {
                Name = "Company Two",
                Email = "two@example.com",
                Url = "https://two.com"
            };

            // Act
            var id1 = await _service.CreateCompanyAsync(model1, TestUserId, TestUsername);
            var id2 = await _service.CreateCompanyAsync(model2, TestUserId, TestUsername);

            // Assert
            id1.Should().NotBe(id2);
            var company1 = await _context.Companies.FindAsync(id1);
            var company2 = await _context.Companies.FindAsync(id2);
            company1.Name.Should().NotBe(company2.Name);
        }

        #endregion

        #region UpdateCompanyAsync Tests

        [Fact]
        public async Task UpdateCompanyAsync_ValidId_ShouldUpdateCompany()
        {
            // Arrange
            var model = new CompanyViewModel
            {
                Name = "Updated Company Name",
                Email = "updated@example.com",
                Url = "https://updated.com"
            };

            var originalUpdatedAt = _context.Companies.Find(1).UpdatedAt;
            await Task.Delay(10); // Small delay to ensure timestamp difference

            // Act
            var result = await _service.UpdateCompanyAsync(1, model, TestUserId, TestUsername);

            // Assert
            result.Should().BeTrue();

            var updatedCompany = await _context.Companies.FindAsync(1);
            updatedCompany.Name.Should().Be(model.Name);
            updatedCompany.Email.Should().Be(model.Email);
            updatedCompany.Website.Should().Be(model.Url);
            updatedCompany.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        }

        [Fact]
        public async Task UpdateCompanyAsync_NonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var model = new CompanyViewModel
            {
                Name = "Update Attempt",
                Email = "update@example.com",
                Url = "https://update.com"
            };

            // Act
            var result = await _service.UpdateCompanyAsync(999, model, TestUserId, TestUsername);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateCompanyAsync_DeletedCompanyId_ShouldReturnFalse()
        {
            // Arrange
            var model = new CompanyViewModel
            {
                Name = "Update Deleted",
                Email = "deleted@example.com",
                Url = "https://deleted.com"
            };

            // Act
            var result = await _service.UpdateCompanyAsync(3, model, TestUserId, TestUsername);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateCompanyAsync_PartialUpdate_ShouldUpdateOnlyProvidedFields()
        {
            // Arrange
            var originalCompany = await _context.Companies.FindAsync(1);
            var originalCreatedAt = originalCompany.CreatedAt;

            var model = new CompanyViewModel
            {
                Name = "Only Name Updated",
                Email = originalCompany.Email,
                Url = originalCompany.Website
            };

            // Act
            var result = await _service.UpdateCompanyAsync(1, model, TestUserId, TestUsername);

            // Assert
            result.Should().BeTrue();

            var updatedCompany = await _context.Companies.FindAsync(1);
            updatedCompany.Name.Should().Be("Only Name Updated");
            updatedCompany.Email.Should().Be(originalCompany.Email);
            updatedCompany.Website.Should().Be(originalCompany.Website);
            updatedCompany.CreatedAt.Should().Be(originalCreatedAt); // Should not change
        }

        #endregion

        #region DeleteCompanyAsync Tests

        [Fact]
        public async Task DeleteCompanyAsync_ValidId_ShouldSoftDeleteCompany()
        {
            // Act
            var result = await _service.DeleteCompanyAsync(1, TestUserId, TestUsername);

            // Assert
            result.Should().BeTrue();

            var deletedCompany = await _context.Companies.FindAsync(1);
            deletedCompany.Should().NotBeNull();
            deletedCompany.IsDeleted.Should().BeTrue();
            deletedCompany.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DeleteCompanyAsync_NonExistentId_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DeleteCompanyAsync(999, TestUserId, TestUsername);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCompanyAsync_AlreadyDeletedCompany_ShouldReturnFalse()
        {
            // Act
            var result = await _service.DeleteCompanyAsync(3, TestUserId, TestUsername);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteCompanyAsync_ShouldNotActuallyRemoveFromDatabase()
        {
            // Act
            await _service.DeleteCompanyAsync(2, TestUserId, TestUsername);

            // Assert
            var allCompanies = await _context.Companies.ToListAsync();
            allCompanies.Should().Contain(c => c.Id == 2);

            var deletedCompany = allCompanies.First(c => c.Id == 2);
            deletedCompany.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteCompanyAsync_DeletedCompany_ShouldNotAppearInGetCompanies()
        {
            // Act
            await _service.DeleteCompanyAsync(1, TestUserId, TestUsername);
            var companies = await _service.GetCompaniesAsync();

            // Assert
            companies.Should().NotContain(c => c.Id == 1);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CreateUpdateDelete_FullLifecycle_ShouldWorkCorrectly()
        {
            // Create
            var createModel = new CompanyViewModel
            {
                Name = "Lifecycle Test Company",
                Email = "lifecycle@example.com",
                Url = "https://lifecycle.com"
            };

            var companyId = await _service.CreateCompanyAsync(createModel, TestUserId, TestUsername);
            companyId.Should().BeGreaterThan(0);

            // Verify creation
            var createdCompany = await _service.GetCompanyByIdAsync(companyId);
            createdCompany.Should().NotBeNull();
            createdCompany.Name.Should().Be("Lifecycle Test Company");

            // Update
            var updateModel = new CompanyViewModel
            {
                Name = "Updated Lifecycle Company",
                Email = "updated.lifecycle@example.com",
                Url = "https://updated-lifecycle.com"
            };

            var updateResult = await _service.UpdateCompanyAsync(companyId, updateModel, TestUserId, TestUsername);
            updateResult.Should().BeTrue();

            // Verify update
            var updatedCompany = await _service.GetCompanyByIdAsync(companyId);
            updatedCompany.Name.Should().Be("Updated Lifecycle Company");
            updatedCompany.Email.Should().Be("updated.lifecycle@example.com");

            // Delete
            var deleteResult = await _service.DeleteCompanyAsync(companyId, TestUserId, TestUsername);
            deleteResult.Should().BeTrue();

            // Verify soft delete
            var deletedCompany = await _service.GetCompanyByIdAsync(companyId);
            deletedCompany.Should().BeNull();

            // Verify company still exists in database but is marked as deleted
            var dbCompany = await _context.Companies.FindAsync(companyId);
            dbCompany.Should().NotBeNull();
            dbCompany.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task ConcurrentOperations_ShouldHandleCorrectly()
        {
            // Arrange
            var tasks = new List<Task<int>>();

            // Act - Create multiple companies concurrently
            for (int i = 0; i < 5; i++)
            {
                var model = new CompanyViewModel
                {
                    Name = $"Concurrent Company {i}",
                    Email = $"concurrent{i}@example.com",
                    Url = $"https://concurrent{i}.com"
                };
                tasks.Add(_service.CreateCompanyAsync(model, TestUserId, TestUsername));
            }

            var ids = await Task.WhenAll(tasks);

            // Assert
            ids.Should().HaveCount(5);
            ids.Should().OnlyHaveUniqueItems();

            var companies = await _service.GetCompaniesAsync();
            var concurrentCompanies = companies.Where(c => c.Name.StartsWith("Concurrent Company"));
            concurrentCompanies.Should().HaveCount(5);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task GetCompaniesAsync_WithLargeDataset_ShouldPerformEfficiently()
        {
            // Arrange - Add many companies
            var companies = new List<Company>();
            for (int i = 0; i < 100; i++)
            {
                companies.Add(new Company
                {
                    Name = $"Company {i}",
                    Email = $"company{i}@example.com",
                    Website = $"https://company{i}.com",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = i % 10 == 0 // Mark every 10th company as deleted
                });
            }
            _context.Companies.AddRange(companies);
            await _context.SaveChangesAsync();

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _service.GetCompaniesAsync();
            var duration = DateTime.UtcNow - startTime;

            // Assert
            result.Should().HaveCountGreaterThan(90);
            duration.Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task CreateCompanyAsync_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var model = new CompanyViewModel
            {
                Name = "Company & Co. \"Special\" <Characters>",
                Email = "special+chars@example.com",
                Url = "https://special-chars.com/path?query=value&param=test"
            };

            // Act
            var companyId = await _service.CreateCompanyAsync(model, TestUserId, TestUsername);

            // Assert
            companyId.Should().BeGreaterThan(0);
            var company = await _context.Companies.FindAsync(companyId);
            company.Name.Should().Be("Company & Co. \"Special\" <Characters>");
            company.Email.Should().Be("special+chars@example.com");
        }

        [Fact]
        public async Task UpdateCompanyAsync_WithEmptyStrings_ShouldUpdateToEmpty()
        {
            // Arrange
            var model = new CompanyViewModel
            {
                Name = "Updated Name",
                Email = "",
                Url = ""
            };

            // Act
            var result = await _service.UpdateCompanyAsync(1, model, TestUserId, TestUsername);

            // Assert
            result.Should().BeTrue();
            var company = await _context.Companies.FindAsync(1);
            company.Email.Should().Be("");
            company.Website.Should().Be("");
        }

        #endregion

        #region Helper Methods

        private async Task ClearAllCompanies()
        {
            var companies = await _context.Companies.ToListAsync();
            _context.Companies.RemoveRange(companies);
            await _context.SaveChangesAsync();
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}