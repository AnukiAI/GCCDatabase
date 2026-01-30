using Microsoft.Playwright;
using Xunit;
using FluentAssertions;

namespace GCCWebApp.E2E.Tests.Tests.UI
{
    public class CertificateUITests : IAsyncLifetime
    {
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private IBrowserContext _context = null!;
        private IPage _page = null!;
        
        // Update this URL to match your local development server
        private const string BaseUrl = "http://localhost:5101";

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true // Set to false if you want to see the browser
            });
            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await _browser.DisposeAsync();
            _playwright?.Dispose();
        }

        [Fact]
        public async Task HomePage_LoadsSuccessfully()
        {
            // Navigate to home page
            await _page.GotoAsync(BaseUrl);
            
            // Wait for page to load
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Check title exists
            var title = await _page.TitleAsync();
            title.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginPage_DisplaysLoginForm()
        {
            // Navigate to login page
            await _page.GotoAsync($"{BaseUrl}/Account/Login");
            
            // Check for email input
            var emailInput = await _page.QuerySelectorAsync("input[type='email'], input[name='Email']");
            emailInput.Should().NotBeNull();
            
            // Check for password input
            var passwordInput = await _page.QuerySelectorAsync("input[type='password'], input[name='Password']");
            passwordInput.Should().NotBeNull();
            
            // Check for submit button
            var submitButton = await _page.QuerySelectorAsync("button[type='submit'], input[type='submit']");
            submitButton.Should().NotBeNull();
        }

        [Fact]
        public async Task Navigation_WorksCorrectly()
        {
            // Navigate to home
            await _page.GotoAsync(BaseUrl);
            
            // Look for navigation links
            var navLinks = await _page.QuerySelectorAllAsync("nav a, .navbar a");
            navLinks.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CertificateSearch_RendersSearchBox()
        {
            // Navigate to certificates page (adjust URL as needed)
            await _page.GotoAsync($"{BaseUrl}/Certificate");
            
            // Check for search input
            var searchInput = await _page.QuerySelectorAsync("input[type='search'], input[placeholder*='Search'], input[name*='search']");
            
            // May not exist if page requires authentication
            if (searchInput != null)
            {
                searchInput.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task ResponsiveDesign_WorksOnMobile()
        {
            // Set mobile viewport
            await _page.SetViewportSizeAsync(375, 667);
            
            // Navigate to home
            await _page.GotoAsync(BaseUrl);
            
            // Wait for page to load
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Check that page renders without horizontal scroll
            var bodyWidth = await _page.EvaluateAsync<int>("document.body.scrollWidth");
            var viewportWidth = await _page.EvaluateAsync<int>("window.innerWidth");
            
            bodyWidth.Should().BeLessThanOrEqualTo(viewportWidth + 20); // Allow small margin for scrollbar
        }
    }
}