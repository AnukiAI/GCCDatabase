using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Services.Audit;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    public class AdminServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<AdminService>> _loggerMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly AdminService _adminService;

        public AdminServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<AdminService>>();
            _auditServiceMock = new Mock<IAuditService>();
            _adminService = new AdminService(_context, _loggerMock.Object, _auditServiceMock.Object);

            // Seed base test data
            SeedProductTypeTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedProductTypeTestData()
        {
            // Create a ProductCategory (required for ProductType)
            var category = new ProductCategory
            {
                Id = 1,
                Name = "Test Category",
                Description = "Test category for unit tests",
                IsActive = true,
                SortOrder = 0
            };
            _context.ProductCategories.Add(category);

            // Create a parent ProductType (Level 0 - Category level)
            var parentProductType = new ProductType
            {
                Id = 1,
                Name = "Parent Type",
                CategoryId = 1,
                ParentId = null,
                Level = 0,
                SortOrder = 0,
                IsActive = true
            };
            _context.ProductTypes.Add(parentProductType);

            // Create a child ProductType (Level 1 - Type level)
            var childProductType = new ProductType
            {
                Id = 2,
                Name = "Child Type",
                CategoryId = 1,
                ParentId = 1,
                Level = 1,
                SortOrder = 0,
                IsActive = true
            };
            _context.ProductTypes.Add(childProductType);

            // Create a ProductType with no children for delete testing
            var leafProductType = new ProductType
            {
                Id = 3,
                Name = "Leaf Type",
                CategoryId = 1,
                ParentId = 2,
                Level = 2,
                SortOrder = 0,
                IsActive = true
            };
            _context.ProductTypes.Add(leafProductType);

            _context.SaveChanges();
        }

        #region CreateProductTypeAsync Tests

        [Fact]
        public async Task CreateProductTypeAsync_WithValidCategory_SetsDefaultCategoryId()
        {
            // Arrange
            var model = new ProductTypeViewModel
            {
                Name = "New Category",
                ParentId = null,
                SortOrder = 1
            };

            // Act
            var result = await _adminService.CreateProductTypeAsync(model);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("New Category");
            result.Data.Level.Should().Be(0); // Root level

            // Verify CategoryId was set in database
            var created = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Name == "New Category");
            created.Should().NotBeNull();
            created!.CategoryId.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreateProductTypeAsync_WithParent_InheritsCategoryIdFromParent()
        {
            // Arrange
            var model = new ProductTypeViewModel
            {
                Name = "New Child Type",
                ParentId = 1, // Parent Type
                SortOrder = 1
            };

            // Act
            var result = await _adminService.CreateProductTypeAsync(model);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("New Child Type");
            result.Data.Level.Should().Be(1); // One level below parent
            result.Data.ParentId.Should().Be(1);

            // Verify CategoryId was inherited from parent
            var created = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Name == "New Child Type");
            created.Should().NotBeNull();
            created!.CategoryId.Should().Be(1); // Same as parent
        }

        [Fact]
        public async Task CreateProductTypeAsync_WithNoExistingCategory_CreatesDefaultCategory()
        {
            // Arrange - Use a fresh context with no categories
            var freshContext = TestDbContextFactory.CreateInMemoryContext("NoCategoryDb");
            var service = new AdminService(freshContext, _loggerMock.Object, _auditServiceMock.Object);

            var model = new ProductTypeViewModel
            {
                Name = "First Category Ever",
                ParentId = null,
                SortOrder = 0
            };

            // Act
            var result = await service.CreateProductTypeAsync(model);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("First Category Ever");

            // Verify a matching category was created with the same name
            var categories = await freshContext.ProductCategories.ToListAsync();
            categories.Should().HaveCountGreaterThan(0);
            categories.First().Name.Should().Be("First Category Ever");

            freshContext.Dispose();
        }

        [Fact]
        public async Task CreateProductTypeAsync_SetsCorrectLevelBasedOnParent()
        {
            // Arrange - Create a subcategory under the child type (level 2)
            var model = new ProductTypeViewModel
            {
                Name = "Subcategory Test",
                ParentId = 2, // Child Type is Level 1
                SortOrder = 0
            };

            // Act
            var result = await _adminService.CreateProductTypeAsync(model);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Level.Should().Be(2); // Parent is Level 1, so this should be Level 2
        }

        #endregion

        #region DeleteProductTypeAsync Tests

        [Fact]
        public async Task DeleteProductTypeAsync_WithChildren_ReturnsErrorMessage()
        {
            // Arrange - Parent Type (Id=1) has Child Type (Id=2) as a child

            // Act
            var (success, message) = await _adminService.DeleteProductTypeAsync(1);

            // Assert
            success.Should().BeFalse();
            message.Should().Contain("Cannot delete");
            message.Should().Contain("types"); // Level 0 children are called "types"
        }

        [Fact]
        public async Task DeleteProductTypeAsync_WithNoChildren_Succeeds()
        {
            // Arrange - Leaf Type (Id=3) has no children

            // Act
            var (success, message) = await _adminService.DeleteProductTypeAsync(3);

            // Assert
            success.Should().BeTrue();
            message.Should().Contain("deleted successfully");

            // Verify it's actually deleted
            var deleted = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Id == 3);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteProductTypeAsync_WithAssignedProducts_ReturnsErrorMessage()
        {
            // Arrange - Add a product assigned to the leaf type
            var product = new Product
            {
                Id = 100,
                Name = "Test Product",
                CategoryId = 1,
                ProductTypeId = 3, // Leaf Type
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var (success, message) = await _adminService.DeleteProductTypeAsync(3);

            // Assert
            success.Should().BeFalse();
            message.Should().Contain("Cannot delete");
            message.Should().Contain("product");
        }

        [Fact]
        public async Task DeleteProductTypeAsync_WithNonExistentId_ReturnsNotFound()
        {
            // Act
            var (success, message) = await _adminService.DeleteProductTypeAsync(9999);

            // Assert
            success.Should().BeFalse();
            message.Should().Contain("not found");
        }

        #endregion

        #region CreateProductAsync Tests

        [Fact]
        public async Task CreateProductAsync_WithValidData_SetsDefaultCategoryId()
        {
            // Arrange
            var model = new ProductViewModel
            {
                Name = "New Test Product",
                Description = "Test product description",
                IsActive = true
            };

            // Act
            var result = await _adminService.CreateProductAsync(model);

            // Assert
            result.Should().BeTrue();

            // Verify CategoryId was set
            var created = await _context.Products.FirstOrDefaultAsync(p => p.Name == "New Test Product");
            created.Should().NotBeNull();
            created!.CategoryId.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreateProductAsync_WithProductType_InheritsCategoryIdFromProductType()
        {
            // Arrange
            var model = new ProductViewModel
            {
                Name = "Product With Type",
                Description = "Test product with product type",
                ProductTypeName = "Parent Type", // This has CategoryId = 1
                IsActive = true
            };

            // Act
            var result = await _adminService.CreateProductAsync(model);

            // Assert
            result.Should().BeTrue();

            // Verify CategoryId was inherited from ProductType
            var created = await _context.Products.FirstOrDefaultAsync(p => p.Name == "Product With Type");
            created.Should().NotBeNull();
            created!.CategoryId.Should().Be(1); // Inherited from Parent Type
            created.ProductTypeId.Should().Be(1); // Parent Type's Id
        }

        [Fact]
        public async Task CreateProductAsync_WithCompany_SetsCompanyId()
        {
            // Arrange - Add a company
            var company = new Company
            {
                Id = 10,
                Name = "Test Company",
                Email = "test@company.com"
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var model = new ProductViewModel
            {
                Name = "Product With Company",
                Description = "Test product with company",
                CompanyName = "Test Company",
                IsActive = true
            };

            // Act
            var result = await _adminService.CreateProductAsync(model);

            // Assert
            result.Should().BeTrue();

            var created = await _context.Products.FirstOrDefaultAsync(p => p.Name == "Product With Company");
            created.Should().NotBeNull();
            created!.CompanyId.Should().Be(10);
        }

        #endregion

        #region GetProductTypesAsync Tests

        [Fact]
        public async Task GetProductTypesAsync_ReturnsHierarchicalStructure()
        {
            // Act
            var result = await _adminService.GetProductTypesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);

            // Root level items should have no parent
            var rootItems = result.Where(pt => pt.ParentId == null).ToList();
            rootItems.Should().HaveCountGreaterThan(0);
        }

        #endregion

        #region UpdateProductTypeAsync Tests

        [Fact]
        public async Task UpdateProductTypeAsync_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var model = new ProductTypeViewModel
            {
                Id = 3, // Leaf Type
                Name = "Updated Leaf Type",
                ParentId = 2,
                SortOrder = 5
            };

            // Act
            var result = await _adminService.UpdateProductTypeAsync(model);

            // Assert
            result.Success.Should().BeTrue();

            var updated = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Id == 3);
            updated.Should().NotBeNull();
            updated!.Name.Should().Be("Updated Leaf Type");
            updated.SortOrder.Should().Be(5);
        }

        [Fact]
        public async Task UpdateProductTypeAsync_WithNonExistentId_ReturnsFalse()
        {
            // Arrange
            var model = new ProductTypeViewModel
            {
                Id = 9999,
                Name = "Non-existent",
                SortOrder = 0
            };

            // Act
            var result = await _adminService.UpdateProductTypeAsync(model);

            // Assert
            result.Success.Should().BeFalse();
        }

        #endregion
    }
}
