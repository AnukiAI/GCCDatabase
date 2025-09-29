using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.RoleBasedUI.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<TestWebApplicationFactory>>();

            db.Database.EnsureCreated();

            try
            {
                SeedTestData(db);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the database with test data.");
            }
        });

        builder.UseEnvironment("Test");
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        if (context.Users.Any())
            return;

        var users = new[]
        {
            new User
            {
                Id = 1,
                Username = "admin@test.com",
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestAdmin123!"),
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = true,
                IsActive = true,
                IsStaff = true,
                IsSuperuser = true
            },
            new User
            {
                Id = 2,
                Username = "analyst@test.com",
                Email = "analyst@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestAnalyst123!"),
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = true,
                IsActive = true,
                IsStaff = true
            },
            new User
            {
                Id = 3,
                Username = "external@test.com",
                Email = "external@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestExternal123!"),
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = true,
                IsActive = true
            }
        };

        context.Users.AddRange(users);

        var companies = new[]
        {
            new Company
            {
                Id = 1,
                Name = "Test Company 1",
                Email = "contact@testcompany1.com",
                CreatedAt = DateTime.UtcNow
            },
            new Company
            {
                Id = 2,
                Name = "Test Company 2",
                Email = "contact@testcompany2.com",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Companies.AddRange(companies);

        var products = new[]
        {
            new Product
            {
                Id = 1,
                Name = "Test Product 1",
                Description = "Test product description",
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 2,
                Name = "Test Product 2",
                Description = "Another test product",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);

        context.SaveChanges();
    }
}