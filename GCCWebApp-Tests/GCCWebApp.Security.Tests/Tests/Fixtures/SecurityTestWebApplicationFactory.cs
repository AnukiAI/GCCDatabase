using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GCCWebApp.Data;
using Microsoft.Extensions.Configuration;

namespace GCCWebApp.Security.Tests.Fixtures
{
    public class SecurityTestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        // Use unique database name per factory instance to avoid conflicts
        private readonly string _databaseName = $"SecurityTestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add an in-memory database for testing with unique name per factory
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                // Override configuration for security testing
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Jwt:Key"] = "ThisIsASecretKeyForTestingPurposesOnlyAndShouldBeAtLeast256Bits",
                        ["Jwt:Issuer"] = "GCCWebApp.Security.Tests",
                        ["Jwt:Audience"] = "GCCWebApp.Security.Tests",
                        ["Jwt:ExpiryMinutes"] = "1", // Short expiry for testing
                        ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                        ["RateLimiting:Enabled"] = "false", // Disable rate limiting for security tests
                        ["RateLimiting:RequestsPerMinute"] = "1000"
                    }!)
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);

                // Ensure the database is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();

                // Seed test data for security tests
                SeedSecurityTestData(db);
            });

            builder.UseEnvironment("Testing");
        }

        private static void SeedSecurityTestData(ApplicationDbContext context)
        {
            // Add test users with different roles for security testing
            if (!context.Users.Any())
            {
                var adminRole = new Models.Role { Id = 1, Name = "Admin", Description = "Administrator role" };
                var userRole = new Models.Role { Id = 2, Name = "User", Description = "Regular user role" };
                var analystRole = new Models.Role { Id = 3, Name = "Analyst", Description = "Analyst role" };

                context.Roles.AddRange(adminRole, userRole, analystRole);

                var adminUser = new Models.User
                {
                    Id = 1,
                    Username = "securitytestadmin",
                    Email = "admin@securitytest.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Roles = new List<Models.Role> { adminRole }
                };

                var regularUser = new Models.User
                {
                    Id = 2,
                    Username = "securitytestuser",
                    Email = "user@securitytest.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserPassword123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Roles = new List<Models.Role> { userRole }
                };

                var inactiveUser = new Models.User
                {
                    Id = 3,
                    Username = "inactiveuser",
                    Email = "inactive@securitytest.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("InactivePassword123!"),
                    Status = "inactive",
                    TwoFactorMethod = "Email",
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    Roles = new List<Models.Role> { userRole }
                };

                context.Users.AddRange(adminUser, regularUser, inactiveUser);

                // Add test API keys - use plain text keys for validation
                // The ApiKeyAuthenticationHandler compares raw keys directly
                var testApiKey = new Models.ApiKey
                {
                    Id = 1,
                    Name = "SecurityTestKey",
                    Key = "test-api-key-123", // Must match what tests use
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Security Test Suite",
                    Scopes = "read,write",
                    AllowedIPs = "*", // Allow all IPs for testing
                    AllowedOrigins = "*", // Allow all origins for testing
                    RequireTLS = false // Allow HTTP for testing
                };

                var restrictedApiKey = new Models.ApiKey
                {
                    Id = 2,
                    Name = "RestrictedTestKey",
                    Key = "restricted_security_test_key", // Plain text for direct comparison
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "Security Test Suite",
                    Scopes = "read",
                    AllowedIPs = "192.168.1.100",
                    AllowedOrigins = "https://trusted-domain.com",
                    RequireTLS = false // Allow HTTP for testing
                };

                context.ApiKeys.AddRange(testApiKey, restrictedApiKey);

                context.SaveChanges();
            }
        }
    }
}