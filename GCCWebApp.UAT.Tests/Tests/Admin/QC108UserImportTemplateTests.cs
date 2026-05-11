using System.Text;
using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.Admin;

/// QC #108 — Admin / User: Template export + Import round-trip rigorous coverage.
/// Tests template headers, sample-data removal, round-trip import, multi-company
/// semicolon parsing, invalid role/company rejection, export header drift, RBAC.
/// Run against localhost (TEST_BASE_URL=http://localhost:5101) using dev admin login.
[TestFixture]
public class QC108UserImportTemplateTests : BaseTest
{
    private const string RunTag = "qc108";
    private string _runStamp = string.Empty;

    [SetUp]
    public void StampRun()
    {
        _runStamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    }

    private async Task GotoUserManagementAsAdmin()
    {
        await LoginAsRole("Admin");
        await NavigateToPage("/Admin/UserManagement");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForFunctionAsync(
            "() => window.userManagement && Array.isArray(window.userManagement.companies)",
            null,
            new PageWaitForFunctionOptions { Timeout = 15000 });
    }

    /// <summary>
    /// Capture the CSV content the Download Template button produces by patching
    /// URL.createObjectURL to intercept the Blob, then invoking downloadImportTemplate().
    /// </summary>
    private async Task<string> CaptureTemplateCsvAsync()
    {
        return await Page.EvaluateAsync<string>(@"async () => {
            return await new Promise((resolve, reject) => {
                const originalCreate = URL.createObjectURL;
                URL.createObjectURL = function(blob) {
                    const reader = new FileReader();
                    reader.onload = () => {
                        URL.createObjectURL = originalCreate;
                        resolve(reader.result);
                    };
                    reader.onerror = () => {
                        URL.createObjectURL = originalCreate;
                        reject(reader.error);
                    };
                    reader.readAsText(blob);
                    return 'blob:intercepted';
                };
                try {
                    window.userManagement.downloadImportTemplate();
                } catch (e) {
                    URL.createObjectURL = originalCreate;
                    reject(e);
                }
            });
        }");
    }

    /// <summary>
    /// Same intercept pattern for export.
    /// </summary>
    private async Task<string> CaptureExportCsvAsync()
    {
        return await Page.EvaluateAsync<string>(@"async () => {
            return await new Promise((resolve, reject) => {
                const originalCreate = URL.createObjectURL;
                URL.createObjectURL = function(blob) {
                    const reader = new FileReader();
                    reader.onload = () => {
                        URL.createObjectURL = originalCreate;
                        resolve(reader.result);
                    };
                    reader.onerror = () => {
                        URL.createObjectURL = originalCreate;
                        reject(reader.error);
                    };
                    reader.readAsText(blob);
                    return 'blob:intercepted';
                };
                try {
                    window.userManagement.exportUsers();
                } catch (e) {
                    URL.createObjectURL = originalCreate;
                    reject(e);
                }
            });
        }");
    }

    /// <summary>
    /// POST CSV bytes to /Admin/ImportUsers using the page's auth context and antiforgery token.
    /// </summary>
    private async Task<IAPIResponse> ImportCsvAsync(string csvContent)
    {
        var token = await Page.EvaluateAsync<string>(@"() => {
            const t = document.querySelector('input[name=""__RequestVerificationToken""]');
            return t ? t.value : '';
        }");
        Assert.That(token, Is.Not.Empty, "Antiforgery token should be present on UserManagement page");

        var multipart = Page.APIRequest.CreateFormData();
        multipart.Set("file", new FilePayload
        {
            Name = $"qc108_test_{_runStamp}.csv",
            MimeType = "text/csv",
            Buffer = Encoding.UTF8.GetBytes(csvContent),
        });

        var response = await Page.APIRequest.PostAsync(BaseUrl + "/Admin/ImportUsers", new APIRequestContextOptions
        {
            Multipart = multipart,
            Headers = new Dictionary<string, string>
            {
                ["RequestVerificationToken"] = token,
            },
        });
        return response;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC108-01 (Baseline): Template headers match the spec exactly.
    // ─────────────────────────────────────────────────────────────────────────
    [Test]
    public async Task TC108_01_Template_HasExpectedHeadersInOrder()
    {
        await GotoUserManagementAsAdmin();

        var csv = await CaptureTemplateCsvAsync();
        TestContext.WriteLine($"Template CSV:\n{csv}");

        Assert.That(csv, Is.Not.Null.And.Not.Empty, "Template CSV should not be empty");
        var lines = csv.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.That(lines.Length, Is.EqualTo(1), $"Template should be headers-only (no sample data rows); got {lines.Length} non-empty lines.");

        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        var expected = new[]
        {
            "Name",
            "Email",
            "Role (Admin/Analyst/External)",
            "Password",
            "Status (Active/Inactive)",
            "RequirePasswordReset (Yes/No)",
            "User Company (semicolon-separated if multiple)",
        };
        Assert.That(headers, Is.EqualTo(expected), "Template headers do not match the spec verbatim or are out of order.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC108-02 (Baseline): No "John Doe"-style sample row, no PII leaks.
    // ─────────────────────────────────────────────────────────────────────────
    [Test]
    public async Task TC108_02_Template_HasNoSampleDataRow()
    {
        await GotoUserManagementAsAdmin();

        var csv = await CaptureTemplateCsvAsync();
        Assert.That(csv, Does.Not.Contain("John Doe"));
        Assert.That(csv, Does.Not.Contain("johndoe"));
        Assert.That(csv, Does.Not.Contain("@example.com"));
        Assert.That(csv, Does.Not.Contain("Sample"));

        var dataRows = csv.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Skip(1).ToArray();
        Assert.That(dataRows.Length, Is.EqualTo(0), $"Template should be headers-only; found {dataRows.Length} data row(s).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC108-03 (Round-trip): Download template → fill 1 valid row → import → success.
    // ─────────────────────────────────────────────────────────────────────────
    [Test]
    public async Task TC108_03_Import_ValidRowRoundTripCreatesUser()
    {
        await GotoUserManagementAsAdmin();
        var templateCsv = await CaptureTemplateCsvAsync();
        Assert.That(templateCsv, Does.Contain("User Company"), "Pre-condition: template should expose the User Company column");

        var email = $"{RunTag}-valid-{_runStamp}@gcc-test.local";
        var csv = templateCsv.TrimEnd() + "\n" +
                  $"QC108 Valid {_runStamp},{email},External,TestPass123!,Active,Yes,";

        var response = await ImportCsvAsync(csv);
        Assert.That(response.Ok, Is.True, $"ImportUsers should return 200; got {response.Status}");

        var body = await response.JsonAsync();
        TestContext.WriteLine($"Import response: {body}");

        var users = await Page.APIRequest.GetAsync(BaseUrl + "/Admin/GetUsers");
        Assert.That(users.Ok, Is.True);
        var usersText = await users.TextAsync();
        Assert.That(usersText, Does.Contain(email), "Imported user email should appear in /Admin/GetUsers response.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC108-04 (Edge / Multi-company): Semicolon-separated company names map to multiple companyIds.
    // ─────────────────────────────────────────────────────────────────────────
    [Test]
    public async Task TC108_04_Import_SemicolonAssignsMultipleCompanies()
    {
        await GotoUserManagementAsAdmin();

        // Pull two real company names from the page's already-loaded company list
        var companies = await Page.EvaluateAsync<string[]>(
            "() => window.userManagement.companies.slice(0, 2).map(c => c.name)");
        Assert.That(companies, Has.Length.EqualTo(2),
            "Need at least 2 companies in dev DB to run this test");

        var email = $"{RunTag}-multi-{_runStamp}@gcc-test.local";
        var csv =
            "Name,Email,Role (Admin/Analyst/External),Password,Status (Active/Inactive),RequirePasswordReset (Yes/No),User Company (semicolon-separated if multiple)\n" +
            $"QC108 Multi {_runStamp},{email},External,TestPass123!,Active,Yes,{companies[0]};{companies[1]}";

        var response = await ImportCsvAsync(csv);
        Assert.That(response.Ok, Is.True, $"ImportUsers should return 200; got {response.Status}");
        var body = await response.JsonAsync();
        TestContext.WriteLine($"Import response: {body}");

        // Reload users in the page and check companyIds count
        await Page.EvaluateAsync("async () => await window.userManagement.loadUsers()");
        var assignedCompanyCount = await Page.EvaluateAsync<int>(
            @"(email) => {
                const u = window.userManagement.users.find(x => x.email && x.email.toLowerCase() === email.toLowerCase());
                if (!u) return -1;
                return (u.companyIds || []).length;
            }", email);

        Assert.That(assignedCompanyCount, Is.EqualTo(2),
            $"Imported user '{email}' should have 2 company assignments (got {assignedCompanyCount}).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC108-05 (Negative): Unknown company name surfaces a specific error.
    // ─────────────────────────────────────────────────────────────────────────
    [Test]
    public async Task TC108_05_Import_UnknownCompanyNameReturnsError()
    {
        await GotoUserManagementAsAdmin();

        var email = $"{RunTag}-bad-co-{_runStamp}@gcc-test.local";
        var csv =
            "Name,Email,Role (Admin/Analyst/External),Password,Status (Active/Inactive),RequirePasswordReset (Yes/No),User Company (semicolon-separated if multiple)\n" +
            $"QC108 Bad Co {_runStamp},{email},External,TestPass123!,Active,Yes,Nonexistent-Co-{_runStamp}";

        var response = await ImportCsvAsync(csv);
        var text = await response.TextAsync();
        TestContext.WriteLine($"Import response: {text}");

        Assert.That(text.ToLowerInvariant().Replace("\"", ""), Does.Contain("not found"),
            "Unknown company name should produce a 'not found' error line.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC108-06 (Negative): Missing Email → row rejected with actionable error.
    // ─────────────────────────────────────────────────────────────────────────
    [Test]
    public async Task TC108_06_Import_MissingEmailRejectedWithSpecificError()
    {
        await GotoUserManagementAsAdmin();

        var csv =
            "Name,Email,Role (Admin/Analyst/External),Password,Status (Active/Inactive),RequirePasswordReset (Yes/No),User Company (semicolon-separated if multiple)\n" +
            $"QC108 NoEmail {_runStamp},,External,TestPass123!,Active,Yes,";

        var response = await ImportCsvAsync(csv);
        var text = await response.TextAsync();
        TestContext.WriteLine($"Import response: {text}");

        Assert.That(text, Does.Contain("Email"), "Missing-email error message should mention 'Email'.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TC108-07 (Cross-feature / Drift): Export CSV header should align with template.
    // Currently expected to FAIL — exposes "Company" vs "User Company" drift.
    // ─────────────────────────────────────────────────────────────────────────
    [Test]
    public async Task TC108_07_Export_HeaderNamingMatchesTemplate()
    {
        await GotoUserManagementAsAdmin();
        await Page.EvaluateAsync("async () => await window.userManagement.loadUsers()");
        // Make sure at least one row exists or the export early-returns.
        var userCount = await Page.EvaluateAsync<int>("() => (window.userManagement.filteredUsers || []).length");
        if (userCount == 0)
        {
            Assert.Ignore("No users in filtered list — cannot exercise export.");
            return;
        }

        var csv = await CaptureExportCsvAsync();
        TestContext.WriteLine($"Export CSV header line: {csv.Split('\n').FirstOrDefault()}");

        // Soft assertion: template uses "User Company"; export should match to avoid client confusion.
        Assert.That(csv, Does.Contain("User Company"),
            "Export header should use 'User Company' to match the import template (Carly QC #108, 5/1 feedback).");
    }
}
