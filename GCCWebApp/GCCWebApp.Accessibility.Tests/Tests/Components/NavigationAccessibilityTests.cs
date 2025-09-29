using GCCWebApp.Accessibility.Tests.Tests.Core;
using Microsoft.Playwright;

namespace GCCWebApp.Accessibility.Tests.Tests.Components;

public class NavigationAccessibilityTests : BaseAccessibilityTest
{
    [Fact]
    public async Task MainNavigation_ShouldHaveProperARIALabels()
    {
        // Arrange
        var url = $"{_baseUrl}/";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "aria-label", "link-name", "button-name" },
            Include = new[] { ".main-nav", ".nav-links", ".navbar" }
        });

        // Assert
        AssertNoViolations(results, "MainNavigation_ARIALabels");
    }

    [Fact]
    public async Task DropdownMenus_ShouldBeKeyboardNavigable()
    {
        // Arrange
        var url = $"{_baseUrl}/";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Test for dropdown presence and keyboard accessibility
        var dropdownToggles = _page.Locator(".dropdown-toggle, [aria-haspopup='true'], .nav-link[data-bs-toggle]");
        
        if (await dropdownToggles.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AxeOptions
            {
                Rules = new[] { "aria-expanded", "aria-haspopup", "focus-order-semantics" },
                Include = new[] { ".dropdown", ".submenu", ".nav-item" }
            });

            // Assert
            AssertNoViolations(results, "DropdownMenus_KeyboardNavigable");
        }
    }

    [Fact]
    public async Task MobileNavigation_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/";
        await _page!.GotoAsync(url);
        
        // Set mobile viewport
        await _page.SetViewportSizeAsync(768, 1024);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "button-name", "aria-expanded", "focus-order-semantics" },
            Include = new[] { ".mobile-toggle", ".mobile-nav" }
        });

        // Assert
        AssertNoViolations(results, "MobileNavigation_Accessible");
    }

    [Fact]
    public async Task Breadcrumbs_ShouldHaveProperStructure()
    {
        // Arrange
        var url = $"{_baseUrl}/Certificate";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check for breadcrumb presence
        var breadcrumbs = _page.Locator(".breadcrumbs, .breadcrumb, [aria-label*='breadcrumb']");
        
        if (await breadcrumbs.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AxeOptions
            {
                Rules = new[] { "aria-label", "link-name", "list" },
                Include = new[] { ".breadcrumbs", ".breadcrumb" }
            });

            // Assert
            AssertNoViolations(results, "Breadcrumbs_ProperStructure");
        }
    }

    [Fact]
    public async Task RoleBasedNavigation_ShouldBeAccessible()
    {
        // Test navigation for different role contexts
        var roleUrls = new[]
        {
            $"{_baseUrl}/", // Public view
            $"{_baseUrl}/Certificate", // External/Analyst view
            $"{_baseUrl}/Admin" // Admin view
        };

        foreach (var url in roleUrls)
        {
            await _page!.GotoAsync(url);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Act
            var results = await RunAccessibilityCheck(url, new AxeOptions
            {
                Rules = new[] { "aria-hidden-focus", "color-contrast", "link-name" },
                Include = new[] { ".nav-links", ".role-element" }
            });

            // Assert
            var testName = $"RoleBasedNavigation_{url.Split('/').LastOrDefault() ?? "Home"}";
            AssertNoViolations(results, testName);
        }
    }

    [Fact]
    public async Task NavigationFocus_ShouldBeVisible()
    {
        // Arrange
        var url = $"{_baseUrl}/";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Test focus visibility on navigation elements
        var navLinks = _page.Locator(".nav-link").First;
        
        if (await navLinks.CountAsync() > 0)
        {
            await navLinks.FocusAsync();
            
            // Check if focus styles are applied
            var focusedElement = await _page.EvaluateAsync<bool>(@"() => {
                const focused = document.activeElement;
                const styles = window.getComputedStyle(focused);
                return styles.outlineWidth !== '0px' || 
                       styles.outlineStyle !== 'none' ||
                       styles.boxShadow !== 'none';
            }");

            Assert.True(focusedElement, "Navigation elements should have visible focus indicators");
        }

        // Also run automated checks
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "focus-order-semantics" }
        });

        AssertNoViolations(results, "NavigationFocus_Visible");
    }

    [Fact]
    public async Task SearchBar_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check for search bar presence
        var searchBar = _page.Locator(".search-bar, input[type='search'], [role='search']");
        
        if (await searchBar.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AxeOptions
            {
                Rules = new[] { "label", "aria-label", "autocomplete-valid" },
                Include = new[] { ".search-bar" }
            });

            // Assert
            AssertNoViolations(results, "SearchBar_Accessible");
        }
    }

    [Fact]
    public async Task UserProfileDropdown_ShouldBeAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Check for user profile dropdown
        var userProfile = _page.Locator(".user-profile, .user-dropdown, .profile-menu");
        
        if (await userProfile.CountAsync() > 0)
        {
            // Act
            var results = await RunAccessibilityCheck(url, new AxeOptions
            {
                Rules = new[] { "button-name", "aria-expanded", "aria-haspopup" },
                Include = new[] { ".user-profile", ".user-dropdown" }
            });

            // Assert
            AssertNoViolations(results, "UserProfileDropdown_Accessible");
        }
    }
}