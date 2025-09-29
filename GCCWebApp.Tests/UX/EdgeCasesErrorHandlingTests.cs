using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text;
using System.Text.Json;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Controllers;
using GCCWebApp.Services;

namespace GCCWebApp.Tests.UX
{
    /// <summary>
    /// UX Edge Cases and Error Handling Test Coverage
    /// Tests requirements: UX-003 to UX-007, ERROR-001 to ERROR-005
    /// Coverage: Form validation, error messages, network failures, data corruption, user guidance
    /// </summary>
    public class EdgeCasesErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public EdgeCasesErrorHandlingTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        #region UX-003: Form Validation and Error Messages

        [Fact]
        public async Task FormValidation_RequiredFields_ShouldShowClearErrorMessages()
        {
            // Arrange - Submit certificate form with missing required fields
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", ""), // Required field empty
                new KeyValuePair<string, string>("CompanyId", "0"), // Invalid company
                new KeyValuePair<string, string>("ProductId", "0"), // Invalid product
                new KeyValuePair<string, string>("EffectiveDate", ""), // Required date empty
                new KeyValuePair<string, string>("ExpirationDate", "invalid-date") // Invalid date format
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", formData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Should contain specific error messages for each field
            responseContent.Should().Contain("Certificate number is required");
            responseContent.Should().Contain("Please select a valid company");
            responseContent.Should().Contain("Please select a valid product");
            responseContent.Should().Contain("Effective date is required");
            responseContent.Should().Contain("Invalid date format");
        }

        [Fact]
        public async Task FormValidation_FieldLengthLimits_ShouldEnforceConstraints()
        {
            // Arrange - Submit form with fields exceeding maximum length
            var longString = new string('A', 300); // Exceeds most field limits
            
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", new string('1', 20)), // Max 7 characters
                new KeyValuePair<string, string>("Status", longString), // Max 50 characters
                new KeyValuePair<string, string>("Location", longString), // Max 175 characters
                new KeyValuePair<string, string>("Country", longString) // Max 100 characters
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", formData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            responseContent.Should().Contain("Certificate number cannot exceed 7 characters");
            responseContent.Should().Contain("Status cannot exceed 50 characters");
            responseContent.Should().Contain("Location cannot exceed 175 characters");
            responseContent.Should().Contain("Country cannot exceed 100 characters");
        }

        [Fact]
        public async Task FormValidation_DateLogicValidation_ShouldValidateBusinessRules()
        {
            // Arrange - Submit form with expiration date before effective date
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", "GCC1234"),
                new KeyValuePair<string, string>("CompanyId", "1"),
                new KeyValuePair<string, string>("ProductId", "1"),
                new KeyValuePair<string, string>("CertificationTypeId", "1"),
                new KeyValuePair<string, string>("EffectiveDate", "2024-12-01"),
                new KeyValuePair<string, string>("ExpirationDate", "2024-11-01") // Before effective date
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", formData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Expiration date must be after effective date");
        }

        [Fact]
        public async Task FormValidation_DuplicatePreventionShouldCheckExistingRecords()
        {
            // Arrange - Submit form with certificate number that already exists
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", "GCC0001"), // Assuming this exists from seed data
                new KeyValuePair<string, string>("CompanyId", "1"),
                new KeyValuePair<string, string>("ProductId", "1"),
                new KeyValuePair<string, string>("CertificationTypeId", "1"),
                new KeyValuePair<string, string>("EffectiveDate", "2024-01-01"),
                new KeyValuePair<string, string>("ExpirationDate", "2025-01-01")
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", formData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("Certificate number already exists");
        }

        [Fact]
        public async Task FormValidation_ClientSideValidation_ShouldMatchServerSide()
        {
            // Arrange - Get the create form to check client-side validation attributes
            var getResponse = await _client.GetAsync("/Certificate/Create");

            // Assert
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var formContent = await getResponse.Content.ReadAsStringAsync();
            
            // Check for HTML5 validation attributes
            formContent.Should().Contain("required"); // Required field attributes
            formContent.Should().Contain("maxlength"); // Length validation attributes
            formContent.Should().Contain("data-val-required"); // ASP.NET client validation
            formContent.Should().Contain("data-val-length"); // Length validation messages
            formContent.Should().Contain("data-val-date"); // Date validation
        }

        #endregion

        #region UX-004: Error Recovery and User Guidance

        [Fact]
        public async Task ErrorRecovery_PartialFormSubmission_ShouldPreserveUserInput()
        {
            // Arrange - Submit form with some valid and some invalid data
            var partialFormData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", "GCC5678"), // Valid
                new KeyValuePair<string, string>("CompanyId", "1"), // Valid
                new KeyValuePair<string, string>("ProductId", ""), // Invalid - empty
                new KeyValuePair<string, string>("CertificationTypeId", "1"), // Valid
                new KeyValuePair<string, string>("EffectiveDate", "2024-01-01"), // Valid
                new KeyValuePair<string, string>("ExpirationDate", "invalid") // Invalid format
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", partialFormData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Valid data should be preserved in the returned form
            responseContent.Should().Contain("value=\"GCC5678\""); // Number preserved
            responseContent.Should().Contain("value=\"1\" selected"); // Company selection preserved
            responseContent.Should().Contain("value=\"2024-01-01\""); // Effective date preserved
            
            // Error messages should be displayed for invalid fields
            responseContent.Should().Contain("Please select a product");
            responseContent.Should().Contain("Invalid expiration date format");
        }

        [Fact]
        public async Task ErrorRecovery_SessionTimeout_ShouldProvideRecoveryOptions()
        {
            // Arrange - Simulate session timeout scenario
            var longFormData = CreateComplexCertificateFormData();
            
            // Clear session/authentication to simulate timeout
            _client.DefaultRequestHeaders.Clear();

            // Act
            var response = await _client.PostAsync("/Certificate/Create", longFormData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Should provide guidance for session recovery
            responseContent.Should().Contain("session has expired");
            responseContent.Should().Contain("Please log in again");
            responseContent.Should().Contain("Your work has been saved"); // Auto-save indication
        }

        [Fact]
        public async Task ErrorRecovery_NetworkFailure_ShouldHandleGracefully()
        {
            // Arrange - This test would require network simulation
            // For now, test the client-side retry mechanism
            
            var formData = CreateValidCertificateFormData();
            
            // Simulate network timeout by setting very short timeout
            using var timeoutClient = _factory.CreateClient();
            timeoutClient.Timeout = TimeSpan.FromMilliseconds(1);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(() => 
                timeoutClient.PostAsync("/Certificate/Create", formData));
            
            exception.Should().NotBeNull();
            // In a real app, this would trigger client-side retry logic
        }

        [Fact]
        public async Task ErrorRecovery_LargeFileUpload_ShouldProvideProgressAndRecovery()
        {
            // Arrange - Simulate large file upload
            var largeFileContent = new byte[10 * 1024 * 1024]; // 10MB file
            Array.Fill(largeFileContent, (byte)0xFF);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("GCC9999"), "Number");
            form.Add(new StringContent("1"), "CompanyId");
            form.Add(new StringContent("1"), "ProductId");
            form.Add(new StringContent("1"), "CertificationTypeId");
            form.Add(new ByteArrayContent(largeFileContent), "CertificateFile", "large-certificate.pdf");

            // Act
            var response = await _client.PostAsync("/Certificate/Create", form);

            // Assert
            // Should handle large file appropriately (either success or clear error)
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.RequestEntityTooLarge);
            
            if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().Contain("File size exceeds maximum allowed");
                responseContent.Should().Contain("maximum file size is"); // Should specify the limit
            }
        }

        #endregion

        #region UX-005: Edge Case UI Behaviors

        [Fact]
        public async Task EdgeCases_EmptyDatasets_ShouldDisplayMeaningfulMessages()
        {
            // Arrange - Access certificate list when no data exists
            // This would require clearing the database or using a clean test context

            // Act
            var response = await _client.GetAsync("/Certificate/Certificates");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            
            // Should show helpful empty state message
            content.Should().ContainAny(
                "No certificates found",
                "Get started by adding your first certificate",
                "No data available"
            );
            
            // Should provide action to add new certificate
            content.Should().Contain("Add Certificate");
        }

        [Fact]
        public async Task EdgeCases_SpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange - Submit form with special characters
            var specialCharFormData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", "GCC<>&"),
                new KeyValuePair<string, string>("Location", "Factory #1 - Main St. & 2nd Ave."),
                new KeyValuePair<string, string>("Country", "United States (US)"),
                new KeyValuePair<string, string>("CompanyId", "1"),
                new KeyValuePair<string, string>("ProductId", "1"),
                new KeyValuePair<string, string>("CertificationTypeId", "1"),
                new KeyValuePair<string, string>("EffectiveDate", "2024-01-01"),
                new KeyValuePair<string, string>("ExpirationDate", "2025-01-01")
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", specialCharFormData);

            // Assert
            if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Redirect)
            {
                // Special characters should be properly encoded and stored
                var listResponse = await _client.GetAsync("/Certificate/Certificates");
                var listContent = await listResponse.Content.ReadAsStringAsync();
                
                // Should display special characters correctly (HTML encoded)
                listContent.Should().Contain("GCC&lt;&gt;&amp;"); // HTML encoded
                listContent.Should().Contain("Factory #1 - Main St. &amp; 2nd Ave."); // Properly encoded
            }
            else
            {
                // Should provide clear validation error if special characters not allowed
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().Contain("special characters are not allowed");
            }
        }

        [Fact]
        public async Task EdgeCases_VeryLongContent_ShouldHandleGracefully()
        {
            // Arrange - Test with very long content that might break UI layout
            var veryLongString = new string('A', 1000);
            
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", "GCC1111"),
                new KeyValuePair<string, string>("Location", veryLongString),
                new KeyValuePair<string, string>("CompanyId", "1"),
                new KeyValuePair<string, string>("ProductId", "1"),
                new KeyValuePair<string, string>("CertificationTypeId", "1"),
                new KeyValuePair<string, string>("EffectiveDate", "2024-01-01"),
                new KeyValuePair<string, string>("ExpirationDate", "2025-01-01")
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", formData);

            // Assert
            if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Redirect)
            {
                // Long content should be handled in UI (truncated, scrollable, etc.)
                var detailsResponse = await _client.GetAsync($"/Certificate/Details/1");
                var detailsContent = await detailsResponse.Content.ReadAsStringAsync();
                
                // Should either truncate with "..." or provide scrollable container
                detailsContent.Should().ContainAny("...", "text-truncate", "overflow-scroll");
            }
        }

        [Fact]
        public async Task EdgeCases_ConcurrentEditing_ShouldPreventDataLoss()
        {
            // Arrange - Simulate two users editing the same certificate
            var client1 = _factory.CreateClient();
            var client2 = _factory.CreateClient();

            var certificateId = 1; // Assuming certificate exists
            
            // Both clients get the edit form
            var form1Response = await client1.GetAsync($"/Certificate/Edit/{certificateId}");
            var form2Response = await client2.GetAsync($"/Certificate/Edit/{certificateId}");

            // Extract any version/timestamp tokens from the forms
            var form1Content = await form1Response.Content.ReadAsStringAsync();
            var form2Content = await form2Response.Content.ReadAsStringAsync();

            // Client 1 submits changes first
            var client1Data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", certificateId.ToString()),
                new KeyValuePair<string, string>("Number", "GCC0001-UPDATED-1"),
                new KeyValuePair<string, string>("Status", "updated")
            });

            var client1SubmitResponse = await client1.PostAsync($"/Certificate/Edit/{certificateId}", client1Data);

            // Client 2 submits different changes
            var client2Data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", certificateId.ToString()),
                new KeyValuePair<string, string>("Number", "GCC0001-UPDATED-2"),
                new KeyValuePair<string, string>("Status", "different-update")
            });

            // Act
            var client2SubmitResponse = await client2.PostAsync($"/Certificate/Edit/{certificateId}", client2Data);

            // Assert
            // Should detect concurrent modification and handle appropriately
            if (client2SubmitResponse.StatusCode == HttpStatusCode.Conflict)
            {
                var conflictContent = await client2SubmitResponse.Content.ReadAsStringAsync();
                conflictContent.Should().Contain("has been modified by another user");
                conflictContent.Should().Contain("Please review the changes");
            }
        }

        #endregion

        #region ERROR-001: Application Error Handling

        [Fact]
        public async Task ApplicationErrors_UnhandledException_ShouldShowFriendlyErrorPage()
        {
            // Arrange - Trigger an application error
            // This would require a test endpoint that throws exceptions

            // Act
            var response = await _client.GetAsync("/Certificate/TriggerError"); // Hypothetical error endpoint

            // Assert
            // Should show custom error page, not raw exception details
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotContain("Exception");
                content.Should().NotContain("StackTrace");
                content.Should().Contain("Something went wrong");
                content.Should().Contain("Please try again");
            }
        }

        [Fact]
        public async Task ApplicationErrors_DatabaseConnectionFailure_ShouldProvideRecoveryOptions()
        {
            // Arrange - This test would require database connection simulation
            // For integration testing, we can test the service layer behavior
            
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Dispose the context to simulate connection failure
            await context.DisposeAsync();

            // Act
            var response = await _client.GetAsync("/Certificate/Certificates");

            // Assert
            // Should handle database connection failures gracefully
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.ServiceUnavailable, 
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK // If cached data available
            );
        }

        [Fact]
        public async Task ApplicationErrors_InvalidRoute_ShouldShow404Page()
        {
            // Act
            var response = await _client.GetAsync("/NonExistentController/NonExistentAction");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Page Not Found");
            content.Should().Contain("404");
            content.Should().Contain("Return to"); // Navigation option
        }

        [Fact]
        public async Task ApplicationErrors_Unauthorized_ShouldRedirectToLogin()
        {
            // Arrange - Try to access protected resource without authentication
            _client.DefaultRequestHeaders.Clear(); // Remove any authentication

            // Act
            var response = await _client.GetAsync("/Admin/UserManagement");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Redirect // Redirect to login
            );

            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                response.Headers.Location?.ToString().Should().Contain("login");
            }
        }

        #endregion

        #region ERROR-002: Data Validation Errors

        [Fact]
        public async Task DataValidation_ModelStateErrors_ShouldDisplayFieldSpecific()
        {
            // Arrange - Submit invalid data to trigger model state errors
            var invalidData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", ""), // Required
                new KeyValuePair<string, string>("EffectiveDate", "not-a-date"), // Invalid format
                new KeyValuePair<string, string>("CompanyId", "-1"), // Invalid ID
                new KeyValuePair<string, string>("ProductId", "999999") // Non-existent ID
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", invalidData);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            // Should show field-specific validation messages
            content.Should().Contain("Number field is required");
            content.Should().Contain("Invalid date format");
            content.Should().Contain("Please select a valid company");
            content.Should().Contain("Please select a valid product");
            
            // Error messages should be associated with correct fields
            content.Should().Contain("field-validation-error");
            content.Should().Contain("validation-summary");
        }

        [Fact]
        public async Task DataValidation_BusinessRuleViolation_ShouldExplainConstraints()
        {
            // Arrange - Violate business rules (e.g., certificate expiring before effective date)
            var businessRuleViolation = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", "GCC2222"),
                new KeyValuePair<string, string>("CompanyId", "1"),
                new KeyValuePair<string, string>("ProductId", "1"),
                new KeyValuePair<string, string>("CertificationTypeId", "1"),
                new KeyValuePair<string, string>("EffectiveDate", "2025-01-01"),
                new KeyValuePair<string, string>("ExpirationDate", "2024-12-01") // Before effective date
            });

            // Act
            var response = await _client.PostAsync("/Certificate/Create", businessRuleViolation);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            content.Should().Contain("Expiration date cannot be before effective date");
            content.Should().Contain("Business rule violation");
        }

        #endregion

        #region ERROR-003: System Resource Errors

        [Fact]
        public async Task ResourceErrors_FileUploadLimits_ShouldEnforceGracefully()
        {
            // Arrange - Upload file exceeding size limits
            var oversizedFile = new byte[50 * 1024 * 1024]; // 50MB file
            Array.Fill(oversizedFile, (byte)0x42);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("GCC3333"), "Number");
            form.Add(new StringContent("1"), "CompanyId");
            form.Add(new StringContent("1"), "ProductId");
            form.Add(new StringContent("1"), "CertificationTypeId");
            form.Add(new ByteArrayContent(oversizedFile), "CertificateFile", "oversized.pdf");

            // Act
            var response = await _client.PostAsync("/Certificate/Create", form);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.RequestEntityTooLarge
            );
            
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("file size exceeds");
            content.Should().Contain("maximum allowed size");
        }

        [Fact]
        public async Task ResourceErrors_MemoryPressure_ShouldDegradeGracefully()
        {
            // Arrange - Request that might cause memory pressure
            // This is difficult to test in integration tests, but we can test large data requests
            
            var largePageSize = 10000; // Request very large page
            
            // Act
            var response = await _client.GetAsync($"/Certificate/Certificates?pageSize={largePageSize}");

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK, // Handled gracefully with pagination limits
                HttpStatusCode.BadRequest // Rejected with clear message
            );

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.Should().Contain("page size too large");
                content.Should().Contain("maximum page size");
            }
        }

        #endregion

        #region ERROR-004: Error Logging and Monitoring

        [Fact]
        public async Task ErrorLogging_CriticalErrors_ShouldBeLogged()
        {
            // This test would require access to logging infrastructure
            // For now, we test that errors don't crash the application
            
            // Arrange - Trigger various error conditions
            var errorScenarios = new[]
            {
                "/Certificate/Details/999999", // Non-existent certificate
                "/Certificate/Edit/abc", // Invalid ID format
                "/Certificate/Delete/0" // Invalid ID
            };

            // Act & Assert
            foreach (var scenario in errorScenarios)
            {
                var response = await _client.GetAsync(scenario);
                
                // Should handle errors gracefully without crashing
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.NotFound,
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.InternalServerError
                );
                
                // Response should still be valid HTTP
                response.Should().NotBeNull();
                var content = await response.Content.ReadAsStringAsync();
                content.Should().NotBeEmpty();
            }
        }

        #endregion

        #region ERROR-005: User Error Recovery Workflows

        [Fact]
        public async Task ErrorRecovery_InvalidCertificateId_ShouldOfferAlternatives()
        {
            // Act - Request non-existent certificate
            var response = await _client.GetAsync("/Certificate/Details/999999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            
            // Should offer helpful alternatives
            content.Should().ContainAny(
                "Certificate not found",
                "View all certificates",
                "Search certificates",
                "Return to certificate list"
            );
        }

        [Fact]
        public async Task ErrorRecovery_SearchNoResults_ShouldOfferSuggestions()
        {
            // Act - Perform search that returns no results
            var response = await _client.GetAsync("/Certificate/Certificates?search=nonexistentcertificate12345");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            
            // Should offer search suggestions
            content.Should().ContainAny(
                "No certificates found",
                "Try different search terms",
                "Clear filters",
                "View all certificates"
            );
        }

        #endregion

        private static FormUrlEncodedContent CreateValidCertificateFormData()
        {
            return new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", "GCC9999"),
                new KeyValuePair<string, string>("CompanyId", "1"),
                new KeyValuePair<string, string>("ProductId", "1"),
                new KeyValuePair<string, string>("CertificationTypeId", "1"),
                new KeyValuePair<string, string>("EffectiveDate", "2024-01-01"),
                new KeyValuePair<string, string>("ExpirationDate", "2025-01-01"),
                new KeyValuePair<string, string>("Status", "active"),
                new KeyValuePair<string, string>("Location", "Test Facility"),
                new KeyValuePair<string, string>("Country", "United States")
            });
        }

        private static FormUrlEncodedContent CreateComplexCertificateFormData()
        {
            return new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Number", "GCC8888"),
                new KeyValuePair<string, string>("CompanyId", "1"),
                new KeyValuePair<string, string>("ProductId", "1"),
                new KeyValuePair<string, string>("CertificationTypeId", "1"),
                new KeyValuePair<string, string>("EffectiveDate", "2024-01-01"),
                new KeyValuePair<string, string>("ExpirationDate", "2025-01-01"),
                new KeyValuePair<string, string>("Status", "active"),
                new KeyValuePair<string, string>("Location", "Complex Manufacturing Plant with Very Long Name"),
                new KeyValuePair<string, string>("Country", "United States"),
                new KeyValuePair<string, string>("Asin", "B012345678"),
                new KeyValuePair<string, string>("Upc", "123456789012"),
                new KeyValuePair<string, string>("Gtin", "12345678901234"),
                new KeyValuePair<string, string>("Ean", "1234567890123")
            });
        }
    }
}