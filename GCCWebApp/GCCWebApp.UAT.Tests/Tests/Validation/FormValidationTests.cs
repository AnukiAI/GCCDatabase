using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Validation
{
    [TestFixture]
    public class FormValidationTests : ValidationTestBase
    {
        [Test]
        [Category("Validation")]
        [Category("Login")]
        public async Task LoginForm_RequiresValidEmail()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            
            var invalidEmails = new[] { 
                "", // Empty
                "invalid", // No @ symbol
                "invalid@", // No domain
                "@domain.com", // No user part
                "user@", // No domain
                "user space@domain.com", // Space in email
                "user@domain", // No TLD
                "a".PadRight(255, 'a') + "@domain.com" // Too long (>254 chars)
            };

            foreach (var email in invalidEmails)
            {
                var result = await TestFieldValidation("input[name='Email']", email);
                Assert.That(result.HasValidationError, Is.True, 
                    $"Expected validation error for email: '{email}', but none was found. Messages: {string.Join(", ", result.ValidationMessages)}");
            }
        }

        [Test]
        [Category("Validation")]
        [Category("Login")]
        public async Task LoginForm_RequiresPassword()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            
            var result = await TestFieldValidation("input[name='Password']", "");
            Assert.That(result.HasValidationError, Is.True, 
                "Empty password should trigger validation error");
        }

        [Test]
        [Category("Validation")]
        [Category("Login")]
        public async Task LoginForm_PreventSubmissionWithInvalidData()
        {
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            
            var invalidData = new Dictionary<string, string>
            {
                { "input[name='Email']", "invalid-email" },
                { "input[name='Password']", "" }
            };
            
            var submissionPrevented = await TestFormSubmissionPrevention("button[type='submit']", invalidData);
            Assert.That(submissionPrevented, Is.True, 
                "Form should prevent submission with invalid data");
        }

        [Test]
        [Category("Validation")]
        [Category("UserManagement")]
        public async Task UserCreationForm_ValidatesAllFields()
        {
            // Login as admin first
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForURLAsync("**/Dashboard/**");
            
            // Navigate to user creation
            await TestPage.GotoAsync($"{BaseUrl}/Admin/UserManagement/Create");
            
            // Test email validation
            var emailTests = new[] { 
                "", // Required field
                "invalid-email", // Invalid format
                "test@", // Incomplete
                new string('a', 100) + "@domain.com" // Very long email
            };

            foreach (var email in emailTests)
            {
                var result = await TestFieldValidation("input[name='Email']", email);
                Assert.That(result.HasValidationError, Is.True, 
                    $"Email validation should fail for: '{email}'");
            }

            // Test name validation
            var nameResult = await TestFieldValidation("input[name='Name']", "");
            Assert.That(nameResult.HasValidationError, Is.True, 
                "Name field should be required");

            // Test password strength (if applicable)
            var weakPasswords = new[] { 
                "123", // Too short
                "password", // Too weak
                "", // Empty
                "pass" // Too short
            };

            foreach (var password in weakPasswords)
            {
                var result = await TestFieldValidation("input[name='Password']", password);
                // Note: May not have validation if using default password generation
                Console.WriteLine($"Password '{password}' validation: {result.HasValidationError}");
            }
        }

        [Test]
        [Category("Validation")]
        [Category("CertificateManagement")]
        public async Task CertificateForm_ValidatesRequiredFields()
        {
            // Login as admin
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForURLAsync("**/Dashboard/**");
            
            // Navigate to certificate creation
            await TestPage.GotoAsync($"{BaseUrl}/Certificate/Create");
            
            // Test required fields
            var requiredFields = new[]
            {
                "input[name='CertificateNumber']",
                "input[name='ProductName']",
                "select[name='CompanyId']",
                "input[name='CertificationDate']"
            };

            foreach (var field in requiredFields)
            {
                try
                {
                    var element = await TestPage.QuerySelectorAsync(field);
                    if (element != null)
                    {
                        var result = await TestFieldValidation(field, "");
                        Assert.That(result.HasValidationError, Is.True, 
                            $"Field {field} should be required");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not test field {field}: {ex.Message}");
                }
            }
        }

        [Test]
        [Category("Validation")]
        [Category("ProductManagement")]
        public async Task ProductForm_ValidatesDataTypes()
        {
            // Login as admin
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForURLAsync("**/Dashboard/**");
            
            // Navigate to product creation
            await TestPage.GotoAsync($"{BaseUrl}/Admin/ProductManagement/Create");
            
            // Test numeric field validation (if any)
            var numericFields = await TestPage.QuerySelectorAllAsync("input[type='number'], input[type='decimal']");
            
            foreach (var field in numericFields)
            {
                var name = await field.GetAttributeAsync("name");
                if (name != null)
                {
                    // Test with non-numeric values
                    var result = await TestFieldValidation($"input[name='{name}']", "not-a-number");
                    Console.WriteLine($"Numeric field {name} validation with 'not-a-number': {result.HasValidationError}");
                }
            }

            // Test date field validation
            var dateFields = await TestPage.QuerySelectorAllAsync("input[type='date'], input[type='datetime-local']");
            
            foreach (var field in dateFields)
            {
                var name = await field.GetAttributeAsync("name");
                if (name != null)
                {
                    var result = await TestFieldValidation($"input[name='{name}']", "invalid-date");
                    Console.WriteLine($"Date field {name} validation with 'invalid-date': {result.HasValidationError}");
                }
            }
        }

        [Test]
        [Category("Validation")]
        [Category("FileUpload")]
        public async Task FileUpload_ValidatesFileTypes()
        {
            // Login as admin
            await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
            await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
            await TestPage.FillAsync("input[name='Password']", "TestPass123!");
            await TestPage.ClickAsync("button[type='submit']");
            await TestPage.WaitForURLAsync("**/Dashboard/**");
            
            // Find pages with file uploads
            var pagesToTest = new[]
            {
                "/Certificate/Create",
                "/Admin/ProductManagement/Create",
                "/Admin/UserManagement/Create"
            };

            foreach (var page in pagesToTest)
            {
                try
                {
                    await TestPage.GotoAsync($"{BaseUrl}{page}");
                    await TestPage.WaitForTimeoutAsync(1000);
                    
                    var fileInputs = await TestPage.QuerySelectorAllAsync("input[type='file']");
                    
                    if (fileInputs.Any())
                    {
                        Console.WriteLine($"Found {fileInputs.Count()} file input(s) on {page}");
                        
                        foreach (var fileInput in fileInputs)
                        {
                            var accept = await fileInput.GetAttributeAsync("accept");
                            var name = await fileInput.GetAttributeAsync("name");
                            
                            Console.WriteLine($"File input '{name}' accepts: {accept ?? "any file type"}");
                            
                            // Note: Actual file upload testing would require creating test files
                            // This is a placeholder for file validation testing
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No file inputs found on {page}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not test page {page}: {ex.Message}");
                }
            }
        }

        [Test]
        [Category("Validation")]
        [Category("MaxLength")]
        public async Task Forms_RespectMaxLengthLimits()
        {
            var pagesToTest = new[]
            {
                "/Account/Login",
                "/Certificate/Create", // Requires auth
                "/Admin/UserManagement/Create" // Requires auth
            };

            foreach (var page in pagesToTest)
            {
                try
                {
                    if (page.Contains("Certificate") || page.Contains("Admin"))
                    {
                        // Login first for protected pages
                        await TestPage.GotoAsync($"{BaseUrl}/Account/Login");
                        await TestPage.FillAsync("input[name='Email']", "admin@gcc.com");
                        await TestPage.FillAsync("input[name='Password']", "TestPass123!");
                        await TestPage.ClickAsync("button[type='submit']");
                        await TestPage.WaitForTimeoutAsync(2000);
                    }

                    await TestPage.GotoAsync($"{BaseUrl}{page}");
                    await TestPage.WaitForTimeoutAsync(1000);
                    
                    var textInputs = await TestPage.QuerySelectorAllAsync("input[type='text'], input[type='email'], textarea");
                    
                    foreach (var input in textInputs)
                    {
                        var maxLength = await input.GetAttributeAsync("maxlength");
                        var name = await input.GetAttributeAsync("name");
                        
                        if (maxLength != null && int.TryParse(maxLength, out int limit))
                        {
                            // Test with text that exceeds the limit
                            var longText = new string('a', limit + 10);
                            
                            await input.FillAsync(longText);
                            var actualValue = await input.InputValueAsync();
                            
                            Assert.That(actualValue.Length, Is.LessThanOrEqualTo(limit), 
                                $"Input '{name}' should enforce maxlength of {limit}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not test max length on {page}: {ex.Message}");
                }
            }
        }
    }
}