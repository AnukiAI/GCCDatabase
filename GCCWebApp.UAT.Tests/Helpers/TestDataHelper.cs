namespace GCCWebApp.UAT.Tests.Helpers;

public static class TestDataHelper
{
    public static class Users
    {
        public const string AdminEmail = "admin@test.com";
        public const string AnalystEmail = "analyst@test.com";
        public const string ExternalEmail = "external@test.com";
        public const string PublicEmail = "public@test.com";
        public const string TestPassword = "TestPass123!";
    }

    public static class TestData
    {
        public static readonly string[] SearchTerms = 
        {
            "Green Circle",
            "Certified",
            "Paper",
            "Packaging",
            "Sustainability"
        };

        public static readonly string[] CompanyNames = 
        {
            "Test Company A",
            "Test Company B", 
            "Sample Corp",
            "Demo Industries"
        };

        public static readonly string[] ProductTypes = 
        {
            "Paper Products",
            "Packaging Materials",
            "Textiles",
            "Building Materials"
        };
    }

    public static class Urls
    {
        public const string Home = "/";
        public const string Login = "/Account/Login";
        public const string Certificates = "/Certificate";
        public const string Admin = "/Admin";
        public const string Dashboard = "/Dashboard";
    }

    public static class WaitTimes
    {
        public const int Short = 2000;   // 2 seconds
        public const int Medium = 5000;  // 5 seconds  
        public const int Long = 10000;   // 10 seconds
    }
}