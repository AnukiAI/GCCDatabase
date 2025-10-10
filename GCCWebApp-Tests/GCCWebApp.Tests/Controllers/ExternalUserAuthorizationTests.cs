using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using GCCWebApp.Controllers;
using GCCWebApp.Models;

namespace GCCWebApp.Tests.Controllers
{
    public class ExternalUserAuthorizationTests : IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly IAuthorizationService _authorizationService;

        public ExternalUserAuthorizationTests()
        {
            var services = new ServiceCollection();

            // Add logging services required by authorization
            services.AddLogging();

            // Add authorization services with the same policies as Program.cs
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("Role", "Admin"));

                options.AddPolicy("AnalystOrAdmin", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(context =>
                              context.User.HasClaim("Role", "Admin") ||
                              context.User.HasClaim("Role", "Analyst")));

                options.AddPolicy("ExternalAccess", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(context =>
                              context.User.HasClaim("Role", "External") ||
                              context.User.HasClaim("Role", "Admin") ||
                              context.User.HasClaim("Role", "Analyst")));

                options.AddPolicy("AuthenticatedUsers", policy =>
                    policy.RequireAuthenticatedUser());
            });

            _services = services.BuildServiceProvider();
            _authorizationService = _services.GetRequiredService<IAuthorizationService>();
        }

        public void Dispose()
        {
            if (_services is IDisposable disposableServices)
                disposableServices.Dispose();
        }

        [Fact]
        public async Task ExternalUser_ShouldHaveAccessToExternalAccessPolicy()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "partner@amazon.com"),
                new Claim(ClaimTypes.Email, "partner@amazon.com"),
                new Claim("Role", "External")
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = await _authorizationService.AuthorizeAsync(user, "ExternalAccess");

            // Assert
            result.Succeeded.Should().BeTrue("External users should have ExternalAccess policy access");
        }

        [Fact]
        public async Task ExternalUser_ShouldNotHaveAccessToAnalystOrAdminPolicy()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "partner@amazon.com"),
                new Claim(ClaimTypes.Email, "partner@amazon.com"),
                new Claim("Role", "External")
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = await _authorizationService.AuthorizeAsync(user, "AnalystOrAdmin");

            // Assert
            result.Succeeded.Should().BeFalse("External users should NOT have AnalystOrAdmin policy access");
        }

        [Fact]
        public async Task ExternalUser_ShouldNotHaveAccessToAdminOnlyPolicy()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "partner@amazon.com"),
                new Claim(ClaimTypes.Email, "partner@amazon.com"),
                new Claim("Role", "External")
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = await _authorizationService.AuthorizeAsync(user, "AdminOnly");

            // Assert
            result.Succeeded.Should().BeFalse("External users should NOT have AdminOnly policy access");
        }

        [Fact]
        public async Task ExternalUser_ShouldHaveAccessToAuthenticatedUsersPolicy()
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "partner@amazon.com"),
                new Claim(ClaimTypes.Email, "partner@amazon.com"),
                new Claim("Role", "External")
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = await _authorizationService.AuthorizeAsync(user, "AuthenticatedUsers");

            // Assert
            result.Succeeded.Should().BeTrue("External users should have AuthenticatedUsers policy access");
        }

        [Fact]
        public async Task CompanyController_ShouldRequireAnalystOrAdminPolicy()
        {
            // Arrange
            var controllerType = typeof(CompanyController);
            
            // Act
            var authorizeAttribute = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                                                   .Cast<AuthorizeAttribute>()
                                                   .FirstOrDefault();

            // Assert
            authorizeAttribute.Should().NotBeNull("CompanyController should have Authorize attribute");
            authorizeAttribute.Policy.Should().Be("AnalystOrAdmin", "CompanyController should require AnalystOrAdmin policy");
        }

        [Fact]
        public async Task ProductController_ShouldRequireExternalAccessPolicy()
        {
            // Arrange
            var controllerType = typeof(ProductController);
            
            // Act
            var authorizeAttribute = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                                                   .Cast<AuthorizeAttribute>()
                                                   .FirstOrDefault();

            // Assert
            authorizeAttribute.Should().NotBeNull("ProductController should have Authorize attribute");
            authorizeAttribute.Policy.Should().Be("ExternalAccess", "ProductController should require ExternalAccess policy");
        }

        [Theory]
        [InlineData("External")]
        [InlineData("Admin")]
        [InlineData("Analyst")]
        public async Task ExternalAccessPolicy_ShouldAllowExpectedRoles(string role)
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, $"{role.ToLower()}@test.com"),
                new Claim(ClaimTypes.Email, $"{role.ToLower()}@test.com"),
                new Claim("Role", role)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = await _authorizationService.AuthorizeAsync(user, "ExternalAccess");

            // Assert
            result.Succeeded.Should().BeTrue($"{role} users should have ExternalAccess policy access");
        }

        [Theory]
        [InlineData("Public")]
        [InlineData("Guest")]
        [InlineData("Unknown")]
        public async Task ExternalAccessPolicy_ShouldDenyUnexpectedRoles(string role)
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, $"{role.ToLower()}@test.com"),
                new Claim(ClaimTypes.Email, $"{role.ToLower()}@test.com"),
                new Claim("Role", role)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = await _authorizationService.AuthorizeAsync(user, "ExternalAccess");

            // Assert
            result.Succeeded.Should().BeFalse($"{role} users should NOT have ExternalAccess policy access");
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Analyst")]
        public async Task AnalystOrAdminPolicy_ShouldAllowExpectedRoles(string role)
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, $"{role.ToLower()}@test.com"),
                new Claim(ClaimTypes.Email, $"{role.ToLower()}@test.com"),
                new Claim("Role", role)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = await _authorizationService.AuthorizeAsync(user, "AnalystOrAdmin");

            // Assert
            result.Succeeded.Should().BeTrue($"{role} users should have AnalystOrAdmin policy access");
        }

        [Theory]
        [InlineData("External")]
        [InlineData("Public")]
        [InlineData("Guest")]
        public async Task AnalystOrAdminPolicy_ShouldDenyUnexpectedRoles(string role)
        {
            // Arrange
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, $"{role.ToLower()}@test.com"),
                new Claim(ClaimTypes.Email, $"{role.ToLower()}@test.com"),
                new Claim("Role", role)
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = await _authorizationService.AuthorizeAsync(user, "AnalystOrAdmin");

            // Assert
            result.Succeeded.Should().BeFalse($"{role} users should NOT have AnalystOrAdmin policy access");
        }

        [Fact]
        public async Task UnauthenticatedUser_ShouldNotHaveAccessToAnyRestrictedPolicy()
        {
            // Arrange
            var user = new ClaimsPrincipal(); // No identity = unauthenticated

            var policies = new[] { "ExternalAccess", "AnalystOrAdmin", "AdminOnly", "AuthenticatedUsers" };

            foreach (var policy in policies)
            {
                // Act
                var result = await _authorizationService.AuthorizeAsync(user, policy);

                // Assert
                result.Succeeded.Should().BeFalse($"Unauthenticated users should NOT have {policy} policy access");
            }
        }
    }
}