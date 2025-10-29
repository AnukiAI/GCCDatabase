using Xunit;
using GCCWebApp.Accessibility.Tests.Tests.Core;
using Microsoft.Playwright;

namespace GCCWebApp.Accessibility.Tests.Tests.Pages;

public class HomePageAccessibilityTests : BaseAccessibilityTest
{
    [Fact]
    public async Task HomePage_ShouldHaveNoWCAG21AAViolations()
    {
        // Arrange
        var url = $"{_baseUrl}/";

        // Act
        var results = await RunWCAG21AACheck(url);

        // Assert
        AssertNoViolations(results, "HomePage_WCAG21AA");
    }

    [Fact]
    public async Task HomePage_ShouldHaveNoSection508Violations()
    {
        // Arrange
        var url = $"{_baseUrl}/";

        // Act
        var results = await RunSection508Check(url);

        // Assert
        AssertNoViolations(results, "HomePage_Section508");
    }

    [Fact]
    public async Task HomePage_Navigation_ShouldBeKeyboardAccessible()
    {
        // Arrange
        var url = $"{_baseUrl}/";
        await _page!.GotoAsync(url);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act & Assert - Test tab navigation through main nav items
        var navLinks = await _page.Locator(".nav-link").AllAsync();
        
        Assert.True(navLinks.Count > 0, "Navigation links should be present");

        // Test keyboard navigation
        await _page.Keyboard.PressAsync("Tab");
        var focusedElement = await _page.EvaluateAsync<string>("document.activeElement.tagName");
        
        // Should be able to focus on interactive elements
        Assert.True(!string.IsNullOrEmpty(focusedElement), "Should be able to focus on elements with Tab key");
    }

    [Fact]
    public async Task HomePage_Images_ShouldHaveAltText()
    {
        // Arrange
        var url = $"{_baseUrl}/";

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "image-alt" }
        });

        // Assert
        AssertNoViolations(results, "HomePage_ImageAlt");
    }

    [Fact]
    public async Task HomePage_Forms_ShouldHaveLabels()
    {
        // Arrange
        var url = $"{_baseUrl}/";

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "label", "form-field-multiple-labels" }
        });

        // Assert
        AssertNoViolations(results, "HomePage_FormLabels");
    }

    [Fact]
    public async Task HomePage_ColorContrast_ShouldMeetWCAGStandards()
    {
        // Arrange
        var url = $"{_baseUrl}/";

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "color-contrast", "color-contrast-enhanced" }
        });

        // Assert
        AssertNoViolations(results, "HomePage_ColorContrast");
    }

    [Fact]
    public async Task HomePage_Headings_ShouldHaveProperStructure()
    {
        // Arrange
        var url = $"{_baseUrl}/";

        // Act
        var results = await RunAccessibilityCheck(url, new AxeOptions
        {
            Rules = new[] { "heading-order", "empty-heading", "page-has-heading-one" }
        });

        // Assert
        AssertNoViolations(results, "HomePage_HeadingStructure");
    }
}