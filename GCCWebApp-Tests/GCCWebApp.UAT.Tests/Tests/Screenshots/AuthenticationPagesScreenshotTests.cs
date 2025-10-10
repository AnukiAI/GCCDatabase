namespace GCCWebApp.UAT.Tests.Tests.Screenshots;

[TestFixture]
public class AuthenticationPagesScreenshotTests : ScreenshotTestBase
{
    private readonly Dictionary<string, string> _authPages = new()
    {
        { "/Account/Login", "LoginPage" },
        { "/Account/SignUp", "SignUpPage" },
        { "/Account/ForgotPassword", "ForgotPasswordPage" },
        { "/Account/ResetPassword", "ResetPasswordPage" },
        { "/Account/PasswordReset", "PasswordResetPage" },
        { "/Account/PasswordResetSuccess", "PasswordResetSuccessPage" },
        { "/Account/VerifyEmail", "VerifyEmailPage" },
        { "/Account/2FAAuthenticate", "TwoFactorAuthPage" },
        { "/Account/AccountLocked", "AccountLockedPage" },
        { "/Account/WelcomeRedirect", "WelcomeRedirectPage" },
        { "/Account/AccessDenied", "AccessDeniedPage" }
    };

    [Test]
    public async Task CaptureAllAuthenticationPages()
    {
        foreach (var authPage in _authPages)
        {
            foreach (var viewport in TestViewports)
            {
                await NavigateAndScreenshot(PublicPage, authPage.Key, authPage.Value, "Public", viewport);
            }
        }
    }

    [Test]
    [TestCaseSource(nameof(TestViewports))]
    public async Task CaptureLoginPage(ViewportSize viewport)
    {
        await NavigateAndScreenshot(PublicPage, "/Account/Login", "LoginPage_Detailed", "Public", viewport);
    }

    [Test]
    [TestCaseSource(nameof(TestViewports))]  
    public async Task CaptureSignUpPage(ViewportSize viewport)
    {
        await NavigateAndScreenshot(PublicPage, "/Account/SignUp", "SignUpPage_Detailed", "Public", viewport);
    }
}