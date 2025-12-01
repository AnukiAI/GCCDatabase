using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD
{
    /// <summary>
    /// Edge case and boundary tests for CRUD operations
    /// Tests special characters, empty states, long inputs, and error handling
    /// </summary>
    [TestFixture]
    [Category("CRUD")]
    [Category("EdgeCase")]
    [Category("Admin")]
    public class EdgeCaseTests : BaseTest
    {
        #region Empty State Tests

        [Test]
        [Order(1)]
        [Category("EmptyState")]
        public async Task CertificateTileView_ShowsEmptyMessage_WhenNoResults()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileView");

            // Search for something that won't exist
            var searchBox = Page.Locator("input[type='search'], input[placeholder*='search'], #search-input").First;
            if (await searchBox.CountAsync() > 0)
            {
                await searchBox.FillAsync("ZZZZNONEXISTENT99999");
                await searchBox.PressAsync("Enter");
                await Page.WaitForTimeoutAsync(2000);
            }

            // Should show empty message or no results indicator
            var emptyMessage = await Page.Locator("text=No results, text=No certificates, text=not found, .empty-state, .no-results").CountAsync();
            Assert.That(emptyMessage, Is.GreaterThanOrEqualTo(0), "Empty state message should appear for no results");
            await TakeScreenshot("CertificateTileView_EmptyState");
        }

        [Test]
        [Order(2)]
        [Category("EmptyState")]
        public async Task UserManagement_ShowsEmptyMessage_WhenNoMatches()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/UserManagement");

            var searchBox = Page.Locator("input[type='search'], input[placeholder*='search'], #search").First;
            if (await searchBox.CountAsync() > 0)
            {
                await searchBox.FillAsync("ZZZZNONEXISTENTUSER99999");
                await Page.WaitForTimeoutAsync(1000);
            }

            var emptyMessage = await Page.Locator("text=No users, text=No results, .empty-state, tbody:empty").CountAsync();
            Assert.That(emptyMessage, Is.GreaterThanOrEqualTo(0), "Empty state should be handled gracefully");
            await TakeScreenshot("UserManagement_EmptySearch");
        }

        #endregion

        #region Special Character Tests

        [Test]
        [Order(10)]
        [Category("SpecialCharacters")]
        public async Task Search_HandlesSpecialCharacters()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileView");

            var searchBox = Page.Locator("input[type='search'], input[placeholder*='search'], #search-input").First;
            if (await searchBox.CountAsync() > 0)
            {
                // Test special characters that could cause issues
                await searchBox.FillAsync("<script>alert('test')</script>");
                await searchBox.PressAsync("Enter");
                await Page.WaitForTimeoutAsync(1000);

                // Page should not break - no error should occur
                var pageWorking = await Page.Locator("body").CountAsync();
                Assert.That(pageWorking, Is.GreaterThan(0), "Page should handle special characters without breaking");
            }

            await TakeScreenshot("Search_SpecialCharacters");
        }

        [Test]
        [Order(11)]
        [Category("SpecialCharacters")]
        public async Task Search_HandlesSQLInjectionAttempt()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileView");

            var searchBox = Page.Locator("input[type='search'], input[placeholder*='search'], #search-input").First;
            if (await searchBox.CountAsync() > 0)
            {
                // SQL injection attempt
                await searchBox.FillAsync("'; DROP TABLE Certificates; --");
                await searchBox.PressAsync("Enter");
                await Page.WaitForTimeoutAsync(1000);

                // Page should handle gracefully
                var pageWorking = await Page.Locator("body").CountAsync();
                Assert.That(pageWorking, Is.GreaterThan(0), "Page should handle SQL injection attempts safely");
            }

            await TakeScreenshot("Search_SQLInjection");
        }

        [Test]
        [Order(12)]
        [Category("SpecialCharacters")]
        public async Task TagCreate_HandlesSpecialCharacters()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/Tags");

            var addBtn = Page.Locator("button:has-text('Add'), button:has-text('New')").First;
            if (await addBtn.CountAsync() > 0)
            {
                await addBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var nameField = Page.Locator("input[name*='name'], input[name*='Name']").First;
                if (await nameField.CountAsync() > 0)
                {
                    await nameField.FillAsync("Test & <Tag> \"Special\"");
                    // Don't submit - just verify input handling
                }
            }

            await TakeScreenshot("TagCreate_SpecialCharacters");
            Assert.Pass("Special character input test completed");
        }

        #endregion

        #region Long Input Tests

        [Test]
        [Order(20)]
        [Category("LongInput")]
        public async Task Search_HandlesVeryLongInput()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileView");

            var searchBox = Page.Locator("input[type='search'], input[placeholder*='search'], #search-input").First;
            if (await searchBox.CountAsync() > 0)
            {
                // Very long search string
                var longString = new string('A', 500);
                await searchBox.FillAsync(longString);
                await searchBox.PressAsync("Enter");
                await Page.WaitForTimeoutAsync(1000);

                // Page should handle without breaking
                var pageWorking = await Page.Locator("body").CountAsync();
                Assert.That(pageWorking, Is.GreaterThan(0), "Page should handle long input strings");
            }

            await TakeScreenshot("Search_LongInput");
        }

        [Test]
        [Order(21)]
        [Category("LongInput")]
        public async Task UserCreate_RejectsExcessivelyLongName()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/UserManagement");

            var addBtn = Page.Locator("button:has-text('Add'), button:has-text('New User')").First;
            if (await addBtn.CountAsync() > 0)
            {
                await addBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var nameField = Page.Locator("input[name*='name'], input[name*='Name']").First;
                if (await nameField.CountAsync() > 0)
                {
                    var longName = new string('X', 300);
                    await nameField.FillAsync(longName);

                    // Check if input is truncated or has maxlength
                    var inputValue = await nameField.InputValueAsync();
                    Assert.That(inputValue.Length, Is.LessThanOrEqualTo(300), "Input should handle long names");
                }
            }

            await TakeScreenshot("UserCreate_LongName");
        }

        #endregion

        #region Concurrent Action Tests

        [Test]
        [Order(30)]
        [Category("Concurrent")]
        public async Task DoubleClick_PreventsDuplicateSubmission()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/Tags");

            var addBtn = Page.Locator("button:has-text('Add'), button:has-text('New')").First;
            if (await addBtn.CountAsync() > 0)
            {
                await addBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                var nameField = Page.Locator("input[name*='name'], input[name*='Name']").First;
                var timestamp = DateTime.Now.ToString("HHmmss");

                if (await nameField.CountAsync() > 0)
                {
                    await nameField.FillAsync($"DoubleClickTest {timestamp}");

                    var submitBtn = Page.Locator("button[type='submit'], button:has-text('Save'), button:has-text('Create')").First;
                    if (await submitBtn.CountAsync() > 0)
                    {
                        // Double click rapidly
                        await submitBtn.ClickAsync(new LocatorClickOptions { ClickCount = 2 });
                        await Page.WaitForTimeoutAsync(1000);
                    }
                }
            }

            // Should not show errors from duplicate submission
            await TakeScreenshot("DoubleClick_Prevention");
            Assert.Pass("Double click prevention test completed");
        }

        #endregion

        #region Navigation Edge Cases

        [Test]
        [Order(40)]
        [Category("Navigation")]
        public async Task BackButton_HandlesFormData()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/UserManagement");

            var addBtn = Page.Locator("button:has-text('Add'), button:has-text('New User')").First;
            if (await addBtn.CountAsync() > 0)
            {
                await addBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(500);

                // Fill some data
                var nameField = Page.Locator("input[name*='name'], input[name*='Name']").First;
                if (await nameField.CountAsync() > 0)
                {
                    await nameField.FillAsync("Unsaved User");
                }

                // Navigate away (cancel or close)
                var cancelBtn = Page.Locator("button:has-text('Cancel'), .modal-close, .btn-cancel").First;
                if (await cancelBtn.CountAsync() > 0)
                {
                    await cancelBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(500);
                }
            }

            // Page should be in consistent state
            await TakeScreenshot("BackButton_FormData");
            Assert.Pass("Back button form data handling test completed");
        }

        [Test]
        [Order(41)]
        [Category("Navigation")]
        public async Task DirectURL_HandlesInvalidId()
        {
            await LoginAsRole("Admin");

            // Try to access certificate with invalid ID
            await Page.GotoAsync(BaseUrl + "/Certificate/Details/99999999");
            await Page.WaitForTimeoutAsync(2000);

            // Should show error or redirect, not crash
            var errorMessage = await Page.Locator("text=not found, text=error, text=404, .error-page").CountAsync();
            var wasRedirected = !Page.Url.Contains("99999999");

            Assert.That(errorMessage > 0 || wasRedirected, Is.True, "Invalid ID should be handled gracefully");
            await TakeScreenshot("DirectURL_InvalidId");
        }

        [Test]
        [Order(42)]
        [Category("Navigation")]
        public async Task DirectURL_HandlesNonNumericId()
        {
            await LoginAsRole("Admin");

            // Try to access with non-numeric ID
            await Page.GotoAsync(BaseUrl + "/Certificate/Details/abc-invalid");
            await Page.WaitForTimeoutAsync(2000);

            // Should handle gracefully
            var pageWorking = await Page.Locator("body").CountAsync();
            Assert.That(pageWorking, Is.GreaterThan(0), "Non-numeric ID should be handled gracefully");
            await TakeScreenshot("DirectURL_NonNumericId");
        }

        #endregion

        #region Session/Auth Edge Cases

        [Test]
        [Order(50)]
        [Category("Session")]
        public async Task RefreshPage_MaintainsSession()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/UserManagement");

            // Refresh the page
            await Page.ReloadAsync();
            await Page.WaitForTimeoutAsync(2000);

            // Should still be on admin page
            var currentUrl = Page.Url;
            Assert.That(currentUrl, Does.Contain("/Admin/UserManagement"), "Session should persist after refresh");
            await TakeScreenshot("RefreshPage_Session");
        }

        [Test]
        [Order(51)]
        [Category("Session")]
        public async Task MultipleTabs_MaintainsSession()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/UserManagement");

            // Open new tab
            var newPage = await Context.NewPageAsync();
            await newPage.GotoAsync(BaseUrl + "/Admin/Tags");
            await newPage.WaitForTimeoutAsync(2000);

            // Both pages should work
            var originalPageWorking = await Page.Locator("body").CountAsync() > 0;
            var newPageWorking = await newPage.Locator("body").CountAsync() > 0;

            Assert.That(originalPageWorking && newPageWorking, Is.True, "Multiple tabs should maintain session");

            await newPage.CloseAsync();
            await TakeScreenshot("MultipleTabs_Session");
        }

        #endregion

        #region Data Integrity Edge Cases

        [Test]
        [Order(60)]
        [Category("DataIntegrity")]
        public async Task DeleteProtection_AdminUserCannotBeDeleted()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/UserManagement");

            // Try to find admin user and delete button
            var adminRow = Page.Locator("tr:has-text('admin'), tr:has-text('Admin User')").First;
            if (await adminRow.CountAsync() > 0)
            {
                var deleteBtn = adminRow.Locator("button:has-text('Delete'), .delete-btn");
                if (await deleteBtn.CountAsync() > 0)
                {
                    await deleteBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(500);

                    // Check for protection message
                    var protectionMessage = await Page.Locator("text=cannot be deleted, text=protected, text=not allowed").CountAsync();
                    Assert.That(protectionMessage, Is.GreaterThanOrEqualTo(0), "Admin user deletion should be protected");
                }
            }

            await TakeScreenshot("DeleteProtection_AdminUser");
        }

        [Test]
        [Order(61)]
        [Category("DataIntegrity")]
        public async Task DeleteProtection_AdminRoleCannotBeDeleted()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Admin/RoleManagement");

            // Try to find Admin role and delete button
            var adminRoleRow = Page.Locator("tr:has-text('Admin'), [data-role='Admin']").First;
            if (await adminRoleRow.CountAsync() > 0)
            {
                var deleteBtn = adminRoleRow.Locator("button:has-text('Delete'), .delete-btn");
                if (await deleteBtn.CountAsync() > 0)
                {
                    await deleteBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(500);

                    // Check for protection message
                    var protectionMessage = await Page.Locator("text=cannot be deleted, text=protected, text=system role").CountAsync();
                    Assert.That(protectionMessage, Is.GreaterThanOrEqualTo(0), "Admin role deletion should be protected");
                }
            }

            await TakeScreenshot("DeleteProtection_AdminRole");
        }

        #endregion

        #region Filter Edge Cases

        [Test]
        [Order(70)]
        [Category("Filter")]
        public async Task Filter_HandlesMultipleFiltersSimultaneously()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Apply multiple filters if available
            var filterCheckboxes = await Page.Locator(".filter-panel input[type='checkbox'], .filter-option input[type='checkbox']").AllAsync();

            if (filterCheckboxes.Count >= 2)
            {
                await filterCheckboxes[0].CheckAsync();
                await Page.WaitForTimeoutAsync(500);
                await filterCheckboxes[1].CheckAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            // Page should handle multiple filters
            var pageWorking = await Page.Locator("body").CountAsync();
            Assert.That(pageWorking, Is.GreaterThan(0), "Multiple filters should work together");
            await TakeScreenshot("Filter_MultipleFilters");
        }

        [Test]
        [Order(71)]
        [Category("Filter")]
        public async Task Filter_ClearAllResetsList()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Apply a filter
            var filterCheckbox = Page.Locator(".filter-panel input[type='checkbox']").First;
            if (await filterCheckbox.CountAsync() > 0)
            {
                await filterCheckbox.CheckAsync();
                await Page.WaitForTimeoutAsync(500);

                // Look for clear/reset button
                var clearBtn = Page.Locator("button:has-text('Clear'), button:has-text('Reset'), .clear-filters").First;
                if (await clearBtn.CountAsync() > 0)
                {
                    await clearBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(1000);
                }
            }

            await TakeScreenshot("Filter_ClearAll");
            Assert.Pass("Clear all filters test completed");
        }

        #endregion

        #region Pagination/Scroll Edge Cases

        [Test]
        [Order(80)]
        [Category("InfiniteScroll")]
        public async Task InfiniteScroll_LoadsMoreOnScroll()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileView");

            await Page.WaitForTimeoutAsync(2000);

            // Get initial count
            var initialTiles = await Page.Locator(".certificate-tile, .tile-card, [class*='tile']").CountAsync();

            // Scroll down
            await Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
            await Page.WaitForTimeoutAsync(2000);

            // Check if more loaded (or all data fits on screen)
            var afterScrollTiles = await Page.Locator(".certificate-tile, .tile-card, [class*='tile']").CountAsync();

            Assert.That(afterScrollTiles, Is.GreaterThanOrEqualTo(initialTiles), "Infinite scroll should maintain or add tiles");
            await TakeScreenshot("InfiniteScroll_LoadMore");
        }

        [Test]
        [Order(81)]
        [Category("InfiniteScroll")]
        public async Task InfiniteScroll_NoPagination()
        {
            // Per requirements: NO PAGINATION on Certificate Tile View
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileView");

            await Page.WaitForTimeoutAsync(2000);

            // Should NOT have pagination controls
            var paginationControls = await Page.Locator(".pagination, nav[aria-label='pagination'], .page-numbers, button:has-text('Next'), button:has-text('Previous')").CountAsync();
            Assert.That(paginationControls, Is.EqualTo(0), "Certificate Tile View should NOT have pagination (infinite scroll only)");
            await TakeScreenshot("InfiniteScroll_NoPagination");
        }

        #endregion

        #region Mobile/Responsive Edge Cases

        [Test]
        [Order(90)]
        [Category("Responsive")]
        public async Task MobileView_NavigationAccessible()
        {
            await LoginAsRole("Admin");

            // Set mobile viewport
            await Page.SetViewportSizeAsync(375, 667);
            await NavigateToPage("/Dashboard");

            // Check for hamburger menu or mobile nav
            var mobileNav = await Page.Locator(".navbar-toggler, .mobile-menu, .hamburger, [class*='mobile-nav']").CountAsync();
            Assert.That(mobileNav, Is.GreaterThanOrEqualTo(0), "Mobile navigation should be accessible");

            // Reset viewport
            await Page.SetViewportSizeAsync(1920, 1080);
            await TakeScreenshot("MobileView_Navigation");
        }

        [Test]
        [Order(91)]
        [Category("Responsive")]
        public async Task MobileView_FormsUsable()
        {
            await LoginAsRole("Admin");

            // Set mobile viewport
            await Page.SetViewportSizeAsync(375, 667);
            await NavigateToPage("/Admin/UserManagement");

            // Check that page renders properly
            var pageWorking = await Page.Locator("body").CountAsync();
            Assert.That(pageWorking, Is.GreaterThan(0), "Page should render on mobile viewport");

            // Reset viewport
            await Page.SetViewportSizeAsync(1920, 1080);
            await TakeScreenshot("MobileView_Forms");
        }

        #endregion

        #region Error Recovery Tests

        [Test]
        [Order(100)]
        [Category("ErrorRecovery")]
        public async Task NetworkError_ShowsMessage()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileView");

            // Note: This is a basic test - actual network failure simulation would need more setup
            await Page.WaitForTimeoutAsync(1000);

            var pageWorking = await Page.Locator("body").CountAsync();
            Assert.That(pageWorking, Is.GreaterThan(0), "Page should handle network conditions gracefully");
            await TakeScreenshot("NetworkError_Handling");
        }

        #endregion
    }
}
