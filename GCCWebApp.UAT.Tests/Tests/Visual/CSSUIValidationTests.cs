using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Visual;

/// <summary>
/// CSS and UI validation tests — the "drywall" the client sees.
/// Tests font sizes, colors, spacing, button styles, modals, toasts,
/// and visual consistency across all major pages.
/// </summary>
[TestFixture]
public class CSSUIValidationTests : BaseTest
{
    // Device viewports for cross-device testing
    private static readonly (string Name, int Width, int Height)[] Devices = new[]
    {
        ("Desktop 1080p", 1920, 1080),
        ("Laptop", 1366, 768),
        ("iPad", 768, 1024),
        ("iPhone 12", 390, 844),
        ("iPhone SE", 375, 667),
        ("Samsung Galaxy S20", 360, 800),
        ("Desktop 4K", 2560, 1440)
    };

    #region Font Size Tests (16px minimum per requirements)

    [Test]
    public async Task AllPages_FontSize_MinimumIs16px()
    {
        var pages = new[]
        {
            ("/", "Home"),
            ("/Account/Login", "Login"),
            ("/Certificate/CertificateTileView", "Tile View")
        };

        foreach (var (path, name) in pages)
        {
            await NavigateToPage(path);

            var smallFonts = await Page.EvaluateAsync<int>(@"() => {
                let count = 0;
                const elements = document.querySelectorAll('p, span, a, button, input, select, label, td, th, li, h1, h2, h3, h4, h5, h6');
                for (const el of elements) {
                    const style = window.getComputedStyle(el);
                    const fontSize = parseFloat(style.fontSize);
                    if (el.offsetWidth > 0 && el.offsetHeight > 0 && fontSize < 16) count++;
                }
                return count;
            }");

            await TakeScreenshot($"font_check_{name}");
            Assert.That(smallFonts, Is.EqualTo(0),
                $"Page '{name}' has {smallFonts} elements with font-size below 16px minimum");
        }
    }

    [Test]
    public async Task AuthenticatedPages_FontSize_MinimumIs16px()
    {
        await LoginAsRole("Admin");

        var pages = new[]
        {
            ("/Admin", "Admin Dashboard"),
            ("/Admin/UserManagement", "User Management"),
            ("/Admin/RoleManagement", "Role Management"),
            ("/Certificate", "Certificates"),
            ("/Product/Management", "Product Management"),
            ("/Company", "Companies"),
            ("/Dashboard", "Dashboard")
        };

        foreach (var (path, name) in pages)
        {
            await NavigateToPage(path);

            var smallFonts = await Page.EvaluateAsync<int>(@"() => {
                let count = 0;
                const elements = document.querySelectorAll('p, span, a, button, input, select, label, td, th, li');
                for (const el of elements) {
                    const style = window.getComputedStyle(el);
                    const fontSize = parseFloat(style.fontSize);
                    if (el.offsetWidth > 0 && el.offsetHeight > 0 && fontSize < 16) count++;
                }
                return count;
            }");

            Assert.That(smallFonts, Is.EqualTo(0),
                $"Page '{name}' has {smallFonts} elements with font-size below 16px minimum");
        }
    }

    #endregion

    #region Color Contrast Tests

    [Test]
    public async Task AllPages_ColorContrast_MeetsWCAGAA()
    {
        var pages = new[] { "/", "/Account/Login", "/Certificate/CertificateTileView" };

        foreach (var path in pages)
        {
            await NavigateToPage(path);

            // Check that text elements have sufficient contrast (4.5:1 for normal text)
            var lowContrastCount = await Page.EvaluateAsync<int>(@"() => {
                function getLuminance(r, g, b) {
                    const a = [r, g, b].map(v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); });
                    return 0.2126 * a[0] + 0.7152 * a[1] + 0.0722 * a[2];
                }
                function parseColor(color) {
                    const m = color.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
                    return m ? [parseInt(m[1]), parseInt(m[2]), parseInt(m[3])] : null;
                }
                let count = 0;
                const els = document.querySelectorAll('p, span, a, button, label, h1, h2, h3, h4, h5, h6, td, th, li');
                for (const el of els) {
                    if (el.offsetWidth === 0 || el.offsetHeight === 0) continue;
                    const style = window.getComputedStyle(el);
                    const fg = parseColor(style.color);
                    const bg = parseColor(style.backgroundColor);
                    if (fg && bg && bg[0] + bg[1] + bg[2] > 0) {
                        const l1 = getLuminance(...fg);
                        const l2 = getLuminance(...bg);
                        const ratio = (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
                        if (ratio < 4.5) count++;
                    }
                }
                return count;
            }");

            await TakeScreenshot($"contrast_check_{path.Replace("/", "_")}");
            Assert.That(lowContrastCount, Is.LessThanOrEqualTo(5),
                $"Page '{path}' has {lowContrastCount} elements with contrast ratio below 4.5:1");
        }
    }

    #endregion

    #region Button Style Consistency

    [Test]
    public async Task AdminPages_Buttons_ConsistentStyling()
    {
        await LoginAsRole("Admin");

        var pages = new[]
        {
            "/Admin/UserManagement",
            "/Admin/RoleManagement",
            "/Admin/ProductTypes",
            "/Admin/CertificateTypes",
            "/Admin/Tags",
            "/Admin/Partners"
        };

        foreach (var path in pages)
        {
            await NavigateToPage(path);

            // Check primary action buttons have consistent styling
            var buttonStyles = await Page.EvaluateAsync<string[]>(@"() => {
                const buttons = document.querySelectorAll('.admin-btn--primary, .btn-primary, button[type=""submit""]');
                return Array.from(buttons).filter(b => b.offsetWidth > 0).map(b => {
                    const s = window.getComputedStyle(b);
                    return `bg:${s.backgroundColor}|color:${s.color}|radius:${s.borderRadius}|font:${s.fontSize}`;
                });
            }");

            if (buttonStyles.Length > 1)
            {
                // All primary buttons should have same background color
                var bgColors = buttonStyles.Select(s => s.Split('|')[0]).Distinct().ToArray();
                Assert.That(bgColors.Length, Is.LessThanOrEqualTo(2),
                    $"Page '{path}' has inconsistent primary button colors: {string.Join(", ", bgColors)}");
            }

            await TakeScreenshot($"button_styles_{path.Replace("/", "_")}");
        }
    }

    #endregion

    #region Layout and Container Tests

    [Test]
    public async Task AdminPages_UseAdminContainer()
    {
        await LoginAsRole("Admin");

        var pages = new[]
        {
            "/Admin/UserManagement",
            "/Admin/RoleManagement",
            "/Admin/ProductTypes",
            "/Admin/CertificateTypes",
            "/Admin/Tags"
        };

        foreach (var path in pages)
        {
            await NavigateToPage(path);

            var hasAdminContainer = await Page.Locator(".admin-container").CountAsync() > 0;
            Assert.That(hasAdminContainer, Is.True,
                $"Page '{path}' should use .admin-container layout");

            // Check no inline max-width overrides (per requirements)
            var inlineMaxWidth = await Page.EvaluateAsync<int>(@"() => {
                return document.querySelectorAll('[style*=""max-width""]').length;
            }");

            Assert.That(inlineMaxWidth, Is.EqualTo(0),
                $"Page '{path}' has {inlineMaxWidth} elements with inline max-width overrides");
        }
    }

    [Test]
    public async Task AllPages_NoHorizontalOverflow()
    {
        foreach (var (name, width, height) in Devices)
        {
            await Page.SetViewportSizeAsync(width, height);
            await NavigateToPage("/Certificate/CertificateTileView");

            var hasOverflow = await Page.EvaluateAsync<bool>(@"() => {
                return document.documentElement.scrollWidth > document.documentElement.clientWidth + 5;
            }");

            await TakeScreenshot($"overflow_check_{name.Replace(" ", "_")}");
            Assert.That(hasOverflow, Is.False,
                $"Tile View has horizontal overflow on {name} ({width}x{height})");
        }

        // Reset viewport
        await Page.SetViewportSizeAsync(1920, 1080);
    }

    #endregion

    #region Page Title Tests (ViewData["Title"] requirement)

    [Test]
    public async Task AllPages_HaveCorrectPageTitle()
    {
        await LoginAsRole("Admin");

        var pages = new[]
        {
            ("/Admin/UserManagement", "User Management"),
            ("/Admin/RoleManagement", "Role Management"),
            ("/Admin/ProductTypes", "Product Type"),
            ("/Admin/CertificateTypes", "Certificate Type"),
            ("/Admin/Tags", "Tag"),
            ("/Admin/Partners", "Partner"),
            ("/Product/Management", "Product"),
            ("/Company", "Compan"),
            ("/Dashboard", "Dashboard")
        };

        foreach (var (path, expectedTitle) in pages)
        {
            await NavigateToPage(path);

            var pageTitle = await Page.Locator("h1.page-title").TextContentAsync();
            Assert.That(pageTitle, Does.Contain(expectedTitle).IgnoreCase,
                $"Page '{path}' title should contain '{expectedTitle}', got '{pageTitle}'");
        }
    }

    [Test]
    public async Task AllPages_NoMultipleH1Tags()
    {
        await LoginAsRole("Admin");

        var pages = new[]
        {
            "/Admin/UserManagement", "/Admin/RoleManagement",
            "/Admin/ProductTypes", "/Certificate",
            "/Product/Management", "/Company", "/Dashboard"
        };

        foreach (var path in pages)
        {
            await NavigateToPage(path);

            var h1Count = await Page.Locator("h1").CountAsync();
            Assert.That(h1Count, Is.LessThanOrEqualTo(1),
                $"Page '{path}' has {h1Count} <h1> tags — should have at most 1 (set via ViewData[\"Title\"])");
        }
    }

    #endregion

    #region Modal and Toast Tests

    [Test]
    public async Task AdminPages_DeleteConfirmation_UsesModal()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        // Look for delete buttons
        var deleteButtons = Page.Locator(".admin-action-delete, button:has-text('Delete'), [data-action='delete']");
        var count = await deleteButtons.CountAsync();

        if (count > 0)
        {
            // Click first delete button
            await deleteButtons.First.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Should show a modal confirmation, NOT a native alert/confirm
            var modalVisible = await Page.Locator(".modal.show, [role='dialog']:visible, .admin-modal").CountAsync() > 0;

            await TakeScreenshot("delete_confirmation_modal");
            Assert.That(modalVisible, Is.True,
                "Delete confirmation should use a modal, not native alert()/confirm()");
        }
    }

    [Test]
    public async Task NoNativeAlerts_OnAnyAction()
    {
        // Monitor for native dialogs — should never appear
        var alertTriggered = false;
        Page.Dialog += (_, dialog) =>
        {
            alertTriggered = true;
            dialog.DismissAsync();
        };

        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");

        // Interact with common UI elements
        var buttons = Page.Locator("button:visible");
        var buttonCount = await buttons.CountAsync();

        for (int i = 0; i < Math.Min(buttonCount, 5); i++)
        {
            try
            {
                await buttons.Nth(i).ClickAsync(new() { Timeout = 2000 });
                await Page.WaitForTimeoutAsync(300);
            }
            catch { /* Button may navigate away */ }
        }

        Assert.That(alertTriggered, Is.False,
            "Native alert()/confirm()/prompt() should never be used — use AdminCore.showConfirmation() and AdminCore.showNotification() instead");
    }

    #endregion

    #region Cross-Device Visual Tests

    [Test]
    [TestCaseSource(nameof(GetDevices))]
    public async Task HomePage_RendersCorrectly_OnAllDevices((string Name, int Width, int Height) device)
    {
        await Page.SetViewportSizeAsync(device.Width, device.Height);
        await NavigateToPage("/");

        // Logo should be visible and reasonably sized
        var logo = Page.Locator(".logo, .navbar-brand, .site-logo").First;
        if (await logo.CountAsync() > 0)
        {
            var box = await logo.BoundingBoxAsync();
            Assert.That(box, Is.Not.Null, $"Logo should be visible on {device.Name}");

            if (device.Width <= 768)
            {
                Assert.That(box!.Width, Is.LessThanOrEqualTo(200),
                    $"Logo should be reduced on mobile ({device.Name})");
            }
        }

        // No horizontal overflow
        var hasOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 5");
        Assert.That(hasOverflow, Is.False, $"No horizontal overflow on {device.Name}");

        await TakeScreenshot($"home_{device.Name.Replace(" ", "_")}");
    }

    [Test]
    [TestCaseSource(nameof(GetDevices))]
    public async Task LoginPage_RendersCorrectly_OnAllDevices((string Name, int Width, int Height) device)
    {
        await Page.SetViewportSizeAsync(device.Width, device.Height);
        await NavigateToPage("/Account/Login");

        // Login form should be visible
        var emailField = Page.Locator("#Email");
        await Expect(emailField).ToBeVisibleAsync();

        var passwordField = Page.Locator("#Password");
        await Expect(passwordField).ToBeVisibleAsync();

        var submitButton = Page.Locator("button[type='submit']");
        await Expect(submitButton).ToBeVisibleAsync();

        // Form should fit within viewport
        var formBox = await Page.Locator("form").First.BoundingBoxAsync();
        if (formBox != null)
        {
            Assert.That(formBox.Width, Is.LessThanOrEqualTo(device.Width),
                $"Login form wider than viewport on {device.Name}");
        }

        await TakeScreenshot($"login_{device.Name.Replace(" ", "_")}");
    }

    [Test]
    [TestCaseSource(nameof(GetDevices))]
    public async Task CertificateTileView_RendersCorrectly_OnAllDevices((string Name, int Width, int Height) device)
    {
        await Page.SetViewportSizeAsync(device.Width, device.Height);
        await NavigateToPage("/Certificate/CertificateTileView");

        // Tiles should be visible
        var tiles = Page.Locator(".certificate-card, .cert-tile");
        var tileCount = await tiles.CountAsync();

        if (tileCount > 0)
        {
            // On mobile, tiles should stack (single column)
            if (device.Width <= 768 && tileCount >= 2)
            {
                var first = await tiles.Nth(0).BoundingBoxAsync();
                var second = await tiles.Nth(1).BoundingBoxAsync();

                if (first != null && second != null)
                {
                    Assert.That(second.Y, Is.GreaterThan(first.Y),
                        $"Tiles should stack vertically on {device.Name}");
                }
            }
        }

        // No horizontal overflow
        var hasOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 5");
        Assert.That(hasOverflow, Is.False, $"No horizontal overflow on {device.Name}");

        await TakeScreenshot($"tiles_{device.Name.Replace(" ", "_")}");
    }

    #endregion

    #region Navigation Consistency

    [Test]
    public async Task MobileNavigation_HamburgerMenu_WorksCorrectly()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToPage("/");

        // Hamburger toggle should be visible on mobile
        var mobileToggle = Page.Locator("#mobile-toggle, .mobile-toggle, .navbar-toggler");
        var toggleVisible = await mobileToggle.CountAsync() > 0 && await mobileToggle.First.IsVisibleAsync();

        Assert.That(toggleVisible, Is.True, "Mobile hamburger menu should be visible on phone viewport");

        // Click to open
        await mobileToggle.First.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Nav links should now be visible
        var navLinks = Page.Locator(".nav-links a, .navbar-nav .nav-link");
        var visibleLinks = 0;
        var linkCount = await navLinks.CountAsync();
        for (int i = 0; i < linkCount; i++)
        {
            if (await navLinks.Nth(i).IsVisibleAsync()) visibleLinks++;
        }

        Assert.That(visibleLinks, Is.GreaterThan(0),
            "Navigation links should be visible after opening hamburger menu");

        await TakeScreenshot("mobile_nav_opened");

        // Reset viewport
        await Page.SetViewportSizeAsync(1920, 1080);
    }

    [Test]
    public async Task BreadcrumbLinks_AllFunctional()
    {
        await LoginAsRole("Admin");

        var pages = new[]
        {
            "/Admin/UserManagement",
            "/Admin/RoleManagement",
            "/Admin/ProductTypes",
            "/Admin/CertificateTypes"
        };

        foreach (var path in pages)
        {
            await NavigateToPage(path);

            var breadcrumbs = Page.Locator(".breadcrumb a, .breadcrumbs a");
            var count = await breadcrumbs.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var href = await breadcrumbs.Nth(i).GetAttributeAsync("href");
                Assert.That(href, Is.Not.EqualTo("#"),
                    $"Breadcrumb link on '{path}' should not be href='#'");
            }
        }
    }

    #endregion

    #region External User Restrictions

    [Test]
    public async Task ExternalUser_NoAddEditButtons_OnCertificates()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileViewExternal");

        var addButtons = Page.Locator("button:has-text('Add'), a:has-text('Add'), button:has-text('Create')");
        var editButtons = Page.Locator("button:has-text('Edit'), a:has-text('Edit')");

        var addCount = await addButtons.CountAsync();
        var editCount = await editButtons.CountAsync();

        Assert.That(addCount, Is.EqualTo(0),
            "External users should NOT see Add/Create buttons on certificates");
        Assert.That(editCount, Is.EqualTo(0),
            "External users should NOT see Edit buttons on certificates");

        await TakeScreenshot("external_no_add_edit");
    }

    [Test]
    public async Task ExternalUser_NoActionsColumn_OnProductManagement()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Product/Management");

        var actionsHeader = Page.Locator("th:has-text('Actions')");
        var actionsVisible = await actionsHeader.CountAsync() > 0 && await actionsHeader.First.IsVisibleAsync();

        Assert.That(actionsVisible, Is.False,
            "External users should NOT see Actions column on Product Management");

        await TakeScreenshot("external_no_actions_column");
    }

    [Test]
    public async Task ExternalUser_CannotViewInternalTags()
    {
        await LoginAsRole("External");
        await NavigateToPage("/Certificate/CertificateTileViewExternal");

        // Internal tags should not be visible to external users
        var internalTags = Page.Locator(".internal-tag, .tag-internal, [data-tag-type='internal']");
        var count = await internalTags.CountAsync();

        Assert.That(count, Is.EqualTo(0),
            "External users should NOT see internal tags");
    }

    #endregion

    #region Touch Target Tests

    [Test]
    public async Task MobilePages_TouchTargets_MinimumSize44px()
    {
        await Page.SetViewportSizeAsync(375, 667);

        var pages = new[] { "/", "/Account/Login", "/Certificate/CertificateTileView" };

        foreach (var path in pages)
        {
            await NavigateToPage(path);

            var smallTargets = await Page.EvaluateAsync<int>(@"() => {
                let count = 0;
                const elements = document.querySelectorAll('a, button, input, select, [role=""button""]');
                for (const el of elements) {
                    const rect = el.getBoundingClientRect();
                    if (rect.width > 0 && rect.height > 0 && (rect.width < 44 || rect.height < 44)) count++;
                }
                return count;
            }");

            // Allow some small targets but flag if too many
            Assert.That(smallTargets, Is.LessThan(10),
                $"Page '{path}' has {smallTargets} touch targets smaller than 44x44px on mobile");
        }

        await Page.SetViewportSizeAsync(1920, 1080);
    }

    #endregion

    #region Test Data Sources

    private static IEnumerable<(string Name, int Width, int Height)> GetDevices()
    {
        return Devices;
    }

    #endregion
}
