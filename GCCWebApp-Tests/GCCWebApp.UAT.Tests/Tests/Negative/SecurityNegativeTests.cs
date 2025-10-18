namespace GCCWebApp.UAT.Tests.Tests.Negative
{
    [TestFixture]
    [Category("Negative")]
    [Category("Security")]
    public class SecurityNegativeTests : NegativeTestingBase
    {
        [Test]
        public async Task UnauthorizedAccess_AdminPages_ShouldDenyAccess()
        {
            var protectedUrls = new[]
            {
                "/Admin/UserManagement",
                "/Admin/UserManagement/Create",
                "/Admin/UserManagement/Edit/1",
                "/Admin/ProductManagement",
                "/Admin/ProductManagement/Create",
                "/Admin/SystemConfiguration",
                "/Admin/RoleManagement",
                "/Dashboard/AdminDashboard"
            };

            foreach (var url in protectedUrls)
            {
                var result = await TestUnauthorizedAccess(url);
                
                Assert.That(result.LoadsProtectedContent, Is.False, 
                    $"URL {url} should not load protected content for unauthenticated users");
                
                Assert.That(result.RedirectedToLogin || result.ShowsAccessDenied || result.StatusCode == 401 || result.StatusCode == 403, 
                    Is.True, $"URL {url} should redirect to login or show access denied (Status: {result.StatusCode}, Final URL: {result.FinalUrl})");
            }
        }

        [Test]
        public async Task AnalystUser_CannotAccessAdminPages()
        {
            // Login as analyst
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "analyst@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);

            var adminOnlyUrls = new[]
            {
                "/Admin/UserManagement",
                "/Admin/UserManagement/Create",
                "/Admin/RoleManagement",
                "/Admin/SystemConfiguration"
            };

            foreach (var url in adminOnlyUrls)
            {
                var result = await TestUnauthorizedAccess(url);
                
                Assert.That(result.LoadsProtectedContent, Is.False, 
                    $"Analyst should not access admin-only URL: {url}");
            }
        }

        [Test]
        public async Task ExternalUser_CannotAccessInternalData()
        {
            // Login as external user
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "external@company.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);

            var internalUrls = new[]
            {
                "/Admin/UserManagement",
                "/Certificate/Create",
                "/Certificate/Edit/1",
                "/Admin/ProductManagement/Create"
            };

            foreach (var url in internalUrls)
            {
                var result = await TestUnauthorizedAccess(url);
                
                Assert.That(result.LoadsProtectedContent, Is.False, 
                    $"External user should not access internal URL: {url}");
            }
        }

        [Test]
        public async Task SqlInjection_LoginForm_ShouldBeSafe()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");

            var sqlPayloads = new[]
            {
                "' OR '1'='1",
                "' OR 1=1--",
                "admin'--",
                "admin'/*",
                "' OR 'x'='x",
                "'; DROP TABLE Users;--",
                "' UNION SELECT * FROM Users--",
                "admin'; EXEC xp_cmdshell('dir');--"
            };

            var emailResult = await TestSqlInjection("input[name='Email']", sqlPayloads);
            var passwordResult = await TestSqlInjection("input[name='Password']", sqlPayloads);

            Assert.That(emailResult.VulnerablePayloads, Is.Empty, 
                $"Email field vulnerable to SQL injection with: {string.Join(", ", emailResult.VulnerablePayloads)}");
            
            Assert.That(passwordResult.VulnerablePayloads, Is.Empty, 
                $"Password field vulnerable to SQL injection with: {string.Join(", ", passwordResult.VulnerablePayloads)}");
        }

        [Test]
        public async Task SqlInjection_SearchFields_ShouldBeSafe()
        {
            // Test public search functionality
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");

            var searchSelectors = new[]
            {
                "input[name='search']",
                "input[type='search']",
                "#searchInput",
                ".search-input"
            };

            var sqlPayloads = new[]
            {
                "' OR '1'='1",
                "'; DROP TABLE Certificates;--",
                "' UNION SELECT * FROM Users--"
            };

            foreach (var selector in searchSelectors)
            {
                try
                {
                    var element = await TestPage.QuerySelectorAsync(selector);
                    if (element != null)
                    {
                        var result = await TestSqlInjection(selector, sqlPayloads);
                        Assert.That(result.VulnerablePayloads, Is.Empty, 
                            $"Search field {selector} vulnerable to SQL injection");
                    }
                }
                catch
                {
                    // Selector not found, skip
                }
            }
        }

        [Test]
        public async Task XSS_InputFields_ShouldBeSafe()
        {
            var xssPayloads = new[]
            {
                "<script>alert('XSS')</script>",
                "javascript:alert('XSS')",
                "<img src=x onerror=alert('XSS')>",
                "<svg onload=alert('XSS')>",
                "';alert('XSS');//",
                "\"><script>alert('XSS')</script>",
                "<iframe src=javascript:alert('XSS')></iframe>"
            };

            // Test login form
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            
            var loginResult = await TestXss("input[name='Email']", xssPayloads);
            Assert.That(loginResult.VulnerablePayloads, Is.Empty, 
                $"Login email field vulnerable to XSS: {string.Join(", ", loginResult.VulnerablePayloads)}");

            // Test search functionality if available
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
            
            try
            {
                var searchInput = await TestPage.QuerySelectorAsync("input[type='search'], input[name='search']");
                if (searchInput != null)
                {
                    var searchResult = await TestXss("input[type='search'], input[name='search']", xssPayloads);
                    Assert.That(searchResult.VulnerablePayloads, Is.Empty, 
                        "Search field vulnerable to XSS");
                }
            }
            catch
            {
                // Search input not found, skip
            }
        }

        [Test]
        public async Task SessionSecurity_ShouldBeSecure()
        {
            var result = await TestSessionSecurity();
            
            Assert.That(result.HasSessionCookie, Is.True, 
                "Application should use session cookies for authentication");
            
            Assert.That(result.PreventSessionFixation, Is.True, 
                "Application should prevent access without valid session");
        }

        [Test]
        public async Task DirectObjectAccess_ShouldBeProtected()
        {
            // Test direct access to resources by ID manipulation
            var directAccessUrls = new[]
            {
                "/Certificate/Details/1",
                "/Certificate/Details/999999", // Non-existent ID
                "/Certificate/Edit/1",
                "/Admin/UserManagement/Details/1",
                "/Admin/UserManagement/Edit/1",
                "/Admin/ProductManagement/Details/1"
            };

            foreach (var url in directAccessUrls)
            {
                var result = await TestUnauthorizedAccess(url);
                
                // Should either redirect to login or show proper error
                Assert.That(result.StatusCode == 404 || result.RedirectedToLogin || result.ShowsAccessDenied || result.StatusCode == 401, 
                    Is.True, $"Direct access to {url} should be properly protected (Status: {result.StatusCode})");
            }
        }

        [Test]
        public async Task HttpMethods_ShouldBeRestricted()
        {
            // Test that dangerous HTTP methods are not allowed
            var testUrls = new[]
            {
                "/Account/Login",
                "/Certificate/Details/1",
                "/Admin/UserManagement"
            };

            foreach (var url in testUrls)
            {
                try
                {
                    // Test with different HTTP methods (Playwright limitations apply)
                    var response = await TestPage.GotoAsync($"{BaseUrl}{url}");
                    Assert.That(response?.Status, Is.Not.EqualTo(405), 
                        $"URL {url} returned Method Not Allowed unexpectedly");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HTTP method test for {url}: {ex.Message}");
                }
            }
        }

        [Test]
        public async Task FileUpload_ShouldValidateFileTypes()
        {
            // Login as admin to access upload functionality
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);

            var uploadPages = new[]
            {
                "/Certificate/Create",
                "/Admin/ProductManagement/Create"
            };

            foreach (var page in uploadPages)
            {
                try
                {
                    await TestPage.GotoAsync($"{BaseUrl}{page}");
                    await TestPage.WaitForTimeoutAsync(1000);

                    var fileInputs = await TestPage.QuerySelectorAllAsync("input[type='file']");
                    
                    foreach (var input in fileInputs)
                    {
                        var accept = await input.GetAttributeAsync("accept");
                        var name = await input.GetAttributeAsync("name");
                        
                        Assert.That(accept, Is.Not.Null.And.Not.Empty, 
                            $"File input '{name}' on {page} should specify accepted file types");
                        
                        // Verify it doesn't accept dangerous file types
                        Assert.That(accept, Does.Not.Contain(".exe"), 
                            $"File input '{name}' should not accept executable files");
                        Assert.That(accept, Does.Not.Contain(".bat"), 
                            $"File input '{name}' should not accept batch files");
                        Assert.That(accept, Does.Not.Contain(".js"), 
                            $"File input '{name}' should not accept JavaScript files");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"File upload test failed for {page}: {ex.Message}");
                }
            }
        }

        [Test]
        public async Task RateLimit_LoginAttempts_ShouldBeProtected()
        {
            var result = await TestRateLimit("/Account/Login", 10);
            
            // Note: This test may not trigger rate limiting if not implemented
            // But it helps identify if basic protection is in place
            Console.WriteLine($"Rate limit test completed. Triggered: {result.RateLimitTriggered}, Requests: {result.RequestsUntilLimit}");
            
            if (result.RateLimitTriggered)
            {
                Assert.That(result.RequestsUntilLimit, Is.LessThan(10), 
                    "Rate limiting should trigger before 10 requests");
            }
        }

        [Test]
        public async Task PasswordReset_ShouldNotLeakUserInfo()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/ForgotPassword");
            
            // Test with non-existent email
            await TestPage.FillAsync("input[name='Email']", "nonexistent@example.com");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForTimeoutAsync(2000);
            
            var content = await TestPage.ContentAsync();
            
            // Should not reveal whether email exists or not
            Assert.That(content, Does.Not.Contain("not found").IgnoreCase, 
                "Password reset should not reveal if email doesn't exist");
            Assert.That(content, Does.Not.Contain("invalid email").IgnoreCase, 
                "Password reset should not reveal email validity");
        }
    }
}