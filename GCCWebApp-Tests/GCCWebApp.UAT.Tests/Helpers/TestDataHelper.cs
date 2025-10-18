namespace GCCWebApp.UAT.Tests.Helpers;

public static class TestDataHelper
{
    public static class Users
    {
        public const string AdminEmail = "admin@test.com";
        public const string AnalystEmail = "analyst@test.com";
        public const string ExternalEmail = "external@test.com";
        public const string ClientEmail = "client@test.com";
        public const string PartnerEmail = "partner@test.com";
        public const string PublicEmail = "public@test.com";
        public const string TestPassword = "TestPass123!";
        
        public static readonly Dictionary<string, (string Email, string Password, string[] ExpectedPages)> RoleCredentials = new()
        {
            ["Admin"] = (AdminEmail, TestPassword, new[] { "/Admin", "/Admin/UserManagement", "/Admin/RoleManagement", "/Admin/ProductManagement" }),
            ["Analyst"] = (AnalystEmail, TestPassword, new[] { "/Certificate", "/Dashboard", "/Product" }),
            ["External"] = (ExternalEmail, TestPassword, new[] { "/Certificate", "/Dashboard" }),
            ["Client"] = (ClientEmail, TestPassword, new[] { "/Certificate", "/Dashboard" }),
            ["Partner"] = (PartnerEmail, TestPassword, new[] { "/Certificate", "/Dashboard" }),
            ["Public"] = ("", "", new[] { "/", "/Certificate/CertificateTileView" })
        };
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
        public const string Logout = "/Account/Logout";
        public const string ForgotPassword = "/Account/ForgotPassword";
        public const string ResetPassword = "/Account/ResetPassword";
        
        // Certificate URLs
        public const string Certificates = "/Certificate";
        public const string CertificateTileView = "/Certificate/CertificateTileView";
        public const string CertificateDetails = "/Certificate/Details";
        public const string CertificateAdd = "/Certificate/Add";
        public const string CertificateEdit = "/Certificate/Edit";
        
        // Admin URLs
        public const string Admin = "/Admin";
        public const string AdminUserManagement = "/Admin/UserManagement";
        public const string AdminRoleManagement = "/Admin/RoleManagement";
        public const string AdminProductManagement = "/Admin/ProductManagement";
        public const string AdminCertificateTypes = "/Admin/CertificateTypes";
        public const string AdminCSFAttributes = "/Admin/CSFAttributes";
        public const string AdminTags = "/Admin/Tags";
        public const string AdminPartners = "/Admin/Partners";
        
        // Dashboard and other features
        public const string Dashboard = "/Dashboard";
        public const string Products = "/Product";
        public const string ProductManagement = "/Product/Management";
        
        // Company URLs
        public const string Companies = "/Company";
        public const string CompanyIndex = "/Company/Index";
        public const string CompanyManagement = "/Company/Management";
    }

    public static class WaitTimes
    {
        public const int Short = 2000;   // 2 seconds
        public const int Medium = 5000;  // 5 seconds  
        public const int Long = 10000;   // 10 seconds
        public const int Navigation = 3000; // 3 seconds for page navigation
        public const int FormSubmission = 4000; // 4 seconds for form submissions
    }
    
    public static class Selectors
    {
        // Login page selectors
        public const string EmailInput = "#Email";
        public const string PasswordInput = "#Password";
        public const string LoginButton = "button[type='submit']";
        public const string ForgotPasswordLink = "a[href*='ForgotPassword']";
        
        // Navigation selectors
        public const string LogoutButton = "a[href*='Logout']";
        public const string AdminMenu = "a[href*='/Admin']";
        public const string CertificatesMenu = "a[href*='/Certificate']";
        
        // Certificate page selectors
        public const string SearchInput = "input[name='search'], #search, .search-input";
        public const string SearchButton = ".search-btn, button[type='submit'], #searchBtn";
        public const string FilterDropdown = ".filter-dropdown, select.filter";
        public const string CertificateCard = ".certificate-card, .cert-tile, .certificate-item";
        public const string ViewToggle = ".view-toggle, .toggle-view";
        
        // Admin page selectors
        public const string AddButton = ".add-btn, .btn-add, button[href*='Add']";
        public const string EditButton = ".edit-btn, .btn-edit, a[href*='Edit']";
        public const string DeleteButton = ".delete-btn, .btn-delete";
        public const string SaveButton = ".save-btn, .btn-save, button[type='submit']";
    }
    
    public static class TestScenarios
    {
        public static readonly Dictionary<string, string[]> RoleBasedWorkflows = new()
        {
            ["Admin"] = new[] 
            {
                "Complete user management workflow",
                "Manage certificate types and CSF attributes", 
                "Product category management",
                "Partner and tag management",
                "System configuration"
            },
            ["Analyst"] = new[]
            {
                "Certificate search and filtering",
                "Certificate data entry and editing",
                "Bulk certificate operations",
                "Report generation",
                "Tag assignment"
            },
            ["External"] = new[]
            {
                "Company-specific certificate access",
                "Certificate search within permissions",
                "Download certificates",
                "Export company data"
            },
            ["Client"] = new[]
            {
                "View company certificates",
                "Track expiration dates",
                "Download certificate files",
                "Filter by status and type"
            },
            ["Partner"] = new[]
            {
                "Program-specific certificate access",
                "Partner data management",
                "Certificate verification",
                "Export program data"
            },
            ["Public"] = new[]
            {
                "Browse public certificates",
                "Search without login",
                "View certificate details",
                "Switch between tile and list views"
            }
        };
    }
}