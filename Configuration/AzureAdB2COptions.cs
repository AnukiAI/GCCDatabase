namespace GCCWebApp.Configuration
{
    public class AzureAdB2COptions
    {
        public const string SectionName = "AzureAdB2C";

        public string Instance { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SignUpSignInPolicyId { get; set; } = string.Empty;
        public string ResetPasswordPolicyId { get; set; } = string.Empty;
        public string EditProfilePolicyId { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = "/signin-oidc";
        public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

        public string Authority => $"{Instance.TrimEnd('/')}/{Domain}/{SignUpSignInPolicyId}/v2.0";
        public string MetadataAddress => $"{Authority}/.well-known/openid_configuration";
        public string SignUpSignInAuthority => $"{Instance.TrimEnd('/')}/{Domain}/{SignUpSignInPolicyId}/v2.0";
        public string ResetPasswordAuthority => $"{Instance.TrimEnd('/')}/{Domain}/{ResetPasswordPolicyId}/v2.0";

        public bool IsConfigured => !string.IsNullOrEmpty(Instance) && 
                                   !string.IsNullOrEmpty(Domain) && 
                                   !string.IsNullOrEmpty(ClientId) && 
                                   !string.IsNullOrEmpty(SignUpSignInPolicyId);
    }
}