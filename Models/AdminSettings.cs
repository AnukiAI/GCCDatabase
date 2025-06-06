namespace GCCWebApp.Models
{
    public class AdminSettings
    {
        public bool EnableGoogleAnalytics { get; set; }
        public string GoogleAnalyticsKey { get; set; }

        public bool EnableAmazonAPI { get; set; }
        public string AmazonAPIKey { get; set; }
    }
}