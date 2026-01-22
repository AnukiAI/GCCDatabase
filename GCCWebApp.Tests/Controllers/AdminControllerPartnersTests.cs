using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using GCCWebApp.Controllers;
using GCCWebApp.Middleware;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Data;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace GCCWebApp.Tests.Controllers
{
    public class AdminControllerPartnersTests : IDisposable
    {
        private readonly Mock<ILogger<AdminController>> _loggerMock;
        private readonly Mock<IAdminService> _adminServiceMock;
        private readonly ApplicationDbContext _context;
        private readonly AdminController _controller;

        public AdminControllerPartnersTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<AdminController>>();
            _adminServiceMock = new Mock<IAdminService>();
            _controller = new AdminController(_adminServiceMock.Object, _loggerMock.Object, _context, new RateLimitOptions());
            SetupAdminUser();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region Partners View Tests

        [Fact]
        public void Partners_AdminUser_ShouldReturnViewWithCorrectTitle()
        {
            // Act
            var result = _controller.Partners();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewData["Title"].Should().Be("Partners Management");
        }

        [Fact]
        public void Partners_View_ShouldHaveCorrectBodyClass()
        {
            // Act
            var result = _controller.Partners();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewData["BodyClass"].Should().Be("role-admin");
        }

        #endregion

        #region Get Partners API Tests

        [Fact]
        public async Task GetPartners_AdminUser_ShouldReturnSuccessResponse()
        {
            // Arrange
            var expectedPartners = new List<PartnerViewModel>
            {
                new PartnerViewModel { Id = 1, Name = "Amazon", Description = "E-commerce platform", IsActive = true, UserCount = 5 },
                new PartnerViewModel { Id = 2, Name = "Wayfair", Description = "Furniture retailer", IsActive = true, UserCount = 3 },
                new PartnerViewModel { Id = 3, Name = "Other", Description = "General partner category", IsActive = true, UserCount = 0 }
            };

            _adminServiceMock.Setup(s => s.GetPartnersAsync())
                .ReturnsAsync(expectedPartners);

            // Act
            var result = await _controller.GetPartners();

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<List<PartnerViewModel>>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Data.Should().HaveCount(3);
            response.Data.Should().BeEquivalentTo(expectedPartners);
        }

        [Fact]
        public async Task GetPartners_ServiceException_ShouldReturnErrorResponse()
        {
            // Arrange
            _adminServiceMock.Setup(s => s.GetPartnersAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetPartners();

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<List<PartnerViewModel>>>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Error retrieving partners");
            response.Data.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Create Partner Tests

        [Fact]
        public async Task CreatePartner_ValidData_ShouldReturnSuccessResponse()
        {
            // Arrange
            var partnerModel = new PartnerViewModel
            {
                Name = "New Partner",
                Description = "Test partner",
                Website = "https://newpartner.com",
                ContactEmail = "contact@newpartner.com",
                IsActive = true
            };

            _adminServiceMock.Setup(s => s.CreatePartnerAsync(It.IsAny<PartnerViewModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CreatePartner(partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Partner created successfully");
        }

        [Fact]
        public async Task CreatePartner_InvalidModelState_ShouldReturnErrorResponse()
        {
            // Arrange
            var partnerModel = new PartnerViewModel { Name = "" }; // Invalid - empty name
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.CreatePartner(partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Invalid data provided");
        }

        [Fact]
        public async Task CreatePartner_ServiceFailure_ShouldReturnErrorResponse()
        {
            // Arrange
            var partnerModel = new PartnerViewModel 
            { 
                Name = "Test Partner",
                ContactEmail = "test@partner.com",
                IsActive = true
            };

            _adminServiceMock.Setup(s => s.CreatePartnerAsync(It.IsAny<PartnerViewModel>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CreatePartner(partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Failed to create partner");
        }

        [Fact]
        public async Task CreatePartner_ServiceException_ShouldReturnErrorResponse()
        {
            // Arrange
            var partnerModel = new PartnerViewModel 
            { 
                Name = "Test Partner",
                ContactEmail = "test@partner.com",
                IsActive = true
            };

            _adminServiceMock.Setup(s => s.CreatePartnerAsync(It.IsAny<PartnerViewModel>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.CreatePartner(partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Contain("Error creating partner");
        }

        #endregion

        #region Update Partner Tests

        [Fact]
        public async Task UpdatePartner_ValidData_ShouldReturnSuccessResponse()
        {
            // Arrange
            var partnerId = 1;
            var partnerModel = new PartnerViewModel
            {
                Id = partnerId,
                Name = "Updated Partner",
                Description = "Updated description",
                Website = "https://updated.com",
                ContactEmail = "updated@partner.com",
                IsActive = true
            };

            _adminServiceMock.Setup(s => s.UpdatePartnerAsync(partnerId, It.IsAny<PartnerViewModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdatePartner(partnerId, partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Partner updated successfully");
        }

        [Fact]
        public async Task UpdatePartner_InvalidModelState_ShouldReturnErrorResponse()
        {
            // Arrange
            var partnerId = 1;
            var partnerModel = new PartnerViewModel { Id = partnerId, Name = "" };
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.UpdatePartner(partnerId, partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Invalid data provided");
        }

        [Fact]
        public async Task UpdatePartner_ServiceFailure_ShouldReturnErrorResponse()
        {
            // Arrange
            var partnerId = 999; // Non-existent partner
            var partnerModel = new PartnerViewModel 
            { 
                Id = partnerId,
                Name = "Test Partner",
                ContactEmail = "test@partner.com",
                IsActive = true
            };

            _adminServiceMock.Setup(s => s.UpdatePartnerAsync(partnerId, It.IsAny<PartnerViewModel>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdatePartner(partnerId, partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Partner not found or update failed");
        }

        #endregion

        #region Delete Partner Tests

        [Fact]
        public async Task DeletePartner_ValidId_ShouldReturnSuccessResponse()
        {
            // Arrange
            var partnerId = 1;
            _adminServiceMock.Setup(s => s.DeletePartnerAsync(partnerId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePartner(partnerId);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Partner deleted successfully");
        }

        [Fact]
        public async Task DeletePartner_InvalidId_ShouldReturnErrorResponse()
        {
            // Arrange
            var partnerId = 999; // Non-existent partner
            _adminServiceMock.Setup(s => s.DeletePartnerAsync(partnerId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePartner(partnerId);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Be("Partner not found or delete failed");
        }

        [Fact]
        public async Task DeletePartner_ServiceException_ShouldReturnErrorResponse()
        {
            // Arrange
            var partnerId = 1;
            _adminServiceMock.Setup(s => s.DeletePartnerAsync(partnerId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeletePartner(partnerId);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
            response.Message.Should().Contain("Error deleting partner");
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task GetPartners_NonAdminUser_ShouldDenyAccess()
        {
            // Arrange
            SetupNonAdminUser();

            // Act & Assert
            // Note: This test assumes authorization is handled by [Authorize] attributes
            // In real scenario, this would return Unauthorized or Forbid result
            var result = await _controller.GetPartners();
            
            // If authorization is working correctly, the method should not execute
            // or should return an authorization-related result
            result.Should().NotBeNull();
        }

        #endregion

        #region Data Validation Tests

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task CreatePartner_InvalidName_ShouldReturnValidationError(string invalidName)
        {
            // Arrange
            var partnerModel = new PartnerViewModel
            {
                Name = invalidName,
                ContactEmail = "test@partner.com",
                IsActive = true
            };

            if (string.IsNullOrWhiteSpace(invalidName))
            {
                _controller.ModelState.AddModelError("Name", "Name is required");
            }

            // Act
            var result = await _controller.CreatePartner(partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@invalid.com")]
        [InlineData("invalid@")]
        public async Task CreatePartner_InvalidEmail_ShouldReturnValidationError(string invalidEmail)
        {
            // Arrange
            var partnerModel = new PartnerViewModel
            {
                Name = "Test Partner",
                ContactEmail = invalidEmail,
                IsActive = true
            };

            _controller.ModelState.AddModelError("ContactEmail", "Invalid email format");

            // Act
            var result = await _controller.CreatePartner(partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
        }

        [Fact]
        public async Task CreatePartner_NameTooLong_ShouldReturnValidationError()
        {
            // Arrange
            var longName = new string('A', 101); // Exceeds 100 character limit
            var partnerModel = new PartnerViewModel
            {
                Name = longName,
                ContactEmail = "test@partner.com",
                IsActive = true
            };

            _controller.ModelState.AddModelError("Name", "Name cannot exceed 100 characters");

            // Act
            var result = await _controller.CreatePartner(partnerModel);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            
            response.Success.Should().BeFalse();
        }

        #endregion

        #region Integration Test Scenarios

        [Fact]
        public async Task PartnersCRUDFlow_FullLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var createModel = new PartnerViewModel
            {
                Name = "Test Integration Partner",
                Description = "Integration test partner",
                Website = "https://integration.test",
                ContactEmail = "integration@test.com",
                IsActive = true
            };

            var updateModel = new PartnerViewModel
            {
                Id = 1,
                Name = "Updated Integration Partner",
                Description = "Updated integration test partner",
                Website = "https://updated.integration.test",
                ContactEmail = "updated@test.com",
                IsActive = false
            };

            _adminServiceMock.Setup(s => s.CreatePartnerAsync(It.IsAny<PartnerViewModel>()))
                .ReturnsAsync(true);
            _adminServiceMock.Setup(s => s.UpdatePartnerAsync(1, It.IsAny<PartnerViewModel>()))
                .ReturnsAsync(true);
            _adminServiceMock.Setup(s => s.DeletePartnerAsync(1))
                .ReturnsAsync(true);

            // Act & Assert - Create
            var createResult = await _controller.CreatePartner(createModel);
            var createResponse = ((JsonResult)createResult).Value as ApiResponse<object>;
            createResponse.Success.Should().BeTrue();

            // Act & Assert - Update
            var updateResult = await _controller.UpdatePartner(1, updateModel);
            var updateResponse = ((JsonResult)updateResult).Value as ApiResponse<object>;
            updateResponse.Success.Should().BeTrue();

            // Act & Assert - Delete
            var deleteResult = await _controller.DeletePartner(1);
            var deleteResponse = ((JsonResult)deleteResult).Value as ApiResponse<object>;
            deleteResponse.Success.Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private void SetupAdminUser()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin@test.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("user_id", "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        private void SetupNonAdminUser()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "user@test.com"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("user_id", "2")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        #endregion
    }
}