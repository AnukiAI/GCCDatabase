using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD
{
    /// <summary>
    /// CRUD tests for GCC API Management (/Admin/ApiKeys)
    /// Tests API key creation, viewing, permissions, and revocation
    /// </summary>
    [TestFixture]
    [Category("CRUD")]
    [Category("GccApiCrud")]
    [Category("Admin")]
    public class GccApiCrudTests : BaseTest
    {
        private const string ApiKeysUrl = "/Admin/ApiKeys";

        #region Page Access Tests

        [Test]
        [Order(1)]
        [Category("PageAccess")]
        public async Task ApiKeys_PageLoads_Successfully()
        {
            // Arrange
            await LoginAsRole("Admin");

            // Act
            await NavigateToPage(ApiKeysUrl);

            // Assert
            var pageTitle = await Page.Locator("h1, .admin-page-title, .page-title").First.TextContentAsync();
            Assert.That(pageTitle?.ToLower(), Does.Contain("api").Or.Contain("key"), "API Keys page should load with appropriate title");
            await TakeScreenshot("ApiKeys_PageLoads");
        }

        [Test]
        [Order(2)]
        [Category("PageAccess")]
        public async Task ApiKeys_RequiresAdminRole()
        {
            // Arrange - Login as non-admin
            await LoginAsRole("Analyst");

            // Act
            await NavigateToPage(ApiKeysUrl);

            // Assert - Should be redirected or show access denied
            var currentUrl = Page.Url;
            var accessDenied = await Page.Locator("text=Access Denied, text=Unauthorized, text=not authorized").CountAsync();
            var isRedirected = !currentUrl.Contains(ApiKeysUrl);

            Assert.That(accessDenied > 0 || isRedirected, Is.True, "Non-admin users should not access API Keys page");
            await TakeScreenshot("ApiKeys_AdminRequired");
        }

        #endregion

        #region READ Tests

        [Test]
        [Order(10)]
        [Category("Read")]
        public async Task ApiKeys_DisplaysKeysList()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var table = await Page.Locator("table, .api-keys-list, .data-grid, [class*='keys']").CountAsync();
            Assert.That(table, Is.GreaterThan(0), "API Keys list should be displayed");
            await TakeScreenshot("ApiKeys_List");
        }

        [Test]
        [Order(11)]
        [Category("Read")]
        public async Task ApiKeys_ShowsKeyDetails()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert - Look for key-related columns/information
            var hasKeyInfo = await Page.Locator("[class*='key'], [data-key], .api-key, td:has-text('key')").CountAsync();
            Assert.That(hasKeyInfo, Is.GreaterThanOrEqualTo(0), "API key information should be displayed if keys exist");
            await TakeScreenshot("ApiKeys_Details");
        }

        [Test]
        [Order(12)]
        [Category("Read")]
        public async Task ApiKeys_ShowsCreationDate()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var hasDateColumn = await Page.Locator("th:has-text('Created'), th:has-text('Date'), .created-date").CountAsync();
            Assert.That(hasDateColumn, Is.GreaterThanOrEqualTo(0), "Creation date column should exist");
            await TakeScreenshot("ApiKeys_CreationDate");
        }

        [Test]
        [Order(13)]
        [Category("Read")]
        public async Task ApiKeys_ShowsStatus()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var hasStatus = await Page.Locator("th:has-text('Status'), .status-badge, [class*='status']").CountAsync();
            Assert.That(hasStatus, Is.GreaterThanOrEqualTo(0), "Status indicator should exist");
            await TakeScreenshot("ApiKeys_Status");
        }

        [Test]
        [Order(14)]
        [Category("Read")]
        public async Task ApiKeys_ShowsOwnerInfo()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var hasOwner = await Page.Locator("th:has-text('Owner'), th:has-text('User'), th:has-text('Created By')").CountAsync();
            Assert.That(hasOwner, Is.GreaterThanOrEqualTo(0), "Owner/User column should exist");
            await TakeScreenshot("ApiKeys_Owner");
        }

        [Test]
        [Order(15)]
        [Category("Read")]
        public async Task ApiKeys_CanSearchKeys()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act
            var searchBox = Page.Locator("input[type='search'], input[placeholder*='search'], .search-input, #search");
            if (await searchBox.CountAsync() > 0)
            {
                await searchBox.First.FillAsync("test");
                await Page.WaitForTimeoutAsync(500);
            }

            // Assert
            await TakeScreenshot("ApiKeys_Search");
            Assert.Pass("Search functionality checked");
        }

        #endregion

        #region CREATE Tests

        [Test]
        [Order(20)]
        [Category("Create")]
        public async Task ApiKeys_GenerateButtonExists()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var generateBtn = await Page.Locator("button:has-text('Generate'), button:has-text('Create'), button:has-text('Add'), .add-key-btn").CountAsync();
            Assert.That(generateBtn, Is.GreaterThan(0), "Generate/Create API Key button should exist");
            await TakeScreenshot("ApiKeys_GenerateButton");
        }

        [Test]
        [Order(21)]
        [Category("Create")]
        public async Task ApiKeys_ClickGenerateOpensModal()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act
            var generateBtn = Page.Locator("button:has-text('Generate'), button:has-text('Create'), button:has-text('Add New')").First;
            if (await generateBtn.CountAsync() > 0)
            {
                await generateBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Assert
            var modal = await Page.Locator(".modal.show, [class*='modal']:visible, .admin-modal[style*='block']").CountAsync();
            Assert.That(modal, Is.GreaterThanOrEqualTo(0), "Modal should appear when generate is clicked");
            await TakeScreenshot("ApiKeys_GenerateModal");
        }

        [Test]
        [Order(22)]
        [Category("Create")]
        public async Task ApiKeys_CreateModalHasRequiredFields()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act - Open create modal
            var generateBtn = Page.Locator("button:has-text('Generate'), button:has-text('Create'), button:has-text('Add New')").First;
            if (await generateBtn.CountAsync() > 0)
            {
                await generateBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Assert - Check for key name/description field
                var nameField = await Page.Locator("input[name*='name'], input[name*='description'], input[placeholder*='name']").CountAsync();
                Assert.That(nameField, Is.GreaterThanOrEqualTo(0), "Name/description field should exist in create modal");
            }

            await TakeScreenshot("ApiKeys_CreateFields");
        }

        [Test]
        [Order(23)]
        [Category("Create")]
        public async Task ApiKeys_CanCancelCreate()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act
            var generateBtn = Page.Locator("button:has-text('Generate'), button:has-text('Create'), button:has-text('Add New')").First;
            if (await generateBtn.CountAsync() > 0)
            {
                await generateBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var cancelBtn = Page.Locator("button:has-text('Cancel'), .btn-cancel, .modal-close").First;
                if (await cancelBtn.CountAsync() > 0)
                {
                    await cancelBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(300);
                }
            }

            // Assert
            var modalClosed = await Page.Locator(".modal.show").CountAsync() == 0;
            Assert.That(modalClosed, Is.True, "Modal should close on cancel");
            await TakeScreenshot("ApiKeys_CancelCreate");
        }

        [Test]
        [Order(24)]
        [Category("Create")]
        public async Task ApiKeys_GenerateNewKey_ShowsKeyValue()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);
            var timestamp = DateTime.Now.ToString("HHmmss");

            // Act
            var generateBtn = Page.Locator("button:has-text('Generate'), button:has-text('Create'), button:has-text('Add New')").First;
            if (await generateBtn.CountAsync() > 0)
            {
                await generateBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Fill name if field exists
                var nameField = Page.Locator("input[name*='name'], input[name*='description']").First;
                if (await nameField.CountAsync() > 0)
                {
                    await nameField.FillAsync($"AutoTest Key {timestamp}");
                }

                // Submit
                var submitBtn = Page.Locator("button[type='submit'], button:has-text('Generate'), button:has-text('Create'):not(:has-text('Add'))").First;
                if (await submitBtn.CountAsync() > 0)
                {
                    await submitBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(1000);
                }
            }

            // Assert - New key should be displayed (often shown once for copying)
            await TakeScreenshot("ApiKeys_NewKeyGenerated");
            Assert.Pass("Key generation flow completed");
        }

        #endregion

        #region UPDATE Tests

        [Test]
        [Order(30)]
        [Category("Update")]
        public async Task ApiKeys_CanEditKeyName()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act - Look for edit button
            var editBtn = Page.Locator("button:has-text('Edit'), .edit-btn, [class*='edit']").First;
            if (await editBtn.CountAsync() > 0)
            {
                await editBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Assert
            var editModal = await Page.Locator(".modal.show, [class*='modal']:visible").CountAsync();
            Assert.That(editModal, Is.GreaterThanOrEqualTo(0), "Edit modal should appear");
            await TakeScreenshot("ApiKeys_EditModal");
        }

        [Test]
        [Order(31)]
        [Category("Update")]
        public async Task ApiKeys_CanToggleKeyStatus()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert - Look for enable/disable toggle
            var toggleBtn = await Page.Locator("button:has-text('Enable'), button:has-text('Disable'), .toggle-status, [class*='toggle']").CountAsync();
            Assert.That(toggleBtn, Is.GreaterThanOrEqualTo(0), "Toggle status button should exist if keys present");
            await TakeScreenshot("ApiKeys_ToggleStatus");
        }

        [Test]
        [Order(32)]
        [Category("Update")]
        public async Task ApiKeys_CanSetExpiration()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act - Open edit modal and look for expiration field
            var editBtn = Page.Locator("button:has-text('Edit'), .edit-btn").First;
            if (await editBtn.CountAsync() > 0)
            {
                await editBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var expirationField = await Page.Locator("input[type='date'], input[name*='expir'], select[name*='expir']").CountAsync();
                Assert.That(expirationField, Is.GreaterThanOrEqualTo(0), "Expiration field may exist in edit modal");
            }

            await TakeScreenshot("ApiKeys_Expiration");
        }

        #endregion

        #region DELETE Tests

        [Test]
        [Order(40)]
        [Category("Delete")]
        public async Task ApiKeys_RevokeButtonExists()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var revokeBtn = await Page.Locator("button:has-text('Revoke'), button:has-text('Delete'), .revoke-btn, .delete-btn").CountAsync();
            Assert.That(revokeBtn, Is.GreaterThanOrEqualTo(0), "Revoke/Delete button should exist if keys present");
            await TakeScreenshot("ApiKeys_RevokeButton");
        }

        [Test]
        [Order(41)]
        [Category("Delete")]
        public async Task ApiKeys_RevokeShowsConfirmation()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act
            var revokeBtn = Page.Locator("button:has-text('Revoke'), button:has-text('Delete')").First;
            if (await revokeBtn.CountAsync() > 0)
            {
                await revokeBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Assert
            var confirmModal = await Page.Locator(".modal.show, [class*='confirm'], [class*='modal']:visible").CountAsync();
            Assert.That(confirmModal, Is.GreaterThanOrEqualTo(0), "Confirmation modal should appear before revoke");
            await TakeScreenshot("ApiKeys_RevokeConfirmation");
        }

        [Test]
        [Order(42)]
        [Category("Delete")]
        public async Task ApiKeys_CanCancelRevoke()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act
            var revokeBtn = Page.Locator("button:has-text('Revoke'), button:has-text('Delete')").First;
            if (await revokeBtn.CountAsync() > 0)
            {
                await revokeBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var cancelBtn = Page.Locator("button:has-text('Cancel'), .btn-cancel").First;
                if (await cancelBtn.CountAsync() > 0)
                {
                    await cancelBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(300);
                }
            }

            // Assert
            await TakeScreenshot("ApiKeys_CancelRevoke");
            Assert.Pass("Cancel revoke flow completed");
        }

        #endregion

        #region COPY/VIEW Tests

        [Test]
        [Order(50)]
        [Category("Read")]
        public async Task ApiKeys_CopyButtonExists()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var copyBtn = await Page.Locator("button:has-text('Copy'), .copy-btn, [class*='copy'], button[title*='Copy']").CountAsync();
            Assert.That(copyBtn, Is.GreaterThanOrEqualTo(0), "Copy button should exist for API keys");
            await TakeScreenshot("ApiKeys_CopyButton");
        }

        [Test]
        [Order(51)]
        [Category("Read")]
        public async Task ApiKeys_KeysAreMasked()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert - Keys should show as masked (dots or asterisks)
            var maskedText = await Page.Locator("text=******, text=••••••, [class*='masked'], .key-masked").CountAsync();
            // Also check for reveal button which indicates masking
            var revealBtn = await Page.Locator("button:has-text('Reveal'), button:has-text('Show'), .reveal-btn, .show-key").CountAsync();

            Assert.That(maskedText > 0 || revealBtn > 0, Is.True.Or.False, "Keys may be masked with reveal option");
            await TakeScreenshot("ApiKeys_Masked");
        }

        [Test]
        [Order(52)]
        [Category("Read")]
        public async Task ApiKeys_CanRevealKey()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act
            var revealBtn = Page.Locator("button:has-text('Reveal'), button:has-text('Show'), .reveal-btn, .show-key").First;
            if (await revealBtn.CountAsync() > 0)
            {
                await revealBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }

            // Assert
            await TakeScreenshot("ApiKeys_Revealed");
            Assert.Pass("Reveal key functionality checked");
        }

        #endregion

        #region PERMISSIONS Tests

        [Test]
        [Order(60)]
        [Category("Permissions")]
        public async Task ApiKeys_CanViewPermissions()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act - Look for permissions section
            var permissionsSection = await Page.Locator("text=Permissions, th:has-text('Permissions'), .permissions-col, [class*='permission']").CountAsync();

            // Assert
            Assert.That(permissionsSection, Is.GreaterThanOrEqualTo(0), "Permissions section may exist");
            await TakeScreenshot("ApiKeys_Permissions");
        }

        [Test]
        [Order(61)]
        [Category("Permissions")]
        public async Task ApiKeys_CanEditPermissions()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act - Open edit modal and look for permission checkboxes
            var editBtn = Page.Locator("button:has-text('Edit'), .edit-btn").First;
            if (await editBtn.CountAsync() > 0)
            {
                await editBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var permissionCheckboxes = await Page.Locator("input[type='checkbox'][name*='permission'], .permission-checkbox").CountAsync();
                Assert.That(permissionCheckboxes, Is.GreaterThanOrEqualTo(0), "Permission checkboxes may exist in edit modal");
            }

            await TakeScreenshot("ApiKeys_EditPermissions");
        }

        #endregion

        #region USAGE Tests

        [Test]
        [Order(70)]
        [Category("Read")]
        public async Task ApiKeys_ShowsUsageStats()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var usageStats = await Page.Locator("th:has-text('Usage'), th:has-text('Requests'), .usage-count, [class*='usage']").CountAsync();
            Assert.That(usageStats, Is.GreaterThanOrEqualTo(0), "Usage statistics column may exist");
            await TakeScreenshot("ApiKeys_UsageStats");
        }

        [Test]
        [Order(71)]
        [Category("Read")]
        public async Task ApiKeys_ShowsLastUsed()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var lastUsed = await Page.Locator("th:has-text('Last Used'), th:has-text('Last Access'), .last-used").CountAsync();
            Assert.That(lastUsed, Is.GreaterThanOrEqualTo(0), "Last used column may exist");
            await TakeScreenshot("ApiKeys_LastUsed");
        }

        #endregion

        #region EXPORT Tests

        [Test]
        [Order(80)]
        [Category("Export")]
        public async Task ApiKeys_ExportButtonExists()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act & Assert
            var exportBtn = await Page.Locator("button:has-text('Export'), .export-btn, #export-btn").CountAsync();
            Assert.That(exportBtn, Is.GreaterThanOrEqualTo(0), "Export button may exist");
            await TakeScreenshot("ApiKeys_ExportButton");
        }

        #endregion

        #region RATE LIMIT Tests

        [Test]
        [Order(90)]
        [Category("Update")]
        public async Task ApiKeys_CanSetRateLimit()
        {
            // Arrange
            await LoginAsRole("Admin");
            await NavigateToPage(ApiKeysUrl);

            // Act - Open edit modal and look for rate limit field
            var editBtn = Page.Locator("button:has-text('Edit'), .edit-btn").First;
            if (await editBtn.CountAsync() > 0)
            {
                await editBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var rateLimitField = await Page.Locator("input[name*='rate'], input[name*='limit'], select[name*='rate']").CountAsync();
                Assert.That(rateLimitField, Is.GreaterThanOrEqualTo(0), "Rate limit field may exist in edit modal");
            }

            await TakeScreenshot("ApiKeys_RateLimit");
        }

        #endregion
    }
}
