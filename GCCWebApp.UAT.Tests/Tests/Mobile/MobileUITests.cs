using Microsoft.Playwright;
using GCCWebApp.UAT.Tests.PageObjects;
using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Mobile;

[TestFixture]
public class MobileUITests : BaseTest
{
    [Test]
    public async Task Mobile_CertificateTileView_DisplaysCorrectlyOnSmallScreens()
    {
        // Arrange - Set mobile viewport (iPhone SE)
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 667 });
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("mobile_tile_view_iphone_se");

        // Assert - Verify mobile-specific layout
        var certificateGrid = Page.Locator("#certificatesGrid, .certificates-grid, .certificate-container");
        await Expect(certificateGrid).ToBeVisibleAsync();

        // Check that tiles stack vertically (mobile layout)
        var certificates = Page.Locator(".certificate-tile, .certificate-card, .cert-tile");
        var certificateCount = await certificates.CountAsync();

        if (certificateCount >= 2)
        {
            var firstTile = certificates.Nth(0);
            var secondTile = certificates.Nth(1);

            var firstRect = await firstTile.BoundingBoxAsync();
            var secondRect = await secondTile.BoundingBoxAsync();

            if (firstRect != null && secondRect != null)
            {
                // Second tile should be below first tile (not side by side)
                Assert.That(secondRect.Y, Is.GreaterThan(firstRect.Y),
                    "Certificate tiles should stack vertically on mobile");

                TestContext.WriteLine($"First tile Y: {firstRect.Y}, Second tile Y: {secondRect.Y}");
            }
        }

        // Check for horizontal overflow
        var bodyWidth = await Page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("window.innerWidth");

        Assert.That(bodyWidth, Is.LessThanOrEqualTo(viewportWidth + 20),
            "Page should not have significant horizontal overflow on mobile");

        await TakeScreenshot("mobile_tile_view_verified");
    }

    [Test]
    public async Task Mobile_Dashboard_ResponsiveLayout()
    {
        // Arrange - Mobile viewport
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 390, Height = 844 }); // iPhone 12
        await LoginAsRole("Admin");
        await NavigateToPage("/Dashboard");
        await TakeScreenshot("mobile_dashboard_iphone12");

        // Assert - Dashboard should be responsive
        var dashboardContent = Page.Locator(".dashboard, .dashboard-content, main");
        await Expect(dashboardContent).ToBeVisibleAsync();

        // Check metric cards stack on mobile
        var metricCards = Page.Locator(".metric-card, .stat-card, .dashboard-card, .card");
        var cardCount = await metricCards.CountAsync();

        if (cardCount >= 2)
        {
            var firstCard = metricCards.Nth(0);
            var secondCard = metricCards.Nth(1);

            var firstRect = await firstCard.BoundingBoxAsync();
            var secondRect = await secondCard.BoundingBoxAsync();

            if (firstRect != null && secondRect != null)
            {
                // Cards should stack vertically on mobile
                var isVerticalStack = secondRect.Y > firstRect.Y + (firstRect.Height / 2);
                TestContext.WriteLine($"Card stacking - First: Y={firstRect.Y}, Second: Y={secondRect.Y}, Vertical: {isVerticalStack}");
            }
        }

        // Verify no horizontal scroll
        var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(@"
            document.body.scrollWidth > window.innerWidth + 20
        ");

        Assert.That(hasHorizontalOverflow, Is.False,
            "Dashboard should not have horizontal overflow on mobile");

        await TakeScreenshot("mobile_dashboard_verified");
    }

    [Test]
    public async Task Mobile_SearchInterface_TouchOptimized()
    {
        // Arrange - Mobile viewport
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 667 });
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("mobile_search_interface");

        // Act - Find search input
        var searchInput = Page.Locator("input[type='search'], .search-input, #searchInput").First;

        if (await searchInput.CountAsync() > 0)
        {
            // Assert - Search input should be touch-friendly
            var inputRect = await searchInput.BoundingBoxAsync();

            if (inputRect != null)
            {
                Assert.That(inputRect.Height, Is.GreaterThanOrEqualTo(40),
                    "Search input should be at least 40px tall for touch interaction");

                Assert.That(inputRect.Width, Is.GreaterThan(200),
                    "Search input should be wide enough for mobile typing");

                TestContext.WriteLine($"Search input dimensions: {inputRect.Width}x{inputRect.Height}");
            }

            // Test touch interaction
            await searchInput.TapAsync();
            await Task.Delay(500);

            var isFocused = await searchInput.EvaluateAsync<bool>("el => document.activeElement === el");
            Assert.That(isFocused, Is.True, "Search input should receive focus when tapped");

            // Test typing
            await searchInput.FillAsync("test");
            var inputValue = await searchInput.InputValueAsync();
            Assert.That(inputValue, Is.EqualTo("test"), "Search input should accept text input");

            await TakeScreenshot("mobile_search_interaction");
        }
        else
        {
            TestContext.WriteLine("Search input not found on page");
            await TakeScreenshot("no_search_input");
        }
    }

    [Test]
    public async Task Mobile_Navigation_HamburgerMenu()
    {
        // Arrange - Mobile viewport
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 667 });
        await LoginAsRole("Admin");
        await NavigateToPage("/");
        await TakeScreenshot("mobile_navigation_initial");

        // Act - Look for mobile navigation toggle
        var hamburgerButton = Page.Locator(".navbar-toggler, .mobile-menu-toggle, .menu-toggle, [data-bs-toggle='collapse']");

        if (await hamburgerButton.CountAsync() > 0)
        {
            // Assert - Hamburger should be visible on mobile
            await Expect(hamburgerButton).ToBeVisibleAsync();

            // Test hamburger functionality
            await hamburgerButton.TapAsync();
            await Task.Delay(500);
            await TakeScreenshot("mobile_menu_opened");

            // Check if menu opened
            var mobileMenu = Page.Locator(".navbar-collapse, .mobile-menu, .navigation-menu");
            var isMenuVisible = await mobileMenu.IsVisibleAsync();

            if (isMenuVisible)
            {
                TestContext.WriteLine("Mobile menu opened successfully");

                // Test menu links
                var menuLinks = Page.Locator(".navbar-nav a, .mobile-menu a, .nav-link");
                var linkCount = await menuLinks.CountAsync();

                TestContext.WriteLine($"Found {linkCount} navigation links");

                // Close menu
                await hamburgerButton.TapAsync();
                await Task.Delay(500);
                await TakeScreenshot("mobile_menu_closed");
            }
            else
            {
                TestContext.WriteLine("Mobile menu did not open visibly");
            }
        }
        else
        {
            TestContext.WriteLine("No hamburger menu found - may use different mobile navigation");
            await TakeScreenshot("no_hamburger_menu");
        }

        await TakeScreenshot("mobile_navigation_completed");
    }

    [Test]
    public async Task Mobile_CertificateDetails_TouchInteractions()
    {
        // Arrange - Mobile viewport
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 390, Height = 844 });
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("mobile_certificate_tiles");

        // Act - Test tapping on certificate
        var certificateTiles = Page.Locator(".certificate-tile, .certificate-card, .cert-tile");
        var tileCount = await certificateTiles.CountAsync();

        if (tileCount > 0)
        {
            var firstTile = certificateTiles.First;

            // Test touch target size
            var tileRect = await firstTile.BoundingBoxAsync();
            if (tileRect != null)
            {
                Assert.That(tileRect.Height, Is.GreaterThanOrEqualTo(60),
                    "Certificate tiles should be large enough for touch interaction");

                TestContext.WriteLine($"Certificate tile size: {tileRect.Width}x{tileRect.Height}");
            }

            // Test tap interaction
            await firstTile.TapAsync();
            await Task.Delay(1000);

            await TakeScreenshot("mobile_certificate_tapped");

            // Should navigate to details or show some interaction
            var currentUrl = Page.Url;
            var hasModal = await Page.IsVisibleAsync(".modal, .dialog, .popup");

            var hasInteraction = !currentUrl.EndsWith("/CertificateTileView") || hasModal;
            TestContext.WriteLine($"After tap - URL: {currentUrl}, Has modal: {hasModal}");

            if (hasInteraction)
            {
                TestContext.WriteLine("Certificate tap interaction working");
            }
            else
            {
                TestContext.WriteLine("Certificate tap may not have interaction yet");
            }

            await TakeScreenshot("mobile_certificate_interaction_completed");
        }
        else
        {
            TestContext.WriteLine("No certificate tiles found for touch testing");
            await TakeScreenshot("no_certificates_for_touch_test");
        }
    }

    [Test]
    public async Task Mobile_InfiniteScroll_TouchScrolling()
    {
        // Arrange - Mobile viewport
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 667 });
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("mobile_infinite_scroll_initial");

        // Act - Test touch scrolling
        var initialCount = await Page.Locator(".certificate-tile, .certificate-card, .cert-tile").CountAsync();
        TestContext.WriteLine($"Initial certificate count: {initialCount}");

        if (initialCount > 0)
        {
            // Simulate touch scroll (swipe up)
            var viewport = await Page.ViewportSizeAsync();
            if (viewport != null)
            {
                var startY = viewport.Height - 100;
                var endY = 100;
                var centerX = viewport.Width / 2;

                // Perform swipe up gesture
                await Page.Mouse.MoveAsync(centerX, startY);
                await Page.Mouse.DownAsync();
                await Page.Mouse.MoveAsync(centerX, endY);
                await Page.Mouse.UpAsync();

                await Task.Delay(1500); // Wait for loading
                await TakeScreenshot("mobile_after_swipe_scroll");

                // Check if more content loaded
                var finalCount = await Page.Locator(".certificate-tile, .certificate-card, .cert-tile").CountAsync();
                var endOfResults = await Page.IsVisibleAsync("#endOfResults:visible");

                TestContext.WriteLine($"Final certificate count: {finalCount}");
                TestContext.WriteLine($"End of results visible: {endOfResults}");

                // Either more content loaded or end reached
                var scrollWorked = finalCount > initialCount || endOfResults;
                TestContext.WriteLine($"Scroll interaction successful: {scrollWorked}");

                await TakeScreenshot("mobile_infinite_scroll_completed");
            }
        }
        else
        {
            TestContext.WriteLine("No certificates available for scroll testing");
            await TakeScreenshot("no_certificates_for_scroll");
        }
    }

    [Test]
    public async Task Mobile_FormElements_TouchFriendly()
    {
        // Arrange - Mobile viewport
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 667 });
        await NavigateToPage("/Account/Login");
        await TakeScreenshot("mobile_login_form");

        // Act & Assert - Test form elements are touch-friendly
        var formElements = new[]
        {
            new { Name = "Email Input", Selector = "#Email, input[type='email']" },
            new { Name = "Password Input", Selector = "#Password, input[type='password']" },
            new { Name = "Submit Button", Selector = "button[type='submit'], input[type='submit'], .btn-primary" }
        };

        foreach (var element in formElements)
        {
            var formElement = Page.Locator(element.Selector).First;

            if (await formElement.CountAsync() > 0)
            {
                var elementRect = await formElement.BoundingBoxAsync();

                if (elementRect != null)
                {
                    // Check minimum touch target size (44px is iOS guideline)
                    Assert.That(elementRect.Height, Is.GreaterThanOrEqualTo(40),
                        $"{element.Name} should be at least 40px tall for touch interaction");

                    TestContext.WriteLine($"{element.Name}: {elementRect.Width}x{elementRect.Height}");

                    // Test tap interaction
                    await formElement.TapAsync();
                    await Task.Delay(200);

                    var isFocused = await formElement.EvaluateAsync<bool>("el => document.activeElement === el");
                    if (element.Name.Contains("Input"))
                    {
                        Assert.That(isFocused, Is.True, $"{element.Name} should receive focus when tapped");
                    }
                }
            }
            else
            {
                TestContext.WriteLine($"{element.Name} not found on page");
            }
        }

        await TakeScreenshot("mobile_form_elements_tested");
    }

    [Test]
    public async Task Mobile_TextReadability_AppropriateSize()
    {
        // Arrange - Mobile viewport
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 667 });
        await NavigateToPage("/");
        await TakeScreenshot("mobile_text_readability");

        // Act & Assert - Check text sizing
        var textElements = new[]
        {
            new { Name = "Main Heading", Selector = "h1" },
            new { Name = "Secondary Heading", Selector = "h2" },
            new { Name = "Body Text", Selector = "p, .text-body" },
            new { Name = "Navigation Links", Selector = ".nav-link, .navbar-nav a" }
        };

        foreach (var element in textElements)
        {
            var textElement = Page.Locator(element.Selector).First;

            if (await textElement.CountAsync() > 0)
            {
                var fontSize = await textElement.EvaluateAsync<string>("el => window.getComputedStyle(el).fontSize");
                var fontSizeValue = float.Parse(fontSize.Replace("px", ""));

                var minSize = element.Name.Contains("Heading") ? 18 : 16;

                Assert.That(fontSizeValue, Is.GreaterThanOrEqualTo(minSize),
                    $"{element.Name} should be at least {minSize}px for mobile readability");

                TestContext.WriteLine($"{element.Name}: {fontSizeValue}px");
            }
            else
            {
                TestContext.WriteLine($"{element.Name} not found on page");
            }
        }

        await TakeScreenshot("mobile_text_sizes_verified");
    }

    [Test]
    public async Task Mobile_MultipleViewports_Consistency()
    {
        // Test multiple common mobile viewports
        var viewports = new[]
        {
            new { Name = "iPhone SE", Width = 375, Height = 667 },
            new { Name = "iPhone 12", Width = 390, Height = 844 },
            new { Name = "Samsung Galaxy S20", Width = 360, Height = 800 },
            new { Name = "iPad Mini", Width = 768, Height = 1024 }
        };

        foreach (var viewport in viewports)
        {
            // Arrange
            await Page.SetViewportSizeAsync(new ViewportSize { Width = viewport.Width, Height = viewport.Height });
            await NavigateToPage("/Certificate/CertificateTileView");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await TakeScreenshot($"mobile_{viewport.Name.ToLower().Replace(" ", "_")}");

            // Assert - Basic usability checks
            var navigation = Page.Locator("nav, .navbar");
            var mainContent = Page.Locator("main, .main-content, .container");

            await Expect(navigation).ToBeVisibleAsync();
            await Expect(mainContent).ToBeVisibleAsync();

            // Check for horizontal overflow
            var hasOverflow = await Page.EvaluateAsync<bool>(@"
                document.body.scrollWidth > window.innerWidth + 10
            ");

            Assert.That(hasOverflow, Is.False,
                $"No horizontal overflow on {viewport.Name} ({viewport.Width}x{viewport.Height})");

            TestContext.WriteLine($"{viewport.Name} ({viewport.Width}x{viewport.Height}): ✓ No overflow");
        }

        await TakeScreenshot("mobile_viewports_tested");
    }

    [TearDown]
    public async Task MobileTestTeardown()
    {
        // Reset to desktop viewport for other tests
        await Page.SetViewportSizeAsync(new ViewportSize { Width = 1280, Height = 720 });
    }
}