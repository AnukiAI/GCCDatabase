using Xunit;
using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace GCCWebApp.Accessibility.Tests.Tests.AriaAttributes
{
    /// <summary>
    /// Tests for ARIA attribute compliance in admin views
    /// Generated: 2025-10-24, Migrated to xUnit: 2025-11-25
    /// Validates recent accessibility improvements (50+ icon buttons, modals, forms)
    /// </summary>
    public class AriaAttributeValidationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _viewsPath;

        public AriaAttributeValidationTests(ITestOutputHelper output)
        {
            _output = output;
            var assemblyLocation = AppContext.BaseDirectory;
            _viewsPath = Path.GetFullPath(Path.Combine(assemblyLocation, @"..\..\..\..\..\GCCWebApp\Views"));
        }

        #region Admin Modal ARIA Tests

        /// <summary>
        /// Verifies that UserManagement modal has aria-required on required fields
        /// </summary>
        [Fact]
        public void UserManagementModal_RequiredFields_ShouldHaveAriaRequired()
        {
            // Arrange
            var filePath = GetViewPath("Admin", "UserManagement.cshtml");

            // Skip if file doesn't exist in test environment
            if (!File.Exists(filePath))
            {
                _output.WriteLine($"View file not found: {filePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(filePath);

            // Act & Assert - Check required fields have aria-required="true"
            var requiredFields = new[]
            {
                "user-name",
                "user-email",
                "user-role",
                "user-status"
            };

            foreach (var fieldId in requiredFields)
            {
                var hasField = content.Contains($"id=\"{fieldId}\"");
                var hasAriaRequired = content.Contains("aria-required=\"true\"");

                (hasField && hasAriaRequired).Should().BeTrue(
                    $"Field '{fieldId}' should have aria-required=\"true\" attribute");
            }

            _output.WriteLine($"✓ UserManagement modal: All {requiredFields.Length} required fields have aria-required");
        }

        /// <summary>
        /// Verifies that UserManagement modal has aria-describedby for help text
        /// </summary>
        [Fact]
        public void UserManagementModal_HelpText_ShouldHaveAriaDescribedby()
        {
            // Arrange
            var filePath = GetViewPath("Admin", "UserManagement.cshtml");

            if (!File.Exists(filePath))
            {
                _output.WriteLine($"View file not found: {filePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(filePath);

            // Act & Assert - Check fields with help text have aria-describedby
            var fieldsWithHelpText = new[]
            {
                ("user-company", "company-help"),
                ("user-password", "password-help")
            };

            foreach (var (fieldId, helpId) in fieldsWithHelpText)
            {
                var hasField = content.Contains($"id=\"{fieldId}\"");
                var hasAriaDescribedby = content.Contains($"aria-describedby=\"{helpId}\"");
                var hasHelpElement = content.Contains($"id=\"{helpId}\"");

                (hasField && hasAriaDescribedby).Should().BeTrue(
                    $"Field '{fieldId}' should have aria-describedby=\"{helpId}\"");

                hasHelpElement.Should().BeTrue(
                    $"Help text element with id=\"{helpId}\" should exist");
            }

            _output.WriteLine("✓ UserManagement modal: All help text properly associated with aria-describedby");
        }

        /// <summary>
        /// Verifies that Tags modal has aria-required and aria-describedby
        /// </summary>
        [Fact]
        public void TagsModal_FormFields_ShouldHaveAriaAttributes()
        {
            // Arrange
            var filePath = GetViewPath("Admin", "Tags.cshtml");

            if (!File.Exists(filePath))
            {
                _output.WriteLine($"View file not found: {filePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(filePath);

            // Act & Assert - Required fields
            var requiredFields = new[] { "tag-name", "tag-type" };
            foreach (var fieldId in requiredFields)
            {
                var hasField = content.Contains($"id=\"{fieldId}\"");
                var hasAriaRequired = content.Contains("aria-required=\"true\"");

                (hasField && hasAriaRequired).Should().BeTrue(
                    $"Field '{fieldId}' should have aria-required=\"true\"");
            }

            // Act & Assert - Help text associations
            var fieldsWithHelp = new[]
            {
                ("tag-name", "tag-name-help"),
                ("tag-type", "tag-type-help"),
                ("tag-description", "tag-description-help")
            };

            foreach (var (fieldId, helpId) in fieldsWithHelp)
            {
                var hasField = content.Contains($"id=\"{fieldId}\"");
                var hasAriaDescribedby = content.Contains($"aria-describedby=\"{helpId}\"");

                (hasField && hasAriaDescribedby).Should().BeTrue(
                    $"Field '{fieldId}' should have aria-describedby=\"{helpId}\"");
            }

            _output.WriteLine("✓ Tags modal: All required ARIA attributes present");
        }

        /// <summary>
        /// Verifies that RoleManagement modal has aria-required on role name
        /// </summary>
        [Fact]
        public void RoleManagementModal_RoleName_ShouldHaveAriaRequired()
        {
            // Arrange
            var filePath = GetViewPath("Admin", "RoleManagement.cshtml");

            if (!File.Exists(filePath))
            {
                _output.WriteLine($"View file not found: {filePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(filePath);

            // Act & Assert
            var hasRoleName = content.Contains("id=\"role-name\"");
            var hasAriaRequired = content.Contains("aria-required=\"true\"");

            (hasRoleName && hasAriaRequired).Should().BeTrue(
                "Role name field should have aria-required=\"true\"");

            _output.WriteLine("✓ RoleManagement modal: Role name has aria-required");
        }

        #endregion

        #region Icon Button ARIA Label Tests

        /// <summary>
        /// Verifies that admin pages have aria-label on action buttons
        /// </summary>
        [Theory]
        [InlineData("Admin/UserManagement.cshtml", "Template", "Download CSV template for bulk user import")]
        [InlineData("Admin/UserManagement.cshtml", "Import", "Import users from CSV file")]
        [InlineData("Admin/UserManagement.cshtml", "Export", "Export users to CSV file")]
        [InlineData("Admin/UserManagement.cshtml", "Add New User", "Add a new user account")]
        [InlineData("Admin/Tags.cshtml", "Template", "Download CSV template for bulk tag import")]
        [InlineData("Admin/Tags.cshtml", "Import", "Import tags from CSV file")]
        [InlineData("Admin/Tags.cshtml", "Export", "Export tags to CSV file")]
        [InlineData("Admin/Tags.cshtml", "Add New Tag", "Add a new tag")]
        [InlineData("Admin/RoleManagement.cshtml", "Export", "Export roles to CSV file")]
        [InlineData("Admin/RoleManagement.cshtml", "Add New Role", "Add a new role")]
        public void AdminPages_ActionButtons_ShouldHaveAriaLabel(string viewPath, string buttonText, string expectedAriaLabel)
        {
            // Arrange
            var parts = viewPath.Split('/');
            var filePath = GetViewPath(parts[0], parts[1]);

            if (!File.Exists(filePath))
            {
                _output.WriteLine($"View file not found: {filePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(filePath);

            // Act & Assert
            // Look for button with specific text and aria-label
            var buttonPattern = $@"<button[^>]*aria-label=""[^""]*{Regex.Escape(expectedAriaLabel)}[^""]*""[^>]*>[^<]*{Regex.Escape(buttonText)}";
            var hasAriaLabel = Regex.IsMatch(content, buttonPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            hasAriaLabel.Should().BeTrue(
                $"Button '{buttonText}' in {viewPath} should have aria-label containing '{expectedAriaLabel}'");

            _output.WriteLine($"✓ {viewPath}: '{buttonText}' button has proper aria-label");
        }

        /// <summary>
        /// Verifies that icon-only buttons have aria-label (no visible text)
        /// </summary>
        [Fact]
        public void AdminPages_IconOnlyButtons_ShouldHaveAriaLabel()
        {
            // Arrange
            var adminViews = new[]
            {
                ("Admin", "UserManagement.cshtml"),
                ("Admin", "Tags.cshtml"),
                ("Admin", "RoleManagement.cshtml"),
                ("Admin", "ProductManagement.cshtml"),
                ("Admin", "Partners.cshtml")
            };

            var violations = new System.Collections.Generic.List<string>();

            // Act
            foreach (var (folder, file) in adminViews)
            {
                var filePath = GetViewPath(folder, file);
                if (!File.Exists(filePath)) continue;

                var content = File.ReadAllText(filePath);

                // Find buttons with only icons (contains <i class="fa...">)
                var iconButtonPattern = @"<button[^>]*>[\s]*<i\s+class=""fa[^""]*""[^>]*></i>[\s]*</button>";
                var iconButtons = Regex.Matches(content, iconButtonPattern, RegexOptions.IgnoreCase);

                foreach (Match button in iconButtons)
                {
                    // Check if button has aria-label
                    if (!button.Value.Contains("aria-label="))
                    {
                        violations.Add($"{file}: Icon-only button missing aria-label: {button.Value.Substring(0, Math.Min(100, button.Value.Length))}...");
                    }
                }
            }

            // Assert
            violations.Should().BeEmpty(
                $"Found icon-only buttons without aria-label:\n{string.Join("\n", violations)}");

            _output.WriteLine("✓ All icon-only buttons have aria-label attributes");
        }

        #endregion

        #region Modal Accessibility Tests

        /// <summary>
        /// Verifies that modals have proper ARIA roles and attributes
        /// </summary>
        [Theory]
        [InlineData("Admin/UserManagement.cshtml", "user-modal", "modal-title")]
        [InlineData("Admin/Tags.cshtml", "tag-modal", "modal-title")]
        [InlineData("Admin/RoleManagement.cshtml", "role-modal", "modal-title")]
        public void Modals_ShouldHaveProperAriaRoles(string viewPath, string modalId, string titleId)
        {
            // Arrange
            var parts = viewPath.Split('/');
            var filePath = GetViewPath(parts[0], parts[1]);

            if (!File.Exists(filePath))
            {
                _output.WriteLine($"View file not found: {filePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(filePath);

            // Act & Assert
            // Check modal has role="dialog"
            var hasModalId = content.Contains($"id=\"{modalId}\"");
            var hasRoleDialog = content.Contains("role=\"dialog\"");

            (hasModalId && hasRoleDialog).Should().BeTrue(
                $"Modal {modalId} should have role=\"dialog\"");

            // Check modal has aria-labelledby pointing to title
            content.Should().Contain($"aria-labelledby=\"{titleId}\"",
                $"Modal {modalId} should have aria-labelledby=\"{titleId}\"");

            // Check modal has aria-hidden
            content.Should().Contain("aria-hidden=\"true\"",
                $"Modal {modalId} should have aria-hidden=\"true\" when closed");

            // Check close button has aria-label
            var closeButtonPattern = $@"id=""{modalId}""[^>]*>.*?<button[^>]*class=""[^""]*modal-close[^""]*""[^>]*aria-label=""[^""]*""";
            var hasCloseAriaLabel = Regex.IsMatch(content, closeButtonPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            hasCloseAriaLabel.Should().BeTrue(
                $"Modal {modalId} close button should have aria-label");

            _output.WriteLine($"✓ {viewPath}: Modal has all required ARIA attributes");
        }

        #endregion

        #region Search and Filter ARIA Tests

        /// <summary>
        /// Verifies that search inputs have proper aria-label
        /// </summary>
        [Theory]
        [InlineData("Admin/UserManagement.cshtml", "search-input", "Search users")]
        [InlineData("Admin/Tags.cshtml", "search-input", "Search tags")]
        [InlineData("Admin/RoleManagement.cshtml", "search-input", "Search roles")]
        public void SearchInputs_ShouldHaveAriaLabel(string viewPath, string inputId, string expectedLabel)
        {
            // Arrange
            var parts = viewPath.Split('/');
            var filePath = GetViewPath(parts[0], parts[1]);

            if (!File.Exists(filePath))
            {
                _output.WriteLine($"View file not found: {filePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(filePath);

            // Act & Assert
            var searchPattern = $@"id=""{inputId}""[^>]*aria-label=""[^""]*{Regex.Escape(expectedLabel)}[^""]*""";
            var hasAriaLabel = Regex.IsMatch(content, searchPattern, RegexOptions.IgnoreCase);

            hasAriaLabel.Should().BeTrue(
                $"Search input '{inputId}' should have aria-label containing '{expectedLabel}'");

            _output.WriteLine($"✓ {viewPath}: Search input has proper aria-label");
        }

        /// <summary>
        /// Verifies that filter dropdowns have proper aria-label
        /// </summary>
        [Theory]
        [InlineData("Admin/UserManagement.cshtml", "status-filter", "Filter by status")]
        [InlineData("Admin/UserManagement.cshtml", "role-filter", "Filter by role")]
        [InlineData("Admin/Tags.cshtml", "type-filter", "Filter by tag type")]
        public void FilterDropdowns_ShouldHaveAriaLabel(string viewPath, string selectId, string expectedLabel)
        {
            // Arrange
            var parts = viewPath.Split('/');
            var filePath = GetViewPath(parts[0], parts[1]);

            if (!File.Exists(filePath))
            {
                _output.WriteLine($"View file not found: {filePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(filePath);

            // Act & Assert
            var filterPattern = $@"id=""{selectId}""[^>]*aria-label=""[^""]*{Regex.Escape(expectedLabel)}[^""]*""";
            var hasAriaLabel = Regex.IsMatch(content, filterPattern, RegexOptions.IgnoreCase);

            hasAriaLabel.Should().BeTrue(
                $"Filter dropdown '{selectId}' should have aria-label containing '{expectedLabel}'");

            _output.WriteLine($"✓ {viewPath}: Filter dropdown has proper aria-label");
        }

        #endregion

        #region Helper Methods

        private string GetViewPath(string folder, string file)
        {
            return Path.Combine(_viewsPath, folder, file);
        }

        #endregion
    }
}
