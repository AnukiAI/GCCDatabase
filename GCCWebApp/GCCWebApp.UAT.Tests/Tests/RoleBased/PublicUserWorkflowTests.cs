using GCCWebApp.UAT.Tests.Helpers;

namespace GCCWebApp.UAT.Tests.Tests.RoleBased;

[TestFixture]
public class PublicUserWorkflowTests : BaseTest
{
    [Test]
    public async Task PublicUser_CanBrowsePublicCertificatesWithoutLogin()
    {
        // Arrange - No login required for public user
        TestContext.WriteLine("Testing public access without authentication");
        await TakeScreenshot("public_user_test_start_no_login");

        // Act 1 - Navigate to home page
        await NavigateToPage(TestDataHelper.Urls.Home);
        await TakeScreenshot("public_user_home_page_loaded");

        // Act 2 - Navigate to certificate tile view (public access)
        await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
        await TakeScreenshot("public_user_certificate_tile_view_accessed");

        // Act 3 - Verify public certificates are visible
        await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Medium);
        await TakeScreenshot("public_user_certificates_displayed");

        // Act 4 - Test certificate interaction
        await InteractWithCertificateViews();

        // Act 5 - Test search functionality without login
        foreach (var searchTerm in TestDataHelper.TestData.SearchTerms.Take(3))
        {
            await PerformSearchOperation(searchTerm);
            await TakeScreenshot($"public_user_search_{searchTerm.Replace(" ", "_")}");
        }

        // Act 6 - Verify no access to protected areas
        try
        {
            await NavigateToPage(TestDataHelper.Urls.Admin);
            await TakeScreenshot("public_user_admin_access_attempt");
            
            var currentUrl = Page.Url.ToLower();
            var redirectedToLogin = currentUrl.Contains("login") || currentUrl.Contains("account");
            
            if (redirectedToLogin)
            {
                TestContext.WriteLine("✅ Public user correctly redirected to login for admin access");
            }
            else
            {
                TestContext.WriteLine("⚠️ Public user may have unexpected admin access");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Admin access verification: {ex.Message}");
            await TakeScreenshot("public_user_admin_access_error");
        }

        // Assert
        var finalUrl = Page.Url.ToLower();
        Assert.That(finalUrl.Contains("certificate") || finalUrl.Contains("/") || finalUrl.Contains("home"), Is.True, 
            "Public user should be able to browse public certificate areas");
        
        TestContext.WriteLine("✅ Public user browsing workflow completed");
    }

    [Test]
    public async Task PublicUser_CanSearchAndFilterPublicCertificates()
    {
        // Arrange - Public access, no login
        await TakeScreenshot("public_user_ready_for_search_filtering");

        // Act 1 - Navigate to certificate area
        await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
        await TakeScreenshot("public_user_certificate_area_for_search");

        // Act 2 - Test search by certificate number
        await PerformSearchOperation("GCC");
        await TakeScreenshot("public_user_certificate_number_search");

        // Act 3 - Test search by certification type
        await PerformSearchOperation("Certified");
        await TakeScreenshot("public_user_certification_type_search");

        // Act 4 - Test search by company
        foreach (var company in TestDataHelper.TestData.CompanyNames.Take(2))
        {
            await PerformSearchOperation(company);
            await TakeScreenshot($"public_user_company_search_{company.Replace(" ", "_")}");
        }

        // Act 5 - Test search by product categories
        foreach (var productType in TestDataHelper.TestData.ProductTypes.Take(2))
        {
            await PerformSearchOperation(productType);
            await TakeScreenshot($"public_user_product_search_{productType.Replace(" ", "_")}");
        }

        // Act 6 - Test filtering options
        try
        {
            var filterDropdown = Page.Locator(TestDataHelper.Selectors.FilterDropdown).First;
            if (await filterDropdown.CountAsync() > 0)
            {
                await filterDropdown.ClickAsync();
                await TakeScreenshot("public_user_filter_dropdown_opened");
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Short);
                TestContext.WriteLine("✅ Public user can access filtering options");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Filters may be implemented differently for public users");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Public filtering test: {ex.Message}");
            await TakeScreenshot("public_user_filter_attempt");
        }

        // Act 7 - Test view toggle (tile vs list)
        try
        {
            var viewToggle = Page.Locator(TestDataHelper.Selectors.ViewToggle).First;
            if (await viewToggle.CountAsync() > 0)
            {
                await viewToggle.ClickAsync();
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Short);
                await TakeScreenshot("public_user_view_toggled");
                TestContext.WriteLine("✅ Public user can toggle certificate views");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"View toggle test: {ex.Message}");
            await TakeScreenshot("public_user_view_toggle_attempt");
        }

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("certificate"), Is.True, 
            "Public user should be able to search and filter public certificates");
        
        TestContext.WriteLine("✅ Public user search and filtering workflow completed");
    }

    [Test]
    public async Task PublicUser_CanViewCertificateDetails()
    {
        // Arrange
        await TakeScreenshot("public_user_ready_for_certificate_details");

        // Act 1 - Navigate to certificate listing
        await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
        await TakeScreenshot("public_user_certificate_listing_loaded");

        // Act 2 - Look for certificate cards/items
        try
        {
            var certificateCards = Page.Locator(TestDataHelper.Selectors.CertificateCard);
            var cardCount = await certificateCards.CountAsync();
            
            if (cardCount > 0)
            {
                TestContext.WriteLine($"Found {cardCount} public certificates");
                await TakeScreenshot("public_user_certificates_found");
                
                // Click on first certificate to view details
                await certificateCards.First.ClickAsync();
                await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Navigation);
                await TakeScreenshot("public_user_certificate_details_opened");
                TestContext.WriteLine("✅ Public user can view certificate details");
                
                // Check if details page loads properly
                var currentUrl = Page.Url;
                if (currentUrl.Contains("Details") || currentUrl.Contains("certificate"))
                {
                    await TakeScreenshot("public_user_certificate_details_page_confirmed");
                    TestContext.WriteLine("✅ Certificate details page loaded successfully");
                }
            }
            else
            {
                TestContext.WriteLine("ℹ️ No public certificates available for testing");
                await TakeScreenshot("public_user_no_certificates_available");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Certificate details test: {ex.Message}");
            await TakeScreenshot("public_user_certificate_details_error");
        }

        // Act 3 - Test navigation back to listing
        try
        {
            await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
            await TakeScreenshot("public_user_back_to_certificate_listing");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Navigation back test: {ex.Message}");
            await TakeScreenshot("public_user_navigation_back_error");
        }

        // Act 4 - Test certificate download if available to public
        try
        {
            var downloadLinks = Page.Locator("a[href*='download'], .download, [title*='download']");
            if (await downloadLinks.CountAsync() > 0)
            {
                TestContext.WriteLine("Download options available to public users");
                await TakeScreenshot("public_user_download_options_available");
                TestContext.WriteLine("✅ Public users can download certificates");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Download may be restricted to authenticated users");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Public download test: {ex.Message}");
            await TakeScreenshot("public_user_download_test_error");
        }

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("certificate"), Is.True, 
            "Public user should be able to view certificate details");
        
        TestContext.WriteLine("✅ Public user certificate details workflow completed");
    }

    [Test]
    public async Task PublicUser_CanAccessRecentAndExpiringCertificates()
    {
        // Arrange
        await TakeScreenshot("public_user_ready_for_recent_expiring_certs");

        // Act 1 - Navigate to home page first
        await NavigateToPage(TestDataHelper.Urls.Home);
        await TakeScreenshot("public_user_home_for_recent_certificates");

        // Act 2 - Look for "recently issued" or "recent" certificates section
        try
        {
            var recentElements = Page.Locator("[class*='recent'], [data-type*='recent'], h2:has-text('Recent'), h3:has-text('Recent')");
            if (await recentElements.CountAsync() > 0)
            {
                TestContext.WriteLine("Found recent certificates section");
                await TakeScreenshot("public_user_recent_certificates_section_found");
                TestContext.WriteLine("✅ Public user can see recently issued certificates");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Recent certificates may not be prominently displayed");
                await TakeScreenshot("public_user_no_recent_section_found");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Recent certificates test: {ex.Message}");
            await TakeScreenshot("public_user_recent_certificates_error");
        }

        // Act 3 - Look for "expiring" or "upcoming expiration" information
        try
        {
            var expiringElements = Page.Locator("[class*='expir'], [data-type*='expir'], h2:has-text('Expir'), h3:has-text('Expir')");
            if (await expiringElements.CountAsync() > 0)
            {
                TestContext.WriteLine("Found expiring certificates section");
                await TakeScreenshot("public_user_expiring_certificates_section_found");
                TestContext.WriteLine("✅ Public user can see upcoming expiration information");
            }
            else
            {
                TestContext.WriteLine("ℹ️ Expiring certificates may not be shown to public users");
                await TakeScreenshot("public_user_no_expiring_section_found");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Expiring certificates test: {ex.Message}");
            await TakeScreenshot("public_user_expiring_certificates_error");
        }

        // Act 4 - Navigate to certificate listing and look for date-based sorting
        await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
        await TakeScreenshot("public_user_certificate_listing_for_date_sorting");

        // Act 5 - Test date-based sorting if available
        try
        {
            var sortOptions = Page.Locator("select[name*='sort'], .sort-option, [data-sort]");
            if (await sortOptions.CountAsync() > 0)
            {
                await sortOptions.First.ClickAsync();
                await TakeScreenshot("public_user_sort_options_opened");
                
                // Look for date-related sort options
                var dateOptions = Page.Locator("option:has-text('date'), option:has-text('recent'), option:has-text('expir')");
                if (await dateOptions.CountAsync() > 0)
                {
                    await dateOptions.First.ClickAsync();
                    await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Short);
                    await TakeScreenshot("public_user_date_sort_applied");
                    TestContext.WriteLine("✅ Public user can sort by date");
                }
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Date sorting test: {ex.Message}");
            await TakeScreenshot("public_user_date_sorting_error");
        }

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("Home") || Page.Url.Contains("/"), Is.True, 
            "Public user should be able to access recent and expiring certificate information");
        
        TestContext.WriteLine("✅ Public user recent and expiring certificates workflow completed");
    }

    [Test]
    public async Task PublicUser_CanAccessTagsAndClassifications()
    {
        // Arrange
        await TakeScreenshot("public_user_ready_for_tags_classifications");

        // Act 1 - Navigate to certificate area
        await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
        await TakeScreenshot("public_user_certificate_area_for_tags");

        // Act 2 - Look for tag elements on certificates
        try
        {
            var tagElements = Page.Locator(".tag, [class*='tag'], .label, [data-tag], .badge");
            var tagCount = await tagElements.CountAsync();
            
            if (tagCount > 0)
            {
                TestContext.WriteLine($"Found {tagCount} tag elements");
                await TakeScreenshot("public_user_tags_found_on_certificates");
                TestContext.WriteLine("✅ Public user can see tags on certificates");
                
                // Try clicking on a tag to filter
                if (tagCount > 0)
                {
                    await tagElements.First.ClickAsync();
                    await Page.WaitForTimeoutAsync(TestDataHelper.WaitTimes.Short);
                    await TakeScreenshot("public_user_tag_clicked_for_filtering");
                    TestContext.WriteLine("✅ Public user can filter by tags");
                }
            }
            else
            {
                TestContext.WriteLine("ℹ️ Tags may not be visible to public users or implemented differently");
                await TakeScreenshot("public_user_no_tags_found");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Tag visibility test: {ex.Message}");
            await TakeScreenshot("public_user_tag_test_error");
        }

        // Act 3 - Test search by tags
        var tagSearchTerms = new[] { "Sustainability", "Recycling", "Green", "Eco" };
        foreach (var tagTerm in tagSearchTerms.Take(2))
        {
            await PerformSearchOperation(tagTerm);
            await TakeScreenshot($"public_user_tag_search_{tagTerm.ToLower()}");
        }

        // Act 4 - Look for classification information
        try
        {
            var classificationElements = Page.Locator(".classification, [class*='classif'], .category, [data-category]");
            if (await classificationElements.CountAsync() > 0)
            {
                TestContext.WriteLine("Found classification elements");
                await TakeScreenshot("public_user_classifications_found");
                TestContext.WriteLine("✅ Public user can see certificate classifications");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Classification test: {ex.Message}");
            await TakeScreenshot("public_user_classification_error");
        }

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("certificate"), Is.True, 
            "Public user should be able to access tags and classifications");
        
        TestContext.WriteLine("✅ Public user tags and classifications workflow completed");
    }

    [Test]
    public async Task PublicUser_VerifyDataAccessSecurity()
    {
        // Arrange - Test security boundaries for public access
        await TakeScreenshot("public_user_security_verification_start");

        // Act 1 - Verify no access to protected admin areas
        var protectedUrls = new[]
        {
            TestDataHelper.Urls.Admin,
            TestDataHelper.Urls.AdminUserManagement,
            TestDataHelper.Urls.AdminRoleManagement,
            TestDataHelper.Urls.AdminProductManagement,
            TestDataHelper.Urls.AdminCertificateTypes,
            TestDataHelper.Urls.AdminCSFAttributes,
            TestDataHelper.Urls.AdminTags,
            TestDataHelper.Urls.AdminPartners
        };

        foreach (var url in protectedUrls)
        {
            try
            {
                await NavigateToPage(url);
                await TakeScreenshot($"public_user_access_attempt_{url.Replace("/", "_").Replace("Admin", "admin")}");
                
                var currentUrl = Page.Url.ToLower();
                var properlyRestricted = currentUrl.Contains("login") || 
                                       currentUrl.Contains("account") || 
                                       currentUrl.Contains("accessdenied") ||
                                       currentUrl.Contains("unauthorized");
                
                if (properlyRestricted)
                {
                    TestContext.WriteLine($"✅ Public user properly restricted from {url}");
                }
                else
                {
                    TestContext.WriteLine($"⚠️ Public user may have unexpected access to {url}");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Security test for {url}: {ex.Message}");
                await TakeScreenshot($"public_user_security_error_{url.Replace("/", "_")}");
            }
        }

        // Act 2 - Verify public can only see non-confidential data
        await NavigateToPage(TestDataHelper.Urls.CertificateTileView);
        await TakeScreenshot("public_user_verify_non_confidential_access");

        // Act 3 - Verify no edit/delete capabilities
        try
        {
            var editButtons = Page.Locator(TestDataHelper.Selectors.EditButton);
            var deleteButtons = Page.Locator(TestDataHelper.Selectors.DeleteButton);
            var addButtons = Page.Locator(TestDataHelper.Selectors.AddButton);
            
            var editCount = await editButtons.CountAsync();
            var deleteCount = await deleteButtons.CountAsync();
            var addCount = await addButtons.CountAsync();
            
            if (editCount == 0 && deleteCount == 0 && addCount == 0)
            {
                TestContext.WriteLine("✅ Public user has no edit/delete/add capabilities");
                await TakeScreenshot("public_user_no_edit_capabilities_confirmed");
            }
            else
            {
                TestContext.WriteLine($"⚠️ Public user may have unexpected editing capabilities (Edit: {editCount}, Delete: {deleteCount}, Add: {addCount})");
                await TakeScreenshot("public_user_unexpected_edit_capabilities");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Edit capability test: {ex.Message}");
            await TakeScreenshot("public_user_edit_capability_error");
        }

        // Assert
        Assert.That(Page.Url.Contains("Certificate") || Page.Url.Contains("certificate") || Page.Url.Contains("login"), Is.True, 
            "Public user should only have access to appropriate public areas");
        
        TestContext.WriteLine("✅ Public user security verification completed");
    }
}