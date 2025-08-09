namespace GCCWebApp.Configuration
{
    /// <summary>
    /// Configuration options for Azure AD B2C
    /// </summary>
    public class AzureAdB2COptions
    {
        public static readonly string SectionName = "AzureAdB2C";

        /// <summary>
        /// The Azure AD B2C instance URL (e.g., https://contoso.b2clogin.com/)
        /// </summary>
        public string Instance { get; set; } = string.Empty;

        /// <summary>
        /// The client ID of the Azure AD B2C application
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// The client secret of the Azure AD B2C application (if using confidential client)
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// The domain of the Azure AD B2C tenant (e.g., contoso.onmicrosoft.com)
        /// </summary>
        public string Domain { get; set; } = string.Empty;

        /// <summary>
        /// The tenant ID of the Azure AD B2C tenant
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// The policy ID for sign-up and sign-in (e.g., B2C_1_SignUpSignIn)
        /// </summary>
        public string SignUpSignInPolicyId { get; set; } = string.Empty;

        /// <summary>
        /// The policy ID for password reset (e.g., B2C_1_PasswordReset)
        /// </summary>
        public string ResetPasswordPolicyId { get; set; } = string.Empty;

        /// <summary>
        /// The policy ID for profile editing (e.g., B2C_1_ProfileEdit)
        /// </summary>
        public string EditProfilePolicyId { get; set; } = string.Empty;

        /// <summary>
        /// The callback path for OIDC sign-in (default: /signin-oidc)
        /// </summary>
        public string CallbackPath { get; set; } = "/signin-oidc";

        /// <summary>
        /// The callback path for OIDC sign-out (default: /signout-callback-oidc)
        /// </summary>
        public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

        /// <summary>
        /// Gets the authority URL for a specific policy
        /// </summary>
        /// <param name="policy">The policy ID</param>
        /// <returns>The complete authority URL</returns>
        public string GetAuthority(string policy)
        {
            return $"{Instance.TrimEnd('/')}/tfp/{TenantId}/{policy}";
        }

        /// <summary>
        /// Gets the sign-up/sign-in authority URL
        /// </summary>
        public string SignUpSignInAuthority => GetAuthority(SignUpSignInPolicyId);

        /// <summary>
        /// Gets the password reset authority URL
        /// </summary>
        public string ResetPasswordAuthority => GetAuthority(ResetPasswordPolicyId);

        /// <summary>
        /// Gets the profile edit authority URL
        /// </summary>
        public string EditProfileAuthority => GetAuthority(EditProfilePolicyId);
    }
}