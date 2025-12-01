using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GCCWebApp;
using GCCWebApp.Data;
using System.Net;
using System.Net.Sockets;

namespace GCCWebApp.Accessibility.Tests.Fixtures;

/// <summary>
/// WebApplicationFactory that creates a real HTTP server for Playwright tests.
/// This implementation creates a real listening server that Playwright can connect to.
/// </summary>
public class AccessibilityTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly int _port;
    private readonly string _serverUrl;
    private IHost? _host;

    public AccessibilityTestWebApplicationFactory()
    {
        // Find an available port upfront
        _port = GetAvailablePort();
        _serverUrl = $"http://127.0.0.1:{_port}";
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Gets the base URL of the test server
    /// </summary>
    public string ServerUrl => _serverUrl;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override the ASPNETCORE_URLS environment variable
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", _serverUrl);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override any URLs from configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "urls", _serverUrl },
                { "ASPNETCORE_URLS", _serverUrl }
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Also remove any DbContext factory registrations
            var contextDescriptors = services.Where(
                d => d.ServiceType == typeof(ApplicationDbContext) ||
                     d.ServiceType.Name.Contains("DbContext")).ToList();

            foreach (var desc in contextDescriptors)
            {
                services.Remove(desc);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("AccessibilityTestDb_" + Guid.NewGuid().ToString());
            });

            // Reduce logging noise
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
        });

        builder.UseEnvironment("Test");

        // Configure to use our specific URL
        builder.UseUrls(_serverUrl);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Create the test host (this creates an in-memory TestServer)
        var testHost = base.CreateHost(builder);

        // Create a separate host that actually listens on the network
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, _port);
            });
        });

        _host = builder.Build();
        _host.Start();

        return testHost;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        await base.DisposeAsync();
    }

    /// <summary>
    /// Ensures the database is initialized with test data
    /// </summary>
    public void EnsureDatabase()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
    }
}
