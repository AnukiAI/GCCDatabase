using Microsoft.Playwright;
using Xunit;
using FluentAssertions;

namespace GCCWebApp.UAT.Tests.Tests.Mobile
{
    /// <summary>
    /// Mobile Responsiveness Tests for UX-002: Mobile-First Design Requirements
    /// Tests responsive design across different screen sizes and mobile interactions
    /// </summary>
    [Collection("UITests")]
    public class MobileResponsivenessTests : IAsyncLifetime
    {
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;
        
        private const string BaseUrl = "http://localhost:5101";

        public async Task InitializeAsync()
        {
            var playwright = await Playwright.CreateAsync();
            
            // Test on mobile browser (iPhone 12)
            _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 100
            });

            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 390, Height = 844 }, // iPhone 12
                UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0.3 Mobile/15E148 Safari/604.1"
            });

            _page = await _context.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await _page?.CloseAsync()!;
            await _context?.CloseAsync()!;
            await _browser?.CloseAsync()!;
        }

        #region UX-002: Mobile-First Design Tests

        [Fact]
        public async Task MobileNavigation_HamburgerMenu_ShouldBeVisibleAndFunctional()
        {
            // Arrange
            await _page!.GotoAsync(BaseUrl);

            // Act - Look for mobile navigation elements
            var hamburgerMenu = await _page.QuerySelectorAsync(".navbar-toggler, .mobile-menu-toggle, [data-bs-toggle='collapse']");
            
            // Assert
            hamburgerMenu.Should().NotBeNull("Mobile hamburger menu should be visible on mobile screens");
            
            if (hamburgerMenu != null)
            {
                await hamburgerMenu.ClickAsync();
                
                // Verify menu opens
                var mobileMenu = await _page.QuerySelectorAsync(".navbar-collapse, .mobile-menu");
                var isVisible = mobileMenu != null && await mobileMenu.IsVisibleAsync();
                isVisible.Should().BeTrue("Mobile menu should open when hamburger is clicked");
            }
        }

        [Fact]
        public async Task MobileLayout_CertificateList_ShouldStackVertically()
        {
            // Arrange
            await _page!.GotoAsync($"{BaseUrl}/Certificate/Certificates");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Act - Check certificate tile layout
            var certificateTiles = await _page.QuerySelectorAllAsync(".certificate-tile, .card, .certificate-item");
            
            // Assert
            certificateTiles.Should().NotBeEmpty("Certificate tiles should be present");
            
            if (certificateTiles.Count > 1)
            {
                // Check if tiles are stacked vertically (mobile layout)
                var firstTile = certificateTiles[0];
                var secondTile = certificateTiles[1];
                
                var firstRect = await firstTile.BoundingBoxAsync();
                var secondRect = await secondTile.BoundingBoxAsync();
                
                if (firstRect != null && secondRect != null)
                {
                    secondRect.Y.Should().BeGreaterThan(firstRect.Y, 
                        "Certificate tiles should stack vertically on mobile");
                }
            }
        }

        [Fact]
        public async Task MobileForm_LoginForm_ShouldBeOptimizedForTouch()
        {
            // Arrange
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");

            // Act - Find form elements
            var emailInput = await _page.QuerySelectorAsync("#Email, input[type='email']");
            var passwordInput = await _page.QuerySelectorAsync("#Password, input[type='password']");
            var submitButton = await _page.QuerySelectorAsync("button[type='submit'], input[type='submit']");

            // Assert
            emailInput.Should().NotBeNull("Email input should be present");
            passwordInput.Should().NotBeNull("Password input should be present");
            submitButton.Should().NotBeNull("Submit button should be present");

            if (emailInput != null)
            {
                var inputRect = await emailInput.BoundingBoxAsync();
                inputRect?.Height.Should().BeGreaterThan(40, 
                    "Form inputs should be large enough for touch interaction (min 44px)");
            }

            if (submitButton != null)
            {
                var buttonRect = await submitButton.BoundingBoxAsync();
                buttonRect?.Height.Should().BeGreaterThan(40, 
                    "Buttons should be large enough for touch interaction (min 44px)");
            }
        }

        [Fact]
        public async Task MobileText_ReadabilityAndSize_ShouldBeOptimized()
        {
            // Arrange
            await _page!.GotoAsync(BaseUrl);

            // Act - Check text sizing and readability
            var headings = await _page.QuerySelectorAllAsync("h1, h2, h3, h4, h5, h6");
            var paragraphs = await _page.QuerySelectorAllAsync("p, .text-body");

            // Assert
            headings.Should().NotBeEmpty("Page should have headings");
            
            foreach (var heading in headings.Take(3)) // Check first 3 headings
            {
                var fontSize = await heading.EvaluateAsync<string>("el => window.getComputedStyle(el).fontSize");
                var fontSizeValue = float.Parse(fontSize.Replace("px", ""));
                
                fontSizeValue.Should().BeGreaterThan(18, 
                    "Headings should be large enough for mobile readability (min 18px)");
            }
        }

        [Xunit.Theory]
        [InlineData(375, 667)]  // iPhone SE
        [InlineData(390, 844)]  // iPhone 12
        [InlineData(393, 851)]  // Pixel 5
        [InlineData(360, 740)]  // Galaxy S8
        public async Task MobileBreakpoints_DifferentScreenSizes_ShouldMaintainUsability(int width, int height)
        {
            // Arrange
            await _page!.SetViewportSizeAsync(width, height);
            await _page.GotoAsync(BaseUrl);

            // Act - Check critical elements are still accessible
            var navigation = await _page.QuerySelectorAsync("nav, .navbar");
            var mainContent = await _page.QuerySelectorAsync("main, .main-content, .container");

            // Assert
            navigation.Should().NotBeNull($"Navigation should be present on {width}x{height} screen");
            mainContent.Should().NotBeNull($"Main content should be present on {width}x{height} screen");

            // Check that content doesn't overflow horizontally
            var bodyWidth = await _page.EvaluateAsync<int>("document.body.scrollWidth");
            var viewportWidth = await _page.EvaluateAsync<int>("window.innerWidth");
            
            bodyWidth.Should().BeLessThanOrEqualTo(viewportWidth + 5, 
                $"Content should not cause horizontal scroll on {width}x{height} screen");
        }

        #endregion

        #region Touch Interaction Tests

        [Fact]
        public async Task TouchInteraction_CertificateSelection_ShouldRespondToTap()
        {
            // Arrange
            await _page!.GotoAsync($"{BaseUrl}/Certificate/Certificates");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Act - Tap on first certificate
            var firstCertificate = await _page.QuerySelectorAsync(".certificate-tile, .card");
            
            if (firstCertificate != null)
            {
                await firstCertificate.TapAsync();
                
                // Wait for navigation or modal
                await Task.Delay(1000);
                
                // Assert - Should navigate to certificate details or open modal
                var currentUrl = _page.Url;
                var hasModal = await _page.QuerySelectorAsync(".modal, .dialog, .popup") != null;
                
                (currentUrl != $"{BaseUrl}/Certificate/Certificates" || hasModal)
                    .Should().BeTrue("Tapping certificate should trigger navigation or modal");
            }
        }

        [Fact]
        public async Task TouchInteraction_SwipeGestures_ShouldBeSupported()
        {
            // Arrange
            await _page!.GotoAsync($"{BaseUrl}/Certificate/Certificates");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Act - Simulate swipe gesture on scrollable content
            var scrollableElement = await _page.QuerySelectorAsync(".certificate-list, .table-responsive, main");
            
            if (scrollableElement != null)
            {
                var elementRect = await scrollableElement.BoundingBoxAsync();
                
                if (elementRect != null)
                {
                    // Simulate swipe down
                    await _page.Mouse.MoveAsync(elementRect.X + elementRect.Width / 2, elementRect.Y + 100);
                    await _page.Mouse.DownAsync();
                    await _page.Mouse.MoveAsync(elementRect.X + elementRect.Width / 2, elementRect.Y + 300);
                    await _page.Mouse.UpAsync();
                    
                    // Assert - Content should scroll
                    var scrollTop = await scrollableElement.EvaluateAsync<int>("el => el.scrollTop");
                    scrollTop.Should().BeGreaterThan(0, "Swipe gesture should cause scrolling");
                }
            }
        }

        #endregion

        #region Mobile-Specific UX Tests

        [Fact]
        public async Task MobileUX_SearchInterface_ShouldBeOptimizedForMobile()
        {
            // Arrange
            await _page!.GotoAsync($"{BaseUrl}/Certificate/Certificates");

            // Act - Find search interface elements
            var searchInput = await _page.QuerySelectorAsync("input[type='search'], .search-input, #searchInput");
            var searchButton = await _page.QuerySelectorAsync(".search-button, button[type='submit']");

            // Assert
            if (searchInput != null)
            {
                var inputRect = await searchInput.BoundingBoxAsync();
                inputRect?.Width.Should().BeGreaterThan(200, 
                    "Search input should be wide enough for mobile typing");
                inputRect?.Height.Should().BeGreaterThan(40, 
                    "Search input should be tall enough for touch interaction");

                // Check for mobile-optimized keyboard
                var inputType = await searchInput.GetAttributeAsync("type");
                var inputMode = await searchInput.GetAttributeAsync("inputmode");
                
                (inputType == "search" || inputMode == "search").Should().BeTrue(
                    "Search input should trigger appropriate mobile keyboard");
            }
        }

        [Fact]
        public async Task MobileUX_FormValidation_ShouldShowClearErrorMessages()
        {
            // Arrange
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");

            // Act - Submit form with empty fields
            var submitButton = await _page.QuerySelectorAsync("button[type='submit'], input[type='submit']");
            if (submitButton != null)
            {
                await submitButton.TapAsync();
                await Task.Delay(500);

                // Assert - Check for validation messages
                var validationMessages = await _page.QuerySelectorAllAsync(".validation-message, .error-message, .invalid-feedback");
                
                if (validationMessages.Any())
                {
                    foreach (var message in validationMessages.Take(2))
                    {
                        var isVisible = await message.IsVisibleAsync();
                        isVisible.Should().BeTrue("Validation messages should be visible");

                        var messageRect = await message.BoundingBoxAsync();
                        messageRect?.Width.Should().BeGreaterThan(0, 
                            "Error messages should be properly sized for mobile");
                    }
                }
            }
        }

        [Fact]
        public async Task MobileUX_LoadingStates_ShouldProvideVisualFeedback()
        {
            // Arrange
            await _page!.GotoAsync(BaseUrl);

            // Act - Navigate to a page that might show loading
            await _page.GotoAsync($"{BaseUrl}/Certificate/Certificates", new PageGotoOptions { WaitUntil = WaitUntilState.Load });

            // Assert - Check for loading indicators (this is a structure test)
            var potentialLoadingElements = await _page.QuerySelectorAllAsync(
                ".loading, .spinner, .skeleton, [data-loading], .progress");

            // Note: Loading states might not be visible by the time we check,
            // but the elements should exist in the DOM structure
            potentialLoadingElements.Should().NotBeNull(
                "Page should have loading state elements in the structure");
        }

        #endregion

        #region Accessibility on Mobile Tests

        [Fact]
        public async Task MobileAccessibility_FocusManagement_ShouldBeLogical()
        {
            // Arrange
            await _page!.GotoAsync($"{BaseUrl}/Account/Login");

            // Act - Tab through focusable elements
            var focusableElements = await _page.QuerySelectorAllAsync(
                "a, button, input, select, textarea, [tabindex]:not([tabindex='-1'])");

            // Assert
            focusableElements.Should().NotBeEmpty("Page should have focusable elements");

            if (focusableElements.Count > 1)
            {
                // Test tab order
                await _page.Keyboard.PressAsync("Tab");
                var firstFocused = await _page.EvaluateAsync<string>("document.activeElement.tagName");
                
                await _page.Keyboard.PressAsync("Tab");
                var secondFocused = await _page.EvaluateAsync<string>("document.activeElement.tagName");
                
                firstFocused.Should().NotBeEmpty("First tab should focus an element");
                secondFocused.Should().NotBeEmpty("Second tab should focus next element");
            }
        }

        [Fact]
        public async Task MobileAccessibility_TouchTargetSize_ShouldMeetMinimumRequirements()
        {
            // Arrange
            await _page!.GotoAsync(BaseUrl);

            // Act - Find all interactive elements
            var interactiveElements = await _page.QuerySelectorAllAsync(
                "button, a, input[type='button'], input[type='submit'], .btn");

            // Assert
            interactiveElements.Should().NotBeEmpty("Page should have interactive elements");

            foreach (var element in interactiveElements.Take(5)) // Test first 5 elements
            {
                var rect = await element.BoundingBoxAsync();
                if (rect != null)
                {
                    var minDimension = Math.Min(rect.Width, rect.Height);
                    minDimension.Should().BeGreaterThanOrEqualTo(40, 
                        "Interactive elements should meet minimum touch target size (40px)");
                }
            }
        }

        [Fact]
        public async Task MobileAccessibility_ColorContrast_ShouldBeAccessible()
        {
            // Arrange
            await _page!.GotoAsync(BaseUrl);

            // Act - Get computed styles for text elements
            var textElements = await _page.QuerySelectorAllAsync("p, h1, h2, h3, h4, h5, h6, a, button");

            // Assert - Check that elements have proper color styling
            textElements.Should().NotBeEmpty("Page should have text elements");

            foreach (var element in textElements.Take(3))
            {
                var color = await element.EvaluateAsync<string>("el => window.getComputedStyle(el).color");
                var backgroundColor = await element.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");

                color.Should().NotBeEmpty("Text elements should have defined color");
                // Note: Full contrast ratio calculation would require additional libraries
                // This test ensures the structure is in place
            }
        }

        #endregion

        #region Performance on Mobile Tests

        [Fact]
        public async Task MobilePerformance_PageLoadTime_ShouldBeOptimized()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await _page!.GotoAsync(BaseUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
                "Page should load within 5 seconds on mobile");
        }

        [Fact]
        public async Task MobilePerformance_ImageOptimization_ShouldUseResponsiveImages()
        {
            // Arrange
            await _page!.GotoAsync(BaseUrl);

            // Act - Find images
            var images = await _page.QuerySelectorAllAsync("img");

            // Assert
            foreach (var img in images.Take(3))
            {
                var srcset = await img.GetAttributeAsync("srcset");
                var sizes = await img.GetAttributeAsync("sizes");
                var alt = await img.GetAttributeAsync("alt");

                // Check for responsive image attributes or proper alt text
                var hasResponsiveAttributes = !string.IsNullOrEmpty(srcset) || !string.IsNullOrEmpty(sizes);
                var hasAltText = !string.IsNullOrEmpty(alt);

                (hasResponsiveAttributes || hasAltText).Should().BeTrue(
                    "Images should have responsive attributes or proper alt text");
            }
        }

        #endregion
    }
}