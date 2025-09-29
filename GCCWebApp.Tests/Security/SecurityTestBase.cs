using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GCCWebApp.Tests.Integration;

namespace GCCWebApp.Tests.Security
{
    public class SecurityTestBase : IntegrationTestBase
    {
        public SecurityTestBase(SecurityTestWebApplicationFactory factory) : base(factory)
        {
        }

        public HttpClient CreateAuthenticatedClient(string role, string userId = "test-user", string email = "test@example.com")
        {
            var client = Factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", options =>
                            {
                                options.Role = role;
                                options.UserId = userId;
                                options.Email = email;
                            });
                });
            }).CreateClient();

            return client;
        }

        public HttpClient CreateAdminClient() => CreateAuthenticatedClient("Admin", "admin-user", "admin@greencirclecertified.com");
        public HttpClient CreateAnalystClient() => CreateAuthenticatedClient("Analyst", "analyst-user", "analyst@greencirclecertified.com");
        public HttpClient CreateExternalClient() => CreateAuthenticatedClient("External", "external-user", "partner@amazon.com");
    }

    public class SecurityTestWebApplicationFactory : IntegrationTestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                // Override authentication for testing
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
            });
        }
    }

    public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public string Role { get; set; } = "Public";
        public string UserId { get; set; } = "test-user";
        public string Email { get; set; } = "test@example.com";
    }

    public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Options.Email),
                new Claim(ClaimTypes.NameIdentifier, Options.UserId),
                new Claim(ClaimTypes.Email, Options.Email),
                new Claim("Role", Options.Role)
            };

            // Add role-specific claims
            switch (Options.Role)
            {
                case "Admin":
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    claims.Add(new Claim("Permission", "UserManagement"));
                    claims.Add(new Claim("Permission", "CertificateManagement"));
                    claims.Add(new Claim("Permission", "SystemConfiguration"));
                    break;
                case "Analyst":
                    claims.Add(new Claim(ClaimTypes.Role, "Analyst"));
                    claims.Add(new Claim("Permission", "CertificateManagement"));
                    claims.Add(new Claim("Permission", "ReportGeneration"));
                    break;
                case "External":
                    claims.Add(new Claim(ClaimTypes.Role, "External"));
                    claims.Add(new Claim("Permission", "CertificateViewing"));
                    claims.Add(new Claim("Partner", "Amazon"));
                    break;
            }

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}