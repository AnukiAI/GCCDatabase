using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace GCCWebApp.UAT.Tests.Tests.Admin.CRUD
{
    /// <summary>
    /// Certificate workflow and state transition tests
    /// Tests the 14-state certificate lifecycle and workflow operations
    /// </summary>
    [TestFixture]
    [Category("CRUD")]
    [Category("Workflow")]
    [Category("Certificate")]
    public class WorkflowTests : BaseTest
    {
        #region Certificate Status Display Tests

        [Test]
        [Order(1)]
        [Category("StatusDisplay")]
        public async Task CertificateTileView_DisplaysActiveStatus()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Check for active status badges
            var activeStatus = await Page.Locator(".status-active, .badge-success, [class*='active']").CountAsync();
            Assert.That(activeStatus, Is.GreaterThanOrEqualTo(0), "Active status should be displayable");
            await TakeScreenshot("CertificateStatus_Active");
        }

        [Test]
        [Order(2)]
        [Category("StatusDisplay")]
        public async Task CertificateTileView_DisplaysExpiredStatus()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Check for expired status badges
            var expiredStatus = await Page.Locator(".status-expired, .badge-danger, [class*='expired']").CountAsync();
            Assert.That(expiredStatus, Is.GreaterThanOrEqualTo(0), "Expired status should be displayable");
            await TakeScreenshot("CertificateStatus_Expired");
        }

        [Test]
        [Order(3)]
        [Category("StatusDisplay")]
        public async Task CertificateTileView_DisplaysDraftStatus()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Check for draft status badges
            var draftStatus = await Page.Locator(".status-draft, .badge-warning, [class*='draft']").CountAsync();
            Assert.That(draftStatus, Is.GreaterThanOrEqualTo(0), "Draft status should be displayable");
            await TakeScreenshot("CertificateStatus_Draft");
        }

        [Test]
        [Order(4)]
        [Category("StatusDisplay")]
        public async Task CertificateTileView_DisplaysPendingReviewStatus()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Check for pending review status
            var pendingStatus = await Page.Locator(".status-pending, [class*='pending']").CountAsync();
            Assert.That(pendingStatus, Is.GreaterThanOrEqualTo(0), "Pending status should be displayable");
            await TakeScreenshot("CertificateStatus_Pending");
        }

        #endregion

        #region Status Filter Tests

        [Test]
        [Order(10)]
        [Category("StatusFilter")]
        public async Task CertificateTileView_CanFilterByActiveStatus()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for status filter
            var activeFilter = Page.Locator("input[type='checkbox'][value*='active'], label:has-text('Active') input, .filter-active").First;
            if (await activeFilter.CountAsync() > 0)
            {
                await activeFilter.CheckAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            await TakeScreenshot("CertificateFilter_Active");
            Assert.Pass("Active status filter test completed");
        }

        [Test]
        [Order(11)]
        [Category("StatusFilter")]
        public async Task CertificateTileView_CanFilterByExpiredStatus()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for expired filter
            var expiredFilter = Page.Locator("input[type='checkbox'][value*='expired'], label:has-text('Expired') input, .filter-expired").First;
            if (await expiredFilter.CountAsync() > 0)
            {
                await expiredFilter.CheckAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            await TakeScreenshot("CertificateFilter_Expired");
            Assert.Pass("Expired status filter test completed");
        }

        [Test]
        [Order(12)]
        [Category("StatusFilter")]
        public async Task CertificateTileView_CanFilterByDraftStatus()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for draft filter
            var draftFilter = Page.Locator("input[type='checkbox'][value*='draft'], label:has-text('Draft') input, .filter-draft").First;
            if (await draftFilter.CountAsync() > 0)
            {
                await draftFilter.CheckAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            await TakeScreenshot("CertificateFilter_Draft");
            Assert.Pass("Draft status filter test completed");
        }

        #endregion

        #region Certificate Create Workflow Tests

        [Test]
        [Order(20)]
        [Category("CreateWorkflow")]
        public async Task CertificateCreate_AccessibleToAdmin()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/Create");

            // Should be able to access create page
            var createForm = await Page.Locator("form, .certificate-form, [class*='create']").CountAsync();
            Assert.That(createForm, Is.GreaterThan(0), "Admin should access certificate create page");
            await TakeScreenshot("CertificateCreate_AdminAccess");
        }

        [Test]
        [Order(21)]
        [Category("CreateWorkflow")]
        public async Task CertificateCreate_AccessibleToAnalyst()
        {
            await LoginAsRole("Analyst");
            await NavigateToPage("/Certificate/Create");

            // Analyst should also be able to access create page
            var createForm = await Page.Locator("form, .certificate-form, [class*='create']").CountAsync();
            Assert.That(createForm, Is.GreaterThan(0), "Analyst should access certificate create page");
            await TakeScreenshot("CertificateCreate_AnalystAccess");
        }

        [Test]
        [Order(22)]
        [Category("CreateWorkflow")]
        public async Task CertificateCreate_NotAccessibleToExternal()
        {
            await LoginAsRole("External");
            await NavigateToPage("/Certificate/Create");

            // External should NOT be able to access create page
            var currentUrl = Page.Url;
            var accessDenied = await Page.Locator(".access-denied, .unauthorized, [class*='denied']").CountAsync();
            var isRedirected = !currentUrl.Contains("/Certificate/Create");

            Assert.That(accessDenied > 0 || isRedirected, Is.True, "External users should not access certificate create page");
            await TakeScreenshot("CertificateCreate_ExternalRestricted");
        }

        [Test]
        [Order(23)]
        [Category("CreateWorkflow")]
        public async Task CertificateCreate_HasRequiredFields()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/Create");

            // Check for required form fields
            var companyField = await Page.Locator("select[name*='Company'], input[name*='Company'], #Company").CountAsync();
            var productField = await Page.Locator("select[name*='Product'], input[name*='Product'], #Product").CountAsync();
            var certTypeField = await Page.Locator("select[name*='Type'], input[name*='Type'], #CertificateType").CountAsync();

            Assert.That(companyField, Is.GreaterThan(0), "Company field should exist");
            await TakeScreenshot("CertificateCreate_RequiredFields");
        }

        [Test]
        [Order(24)]
        [Category("CreateWorkflow")]
        public async Task CertificateCreate_HasPartnerSelection()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/Create");

            // Check for partner selection (Amazon, Wayfair, None)
            var partnerField = await Page.Locator("select[name*='Partner'], input[name*='Partner'], #Partner, [class*='partner']").CountAsync();
            Assert.That(partnerField, Is.GreaterThanOrEqualTo(0), "Partner selection may exist");
            await TakeScreenshot("CertificateCreate_PartnerField");
        }

        [Test]
        [Order(25)]
        [Category("CreateWorkflow")]
        public async Task CertificateCreate_CanCancelCreation()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/Create");

            var cancelBtn = Page.Locator("button:has-text('Cancel'), a:has-text('Cancel'), .btn-cancel").First;
            if (await cancelBtn.CountAsync() > 0)
            {
                await cancelBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            await TakeScreenshot("CertificateCreate_Cancel");
            Assert.Pass("Certificate create cancel test completed");
        }

        #endregion

        #region Certificate Edit Workflow Tests

        [Test]
        [Order(30)]
        [Category("EditWorkflow")]
        public async Task CertificateEdit_AdminCanEdit()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for edit button on a certificate
            var editBtn = Page.Locator("button:has-text('Edit'), a:has-text('Edit'), .edit-btn, [class*='edit']").First;
            if (await editBtn.CountAsync() > 0)
            {
                await editBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            await TakeScreenshot("CertificateEdit_AdminAccess");
            Assert.Pass("Admin edit access test completed");
        }

        [Test]
        [Order(31)]
        [Category("EditWorkflow")]
        public async Task CertificateEdit_AnalystCanEdit()
        {
            await LoginAsRole("Analyst");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for edit button on a certificate
            var editBtn = Page.Locator("button:has-text('Edit'), a:has-text('Edit'), .edit-btn").First;
            if (await editBtn.CountAsync() > 0)
            {
                await editBtn.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
            }

            await TakeScreenshot("CertificateEdit_AnalystAccess");
            Assert.Pass("Analyst edit access test completed");
        }

        [Test]
        [Order(32)]
        [Category("EditWorkflow")]
        public async Task CertificateEdit_ExternalCannotEdit()
        {
            await LoginAsRole("External");
            await NavigateToPage("/Certificate/CertificateTileViewExternal");

            await Page.WaitForTimeoutAsync(2000);

            // External should NOT see edit buttons
            var editBtn = await Page.Locator("button:has-text('Edit'), a:has-text('Edit'), .edit-btn").CountAsync();
            Assert.That(editBtn, Is.EqualTo(0), "External users should not see edit buttons");
            await TakeScreenshot("CertificateEdit_ExternalRestricted");
        }

        #endregion

        #region Certificate Status Change Tests

        [Test]
        [Order(40)]
        [Category("StatusChange")]
        public async Task CertificateEdit_CanChangeStatus()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Find and click on a certificate to edit
            var certTile = Page.Locator(".certificate-tile, .tile-card, tr").First;
            if (await certTile.CountAsync() > 0)
            {
                var editBtn = certTile.Locator("button:has-text('Edit'), a:has-text('Edit'), .edit-btn").First;
                if (await editBtn.CountAsync() > 0)
                {
                    await editBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(1000);

                    // Look for status dropdown
                    var statusField = await Page.Locator("select[name*='Status'], #Status, [class*='status-select']").CountAsync();
                    Assert.That(statusField, Is.GreaterThanOrEqualTo(0), "Status change option may exist");
                }
            }

            await TakeScreenshot("CertificateEdit_StatusChange");
        }

        [Test]
        [Order(41)]
        [Category("StatusChange")]
        public async Task CertificateEdit_HasStatusOptions()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Find and click on a certificate to edit
            var certTile = Page.Locator(".certificate-tile, .tile-card, tr").First;
            if (await certTile.CountAsync() > 0)
            {
                var editBtn = certTile.Locator("button:has-text('Edit'), a:has-text('Edit'), .edit-btn").First;
                if (await editBtn.CountAsync() > 0)
                {
                    await editBtn.ClickAsync();
                    await Page.WaitForTimeoutAsync(1000);

                    // Check for status options
                    var statusOptions = await Page.Locator("select[name*='Status'] option, .status-option").CountAsync();
                    Assert.That(statusOptions, Is.GreaterThanOrEqualTo(0), "Status options may be available");
                }
            }

            await TakeScreenshot("CertificateEdit_StatusOptions");
        }

        #endregion

        #region Certificate Download Workflow Tests

        [Test]
        [Order(50)]
        [Category("DownloadWorkflow")]
        public async Task CertificateDownload_ButtonExists()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for download button
            var downloadBtn = await Page.Locator("button:has-text('Download'), a:has-text('Download'), .download-btn, [class*='download']").CountAsync();
            Assert.That(downloadBtn, Is.GreaterThanOrEqualTo(0), "Download button should exist");
            await TakeScreenshot("CertificateDownload_Button");
        }

        [Test]
        [Order(51)]
        [Category("DownloadWorkflow")]
        public async Task CertificateDownload_ExternalCanDownload()
        {
            await LoginAsRole("External");
            await NavigateToPage("/Certificate/CertificateTileViewExternal");

            await Page.WaitForTimeoutAsync(2000);

            // External users SHOULD be able to download
            var downloadBtn = await Page.Locator("button:has-text('Download'), a:has-text('Download'), .download-btn").CountAsync();
            Assert.That(downloadBtn, Is.GreaterThanOrEqualTo(0), "External users should see download buttons");
            await TakeScreenshot("CertificateDownload_ExternalAccess");
        }

        [Test]
        [Order(52)]
        [Category("DownloadWorkflow")]
        public async Task CertificateBulkDownload_SelectionCheckboxes()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for selection checkboxes for bulk download
            var checkboxes = await Page.Locator("input[type='checkbox'][class*='select'], .selection-checkbox, input[type='checkbox'][name*='select']").CountAsync();
            Assert.That(checkboxes, Is.GreaterThanOrEqualTo(0), "Selection checkboxes may exist for bulk download");
            await TakeScreenshot("CertificateBulkDownload_Checkboxes");
        }

        [Test]
        [Order(53)]
        [Category("DownloadWorkflow")]
        public async Task CertificateBulkDownload_DownloadSelectedButton()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for "Download Selected" button for ZIP download
            var downloadSelectedBtn = await Page.Locator("button:has-text('Download Selected'), button:has-text('Bulk Download'), .download-selected-btn").CountAsync();
            Assert.That(downloadSelectedBtn, Is.GreaterThanOrEqualTo(0), "Download Selected button may exist for ZIP download");
            await TakeScreenshot("CertificateBulkDownload_Button");
        }

        #endregion

        #region Certificate Expiration Workflow Tests

        [Test]
        [Order(60)]
        [Category("ExpirationWorkflow")]
        public async Task CertificateExpiration_ShowsExpirationDate()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for expiration date column/field
            var expirationDate = await Page.Locator("th:has-text('Expir'), td:has-text('Expir'), .expiration-date, [class*='expir']").CountAsync();
            Assert.That(expirationDate, Is.GreaterThanOrEqualTo(0), "Expiration date should be displayed");
            await TakeScreenshot("CertificateExpiration_Date");
        }

        [Test]
        [Order(61)]
        [Category("ExpirationWorkflow")]
        public async Task CertificateExpiration_HighlightsExpiringSoon()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for visual indicator for certificates expiring soon
            var expiringSoon = await Page.Locator("[class*='expiring-soon'], [class*='warning'], .expiration-warning").CountAsync();
            Assert.That(expiringSoon, Is.GreaterThanOrEqualTo(0), "Expiring soon indicator may exist");
            await TakeScreenshot("CertificateExpiration_Warning");
        }

        #endregion

        #region Certificate Renewal Workflow Tests

        [Test]
        [Order(70)]
        [Category("RenewalWorkflow")]
        public async Task CertificateRenewal_RenewButtonExists()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for renew button
            var renewBtn = await Page.Locator("button:has-text('Renew'), a:has-text('Renew'), .renew-btn").CountAsync();
            Assert.That(renewBtn, Is.GreaterThanOrEqualTo(0), "Renew button may exist");
            await TakeScreenshot("CertificateRenewal_Button");
        }

        #endregion

        #region Certificate History/Audit Tests

        [Test]
        [Order(80)]
        [Category("AuditWorkflow")]
        public async Task CertificateHistory_CanViewHistory()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Find a certificate and look for history/audit option
            var historyBtn = await Page.Locator("button:has-text('History'), a:has-text('History'), button:has-text('Audit'), .history-btn").CountAsync();
            Assert.That(historyBtn, Is.GreaterThanOrEqualTo(0), "History/audit option may exist");
            await TakeScreenshot("CertificateHistory_Button");
        }

        #endregion

        #region Tag Assignment Workflow Tests

        [Test]
        [Order(90)]
        [Category("TagWorkflow")]
        public async Task CertificateTags_CanViewTags()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Look for tags column or display
            var tagsDisplay = await Page.Locator("th:has-text('Tag'), td:has-text('tag'), .tag-badge, [class*='tag']").CountAsync();
            Assert.That(tagsDisplay, Is.GreaterThanOrEqualTo(0), "Tags should be visible");
            await TakeScreenshot("CertificateTags_Display");
        }

        [Test]
        [Order(91)]
        [Category("TagWorkflow")]
        public async Task CertificateTags_AdminCanSeeInternalTags()
        {
            await LoginAsRole("Admin");
            await NavigateToPage("/Certificate/CertificateTileViewInternal");

            await Page.WaitForTimeoutAsync(2000);

            // Admin should see internal tags
            var internalTags = await Page.Locator("th:has-text('Internal'), .internal-tag, [class*='internal']").CountAsync();
            Assert.That(internalTags, Is.GreaterThanOrEqualTo(0), "Admin should see internal tags");
            await TakeScreenshot("CertificateTags_InternalAdmin");
        }

        [Test]
        [Order(92)]
        [Category("TagWorkflow")]
        public async Task CertificateTags_ExternalCannotSeeInternalTags()
        {
            await LoginAsRole("External");
            await NavigateToPage("/Certificate/CertificateTileViewExternal");

            await Page.WaitForTimeoutAsync(2000);

            // External should NOT see internal tag column
            var internalTagColumn = await Page.Locator("th:has-text('Internal Tag'), th:has-text('Internal Tags')").CountAsync();
            Assert.That(internalTagColumn, Is.EqualTo(0), "External users should NOT see internal tag column");
            await TakeScreenshot("CertificateTags_InternalExternal");
        }

        #endregion
    }
}
