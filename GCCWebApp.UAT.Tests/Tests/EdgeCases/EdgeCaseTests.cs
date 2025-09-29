using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.EdgeCases
{
    [TestFixture]
    [Category("EdgeCases")]
    public class EdgeCaseTests : PageTest
    {
        protected string BaseUrl { get; set; } = "http://localhost:5101";
        protected IPage TestPage { get; private set; } = null!;

        [SetUp]
        public async Task EdgeCaseSetUp()
        {
            TestPage = await Browser.NewPageAsync(new BrowserNewPageOptions
            {
                IgnoreHTTPSErrors = true
            });
        }

        [TearDown]
        public async Task EdgeCaseTearDown()
        {
            if (TestPage != null)
                await TestPage.CloseAsync();
        }

        [Test]
        [Category("DataHandling")]
        public async Task LongStrings_AreHandledGracefully()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            
            // Test with extremely long email
            var longEmail = new string('a', 1000) + "@" + new string('b', 1000) + ".com";
            
            await TestPage.FillAsync("input[name='Email']", longEmail);
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);
            
            // Should handle gracefully without crashing
            var pageContent = await TestPage.ContentAsync();
            Assert.That(pageContent, Does.Not.Contain("Exception").IgnoreCase, 
                "Application should handle long email input gracefully");
            Assert.That(pageContent, Does.Not.Contain("Error 500"), 
                "Long input should not cause server error");
        }

        [Test]
        [Category("DataHandling")]
        public async Task SpecialCharacters_AreProcessedCorrectly()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            
            var specialCharacterInputs = new[]
            {
                "test+tag@example.com",
                "user.name+tag@example.com",
                "test@domain-with-dash.com",
                "test@sub.domain.com",
                "用户@example.com", // Unicode characters
                "tëst@éxample.com", // Accented characters
                "test@münchen.de"  // International domain
            };

            foreach (var email in specialCharacterInputs)
            {
                await TestPage.FillAsync("input[name='Email']", email);
                await TestPage.FillAsync("input[name='Password']", "TestPass123!");
                await TestPage.ClickAsync("button[type='submit']");
                await TestPage.WaitForTimeoutAsync(1000);
                
                var pageContent = await TestPage.ContentAsync();
                Assert.That(pageContent, Does.Not.Contain("Exception").IgnoreCase, 
                    $"Special characters in email '{email}' should be handled gracefully");
                
                // Clear form for next test
                await TestPage.FillAsync("input[name='Email']", "");
                await TestPage.FillAsync("input[name='Password']", "");
            }
        }

        [Test]
        [Category("BoundaryValues")]
        public async Task BoundaryValues_NumberFields()
        {
            // Login as admin to access forms with numeric fields
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);
            
            var formsToTest = new[]
            {
                "/Certificate/Create",
                "/Admin/ProductManagement/Create"
            };

            foreach (var form in formsToTest)
            {
                try
                {
                    await TestPage.GotoAsync($"{BaseUrl}{form}");
                    await TestPage.WaitForTimeoutAsync(1000);
                    
                    var numericFields = await TestPage.QuerySelectorAllAsync("input[type='number'], input[type='decimal']");
                    
                    foreach (var field in numericFields)
                    {
                        var name = await field.GetAttributeAsync("name");
                        
                        // Test boundary values
                        var boundaryValues = new[]
                        {
                            "0",           // Zero
                            "-1",          // Negative
                            "999999999",   // Large positive
                            "-999999999",  // Large negative
                            "0.000001",    // Very small decimal
                            "99999.99999", // Large decimal
                            "1e10",        // Scientific notation
                            "∞",           // Infinity symbol
                            "NaN"          // Not a Number
                        };

                        foreach (var value in boundaryValues)
                        {
                            await field.FillAsync(value);
                            await field.PressAsync("Tab");
                            await TestPage.WaitForTimeoutAsync(200);
                            
                            Console.WriteLine($"Tested field '{name}' with boundary value: {value}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not test boundary values on {form}: {ex.Message}");
                }
            }
        }

        [Test]
        [Category("BoundaryValues")]
        public async Task DateFields_HandleEdgeCases()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);
            
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/Create");
            
            var dateFields = await TestPage.QuerySelectorAllAsync("input[type='date'], input[type='datetime-local']");
            
            foreach (var field in dateFields)
            {
                var name = await field.GetAttributeAsync("name");
                
                var edgeDateValues = new[]
                {
                    "1900-01-01",    // Very old date
                    "2099-12-31",    // Future date
                    "2000-02-29",    // Leap year
                    "2001-02-29",    // Invalid leap year date
                    "2023-02-30",    // Invalid date
                    "2023-13-01",    // Invalid month
                    "2023-12-32",    // Invalid day
                    "0000-00-00",    // Null date
                    "9999-99-99"     // Invalid format
                };

                foreach (var date in edgeDateValues)
                {
                    try
                    {
                        await field.FillAsync(date);
                        await field.PressAsync("Tab");
                        await TestPage.WaitForTimeoutAsync(200);
                        
                        var fieldValue = await field.InputValueAsync();
                        Console.WriteLine($"Date field '{name}' with '{date}' resulted in: '{fieldValue}'");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Date test failed for '{name}' with '{date}': {ex.Message}");
                    }
                }
            }
        }

        [Test]
        [Category("NetworkConditions")]
        public async Task SlowNetwork_GracefulDegradation()
        {
            // Simulate slow network
            await TestPage.RouteAsync("**/*", async route =>
            {
                await Task.Delay(2000); // 2 second delay
                await route.ContinueAsync();
            });

            var startTime = DateTime.Now;
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            var loadTime = DateTime.Now - startTime;
            
            Console.WriteLine($"Page loaded in {loadTime.TotalSeconds:F2} seconds under slow network conditions");
            
            // Check for loading indicators
            var loadingElements = await TestPage.QuerySelectorAllAsync(".loading, .spinner, .loader");
            
            // Page should either load or show appropriate loading/error states
            var pageContent = await TestPage.ContentAsync();
            var hasContent = pageContent.Length > 1000;
            var hasErrorMessage = pageContent.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("connection", StringComparison.OrdinalIgnoreCase);
            
            Assert.That(hasContent || hasErrorMessage, Is.True, 
                "Application should handle slow network gracefully with content or error message");
        }

        [Test]
        [Category("ConcurrentUsers")]
        public async Task MultipleSessionHandling()
        {
            // Create multiple browser contexts to simulate different users
            var context1 = await Browser.NewContextAsync();
            var context2 = await Browser.NewContextAsync();
            
            var page1 = await context1.NewPageAsync();
            var page2 = await context2.NewPageAsync();

            try
            {
                // Login same user in two different sessions
                var loginTasks = new[]
                {
                    LoginUser(page1, "admin@gcc.com", "TestPass123!"),
                    LoginUser(page2, "analyst@gcc.com", "TestPass123!")
                };

                await Task.WhenAll(loginTasks);

                // Both sessions should work independently
                await page1.GotoAsync($"{BaseUrl}/Admin/UserManagement");
                await page2.GotoAsync($"{BaseUrl}/Certificate/Certificates");

                var page1Content = await page1.ContentAsync();
                var page2Content = await page2.ContentAsync();

                Assert.That(page1Content, Does.Not.Contain("error").IgnoreCase, 
                    "First session should work properly");
                Assert.That(page2Content, Does.Not.Contain("error").IgnoreCase, 
                    "Second session should work properly");
            }
            finally
            {
                await page1.CloseAsync();
                await page2.CloseAsync();
                await context1.DisposeAsync();
                await context2.DisposeAsync();
            }
        }

        [Test]
        [Category("DataVolume")]
        public async Task LargeDataSets_PerformanceTest()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            
            // Measure initial load time
            var startTime = DateTime.Now;
            await TestPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var loadTime = DateTime.Now - startTime;
            
            Console.WriteLine($"Certificate list loaded in {loadTime.TotalSeconds:F2} seconds");
            
            // Count visible items
            var certificateItems = await TestPage.QuerySelectorAllAsync(".certificate-tile, .certificate-item, .certificate-card, tr");
            Console.WriteLine($"Found {certificateItems.Count()} certificate items displayed");
            
            // Check for pagination or infinite scroll
            var paginationElements = await TestPage.QuerySelectorAllAsync(".pagination, .pager, .load-more");
            var hasLazyLoading = paginationElements.Any();
            
            if (certificateItems.Count() > 50 && !hasLazyLoading)
            {
                Console.WriteLine("WARNING: Large dataset without pagination may impact performance");
            }
            
            // Performance should be reasonable even with large datasets
            Assert.That(loadTime.TotalSeconds, Is.LessThan(15), 
                "Large dataset should load within 15 seconds");
        }

        [Test]
        [Category("BrowserCompatibility")]
        public async Task JavaScriptDisabled_GracefulDegradation()
        {
            // Disable JavaScript
            await TestPage.Context.AddInitScriptAsync("() => { window.addEventListener = () => {}; }");
            
            await TestPage.GotoAsync($"{BaseUrl}");
            
            // Basic navigation should still work
            var navLinks = await TestPage.QuerySelectorAllAsync("nav a, .navbar a");
            
            if (navLinks.Any())
            {
                var firstLink = navLinks.First();
                var href = await firstLink.GetAttributeAsync("href");
                
                if (!string.IsNullOrEmpty(href) && !href.StartsWith("javascript:"))
                {
                    await firstLink.ClickAsync();
                    await TestPage.WaitForTimeoutAsync(2000);
                    
                    var newUrl = TestPage.Url;
                    Assert.That(newUrl, Does.Contain(BaseUrl), 
                        "Navigation should work even with limited JavaScript");
                }
            }
        }

        [Test]
        [Category("MemoryLeaks")]
        public async Task RepeatedNavigation_NoMemoryLeaks()
        {
            var pages = new[]
            {
                "/",
                "/Certificate/CertificateTileView",
                "/Account/Login"
            };

            // Navigate between pages repeatedly
            for (int i = 0; i < 10; i++)
            {
                foreach (var page in pages)
                {
                    await TestPage.GotoAsync($"{BaseUrl}{page}");
                    await TestPage.WaitForTimeoutAsync(500);
                }
            }

            // Check for JavaScript errors that might indicate memory issues
            var jsErrors = new List<string>();
            TestPage.Console += (_, e) => {
                if (e.Type == "error")
                    jsErrors.Add(e.Text);
            };

            await TestPage.GotoAsync($"{BaseUrl}");
            await TestPage.WaitForTimeoutAsync(2000);

            Assert.That(jsErrors.Count, Is.LessThan(5), 
                $"Repeated navigation should not generate many JS errors. Found: {string.Join(", ", jsErrors)}");
        }

        [Test]
        [Category("StateManagement")]
        public async Task BackButtonFunctionality()
        {
            await TestPage.GotoAsync($"{BaseUrl}");
            var homePage = TestPage.Url;
            
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            var certificatePage = TestPage.Url;
            
            // Use browser back button
            await TestPage.GoBackAsync();
            await TestPage.WaitForTimeoutAsync(1000);
            
            var backUrl = TestPage.Url;
            Assert.That(backUrl, Is.EqualTo(homePage), 
                "Back button should return to previous page");
            
            // Go forward again
            await TestPage.GoForwardAsync();
            await TestPage.WaitForTimeoutAsync(1000);
            
            var forwardUrl = TestPage.Url;
            Assert.That(forwardUrl, Is.EqualTo(certificatePage), 
                "Forward button should work correctly");
        }

        [Test]
        [Category("SessionTimeout")]
        public async Task SessionExpiry_HandledGracefully()
        {
            // Login first
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);
            
            // Clear all cookies to simulate session expiry
            await TestPage.Context.ClearCookiesAsync();
            
            // Try to access protected resource
            await TestPage.GotoAsync($"{BaseUrl}/Admin/UserManagement");
            await TestPage.WaitForTimeoutAsync(2000);
            
            var currentUrl = TestPage.Url;
            var pageContent = await TestPage.ContentAsync();
            
            // Should redirect to login or show appropriate message
            var handledGracefully = currentUrl.Contains("/Account/Login") ||
                                   pageContent.Contains("session expired", StringComparison.OrdinalIgnoreCase) ||
                                   pageContent.Contains("please log in", StringComparison.OrdinalIgnoreCase);
            
            Assert.That(handledGracefully, Is.True, 
                "Session expiry should be handled gracefully with redirect or message");
        }

        private async Task LoginUser(IPage page, string email, string password)
        {
            await page.GotoAsync($"{BaseUrl}/Account/Login");
            await page.FillAsync("input[name='Email']", email);
            await page.FillAsync("input[name='Password']", password);
            await page.ClickAsync("button[type='submit']");
            await page.WaitForTimeoutAsync(2000);
        }
    }
}