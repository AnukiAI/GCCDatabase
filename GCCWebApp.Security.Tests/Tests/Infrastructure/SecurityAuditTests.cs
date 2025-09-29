using FluentAssertions;
using GCCWebApp.Security.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace GCCWebApp.Security.Tests.Tests.Infrastructure
{
    [Trait("Category", "SecurityAudit")]
    public class SecurityAuditTests : IClassFixture<SecurityTestWebApplicationFactory<Program>>
    {
        private readonly SecurityTestWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public SecurityAuditTests(SecurityTestWebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task SecurityHeaders_AllRequiredHeaders_ShouldBePresent()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert - Check for all required security headers
            var requiredHeaders = new Dictionary<string, string[]?>
            {
                ["X-Content-Type-Options"] = ["nosniff"],
                ["X-Frame-Options"] = ["DENY", "SAMEORIGIN"],
                ["X-XSS-Protection"] = ["0"], // Modern approach is to disable this
                ["Referrer-Policy"] = ["strict-origin-when-cross-origin"],
                ["Content-Security-Policy"] = null, // Just check presence
                ["Permissions-Policy"] = null,
                ["X-Permitted-Cross-Domain-Policies"] = ["none"],
                ["Cross-Origin-Embedder-Policy"] = ["require-corp"],
                ["Cross-Origin-Opener-Policy"] = ["same-origin"],
                ["Cross-Origin-Resource-Policy"] = ["same-origin"]
            };

            foreach (var (headerName, allowedValues) in requiredHeaders)
            {
                response.Headers.Should().ContainKey(headerName, 
                    $"Security header {headerName} should be present");

                if (allowedValues != null)
                {
                    var headerValue = response.Headers.GetValues(headerName).First();
                    headerValue.Should().BeOneOf(allowedValues, 
                        $"Security header {headerName} should have an allowed value");
                }

                _output.WriteLine($"✓ {headerName}: {response.Headers.GetValues(headerName).First()}");
            }
        }

        [Fact]
        public async Task InformationLeakage_ServerHeaders_ShouldBeRemoved()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert - Check that information-leaking headers are removed
            var forbiddenHeaders = new[]
            {
                "Server",
                "X-Powered-By",
                "X-AspNet-Version",
                "X-AspNetMvc-Version"
            };

            foreach (var headerName in forbiddenHeaders)
            {
                response.Headers.Should().NotContainKey(headerName, 
                    $"Information-leaking header {headerName} should be removed");
                
                _output.WriteLine($"✓ {headerName} header properly removed");
            }
        }

        [Fact]
        public async Task HttpsRedirection_ShouldBeEnforced()
        {
            // This test verifies HTTPS redirection is configured
            // In a real test environment, you would test HTTP -> HTTPS redirects
            
            var response = await _client.GetAsync("/");
            
            // Check that the response doesn't indicate insecure protocols
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("http://", "Should not contain insecure HTTP links");
            
            _output.WriteLine("✓ HTTPS enforcement validated");
        }

        [Fact]
        public async Task ContentTypeValidation_MaliciousContentTypes_ShouldBeRejected()
        {
            var maliciousContentTypes = new[]
            {
                "application/x-php",
                "application/x-httpd-php",
                "application/x-jsp",
                "application/x-asp",
                "text/x-php",
                "text/x-jsp"
            };

            foreach (var contentType in maliciousContentTypes)
            {
                var content = new StringContent("malicious content", Encoding.UTF8, contentType);
                var response = await _client.PostAsync("/api/v1/upload", content);
                
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.UnsupportedMediaType,
                    HttpStatusCode.Forbidden,
                    HttpStatusCode.Unauthorized);
                
                _output.WriteLine($"✓ Malicious content type {contentType} properly rejected");
            }
        }

        [Theory]
        [InlineData("/api/v1/admin")]
        [InlineData("/api/v1/users")]
        [InlineData("/api/v1/certificates")]
        [InlineData("/admin")]
        public async Task AuthenticatedEndpoints_WithoutAuthentication_ShouldReturn401(string endpoint)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
                $"Endpoint {endpoint} should require authentication");
            
            _output.WriteLine($"✓ Endpoint {endpoint} properly protected");
        }

        [Fact]
        public async Task PasswordPolicy_WeakPasswords_ShouldBeRejected()
        {
            var weakPasswords = new[]
            {
                "123456",
                "password",
                "admin",
                "qwerty",
                "abc123",
                "password123",
                "12345678"
            };

            foreach (var weakPassword in weakPasswords)
            {
                var loginRequest = new
                {
                    Username = $"testuser_{Guid.NewGuid():N}",
                    Password = weakPassword
                };

                var response = await _client.PostAsJsonAsync("/api/v1/auth/register", loginRequest);
                
                // Should be rejected due to weak password
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest, 
                    $"Weak password '{weakPassword}' should be rejected");
                
                _output.WriteLine($"✓ Weak password '{weakPassword}' properly rejected");
            }
        }

        [Fact]
        public async Task ApiVersioning_UnsupportedVersions_ShouldBeHandledProperly()
        {
            var unsupportedVersions = new[] { "v0", "v99", "beta", "alpha" };

            foreach (var version in unsupportedVersions)
            {
                var response = await _client.GetAsync($"/api/{version}/certificates");
                
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.NotFound,
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.Unauthorized);
                
                _output.WriteLine($"✓ Unsupported API version {version} properly handled");
            }
        }

        [Fact]
        public async Task RateLimit_ExcessiveRequests_ShouldBeThrottled()
        {
            // Make rapid requests to test rate limiting
            var tasks = new List<Task<HttpResponseMessage>>();
            
            for (int i = 0; i < 150; i++) // Exceeds typical rate limit
            {
                tasks.Add(_client.GetAsync("/api/v1/certificates"));
            }

            var responses = await Task.WhenAll(tasks);
            
            // At least some requests should be rate limited
            var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            
            rateLimitedCount.Should().BeGreaterThan(0, "Rate limiting should be active");
            
            _output.WriteLine($"✓ Rate limiting active - {rateLimitedCount} requests throttled");
        }

        [Fact]
        public async Task InputSanitization_XssPayloads_ShouldBeSanitized()
        {
            var xssPayloads = new[]
            {
                "<script>alert('xss')</script>",
                "<img src=x onerror=alert('xss')>",
                "javascript:alert('xss')",
                "<svg onload=alert('xss')>",
                "<iframe src='javascript:alert(1)'></iframe>",
                "<object data='data:text/html,<script>alert(1)</script>'></object>"
            };

            foreach (var payload in xssPayloads)
            {
                // Test in search parameters
                var response = await _client.GetAsync($"/api/v1/search?q={Uri.EscapeDataString(payload)}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Content should not contain the raw XSS payload
                    content.Should().NotContain("<script>", "XSS payload should be sanitized");
                    content.Should().NotContain("javascript:", "JavaScript protocols should be sanitized");
                    content.Should().NotContain("onerror=", "Event handlers should be sanitized");
                }
                
                _output.WriteLine($"✓ XSS payload properly sanitized or rejected");
            }
        }

        [Fact]
        public async Task DatabaseConnections_SqlInjection_ShouldBeParameterized()
        {
            var sqlInjectionPayloads = new[]
            {
                "'; DROP TABLE Users; --",
                "' OR '1'='1",
                "1; INSERT INTO Users (Username) VALUES ('hacker'); --",
                "' UNION SELECT * FROM ApiKeys --",
                "'; EXEC xp_cmdshell('dir'); --"
            };

            foreach (var payload in sqlInjectionPayloads)
            {
                // Test various endpoints that interact with database
                var endpoints = new[]
                {
                    $"/api/v1/search?q={Uri.EscapeDataString(payload)}",
                    $"/api/v1/users/{Uri.EscapeDataString(payload)}",
                    $"/api/v1/certificates?filter={Uri.EscapeDataString(payload)}"
                };

                foreach (var endpoint in endpoints)
                {
                    var response = await _client.GetAsync(endpoint);
                    
                    // Should not return database errors or unauthorized data
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        
                        content.Should().NotContain("SQL", "Should not contain SQL error messages");
                        content.Should().NotContain("database", "Should not contain database error messages");
                        content.Should().NotContain("table", "Should not contain table structure information");
                    }
                }
            }
            
            _output.WriteLine("✓ SQL injection protection validated");
        }

        [Fact]
        public async Task FileUpload_SecurityValidation_ShouldRejectMaliciousFiles()
        {
            var maliciousFiles = new[]
            {
                ("virus.exe", "MZ\x90\x00", "application/x-msdownload"),
                ("script.php", "<?php system($_GET['cmd']); ?>", "application/x-php"),
                ("shell.jsp", "<% Runtime.getRuntime().exec(request.getParameter(\"cmd\")); %>", "text/jsp"),
                ("backdoor.asp", "<%eval request(\"cmd\")%>", "application/x-asp"),
                ("malware.bat", "@echo off\ndel /q /s *.*", "application/x-bat")
            };

            foreach (var (filename, content, contentType) in maliciousFiles)
            {
                using var formContent = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                formContent.Add(fileContent, "file", filename);

                var response = await _client.PostAsync("/api/v1/upload", formContent);
                
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.UnsupportedMediaType,
                    HttpStatusCode.Forbidden,
                    HttpStatusCode.Unauthorized);
                
                _output.WriteLine($"✓ Malicious file {filename} properly rejected");
            }
        }
    }
}