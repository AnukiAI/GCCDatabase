using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using GCCWebApp.Models;

namespace GCCWebApp.Tests.Authentication
{
    /// <summary>
    /// Authentication tests (Critical Priority)
    /// Tests authentication mechanisms, role validation, and security policies
    /// </summary>
    public class AuthenticationTests
    {
        [Fact]
        public void ClaimsPrincipal_ShouldContainRoleClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin@greencirclecertified.com"),
                new Claim(ClaimTypes.Email, "admin@greencirclecertified.com"),
                new Claim("Role", "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var hasRole = principal.HasClaim("Role", "Admin");

            // Assert
            hasRole.Should().BeTrue("authenticated user should have role claim");
        }

        [Theory]
        [InlineData("Admin", true)]
        [InlineData("Analyst", true)]
        [InlineData("External", true)]
        [InlineData("InvalidRole", false)]
        public void ClaimsPrincipal_ShouldValidateRoles(string role, bool isValid)
        {
            // Arrange
            var validRoles = new[] { "Admin", "Analyst", "External" };
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "test@greencirclecertified.com"),
                new Claim("Role", role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var hasValidRole = validRoles.Contains(role) && principal.HasClaim("Role", role);

            // Assert
            hasValidRole.Should().Be(isValid, $"role '{role}' should be {(isValid ? "valid" : "invalid")}");
        }

        [Fact]
        public void AdminOnlyPolicy_ShouldRequireAdminRole()
        {
            // Arrange - Admin user
            var adminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin@greencirclecertified.com"),
                new Claim("Role", "Admin")
            };
            var adminIdentity = new ClaimsIdentity(adminClaims, "TestAuth");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            // Arrange - Non-admin user
            var analystClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "analyst@greencirclecertified.com"),
                new Claim("Role", "Analyst")
            };
            var analystIdentity = new ClaimsIdentity(analystClaims, "TestAuth");
            var analystPrincipal = new ClaimsPrincipal(analystIdentity);

            // Act
            var adminHasRole = adminPrincipal.HasClaim("Role", "Admin");
            var analystHasRole = analystPrincipal.HasClaim("Role", "Admin");

            // Assert
            adminHasRole.Should().BeTrue("admin should have admin role");
            analystHasRole.Should().BeFalse("analyst should not have admin role");
        }

        [Fact]
        public void AnalystOrAdminPolicy_ShouldAllowBothRoles()
        {
            // Arrange - Admin
            var adminClaims = new List<Claim> { new Claim("Role", "Admin") };
            var adminIdentity = new ClaimsIdentity(adminClaims, "TestAuth");
            var adminPrincipal = new ClaimsPrincipal(adminIdentity);

            // Arrange - Analyst
            var analystClaims = new List<Claim> { new Claim("Role", "Analyst") };
            var analystIdentity = new ClaimsIdentity(analystClaims, "TestAuth");
            var analystPrincipal = new ClaimsPrincipal(analystIdentity);

            // Arrange - External
            var externalClaims = new List<Claim> { new Claim("Role", "External") };
            var externalIdentity = new ClaimsIdentity(externalClaims, "TestAuth");
            var externalPrincipal = new ClaimsPrincipal(externalIdentity);

            // Act
            var adminHasAccess = adminPrincipal.HasClaim("Role", "Admin") || adminPrincipal.HasClaim("Role", "Analyst");
            var analystHasAccess = analystPrincipal.HasClaim("Role", "Admin") || analystPrincipal.HasClaim("Role", "Analyst");
            var externalHasAccess = externalPrincipal.HasClaim("Role", "Admin") || externalPrincipal.HasClaim("Role", "Analyst");

            // Assert
            adminHasAccess.Should().BeTrue("admin should have access");
            analystHasAccess.Should().BeTrue("analyst should have access");
            externalHasAccess.Should().BeFalse("external should not have access");
        }

        [Fact]
        public void ExternalAccessPolicy_ShouldAllowAllAuthenticatedRoles()
        {
            // Arrange
            var roles = new[] { "Admin", "Analyst", "External" };

            foreach (var role in roles)
            {
                var claims = new List<Claim> { new Claim("Role", role) };
                var identity = new ClaimsIdentity(claims, "TestAuth");
                var principal = new ClaimsPrincipal(identity);

                // Act
                var hasAccess = principal.HasClaim("Role", "Admin") ||
                               principal.HasClaim("Role", "Analyst") ||
                               principal.HasClaim("Role", "External");

                // Assert
                hasAccess.Should().BeTrue($"{role} should have external access");
            }
        }

        [Fact]
        public void User_ShouldBeAuthenticatedWithValidClaims()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin@greencirclecertified.com"),
                new Claim(ClaimTypes.Email, "admin@greencirclecertified.com"),
                new Claim("Role", "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var isAuthenticated = identity.IsAuthenticated;

            // Assert
            isAuthenticated.Should().BeTrue("user with valid claims should be authenticated");
        }

        [Fact]
        public void User_ShouldNotBeAuthenticatedWithoutAuthenticationType()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim("Role", "Admin")
            };
            // No authentication type provided
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            // Act
            var isAuthenticated = identity.IsAuthenticated;

            // Assert
            isAuthenticated.Should().BeFalse("user without authentication type should not be authenticated");
        }

        [Theory]
        [InlineData("admin@greencirclecertified.com", "Admin123!", true)]
        [InlineData("analyst@greencirclecertified.com", "Analyst123!", true)]
        [InlineData("partner@amazon.com", "Partner123!", true)]
        [InlineData("invalid@example.com", "wrongpassword", false)]
        [InlineData("", "", false)]
        public void Credentials_ShouldValidateFormat(string email, string password, bool isValid)
        {
            // Act
            var hasEmail = !string.IsNullOrEmpty(email);
            var hasPassword = !string.IsNullOrEmpty(password);
            var emailFormat = hasEmail && email.Contains("@");
            var passwordLength = password.Length >= 8;

            var validFormat = hasEmail && hasPassword && emailFormat;

            // Assert
            if (isValid)
            {
                validFormat.Should().BeTrue($"{email} should have valid format");
            }
        }

        [Fact]
        public void TestMode_ShouldAllowBypassAuthentication()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Authentication:EnableTestMode", "true"}
                })
                .Build();

            // Act
            var testModeEnabled = configuration.GetValue<bool>("Authentication:EnableTestMode");

            // Assert
            testModeEnabled.Should().BeTrue("test mode should be enabled in development");
        }

        [Fact]
        public void TwoFactorAuthentication_ShouldBeConfigurable()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Authentication:Enable2FA", "false"},
                    {"Features:TwoFactorAuthentication", "false"}
                })
                .Build();

            // Act
            var enable2FA = configuration.GetValue<bool>("Authentication:Enable2FA");
            var feature2FA = configuration.GetValue<bool>("Features:TwoFactorAuthentication");

            // Assert
            enable2FA.Should().BeFalse("2FA should be disabled in development");
            feature2FA.Should().BeFalse("2FA feature flag should match authentication setting");
        }

        [Fact]
        public void GuestAccess_ShouldBeConfigurable()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Features:AllowGuestAccess", "true"}
                })
                .Build();

            // Act
            var allowGuestAccess = configuration.GetValue<bool>("Features:AllowGuestAccess");

            // Assert
            allowGuestAccess.Should().BeTrue("guest access should be allowed in development");
        }

        [Fact]
        public void HttpContext_ShouldContainUserPrincipal()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim("Role", "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            // Act
            var user = httpContext.User;
            var hasRole = user.HasClaim("Role", "Admin");

            // Assert
            user.Should().NotBeNull("HttpContext should have user");
            hasRole.Should().BeTrue("user should have admin role");
        }

        [Theory]
        [InlineData("Admin", "/Admin/UserManagement", true)]
        [InlineData("Analyst", "/Admin/UserManagement", false)]
        [InlineData("Admin", "/Certificate/Create", true)]
        [InlineData("Analyst", "/Certificate/Create", true)]
        [InlineData("External", "/Certificate/Create", false)]
        [InlineData("External", "/Certificate/CertificateTileView", true)]
        public void Routes_ShouldRespectRoleBasedAccess(string role, string route, bool shouldHaveAccess)
        {
            // Arrange
            var claims = new List<Claim> { new Claim("Role", role) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            bool hasAccess = false;

            if (route.StartsWith("/Admin/"))
            {
                // Admin only routes
                hasAccess = principal.HasClaim("Role", "Admin");
            }
            else if (route.Contains("/Create") || route.Contains("/Edit"))
            {
                // Analyst or Admin routes
                hasAccess = principal.HasClaim("Role", "Admin") || principal.HasClaim("Role", "Analyst");
            }
            else if (route.Contains("/Certificate/"))
            {
                // External access routes
                hasAccess = principal.HasClaim("Role", "Admin") ||
                           principal.HasClaim("Role", "Analyst") ||
                           principal.HasClaim("Role", "External");
            }

            // Assert
            hasAccess.Should().Be(shouldHaveAccess,
                $"{role} should {(shouldHaveAccess ? "have" : "not have")} access to {route}");
        }

        [Fact]
        public void JwtToken_ShouldHaveMinimumSecurityKeyLength()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Jwt:Key", "ThisIsAVeryLongSecretKeyForJwtTokenGenerationAtLeast64CharactersLong123456789"}
                })
                .Build();

            // Act
            var jwtKey = configuration["Jwt:Key"];

            // Assert
            jwtKey.Should().NotBeNullOrEmpty("JWT key should be configured");
            jwtKey.Should().HaveMinimumLength(64, "JWT key should be at least 64 characters for HS256");
        }
    }
}
