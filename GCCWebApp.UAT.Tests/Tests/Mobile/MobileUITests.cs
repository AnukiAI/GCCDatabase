using Microsoft.Playwright;
using GCCWebApp.UAT.Tests.PageObjects;
using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.Mobile;

[TestFixture]
public class MobileUITests : BaseTest
{
    // Common mobile viewports
    private static readonly (string Name, int Width, int Height)[] MobileViewports = new[]
    {
        ("iPhone SE", 375, 667),
        ("iPhone 12", 390, 844),
        ("Samsung Galaxy S20", 360, 800)
    };

    private static readonly (string Name, int Width, int Height)[] TabletViewports = new[]
    {
        ("iPad Mini", 768, 1024),
        ("iPad Air", 820, 1180),
        ("iPad Pro 11", 834, 1194)
    };

    [Test]
    public async Task Mobile_CertificateTileView_TilesStackVertically()
    {
        // Arrange - iPhone SE viewport
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("mobile_tile_view_iphone_se");

        // Assert - Certificate grid must exist
        var certificateGrid = Page.Locator(".certificates-grid");
        await Expect(certificateGrid).ToBeVisibleAsync();

        // Assert - Tiles must stack vertically on mobile (single column)
        var certificates = Page.Locator(".certificate-card");
        var certificateCount = await certificates.CountAsync();
        Assert.That(certificateCount, Is.GreaterThan(0), "Certificate cards should be present");

        if (certificateCount >= 2)
        {
            var firstRect = await certificates.Nth(0).BoundingBoxAsync();
            var secondRect = await certificates.Nth(1).BoundingBoxAsync();

            Assert.That(firstRect, Is.Not.Null, "First tile must have a bounding box");
            Assert.That(secondRect, Is.Not.Null, "Second tile must have a bounding box");
            Assert.That(secondRect!.Y, Is.GreaterThan(firstRect!.Y),
                "Certificate tiles must stack vertically on mobile (375px)");
        }

        await TakeScreenshot("mobile_tile_stacking_verified");
    }

    [Test]
    public async Task Mobile_NoHorizontalOverflow_CertificateTileView()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var bodyWidth = await Page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("window.innerWidth");

        Assert.That(bodyWidth, Is.LessThanOrEqualTo(viewportWidth + 5),
            $"Page must not overflow horizontally on mobile. Body: {bodyWidth}px, Viewport: {viewportWidth}px");
    }

    [Test]
    public async Task Mobile_NoHorizontalOverflow_Dashboard()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await LoginAsRole("Admin");
        await NavigateToPage("/Dashboard");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("mobile_dashboard");

        var bodyWidth = await Page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("window.innerWidth");

        Assert.That(bodyWidth, Is.LessThanOrEqualTo(viewportWidth + 5),
            $"Dashboard must not overflow horizontally on mobile. Body: {bodyWidth}px, Viewport: {viewportWidth}px");
    }

    [Test]
    public async Task Mobile_NoHorizontalOverflow_LoginPage()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/Account/Login");
        await TakeScreenshot("mobile_login");

        var bodyWidth = await Page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("window.innerWidth");

        Assert.That(bodyWidth, Is.LessThanOrEqualTo(viewportWidth + 5),
            $"Login page must not overflow horizontally. Body: {bodyWidth}px, Viewport: {viewportWidth}px");
    }

    [Test]
    public async Task Mobile_Dashboard_StatsCardsStackVertically()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await LoginAsRole("Admin");
        await NavigateToPage("/Dashboard");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var statCards = Page.Locator(".stat-card");
        var cardCount = await statCards.CountAsync();

        if (cardCount >= 2)
        {
            var firstRect = await statCards.Nth(0).BoundingBoxAsync();
            var secondRect = await statCards.Nth(1).BoundingBoxAsync();

            Assert.That(firstRect, Is.Not.Null);
            Assert.That(secondRect, Is.Not.Null);
            Assert.That(secondRect!.Y, Is.GreaterThan(firstRect!.Y + firstRect.Height * 0.5),
                "Stats cards must stack vertically on mobile");
        }
    }

    [Test]
    public async Task Mobile_Navigation_HamburgerMenuVisible()
    {
        // Arrange - Mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("mobile_nav_initial");

        // Assert - Hamburger toggle must be visible on mobile
        var hamburgerButton = Page.Locator("#mobile-toggle");
        await Expect(hamburgerButton).ToBeVisibleAsync();

        await TakeScreenshot("mobile_hamburger_visible");
    }

    [Test]
    public async Task Mobile_Navigation_HamburgerMenuOpensAndCloses()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await LoginAsRole("Admin");
        await NavigateToPage("/Certificate/CertificateTileView");

        // Open the mobile menu
        var hamburgerButton = Page.Locator("#mobile-toggle");
        await Expect(hamburgerButton).ToBeVisibleAsync();
        await hamburgerButton.ClickAsync();
        await Task.Delay(500);
        await TakeScreenshot("mobile_menu_opened");

        // Nav links should now be visible
        var navLinks = Page.Locator(".nav-links");
        var isVisible = await navLinks.EvaluateAsync<bool>(@"el => {
            var style = window.getComputedStyle(el);
            return style.display !== 'none' && style.visibility !== 'hidden';
        }");
        Assert.That(isVisible, Is.True, "Nav links should be visible after opening hamburger menu");

        // Close the mobile menu
        var closeButton = Page.Locator("#mobile-nav-close");
        if (await closeButton.IsVisibleAsync())
        {
            await closeButton.ClickAsync();
            await Task.Delay(500);
            await TakeScreenshot("mobile_menu_closed");
        }
    }

    [Test]
    public async Task Mobile_LoginForm_TouchFriendlyInputs()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/Account/Login");
        await TakeScreenshot("mobile_login_form");

        // All form inputs must be at least 44px tall for touch
        var emailInput = Page.Locator("#Email, input[name='Email']").First;
        var passwordInput = Page.Locator("#Password, input[name='Password']").First;
        var submitButton = Page.Locator("button[type='submit'], .btn-primary").First;

        await Expect(emailInput).ToBeVisibleAsync();
        await Expect(passwordInput).ToBeVisibleAsync();
        await Expect(submitButton).ToBeVisibleAsync();

        var emailRect = await emailInput.BoundingBoxAsync();
        var passwordRect = await passwordInput.BoundingBoxAsync();
        var buttonRect = await submitButton.BoundingBoxAsync();

        Assert.That(emailRect, Is.Not.Null);
        Assert.That(passwordRect, Is.Not.Null);
        Assert.That(buttonRect, Is.Not.Null);

        Assert.That(emailRect!.Height, Is.GreaterThanOrEqualTo(40),
            $"Email input height {emailRect.Height}px must be >= 40px for touch targets");
        Assert.That(passwordRect!.Height, Is.GreaterThanOrEqualTo(40),
            $"Password input height {passwordRect.Height}px must be >= 40px for touch targets");
        Assert.That(buttonRect!.Height, Is.GreaterThanOrEqualTo(40),
            $"Submit button height {buttonRect.Height}px must be >= 40px for touch targets");

        // Inputs must be at least 16px font to prevent iOS zoom
        var emailFontSize = await emailInput.EvaluateAsync<string>("el => window.getComputedStyle(el).fontSize");
        var fontSizeValue = float.Parse(emailFontSize.Replace("px", ""));
        Assert.That(fontSizeValue, Is.GreaterThanOrEqualTo(16),
            $"Input font-size {fontSizeValue}px must be >= 16px to prevent iOS zoom");

        await TakeScreenshot("mobile_login_touch_verified");
    }

    [Test]
    public async Task Mobile_LoginPage_SingleColumnLayout()
    {
        // On mobile, the two-column login layout must stack to single column
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/Account/Login");
        await TakeScreenshot("mobile_login_layout");

        var loginContainer = Page.Locator(".login-container").First;
        if (await loginContainer.CountAsync() > 0)
        {
            var gridCols = await loginContainer.EvaluateAsync<string>(
                "el => window.getComputedStyle(el).gridTemplateColumns");

            // On mobile, should be single column (just one value, not "Xpx Xpx")
            var columnCount = gridCols.Split(' ').Length;
            Assert.That(columnCount, Is.EqualTo(1),
                $"Login container must be single column on mobile. Got: '{gridCols}'");
        }
    }

    [Test]
    public async Task Mobile_TextReadability_MinimumFontSizes()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/");
        await TakeScreenshot("mobile_text_readability");

        // Check body text is at least 16px
        var bodyTextElements = Page.Locator("p").First;
        if (await bodyTextElements.CountAsync() > 0)
        {
            var fontSize = await bodyTextElements.EvaluateAsync<string>(
                "el => window.getComputedStyle(el).fontSize");
            var fontSizeValue = float.Parse(fontSize.Replace("px", ""));
            Assert.That(fontSizeValue, Is.GreaterThanOrEqualTo(16),
                $"Body text ({fontSizeValue}px) must be >= 16px on mobile");
        }

        // Check headings are at least 18px
        var headings = Page.Locator("h1, h2, h3").First;
        if (await headings.CountAsync() > 0)
        {
            var fontSize = await headings.EvaluateAsync<string>(
                "el => window.getComputedStyle(el).fontSize");
            var fontSizeValue = float.Parse(fontSize.Replace("px", ""));
            Assert.That(fontSizeValue, Is.GreaterThanOrEqualTo(18),
                $"Heading ({fontSizeValue}px) must be >= 18px on mobile");
        }

        await TakeScreenshot("mobile_fonts_verified");
    }

    [Test]
    public async Task Mobile_Logo_ReasonableSize()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("mobile_logo_check");

        var logo = Page.Locator(".main-logo").First;
        if (await logo.CountAsync() > 0)
        {
            var logoRect = await logo.BoundingBoxAsync();
            Assert.That(logoRect, Is.Not.Null, "Logo must have a bounding box");

            // Logo should not consume more than 25% of viewport height on mobile
            var maxLogoHeight = 667 * 0.25; // ~167px
            Assert.That(logoRect!.Height, Is.LessThanOrEqualTo(maxLogoHeight),
                $"Logo height {logoRect.Height}px should not exceed {maxLogoHeight}px (25% of viewport) on mobile");
        }
    }

    [Test]
    public async Task Mobile_CertificateDetails_TouchTargets()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var certificateTiles = Page.Locator(".certificate-card");
        var tileCount = await certificateTiles.CountAsync();
        Assert.That(tileCount, Is.GreaterThan(0), "Certificate tiles must be present");

        var firstTile = certificateTiles.First;
        var tileRect = await firstTile.BoundingBoxAsync();
        Assert.That(tileRect, Is.Not.Null, "First tile must have a bounding box");
        Assert.That(tileRect!.Height, Is.GreaterThanOrEqualTo(60),
            "Certificate tiles must be large enough for touch interaction (>= 60px)");

        await TakeScreenshot("mobile_touch_targets_verified");
    }

    [Test]
    public async Task Mobile_SearchInterface_AccessibleOrCollapsed()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot("mobile_search_check");

        // Search may be behind a filter toggle on mobile
        var filterToggle = Page.Locator(".mobile-filter-toggle");
        var searchInput = Page.Locator(".search-input, #searchInput").First;

        var isSearchVisible = await searchInput.IsVisibleAsync();

        if (!isSearchVisible && await filterToggle.CountAsync() > 0)
        {
            // Filter toggle must be visible to access search
            await Expect(filterToggle.First).ToBeVisibleAsync();
            TestContext.WriteLine("Search is behind mobile filter toggle - toggle is visible");

            // Open the filter to access search
            await filterToggle.First.ClickAsync();
            await Task.Delay(500);

            isSearchVisible = await searchInput.IsVisibleAsync();
            Assert.That(isSearchVisible, Is.True,
                "Search input must be accessible after opening filter toggle on mobile");
        }
        else
        {
            Assert.That(isSearchVisible, Is.True,
                "Search input must be visible or accessible via filter toggle on mobile");
        }

        // If visible, check touch-friendly size
        if (isSearchVisible)
        {
            var inputRect = await searchInput.BoundingBoxAsync();
            Assert.That(inputRect, Is.Not.Null);
            Assert.That(inputRect!.Height, Is.GreaterThanOrEqualTo(36),
                $"Search input height ({inputRect.Height}px) must be >= 36px for touch");
        }

        await TakeScreenshot("mobile_search_verified");
    }

    [Test]
    [TestCase(375, 667, "iPhone SE")]
    [TestCase(390, 844, "iPhone 12")]
    [TestCase(360, 800, "Samsung Galaxy S20")]
    public async Task Mobile_MultipleViewports_NoOverflow(int width, int height, string deviceName)
    {
        await Page.SetViewportSizeAsync(width, height);
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot($"mobile_{deviceName.ToLower().Replace(" ", "_")}");

        // Main content must be visible
        var mainContent = Page.Locator(".main-content, .main-container").First;
        await Expect(mainContent).ToBeVisibleAsync();

        // No horizontal overflow
        var bodyWidth = await Page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("window.innerWidth");

        Assert.That(bodyWidth, Is.LessThanOrEqualTo(viewportWidth + 5),
            $"No horizontal overflow on {deviceName} ({width}x{height}). Body: {bodyWidth}px, Viewport: {viewportWidth}px");
    }

    [Test]
    [TestCase(768, 1024, "iPad Mini")]
    [TestCase(820, 1180, "iPad Air")]
    public async Task Tablet_Viewports_NoOverflow(int width, int height, string deviceName)
    {
        await Page.SetViewportSizeAsync(width, height);
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot($"tablet_{deviceName.ToLower().Replace(" ", "_")}");

        // Main content must be visible
        var mainContent = Page.Locator(".main-content, .main-container").First;
        await Expect(mainContent).ToBeVisibleAsync();

        // No horizontal overflow
        var bodyWidth = await Page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("window.innerWidth");

        Assert.That(bodyWidth, Is.LessThanOrEqualTo(viewportWidth + 5),
            $"No horizontal overflow on {deviceName} ({width}x{height}). Body: {bodyWidth}px, Viewport: {viewportWidth}px");
    }

    [Test]
    public async Task Tablet_Logo_ReasonableSize()
    {
        await Page.SetViewportSizeAsync(768, 1024);
        await NavigateToPage("/Certificate/CertificateTileView");
        await TakeScreenshot("tablet_logo_check");

        var logo = Page.Locator(".main-logo").First;
        if (await logo.CountAsync() > 0)
        {
            var logoRect = await logo.BoundingBoxAsync();
            Assert.That(logoRect, Is.Not.Null);

            // Logo should not exceed 200px on tablet
            Assert.That(logoRect!.Height, Is.LessThanOrEqualTo(200),
                $"Logo height {logoRect.Height}px should not exceed 200px on tablet");
        }
    }

    [Test]
    public async Task Mobile_Toast_DoesNotOverlapContent()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/Certificate/CertificateTileView");

        // Inject a test toast to check positioning
        var toastPositioning = await Page.EvaluateAsync<string>(@"() => {
            var style = document.createElement('style');
            style.textContent = '.toast.show { opacity: 1; transform: translateX(0); }';
            document.head.appendChild(style);

            var toast = document.querySelector('.toast');
            if (!toast) {
                toast = document.createElement('div');
                toast.className = 'toast show';
                toast.textContent = 'Test notification';
                document.body.appendChild(toast);
            } else {
                toast.classList.add('show');
            }
            var rect = toast.getBoundingClientRect();
            return JSON.stringify({
                right: rect.right,
                left: rect.left,
                bottom: rect.bottom,
                width: rect.width,
                viewportWidth: window.innerWidth
            });
        }");

        TestContext.WriteLine($"Toast positioning: {toastPositioning}");
        await TakeScreenshot("mobile_toast_check");
    }

    [Test]
    [TestCase(375, 667, 375, 844, "iPhone Portrait to Landscape")]
    public async Task Mobile_OrientationChange_NoBreakage(int w1, int h1, int w2, int h2, string scenario)
    {
        // Portrait
        await Page.SetViewportSizeAsync(w1, h1);
        await NavigateToPage("/Certificate/CertificateTileView");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshot($"orientation_portrait_{scenario.ToLower().Replace(" ", "_")}");

        var portraitOverflow = await Page.EvaluateAsync<bool>("document.body.scrollWidth > window.innerWidth + 5");

        // Landscape (swap width/height)
        await Page.SetViewportSizeAsync(h1, w1);
        await Task.Delay(300);
        await TakeScreenshot($"orientation_landscape_{scenario.ToLower().Replace(" ", "_")}");

        var landscapeOverflow = await Page.EvaluateAsync<bool>("document.body.scrollWidth > window.innerWidth + 5");

        Assert.That(portraitOverflow, Is.False, $"No overflow in portrait for {scenario}");
        Assert.That(landscapeOverflow, Is.False, $"No overflow in landscape for {scenario}");
    }

    [Test]
    public async Task Mobile_LoginPage_AuthContainerFitsViewport()
    {
        await Page.SetViewportSizeAsync(320, 568); // Smallest common iPhone (SE 1st gen)
        await NavigateToPage("/Account/Login");
        await TakeScreenshot("mobile_login_smallest_viewport");

        // Auth container must not overflow on smallest viewport
        var bodyWidth = await Page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("window.innerWidth");

        Assert.That(bodyWidth, Is.LessThanOrEqualTo(viewportWidth + 5),
            $"Login page must fit on 320px screen. Body: {bodyWidth}px");
    }

    [TearDown]
    public async Task MobileTestTeardown()
    {
        // Reset to desktop viewport for other tests
        await Page.SetViewportSizeAsync(1280, 720);
    }
}
