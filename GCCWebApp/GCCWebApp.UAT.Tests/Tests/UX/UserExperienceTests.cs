namespace GCCWebApp.UAT.Tests.Tests.UX
{
    [TestFixture]
    [Category("UX")]
    [Category("UserExperience")]
    public class UserExperienceTests : UXUITestBase
    {
        [Test]
        public async Task HomePage_LoadsWithinReasonableTime()
        {
            var result = await TestPagePerformance("/");
            
            Assert.That(result.LoadTime.TotalSeconds, Is.LessThan(5), 
                $"Home page should load within 5 seconds, took {result.LoadTime.TotalSeconds:F2} seconds");
            
            Assert.That(result.StatusCode, Is.EqualTo(200), 
                $"Home page should return 200 OK, got {result.StatusCode}");
        }

        [Test]
        public async Task CertificatePages_PerformanceTest()
        {
            var pagesToTest = new[]
            {
                "/Certificate/CertificateTileView",
                "/Certificate/CertificateExternalView"
            };

            foreach (var page in pagesToTest)
            {
                var result = await TestPagePerformance(page);
                
                Assert.That(result.LoadTime.TotalSeconds, Is.LessThan(8), 
                    $"Page {page} should load within 8 seconds, took {result.LoadTime.TotalSeconds:F2} seconds");
            }
        }

        [Test]
        public async Task Navigation_IsUserFriendly()
        {
            await TestPage.GotoAsync($"{BaseUrl}");
            
            var result = await TestNavigationUsability();
            
            Assert.That(result.HasMainNavigation, Is.True, 
                "Application should have clear main navigation");
            
            Assert.That(result.NavigationLinksCount, Is.GreaterThan(0), 
                "Navigation should contain actionable links");
            
            Assert.That(result.KeyboardNavigationWorks, Is.True, 
                "Navigation should be accessible via keyboard");
        }

        [Test]
        public async Task LoginForm_UsabilityTest()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            
            var result = await TestFormUsability("form");
            
            Assert.That(result.TotalInputs, Is.GreaterThan(0), 
                "Login form should have input fields");
            
            // At least 80% of inputs should have proper labels or accessibility features
            var labeledInputs = result.InputsWithLabels + result.InputsWithAriaLabels + result.InputsWithPlaceholders;
            var labelPercentage = (double)labeledInputs / result.TotalInputs;
            
            Assert.That(labelPercentage, Is.GreaterThan(0.8), 
                $"At least 80% of form inputs should be properly labeled, found {labelPercentage:P0}");
        }

        [Test]
        public async Task UserManagementForm_UsabilityTest()
        {
            // Login as admin
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);
            
            await TestPage.GotoAsync($"{BaseUrl}/Admin/UserManagement/Create");
            
            var result = await TestFormUsability("form");
            
            Assert.That(result.TotalInputs, Is.GreaterThan(0), 
                "User management form should have input fields");
            
            Assert.That(result.InputsWithLabels, Is.GreaterThan(0), 
                "User management form should use proper form labels");
            
            Console.WriteLine($"Form usability: {result.InputsWithLabels}/{result.TotalInputs} inputs have labels");
            Console.WriteLine($"Tab order test: {result.TabOrderTest}");
        }

        [Test]
        public async Task SearchFunctionality_UserFriendly()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            
            // Look for search functionality
            var searchInputs = await TestPage.QuerySelectorAllAsync("input[type='search'], input[name*='search'], .search-input");
            
            if (searchInputs.Any())
            {
                var searchInput = searchInputs.First();
                
                // Check for placeholder text
                var placeholder = await searchInput.GetAttributeAsync("placeholder");
                Assert.That(placeholder, Is.Not.Null.And.Not.Empty, 
                    "Search input should have helpful placeholder text");
                
                // Test search functionality
                await searchInput.FillAsync("Green Circle");
                await TestPage.Keyboard.PressAsync("Enter");
                await TestPage.WaitForTimeoutAsync(2000);
                
                // Check for search results or feedback
                var hasResults = await TestPage.QuerySelectorAsync(".search-results, .results, .certificate-tile, .certificate-item") != null;
                var hasNoResultsMessage = await TestPage.QuerySelectorAsync(".no-results, .empty-state") != null;
                
                Assert.That(hasResults || hasNoResultsMessage, Is.True, 
                    "Search should show either results or a 'no results' message");
            }
            else
            {
                Console.WriteLine("No search functionality found on certificate page");
            }
        }

        [Test]
        public async Task ErrorHandling_UserFriendly()
        {
            // Test 404 error page
            await TestPage.GotoAsync($"{BaseUrl}/nonexistent-page");
            
            var pageContent = await TestPage.ContentAsync();
            var hasErrorMessage = pageContent.Contains("404", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("page doesn't exist", StringComparison.OrdinalIgnoreCase);
            
            Assert.That(hasErrorMessage, Is.True, 
                "404 error page should display user-friendly error message");
            
            // Check for navigation back to home
            var homeLinks = await TestPage.QuerySelectorAllAsync("a[href='/'], a[href=''], .home-link");
            Assert.That(homeLinks.Any(), Is.True, 
                "Error page should provide way to navigate back to home");
        }

        [Test]
        public async Task Accessibility_MeetsBasicStandards()
        {
            await TestPage.GotoAsync($"{BaseUrl}");
            
            var result = await TestAccessibilityFeatures();
            
            Assert.That(result.HasProperHeadingHierarchy, Is.True, 
                $"Page should start with H1 heading, found structure: {result.HeadingStructure}");
            
            Assert.That(result.LandmarksCount, Is.GreaterThan(0), 
                "Page should have semantic landmarks (main, nav, header, footer)");
            
            if (result.TotalImages > 0)
            {
                var altTextPercentage = (double)result.ImagesWithAltText / result.TotalImages;
                Assert.That(altTextPercentage, Is.GreaterThan(0.8), 
                    $"At least 80% of images should have alt text, found {altTextPercentage:P0}");
            }
            
            Console.WriteLine($"Accessibility check: {result.ImagesWithAltText}/{result.TotalImages} images have alt text");
            Console.WriteLine($"Background: {result.BackgroundColor}, Text: {result.TextColor}");
        }

        [Test]
        public async Task MobileResponsiveness_KeyPages()
        {
            var pagesToTest = new[]
            {
                "/",
                "/Account/Login", 
                "/Certificate/CertificateTileView"
            };

            foreach (var page in pagesToTest)
            {
                await TestPage.GotoAsync($"{BaseUrl}{page}");
                
                var result = await TestMobileUsability();
                
                Assert.That(result.HasViewportMeta, Is.True, 
                    $"Page {page} should have viewport meta tag for mobile optimization");
                
                Assert.That(result.HasHorizontalScroll, Is.False, 
                    $"Page {page} should not have horizontal scrolling on mobile");
                
                if (result.TotalClickableElements > 0)
                {
                    var touchFriendlyPercentage = (double)result.TouchFriendlyElements / result.TotalClickableElements;
                    Assert.That(touchFriendlyPercentage, Is.GreaterThan(0.7), 
                        $"Page {page}: At least 70% of clickable elements should be touch-friendly (44px+), found {touchFriendlyPercentage:P0}");
                }
                
                Console.WriteLine($"Mobile usability for {page}: {result.TouchFriendlyElements}/{result.TotalClickableElements} elements are touch-friendly");
            }
        }

        [Test]
        public async Task LoadingStates_ProvideGoodUX()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            
            // Check for loading indicators
            var loadingIndicators = await TestPage.QuerySelectorAllAsync(".loading, .spinner, .loader, .progress");
            
            // Check for skeleton screens or placeholders
            var placeholders = await TestPage.QuerySelectorAllAsync(".placeholder, .skeleton, .loading-placeholder");
            
            Console.WriteLine($"Found {loadingIndicators.Count()} loading indicators and {placeholders.Count()} placeholders");
            
            // At least one form of loading feedback is good UX
            Assert.That(loadingIndicators.Any() || placeholders.Any(), Is.True, 
                "Pages with dynamic content should provide loading feedback");
        }

        [Test]
        public async Task FormFeedback_IsHelpful()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            
            // Try to submit empty form
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(1000);
            
            // Check for validation messages
            var errorMessages = await TestPage.QuerySelectorAllAsync(".field-validation-error, .invalid-feedback, .error-message, .validation-error");
            
            if (errorMessages.Any())
            {
                foreach (var error in errorMessages)
                {
                    var text = await error.TextContentAsync();
                    Assert.That(text, Is.Not.Null.And.Not.Empty, 
                        "Error messages should contain helpful text");
                    
                    // Error messages should be descriptive, not just "Required"
                    Console.WriteLine($"Validation message: {text}");
                }
            }
            
            // Check for visual feedback (red borders, icons, etc.)
            var invalidFields = await TestPage.QuerySelectorAllAsync(".is-invalid, .error, .field-validation-error");
            
            Console.WriteLine($"Found {errorMessages.Count()} error messages and {invalidFields.Count()} visually marked invalid fields");
        }

        [Test]
        public async Task DataTables_AreUsable()
        {
            // Login as admin to access data tables
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);
            
            var tablePages = new[]
            {
                "/Admin/UserManagement",
                "/Certificate/Certificates"
            };

            foreach (var page in tablePages)
            {
                try
                {
                    await TestPage.GotoAsync($"{BaseUrl}{page}");
                    await TestPage.WaitForTimeoutAsync(1000);
                    
                    var tables = await TestPage.QuerySelectorAllAsync("table");
                    
                    foreach (var table in tables)
                    {
                        // Check for proper table headers
                        var headers = await table.QuerySelectorAllAsync("th");
                        Assert.That(headers.Any(), Is.True, 
                            $"Data table on {page} should have column headers");
                        
                        // Check for sortable columns
                        var sortableHeaders = await table.QuerySelectorAllAsync("th[aria-sort], th.sortable, th[data-sort]");
                        
                        // Check for pagination if table has many rows
                        var rows = await table.QuerySelectorAllAsync("tbody tr");
                        if (rows.Count() > 10)
                        {
                            var paginationElements = await TestPage.QuerySelectorAllAsync(".pagination, .pager, .page-nav");
                            Console.WriteLine($"Table with {rows.Count()} rows has pagination: {paginationElements.Any()}");
                        }
                        
                        Console.WriteLine($"Table on {page}: {headers.Count()} headers, {sortableHeaders.Count()} sortable, {rows.Count()} rows");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not test table on {page}: {ex.Message}");
                }
            }
        }

        [Test]
        public async Task BreadcrumbNavigation_ImprovesSiteNavigation()
        {
            // Login as admin to access deeper pages
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);
            
            await TestPage.GotoAsync($"{BaseUrl}/Admin/UserManagement/Create");
            
            // Check for breadcrumbs
            var breadcrumbs = await TestPage.QuerySelectorAllAsync(".breadcrumb, nav[aria-label*='breadcrumb'], .breadcrumb-nav");
            
            if (breadcrumbs.Any())
            {
                var breadcrumbLinks = await TestPage.QuerySelectorAllAsync(".breadcrumb a, nav[aria-label*='breadcrumb'] a");
                Assert.That(breadcrumbLinks.Any(), Is.True, 
                    "Breadcrumbs should contain clickable links to parent pages");
                
                Console.WriteLine($"Found breadcrumb navigation with {breadcrumbLinks.Count()} links");
            }
            else
            {
                Console.WriteLine("No breadcrumb navigation found - consider adding for better UX");
            }
        }
    }
}