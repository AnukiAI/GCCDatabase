using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GCCWebApp.Data;

namespace GCCWebApp.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Configures the test server to:
/// - Use in-memory database
/// - Disable HTTPS redirection (fixes "Failed to determine the https port" error)
/// - Configure test-specific settings
/// </summary>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            });

            // Ensure database is created and seeded
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

            // Seed test data
            SeedTestData(db);
        });

        // Configure Kestrel to not use HTTPS for tests
        builder.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(5000); // HTTP only for tests
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Disable HTTPS redirection middleware for tests
        builder.ConfigureServices(services =>
        {
            // Add configuration to disable HTTPS redirection
            services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options =>
            {
                options.HttpsPort = null; // Disable HTTPS port requirement
            });
        });

        return base.CreateHost(builder);
    }

    private static void SeedTestData(ApplicationDbContext db)
    {
        // Seed roles if they don't exist
        if (!db.Roles.Any())
        {
            var adminRole = new GCCWebApp.Models.Role { Id = 1, Name = "Admin", Description = "Administrator role" };
            var analystRole = new GCCWebApp.Models.Role { Id = 2, Name = "Analyst", Description = "Analyst role" };
            var externalRole = new GCCWebApp.Models.Role { Id = 3, Name = "External", Description = "External user role" };

            db.Roles.AddRange(adminRole, analystRole, externalRole);
            db.SaveChanges();
        }

        // Seed test users if they don't exist
        if (!db.Users.Any())
        {
            // BCrypt hash for "TestPass123!"
            var passwordHash = "$2a$11$24Y94.bK.yWpd7.p/YDwZOy6qriBEiO5OGe0MmjsYjDPV04YLpqpa";
            var adminRole = db.Roles.First(r => r.Name == "Admin");
            var analystRole = db.Roles.First(r => r.Name == "Analyst");
            var externalRole = db.Roles.First(r => r.Name == "External");

            var adminUser = new GCCWebApp.Models.User
            {
                Id = 1,
                Username = "admin",
                Email = "swest@anukiconsulting.com",
                PasswordHash = passwordHash,
                Name = "Admin User",
                FullName = "Admin User",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true
            };
            adminUser.Roles.Add(adminRole);

            var analystUser = new GCCWebApp.Models.User
            {
                Id = 2,
                Username = "analyst",
                Email = "analyst@test.com",
                PasswordHash = passwordHash,
                Name = "Jane Analyst",
                FullName = "Jane Analyst",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true
            };
            analystUser.Roles.Add(analystRole);

            var externalUser = new GCCWebApp.Models.User
            {
                Id = 3,
                Username = "external",
                Email = "external@test.com",
                PasswordHash = passwordHash,
                Name = "External User",
                FullName = "External User",
                Status = "active",
                TwoFactorMethod = "Email",
                IsActive = true
            };
            externalUser.Roles.Add(externalRole);

            db.Users.AddRange(adminUser, analystUser, externalUser);
            db.SaveChanges();
        }

        // Seed certification types if they don't exist
        if (!db.CertificationTypes.Any())
        {
            db.CertificationTypes.AddRange(
                new GCCWebApp.Models.CertificationType { Id = 1, Name = "Certified Energy Savings", Description = "Energy efficiency certification", IsActive = true },
                new GCCWebApp.Models.CertificationType { Id = 2, Name = "Certified Recycled Content", Description = "Recycled content certification", IsActive = true }
            );
            db.SaveChanges();
        }

        // Seed companies if they don't exist
        if (!db.Companies.Any())
        {
            db.Companies.AddRange(
                new GCCWebApp.Models.Company { Id = 1, Name = "Test Company", Email = "test@company.com", IsActive = true },
                new GCCWebApp.Models.Company { Id = 2, Name = "Sample Corp", Email = "sample@corp.com", IsActive = true }
            );
            db.SaveChanges();
        }

        // Seed products if they don't exist
        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new GCCWebApp.Models.Product { Id = 1, Name = "Test Product", CompanyId = 1, IsActive = true },
                new GCCWebApp.Models.Product { Id = 2, Name = "Sample Product", CompanyId = 2, IsActive = true }
            );
            db.SaveChanges();
        }
    }
}
