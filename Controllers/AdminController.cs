using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GCCWebApp.Models;
using GCCWebApp.Services;
using System.Text.Json;

namespace GCCWebApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // GET: Admin Dashboard
        public async Task<IActionResult> Index()
        {
            var dashboardModel = await _adminService.GetDashboardDataAsync();
            return View(dashboardModel);
        }

        // User Management
        public IActionResult UserManagement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _adminService.GetUsersAsync();
                return Json(new ApiResponse<List<UserViewModel>> { Success = true, Data = users });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<UserViewModel>> 
                { 
                    Success = false, 
                    Message = "Error retrieving users: " + ex.Message 
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserViewModel model)
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

            try
            {
                var success = await _adminService.CreateUserAsync(model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "User created successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Failed to create user" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating user: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserViewModel model)
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

            try
            {
                var success = await _adminService.UpdateUserAsync(id, model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "User updated successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "User not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating user: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var success = await _adminService.DeleteUserAsync(id);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "User deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "User not found or delete failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting user: " + ex.Message });
            }
        }

        // Role Management
        public IActionResult RoleManagement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _adminService.GetRolesAsync();
                return Json(new ApiResponse<List<RoleViewModel>> { Success = true, Data = roles });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<RoleViewModel>> 
                { 
                    Success = false, 
                    Message = "Error retrieving roles: " + ex.Message 
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPermissions()
        {
            try
            {
                var permissions = await _adminService.GetPermissionsAsync();
                return Json(new ApiResponse<List<PermissionViewModel>> { Success = true, Data = permissions });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<PermissionViewModel>> 
                { 
                    Success = false, 
                    Message = "Error retrieving permissions: " + ex.Message 
                });
            }
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
        [ValidateAntiForgeryToken]
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
        public async Task<IActionResult> GetProductTypes()
        {
            try
            {
                var productTypes = await _adminService.GetProductTypesAsync();
                return Json(new ApiResponse<List<ProductTypeViewModel>> { Success = true, Data = productTypes });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<ProductTypeViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving product types: " + ex.Message
                });
            }
        }

        // Certificate Types
        public IActionResult CertificateTypes()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCertificateTypes()
        {
            try
            {
                var certificateTypes = await _adminService.GetCertificateTypesAsync();
                return Json(new ApiResponse<List<CertificateTypeViewModel>> { Success = true, Data = certificateTypes });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<CertificateTypeViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving certificate types: " + ex.Message
                });
            }
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
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _adminService.GetProductsAsync();
                return Json(new ApiResponse<List<ProductViewModel>> { Success = true, Data = products });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<ProductViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving products: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct([FromBody] ProductViewModel model)
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

            try
            {
                var success = await _adminService.CreateProductAsync(model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Product created successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Failed to create product" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating product: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductViewModel model)
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

            try
            {
                var success = await _adminService.UpdateProductAsync(id, model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Product updated successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Product not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating product: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var success = await _adminService.DeleteProductAsync(id);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Product deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Product not found or delete failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting product: " + ex.Message });
            }
        }

        // GCC API
        public IActionResult GCCAPI()
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
        [ValidateAntiForgeryToken]
        public IActionResult SaveGCCAPISettings([FromBody] AmazonAPISettingsViewModel model)
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
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _adminService.GetCompaniesAsync();
                return Json(new ApiResponse<List<LookupItem>> { Success = true, Data = companies });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<LookupItem>>
                {
                    Success = false,
                    Message = "Error retrieving companies: " + ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRolesList()
        {
            try
            {
                var roles = await _adminService.GetRolesListAsync();
                return Json(new ApiResponse<List<LookupItem>> { Success = true, Data = roles });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<LookupItem>>
                {
                    Success = false,
                    Message = "Error retrieving roles: " + ex.Message
                });
            }
        }

        // Generic CRUD operations for simple entities
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public async Task<IActionResult> ExportData(string entityType, string format = "csv")
        {
            try
            {
                var bytes = await _adminService.ExportDataAsync(entityType, format);
                var fileName = $"{entityType}_{DateTime.Now:yyyyMMdd}.{format}";
                
                var contentType = format.ToLower() switch
                {
                    "csv" => "text/csv",
                    "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "text/plain"
                };
                
                return File(bytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                // Return error as plain text file
                var errorContent = $"Error exporting {entityType}: {ex.Message}";
                var errorBytes = System.Text.Encoding.UTF8.GetBytes(errorContent);
                return File(errorBytes, "text/plain", $"error_{DateTime.Now:yyyyMMdd}.txt");
            }
        }

        // Legacy action for existing view
        public IActionResult CreateUser()
        {
            return View();
        }
    }
}