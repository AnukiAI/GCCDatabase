using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;

namespace GCCWebApp.Tests.Configuration
{
    /// <summary>
    /// Configuration validation tests (Critical Priority)
    /// Tests configuration loading, validation, and database provider detection
    /// </summary>
    public class ConfigurationValidationTests
    {
        [Fact]
        public void Configuration_ShouldLoadDevelopmentSettings()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .SetBasePath(GetProjectRoot())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Act
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var logLevel = configuration["Logging:LogLevel:Default"];

            // Assert
            connectionString.Should().NotBeNullOrEmpty("connection string should be configured");
            logLevel.Should().NotBeNullOrEmpty("logging level should be configured");
        }

        [Fact]
        public void Configuration_ShouldHaveRequiredConnectionString()
        {
            // Arrange
            var configuration = BuildConfiguration("Development");

            // Act
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Assert
            connectionString.Should().NotBeNullOrEmpty("connection string is required");
        }

        [Fact]
        public void Configuration_ShouldDetectSqliteProvider()
        {
            // Arrange
            var sqliteConnectionString = "Data Source=test.db";

            // Act
            var isSqlite = sqliteConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) &&
                          !sqliteConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase);

            // Assert
            isSqlite.Should().BeTrue("SQLite connection strings contain 'Data Source='");
        }

        [Fact]
        public void Configuration_ShouldDetectSqlServerProvider()
        {
            // Arrange
            var sqlServerConnectionString = "Server=(localdb)\\mssqllocaldb;Database=GCCDatabase;Trusted_Connection=true";

            // Act
            var isSqlServer = sqlServerConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                             sqlServerConnectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) &&
                             !sqlServerConnectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase);

            // Assert
            isSqlServer.Should().BeTrue("SQL Server connection strings contain 'Server='");
        }

        [Fact]
        public void Configuration_ShouldValidateJwtSettings()
        {
            // Arrange
            var configuration = BuildConfiguration("Development");

            // Act
            var jwtKey = configuration["Jwt:Key"];
            var jwtIssuer = configuration["Jwt:Issuer"];
            var jwtAudience = configuration["Jwt:Audience"];
            var jwtExpiryMinutes = configuration["Jwt:ExpiryMinutes"];

            // Assert
            jwtKey.Should().NotBeNullOrEmpty("JWT key is required");
            jwtKey.Should().HaveMinimumLength(64, "JWT key should be at least 64 characters for security");
            jwtIssuer.Should().NotBeNullOrEmpty("JWT issuer is required");
            jwtAudience.Should().NotBeNullOrEmpty("JWT audience is required");
            jwtExpiryMinutes.Should().NotBeNullOrEmpty("JWT expiry time is required");
        }

        [Fact]
        public void Configuration_ShouldHaveFeatureFlags()
        {
            // Arrange
            var configuration = BuildConfiguration("Development");

            // Act
            var twoFactorAuth = configuration["Features:TwoFactorAuthentication"];
            var emailConfirmation = configuration["Features:RequireEmailConfirmation"];
            var guestAccess = configuration["Features:AllowGuestAccess"];

            // Assert
            twoFactorAuth.Should().NotBeNull("TwoFactorAuthentication feature flag should be defined");
            emailConfirmation.Should().NotBeNull("RequireEmailConfirmation feature flag should be defined");
            guestAccess.Should().NotBeNull("AllowGuestAccess feature flag should be defined");
        }

        [Fact]
        public void Configuration_ShouldHaveAuthenticationSettings()
        {
            // Arrange
            var configuration = BuildConfiguration("Development");

            // Act
            var enableTestMode = configuration["Authentication:EnableTestMode"];
            var enable2FA = configuration["Authentication:Enable2FA"];

            // Assert
            enableTestMode.Should().NotBeNull("EnableTestMode should be defined");
            enable2FA.Should().NotBeNull("Enable2FA should be defined");
        }

        [Fact]
        public void Configuration_ShouldHaveKestrelEndpoint()
        {
            // Arrange
            var configuration = BuildConfiguration("Development");

            // Act
            var httpUrl = configuration["Kestrel:Endpoints:Http:Url"];

            // Assert
            httpUrl.Should().NotBeNullOrEmpty("Kestrel HTTP endpoint should be configured");
            httpUrl.Should().Contain("localhost", "development should use localhost");
        }

        [Fact]
        public void Configuration_ShouldHaveRateLimitingSettings()
        {
            // Arrange
            var configuration = BuildConfiguration("Development");

            // Act
            var enableEndpointRateLimiting = configuration["IpRateLimiting:EnableEndpointRateLimiting"];
            var httpStatusCode = configuration["IpRateLimiting:HttpStatusCode"];

            // Assert
            enableEndpointRateLimiting.Should().NotBeNull("Rate limiting enable flag should be defined");
            httpStatusCode.Should().NotBeNullOrEmpty("Rate limiting HTTP status code should be configured");

            if (!string.IsNullOrEmpty(httpStatusCode))
            {
                int.Parse(httpStatusCode).Should().Be(429, "rate limiting should return HTTP 429 Too Many Requests");
            }
        }

        [Theory]
        [InlineData("Data Source=test.db", true)]
        [InlineData("Data Source=gcc_database.db", true)]
        [InlineData("Server=(localdb)\\mssqllocaldb;Database=GCC", false)]
        [InlineData("Server=localhost;Database=GCC;User=sa;Password=test", false)]
        public void Configuration_ShouldCorrectlyIdentifySqliteConnections(string connectionString, bool expectedIsSqlite)
        {
            // Act
            var isSqlite = IsSqliteConnectionString(connectionString);

            // Assert
            isSqlite.Should().Be(expectedIsSqlite);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Production")]
        public void Configuration_ShouldLoadForAllEnvironments(string environment)
        {
            // Arrange & Act
            var configuration = BuildConfiguration(environment);
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Assert
            connectionString.Should().NotBeNullOrEmpty($"connection string should exist for {environment}");
        }

        #region Helper Methods

        private IConfiguration BuildConfiguration(string environment)
        {
            var basePath = GetProjectRoot();

            return new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();
        }

        private string GetProjectRoot()
        {
            // Get the test project directory
            var testProjectDir = Directory.GetCurrentDirectory();

            // Navigate up to find GCCWebApp project
            var parentDir = Directory.GetParent(testProjectDir);
            while (parentDir != null)
            {
                var gccWebAppPath = Path.Combine(parentDir.FullName, "GCCWebApp");
                if (Directory.Exists(gccWebAppPath))
                {
                    return gccWebAppPath;
                }
                parentDir = parentDir.Parent;
            }

            throw new DirectoryNotFoundException("Could not find GCCWebApp project directory");
        }

        private bool IsSqliteConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            return (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
                   connectionString.Contains("DataSource=", StringComparison.OrdinalIgnoreCase)) &&
                   !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
