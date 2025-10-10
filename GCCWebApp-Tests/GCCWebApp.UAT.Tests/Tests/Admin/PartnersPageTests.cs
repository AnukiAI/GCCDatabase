using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Admin;

[TestFixture]
public class PartnersPageTests : BaseTest
{
    [Test]
    public async Task Admin_CanViewPartnersPage()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Partners");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("partners_page_loaded");
        
        var hasPartnersTable = await Page.IsVisibleAsync("#partners-table, .partners-table, table");
        var hasAddButton = await Page.IsVisibleAsync("#partners-add-btn, button:has-text('Add'), .btn-add");
        var hasExportButton = await Page.IsVisibleAsync("#partners-export-btn, button:has-text('Export'), .btn-export");
        
        Assert.That(hasPartnersTable, Is.True, "Partners page should display partners table");
        Assert.That(hasAddButton, Is.True, "Partners page should have add button");
        Assert.That(hasExportButton, Is.True, "Partners page should have export button");
    }

    [Test]
    public async Task Admin_CanSearchPartners()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Partners");
        
        await Page.WaitForSelectorAsync("body");
        
        var searchInput = await Page.QuerySelectorAsync("#partners-search-input, input[type='search'], input[placeholder*='search']");
        if (searchInput != null)
        {
            await searchInput.FillAsync("Amazon");
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("partners_search_results");
        }
        
        Assert.That(searchInput, Is.Not.Null, "Partners page should have search functionality");
    }

    [Test]
    public async Task Admin_CanAddNewPartner()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/Partners");
        
        await Page.WaitForSelectorAsync("body");
        
        var addButton = await Page.QuerySelectorAsync("#partners-add-btn, button:has-text('Add'), .btn-add");
        if (addButton != null)
        {
            await addButton.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);
            await TakeScreenshot("partners_add_form");
            
            var nameInput = await Page.QuerySelectorAsync("#new-partner-name, input[name='name'], input[placeholder*='name']");
            if (nameInput != null)
            {
                await nameInput.FillAsync("Test Partner UAT");
                await TakeScreenshot("partners_add_form_filled");
            }
        }
        
        Assert.That(addButton, Is.Not.Null, "Partners page should have add partner functionality");
    }

    [Test]
    public async Task NonAdmin_CannotAccessPartnersPage()
    {
        await LoginAsRole("User");
        await NavigateToPage("/Admin/Partners");
        
        await Page.WaitForSelectorAsync("body");
        await TakeScreenshot("partners_access_denied");
        
        var currentUrl = Page.Url;
        var isOnLoginPage = currentUrl.Contains("/Account/Login") || currentUrl.Contains("login");
        var hasAccessDenied = await Page.IsVisibleAsync("text=Access Denied, text=Forbidden, text=401, text=403");
        
        Assert.That(isOnLoginPage || hasAccessDenied, Is.True, "Non-admin users should not access partners page");
    }
}