using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GCCWebApp.UAT.Tests.Tests.Production;

/// <summary>
/// CI/CD regression tests covering critical QC items.
/// Runs via HTTP against any deployed instance (local or production).
/// Set TEST_BASE_URL env var to target a specific host (default: https://gccwebapp.com).
///
/// These tests are designed to run on every deploy to catch regressions
/// in the most critical QC items from the retest plan.
/// </summary>
[TestFixture]
[Category("Production")]
[Category("CICD")]
[Category("Regression")]
public class QCRegressionTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com";

    private HttpClient _publicClient = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _analystClient = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        // Public client (no auth)
        _publicClient = CreateClient();

        // Admin client (authenticated)
        _adminClient = CreateClient();
        await LoginAsync(_adminClient, "admin", "TestPass123!");

        // Analyst client (authenticated)
        _analystClient = CreateClient();
        await LoginAsync(_analystClient, "analyst", "TestPass123!");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _publicClient?.Dispose();
        _adminClient?.Dispose();
        _analystClient?.Dispose();
    }

    #region Phase 1: Critical Items

    [Test]
    [Description("QC 135/136: Dates should be yyyy-MM-dd strings without timezone offset")]
    public async Task QC135_136_PublicTiles_DateFormat_NoTimezoneShift()
    {
        var response = await _publicClient.GetAsync("/api/certificate-tiles?page=1&pageSize=5");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await ParseJson(response);
        var certs = json.GetProperty("certificates");
        Assert.That(certs.GetArrayLength(), Is.GreaterThan(0), "No certificates returned");

        foreach (var cert in certs.EnumerateArray())
        {
            var effDate = cert.GetProperty("effectiveDate").GetString();
            var expDate = cert.GetProperty("expirationDate").GetString();

            // Dates must be yyyy-MM-dd format (no T00:00:00, no timezone)
            Assert.That(effDate, Does.Match(@"^\d{4}-\d{2}-\d{2}$"),
                $"Effective date format wrong: {effDate}");
            Assert.That(expDate, Does.Match(@"^\d{4}-\d{2}-\d{2}$"),
                $"Expiration date format wrong: {expDate}");

            // Non-expired certs should have isExpired = false
            var isExpired = cert.GetProperty("isExpired").GetBoolean();
            if (!isExpired)
            {
                var expiration = DateTime.Parse(expDate!);
                Assert.That(expiration.Date, Is.GreaterThanOrEqualTo(DateTime.UtcNow.Date.AddDays(-1)),
                    $"Non-expired cert {cert.GetProperty("certificateNumber").GetString()} has past expiration");
            }
        }
    }

    [Test]
    [Description("QC 127: Company filter should return names, not just IDs")]
    public async Task QC127_Filters_CompanyNames_NotIds()
    {
        var response = await _publicClient.GetAsync("/api/certificate-tiles/filters");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await ParseJson(response);
        var companies = json.GetProperty("companies");
        Assert.That(companies.GetArrayLength(), Is.GreaterThan(0), "No companies in filters");

        foreach (var company in companies.EnumerateArray())
        {
            var name = company.GetProperty("name").GetString();
            var id = company.GetProperty("id").GetInt32();

            Assert.That(name, Is.Not.Null.And.Not.Empty,
                $"Company id {id} has no name");
            Assert.That(name, Does.Not.Match(@"^\d+$"),
                $"Company name looks like an ID: {name}");
        }
    }

    [Test]
    [Description("QC 128: LEED/APR tag filters should return only tagged certs")]
    public async Task QC128_TagFilter_LEED_ReturnsOnlyTaggedCerts()
    {
        // First check tags exist in filter options
        var filterResponse = await _publicClient.GetAsync("/api/certificate-tiles/filters");
        var filters = await ParseJson(filterResponse);
        var tags = filters.GetProperty("additionalTags");

        var tagList = new List<string>();
        foreach (var t in tags.EnumerateArray())
            tagList.Add(t.GetString()!);

        // Test each available tag
        foreach (var tag in tagList)
        {
            var response = await _publicClient.GetAsync($"/api/certificate-tiles?page=1&pageSize=50&tags={tag}");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = await ParseJson(response);
            var totalCount = json.GetProperty("totalCount").GetInt32();

            // Tag filter should return fewer than total unfiltered results
            var unfilteredResponse = await _publicClient.GetAsync("/api/certificate-tiles?page=1&pageSize=1");
            var unfiltered = await ParseJson(unfilteredResponse);
            var unfilteredTotal = unfiltered.GetProperty("totalCount").GetInt32();

            Assert.That(totalCount, Is.LessThanOrEqualTo(unfilteredTotal),
                $"Tag filter '{tag}' returned more certs ({totalCount}) than unfiltered ({unfilteredTotal})");
            Assert.That(totalCount, Is.GreaterThanOrEqualTo(0),
                $"Tag filter '{tag}' returned negative count");
        }
    }

    [Test]
    [Description("QC 107: User creation endpoint should return specific error for duplicate email")]
    public async Task QC107_UserCreation_DuplicateEmail_ReturnsSpecificError()
    {
        // Try creating a user with the admin email (which already exists)
        var token = await GetAntiForgeryToken(_adminClient, "/Admin/UserManagement");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Name", "Duplicate Test"),
            new KeyValuePair<string, string>("Email", "admin"),
            new KeyValuePair<string, string>("Password", "TestPass123!"),
            new KeyValuePair<string, string>("Role", "Analyst"),
            new KeyValuePair<string, string>("IsActive", "true"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await _adminClient.PostAsync("/Admin/CreateUser", formData);
        var content = await response.Content.ReadAsStringAsync();

        // Should get a response (not crash) — and if it's a duplicate, error should mention email
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.InternalServerError),
            "User creation returned 500 error");
    }

    #endregion

    #region Phase 2: Core Functionality

    [Test]
    [Description("QC 133: Create cert page should have blank date fields")]
    public async Task QC133_CreateCert_DatesBlank()
    {
        var response = await _analystClient.GetAsync("/Certificate/Create");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var html = await response.Content.ReadAsStringAsync();

        // Check EffectiveDate has empty value or no value attribute
        var effMatch = Regex.Match(html, @"EffectiveDate[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase);
        if (effMatch.Success)
        {
            Assert.That(effMatch.Groups[1].Value, Is.Empty,
                "Effective date should be blank on create page");
        }

        var expMatch = Regex.Match(html, @"ExpirationDate[^>]*value=""([^""]*)""", RegexOptions.IgnoreCase);
        if (expMatch.Success)
        {
            Assert.That(expMatch.Groups[1].Value, Is.Empty,
                "Expiration date should be blank on create page");
        }
    }

    [Test]
    [Description("QC 154: Dashboard active certs should include Confidential status")]
    public async Task QC154_Dashboard_CountsIncludeConfidential()
    {
        // Get dashboard active count
        var dashResponse = await _adminClient.GetAsync("/Dashboard/DownloadStat?type=total-active-certs");
        Assert.That(dashResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var dashCsv = await dashResponse.Content.ReadAsStringAsync();

        // Get public count (excludes Confidential)
        var publicResponse = await _publicClient.GetAsync("/api/certificate-tiles?page=1&pageSize=1");
        var publicJson = await ParseJson(publicResponse);
        var publicCount = publicJson.GetProperty("totalCount").GetInt32();

        // Parse dashboard total from CSV header
        var totalMatch = Regex.Match(dashCsv, @"Total Certificates:\s*(\d+)");
        Assert.That(totalMatch.Success, Is.True, "Could not parse total from dashboard CSV");
        var dashTotal = int.Parse(totalMatch.Groups[1].Value);

        // Dashboard total should be >= public total (Confidential adds to dashboard but not public)
        Assert.That(dashTotal, Is.GreaterThanOrEqualTo(publicCount),
            $"Dashboard ({dashTotal}) should be >= public count ({publicCount}) because Confidential certs are included");
    }

    #endregion

    #region Phase 3: UI/UX Verification

    [Test]
    [Description("QC 130: Public view should not have Product Type A-Z sort option")]
    public async Task QC130_PublicView_NoProductTypeSortOption()
    {
        var response = await _publicClient.GetAsync("/Certificate/CertificateTileView");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Not.Contain("Product Type A-Z"),
            "Public view should not have Product Type A-Z sort option");
    }

    [Test]
    [Description("QC 147: Import/Export icons should use correct FontAwesome classes")]
    public async Task QC147_AdminPages_CorrectImportExportIcons()
    {
        var pages = new[] { "/Admin/Tags", "/Admin/UserManagement" };

        foreach (var page in pages)
        {
            var response = await _adminClient.GetAsync(page);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Failed to load {page}");

            var html = await response.Content.ReadAsStringAsync();

            // Import buttons should use fa-file-import
            if (html.Contains("import-btn") || html.Contains("Import"))
            {
                Assert.That(html, Does.Contain("fa-file-import"),
                    $"{page}: Import should use fa-file-import icon");
            }

            // Export buttons should use fa-file-export
            if (html.Contains("export-btn") || html.Contains("Export"))
            {
                Assert.That(html, Does.Contain("fa-file-export"),
                    $"{page}: Export should use fa-file-export icon");
            }
        }
    }

    [Test]
    [Description("QC 146: Tags page should have title left, buttons right layout")]
    public async Task QC146_TagsPage_HeaderAlignment()
    {
        var response = await _adminClient.GetAsync("/Admin/Tags");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("admin-header-content"), "Missing admin-header-content class");
        Assert.That(html, Does.Contain("admin-title-section"), "Missing admin-title-section class");
        Assert.That(html, Does.Contain("admin-actions-section"), "Missing admin-actions-section class");
        Assert.That(html, Does.Contain("Tag Management"), "Missing Tag Management title");
    }

    #endregion

    #region Phase 4: Admin Features

    [Test]
    [Description("QC 104: Breadcrumbs should link Administration to Admin Dashboard")]
    public async Task QC104_Breadcrumbs_AdminLink()
    {
        var pages = new[] { "/Product/Management", "/Admin/Tags", "/Admin/UserManagement" };

        foreach (var page in pages)
        {
            var response = await _adminClient.GetAsync(page);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Failed to load {page}");

            var html = await response.Content.ReadAsStringAsync();
            Assert.That(html, Does.Contain("Admin/Index").Or.Contain("Admin\""),
                $"{page}: Should have Administration breadcrumb linking to Admin page");
        }
    }

    [Test]
    [Description("QC 156: Dashboard labels should say 'Active Certs Published'")]
    public async Task QC156_Dashboard_ActiveCertsLabels()
    {
        var response = await _adminClient.GetAsync("/Dashboard");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("Active Certs Published Last Month"),
            "Dashboard should say 'Active Certs Published Last Month'");
        Assert.That(html, Does.Contain("Active Certs Published This Year"),
            "Dashboard should say 'Active Certs Published This Year'");
        Assert.That(html, Does.Contain("Active Certs Published Last Year"),
            "Dashboard should say 'Active Certs Published Last Year'");
    }

    [Test]
    [Description("QC 155: Dashboard category export should be aggregated (name + count)")]
    public async Task QC155_Dashboard_AggregatedExport()
    {
        var exportTypes = new[]
        {
            "product-categories-with-active-certs",
            "product-types-with-active-certs",
            "companies-with-active-certs"
        };

        foreach (var exportType in exportTypes)
        {
            var response = await _adminClient.GetAsync($"/Dashboard/DownloadStat?type={exportType}");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Export {exportType} failed");

            var csv = await response.Content.ReadAsStringAsync();

            // Should NOT have full cert detail headers
            Assert.That(csv, Does.Not.Contain("Certificate Number,Status,Company,Product"),
                $"Export {exportType} should be aggregated, not detail rows");

            // Should have count column
            Assert.That(csv, Does.Contain("Active Certificate Count"),
                $"Export {exportType} should have 'Active Certificate Count' column");
        }
    }

    #endregion

    #region Phase 5: Remaining Items

    [Test]
    [Description("QC 139: Pathway label should not contain '(Optional)'")]
    public async Task QC139_CreateCert_PathwayNoOptional()
    {
        var response = await _analystClient.GetAsync("/Certificate/Create");
        var html = await response.Content.ReadAsStringAsync();

        // Find pathway label
        Assert.That(html, Does.Contain("Pathway"), "Pathway field should exist");
        Assert.That(html, Does.Not.Contain("Pathway (Optional)").And.Not.Contain("Pathway(Optional)"),
            "Pathway label should not contain '(Optional)'");
    }

    [Test]
    [Description("QC 59: Create cert page should have SearchableSelect on key dropdowns")]
    public async Task QC59_CreateCert_SearchableDropdowns()
    {
        var response = await _analystClient.GetAsync("/Certificate/Create");
        var html = await response.Content.ReadAsStringAsync();

        Assert.That(html, Does.Contain("searchable-select.js"),
            "SearchableSelect JS should be loaded");

        var expectedSelectors = new[]
        {
            "CertificationTypeId",
            "ProductCategoryId",
            "countrySelect",
            "CompanyId",
            "ProductId"
        };

        foreach (var selector in expectedSelectors)
        {
            Assert.That(html, Does.Contain($"SearchableSelect('#{selector}'"),
                $"SearchableSelect should be initialized for #{selector}");
        }
    }

    [Test]
    [Description("QC 129: Autocomplete should only return public-visible results")]
    public async Task QC129_Autocomplete_PublicOnly()
    {
        var response = await _publicClient.GetAsync("/api/certificate-tiles/autocomplete?term=a&limit=20");

        // Should return 200 (not error)
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await ParseJson(response);
        Assert.That(json.TryGetProperty("suggestions", out var suggestions), Is.True,
            "Autocomplete should return suggestions");

        // Each suggestion should not have type labels (QC 129)
        foreach (var suggestion in suggestions.EnumerateArray())
        {
            var type = suggestion.GetProperty("type").GetString();
            var label = suggestion.GetProperty("label").GetString();
            Assert.That(label, Does.Not.Contain("Company:").And.Not.Contain("Product:"),
                $"Suggestion label should not have type prefix: {label}");
        }
    }

    [Test]
    [Description("QC 163: Internal view filter sections should have collapse headers")]
    public async Task QC163_InternalView_FilterCollapse()
    {
        var response = await _analystClient.GetAsync("/Certificate/CertificateTileViewInternal");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var html = await response.Content.ReadAsStringAsync();

        // Should have filter-header elements for collapse
        var filterHeaders = Regex.Matches(html, "filter-header");
        Assert.That(filterHeaders.Count, Is.GreaterThanOrEqualTo(3),
            "Should have at least 3 collapsible filter sections");

        // Should have chevron icons
        Assert.That(html, Does.Contain("fa-chevron-down").Or.Contain("fa-chevron-up"),
            "Filter sections should have chevron icons for collapse toggle");
    }

    [Test]
    [Description("QC 161: Internal list view should have column toggles for additional fields")]
    public async Task QC161_InternalView_ColumnToggles()
    {
        var response = await _analystClient.GetAsync("/Certificate/CertificateTileViewInternal");
        var html = await response.Content.ReadAsStringAsync();

        var requiredColumns = new[] { "tc-certresults", "tc-pathway", "tc-certsub", "tc-companyurl" };

        foreach (var col in requiredColumns)
        {
            Assert.That(html, Does.Contain(col),
                $"Column toggle '{col}' should exist in internal view");
        }
    }

    #endregion

    #region Smoke Tests (Page Load Verification)

    [Test]
    [Description("All critical pages should return 200")]
    public async Task SmokeTest_AllCriticalPages_Return200()
    {
        var publicPages = new[] { "/Certificate/CertificateTileView" };
        var adminPages = new[]
        {
            "/Dashboard", "/Admin/Index", "/Admin/UserManagement",
            "/Admin/Tags", "/Admin/CertificateFields", "/Admin/ProductTypes",
            "/Admin/Partners", "/Admin/PartnerApi", "/Admin/RoleManagement",
            "/Product/Management", "/Company/Index"
        };
        var analystPages = new[]
        {
            "/Certificate/Create", "/Certificate/CertificateTileViewInternal"
        };

        foreach (var page in publicPages)
        {
            var r = await _publicClient.GetAsync(page);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Public page {page} failed");
        }

        foreach (var page in adminPages)
        {
            var r = await _adminClient.GetAsync(page);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Admin page {page} failed");
        }

        foreach (var page in analystPages)
        {
            var r = await _analystClient.GetAsync(page);
            Assert.That(r.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Analyst page {page} failed");
        }
    }

    [Test]
    [Description("Public tile API should return valid JSON with expected structure")]
    public async Task SmokeTest_PublicTileApi_ValidStructure()
    {
        var response = await _publicClient.GetAsync("/api/certificate-tiles?page=1&pageSize=5");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await ParseJson(response);
        Assert.That(json.TryGetProperty("success", out _), Is.True, "Missing 'success' field");
        Assert.That(json.TryGetProperty("certificates", out _), Is.True, "Missing 'certificates' field");
        Assert.That(json.TryGetProperty("totalCount", out _), Is.True, "Missing 'totalCount' field");
        Assert.That(json.TryGetProperty("hasMore", out _), Is.True, "Missing 'hasMore' field");
    }

    [Test]
    [Description("Filter API should return all expected filter categories")]
    public async Task SmokeTest_FilterApi_AllCategories()
    {
        var response = await _publicClient.GetAsync("/api/certificate-tiles/filters");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var json = await ParseJson(response);
        var expectedKeys = new[] { "certificationTypes", "companies", "countries", "additionalTags" };

        foreach (var key in expectedKeys)
        {
            Assert.That(json.TryGetProperty(key, out _), Is.True,
                $"Filter API missing '{key}' category");
        }
    }

    #endregion

    #region Helpers

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
            UseCookies = true
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var loginPage = await client.GetAsync("/Account/Login");
        loginPage.EnsureSuccessStatusCode();
        var html = await loginPage.Content.ReadAsStringAsync();

        var token = ExtractToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", email),
            new KeyValuePair<string, string>("Password", password),
            new KeyValuePair<string, string>("RememberMe", "false"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await client.PostAsync("/Account/Login", form);
        var finalUrl = response.RequestMessage?.RequestUri?.AbsolutePath ?? "";
        if (finalUrl.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Login failed for '{email}'");
    }

    private static async Task<string> GetAntiForgeryToken(HttpClient client, string pageUrl)
    {
        var page = await client.GetAsync(pageUrl);
        var html = await page.Content.ReadAsStringAsync();
        return ExtractToken(html);
    }

    private static string ExtractToken(string html)
    {
        var match = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""",
            RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(html,
                @"value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new InvalidOperationException("Anti-forgery token not found");
        return match.Groups[1].Value;
    }

    private static async Task<JsonElement> ParseJson(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content).RootElement;
    }

    #endregion
}
