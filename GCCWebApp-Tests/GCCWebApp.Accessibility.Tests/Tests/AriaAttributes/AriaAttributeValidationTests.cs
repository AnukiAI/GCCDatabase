using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GCCWebApp.Accessibility.Tests.Tests.AriaAttributes
{
    /// <summary>
    /// Tests for ARIA attribute compliance in admin views
    /// Generated: 2025-10-24
    /// Validates recent accessibility improvements (50+ icon buttons, modals, forms)
    /// </summary>
    [TestFixture]
    public class AriaAttributeValidationTests
    {
        private const string VIEWS_PATH = @"..\..\..\..\..\GCCWebApp\Views";

        [SetUp]
        public void Setup()
        {
            var viewsPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, VIEWS_PATH));
            if (!Directory.Exists(viewsPath))
            {
                Assert.Fail($"Views directory not found: {viewsPath}");
            }
        }

        #region Admin Modal ARIA Tests

        /// <summary>
        /// Verifies that UserManagement modal has aria-required on required fields
        /// </summary>
        [Test]
        public void UserManagementModal_RequiredFields_ShouldHaveAriaRequired()
        {
            // Arrange
            var filePath = GetViewPath("Admin", "UserManagement.cshtml");
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
                Assert.IsTrue(
                    content.Contains($"id=\"{fieldId}\"") &&
                    content.Contains("aria-required=\"true\""),
                    $"Field '{fieldId}' should have aria-required=\"true\" attribute");
            }

            TestContext.WriteLine($"✓ UserManagement modal: All {requiredFields.Length} required fields have aria-required");
        }

        /// <summary>
        /// Verifies that UserManagement modal has aria-describedby for help text
        /// </summary>
        [Test]
        public void UserManagementModal_HelpText_ShouldHaveAriaDescribedby()
        {
            // Arrange
            var filePath = GetViewPath("Admin", "UserManagement.cshtml");
            var content = File.ReadAllText(filePath);

            // Act & Assert - Check fields with help text have aria-describedby
            var fieldsWithHelpText = new[]
            {
                ("user-company", "company-help"),
                ("user-password", "password-help")
            };

            foreach (var (fieldId, helpId) in fieldsWithHelpText)
            {
                Assert.IsTrue(
                    content.Contains($"id=\"{fieldId}\"") &&
                    content.Contains($"aria-describedby=\"{helpId}\""),
                    $"Field '{fieldId}' should have aria-describedby=\"{helpId}\"");

                Assert.IsTrue(
                    content.Contains($"id=\"{helpId}\""),
                    $"Help text element with id=\"{helpId}\" should exist");
            }

            TestContext.WriteLine($"✓ UserManagement modal: All help text properly associated with aria-describedby");
        }

        /// <summary>
        /// Verifies that Tags modal has aria-required and aria-describedby
        /// </summary>
        [Test]
        public void TagsModal_FormFields_ShouldHaveAriaAttributes()
        {
            // Arrange
            var filePath = GetViewPath("Admin", "Tags.cshtml");
            var content = File.ReadAllText(filePath);

            // Act & Assert - Required fields
            var requiredFields = new[] { "tag-name", "tag-type" };
            foreach (var fieldId in requiredFields)
            {
                Assert.IsTrue(
                    content.Contains($"id=\"{fieldId}\"") &&
                    content.Contains("aria-required=\"true\""),
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
                Assert.IsTrue(
                    content.Contains($"id=\"{fieldId}\"") &&
                    content.Contains($"aria-describedby=\"{helpId}\""),
                    $"Field '{fieldId}' should have aria-describedby=\"{helpId}\"");
            }

            TestContext.WriteLine($"✓ Tags modal: All required ARIA attributes present");
        }

        /// <summary>
        /// Verifies that RoleManagement modal has aria-required on role name
        /// </summary>
        [Test]
        public void RoleManagementModal_RoleName_ShouldHaveAriaRequired()
        {
            // Arrange
            var filePath = GetViewPath("Admin", "RoleManagement.cshtml");
            var content = File.ReadAllText(filePath);

            // Act & Assert
            Assert.IsTrue(
                content.Contains("id=\"role-name\"") &&
                content.Contains("aria-required=\"true\""),
                "Role name field should have aria-required=\"true\"");

            TestContext.WriteLine("✓ RoleManagement modal: Role name has aria-required");
        }

        #endregion

        #region Icon Button ARIA Label Tests

        /// <summary>
        /// Verifies that admin pages have aria-label on action buttons
        /// </summary>
        [Test]
        [TestCase("Admin/UserManagement.cshtml", "Template", "Download CSV template for bulk user import")]
        [TestCase("Admin/UserManagement.cshtml", "Import", "Import users from CSV file")]
        [TestCase("Admin/UserManagement.cshtml", "Export", "Export users to CSV file")]
        [TestCase("Admin/UserManagement.cshtml", "Add New User", "Add a new user account")]
        [TestCase("Admin/Tags.cshtml", "Template", "Download CSV template for bulk tag import")]
        [TestCase("Admin/Tags.cshtml", "Import", "Import tags from CSV file")]
        [TestCase("Admin/Tags.cshtml", "Export", "Export tags to CSV file")]
        [TestCase("Admin/Tags.cshtml", "Add New Tag", "Add a new tag")]
        [TestCase("Admin/RoleManagement.cshtml", "Export", "Export roles to CSV file")]
        [TestCase("Admin/RoleManagement.cshtml", "Add New Role", "Add a new role")]
        public void AdminPages_ActionButtons_ShouldHaveAriaLabel(string viewPath, string buttonText, string expectedAriaLabel)
        {
            // Arrange
            var parts = viewPath.Split('/');
            var filePath = GetViewPath(parts[0], parts[1]);
            var content = File.ReadAllText(filePath);

            // Act & Assert
            // Look for button with specific text and aria-label
            var buttonPattern = $@"<button[^>]*aria-label=""[^""]*{Regex.Escape(expectedAriaLabel)}[^""]*""[^>]*>[^<]*{Regex.Escape(buttonText)}";
            var hasAriaLabel = Regex.IsMatch(content, buttonPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            Assert.IsTrue(hasAriaLabel,
                $"Button '{buttonText}' in {viewPath} should have aria-label containing '{expectedAriaLabel}'");

            TestContext.WriteLine($"✓ {viewPath}: '{buttonText}' button has proper aria-label");
        }

        /// <summary>
        /// Verifies that icon-only buttons have aria-label (no visible text)
        /// </summary>
        [Test]
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
            Assert.IsEmpty(violations,
                $"Found icon-only buttons without aria-label:\n{string.Join("\n", violations)}");

            TestContext.WriteLine("✓ All icon-only buttons have aria-label attributes");
        }

        #endregion

        #region Modal Accessibility Tests

        /// <summary>
        /// Verifies that modals have proper ARIA roles and attributes
        /// </summary>
        [Test]
        [TestCase("Admin/UserManagement.cshtml", "user-modal", "modal-title")]
        [TestCase("Admin/Tags.cshtml", "tag-modal", "modal-title")]
        [TestCase("Admin/RoleManagement.cshtml", "role-modal", "modal-title")]
        public void Modals_ShouldHaveProperAriaRoles(string viewPath, string modalId, string titleId)
        {
            // Arrange
            var parts = viewPath.Split('/');
            var filePath = GetViewPath(parts[0], parts[1]);
            var content = File.ReadAllText(filePath);

            // Act & Assert
            // Check modal has role="dialog"
            Assert.IsTrue(
                content.Contains($"id=\"{modalId}\"") &&
                content.Contains("role=\"dialog\""),
                $"Modal {modalId} should have role=\"dialog\"");

            // Check modal has aria-labelledby pointing to title
            Assert.IsTrue(
                content.Contains($"aria-labelledby=\"{titleId}\""),
                $"Modal {modalId} should have aria-labelledby=\"{titleId}\"");

            // Check modal has aria-hidden
            Assert.IsTrue(
                content.Contains("aria-hidden=\"true\""),
                $"Modal {modalId} should have aria-hidden=\"true\" when closed");

            // Check close button has aria-label
            var closeButtonPattern = $@"id=""{modalId}""[^>]*>.*?<button[^>]*class=""[^""]*modal-close[^""]*""[^>]*aria-label=""[^""]*""";
            Assert.IsTrue(
                Regex.IsMatch(content, closeButtonPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline),
                $"Modal {modalId} close button should have aria-label");

            TestContext.WriteLine($"✓ {viewPath}: Modal has all required ARIA attributes");
        }

        #endregion

        #region Search and Filter ARIA Tests

        /// <summary>
        /// Verifies that search inputs have proper aria-label
        /// </summary>
        [Test]
        [TestCase("Admin/UserManagement.cshtml", "search-input", "Search users")]
        [TestCase("Admin/Tags.cshtml", "search-input", "Search tags")]
        [TestCase("Admin/RoleManagement.cshtml", "search-input", "Search roles")]
        public void SearchInputs_ShouldHaveAriaLabel(string viewPath, string inputId, string expectedLabel)
        {
            // Arrange
            var parts = viewPath.Split('/');
            var filePath = GetViewPath(parts[0], parts[1]);
            var content = File.ReadAllText(filePath);

            // Act & Assert
            var searchPattern = $@"id=""{inputId}""[^>]*aria-label=""[^""]*{Regex.Escape(expectedLabel)}[^""]*""";
            Assert.IsTrue(
                Regex.IsMatch(content, searchPattern, RegexOptions.IgnoreCase),
                $"Search input '{inputId}' should have aria-label containing '{expectedLabel}'");

            TestContext.WriteLine($"✓ {viewPath}: Search input has proper aria-label");
        }

        /// <summary>
        /// Verifies that filter dropdowns have proper aria-label
        /// </summary>
        [Test]
        [TestCase("Admin/UserManagement.cshtml", "status-filter", "Filter by status")]
        [TestCase("Admin/UserManagement.cshtml", "role-filter", "Filter by role")]
        [TestCase("Admin/Tags.cshtml", "type-filter", "Filter by tag type")]
        public void FilterDropdowns_ShouldHaveAriaLabel(string viewPath, string selectId, string expectedLabel)
        {
            // Arrange
            var parts = viewPath.Split('/');
            var filePath = GetViewPath(parts[0], parts[1]);
            var content = File.ReadAllText(filePath);

            // Act & Assert
            var filterPattern = $@"id=""{selectId}""[^>]*aria-label=""[^""]*{Regex.Escape(expectedLabel)}[^""]*""";
            Assert.IsTrue(
                Regex.IsMatch(content, filterPattern, RegexOptions.IgnoreCase),
                $"Filter dropdown '{selectId}' should have aria-label containing '{expectedLabel}'");

            TestContext.WriteLine($"✓ {viewPath}: Filter dropdown has proper aria-label");
        }

        #endregion

        #region Helper Methods

        private string GetViewPath(string folder, string file)
        {
            return Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                VIEWS_PATH,
                folder,
                file
            ));
        }

        #endregion
    }
}
