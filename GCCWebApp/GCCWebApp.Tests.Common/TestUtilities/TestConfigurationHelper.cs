using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GCCWebApp.Tests.Common.TestUtilities;

/// <summary>
/// Helper class for test configuration management
/// </summary>
public static class TestConfigurationHelper
{
    /// <summary>
    /// Creates a test configuration with common test settings
    /// </summary>
    public static IConfiguration CreateTestConfiguration()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(GetTestConfigurationData())
            .AddEnvironmentVariables();

        return configBuilder.Build();
    }

    /// <summary>
    /// Gets default test configuration data
    /// </summary>
    private static Dictionary<string, string?> GetTestConfigurationData()
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "InMemory",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning",
            ["CertificateSettings:DefaultPageSize"] = "10",
            ["CertificateSettings:MaxPageSize"] = "50",
            ["CertificateSettings:EnablePublicAccess"] = "true",
            ["CertificateSettings:EnableDownloads"] = "false",
            ["CertificateSettings:CacheExpirationMinutes"] = "5",
            ["JWT:SecretKey"] = "TestSecretKeyForJWTTokenGeneration123456789",
            ["JWT:Issuer"] = "GCCTestApp",
            ["JWT:Audience"] = "GCCTestUsers",
            ["JWT:ExpirationHours"] = "1",
            ["Environment"] = "Testing"
        };
    }

    /// <summary>
    /// Configures test services with common test dependencies
    /// </summary>
    public static void ConfigureTestServices(IServiceCollection services)
    {
        // Add test-specific services here
        // This method can be extended by specific test projects
    }
}