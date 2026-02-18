namespace GCCWebApp.UAT.Tests.Helpers;

public static class TestDataHelper
{
    public static class Users
    {
        // Development uses USERNAME for login, not email
        public const string AdminEmail = "admin";
        public const string AnalystEmail = "analyst";
        public const string ExternalEmail = "external";
        public const string ClientEmail = "external";  // Uses external user in dev
        public const string PartnerEmail = "external";  // Uses external user in dev
        public const string PublicEmail = "public.user";
        public const string TestPassword = "TestPass123!";

        public static readonly Dictionary<string, (string Email, string Password, string[] ExpectedPages)> RoleCredentials = new()
        {
            ["Admin"] = (AdminEmail, TestPassword, new[] { "/Admin", "/Admin/UserManagement", "/Admin/RoleManagement", "/Admin/ProductManagement" }),
            ["Analyst"] = (AnalystEmail, TestPassword, new[] { "/Certificate", "/Dashboard", "/Product" }),
            ["External"] = (ExternalEmail, TestPassword, new[] { "/Certificate", "/Certificate/CertificateTileView" }),
            ["Client"] = (ClientEmail, TestPassword, new[] { "/Certificate", "/Certificate/CertificateTileView" }),
            ["Partner"] = (PartnerEmail, TestPassword, new[] { "/Certificate", "/Certificate/CertificateTileView" }),
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
        public const string LogoutButton = "a[href*='Logout'], .logout-form button";
        public const string AdminMenu = "a[href*='/Admin']";
        public const string CertificatesMenu = "a[href*='/Certificate']";

        // Certificate page selectors
        public const string SearchInput = "#search-input, .admin-search-input, input[name='search'], #search";
        public const string SearchButton = ".search-btn, button[type='submit'], #searchBtn";
        public const string FilterDropdown = ".admin-filter-select, .filter-dropdown, select.filter";
        public const string CertificateCard = ".certificate-card, .cert-tile, .certificate-item";
        public const string ViewToggle = ".view-toggle, .toggle-view";

        // Admin page selectors - Updated to match actual UI
        public const string AdminContainer = ".admin-container";
        public const string AdminTable = "#user-table, .admin-table, table";
        public const string AdminTableBody = ".admin-table-body, tbody";
        public const string AdminCard = ".admin-card, .stat-card";
        public const string AdminHeader = ".admin-header";
        public const string AdminContent = ".admin-content, .admin-body-content";

        // Admin action buttons
        public const string AddButton = "#add-user-btn, .admin-btn--primary:has-text('Add'), button:has-text('Add New')";
        public const string AddUserButton = "#add-user-btn";
        public const string EditButton = ".admin-action-edit, button:has-text('Edit'), a:has-text('Edit')";
        public const string DeleteButton = ".admin-action-delete, button:has-text('Delete')";
        public const string SaveButton = "button[type='submit'], .admin-btn--primary:has-text('Save')";
        public const string ExportButton = "#export-btn, button:has-text('Export')";
        public const string ImportButton = "#import-btn, button:has-text('Import')";
        public const string TemplateButton = "#template-btn, button:has-text('Template')";

        // Dashboard selectors
        public const string DashboardTitle = ".dashboard-title, .admin-page-subtitle";
        public const string DashboardStats = ".stats-grid, .admin-dashboard-cards";
        public const string StatCard = ".stat-card, .admin-card";

        // User Management specific
        public const string UserTable = "#user-table";
        public const string UserTableRow = ".admin-table-body tr";
        public const string StatusFilter = "#status-filter";
        public const string RoleFilter = "#role-filter";
        public const string CompanyFilter = "#company-filter";

        // 2FA selectors
        public const string TwoFADropdown = ".dropdown-menu, .twofa-dropdown";
        public const string TwoFAEnableButton = "button:has-text('Enable 2FA'), .enable-2fa";
        public const string TwoFADisableButton = "button:has-text('Disable 2FA'), .disable-2fa";
        public const string TwoFAResetButton = "button:has-text('Reset 2FA'), .reset-2fa";

        // Modal selectors
        public const string Modal = ".modal, .admin-modal, [role='dialog']";
        public const string ModalBackdrop = ".modal-backdrop, .admin-modal-backdrop";
        public const string ModalClose = ".modal-close, .admin-modal-close, button:has-text('Close')";
        public const string ModalConfirm = ".modal-confirm, button:has-text('Confirm'), button:has-text('Yes')";
        public const string ModalCancel = ".modal-cancel, button:has-text('Cancel'), button:has-text('No')";

        // Form selectors
        public const string FormInput = "input.form-control, .admin-input";
        public const string FormSelect = "select.form-control, .admin-select";
        public const string ValidationError = ".field-validation-error, .text-danger, .validation-summary-errors";
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