using Microsoft.Playwright;
using Xunit;
using FluentAssertions;

namespace GCCWebApp.UAT.Tests.Tests.Mobile
{
    /// <summary>
    /// Mobile Responsiveness Tests - Strict assertions for layout, touch targets,
    /// accessibility, and performance on mobile/tablet viewports.
    /// Uses real app selectors (.mobile-toggle, .nav-links, .certificate-card, etc.)
    /// </summary>
    [Collection("UITests")]
    public class MobileResponsivenessTests : IAsyncLifetime
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        private static readonly string BaseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com";

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();

            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 390, Height = 844 },
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
                HasTouch = true
            });

            _page = await _context.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            if (_page != null) await _page.CloseAsync();
            if (_context != null) await _context.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
        }

        #region Navigation Tests

        [Fact]
        public async Task MobileNavigation_HamburgerMenu_MustBeVisibleOnMobile()
        {
            await _page!.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // The app uses .mobile-toggle#mobile-toggle for the hamburger button
            var hamburgerMenu = await _page.QuerySelectorAsync("#mobile-toggle");

            hamburgerMenu.Should().NotBeNull(
                "Hamburger menu (#mobile-toggle) must exist on mobile viewport");

            var isVisible = await hamburgerMenu!.IsVisibleAsync();
            isVisible.Should().BeTrue(
                "Hamburger menu must be visible on 390px mobile viewport");
        }

        [Fact]
        public async Task MobileNavigation_HamburgerMenu_OpensNavLinks()
        {
            await _page!.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var hamburgerMenu = await _page.QuerySelectorAsync("#mobile-toggle");
            hamburgerMenu.Should().NotBeNull();

            await hamburgerMenu!.ClickAsync();
            await Task.Delay(500);

            // Nav links should become visible
            var navLinksVisible = await _page.EvaluateAsync<bool>(@"() => {
                var navLinks = document.querySelector('.nav-links');
                if (!navLinks) return false;
                var style = window.getComputedStyle(navLinks);
                return style.display !== 'none' && style.visibility !== 'hidden';
            }");

            navLinksVisible.Should().BeTrue(
                "Nav links must become visible after clicking hamburger menu");

            // Close button must be visible
            var closeButton = await _page.QuerySelectorAsync("#mobile-nav-close");
            closeButton.Should().NotBeNull("Close button must exist in mobile nav");
        }

        [Fact]
        public async Task MobileNavigation_AnonUsers_NavHidden()
        {
            // Anonymous users should not see nav on mobile (nav-anon is hidden)
            await _page!.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var navWrapper = await _page.QuerySelectorAsync(".nav-wrapper");
            if (navWrapper != null)
            {
                var hasAnonClass = await navWrapper.EvaluateAsync<bool>(
                    "el => el.classList.contains('nav-anon')");

                if (hasAnonClass)
                {
                    var isVisible = await navWrapper.IsVisibleAsync();
                    isVisible.Should().BeFalse(
                        "Anonymous nav wrapper should be hidden on mobile");
                }
            }
        }

        #endregion

        #region Layout Tests

        [Fact]
        public async Task MobileLayout_CertificateTiles_MustStackVertically()
        {
            await _page!.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var tiles = await _page.QuerySelectorAllAsync(".certificate-card");
            tiles.Should().NotBeEmpty("Certificate cards must be present");

            if (tiles.Count > 1)
            {
                var firstRect = await tiles[0].BoundingBoxAsync();
                var secondRect = await tiles[1].BoundingBoxAsync();

                firstRect.Should().NotBeNull();
                secondRect.Should().NotBeNull();

                secondRect!.Y.Should().BeGreaterThan(firstRect!.Y,
                    "Certificate cards must stack vertically on mobile (390px)");
            }
        }

        [Fact]
        public async Task MobileLayout_LoginPage_MustBeSingleColumn()
        {
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var loginContainer = await _page.QuerySelectorAsync(".login-container");
            loginContainer.Should().NotBeNull("Login container must exist");

            var gridCols = await loginContainer!.EvaluateAsync<string>(
                "el => window.getComputedStyle(el).gridTemplateColumns");

            var columnCount = gridCols.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            columnCount.Should().Be(1,
                $"Login must be single column on mobile. Got grid-template-columns: '{gridCols}'");
        }

        [Fact]
        public async Task MobileLayout_Logo_MustNotDominateViewport()
        {
            await _page!.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var logo = await _page.QuerySelectorAsync(".main-logo");
            if (logo != null)
            {
                var logoRect = await logo.BoundingBoxAsync();
                logoRect.Should().NotBeNull();

                // Logo must not exceed 25% of 844px viewport height = ~210px
                logoRect!.Height.Should().BeLessThanOrEqualTo(210,
                    $"Logo height ({logoRect.Height}px) must not exceed 25% of viewport on mobile");
            }
        }

        #endregion

        #region Touch Target Tests

        [Fact]
        public async Task MobileForm_LoginInputs_MustMeetTouchTargetSize()
        {
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Check email input
            var emailInput = await _page.QuerySelectorAsync("#Email, input[name='Email']");
            emailInput.Should().NotBeNull("Email input must exist on login page");

            var emailRect = await emailInput!.BoundingBoxAsync();
            emailRect.Should().NotBeNull();
            emailRect!.Height.Should().BeGreaterThanOrEqualTo(40,
                $"Email input ({emailRect.Height}px) must be >= 40px for touch targets");

            // Check password input
            var passwordInput = await _page.QuerySelectorAsync("#Password, input[name='Password']");
            passwordInput.Should().NotBeNull("Password input must exist");

            var passRect = await passwordInput!.BoundingBoxAsync();
            passRect.Should().NotBeNull();
            passRect!.Height.Should().BeGreaterThanOrEqualTo(40,
                $"Password input ({passRect.Height}px) must be >= 40px for touch targets");

            // Check submit button
            var submitButton = await _page.QuerySelectorAsync("button[type='submit'], .btn-primary");
            submitButton.Should().NotBeNull("Submit button must exist");

            var btnRect = await submitButton!.BoundingBoxAsync();
            btnRect.Should().NotBeNull();
            btnRect!.Height.Should().BeGreaterThanOrEqualTo(40,
                $"Submit button ({btnRect.Height}px) must be >= 40px for touch targets");
        }

        [Fact]
        public async Task MobileAccessibility_TouchTargets_MustMeetMinimumSize()
        {
            await _page!.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var interactiveElements = await _page.QuerySelectorAllAsync(
                "button:not([hidden]), a.btn, input[type='submit']");

            interactiveElements.Should().NotBeEmpty("Page must have interactive elements");

            var tooSmallElements = new List<string>();
            foreach (var element in interactiveElements.Take(10))
            {
                var rect = await element.BoundingBoxAsync();
                if (rect != null && rect.Height > 0 && rect.Width > 0)
                {
                    if (rect.Height < 36 || rect.Width < 36)
                    {
                        var text = await element.InnerTextAsync();
                        tooSmallElements.Add($"'{text.Trim().Substring(0, Math.Min(text.Trim().Length, 20))}' ({rect.Width:F0}x{rect.Height:F0}px)");
                    }
                }
            }

            tooSmallElements.Should().BeEmpty(
                $"All interactive elements must be >= 36px. Too small: {string.Join(", ", tooSmallElements)}");
        }

        #endregion

        #region Font Size Tests

        [Fact]
        public async Task MobileText_BodyText_MustBeAtLeast16px()
        {
            await _page!.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Check body text font sizes - 16px minimum prevents iOS auto-zoom
            var tooSmallFonts = await _page.EvaluateAsync<string[]>(@"() => {
                var tooSmall = [];
                var elements = document.querySelectorAll('p, label, span, li, td, th, a');
                for (var i = 0; i < Math.min(elements.length, 30); i++) {
                    var el = elements[i];
                    var style = window.getComputedStyle(el);
                    var size = parseFloat(style.fontSize);
                    if (size < 16 && style.display !== 'none' && el.offsetHeight > 0) {
                        var text = el.textContent.trim().substring(0, 30);
                        if (text) tooSmall.push(el.tagName + ': ' + size + 'px - ' + text);
                    }
                }
                return tooSmall;
            }");

            tooSmallFonts.Should().BeEmpty(
                $"All visible text must be >= 16px on mobile. Too small: {string.Join("; ", tooSmallFonts)}");
        }

        [Fact]
        public async Task MobileText_LoginPage_MustBeAtLeast16px()
        {
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var tooSmallFonts = await _page.EvaluateAsync<string[]>(@"() => {
                var tooSmall = [];
                var elements = document.querySelectorAll('p, label, span, a, input, button, .support-info *, .login-footer *, .resend-section *');
                for (var i = 0; i < elements.length; i++) {
                    var el = elements[i];
                    var style = window.getComputedStyle(el);
                    var size = parseFloat(style.fontSize);
                    if (size < 16 && style.display !== 'none' && el.offsetHeight > 0) {
                        var text = el.textContent.trim().substring(0, 30);
                        if (text) tooSmall.push(el.tagName + '.' + el.className.split(' ')[0] + ': ' + size + 'px');
                    }
                }
                return tooSmall;
            }");

            tooSmallFonts.Should().BeEmpty(
                $"All login page text must be >= 16px on mobile. Too small: {string.Join("; ", tooSmallFonts)}");
        }

        #endregion

        #region Overflow Tests

        [Xunit.Theory]
        [InlineData(375, 667, "iPhone SE")]
        [InlineData(390, 844, "iPhone 12")]
        [InlineData(360, 800, "Samsung Galaxy S20")]
        [InlineData(320, 568, "iPhone SE 1st Gen")]
        public async Task MobileBreakpoints_NoHorizontalOverflow(int width, int height, string device)
        {
            await _page!.SetViewportSizeAsync(width, height);
            await _page.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var bodyWidth = await _page.EvaluateAsync<int>("document.body.scrollWidth");
            var viewportWidth = await _page.EvaluateAsync<int>("window.innerWidth");

            bodyWidth.Should().BeLessThanOrEqualTo(viewportWidth + 5,
                $"No horizontal overflow on {device} ({width}x{height}). Body: {bodyWidth}px, Viewport: {viewportWidth}px");
        }

        [Xunit.Theory]
        [InlineData(375, 667, "iPhone SE")]
        [InlineData(390, 844, "iPhone 12")]
        public async Task MobileBreakpoints_LoginPage_NoOverflow(int width, int height, string device)
        {
            await _page!.SetViewportSizeAsync(width, height);
            await _page.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var bodyWidth = await _page.EvaluateAsync<int>("document.body.scrollWidth");
            var viewportWidth = await _page.EvaluateAsync<int>("window.innerWidth");

            bodyWidth.Should().BeLessThanOrEqualTo(viewportWidth + 5,
                $"Login page must not overflow on {device} ({width}x{height}). Body: {bodyWidth}px");
        }

        [Xunit.Theory]
        [InlineData(768, 1024, "iPad Mini")]
        [InlineData(820, 1180, "iPad Air")]
        public async Task TabletBreakpoints_NoHorizontalOverflow(int width, int height, string device)
        {
            await _page!.SetViewportSizeAsync(width, height);
            await _page.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var bodyWidth = await _page.EvaluateAsync<int>("document.body.scrollWidth");
            var viewportWidth = await _page.EvaluateAsync<int>("window.innerWidth");

            bodyWidth.Should().BeLessThanOrEqualTo(viewportWidth + 5,
                $"No overflow on {device} ({width}x{height}). Body: {bodyWidth}px, Viewport: {viewportWidth}px");
        }

        #endregion

        #region Orientation Tests

        [Fact]
        public async Task MobileOrientation_PortraitToLandscape_NoBreakage()
        {
            // Portrait
            await _page!.SetViewportSizeAsync(390, 844);
            await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var portraitOverflow = await _page.EvaluateAsync<bool>(
                "document.body.scrollWidth > window.innerWidth + 5");

            // Landscape
            await _page.SetViewportSizeAsync(844, 390);
            await Task.Delay(300);

            var landscapeOverflow = await _page.EvaluateAsync<bool>(
                "document.body.scrollWidth > window.innerWidth + 5");

            portraitOverflow.Should().BeFalse("No overflow in portrait mode");
            landscapeOverflow.Should().BeFalse("No overflow in landscape mode");
        }

        [Fact]
        public async Task TabletOrientation_PortraitToLandscape_NoBreakage()
        {
            // Portrait
            await _page!.SetViewportSizeAsync(768, 1024);
            await _page.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var portraitOverflow = await _page.EvaluateAsync<bool>(
                "document.body.scrollWidth > window.innerWidth + 5");

            // Landscape
            await _page.SetViewportSizeAsync(1024, 768);
            await Task.Delay(300);

            var landscapeOverflow = await _page.EvaluateAsync<bool>(
                "document.body.scrollWidth > window.innerWidth + 5");

            portraitOverflow.Should().BeFalse("No overflow in tablet portrait");
            landscapeOverflow.Should().BeFalse("No overflow in tablet landscape");
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task MobilePerformance_PageLoadTime_Under5Seconds()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await _page!.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
                $"Page must load within 5 seconds on mobile. Took: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task MobilePerformance_LoginPageLoad_Under3Seconds()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await _page!.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000,
                $"Login page must load within 3 seconds. Took: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Accessibility on Mobile Tests

        [Fact]
        public async Task MobileAccessibility_FocusManagement_TabOrderWorks()
        {
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Tab through form elements
            await _page.Keyboard.PressAsync("Tab");
            var firstFocused = await _page.EvaluateAsync<string>("document.activeElement?.tagName");

            await _page.Keyboard.PressAsync("Tab");
            var secondFocused = await _page.EvaluateAsync<string>("document.activeElement?.tagName");

            firstFocused.Should().NotBeNullOrEmpty("First tab must focus an element");
            secondFocused.Should().NotBeNullOrEmpty("Second tab must focus next element");

            // Elements should be focusable form elements
            var validTags = new[] { "INPUT", "BUTTON", "A", "SELECT", "TEXTAREA" };
            validTags.Should().Contain(firstFocused!,
                "Tab should focus interactive elements on login page");
        }

        [Fact]
        public async Task MobileAccessibility_ImagesHaveAltText()
        {
            await _page!.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var imagesWithoutAlt = await _page.EvaluateAsync<int>(@"() => {
                var images = document.querySelectorAll('img');
                var count = 0;
                images.forEach(img => {
                    if (!img.alt && !img.getAttribute('role') && img.offsetHeight > 0) count++;
                });
                return count;
            }");

            imagesWithoutAlt.Should().Be(0,
                "All visible images must have alt text for accessibility");
        }

        #endregion
    }
}
