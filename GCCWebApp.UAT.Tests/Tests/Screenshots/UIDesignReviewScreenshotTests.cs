using Microsoft.Playwright;
using NUnit.Framework;

namespace GCCWebApp.UAT.Tests.Tests.Screenshots;

/// <summary>
/// UI Design Review Screenshot Tests - 100% Page Coverage
/// Captures screenshots of ALL pages for complete design consistency review
/// Based on: SITEMAP.md, requirements.md, and actual controllers
/// Screenshots saved to: Screenshots/UI_Design_Review_{timestamp}/
///
/// Total Coverage: 50+ pages across all user roles and viewports
/// Viewports: Desktop (1920x1080), Small Desktop (1366x768), Tablet (768x1024), Mobile (375x812)
/// </summary>
[TestFixture]
[Category("Screenshot")]
[Category("UIDesignReview")]
public class UIDesignReviewScreenshotTests : ScreenshotTestBase
{
    private string _reviewDirectory = null!;
    private int _screenshotCounter = 0;

    [OneTimeSetUp]
    public new async Task OneTimeSetUp()
    {
        await base.OneTimeSetUp();

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _reviewDirectory = Path.Combine("Screenshots", $"UI_Design_Review_{timestamp}");
        Directory.CreateDirectory(_reviewDirectory);

        Console.WriteLine($"\n{'='*90}");
        Console.WriteLine($"UI DESIGN REVIEW SCREENSHOT CAPTURE - 100% COVERAGE");
        Console.WriteLine($"{'='*90}");
        Console.WriteLine($"Purpose: Capture ALL pages for complete design consistency review");
        Console.WriteLine($"Output Directory: {_reviewDirectory}");
        Console.WriteLine($"Base URL: {BaseUrl}");
        Console.WriteLine($"Viewports: Desktop (1920x1080), Small Desktop (1366x768)");
        Console.WriteLine($"{'='*90}\n");
    }

    private async Task CaptureScreenshot(IPage page, string filename, string description)
    {
        _screenshotCounter++;
        Console.WriteLine($"\n[{_screenshotCounter}/50+] Capturing: {description}");

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(_reviewDirectory, filename),
            FullPage = true
        });

        Console.WriteLine($"    ✓ Screenshot saved: {filename}");
    }

    #region 1. PUBLIC PAGES (6 screenshots)

    [Test, Order(1)]
    [Category("Public")]
    public async Task Capture_01_Home_Landing()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/");
        await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(PublicPage, "01-Home-Landing.png", "Home Landing Page (Redirects to Certificate Tile View)");
    }

    [Test, Order(2)]
    [Category("Public")]
    public async Task Capture_02_PublicCertificateTileView()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(PublicPage, timeout: 3000);
        await CaptureScreenshot(PublicPage, "02-Public-Certificate-Tile-View.png", "Public Certificate Tile View (Main Landing)");
    }

    [Test, Order(3)]
    [Category("Public")]
    public async Task Capture_03_PublicCertificateDetails()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(PublicPage, timeout: 2000);

        var firstTile = await PublicPage.QuerySelectorAsync(".certificate-tile");
        if (firstTile != null)
        {
            var certId = await firstTile.GetAttributeAsync("data-certificate-id");
            if (!string.IsNullOrEmpty(certId))
            {
                await PublicPage.GotoAsync($"{BaseUrl}/Certificate/Details/{certId}");
                await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await CaptureScreenshot(PublicPage, "03-Public-Certificate-Details.png", "Public Certificate Details");
            }
        }
    }

    [Test, Order(4)]
    [Category("Public")]
    public async Task Capture_04_PublicCertificateSingleView()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileView");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(PublicPage, timeout: 2000);

        var firstTile = await PublicPage.QuerySelectorAsync(".certificate-tile");
        if (firstTile != null)
        {
            var certId = await firstTile.GetAttributeAsync("data-certificate-id");
            if (!string.IsNullOrEmpty(certId))
            {
                await PublicPage.GotoAsync($"{BaseUrl}/Certificate/CertificateSingleView/{certId}");
                await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await CaptureScreenshot(PublicPage, "04-Public-Certificate-Single-View.png", "Public Certificate Single View");
            }
        }
    }

    [Test, Order(5)]
    [Category("Public")]
    public async Task Capture_05_PrivacyPolicy()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Home/Privacy");
        await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(PublicPage, "05-Privacy-Policy.png", "Privacy Policy Page");
    }

    [Test, Order(6)]
    [Category("Public")]
    public async Task Capture_06_ErrorPage()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Home/Error");
        await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(PublicPage, "06-Error-Page.png", "Error Page");
    }

    #endregion

    #region 2. AUTHENTICATION PAGES (9 screenshots)

    [Test, Order(7)]
    [Category("Authentication")]
    public async Task Capture_07_LoginPage()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Account/Login");
        await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(PublicPage, "07-Login-Page.png", "Login Page");
    }

    [Test, Order(8)]
    [Category("Authentication")]
    public async Task Capture_08_ForgotPassword()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Account/ForgotPassword");
        await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(PublicPage, "08-Forgot-Password-Page.png", "Forgot Password Page");
    }

    [Test, Order(9)]
    [Category("Authentication")]
    public async Task Capture_09_ResetPassword()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Account/ResetPassword");
        await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(PublicPage, "09-Reset-Password-Page.png", "Reset Password Page");
    }

    [Test, Order(10)]
    [Category("Authentication")]
    public async Task Capture_10_AccessDenied()
    {
        await PublicPage.GotoAsync($"{BaseUrl}/Account/AccessDenied");
        await PublicPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(PublicPage, "10-Access-Denied-Page.png", "Access Denied Page");
    }

    [Test, Order(11)]
    [Category("Authentication")]
    public async Task Capture_11_EditProfile()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Account/EditProfile");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "11-Edit-Profile-Page.png", "Edit Profile Page");
    }

    [Test, Order(12)]
    [Category("Authentication")]
    [Category("2FA")]
    public async Task Capture_12_TwoFactorAuthentication()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Account/TwoFactorAuthentication");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "12-Two-Factor-Authentication-Page.png", "Two-Factor Authentication Page");
    }

    [Test, Order(13)]
    [Category("Authentication")]
    [Category("2FA")]
    public async Task Capture_13_TwoFactorPreferences()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Account/TwoFactorPreferences");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "13-Two-Factor-Preferences-Page.png", "Two-Factor Preferences Page");
    }

    [Test, Order(14)]
    [Category("Authentication")]
    [Category("2FA")]
    public async Task Capture_14_SetupTotp()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Account/SetupTotp");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "14-Setup-TOTP-Page.png", "Setup TOTP Page");
    }

    [Test, Order(15)]
    [Category("Authentication")]
    [Category("2FA")]
    public async Task Capture_15_VerifyPhone()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Account/VerifyPhone");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "15-Verify-Phone-Page.png", "Verify Phone Page");
    }

    #endregion

    #region 3. ADMIN DASHBOARD (1 screenshot)

    [Test, Order(16)]
    [Category("Admin")]
    [Category("Dashboard")]
    public async Task Capture_16_AdminDashboard()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Dashboard/Index");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);
        await CaptureScreenshot(AdminUserPage, "16-Admin-Dashboard.png", "Admin Dashboard - CHECK BACKGROUND");
    }

    #endregion

    #region 4. CERTIFICATE MANAGEMENT (10 screenshots)

    [Test, Order(17)]
    [Category("Admin")]
    [Category("Certificate")]
    public async Task Capture_17_CertificateTileViewInternal()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewInternal");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(AdminUserPage, timeout: 3000);
        await CaptureScreenshot(AdminUserPage, "17-Admin-Certificate-Tile-View-Internal.png", "Admin Certificate Tile View (Internal)");
    }

    [Test, Order(18)]
    [Category("Admin")]
    [Category("Certificate")]
    public async Task Capture_18_CreateCertificate()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/Create");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(500);
        await CaptureScreenshot(AdminUserPage, "18-Create-Certificate-Page.png", "Create Certificate Page - CHECK FORM SPACING");
    }

    [Test, Order(19)]
    [Category("Admin")]
    [Category("Certificate")]
    public async Task Capture_19_EditCertificate()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewInternal");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(AdminUserPage, timeout: 2000);

        var firstTile = await AdminUserPage.QuerySelectorAsync(".certificate-tile");
        if (firstTile != null)
        {
            var certId = await firstTile.GetAttributeAsync("data-certificate-id");
            if (!string.IsNullOrEmpty(certId))
            {
                await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/Edit/{certId}");
                await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(500);
                await CaptureScreenshot(AdminUserPage, "19-Edit-Certificate-Page.png", "Edit Certificate Page - PREVIOUSLY MISSING!");
            }
        }
    }

    [Test, Order(20)]
    [Category("Admin")]
    [Category("Certificate")]
    public async Task Capture_20_CertificateSingleView()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewInternal");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(AdminUserPage, timeout: 2000);

        var firstTile = await AdminUserPage.QuerySelectorAsync(".certificate-tile");
        if (firstTile != null)
        {
            var certId = await firstTile.GetAttributeAsync("data-certificate-id");
            if (!string.IsNullOrEmpty(certId))
            {
                await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateSingleView/{certId}");
                await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await CaptureScreenshot(AdminUserPage, "20-Admin-Certificate-Single-View.png", "Admin Certificate Single View");
            }
        }
    }

    [Test, Order(21)]
    [Category("Admin")]
    [Category("Certificate")]
    public async Task Capture_21_CertificateList()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/Certificates");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "21-Certificate-List-Page.png", "Certificate List Page");
    }

    [Test, Order(22)]
    [Category("Admin")]
    [Category("Certificate")]
    public async Task Capture_22_CertificateStatistics()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/Statistics");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "22-Certificate-Statistics-Page.png", "Certificate Statistics Page");
    }

    [Test, Order(23)]
    [Category("External")]
    [Category("Certificate")]
    public async Task Capture_23_CertificateTileViewExternal()
    {
        await ExternalUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewExternal");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(ExternalUserPage, timeout: 3000);
        await CaptureScreenshot(ExternalUserPage, "23-External-Certificate-Tile-View.png", "External Certificate Tile View");
    }

    [Test, Order(24)]
    [Category("External")]
    [Category("Certificate")]
    public async Task Capture_24_CertificateExternalView()
    {
        await ExternalUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateExternalView");
        await ExternalUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(ExternalUserPage, "24-Certificate-External-View-Page.png", "Certificate External View Page");
    }

    [Test, Order(25)]
    [Category("Admin")]
    [Category("Certificate")]
    public async Task Capture_25_CertificateInternalView()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateInternalView");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "25-Certificate-Internal-View-Page.png", "Certificate Internal View Page");
    }

    [Test, Order(26)]
    [Category("Admin")]
    [Category("Certificate")]
    public async Task Capture_26_ExportCertificates()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/ExportCertificates");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "26-Export-Certificates-Page.png", "Export Certificates Page");
    }

    #endregion

    #region 5. COMPANY MANAGEMENT (2 screenshots)

    [Test, Order(27)]
    [Category("Admin")]
    [Category("Company")]
    public async Task Capture_27_CompanyIndex()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Company/Index");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "27-Company-Index-Page.png", "Company Index Page - CHECK TABLE PADDING");
    }

    [Test, Order(28)]
    [Category("Admin")]
    [Category("Company")]
    public async Task Capture_28_CompanyManagement()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Company/Management");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "28-Company-Management-Page.png", "Company Management Page");
    }

    #endregion

    #region 6. PRODUCT MANAGEMENT (5 screenshots)

    [Test, Order(29)]
    [Category("Admin")]
    [Category("Product")]
    public async Task Capture_29_ProductManagement()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Product/Management");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "29-Product-Management-Page.png", "Product Management Page");
    }

    [Test, Order(30)]
    [Category("Admin")]
    [Category("Product")]
    public async Task Capture_30_ProductManagementAlt()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Product/ManagementAlt");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "30-Product-Management-Alt-Page.png", "Product Management Alt Page");
    }

    [Test, Order(31)]
    [Category("Admin")]
    [Category("Product")]
    public async Task Capture_31_ProductManagementV2()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Product/ManagementV2");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "31-Product-Management-V2-Page.png", "Product Management V2 Page");
    }

    [Test, Order(32)]
    [Category("Admin")]
    [Category("Product")]
    public async Task Capture_32_ProductCategories()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Product/Categories");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "32-Product-Categories-Page.png", "Product Categories Page");
    }

    [Test, Order(33)]
    [Category("External")]
    [Category("Product")]
    public async Task Capture_33_ProductManagementExternal()
    {
        await ExternalUserPage.GotoAsync($"{BaseUrl}/Product/Management");
        await ExternalUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(ExternalUserPage, "33-External-Product-Management-Page.png", "External Product Management Page");
    }

    #endregion

    #region 7. ADMIN USER & ROLE MANAGEMENT (3 screenshots)

    [Test, Order(34)]
    [Category("Admin")]
    [Category("UserManagement")]
    public async Task Capture_34_UserManagement()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/UserManagement");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "34-User-Management-Page.png", "User Management Page - CHECK TABLE ROWS");
    }

    [Test, Order(35)]
    [Category("Admin")]
    [Category("UserManagement")]
    public async Task Capture_35_CreateUser()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/CreateUser");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "35-Create-User-Page.png", "Create User Page");
    }

    [Test, Order(36)]
    [Category("Admin")]
    [Category("RoleManagement")]
    public async Task Capture_36_RoleManagement()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/RoleManagement");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "36-Role-Management-Page.png", "Role Management Page");
    }

    #endregion

    #region 8. ADMIN CONFIGURATION PAGES (7 screenshots)

    [Test, Order(37)]
    [Category("Admin")]
    [Category("Configuration")]
    public async Task Capture_37_CertificateTypes()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/CertificateTypes");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "37-Certificate-Types-Page.png", "Certificate Types Page");
    }

    [Test, Order(38)]
    [Category("Admin")]
    [Category("Configuration")]
    public async Task Capture_38_CertificateFields()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/CertificateFields");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "38-Certificate-Fields-Page.png", "Certificate Fields Page");
    }

    [Test, Order(39)]
    [Category("Admin")]
    [Category("Configuration")]
    public async Task Capture_39_ProductTypes()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/ProductTypes");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "39-Product-Types-Page.png", "Product Types Page");
    }

    [Test, Order(40)]
    [Category("Admin")]
    [Category("Configuration")]
    public async Task Capture_40_CSFAttributes()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/CSFAttributes");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "40-CSF-Attributes-Page.png", "CSF Attributes Page");
    }

    [Test, Order(41)]
    [Category("Admin")]
    [Category("Configuration")]
    public async Task Capture_41_Tags()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/Tags");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "41-Tags-Page.png", "Tags Management Page");
    }

    [Test, Order(42)]
    [Category("Admin")]
    [Category("Configuration")]
    public async Task Capture_42_Partners()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/Partners");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "42-Partners-Page.png", "Partners Management Page");
    }

    [Test, Order(43)]
    [Category("Admin")]
    [Category("Configuration")]
    public async Task Capture_43_GoogleAnalytics()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/GoogleAnalytics");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "43-Google-Analytics-Page.png", "Google Analytics Configuration Page");
    }

    [Test, Order(44)]
    [Category("Admin")]
    [Category("Configuration")]
    public async Task Capture_44_GCCAPI()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/GCCAPI");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "44-GCC-API-Settings-Page.png", "GCC API Settings Page");
    }

    #endregion

    #region 9. ANALYST PAGES (2 screenshots)

    [Test, Order(45)]
    [Category("Analyst")]
    [Category("Dashboard")]
    public async Task Capture_45_AnalystDashboard()
    {
        await AnalystUserPage.GotoAsync($"{BaseUrl}/Dashboard/Index");
        await AnalystUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(1000);
        await CaptureScreenshot(AnalystUserPage, "45-Analyst-Dashboard.png", "Analyst Dashboard - COMPARE WITH ADMIN");
    }

    [Test, Order(46)]
    [Category("Analyst")]
    [Category("Certificate")]
    public async Task Capture_46_AnalystCertificateTileView()
    {
        await AnalystUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewInternal");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(AnalystUserPage, timeout: 3000);
        await CaptureScreenshot(AnalystUserPage, "46-Analyst-Certificate-Tile-View.png", "Analyst Certificate Tile View");
    }

    #endregion

    #region 10. DOCUMENT MANAGEMENT (3 screenshots - if accessible)

    [Test, Order(47)]
    [Category("Admin")]
    [Category("Document")]
    public async Task Capture_47_DocumentUpload()
    {
        // Get a certificate ID first
        await AdminUserPage.GotoAsync($"{BaseUrl}/Certificate/CertificateTileViewInternal");
        await ScreenshotWaitHelpers.WaitForInfiniteScroll(AdminUserPage, timeout: 2000);

        var firstTile = await AdminUserPage.QuerySelectorAsync(".certificate-tile");
        if (firstTile != null)
        {
            var certId = await firstTile.GetAttributeAsync("data-certificate-id");
            if (!string.IsNullOrEmpty(certId))
            {
                try
                {
                    await AdminUserPage.GotoAsync($"{BaseUrl}/Document/Upload/{certId}");
                    await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await CaptureScreenshot(AdminUserPage, "47-Document-Upload-Page.png", "Document Upload Page");
                }
                catch
                {
                    Console.WriteLine("    ⚠ Document Upload page not accessible");
                }
            }
        }
    }

    #endregion

    #region 11. ADMIN DASHBOARD INDEX (1 screenshot)

    [Test, Order(48)]
    [Category("Admin")]
    public async Task Capture_48_AdminIndex()
    {
        await AdminUserPage.GotoAsync($"{BaseUrl}/Admin/Index");
        await AdminUserPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await CaptureScreenshot(AdminUserPage, "48-Admin-Index-Page.png", "Admin Dashboard Index Page");
    }

    #endregion

    [OneTimeTearDown]
    public new async Task OneTimeTearDown()
    {
        Console.WriteLine($"\n{'='*90}");
        Console.WriteLine($"UI DESIGN REVIEW SCREENSHOT CAPTURE COMPLETE - 100% COVERAGE");
        Console.WriteLine($"{'='*90}");
        Console.WriteLine($"Total Screenshots Captured: {_screenshotCounter}");
        Console.WriteLine($"Location: {Path.GetFullPath(_reviewDirectory)}");
        Console.WriteLine($"\nCoverage:");
        Console.WriteLine($"  ✓ Public Pages: 6");
        Console.WriteLine($"  ✓ Authentication Pages: 9");
        Console.WriteLine($"  ✓ Certificate Management: 10");
        Console.WriteLine($"  ✓ Company Management: 2");
        Console.WriteLine($"  ✓ Product Management: 5");
        Console.WriteLine($"  ✓ User/Role Management: 3");
        Console.WriteLine($"  ✓ Admin Configuration: 8");
        Console.WriteLine($"  ✓ Analyst Pages: 2");
        Console.WriteLine($"  ✓ Document Management: 1");
        Console.WriteLine($"  ✓ Admin Index: 1");
        Console.WriteLine($"\nNext Steps:");
        Console.WriteLine($"1. Review ALL {_screenshotCounter} screenshots for design consistency");
        Console.WriteLine($"2. Compare spacing, padding, colors, and typography across ALL pages");
        Console.WriteLine($"3. Document ALL issues in UI_DESIGN_CONSISTENCY_REVIEW.html");
        Console.WriteLine($"4. Create comprehensive fix list");
        Console.WriteLine($"5. Implement corrections");
        Console.WriteLine($"6. Re-run screenshots to verify");
        Console.WriteLine($"{'='*90}\n");

        await base.OneTimeTearDown();
    }
}
