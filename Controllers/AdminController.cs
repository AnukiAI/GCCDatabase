using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using System.Text.Json;

namespace GCCWebApp.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin Dashboard
        public IActionResult Index()
        {
            var dashboardModel = new AdminDashboardViewModel
            {
                TotalUsers = 45,
                TotalCompanies = 12,
                TotalCertificates = 156,
                TotalProducts = 89,
                ActiveUsers = 38,
                PendingCertificates = 7,
                LastDataUpdate = DateTime.Now,
                RecentActivity = new List<string>
                {
                    "New user Sarah Johnson added",
                    "Certificate updated for Product ABC",
                    "New company ASSA ABLOY registered",
                    "Tag 'Sustainable Materials' updated"
                }
            };
            
            return View(dashboardModel);
        }

        // User Management
        public IActionResult UserManagement()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            // Sample data - in real app, fetch from database
            var users = new List<UserViewModel>
            {
                new UserViewModel 
                { 
                    Id = 1, 
                    Name = "Sarah Johnson", 
                    Email = "sarah.johnson@assaabloy.com", 
                    Role = "admin", 
                    Status = "active",
                    CompanyNames = new List<string> { "ASSA ABLOY" },
                    PartnerNames = new List<string> { "Amazon" },
                    LastLogin = DateTime.Now.AddHours(-2)
                },
                new UserViewModel 
                { 
                    Id = 2, 
                    Name = "Mike Chen", 
                    Email = "mike.chen@greencircle.com", 
                    Role = "user", 
                    Status = "active",
                    CompanyNames = new List<string> { "GreenCircle" },
                    PartnerNames = new List<string> { "Amazon", "Wayfair" },
                    LastLogin = DateTime.Now.AddDays(-1)
                }
            };

            return Json(new ApiResponse<List<UserViewModel>> { Success = true, Data = users });
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // In real app: save to database
            // For demo: return success
            return Json(new ApiResponse<object> { Success = true, Message = "User created successfully" });
        }

        [HttpPut]
        public IActionResult UpdateUser(int id, [FromBody] UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // In real app: update in database
            return Json(new ApiResponse<object> { Success = true, Message = "User updated successfully" });
        }

        [HttpDelete]
        public IActionResult DeleteUser(int id)
        {
            // In real app: delete from database
            return Json(new ApiResponse<object> { Success = true, Message = "User deleted successfully" });
        }

        // Role Management
        public IActionResult RoleManagement()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetRoles()
        {
            var roles = new List<RoleViewModel>
            {
                new RoleViewModel 
                { 
                    Id = 1, 
                    Name = "Admin", 
                    Description = "Full system access",
                    Permissions = new List<string> { "view-certificates", "edit-certificates", "delete-certificates", "manage-users", "admin-access" },
                    UserCount = 2
                },
                new RoleViewModel 
                { 
                    Id = 2, 
                    Name = "User", 
                    Description = "Standard user access",
                    Permissions = new List<string> { "view-certificates", "edit-certificates" },
                    UserCount = 15
                }
            };

            return Json(new ApiResponse<List<RoleViewModel>> { Success = true, Data = roles });
        }

        [HttpGet]
        public IActionResult GetPermissions()
        {
            var permissions = new List<PermissionViewModel>
            {
                new PermissionViewModel { Name = "view-certificates", DisplayName = "View Certificates", Category = "Certificates" },
                new PermissionViewModel { Name = "edit-certificates", DisplayName = "Edit Certificates", Category = "Certificates" },
                new PermissionViewModel { Name = "delete-certificates", DisplayName = "Delete Certificates", Category = "Certificates" },
                new PermissionViewModel { Name = "export-certificates", DisplayName = "Export Certificates", Category = "Certificates" },
                new PermissionViewModel { Name = "manage-users", DisplayName = "Manage Users", Category = "User Management" },
                new PermissionViewModel { Name = "manage-companies", DisplayName = "Manage Companies", Category = "Company Management" },
                new PermissionViewModel { Name = "view-analytics", DisplayName = "View Analytics", Category = "Analytics" },
                new PermissionViewModel { Name = "admin-access", DisplayName = "Admin Access", Category = "System" }
            };

            return Json(new ApiResponse<List<PermissionViewModel>> { Success = true, Data = permissions });
        }

        // Google Analytics
        public IActionResult GoogleAnalytics()
        {
            var model = new GoogleAnalyticsViewModel
            {
                TrackingId = "G-XXXXXXXXXX",
                IsEnabled = true,
                TrackPageViews = true,
                TrackEvents = true,
                TrackConversions = false,
                LastUpdated = DateTime.Now.AddDays(-5),
                UpdatedBy = "Admin User"
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult SaveGoogleAnalytics([FromBody] GoogleAnalyticsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // In real app: save to database
            return Json(new ApiResponse<object> { Success = true, Message = "Google Analytics settings saved successfully" });
        }

        // Product Types
        public IActionResult ProductTypes()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetProductTypes()
        {
            var productTypes = new List<ProductTypeViewModel>
            {
                new ProductTypeViewModel { Id = 1, Name = "Construction & Building Materials", Level = 1, SortOrder = 1 },
                new ProductTypeViewModel { Id = 2, Name = "Adhesives & Sealants", ParentId = 1, Level = 2, SortOrder = 1, ParentName = "Construction & Building Materials" },
                new ProductTypeViewModel { Id = 3, Name = "Building Materials", ParentId = 1, Level = 2, SortOrder = 2, ParentName = "Construction & Building Materials" },
                new ProductTypeViewModel { Id = 4, Name = "Paper & Packaging", Level = 1, SortOrder = 2 },
                new ProductTypeViewModel { Id = 5, Name = "Corrugated Products", ParentId = 4, Level = 2, SortOrder = 1, ParentName = "Paper & Packaging" }
            };

            return Json(new ApiResponse<List<ProductTypeViewModel>> { Success = true, Data = productTypes });
        }

        // Certificate Types
        public IActionResult CertificateTypes()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetCertificateTypes()
        {
            var certificateTypes = new List<CertificateTypeViewModel>
            {
                new CertificateTypeViewModel { Id = 1, Name = "Environmental Product Declaration", Description = "EPD certification", IsActive = true, CertificateCount = 25 },
                new CertificateTypeViewModel { Id = 2, Name = "GREENGUARD Certified", Description = "Low chemical emissions", IsActive = true, CertificateCount = 18 },
                new CertificateTypeViewModel { Id = 3, Name = "Cradle to Cradle", Description = "Circular economy certification", IsActive = true, CertificateCount = 12 }
            };

            return Json(new ApiResponse<List<CertificateTypeViewModel>> { Success = true, Data = certificateTypes });
        }

        // Certificate Fields
        public IActionResult CertificateFields()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetCertificateFields()
        {
            var fields = new List<CertificateFieldViewModel>
            {
                new CertificateFieldViewModel { Id = 1, Name = "Certificate Number", FieldType = "text", IsRequired = true, IsVisible = true, SortOrder = 1 },
                new CertificateFieldViewModel { Id = 2, Name = "Issue Date", FieldType = "date", IsRequired = true, IsVisible = true, SortOrder = 2 },
                new CertificateFieldViewModel { Id = 3, Name = "Expiry Date", FieldType = "date", IsRequired = false, IsVisible = true, SortOrder = 3 },
                new CertificateFieldViewModel { Id = 4, Name = "Certification Body", FieldType = "select", IsRequired = true, IsVisible = true, SortOrder = 4, Options = "UL,SGS,TUV,Bureau Veritas" }
            };

            return Json(new ApiResponse<List<CertificateFieldViewModel>> { Success = true, Data = fields });
        }

        // CSF Attributes
        public IActionResult CSFAttributes()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetCSFAttributes()
        {
            var attributes = new List<CSFAttributeViewModel>
            {
                new CSFAttributeViewModel { Id = 1, Name = "Biobased", Description = "Made from renewable biological resources", IsActive = true },
                new CSFAttributeViewModel { Id = 2, Name = "Biodegradable", Description = "Capable of being decomposed by bacteria", IsActive = true },
                new CSFAttributeViewModel { Id = 3, Name = "Carbon Footprint Reduction", Description = "Reduces carbon emissions", IsActive = true },
                new CSFAttributeViewModel { Id = 4, Name = "Certified Energy Savings", Description = "Verified energy efficiency improvements", IsActive = true }
            };

            return Json(new ApiResponse<List<CSFAttributeViewModel>> { Success = true, Data = attributes });
        }

        // Tags
        public IActionResult Tags()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetTags()
        {
            var tags = new List<TagViewModel>
            {
                new TagViewModel { Id = 1, Name = "Biodegradable", TagType = "Internal", ProductCount = 17 },
                new TagViewModel { Id = 2, Name = "Carbon Neutral", TagType = "External", ProductCount = 22 },
                new TagViewModel { Id = 3, Name = "Energy Efficient", TagType = "External", ProductCount = 28 },
                new TagViewModel { Id = 4, Name = "Post-Consumer Recycled", TagType = "External", ProductCount = 29 },
                new TagViewModel { Id = 5, Name = "Sustainable Materials", TagType = "External", ProductCount = 42 }
            };

            return Json(new ApiResponse<List<TagViewModel>> { Success = true, Data = tags });
        }

        // Partners
        public IActionResult Partners()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetPartners()
        {
            var partners = new List<PartnerViewModel>
            {
                new PartnerViewModel { Id = 1, Name = "Amazon", Description = "E-commerce marketplace", Website = "amazon.com", IsActive = true, UserCount = 15, CreatedDate = DateTime.Now.AddMonths(-6) },
                new PartnerViewModel { Id = 2, Name = "Wayfair", Description = "Online furniture retailer", Website = "wayfair.com", IsActive = true, UserCount = 8, CreatedDate = DateTime.Now.AddMonths(-3) },
                new PartnerViewModel { Id = 3, Name = "Other", Description = "Other retail partners", Website = "", IsActive = true, UserCount = 2, CreatedDate = DateTime.Now.AddMonths(-12) }
            };

            return Json(new ApiResponse<List<PartnerViewModel>> { Success = true, Data = partners });
        }

        // Product Management
        public IActionResult ProductManagement()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = new List<ProductViewModel>
            {
                new ProductViewModel { Id = 1, Name = "Product 1", Description = "Sample product", ProductTypeName = "Adhesives", CompanyName = "ASSA ABLOY", IsActive = true, CertificateCount = 3, CreatedDate = DateTime.Now.AddMonths(-2) },
                new ProductViewModel { Id = 2, Name = "Product 2", Description = "Another product", ProductTypeName = "Building Materials", CompanyName = "GreenCircle", IsActive = true, CertificateCount = 5, CreatedDate = DateTime.Now.AddMonths(-1) }
            };

            return Json(new ApiResponse<List<ProductViewModel>> { Success = true, Data = products });
        }

        // Amazon API
        public IActionResult AmazonAPI()
        {
            var model = new AmazonAPISettingsViewModel
            {
                AccessKeyId = "AKIA****************",
                SecretAccessKey = "************************************",
                Region = "us-east-1",
                MarketplaceId = "ATVPDKIKX0DER",
                IsEnabled = true,
                LastSync = DateTime.Now.AddHours(-6),
                LastSyncStatus = "Success",
                SyncIntervalHours = 24
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult SaveAmazonAPISettings([FromBody] AmazonAPISettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            // In real app: save to database and test connection
            return Json(new ApiResponse<object> { Success = true, Message = "Amazon API settings saved successfully" });
        }

        // Lookup data for dropdowns
        [HttpGet]
        public IActionResult GetCompanies()
        {
            var companies = new List<LookupItem>
            {
                new LookupItem { Id = 1, Name = "ASSA ABLOY", IsActive = true },
                new LookupItem { Id = 2, Name = "GreenCircle", IsActive = true },
                new LookupItem { Id = 3, Name = "Test Company", IsActive = true }
            };

            return Json(new ApiResponse<List<LookupItem>> { Success = true, Data = companies });
        }

        [HttpGet]
        public IActionResult GetRolesList()
        {
            var roles = new List<LookupItem>
            {
                new LookupItem { Id = 1, Name = "admin", Description = "Administrator" },
                new LookupItem { Id = 2, Name = "user", Description = "Standard User" },
                new LookupItem { Id = 3, Name = "viewer", Description = "Read-only User" }
            };

            return Json(new ApiResponse<List<LookupItem>> { Success = true, Data = roles });
        }

        // Generic CRUD operations for simple entities
        [HttpPost]
        public IActionResult CreateEntity(string entityType, [FromBody] object data)
        {
            // Generic create method - in real app, route to specific service based on entityType
            return Json(new ApiResponse<object> { Success = true, Message = $"{entityType} created successfully" });
        }

        [HttpPut]
        public IActionResult UpdateEntity(string entityType, int id, [FromBody] object data)
        {
            // Generic update method
            return Json(new ApiResponse<object> { Success = true, Message = $"{entityType} updated successfully" });
        }

        [HttpDelete]
        public IActionResult DeleteEntity(string entityType, int id)
        {
            // Generic delete method
            return Json(new ApiResponse<object> { Success = true, Message = $"{entityType} deleted successfully" });
        }

        // Export functionality
        [HttpGet]
        public IActionResult ExportData(string entityType, string format = "csv")
        {
            // In real app: generate actual export file
            var fileName = $"{entityType}_{DateTime.Now:yyyyMMdd}.{format}";
            
            // Return sample CSV content
            var content = "Name,Type,Status\nSample Data,Test,Active";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            
            return File(bytes, "text/csv", fileName);
        }

        // Legacy action for existing view
        public IActionResult CreateUser()
        {
            return View();
        }
    }
}