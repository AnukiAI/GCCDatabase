using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using GCCWebApp;
using Microsoft.Extensions.DependencyInjection;
using GCCWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Tests.Authentication;

/// <summary>
/// CRITICAL: Login integration tests that must pass before deployment
/// These tests verify the complete login flow works end-to-end
/// </summary>
public class LoginIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LoginIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // Don't follow redirects automatically
        });
    }

    [Fact]
    [Trait("Category", "Critical")]
    [Trait("Category", "PreDeployment")]
    public async Task Login_WithValidAdminCredentials_ShouldSetAuthCookieAndRedirect()
    {
        // Arrange
        var loginData = new Dictionary<string, string>
        {
            ["Email"] = "admin",
            ["Password"] = "TestPass123!",
            ["RememberMe"] = "false"
        };

        // Act - First get the login page to get anti-forgery token
        var loginPage = await _client.GetAsync("/Account/Login");
        var cookies = loginPage.Headers.GetValues("Set-Cookie").ToList();

        // Extract anti-forgery token from cookies
        var antiForgeryToken = ExtractAntiForgeryToken(cookies);

        // Add cookies to the request
        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login")
        {
            Content = new FormUrlEncodedContent(loginData)
        };

        // Add cookies and anti-forgery token
        foreach (var cookie in cookies)
        {
            request.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        if (!string.IsNullOrEmpty(antiForgeryToken))
        {
            loginData["__RequestVerificationToken"] = antiForgeryToken;
            request.Content = new FormUrlEncodedContent(loginData);
        }

        // Act - Submit login form
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            "login should redirect after successful authentication");

        // Verify auth cookie is set
        response.Headers.Should().Contain(h => h.Key == "Set-Cookie",
            "authentication cookie must be set");

        var authCookie = response.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.Contains("GCC.Auth"));

        authCookie.Should().NotBeNull("GCC.Auth cookie must be present");
        authCookie.Should().NotContain("expires=Thu, 01 Jan 1970",
            "cookie should not be immediately expired");

        // Verify redirect location
        var redirectLocation = response.Headers.Location?.ToString();
        redirectLocation.Should().NotBeNullOrEmpty("redirect location should be specified");
        redirectLocation.Should().Match(url =>
            url.Contains("/Dashboard") || url.Contains("/Certificate"),
            "should redirect to dashboard or certificate page based on role");
    }

    [Fact]
    [Trait("Category", "Critical")]
    [Trait("Category", "PreDeployment")]
    public async Task Login_WithInvalidCredentials_ShouldShowError()
    {
        // Arrange
        var loginData = new Dictionary<string, string>
        {
            ["Email"] = "admin",
            ["Password"] = "WrongPassword123!",
            ["RememberMe"] = "false"
        };

        // Act
        var response = await _client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(loginData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "should return to login page with error");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid",
            "should show invalid credentials error");
    }

    [Theory]
    [Trait("Category", "Critical")]
    [Trait("Category", "PreDeployment")]
    [InlineData("admin", "TestPass123!", "Admin")]
    [InlineData("analyst", "TestPass123!", "Analyst")]
    [InlineData("external", "TestPass123!", "External")]
    public async Task Login_WithDifferentRoles_ShouldWorkForAllUsers(
        string username, string password, string expectedRole)
    {
        // Arrange
        var loginData = new Dictionary<string, string>
        {
            ["Email"] = username,
            ["Password"] = password,
            ["RememberMe"] = "false"
        };

        // Act
        var response = await _client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(loginData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect,
            $"{expectedRole} user should be able to login");

        var authCookie = response.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.Contains("GCC.Auth"));

        authCookie.Should().NotBeNull(
            $"{expectedRole} user should receive auth cookie");
    }

    [Fact]
    [Trait("Category", "Critical")]
    [Trait("Category", "PreDeployment")]
    public async Task Login_AfterSuccessfulLogin_ShouldBeAbleToAccessProtectedPage()
    {
        // Arrange - First login
        var loginData = new Dictionary<string, string>
        {
            ["Email"] = "admin",
            ["Password"] = "TestPass123!",
            ["RememberMe"] = "false"
        };

        var loginResponse = await _client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(loginData));

        // Extract auth cookie
        var authCookie = loginResponse.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.Contains("GCC.Auth"));

        authCookie.Should().NotBeNull("login should set auth cookie");

        // Act - Try to access protected page with auth cookie
        var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/Dashboard");
        protectedRequest.Headers.Add("Cookie", authCookie!.Split(';')[0]);

        var protectedResponse = await _client.SendAsync(protectedRequest);

        // Assert
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "authenticated user should access dashboard");

        var content = await protectedResponse.Content.ReadAsStringAsync();
        content.Should().NotContain("/Account/Login",
            "should not redirect to login page");
    }

    [Fact]
    [Trait("Category", "Critical")]
    [Trait("Category", "PreDeployment")]
    public async Task TestAuth_Endpoint_ShouldDiagnoseLoginIssues()
    {
        // Act
        var response = await _client.GetAsync(
            "/Account/TestAuth?username=admin&password=TestPass123!");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "TestAuth endpoint should be accessible");

        var content = await response.Content.ReadAsStringAsync();

        // Should show diagnostic information
        content.Should().Contain("Step", "should show diagnostic steps");
        content.Should().Contain("admin", "should show username being tested");

        // Parse JSON response and verify key checks
        content.Should().Contain("true",
            "should show success for user found, active, and password verified");
    }

    private string? ExtractAntiForgeryToken(IEnumerable<string> cookies)
    {
        // This is a simplified extraction - in real scenarios you'd parse the HTML
        // for the __RequestVerificationToken hidden field
        var afCookie = cookies.FirstOrDefault(c => c.Contains("Antiforgery"));
        return afCookie?.Split('=')[1]?.Split(';')[0];
    }
}

/// <summary>
/// Pre-deployment smoke test - runs the most critical login test
/// Run this before every deployment to ensure login works
/// </summary>
[Collection("PreDeployment")]
public class PreDeploymentLoginSmokeTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PreDeploymentLoginSmokeTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "SmokeTest")]
    [Trait("Priority", "Critical")]
    public async Task PREDEPLOYMENT_AdminLoginMustWork()
    {
        // This test MUST pass before any deployment
        var client = _factory.CreateClient();

        var loginData = new Dictionary<string, string>
        {
            ["Email"] = "admin",
            ["Password"] = "TestPass123!",
            ["RememberMe"] = "false"
        };

        var response = await client.PostAsync("/Account/Login",
            new FormUrlEncodedContent(loginData));

        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect,
            "❌ DEPLOYMENT BLOCKED: Admin login failed! " +
            "Do not deploy until this test passes. " +
            "Check cookies, DataProtection keys, and user/role configuration."
        );

        var authCookie = response.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.Contains("GCC.Auth"));

        Assert.NotNull(authCookie);
        Assert.DoesNotContain("expires=Thu, 01 Jan 1970", authCookie);
    }
}
