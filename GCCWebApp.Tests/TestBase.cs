using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Moq;
using GCCWebApp.Data;
using GCCWebApp.Services;

namespace GCCWebApp.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ApplicationDbContext DbContext;

        protected TestBase()
        {
            ServiceProvider = ConfigureServices();
            DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        protected virtual IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add in-memory database with unique name for each test
            var databaseName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            // Add logging with console provider and all required generic loggers
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Register specific ILogger instances that are commonly needed
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            // Add authorization services
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

            // Mock external services
            ConfigureMockServices(services);

            return services.BuildServiceProvider();
        }

        protected virtual void ConfigureMockServices(IServiceCollection services)
        {
            // Mock Email Service
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                           .Returns(Task.CompletedTask);
            mockEmailService.Setup(x => x.TestConnectionAsync())
                           .ReturnsAsync(true);
            services.AddSingleton(mockEmailService.Object);

            // Mock File Upload Service (if it exists)
            try
            {
                var fileServiceType = Type.GetType("GCCWebApp.Services.IFileUploadService, GCCWebApp");
                if (fileServiceType != null)
                {
                    var mockFileService = new Mock<object>();
                    services.AddSingleton(fileServiceType, mockFileService.Object);
                }
            }
            catch
            {
                // Service doesn't exist, skip
            }

            // Mock JWT Token Service (if it exists)
            try
            {
                var jwtServiceType = Type.GetType("GCCWebApp.Services.IJwtTokenService, GCCWebApp");
                if (jwtServiceType != null)
                {
                    var mockJwtService = new Mock<object>();
                    services.AddSingleton(jwtServiceType, mockJwtService.Object);
                }
            }
            catch
            {
                // Service doesn't exist, skip
            }

            // Mock Two Factor Service (if it exists)
            try
            {
                var twoFactorServiceType = Type.GetType("GCCWebApp.Services.ITwoFactorService, GCCWebApp");
                if (twoFactorServiceType != null)
                {
                    var mockTwoFactorService = new Mock<object>();
                    services.AddSingleton(twoFactorServiceType, mockTwoFactorService.Object);
                }
            }
            catch
            {
                // Service doesn't exist, skip
            }
        }

        protected virtual void SeedTestData()
        {
            // Override in derived classes to add test data
        }

        public virtual void Dispose()
        {
            DbContext?.Dispose();
            if (ServiceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
    }
}